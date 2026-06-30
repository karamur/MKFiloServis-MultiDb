using System.Globalization;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using MKFiloServis.Web.Services.Calculation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace MKFiloServis.Web.Components.Pages.Personel;

public partial class PuantajExcelGrid
{
    // ── State ──────────────────────────────────────────────────────
    private List<Firma> _firmalar = new();
    private List<PuantajGridSatir> satirlar = new();
    private int _firmaId = 1;
    private int _yil = DateTime.Today.Year;
    private int _ay = DateTime.Today.Month;
    private int _gunSayisi => DateTime.DaysInMonth(_yil, _ay);
    private bool _yukleniyor;
    private bool _kaydediliyor;
    private bool _degisiklikVar;
    private string? _mesaj;
    private bool _mesajHata;

    // Aktif hücre
    private int _aktifSatirIdx = -1;
    private int _aktifGun = -1;

    // Undo/Redo stack
    private readonly Stack<PuantajGridState> _undoStack = new();
    private readonly Stack<PuantajGridState> _redoStack = new();

    // Seçim state — Shift+Click ile aralık seçimi
    private int _secimAnchorSatir = -1;
    private int _secimAnchorGun = -1;
    private bool _shiftHeld;

    // Grid container ref
    private ElementReference _gridContainer;

    // Modallar
    private bool _showSutunModal;
    private int _sutunGun;
    private bool _showSatirModal;
    private int _satirModalIdx = -1;

    // Hücre input debounce — blur'da save state
    private int _hucreEditOncekiDeger;

    // ── Lifecycle ──────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        _firmalar = await context.Firmalar.AsNoTracking().Where(f => !f.IsDeleted).OrderBy(f => f.FirmaAdi).ToListAsync();
        if (_firmalar.Any())
            _firmaId = _firmalar.First().Id;
        await Yukle();
    }

    // ── Veri Yükleme ───────────────────────────────────────────────
    private async Task Yukle()
    {
        _yukleniyor = true;
        _mesaj = null;
        StateHasChanged();

        try
        {
            await using var context = await DbFactory.CreateDbContextAsync();
            var kayitlar = await context.PuantajKayitlar
                .AsNoTracking()
                .Include(p => p.Sofor)
                .Include(p => p.Guzergah)
                .Include(p => p.Arac)
                .Where(p => p.IsverenFirmaId == _firmaId && p.Yil == _yil && p.Ay == _ay && !p.IsDeleted)
                .OrderBy(p => p.Guzergah != null ? p.Guzergah.GuzergahAdi : "")
                .ThenBy(p => p.Sofor != null ? p.Sofor.Ad : "")
                .ToListAsync();

            satirlar = kayitlar.Select(k => new PuantajGridSatir
            {
                KayitId = k.Id,
                KurumAdi = k.KurumAdi,
                GuzergahAdi = k.Guzergah?.GuzergahAdi ?? k.GuzergahAdi,
                SoforAdi = k.Sofor?.TamAd ?? k.SoforAdi,
                Plaka = k.Arac?.AktifPlaka ?? k.Plaka,
                BirimFiyat = k.BirimGider > 0 ? k.BirimGider : k.BirimGelir,
                Kesinti = k.GiderKesinti + k.GelirKesinti,
                Hucreler = Enumerable.Range(1, _gunSayisi).Select(gun => new PuantajHucre
                {
                    Gun = gun,
                    Deger = k.GetGunDeger(gun),
                    Mod = k.Kaynak == PuantajKaynak.Manuel ? PuantajHucreModu.Manual : PuantajHucreModu.Auto
                }).ToList()
            }).ToList();

            _undoStack.Clear();
            _redoStack.Clear();
            _degisiklikVar = false;
            _aktifSatirIdx = satirlar.Any() ? 0 : -1;
            _aktifGun = satirlar.Any() ? 1 : -1;
            _secimAnchorSatir = -1;
            _secimAnchorGun = -1;
        }
        catch (Exception ex)
        {
            _mesaj = $"Yükleme hatası: {ex.Message}";
            _mesajHata = true;
        }
        finally
        {
            _yukleniyor = false;
            StateHasChanged();
        }
    }

    // ── Hücre Stil ─────────────────────────────────────────────────
    private string GetHucreClass(PuantajHucre h)
    {
        if (h.Mod == PuantajHucreModu.Manual) return "cell-manual";
        if (h.Mod == PuantajHucreModu.Auto && h.Deger > 0) return "cell-auto";
        return "cell-empty";
    }

    // ── Hücre Tıklama / Seçim ────────────────────────────────────────
    private void HucreTikla(int satirIdx, int gun, bool fromClick)
    {
        if (_shiftHeld && _secimAnchorSatir >= 0 && _secimAnchorGun >= 0)
        {
            // Shift+Click → aralık seç
            SelectRange(_secimAnchorSatir, _secimAnchorGun, satirIdx, gun);
        }
        else if (!_shiftHeld)
        {
            // Normal tık → tek hücre seç, anchor ata
            TumSecimiTemizle();
            _secimAnchorSatir = satirIdx;
            _secimAnchorGun = gun;
        }
        _aktifSatirIdx = satirIdx;
        _aktifGun = gun;
    }

    private void SelectRange(int r1, int c1, int r2, int c2)
    {
        TumSecimiTemizle();
        int rMin = Math.Min(r1, r2), rMax = Math.Max(r1, r2);
        int cMin = Math.Min(c1, c2), cMax = Math.Max(c1, c2);
        for (int r = rMin; r <= rMax; r++)
            for (int c = cMin; c <= cMax; c++)
                satirlar[r].Hucreler[c - 1].Secili = true;
    }

    private void TumSecimiTemizle()
    {
        foreach (var s in satirlar)
            foreach (var h in s.Hucreler)
                h.Secili = false;
    }

    private void TumunuSec()
    {
        for (int r = 0; r < satirlar.Count; r++)
            for (int c = 0; c < _gunSayisi; c++)
                satirlar[r].Hucreler[c].Secili = true;
    }

    // ── Doğrudan Hücre Yazma (OnFocus / OnInput / OnBlur) ────────────
    private void HucreFocus(int satirIdx, int gun)
    {
        _aktifSatirIdx = satirIdx;
        _aktifGun = gun;
        _hucreEditOncekiDeger = satirlar[satirIdx].Hucreler[gun - 1].Deger;
        // Tek hücre seçimi (shift yoksa)
        if (!_shiftHeld)
        {
            TumSecimiTemizle();
            _secimAnchorSatir = satirIdx;
            _secimAnchorGun = gun;
        }
    }

    private void HucreInput(int satirIdx, int gun, ChangeEventArgs e)
    {
        var raw = e.Value?.ToString() ?? "";
        if (raw.Length == 0)
        {
            SetHucreDegerInternal(satirIdx, gun, 0);
            return;
        }
        // Sadece rakam kabul et, 0-2 arası sınırla
        if (int.TryParse(raw, out var val))
        {
            val = Math.Clamp(val, 0, 2);
            SetHucreDegerInternal(satirIdx, gun, val);
        }
    }

    private void HucreBlur(int satirIdx, int gun, FocusEventArgs e)
    {
        var hucre = satirlar[satirIdx].Hucreler[gun - 1];
        // Değer değiştiyse state kaydet
        if (hucre.Deger != _hucreEditOncekiDeger)
        {
            SaveState();
            _degisiklikVar = true;
            hucre.Mod = PuantajHucreModu.Manual;
        }
    }

    private async Task HucreKeyDown(int satirIdx, int gun, KeyboardEventArgs e)
    {
        var key = e.Key;
        bool ctrl = e.CtrlKey || e.MetaKey;

        // Enter → aşağı, Tab → sağa
        if (key == "Enter")
        {
            if (satirIdx + 1 < satirlar.Count)
            {
                _aktifSatirIdx = satirIdx + 1;
                _aktifGun = gun;
            }
            await FocusCell(_aktifSatirIdx, _aktifGun);
            return;
        }
        if (key == "Tab")
        {
            int nextGun = e.ShiftKey ? gun - 1 : gun + 1;
            int nextSatir = satirIdx;
            if (nextGun < 1) { nextGun = _gunSayisi; nextSatir--; }
            if (nextGun > _gunSayisi) { nextGun = 1; nextSatir++; }
            if (nextSatir >= 0 && nextSatir < satirlar.Count)
            {
                _aktifSatirIdx = nextSatir;
                _aktifGun = nextGun;
            }
            await FocusCell(_aktifSatirIdx, _aktifGun);
            return;
        }

        // Ok tuşları
        if (key == "ArrowLeft" && gun > 1) { _aktifGun = gun - 1; if (!e.ShiftKey) { TumSecimiTemizle(); _secimAnchorSatir = _aktifSatirIdx; _secimAnchorGun = _aktifGun; } await FocusCell(_aktifSatirIdx, _aktifGun); return; }
        if (key == "ArrowRight" && gun < _gunSayisi) { _aktifGun = gun + 1; if (!e.ShiftKey) { TumSecimiTemizle(); _secimAnchorSatir = _aktifSatirIdx; _secimAnchorGun = _aktifGun; } await FocusCell(_aktifSatirIdx, _aktifGun); return; }
        if (key == "ArrowUp" && satirIdx > 0) { _aktifSatirIdx = satirIdx - 1; _aktifGun = gun; if (!e.ShiftKey) { TumSecimiTemizle(); _secimAnchorSatir = _aktifSatirIdx; _secimAnchorGun = _aktifGun; } await FocusCell(_aktifSatirIdx, _aktifGun); return; }
        if (key == "ArrowDown" && satirIdx < satirlar.Count - 1) { _aktifSatirIdx = satirIdx + 1; _aktifGun = gun; if (!e.ShiftKey) { TumSecimiTemizle(); _secimAnchorSatir = _aktifSatirIdx; _secimAnchorGun = _aktifGun; } await FocusCell(_aktifSatirIdx, _aktifGun); return; }

        // Değer girişi (0-2) — input'a bırak, oninput handles
        if (key is "0" or "1" or "2" or "Backspace" or "Delete")
            return; // input doğal işlesin

        // Ctrl+C / Ctrl+V / Ctrl+Z / Ctrl+Y / Ctrl+A → container'a bırak
    }

    private async Task FocusCell(int satirIdx, int gun)
    {
        // JS interop ile input'a focus
        await JS.InvokeVoidAsync("eval",
            $"document.querySelector('input[data-row=\"{satirIdx}\"][data-gun=\"{gun}\"]')?.focus()");
        StateHasChanged();
    }

    private void SetHucreDegerInternal(int satirIdx, int gun, int deger)
    {
        if (satirIdx < 0 || satirIdx >= satirlar.Count) return;
        if (gun < 1 || gun > _gunSayisi) return;
        deger = Math.Clamp(deger, 0, 2);
        satirlar[satirIdx].Hucreler[gun - 1].Deger = deger;
    }

    // ── Klavye (Grid Container) ────────────────────────────────────
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (!satirlar.Any()) return;

        bool ctrl = e.CtrlKey || e.MetaKey;
        _shiftHeld = e.ShiftKey;

        switch (e.Key)
        {
            case "Delete" when !ctrl:
                SaveState();
                bool anySelected = satirlar.Any(s => s.Hucreler.Any(h => h.Secili));
                if (anySelected)
                {
                    foreach (var s in satirlar)
                        foreach (var h in s.Hucreler.Where(h => h.Secili))
                        { h.Deger = 0; h.Mod = PuantajHucreModu.Manual; }
                }
                else if (_aktifSatirIdx >= 0 && _aktifGun >= 1)
                {
                    var h = satirlar[_aktifSatirIdx].Hucreler[_aktifGun - 1];
                    h.Deger = 0; h.Mod = PuantajHucreModu.Manual;
                }
                _degisiklikVar = true;
                break;

            case "c" or "C" when ctrl:
                await CopyToClipboard();
                break;

            case "v" or "V" when ctrl:
                await PasteFromClipboard();
                break;

            case "z" or "Z" when ctrl:
                Undo();
                break;

            case "y" or "Y" when ctrl:
                Redo();
                break;

            case "a" or "A" when ctrl:
                TumunuSec();
                break;
        }
    }

    // ── Copy/Paste ─────────────────────────────────────────────────
    private async Task CopyToClipboard()
    {
        var selected = new List<(int r, int c, PuantajHucre h)>();
        for (int r = 0; r < satirlar.Count; r++)
            for (int c = 0; c < _gunSayisi; c++)
                if (satirlar[r].Hucreler[c].Secili)
                    selected.Add((r, c, satirlar[r].Hucreler[c]));

        if (!selected.Any())
        {
            if (_aktifSatirIdx >= 0 && _aktifGun >= 1)
                selected.Add((_aktifSatirIdx, _aktifGun - 1, satirlar[_aktifSatirIdx].Hucreler[_aktifGun - 1]));
            else return;
        }

        int minR = selected.Min(x => x.r), minC = selected.Min(x => x.c);
        int maxR = selected.Max(x => x.r), maxC = selected.Max(x => x.c);

        var sb = new System.Text.StringBuilder();
        for (int r = minR; r <= maxR; r++)
        {
            var rowVals = new List<string>();
            for (int c = minC; c <= maxC; c++)
            {
                var found = selected.FirstOrDefault(x => x.r == r && x.c == c);
                rowVals.Add(found.h?.Deger.ToString() ?? "");
            }
            sb.AppendLine(string.Join("\t", rowVals));
        }

        var text = sb.ToString().TrimEnd();
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
        _mesaj = $"{maxR - minR + 1}×{maxC - minC + 1} hücre kopyalandı";
        _mesajHata = false;
    }

    private async Task PasteFromClipboard()
    {
        try
        {
            var text = await JS.InvokeAsync<string>("navigator.clipboard.readText");
            if (string.IsNullOrWhiteSpace(text)) return;

            SaveState();
            var rows = text.TrimEnd().Split('\n');
            int startR = Math.Max(0, _aktifSatirIdx);
            int startC = Math.Max(0, _aktifGun - 1);

            TumSecimiTemizle();
            for (int i = 0; i < rows.Length && startR + i < satirlar.Count; i++)
            {
                var cols = rows[i].TrimEnd('\r').Split('\t');
                for (int j = 0; j < cols.Length && startC + j < _gunSayisi; j++)
                {
                    if (int.TryParse(cols[j], out var val))
                    {
                        val = Math.Clamp(val, 0, 2);
                        var h = satirlar[startR + i].Hucreler[startC + j];
                        h.Deger = val;
                        h.Mod = PuantajHucreModu.Manual;
                        h.Secili = true;
                    }
                }
            }
            _degisiklikVar = true;
            _mesaj = "Yapıştırıldı";
            _mesajHata = false;
        }
        catch
        {
            _mesaj = "Yapıştırma başarısız. Manuel yapıştırın veya konsoldan izin verin.";
            _mesajHata = true;
        }
    }

    // ── Undo/Redo ──────────────────────────────────────────────────
    private void SaveState()
    {
        var state = new PuantajGridState
        {
            Satirlar = satirlar.Select(s => s.Clone()).ToList()
        };
        _undoStack.Push(state);
        _redoStack.Clear();
        while (_undoStack.Count > 50) { /* limit */ }
    }

    private void Undo()
    {
        if (!_undoStack.Any()) return;
        _redoStack.Push(new PuantajGridState { Satirlar = satirlar.Select(s => s.Clone()).ToList() });
        satirlar = _undoStack.Pop().Satirlar;
        _degisiklikVar = true;
        StateHasChanged();
    }

    private void Redo()
    {
        if (!_redoStack.Any()) return;
        _undoStack.Push(new PuantajGridState { Satirlar = satirlar.Select(s => s.Clone()).ToList() });
        satirlar = _redoStack.Pop().Satirlar;
        _degisiklikVar = true;
        StateHasChanged();
    }

    // ── Seçili Hücrelere Toplu Değer ────────────────────────────────
    private void SeciliHuclereSet(int deger)
    {
        deger = Math.Clamp(deger, 0, 2);
        var anySelected = satirlar.Any(s => s.Hucreler.Any(h => h.Secili));
        if (!anySelected)
        {
            _mesaj = "Önce hücre seçin (Shift+Click veya Ctrl+A)";
            _mesajHata = true;
            return;
        }
        SaveState();
        foreach (var s in satirlar)
            foreach (var h in s.Hucreler.Where(h => h.Secili))
            { h.Deger = deger; h.Mod = PuantajHucreModu.Manual; }
        _degisiklikVar = true;
        _mesaj = $"Seçili hücreler → {deger}";
        _mesajHata = false;
    }

    // ── Toplu Satır İşlemleri ───────────────────────────────────────
    private void SatirTopluAc(int satirIdx)
    {
        _satirModalIdx = satirIdx;
        _showSatirModal = true;
    }

    private void SatirSecimToggle(int idx, ChangeEventArgs e)
    {
        if (idx >= 0 && idx < satirlar.Count)
            satirlar[idx].IsSelected = e.Value is bool b && b;
    }

    private void TumSatirlariSecToggle(ChangeEventArgs e)
    {
        bool sec = e.Value is bool b && b;
        foreach (var s in satirlar) s.IsSelected = sec;
    }

    private void TopluSatirUygula(string mod)
    {
        // 🔴 SCOPE: Seçili satırlar → tek satır fallback → aktif satır
        var targetRows = satirlar.Where(s => s.IsSelected).ToList();
        if (!targetRows.Any() && _satirModalIdx >= 0 && _satirModalIdx < satirlar.Count)
            targetRows.Add(satirlar[_satirModalIdx]);
        if (!targetRows.Any()) return;

        SaveState();
        foreach (var satir in targetRows)
        {
            for (int gun = 1; gun <= _gunSayisi; gun++)
            {
                var tarih = new DateTime(_yil, _ay, gun);
                bool haftaIci = tarih.DayOfWeek != DayOfWeek.Saturday && tarih.DayOfWeek != DayOfWeek.Sunday;
                var h = satir.Hucreler[gun - 1];

                switch (mod)
                {
                    case "haftaici_calisir":
                        if (haftaIci) { h.Deger = 1; h.Mesai = 0; h.EkSefer = 0; h.Mod = PuantajHucreModu.Manual; }
                        else { h.Deger = 0; h.Mesai = 0; h.EkSefer = 0; h.Mod = PuantajHucreModu.Manual; }
                        break;
                    case "tum_calisir":
                        h.Deger = 1; h.Mesai = 0; h.EkSefer = 0; h.Mod = PuantajHucreModu.Manual;
                        break;
                    case "haftaici_izin":
                        h.Deger = 0; h.Mesai = 0; h.EkSefer = 0; h.Mod = PuantajHucreModu.Manual;
                        break;
                    case "tum_izin":
                        h.Deger = 0; h.Mesai = 0; h.EkSefer = 0; h.Mod = PuantajHucreModu.Manual;
                        break;
                }
            }
        }
        _degisiklikVar = true;
    }

    private string GetAyAdi() => new DateTime(_yil, _ay, 1).ToString("MMMM", new CultureInfo("tr-TR"));

    // ── Toplu Sütun İşlemleri ──────────────────────────────────────
    private void SutunTopluDoldur(int gun)
    {
        _sutunGun = gun;
        _showSutunModal = true;
    }

    private void SutunTopluSet(int deger)
    {
        SaveState();
        deger = Math.Clamp(deger, 0, 2);
        foreach (var satir in satirlar)
        {
            var h = satir.Hucreler[_sutunGun - 1];
            h.Deger = deger;
            h.Mod = PuantajHucreModu.Manual;
        }
        _degisiklikVar = true;
        _showSutunModal = false;
    }

    // ── Kaydet ─────────────────────────────────────────────────────
    private async Task Kaydet()
    {
        _kaydediliyor = true;
        StateHasChanged();
        try
        {
            await using var context = await DbFactory.CreateDbContextAsync();

            // 🔴 Write protection: Rebuild altında veya Critical modda kayıt YAPILAMAZ
            var locked = await context.SystemLocks.AnyAsync(l => l.Key == "REBUILD" && l.IsLocked && !l.IsDeleted);
            if (locked)
                throw new InvalidOperationException("Sistem şu anda bakımda (rebuild). Lütfen biraz sonra tekrar deneyin.");

            var health = await context.SystemHealths.FirstOrDefaultAsync(h => h.FirmaId == _firmaId && h.Yil == _yil && h.Ay == _ay && !h.IsDeleted);
            if (health != null && health.Status == "Critical")
                throw new InvalidOperationException("Sistem kritik durumda — veri girişi geçici olarak durduruldu. Lütfen yöneticinize başvurun.");
            foreach (var satir in satirlar)
            {
                var kayit = await context.PuantajKayitlar.FindAsync(satir.KayitId);
                if (kayit == null) continue;

                for (int gun = 1; gun <= _gunSayisi; gun++)
                {
                    var deger = satir.Hucreler[gun - 1].Deger;
                    kayit.SetGunDeger(gun, deger);
                }

                bool anyManual = satir.Hucreler.Any(h => h.Mod == PuantajHucreModu.Manual);
                if (anyManual) kayit.Kaynak = PuantajKaynak.Manuel;

                kayit.HesaplaPuantajToplam();
                kayit.UpdatedAt = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();

            // 🔴 AI Anomaly check: anormal sefer/mesai pattern'i var mı?
            int anomaliCount = 0;
            foreach (var satir in satirlar)
            {
                var (isAnomaly, score, reason) = AIService.Predict(
                    satir.ToplamSefer, satir.Hucreler.Sum(h => h.Mesai),
                    satir.Hucreler.Sum(h => h.EkSefer), satir.BirimFiyat);
                if (isAnomaly) anomaliCount++;
            }
            if (anomaliCount > 0)
                _mesaj = $"AI uyarısı: {anomaliCount} satırda anormal pattern tespit edildi. Lütfen kontrol edin.";
            // AI block etmez, sadece uyarır

            // Faz 4: Grid → HakedisPuantaj senkronizasyonu
            try
            {
                await SyncService.SyncFromGridAsync(_firmaId, _yil, _ay, satirlar);

                // 🔴 Backend validation: Grid toplamı == HakedisPuantaj toplamı mı?
                var gridToplam = satirlar.Sum(s => s.ToplamSefer);
                var hakedisToplam = await context.HakedisPuantajlar
                    .Where(h => h.FirmaId == _firmaId && h.Yil == _yil && h.Ay == _ay && !h.IsDeleted)
                    .SumAsync(h => h.ToplamSefer);
                if (gridToplam != hakedisToplam)
                {
                    _mesaj = $"UYARI: Grid toplamı ({gridToplam}) ≠ Hakedis toplamı ({hakedisToplam}). Lütfen denetim çalıştırın.";
                    _mesajHata = true;
                }
            }
            catch { /* Non-critical */ }

            _degisiklikVar = false;
            _undoStack.Clear();
            _redoStack.Clear();
            var changedCount = satirlar.Count(s => s.IsDirty);
            var changedCells = satirlar.Sum(s => s.Hucreler.Count(h => h.IsDirty));
            _mesaj = $"{changedCount} satır ({changedCells} hücre) kaydedildi.";
            _mesajHata = false;
        }
        catch (Exception ex)
        {
            _mesaj = $"Kaydetme hatası: {ex.Message}";
            _mesajHata = true;
        }
        finally
        {
            _kaydediliyor = false;
            StateHasChanged();
        }
    }

    // ── Engine Hesapla ─────────────────────────────────────────────
    private async Task EngineHesapla()
    {
        if (!satirlar.Any()) return;
        SaveState();

        foreach (var satir in satirlar)
        {
            foreach (var hucre in satir.Hucreler)
            {
                // 🔴 ENGINE MANUAL HÜCREYE ASLA DOKUNAMAZ
                if (hucre.IsManual) continue;

                // Auto hücre: Varsayılan değer = 0 (engine override yapmaz)
                // Grid hücre değeri TEK GERÇEK
                hucre.Deger = 0;
                hucre.Mesai = 0;
                hucre.EkSefer = 0;
            }
        }

        _degisiklikVar = true;
        _mesaj = "Engine hesaplaması: Auto hücreler sıfırlandı, Manual hücreler korundu.";
        _mesajHata = false;
        StateHasChanged();
    }
}





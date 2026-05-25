using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KOAFiloServis.Web.Components.Pages.Operasyon;

public partial class OperasyonGiris : ComponentBase
{
    [Inject] private IOperasyonKaydiService OperasyonService { get; set; } = null!;
    [Inject] private IAracService AracService { get; set; } = null!;
    [Inject] private ISoforService SoforService { get; set; } = null!;
    [Inject] private IKurumService KurumService { get; set; } = null!;
    [Inject] private IGuzergahService GuzergahService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    // ── Filtre State ──────────────────────────────────────────────────────
    private DateTime seciliTarih = DateTime.Today;
    private int? seciliKurumId;
    private int? seciliGuzergahId;

    // ── Autocomplete State ─────────────────────────────────────────────────
    private string kurumArama = "";
    private List<Kurum> kurumOnerileri = new();

    // ── Lookup Listeleri ───────────────────────────────────────────────────
    private List<Arac> tumAraclar = new();
    private List<Sofor> tumSoforler = new();
    private List<Kurum> tumKurumlar = new();
    private List<Guzergah> guzergahListesi = new();

    // ── Grid State ─────────────────────────────────────────────────────────
    private List<OperasyonKaydi> operasyonlar = new();
    private readonly HashSet<OperasyonKaydi> degisikSatirlar = new();
    private bool yukleniyor;
    private bool kaydediliyor;
    private string? hataMesaji;

    // ── Yeni Kayıt Form State ──────────────────────────────────────────────
    private bool yeniKayitFormAcik;
    private OperasyonKaydi yeniKayit = null!;
    private string yeniAracArama = "";
    private List<Arac> yeniAracOnerileri = new();
    private string yeniSoforArama = "";
    private List<Sofor> yeniSoforOnerileri = new();

    // ── Computed ───────────────────────────────────────────────────────────
    private bool degisiklikVar => degisikSatirlar.Count > 0;
    private string tarihGoster => seciliTarih.ToString("dd MMMM yyyy dddd");

    protected override async Task OnInitializedAsync()
    {
        await LoadLookups();
        seciliTarih = DateTime.Today;
        await LoadOperasyonlar();
    }

    private async Task LoadLookups()
    {
        tumAraclar = await AracService.GetActiveAsync();
        tumSoforler = await SoforService.GetActiveSoforlerAsync();
        tumKurumlar = await KurumService.GetAktifAsync();
        guzergahListesi = await GuzergahService.GetActiveAsync();
    }

    // ── Filtre ────────────────────────────────────────────────────────────

    private async Task TarihDegisti(ChangeEventArgs e)
    {
        if (DateTime.TryParse(e?.Value?.ToString(), out var t))
        {
            seciliTarih = t;
            await LoadOperasyonlar();
        }
    }

    private async Task OncekiGun()
    {
        seciliTarih = seciliTarih.AddDays(-1);
        await LoadOperasyonlar();
    }

    private async Task SonrakiGun()
    {
        seciliTarih = seciliTarih.AddDays(1);
        await LoadOperasyonlar();
    }

    private async Task BuguneGit()
    {
        seciliTarih = DateTime.Today;
        await LoadOperasyonlar();
    }

    private async Task KurumAramaGuncelle(ChangeEventArgs e)
    {
        kurumArama = e?.Value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(kurumArama))
        {
            kurumOnerileri = new();
            return;
        }
        kurumOnerileri = tumKurumlar
            .Where(k => (k.KurumAdi ?? "").Contains(kurumArama, StringComparison.OrdinalIgnoreCase)
                     || (k.UnvanTam ?? "").Contains(kurumArama, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();
    }

    private async Task KurumSec(Kurum kurum)
    {
        seciliKurumId = kurum.Id;
        kurumArama = kurum.KurumAdi ?? kurum.UnvanTam ?? "";
        kurumOnerileri = new();

        // Güzergah listesini cascade
        guzergahListesi = (await GuzergahService.GetActiveAsync())
            .Where(g => g.KurumId == kurum.Id)
            .ToList();
        seciliGuzergahId = null;

        await LoadOperasyonlar();
    }

    private async Task KurumTemizle()
    {
        seciliKurumId = null;
        kurumArama = "";
        guzergahListesi = await GuzergahService.GetActiveAsync();
        seciliGuzergahId = null;
        await LoadOperasyonlar();
    }

    private async Task GuzergahDegisti(ChangeEventArgs e)
    {
        if (int.TryParse(e?.Value?.ToString(), out var id) && id > 0)
            seciliGuzergahId = id;
        else
            seciliGuzergahId = null;
        await LoadOperasyonlar();
    }

    // ── Veri Yükleme ──────────────────────────────────────────────────────

    private async Task LoadOperasyonlar()
    {
        yukleniyor = true;
        hataMesaji = null;
        degisikSatirlar.Clear();

        try
        {
            if (seciliKurumId.HasValue || seciliGuzergahId.HasValue)
            {
                operasyonlar = await OperasyonService.GetByDateRangeAsync(
                    seciliTarih, seciliTarih.AddDays(1), seciliKurumId);
            }
            else
            {
                operasyonlar = await OperasyonService.GetByDateRangeAsync(
                    seciliTarih, seciliTarih.AddDays(1));
            }

            if (seciliGuzergahId.HasValue)
                operasyonlar = operasyonlar.Where(o => o.GuzergahId == seciliGuzergahId.Value).ToList();
        }
        catch (Exception ex)
        {
            hataMesaji = ex.Message;
            operasyonlar = new();
        }
        finally
        {
            yukleniyor = false;
        }
    }

    // ── Grid Hücre Edit ────────────────────────────────────────────────────

    private void SlotDegisti(OperasyonKaydi kayit, ChangeEventArgs e)
    {
        if (Enum.TryParse<SeferSlot>(e?.Value?.ToString(), out var slot))
        {
            kayit.Slot = slot;
            MarkDirty(kayit);
        }
    }

    private void SeferSayisiDegisti(OperasyonKaydi kayit, ChangeEventArgs e)
    {
        if (int.TryParse(e?.Value?.ToString(), out var v) && v >= 0)
        {
            kayit.SeferSayisi = v;
            MarkDirty(kayit);
        }
    }

    private void DurumDegisti(OperasyonKaydi kayit, ChangeEventArgs e)
    {
        if (Enum.TryParse<OperasyonDurumu>(e?.Value?.ToString(), out var d))
        {
            kayit.OperasyonDurumu = d;
            if (d != OperasyonDurumu.Gitti)
                kayit.SeferSayisi = 0;
            else if (kayit.SeferSayisi == 0)
                kayit.SeferSayisi = 1;
            MarkDirty(kayit);
        }
    }

    private void MarkDirty(OperasyonKaydi kayit)
    {
        kayit.UpdatedAt = DateTime.UtcNow;
        degisikSatirlar.Add(kayit);
    }

    // ── Satır Sil ─────────────────────────────────────────────────────────

    private async Task SatirSil(OperasyonKaydi kayit)
    {
        if (kayit.Id > 0)
        {
            await OperasyonService.DeleteAsync(kayit.Id);
            degisikSatirlar.Remove(kayit);
        }
        operasyonlar.Remove(kayit);
        StateHasChanged();
    }

    // ── Yeni Kayıt ────────────────────────────────────────────────────────

    private void YeniKayitFormAc()
    {
        yeniKayit = new OperasyonKaydi
        {
            Tarih = seciliTarih,
            GuzergahId = seciliGuzergahId ?? 0,
            KurumId = seciliKurumId,
            Slot = SeferSlot.Sabah,
            SeferSayisi = 1,
            PuantajCarpani = 1.0m,
            OperasyonDurumu = OperasyonDurumu.Gitti,
            KaynakTipi = PlanlamaKaynakTipi.Kendi,
            FinansYonu = PlanlamaFinansYonu.Giden,
            SoforOdemeTipi = SoforOdemeTipi.Ozmal,
            Kaynak = PuantajKaynak.Manuel,
            CreatedAt = DateTime.UtcNow
        };
        yeniAracArama = "";
        yeniAracOnerileri = new();
        yeniSoforArama = "";
        yeniSoforOnerileri = new();
        yeniKayitFormAcik = true;
    }

    private void YeniKayitIptal()
    {
        yeniKayitFormAcik = false;
        yeniKayit = null!;
    }

    private void YeniAracAramaGuncelle(ChangeEventArgs e)
    {
        yeniAracArama = e?.Value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(yeniAracArama))
        {
            yeniAracOnerileri = new();
            return;
        }
        yeniAracOnerileri = tumAraclar
            .Where(a => (a.AktifPlaka ?? a.Plaka).Contains(yeniAracArama, StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToList();
    }

    private void YeniAracSec(Arac arac)
    {
        yeniKayit.AracId = arac.Id;
        yeniAracArama = arac.AktifPlaka ?? arac.Plaka;
        yeniAracOnerileri = new();
    }

    private void YeniSoforAramaGuncelle(ChangeEventArgs e)
    {
        yeniSoforArama = e?.Value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(yeniSoforArama))
        {
            yeniSoforOnerileri = new();
            return;
        }
        yeniSoforOnerileri = tumSoforler
            .Where(s => $"{s.Ad} {s.Soyad}".Contains(yeniSoforArama, StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToList();
    }

    private void YeniSoforSec(Sofor sofor)
    {
        yeniKayit.SoforId = sofor.Id;
        yeniSoforArama = $"{sofor.Ad} {sofor.Soyad}";
        yeniSoforOnerileri = new();
    }

    private void YeniSlotSec(SeferSlot slot)
    {
        yeniKayit.Slot = slot;
    }

    private async Task YeniKayitEkle()
    {
        if (yeniKayit.AracId <= 0)
        {
            hataMesaji = "Lütfen bir araç seçin.";
            return;
        }
        if (yeniKayit.GuzergahId <= 0 && seciliGuzergahId.HasValue)
            yeniKayit.GuzergahId = seciliGuzergahId.Value;
        if (yeniKayit.GuzergahId <= 0)
        {
            hataMesaji = "Lütfen bir güzergah seçin.";
            return;
        }

        operasyonlar.Add(yeniKayit);
        degisikSatirlar.Add(yeniKayit);
        yeniKayitFormAcik = false;
        hataMesaji = null;
        StateHasChanged();
    }

    // ── Toplu Kaydet ──────────────────────────────────────────────────────

    private async Task TopluKaydet()
    {
        if (!degisiklikVar) return;

        kaydediliyor = true;
        hataMesaji = null;

        try
        {
            var degisenListe = degisikSatirlar.ToList();

            // Çakışma kontrolü
            foreach (var k in degisenListe)
            {
                var ruleErrors = OperasyonKaydiBusinessRules.CheckOperationalRules(k);
                if (ruleErrors.Any())
                {
                    hataMesaji = string.Join("; ", ruleErrors);
                    kaydediliyor = false;
                    return;
                }
            }

            await OperasyonService.TopluSaveAsync(degisenListe);
            degisikSatirlar.Clear();
            ToastService.ShowSuccess($"{degisenListe.Count} operasyon kaydedildi.");
            await LoadOperasyonlar();
        }
        catch (Exception ex)
        {
            hataMesaji = ex.Message;
        }
        finally
        {
            kaydediliyor = false;
        }
    }

    // ── Slot / Durum etiketleri ────────────────────────────────────────────

    private static string SlotBadgeClass(SeferSlot slot) => slot switch
    {
        SeferSlot.Sabah => "bg-primary",
        SeferSlot.Aksam => "bg-warning text-dark",
        SeferSlot.Mesai => "bg-info text-dark",
        _ => "bg-secondary"
    };

    private static string SlotAd(SeferSlot slot) => slot switch
    {
        SeferSlot.Sabah => "Sabah",
        SeferSlot.Aksam => "Akşam",
        SeferSlot.Mesai => "Mesai",
        _ => slot.ToString()
    };

    private static string DurumBadgeClass(OperasyonDurumu d) => d switch
    {
        OperasyonDurumu.Gitti => "bg-success",
        OperasyonDurumu.Gitmedi_Mazeretli => "bg-warning text-dark",
        OperasyonDurumu.Gitmedi_Mazeretsiz => "bg-danger",
        OperasyonDurumu.Iptal_KurumTarafindan => "bg-secondary",
        _ => "bg-secondary"
    };

    private static string DurumAd(OperasyonDurumu d) => d switch
    {
        OperasyonDurumu.Gitti => "Gitti",
        OperasyonDurumu.Gitmedi_Mazeretli => "Mazeretli",
        OperasyonDurumu.Gitmedi_Mazeretsiz => "Mazeretsiz",
        OperasyonDurumu.Taksiyle_Gidildi => "Taksi",
        OperasyonDurumu.Arizalandi_YoldaKaldi => "Arıza",
        OperasyonDurumu.Iptal_KurumTarafindan => "İptal",
        _ => d.ToString()
    };
}

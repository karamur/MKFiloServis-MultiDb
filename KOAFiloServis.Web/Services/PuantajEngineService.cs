using System.Diagnostics;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// OperasyonKaydi → PuantajKayit dönüşüm motoru.
/// </summary>
public sealed class PuantajEngineService : IPuantajEngineService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public PuantajEngineService(IDbContextFactory<ApplicationDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task<PuantajEngineSonuc> ProcessDonemAsync(int yil, int ay, int? kurumId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        // 1. İşlenmemiş operasyon kayıtlarını al
        var query = db.OperasyonKayitlari
            .Where(o => !o.IsDeleted && !o.Islendi
                        && o.Tarih >= baslangic && o.Tarih <= bitis);

        if (kurumId.HasValue && kurumId.Value > 0)
        {
            var guzergahIds = await db.Guzergahlar
                .Where(g => !g.IsDeleted && g.KurumId == kurumId.Value)
                .Select(g => g.Id)
                .ToListAsync();
            query = query.Where(o => guzergahIds.Contains(o.GuzergahId));
        }

        var operasyonlar = await query
            .Include(o => o.Guzergah)
            .ToListAsync();

        if (!operasyonlar.Any())
            return new PuantajEngineSonuc { IslenenOperasyonKaydi = 0, UretilenPuantajKayit = 0, GuncellenenPuantajKayit = 0 };

        // 2. Pricing verilerini toplu yükle
        var guzergahIds2 = operasyonlar.Select(o => o.GuzergahId).Distinct().ToList();
        var guzergahlar = await db.Guzergahlar
            .Where(g => guzergahIds2.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id);

        var eslestirmeler = await db.FiloGuzergahEslestirmeleri
            .Where(e => guzergahIds2.Contains(e.GuzergahId) && e.IsActive)
            .ToListAsync();

        // 3. Mevcut PuantajKayit'ları yükle
        var mevcutPuantajlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay)
            .ToListAsync();

        // 4. GuzergahId + AracId + Slot bazında grupla
        var gruplar = operasyonlar
            .GroupBy(o => new { o.GuzergahId, o.AracId, o.Slot })
            .ToList();

        int uretilen = 0, guncellenen = 0;

        foreach (var grup in gruplar)
        {
            var ilk = grup.First();
            var guzergahId = grup.Key.GuzergahId;
            var aracId = grup.Key.AracId;
            var slot = grup.Key.Slot;

            // 5. Aylık gün değerlerini hesapla
            var puantajKayit = mevcutPuantajlar.FirstOrDefault(p =>
                p.GuzergahId == guzergahId && p.AracId == aracId && p.Slot == slot);

            bool yeniKayit = puantajKayit == null;
            if (yeniKayit)
            {
                puantajKayit = new PuantajKayit
                {
                    Yil = yil,
                    Ay = ay,
                    GuzergahId = guzergahId,
                    AracId = aracId,
                    Slot = slot,
                    Kaynak = PuantajKaynak.ServisCalismaOtomatik,
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Günlük sefer sayılarını Gun01-Gun31'e aktar
            Debug.Assert(puantajKayit != null);
            puantajKayit.Gun01 = 0; puantajKayit.Gun02 = 0; puantajKayit.Gun03 = 0;
            puantajKayit.Gun04 = 0; puantajKayit.Gun05 = 0; puantajKayit.Gun06 = 0;
            puantajKayit.Gun07 = 0; puantajKayit.Gun08 = 0; puantajKayit.Gun09 = 0;
            puantajKayit.Gun10 = 0; puantajKayit.Gun11 = 0; puantajKayit.Gun12 = 0;
            puantajKayit.Gun13 = 0; puantajKayit.Gun14 = 0; puantajKayit.Gun15 = 0;
            puantajKayit.Gun16 = 0; puantajKayit.Gun17 = 0; puantajKayit.Gun18 = 0;
            puantajKayit.Gun19 = 0; puantajKayit.Gun20 = 0; puantajKayit.Gun21 = 0;
            puantajKayit.Gun22 = 0; puantajKayit.Gun23 = 0; puantajKayit.Gun24 = 0;
            puantajKayit.Gun25 = 0; puantajKayit.Gun26 = 0; puantajKayit.Gun27 = 0;
            puantajKayit.Gun28 = 0; puantajKayit.Gun29 = 0; puantajKayit.Gun30 = 0;
            puantajKayit.Gun31 = 0;

            decimal toplamSefer = 0;
            foreach (var o in grup)
            {
                var gun = o.Tarih.Day;
                var seferDeger = (int)(o.SeferSayisi * o.PuantajCarpani);
                if (o.OperasyonDurumu == OperasyonDurumu.Gitti)
                {
                    puantajKayit.SetGunDeger(gun, seferDeger);
                    toplamSefer += seferDeger;
                }
                // Gitmedi veya İptal durumlarında 0 olarak kalır
            }

            // 6. Diğer alanları ilk kayıttan kopyala
            puantajKayit.SoforId = ilk.SoforId;
            puantajKayit.SlotAdi = ilk.SlotAdi;
            puantajKayit.Yon = ilk.Yon;
            puantajKayit.KurumId = ilk.KurumId;
            puantajKayit.IsverenFirmaId = ilk.IsverenFirmaId;
            puantajKayit.SeferSayisi = (int)toplamSefer;
            puantajKayit.KaynakTipi = ilk.KaynakTipi;
            puantajKayit.FinansYonu = ilk.FinansYonu;
            puantajKayit.SoforOdemeTipi = ilk.SoforOdemeTipi;
            puantajKayit.OdemeYapilacakCariId = ilk.OdemeYapilacakCariId;
            puantajKayit.FaturaKesiciCariId = ilk.FaturaKesiciCariId;
            puantajKayit.BelgeNo = ilk.BelgeNo;
            puantajKayit.TransferDurum = ilk.TransferDurum;
            puantajKayit.Notlar = ilk.Notlar;
            puantajKayit.UpdatedAt = DateTime.UtcNow;

            // Guzergah bilgilerini doldur
            if (guzergahlar.TryGetValue(guzergahId, out var guzergah))
            {
                puantajKayit.GuzergahAdi = guzergah.GuzergahAdi;
            }

            // 7. Pricing hesapla
            var eslestirme = eslestirmeler.FirstOrDefault(e =>
                e.GuzergahId == guzergahId && e.AracId == aracId);

            if (eslestirme != null)
            {
                puantajKayit.BirimGelir = eslestirme.KurumaKesilecekUcret;
                puantajKayit.BirimGider = eslestirme.TaseronaOdenenUcret;
            }
            else if (guzergahlar.TryGetValue(guzergahId, out var g))
            {
                puantajKayit.BirimGelir = g.GelirFiyat;
                puantajKayit.BirimGider = g.GiderFiyat;
            }

            // 8. Finansal hesaplamaları yap
            puantajKayit.HesaplaPuantajToplam();
            puantajKayit.HesaplaGelir();
            puantajKayit.HesaplaGider();

            if (yeniKayit)
            {
                db.PuantajKayitlar.Add(puantajKayit);
                uretilen++;
            }
            else
            {
                guncellenen++;
            }
        }

        // 9. Önce PuantajKayit'ları kaydet (Id'lerin oluşması için)
        await db.SaveChangesAsync();

        // 10. OperasyonKaydi'ları işlenmiş olarak işaretle ve PuantajKayit'e bağla
        var puantajMap = db.PuantajKayitlar.Local
            .Where(p => p.Yil == yil && p.Ay == ay && !p.IsDeleted)
            .ToLookup(p => (p.GuzergahId, p.AracId, p.Slot));

        var simdi = DateTime.UtcNow;
        foreach (var o in operasyonlar)
        {
            var match = puantajMap[(o.GuzergahId, o.AracId, o.Slot)].FirstOrDefault();
            if (match != null)
            {
                o.Islendi = true;
                o.IslenmeTarihi = simdi;
                o.PuantajKayitId = match.Id;
                o.UpdatedAt = simdi;
            }
        }

        await db.SaveChangesAsync();

        return new PuantajEngineSonuc
        {
            IslenenOperasyonKaydi = operasyonlar.Count,
            UretilenPuantajKayit = uretilen,
            GuncellenenPuantajKayit = guncellenen
        };
    }

    public async Task ProcessSingleAsync(int operasyonKaydiId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var o = await db.OperasyonKayitlari
            .FirstOrDefaultAsync(x => x.Id == operasyonKaydiId && !x.IsDeleted);

        if (o == null || o.Islendi) return;

        // İlgili dönem için tüm operasyonları işle (tutarlılık için)
        await ProcessDonemInternalAsync(db, o.Tarih.Year, o.Tarih.Month, guzergahIds: new[] { o.GuzergahId });
    }

    public async Task<PuantajEngineSonuc> ReprocessDonemAsync(int yil, int ay, int? kurumId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // 1. Mevcut operasyonların Islendi bayrağını sıfırla
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        var query = db.OperasyonKayitlari
            .Where(o => !o.IsDeleted && o.Tarih >= baslangic && o.Tarih <= bitis);

        if (kurumId.HasValue && kurumId.Value > 0)
        {
            var gIds = await db.Guzergahlar
                .Where(g => !g.IsDeleted && g.KurumId == kurumId.Value)
                .Select(g => g.Id)
                .ToListAsync();
            query = query.Where(o => gIds.Contains(o.GuzergahId));
        }

        var ops = await query.ToListAsync();
        foreach (var o in ops)
        {
            o.Islendi = false;
            o.IslenmeTarihi = null;
            o.PuantajKayitId = null;
        }
        await db.SaveChangesAsync();

        // 2. Normal process'i çalıştır
        return await ProcessDonemAsync(yil, ay, kurumId);
    }

    private async Task ProcessDonemInternalAsync(ApplicationDbContext db, int yil, int ay, int[] guzergahIds)
    {
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        var operasyonlar = await db.OperasyonKayitlari
            .Where(o => !o.IsDeleted && !o.Islendi
                        && o.Tarih >= baslangic && o.Tarih <= bitis
                        && guzergahIds.Contains(o.GuzergahId))
            .ToListAsync();

        if (!operasyonlar.Any()) return;

        var guzergahlar = await db.Guzergahlar
            .Where(g => guzergahIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id);

        var eslestirmeler = await db.FiloGuzergahEslestirmeleri
            .Where(e => guzergahIds.Contains(e.GuzergahId) && e.IsActive)
            .ToListAsync();

        var mevcutPuantajlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay)
            .ToListAsync();

        var gruplar = operasyonlar
            .GroupBy(o => new { o.GuzergahId, o.AracId, o.Slot });

        foreach (var grup in gruplar)
        {
            var ilk = grup.First();
            var guzergahId = grup.Key.GuzergahId;
            var aracId = grup.Key.AracId;
            var slot = grup.Key.Slot;

            var pk = mevcutPuantajlar.FirstOrDefault(p =>
                p.GuzergahId == guzergahId && p.AracId == aracId && p.Slot == slot);

            bool yeniKayit = pk == null;
            if (yeniKayit)
            {
                pk = new PuantajKayit
                {
                    Yil = yil, Ay = ay, GuzergahId = guzergahId,
                    AracId = aracId, Slot = slot,
                    Kaynak = PuantajKaynak.ServisCalismaOtomatik,
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Gun01-Gun31 sıfırla
            Debug.Assert(pk != null);
            pk.Gun01 = 0; pk.Gun02 = 0; pk.Gun03 = 0; pk.Gun04 = 0; pk.Gun05 = 0;
            pk.Gun06 = 0; pk.Gun07 = 0; pk.Gun08 = 0; pk.Gun09 = 0; pk.Gun10 = 0;
            pk.Gun11 = 0; pk.Gun12 = 0; pk.Gun13 = 0; pk.Gun14 = 0; pk.Gun15 = 0;
            pk.Gun16 = 0; pk.Gun17 = 0; pk.Gun18 = 0; pk.Gun19 = 0; pk.Gun20 = 0;
            pk.Gun21 = 0; pk.Gun22 = 0; pk.Gun23 = 0; pk.Gun24 = 0; pk.Gun25 = 0;
            pk.Gun26 = 0; pk.Gun27 = 0; pk.Gun28 = 0; pk.Gun29 = 0; pk.Gun30 = 0;
            pk.Gun31 = 0;

            decimal toplamSefer = 0;
            foreach (var o in grup)
            {
                var gun = o.Tarih.Day;
                if (o.OperasyonDurumu == OperasyonDurumu.Gitti)
                {
                    var seferDeger = (int)(o.SeferSayisi * o.PuantajCarpani);
                    pk.SetGunDeger(gun, seferDeger);
                    toplamSefer += seferDeger;
                }
            }

            pk.SoforId = ilk.SoforId;
            pk.SlotAdi = ilk.SlotAdi;
            pk.Yon = ilk.Yon;
            pk.KurumId = ilk.KurumId;
            pk.IsverenFirmaId = ilk.IsverenFirmaId;
            pk.SeferSayisi = (int)toplamSefer;
            pk.KaynakTipi = ilk.KaynakTipi;
            pk.FinansYonu = ilk.FinansYonu;
            pk.SoforOdemeTipi = ilk.SoforOdemeTipi;
            pk.OdemeYapilacakCariId = ilk.OdemeYapilacakCariId;
            pk.FaturaKesiciCariId = ilk.FaturaKesiciCariId;
            pk.BelgeNo = ilk.BelgeNo;
            pk.TransferDurum = ilk.TransferDurum;
            pk.Notlar = ilk.Notlar;
            pk.UpdatedAt = DateTime.UtcNow;

            if (guzergahlar.TryGetValue(guzergahId, out var g))
                pk.GuzergahAdi = g.GuzergahAdi;

            var eslestirme = eslestirmeler.FirstOrDefault(e =>
                e.GuzergahId == guzergahId && e.AracId == aracId);

            if (eslestirme != null)
            {
                pk.BirimGelir = eslestirme.KurumaKesilecekUcret;
                pk.BirimGider = eslestirme.TaseronaOdenenUcret;
            }
            else if (guzergahlar.TryGetValue(guzergahId, out var g2))
            {
                pk.BirimGelir = g2.GelirFiyat;
                pk.BirimGider = g2.GiderFiyat;
            }

            pk.HesaplaPuantajToplam();
            pk.HesaplaGelir();
            pk.HesaplaGider();

            if (yeniKayit)
                db.PuantajKayitlar.Add(pk);
        }

        await db.SaveChangesAsync();

        var puantajMap = db.PuantajKayitlar.Local
            .Where(p => p.Yil == yil && p.Ay == ay && !p.IsDeleted)
            .ToLookup(p => (p.GuzergahId, p.AracId, p.Slot));

        var simdi = DateTime.UtcNow;
        foreach (var o in operasyonlar)
        {
            var match = puantajMap[(o.GuzergahId, o.AracId, o.Slot)].FirstOrDefault();
            if (match != null)
            {
                o.Islendi = true;
                o.IslenmeTarihi = simdi;
                o.PuantajKayitId = match.Id;
                o.UpdatedAt = simdi;
            }
        }

        await db.SaveChangesAsync();
    }
}

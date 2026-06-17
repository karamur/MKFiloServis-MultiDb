using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Models;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// PuantajKayit → HakedisPuantaj senkronizasyon motoru.
/// Grid'deki günlük sefer değerlerini HakedisPuantaj + HakedisPuantajDetay'a dönüştürür.
/// </summary>
public class PuantajHakedisSyncService : IPuantajHakedisSyncService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PuantajHakedisSyncService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task SyncFromPuantajKayitAsync(int firmaId, int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Grid'deki tüm aktif PuantajKayit'ları oku
        var puantajKayitlar = await context.PuantajKayitlar
            .Where(p => p.IsverenFirmaId == firmaId && p.Yil == yil && p.Ay == ay && !p.IsDeleted)
            .ToListAsync();

        if (!puantajKayitlar.Any()) return;

        // Mevcut HakedisPuantaj'ları oku (key = GuzergahId, AracId, SoforId)
        var mevcutHakedisler = await context.HakedisPuantajlar
            .Include(h => h.Detaylar)
            .Include(h => h.Kesintiler)
            .Where(h => h.FirmaId == firmaId && h.Yil == yil && h.Ay == ay && !h.IsDeleted)
            .ToListAsync();

        var mevcutMap = mevcutHakedisler.ToDictionary(
            h => (GuzergahId: h.GuzergahId, AracId: h.AracId, SoforId: h.SoforId),
            h => h);

        int olusturulan = 0, guncellenen = 0;

        foreach (var kayit in puantajKayitlar)
        {
            var key = (GuzergahId: kayit.GuzergahId ?? 0, AracId: kayit.AracId ?? 0, SoforId: kayit.SoforId ?? 0);

            if (key.GuzergahId == 0 || key.SoforId == 0)
                continue; // Güzergah ve Şoför zorunlu

            if (mevcutMap.TryGetValue(key, out var existing))
            {
                // SADECE Taslak durumdakileri güncelle
                if (existing.Durum != HakedisDurumu.Taslak) continue;

                MapKayitToHakedis(kayit, existing, yil, ay);
                guncellenen++;
            }
            else
            {
                var hakedis = new HakedisPuantaj
                {
                    FirmaId = firmaId,
                    Yil = yil, Ay = ay,
                    CreatedAt = DateTime.UtcNow
                };
                MapKayitToHakedis(kayit, hakedis, yil, ay);
                context.HakedisPuantajlar.Add(hakedis);
                olusturulan++;
            }
        }

        await context.SaveChangesAsync();
    }

    private static void MapKayitToHakedis(PuantajKayit kayit, HakedisPuantaj hakedis, int yil, int ay)
    {
        // Temel alanlar
        hakedis.GuzergahId = kayit.GuzergahId ?? 0;
        hakedis.AracId = kayit.AracId ?? 0;
        hakedis.SoforId = kayit.SoforId ?? 0;
        hakedis.CariId = kayit.KurumCariId ?? kayit.OdemeYapilacakCariId ?? 0;

        // Yön tipi dönüşümü
        hakedis.YonTipi = kayit.Yon switch
        {
            PuantajYon.Sabah => YonTipi.Sabah,
            PuantajYon.Aksam => YonTipi.Aksam,
            PuantajYon.SabahAksam => YonTipi.SabahAksam,
            _ => YonTipi.SabahAksam
        };

        // 🔴 Yön = sadece label, hesaplamaya ETKİ ETMEZ. Grid hücre değeri tek gerçek.
        hakedis.GunlukSeferSayisi = 1;

        // Fiyatlandırma
        hakedis.GelirBirimFiyat = kayit.BirimGelir > 0 ? kayit.BirimGelir : kayit.BirimGider;
        hakedis.GiderBirimFiyat = kayit.BirimGider > 0 ? kayit.BirimGider : kayit.BirimGelir;
        hakedis.KdvOrani = kayit.GelirKdvOrani > 0 ? kayit.GelirKdvOrani : 20;

        // Günlük detayları sync et
        SyncDetaylar(kayit, hakedis, yil, ay);

        // Kesinti varsa ekle
        SyncKesintiler(kayit, hakedis);

        // Toplamları yeniden hesapla
        hakedis.Hesapla();

        hakedis.UpdatedAt = DateTime.UtcNow;
    }

    private static void SyncDetaylar(PuantajKayit kayit, HakedisPuantaj hakedis, int yil, int ay)
    {
        int gunSayisi = DateTime.DaysInMonth(yil, ay);

        // Mevcut detayları eşle (varsa)
        var mevcutDetayMap = hakedis.Detaylar.ToDictionary(d => d.Gun, d => d);

        for (int gun = 1; gun <= gunSayisi; gun++)
        {
            var seferDeger = kayit.GetGunDeger(gun);

            if (mevcutDetayMap.TryGetValue(gun, out var detay))
            {
                detay.SeferSayisi = seferDeger;
                detay.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                hakedis.Detaylar.Add(new HakedisPuantajDetay
                {
                    Gun = gun,
                    SeferSayisi = seferDeger,
                    FiyatCarpani = 1,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }

    private static void SyncKesintiler(PuantajKayit kayit, HakedisPuantaj hakedis)
    {
        // Gelir kesintisi
        if (kayit.GelirKesinti > 0)
        {
            var mevcut = hakedis.Kesintiler.FirstOrDefault(k => k.KesintiAdi == "GelirKesinti");
            if (mevcut != null) mevcut.Tutar = kayit.GelirKesinti;
            else hakedis.Kesintiler.Add(new HakedisKesinti
            {
                KesintiAdi = "GelirKesinti", Tutar = kayit.GelirKesinti, CreatedAt = DateTime.UtcNow
            });
        }

        // Gider kesintisi
        if (kayit.GiderKesinti > 0)
        {
            var mevcut = hakedis.Kesintiler.FirstOrDefault(k => k.KesintiAdi == "GiderKesinti");
            if (mevcut != null) mevcut.Tutar = kayit.GiderKesinti;
            else hakedis.Kesintiler.Add(new HakedisKesinti
            {
                KesintiAdi = "GiderKesinti", Tutar = kayit.GiderKesinti, CreatedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Grid'den direkt veri alarak sync yapar.
    /// Mesai/EkSefer/FiyatCarpani bilgilerini PuantajHucre'den okur.
    /// </summary>
    public async Task SyncFromGridAsync(int firmaId, int yil, int ay, List<PuantajGridSatir> satirlar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (!satirlar.Any()) return;

        var mevcutHakedisler = await context.HakedisPuantajlar
            .Include(h => h.Detaylar)
            .Include(h => h.Kesintiler)
            .Where(h => h.FirmaId == firmaId && h.Yil == yil && h.Ay == ay && !h.IsDeleted)
            .ToListAsync();

        var mevcutMap = mevcutHakedisler.ToDictionary(
            h => (GuzergahId: h.GuzergahId, AracId: h.AracId, SoforId: h.SoforId), h => h);

        foreach (var satir in satirlar)
        {
            var kayit = await context.PuantajKayitlar.FindAsync(satir.KayitId);
            if (kayit == null) continue;

            var key = (GuzergahId: kayit.GuzergahId ?? 0, AracId: kayit.AracId ?? 0, SoforId: kayit.SoforId ?? 0);
            if (key.GuzergahId == 0 || key.SoforId == 0) continue;

            if (mevcutMap.TryGetValue(key, out var existing))
            {
                if (existing.Durum != HakedisDurumu.Taslak) continue;
                MapKayitToHakedis(kayit, existing, yil, ay);
                SyncDetaylarFromGrid(satir, existing, yil, ay);
            }
            else
            {
                var hakedis = new HakedisPuantaj { FirmaId = firmaId, Yil = yil, Ay = ay, CreatedAt = DateTime.UtcNow };
                MapKayitToHakedis(kayit, hakedis, yil, ay);
                SyncDetaylarFromGrid(satir, hakedis, yil, ay);
                context.HakedisPuantajlar.Add(hakedis);
            }
        }
        await context.SaveChangesAsync();
    }

    // 🔴 TEK DOĞRU: detay.SeferSayisi = Deger + Mesai + EkSefer. Yön etki ETMEZ.
    private static void SyncDetaylarFromGrid(PuantajGridSatir satir, HakedisPuantaj hakedis, int yil, int ay)
    {
        int gunSayisi = DateTime.DaysInMonth(yil, ay);
        var mevcutDetayMap = hakedis.Detaylar.ToDictionary(d => d.Gun, d => d);

        foreach (var hucre in satir.Hucreler)
        {
            if (hucre.Gun > gunSayisi) continue;

            if (mevcutDetayMap.TryGetValue(hucre.Gun, out var detay))
            {
                detay.SeferSayisi = hucre.ToplamSefer;
                detay.MesaiMi = hucre.Mesai > 0;
                detay.EkSeferMi = hucre.EkSefer > 0;
                detay.FiyatCarpani = hucre.FiyatCarpani;
                detay.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                hakedis.Detaylar.Add(new HakedisPuantajDetay
                {
                    Gun = hucre.Gun,
                    SeferSayisi = hucre.ToplamSefer,
                    MesaiMi = hucre.Mesai > 0,
                    EkSeferMi = hucre.EkSefer > 0,
                    FiyatCarpani = hucre.FiyatCarpani,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }
}

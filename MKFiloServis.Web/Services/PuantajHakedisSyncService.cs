using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// PuantajKayit → Hakedis (yeni model) senkronizasyon motoru.
/// Grid'deki günlük sefer değerlerini Hakedis + HakedisDetay'a dönüştürür.
/// </summary>
public class PuantajHakedisSyncService : IPuantajHakedisSyncService
{
    private const string SyncSourceTag = "PuantajSyncV2";
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PuantajHakedisSyncService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task SyncFromPuantajKayitAsync(int firmaId, int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var puantajKayitlar = await context.PuantajKayitlar
            .Where(p => p.IsverenFirmaId == firmaId && p.Yil == yil && p.Ay == ay && !p.IsDeleted)
            .ToListAsync();

        if (!puantajKayitlar.Any()) return;

        await SyncAsync(context, firmaId, yil, ay, puantajKayitlar, null);
    }

    /// <summary>
    /// Grid'den direkt veri alarak sync yapar.
    /// Mesai/EkSefer/FiyatCarpani bilgilerini PuantajHucre'den okur.
    /// </summary>
    public async Task SyncFromGridAsync(int firmaId, int yil, int ay, List<PuantajGridSatir> satirlar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (!satirlar.Any()) return;

        var satirMap = satirlar.ToDictionary(s => s.KayitId, s => s);
        var kayitIdler = satirMap.Keys.ToList();

        var puantajKayitlar = await context.PuantajKayitlar
            .Where(p => p.IsverenFirmaId == firmaId && p.Yil == yil && p.Ay == ay && !p.IsDeleted && kayitIdler.Contains(p.Id))
            .ToListAsync();

        if (!puantajKayitlar.Any()) return;

        await SyncAsync(context, firmaId, yil, ay, puantajKayitlar, satirMap);
    }

    private static async Task SyncAsync(
        ApplicationDbContext context,
        int firmaId,
        int yil,
        int ay,
        List<PuantajKayit> kayitlar,
        Dictionary<int, PuantajGridSatir>? satirMap)
    {
        var mevcutHakedisler = await context.Hakedisler
            .Include(h => h.Detaylar)
            .Where(h => h.FirmaId == firmaId
                && h.Yil == yil
                && h.Ay == ay
                && h.Tip == HakedisTipi.Kurum
                && h.GenerationParams == SyncSourceTag
                && !h.IsDeleted)
            .ToListAsync();

        var mevcutMap = mevcutHakedisler.ToDictionary(h => h.ReferansId, h => h);
        var aktifReferanslar = new HashSet<int>();

        var gruplar = kayitlar
            .Where(k => (k.KurumCariId ?? 0) > 0)
            .GroupBy(k => k.KurumCariId!.Value)
            .ToList();

        foreach (var grup in gruplar)
        {
            var referansId = grup.Key;
            aktifReferanslar.Add(referansId);

            if (!mevcutMap.TryGetValue(referansId, out var hakedis))
            {
                hakedis = new Hakedis
                {
                    FirmaId = firmaId,
                    Yil = yil,
                    Ay = ay,
                    Tip = HakedisTipi.Kurum,
                    ReferansId = referansId,
                    Durum = HakedisDurum.Taslak,
                    GenerationParams = SyncSourceTag,
                    CreatedAt = DateTime.UtcNow
                };
                context.Hakedisler.Add(hakedis);
                mevcutMap[referansId] = hakedis;
            }

            if (hakedis.Durum != HakedisDurum.Taslak)
                continue;

            if (hakedis.Detaylar.Any())
                context.HakedisDetaylari.RemoveRange(hakedis.Detaylar.Where(d => !d.IsDeleted));

            decimal toplamSefer = 0m;
            decimal toplamTutar = 0m;
            decimal kdvOran = 20m;

            foreach (var kayit in grup)
            {
                var birimFiyat = ResolveBirimFiyat(kayit);
                if (kayit.GelirKdvOrani > 0) kdvOran = kayit.GelirKdvOrani;

                var gunSayisi = DateTime.DaysInMonth(yil, ay);
                for (var gun = 1; gun <= gunSayisi; gun++)
                {
                    var sefer = GetGunSeferSayisi(kayit, gun, satirMap);
                    if (sefer <= 0) continue;

                    var tarih = new DateTime(yil, ay, gun, 0, 0, 0, DateTimeKind.Utc);
                    var satirTutar = sefer * birimFiyat;

                    hakedis.Detaylar.Add(new HakedisDetay
                    {
                        FirmaId = firmaId,
                        Tarih = tarih,
                        ServisTuru = MapServisTuru(kayit.Yon),
                        AracId = kayit.AracId,
                        SoforId = kayit.SoforId,
                        GuzergahId = kayit.GuzergahId,
                        SeferSayisi = sefer,
                        BirimFiyat = birimFiyat,
                        Tutar = satirTutar,
                        Aciklama = "Puantaj grid sync",
                        CreatedAt = DateTime.UtcNow
                    });

                    toplamSefer += sefer;
                    toplamTutar += satirTutar;
                }
            }

            hakedis.ToplamSeferSayisi = toplamSefer;
            hakedis.Tutar = toplamTutar;
            hakedis.BirimFiyat = toplamSefer > 0 ? toplamTutar / toplamSefer : 0;
            hakedis.KdvOran = kdvOran;
            hakedis.KdvTutar = hakedis.Tutar * hakedis.KdvOran / 100m;
            hakedis.GenelToplam = hakedis.Tutar + hakedis.KdvTutar;
            hakedis.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var eski in mevcutHakedisler.Where(h => h.Durum == HakedisDurum.Taslak && !aktifReferanslar.Contains(h.ReferansId)))
        {
            eski.IsDeleted = true;
            eski.DeletedAt = DateTime.UtcNow;
            foreach (var detay in eski.Detaylar.Where(d => !d.IsDeleted))
            {
                detay.IsDeleted = true;
                detay.DeletedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
    }

    private static int GetGunSeferSayisi(PuantajKayit kayit, int gun, Dictionary<int, PuantajGridSatir>? satirMap)
    {
        if (satirMap != null && satirMap.TryGetValue(kayit.Id, out var satir))
        {
            var hucre = satir.Hucreler.FirstOrDefault(h => h.Gun == gun);
            if (hucre != null) return hucre.ToplamSefer;
        }

        return kayit.GetGunDeger(gun);
    }

    private static decimal ResolveBirimFiyat(PuantajKayit kayit)
        => kayit.BirimGelir > 0 ? kayit.BirimGelir : kayit.BirimGider;

    private static ServisTuru MapServisTuru(PuantajYon yon)
        => yon switch
        {
            PuantajYon.Sabah => ServisTuru.Sabah,
            PuantajYon.Aksam => ServisTuru.Aksam,
            _ => ServisTuru.SabahAksam
        };
}



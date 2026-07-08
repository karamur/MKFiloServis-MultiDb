using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Hakediş raporlama servisi (yeni model: Hakedis + HakedisDetay).
/// </summary>
public class HakedisRaporService : IHakedisRaporService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public HakedisRaporService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region 1. Araç Bazlı Rapor

    public async Task<List<AracRapor>> GetAracRaporuAsync(int? firmaId = null, int? yil = null, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var detaylar = await context.HakedisDetaylari
            .AsNoTracking()
            .Where(d => !d.IsDeleted
                     && d.AracId.HasValue
                     && !d.Hakedis!.IsDeleted
                     && (!firmaId.HasValue || d.Hakedis.FirmaId == firmaId.Value)
                     && (!yil.HasValue || d.Hakedis.Yil == yil.Value)
                     && (!ay.HasValue || d.Hakedis.Ay == ay.Value))
            .Select(d => new
            {
                d.AracId,
                d.SeferSayisi,
                d.Tutar,
                d.Hakedis!.Tip
            })
            .ToListAsync();

        var aracIds = detaylar.Select(x => x.AracId!.Value).Distinct().ToList();
        var plakaByAracId = await context.Araclar
            .AsNoTracking()
            .Where(a => aracIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.AktifPlaka ?? a.Plaka ?? a.Id.ToString());

        return detaylar
            .GroupBy(x => x.AracId!.Value)
            .Select(g =>
            {
                var gelir = g.Where(x => x.Tip == HakedisTipi.Kurum).Sum(x => x.Tutar);
                var gider = g.Where(x => x.Tip == HakedisTipi.Tedarikci).Sum(x => x.Tutar);
                var seferDec = g.Where(x => x.Tip == HakedisTipi.Kurum).Sum(x => x.SeferSayisi);
                if (seferDec <= 0)
                    seferDec = g.Where(x => x.Tip != HakedisTipi.Arac).Sum(x => x.SeferSayisi);

                return new AracRapor
                {
                    AracId = g.Key,
                    Plaka = plakaByAracId.TryGetValue(g.Key, out var plaka) ? plaka : g.Key.ToString(),
                    ToplamSefer = (int)Math.Round(seferDec, MidpointRounding.AwayFromZero),
                    GelirToplam = gelir,
                    GiderToplam = gider,
                    KarTutar = gelir - gider
                };
            })
            .OrderByDescending(r => r.ToplamSefer)
            .ToList();
    }

    #endregion

    #region 2. Şoför Bazlı Rapor

    public async Task<List<SoforRapor>> GetSoforRaporuAsync(int? firmaId = null, int? yil = null, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var detaylar = await context.HakedisDetaylari
            .AsNoTracking()
            .Where(d => !d.IsDeleted
                     && d.SoforId.HasValue
                     && !d.Hakedis!.IsDeleted
                     && (!firmaId.HasValue || d.Hakedis.FirmaId == firmaId.Value)
                     && (!yil.HasValue || d.Hakedis.Yil == yil.Value)
                     && (!ay.HasValue || d.Hakedis.Ay == ay.Value))
            .Select(d => new
            {
                d.SoforId,
                d.SeferSayisi,
                d.Tutar,
                d.Hakedis!.Tip
            })
            .ToListAsync();

        var soforIds = detaylar.Select(x => x.SoforId!.Value).Distinct().ToList();
        var adBySoforId = await context.Soforler
            .AsNoTracking()
            .Where(s => soforIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.TamAd);

        return detaylar
            .GroupBy(x => x.SoforId!.Value)
            .Select(g =>
            {
                var seferDec = g.Where(x => x.Tip == HakedisTipi.Kurum).Sum(x => x.SeferSayisi);
                if (seferDec <= 0)
                    seferDec = g.Where(x => x.Tip != HakedisTipi.Arac).Sum(x => x.SeferSayisi);

                var kazanc = g.Where(x => x.Tip == HakedisTipi.Tedarikci).Sum(x => x.Tutar);
                if (kazanc <= 0)
                    kazanc = g.Where(x => x.Tip == HakedisTipi.Kurum).Sum(x => x.Tutar);

                return new SoforRapor
                {
                    SoforId = g.Key,
                    SoforAdi = adBySoforId.TryGetValue(g.Key, out var ad) ? ad : g.Key.ToString(),
                    ToplamSefer = (int)Math.Round(seferDec, MidpointRounding.AwayFromZero),
                    ToplamKazanc = kazanc
                };
            })
            .OrderByDescending(r => r.ToplamSefer)
            .ToList();
    }

    #endregion

    #region 3. Güzergah Bazlı Rapor

    public async Task<List<GuzergahRapor>> GetGuzergahRaporuAsync(int? firmaId = null, int? yil = null, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var detaylar = await context.HakedisDetaylari
            .AsNoTracking()
            .Where(d => !d.IsDeleted
                     && d.GuzergahId.HasValue
                     && !d.Hakedis!.IsDeleted
                     && (!firmaId.HasValue || d.Hakedis.FirmaId == firmaId.Value)
                     && (!yil.HasValue || d.Hakedis.Yil == yil.Value)
                     && (!ay.HasValue || d.Hakedis.Ay == ay.Value))
            .Select(d => new
            {
                d.GuzergahId,
                d.SeferSayisi,
                d.Tutar,
                d.Hakedis!.Tip
            })
            .ToListAsync();

        var guzergahIds = detaylar.Select(x => x.GuzergahId!.Value).Distinct().ToList();
        var adByGuzergahId = await context.Guzergahlar
            .AsNoTracking()
            .Where(g => guzergahIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.GuzergahAdi ?? g.GuzergahKodu ?? g.Id.ToString());

        return detaylar
            .GroupBy(x => x.GuzergahId!.Value)
            .Select(g =>
            {
                var gelir = g.Where(x => x.Tip == HakedisTipi.Kurum).Sum(x => x.Tutar);
                var gider = g.Where(x => x.Tip == HakedisTipi.Tedarikci).Sum(x => x.Tutar);
                var seferDec = g.Where(x => x.Tip == HakedisTipi.Kurum).Sum(x => x.SeferSayisi);
                if (seferDec <= 0)
                    seferDec = g.Where(x => x.Tip != HakedisTipi.Arac).Sum(x => x.SeferSayisi);

                var toplamSefer = (int)Math.Round(seferDec, MidpointRounding.AwayFromZero);

                return new GuzergahRapor
                {
                    GuzergahId = g.Key,
                    GuzergahAdi = adByGuzergahId.TryGetValue(g.Key, out var ad) ? ad : g.Key.ToString(),
                    ToplamSefer = toplamSefer,
                    OrtalamaMaliyet = toplamSefer > 0 ? gider / toplamSefer : 0,
                    KarlilikOrani = gider > 0 ? (gelir - gider) / gider * 100 : 0
                };
            })
            .OrderByDescending(r => r.ToplamSefer)
            .ToList();
    }

    #endregion

    #region 4. Günlük Operasyon Raporu

    public async Task<List<GunlukRapor>> GetGunlukRaporAsync(int yil, int ay, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var detaylar = await context.HakedisDetaylari
            .AsNoTracking()
            .Where(d => !d.IsDeleted
                     && d.Tarih.Year == yil
                     && d.Tarih.Month == ay
                     && !d.Hakedis!.IsDeleted
                     && (!firmaId.HasValue || d.Hakedis.FirmaId == firmaId.Value))
            .Select(d => new
            {
                Gun = d.Tarih.Day,
                d.SeferSayisi,
                d.Tutar,
                d.Hakedis!.Tip
            })
            .ToListAsync();

        return detaylar
            .GroupBy(x => x.Gun)
            .Select(g =>
            {
                var seferDec = g.Where(x => x.Tip == HakedisTipi.Kurum).Sum(x => x.SeferSayisi);
                if (seferDec <= 0)
                    seferDec = g.Where(x => x.Tip != HakedisTipi.Arac).Sum(x => x.SeferSayisi);

                return new GunlukRapor
                {
                    Gun = g.Key,
                    ToplamSefer = (int)Math.Round(seferDec, MidpointRounding.AwayFromZero),
                    GunlukGelir = g.Where(x => x.Tip == HakedisTipi.Kurum).Sum(x => x.Tutar),
                    GunlukGider = g.Where(x => x.Tip == HakedisTipi.Tedarikci).Sum(x => x.Tutar),
                    MesaiSefer = 0,
                    EkSefer = 0
                };
            })
            .OrderBy(r => r.Gun)
            .ToList();
    }

    #endregion

    #region 5. Dashboard KPI

    public async Task<HakedisDashboardKpi> GetDashboardKpiAsync(int yil, int ay, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var hakedisler = await context.Hakedisler
            .AsNoTracking()
            .Where(h => h.Yil == yil && h.Ay == ay && !h.IsDeleted)
            .Where(h => !firmaId.HasValue || h.FirmaId == firmaId.Value)
            .ToListAsync();

        var toplamSeferDec = hakedisler.Where(h => h.Tip == HakedisTipi.Kurum).Sum(h => h.ToplamSeferSayisi);
        if (toplamSeferDec <= 0)
            toplamSeferDec = hakedisler.Where(h => h.Tip != HakedisTipi.Arac).Sum(h => h.ToplamSeferSayisi);

        var toplamSefer = (int)Math.Round(toplamSeferDec, MidpointRounding.AwayFromZero);
        var toplamGelir = hakedisler.Where(h => h.Tip == HakedisTipi.Kurum).Sum(h => h.GenelToplam);
        var toplamGider = hakedisler.Where(h => h.Tip == HakedisTipi.Tedarikci).Sum(h => h.GenelToplam);
        var toplamKar = toplamGelir - toplamGider;

        var detaylar = await context.HakedisDetaylari
            .AsNoTracking()
            .Where(d => !d.IsDeleted
                     && d.Tarih.Year == yil
                     && d.Tarih.Month == ay
                     && !d.Hakedis!.IsDeleted
                     && (!firmaId.HasValue || d.Hakedis.FirmaId == firmaId.Value))
            .Select(d => new
            {
                Gun = d.Tarih.Day,
                d.SeferSayisi,
                d.Tutar,
                d.Hakedis!.Tip
            })
            .ToListAsync();

        var gunlukTrend = detaylar
            .GroupBy(d => d.Gun)
            .Select(g =>
            {
                var seferDec = g.Where(x => x.Tip == HakedisTipi.Kurum).Sum(x => x.SeferSayisi);
                if (seferDec <= 0)
                    seferDec = g.Where(x => x.Tip != HakedisTipi.Arac).Sum(x => x.SeferSayisi);

                return new GunlukTrend
                {
                    Gun = g.Key,
                    Sefer = (int)Math.Round(seferDec, MidpointRounding.AwayFromZero),
                    Gelir = g.Where(x => x.Tip == HakedisTipi.Kurum).Sum(x => x.Tutar),
                    Gider = g.Where(x => x.Tip == HakedisTipi.Tedarikci).Sum(x => x.Tutar)
                };
            })
            .OrderBy(t => t.Gun)
            .ToList();

        return new HakedisDashboardKpi
        {
            ToplamSefer = toplamSefer,
            ToplamGelir = toplamGelir,
            ToplamGider = toplamGider,
            ToplamKar = toplamKar,
            OrtalamaSeferMaliyeti = toplamSefer > 0 ? toplamGider / toplamSefer : 0,
            KarlilikOrani = toplamGelir > 0 ? toplamKar / toplamGelir * 100 : 0,
            GunlukTrend = gunlukTrend
        };
    }

    #endregion
}

#region Rapor Modelleri

public class AracRapor
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = "";
    public int ToplamSefer { get; set; }
    public decimal GelirToplam { get; set; }
    public decimal GiderToplam { get; set; }
    public decimal KarTutar { get; set; }
}

public class SoforRapor
{
    public int SoforId { get; set; }
    public string SoforAdi { get; set; } = "";
    public int ToplamSefer { get; set; }
    public decimal ToplamKazanc { get; set; }
}

public class GuzergahRapor
{
    public int GuzergahId { get; set; }
    public string GuzergahAdi { get; set; } = "";
    public int ToplamSefer { get; set; }
    public decimal OrtalamaMaliyet { get; set; }
    public decimal KarlilikOrani { get; set; }
}

public class GunlukRapor
{
    public int Gun { get; set; }
    public int ToplamSefer { get; set; }
    public decimal GunlukGelir { get; set; }
    public decimal GunlukGider { get; set; }
    public decimal GunlukKar => GunlukGelir - GunlukGider;
    public int MesaiSefer { get; set; }
    public int EkSefer { get; set; }
}

public class HakedisDashboardKpi
{
    public int ToplamSefer { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal ToplamKar { get; set; }
    public decimal OrtalamaSeferMaliyeti { get; set; }
    public decimal KarlilikOrani { get; set; }
    public List<GunlukTrend> GunlukTrend { get; set; } = new();
}

public class GunlukTrend
{
    public int Gun { get; set; }
    public int Sefer { get; set; }
    public decimal Gelir { get; set; }
    public decimal Gider { get; set; }
    public decimal Kar => Gelir - Gider;
}

#endregion

using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Hakediş Puantaj Raporlama servisi.
/// Tüm sorgular IQueryable + AsNoTracking + FirmaId filtreli.
/// </summary>
public class HakedisRaporService : IHakedisRaporService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public HakedisRaporService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private async Task<IQueryable<HakedisPuantaj>> BaseQuery(int? firmaId, int? yil, int? ay)
    {
        var context = await _contextFactory.CreateDbContextAsync();
        var query = context.HakedisPuantajlar
            .AsNoTracking()
            .Where(h => !h.IsDeleted);

        if (firmaId.HasValue) query = query.Where(h => h.FirmaId == firmaId.Value);
        if (yil.HasValue) query = query.Where(h => h.Yil == yil.Value);
        if (ay.HasValue) query = query.Where(h => h.Ay == ay.Value);

        return query;
    }

    #region 1. Araç Bazlı Rapor

    public async Task<List<AracRapor>> GetAracRaporuAsync(int? firmaId = null, int? yil = null, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.HakedisPuantajlar
            .AsNoTracking()
            .Include(h => h.Arac)
            .Where(h => !h.IsDeleted);

        if (firmaId.HasValue) query = query.Where(h => h.FirmaId == firmaId.Value);
        if (yil.HasValue) query = query.Where(h => h.Yil == yil.Value);
        if (ay.HasValue) query = query.Where(h => h.Ay == ay.Value);

        return await query
            .GroupBy(h => new { h.AracId, Plaka = h.Arac != null ? h.Arac.Plaka : h.AracId.ToString() })
            .Select(g => new AracRapor
            {
                AracId = g.Key.AracId,
                Plaka = g.Key.Plaka,
                ToplamSefer = g.Sum(h => h.ToplamSefer),
                GelirToplam = g.Sum(h => h.GelirToplam),
                GiderToplam = g.Sum(h => h.GiderToplam),
                KarTutar = g.Sum(h => h.TahsilEdilecekTutar - h.OdenecekTutar)
            })
            .OrderByDescending(r => r.ToplamSefer)
            .ToListAsync();
    }

    #endregion

    #region 2. Şoför Bazlı Rapor

    public async Task<List<SoforRapor>> GetSoforRaporuAsync(int? firmaId = null, int? yil = null, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.HakedisPuantajlar
            .AsNoTracking()
            .Include(h => h.Sofor)
            .Where(h => !h.IsDeleted);

        if (firmaId.HasValue) query = query.Where(h => h.FirmaId == firmaId.Value);
        if (yil.HasValue) query = query.Where(h => h.Yil == yil.Value);
        if (ay.HasValue) query = query.Where(h => h.Ay == ay.Value);

        return await query
            .GroupBy(h => new { h.SoforId, SoforAdi = h.Sofor != null ? h.Sofor.TamAd : h.SoforId.ToString() })
            .Select(g => new SoforRapor
            {
                SoforId = g.Key.SoforId,
                SoforAdi = g.Key.SoforAdi,
                ToplamSefer = g.Sum(h => h.ToplamSefer),
                ToplamKazanc = g.Sum(h => h.OdenecekTutar)
            })
            .OrderByDescending(r => r.ToplamSefer)
            .ToListAsync();
    }

    #endregion

    #region 3. Güzergah Bazlı Rapor

    public async Task<List<GuzergahRapor>> GetGuzergahRaporuAsync(int? firmaId = null, int? yil = null, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.HakedisPuantajlar
            .AsNoTracking()
            .Include(h => h.Guzergah)
            .Where(h => !h.IsDeleted);

        if (firmaId.HasValue) query = query.Where(h => h.FirmaId == firmaId.Value);
        if (yil.HasValue) query = query.Where(h => h.Yil == yil.Value);
        if (ay.HasValue) query = query.Where(h => h.Ay == ay.Value);

        var data = await query
            .GroupBy(h => new { h.GuzergahId, GuzergahAdi = h.Guzergah != null ? h.Guzergah.GuzergahAdi : h.GuzergahId.ToString() })
            .Select(g => new
            {
                g.Key.GuzergahId,
                g.Key.GuzergahAdi,
                ToplamSefer = g.Sum(h => h.ToplamSefer),
                ToplamGelir = g.Sum(h => h.GelirToplam),
                ToplamGider = g.Sum(h => h.GiderToplam)
            })
            .ToListAsync();

        return data.Select(g => new GuzergahRapor
        {
            GuzergahId = g.GuzergahId,
            GuzergahAdi = g.GuzergahAdi,
            ToplamSefer = g.ToplamSefer,
            OrtalamaMaliyet = g.ToplamSefer > 0 ? g.ToplamGider / g.ToplamSefer : 0,
            KarlilikOrani = g.ToplamGider > 0 ? (g.ToplamGelir - g.ToplamGider) / g.ToplamGider * 100 : 0
        })
        .OrderByDescending(r => r.ToplamSefer)
        .ToList();
    }

    #endregion

    #region 4. Günlük Operasyon Raporu

    public async Task<List<GunlukRapor>> GetGunlukRaporAsync(int yil, int ay, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var hakedisler = await context.HakedisPuantajlar
            .AsNoTracking()
            .Where(h => h.Yil == yil && h.Ay == ay && !h.IsDeleted)
            .Where(h => !firmaId.HasValue || h.FirmaId == firmaId.Value)
            .SelectMany(
                h => h.Detaylar!.Where(d => !d.IsDeleted),
                (h, d) => new { d.Gun, d.SeferSayisi, h.GelirSeferBirimFiyat, h.GiderSeferBirimFiyat, d.FiyatCarpani, d.EkSeferMi, d.MesaiMi }
            )
            .ToListAsync();

        return hakedisler
            .GroupBy(x => x.Gun)
            .Select(g => new GunlukRapor
            {
                Gun = g.Key,
                ToplamSefer = g.Sum(x => x.SeferSayisi),
                GunlukGelir = g.Sum(x => x.SeferSayisi * x.GelirSeferBirimFiyat * x.FiyatCarpani),
                GunlukGider = g.Sum(x => x.SeferSayisi * x.GiderSeferBirimFiyat * x.FiyatCarpani),
                MesaiSefer = g.Count(x => x.MesaiMi),
                EkSefer = g.Count(x => x.EkSeferMi)
            })
            .OrderBy(r => r.Gun)
            .ToList();
    }

    #endregion

    #region 5. Dashboard KPI

    public async Task<HakedisDashboardKpi> GetDashboardKpiAsync(int yil, int ay, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.HakedisPuantajlar
            .AsNoTracking()
            .Where(h => h.Yil == yil && h.Ay == ay && !h.IsDeleted);

        if (firmaId.HasValue) query = query.Where(h => h.FirmaId == firmaId.Value);

        var data = await query.ToListAsync();

        var toplamSefer = data.Sum(h => h.ToplamSefer);
        var toplamGelir = data.Sum(h => h.GelirToplam);
        var toplamGider = data.Sum(h => h.GiderToplam);
        var toplamKar = data.Sum(h => h.TahsilEdilecekTutar - h.OdenecekTutar);

        // Günlük trend
        var detaylar = await context.HakedisPuantajDetaylar
            .AsNoTracking()
            .Where(d => !d.IsDeleted && data.Select(h => h.Id).Contains(d.HakedisPuantajId))
            .ToListAsync();

        var gunlukTrend = detaylar
            .GroupBy(d => d.Gun)
            .Select(g => new GunlukTrend { Gun = g.Key, SeferSayisi = g.Sum(d => d.SeferSayisi) })
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
    public int SeferSayisi { get; set; }
}

#endregion

using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;

namespace MKFiloServis.Web.Services;

public class DashboardGrafikService : IDashboardGrafikService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICacheService _cache;
    private static readonly CultureInfo TrCulture = new("tr-TR");

    public DashboardGrafikService(IDbContextFactory<ApplicationDbContext> contextFactory, ICacheService cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    public async Task<AylikGrafikData> GetAylikGelirGiderAsync(int yil)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cacheKey = $"{CacheKeys.Prefix}Dashboard:GelirGider:{yil}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var data = new AylikGrafikData
            {
                Veri1Label = "Gelir",
                Veri2Label = "Gider"
            };

            var aylar = new[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };
            data.Aylar = aylar.ToList();

            // Aylık gelirler (servis çalışmalarından)
            var calismalar = await context.ServisCalismalari
                .AsNoTracking()
                .Where(c => c.CalismaTarihi.Year == yil)
                .GroupBy(c => c.CalismaTarihi.Month)
                .Select(g => new { Ay = g.Key, Toplam = g.Sum(c => c.Fiyat ?? 0) })
                .ToListAsync();

            // Aylık giderler (araç masraflarından)
            var masraflar = await context.AracMasraflari
                .AsNoTracking()
                .Where(m => m.MasrafTarihi.Year == yil)
                .GroupBy(m => m.MasrafTarihi.Month)
                .Select(g => new { Ay = g.Key, Toplam = g.Sum(m => m.Tutar) })
                .ToListAsync();

            for (int ay = 1; ay <= 12; ay++)
            {
                var gelir = calismalar.FirstOrDefault(c => c.Ay == ay)?.Toplam ?? 0;
                var gider = masraflar.FirstOrDefault(m => m.Ay == ay)?.Toplam ?? 0;
                data.Veri1.Add(gelir);
                data.Veri2.Add(gider);
            }

            return data;
        }, CacheDurations.Medium);
    }

    public async Task<AylikGrafikData> GetAylikSeferSayisiAsync(int yil)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cacheKey = $"{CacheKeys.Prefix}Dashboard:SeferSayisi:{yil}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var data = new AylikGrafikData
            {
                Veri1Label = "Sefer Sayısı"
            };

            var aylar = new[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };
            data.Aylar = aylar.ToList();

            var calismalar = await context.ServisCalismalari
                .AsNoTracking()
                .Where(c => c.CalismaTarihi.Year == yil)
                .GroupBy(c => c.CalismaTarihi.Month)
                .Select(g => new { Ay = g.Key, Sayi = g.Count() })
                .ToListAsync();

            for (int ay = 1; ay <= 12; ay++)
            {
                var sayi = calismalar.FirstOrDefault(c => c.Ay == ay)?.Sayi ?? 0;
                data.Veri1.Add(sayi);
            }

            return data;
        }, CacheDurations.Medium);
    }

    public async Task<List<AracPerformansData>> GetAracPerformansAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cacheKey = $"{CacheKeys.Prefix}Dashboard:AracPerformans:{yil}:{ay}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var calismalar = await context.ServisCalismalari
                .AsNoTracking()
                .Include(c => c.Arac)
                .Where(c => c.CalismaTarihi.Year == yil && c.CalismaTarihi.Month == ay)
                .GroupBy(c => new { c.AracId, Plaka = c.Arac!.AktifPlaka ?? "" })
                .Select(g => new
                {
                    Plaka = g.Key.Plaka,
                    AracId = g.Key.AracId,
                    SeferSayisi = g.Count(),
                    ToplamCiro = g.Sum(c => c.Fiyat ?? 0)
                })
                .ToListAsync();

            var masraflar = await context.AracMasraflari
                .AsNoTracking()
                .Where(m => m.MasrafTarihi.Year == yil && m.MasrafTarihi.Month == ay)
                .GroupBy(m => m.AracId)
                .Select(g => new { AracId = g.Key, ToplamMasraf = g.Sum(m => m.Tutar) })
                .ToListAsync();

            return calismalar.Select(c => new AracPerformansData
            {
                Plaka = c.Plaka,
                SeferSayisi = c.SeferSayisi,
                ToplamCiro = c.ToplamCiro,
                ToplamMasraf = masraflar.FirstOrDefault(m => m.AracId == c.AracId)?.ToplamMasraf ?? 0
            })
            .OrderByDescending(a => a.ToplamCiro)
            .Take(10)
            .ToList();
        }, CacheDurations.Medium);
    }

    public async Task<List<CariPerformansData>> GetCariPerformansAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cacheKey = $"{CacheKeys.Prefix}Dashboard:CariPerformans:{yil}:{ay}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var faturalar = await context.Faturalar
                .AsNoTracking()
                .Include(f => f.Cari)
                .Where(f => f.FaturaTarihi.Year == yil && f.FaturaTarihi.Month == ay && f.FaturaTipi == FaturaTipi.SatisFaturasi)
                .GroupBy(f => new { f.CariId, f.Cari!.Unvan })
                .Select(g => new CariPerformansData
                {
                    CariUnvan = g.Key.Unvan,
                    SeferSayisi = g.Count(),
                    ToplamCiro = g.Sum(f => f.GenelToplam),
                    OdenenTutar = g.Sum(f => f.OdenenTutar),
                    KalanBakiye = g.Sum(f => f.GenelToplam - f.OdenenTutar)
                })
                .OrderByDescending(c => c.ToplamCiro)
                .Take(10)
                .ToListAsync();

            return faturalar;
        }, CacheDurations.Medium);
    }

    public async Task<List<MasrafKategoriDagilimi>> GetMasrafKategoriDagilimiAsync(int aySayisi = 6)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var baslangic = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-aySayisi + 1);
        var bitis = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(1).AddDays(-1);

        // Araç masraflarından kategori dağılımı
        var masraflar = await context.AracMasraflari
            .AsNoTracking()
            .Include(m => m.MasrafKalemi)
            .Where(m => m.MasrafTarihi >= baslangic && m.MasrafTarihi <= bitis)
            .Select(m => new
            {
                KategoriAdi = m.MasrafKalemi != null ? m.MasrafKalemi.MasrafAdi : "Diğer",
                m.Tutar
            })
            .ToListAsync();

        var gruplar = masraflar
            .GroupBy(m => m.KategoriAdi)
            .Select(g => new MasrafKategoriDagilimi
            {
                KategoriAdi = g.Key,
                Tutar = g.Sum(x => x.Tutar),
                Adet = g.Count()
            })
            .OrderByDescending(x => x.Tutar)
            .Take(8) // En yüksek 8 kategori
            .ToList();

        return gruplar;
    }

    public async Task<List<CariTipDagilimi>> GetCariTipDagilimiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            var cariler = await context.Cariler
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Select(c => new
                {
                    c.CariTipi,
                    c.Borc,
                    c.Alacak
                })
                .ToListAsync();

            return cariler
                .GroupBy(c => c.CariTipi)
                .Select(g => new CariTipDagilimi
                {
                    TipAdi = GetCariTipAdi(g.Key),
                    Adet = g.Count(),
                    ToplamBakiye = g.Sum(x => x.Alacak - x.Borc)
                })
                .OrderByDescending(x => x.Adet)
                .ToList();
        }
        catch (PostgresException ex) when (ex.SqlState == "42703")
        {
            // Eski tenant şemasında Borc/Alacak kolonu yoksa yalnızca adet dağılımını döndür.
            var cariler = await context.Cariler
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Select(c => c.CariTipi)
                .ToListAsync();

            return cariler
                .GroupBy(c => c)
                .Select(g => new CariTipDagilimi
                {
                    TipAdi = GetCariTipAdi(g.Key),
                    Adet = g.Count(),
                    ToplamBakiye = 0
                })
                .OrderByDescending(x => x.Adet)
                .ToList();
        }
    }

    public async Task<List<AylikButceVeri>> GetAylikButceAsync(int aySayisi = 6)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var baslangic = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-aySayisi + 1);
        var bitis = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(1).AddDays(-1);

        // Bütçe ödemelerinden aylık veri
        var odemeler = await context.BudgetOdemeler
            .AsNoTracking()
            .Where(o => o.OdemeTarihi >= baslangic && o.OdemeTarihi <= bitis)
            .Select(o => new
            {
                o.OdemeTarihi,
                o.Miktar,
                o.Durum
            })
            .ToListAsync();

        // Son N ayı oluştur
        var sonuc = new List<AylikButceVeri>();
        for (int i = 0; i < aySayisi; i++)
        {
            var tarih = bugun.AddMonths(-aySayisi + 1 + i);
            var yil = tarih.Year;
            var ay = tarih.Month;

            var aylikOdemeler = odemeler.Where(o => 
                o.OdemeTarihi.Year == yil && o.OdemeTarihi.Month == ay);

            var planlanan = aylikOdemeler.Sum(o => o.Miktar);
            var gerceklesen = aylikOdemeler
                .Where(o => o.Durum == OdemeDurum.Odendi)
                .Sum(o => o.Miktar);

            sonuc.Add(new AylikButceVeri
            {
                Yil = yil,
                Ay = ay,
                AyAdi = new DateTime(yil, ay, 1).ToString("MMM", TrCulture),
                PlanlananOdeme = planlanan,
                GerceklesenOdeme = gerceklesen
            });
        }

        return sonuc;
    }

    private static string GetCariTipAdi(CariTipi tip) => tip switch
    {
        CariTipi.Musteri => "Müşteri",
        CariTipi.Tedarikci => "Tedarikçi",
        CariTipi.MusteriTedarikci => "Müşteri/Tedarikçi",
        CariTipi.Personel => "Personel",
        _ => "Diğer"
    };
}



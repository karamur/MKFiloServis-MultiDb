using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public interface IPiyasaKaynakService
{
    Task<List<PiyasaKaynak>> GetAllAsync();
    Task<List<PiyasaKaynak>> GetAktifKaynaklarAsync();
    Task<PiyasaKaynak?> GetByIdAsync(int id);
    Task<PiyasaKaynak?> GetByKodAsync(string kod);
    Task<PiyasaKaynak> CreateAsync(PiyasaKaynak kaynak);
    Task<PiyasaKaynak> UpdateAsync(PiyasaKaynak kaynak);
    Task DeleteAsync(int id);
    Task<bool> KodVarMiAsync(string kod, int? excludeId = null);
    Task SeedDefaultKaynaklarAsync();
}

public class PiyasaKaynakService : IPiyasaKaynakService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<PiyasaKaynakService> _logger;

    public PiyasaKaynakService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<PiyasaKaynakService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<PiyasaKaynak>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            return await context.PiyasaKaynaklar
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Sira)
                .ThenBy(x => x.Ad)
                .ToListAsync();
        }
        catch
        {
            return new List<PiyasaKaynak>();
        }
    }

    public async Task<List<PiyasaKaynak>> GetAktifKaynaklarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            return await context.PiyasaKaynaklar
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Aktif)
                .OrderBy(x => x.Sira)
                .ToListAsync();
        }
        catch
        {
            return new List<PiyasaKaynak>();
        }
    }

    public async Task<PiyasaKaynak?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            return await context.PiyasaKaynaklar
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PiyasaKaynak?> GetByKodAsync(string kod)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            return await context.PiyasaKaynaklar
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Kod == kod && !x.IsDeleted);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PiyasaKaynak> CreateAsync(PiyasaKaynak kaynak)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            kaynak.OlusturmaTarihi = DateTime.UtcNow;
            kaynak.Kod = SlugOlustur(kaynak.Kod);
            context.PiyasaKaynaklar.Add(kaynak);
            await context.SaveChangesAsync();
            return kaynak;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kaynak olusturulamadi: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<PiyasaKaynak> UpdateAsync(PiyasaKaynak kaynak)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var existing = await context.PiyasaKaynaklar.FindAsync(kaynak.Id);
            if (existing == null)
                throw new InvalidOperationException($"Piyasa kaynağı bulunamadı. Id: {kaynak.Id}");

            existing.Kod = SlugOlustur(kaynak.Kod);
            existing.Ad = kaynak.Ad;
            existing.BaseUrl = kaynak.BaseUrl;
            existing.AramaUrl = kaynak.AramaUrl;
            existing.AramaParametreleri = kaynak.AramaParametreleri;
            existing.Selectors = kaynak.Selectors;
            existing.DesteklenenMarkalar = kaynak.DesteklenenMarkalar;
            existing.KaynakTipi = kaynak.KaynakTipi;
            existing.Aktif = kaynak.Aktif;
            existing.Sira = kaynak.Sira;
            existing.IsDeleted = kaynak.IsDeleted;
            existing.GuncellemeTarihi = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kaynak guncellenemedi: {Message}", ex.Message);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var kaynak = await context.PiyasaKaynaklar.FindAsync(id);
            if (kaynak != null)
            {
                kaynak.IsDeleted = true;
                kaynak.GuncellemeTarihi = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kaynak silinemedi: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<bool> KodVarMiAsync(string kod, int? excludeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var slug = SlugOlustur(kod);
            var query = context.PiyasaKaynaklar
                .AsNoTracking()
                .Where(x => x.Kod == slug && !x.IsDeleted);
            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);
            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kod kontrolu sirasinda hata olustu: {Message}", ex.Message);
            return false;
        }
    }

    public async Task SeedDefaultKaynaklarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            await context.PiyasaKaynaklar.AsNoTracking().AnyAsync();
        }
        catch
        {
            // Tablo yoksa sessizce cik - migration henuz yapilmamis
            _logger.LogWarning("PiyasaKaynaklar tablosu henuz olusturulmamis, seed atlaniyor.");
            return;
        }

        try
        {
            var mevcutSayisi = await context.PiyasaKaynaklar
                .AsNoTracking()
                .CountAsync();
            if (mevcutSayisi > 0) return;

            var defaultKaynaklar = GetDefaultKaynaklar();
            context.PiyasaKaynaklar.AddRange(defaultKaynaklar);
            await context.SaveChangesAsync();
            _logger.LogInformation("{Count} varsayilan piyasa kaynagi eklendi", defaultKaynaklar.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Piyasa kaynaklari seed edilemedi");
        }
    }

    private List<PiyasaKaynak> GetDefaultKaynaklar()
    {
        return new List<PiyasaKaynak>
        {
            // Ana Pazaryerleri
            new PiyasaKaynak
            {
                Ad = "Sahibinden.com",
                Kod = "sahibinden",
                BaseUrl = "https://www.sahibinden.com",
                AramaUrl = "/{kategori}-{marka}-{model}",
                KaynakTipi = "Genel",
                Sira = 1,
                Aktif = true,
                Selectors = JsonSerializer.Serialize(new PiyasaKaynakSelector
                {
                    IlanListesi = "tr.searchResultsItem, tbody tr[data-id]",
                    IlanLink = "td.searchResultsTitleValue a",
                    IlanBaslik = "td.searchResultsTitleValue a",
                    Fiyat = "td.searchResultsPriceValue span",
                    Yil = "td.searchResultsAttributeValue:nth-child(1)",
                    Kilometre = "td.searchResultsAttributeValue:nth-child(2)",
                    Resim = "td.searchResultsLargeThumbnail img",
                    Konum = "td.searchResultsLocationValue",
                    Tarih = "td.searchResultsDateValue",
                    CookieKabul = "#onetrust-accept-btn-handler"
                })
            },
            new PiyasaKaynak
            {
                Ad = "Arabam.com",
                Kod = "arabam",
                BaseUrl = "https://www.arabam.com",
                AramaUrl = "/ikinci-el/{marka}-{model}",
                KaynakTipi = "Genel",
                Sira = 2,
                Aktif = true,
                Selectors = JsonSerializer.Serialize(new PiyasaKaynakSelector
                {
                    IlanListesi = "tr[class*='listing-list-item'], .listing-item",
                    IlanLink = "a[href*='/ilan/']",
                    IlanBaslik = "h3, .listing-title",
                    Fiyat = "[class*='price'], .listing-price",
                    Resim = "img[src*='arabam']",
                    Konum = "[class*='location']",
                    CookieKabul = "button[data-testid='accept-all-cookies']"
                })
            },
            
            // Diger Pazaryerleri
            new PiyasaKaynak
            {
                Ad = "Otoshops",
                Kod = "otoshops",
                BaseUrl = "https://www.otoshops.com",
                AramaUrl = "/ikinci-el-arac/{marka}/{model}",
                KaynakTipi = "Genel",
                Sira = 3,
                Aktif = false // Varsayilan olarak kapali
            },
            new PiyasaKaynak
            {
                Ad = "Cardata",
                Kod = "cardata",
                BaseUrl = "https://www.cardata.com.tr",
                AramaUrl = "/arac-ara",
                KaynakTipi = "Galeri",
                Sira = 4,
                Aktif = false // Varsayilan olarak kapali
            },

            // Yetkili Bayi Siteleri
            new PiyasaKaynak
            {
                Ad = "Spoticar",
                Kod = "spoticar",
                BaseUrl = "https://www.spoticar.com.tr",
                AramaUrl = "/arac-bul",
                KaynakTipi = "YetkiliBayi",
                DesteklenenMarkalar = "Peugeot,Citroen,Opel,DS",
                Sira = 5,
                Aktif = false
            },
            new PiyasaKaynak
            {
                Ad = "Borusan Ikinci El",
                Kod = "borusan",
                BaseUrl = "https://www.borusanikinciel.com",
                AramaUrl = "/ikinci-el-araclar",
                KaynakTipi = "YetkiliBayi",
                DesteklenenMarkalar = "BMW,Mini,Land Rover,Jaguar",
                Sira = 6,
                Aktif = false
            },
            new PiyasaKaynak
            {
                Ad = "DOD",
                Kod = "dod",
                BaseUrl = "https://www.dod.com.tr",
                AramaUrl = "/ikinci-el-araclar",
                KaynakTipi = "YetkiliBayi",
                DesteklenenMarkalar = "Volkswagen,Audi,Seat,Skoda,Porsche",
                Sira = 7,
                Aktif = false
            },
            new PiyasaKaynak
            {
                Ad = "Ford Ikinci El",
                Kod = "ford2el",
                BaseUrl = "https://www.fordikinciel.com",
                AramaUrl = "/ikinci-el-ford",
                KaynakTipi = "YetkiliBayi",
                DesteklenenMarkalar = "Ford",
                Sira = 8,
                Aktif = true
            },
            new PiyasaKaynak
            {
                Ad = "Otokoc 2. El",
                Kod = "otokoc",
                BaseUrl = "https://www.otokoc2el.com.tr",
                AramaUrl = "/ikinci-el-araclar",
                KaynakTipi = "YetkiliBayi",
                DesteklenenMarkalar = "Toyota,Lexus",
                Sira = 9,
                Aktif = true
            },
            new PiyasaKaynak
            {
                Ad = "Dogus Oto 2. El",
                Kod = "dogusoto",
                BaseUrl = "https://www.dogusotomotiv2el.com",
                AramaUrl = "/arac-listesi",
                KaynakTipi = "YetkiliBayi",
                DesteklenenMarkalar = "Volkswagen,Audi,Seat,Skoda,Porsche",
                Sira = 10,
                Aktif = true
            },

            // Yeni Eklenen Kaynaklar
            new PiyasaKaynak
            {
                Ad = "Otosor",
                Kod = "otosor",
                BaseUrl = "https://www.otosor.com",
                AramaUrl = "/ikinci-el-arac",
                KaynakTipi = "Galeri",
                Sira = 11,
                Aktif = true,
                Selectors = JsonSerializer.Serialize(new PiyasaKaynakSelector
                {
                    IlanListesi = ".car-card, .vehicle-item, .listing-item",
                    IlanLink = "a[href*='/ilan'], a[href*='/arac']",
                    Fiyat = "[class*='price'], .price",
                    Resim = "img[src*='otosor'], img"
                })
            },
            new PiyasaKaynak
            {
                Ad = "Otoplus",
                Kod = "otoplus",
                BaseUrl = "https://www.otoplus.com",
                AramaUrl = "/arac-listesi",
                KaynakTipi = "Galeri",
                Sira = 12,
                Aktif = true,
                Selectors = JsonSerializer.Serialize(new PiyasaKaynakSelector
                {
                    IlanListesi = ".car-card, .vehicle-item, .listing-item",
                    IlanLink = "a[href*='/ilan'], a[href*='/arac']",
                    Fiyat = "[class*='price'], .price",
                    Resim = "img"
                })
            },
            new PiyasaKaynak
            {
                Ad = "VavaCars",
                Kod = "vavacars",
                BaseUrl = "https://www.vavacars.com",
                AramaUrl = "/arac-satin-al",
                KaynakTipi = "Galeri",
                Sira = 13,
                Aktif = true,
                Selectors = JsonSerializer.Serialize(new PiyasaKaynakSelector
                {
                    IlanListesi = ".car-card, .vehicle-card, [class*='car-item']",
                    IlanLink = "a[href*='/arac'], a[href*='/detay']",
                    Fiyat = "[class*='price'], .price",
                    Resim = "img[src*='vavacars'], img"
                })
            },
            new PiyasaKaynak
            {
                Ad = "Otocars",
                Kod = "otocars",
                BaseUrl = "https://www.otocars.com.tr",
                AramaUrl = "/araclar",
                KaynakTipi = "Galeri",
                Sira = 14,
                Aktif = true,
                Selectors = JsonSerializer.Serialize(new PiyasaKaynakSelector
                {
                    IlanListesi = ".car-card, .vehicle-item",
                    IlanLink = "a[href*='/arac']",
                    Fiyat = "[class*='price']",
                    Resim = "img"
                })
            },

            // Sıfır Araç Siteleri
            new PiyasaKaynak
            {
                Ad = "Toyota Türkiye",
                Kod = "toyota",
                BaseUrl = "https://www.toyota.com.tr",
                AramaUrl = "/modeller",
                KaynakTipi = "Sifir",
                DesteklenenMarkalar = "Toyota",
                Sira = 20,
                Aktif = false
            },
            new PiyasaKaynak
            {
                Ad = "Renault Türkiye",
                Kod = "renault",
                BaseUrl = "https://www.renault.com.tr",
                AramaUrl = "/araclar",
                KaynakTipi = "Sifir",
                DesteklenenMarkalar = "Renault",
                Sira = 21,
                Aktif = false
            }
        };
    }

    private string SlugOlustur(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.ToLower()
            .Replace("ı", "i").Replace("ö", "o").Replace("ü", "u")
            .Replace("ş", "s").Replace("ğ", "g").Replace("ç", "c")
            .Replace(" ", "-").Replace(".", "").Replace(",", "")
            .Replace("--", "-").Trim('-');
    }
}




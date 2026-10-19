using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Genel arama (kenar çubuğu). Türkçe harf ve büyük/küçük harf duyarsızdır ("ali" → ALİ, "filo" → FİLO,
/// "sofor" → ŞOFÖR) ve çok kelimeli aramada kelimelerin hepsini ister ("ali aydın").
/// Veritabanı yalnızca kaba bir LIKE deseniyle aday getirir; kesin eşleştirme ve puanlama
/// <see cref="AramaMetni"/> ile bellekte yapılır (ayrıntı için o sınıfa bakın).
/// </summary>
public class GlobalSearchService : IGlobalSearchService
{
    /// <summary>Kaba süzgeçten geçen adaylardan en fazla bu kadarı belleğe alınır.</summary>
    private const int KabaAdayLimiti = 400;

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly LicenseService _licenses;
    private readonly IKullaniciService _kullaniciService;

    public GlobalSearchService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        LicenseService licenses,
        IKullaniciService kullaniciService)
    {
        _contextFactory = contextFactory;
        _licenses = licenses;
        _kullaniciService = kullaniciService;
    }

    public async Task<GlobalSearchResult> SearchAsync(string searchTerm, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Trim().Length < 2)
            return new GlobalSearchResult();

        var kelimeler = AramaMetni.Kelimeler(searchTerm);
        if (kelimeler.Count == 0)
            return new GlobalSearchResult();

        var result = new GlobalSearchResult();
        var permissions = await _kullaniciService.GetCurrentUserYetkilerAsync();

        bool CanSearch(string permission, string module) =>
            _licenses.HasModulePermission(module) &&
            (permissions.Contains(permission) || permissions.Contains("*"));

        // Her paralel çağrı için ayrı DbContext kullan (thread-safe)
        var cariTask = CanSearch(Yetkiler.CarilerOku, "cari") ? Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await SearchCarilerAsync(context, kelimeler, maxResults);
        }) : Task.FromResult(new List<SearchResultItem>());

        var aracTask = CanSearch(Yetkiler.AraclarOku, "filoservis") ? Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await SearchAraclarAsync(context, kelimeler, maxResults);
        }) : Task.FromResult(new List<SearchResultItem>());

        var personelTask = CanSearch(Yetkiler.PersonelOku, "personel") ? Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await SearchPersonellerAsync(context, kelimeler, maxResults);
        }) : Task.FromResult(new List<SearchResultItem>());

        var faturaTask = CanSearch(Yetkiler.FaturalarOku, "fatura") ? Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await SearchFaturalarAsync(context, kelimeler, maxResults);
        }) : Task.FromResult(new List<SearchResultItem>());

        var guzergahTask = CanSearch(Yetkiler.GuzergahlarOku, "filoservis") ? Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await SearchGuzergahlarAsync(context, kelimeler, maxResults);
        }) : Task.FromResult(new List<SearchResultItem>());

        await Task.WhenAll(cariTask, aracTask, personelTask, faturaTask, guzergahTask);

        result.Cariler = await cariTask;
        result.Araclar = await aracTask;
        result.Personeller = await personelTask;
        result.Faturalar = await faturaTask;
        result.Guzergahlar = await guzergahTask;

        return result;
    }

    internal async Task<List<SearchResultItem>> SearchCarilerAsync(ApplicationDbContext context, IReadOnlyList<string> kelimeler, int max)
    {
        var desen = AramaMetni.KabaDesen(AramaMetni.EnUzun(kelimeler));
        var adaylar = await context.Cariler
            .AsNoTracking()
            .Where(c => EF.Functions.Like(c.CariKodu.ToLower(), desen, AramaMetni.KacisKarakteri)
                     || EF.Functions.Like(c.Unvan.ToLower(), desen, AramaMetni.KacisKarakteri)
                     || (c.VergiNo != null && EF.Functions.Like(c.VergiNo, desen, AramaMetni.KacisKarakteri))
                     || (c.TcKimlikNo != null && EF.Functions.Like(c.TcKimlikNo, desen, AramaMetni.KacisKarakteri))
                     || (c.Telefon != null && EF.Functions.Like(c.Telefon, desen, AramaMetni.KacisKarakteri)))
            .Take(KabaAdayLimiti)
            .ToListAsync();

        return adaylar
            .Where(c => AramaMetni.HepsiEslesir(kelimeler, c.Unvan, c.CariKodu, c.VergiNo, c.TcKimlikNo, c.Telefon))
            .Select(c => new SearchResultItem
            {
                Id = c.Id,
                Baslik = c.Unvan,
                AltBaslik = $"{c.CariKodu} - {c.CariTipi}",
                Kategori = "Cari",
                Icon = "bi-people",
                Url = $"/cariler/{c.Id}",
                BadgeClass = "bg-primary",
                Skor = AramaMetni.Puan(kelimeler, c.Unvan, c.CariKodu)
            })
            .OrderByDescending(x => x.Skor)
            .ThenBy(x => x.Baslik, StringComparer.CurrentCultureIgnoreCase)
            .Take(max)
            .ToList();
    }

    internal async Task<List<SearchResultItem>> SearchAraclarAsync(ApplicationDbContext context, IReadOnlyList<string> kelimeler, int max)
    {
        var desen = AramaMetni.KabaDesen(AramaMetni.EnUzun(kelimeler));
        var adaylar = await context.Araclar
            .AsNoTracking()
            .Where(a => (a.AktifPlaka != null && EF.Functions.Like(a.AktifPlaka.ToLower(), desen, AramaMetni.KacisKarakteri))
                     // plaka "34 ABC 123" kayıtlıyken "34abc123" yazılabilir
                     || (a.AktifPlaka != null && EF.Functions.Like(a.AktifPlaka.ToLower().Replace(" ", ""), desen, AramaMetni.KacisKarakteri))
                     || (a.Marka != null && EF.Functions.Like(a.Marka.ToLower(), desen, AramaMetni.KacisKarakteri))
                     || (a.Model != null && EF.Functions.Like(a.Model.ToLower(), desen, AramaMetni.KacisKarakteri)))
            .Take(KabaAdayLimiti)
            .ToListAsync();

        return adaylar
            .Where(a => AramaMetni.HepsiEslesir(kelimeler, a.AktifPlaka, a.Marka, a.Model))
            .Select(a => new SearchResultItem
            {
                Id = a.Id,
                Baslik = a.AktifPlaka ?? string.Empty,
                AltBaslik = $"{a.Marka} {a.Model} - {a.AracTipi}",
                Kategori = "Araç",
                Icon = "bi-truck",
                Url = $"/araclar/{a.Id}",
                BadgeClass = "bg-success",
                Skor = AramaMetni.Puan(kelimeler, a.AktifPlaka, a.Marka)
            })
            .OrderByDescending(x => x.Skor)
            .ThenBy(x => x.Baslik, StringComparer.CurrentCultureIgnoreCase)
            .Take(max)
            .ToList();
    }

    internal async Task<List<SearchResultItem>> SearchPersonellerAsync(ApplicationDbContext context, IReadOnlyList<string> kelimeler, int max)
    {
        var desen = AramaMetni.KabaDesen(AramaMetni.EnUzun(kelimeler));
        var adaylar = await context.Soforler
            .AsNoTracking()
            .Where(s => EF.Functions.Like(s.SoforKodu.ToLower(), desen, AramaMetni.KacisKarakteri)
                     || EF.Functions.Like(s.Ad.ToLower(), desen, AramaMetni.KacisKarakteri)
                     || EF.Functions.Like(s.Soyad.ToLower(), desen, AramaMetni.KacisKarakteri)
                     || (s.TcKimlikNo != null && EF.Functions.Like(s.TcKimlikNo, desen, AramaMetni.KacisKarakteri))
                     || (s.Telefon != null && EF.Functions.Like(s.Telefon, desen, AramaMetni.KacisKarakteri)))
            .Take(KabaAdayLimiti)
            .ToListAsync();

        // "ali aydın" gibi ad + soyad birlikte yazılırsa kelimeler iki alana dağılır: birleşik metinde aranır.
        return adaylar
            .Where(s => AramaMetni.HepsiEslesir(kelimeler, s.Ad, s.Soyad, s.SoforKodu, s.TcKimlikNo, s.Telefon))
            .Select(p => new SearchResultItem
            {
                Id = p.Id,
                Baslik = p.TamAd,
                AltBaslik = $"{p.SoforKodu} - {GetGorevAdi(p.Gorev)}",
                Kategori = "Personel",
                Icon = "bi-person-badge",
                Url = $"/soforler/{p.Id}",
                BadgeClass = "bg-warning text-dark",
                Skor = AramaMetni.Puan(kelimeler, p.TamAd, p.SoforKodu)
            })
            .OrderByDescending(x => x.Skor)
            .ThenBy(x => x.Baslik, StringComparer.CurrentCultureIgnoreCase)
            .Take(max)
            .ToList();
    }

    internal async Task<List<SearchResultItem>> SearchFaturalarAsync(ApplicationDbContext context, IReadOnlyList<string> kelimeler, int max)
    {
        var desen = AramaMetni.KabaDesen(AramaMetni.EnUzun(kelimeler));
        var adaylar = await context.Faturalar
            .AsNoTracking()
            .Include(f => f.Cari)
            .Where(f => EF.Functions.Like(f.FaturaNo.ToLower(), desen, AramaMetni.KacisKarakteri)
                     || EF.Functions.Like(f.Cari.Unvan.ToLower(), desen, AramaMetni.KacisKarakteri))
            .OrderByDescending(f => f.FaturaTarihi)
            .Take(KabaAdayLimiti)
            .ToListAsync();

        return adaylar
            .Where(f => AramaMetni.HepsiEslesir(kelimeler, f.FaturaNo, f.Cari?.Unvan))
            .Select(f => new SearchResultItem
            {
                Id = f.Id,
                Baslik = f.FaturaNo,
                AltBaslik = $"{f.Cari?.Unvan} - {f.GenelToplam:N0} ₺",
                Kategori = "Fatura",
                Icon = "bi-receipt",
                Url = $"/faturalar/{f.Id}",
                BadgeClass = "bg-info",
                Skor = AramaMetni.Puan(kelimeler, f.FaturaNo, f.Cari?.Unvan)
            })
            .OrderByDescending(x => x.Skor)
            .Take(max)
            .ToList();
    }

    internal async Task<List<SearchResultItem>> SearchGuzergahlarAsync(ApplicationDbContext context, IReadOnlyList<string> kelimeler, int max)
    {
        var desen = AramaMetni.KabaDesen(AramaMetni.EnUzun(kelimeler));
        var adaylar = await context.Guzergahlar
            .AsNoTracking()
            .Include(g => g.Cari)
            .Where(g => EF.Functions.Like(g.GuzergahKodu.ToLower(), desen, AramaMetni.KacisKarakteri)
                     || EF.Functions.Like(g.GuzergahAdi.ToLower(), desen, AramaMetni.KacisKarakteri)
                     || EF.Functions.Like(g.Cari.Unvan.ToLower(), desen, AramaMetni.KacisKarakteri))
            .Take(KabaAdayLimiti)
            .ToListAsync();

        return adaylar
            .Where(g => AramaMetni.HepsiEslesir(kelimeler, g.GuzergahAdi, g.GuzergahKodu, g.Cari?.Unvan))
            .Select(g => new SearchResultItem
            {
                Id = g.Id,
                Baslik = g.GuzergahAdi,
                AltBaslik = $"{g.GuzergahKodu} - {g.Cari?.Unvan}",
                Kategori = "Güzergah",
                Icon = "bi-signpost-split",
                Url = $"/guzergahlar/{g.Id}",
                BadgeClass = "bg-secondary",
                Skor = AramaMetni.Puan(kelimeler, g.GuzergahAdi, g.GuzergahKodu)
            })
            .OrderByDescending(x => x.Skor)
            .ThenBy(x => x.Baslik, StringComparer.CurrentCultureIgnoreCase)
            .Take(max)
            .ToList();
    }

    private string GetGorevAdi(PersonelGorev gorev)
    {
        return gorev switch
        {
            PersonelGorev.Sofor => "Şoför",
            PersonelGorev.OfisCalisani => "Ofis Çalışanı",
            PersonelGorev.Muhasebe => "Muhasebe",
            PersonelGorev.Yonetici => "Yönetici",
            PersonelGorev.Teknik => "Teknik",
            PersonelGorev.Diger => "Diğer",
            _ => gorev.ToString()
        };
    }
}

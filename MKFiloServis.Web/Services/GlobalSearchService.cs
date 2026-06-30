using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class GlobalSearchService : IGlobalSearchService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public GlobalSearchService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<GlobalSearchResult> SearchAsync(string searchTerm, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return new GlobalSearchResult();

        var result = new GlobalSearchResult();
        var term = searchTerm.ToLower().Trim();

        // Her paralel çağrı için ayrı DbContext kullan (thread-safe)
        var cariTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await SearchCarilerAsync(context, term, maxResults);
        });

        var aracTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await SearchAraclarAsync(context, term, maxResults);
        });

        var personelTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await SearchPersonellerAsync(context, term, maxResults);
        });

        var faturaTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await SearchFaturalarAsync(context, term, maxResults);
        });

        var guzergahTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await SearchGuzergahlarAsync(context, term, maxResults);
        });

        await Task.WhenAll(cariTask, aracTask, personelTask, faturaTask, guzergahTask);

        result.Cariler = await cariTask;
        result.Araclar = await aracTask;
        result.Personeller = await personelTask;
        result.Faturalar = await faturaTask;
        result.Guzergahlar = await guzergahTask;

        return result;
    }

    private async Task<List<SearchResultItem>> SearchCarilerAsync(ApplicationDbContext context, string term, int max)
    {
        var cariler = await context.Cariler
            .Where(c => c.CariKodu.ToLower().Contains(term) ||
                       c.Unvan.ToLower().Contains(term) ||
                       (c.VergiNo != null && c.VergiNo.Contains(term)) ||
                       (c.Telefon != null && c.Telefon.Contains(term)))
            .Take(max)
            .ToListAsync();

        return cariler.Select(c => new SearchResultItem
        {
            Id = c.Id,
            Baslik = c.Unvan,
            AltBaslik = $"{c.CariKodu} - {c.CariTipi}",
            Kategori = "Cari",
            Icon = "bi-people",
            Url = $"/cariler/{c.Id}",
            BadgeClass = "bg-primary",
            Skor = CalculateScore(term, c.Unvan, c.CariKodu)
        }).ToList();
    }

    private async Task<List<SearchResultItem>> SearchAraclarAsync(ApplicationDbContext context, string term, int max)
    {
        var araclar = await context.Araclar
            .Where(a => (a.AktifPlaka != null && a.AktifPlaka.ToLower().Contains(term)) ||
                       (a.Marka != null && a.Marka.ToLower().Contains(term)) ||
                       (a.Model != null && a.Model.ToLower().Contains(term)))
            .Take(max)
            .ToListAsync();

        return araclar.Select(a => new SearchResultItem
        {
            Id = a.Id,
            Baslik = a.AktifPlaka ?? string.Empty,
            AltBaslik = $"{a.Marka} {a.Model} - {a.AracTipi}",
            Kategori = "Araç",
            Icon = "bi-truck",
            Url = $"/araclar/{a.Id}",
            BadgeClass = "bg-success",
            Skor = CalculateScore(term, a.AktifPlaka ?? string.Empty, a.Marka ?? "")
        }).ToList();
    }

    private async Task<List<SearchResultItem>> SearchPersonellerAsync(ApplicationDbContext context, string term, int max)
    {
        var personeller = await context.Soforler
            .Where(s => s.SoforKodu.ToLower().Contains(term) ||
                       s.Ad.ToLower().Contains(term) ||
                       s.Soyad.ToLower().Contains(term) ||
                       (s.TcKimlikNo != null && s.TcKimlikNo.Contains(term)) ||
                       (s.Telefon != null && s.Telefon.Contains(term)))
            .Take(max)
            .ToListAsync();

        return personeller.Select(p => new SearchResultItem
        {
            Id = p.Id,
            Baslik = p.TamAd,
            AltBaslik = $"{p.SoforKodu} - {GetGorevAdi(p.Gorev)}",
            Kategori = "Personel",
            Icon = "bi-person-badge",
            Url = $"/soforler/{p.Id}",
            BadgeClass = "bg-warning text-dark",
            Skor = CalculateScore(term, p.TamAd, p.SoforKodu)
        }).ToList();
    }

    private async Task<List<SearchResultItem>> SearchFaturalarAsync(ApplicationDbContext context, string term, int max)
    {
        var faturalar = await context.Faturalar
            .Include(f => f.Cari)
            .Where(f => f.FaturaNo.ToLower().Contains(term) ||
                       f.Cari.Unvan.ToLower().Contains(term))
            .Take(max)
            .ToListAsync();

        return faturalar.Select(f => new SearchResultItem
        {
            Id = f.Id,
            Baslik = f.FaturaNo,
            AltBaslik = $"{f.Cari?.Unvan} - {f.GenelToplam:N0} ₺",
            Kategori = "Fatura",
            Icon = "bi-receipt",
            Url = $"/faturalar/{f.Id}",
            BadgeClass = "bg-info",
            Skor = CalculateScore(term, f.FaturaNo, f.Cari?.Unvan ?? "")
        }).ToList();
    }

    private async Task<List<SearchResultItem>> SearchGuzergahlarAsync(ApplicationDbContext context, string term, int max)
    {
        var guzergahlar = await context.Guzergahlar
            .Include(g => g.Cari)
            .Where(g => g.GuzergahKodu.ToLower().Contains(term) ||
                       g.GuzergahAdi.ToLower().Contains(term) ||
                       g.Cari.Unvan.ToLower().Contains(term))
            .Take(max)
            .ToListAsync();

        return guzergahlar.Select(g => new SearchResultItem
        {
            Id = g.Id,
            Baslik = g.GuzergahAdi,
            AltBaslik = $"{g.GuzergahKodu} - {g.Cari?.Unvan}",
            Kategori = "Güzergah",
            Icon = "bi-signpost-split",
            Url = $"/guzergahlar/{g.Id}",
            BadgeClass = "bg-secondary",
            Skor = CalculateScore(term, g.GuzergahAdi, g.GuzergahKodu)
        }).ToList();
    }

    private int CalculateScore(string term, string primary, string secondary)
    {
        int score = 0;
        var termLower = term.ToLower();
        var primaryLower = primary.ToLower();
        var secondaryLower = secondary.ToLower();

        // Tam eşleşme en yüksek skor
        if (primaryLower == termLower) score += 100;
        else if (primaryLower.StartsWith(termLower)) score += 80;
        else if (primaryLower.Contains(termLower)) score += 50;

        if (secondaryLower == termLower) score += 60;
        else if (secondaryLower.StartsWith(termLower)) score += 40;
        else if (secondaryLower.Contains(termLower)) score += 20;

        return score;
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




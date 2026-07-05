using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public sealed class BrandingService
{
    public const string DefaultIconPath = "images/logo.png";
    public const string DefaultTextLogoPath = "images/YaziLogo.png";

    private const string IconKey = "Branding.IconLogo";
    private const string TextLogoKey = "Branding.TextLogo";

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public BrandingService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<BrandingSettings> GetAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.AppAyarlari
            .AsNoTracking()
            .Where(x => x.Anahtar == IconKey || x.Anahtar == TextLogoKey)
            .ToListAsync();

        return new BrandingSettings
        {
            IconLogo = settings.FirstOrDefault(x => x.Anahtar == IconKey)?.Deger,
            TextLogo = settings.FirstOrDefault(x => x.Anahtar == TextLogoKey)?.Deger
        };
    }

    public async Task SaveAsync(string? iconLogo, string? textLogo)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        await UpsertAsync(context, IconKey, Normalize(iconLogo), "Giris ve menu icin amblem/logo");
        await UpsertAsync(context, TextLogoKey, Normalize(textLogo), "Giris ve menu icin yazi logosu");

        await context.SaveChangesAsync();
    }

    private static async Task UpsertAsync(ApplicationDbContext context, string key, string? value, string description)
    {
        var existing = await context.AppAyarlari.FirstOrDefaultAsync(x => x.Anahtar == key);
        if (existing == null)
        {
            context.AppAyarlari.Add(new AppAyarlari
            {
                Anahtar = key,
                Deger = value ?? string.Empty,
                Aciklama = description,
                Kategori = "Branding",
                GuncellenmeTarihi = DateTime.UtcNow
            });
            return;
        }

        existing.Deger = value ?? string.Empty;
        existing.Aciklama = description;
        existing.Kategori = "Branding";
        existing.GuncellenmeTarihi = DateTime.UtcNow;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class BrandingSettings
{
    public string? IconLogo { get; set; }
    public string? TextLogo { get; set; }

    public string ResolvedIconLogo => string.IsNullOrWhiteSpace(IconLogo)
        ? BrandingService.DefaultIconPath
        : IconLogo;

    public string ResolvedTextLogo => string.IsNullOrWhiteSpace(TextLogo)
        ? BrandingService.DefaultTextLogoPath
        : TextLogo;
}

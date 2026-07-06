using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public sealed class BrandingService
{
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
            TextLogo = settings
                .Where(x => x.Anahtar == TextLogoKey)
                .OrderByDescending(x => x.GuncellenmeTarihi)
                .Select(x => x.Deger)
                .FirstOrDefault()
                ?? settings
                    .Where(x => x.Anahtar == IconKey)
                    .OrderByDescending(x => x.GuncellenmeTarihi)
                    .Select(x => x.Deger)
                    .FirstOrDefault()
        };
    }

    public async Task SaveAsync(string? textLogo)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var normalizedLogo = Normalize(textLogo);
        await UpsertAsync(context, IconKey, normalizedLogo, "Giris ve menu icin amblem/logo");
        await UpsertAsync(context, TextLogoKey, normalizedLogo, "Giris ve menu icin yazi logosu");

        await context.SaveChangesAsync();
    }

    private static async Task UpsertAsync(ApplicationDbContext context, string key, string? value, string description)
    {
        var existingEntries = await context.AppAyarlari
            .AsTracking()
            .Where(x => x.Anahtar == key)
            .OrderByDescending(x => x.GuncellenmeTarihi)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        var existing = existingEntries.FirstOrDefault();
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

        if (existingEntries.Count > 1)
        {
            context.AppAyarlari.RemoveRange(existingEntries.Skip(1));
        }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class BrandingSettings
{
    public string? TextLogo { get; set; }

    public string ResolvedTextLogo => string.IsNullOrWhiteSpace(TextLogo)
        ? BrandingService.DefaultTextLogoPath
        : TextLogo;
}

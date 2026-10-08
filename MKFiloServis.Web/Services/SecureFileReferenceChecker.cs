using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public sealed class SecureFileReferenceChecker : ISecureFileReferenceChecker
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly string _storageRoot;

    public SecureFileReferenceChecker(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IWebHostEnvironment environment)
    {
        _contextFactory = contextFactory;
        _storageRoot = AppStoragePaths.GetStorageRoot(environment.ContentRootPath);
    }

    public async Task<bool> IsReferencedAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var aliases = SecureFileOrphanScanner.GetStorageKeyAliases(_storageRoot, relativePath);
        if (aliases.Count == 0)
            return true; // Invalid/unknown paths fail closed: never auto-delete them.

        if (await DatabaseFilePathInventory.IsReferencedAsync(db, aliases, cancellationToken))
            return true;

        // Windows path karşılaştırması harf/ayraç farkını DB tarafında kontrol eder;
        // her silme için bütün path sütunlarını uygulamaya taşımak gerekmez.
        if (await DatabaseFilePathInventory.IsReferencedNormalizedAsync(
                db, aliases, OperatingSystem.IsWindows(), cancellationToken))
            return true;

        return false;
    }
}

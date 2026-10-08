using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Services;

internal static class DatabaseFilePathInventory
{
    private static readonly HashSet<string> PathPropertyNames = new(StringComparer.Ordinal)
    {
        "DosyaYolu", "PdfDosyaYolu", "XmlDosyaYolu"
    };

    private static readonly MethodInfo ReadPropertyMethod = typeof(DatabaseFilePathInventory)
        .GetMethod(nameof(ReadPropertyAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static async Task<HashSet<string>> ReadAllAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entityType in db.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.FindPrimaryKey() is null)
                continue;

            foreach (var property in entityType.GetProperties()
                         .Where(property => property.ClrType == typeof(string) && PathPropertyNames.Contains(property.Name)))
            {
                ct.ThrowIfCancellationRequested();
                var method = ReadPropertyMethod.MakeGenericMethod(entityType.ClrType);
                var readTask = (Task<List<string>>)method.Invoke(null, [db, property.Name, ct])!;
                paths.UnionWith(await readTask);
            }
        }

        return paths;
    }

    public static async Task<bool> IsReferencedAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<string> pathAliases,
        CancellationToken ct)
    {
        foreach (var entityType in db.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.FindPrimaryKey() is null)
                continue;

            foreach (var property in entityType.GetProperties()
                         .Where(property => property.ClrType == typeof(string) && PathPropertyNames.Contains(property.Name)))
            {
                ct.ThrowIfCancellationRequested();
                var method = typeof(DatabaseFilePathInventory)
                    .GetMethod(nameof(HasPropertyMatchAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                var checkTask = (Task<bool>)method.Invoke(null, [db, property.Name, pathAliases, ct])!;
                if (await checkTask)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Windows disk yolu karşılaştırması: ayraç ve harf farklarını DB tarafında
    /// normalize eder. Böylece silme başına tüm dosya yolu envanteri belleğe alınmaz.
    /// </summary>
    public static Task<bool> IsReferencedIgnoringCaseAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<string> pathAliases,
        CancellationToken ct)
        => IsReferencedNormalizedAsync(db, pathAliases, true, ct);

    public static async Task<bool> IsReferencedNormalizedAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<string> pathAliases,
        bool ignoreCase,
        CancellationToken ct)
    {
        var normalizedAliases = pathAliases
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => ignoreCase ? NormalizePathForComparison(path) : path.Trim().Replace('\\', '/'))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedAliases.Length == 0)
            return true;
        // SQLite lower() is ASCII-only; DB collations also need not match Windows
        // OrdinalIgnoreCase. Preserve non-ASCII file names with a bounded-memory
        // fallback rather than interpreting a SQL miss as permission to delete.
        var ordinalAliases = ignoreCase && pathAliases.Any(path => path.Any(ch => ch > 127))
            ? new HashSet<string>(pathAliases.Select(path => path.Trim().Replace('\\', '/')), StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var entityType in db.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.FindPrimaryKey() is null)
                continue;

            foreach (var property in entityType.GetProperties()
                         .Where(property => property.ClrType == typeof(string) && PathPropertyNames.Contains(property.Name)))
            {
                ct.ThrowIfCancellationRequested();
                var method = typeof(DatabaseFilePathInventory)
                    .GetMethod(nameof(HasNormalizedPropertyMatchAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                var checkTask = (Task<bool>)method.Invoke(null, [db, property.Name, normalizedAliases, ignoreCase, ct])!;
                if (await checkTask)
                    return true;
                if (ordinalAliases is not null)
                {
                    var ordinalMethod = typeof(DatabaseFilePathInventory)
                        .GetMethod(nameof(HasOrdinalPropertyMatchAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(entityType.ClrType);
                    if (await (Task<bool>)ordinalMethod.Invoke(null, [db, property.Name, ordinalAliases, ct])!)
                        return true;
                }
            }
        }

        return false;
    }

    private static async Task<List<string>> ReadPropertyAsync<TEntity>(
        ApplicationDbContext db,
        string propertyName,
        CancellationToken ct)
        where TEntity : class
        => await db.Set<TEntity>().IgnoreQueryFilters().AsNoTracking()
            .Select(entity => EF.Property<string?>(entity, propertyName))
            .Where(path => path != null)
            .Select(path => path!)
            .Distinct()
            .ToListAsync(ct);

    private static async Task<bool> HasPropertyMatchAsync<TEntity>(
        ApplicationDbContext db,
        string propertyName,
        IReadOnlyCollection<string> pathAliases,
        CancellationToken ct)
        where TEntity : class
        => await db.Set<TEntity>().IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(entity => pathAliases.Contains(EF.Property<string?>(entity, propertyName)), ct);

    private static async Task<bool> HasNormalizedPropertyMatchAsync<TEntity>(
        ApplicationDbContext db,
        string propertyName,
        IReadOnlyCollection<string> normalizedAliases,
        bool ignoreCase,
        CancellationToken ct)
        where TEntity : class
    {
        var query = db.Set<TEntity>().IgnoreQueryFilters().AsNoTracking();
        if (!ignoreCase)
            return await query.AnyAsync(entity => EF.Property<string?>(entity, propertyName) != null &&
                normalizedAliases.Contains(EF.Property<string?>(entity, propertyName)!.Trim().Replace("\\", "/")), ct);

        return await query
            .AnyAsync(entity => EF.Property<string?>(entity, propertyName) != null &&
                normalizedAliases.Contains(
                    EF.Property<string?>(entity, propertyName)!.Trim().Replace("\\", "/").ToLower()), ct);
    }

    private static string NormalizePathForComparison(string path) =>
        path.Trim().Replace('\\', '/').ToLowerInvariant();

    private static async Task<bool> HasOrdinalPropertyMatchAsync<TEntity>(ApplicationDbContext db,
        string propertyName, HashSet<string> aliases, CancellationToken ct) where TEntity : class
    {
        var paths = db.Set<TEntity>().IgnoreQueryFilters().AsNoTracking()
            .Select(entity => EF.Property<string?>(entity, propertyName))
            .Where(path => path != null).AsAsyncEnumerable();
        await foreach (var path in paths.WithCancellation(ct))
            if (aliases.Contains(path!.Trim().Replace('\\', '/')))
                return true;
        return false;
    }
}

using System.Collections.Concurrent;
using KOAFiloServis.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KOAFiloServis.Web.Data;

/// <summary>
/// [OBSOLETE] Nihai mimari (2026) ile kullanımdan kaldırılmıştır.
/// Tenant-per-DB yaklaşımı terk edilmiş, tüm firmalar tek veritabanında çalışmaktadır.
/// </summary>
[Obsolete("Nihai mimari: TenantDbContextFactory kaldırıldı. PooledDbContextFactory<ApplicationDbContext> kullanın.")]
public sealed class TenantDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly ITenantConnectionStringProvider _connectionStringProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _rootServiceProvider;
    private readonly AktiviteLogInterceptor _interceptor;
    private readonly ConcurrentDictionary<string, DbContextOptions<ApplicationDbContext>> _optionsCache = new();

    public TenantDbContextFactory(
        ITenantConnectionStringProvider connectionStringProvider,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider rootServiceProvider,
        AktiviteLogInterceptor interceptor)
    {
        _connectionStringProvider = connectionStringProvider;
        _httpContextAccessor = httpContextAccessor;
        _rootServiceProvider = rootServiceProvider;
        _interceptor = interceptor;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var connStr = _connectionStringProvider.GetTenantConnectionString()
            ?? throw new InvalidOperationException("Tenant connection string alınamadı.");

        var options = _optionsCache.GetOrAdd(connStr, BuildOptions);
        var ctx = new ApplicationDbContext(options);
        ctx.SetServiceProvider(ResolveScope());
        return ctx;
    }

    public async Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var connStr = _connectionStringProvider.GetTenantConnectionString()
            ?? throw new InvalidOperationException("Tenant connection string alınamadı.");

        var options = _optionsCache.GetOrAdd(connStr, BuildOptions);
        var ctx = new ApplicationDbContext(options);
        ctx.SetServiceProvider(ResolveScope());
        return ctx;
    }

    private DbContextOptions<ApplicationDbContext> BuildOptions(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(30);
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });
        builder.ConfigureWarnings(w =>
        {
            w.Ignore(RelationalEventId.PendingModelChangesWarning);
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);
        });
        builder.AddInterceptors(_interceptor);

        return builder.Options;
    }

    private IServiceProvider ResolveScope()
        => _httpContextAccessor.HttpContext?.RequestServices ?? _rootServiceProvider;
}

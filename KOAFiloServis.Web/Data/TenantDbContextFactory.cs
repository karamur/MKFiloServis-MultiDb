using KOAFiloServis.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KOAFiloServis.Web.Data;

public sealed class TenantDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly ITenantConnectionStringProvider _connectionStringProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _rootServiceProvider;
    private readonly AktiviteLogInterceptor _interceptor;

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
            ?? _connectionStringProvider.GetMasterConnectionString();

        var options = BuildOptions(connStr);
        var ctx = new ApplicationDbContext(options);
        ctx.SetServiceProvider(ResolveScope());
        return ctx;
    }

    public async Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var connStr = _connectionStringProvider.GetTenantConnectionString()
            ?? _connectionStringProvider.GetMasterConnectionString();

        var options = BuildOptions(connStr);
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
        builder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        builder.AddInterceptors(_interceptor);

        return builder.Options;
    }

    private IServiceProvider ResolveScope()
        => _httpContextAccessor.HttpContext?.RequestServices ?? _rootServiceProvider;
}

using KOAFiloServis.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Data;

public sealed class TenantDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly ITenantConnectionStringProvider _connectionStringProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _rootServiceProvider;
    private readonly DbContextOptions<ApplicationDbContext> _optionsTemplate;

    public TenantDbContextFactory(
        ITenantConnectionStringProvider connectionStringProvider,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider rootServiceProvider,
        DbContextOptions<ApplicationDbContext> optionsTemplate)
    {
        _connectionStringProvider = connectionStringProvider;
        _httpContextAccessor = httpContextAccessor;
        _rootServiceProvider = rootServiceProvider;
        _optionsTemplate = optionsTemplate;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var connStr = _connectionStringProvider.GetTenantConnectionString()
            ?? _connectionStringProvider.GetMasterConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connStr)
            .Options;

        var ctx = new ApplicationDbContext(options);
        ctx.SetServiceProvider(ResolveScope());
        return ctx;
    }

    public async Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var connStr = _connectionStringProvider.GetTenantConnectionString()
            ?? _connectionStringProvider.GetMasterConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connStr)
            .Options;

        var ctx = new ApplicationDbContext(options);
        ctx.SetServiceProvider(ResolveScope());
        return ctx;
    }

    private IServiceProvider ResolveScope()
        => _httpContextAccessor.HttpContext?.RequestServices ?? _rootServiceProvider;
}

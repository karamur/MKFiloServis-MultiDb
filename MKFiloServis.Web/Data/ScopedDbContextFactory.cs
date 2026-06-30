using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data;

/// <summary>
/// Her context oluşturulduğunda SetServiceProvider çağrılmasını sağlar.
/// Bu olmadan Global Query Filter (FirmaId izolasyonu) IAktifFirmaProvider'a erişemez
/// ve tüm veriler filtrelenir.
/// PooledDbContextFactory'nin aksine her seferinde yeni context oluşturur.
/// </summary>
public sealed class ScopedDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly IServiceProvider _serviceProvider;

    public ScopedDbContextFactory(
        DbContextOptions<ApplicationDbContext> options,
        IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var ctx = new ApplicationDbContext(_options);
        ctx.SetServiceProvider(_serviceProvider);
        return ctx;
    }
}



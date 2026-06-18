using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// EvrakDosya tabanlı geçerlilik takibi.
/// Dashboard ve uyarı sistemleri için expiry bilgisi sağlar.
/// </summary>
public class EvrakExpiryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public EvrakExpiryService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<EvrakExpiryOzet> GetExpiryOzetAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var now = DateTime.UtcNow;
        var warningDate = now.AddDays(7);

        var expiredCount = await db.EvrakDosyalari
            .Where(x => !x.IsDeleted &&
                   x.GecerlilikTarihi != null &&
                   x.GecerlilikTarihi < now)
            .CountAsync();

        var warningCount = await db.EvrakDosyalari
            .Where(x => !x.IsDeleted &&
                   x.GecerlilikTarihi != null &&
                   x.GecerlilikTarihi >= now &&
                   x.GecerlilikTarihi <= warningDate)
            .CountAsync();

        var expiredList = await db.EvrakDosyalari
            .Where(x => !x.IsDeleted &&
                   x.GecerlilikTarihi != null &&
                   x.GecerlilikTarihi < now)
            .OrderBy(x => x.GecerlilikTarihi)
            .Take(5)
            .ToListAsync();

        var warningList = await db.EvrakDosyalari
            .Where(x => !x.IsDeleted &&
                   x.GecerlilikTarihi != null &&
                   x.GecerlilikTarihi >= now &&
                   x.GecerlilikTarihi <= warningDate)
            .OrderBy(x => x.GecerlilikTarihi)
            .Take(5)
            .ToListAsync();

        return new EvrakExpiryOzet
        {
            ExpiredCount = expiredCount,
            WarningCount = warningCount,
            ExpiredList = expiredList,
            WarningList = warningList
        };
    }
}

public class EvrakExpiryOzet
{
    public int ExpiredCount { get; set; }
    public int WarningCount { get; set; }
    public List<KOAFiloServis.Shared.Entities.EvrakDosya> ExpiredList { get; set; } = new();
    public List<KOAFiloServis.Shared.Entities.EvrakDosya> WarningList { get; set; } = new();
}

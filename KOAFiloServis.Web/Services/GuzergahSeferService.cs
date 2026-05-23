using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class GuzergahSeferService : IGuzergahSeferService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public GuzergahSeferService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<GuzergahSefer>> GetByGuzergahIdAsync(int guzergahId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.GuzergahSeferleri
            .AsNoTracking()
            .Where(s => s.GuzergahId == guzergahId)
            .OrderBy(s => s.Sira)
            .ToListAsync();
    }

    public async Task<Dictionary<int, List<GuzergahSefer>>> GetAllGroupedAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var liste = await context.GuzergahSeferleri
            .AsNoTracking()
            .OrderBy(s => s.GuzergahId).ThenBy(s => s.Sira)
            .ToListAsync();
        return liste.GroupBy(s => s.GuzergahId).ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task ReplaceAllAsync(int guzergahId, List<GuzergahSefer> seferler)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.GuzergahSeferleri
            .Where(s => s.GuzergahId == guzergahId)
            .ToListAsync();
        if (mevcut.Count > 0)
            context.GuzergahSeferleri.RemoveRange(mevcut);

        int sira = 1;
        foreach (var s in seferler)
        {
            s.Id = 0;
            s.GuzergahId = guzergahId;
            s.FirmaId = null;
            s.Firma = null;
            s.Sira = sira++;
            s.Guzergah = null;
            s.Arac = null;
            s.CreatedAt = DateTime.UtcNow;
            context.GuzergahSeferleri.Add(s);
        }

        await context.SaveChangesAsync();
    }
}

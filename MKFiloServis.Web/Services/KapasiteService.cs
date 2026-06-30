using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class KapasiteService : IKapasiteService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICacheService _cache;

    public KapasiteService(IDbContextFactory<ApplicationDbContext> contextFactory, ICacheService cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    public Task<List<Kapasite>> GetAllAsync() =>
        _cache.GetOrSetAsync(CacheKeys.KapasiteListesi, async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Kapasiteler
                .AsNoTracking()
                .OrderBy(k => k.KapasiteAdi)
                .ToListAsync();
        }, CacheDurations.Long);

    public Task<List<Kapasite>> GetActiveAsync() =>
        _cache.GetOrSetAsync(CacheKeys.KapasiteAktif, async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Kapasiteler
                .AsNoTracking()
                .Where(k => k.Aktif)
                .OrderBy(k => k.KapasiteAdi)
                .ToListAsync();
        }, CacheDurations.Long);

    public async Task<Kapasite?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Kapasiteler
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<Kapasite> CreateAsync(Kapasite kapasite)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Kapasiteler.Add(kapasite);
        await context.SaveChangesAsync();
        await _cache.RemoveByPrefixAsync(CacheKeys.KapasitePrefix);
        return kapasite;
    }

    public async Task<Kapasite> UpdateAsync(Kapasite kapasite)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Kapasiteler.FindAsync(kapasite.Id);
        if (existing == null)
            throw new InvalidOperationException($"Kapasite bulunamadı. Id: {kapasite.Id}");

        existing.KapasiteAdi = kapasite.KapasiteAdi;
        existing.Aciklama = kapasite.Aciklama;
        existing.Carpan = kapasite.Carpan;
        existing.Aktif = kapasite.Aktif;
        existing.IsDeleted = kapasite.IsDeleted;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await _cache.RemoveByPrefixAsync(CacheKeys.KapasitePrefix);
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kapasite = await context.Kapasiteler.FindAsync(id);
        if (kapasite != null)
        {
            kapasite.IsDeleted = true;
            kapasite.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(CacheKeys.KapasitePrefix);
        }
    }
}


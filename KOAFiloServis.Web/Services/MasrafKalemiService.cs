using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class MasrafKalemiService : IMasrafKalemiService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICacheService _cache;
    private readonly NumaraSerisiService _numaraSerisi;

    public MasrafKalemiService(IDbContextFactory<ApplicationDbContext> contextFactory, ICacheService cache, NumaraSerisiService numaraSerisi)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _numaraSerisi = numaraSerisi;
    }

    public Task<List<MasrafKalemi>> GetAllAsync() =>
        _cache.GetOrSetAsync(CacheKeys.MasrafKalemiListesi, async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.MasrafKalemleri
                .AsNoTracking()
                .OrderBy(m => m.Kategori)
                .ThenBy(m => m.MasrafAdi)
                .ToListAsync();
        }, CacheDurations.Long);

    public Task<List<MasrafKalemi>> GetActiveAsync() =>
        _cache.GetOrSetAsync(CacheKeys.MasrafKalemiAktif, async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.MasrafKalemleri
                .AsNoTracking()
                .Where(m => m.Aktif)
                .OrderBy(m => m.Kategori)
                .ThenBy(m => m.MasrafAdi)
                .ToListAsync();
        }, CacheDurations.Long);

    public async Task<List<MasrafKalemi>> GetByKategoriAsync(MasrafKategori kategori)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MasrafKalemleri
            .AsNoTracking()
            .Where(m => m.Kategori == kategori && m.Aktif)
            .OrderBy(m => m.MasrafAdi)
            .ToListAsync();
    }

    public async Task<MasrafKalemi?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MasrafKalemleri
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<MasrafKalemi> CreateAsync(MasrafKalemi masrafKalemi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.MasrafKalemleri.Add(masrafKalemi);
        await context.SaveChangesAsync();
        await _cache.RemoveByPrefixAsync(CacheKeys.MasrafKalemiPrefix);
        return masrafKalemi;
    }

    public async Task<MasrafKalemi> UpdateAsync(MasrafKalemi masrafKalemi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.MasrafKalemleri.FindAsync(masrafKalemi.Id);
        if (existing == null)
            throw new InvalidOperationException($"Masraf kalemi bulunamadı. Id: {masrafKalemi.Id}");

        existing.MasrafKodu = masrafKalemi.MasrafKodu;
        existing.MasrafAdi = masrafKalemi.MasrafAdi;
        existing.Kategori = masrafKalemi.Kategori;
        existing.Notlar = masrafKalemi.Notlar;
        existing.Aktif = masrafKalemi.Aktif;
        existing.IsDeleted = masrafKalemi.IsDeleted;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await _cache.RemoveByPrefixAsync(CacheKeys.MasrafKalemiPrefix);
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var masrafKalemi = await context.MasrafKalemleri.FindAsync(id);
        if (masrafKalemi != null)
        {
            masrafKalemi.IsDeleted = true;
            masrafKalemi.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(CacheKeys.MasrafKalemiPrefix);
        }
    }

    public async Task<int> DeleteDuplicatesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kalemler = await context.MasrafKalemleri
            .IgnoreQueryFilters()
            .OrderBy(m => m.Id)
            .ToListAsync();

        var silinecekler = kalemler
            .GroupBy(m => m.MasrafAdi.Trim().ToLower())
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        foreach (var k in silinecekler)
        {
            k.IsDeleted = true;
            k.UpdatedAt = DateTime.UtcNow;
        }

        if (silinecekler.Any())
        {
            await context.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(CacheKeys.MasrafKalemiPrefix);
        }

        return silinecekler.Count;
    }

    public async Task<string> GenerateNextKodAsync()
    {
        var nextNumber = await _numaraSerisi.GenerateNextAsync("MSR", 0, "GLOBAL");
        return $"MSR-{nextNumber:D4}";
    }
}

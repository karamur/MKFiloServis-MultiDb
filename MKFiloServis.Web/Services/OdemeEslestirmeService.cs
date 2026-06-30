using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class OdemeEslestirmeService : IOdemeEslestirmeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IFaturaService _faturaService;

    public OdemeEslestirmeService(IDbContextFactory<ApplicationDbContext> contextFactory, IFaturaService faturaService)
    {
        _contextFactory = contextFactory;
        _faturaService = faturaService;
    }

    public async Task<List<OdemeEslestirme>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OdemeEslestirmeleri
            .Include(e => e.Fatura)
                .ThenInclude(f => f.Cari)
            .Include(e => e.BankaKasaHareket)
                .ThenInclude(h => h.BankaHesap)
            .OrderByDescending(e => e.EslestirmeTarihi)
            .ToListAsync();
    }

    public async Task<List<OdemeEslestirme>> GetByFaturaIdAsync(int faturaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OdemeEslestirmeleri
            .Include(e => e.BankaKasaHareket)
                .ThenInclude(h => h.BankaHesap)
            .Where(e => e.FaturaId == faturaId)
            .OrderByDescending(e => e.EslestirmeTarihi)
            .ToListAsync();
    }

    public async Task<List<OdemeEslestirme>> GetByHareketIdAsync(int hareketId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OdemeEslestirmeleri
            .Include(e => e.Fatura)
                .ThenInclude(f => f.Cari)
            .Where(e => e.BankaKasaHareketId == hareketId)
            .OrderByDescending(e => e.EslestirmeTarihi)
            .ToListAsync();
    }

    public async Task<OdemeEslestirme?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OdemeEslestirmeleri
            .Include(e => e.Fatura)
            .Include(e => e.BankaKasaHareket)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<OdemeEslestirme> CreateAsync(OdemeEslestirme eslestirme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.OdemeEslestirmeleri.Add(eslestirme);
        await context.SaveChangesAsync();

        // Faturan�n �denen tutar�n� g�ncelle
        await _faturaService.UpdateOdenenTutarAsync(eslestirme.FaturaId);

        return eslestirme;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var eslestirme = await context.OdemeEslestirmeleri.FindAsync(id);
        if (eslestirme != null)
        {
            var faturaId = eslestirme.FaturaId;
            eslestirme.IsDeleted = true;
            await context.SaveChangesAsync();

            // Faturan�n �denen tutar�n� g�ncelle
            await _faturaService.UpdateOdenenTutarAsync(faturaId);
        }
    }

    public async Task<decimal> GetFaturaEslestirilenTutarAsync(int faturaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OdemeEslestirmeleri
            .Where(e => e.FaturaId == faturaId)
            .SumAsync(e => e.EslestirilenTutar);
    }

    public async Task<decimal> GetHareketEslestirilenTutarAsync(int hareketId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OdemeEslestirmeleri
            .Where(e => e.BankaKasaHareketId == hareketId)
            .SumAsync(e => e.EslestirilenTutar);
    }
}




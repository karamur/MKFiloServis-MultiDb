using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class OdemeEslestirmeService : IOdemeEslestirmeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly CurrentPermissionGuard _permissionGuard;

    public OdemeEslestirmeService(IDbContextFactory<ApplicationDbContext> contextFactory, CurrentPermissionGuard permissionGuard)
    {
        _contextFactory = contextFactory;
        _permissionGuard = permissionGuard;
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
        await _permissionGuard.RequireAnyAsync(Yetkiler.OdemeEslestirmeYaz);
        if (eslestirme.EslestirilenTutar <= 0) throw new InvalidOperationException("Eşleştirme tutarı pozitif olmalıdır.");
        await using var strategyDb = await _contextFactory.CreateDbContextAsync();
        return await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var fatura = await context.Faturalar.AsTracking().FirstOrDefaultAsync(f => f.Id == eslestirme.FaturaId && !f.IsDeleted)
                ?? throw new InvalidOperationException("Fatura bulunamadı.");
            var hareket = await context.BankaKasaHareketleri.AsTracking().FirstOrDefaultAsync(h => h.Id == eslestirme.BankaKasaHareketId && !h.IsDeleted)
                ?? throw new InvalidOperationException("Banka/kasa hareketi bulunamadı.");
            if (fatura.FirmaId is not > 0 || hareket.FirmaId != fatura.FirmaId)
                throw new InvalidOperationException("Fatura ve ödeme hareketi aynı firmaya bağlı olmalıdır.");
            var faturaBagli = await context.OdemeEslestirmeleri
                .Where(e => e.FaturaId == fatura.Id && !e.IsDeleted).SumAsync(e => (decimal?)e.EslestirilenTutar) ?? 0m;
            var hareketBagli = await context.OdemeEslestirmeleri
                .Where(e => e.BankaKasaHareketId == hareket.Id && !e.IsDeleted).SumAsync(e => (decimal?)e.EslestirilenTutar) ?? 0m;
            if (faturaBagli + eslestirme.EslestirilenTutar > fatura.GenelToplam ||
                hareketBagli + eslestirme.EslestirilenTutar > Math.Abs(hareket.Tutar))
                throw new InvalidOperationException("Eşleştirme tutarı fatura veya hareket kalan tutarını aşıyor.");
            eslestirme.Fatura = fatura;
            eslestirme.BankaKasaHareket = hareket;
            context.OdemeEslestirmeleri.Add(eslestirme);
            fatura.OdenenTutar = faturaBagli + eslestirme.EslestirilenTutar;
            fatura.Durum = fatura.OdenenTutar >= fatura.GenelToplam ? FaturaDurum.Odendi
                : fatura.OdenenTutar > 0 ? FaturaDurum.KismiOdendi : FaturaDurum.Beklemede;
            await context.SaveChangesAsync();
            await tx.CommitAsync();
            return eslestirme;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.OdemeEslestirmeDuzenle);
        await using var strategyDb = await _contextFactory.CreateDbContextAsync();
        await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var eslestirme = await context.OdemeEslestirmeleri.AsTracking().FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
            if (eslestirme == null) return;
            var fatura = await context.Faturalar.AsTracking().FirstOrDefaultAsync(f => f.Id == eslestirme.FaturaId && !f.IsDeleted)
                ?? throw new InvalidOperationException("Eşleştirmenin faturası bulunamadı.");
            eslestirme.IsDeleted = true;
            var kalan = await context.OdemeEslestirmeleri
                .Where(e => e.FaturaId == fatura.Id && !e.IsDeleted && e.Id != eslestirme.Id)
                .SumAsync(e => (decimal?)e.EslestirilenTutar) ?? 0m;
            fatura.OdenenTutar = kalan;
            fatura.Durum = kalan >= fatura.GenelToplam ? FaturaDurum.Odendi
                : kalan > 0 ? FaturaDurum.KismiOdendi : FaturaDurum.Beklemede;
            await context.SaveChangesAsync();
            await tx.CommitAsync();
        });
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




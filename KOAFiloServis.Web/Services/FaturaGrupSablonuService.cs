using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Fatura hazırlık raporu ağaç gruplama şablonu CRUD servisi.
/// Her kullanıcı birden fazla şablon kaydedebilir, birini varsayılan yapabilir.
/// </summary>
public class FaturaGrupSablonuService : IFaturaGrupSablonuService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public FaturaGrupSablonuService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<FaturaGrupSablonu>> GetByFirmaAsync(int firmaId, int? kullaniciId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.FaturaGrupSablonlari
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted);

        if (kullaniciId.HasValue)
            query = query.Where(x => x.KullaniciId == kullaniciId.Value || x.KullaniciId == null);

        return await query.OrderByDescending(x => x.VarsayilanMi).ThenBy(x => x.Ad)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task<FaturaGrupSablonu?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.FaturaGrupSablonlari
            .Where(x => x.Id == id && !x.IsDeleted)
            .AsNoTracking().FirstOrDefaultAsync(ct);
    }

    public async Task<FaturaGrupSablonu?> GetVarsayilanAsync(int firmaId, int? kullaniciId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.FaturaGrupSablonlari
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted && x.VarsayilanMi);

        if (kullaniciId.HasValue)
            query = query.Where(x => x.KullaniciId == kullaniciId.Value);

        return await query.AsNoTracking().FirstOrDefaultAsync(ct);
    }

    public async Task<FaturaGrupSablonu> CreateAsync(FaturaGrupSablonu sablon, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        sablon.CreatedAt = DateTime.UtcNow;

        // Eğer varsayılan ise, mevcut varsayılanları kaldır
        if (sablon.VarsayilanMi)
            await UnsetVarsayilanAsync(db, sablon.FirmaId, sablon.KullaniciId, ct);

        db.FaturaGrupSablonlari.Add(sablon);
        await db.SaveChangesAsync(ct);
        return sablon;
    }

    public async Task<FaturaGrupSablonu> UpdateAsync(FaturaGrupSablonu sablon, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.FaturaGrupSablonlari
            .Where(x => x.Id == sablon.Id && !x.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Şablon bulunamadı: {sablon.Id}");

        existing.Ad = sablon.Ad;
        existing.AgacYapisi = sablon.AgacYapisi;
        existing.UpdatedAt = DateTime.UtcNow;

        // Varsayılan durumu değiştiyse
        if (sablon.VarsayilanMi && !existing.VarsayilanMi)
        {
            await UnsetVarsayilanAsync(db, existing.FirmaId, existing.KullaniciId, ct);
            existing.VarsayilanMi = true;
        }
        else if (!sablon.VarsayilanMi)
        {
            existing.VarsayilanMi = false;
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var sablon = await db.FaturaGrupSablonlari
            .Where(x => x.Id == id && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (sablon == null) return false;

        sablon.IsDeleted = true;
        sablon.DeletedAt = DateTime.UtcNow;
        sablon.VarsayilanMi = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetVarsayilanAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var sablon = await db.FaturaGrupSablonlari
            .Where(x => x.Id == id && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (sablon == null) return false;

        await UnsetVarsayilanAsync(db, sablon.FirmaId, sablon.KullaniciId, ct);
        sablon.VarsayilanMi = true;
        sablon.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Helper ──

    private static async Task UnsetVarsayilanAsync(ApplicationDbContext db, int? firmaId, int? kullaniciId, CancellationToken ct)
    {
        var query = db.FaturaGrupSablonlari
            .Where(x => x.FirmaId == firmaId && x.VarsayilanMi && !x.IsDeleted);

        if (kullaniciId.HasValue)
            query = query.Where(x => x.KullaniciId == kullaniciId.Value);
        else
            query = query.Where(x => x.KullaniciId == null);

        await query.ExecuteUpdateAsync(
            s => s.SetProperty(x => x.VarsayilanMi, false)
                  .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
            ct);
    }
}

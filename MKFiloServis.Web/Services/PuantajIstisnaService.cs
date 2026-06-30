using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Puantaj istisna yönetimi CRUD servisi.
/// Onaylı puantajda değişiklik engellenir.
/// </summary>
public class PuantajIstisnaService : IPuantajIstisnaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public PuantajIstisnaService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<PuantajIstisna>> GetByPuantajKayitAsync(int puantajKayitId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.PuantajIstisnalar
            .Where(x => x.PuantajKayitId == puantajKayitId && !x.IsDeleted)
            .Include(x => x.EskiArac).Include(x => x.YeniArac)
            .OrderBy(x => x.Gun).ThenBy(x => x.Id)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task<PuantajIstisna?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.PuantajIstisnalar
            .Where(x => x.Id == id && !x.IsDeleted)
            .AsNoTracking().FirstOrDefaultAsync(ct);
    }

    public async Task<PuantajIstisna> CreateAsync(PuantajIstisna istisna, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Onaylı puantaja istisna eklenemez
        var pk = await db.PuantajKayitlar
            .Where(x => x.Id == istisna.PuantajKayitId && !x.IsDeleted)
            .Select(x => new { x.OnayDurum })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Puantaj kaydı bulunamadı.");

        if (pk.OnayDurum == PuantajOnayDurum.Onaylandi)
            throw new InvalidOperationException("Onaylanmış puantaja istisna eklenemez.");

        istisna.CreatedAt = DateTime.UtcNow;
        db.PuantajIstisnalar.Add(istisna);
        await db.SaveChangesAsync(ct);
        return istisna;
    }

    public async Task<PuantajIstisna> UpdateAsync(PuantajIstisna istisna, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.PuantajIstisnalar
            .Where(x => x.Id == istisna.Id && !x.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("İstisna bulunamadı.");

        // Onay kontrolü
        var pk = await db.PuantajKayitlar
            .Where(x => x.Id == existing.PuantajKayitId && !x.IsDeleted)
            .Select(x => new { x.OnayDurum })
            .FirstOrDefaultAsync(ct);

        if (pk?.OnayDurum == PuantajOnayDurum.Onaylandi)
            throw new InvalidOperationException("Onaylanmış puantajda istisna güncellenemez.");

        existing.IstisnaTipi = istisna.IstisnaTipi;
        existing.KararTipi = istisna.KararTipi;
        existing.Tutar = istisna.Tutar;
        existing.Gun = istisna.Gun;
        existing.Aciklama = istisna.Aciklama;
        existing.EskiAracId = istisna.EskiAracId;
        existing.YeniAracId = istisna.YeniAracId;
        existing.FisNo = istisna.FisNo;
        existing.OperasyonKaydiId = istisna.OperasyonKaydiId;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var istisna = await db.PuantajIstisnalar
            .Where(x => x.Id == id && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (istisna == null) return false;

        // Onay kontrolü
        var pk = await db.PuantajKayitlar
            .Where(x => x.Id == istisna.PuantajKayitId && !x.IsDeleted)
            .Select(x => new { x.OnayDurum })
            .FirstOrDefaultAsync(ct);

        if (pk?.OnayDurum == PuantajOnayDurum.Onaylandi)
            throw new InvalidOperationException("Onaylanmış puantajdan istisna silinemez.");

        istisna.IsDeleted = true;
        istisna.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<PuantajIstisna>> GetByDonemAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.PuantajIstisnalar
            .Where(x => !x.IsDeleted)
            .Join(db.PuantajKayitlar.Where(pk => pk.Yil == yil && pk.Ay == ay && !pk.IsDeleted),
                i => i.PuantajKayitId, pk => pk.Id, (i, pk) => new { Istisna = i, PuantajKayit = pk });

        if (kurumId.HasValue)
            query = query.Where(x => x.PuantajKayit.KurumId == kurumId.Value);

        return await query
            .Select(x => x.Istisna)
            .Include(x => x.EskiArac).Include(x => x.YeniArac)
            .OrderBy(x => x.PuantajKayitId).ThenBy(x => x.Gun)
            .AsNoTracking().ToListAsync(ct);
    }
}



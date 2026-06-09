using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class GuzergahService : IGuzergahService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICacheService _cache;
    private readonly NumaraSerisiService _numaraSerisi;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public GuzergahService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICacheService cache,
        NumaraSerisiService numaraSerisi,
        IAktifFirmaProvider aktifFirmaProvider)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _numaraSerisi = numaraSerisi;
        _aktifFirmaProvider = aktifFirmaProvider;
    }

    public Task<List<Guzergah>> GetAllAsync() =>
        _cache.GetOrSetAsync(CacheKeys.GuzergahListesi, async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Guzergahlar
                .AsNoTracking()
                .Include(g => g.Cari)
                .Include(g => g.Firma)
                .Include(g => g.Kurum)
                .Include(g => g.VarsayilanArac)
                .Include(g => g.VarsayilanSofor)
                .Where(g => g.Cari == null || !g.Cari.IsDeleted)
                .OrderBy(g => g.GuzergahAdi)
                .ToListAsync();
        }, CacheDurations.Long);

    public Task<List<Guzergah>> GetActiveAsync() =>
        _cache.GetOrSetAsync(CacheKeys.GuzergahAktif, async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Guzergahlar
                .AsNoTracking()
                .Include(g => g.Cari)
                .Include(g => g.Firma)
                .Where(g => g.Aktif)
                .Where(g => g.Cari == null || !g.Cari.IsDeleted)
                .OrderBy(g => g.GuzergahAdi)
                .ToListAsync();
        }, CacheDurations.Long);

    public async Task<List<Guzergah>> GetByCariIdAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .AsNoTracking()
            .Where(g => g.CariId == cariId)
            .OrderBy(g => g.GuzergahAdi)
            .ToListAsync();
    }

    public async Task<List<Guzergah>> GetByFirmaIdAsync(int firmaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .AsNoTracking()
            .Include(g => g.VarsayilanArac)
            .Include(g => g.VarsayilanSofor)
            .Where(g => g.FirmaId == firmaId && g.Aktif)
            .OrderBy(g => g.GuzergahAdi)
            .ToListAsync();
    }

    public async Task<Guzergah?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .AsNoTracking()
            .Include(g => g.Cari)
            .Include(g => g.Firma)
            .Include(g => g.Kurum)
            .Include(g => g.VarsayilanArac)
            .Include(g => g.VarsayilanSofor)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Guzergah> CreateAsync(Guzergah guzergah)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Tenant/firma görünürlüğü için create anında FirmaId'yi netleştir.
        if (!guzergah.FirmaId.HasValue || guzergah.FirmaId.Value <= 0)
        {
            var aktifFirmaId = _aktifFirmaProvider.AktifFirmaId ?? _aktifFirmaProvider.Mevcut.FirmaId;
            if (aktifFirmaId > 0)
            {
                guzergah.FirmaId = aktifFirmaId;
            }
        }

        context.Guzergahlar.Add(guzergah);
        await context.SaveChangesAsync();

        // Yazım doğrulaması: kayıt gerçekten DB'ye geçti mi?
        var persisted = await context.Guzergahlar
            .AsNoTracking()
            .AnyAsync(g => g.Id == guzergah.Id);

        if (!persisted)
            throw new InvalidOperationException("Güzergah kaydı doğrulanamadı. Kayıt veritabanına yansımadı.");

        await _cache.RemoveByPrefixAsync(CacheKeys.GuzergahPrefix);
        return guzergah;
    }

    public async Task<Guzergah> AddAsync(Guzergah guzergah)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await CreateAsync(guzergah);
    }

    public async Task<Guzergah> UpdateAsync(Guzergah guzergah)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Guzergahlar.FindAsync(guzergah.Id);
        if (existing == null)
            throw new InvalidOperationException($"Güzergah bulunamadı. Id: {guzergah.Id}");

        existing.GuzergahKodu = guzergah.GuzergahKodu;
        existing.GuzergahAdi = guzergah.GuzergahAdi;
        existing.BaslangicNoktasi = guzergah.BaslangicNoktasi;
        existing.BitisNoktasi = guzergah.BitisNoktasi;
        existing.BaslangicLatitude = guzergah.BaslangicLatitude;
        existing.BaslangicLongitude = guzergah.BaslangicLongitude;
        existing.BitisLatitude = guzergah.BitisLatitude;
        existing.BitisLongitude = guzergah.BitisLongitude;
        existing.RotaRengi = guzergah.RotaRengi;
        existing.Mesafe = guzergah.Mesafe;
        existing.TahminiSure = guzergah.TahminiSure;
        existing.GelirFiyat = guzergah.GelirFiyat;
        existing.GiderFiyat = guzergah.GiderFiyat;
        existing.SeferTipi = guzergah.SeferTipi;
        existing.PersonelSayisi = guzergah.PersonelSayisi;
        existing.KapasiteAdi = guzergah.KapasiteAdi;
        existing.CariId = guzergah.CariId;
        existing.KurumId = guzergah.KurumId;
        existing.FirmaId = guzergah.FirmaId > 0 ? guzergah.FirmaId : null;
        existing.VarsayilanAracId = guzergah.VarsayilanAracId;
        existing.VarsayilanSoforId = guzergah.VarsayilanSoforId;
        existing.Notlar = guzergah.Notlar;
        existing.PuantajCarpani = guzergah.PuantajCarpani;
        existing.Aktif = guzergah.Aktif;
        existing.IsDeleted = guzergah.IsDeleted;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await _cache.RemoveByPrefixAsync(CacheKeys.GuzergahPrefix);
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var guzergah = await context.Guzergahlar.FindAsync(id);
        if (guzergah != null)
        {
            guzergah.IsDeleted = true;
            guzergah.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(CacheKeys.GuzergahPrefix);
        }
    }

    public async Task<string> GenerateNextKodAsync()
    {
        // Kural 15: Atomik numara üretimi (global)
        var nextNumber = await _numaraSerisi.GenerateNextAsync("GZR", 0, "GLOBAL");
        return $"GZR-{nextNumber:D4}";
    }

    public async Task<string> GenerateGuzergahKoduAsync(int firmaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var firma = await context.Firmalar
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == firmaId);
        var firmaKisaltma = firma?.FirmaAdi?.Length >= 3
            ? firma.FirmaAdi.Substring(0, 3).ToUpperInvariant()
            : "GZR";

        // Kural 15: FirmaId bazlı atomik numara
        var sayi = await _numaraSerisi.GenerateNextAsync(firmaKisaltma, firmaId, "GLOBAL");
        return $"{firmaKisaltma}-{sayi:D3}";
    }

    #region Doğrulama Metodları

    public async Task<bool> FaturaKalemdenGuzergahVarMiAsync(int faturaKalemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .AsNoTracking()
            .AnyAsync(g => g.FaturaKalemId == faturaKalemId && !g.IsDeleted);
    }

    public async Task<Guzergah?> GetByFaturaKalemIdAsync(int faturaKalemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .AsNoTracking()
            .Include(g => g.Firma)
            .FirstOrDefaultAsync(g => g.FaturaKalemId == faturaKalemId && !g.IsDeleted);
    }

    public async Task<bool> BenzersizGuzergahMiAsync(int firmaId, string guzergahAdi, int? haricId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var normalized = guzergahAdi.Trim();

        var query = context.Guzergahlar
            .AsNoTracking()
            .Where(g => g.FirmaId == firmaId && !g.IsDeleted
                        && g.GuzergahAdi.Trim().ToLower() == normalized.ToLowerInvariant());

        if (haricId.HasValue)
            query = query.Where(g => g.Id != haricId.Value);

        return !await query.AnyAsync();
    }

    #endregion
}

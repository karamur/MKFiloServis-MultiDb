using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class GuzergahSeferService : IGuzergahSeferService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;
    private readonly ILogger<GuzergahSeferService> _logger;

    public GuzergahSeferService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IAktifFirmaProvider aktifFirmaProvider,
        ILogger<GuzergahSeferService> logger)
    {
        _contextFactory = contextFactory;
        _aktifFirmaProvider = aktifFirmaProvider;
        _logger = logger;
    }

    public async Task<List<GuzergahSefer>> GetByGuzergahIdAsync(int guzergahId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Parent Guzergah doğrulaması
        await VerifyGuzergahAccessAsync(context, guzergahId);

        return await context.GuzergahSeferleri
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.GuzergahId == guzergahId && !s.IsDeleted)
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

    // ── Ortak aktif/tüm sefer query helper'ları ──
    private static IQueryable<GuzergahSefer> AllSeferQuery(ApplicationDbContext db, int guzergahId)
        => db.GuzergahSeferleri.IgnoreQueryFilters().Where(s => s.GuzergahId == guzergahId);

    private static IQueryable<GuzergahSefer> ActiveSeferQuery(ApplicationDbContext db, int guzergahId)
        => AllSeferQuery(db, guzergahId).Where(s => s.IsDeleted != true);

    /// <summary>
    /// Verilen DbContext ile seferleri replace eder. Transaction AÇMAZ.
    /// Üst seviye transaction içinden çağrılmak üzere tasarlanmıştır.
    /// </summary>
    internal async Task ReplaceAllInCurrentDbAsync(
        ApplicationDbContext context, int guzergahId, List<GuzergahSefer> seferler, DateTime now)
    {
        seferler ??= [];
        var target = seferler.Count;

        // Parent Guzergah doğrulaması
        var guzergah = await VerifyGuzergahAccessAsync(context, guzergahId);
        var parentFirmaId = guzergah.FirmaId;

        // ── AŞAMA 1: BEFORE ──
        var beforeTotal = await AllSeferQuery(context, guzergahId).CountAsync();
        var beforeActive = await ActiveSeferQuery(context, guzergahId).CountAsync();
        var beforeDeleted = beforeTotal - beforeActive;

        _logger.LogWarning(
            "GUZERGAH_REPLACE Before GuzergahId={GuzergahId} Target={Target} Total={Total} Active={Active} Deleted={Deleted}",
            guzergahId, target, beforeTotal, beforeActive, beforeDeleted);

        // ── AŞAMA 2: SOFT-DELETE ──
        var affected = await AllSeferQuery(context, guzergahId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.UpdatedAt, now));

        _logger.LogWarning(
            "GUZERGAH_REPLACE SoftDeleteExecute GuzergahId={GuzergahId} Affected={Affected}",
            guzergahId, affected);

        var afterSoftDeleteActive = await ActiveSeferQuery(context, guzergahId).CountAsync();
        _logger.LogWarning(
            "GUZERGAH_REPLACE AfterSoftDelete GuzergahId={GuzergahId} Target={Target} Active={Active}",
            guzergahId, target, afterSoftDeleteActive);

        if (afterSoftDeleteActive != 0)
        {
            var kalanlar = await ActiveSeferQuery(context, guzergahId)
                .Select(x => new { x.Id, x.Sira, x.IsDeleted, x.FirmaId }).ToListAsync();
            throw new InvalidOperationException(
                $"Soft-delete BAŞARISIZ! GuzergahId={guzergahId}, KalanAktif={afterSoftDeleteActive}, " +
                $"Kalanlar={string.Join(" | ", kalanlar.Select(x => $"Id={x.Id},Sira={x.Sira},IsDeleted={x.IsDeleted}"))}");
        }

        // ── AŞAMA 3: INSERT ──
        int sira = 1;
        var yeniEntities = seferler.Select(s => new GuzergahSefer
        {
            GuzergahId = guzergahId, FirmaId = parentFirmaId, Sira = sira++,
            Slot = s.Slot, SeferTipi = s.SeferTipi, KapasiteAdi = s.KapasiteAdi,
            AracId = s.AracId, SoforAd = s.SoforAd, SoforTelefon = s.SoforTelefon,
            FirmaAdiSerbest = s.FirmaAdiSerbest,
            IsDeleted = false, CreatedAt = now, UpdatedAt = now
        }).ToList();

        if (yeniEntities.Count != target)
            throw new InvalidOperationException($"Yeni entity count HEDEFLE UYUŞMUYOR! Target={target}, EntityCount={yeniEntities.Count}");

        _logger.LogWarning(
            "GUZERGAH_REPLACE InsertPrepare GuzergahId={GuzergahId} Target={Target} EntityCount={EntityCount}",
            guzergahId, target, yeniEntities.Count);

        context.GuzergahSeferleri.AddRange(yeniEntities);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // ── AŞAMA 4: AFTER INSERT GUARD ──
        var afterInsertActive = await ActiveSeferQuery(context, guzergahId).CountAsync();
        _logger.LogWarning(
            "GUZERGAH_REPLACE AfterInsert GuzergahId={GuzergahId} Target={Target} Active={Active}",
            guzergahId, target, afterInsertActive);

        if (afterInsertActive != target)
        {
            var aktifler = await ActiveSeferQuery(context, guzergahId)
                .Select(x => new { x.Id, x.Sira, x.IsDeleted, x.FirmaId }).ToListAsync();
            throw new InvalidOperationException(
                $"SEFER SAYISI UYUŞMAZLIĞI! GuzergahId={guzergahId}, Hedef={target}, DB_Aktif={afterInsertActive}. " +
                $"Aktifler={string.Join(" | ", aktifler.Select(x => $"Id={x.Id},Sira={x.Sira}"))}");
        }
    }

    /// <summary>
    /// Bağımsız çağrılar için: kendi ExecutionStrategy + transaction açar.
    /// </summary>
    public async Task ReplaceAllAsync(int guzergahId, List<GuzergahSefer> seferler)
    {
        seferler ??= [];

        await using var tempContext = await _contextFactory.CreateDbContextAsync();
        var strategy = tempContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();

            try
            {
                await ReplaceAllInCurrentDbAsync(context, guzergahId, seferler, DateTime.UtcNow);
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    private async Task<Guzergah> VerifyGuzergahAccessAsync(ApplicationDbContext context, int guzergahId)
    {
        var guzergah = await context.Guzergahlar
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guzergahId && !g.IsDeleted);

        if (guzergah == null)
            throw new InvalidOperationException($"Güzergah bulunamadı. Id={guzergahId}");

        var aktifFirmaId = _aktifFirmaProvider.AktifFirmaId ?? _aktifFirmaProvider.Mevcut.FirmaId;

        if (aktifFirmaId > 0 && guzergah.FirmaId.HasValue && guzergah.FirmaId != aktifFirmaId)
        {
            _logger.LogWarning(
                "Firma dışı güzergah sefer erişimi engellendi. GuzergahId={GuzergahId}, GuzergahFirmaId={GuzergahFirmaId}, AktifFirmaId={AktifFirmaId}",
                guzergahId, guzergah.FirmaId, aktifFirmaId);
            throw new UnauthorizedAccessException(
                $"Güzergah aktif firmaya ait değil. GuzergahId={guzergahId}");
        }

        return guzergah;
    }
}

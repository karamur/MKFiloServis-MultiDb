using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Services;

public interface IPuantajReconciliationService
{
    Task<ReconciliationReport> RunAsync(CancellationToken ct = default);
}

public sealed class ReconciliationReport
{
    public int TenantsScanned { get; set; }
    public int StaleMutexCleaned { get; set; }
    public int MissingAuditLogsFound { get; set; }
    public int MissingAuditLogsFixed { get; set; }
    public int OrphanAuditLogsFound { get; set; }
    public int InconsistentMutexFixed { get; set; }
    public int OrphanFinansalKayitFound { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<string> Issues { get; } = new();

    public bool HasWork => StaleMutexCleaned + MissingAuditLogsFixed
        + InconsistentMutexFixed + OrphanAuditLogsFound > 0;
}

public sealed class PuantajReconciliationService : IPuantajReconciliationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbContextFactory<MasterDbContext> _masterDbFactory;
    private readonly ILogger<PuantajReconciliationService> _logger;

    public PuantajReconciliationService(
        IServiceScopeFactory scopeFactory,
        IDbContextFactory<MasterDbContext> masterDbFactory,
        ILogger<PuantajReconciliationService> logger)
    {
        _scopeFactory = scopeFactory;
        _masterDbFactory = masterDbFactory;
        _logger = logger;
    }

    public async Task<ReconciliationReport> RunAsync(CancellationToken ct = default)
    {
        var report = new ReconciliationReport { StartedAt = DateTime.UtcNow };
        _logger.LogInformation("Reconciliation başladı");

        var firmalar = await GetFirmalarAsync(ct);
        report.TenantsScanned = firmalar.Count;

        foreach (var firma in firmalar)
        {
            ct.ThrowIfCancellationRequested();
            await using var scope = CreateTenantScope(firma);

            try
            {
                var tenantReport = await ReconcileTenantAsync(scope, ct);
                report.StaleMutexCleaned += tenantReport.StaleMutexCleaned;
                report.MissingAuditLogsFound += tenantReport.MissingAuditLogsFound;
                report.MissingAuditLogsFixed += tenantReport.MissingAuditLogsFixed;
                report.OrphanAuditLogsFound += tenantReport.OrphanAuditLogsFound;
                report.InconsistentMutexFixed += tenantReport.InconsistentMutexFixed;
                report.OrphanFinansalKayitFound += tenantReport.OrphanFinansalKayitFound;

                if (tenantReport.HasWork)
                    _logger.LogInformation(
                        "Firma {FirmaId} ({FirmaAdi}): stale={Stale} audit={Audit} orphan={Orphan} inconsistent={Inconsistent}",
                        firma.Id, firma.FirmaAdi,
                        tenantReport.StaleMutexCleaned,
                        tenantReport.MissingAuditLogsFixed,
                        tenantReport.OrphanAuditLogsFound,
                        tenantReport.InconsistentMutexFixed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconciliation failed for Firma {FirmaId}", firma.Id);
                report.Issues.Add($"Firma {firma.Id}: {ex.Message}");
            }
        }

        report.CompletedAt = DateTime.UtcNow;
        _logger.LogInformation(
            "Reconciliation tamamlandı: {Tenants} tenant, {Stale} stale, {AuditFixed} audit, {Orphan} orphan, {Inconsistent} inconsistent — {Duration:F1}s",
            report.TenantsScanned, report.StaleMutexCleaned,
            report.MissingAuditLogsFixed, report.OrphanAuditLogsFound,
            report.InconsistentMutexFixed,
            (report.CompletedAt - report.StartedAt).TotalSeconds);

        return report;
    }

    private static async Task<ReconciliationReport> ReconcileTenantAsync(
        AsyncServiceScope scope, CancellationToken ct)
    {
        var dbFactory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var report = new ReconciliationReport();

        // ── 1. Stale mutex cleanup (Running > 30 min) ──────────────
        var threshold = DateTime.UtcNow.AddMinutes(-30);
        var staleMutex = await db.PuantajJobExecutions
            .Where(j => j.Durum == PuantajJobExecutionDurum.Running
                        && j.Baslangic < threshold
                        && !j.IsDeleted)
            .ToListAsync(ct);

        foreach (var m in staleMutex)
        {
            // Check: Does this tenant/month have an Aktif period?
            var hasActive = await db.PuantajHesapDonemleri
                .Where(h => h.FirmaId == m.FirmaId
                            && h.Yil == m.Yil && h.Ay == m.Ay
                            && h.Durum == PuantajHesapDurum.Aktif
                            && !h.IsDeleted)
                .AnyAsync(ct);

            if (hasActive)
            {
                // Engine succeeded, mutex wasn't updated → Complete it
                m.Durum = PuantajJobExecutionDurum.Completed;
                m.HataMesaji = "Reconciliation: engine OK, mutex completed";
                report.InconsistentMutexFixed++;
            }
            else
            {
                // Engine never ran or rolled back → Failed
                m.Durum = PuantajJobExecutionDurum.Failed;
                m.HataMesaji = "Reconciliation: stale Running — engine did not complete";
                report.StaleMutexCleaned++;
            }
            m.Bitis = DateTime.UtcNow;
        }

        // ── 2. Missing audit logs ──────────────────────────────────
        var activeDonemler = await db.PuantajHesapDonemleri
            .Where(h => h.Durum == PuantajHesapDurum.Aktif && !h.IsDeleted)
            .ToListAsync(ct);

        foreach (var donem in activeDonemler)
        {
            var hasAudit = await db.PuantajAuditLogs
                .Where(l => l.HesapDonemiId == donem.Id
                            && l.Aksiyon == PuantajAuditAksiyon.Hesaplandi
                            && !l.IsDeleted)
                .AnyAsync(ct);

            if (!hasAudit)
            {
                report.MissingAuditLogsFound++;
                db.PuantajAuditLogs.Add(new PuantajAuditLog
                {
                    FirmaId = donem.FirmaId,
                    HesapDonemiId = donem.Id,
                    Aksiyon = PuantajAuditAksiyon.Hesaplandi,
                    Kullanici = "Reconciliation",
                    AksiyonTarihi = donem.HesaplamaTarihi,
                    OncekiDurum = "Yok",
                    YeniDurum = $"Aktif V{donem.Versiyon}",
                    Aciklama = "Reconciliation: eksik audit log tamamlandı",
                    CreatedAt = DateTime.UtcNow
                });
                report.MissingAuditLogsFixed++;
            }
        }

        // ── 3. Orphan audit logs (HesapDonemi deleted/iptal) ──────
        var allAuditLogs = await db.PuantajAuditLogs
            .Where(l => l.Aksiyon == PuantajAuditAksiyon.Hesaplandi && !l.IsDeleted)
            .ToListAsync(ct);

        foreach (var log in allAuditLogs)
        {
            if (log.HesapDonemiId == null) continue;

            var donemExists = await db.PuantajHesapDonemleri
                .Where(h => h.Id == log.HesapDonemiId)
                .AnyAsync(ct);

            if (!donemExists)
            {
                log.Aciklama = (log.Aciklama ?? "") +
                    " | Reconciliation: HesapDonemi bulunamadı (silinmiş olabilir)";
                report.OrphanAuditLogsFound++;
            }
        }

        // ── 4. Orphan finansal kayıtlar ───────────────────────────
        var finansalDonemIds = await db.PuantajFinansalKayitlar
            .Where(f => !f.IsDeleted)
            .Select(f => f.HesapDonemiId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var donemId in finansalDonemIds)
        {
            var donemExists = await db.PuantajHesapDonemleri
                .AnyAsync(h => h.Id == donemId, ct);

            if (!donemExists)
                report.OrphanFinansalKayitFound++;
        }

        if (report.HasWork)
            await db.SaveChangesAsync(ct);

        return report;
    }

    private async Task<List<Firma>> GetFirmalarAsync(CancellationToken ct)
    {
        await using var db = await _masterDbFactory.CreateDbContextAsync(ct);
        return await db.Firmalar
            .Where(f => !f.IsDeleted && f.Aktif)
            .OrderBy(f => f.Id)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    private AsyncServiceScope CreateTenantScope(Firma firma)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IAktifFirmaProvider>();
        provider.Set(new AktifFirmaBilgisi
        {
            FirmaId = firma.Id,
            FirmaKodu = firma.FirmaKodu,
            FirmaAdi = firma.FirmaAdi,
            DatabaseName = firma.DatabaseName,
            AktifDonemYil = firma.AktifDonemYil,
            AktifDonemAy = firma.AktifDonemAy
        });
        return scope;
    }
}

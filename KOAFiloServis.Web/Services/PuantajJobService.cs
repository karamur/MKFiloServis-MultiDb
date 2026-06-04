using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Shared.Exceptions;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Services;

public sealed class PuantajJobService : IPuantajJobService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbContextFactory<ApplicationDbContext> _masterDbFactory;
    private readonly IPuantajRetryPolicy _retryPolicy;
    private readonly ILogger<PuantajJobService> _logger;

    public PuantajJobService(
        IServiceScopeFactory scopeFactory,
        IDbContextFactory<ApplicationDbContext> masterDbFactory,
        IPuantajRetryPolicy retryPolicy,
        ILogger<PuantajJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _masterDbFactory = masterDbFactory;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task<PuantajJobExecution> ProcessAllTenantsAsync(
        int yil, int ay, string tetikleyen, CancellationToken ct = default)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["JobRunId"] = Guid.NewGuid(),
            ["Yil"] = yil, ["Ay"] = ay, ["Tetikleyen"] = tetikleyen
        });

        var startedAt = DateTime.UtcNow;
        _logger.LogInformation("PuantajJob başladı: {Yil}/{Ay}", yil, ay);

        List<Firma> firmalar;
        try
        {
            firmalar = await GetAktifFirmalarAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Job iptal edildi — firma listesi alınırken");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Firma listesi alınamadı");
            return new PuantajJobExecution
            {
                Yil = yil, Ay = ay, Tetikleyen = tetikleyen,
                Durum = PuantajJobExecutionDurum.Failed,
                Baslangic = startedAt, Bitis = DateTime.UtcNow,
                HataMesaji = $"Firma listesi alınamadı: {ex.Message}"
            };
        }

        _logger.LogInformation("İşlenecek firma: {Count}", firmalar.Count);

        var metrics = new PuantajJobMetrics { StartedAt = startedAt };
        var previousResult = (PuantajJobExecution?)null;

        foreach (var firma in firmalar)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                ConfigureTenantScope(scope, firma);

                var result = await ProcessSingleTenantAsync(
                    scope, firma.Id, firma.FirmaAdi, yil, ay, tetikleyen, ct);

                previousResult = result.Record;
                metrics.Add(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Job iptal edildi — {FirmaId} işlenemedi", firma.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firma {FirmaId} ({FirmaAdi}) beklenmeyen hata",
                    firma.Id, firma.FirmaAdi);
                metrics.AddFailed(firma.Id, ex.Message);
            }
        }

        var elapsed = DateTime.UtcNow - startedAt;
        _logger.LogInformation(
            "PuantajJob tamamlandı: {Yil}/{Ay} — Başarılı={Ok} Atlanan={Skip} Hatalı={Fail} Süre={Elapsed:F1}sn",
            yil, ay, metrics.Successful, metrics.Skipped, metrics.Failed, elapsed.TotalSeconds);

        return new PuantajJobExecution
        {
            Yil = yil, Ay = ay, Tetikleyen = tetikleyen,
            Durum = metrics.Failed > 0
                ? (metrics.Successful > 0
                    ? PuantajJobExecutionDurum.PartialSuccess
                    : PuantajJobExecutionDurum.Failed)
                : PuantajJobExecutionDurum.Completed,
            Baslangic = startedAt, Bitis = DateTime.UtcNow,
            IslenenOperasyon = metrics.Successful, UretilenPuantaj = metrics.Skipped
        };
    }

    public async Task ProcessTenantAsync(
        int firmaId, string? databaseName, int yil, int ay,
        string tetikleyen, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var firmaInfo = new AktifFirmaBilgisi
        {
            FirmaId = firmaId,
            FirmaKodu = $"F{firmaId}",
            FirmaAdi = $"Firma {firmaId}",
            DatabaseName = databaseName,
            AktifDonemYil = yil,
            AktifDonemAy = ay
        };
        ConfigureTenantScope(scope, firmaInfo);

        await ProcessSingleTenantAsync(scope, firmaId, firmaInfo.FirmaAdi, yil, ay, tetikleyen, ct);
    }

    // ── Core: single tenant processing ─────────────────────────────────

    private async Task<TenantProcessResult> ProcessSingleTenantAsync(
        AsyncServiceScope scope, int firmaId, string firmaAdi,
        int yil, int ay, string tetikleyen, CancellationToken ct)
    {
        using var tenantScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["FirmaId"] = firmaId, ["FirmaAdi"] = firmaAdi,
            ["Yil"] = yil, ["Ay"] = ay
        });

        var mutex = scope.ServiceProvider.GetRequiredService<IPuantajMutexService>();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        // 1. Stale cleanup
        await mutex.CleanupStaleAsync(ct);

        // 2. Mutex acquire
        var acquire = await mutex.TryAcquireAsync(firmaId, yil, ay, tetikleyen, ct);
        if (!acquire.Acquired)
        {
            return TenantProcessResult.Skipped(acquire.FailureReason ?? "Mutex alınamadı");
        }

        var record = acquire.Record!;

        try
        {
            // 3. Idempotency check
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var existing = await db.PuantajHesapDonemleri
                .Where(h => !h.IsDeleted && h.Yil == yil && h.Ay == ay
                            && h.Durum == PuantajHesapDurum.Aktif)
                .OrderByDescending(h => h.Versiyon)
                .FirstOrDefaultAsync(ct);

            if (existing != null)
            {
                await mutex.UpdateToSkippedAsync(record,
                    $"Zaten hesaplanmış (V{existing.Versiyon})",
                    existing.Id, ct);
                _logger.LogInformation("Zaten hesaplanmış V{Version}, atlanıyor", existing.Versiyon);
                return TenantProcessResult.Skipped($"V{existing.Versiyon}", record);
            }

            // 4. Engine — Polly retry pipeline
            var engine = scope.ServiceProvider.GetRequiredService<IPuantajEngineService>();

            PuantajEngineSonucV1 engineResult;
            try
            {
                engineResult = await _retryPolicy.ExecuteAsync(
                    async innerCt => await engine.ProcessDonemAsync(
                        yil, ay, kurumId: null, hesaplayan: tetikleyen,
                        notlar: $"Auto ({tetikleyen})", ct: innerCt),
                    $"Engine:F{firmaId}/{yil}/{ay:00}", ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Engine başarısız (retry tükendi)");
                await mutex.UpdateToFailedAsync(record, $"{ex.GetType().Name}: {ex.Message}", ct);
                return TenantProcessResult.Failed(firmaId, ex.Message, record);
            }

            // 5. Audit log — best-effort (engine already committed)
            try
            {
                await using var auditDb = await dbFactory.CreateDbContextAsync(ct);
                auditDb.PuantajAuditLogs.Add(new PuantajAuditLog
                {
                    FirmaId = firmaId,
                    HesapDonemiId = engineResult.HesapDonemiId,
                    Aksiyon = PuantajAuditAksiyon.Hesaplandi,
                    Kullanici = tetikleyen,
                    AksiyonTarihi = DateTime.UtcNow,
                    OncekiDurum = "Yok",
                    YeniDurum = $"Aktif V{engineResult.Versiyon}",
                    Aciklama = $"Job: {engineResult.IslenenOperasyonSayisi} op → {engineResult.UretilenPuantajKayit} kayıt",
                    CreatedAt = DateTime.UtcNow
                });
                await auditDb.SaveChangesAsync(ct);
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx,
                    "Audit log yazılamadı (engine OK) — Reconciliation düzeltecek");
            }

            // 6. Mutex → Completed (ALWAYS, even if audit failed)
            await mutex.UpdateToCompletedAsync(record, engineResult, ct);

            _logger.LogInformation(
                "Hesaplandı V{Version}: {Ops} op → {Pk} kayıt",
                engineResult.Versiyon, engineResult.IslenenOperasyonSayisi,
                engineResult.UretilenPuantajKayit);

            return TenantProcessResult.Completed(record, engineResult);
        }
        catch (OperationCanceledException)
        {
            // ── Cancellation — propagate ──
            _logger.LogWarning("İptal edildi — Firma {FirmaId}", firmaId);
            await mutex.UpdateToFailedAsync(record, "İptal edildi", ct);
            throw;
        }
        catch (PuantajBusinessException ex)
        {
            // ── Business rule — NO retry, Skipped ──
            _logger.LogWarning(ex, "İş kuralı: {ExceptionType} — Skipped", ex.GetType().Name);
            await mutex.UpdateToSkippedAsync(record, ex.Message, ct: ct);
            return TenantProcessResult.Skipped(ex.Message, record);
        }
        catch (PuantajInfrastructureException ex)
        {
            // ── Infrastructure — retry exhausted, Failed ──
            _logger.LogError(ex, "Altyapı hatası (retry tükendi): {ExceptionType} — Failed",
                ex.GetType().Name);
            await mutex.UpdateToFailedAsync(record, ex.Message, ct);
            return TenantProcessResult.Failed(firmaId, ex.Message, record);
        }
        catch (PuantajFatalException ex)
        {
            // ── Fatal — STOP entire job ──
            _logger.LogCritical(ex, "FATAL: {ExceptionType} — tüm job durduruluyor",
                ex.GetType().Name);
            await mutex.UpdateToFailedAsync(record, $"FATAL: {ex.Message}", ct);
            throw;
        }
        catch (Exception ex)
        {
            // ── Unknown — Failed (defensive, alert-worthy) ──
            _logger.LogError(ex, "Beklenmeyen hata: {ExceptionType} — Failed",
                ex.GetType().Name);
            await mutex.UpdateToFailedAsync(record, $"{ex.GetType().Name}: {ex.Message}", ct);
            return TenantProcessResult.Failed(firmaId, ex.Message, record);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private async Task<List<Firma>> GetAktifFirmalarAsync(CancellationToken ct)
    {
        await using var db = await _masterDbFactory.CreateDbContextAsync(ct);
        return await db.Firmalar
            .Where(f => !f.IsDeleted && f.Aktif)
            .OrderBy(f => f.Id)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    private static void ConfigureTenantScope(AsyncServiceScope scope, Firma firma)
    {
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
    }

    private static void ConfigureTenantScope(AsyncServiceScope scope, AktifFirmaBilgisi info)
    {
        var provider = scope.ServiceProvider.GetRequiredService<IAktifFirmaProvider>();
        provider.Set(info);
    }
}

// ── Internal types ─────────────────────────────────────────────────────

internal sealed class PuantajJobMetrics
{
    public int TotalTenants { get; set; }
    public int Successful { get; private set; }
    public int Failed { get; private set; }
    public int Skipped { get; private set; }
    public DateTime StartedAt { get; set; }
    public List<TenantProcessResult> Details { get; } = new();

    public void Add(TenantProcessResult r)
    {
        TotalTenants++;
        if (r.Status == TenantProcessStatus.Completed) Successful++;
        else if (r.Status == TenantProcessStatus.Skipped) Skipped++;
        else Failed++;
        Details.Add(r);
    }

    public void AddFailed(int firmaId, string error) =>
        Add(TenantProcessResult.Failed(firmaId, error));
}

internal sealed class TenantProcessResult
{
    public int? FirmaId { get; set; }
    public TenantProcessStatus Status { get; set; }
    public string? Message { get; set; }
    public PuantajJobExecution? Record { get; set; }
    public PuantajEngineSonucV1? EngineResult { get; set; }

    public static TenantProcessResult Completed(
        PuantajJobExecution record, PuantajEngineSonucV1 engine) => new()
    {
        FirmaId = record.FirmaId, Status = TenantProcessStatus.Completed,
        Record = record, EngineResult = engine
    };

    public static TenantProcessResult Skipped(string reason,
        PuantajJobExecution? record = null) => new()
    {
        FirmaId = record?.FirmaId, Status = TenantProcessStatus.Skipped,
        Message = reason, Record = record
    };

    public static TenantProcessResult Failed(int firmaId, string error,
        PuantajJobExecution? record = null) => new()
    {
        FirmaId = firmaId, Status = TenantProcessStatus.Failed,
        Message = error, Record = record
    };
}

internal enum TenantProcessStatus { Completed, Skipped, Failed }

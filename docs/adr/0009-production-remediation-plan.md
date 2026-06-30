# ADR-0009: Production Remediation Plan — Puantaj Background Job

> Senior Distributed Systems Architect | Tarih: 2026-05-26 | Status: Plan

## 1. Refactoring Roadmap

```
Sprint 9 (şimdi):
  Phase A: Exception hierarchy + magic strings
  Phase B: Config-driven Polly timeout
  Phase C: Idempotent audit log

Sprint 10:
  Phase D: Health checks
  Phase E: Observability (OTel + Prometheus)
  Phase F: Reconciliation job
  Phase G: Chaos testing
```

### Phase A: Exception Hierarchy

```csharp
// ── MKFiloServis.Shared/Exceptions/PuantajJobExceptions.cs ──

namespace MKFiloServis.Shared.Exceptions;

/// <summary>Engine tarafından fırlatılan, retry gerektirmeyen iş kuralı hataları.</summary>
public abstract class PuantajBusinessException : Exception
{
    protected PuantajBusinessException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>Hesap dönemi kilitli — revizyon yapılamaz.</summary>
public sealed class PuantajDonemKilitliException : PuantajBusinessException
{
    public int HesapDonemiId { get; }
    public int Versiyon { get; }

    public PuantajDonemKilitliException(int hesapDonemiId, int versiyon)
        : base($"Dönem kilitli: HesapDonemiId={hesapDonemiId}, V{versiyon}")
        => (HesapDonemiId, Versiyon) = (hesapDonemiId, versiyon);
}

/// <summary>Operasyon kaydı bulunamadı — hesaplanacak veri yok.</summary>
public sealed class PuantajOperasyonBulunamadiException : PuantajBusinessException
{
    public PuantajOperasyonBulunamadiException(int yil, int ay)
        : base($"{yil}/{ay:D2} döneminde işlenecek operasyon bulunamadı.") { }
}

/// <summary>Tenant veritabanına erişilemiyor.</summary>
public sealed class PuantajTenantOfflineException : PuantajBusinessException
{
    public int FirmaId { get; }
    public string? DatabaseName { get; }

    public PuantajTenantOfflineException(int firmaId, string? dbName, Exception inner)
        : base($"Firma {firmaId} tenant DB offline: {dbName ?? "shared"}", inner)
        => (FirmaId, DatabaseName) = (firmaId, dbName);
}
```

**Engine tarafında kullanım:**

```csharp
// PuantajEngineService.cs — mevcut throw'ları değiştir
if (oncekiAktif?.OnayDurum == PuantajDonemOnayDurum.Kilitli)
    throw new PuantajDonemKilitliException(oncekiAktif.Id, oncekiAktif.Versiyon);
```

**Job tarafında catch güncellemesi:**

```csharp
// PuantajJobService.cs ProcessSingleTenantAsync — catch blokları
try
{
    // ... idempotency + engine + audit
}
catch (OperationCanceledException)
{
    await mutex.UpdateToFailedAsync(record, "İptal edildi", ct);
    throw;
}
catch (PuantajBusinessException ex)
{
    // İş kuralı hatası — retry yok, Failed yap
    _logger.LogWarning(ex, "İş kuralı hatası — Failed");
    await mutex.UpdateToFailedAsync(record, ex.Message, ct);
    return TenantProcessResult.Failed(firmaId, ex.Message, record);
}
catch (Exception ex)
{
    // Beklenmeyen hata — Failed yap
    _logger.LogError(ex, "Beklenmeyen hata — Failed");
    await mutex.UpdateToFailedAsync(record, $"{ex.GetType().Name}: {ex.Message}", ct);
    return TenantProcessResult.Failed(firmaId, ex.Message, record);
}
```

### Phase B: Config-Driven Polly + Magic Strings

**appsettings.json:**

```json
{
  "PuantajEngine": {
    "AutoProcess": {
      "Enabled": true,
      "CronExpression": "0 30 0 1 * ?",
      "RetryCount": 3,
      "RetryDelaySeconds": [1, 2, 4],
      "EngineTimeoutSeconds": 300,
      "StaleTimeoutMinutes": 15
    }
  }
}
```

**Options class:**

```csharp
// MKFiloServis.Web/Services/PuantajJobOptions.cs
public sealed class PuantajJobOptions
{
    public const string Section = "PuantajEngine:AutoProcess";

    public bool Enabled { get; set; } = true;
    public string CronExpression { get; set; } = "0 30 0 1 * ?";
    public int RetryCount { get; set; } = 3;
    public int[] RetryDelaySeconds { get; set; } = [1, 2, 4];
    public int EngineTimeoutSeconds { get; set; } = 300;
    public int StaleTimeoutMinutes { get; set; } = 15;
}
```

**DI registration:**

```csharp
// Program.cs
builder.Services.Configure<PuantajJobOptions>(
    builder.Configuration.GetSection(PuantajJobOptions.Section));
```

**Refactored retry policy (non-static, config-driven):**

```csharp
// MKFiloServis.Web/Services/PuantajRetryPolicy.cs
public interface IPuantajRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action,
        string operationContext, CancellationToken ct);
}

public sealed class PuantajRetryPolicy : IPuantajRetryPolicy
{
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<PuantajRetryPolicy> _logger;

    public PuantajRetryPolicy(IOptions<PuantajJobOptions> options,
        ILogger<PuantajRetryPolicy> logger)
    {
        _logger = logger;
        var opt = options.Value;

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = opt.RetryCount,
                Delay = TimeSpan.FromSeconds(opt.RetryDelaySeconds.FirstOrDefault(1)),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<NpgsqlException>()
                    .Handle<TimeoutException>()
                    .Handle<PostgresException>(ex => ex.IsTransient),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry {Attempt}/{Max} for {Context} after {Delay}ms: {Error}",
                        args.AttemptNumber + 1, opt.RetryCount,
                        args.Context.OperationKey,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(opt.EngineTimeoutSeconds))
            .Build();
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action,
        string operationContext, CancellationToken ct)
    {
        return await _pipeline.ExecuteAsync(
            async innerCt => await action(innerCt),
            ct);
    }
}
```

**Refactored PuantajJobService (SRP clean):**

```csharp
// PuantajJobService.cs — refactored constructor
public sealed class PuantajJobService : IPuantajJobService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbContextFactory<MasterDbContext> _masterDbFactory;
    private readonly IPuantajRetryPolicy _retryPolicy;
    private readonly IPuantajJobMetricsCollector _metrics;
    private readonly PuantajJobOptions _options;
    private readonly ILogger<PuantajJobService> _logger;

    public PuantajJobService(
        IServiceScopeFactory scopeFactory,
        IDbContextFactory<MasterDbContext> masterDbFactory,
        IPuantajRetryPolicy retryPolicy,
        IPuantajJobMetricsCollector metrics,
        IOptions<PuantajJobOptions> options,
        ILogger<PuantajJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _masterDbFactory = masterDbFactory;
        _retryPolicy = retryPolicy;
        _metrics = metrics;
        _options = options.Value;
        _logger = logger;
    }
}
```

## 2. Exception Hierarchy Design

```
Exception
├── PuantajBusinessException (abstract)
│   ├── PuantajDonemKilitliException
│   ├── PuantajOperasyonBulunamadiException
│   └── PuantajTenantOfflineException
├── PuantajMutexException
│   └── PuantajMutexAcquireFailedException
└── PuantajJobException
    └── PuantajFirmaEnumerationFailedException
```

**Polly ShouldHandle güncellemesi:**

```csharp
ShouldHandle = new PredicateBuilder()
    .Handle<NpgsqlException>()
    .Handle<TimeoutException>()
    .Handle<PostgresException>(ex => ex.IsTransient)
    // Explicitly DO NOT retry business exceptions
    // .Handle<PuantajBusinessException>() ← YOK, retry yapılmaz
```

## 3. Idempotent Audit Strategy

**Problem:** Engine commit eder, audit log yazılamaz → Aktif dönem var, audit yok.

**Çözüm: Audit-first approach + idempotent retry.**

```csharp
// PuantajJobService.cs — refactored ProcessSingleTenantAsync core

// 4. Engine çalıştır
var engineResult = await _retryPolicy.ExecuteAsync(
    async innerCt =>
    {
        var result = await engine.ProcessDonemAsync(
            yil, ay, kurumId: null, hesaplayan: tetikleyen,
            notlar: $"Auto ({tetikleyen})", ct: innerCt);
        return result;
    }, $"Firma{firmaId}/{yil}/{ay:00}", ct);

// 5. Audit log — idempotent yaz (fail olsa da mutex Completed)
try
{
    await WriteAuditLogAsync(dbFactory, firmaId, tetikleyen, engineResult, ct);
}
catch (Exception auditEx)
{
    // Audit log yazılamadı AMA engine commit etti → Completed say
    _logger.LogError(auditEx,
        "Audit log yazılamadı (engine OK) — Firma {FirmaId} V{Version}",
        firmaId, engineResult.Versiyon);
    // Reconciliation job daha sonra eksik audit log'u tamamlar
}

// 6. Mutex → Completed (HER ZAMAN)
await mutex.UpdateToCompletedAsync(record, engineResult, ct);
```

**Idempotent audit log yazma:**

```csharp
private static async Task WriteAuditLogAsync(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    int firmaId, string kullanici,
    PuantajEngineSonucV1 result, CancellationToken ct)
{
    await using var db = await dbFactory.CreateDbContextAsync(ct);

    // Idempotency: Aynı HesapDonemiId için zaten audit log var mı?
    var existing = await db.PuantajAuditLogs
        .Where(l => l.HesapDonemiId == result.HesapDonemiId
                    && l.Aksiyon == PuantajAuditAksiyon.Hesaplandi)
        .AnyAsync(ct);

    if (existing) return; // Zaten yazılmış

    db.PuantajAuditLogs.Add(new PuantajAuditLog
    {
        FirmaId = firmaId,
        HesapDonemiId = result.HesapDonemiId,
        Aksiyon = PuantajAuditAksiyon.Hesaplandi,
        Kullanici = kullanici,
        AksiyonTarihi = DateTime.UtcNow,
        OncekiDurum = "Yok",
        YeniDurum = $"Aktif V{result.Versiyon}",
        Aciklama = $"Job: {result.IslenenOperasyonSayisi} op → {result.UretilenPuantajKayit} kayıt",
        CreatedAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync(ct);
}
```

## 4. Recovery / Reconciliation Job Design

**Amaç:** Audit log eksikliklerini, inconsistent mutex durumlarını ve orphan kayıtları tespit edip düzeltmek.

```csharp
// MKFiloServis.Web/Jobs/PuantajReconciliationJob.cs

[DisallowConcurrentExecution]
public class PuantajReconciliationJob : IJob
{
    private readonly IReconciliationService _reconciliation;

    public async Task Execute(IJobExecutionContext context)
    {
        await _reconciliation.RunAsync(context.CancellationToken);
    }
}

public interface IReconciliationService
{
    Task<ReconciliationReport> RunAsync(CancellationToken ct);
}

public sealed class ReconciliationReport
{
    public int MissingAuditLogsFound { get; set; }
    public int MissingAuditLogsFixed { get; set; }
    public int StaleMutexCleaned { get; set; }
    public int OrphanActivePeriodsFound { get; set; }
    public List<string> Issues { get; } = new();
}

public sealed class ReconciliationService : IReconciliationService
{
    private readonly IDbContextFactory<MasterDbContext> _masterDb;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReconciliationService> _logger;

    public async Task<ReconciliationReport> RunAsync(CancellationToken ct)
    {
        var report = new ReconciliationReport();
        var firmalar = await GetFirmalarAsync(ct);

        foreach (var firma in firmalar)
        {
            ct.ThrowIfCancellationRequested();
            await using var scope = CreateTenantScope(firma);

            try
            {
                await ReconcileTenantAsync(scope, report, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconciliation failed for Firma {FirmaId}", firma.Id);
                report.Issues.Add($"Firma {firma.Id}: {ex.Message}");
            }
        }

        _logger.LogInformation("Reconciliation completed: {Report}", report);
        return report;
    }

    private static async Task ReconcileTenantAsync(
        AsyncServiceScope scope, ReconciliationReport report, CancellationToken ct)
    {
        var dbFactory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // 1. Eksik audit log'ları bul ve düzelt
        var activeDonemIds = await db.PuantajHesapDonemleri
            .Where(h => h.Durum == PuantajHesapDurum.Aktif && !h.IsDeleted)
            .Select(h => h.Id)
            .ToListAsync(ct);

        foreach (var donemId in activeDonemIds)
        {
            var hasAudit = await db.PuantajAuditLogs
                .Where(l => l.HesapDonemiId == donemId
                            && l.Aksiyon == PuantajAuditAksiyon.Hesaplandi
                            && !l.IsDeleted)
                .AnyAsync(ct);

            if (!hasAudit)
            {
                report.MissingAuditLogsFound++;

                var donem = await db.PuantajHesapDonemleri.FindAsync(donemId);
                if (donem == null) continue;

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

        await db.SaveChangesAsync(ct);

        // 2. Stale mutex temizliği
        var staleThreshold = DateTime.UtcNow.AddMinutes(-30);
        var staleMutex = await db.PuantajJobExecutions
            .Where(j => j.Durum == PuantajJobExecutionDurum.Running
                        && j.Baslangic < staleThreshold
                        && !j.IsDeleted)
            .ToListAsync(ct);

        foreach (var m in staleMutex)
        {
            // Eğer bu tenant/ay için Aktif HesapDonemi varsa → mutex Completed yap
            var hasActive = await db.PuantajHesapDonemleri
                .Where(h => h.FirmaId == m.FirmaId && h.Yil == m.Yil
                            && h.Ay == m.Ay && h.Durum == PuantajHesapDurum.Aktif
                            && !h.IsDeleted)
                .AnyAsync(ct);

            if (hasActive)
            {
                m.Durum = PuantajJobExecutionDurum.Completed;
                m.HataMesaji = "Reconciliation: engine başarılı ama mutex güncellenmemiş";
            }
            else
            {
                m.Durum = PuantajJobExecutionDurum.Failed;
                m.HataMesaji = "Reconciliation: stale Running — engine çalışmamış";
            }
            m.Bitis = DateTime.UtcNow;
            report.StaleMutexCleaned++;
        }

        await db.SaveChangesAsync(ct);
    }
}
```

**Schedule:** Her gün 03:00 (backup'tan sonra), veya haftada bir Pazar.

## 5. Transaction Boundary Redesign

### Mevcut Durum

```
Connection 1: Mutex INSERT        → COMMIT (autocommit)
Connection 2: Idempotency SELECT  → (read only)
Connection 3: Engine BEGIN TRAN   → INSERT/UPDATE → COMMIT
Connection 4: Audit INSERT        → COMMIT (autocommit)
Connection 5: Mutex UPDATE        → COMMIT (autocommit)
```

### Hedef Durum (Mutex + Engine aynı transaction'da OLAMAZ — engine kendi DbContext'ini yaratır)

**Kabul:** Engine ve mutex farklı transaction'larda. Bu tasarımın doğal sonucu. Çözüm: **Saga pattern** — her adım idempotent, her adımın compensating action'ı var.

```
┌─────────────────────────────────────────────────────────┐
│                     SAGA (Orchestrator)                  │
├──────────┬────────────────────┬─────────────────────────┤
│  Step    │  Action            │  Compensating Action    │
├──────────┼────────────────────┼─────────────────────────┤
│  1       │  Mutex INSERT      │  Mutex UPDATE → Failed  │
│  2       │  Idempotency check │  N/A (read-only)        │
│  3       │  Engine execution  │  IptalEtAsync (engine)  │
│  4       │  Audit log INSERT  │  Reconciliation job     │
│  5       │  Mutex → Completed │  Mutex → Failed         │
└──────────┴────────────────────┴─────────────────────────┘
```

**Implementation:**

```csharp
// Saga orkestratörü — her adımın başarısızlığında compensating action
private async Task<TenantProcessResult> ProcessSingleTenantAsync(
    AsyncServiceScope scope, int firmaId, string firmaAdi,
    int yil, int ay, string tetikleyen, CancellationToken ct)
{
    var mutex = scope.ServiceProvider.GetRequiredService<IPuantajMutexService>();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    var saga = new PuantajSaga(mutex, dbFactory, _logger);

    // Step 1: Mutex
    var acquire = await mutex.TryAcquireAsync(firmaId, yil, ay, tetikleyen, ct);
    if (!acquire.Acquired)
        return TenantProcessResult.Skipped(acquire.FailureReason ?? "Mutex");

    saga.RecordStep("mutex_acquired", acquire.Record!);

    try
    {
        // Step 2: Idempotency
        if (await saga.CheckIdempotencyAsync(firmaId, yil, ay, ct))
            return saga.Complete();

        // Step 3: Engine
        var engine = scope.ServiceProvider.GetRequiredService<IPuantajEngineService>();
        var engineResult = await _retryPolicy.ExecuteAsync(
            async innerCt => await engine.ProcessDonemAsync(
                yil, ay, null, tetikleyen, $"Auto ({tetikleyen})", innerCt),
            $"Engine:F{firmaId}/{yil}/{ay:00}", ct);

        saga.RecordStep("engine_completed", engineResult);

        // Step 4: Audit (best-effort, compensating: reconciliation)
        await saga.WriteAuditAsync(firmaId, tetikleyen, engineResult, ct);

        // Step 5: Mutex → Completed
        await mutex.UpdateToCompletedAsync(saga.MutexRecord!, engineResult, ct);
        saga.RecordStep("mutex_completed");

        return saga.Complete(engineResult);
    }
    catch (OperationCanceledException)
    {
        await saga.CompensateAsync("İptal edildi", ct);
        throw;
    }
    catch (PuantajBusinessException ex)
    {
        await saga.CompensateAsync(ex.Message, ct);
        return saga.FailedResult(firmaId);
    }
    catch (Exception ex)
    {
        await saga.CompensateAsync($"{ex.GetType().Name}: {ex.Message}", ct);
        return saga.FailedResult(firmaId);
    }
}
```

## 6. Observability Improvements

### Structured Logging Enrichment

```csharp
// Her tenant işlemi için zengin scope
using var tenantScope = _logger.BeginScope(new Dictionary<string, object>
{
    ["Tenant.FirmaId"] = firmaId,
    ["Tenant.FirmaAdi"] = firmaAdi,
    ["Puantaj.Yil"] = yil,
    ["Puantaj.Ay"] = ay,
    ["Job.RunId"] = jobRunId,
    ["Job.Tetikleyen"] = tetikleyen,
    ["Correlation.Id"] = Activity.Current?.Id ?? "none"
});
```

### Key Metrics (Numeric + Structured)

```csharp
// MKFiloServis.Web/Telemetry/PuantajJobMetrics.cs
public sealed class PuantajJobMetricsDefinitions
{
    // Counters
    public const string TenantsTotal = "puantaj_job_tenants_total";
    public const string TenantsSucceeded = "puantaj_job_tenants_succeeded";
    public const string TenantsFailed = "puantaj_job_tenants_failed";
    public const string TenantsSkipped = "puantaj_job_tenants_skipped";

    // Gauges
    public const string JobDurationSeconds = "puantaj_job_duration_seconds";
    public const string EngineDurationSeconds = "puantaj_engine_duration_seconds";

    // Histograms
    public const string OperationsPerTenant = "puantaj_operations_per_tenant";
    public const string MutexAcquireLatencyMs = "puantaj_mutex_acquire_latency_ms";

    // Tags
    public const string TagFirmaId = "firma_id";
    public const string TagYil = "yil";
    public const string TagAy = "ay";
    public const string TagTrigger = "trigger";
    public const string TagStatus = "status";
}
```

## 7. OpenTelemetry + Prometheus Metrics Plan

### NuGet Packages

```xml
<!-- MKFiloServis.Web.csproj -->
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.11.2" />
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.11.1-beta.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.11.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.11.0-beta.2" />
<PackageReference Include="OpenTelemetry.Instrumentation.Quartz" Version="1.10.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.11.1" />
```

### OTel Setup (Program.cs)

```csharp
// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddQuartzInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MKFiloServis.Puantaj")
        .AddPrometheusExporter())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddNpgsql()
        .AddQuartzInstrumentation()
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"]
                ?? "http://localhost:4317");
        }));

// Prometheus endpoint
app.UseOpenTelemetryPrometheusScrapingEndpoint(); // GET /metrics
```

### Custom Meter

```csharp
// MKFiloServis.Web/Telemetry/PuantajMeter.cs
public sealed class PuantajMeter : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _tenantsProcessed;
    private readonly Histogram<double> _engineDuration;
    private readonly Histogram<double> _mutexLatency;

    public PuantajMeter()
    {
        _meter = new Meter("MKFiloServis.Puantaj", "1.0.0");

        _tenantsProcessed = _meter.CreateCounter<long>(
            "puantaj_tenants_processed_total",
            description: "Total tenants processed by the puantaj job");

        _engineDuration = _meter.CreateHistogram<double>(
            "puantaj_engine_duration_seconds",
            unit: "s",
            description: "Engine execution duration per tenant");

        _mutexLatency = _meter.CreateHistogram<double>(
            "puantaj_mutex_acquire_latency_ms",
            unit: "ms",
            description: "Mutex acquire latency");
    }

    public void RecordTenant(int firmaId, string status, double engineDurationMs)
    {
        var tags = new TagList
        {
            { "firma_id", firmaId },
            { "status", status }
        };
        _tenantsProcessed.Add(1, tags);
        _engineDuration.Record(engineDurationMs / 1000.0,
            new TagList { { "firma_id", firmaId } });
    }

    public void RecordMutexLatency(double latencyMs, bool acquired)
    {
        _mutexLatency.Record(latencyMs,
            new TagList { { "acquired", acquired } });
    }

    public void Dispose() => _meter.Dispose();
}

// DI
builder.Services.AddSingleton<PuantajMeter>();
```

## 8. Health Check Strategy

### Endpoints

| Path | Purpose | Degraded When |
|------|---------|---------------|
| `/healthz` | Liveness (K8s) | App çalışıyor mu |
| `/readyz` | Readiness (K8s) | DB + Quartz hazır mı |
| `/health/puantaj-job` | Job status | Son çalışma 24 saati geçti mi |

### Implementation

```csharp
// MKFiloServis.Web/HealthChecks/PuantajJobHealthCheck.cs
public sealed class PuantajJobHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var lastRun = await db.PuantajJobExecutions
            .Where(j => !j.IsDeleted)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new { j.Durum, j.Baslangic, j.Tetikleyen })
            .FirstOrDefaultAsync(ct);

        if (lastRun == null)
            return HealthCheckResult.Healthy("Henüz çalışmadı");

        var elapsed = DateTime.UtcNow - (lastRun.Baslangic ?? DateTime.MinValue);

        var data = new Dictionary<string, object>
        {
            ["LastRun"] = lastRun.Baslangic?.ToString("O") ?? "N/A",
            ["LastStatus"] = lastRun.Durum.ToString(),
            ["Trigger"] = lastRun.Tetikleyen,
            ["HoursSinceLastRun"] = elapsed.TotalHours.ToString("F1")
        };

        if (elapsed.TotalHours > 24 && lastRun.Durum != PuantajJobExecutionDurum.Running)
            return HealthCheckResult.Degraded(
                $"Son çalışma {elapsed.TotalHours:F0} saat önce", data: data);

        if (lastRun.Durum == PuantajJobExecutionDurum.Failed)
            return HealthCheckResult.Degraded("Son çalışma Failed", data: data);

        return HealthCheckResult.Healthy("OK", data);
    }
}
```

**Registration (Program.cs):**

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<PuantajJobHealthCheck>("puantaj_job", tags: ["job"])
    .AddNpgSql(masterConnectionString, name: "master_db", tags: ["db"])
    .AddProcessAllocatedMemoryHealthCheck(500_000_000, "memory"); // 500MB

app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => false, // liveness: sadece process alive
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/puantaj-job", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("job"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

## 9. Production Deployment Checklist

### Deploy Öncesi

- [ ] `PuantajEngine:AutoProcess:Enabled = true` production config'te doğru mu?
- [ ] `CronExpression = "0 30 0 1 * ?"` timezone UTC ile uyumlu mu? (Türkiye UTC+3 → 00:30 UTC = 03:30 TR)
- [ ] Connection string'ler tüm ortamlarda (dev/staging/prod) doğru mu?
- [ ] `PuantajJobExecutions` migration'ı tüm tenant DB'lere uygulandı mı?
- [ ] Polly 8.6.6 NuGet package restore başarılı mı?
- [ ] Build: `dotnet build --configuration Release` 0 error?
- [ ] Tests: `dotnet test --configuration Release` hepsi geçiyor mu?
- [ ] Stale timeout 30dk → production'da 15dk yap (config'den)

### Deploy Sırası

- [ ] Maintenance window: Ayın 1'i 00:00-04:00 arası deploy planla
- [ ] Deployment order: Staging → Canary (1 node) → Full prod
- [ ] Rollback plan: `PuantajEngine:AutoProcess:Enabled = false` config değişikliği (kod deploy gerektirmez)
- [ ] DB migration'ı önce çalıştır (`dotnet ef database update`), SONRA app deploy

### Deploy Sonrası (ilk 24 saat)

- [ ] Log'ları kontrol et: `PuantajJob başladı` mesajı var mı?
- [ ] `/health/puantaj-job` endpoint'ine istek at, sonuç Healthy/Degraded?
- [ ] `PuantajJobExecutions` tablosunda yeni kayıt var mı?
- [ ] Engine başarılı mı? `PuantajHesapDonemleri` tablosunda yeni Aktif kayıt?
- [ ] Eğer job elle tetiklendiyse: `POST /api/puantaj/jobs/process/{yil}/{ay}` cevabı OK?

### Monitoring

- [ ] Grafana dashboard: `puantaj_tenants_processed_total` metric
- [ ] Alert: `puantaj_job_tenants_failed > 0` → Slack/Email
- [ ] Alert: `puantaj_job_duration_seconds > 600` (10dk)
- [ ] Alert: `/health/puantaj-job` Degraded > 1 saat

## 10. Chaos / Failure Test Scenarios

| # | Senaryo | Nasıl Test Edilir | Beklenen Davranış |
|---|---------|-------------------|-------------------|
| **F1** | Engine crash mid-process | `ProcessDonemAsync`'te `throw new Exception("simulated crash")` | Mutex Failed, diğer tenant'lar devam eder |
| **F2** | PostgreSQL restart mid-job | `pg_ctl restart` engine çalışırken | Polly retry 3x → Failed. Sonraki run'da tekrar dener |
| **F3** | Connection pool exhaustion | `MaxPoolSize=1` yap, 2 tenant aynı anda | Sequential çalışır, sırayla pool kullanılır |
| **F4** | Stale mutex after crash | Kill process mutex insert'ten hemen sonra | 15dk sonra CleanupStale → Failed. Sonraki run başarılı |
| **F5** | Concurrent Quartz + Manual | İki terminalden aynı anda tetikle | Biri mutex alır, diğeri Skipped |
| **F6** | Tenant DB offline | Tenant DB'yi `pg_ctl stop` yap | O tenant Failed, diğerleri devam eder |
| **F7** | Master DB offline | Master DB'yi kapat | Tüm job Failed (firma listesi alınamaz) |
| **F8** | Large dataset (100K+ operasyon) | Test DB'sine 100K OperasyonKaydi ekle | Timeout aşmazsa başarılı, aşarsa Failed |
| **F9** | Network partition | `iptables -A OUTPUT -p tcp --dport 5432 -j DROP` | Polly retry 3x → Failed |
| **F10** | Out of memory | `GC.AddMemoryPressure(2GB)` | Process restart, Quartz DoNothing → sonraki schedule'da düzgün çalışır |

### Chaos Test Script (Bash/PowerShell)

```powershell
# chaos-test.ps1 — F4: Stale mutex simulation
Write-Host "=== F4: Stale mutex recovery test ==="

# 1. Start a job manually, kill it mid-way
$job = Start-Job -ScriptBlock {
    Invoke-RestMethod -Uri "http://localhost:5190/api/puantaj/jobs/process/2026/5" `
        -Method Post -Headers @{ Authorization = "Bearer $env:TEST_TOKEN" }
}
Start-Sleep -Seconds 2
Stop-Job -Job $job

# 2. Verify Running record exists
$running = psql -c "SELECT count(*) FROM ""PuantajJobExecutions"" WHERE ""Durum"" = 0;"
Write-Host "Running records: $running"

# 3. Wait for stale timeout (or trigger cleanup)
Write-Host "Waiting for stale timeout..."
Start-Sleep -Seconds 15  # Test için 15dk değil, config'i 1dk yap

# 4. Run job again — should cleanup stale and succeed
$response = Invoke-RestMethod -Uri "http://localhost:5190/api/puantaj/jobs/process/2026/5" `
    -Method Post -Headers @{ Authorization = "Bearer $env:TEST_TOKEN" }
Write-Host "Result: $($response.durumAdi)"

# 5. Verify no duplicate Aktif periods
$active = psql -c "SELECT count(*) FROM ""PuantajHesapDonemleri"" WHERE ""Durum"" = 1;"
Write-Host "Active periods: $active"
if ($active -eq 1) { Write-Host "PASS" -ForegroundColor Green }
else { Write-Host "FAIL" -ForegroundColor Red }
```

---

## Summary: Implementation Priority Matrix

| Priority | Item | Effort | Impact |
|:--------:|------|:------:|:------:|
| P0 | Exception hierarchy + catch fix (Phase A) | 2h | Yüksek |
| P0 | Config-driven timeout (Phase B) | 1h | Yüksek |
| P0 | Idempotent audit log (Phase C) | 1h | Yüksek |
| P1 | Health checks (Phase D) | 2h | Orta |
| P1 | Reconciliation job (Phase F) | 3h | Orta |
| P1 | Stale timeout configurable | 0.5h | Orta |
| P2 | OTel + Prometheus (Phase E) | 4h | Orta |
| P2 | Chaos test scripts (Phase G) | 3h | Düşük |
| P2 | SRP refactor (retry, metrics, scope) | 4h | Düşük |
| P3 | Integration test suite (PostgreSQL container) | 6h | Düşük |


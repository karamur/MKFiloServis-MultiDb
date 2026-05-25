# ADR-0010: Observability Architecture — Puantaj Background Jobs

> Senior SRE + Observability Architect | Tarih: 2026-05-26

---

## 1. Trace Strategy

### Span Hierarchy

```
PuantajEngineJob.Execute
├── GetAktifFirmalarAsync (Master DB)
│   └── SELECT * FROM Firmalar
├── [For each tenant]
│   └── ProcessTenantAsync:FirmaId={id}
│       ├── PuantajMutex.CleanupStale
│       │   └── UPDATE PuantajJobExecutions (stale → Failed)
│       ├── PuantajMutex.TryAcquire
│       │   └── INSERT INTO PuantajJobExecutions
│       ├── IdempotencyCheck
│       │   └── SELECT FROM PuantajHesapDonemleri
│       ├── Engine.ProcessDonemAsync [Polly retry wrapper]
│       │   ├── SELECT OperasyonKayitlari
│       │   ├── SELECT Guzergahlar
│       │   ├── BEGIN TRANSACTION
│       │   ├── INSERT PuantajHesapDonemleri
│       │   ├── INSERT PuantajKayitlar
│       │   ├── INSERT PuantajDetaylari
│       │   └── COMMIT
│       ├── WriteAuditLog
│       │   └── INSERT PuantajAuditLogs
│       └── PuantajMutex.UpdateToCompleted
│           └── UPDATE PuantajJobExecutions
└── ReconciliationJob.Execute
    ├── [For each tenant]
    │   └── ReconcileTenant:FirmaId={id}
    │       ├── FindMissingAuditLogs
    │       └── CleanupStaleMutex
```

### Span Attributes Convention

| Attribute | Type | Example | Scope |
|-----------|------|---------|-------|
| `job.name` | string | `puantaj_engine` | Root span |
| `job.trigger` | string | `Quartz`, `Manuel`, `Manuel-Retry` | Root span |
| `job.run_id` | string | `a1b2c3d4` | Root span (GUID) |
| `tenant.firma_id` | int | `5` | Tenant span |
| `tenant.firma_kodu` | string | `KOA` | Tenant span |
| `tenant.db_name` | string | `Koa_KOA_005` | Tenant span |
| `puantaj.yil` | int | `2026` | Engine span |
| `puantaj.ay` | int | `5` | Engine span |
| `mutex.acquired` | bool | `true` | Mutex span |
| `mutex.latency_ms` | double | `12.5` | Mutex span |
| `engine.version` | int | `3` | Engine span |
| `engine.operations` | int | `150` | Engine span |
| `retry.attempt` | int | `2` | Retry span |
| `retry.max_attempts` | int | `3` | Retry span |
| `db.operation` | string | `SELECT` | DB span |
| `db.table` | string | `PuantajHesapDonemleri` | DB span |
| `db.rows_affected` | int | `10` | DB span |
| `error.type` | string | `NpgsqlException` | Error span |
| `error.message` | string | `Connection reset` | Error span |

---

## 2. Correlation ID Propagation

### Correlation Context

```csharp
// KOAFiloServis.Web/Telemetry/CorrelationContext.cs
public sealed class CorrelationContext
{
    public string JobRunId { get; init set; } = Guid.NewGuid().ToString("N")[..8];
    public string? TenantFirmaId { get; set; }
    public string? TriggerType { get; init set; }
    public int Yil { get; init set; }
    public int Ay { get; init set; }

    public IDisposable BeginJobScope(ILogger logger)
    {
        var state = new Dictionary<string, object>
        {
            ["Correlation.JobRunId"] = JobRunId,
            ["Correlation.Trigger"] = TriggerType ?? "unknown",
            ["Correlation.Yil"] = Yil,
            ["Correlation.Ay"] = Ay
        };
        return logger.BeginScope(state);
    }

    public IDisposable BeginTenantScope(ILogger logger, int firmaId, string firmaAdi)
    {
        TenantFirmaId = firmaId.ToString();
        var state = new Dictionary<string, object>
        {
            ["Correlation.JobRunId"] = JobRunId,
            ["Correlation.TenantId"] = firmaId,
            ["Correlation.TenantName"] = firmaAdi
        };
        return logger.BeginScope(state);
    }
}
```

### Propagation Chain

```
┌───────────────────────────────────────────────────────────┐
│                    TRACE CONTEXT                          │
│  traceparent: 00-{trace_id}-{span_id}-01                  │
│  tracestate: koa=job_run_id:a1b2c3d4                     │
├───────────────────────────────────────────────────────────┤
│  Quartz Job → IPuantajJobService                          │
│    │ Activity.Current.Id = span_id                        │
│    │ Baggage["JobRunId"] = "a1b2c3d4"                     │
│    │                                                      │
│    ├─ GetAktifFirmalarAsync                               │
│    │   └─ ActivitySource.StartActivity("GetFirmalar")     │
│    │      Links: [] (no parent link)                      │
│    │                                                      │
│    └─ ProcessTenantAsync                                  │
│        └─ ActivitySource.StartActivity("ProcessTenant")   │
│           Links: [new ActivityLink(root.Context)]          │
│           Baggage["TenantId"] = firmaId                    │
│                                                           │
│           └─ Mutex.TryAcquire                             │
│              └─ ActivitySource.StartActivity("Mutex")     │
│                 Links: [parent]                            │
│                                                           │
│           └─ Engine.ProcessDonemAsync                     │
│              └─ ActivitySource.StartActivity("Engine")    │
│                 Links: [parent]                            │
│                 │                                         │
│                 └─ EF Core: tags["db.system"]="postgresql"│
│                    traceparent header → DB query log       │
└───────────────────────────────────────────────────────────┘
```

### ActivitySource Setup

```csharp
// KOAFiloServis.Web/Telemetry/PuantajActivitySource.cs
public static class PuantajActivitySource
{
    public const string Name = "KOAFiloServis.Puantaj";
    public static readonly ActivitySource Source = new(Name, "1.0.0");

    // Span name constants — cardinality control
    public static class Spans
    {
        public const string JobRun = "puantaj.job.run";
        public const string TenantProcess = "puantaj.tenant.process";
        public const string MutexAcquire = "puantaj.mutex.acquire";
        public const string MutexRelease = "puantaj.mutex.release";
        public const string EngineExecute = "puantaj.engine.execute";
        public const string IdempotencyCheck = "puantaj.idempotency.check";
        public const string AuditWrite = "puantaj.audit.write";
        public const string Reconciliation = "puantaj.reconciliation.run";
        public const string RetryAttempt = "puantaj.retry.attempt";
    }
}
```

**Usage in PuantajJobService:**

```csharp
public async Task<PuantajJobExecution> ProcessAllTenantsAsync(
    int yil, int ay, string tetikleyen, CancellationToken ct = default)
{
    using var activity = PuantajActivitySource.Source.StartActivity(
        PuantajActivitySource.Spans.JobRun,
        ActivityKind.Internal);

    activity?.SetTag("job.trigger", tetikleyen);
    activity?.SetTag("puantaj.yil", yil);
    activity?.SetTag("puantaj.ay", ay);

    var ctx = new CorrelationContext
    {
        JobRunId = activity?.TraceId.ToString()[..8] ?? Guid.NewGuid().ToString("N")[..8],
        TriggerType = tetikleyen, Yil = yil, Ay = ay
    };

    using var _ = ctx.BeginJobScope(_logger);
    // ... rest of the method
}
```

---

## 3. Structured Logging Schema

### Log Event Schema (JSON)

```json
{
  "@t": "2026-05-26T00:30:05.123Z",
  "@m": "Hesaplandı V{Version}: {Ops} op → {Pk} kayıt",
  "@l": "Information",
  "@mt": "Hesaplandı V{Version}: {Ops} op → {Pk} kayıt",
  "@tr": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
  "@sp": "c1d2e3f4a5b6c7d8",
  "Correlation.JobRunId": "a1b2c3d4",
  "Correlation.Trigger": "Quartz",
  "Correlation.TenantId": 5,
  "Correlation.TenantName": "KOA A.Ş.",
  "Puantaj.Yil": 2026,
  "Puantaj.Ay": 5,
  "Version": 2,
  "Ops": 150,
  "Pk": 45,
  "Engine.HesapDonemiId": 1250,
  "Engine.ElapsedMs": 3200,
  "Mutex.LatencyMs": 8.5,
  "Retry.Attempt": 0,
  "SourceContext": "KOAFiloServis.Web.Services.PuantajJobService",
  "RequestPath": "/_blazor",
  "ConnectionId": "0HNA8FTE0P0TR"
}
```

### Serilog Configuration

```csharp
// Program.cs
builder.Host.UseSerilog((ctx, services, config) =>
{
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "KOAFiloServis")
        .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
        .Filter.ByExcluding(logEvent =>
            logEvent.Properties.GetValueOrDefault("RequestPath")?.ToString()
                .Contains("/health") == true)
        .WriteTo.Console(new CompactJsonFormatter())
        .WriteTo.OpenTelemetry(opt =>
        {
            opt.Endpoint = ctx.Configuration["Otlp:Endpoint"] ?? "http://localhost:4317";
            opt.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "KOAFiloServis",
                ["service.version"] = "1.0.0",
                ["deployment.environment"] = ctx.HostingEnvironment.EnvironmentName
            };
        });
});
```

### Log Levels by Component

| Component | Default | Production | Rationale |
|-----------|:-------:|:----------:|-----------|
| PuantajJobService | Information | Information | İş akışı takibi |
| PuantajMutexService | Information | Warning | Sadece çakışma log'lanır |
| PuantajEngineService | Information | Information | Hesaplama sonucu |
| Polly RetryPolicy | Warning | Warning | Sadece retry olduğunda |
| ReconciliationService | Information | Information | Tamirat işlemleri |
| EF Core (queries) | Debug | None | Production'da trace yeterli |
| Quartz scheduler | Information | Warning | Schedule değişiklikleri |

---

## 4. Prometheus Metrics Design

### Metric Catalog

```csharp
// KOAFiloServis.Web/Telemetry/PuantajMetrics.cs
public static class PuantajMetricDefinitions
{
    public const string MeterName = "KOAFiloServis.Puantaj";

    // ── COUNTERS (monotonically increasing) ──────────────────────

    /// <summary>Total job runs (by trigger type).</summary>
    public static readonly MetricDef JobRunsTotal = new(
        "puantaj_job_runs_total", "counter",
        "Total number of puantaj job executions",
        ["trigger"]); // Quartz | Manuel | Manuel-Retry

    /// <summary>Total tenants processed (by status).</summary>
    public static readonly MetricDef TenantsProcessedTotal = new(
        "puantaj_tenants_processed_total", "counter",
        "Total tenants processed with final status",
        ["firma_id", "status"]); // completed | failed | skipped

    /// <summary>Total engine invocations (by retry attempt).</summary>
    public static readonly MetricDef EngineInvocationsTotal = new(
        "puantaj_engine_invocations_total", "counter",
        "Total engine ProcessDonemAsync calls",
        ["firma_id", "retry_attempt", "outcome"]); // success | error

    /// <summary>Total mutex operations.</summary>
    public static readonly MetricDef MutexOperationsTotal = new(
        "puantaj_mutex_operations_total", "counter",
        "Total mutex acquire/release operations",
        ["firma_id", "operation", "result"]); // acquire | release, acquired | collision | failed

    /// <summary>Total reconciliation fixes.</summary>
    public static readonly MetricDef ReconciliationFixesTotal = new(
        "puantaj_reconciliation_fixes_total", "counter",
        "Total fixes applied by reconciliation job",
        ["fix_type"]); // missing_audit_log | stale_mutex | orphan_period

    // ── HISTOGRAMS ──────────────────────────────────────────────

    /// <summary>Job total duration (seconds).</summary>
    public static readonly MetricDef JobDurationSeconds = new(
        "puantaj_job_duration_seconds", "histogram",
        "Total job execution duration",
        ["trigger"],
        buckets: [1, 5, 10, 30, 60, 120, 300, 600]);

    /// <summary>Engine execution duration per tenant (seconds).</summary>
    public static readonly MetricDef EngineDurationSeconds = new(
        "puantaj_engine_duration_seconds", "histogram",
        "Engine ProcessDonemAsync duration per tenant",
        ["firma_id"],
        buckets: [0.5, 1, 2, 5, 10, 30, 60, 120]);

    /// <summary>Mutex acquire latency (milliseconds).</summary>
    public static readonly MetricDef MutexAcquireLatencyMs = new(
        "puantaj_mutex_acquire_latency_ms", "histogram",
        "Mutex TryAcquireAsync latency",
        ["firma_id", "result"],
        buckets: [1, 5, 10, 25, 50, 100, 250, 500]);

    // ── GAUGES ──────────────────────────────────────────────────

    /// <summary>Currently running jobs (should be 0 or 1).</summary>
    public static readonly MetricDef JobsInFlight = new(
        "puantaj_jobs_in_flight", "gauge",
        "Number of currently running puantaj jobs");

    /// <summary>Hours since last successful job run.</summary>
    public static readonly MetricDef HoursSinceLastSuccess = new(
        "puantaj_hours_since_last_success", "gauge",
        "Hours since the last successful job completion");

    /// <summary>Number of stale mutex records.</summary>
    public static readonly MetricDef StaleMutexCount = new(
        "puantaj_stale_mutex_count", "gauge",
        "Number of stale (Running > threshold) mutex records");
}

public sealed record MetricDef(
    string Name, string Type, string Description,
    string[]? Labels = null, double[]? Buckets = null);
```

### Meter Implementation

```csharp
// KOAFiloServis.Web/Telemetry/PuantajMeter.cs
public sealed class PuantajMeter : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _jobRuns;
    private readonly Counter<long> _tenantsProcessed;
    private readonly Counter<long> _engineInvocations;
    private readonly Counter<long> _mutexOperations;
    private readonly Counter<long> _reconciliationFixes;
    private readonly Histogram<double> _jobDuration;
    private readonly Histogram<double> _engineDuration;
    private readonly Histogram<double> _mutexLatency;
    private readonly UpDownCounter<long> _jobsInFlight;
    private readonly ObservableGauge<double> _hoursSinceLastSuccess;

    public PuantajMeter()
    {
        _meter = new Meter(PuantajMetricDefinitions.MeterName, "1.0.0");

        _jobRuns = _meter.CreateCounter<long>("puantaj_job_runs_total",
            description: "Total job runs");
        _tenantsProcessed = _meter.CreateCounter<long>("puantaj_tenants_processed_total",
            description: "Total tenants processed");
        _engineInvocations = _meter.CreateCounter<long>("puantaj_engine_invocations_total",
            description: "Total engine calls");
        _mutexOperations = _meter.CreateCounter<long>("puantaj_mutex_operations_total",
            description: "Total mutex operations");
        _reconciliationFixes = _meter.CreateCounter<long>("puantaj_reconciliation_fixes_total",
            description: "Total reconciliation fixes");
        _jobDuration = _meter.CreateHistogram<double>("puantaj_job_duration_seconds",
            unit: "s", description: "Job duration");
        _engineDuration = _meter.CreateHistogram<double>("puantaj_engine_duration_seconds",
            unit: "s", description: "Engine duration");
        _mutexLatency = _meter.CreateHistogram<double>("puantaj_mutex_acquire_latency_ms",
            unit: "ms", description: "Mutex latency");
        _jobsInFlight = _meter.CreateUpDownCounter<long>("puantaj_jobs_in_flight",
            description: "Jobs in flight");
    }

    public void RecordJobRun(string trigger, double durationSeconds) { ... }
    public void RecordTenant(int firmaId, string status, double engineDurationSeconds) { ... }
    public void RecordEngineInvocation(int firmaId, int retryAttempt, string outcome) { ... }
    public void RecordMutexOp(int firmaId, string operation, string result, double latencyMs) { ... }
    public void RecordReconciliationFix(string fixType, int count = 1) { ... }
    public void JobStarted() => _jobsInFlight.Add(1);
    public void JobCompleted() => _jobsInFlight.Add(-1);

    public void Dispose() => _meter.Dispose();
}
```

---

## 5. Grafana Dashboard Plan

### Dashboard: `KOAFiloServis Puantaj Jobs`

**Row 1: Overview (stat panels)**

| Panel | Metric | Visual |
|-------|--------|--------|
| Jobs Today | `sum(increase(puantaj_job_runs_total[24h]))` | Stat (spark line) |
| Tenants Processed | `sum(increase(puantaj_tenants_processed_total[24h]))` | Stat |
| Success Rate | `sum(rate(puantaj_tenants_processed_total{status="completed"}[24h])) / sum(rate(puantaj_tenants_processed_total[24h]))` | Gauge (%) |
| Last Job Status | `puantaj_hours_since_last_success` | Stat (colored) |

**Row 2: Job duration over time**

```promql
histogram_quantile(0.50, rate(puantaj_job_duration_seconds_bucket[30d])) as p50
histogram_quantile(0.95, rate(puantaj_job_duration_seconds_bucket[30d])) as p95
histogram_quantile(0.99, rate(puantaj_job_duration_seconds_bucket[30d])) as p99
```
→ Time series panel, 30-day window.

**Row 3: Tenant processing heatmap**

| Panel | Query |
|-------|-------|
| Per-tenant success/fail | `sum by (firma_id, status) (increase(puantaj_tenants_processed_total[$__range]))` |
| Per-tenant engine duration (p95) | `histogram_quantile(0.95, sum by (firma_id, le) (rate(puantaj_engine_duration_seconds_bucket[$__range])))` |

**Row 4: Mutex & Retry health**

| Panel | Query |
|-------|-------|
| Mutex collision rate | `sum(rate(puantaj_mutex_operations_total{result="collision"}[1h]))` |
| Retry attempts over time | `sum by (retry_attempt) (rate(puantaj_engine_invocations_total{outcome="error"}[1h]))` |
| Stale mutex gauge | `puantaj_stale_mutex_count` |

**Row 5: EF Core query performance (from OTel spans)**

| Panel | TraceQL (Tempo) |
|-------|-----------------|
| Slow queries (>1s) | `{span.db.system="postgresql" && span.puantaj.job != ""} \| span.duration > 1s` |
| N+1 query detection | `{span.db.system="postgresql"} \| rate() by (span.name)` |

**Row 6: Logs (Loki)**

| Panel | LogQL |
|-------|-------|
| Error logs (last 1h) | `{job="koa-filoservis"} \|= "Error" \| json \| line_format "{{.Correlation_TenantId}} {{.Correlation_JobRunId}} {{.@m}}"` |
| Retry warnings | `{job="koa-filoservis"} \|= "retry" \| json` |

---

## 6. Alert Rules

```yaml
# prometheus-rules.yml
groups:
  - name: puantaj_job_alerts
    rules:
      # ── CRITICAL: Job hasn't run ──────────────────────────
      - alert: PuantajJobNotRun
        expr: puantaj_hours_since_last_success > 36
        for: 10m
        labels:
          severity: critical
          team: backend
        annotations:
          summary: "Puantaj job hasn't completed successfully in 36 hours"
          description: "Last successful run was {{ $value }}h ago. Check Quartz scheduler and DB connectivity."
          runbook_url: "https://wiki.internal/runbooks/puantaj-job-not-run"

      # ── CRITICAL: All tenants failing ──────────────────────
      - alert: PuantajAllTenantsFailed
        expr: |
          sum(rate(puantaj_tenants_processed_total{status="completed"}[1h])) == 0
          and
          sum(rate(puantaj_tenants_processed_total{status="failed"}[1h])) > 0
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "All tenants failing in puantaj job"
          description: "Success=0, Failed={{ $value }}. Check engine logs."

      # ── WARNING: High retry rate ───────────────────────────
      - alert: PuantajHighRetryRate
        expr: |
          sum(rate(puantaj_engine_invocations_total{outcome="error"}[15m]))
          /
          sum(rate(puantaj_engine_invocations_total[15m])) > 0.3
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "High engine retry rate (>30%)"
          description: "Retry rate: {{ $value | humanizePercentage }}. Check DB connection health."

      # ── WARNING: Stale mutex accumulation ──────────────────
      - alert: PuantajStaleMutexAccumulating
        expr: puantaj_stale_mutex_count > 5
        for: 15m
        labels:
          severity: warning
        annotations:
          summary: "{{ $value }} stale mutex records detected"
          description: "Stale mutex records indicate crashes or reconciliation lag."

      # ── INFO: Job duration spike ───────────────────────────
      - alert: PuantajJobDurationSpike
        expr: |
          histogram_quantile(0.95,
            rate(puantaj_job_duration_seconds_bucket[1h])) > 600
        for: 15m
        labels:
          severity: info
        annotations:
          summary: "Puantaj job p95 duration > 10 minutes"
          description: "p95: {{ $value }}s. May indicate large dataset or slow DB."

      # ── CRITICAL: Reconciliation not running ───────────────
      - alert: PuantajReconciliationNotRun
        expr: |
          sum(increase(puantaj_reconciliation_fixes_total[25h])) == 0
        for: 1h
        labels:
          severity: warning
        annotations:
          summary: "Reconciliation job hasn't run in 25 hours"
```

---

## 7. SLO / SLA Definitions

| Service Level | Target | Measurement Window | Metric |
|---------------|:------:|:------------------:|--------|
| **SLO: Job completion** | 99.5% | 30 days | `puantaj_job_runs_total{status="completed"} / puantaj_job_runs_total` |
| **SLO: Per-tenant success** | 99.9% | 30 days | `puantaj_tenants_processed_total{status="completed"} / puantaj_tenants_processed_total` |
| **SLO: Job timeliness** | p95 < 300s | 30 days | `puantaj_job_duration_seconds` |
| **SLO: Mutex latency** | p99 < 100ms | 7 days | `puantaj_mutex_acquire_latency_ms` |
| **SLI: Engine p95** | < 30s | 30 days | `puantaj_engine_duration_seconds` |
| **SLI: Retry rate** | < 5% | 7 days | `puantaj_engine_invocations_total{outcome="error"} / total` |
| **SLI: Stale mutex** | < 3 | instantaneous | `puantaj_stale_mutex_count` |

### SLO Dashboard Panel (PromQL)

```promql
# Job completion SLO (99.5% over 30 days)
(
  sum(increase(puantaj_job_runs_total{status="completed"}[30d]))
  /
  sum(increase(puantaj_job_runs_total[30d]))
) * 100
```

---

## 8. Error Budget Strategy

### Monthly Error Budget Calculation

```
Monthly job runs: ~1 (aylık) × 1 trigger = 1 run/month (Quartz)
                + ~5 manual triggers/month
                = ~6 runs/month

SLO: 99.5% completion → 0.5% error budget
Error budget: 6 × 0.005 = 0.03 runs/month → effectively 0 failures allowed per month

Per-tenant (more granular):
Tenants: 10 firms × 12 months = 120 tenant-processes/year = ~10/month
SLO: 99.9% → 0.1% error budget
Error budget: 10 × 0.001 = 0.01 failures/month → 0 failures/quarter
```

### Error Budget Policy

| Budget Remaining | Action |
|:-----------------|--------|
| > 50% | Normal operations. Feature deploys allowed. |
| 50% – 20% | Freeze non-critical deploys. Start incident review. |
| < 20% | **Code freeze.** Only hotfixes allowed. All hands on reliability. |
| 0% (exhausted) | **Emergency mode.** Rollback last deploy. RCA required before next deploy. |

### Error Budget Burn Rate Alert

```yaml
- alert: PuantajErrorBudgetBurnRateHigh
  expr: |
    (
      sum(increase(puantaj_tenants_processed_total{status="failed"}[1h]))
      /
      sum(increase(puantaj_tenants_processed_total[1h]))
    ) > (0.001 * 14.4)  # 14.4x burn rate → exhausts 30-day budget in 2 days
  for: 1h
  labels:
    severity: critical
  annotations:
    summary: "Error budget burn rate critical (14.4x)"
```

---

## 9. Tenant-Level Metrics

### Per-Tenant Dashboard Row

```promql
# Tenant health score
(
  sum by (firma_id) (rate(puantaj_tenants_processed_total{status="completed"}[90d]))
  /
  sum by (firma_id) (rate(puantaj_tenants_processed_total[90d]))
) * 100

# Tenant processing volume
sum by (firma_id) (puantaj_engine_invocations_total)

# Tenant average engine duration
histogram_quantile(0.50, sum by (firma_id, le) (
  rate(puantaj_engine_duration_seconds_bucket[30d])
))
```

### Tenant-Scoped Log Filtering (Loki)

```logql
{job="koa-filoservis"}
  | json
  | Correlation_TenantId = 5
  | line_format "{{.@t}} [{{.@l}}] {{.@m}}"
```

---

## 10. Quartz Job Telemetry

### Quartz-Specific Metrics

```csharp
// Registered via OpenTelemetry.Instrumentation.Quartz
// Automatic metrics from Quartz instrumentation:
// - quartz.job.execution.count
// - quartz.job.execution.duration
// - quartz.trigger.fired.count
// - quartz.scheduler.listener.count

// Custom enrichment:
public class PuantajQuartzJobListener : JobListenerSupport
{
    private readonly PuantajMeter _meter;
    private readonly ILogger<PuantajQuartzJobListener> _logger;

    public override string Name => "Puantaj Job Listener";

    public override async Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken ct)
    {
        var elapsed = context.JobRunTime;
        var jobName = context.JobDetail.Key.Name;

        _logger.LogInformation(
            "Quartz job {JobName} completed in {Elapsed}ms. " +
            "FireTime={FireTime}, ScheduledTime={Scheduled}, " +
            "PreviousFireTime={Previous}, NextFireTime={Next}",
            jobName, elapsed.TotalMilliseconds,
            context.FireTimeUtc,
            context.ScheduledFireTimeUtc,
            context.PreviousFireTimeUtc,
            context.NextFireTimeUtc);

        if (jobException != null)
        {
            _logger.LogError(jobException,
                "Quartz job {JobName} failed: {Error}",
                jobName, jobException.Message);
        }
    }
}
```

### Quartz Misfire Dashboard

```promql
# Misfire rate
sum(rate(quartz_trigger_misfired_total{trigger_group="puantaj"}[24h]))

# Job execution timeline
quartz_job_execution_duration_seconds{job_name="puantaj-engine-job"}
```

---

## 11. EF Core Query Tracing

### Npgsql + OTel Integration

```csharp
// Program.cs — OTel setup with Npgsql instrumentation
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddNpgsql()  // <-- Npgsql command traces
        .AddEntityFrameworkCoreInstrumentation(opt =>
        {
            opt.SetDbStatementForText = true;   // SQL text in traces
            opt.SetDbStatementForStoredProcedure = true;
            opt.EnrichWithIDbCommand = (activity, command) =>
            {
                // Tag slow queries for Tempo search
                var cmdText = command.CommandText;
                activity.SetTag("db.query.truncated",
                    cmdText.Length > 500 ? cmdText[..500] : cmdText);
                activity.SetTag("db.query.tables",
                    ExtractTableNames(cmdText)); // custom: regex parse FROM/JOIN
            };
        }));
```

### Slow Query Detection (Tempo)

```tempo-ql
{span.db.system="postgresql" && resource.service.name="KOAFiloServis"}
| span.duration > 1000ms
| select(span.db.query.truncated, span.duration)
```

### EF Core Query Statistics Dashboard

```promql
# Top 10 slowest query patterns
topk(10,
  histogram_quantile(0.99,
    sum by (db_operation, span_name) (
      rate(db_client_operation_duration_seconds_bucket[1h])
    )
  )
)
```

---

## 12. Retry Visibility

### Polly + OTel Retry Tracing

```csharp
// PuantajRetryPolicy.cs — enriched with OTel events
public async Task<T> ExecuteAsync<T>(
    Func<CancellationToken, Task<T>> action,
    string context, CancellationToken ct)
{
    var attempt = 0;
    return await _pipeline.ExecuteAsync(async innerCt =>
    {
        var currentAttempt = Interlocked.Increment(ref attempt);

        if (currentAttempt > 1)
        {
            // Add retry event to current span
            var activity = Activity.Current;
            activity?.AddEvent(new ActivityEvent("polly.retry", tags: new ActivityTagsCollection
            {
                ["retry.attempt"] = currentAttempt,
                ["retry.context"] = context
            }));
        }

        using var retryActivity = PuantajActivitySource.Source
            .StartActivity($"retry.{context}");

        retryActivity?.SetTag("retry.attempt", currentAttempt);
        retryActivity?.SetTag("retry.max", _options.RetryCount);
        retryActivity?.SetTag("retry.context", context);

        return await action(innerCt);
    }, ct);
}
```

### Retry Dashboard Panel

```promql
# Retry attempts by attempt number
sum by (retry_attempt) (
  rate(puantaj_engine_invocations_total{outcome="error"}[15m])
)

# Retry waterfall (Grafana state timeline)
puantaj_engine_invocations_total{outcome="error"} > 0
```

---

## 13. Dead Letter Strategy

**Tanım:** Tenant 3 kere retry sonrası hala başarısızsa → "dead letter" durumu.

### Dead Letter Queue (DB Table)

`PuantajJobExecutions` zaten bu amaçla kullanılıyor. Ama eksik: **escalation state.**

```csharp
// PuantajJobExecution entity'sine eklenecek alanlar
public class PuantajJobExecution : BaseEntity, IFirmaTenant
{
    // ... existing fields ...

    /// <summary>Dead letter escalation count (kaç kere retry'den sonra failed)</summary>
    public int DeadLetterCount { get; set; }

    /// <summary>Son retry zamanı</summary>
    public DateTime? LastRetryAt { get; set; }

    /// <summary>Escalation seviyesi: 0=yok, 1=ilgiliye bildir, 2=yöneticiye bildir</summary>
    public int EscalationLevel { get; set; }

    /// <summary>Manuel müdahale notu</summary>
    [StringLength(500)]
    public string? ResolutionNote { get; set; }
}
```

### Dead Letter Processing Job

```csharp
[DisallowConcurrentExecution]
public class PuantajDeadLetterJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // 1. Find dead letters (Failed with DeadLetterCount < 3)
        // 2. Re-attempt ProcessTenantAsync
        // 3. Increment DeadLetterCount
        // 4. If DeadLetterCount >= 3 → EscalationLevel++
        // 5. EscalationLevel 1 → email ilgili operatöre
        // 6. EscalationLevel 2 → Slack kanalına mesaj
    }
}
```

### Dead Letter Dashboard

```promql
# Active dead letters (needs attention)
count(puantaj_tenants_processed_total{status="failed"} > 0)

# Dead letter aging (hours since last retry)
time() - puantaj_dead_letter_last_retry_timestamp_seconds
```

---

## 14. Sample OpenTelemetry Setup Code

### Complete OTel Registration

```csharp
// Program.cs — full OTel setup
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var otelResource = ResourceBuilder.CreateDefault()
    .AddService("KOAFiloServis",
        serviceVersion: "1.0.0",
        serviceInstanceId: Environment.MachineName)
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = builder.Environment.EnvironmentName,
        ["host.name"] = Environment.MachineName
    });

// ── Metrics ────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(otelResource)
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddQuartzInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(PuantajMetricDefinitions.MeterName)
        .AddPrometheusExporter(opt =>
        {
            opt.ScrapeEndpointPath = "/metrics"; // default
            opt.ScrapeResponseCacheDurationMilliseconds = 30000;
        }));

// ── Tracing ────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(otelResource)
        .AddAspNetCoreInstrumentation(opt =>
        {
            opt.Filter = ctx =>
                !ctx.Request.Path.StartsWithSegments("/health") &&
                !ctx.Request.Path.StartsWithSegments("/metrics") &&
                !ctx.Request.Path.StartsWithSegments("/_blazor/negotiate");
            opt.EnrichWithHttpRequest = (activity, request) =>
                activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
        })
        .AddEntityFrameworkCoreInstrumentation()
        .AddNpgsql()
        .AddQuartzInstrumentation()
        .AddSource(PuantajActivitySource.Name)
        .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(
            builder.Environment.IsProduction() ? 0.10 : 1.0))) // 10% prod
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri(
                builder.Configuration["Otlp:Endpoint"] ?? "http://localhost:4317");
            opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        }));

// ── Prometheus endpoint ────────────────────────────────────
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// ── EF Core Diagnostic Listener ────────────────────────────
var efListener = app.Services.GetRequiredService<DiagnosticListener>();
efListener.Subscribe(new EfCoreDiagnosticObserver());
```

### EF Core Diagnostic Observer

```csharp
public sealed class EfCoreDiagnosticObserver : IObserver<KeyValuePair<string, object?>>
{
    private readonly ILogger<EfCoreDiagnosticObserver> _logger;

    public void OnNext(KeyValuePair<string, object?> evt)
    {
        if (evt.Key == "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting")
        {
            var command = evt.Value?.GetType()
                .GetProperty("Command")?.GetValue(evt.Value) as IDbCommand;

            if (command?.CommandTimeout > 10)
            {
                _logger.LogWarning(
                    "Slow query detected: {CommandText} (timeout={Timeout}s)",
                    command.CommandText[..Math.Min(command.CommandText.Length, 300)],
                    command.CommandTimeout);
            }
        }
    }

    public void OnCompleted() { }
    public void OnError(Exception error) { }
}
```

---

## 15. Sample Grafana Panels

### Panel: `Puantaj Job Health Overview`

```json
{
  "dashboard": {
    "title": "KOAFiloServis - Puantaj Jobs",
    "uid": "koa-puantaj-jobs",
    "panels": [
      {
        "id": 1,
        "title": "Job Runs (24h)",
        "type": "stat",
        "targets": [
          {
            "expr": "sum(increase(puantaj_job_runs_total[24h]))",
            "legendFormat": "Runs"
          }
        ],
        "fieldConfig": {
          "defaults": {
            "color": { "mode": "thresholds" },
            "thresholds": {
              "steps": [
                { "value": -1, "color": "red" },
                { "value": 0, "color": "green" }
              ]
            }
          }
        }
      },
      {
        "id": 2,
        "title": "Tenant Success Rate (30d)",
        "type": "gauge",
        "targets": [
          {
            "expr": "sum(rate(puantaj_tenants_processed_total{status=\"completed\"}[30d])) / sum(rate(puantaj_tenants_processed_total[30d])) * 100"
          }
        ],
        "fieldConfig": {
          "defaults": {
            "min": 95, "max": 100,
            "thresholds": {
              "steps": [
                { "value": -1, "color": "red" },
                { "value": 99.5, "color": "yellow" },
                { "value": 99.9, "color": "green" }
              ]
            }
          }
        }
      },
      {
        "id": 3,
        "title": "Engine Duration (p50, p95, p99) - 30d",
        "type": "timeseries",
        "targets": [
          {
            "expr": "histogram_quantile(0.50, sum(rate(puantaj_engine_duration_seconds_bucket[30d])) by (le))",
            "legendFormat": "p50"
          },
          {
            "expr": "histogram_quantile(0.95, sum(rate(puantaj_engine_duration_seconds_bucket[30d])) by (le))",
            "legendFormat": "p95"
          },
          {
            "expr": "histogram_quantile(0.99, sum(rate(puantaj_engine_duration_seconds_bucket[30d])) by (le))",
            "legendFormat": "p99"
          }
        ],
        "fieldConfig": {
          "defaults": {
            "unit": "s",
            "custom": { "lineInterpolation": "smooth" }
          }
        }
      },
      {
        "id": 4,
        "title": "Mutex Collisions (1h rate)",
        "type": "timeseries",
        "targets": [
          {
            "expr": "sum(rate(puantaj_mutex_operations_total{result=\"collision\"}[1h]))",
            "legendFormat": "Collisions/s"
          }
        ]
      },
      {
        "id": 5,
        "title": "Retry Attempts Distribution",
        "type": "barchart",
        "targets": [
          {
            "expr": "sum by (retry_attempt) (increase(puantaj_engine_invocations_total[24h]))",
            "legendFormat": "Attempt {{retry_attempt}}"
          }
        ]
      },
      {
        "id": 6,
        "title": "Job Duration Timeline (last 12 runs)",
        "type": "state-timeline",
        "targets": [
          {
            "expr": "puantaj_job_duration_seconds",
            "legendFormat": "Duration"
          }
        ]
      },
      {
        "id": 7,
        "title": "Tenant Heatmap (last 12 months)",
        "type": "heatmap",
        "targets": [
          {
            "expr": "sum by (firma_id, status) (increase(puantaj_tenants_processed_total[365d]))",
            "legendFormat": "Firma {{firma_id}} - {{status}}"
          }
        ]
      },
      {
        "id": 8,
        "title": "Reconciliation Fixes",
        "type": "timeseries",
        "targets": [
          {
            "expr": "sum by (fix_type) (rate(puantaj_reconciliation_fixes_total[7d]))",
            "legendFormat": "{{fix_type}}"
          }
        ]
      },
      {
        "id": 9,
        "title": "Error Budget Remaining (30d)",
        "type": "gauge",
        "targets": [
          {
            "expr": "100 - (sum(increase(puantaj_tenants_processed_total{status=\"failed\"}[30d])) / sum(increase(puantaj_tenants_processed_total[30d])) * 100 / 0.001 * 100)"
          }
        ],
        "fieldConfig": {
          "defaults": {
            "min": 0, "max": 100, "unit": "percent",
            "thresholds": {
              "steps": [
                { "value": -1, "color": "red" },
                { "value": 20, "color": "yellow" },
                { "value": 50, "color": "green" }
              ]
            }
          }
        }
      }
    ]
  }
}
```

---

## 16. Production Incident Workflow

### Incident Severity Classification

| Severity | Definition | Response SLA | Notification |
|:--------:|------------|:------------:|--------------|
| **SEV1** | Job hasn't run in 36h OR all tenants failing | 15 min | PagerDuty + Slack @channel |
| **SEV2** | >30% retry rate OR 1+ tenant consistently failing | 30 min | Slack @team-backend |
| **SEV3** | Stale mutex > 5 OR reconciliation not running | 2 hours | Slack #alerts |
| **SEV4** | Minor metric degradation (duration spike) | Next business day | Dashboard annotation |

### Incident Response Runbook: `Puantaj Job Failed`

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 1: TRIAGE (first 5 min)                                │
├─────────────────────────────────────────────────────────────┤
│ ☐ Check Grafana dashboard: "Puantaj Jobs"                   │
│ ☐ Which tenant(s) failed? ``puantaj_tenants_processed_total │
│    {status="failed"}``                                      │
│ ☐ Check Loki for error logs:                                │
│    {job="koa-filoservis"} |= "Error" | json                 │
│    | Correlation_JobRunId = "<from dashboard>"              │
│ ☐ Check Tempo for failed span:                              │
│    {span.puantaj.job.run} && span.status.code = Error       │
├─────────────────────────────────────────────────────────────┤
│ STEP 2: DIAGNOSE (next 10 min)                              │
├─────────────────────────────────────────────────────────────┤
│ ☐ Is it a DB connectivity issue?                            │
│    → Check /readyz endpoint                                 │
│    → Check PG logs: tail -f /var/log/postgresql/*.log       │
│ ☐ Is it a specific tenant DB offline?                       │
│    → Check per-tenant metric: engine failures by firma_id   │
│ ☐ Is it a data issue?                                       │
│    → Check OperasyonKaydi count for failed tenant/month     │
│ ☐ Is it a timeout?                                          │
│    → Check engine_duration_seconds near 300s (config limit) │
│ ☐ Is it a mutex issue?                                      │
│    → Check puantaj_stale_mutex_count                        │
├─────────────────────────────────────────────────────────────┤
│ STEP 3: MITIGATE (next 15 min)                              │
├─────────────────────────────────────────────────────────────┤
│ Option A: Retry single tenant                               │
│   POST /api/puantaj/jobs/process/{firmaId}/{yil}/{ay}       │
│                                                             │
│ Option B: Re-run entire job                                 │
│   POST /api/puantaj/jobs/process/{yil}/{ay}                 │
│                                                             │
│ Option C: Force reset stale mutex (SQL)                     │
│   UPDATE "PuantajJobExecutions"                             │
│   SET "Durum" = 3, "HataMesaji" = 'Manual reset'            │
│   WHERE "Durum" = 0 AND "Baslangic" < now() - '1h'::interval│
│                                                             │
│ Option D: Scale engine timeout config                       │
│   PuantajEngine:AutoProcess:EngineTimeoutSeconds = 600      │
│   → restart app                                             │
├─────────────────────────────────────────────────────────────┤
│ STEP 4: VERIFY (next 10 min)                                │
├─────────────────────────────────────────────────────────────┤
│ ☐ Check /health/puantaj-job → Healthy                      │
│ ☐ Verify PuantajHesapDonemleri has new Aktif record         │
│ ☐ Verify PuantajJobExecutions shows Completed               │
│ ☐ Check Grafana dashboard returns to green                  │
│ ☐ Send incident summary to Slack #incidents                 │
├─────────────────────────────────────────────────────────────┤
│ STEP 5: POSTMORTEM (within 24h)                             │
├─────────────────────────────────────────────────────────────┤
│ ☐ Write postmortem doc (template below)                     │
│ ☐ Create follow-up tickets for root cause fixes             │
│ ☐ Update runbook with new diagnostic steps                  │
│ ☐ Schedule postmortem review meeting                        │
└─────────────────────────────────────────────────────────────┘
```

### Postmortem Template

```markdown
# Incident Postmortem: Puantaj Job Failure

- **Date:** YYYY-MM-DD
- **Duration:** X hours Y minutes
- **Severity:** SEV1 / SEV2 / SEV3
- **Summary:** 1-2 sentences

## Timeline (UTC)
| Time | Event |
|------|-------|
| HH:MM | Alert fired |
| HH:MM | On-call acknowledged |
| HH:MM | Root cause identified |
| HH:MM | Mitigation applied |
| HH:MM | System verified healthy |

## Root Cause
Technical explanation...

## Impact
- Tenants affected: N
- Data inconsistency (if any): ...
- Financial impact (if any): ...

## What Went Well
- Alert fired within expected window
- Runbook was accurate
- ...

## What Went Wrong
- Root cause not obvious from logs
- Postmortem ticket wasn't created promptly
- ...

## Action Items
- [ ] #1234: Add diagnostic log for XYZ scenario
- [ ] #1235: Update health check to catch ABC condition
- [ ] #1236: Automate recovery step for DEF
```

---

## Observability Maturity Model: Current → Target

| Capability | Current | Sprint 9 | Sprint 10 | Target |
|------------|:-------:|:--------:|:---------:|:------:|
| Structured logging | ✅ Compact JSON | ✅ OTel export | - | ✅ |
| Correlation IDs | ✅ BeginScope | ✅ TraceId link | - | ✅ |
| Metrics (Prometheus) | - | ✅ Custom meter | ✅ OTel auto | ✅ |
| Tracing (Tempo) | - | - | ✅ EF Core + Quartz | ✅ |
| Alerting (Prometheus) | - | ✅ 6 rules | - | ✅ |
| Dashboards (Grafana) | - | ✅ 9 panels | - | ✅ |
| SLO/SLA tracking | - | ✅ Definitions | ✅ Error budget | ✅ |
| Incident runbook | - | ✅ | - | ✅ |
| Dead letter handling | - | - | ✅ | ✅ |
| Synthetic monitoring | - | - | - | Backlog |

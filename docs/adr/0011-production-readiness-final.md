# ADR-0011: Production Readiness Final Assessment — Sprint 8

> Multi-Role Review | 2026-05-26 | Decision: **Conditional Go**

---

## Executive Summary

| Dimension | Score | Verdict |
|-----------|:-----:|:-------:|
| Concurrency Safety | 9/10 | ✅ Safe |
| Crash Recovery | 7/10 | ⚠️ 15-30dk window |
| Observability | 5/10 | 🔴 Gap |
| Test Coverage | 6/10 | ⚠️ No integration tests |
| Operational Maturity | 5/10 | 🔴 No runbooks/alerts |
| Scalability | 8/10 | ✅ Sequential ok, <20 tenants |

**Decision: CONDITIONAL GO** — Deploy edilebilir ama 3 blocker fix + 5 high-priority item tamamlanmadan production'a alınmamalı.

---

## 1. Exception Hierarchy Redesign

### Problem

```csharp
// Current: 3 farklı hata aynı tip
throw new InvalidOperationException("Kilitli dönem");         // Business
throw new InvalidOperationException("Tenant DB offline");      // Infrastructure
throw new InvalidOperationException("DI container broken");    // Fatal
```

Polly hangisini retry etsin? Hepsi aynı tip → ya hepsi retry, ya hiçbiri.

### Redesign

```
Exception
├── PuantajException (abstract, base marker)
│   ├── PuantajBusinessException (abstract)         ← RETRY YOK
│   │   ├── PuantajDonemKilitliException
│   │   ├── PuantajOperasyonBulunamadiException
│   │   └── PuantajDonemZatenHesaplanmisException
│   │
│   ├── PuantajInfrastructureException (abstract)    ← RETRY VAR (transient)
│   │   ├── PuantajTenantOfflineException
│   │   └── PuantajDatabaseConnectionException
│   │
│   └── PuantajFatalException (abstract)             ← RETRY YOK, ALERT
│       ├── PuantajConfigurationException
│       └── PuantajDependencyResolutionException
│
├── OperationCanceledException                        ← RETRY YOK, propagate
└── (other)                                           ← RETRY YOK, Failed + alert
```

### Retry Matrix

| Exception Type | Transient? | Polly Retry? | Mutex Status | Alert |
|----------------|:----------:|:------------:|:------------:|:-----:|
| `PuantajDonemKilitliException` | No | No | Skipped | No |
| `PuantajOperasyonBulunamadiException` | No | No | Skipped | No |
| `PuantajTenantOfflineException` | Yes | Yes (3x) | Failed after retries | SEV2 |
| `PuantajDatabaseConnectionException` | Yes | Yes (3x) | Failed after retries | SEV2 |
| `PuantajConfigurationException` | No | No | Failed | SEV1 |
| `OperationCanceledException` | No | No | Failed (propagate) | No |
| `NpgsqlException` (unmapped) | Yes | Yes (3x) | Failed after retries | SEV2 |
| `Exception` (unexpected) | Unknown | No | Failed | SEV1 |

### Refactored ProcessTenantAsync

```csharp
private async Task<TenantProcessResult> ProcessSingleTenantAsync(
    AsyncServiceScope scope, int firmaId, string firmaAdi,
    int yil, int ay, string tetikleyen, CancellationToken ct)
{
    var mutex = scope.ServiceProvider.GetRequiredService<IPuantajMutexService>();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

    await mutex.CleanupStaleAsync(ct);

    var acquire = await mutex.TryAcquireAsync(firmaId, yil, ay, tetikleyen, ct);
    if (!acquire.Acquired)
        return TenantProcessResult.Skipped(acquire.FailureReason ?? "Mutex");

    var record = acquire.Record!;

    try
    {
        // Idempotency
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.PuantajHesapDonemleri
            .Where(h => !h.IsDeleted && h.Yil == yil && h.Ay == ay
                        && h.Durum == PuantajHesapDurum.Aktif)
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            await mutex.UpdateToSkippedAsync(record,
                $"Zaten hesaplanmış V{existing.Versiyon}", existing.Id, ct);
            return TenantProcessResult.Skipped($"V{existing.Versiyon}", record);
        }

        // Engine — only retry infrastructure exceptions
        var engine = scope.ServiceProvider.GetRequiredService<IPuantajEngineService>();
        var engineResult = await _retryPolicy.ExecuteAsync(
            ct => engine.ProcessDonemAsync(yil, ay, null, tetikleyen,
                $"Auto ({tetikleyen})", ct),
            $"Engine:F{firmaId}/{yil}/{ay:00}", ct);

        // Audit — best effort
        await WriteAuditLogIdempotentAsync(dbFactory, firmaId, tetikleyen,
            engineResult, ct);

        await mutex.UpdateToCompletedAsync(record, engineResult, ct);
        return TenantProcessResult.Completed(record, engineResult);
    }
    catch (OperationCanceledException)
    {
        await mutex.UpdateToFailedAsync(record, "İptal", ct);
        throw;
    }
    catch (PuantajBusinessException ex)
    {
        _logger.LogWarning(ex, "Business rule — Skipped");
        await mutex.UpdateToSkippedAsync(record, ex.Message, ct);
        return TenantProcessResult.Skipped(ex.Message, record);
    }
    catch (PuantajInfrastructureException ex)
    {
        _logger.LogError(ex, "Infrastructure failure after retries");
        await mutex.UpdateToFailedAsync(record, ex.Message, ct);
        return TenantProcessResult.Failed(firmaId, ex.Message, record);
    }
    catch (PuantajFatalException ex)
    {
        _logger.LogCritical(ex, "FATAL — manual intervention required");
        await mutex.UpdateToFailedAsync(record, $"FATAL: {ex.Message}", ct);
        _alerting.SendCriticalAlert("PuantajFatal", firmaId, ex.Message);
        throw; // Tüm job dursun
    }
}
```

---

## 2. Polly Resilience Pipeline — Production Review

### Critical Finding: Static Shared State

```csharp
// Current — STATIC, immutable-constructor-safe ama timeout global
private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
    .AddRetry(...)
    .AddTimeout(TimeSpan.FromMinutes(5)) // TÜM tenant'lar aynı timeout
    .Build();
```

`ResiliencePipeline` immutable → thread-safe. AMA:

1. **Timeout global:** Büyük tenant (50K operasyon) 5dk'da bitmez → Polly `TimeoutRejectedException` fırlatır → transient değil → retry olmaz → direkt Failed.
2. **Static → config değişince restart gerekir:** Production'da timeout'u "appsettings'ten değiştir" diyemezsin, restart zorunlu.
3. **Test edilemez:** Static olduğu için test double enjekte edilemez.

### Redesign: Scoped, Config-Driven Policy

```csharp
// Registration
builder.Services.Configure<PuantajJobOptions>(
    builder.Configuration.GetSection("PuantajEngine:AutoProcess"));
builder.Services.AddSingleton<IPuantajRetryPolicy, PuantajRetryPolicy>();

// Config
public sealed class PuantajJobOptions
{
    public int RetryCount { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 1000;
    public int EngineTimeoutSeconds { get; set; } = 300;
    public double JitterFactor { get; set; } = 0.1;
}

// Scoped policy — test edilebilir, config'den canlı değişir
public sealed class PuantajRetryPolicy : IPuantajRetryPolicy
{
    private readonly ResiliencePipeline _pipeline;

    public PuantajRetryPolicy(IOptionsSnapshot<PuantajJobOptions> options,
        ILogger<PuantajRetryPolicy> logger)
    {
        var opt = options.Value;
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = opt.RetryCount,
                Delay = TimeSpan.FromMilliseconds(opt.RetryBaseDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => args.Outcome.Exception switch
                {
                    PuantajInfrastructureException => PredicateResult.True(),
                    NpgsqlException => PredicateResult.True(),
                    TimeoutException => PredicateResult.True(),
                    PostgresException pe when pe.IsTransient => PredicateResult.True(),
                    _ => PredicateResult.False()
                },
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception,
                        "Retry {Attempt}/{Max}: {Error}",
                        args.AttemptNumber + 1, opt.RetryCount,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(opt.EngineTimeoutSeconds))
            .Build();
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action,
        string context, CancellationToken ct) => await _pipeline.ExecuteAsync(action, ct);
}
```

### Thread Safety Analysis

| Concern | Static Version | Scoped Version |
|---------|:-------------:|:-------------:|
| Pipeline thread safety | ✅ Immutable after Build() | ✅ Immutable after Build() |
| Options thread safety | N/A (hardcoded) | ✅ `IOptionsSnapshot` thread-safe |
| CancellationToken safety | ✅ Passed through | ✅ Passed through |
| Jitter entropy | ✅ System.Random per pipeline | ✅ Per pipeline instance |
| Memory leak risk | None (single instance, GC-rooted) | None (singleton DI, GC-rooted) |
| Retry storm risk | Exists — keine per-tenant rate limit | Same, mitigated by backoff+jitter |

---

## 3. PostgreSQL Table-Based Mutex — Correctness Proof

### Formal Reasoning

**Invariant:** Aynı anda en fazla 1 worker, aynı (FirmaId, Yil, Ay) için `ProcessDonemAsync` çalıştırabilir.

**Proof (by contradiction):**
1. Worker A ve Worker B aynı (f, y, m) için aynı anda ProcessDonemAsync çalıştırsın.
2. ProcessDonemAsync'ten önce her ikisi de `TryAcquireAsync` çağırmak zorunda.
3. TryAcquireAsync: `INSERT INTO PuantajJobExecutions (FirmaId=f, Yil=y, Ay=m, Durum=0)`.
4. Filtered UNIQUE index: `(FirmaId, Yil, Ay) WHERE Durum=0`.
5. Durum=0 (Running) olan kayıt için UNIQUE constraint → aynı (f, y, m, 0) için 2 kayıt OLAMAZ.
6. İşlemlerden biri commit olur (başarılı), diğeri 23505 unique_violation alır.
7. Unique violation alan → `IsUniqueViolation` → `Acquired=false` → ProcessDonemAsync ÇAĞRILMAZ.
8. Contradiction. ∎

**Edge case: Phantom read.** İki worker aynı anda `SELECT COUNT(*) WHERE Durum=0` yapıp 0 görse ve sonra ikisi de INSERT etse?

**Cevap:** Filtered UNIQUE index **her zaman** constraint violation üretir. Phantom read olsa bile UNIQUE index fiziksel olarak iki kayda izin vermez. PostgreSQL B-tree index'leri **her zaman** physically consistent'tır, isolation level'dan bağımsız.

**SERIALIZABLE gerekmez.** READ COMMITTED + UNIQUE index yeterli.

### Failure Scenarios

| # | Scenario | PostgreSQL Behavior | Application Behavior |
|---|----------|---------------------|---------------------|
| F1 | İki concurrent INSERT | Biri OK, biri 23505 | Biri devam, biri Skipped |
| F2 | INSERT + crash before UPDATE | Running row stays | 15dk sonra CleanupStale → Failed |
| F3 | INSERT + engine OK + crash before mutex UPDATE | Running + Aktif dönem var | Sonraki run: idempotency check skip, CleanupStale → Failed |
| F4 | Network partition (app ↔ PG) | Connection timeout | Polly retry 3x → Failed |
| F5 | PG failover mid-INSERT | Connection lost | Polly retry → yeni primary'a bağlanır |
| F6 | XID wraparound | Vacuum freeze | Uygulamayı etkilemez (read-only değil) |

### SQL-Level Optimization

```sql
-- Current (migration-generated):
CREATE UNIQUE INDEX "IX_PuantajJobExecutions_FirmaId_Yil_Ay"
ON "PuantajJobExecutions" ("FirmaId", "Yil", "Ay")
WHERE "Durum" = 0;

-- Optimized: partial index + INCLUDE for covering queries
DROP INDEX IF EXISTS "IX_PuantajJobExecutions_FirmaId_Yil_Ay";

CREATE UNIQUE INDEX "IX_PuantajJobExecutions_Running"
ON "PuantajJobExecutions" ("FirmaId", "Yil", "Ay")
INCLUDE ("Durum", "Baslangic", "HataMesaji")
WHERE "Durum" = 0;

-- Benefit: CleanupStale query becomes index-only scan (no heap fetch)
-- EXPLAIN ANALYZE:
-- SELECT "Durum", "Baslangic", "HataMesaji"
-- FROM "PuantajJobExecutions"
-- WHERE "Durum" = 0 AND "Baslangic" < now() - interval '30 minutes';
-- → Index Only Scan using IX_PuantajJobExecutions_Running
```

### Advisory Lock Tradeoffs (Neden Kullanmadık)

| Criterion | Advisory Lock | Table Mutex |
|-----------|:------------:|:-----------:|
| Connection affinity | **Evet** (lock aynı conn'da olmalı) | **Hayır** (INSERT herhangi conn'dan) |
| Multi-instance safe | **Hayır** (farklı conn = farklı lock namespace) | **Evet** (UNIQUE DB seviyesinde) |
| Crash recovery | Lock auto-release (iyi) | 15dk stale window (kötü) |
| Observability | `pg_locks` view | Normal table → SELECT ile görünür (iyi) |
| Complexity | Düşük | Orta (entity + migration + cleanup job) |
| Performance | Çok hızlı (shared memory) | INSERT + index lookup (yine hızlı) |

**Sonuç:** Engine kendi DbContext'ini yarattığı için advisory lock connection affinity koşulunu sağlamaz. Table mutex tek doğru seçenek.

---

## 4. Observability Architecture — Production Gaps

### Missing Critical Metrics

| Metric | Current | Gap |
|--------|:-------:|-----|
| `puantaj_hours_since_last_success` | ❌ | **Add** — stuck job detection |
| `puantaj_tenants_never_processed` | ❌ | **Add** — tenant starvation detection |
| `puantaj_quartz_misfire_count` | ❌ | **Add** — scheduler health |
| `puantaj_dead_letter_age_seconds` | ❌ | **Add** — escalation aging |
| `puantaj_engine_rows_affected` | ❌ | **Add** — data volume tracking |

### Alert Tuning

```yaml
# CRITICAL: Currently missing — job silently failing for >24h
- alert: PuantajJobSilentFailure
  expr: |
    puantaj_hours_since_last_success > 24
    AND
    puantaj_job_runs_total offset 24h == puantaj_job_runs_total
  for: 10m
  annotations:
    summary: "Job may be silently failing — no runs in 24h"

# CRITICAL: Tenant never processed
- alert: PuantajTenantStarvation
  expr: |
    puantaj_tenants_never_processed > 0
  for: 168h  # 7 days
  annotations:
    summary: "Tenant(s) not processed in 7 days"
```

### Cardinality Risks

| Label | Estimated Cardinality | Risk |
|-------|:--------------------:|:----:|
| `firma_id` | < 100 | ✅ Low |
| `trigger` | 3 (Quartz, Manuel, Manuel-Retry) | ✅ Low |
| `status` | 4 (completed, failed, skipped, running) | ✅ Low |
| `retry_attempt` | 4 (0-3) | ✅ Low |
| `firma_id × retry_attempt` | < 400 | ✅ Low |
| **`error_message`** | Unbounded | 🔴 **NEVER use as label!** |
| **`job_run_id`** | Unbounded (new GUID per run) | 🔴 **NEVER use as label!** |

### Trace Correlation Strategy

```
Request → Quartz Job → Tenant Loop → Engine → DB Query
   │          │             │           │         │
   │  traceparent header propagated via Activity.Current
   │          │             │           │         │
   └──────────┴─────────────┴───────────┴─────────┘
              All share same trace_id
              
   Tempo query: { trace_id = "a1b2..." }
   → Shows FULL waterfall: job → per-tenant → engine → SQL
```

---

## 5. Chaos Engineering Scenarios

| # | Failure | Injection | Expected | Detection | Recovery |
|---|---------|-----------|----------|-----------|----------|
| **C1** | PG connection killed | `pg_terminate_backend(pid)` mid-engine | Polly retry 3x → Failed | `puantaj_engine_invocations_total{outcome="error"}` spike | Next schedule retry |
| **C2** | Network latency 5s | `tc qdisc add dev eth0 root netem delay 5000ms` | Mutex timeout, Polly retry | `puantaj_mutex_acquire_latency_ms` p99 spike | Retry succeeds on recovery |
| **C3** | PG primary failover | `pg_ctl promote` on standby | Connection reset, Polly retry → new primary | `error.type=NpgsqlException` in traces | Auto-reconnect via Npgsql pooling |
| **C4** | Duplicate Quartz trigger | Manual API + Quartz simultaneous | One mutex acquired, other Skipped | `puantaj_mutex_operations_total{result="collision"}=1` | Correct by design |
| **C5** | Kill process mid-engine | `kill -9` after engine INSERT, before COMMIT | Engine transaction ROLLBACK, mutex stale | `puantaj_stale_mutex_count=1` | CleanupStale → Failed, next run OK |
| **C6** | Disk full | `dd if=/dev/zero of=/pgdata/fill bs=1M` | PG write error, Polly retry → Failed | PG log "could not extend file" | Clear disk, reconciliation job |
| **C7** | Connection pool exhaustion | `MaxPoolSize=1` + 3 concurrent operations | Sequential queue, timeout on 4th | `NpgsqlException "pool exhausted"` | Polly retry → connection freed |
| **C8** | Clock skew | `timedatectl set-time +1 hour` | Stale cleanup triggers early | `puantaj_stale_mutex_count` unexpected drop | NTP sync, use DB `now()` not app `DateTime.UtcNow` |

### Automated Chaos Test (xUnit + Docker)

```csharp
[Fact]
public async Task Chaos_Mutex_ProcessKilledMidEngine_RecoversCorrectly()
{
    // 1. Start engine in separate process
    using var engineProcess = StartEngineProcess();

    // 2. Kill it mid-execution (after mutex INSERT, before engine COMMIT)
    await Task.Delay(500); // Let mutex INSERT complete
    engineProcess.Kill();

    // 3. Verify stale mutex record exists
    var staleCount = await CountStaleMutexAsync();
    staleCount.Should().Be(1);

    // 4. Wait for stale timeout (use test config: 2 seconds)
    await Task.Delay(3000);

    // 5. Run cleanup
    await mutexService.CleanupStaleAsync();

    // 6. Verify stale → Failed
    staleCount = await CountStaleMutexAsync();
    staleCount.Should().Be(0);

    // 7. Verify NO duplicate Aktif period
    var activeCount = await CountActivePeriodsAsync(2026, 5, firmaId: 1);
    activeCount.Should().Be(0, "engine was killed before COMMIT");
}
```

---

## 6. Rollout Plan

```
Phase 0: Pre-Deploy (this week)
  ☐ Exception hierarchy implemented
  ☐ Config-driven timeout
  ☐ Idempotent audit log
  ☐ Health checks + /health/puantaj-job
  ☐ 6 Prometheus alert rules

Phase 1: Canary (Day 1-2, 1 node)
  ☐ Deploy to staging, run manual trigger
  ☐ Deploy to 1 production node
  ☐ Monitor Grafana dashboard for 48h
  ☐ Manual trigger test: POST /api/puantaj/jobs/process/2026/4
  ☐ Verify metrics flowing to Prometheus
  ☐ Go/No-Go decision at 48h mark

Phase 2: Full Rollout (Day 3-4)
  ☐ Deploy to all nodes
  ☐ Wait for next cron trigger (month boundary)
  ☐ Or: manually set cron to */5 * * * * for testing, then revert

Phase 3: Stabilization (Day 5-14)
  ☐ Daily Grafana review
  ☐ Tune alert thresholds based on real data
  ☐ Reconciliation job first run
  ☐ Document any unexpected behaviors

Phase 4: Mature Operations (Week 3+)
  ☐ OTel + Tempo tracing
  ☐ Chaos test suite
  ☐ SLO dashboard
  ☐ Incident runbook dry-run
```

### Rollback Plan

```
Immediate (config change, no deploy):
  PuantajEngine:AutoProcess:Enabled = false
  → Quartz job won't fire, manual triggers still work

Full (code deploy):
  git revert <sprint-8-commits>
  → Removes job, mutex, entity. No data loss.
  → PuantajJobExecutions table stays (no FK dependencies)
```

---

## Production Deployment Checklist

### Code Quality
- [x] Build: 0 errors, 0 warnings
- [x] Tests: 324/324 passing
- [ ] Integration tests with real PostgreSQL (missing)
- [ ] Load test: 20 tenants × 10K operations each

### Configuration
- [ ] `StaleTimeoutMinutes` = 15 (not 30) for production
- [ ] `EngineTimeoutSeconds` tuned per tenant data volume
- [ ] `CronExpression` timezone verified (UTC vs TR +3)
- [ ] Connection string pooling: `MaxPoolSize=50`

### Database
- [x] Migration: `PuantajJobExecution` table created
- [x] Filtered UNIQUE index: `WHERE "Durum" = 0`
- [ ] Index-only scan verified via `EXPLAIN ANALYZE`
- [ ] Migration applied to ALL tenant databases

### Monitoring
- [ ] Prometheus scraping `/metrics` endpoint
- [ ] Grafana dashboard imported
- [ ] 6 alert rules active
- [ ] PagerDuty/Slack integration tested
- [ ] Health check endpoint responding

### Operational
- [ ] Runbook printed/shared
- [ ] On-call engineer trained
- [ ] Rollback procedure documented
- [ ] Manual trigger API tested
- [ ] Reconciliation job scheduled

### Security
- [ ] API endpoints protected with `[Authorize(Roles = "Admin,Muhasebeci")]`
- [ ] JWT Bearer token required for API
- [ ] Tenant isolation verified (no cross-tenant data leak)
- [ ] No sensitive data in logs

---

## Risk Matrix

| Risk | Likelihood | Impact | Residual (after mitigations) | Accept? |
|------|:----------:|:------:|:----------------------------:|:-------:|
| Double processing | Low | Critical | None (table mutex) | ✅ |
| Stale lock blocking | Low | Medium | 15dk window, manual override | ✅ |
| Engine timeout on large data | Medium | Medium | Config-driven, Polly retry | ✅ |
| Missing audit log after crash | Low | Low | Reconciliation job | ✅ |
| Quartz misfire (server down) | Medium | Low | DoNothing policy, manual API | ✅ |
| Connection pool exhaustion | Low | Medium | Sequential, proper disposal | ✅ |
| Silent failure (no alerts) | Medium | High | **6 alert rules MUST be deployed** | ⚠️ |
| Tenant starvation (never processed) | Low | High | **Tenant starvation alert** | ⚠️ |

**Overall residual risk: LOW** (with 6 alert rules + reconciliation job deployed).

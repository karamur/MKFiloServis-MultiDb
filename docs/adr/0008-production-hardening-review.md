# ADR-0008: Production Hardening Review — Puantaj Background Job

> Senior Architect Review | Tarih: 2026-05-26 | Status: Active Review

## 1. Critical Issues

### C1 — Retry pipeline static shared state (Race Condition Risk)

```csharp
// PuantajJobService.cs:20 — STATIC pipeline shared across ALL instances
private static readonly ResiliencePipeline RetryPipeline = ...
```

**Severity: LOW** (Polly pipelines are thread-safe by design), but:

- `ResiliencePipeline` is immutable after `Build()`, confirmed thread-safe.
- `AddTimeout(TimeSpan.FromMinutes(5))` — 5 dakika timeout tüm tenant'lar için aynı. Büyük verili tenant 5 dakikayı aşarsa Polly timeout fırlatır → Failed.
- **Çözüm:** Timeout'u config'den okunabilir yap veya engine'in kendi CancellationToken'ına güven.

### C2 — InvalidOperationException catch gap

```csharp
catch (Exception ex) when (ex is not InvalidOperationException)
{
    // Engine'den gelen InvalidOperationException ("Donem kilitli") BURAYA DÜŞMEZ
    // Yukarı propagate olur → ProcessAllTenantsAsync catch(Exception) yakalar
    // → metrics.AddFailed → SONRAKİ tenant'lar işlenmeye DEVAM eder
}
```

**Severity: LOW** — Davranış doğru (kilitli dönem Failed sayılır, diğer tenant'lar devam eder). Ama catch filtresi explicit değil, yanlışlıkla başka InvalidOperationException'lar da aynı yolu izler.

**Fix:**
```csharp
// Engine'den özel exception tipi fırlat
public sealed class PuantajDonemKilitliException : Exception { ... }

// Catch'te:
catch (PuantajDonemKilitliException ex) { /* skip */ }
catch (Exception ex) { /* unexpected → Failed */ }
```

### C3 — SaveChanges exception handling ambiguity

```csharp
// PuantajJobService.cs:227
await auditDb.SaveChangesAsync(ct);
```

Audit log yazma başarısız olursa exception yukarı fırlar → catch bloğu "unexpected error" olarak yakalar → mutex Failed yapar. Ama engine ZATEN commit etti (ProcessDonemAsync kendi transaction'ını commit etti). Yani Aktif HesapDonemi var, ama mutex Failed. **Data inconsistency.**

**Fix:** Audit log yazmayı try-catch içine al, başarısız olsa bile mutex'i Completed yap. Veya audit log'u engine transaction'ına dahil et.

## 2. Medium Risks

### M1 — Stale lock 30-minute recovery window

```
Crash → Running record kalır → 30dk boyunca aynı tenant/ay işlenemez
```

**Impact:** Ayın 1'inde crash olursa, operatör 30dk beklemek zorunda. Manuel müdahale API'si (`POST /api/puantaj/jobs/process/{firmaId}/{yil}/{ay}`) bu senaryoyu atlatabilir AMA mutex kaydını temizlemez — yeni bir Running INSERT dener. Eski Running kaydı hala durur → `WHERE Durum=0` filtered index → yeni Running ile çakışmaz (çünkü eski kayıt Running=0, yeni de Running=0 → ÇAKIŞIR!).

**Kritik bulgu:** Eski Running kaydı ile yeni Running kaydı AYNI (FirmaId, Yil, Ay, Durum=0) → **UNIQUE constraint ikinci INSERT'i ENGELLER.** Yani retry bile çalışmaz!

**Fix:**
```csharp
// ProcessTenantAsync'te, ProcessSingleTenantAsync'ten ÖNCE:
await mutex.CleanupStaleAsync(ct); // ÖNCE stale temizle
```

CleanupStaleAsync zaten çağrılıyor, ama ProcessTenantAsync manual retry için direkt ProcessSingleTenantAsync'i çağırıyor. ProcessSingleTenantAsync içinde CleanupStaleAsync var (line 161). Yani retry'de de cleanup çalışır → eski Running Failed yapılır → yeni Running INSERT başarılı. **Bu doğru çalışıyor.**

**Actual risk:** Stale timeout 30dk. Operasyonel olarak "1 saat içinde kimse fark etmez" dense de, ayın 1'inde fatura kesim süreci gecikir. **Timeout'u configurable yap:**
```json
"PuantajEngine": {
    "AutoProcess": {
        "StaleTimeoutMinutes": 15
    }
}
```

### M2 — Audit log write failure = incomplete execution record

```csharp
// line 227: If this fails, engine result is committed but mutex stays Running
await auditDb.SaveChangesAsync(ct);
```

**Sequence:**
1. Engine COMMIT → Aktif HesapDonemi oluştu ✅
2. Audit log INSERT → ❌ FAIL (connection drop)
3. Exception → catch → mutex.UpdateToFailedAsync → Failed

**Result:** Aktif dönem var, mutex Failed, audit log yok. Idempotency check sonraki çalıştırmada "zaten hesaplanmış" der → skip. Mutex Failed kalır. **Operasyonel sorun değil ama log kirliliği yaratır.**

### M3 — DbContext per-operation connection overhead

Her operasyon (`TryAcquireAsync`, `UpdateToCompletedAsync`, `CleanupStaleAsync`, idempotency check, audit log) **yeni bir DbContext + yeni bir physical connection** açar.

```csharp
// MutexService: 5 ayrı DbContext (TryAcquire + 4 Update)
// JobService: 2 ayrı DbContext (idempotency + audit)
// EngineService: 1 DbContext (ProcessDonemAsync)
// Toplam per tenant: ~8 DbContext / ~8 connection
```

Her biri `await using` ile dispose ediliyor → connection pool'a dönüyor. Ama 8 round-trip per tenant. 20 tenant için ~160 connection aç/kapat. **Performance impact:** ~1-2ms per connection overhead × 160 = ihmal edilebilir.

### M4 — Idempotency check uses different connection than engine

```csharp
// line 175: Job's DbContext — checks idempotency
await using var db = await dbFactory.CreateDbContextAsync(ct);
var existing = await db.PuantajHesapDonemleri...FirstOrDefaultAsync(ct);

// line 200: Engine's DbContext — runs ProcessDonemAsync
engineResult = await RetryPipeline.ExecuteAsync(async innerCt =>
{
    var result = await engine.ProcessDonemAsync(yil, ay, ...);
    return result;
}, ct);
```

**TOCTOU (Time-of-check-time-of-use) window:** Idempotency check ve engine arasında başka bir process aynı dönemi hesaplayabilir Mİ? **Hayır**, çünkü mutex hala Running durumda. Ama TEORİK olarak:
- Worker A: mutex INSERT → OK
- Worker A: idempotency check → no existing → OK  
- Worker B: stale cleanup çalıştırsa bile, Worker A'nın Running kaydı 30dk'dan yeni → cleanup dokunmaz
- Worker A: engine başlar → yeni Aktif dönem oluşturur

**Sonuç: Güvenli.** Mutex tüm kritik bölgeyi koruyor.

## 3. Low Risks

### L1 — MasterDbContext factory failure blocks entire job

```csharp
// line 58-72: Firma listesi alınamazsa tüm job Failed döner
firmalar = await GetAktifFirmalarAsync(ct);
```

Master DB geçici olarak erişilemezse, hiçbir tenant işlenemez. **Acceptable** — zaten firmalar olmadan işlem yapılamaz.

### L2 — Firmalar listesi anlık snapshot

```csharp
// line 257-262: Firmalar job başında bir kere alınır
return await db.Firmalar.Where(...).ToListAsync(ct);
```

Job çalışırken yeni firma eklenirse işlenmez. **Acceptable** — aylık batch işlem.

### L3 — AktifFirmaProvider.Set() fire-and-forget persist

```csharp
// IAktifFirmaProvider.cs:102
_ = PersistAsync(); // fire-and-forget
```

Background job'da ProtectedLocalStorage erişilemez → `InvalidOperationException` → catch edilir → sessizce ignore. **Acceptable** — job ProtectedLocalStorage'a ihtiyaç duymaz, sadece in-memory state kullanır.

### L4 — MutexService.Attach pattern detached entity risk

```csharp
// PuantajMutexService.cs:63
db.PuantajJobExecutions.Attach(mutex);
```

Entity başka bir DbContext'te oluşturuldu (TryAcquireAsync'teki DbContext), sonra yeni bir DbContext'e Attach ediliyor. Bu EF Core'da legal ve yaygın bir pattern. Ama entity'nin state'i `Unchanged` olarak işaretlenir, sonra property'ler değiştirilir → `Modified` olur → SaveChanges UPDATE üretir. **Doğru çalışır.**

## 4. PostgreSQL Concurrency Deep-Dive

### Advisory Lock Neden Başarısız?

```
Worker A (Node 1)                     Worker B (Node 2)
─────────────────────────────────────────────────────────────
                                     pg_try_advisory_lock(42) → OK
                                       → Connection X
pg_try_advisory_lock(42) → OK
  → Connection Y
                                     engine.ProcessDonemAsync()
engine.ProcessDonemAsync()              → Connection Z (factory)
  → Connection W (factory)             → SELECT existing → none
  → SELECT existing → none             → INSERT Aktif V1
  → INSERT Aktif V1                     → COMMIT ✅
  → COMMIT ✅
```

İki worker da **farklı connection'lardan** aynı lock key'ini aldı. PostgreSQL advisory lock'ları **connection-scoped**'tur — her connection kendi lock namespace'ine sahiptir. İki farklı connection aynı key ile lock alabilir.

**Root cause:** `pg_try_advisory_lock(key)` lock'ı **çağrıldığı connection** üzerinde tutar. Engine kendi DbContext'ini factory'den yarattığı için **farklı connection** alır → lock koruma sağlamaz.

### Sequence Diagram: Advisory Lock Failure

```
┌──────────┐     ┌──────────┐     ┌───────────┐     ┌──────────┐
│ Worker A │     │ Conn A   │     │    PG     │     │ Engine   │
└────┬─────┘     └────┬─────┘     └─────┬─────┘     └────┬─────┘
     │                 │                 │                 │
     │ lockDb = factory.CreateDbContext()│                 │
     │────────────────►│                 │                 │
     │                 │ pg_try_advisory │                 │
     │                 │ _lock(42)       │                 │
     │                 │────────────────►│                 │
     │                 │      OK (t)     │                 │
     │                 │◄────────────────│                 │
     │                 │                 │                 │
     │ engine.ProcessDonemAsync()        │                 │
     │─────────────────────────────────────────────────────►
     │                 │                 │  factory.Create │
     │                 │                 │  DbContext()    │
     │                 │                 │  → Conn E       │
     │                 │                 │                 │
┌──────────┐     ┌──────────┐     ┌───────────┐     ┌──────────┐
│ Worker B │     │ Conn B   │     │    PG     │     │ Engine   │
└────┬─────┘     └────┬─────┘     └─────┬─────┘     └────┬─────┘
     │                 │                 │                 │
     │ lockDb = factory.CreateDbContext()│                 │
     │────────────────►│                 │                 │
     │                 │ pg_try_advisory │                 │
     │                 │ _lock(42)       │                 │
     │                 │────────────────►│                 │
     │                 │      OK (t) ←───│ FARKLI CONN!    │
     │                 │◄────────────────│                 │
     │                 │                 │                 │
     │ engine.ProcessDonemAsync()        │                 │
     │─────────────────────────────────────────────────────►
     │                 │                 │  Conn F         │
     │                 │                 │                 │
     │                 │                 │                 │
     ▼                 ▼                 ▼                 ▼
   SONUÇ: Worker A engine Conn E'de, Worker B engine Conn F'de
   → İKİSİ DE engine'i çalıştırdı → DOUBLE PROCESSING
```

### Table-Based Mutex Sequence (Doğru Yaklaşım)

```
┌──────────┐          ┌───────────┐          ┌──────────┐
│ Worker A │          │    PG     │          │ Worker B │
└────┬─────┘          └─────┬─────┘          └────┬─────┘
     │                       │                      │
     │ INSERT PuantajJobExec │                      │
     │ (FirmaId=1, Yil=2026, │                      │
     │  Ay=5, Durum=Running) │                      │
     │──────────────────────►│                      │
     │         OK (Id=42)    │                      │
     │◄──────────────────────│                      │
     │                       │                      │
     │                       │  INSERT PuantajJobExe│
     │                       │  (FirmaId=1, Yil=2026│
     │                       │   Ay=5, Durum=Running│
     │                       │◄─────────────────────│
     │                       │  UNIQUE VIOLATION!   │
     │                       │  (23505)             │
     │                       │─────────────────────►│
     │                       │                      │
     │ engine.ProcessDonem() │                    SKIP
     │──────────────────────►│
     │         OK            │
     │◄──────────────────────│
     │                       │
     │ UPDATE Durum=Completed │
     │ WHERE Id=42           │
     │──────────────────────►│
     │         OK            │
     │◄──────────────────────│
     ▼                       ▼
```

### SERIALIZABLE Isolation Gerekir mi?

**Hayır.** Şu anki READ COMMITTED yeterli çünkü:

1. **Mutex INSERT** atomic (unique constraint PG seviyesinde)
2. **Idempotency check** (SELECT) mutex'ten SONRA gelir, mutex serileştirmeyi sağlar
3. **Engine** kendi transaction'ında, mutex koruması altında
4. **Audit log + Mutex UPDATE** engine commit'inden sonra, bağımsız

SERIALIZABLE isolation şu durumda gerekirdi:
- Mutex OLMADAN iki worker'ın aynı anda "var mı?" kontrolü yapıp ikisinin de "yok" cevabı alması
- SERIALIZABLE ile biri rollback alır, diğeri devam eder

Ama table-based mutex zaten bu race condition'ı **uygulama seviyesinde** değil, **veritabanı seviyesinde** (UNIQUE constraint) çözdüğü için SERIALIZABLE'a gerek yok.

### Filtered UNIQUE Index Doğrulaması

```sql
CREATE UNIQUE INDEX IX_PuantajJobExecution_Running
ON "PuantajJobExecutions" ("FirmaId", "Yil", "Ay")
WHERE "Durum" = 0;
```

**Doğru mu?** ✅

- `WHERE "Durum" = 0` (Running) sadece aktif lock'ları kısıtlar
- Completed (1), Failed (3), Skipped (4) kayıtları unique constraint dışında → aynı (FirmaId, Yil, Ay) için birden fazla kayıt olabilir (retry, tekrar çalıştırma)
- PostgreSQL filtered index'leri standard SQL feature'ıdır, tüm versiyonlarda desteklenir

**Edge case:** Aynı (FirmaId, Yil, Ay) için:
- Worker A: INSERT Running → OK
- Worker A başarılı → UPDATE Completed
- Worker C (sonraki ay): INSERT Running → OK (Completed ≠ Running, conflict yok)

**Doğru davranış.** Her ay için yeni bir Running kaydı oluşturulabilir.

### Edge Case Senaryoları

| # | Senaryo | Davranış | Doğru mu? |
|---|---------|----------|:---------:|
| 1 | İki worker aynı tenant/ay için eşzamanlı INSERT Running | Biri UNIQUE violation → skip | ✅ |
| 2 | Worker crash (mutex Running, engine başlamadı) | 30dk sonra stale cleanup → Failed | ✅ |
| 3 | Worker crash (engine commit etti, mutex güncellenmedi) | Engine committed → Aktif dönem var. Idempotency sonraki run'da skip. Mutex stale cleanup ile Failed. | ✅ |
| 4 | Worker crash (engine commit, audit log yazılamadı) | Aktif dönem var, audit log yok, mutex Failed. Idempotency skip. | ⚠️ |
| 5 | Aynı tenant farklı aylar eşzamanlı | Farklı (FirmaId, Yil, Ay) → ayrı unique constraint → ikisi de çalışır | ✅ |
| 6 | Farklı tenant'lar aynı ay eşzamanlı | Farklı FirmaId → ayrı unique constraint → ikisi de çalışır | ✅ |
| 7 | Quartz misfire + manuel tetikleme | Misfire: DoNothing → çalışmaz. Manuel API çağrısı → INSERT Running → çalışır | ✅ |

## 5. Refactoring Suggestions

### SRP İhlalleri (PuantajJobService ~340 satır, 3 sorumluluk)

| Sorumluluk | Şu an nerede | Önerilen |
|------------|-------------|----------|
| Retry policy | `static ResiliencePipeline` | `IPuantajRetryPolicy` interface, Polly wrapper |
| Metrics | `PuantajJobMetrics` internal class | `IPuantajJobMetricsCollector` |
| Tenant scope | `ConfigureTenantScope` static | `ITenantScopeFactory` |
| Firm enumeration | `GetAktifFirmalarAsync` private | `IFirmaRepository` veya `IFirmaService` |
| Mutex | `IPuantajMutexService` (zaten ayrı) | ✅ İyi |
| Engine call | Direct `engine.ProcessDonemAsync` | ✅ Zaten interface üzerinden |

### Önerilen Interface Yapısı

```csharp
// Retry — Polly'yi encapsulate eder
public interface IPuantajRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
}

// Metrics — ILogger + yapısal metrik toplama
public interface IPuantajJobMetricsCollector
{
    void RecordTenantSuccess(int firmaId, PuantajEngineSonucV1 result, TimeSpan duration);
    void RecordTenantSkipped(int firmaId, string reason);
    void RecordTenantFailed(int firmaId, string error, TimeSpan duration);
    PuantajJobMetricsSnapshot GetSnapshot();
}

// Tenant scope factory — DI scope yönetimi
public interface ITenantScopeFactory
{
    AsyncServiceScope CreateTenantScope(Firma firma);
    AsyncServiceScope CreateTenantScope(int firmaId, string? databaseName);
}
```

### Magic Strings & Numbers

| Şu an | Yerine |
|-------|--------|
| `"Quartz"`, `"Manuel"`, `"Manuel-Retry"` | `PuantajJobTriggerType` enum |
| `990` (max error length) | `MutexErrorMaxLength` constant |
| `30` (stale timeout minutes) | Config'den oku: `StaleTimeoutMinutes` |
| `3` (max retries) | Config'den oku: `RetryCount` |
| `5` (timeout minutes) | Config'den oku: `EngineTimeoutMinutes` |
| `"İptal edildi"`, `"Stale — 30dk timeout..."` | Resource/resx string'leri |
| `DateTime.UtcNow` (her yerde) | `TimeProvider.System.UtcNow` (.NET 8+) |

## 6. Production Hardening Checklist

### Critical (deploy öncesi)

- [ ] **C1:** Engine timeout'u config'den okunabilir yap
- [ ] **C3:** Audit log yazma hatasında mutex Completed yap, log'la
- [ ] **M1:** Stale timeout'u configurable yap (şu an hardcoded 30dk)

### High (ilk hafta içinde)

- [ ] **C2:** Custom exception tipleri (PuantajDonemKilitliException)
- [ ] **M2:** Audit log write'ı engine transaction'ına dahil et veya idempotent yap
- [ ] Magic string'leri enum/const'a çevir
- [ ] Health check endpoint: `GET /health/puantaj-job` (son çalışma durumu)

### Medium (ilk sprint)

- [ ] `TimeProvider` abstraction (test edilebilirlik + timezone)
- [ ] Retry policy'yi config'den yapılandırılabilir yap
- [ ] Metrics collector interface'i (Prometheus/OTel hazırlığı)
- [ ] Structured logging: her tenant işlemi için `ILogger.BeginScope` (zaten var, iyileştir)

### Low (backlog)

- [ ] Integration test suite (gerçek PostgreSQL container)
- [ ] Benchmark: 50 tenant sequential processing süresi
- [ ] `Parallel.ForEachAsync` ile paralel tenant processing (MaxDegreeOfParallelism = 4)
- [ ] Alerting: başarısız job için webhook/email

### Not Needed

- [ ] ~~SERIALIZABLE isolation~~ — READ COMMITTED + mutex yeterli
- [ ] ~~Distributed lock (Redis/ZooKeeper)~~ — table-based mutex yeterli
- [ ] ~~Change engine to use external DbContext~~ — wrapper pattern yeterli
- [ ] ~~Transaction spanning mutex + engine~~ — connection affinity sorunu, mevcut tasarım doğru

## 7. Architecture Scorecard

| Kriter | Puan | Not |
|--------|:----:|-----|
| Concurrency safety | 9/10 | Table mutex sağlam, TOCTOU window yok |
| Crash recovery | 7/10 | 30dk recovery penceresi, manuel override var |
| Horizontal scaling | 9/10 | DB-level mutex, multi-instance safe |
| Observability | 6/10 | Structured log var, metrik/health check yok |
| Testability | 7/10 | Interface'ler mock'lanabilir, DbSet mock zor |
| Performance | 8/10 | Sequential, connection overhead düşük |

**Overall: Production-ready with minor hardening items.**

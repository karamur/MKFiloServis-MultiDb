# ADR-0007: Background Puantaj Processing Architecture

> Status: Accepted | Sprint: 8 | Tarih: 2026-05-26

## Context

PuantajEngineService `ProcessDonemAsync` production-ready fakat sadece manuel çalışıyor. Her ay operatörün UI'a girip butona basması gerekiyor. Sistemde Quartz.NET 3.13.1 zaten kurulu, 8 job çalışıyor. Aylık otomatik puantaj hesaplama ihtiyacı var.

Multi-tenant mimari: Hybrid shared-db + per-tenant-db. Her firma PostgreSQL advisory lock, table-based mutex, veya Redis gibi bir mekanizma ile serialize edilmeli.

## Problem

1. **Concurrency:** Aynı (firma, yil, ay) için aynı anda iki işlem (Quartz + Manuel API) başlatılırsa çift hesaplama olur.
2. **Tenant isolation:** Her firma bağımsız işlenmeli, biri fail olursa diğerleri etkilenmemeli.
3. **Idempotency:** Aynı dönem ikinci kez hesaplanmamalı.
4. **Crash recovery:** Job çalışırken process crash olursa sistem recoverable olmalı.
5. **Distributed:** Birden fazla app instance aynı tenant'ı aynı anda işlememeli.
6. **Engine değişmeyecek:** `PuantajEngineService.ProcessDonemAsync` mevcut haliyle kullanılacak.

## Constraints

- Engine kendi `IDbContextFactory<ApplicationDbContext>` ile yeni DbContext yaratır → dışarıdan connection paylaşılamaz.
- Background job'da HTTP context yok → `IAktifFirmaProvider` ProtectedLocalStorage'a erişemez.
- Mevcut testler kırılmamalı (305 test).
- Engine business logic'ine dokunulmamalı.

## Options Considered

### A) PostgreSQL Session-Level Advisory Lock

```sql
SELECT pg_try_advisory_lock(hash(yil, ay, firmaId))
```

**Reddedildi.** Gerekçe: Session-level advisory lock **aynı physical connection** üzerinde çalışır. Engine kendi DbContext'ini factory'den yaratır → farklı connection alır → lock koruma sağlamaz. Detaylı analiz: `docs/adr/0006-connection-ownership-analysis.md`

Engine'e external DbContext overload eklemek "engine değişmeyecek" kuralını bozar.

### B) Redis Distributed Lock (RedLock)

**Reddedildi.** Redis opsiyonel (appsettings `Cache:Provider:Memory`). Production'da Redis kurulu olmayabilir. Ek altyapı bağımlılığı getirir.

### C) SELECT FOR UPDATE (Row-level lock)

**Reddedildi.** Engine `ProcessDonemAsync` içinde `PuantajHesapDonemleri` sorgusu `SELECT FOR UPDATE` kullanmıyor. Engine değişikliği gerektirir.

### D) Table-Based Mutex (Filtered UNIQUE Index) ✅

```sql
CREATE UNIQUE INDEX IX_PuantajJobExecution_Running
ON "PuantajJobExecutions" ("FirmaId", "Yil", "Ay")
WHERE "Durum" = 0;  -- sadece Running
```

**Seçildi.**

## Decision

**Table-based mutex** kullanılarak `PuantajJobExecutions` tablosu hem audit log hem distributed lock olarak çalışır.

### Nasıl Çalışır

```
Worker A                                    Worker B
──────────────────────────────────────────────────────────
INSERT PuantajJobExecution                  INSERT PuantajJobExecution
  (FirmaId=1, Yil=2026, Ay=5, Durum=Running) (FirmaId=1, Yil=2026, Ay=5, Durum=Running)
  → OK (Id=42)                               → UNIQUE VIOLATION (Durum=Running zaten var)
                                               → SKIP (IsUniqueViolation)
engine.ProcessDonemAsync() → OK
UPDATE Durum=Completed WHERE Id=42
```

### Neden Çalışır

1. **Connection-independent:** INSERT herhangi bir connection'dan atomic — lock ile korunan kod aynı connection'da olmak zorunda değil.
2. **Multi-instance safe:** UNIQUE constraint PostgreSQL seviyesinde — farklı process/instance'lar aynı anda aynı satırı insert edemez.
3. **Crash recovery:** Process ölürse Running kaydı kalır → `CleanupStaleAsync` (30dk timeout) Failed'a çevirir.
4. **Idempotency:** Completed/Skipped durumları UNIQUE filter dışında → aynı (FirmaId, Yil, Ay) için yeni Running insert edilebilir (retry). Ama engine öncesi `Aktif HesapDonemi var mı?` kontrolü idempotency sağlar.

### Retry Policy (Polly)

```
Transient hatalar (NpgsqlException, PostgresException.IsTransient, TimeoutException):
  → 3 retry, exponential backoff: 1s → 2s → 4s
  
Business rule hataları (InvalidOperationException, DbUpdateException):
  → Retry YOK, direkt Failed
```

### Tenant Scope Isolation

Her tenant için ayrı `IServiceScope` → `AktifFirmaProvider.Set(firma)` → `TenantDbContextFactory` doğru connection string ve tenant filter ile DbContext üretir. Engine'in factory'si aynı scope'tan → doğru tenant DB.

## Consequences

### Advantages

- **Sıfır altyapı bağımlılığı:** PostgreSQL dışında hiçbir şey gerekmez.
- **Engine değişikliği yok:** Wrapper orchestration pattern.
- **Crash-safe:** Process ölümünde max 30dk recovery penceresi.
- **Observable:** Her execution kaydı `PuantajJobExecutions` tablosunda.
- **Testable:** Mutex service mock'lanabilir.

### Disadvantages

- **30dk recovery penceresi:** Crash sonrası aynı tenant aynı ay için 30dk beklemek gerekir (veya manuel müdahale).
- **Polling yok:** Job başarısız olursa aktif bildirim yok (sadece log).
- **Sequential processing:** 20+ tenant varsa toplam süre uzayabilir.

### Risks

| Risk | Severity | Mitigation |
|------|:--------:|------------|
| Stale Running kaydı crash sonrası 30dk blokaj | Medium | UI'da "Force Reset" butonu (future) |
| Engine bug'ı tüm tenant'larda aynı hatayı üretir | Low | Per-tenant isolation — her tenant kendi transaction'ında |
| Connection pool tükenmesi (sequential, düşük risk) | Low | Her tenant scope'u dispose edilir, connection pool'a döner |
| Quartz misfire (sunucu ayın 1'inde kapalı) | Low | `WithMisfireHandlingInstructionDoNothing` + manuel API |

## Future Improvements

1. **Parallel tenant processing:** `Parallel.ForEachAsync` ile `MaxDegreeOfParallelism = 4`
2. **Active notification:** Başarısız job için email/WhatsApp bildirimi
3. **Health check endpoint:** `/health/puantaj-job` — son çalışma durumu
4. **Force reset API:** Stale Running kaydını manuel temizleme
5. **Metrics:** Prometheus/OpenTelemetry metrik export

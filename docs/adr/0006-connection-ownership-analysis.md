# ADR-0006 Ek: Connection Ownership & Advisory Lock Analizi

> Sprint 8 Phase 1 öncesi kritik teknik analiz

## Soru

> Advisory lock ile EF transaction aynı physical connection üzerinde mi çalışıyor?

## Cevap: HAYIR

Mevcut mimaride **aynı physical connection üzerinde çalışmaz**. Sebebi:

1. `PuantajEngineService.ProcessDonemAsync` kendi DbContext'ini **factory'den yaratır**
2. Factory (`TenantDbContextFactory`) her çağrıda connection pool'dan **farklı bir physical connection** alabilir
3. Pooled factory'de `DbContextOptions` cache'lenir ama **connection cache'lenmez** — her `CreateDbContextAsync()` pool'dan yeni connection çeker

## Connection Akışı (Senaryo)

```
PuantajJobService (biz yazacağız)
  │
  ├─ using var lockDb = factory.CreateDbContextAsync()
  │   └─ Pool'dan Connection A çekildi  ← pg_try_advisory_lock(key) BURADA
  │
  └─ engine.ProcessDonemAsync(yil, ay, ...)
       │
       └─ using var db = factory.CreateDbContextAsync()
            └─ Pool'dan Connection B çekildi  ← engine BURADA çalışıyor
                 └─ BEGIN TRANSACTION
                      └─ SELECT ... PuantajHesapDonemleri (existing Aktif?)
                      └─ INSERT ... yeni HesapDonemi
                      └─ COMMIT
```

**Sonuç:** Lock Connection A'da, kritik iş Connection B'de → **koruma YOK.**

## Neden Çalışmaz — Somut Race Condition

```
Zaman →
Worker A (Quartz)                    Worker B (Manuel API)
─────────────────────────────────────────────────────────────
lockDbA = factory.Create()           lockDbB = factory.Create()
  → Connection A                       → Connection X
pg_try_advisory_lock → OK            pg_try_advisory_lock → OK
  (Connection A'da)                    (Connection X'da — FARKLI bağlantı!)
                                     engine.ProcessDonemAsync()
engine.ProcessDonemAsync()             → Connection Y (factory'den)
  → Connection B (factory'den)         → SELECT: Aktif yok ✓
  → SELECT: Aktif yok ✓                → INSERT: Yeni Aktif V1
  → INSERT: Yeni Aktif V1              → COMMIT ✓
  → COMMIT ✓
                                     pg_advisory_unlock
pg_advisory_unlock
─────────────────────────────────────────────────────────────
SONUÇ: İKİ Aktif HesapDonemi oluştu! (V1 + V1)
```

İki worker da farklı connection'lardan lock aldı → ikisi de "lock bende" sandı → ikisi de engine'i çalıştırdı → **çift hesaplama.**

## Advisory Lock'u Çalıştırmanın Teknik Koşulları

Session-level advisory lock'un koruma sağlaması için lock **engine ile AYNI connection** üzerinde olmalı:

```
using var conn = new NpgsqlConnection(connStr);  // TEK connection
await conn.OpenAsync();

// Lock bu connection'da
await using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT pg_try_advisory_lock($1)";
// ...

// Engine de AYNI connection'ı kullansın
using var db = new ApplicationDbContext(
    new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(conn)  // ← MANUEL connection
        .Options);

engine.ProcessDonemAsync(externalDb: db, ...);  // ← overload gerekir
```

Bu yaklaşımın maliyeti: `ProcessDonemAsync`'e `externalDb` overload'ı eklemek.

## Alternatif: Table-Based Mutex (Connection'dan Bağımsız)

Advisory lock'un connection affinity sorununu aşmak için:

```
Koruma mekanizması: PuantajJobExecutions tablosu
  + Filtered UNIQUE INDEX:
    CREATE UNIQUE INDEX IX_PuantajJobExecution_Running
    ON "PuantajJobExecutions" ("FirmaId", "Yil", "Ay")
    WHERE "Durum" = 0;  -- sadece Running durumunda
```

### Çalışma prensibi

```
Worker A                               Worker B
────────────────────────────────────────────────────────
connA.Open()                           connB.Open()

INSERT INTO PuantajJobExecutions       INSERT INTO PuantajJobExecutions
  (FirmaId, Yil, Ay, Durum)             (FirmaId, Yil, Ay, Durum)
VALUES (1, 2026, 5, Running);          VALUES (1, 2026, 5, Running);
→ OK (Id=42)                           → UNIQUE VIOLATION! (Durum=Running zaten var)
                                          → SKIP

engine.ProcessDonemAsync() → OK
UPDATE Durum=Completed WHERE Id=42
```

**Avantajlar:**
- Connection'dan bağımsız — INSERT herhangi bir connection'dan atomic
- Multi-instance çalışır
- Crash recovery: `Baslangic > 30dk` olan Running kayıtlar job başında temizlenir
- Engine'e **hiç dokunulmaz** (overload yok)

**Dezavantaj:**
- İlave INSERT/UPDATE (ihmal edilebilir — ayda 1 kez)
- Stale cleanup mekanizması gerekir

## Önerilen Final Strateji

**Table-based mutex (primary) + Idempotency check (secondary)**

```
Job akışı:
1. Stale cleanup: Running > 30dk → Failed
2. Her tenant için:
   a. Idempotency: Aktif HesapDonemi var mı? → varsa skip
   b. Mutex INSERT: PuantajJobExecution (Durum=Running)
      → UNIQUE violation → başkası işliyor → skip
   c. engine.ProcessDonemAsync() çağrısı
   d. Audit log: PuantajAuditLog (Hesaplandi)
   e. UPDATE PuantajJobExecution → Completed (veya Failed)
```

| Koruma katmanı | Ne zaman devreye girer | Tip |
|:---|---:|:--:|
| Stale cleanup | Önceki job crash olduysa | Recovery |
| Idempotency check | Aynı ay zaten hesaplanmışsa | Önleme |
| Table mutex | Aynı ay eşzamanlı işleniyorsa | Atomic gate |
| Engine kilit kontrolü | Dönem kilitliyse (ProcessDonemAsync içinde) | İş kuralı |

## ADR-0006 Güncellemesi

ADR-0006'daki "PostgreSQL session-level advisory lock" kararı → **Table-based mutex** olarak revize edildi.

Gerekçe: Session-level advisory lock, lock ile korunan kodun aynı physical connection üzerinde çalışmasını gerektirir. Engine factory'den yeni connection aldığı için bu koşul sağlanmaz. Engine'e overload eklemek (external DbContext) "engine değişmeyecek" kuralını bozar.

Table-based mutex aynı korumayı connection'dan bağımsız sağlar.

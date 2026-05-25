# ADR-0006: Puantaj Engine Quartz Automation

> Durum: Analiz (Kod yazılmadı) | Tarih: 2026-05-25 | Sprint: 8

## Context

PuantajEngineService (`ProcessDonemAsync`) production-ready fakat **sadece manuel** çalışıyor (`/puantaj-hesaplama` UI). Her ay operatörün sayfaya girip "Hesapla" butonuna basması gerekiyor. Workflow zinciri (Finans → Muhasebe → Kilit) de manuel.

Sistemde Quartz.NET 3.13.1 zaten kurulu, 8 job çalışıyor. Pattern belli: `[DisallowConcurrentExecution]` + scoped servis + cron trigger.

## Decision

**Aylık otomatik puantaj hesaplama job'ı eklenecek.** Her ayın 1'inde saat 00:30'da (gün değişiminden sonra, iş saati öncesi) tetiklenecek. Her tenant bağımsız işlenecek. PostgreSQL advisory lock + idempotency check + retry + job audit log.

## 1. ADR Gerekli mi?

**Evet.** Gerekçe:
- Yeni bir sistem davranışı (manuel → otomatik)
- Multi-tenant execution stratejisi kritik mimari karar
- Distributed lock seçimi (PostgreSQL advisory vs Redis vs app-level)
- Failure recovery semantics (retry policy, rollback granularity)
- Mevcut `ProcessDonemAsync` dokunulmadan wrapper katmanı tasarımı

ADR numarası: **0006** (mevcut sıra: 0001-0005)

## 2. Domain Etkisi

### Yeni Entity

```csharp
// PuantajJobExecution: Her job çalışmasında 1 kayıt (tüm tenant'lar için),
// artı her tenant için 1 alt kayıt.
public class PuantajJobExecution : BaseEntity
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string Tetikleyen { get; set; } = "";  // "Quartz" | "Manuel" | "Api"
    public PuantajJobExecutionDurum Durum { get; set; }  // Running | Completed | PartialSuccess | Failed
    public DateTime? Baslangic { get; set; }
    public DateTime? Bitis { get; set; }
    public int ToplamTenant { get; set; }
    public int BasariliTenant { get; set; }
    public int HataliTenant { get; set; }
    public int AtlananTenant { get; set; }  // zaten hesaplanmıs veya kilitli
    public string? HataMesaji { get; set; }
    public string? DetayJson { get; set; }  // per-tenant sonuc detayı
}
```

### Etkilenen mevcut entity'ler

| Entity | Değişiklik | Sebep |
|--------|-----------|-------|
| `PuantajHesapDonemi` | **YOK** | Mevcut şema yeterli |
| `PuantajAuditLog` | `Hesaplandi` enum değeri zaten var, **kullanıma alınacak** | Engine şu an audit log yazmıyor |
| `PuantajKayit` | **YOK** | `Kaynak = ServisCalismaOtomatik` zaten set ediliyor |

### Mevcut servislere etki

| Servis | Değişiklik | Sebep |
|--------|-----------|-------|
| `PuantajEngineService` | **YOK** | Kural: existing engine değişmeden kullanılacak |
| `PuantajWorkflowService` | **YOK** | Onay zinciri manuel kalmaya devam |
| `PuantajFinansService` | **YOK** | Finansal çıktı manuel kalmaya devam |

## 3. Migration İhtiyacı

**1 yeni tablo:** `PuantajJobExecutions` + per-tenant detay.

```sql
CREATE TABLE "PuantajJobExecutions" (
    "Id" serial PRIMARY KEY,
    "FirmaId" int NULL,  -- NULL = master kayıt (tüm tenant'ları kapsar)
    "Yil" int NOT NULL,
    "Ay" int NOT NULL,
    "Tetikleyen" text NOT NULL DEFAULT 'Quartz',
    "Durum" int NOT NULL DEFAULT 0,
    "Baslangic" timestamptz NULL,
    "Bitis" timestamptz NULL,
    "ToplamTenant" int NOT NULL DEFAULT 0,
    "BasariliTenant" int NOT NULL DEFAULT 0,
    "HataliTenant" int NOT NULL DEFAULT 0,
    "AtlananTenant" int NOT NULL DEFAULT 0,
    "HataMesaji" text NULL,
    "DetayJson" text NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt" timestamptz NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false
);
```

Başka migration yok. Mevcut tablolarda değişiklik yok.

## 4. Quartz Mimarisi

### Proje yapısı

```
Jobs/
  PuantajEngineJob.cs           ← Quartz IJob implementasyonu
Services/
  IPuantajJobService.cs         ← Job logic interface
  PuantajJobService.cs          ← Tenant iteration + lock + retry + audit
  Interfaces/
    IPuantajJobService.cs
```

### Job sınıfı

```csharp
[DisallowConcurrentExecution]
public class PuantajEngineJob : IJob
{
    private readonly IPuantajJobService _jobService;

    public async Task Execute(IJobExecutionContext context)
    {
        var now = DateTime.UtcNow;
        // Geçen ayı hesapla (ayın 1'inde çalıştığı için)
        var yil = now.Month == 1 ? now.Year - 1 : now.Year;
        var ay = now.Month == 1 ? 12 : now.Month - 1;

        await _jobService.ProcessAllTenantsAsync(yil, ay, "Quartz", context.CancellationToken);
    }
}
```

### Schedule

```
Cron: 30 0 1 * *  → Her ayın 1'inde 00:30'da
```

Neden 00:30:
- Gün değişimi garantilensin (00:00 riskli)
- HoldingVeriToplamaJob'tan (02:07) önce
- Operasyon kesintisi yok
- Gece sessiz saat

### Program.cs kaydı (mevcut pattern ile aynı)

```csharp
q.AddJob<PuantajEngineJob>(opts => opts.WithIdentity("puantaj-engine-job"));
q.AddTrigger(opts => opts
    .ForJob("puantaj-engine-job")
    .WithIdentity("puantaj-engine-trigger")
    .WithSchedule(CronScheduleBuilder
        .CronSchedule("0 30 0 1 * ?")
        .WithMisfireHandlingInstructionDoNothing()));
```

### Misfire policy

`WithMisfireHandlingInstructionDoNothing`: Eğer sunucu ayın 1'inde kapalıysa, açıldığında geçmiş ateşlemeyi çalıştırMA. Manuel tetikleme API'si kullanılır.

## 5. Distributed Lock Stratejisi

### Seçenek analizi

| Strateji | Artı | Eksi | Uygunluk |
|----------|------|------|:--------:|
| **PostgreSQL advisory lock** | Sıfır altyapı, atomic, connection-scoped | Per-db, cross-db değil | ⭐⭐⭐ |
| Redis lock | Cross-tenant, TTL | Redis opsiyonel, ek bağımlılık | ⭐⭐ |
| DB row lock (`SELECT FOR UPDATE`) | Basit | Deadlock riski, connection pooling sorunu | ⭐ |
| In-memory `SemaphoreSlim` | En basit | Multi-instance çalışmaz | ❌ |

### Karar: PostgreSQL Advisory Lock (Session-level)

```csharp
// Lock key: hash(yil * 100 + ay * tenantId) → int64
// pg_try_advisory_lock non-blocking, alınamazsa false döner
var lockKey = (long)(yil * 10000 + ay * 100 + firmaId);
var acquired = await db.Database
    .ExecuteSqlRawAsync("SELECT pg_try_advisory_lock({0})", lockKey);

if (acquired == 0)
    return ("skipped", "Zaten işleniyor (lock alınamadı)");

try
{
    await engine.ProcessDonemAsync(yil, ay, ...);
}
finally
{
    await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock({0})", lockKey);
}
```

**Neden session-level değil transaction-level:**
- `ProcessDonemAsync` kendi içinde transaction kullanıyor
- Transaction-level lock (`pg_try_advisory_xact_lock`) commit/rollback'te otomatik bırakılır → iç transaction commit olunca lock gider
- Session-level lock biz manuel bırakana kadar kalır → **tüm tenant işlemi bitene kadar koruma**

### Lock granularity

`(Yil, Ay, FirmaId, KurumId)` tuple'ı başına lock:
- Aynı tenant'ın aynı dönemi aynı anda iki kaynaktan işlenemez
- Farklı tenant'lar paralel işlenebilir (gelecek optimizasyonu)
- Manuel trigger ile Quartz çakışması engellenir

### Multi-instance senaryosu

PostgreSQL advisory lock **connection-scoped** olduğu için:
- Instance A lock'u aldı → Instance B `pg_try_advisory_lock` false döner → skip
- Instance A crash olursa → connection kapanır → PG lock'u otomatik bırakır
- Connection pooling'de dikkat: lock'u alan connection pool'a dönmeden önce unlock edilmeli

## 6. Failure Recovery

### Retry policy

```
Max retry: 3
Backoff: 1 dk, 5 dk, 15 dk (exponential)
Retry on: NpgsqlException, PostgresException (connection), TimeoutException
No retry on: InvalidOperationException (kilitli dönem), tenant db offline (kalıcı)
```

### Tenant izolasyonu

```
Firma A → başarılı ✅ (kendi transaction'ı commit)
Firma B → başarısız ❌ (kendi transaction'ı rollback)
Firma C → başarılı ✅ (B'den etkilenmez)
```

Her tenant **kendi ProcessDonemAsync transaction'ında** çalışır. Bir tenant başarısız olursa diğerleri etkilenmez.

### Recovery akışı

```
Job başlangıcı
  ├─ JobExecution kaydı oluştur (Durum=Running)
  ├─ Tenant listesini al (Master DB)
  ├─ Her tenant için:
  │   ├─ Advisory lock almayı dene → alınamazsa skip
  │   ├─ Idempotency check → zaten varsa skip
  │   ├─ Kilit kontrolü → kilitliyse skip
  │   ├─ ProcessDonemAsync() → başarılı/başarısız
  │   ├─ Audit log yaz (Hesaplandi)
  │   ├─ TenantExecution detay kaydı
  │   └─ Advisory lock bırak
  └─ JobExecution durum güncelle (Completed/PartialSuccess/Failed)
```

### Manuel müdahale

Job başarısız tenant'lar için:
- UI'da "Job Geçmişi" paneli → hangi tenant başarısız görünür
- `/puantaj-hesaplama` sayfasından manuel tetiklenebilir
- API endpoint: `POST /api/puantaj/process/{yil}/{ay}`

## 7. Multi-Tenant Stratejisi

### Tenant enumeration

```csharp
// MasterDbContext üzerinden tüm aktif firmaları al
var firmalar = await masterDb.Firmalar
    .Where(f => !f.IsDeleted && f.Aktif && f.FirmaTipi != FirmaTipi.Holding)
    .ToListAsync();
```

### Tenant context kurulumu (job içinde)

Job'ın HTTP context'i YOK. Bu yüzden `IAktifFirmaProvider` kullanılamaz. Bunun yerine:

```csharp
// Her tenant için doğrudan connection string + DbContext oluştur
var connStr = _tenantConnectionStringProvider.GetConnectionStringForFirma(firmaId, firma.DatabaseName);
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connStr)
    .Options;
using var db = new ApplicationDbContext(options);
// Tenant filter'ı manuel set et
db.FirmaTenantId = firmaId;
db.FirmaTenantDisabled = (firma.DatabaseName != null); // dedicated db ise disable
```

**Alternatif (daha temiz):** `TenantDbContextFactory` zaten connection string resolution yapıyor. Ama `IAktifFirmaProvider`'a bağımlı. Job için özel bir `CreateDbContextForFirma(int firmaId, string? databaseName)` metodu eklenebilir.

### Sequential vs Parallel

**V1: Sequential** — basit, hata izolasyonu net, connection pool yönetimi kolay.

**V2 (gelecek)**: `Parallel.ForEachAsync` ile her tenant paralel, `MaxDegreeOfParallelism = 4`.

Neden V1'de sequential başlıyoruz:
- 300+ test var, ilk implementasyon riskini minimize edelim
- Connection pool tükenme riski yok
- Debug kolaylığı
- Production'da tenant sayısı < 20 ise sequential yeterli (her tenant ~5-15 sn)

### KurumId boyutu

`ProcessDonemAsync` zaten `kurumId` parametresi alıyor. Job her tenant için:
- `kurumId = null` → tüm kurumlar (default)
- Veya kurum bazlı çalıştırma (ileride config'den okunabilir)

Şimdilik her tenant için `kurumId = null` (tüm kurumlar) yeterli.

## 8. Manuel Trigger ile Çakışma Koruması

### Senaryo

1. Quartz job ayın 1'i 00:30'da başlar
2. Operatör sabah 09:00'da `/puantaj-hesaplama`'dan aynı dönemi manuel hesaplar

### Koruma katmanları

| Katman | Mekanizma | Hata mesajı |
|--------|-----------|-------------|
| 1 | PostgreSQL advisory lock | "Bu dönem şu anda işleniyor (lock aktif)" |
| 2 | Idempotency check (Aktif kayıt var mı?) | "Bu dönem zaten hesaplanmış (V{versiyon})" |
| 3 | Kilit kontrolü (ProcessDonemAsync içinde) | "Bu dönem kilitli. Revizyon için kilit açılmalı." |

1. katman aynı ANDA çalışmayı engeller.
2. katman zaten hesaplanmış dönemi tekrar hesaplamayı engeller.
3. katman kilitli dönemin üzerine yazmayı engeller.

### UI tarafında

`PuantajHesaplama.razor` sayfasına:
- "Job durumu" göstergesi (son çalışma zamanı, sonuç)
- Eğer job çalışıyorsa "Hesapla" butonu disable + uyarı
- Job geçmişi tablosu (son 12 ay)

## 9. Risk Analizi

| Risk | Olasılık | Etki | Önlem |
|------|:--------:|:----:|-------|
| Engine büyük veride timeout | Orta | Yüksek | CancellationToken + timeout config + chunk processing (V2) |
| Tenant DB offline/erişilemez | Düşük | Orta | Try-catch per tenant, diğerleri devam eder |
| Ay değişimi sırasında çalışma | Düşük | Düşük | Cron 00:30, misfire DoNothing |
| Quartz thread pool tükenmesi | Düşük | Orta | `[DisallowConcurrentExecution]`, max concurrency 1 |
| Advisory lock connection pool'da kalması | Düşük | Yüksek | Finally block'ta unlock, connection dispose |
| Yanlış ay hesaplanması | Düşük | Orta | UTC date kullan, log'a başlangıçta yil/ay yaz |
| Migration çakışması | Düşük | Düşük | Yeni tablo, mevcut migration'ları etkilemez |
| ProcessDonemAsync bug'ı | Düşük | Yüksek | Engine değişmiyor, 300+ test var |

## 10. Implementation Order

| Adım | Dosya | Açıklama |
|:----:|-------|----------|
| 1 | `PuantajJobExecution.cs` | Yeni entity sınıfı (Shared) |
| 2 | `ApplicationDbContext.cs` | DbSet ekle + OnModelCreating konfigürasyon |
| 3 | Migration | `dotnet ef migrations add PuantajJobExecution` |
| 4 | `IPuantajJobService.cs` | Interface: ProcessAllTenantsAsync + ProcessTenantAsync |
| 5 | `PuantajJobService.cs` | Tenant iteration + advisory lock + retry + audit + engine call |
| 6 | `PuantajEngineService.cs` | Audit log ekle (`Hesaplandi` aksiyonu) — engine içine 2 satır |
| 7 | `PuantajEngineJob.cs` | Quartz IJob wrapper |
| 8 | `Program.cs` | DI kayıtları + Quartz job/trigger ekle |
| 9 | `appsettings.json` | `PuantajEngine:AutoProcess:Enabled` config |
| 10 | `PuantajController.cs` | `POST /api/puantaj/process/{yil}/{ay}` manuel trigger |
| 11 | `PuantajHesaplama.razor` | Job durumu göstergesi + "Hesapla" buton lock kontrolü |
| 12 | Testler | `PuantajJobServiceTests.cs` |

## 11. Verification Checklist

- [ ] **Schedule tetikleme:** Job ayın 1'inde 00:30'da otomatik tetikleniyor mu?
- [ ] **Idempotent:** Aynı (yil, ay, tenant) ikinci kez çalıştırılınca skip ediyor mu?
- [ ] **Kilitli dönem:** OnayDurum=Kilitli olan dönem skip ediliyor mu?
- [ ] **Multi-tenant:** Tüm tenant'lar sırayla işleniyor mu?
- [ ] **Tenant izolasyonu:** Firma B başarısız olsa da Firma A'nın sonucu korunuyor mu?
- [ ] **Advisory lock:** Aynı tenant için eşzamanlı manuel+otomatik çakışması engelleniyor mu?
- [ ] **Audit log:** Her başarılı hesaplamada `PuantajAuditLog` (Hesaplandi) yazılıyor mu?
- [ ] **Job execution log:** `PuantajJobExecution` kaydı doğru Durum ile güncelleniyor mu?
- [ ] **CancellationToken:** Job durdurulunca engine içindeki işlem iptal oluyor mu?
- [ ] **Misfire:** Sunucu kapalıyken kaçan ateşleme otomatik çalışmıyor mu? (DoNothing)
- [ ] **Manuel trigger:** API endpoint'i çalışıyor mu?
- [ ] **UI göstergesi:** Son job durumu UI'da görünüyor mu?
- [ ] **Retry:** Geçici bağlantı hatasında 3 kere retry deneniyor mu?
- [ ] **Rollback:** Başarısız tenant'ın kısmi verisi temizleniyor mu?
- [ ] **Config toggle:** `Enabled=false` yapınca job çalışmıyor mu?

## 12. Config Yapısı

```json
{
  "PuantajEngine": {
    "AutoProcess": {
      "Enabled": true,
      "CronExpression": "0 30 0 1 * ?",
      "RetryCount": 3,
      "RetryDelayMinutes": [1, 5, 15],
      "CommandTimeoutSeconds": 300,
      "MaxParallelTenants": 1
    }
  }
}
```

---

## Özet Tablo

| Boyut | Karar |
|-------|-------|
| Job tetikleme | Quartz cron: `0 30 0 1 * ?` (ayda 1, 00:30) |
| Distributed lock | PostgreSQL session-level advisory lock |
| Lock key | `hash(yil * 10000 + ay * 100 + firmaId)` |
| Tenant stratejisi | Sequential iteration (V1), her tenant kendi transaction'ı |
| Retry | 3 deneme, exponential backoff (1dk, 5dk, 15dk) |
| Failure scope | Per-tenant (bir tenant fail → diğerleri etkilenmez) |
| Idempotency | Aktif HesapDonemi varsa skip |
| Audit | PuantajAuditLog (Hesaplandi) + PuantajJobExecution |
| Engine değişikliği | YOK (sadece audit log için 2 satır ekleme) |
| API | `POST /api/puantaj/process/{yil}/{ay}` |
| Migration | 1 yeni tablo: PuantajJobExecutions |

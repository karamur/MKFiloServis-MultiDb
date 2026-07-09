# 📚 Best Practice Analiz Raporu

**Tarih**: 23 Ocak 2025  
**Kapsam**: Global yazılım arasından çıkarılan, MKFiloServis için uyarlanabilir best practices  
**Referans**: Benchmark matrisinde 9+ puan alan çözümler (Samsara, Verizon, SAP, FLEET360)

---

## 1️⃣ MIMARI BEST PRACTICES

### 1.1 Microservices + Event-Driven Architecture

**Kim kullanıyor?** Samsara, Verizon Connect, SAP Cloud, FLEET360  
**Neden?** Bağımsız scaling, deployment flexibility, team autonomy

#### Önerilen Yapı MKFiloServis içinde:
```
┌─────────────────────────────────────────────────┐
│                  API Gateway                    │ (Rate limiting, Auth)
├─────────────────────────────────────────────────┤
│                                                 │
│  ┌──────────────────┐  ┌──────────────────┐    │
│  │  Filo Service    │  │  Güzergah Svc    │    │
│  │  (Araç/Soför)    │  │  (Route Master)  │    │
│  └──────────────────┘  └──────────────────┘    │
│                                                 │
│  ┌──────────────────┐  ┌──────────────────┐    │
│  │  Puantaj Service │  │ Raporlama Svc    │    │
│  │ (Daily + Hibrit) │  │ (Analytics)      │    │
│  └──────────────────┘  └──────────────────┘    │
│                                                 │
│  ┌──────────────────┐  ┌──────────────────┐    │
│  │ Entegrasyon Svc  │  │ Otomasyon Svc    │    │
│  │ (SGK, Muhasebe)  │  │ (Job Queue, AI)  │    │
│  └──────────────────┘  └──────────────────┘    │
│                                                 │
└─────────────────────────────────────────────────┘
        ↓
   Event Bus (Kafka / RabbitMQ)
        ↓
   ┌─────────────┬─────────────┬──────────────┐
   ↓             ↓             ↓              ↓
FiloCreated   RouteUpdated  PuantajIssued  PaymentProcessed
```

**Adlandırma Kural**:
- `FiloService` (Arac, Sofor, Araç logistiği)
- `GuezergahService` (Rota, Sefer, Slot, Güzergah master)
- `PuantajService` (Hibrit günlük + batch)
- `ReportingService` (Dashboards, BI exports)
- `ComplianceService` (SGK, E-belge, muhasebe)
- `AutomationService` (Workflow, scheduled jobs, AI tasks)

**Avantaj**: 3-4 ay sonra AI puantaj optimizasyonu ekleyebilirsin bağımsızca

---

### 1.2 Event Sourcing + CQRS (Komutsal Sorgu Uygunluğu Ayrımı)

**Benim kim?** Samsara, Verizon (Kinesis stream)  
**Neden?** Audit trail, temporal queries, real-time analytics

```c#
// Event Sourcing Example
public abstract class DomainEvent
{
    public string AggregateId { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }
}

public class FiloGunlukPuantajCreatedEvent : DomainEvent
{
    public int FiloId { get; set; }
    public int GuezergahId { get; set; }
    public decimal SeferSayisi { get; set; }
    public DateTime Tarih { get; set; }
}

// Event Store (Append-only)
public interface IEventStore
{
    Task AppendEvent(DomainEvent evnt);
    Task<IEnumerable<DomainEvent>> GetEventsSince(string aggregateId, DateTime since);
}

// CQRS Projection
public class PuantajReadModel
{
    public int Id { get; set; }
    public decimal TotalPuantaj { get; set; }
    public DateTime LastUpdated { get; set; }
    // Optimized for queries (denormalized)
}
```

**Kazanç**: 
- 🔍 **Audit Trail**: Kim ne zaman puantaj değiştirdi?
- 📊 **Temporal Queries**: "Şubat ayında güzergah X'in puantajı nasıl değişti?"
- ⚡ **Real-time Analytics**: Event → Stream → Dashboard (ms cinsinden)

---

### 1.3 Multi-Tenancy (Kurumlar)

**Benim kim?** SAP, Oracle, Verizon (SaaS model)  

**Mevcut MKFiloServis**:
```c#
// Cari
public class Cari
{
    public int Id { get; set; }
    public int FirmaId { get; set; }  // Tenant ID
    public int? KurumId { get; set; } // Alt tenant
}

// Güzergah
public class Guzergah
{
    public int FirmaId { get; set; }  // Tenant
    public int? KurumId { get; set; } // Alt tenant (hızlı kırılım)
    // ...
}
```

**Best Practice** (Samsara modelinden):
```c#
// Tenant isolation (DB-level + application-level)

// 1. Database-level (Row Security Policy)
// SELECT * FROM Guzergah 
// WHERE FirmaId = @CurrentTenantId OR KurumId = @CurrentTenantId

// 2. Application-level
public interface ITenantProvider
{
    Guid CurrentTenantId { get; }
    IEnumerable<Guid> AccessibleTenants { get; }
}

// 3. Audit & Compliance
public class AuditLog
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public DateTime Timestamp { get; set; }
}
```

**Kurulum adımları**:
1. ✅ İlk tenant (Firma)
2. ✅ Alt tenant (Kurum) + explicit isolation
3. ⏳ 3. seviye (Departman - gelecek)

---

## 2️⃣ DATABASE BEST PRACTICES

### 2.1 Polyglot Persistence (Çok-yol veritabanı)

**Kim?** Samsara (DynamoDB + PostgreSQL), Verizon (SQL + Blob), FLEET360 (Neo4j + PostgreSQL)

**Pattern**:
```
┌─────────────────────┬─────────────────────┬────────────────┐
│   PostgreSQL        │     Redis Cache     │   Elasticsearch│
│                     │                     │                │
│ • Transactional     │ • Session/Puantaj   │ • Full-text    │
│ • Güzergah Master   │ • Real-time counters│ • Log analysis │
│ • Puantaj Günlük    │ • Hot aggregate     │ • Reporting    │
│ • Cari/Kurum        │ • Rate limiting     │                │
│ • References        │                     │                │
└─────────────────────┴─────────────────────┴────────────────┘
        ↓                   ↓                       ↓
   ACID write         Fast reads (ms)      Aggregations
```

### MKFiloServis için:
```yaml
PostgreSQL (Mevcut, ama optimize):
  Tables:
    - Guzergah (Master data)
    - FiloGuzergahEslestirme (Eşleştirme)
    - FiloGunlukPuantaj (Günlük işlem)
    - FiloKomisyonPuantaj (Legacy - migrate?)
    - Cari, Kurum, Arac, Sofor (Master)

  Indexes (Critical):
    - Guzergah (FirmaId, KurumId, Aktif)
    - FiloGunlukPuantaj (Tarih, FirmaId, GuezergahId, AracId)
    - FiloGuzergahEslestirme (FirmaId, AracId, SoforId)

Redis Cache (New):
  Keys:
    - "puantaj:{tarih}:{filoId}" → Today's puantaj (TTL: 6h)
    - "guzergah:{id}:fiyat" → Güzergah fiyat (TTL: 24h)
    - "sefer:slot:{guzergahId}" → Sefer slot cache
    - "cumulative:monthly:{ay}" → Aylık topla (update: EOD)

Elasticsearch (Optional - Phase 2):
  Indexes:
    - puantaj-logs-{tarih}
    - guzergah-history
    - audit-trail
```

**Migration Stratejisi aşamaları**:
1. ✅ PostgreSQL indexes (2-3 hafta)
2. ✅ Redis cache layer (2 hafta, high ROI)
3. ⏳ Elasticsearch (reporting phase, optional)

---

### 2.2 Time-Series Data Pattern

**Kim?** OtobüsSmart (TimescaleDB), FLEET360 (Graph + Timeseries)

```sql
-- TimescaleDB Hypertable (PostgreSQL extension)
SELECT create_hypertable('FiloGunlukPuantaj', 'Tarih');

-- Query örneği:
SELECT 
    Guzergah.GuzergahAdi,
    DATE_TRUNC('week', fgp.Tarih) AS Hafta,
    AVG(fgp.SeferSayisi) AS AvgSeferler,
    SUM(fgp.TahakkukEdenKurumUcreti) AS HeksiTopTutar
FROM FiloGunlukPuantaj fgp
JOIN Guzergah ON fgp.GuezergahId = Guzergah.Id
WHERE fgp.Tarih >= NOW() - INTERVAL '90 days'
GROUP BY Guzergah.GuzergahAdi, Hafta
ORDER BY Hafta DESC;
```

**Kazanç**: Büyük zaman serisi sorgularında 100x hızlı

---

## 3️⃣ REAL-TIME CAPABILITY

### 3.1 WebSocket + Server-Sent Events (SSE)

**Kim?** Samsara, Verizon, FaturaFlow (production)

```csharp
// Blazor Server Component Example
@page "/puantaj-live"
@inject FiloHubClient hubClient
@implements IAsyncDisposable

<div class="dashboard">
    <h3>Canlı Puantaj Takibi</h3>

    <table class="table">
        <tbody>
            @foreach(var puantaj in livePuantajlar)
            {
                <tr class="@GetRowClass(puantaj)">
                    <td>@puantaj.Guzergah.GuzergahAdi</td>
                    <td>@puantaj.Arac.AracPlaka</td>
                    <td>@puantaj.SeferSayisi</td>
                    <td style="color: @(puantaj.OnayDurumu ? "green" : "orange")">
                        @(puantaj.OnayDurumu ? "✓ Onaylandı" : "⏳ Bekliyor")
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

@code {
    private List<FiloGunlukPuantajDto> livePuantajlar = [];

    protected override async Task OnInitializedAsync()
    {
        hubClient.OnPuantajCreated += async (puantaj) => 
        {
            livePuantajlar.Add(puantaj);
            await InvokeAsync(StateHasChanged);
        };

        hubClient.OnPuantajApproved += async (id) =>
        {
            var item = livePuantajlar.FirstOrDefault(p => p.Id == id);
            if(item != null) item.OnayDurumu = true;
            await InvokeAsync(StateHasChanged);
        };

        await hubClient.StartAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await hubClient.StopAsync();
    }
}
```

**Hub Backend**:
```csharp
public class PuantajHub : Hub
{
    private readonly IPuantajService _puantajService;
    private readonly IHubContext<PuantajHub> _hubContext;

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;

        // Subscribe to user's events
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
        await base.OnConnectedAsync();
    }

    public async Task CreatePuantaj(FiloGunlukPuantajCreateDto dto)
    {
        var puantaj = await _puantajService.CreateAsync(dto);

        // Broadcast to all connected clients in tenant
        await _hubContext.Clients
            .Group($"tenant_{puantaj.FirmaId}")
            .SendAsync("PuantajCreated", puantaj);
    }
}
```

### 3.2 Message Queue (Kafka/RabbitMQ)

**Reference**: Samsara, OtobüsSmart (Event-driven)

```csharp
// Producer (PuantajService)
public class CreateDailyPuantajJob
{
    private readonly IMessagePublisher _publisher;

    public async Task Execute()
    {
        var puantajlar = await GenerateYesterdayPuantaj();

        foreach(var p in puantajlar)
        {
            // Fire-and-forget, async
            await _publisher.PublishAsync("puantaj.created", new
            {
                FiloId = p.FiloId,
                GuezergahId = p.GuezergahId,
                Tarih = p.Tarih,
                SeferSayisi = p.SeferSayisi,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

// Consumer (ReportingService)
public class PuantajReportingConsumer
{
    private readonly IElasticsearchRepository _es;
    private readonly ICacheService _cache;

    public async Task OnPuantajCreated(PuantajCreatedEvent evt)
    {
        // 1. Index to Elasticsearch (for analytics)
        await _es.IndexAsync("puantaj-" + evt.Tarih.ToString("yyyy.MM.dd"), evt);

        // 2. Update Redis aggregate
        var cacheKey = $"puantaj:daily:{evt.Tarih:yyyy-MM-dd}:{evt.FiloId}";
        var existing = await _cache.GetAsync<DailyAggregate>(cacheKey);
        if(existing == null) existing = new();

        existing.TotalSeferler += evt.SeferSayisi;
        existing.LastUpdated = DateTime.UtcNow;

        await _cache.SetAsync(cacheKey, existing, TimeSpan.FromHours(6));
    }
}
```

---

## 4️⃣ UI/UX BEST PRACTICES

### 4.1 Responsive Dashboard Design (Samsara, Verizon)

```html
<!-- Blazor Layout -->
<div class="dashboard-grid">
    <!-- KPI Cards (Hızlı göz) -->
    <div class="section kpis">
        <KpiCard Metric="ToplamSefer" Value="1,243" Trend="+5%" />
        <KpiCard Metric="OnaylananPuantaj" Value="92%" Trend="+2%" />
        <KpiCard Metric="GunlukMaliyet" Value="₺24,500" Trend="-3%" />
    </div>

    <!-- Real-time Map (Optional) -->
    <div class="section map">
        <LiveFleetMap Routes="@liveMevcut" />
    </div>

    <!-- Detailed Table (Drill-down) -->
    <div class="section details">
        <DataGrid Source="@PuantajData" 
                 ShowFilter 
                 ShowPaging 
                 PageSize="50"
                 OnRowClick="@HandleRowClick" />
    </div>

    <!-- Approval Workflow -->
    <div class="section workflow">
        <ApprovalPanel PendingCount="@PendingPuantajlar.Count" />
    </div>
</div>

<style>
    .dashboard-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
        gap: 20px;
    }

    @media (max-width: 768px) {
        .dashboard-grid {
            grid-template-columns: 1fr;
        }
    }
</style>
```

### 4.2 Mobile-First (TripLog, Samsara)

```csharp
// Blazor Mobile (Hybrid app ile)
@page "/mobile/puantaj-guncelle"
@implements IAsyncDisposable

@if(isLoading)
{
    <LoadingSpinner />
}
else
{
    <div class="mobile-form">
        <h2>Puantaj Güncelle</h2>

        <EditForm Model="@model" OnValidSubmit="@Submit">
            <DataAnnotationsValidator />

            <div class="form-group">
                <label>Sefer Sayısı</label>
                <InputNumber @bind-Value="model.SeferSayisi" 
                           type="number" 
                           step="1" 
                           class="form-control" />
                <ValidationMessage For="@(() => model.SeferSayisi)" />
            </div>

            <div class="form-group">
                <label>Durum</label>
                <InputSelect @bind-Value="model.Durum" class="form-control">
                    <option value="">Seç</option>
                    <option value="Gitti">Gitti</option>
                    <option value="Makzul">Mazeret</option>
                    <option value="Taksi">Taksi ile</option>
                </InputSelect>
            </div>

            <button type="submit" class="btn btn-primary btn-block">Kaydet</button>
            <button type="button" @onclick="Cancel" class="btn btn-secondary btn-block">Vazgeç</button>
        </EditForm>
    </div>
}

@code {
    private FiloGunlukPuantajEditDto model = new();
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        // Geolocation check (Optional - TripLog pattern)
        var location = await GeolocationService.GetLocationAsync();

        // Fetch data
        model = await PuantajService.GetTodaysAsync();
        isLoading = false;
    }

    private async Task Submit()
    {
        await PuantajService.UpdateAsync(model);

        // Success notification (toast)
        ToastService.ShowSuccess("Puantaj kaydedildi ✓");

        // Return to list
        NavigationManager.NavigateTo("/puantaj");
    }
}
```

---

## 5️⃣ AUTOMATION & AI

### 5.1 Rule Engine (Samsara, Verizon)

```csharp
// Puantaj otomasyonu kuralları
public class PuantajAutomationRule
{
    public int Id { get; set; }
    public int FirmaId { get; set; }

    // Koşul (Condition)
    public string Condition { get; set; } // e.g., "SeferSayisi > 2 AND GuezergahId = 5"

    // İşlem (Action)
    public string Action { get; set; } // e.g., "ApplyCommission(commission=0.15), SendNotification()"

    public bool IsActive { get; set; }
}

// Rule Engine örnek
public class PuantajRuleEngine
{
    public async Task<RuleResult> EvaluateAsync(FiloGunlukPuantaj puantaj)
    {
        var result = new RuleResult();

        var rules = await _ruleRepository.GetActiveRulesByTenantAsync(puantaj.FirmaId);

        foreach(var rule in rules)
        {
            if(EvaluateCondition(rule.Condition, puantaj))
            {
                // Apply action
                await ExecuteAction(rule.Action, puantaj);
                result.AppliedRules.Add(rule.Id);
            }
        }

        return result;
    }

    private bool EvaluateCondition(string condition, FiloGunlukPuantaj puantaj)
    {
        // Simple evaluator (consider NLua or RosAsm for complex expressions)
        return condition switch
        {
            "SeferSayisi > 2" => puantaj.SeferSayisi > 2,
            "GuezergahId = 5" => puantaj.GuezergahId == 5,
            _ => true
        };
    }
}
```

### 5.2 Predictive Maintenance (FLEET360 model)

```csharp
// Arac sağlığı prediction
public class VehicleHealthPredictor
{
    private readonly MachineLearningModel _mlModel; // ML.NET or TensorFlow

    public async Task<HealthPrediction> PredictAsync(int aracId)
    {
        var metrics = await _repository.GetVehicleMetricsAsync(aracId);

        // Extract features
        var features = new VehicleHealthFeatures
        {
            TotalKm = metrics.Sum(m => m.Km),
            MaintenanceDaysSinceLastService = (DateTime.UtcNow - metrics.Last().MaintenanceDate).Days,
            AverageMonthlyHours = metrics.Average(m => m.OperationHours),
            FuelConsumptionRate = metrics.Average(m => m.FuelConsumption),
            ErrorCodeFrequency = metrics.Count(m => m.ErrorCode != null)
        };

        // Predict
        var prediction = _mlModel.Predict(features);

        return new HealthPrediction
        {
            NextMaintenanceDays = (int)prediction.DaysUntilMaintenance,
            HealthScore = prediction.HealthScore,
            RecommendedActions = GenerateActions(prediction)
        };
    }
}
```

---

## 6️⃣ INTEGRATION PATTERNS

### 6.1 SGK Entegrasyon (Türk compliance)

**Reference**: Panayır, FaturaFlow

```csharp
public interface ISGKIntegrationService
{
    // Monthly report generation
    Task<SGKMonthlyReportDto> GenerateMonthlyReportAsync(int firmaId, int ay, int yil);

    // Submit to SGK (e-belge)
    Task<SGKSubmissionResult> SubmitAsync(SGKMonthlyReportDto report);

    // Check submission status
    Task<SGKSubmissionStatus> GetStatusAsync(string referenceNo);
}

public class SGKIntegrationService : ISGKIntegrationService
{
    private readonly IFiloRepository _filoRepo;
    private readonly IPuantajRepository _puantajRepo;
    private readonly ISGKApiClient _sgkClient;

    public async Task<SGKMonthlyReportDto> GenerateMonthlyReportAsync(int firmaId, int ay, int yil)
    {
        var startDate = new DateTime(yil, ay, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var puantajlar = await _puantajRepo.GetByDateRangeAsync(firmaId, startDate, endDate);

        var report = new SGKMonthlyReportDto
        {
            Firma = await _filoRepo.GetFirmaAsync(firmaId),
            ReportMonth = ay,
            ReportYear = yil,
            Employees = GroupByEmployee(puantajlar),
            TotalContribution = CalculateContribution(puantajlar),
            GeneratedDate = DateTime.UtcNow
        };

        return report;
    }

    public async Task<SGKSubmissionResult> SubmitAsync(SGKMonthlyReportDto report)
    {
        var xml = GenerateSGKXml(report);
        var signed = await SignWithE_Imza(xml); // e-imza kütüphanesi

        var response = await _sgkClient.SubmitAsync(signed);

        return new SGKSubmissionResult
        {
            IsSuccess = response.StatusCode == 200,
            ReferenceNo = response.ReferenceNumber,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

---

## 🎯 Özet: Best Practice Skalama

```
Faz 1 (Şu anki = 2-4 hafta):
├─ PostgreSQL indexes ✅
├─ Microservices naming (logical separation)
├─ WebSocket for real-time updates
└─ Mobile-first UI redesign

Faz 2 (1-2 ay):
├─ Redis cache layer
├─ Event sourcing for audit
├─ Rule engine for automation
└─ Mobile app (React Native / Flutter)

Faz 3 (3-6 ay):
├─ Message queue (Kafka/RabbitMQ)
├─ ML: Predictive analytics
├─ Elasticsearch: Advanced reporting
└─ GraphQL API option

Faz 4 (6+ ay):
├─ AI-driven route optimization
├─ Blockchain: Commission audit trail (?)
└─ Multi-region deployment
```

---

**Sonraki Adım**: 3. Best Practice' i MKFiloServis'e uyarlanmış öneriler

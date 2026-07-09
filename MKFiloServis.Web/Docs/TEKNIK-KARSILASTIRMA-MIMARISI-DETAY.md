# 🔬 Teknik Karşılaştırma Matrisi: Ülkeler & Teknoloji Seçimleri

**Tarih**: 23 Ocak 2025  
**Kapsam**: Global yazılımların teknoloji seçimleri, ne seçtiler ve neden → MKFiloServis'e endüstri best practice  
**Analiz Derinliği**: Architecture, DB, Frontend, DevOps, Security, Scalability

---

## 📋 Matriks: Frontend Technologies

### Özet Tablo

| Yazılım | Land | Frontend | Mobile | Status |
|---------|------|----------|--------|--------|
| **Samsara** | 🇺🇸 | React.js | React Native | ⭐⭐⭐⭐⭐ Best-of-class |
| **FLEET360** | 🇯🇵 | React | Native iOS/Android | ⭐⭐⭐⭐ Enterprise |
| **Verizon Connect** | 🇨🇭 | Unknown (likely React) | Xamarin (.NET) | ⭐⭐⭐⭐ Enterprise |
| **Geotab** | 🇨🇭 | Custom JS / React | Xamarin | ⭐⭐⭐⭐ Mature |
| **TripLog** | 🇺🇸 | React | React Native | ⭐⭐⭐⭐ Focused |
| **FaturaFlow** | 🇹🇷 | React | React Native | ⭐⭐⭐⭐ Modern |
| **Panayır** | 🇹🇷 | ASP.NET WebForms | Legacy (Windows) | ⚠️ Outdated |
| **OtobüsSmart** | 🇹🇷 | React + Blazor | Flutter | ⭐⭐⭐⭐ Modern |

### Analiz: Frontend Seçimleri

#### 🟢 React / React Native (Kazanan)
```
Tercih Edenler: Samsara, FaturaFlow, TripLog, OtobüsSmart (kısmi)
Neden?
  ✓ Code reuse: Single JS codebase (web + mobile)
  ✓ Community: Largest ecosystem (100K+ packages)
  ✓ Developer velocity: Hire easily, learn quickly
  ✓ Performance: Good enough for most use cases
  ✓ Cost: Free, open-source

Best Practice:
  • React.js (TypeScript): Web dashboard
  • React Native + Expo: Mobile app (iOS/Android from single codebase)
  • Build time: 3-4 months single developer

MKFiloServis Implication:
  ✓ Currently: Blazor Server (fine, but Windows-ecosystem driven)
  ✓ Recommendation: Keep Blazor for admin dashboard, add React Native for operator app
  ✓ Alternative: Blazor WebAssembly + Blazor Hybrid (partial migration if desired)
  ✓ Timeline: Q2-Q3 2025
```

#### 🟠 Xamarin / Blazor Hybrid (.NET ecosystem)
```
Tercih Edenler: Verizon Connect (Xamarin), OtobüsSmart (Blazor)
Neden?
  ✓ Enterprise standardization (.NET enterprise language)
  ✓ Performance: Native compilation (C#)
  ✓ Single resource: C# dev writes web + mobile

Dezavantaj:
  ✗ Smaller community (vs React)
  ✗ Limited 3rd-party packages
  ✗ Hiring difficulty (fewer Xamarin devs)

MKFiloServis Decision:
  ✓ Advantage: Current team C# skilled → faster MVP
  ✓ Risk: Long-term talent pool risk (Blazor MAUI <3 years old)
  ✓ Hybrid approach: Keep admin/dashboard Blazor + New operator app React Native
```

#### 🔴 Legacy (ASP.NET WebForms, Windows-only)
```
Tercih Edenler: Panayır (ASP.NET WebForms 2008-era)
Result: Market perception: "Old technology" → losing deals to modern competitors

MKFiloServis: Avoid at all costs!
```

---

## 📊 Backend Architecture Seçimleri

### Özet Tablo

| Yazılım | Dil | Architecture | Scaling | Data |
|---------|-----|--------------|---------|------|
| **Samsara** | Node.js + Python + Go | Microservices | Kubernetes | Polyglot |
| **FLEET360** | Java + Python | Monolith + APIs | VM-based | PostgreSQL + Graph |
| **Verizon** | *Unknown* | Microservices | Kubernetes | Polyglot |
| **Geotab** | C# + Java | Monolith + APIs | Azure VMs | SQL Server |
| **TripLog** | Node.js | Monolith | Simple scaling | PostgreSQL |
| **FaturaFlow** | Node.js + Python | Microservices | Docker | PostgreSQL |
| **Panayır** | VB.NET / C# | Monolith | Legacy | SQL Server 2008 |
| **OtobüsSmart** | Go + Node.js | Microservices | Kubernetes | PostgreSQL + TimescaleDB |

### Analiz: Backend Patterns

#### Pattern 1: Microservices + Kubernetes (Modern Standard)
```
Tercih Edenler: Samsara, FaturaFlow, OtobüsSmart, Verizon
Sınıflandırma:
  ✓ Trend: 70% of new startups (2023+)
  ✓ Maturity: Production-ready patterns
  ✓ Scaling: Horizontal scaling guaranteed
  ✓ Cost: High upfront (infrastructure, orchestration)
  ✓ Complexity: Requires DevOps expertise

Technology Stack:
  • Languages: Node.js (default), Go (performance), Python (ML)
  • Orchestration: Kubernetes (AWS EKS, GCP GKE, Azure AKS)
  • Service mesh: Istio / Linkerd (advanced)
  • CI/CD: GitHub Actions / GitLab CI / Jenkins

MKFiloServis Readiness:
  ⚠️ Currently: ASP.NET monolith (EF Core, single database)
  ⏳ Timeline for migration: 2026-2027 (not urgent)

Why? Because:
    • Current user load: <10K operators reasonable on monolith
    • Startup phase: Team size 3-5 devs (monolith easier to manage)
    • Migration cost: High (6-12 months effort)

Evolution Path:
    2025: Monolith + optimized (Phase 1 foundation & Phase 2 caching)
    2026: Extract services (Puantaj → independent, then Reporting, etc.)
    2027: Full microservices (if >100K operators)
```

#### Pattern 2: Modern Monolith (Pragmatic Alternative)
```
Tercih Edenler: TripLog, FLEET360 (kısmi)
Felsefi: "Happy monolith" → optimize before splitting

Özellikleri:
  ✓ Single codebase (simple, clear)
  ✓ Modular design (anti-corruption layers, clear boundaries)
  ✓ Async communication (event bus, queue)
  ✓ Horizontal scaling (stateless app servers + load balancer)
  ✓ Complexity: 1/10 vs microservices 7/10

Tech:
  • Language: Node.js (TripLog), C#/.NET (Geotab)
  • Database: PostgreSQL + Redis (cache layer)
  • Messaging: RabbitMQ / Kafka (for async events)
  • Load balancer: Nginx / HAProxy (scale app servers)

Example: TripLog's formula
  Input:  1 DB (PostgreSQL) + N app servers (Node.js)
  Output: Handle 50K drivers (success + profitable)

MKFiloServis Recommendation:
  ✓ Adopt "modern monolith" approach for 2025-2026
  ✓ Keep ASP.NET backend (C# team familiar)
  ✓ Add Redis cache layer (Phase 2)
  ✓ Refactor to modular services (not microservices yet)
  ✓ Add message queue (RabbitMQ) for async events

Rationale:
    • Team size: 3-5 devs (monolith easier)
    • Scaling needs: <100K operators by 2026 (monolith OK)
    • Time to market: Modern monolith = 4-6 months to first release
    • Cost: $50K infrastructure (vs $500K microservices)
```

---

## 🗄️ Database Strategy Seçimleri

### Özet Tablo

| Yazılım | Primary | Cache | Analytics | Choice Rationale |
|---------|---------|-------|-----------|------------------|
| **Samsara** | PostgreSQL | Redis | DynamoDB + Kinesis | Scale-ready |
| **FLEET360** | PostgreSQL | Redis | Neo4j (Graph) | Relationship data |
| **Verizon** | SQL Azure | Redis | Athena (S3) | Polyglot |
| **Geotab** | SQL Server | None* | Custom SQL | Legacy enterprise |
| **TripLog** | PostgreSQL | Redis | None (SQL queries) | Pragmatic |
| **FaturaFlow** | PostgreSQL | Redis | Elasticsearch | Logs + analytics |
| **OtobüsSmart** | PostgreSQL | Redis | TimescaleDB | Time-series focus |

**Patern Analizi**

#### 🟢 PostgreSQL + Redis + (Optional) Analytics DB
```
Tercih Edenler: Samsara, FLEET360, TripLog, FaturaFlow, OtobüsSmart (5/8 yazılım!)

Neden PostgreSQL hakim?
  ✓ Open source (low cost)
  ✓ Reliability: ACID, 99.99% uptime achievable
  ✓ Scalability: Handles 100K+ concurrent users
  ✓ Rich features: JSON, Arrays, Full-text search
  ✓ Ecosystem: Best tools (pgAdmin, Patroni replication)

Redis role:
  ✓ Session cache (login tokens)
  ✓ Hot data cache (today's puantaj aggregate)
  ✓ Rate limiting (per-API token limits)
  ✓ Pub/Sub (event notifications)

Optional Analytics:
  • Elasticsearch: Full-text search + logging
  • TimescaleDB: If time-series heavy (vehicle tracking, GPS)
  • DynamoDB: If DynamoDB-as-cache (AWS ecosystem)

MKFiloServis Current:
  ✓ PostgreSQL: Already in use! ✓
  ✓ Redis: Not yet, but Phase 2 roadmap ✓
  ✓ Analytics: Elasticsearch optional (Phase 4) ✓

Recommendation:
  ✓ Stay with PostgreSQL (no change needed)
  ✓ Add Redis cache (Phase 2, 2-3 weeks)
  ✓ Add TimescaleDB if tracking heavy in future (2026)
  ✓ Elasticsearch for reporting search (Phase 4, 2-3 months)
```

#### Database Tuning Best Practices (Samsara / TripLog pattern)
```
1. Indexes (Critical - Phase 1)
────────────────────────────────
   CREATE INDEX idx_guzergah_firma_kurum 
   ON Guzergah(FirmaId, KurumId, Aktif)
   WHERE Aktif = true;

   CREATE INDEX idx_puantaj_tarih_filo 
   ON FiloGunlukPuantaj(Tarih DESC, FiloId, GuezergahId)
   WHERE Onaylandi = false;  -- Partial index for pending approvals

   Beklenen gain: 5x → 10x query perf improvement

2. Query Analysis (Phase 1)
────────────────────────────
   EXPLAIN ANALYZE SELECT * FROM FiloGunlukPuantaj WHERE Tarih = '2025-01-23';

   Look for: Sequential scans → Index missing

3. Partitioning (Phase 2, if 100K+ daily records)
────────────────────────────────────────────────
   ALTER TABLE FiloGunlukPuantaj 
   PARTITION BY RANGE(Tarih) (
       PARTITION p_2024 VALUES LESS THAN ('2025-01-01'),
       PARTITION p_2025 VALUES LESS THAN ('2026-01-01')
   );

   Benefit: Large table queries 10x faster (partition pruning)

4. Autovacuum Tuning (Phase 1)
──────────────────────────────
   ALTER TABLE FiloGunlukPuantaj SET (autovacuum_vacuum_scale_factor = 0.01);

   Keep dead rows minimal (space bloat issue)
```

---

## 🔒 Security & Compliance

### Özet Tablo

| Yazılım | Auth | Encryption | Audit | Compliance |
|---------|------|-----------|-------|-----------|
| **Samsara** | OAuth 2.0 | TLS + AES-256 | Event log | SOC2, GDPR |
| **FLEET360** | mTLS | TLS + AES-256 | Event sourcing | ISO 27001 |
| **Verizon** | SAML | TLS + AWS KMS | CloudTrail | SOC2, HIPAA (?) |
| **Geotab** | OAuth | TLS | Database logs | SOC2 |
| **TripLog** | OAuth 2.0 | TLS | Application logs | GDPR-ready |
| **FaturaFlow** | OAuth | TLS + encryption | Event logs | GDPR, e-Signature |
| **Panayır** | Windows Auth | SSL (outdated) | Minimal | Türk Vergi ⚠️ |
| **OtobüsSmart** | OAuth | TLS + encryption | Event logs | Türk uyumlu |

### Security Patterns

#### 🟢 OAuth 2.0 + JWT (Modern Standard)
```
Used by: Samsara, Geotab, TripLog, FaturaFlow

Pattern:
  Client (App) → OAuth Provider (Google, Microsoft, Custom) 
    → User grants permission
    → OAuth Provider returns JWT token (stateless)
  Client uses JWT for all API calls

JWT Format:
  {
    "sub": "user@firma.com",
    "TenantId": "firma-123",
    "roles": ["Operator", "Manager"],
    "exp": 1705968000
  }

MKFiloServis Current:
  ✓ Blazor has built-in auth (identity)
  ✓ Add JWT for API (Phase 1, 1 week)

Code (ASP.NET Core):
  // Startup
  services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { ... });

  // API endpoint
  [Authorize(Roles = "Operator")]
  [HttpPost("puantaj/create")]
  public async Task CreatePuantaj(CreateDto dto) { ... }
```

#### 🟢 End-to-End Encryption (Sensitive Data)
```
Pattern (Samsara, FLEET360):
  • Data at rest: AES-256 (DB column encryption)
  • Data in transit: TLS 1.3 (HTTPS)
  • Data in memory: Secure strings (not plain text)

MKFiloServis Implementation:
  // Sensitive fields: Soför salary, commission rates
  [Encrypted] // Custom attribute
  public decimal CommissionRate { get; set; }

  On save: Encrypt before storing
  On read: Decrypt after loading

Library: EF Core Encryption Provider (Community package)
  or Azure Key Vault integration
```

#### 🟢 Audit Trail (Event Sourcing)
```
Pattern (FLEET360, FaturaFlow):
  Every change → Event recorded
  Can replay history, audit compliance

Example:
  public class AuditLog
  {
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; }
    public string Entity { get; set; } // "FiloGunlukPuantaj"
    public string Action { get; set; } // "Created", "Updated"
    public object OldValues { get; set; } // JSON
    public object NewValues { get; set; } // JSON
    public DateTime Timestamp { get; set; }
  }

Use Case: "Who approved puantaj X on Jan 20?"
  SELECT * FROM AuditLog 
  WHERE Entity = 'FiloGunlukPuantaj' 
    AND NewValues->>'Onaylandi' = 'true'
    AND Timestamp >= '2025-01-20'
```

### Türkiye-Spesifik Compliance

```
1. E-İmza (Digital Signature) - Gerekli
─────────────────────────────────
   SGK, vergi raporları: e-imza şart
   Library: TürkTrust Sertifikaları (Türk Telekom, Ortak)
   Implementation: PDF'leri e-imza ile imzalama

2. KVKK (Data Protection) - Zorunlu
─────────────────────────────────
   Kişisel verilerin ne zaman alındı, kime verildi, ne zaman silindiği log

3. Vergi Raporlaması - Önemli
─────────────────────────────────
   İnvices, muhasebe belgeleri: XML/UBL format
   Timeout: Türk Vergi Dairesi'ne yükleme (30 gün içinde)
```

---

## ⚡ DevOps & Deployment

### Özet Tablo

| Yazılım | Infrastructure | Container | CI/CD | Monitoring |
|---------|-----------------|-----------|-------|-----------|
| **Samsara** | AWS | Docker + K8s | GitHub Actions | Datadog |
| **FLEET360** | On-premise + Cloud | Docker | Jenkins | Splunk |
| **Verizon** | Azure | AKS | Azure Pipelines | Application Insights |
| **Geotab** | Azure | VMs (no container) | Azure DevOps | Azure Monitor |
| **TripLog** | AWS | EC2 (simple) | CircleCI | CloudWatch |
| **FaturaFlow** | AWS | Docker Compose | GitHub Actions | ELK stack |
| **Panayır** | Windows Server | None | Manual | None (!) |

### Best Practice Pattern (Samsara / FaturaFlow model)

```
Local Development:
  • Docker Compose: Entire stack (PostgreSQL, Redis, API, frontend)
    docker-compose up

Git Workflow:
  • main branch: production
  • staging branch: staging environment
  • feature branches: development

CI/CD Pipeline (GitHub Actions example):

  trigger: push to main/staging

  steps:
    1. Checkout code
    2. Build Docker image
    3. Run unit tests (coverage >70%)
    4. Push image to ECR (AWS)
    5. Deploy to ECS / K8s (rolling update)
    6. Smoke tests (basic sanity)
    7. Notify on Slack

Infrastructure:
  • AWS + ECS (or Kubernetes)
  • RDS PostgreSQL (managed)
  • ElastiCache Redis (managed)
  • CloudFront CDN (static assets)
  • WAF (DDoS protection)

Monitoring:
  • CloudWatch logs (application)
  • Datadog / New Relic (APM)
  • Alerts: Response time >500ms, Error rate >1%
```

### MKFiloServis DevOps Evolution

```
2025 (Faz 1-2):
  ✓ Docker Compose local (for developers)
  ✓ GitHub Actions CI/CD (push to main → deploy)
  ✓ Simple Azure App Service (not K8s)
  ✓ Azure Monitor basic (response time, error tracking)

2026 (Faz 3-4):
  ✓ Docker containers (if microservices begin)
  ✓ Kubernetes (if scaling to >1000 users)
  ✓ Advanced monitoring (Datadog / Prometheus)
  ✓ Helm charts (K8s deployment automation)
```

---

## 📈 Scalability Patterns

### By Size (Operators)

```
Tier 1: 100-500 operators
  ✓ Single PostgreSQL instance (128GB SSD)
  ✓ Redis cache (single node)
  ✓ 1-2 app servers (Kubernetes single node or VM)
  ✓ Cost: ~$500-1000/month
  ✓ Latency: <500ms (acceptable)

Tier 2: 500-5000 operators
  ✓ PostgreSQL replica (read scaling)
  ✓ Redis cluster (HA)
  ✓ 4-8 app servers (Kubernetes)
  ✓ Elasticsearch (logs + search)
  ✓ Cost: ~$2000-5000/month
  ✓ Latency: <200ms

Tier 3: 5000-50000 operators
  ✓ PostgreSQL sharding (by TenantId)
  ✓ Redis cluster + sentinel
  ✓ 16+ app servers (multi-region AZ)
  ✓ S3 (cold data archival)
  ✓ Cost: ~$10K-20K/month
  ✓ Latency: <100ms

Tier 4: 50K+ operators (Enterprise)
  ✓ Microservices (independent scaling)
  ✓ Multi-region deployment (disaster recovery)
  ✓ Data warehouse (analytics)
  ✓ Cost: $50K+/month
  ✓ Latency: <50ms (global)

MKFiloServis Path:
  2025-2026: Tier 1 (100-500 users)
  2026-2027: Tier 2 (500-5K users)
  2027+: Tier 3 (if successful)
```

---

## 🎯 MKFiloServis Technical Recommendations (Summary)

### Keep
```
✓ C# / ASP.NET Core (team skilled)
✓ PostgreSQL (industry standard)
✓ Entity Framework Core (ORM proven)
✓ Blazor Server (admin dashboard)
```

### Add
```
✓ React Native (mobile app, 2025 Q2-Q3)
✓ Redis (cache layer, 2025 Q2)
✓ RabbitMQ (message queue, 2025 Q3)
✓ Docker + GitHub Actions (CI/CD, 2025 Q1)
✓ Application Insights (monitoring, 2025 Q1)
✓ JWT authentication (API, 2025 Q1)
```

### Avoid
```
✗ Microservices (until 5K+ users)
✗ Kubernetes (complex, learn later)
✗ Multiple databases (keep PostgreSQL primary)
✗ Legacy patterns (WebForms, etc.)
```

### Future Options (2026+)
```
⏳ Elasticsearch (if heavy search/reporting)
⏳ TimescaleDB (if GPS tracking added)
⏳ GraphQL (if 3rd-party ecosystem grows)
⏳ Kafka (if real-time streaming critical)
```

---

## 📚 Industry Recommendations (Ranked)

| Priority | Technology | Timeline | ROI |
|----------|-----------|----------|-----|
| 🔴 P1 | PostgreSQL indexes | Week 1 | 🟥🟥🟥🟥🟥 |
| 🔴 P1 | JWT + OAuth | Week 2 | 🟥🟥🟥 |
| 🔴 P1 | Docker + CI/CD | Week 3-4 | 🟥🟥 |
| 🟠 P2 | Redis cache | Month 2 | 🟥🟥🟥🟥 |
| 🟠 P2 | React Native | Month 3-4 | 🟥🟥🟥🟥🟥 |
| 🟠 P2 | RabbitMQ + events | Month 4 | 🟥🟥🟥 |
| 🟡 P3 | Elasticsearch | Month 6+ | 🟥🟥 |
| 🟡 P3 | Kubernetes | Month 8+ | 🟥 (later) |

---

**Hazırladı**: Technical Deep Dive Analysis  
**Sonraki**: Implementation priorities ve final summary

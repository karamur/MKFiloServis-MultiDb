# 🚍 KOAFiloServis

<div align="center">

**Tek PostgreSQL + FirmId İzolasyonu ile Kurumsal Filo Yönetim Platformu**

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![Blazor](https://img.shields.io/badge/Blazor-Interactive%20Server-512BD4?style=flat-square&logo=blazor&logoColor=white)](https://learn.microsoft.com/aspnet/core/blazor/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-68217A?style=flat-square&logo=microsoft&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14%2B-336791?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![Quartz.NET](https://img.shields.io/badge/Quartz.NET-3.x-FB7A24?style=flat-square)](https://www.quartz-scheduler.net)
[![Build](https://img.shields.io/badge/Build-Passing-success?style=flat-square)]()
[![Version](https://img.shields.io/badge/Version-2.0.0-blue?style=flat-square)]()

</div>

---

## ⚡ Nihai Mimari (Haziran 2026)

**KOAFiloServis artık Tenant Database (Database-per-Tenant) mimarisi kullanmamaktadır.**

> 🔄 **32 commit'lik dönüşüm** ile tek PostgreSQL veritabanı + FirmId bazlı satır seviyesi izolasyona geçilmiştir.

### Mimari

```
Tek PostgreSQL: KOAFiloServis
        ↓
Organizasyon (Üstün Holding)
    ├── Üstün Grup
    ├── Üstün Filo
    └── Recep Üstün
        ↓
Firma → Global Query Filter (HasQueryFilter(x => x.FirmaId == CurrentFirmaId))
        ↓
Şube (opsiyonel, SubeId INT NULL)
```

### Kaldırılan Yapılar

- ❌ TenantConnectionStringProvider
- ❌ TenantDbContextFactory
- ❌ MasterDbContext
- ❌ HoldingDbContext
- ❌ Firma bazlı ayrı PostgreSQL veritabanları

### Yeni Yapılar

- ✅ Tek `ApplicationDbContext`
- ✅ `FirmaBaseEntity` — tüm iş tabloları için ortak temel sınıf
- ✅ `Organizasyon` → `Firma` → `Şube` hiyerarşisi
- ✅ `NumaraSerisiService` — atomik, FirmaId bazlı belge numaraları
- ✅ Global Query Filter: `FirmaId` + `IsDeleted`

### Dönüşüm Özeti (Haziran 2026)

| Eski | Yeni |
|------|------|
| Master DB + Tenant DB + Holding DB | Tek `KOAFiloServis` |
| `MasterDbContext` | `ApplicationDbContext` (birleşik) |
| `TenantDbContextFactory` | `IDbContextFactory<ApplicationDbContext>` |
| `ITenantConnectionStringProvider` | Tek connection string |
| FirmId opsiyonel (`int?`) | FirmId zorunlu (`NOT NULL`) — 18 entity |
| MAX() tabanlı numara | `NumaraSerisiService` (atomik) — 11 metod |
| Tenant bazlı audit | FirmaId'li `AktiviteLog` |

## 📌 Genel Bakış

**KOAFiloServis**, personel taşımacılığı firmaları için tasarlanmış kurumsal bir **Blazor Interactive Server** uygulamasıdır. Araç, şoför, güzergâh ve müşteri verilerinden başlayan operasyonel zinciri **puantaj → hakediş → fatura → muhasebe** akışıyla tek platformda yönetir.

> 🏢 Çok-firmalı altyapı, FirmId bazlı veri izolasyonu, rol bazlı yetkilendirme ve AI destekli servislerle kurumsal ölçeğe hazırdır.

### 🎯 Hedef Kullanıcılar

- Personel taşımacılığı işletmeleri (kurumsal servis filoları)
- Karma filo işleten lojistik firmaları (özmal + kiralık + tedarikçi araç)
- Çok-firmalı holding yapılarındaki taşımacılık birimleri

---

## ✨ Öne Çıkan Modüller

| Modül | Yetenekler |
|-------|-----------|
| 🚐 **Filo & Araç** | Tekil araç kartı, plaka geçmişi, özmal/kiralık/tedarikçi tipleri, evrak arşivi |
| 👥 **Personel & Şoför** | Özlük dosyası, ehliyet/MYK/psikoteknik takibi, araç atama, izin yönetimi |
| 🛣️ **Güzergâh & Puantaj** | Kurum/Cari ayrımı, günlük puantaj, toplu onay, otomatik şablon, Excel import/export |
| 💰 **Hakediş & Fatura** | Operasyonel hakediş, fatura kalemleri, tahsilat, banka/kasa, masraf, mali analiz |
| 🗂️ **EBYS** | Gelen/giden/özlük/araç evrak, AI destekli arama, şifreli depolama |
| 🔔 **Uyarı Sistemi** | Evrak/plaka/sözleşme süre takibi, merkezi uyarı paneli |
| 🤖 **AI / Otomasyon** | Araç değerleme, belge sınıflandırma, semantik arama (Ollama / OpenAI) |
| 🛡️ **Kurumsal** | FirmId bazlı veri izolasyonu, rol & yetki, JWT API, audit log |

---

## 🏢 Tek Veritabanı + FirmId İzolasyonu

```
PostgreSQL: KOAFiloServis (tek veritabanı)
        ↓
Organizasyon → Firma → Şube hiyerarşisi
        ↓
Global Query Filter: HasQueryFilter(x => x.FirmaId == CurrentFirmaId)
        ↓
Tüm CRUD işlemleri FirmaId filtreli
```

### Temel Bileşenler

| Bileşen | Görev |
|---------|-------|
| `ApplicationDbContext` | Tek operasyonel DbContext (tüm entity'ler) |
| `IAktifFirmaProvider` | Per-circuit firma context (Scoped) |
| `NumaraSerisiService` | Atomik, FirmaId bazlı belge numaraları |
| `FirmaBaseEntity` | Tüm iş tabloları için ortak temel sınıf |
| `Organizasyon` / `Firma` / `Sube` | Organizasyon hiyerarşisi entity'leri |

---

## 🏗️ Mimari

```
┌──────────────────────────────────────────────────────────────┐
│                     KOAFiloServis.Web                         │
│  ┌─────────────────┐  ┌──────────────┐  ┌────────────────┐   │
│  │ Blazor Server    │  │ REST API     │  │ Quartz Jobs    │   │
│  │ Components/Pages │  │ Controllers  │  │ Backup, Engine │   │
│  │ SignalR
 Hubs     │  │ JWT + Swagger│  │ GunlukOzet     │   │
│  └────────┬─────────┘  └──────┬───────┘  └───────┬────────┘   │
│           └───────────────────┼──────────────────┘            │
│                               ▼                               │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │ Services Katmanı (100+ servis)                            │ │
│  │ Filo · Puantaj · Hakedis · Fatura · Muhasebe · Bordro    │ │
│  │ EBYS · AI · Cache · SecureFile · AuditLog                │ │
│  └──────────────────────────────────────────────────────────┘ │
│                               ▼                               │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │ EF Core 10                                               │ │
│  │ ApplicationDbContext (Tek DbContext)                      │ │
│  │ Global Query Filter (FirmaId + IsDeleted)                 │ │
│  └──────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
                                ▼
┌──────────────────────────────────────────────────────────────┐
│ PostgreSQL 14+ (varsayılan) · SQLite · SQL Server · MySQL    │

└──────────────────────────────────────────────────────────────┘
```

---

## 🚀 Hızlı Başlangıç

### Önkoşullar

| Bileşen | Versiyon |
|---------|----------|
| .NET SDK | 10.0 |
| PostgreSQL | 14+ |
| `dotnet-ef` | 9.x+ |

### Geliştirme

```bash
git clone https://github.com/karamur/KOAFiloServis-MultiDb.git
cd KOAFiloServis-MultiDb
dotnet build && dotnet test
dotnet run --project KOAFiloServis.Web
```

Uygulama **`https://localhost:5001`** adresinde başlar.

### Varsayılan Giriş

| Kullanıcı | Şifre | Rol |
|-----------|-------|-----|
| `admin` | `admin123` | Admin |

> ⚠️ Production ortamında şifreyi değiştirin.

---

## ⚙️ Yapılandırma

### Temel Ayarlar

```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=KOAFiloServis;Username=postgres;Password=***"
  },
  "Jwt": {
    "Secret": "<min-32-char-secret>",
    "Issuer": "KOAFiloServis",
    "Audience": "KOAFiloServis-API",
    "ExpirationHours": 8
  },
  "GpsApi": { "ApiKey": "<gps-device-api-key>" },
  "Cache": { "Provider": "Memory" },
  "Backup": { "Enabled": true, "Path": "/var/koa/backups", "RetentionDays": 30 }
}
```

> 🔐 Production'da environment variable veya secret manager kullanın.

### Veritabanı Bağlantısı (`dbsettings.json`)

İlk kurulumda hedef veritabanı:

```json
{
  "Provider": 2,
  "Host": "localhost",
  "Port": 5432,
  "DatabaseName": "KOAFiloServis",
  "Username": "postgres",
  "Password": "***"
}
```

| Provider | Değer |
|----------|-------|
| PostgreSQL | 2 |
| SQLite | 1 |
| SQL Server | 3 |
| MySQL | 4 |

---

## 📦 Kurulum & Deploy

### Setup Paketi (Inno Setup)

```bash
iscc setup/Setup.iss          # Tam: Web + Lisans + DataSync (129 MB)
iscc setup/MusteriSetup.iss   # Müşteri: Web + DataSync (95 MB)
```

Çıktı: `setup/output/v1.0.24/KOAFiloServisKurulum-1.0.24.exe`

### Kurulum

1. Setup exe'sini **yönetici olarak** çalıştır
2. IIS + .NET Hosting Bundle otomatik kurulum (opsiyonel)
3. IIS Site + AppPool otomatik yapılandırma (`localhost:5190`)
4. Firewall kuralı (opsiyonel)

Kurulum dizini: `C:\KOAFiloServis`

### Güncelleme

Setup mevcut kurulumu otomatik algılar → IIS durdurulur → DB yedeklenir → dosyalar güncellenir (konfigürasyon korunur) → IIS başlatılır.

### Manual Deploy

```bash
dotnet publish KOAFiloServis.Web -c Release -o ./publish-prod
# publish-prod dizinini sunucuya kopyala
```

---

## 🗄️ Veritabanı & Migration

- **EF Core migration'ları** → Shared DB şema evrimi
- **MigrationHelper sınıfları** → Idempotent raw SQL (tek veritabanı)
- **Startup pipeline** → MasterDatabase → DbInitializer → ApplyMigrations

### Yedekleme

```bash
pg_dump -h <host> -U <user> -d KOAFiloServis_Master -F c -f master-$(date +%Y%m%d-%H%M).dump
```

> ⚠️ Yıkıcı migration'lardan önce backup zorunludur.

---

## 🔐 Güvenlik

- ASP.NET Core Identity + cookie/JWT dual auth
- `AuthorizeRouteView` → tüm sayfalar varsayılan olarak korumalı
- Rol bazlı yetkilendirme (Admin, Operasyon, Muhasebeci, HoldingYoneticisi)
- `IFirmaTenant` global query filter → otomatik tenant izolasyonu
- `BordroService`, `CariService` → servis katmanı FirmaId kontrolü
- `SecureFileService` → AES-256-GCM disk şifreleme
- `AuditLogService` → tüm CRUD işlemlerinin kaydı
- GPS API key doğrulaması (`GpsApi:ApiKey`)
- SignalR hub `[Authorize]` koruması
- Soft-delete ile kalıcı veri kaybı önleme

---

## 🔄 Veri Aktarımı

### PostgreSQL → SQLite

`KOAFiloServis.DataSync.exe` GUI/CLI ile PostgreSQL ↔ SQLite aktarımı.

### DestekCRMServisBlazorDb → KOAFiloServis

İlk kurulumda `dbsettings.json` hedef veritabanını gösterir. `DbInitializer` otomatik olarak bağlanır, EF Core migration'larını uygular, rolleri ve admin kullanıcısını seed eder. Mevcut veriler korunur.

---

## 🧪 Test

```bash
dotnet test  # 363 test, < 1 saniye
```

| Tür | Çatı |
|-----|------|
| Birim | xUnit + Moq + FluentAssertions |
| E2E | Playwright + Selenium |

---

## 🧰 Teknoloji Yığını

| Katman | Teknoloji |
|--------|-----------|
| Runtime | .NET 10.0 |
| UI | Blazor Interactive Server, Bootstrap 5 |
| ORM | EF Core 10 (Npgsql, Sqlite, SqlServer, MySQL) |
| Cache | Redis + InMemory |
| Jobs | Quartz.NET 3.x |
| Doküman | ClosedXML, EPPlus, QuestPDF |
| AI | Microsoft.Extensions.AI, OllamaSharp |
| Auth | ASP.NET Core Identity + JWT |
| Test | xUnit, Moq, FluentAssertions, Playwright |
| Setup | Inno Setup 6 |
| Lisans | WinForms (.NET 10) |

---

## 📂 Proje Yapısı

```
KOAFiloServis-MultiDb/
├── KOAFiloServis.Web/          # Ana Blazor uygulaması
│   ├── Components/Pages/       # Modül sayfaları
│   ├── Controllers/            # REST API (JWT)
│   ├── Services/               # İş katmanı (100+ servis)
│   ├── Data/                   # DbContext, Migration
│   ├── Hubs/                   # SignalR
│   └── Jobs/                   # Quartz job'ları
├── KOAFiloServis.Shared/       # Entity, DTO, kontratlar
├── KOAFiloServis.DataSync/     # Veri aktarım aracı
├── KOAFiloServis.LisansDesktop/# Lisans yöneticisi
├── KOAFiloServis.Tests/        # Test suite
├── setup/                      # Inno Setup installer
└── docs/                       # Mimari dokümanlar
```

---

## 🗺️ Yol Haritası

- [ ] MAUI şoför uygulaması
- [ ] e-Fatura / e-Arşiv entegrasyonu
- [ ] AI puantaj anomali tespiti
- [ ] Çoklu dil desteği (i18n)
- [x] Faz 4 — IFirmaTenant temizliği ✅ (Haziran 2026: 10/10 kritik risk kapatıldı)
- [x] Faz 5 — Holding konsolidasyon ✅ (BUTCE + FK + Quartz job)

---

## 🤝 Katkıda Bulunma

```bash
git checkout -b feature/yeni-ozellik
dotnet build && dotnet test
git commit -m "feat(modul): kisa aciklama"
git push origin feature/yeni-ozellik
```

Commit konvansiyonu: `<tip>(<modul>): <aciklama>` — `feat`, `fix`, `refactor`, `tenant`, `docs`, `chore`, `test`

---

## 📄 Lisans

© **Karamur Yazılım**. Tüm hakları saklıdır. İzinsiz kopyalama, dağıtma veya türev çalışma üretilmesi yasaktır.

---

<div align="center">

**KOA Filo Servis** — Operasyondan muhasebeye, filodan hakedişe tek panel.

<sub>.NET 10 · Blazor · PostgreSQL · Quartz · SignalR</sub>

</div>

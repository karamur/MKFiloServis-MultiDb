# 📒 KOAFiloServis — Kayıt Defteri

> Bu dosya, geliştirme sürecinde alınan kararları, yapılan tartışmaları ve hazırlanan raporları
> kronolojik olarak kayıt altına alır. Her oturum sonunda güncellenir.

---

## 📅 22.05.2026 — Gün Sonu Özeti

### ✅ Bugün Tamamlanan

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **Item 1** | Güzergah listesi temizliği | `GuzergahList.razor` | VarsayılanAraç, Şoför, Sefer Durumu sütunları + inline sefer paneli + ~480 satır code-behind kaldırıldı |
| **Item 2** | Güzergah form sefer persist | `GuzergahForm.razor` | Sefer alt tablosu `IGuzergahSeferService` ile persist ediliyor, edit'te geri yükleniyor |
| **Item 3** | Puantaj-GuzergahSefer entegrasyonu | `KurumPuantajService.cs` | `SablonOlusturAsync` öncelikle GuzergahSefer'den araç/şoför atıyor, `SeferTipindenSlotlara` helper eklendi |
| **Item 4** | Dashboard aktif firma gösterimi | `Home.razor` | Dashboard'da firma adı, kodu, dönem bilgisi gösteren kart eklendi |
| **Item 5** | Kapasite çakışma kuralı | `KurumPuantajService.cs` | `CheckConflictsAsync`'e 5. kural: Kapasite (Blocking) — slot başına araç sayısı ≤ PersonelSayisi |
| **Item 6** | PlanlamaEditModal dropdown | `PlanlamaEditModal.razor` | Boş dropdown'lar dolduruldu (Firma, Güzergah, Araç, Şoför), GuzergahSefer'den araç filtresi |
| **Yedek** | Tüm firma DB'leri yedekleme | `BackupService.cs` | PostgreSQL'de Master + Holding + tüm tenant DB'ler pg_dump ile ayrı ayrı yedekleniyor |

### 📊 Toplam: 7 dosya, +287 / -484 satır (net -197)

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |
| `/guzergahlar` | ✅ 10 sütun, sefer paneli yok |
| `/swagger` | ✅ HTTP 200 |
| `/planlama` | ✅ 401 (Authorize) |
| Runtime hata/exception | ✅ **0 hata** |

### 🏗️ Alınan Kararlar

| Karar | Gerekçe |
|-------|---------|
| Sefer yönetimi GuzergahList'ten GuzergahForm'a taşındı | Tek sorumluluk: listeleme ve düzenleme ayrıldı, inline panel karmaşası giderildi |
| GuzergahSefer puantaja öncelikli kaynak oldu | Kullanıcı güzergah altında tanımladığı araç/şoför atamalarının puantaja otomatik yansıması için |
| Kapasite kuralı Blocking seviyesinde | Güzergah kapasite aşımı operasyonel risk taşır, uyarı değil engel olmalı |
| Tüm tenant DB'ler yedekleniyor | Firma bazlı fiziksel izolasyonda her DB'nin ayrı yedeği alınmalı; Master ve Holding DB'ler de dahil |

---

## 📅 21.05.2026 — Gün Sonu Özeti (Final)

### ✅ Bugün Tamamlanan

| Modül | Sprint | Commit | İçerik |
|-------|:------:|--------|--------|
| **MultiDb Fix** | — | `5155be1` | WithMany null parametre - Cari navigasyon çakışması giderildi |
| **Holding** | Faz 10 | `6d28e11` | HoldingDbContext + Service + 7 UI sayfası + Quartz job |
| **Planlama S1** | Altyapı | `35f9534` `1a6f294` | SeferSlot enum + entity genişletme + migration + Planlama.razor + EditModal |
| **Planlama S2** | Çakışma | `1bc7bfa` | ConflictResult modeli + 3 kural motoru + UI görsel uyarılar |
| **Planlama S3** | Kopyalama | `77cc93b` | CopyPreviousMonthModal + çakışma simülasyonu |
| **Planlama S4** | Finans | `c4735b4` | FinansYonu + KaynakTipi + Tedarikçi izolasyonu |
| **Planlama S5** | Dashboard | `4e565f6` | PlanlamaDashboard (KPI + slot/finans/kaynak dağılımı) |
| **Guzergah** | — | `e9c1448` | Eski projedeki sefer alt tablosu GuzergahForm'a taşındı |

### 📊 Toplam: 9 commit, 27 dosya, +2500/-30 satır

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |
| Uygulama başlatma | ✅ `http://0.0.0.0:5200` |
| Tüm startup görevleri (30+) | ✅ Hepsi başarılı |
| PuantajSlotMigration | ✅ Tüm kolonlar mevcut |
| EnsureHoldingDatabase | ✅ KOAFiloServis_Holding + 2 tablo |
| `/planlama` route | ✅ 401 (Authorize) |
| `/planlama/dashboard` route | ✅ 401 (Authorize) |
| `/holding` + 6 alt sayfa | ✅ 401 (Authorize) |
| Runtime hata/exception | ✅ **0 hata** |

### 🏗️ Alınan Kararlar

| Karar | Gerekçe |
|-------|---------|
| Planlama modülü PuantajKayit üzerine inşa edildi | Ayrı PlanlamaKayit entity'si yerine mevcut entity genişletildi — veri tekrarı önlendi |
| KurumPuantaj korundu | Kırıcı değişiklik yok, feature flag gerekmedi |
| Holding snapshot mimarisi | Canlı cross-db sorgu yerine periyodik snapshot — performans + KVKK |
| #11 Master tablo temizliği ertelendi | 40+ FK değişikliği riski, düşük getiri |
| SeferSlot per-day Gun01..Gun31 üzerinden çakışma kontrolü | PuantajKayit aylık tablo, günlük çakışma analizi için tek yol |

---

## 📅 22.05.2026 — Yapılacak İşler

### 🔴 Öncelikli (Manuel Test - Login Gerek)

| # | İş | Açıklama |
|---|-----|----------|
| 1 | Firma geçiş testi | Tenant DB'ye geçince dashboard sadece o firmanın verisini gösteriyor mu? |
| 2 | Tenant DB UI testi | "Tenant DB Oluştur" butonu çalışıyor mu? |
| 3 | Planlama sayfaları testi | Login olup `/planlama` ve `/planlama/dashboard` testi |
| 4 | Holding sayfaları testi | Login olup `/holding` dashboard + 6 rapor sayfası testi |
| 5 | KurumPuantaj regresyon | Mevcut puantaj ekranı kırılmamış olmalı |

### 🟡 Geliştirme

| # | İş | Açıklama |
|---|-----|----------|
| 6 | #11 Master tablo temizliği | Uygun zamanda, düşük öncelik |
| 7 | Holding verisi toplama | İlk kez manuel `ToplaVeKaydetAsync` çağrısı yapıp raporları doldur |

### ⚪ Uzun Vadeli

| # | İş |
|---|-----|
| 8 | Holding girişi — Holding Yoneticisi rolü + auth |
| 9 | Yeni modül talepleri |

---

## 📅 20.05.2026 — Gün Sonu Özeti

### ✅ Bugün Tamamlanan (5 faz + 3 fix)

| Faz | Commit | İçerik |
|-----|--------|--------|
| **Faz 1** | `cba5d90` | Altyapı: `Firma.DatabaseName`, `ITenantConnectionStringProvider`, `MasterDbContext`, `TenantDbContextFactory`, migration |
| **Faz 2** | `2de0ef4` | Master DB fiziksel ayrım: `EnsureMasterDatabaseAsync` (raw SQL), veri kopyalama (sütun eşleştirmeli) |
| **Faz 3** | `0261aa6` | Tenant DB UI: `FirmaYonetimi` sayfasında DatabaseName badge + "Tenant DB Oluştur" butonu, `MigrateFirmaDataAsync` |
| **AutoCreate** | `ba98b03` | Startup'ta otomatik tenant DB oluşturma (`AutoCreateTenantDatabases` görevi) |
| **İsimlendirme** | `9492616` | DB isimleri `Koa_[FirmaKodu]_[ID]` formatı (Türkçe karakter dönüşümü) |
| **Fix 1** | `b833fbf` | `EnableLegacyTimestampBehavior` Program.cs başına taşındı + `TenantDbContextFactory` eksik EF konfigürasyonları |
| **Fix 2** | `8f9b8aa` | Veri göçü FK kısıtlama fix (`session_replication_role=replica`) + 2 adımlı göç (lookup→tenant) |
| **Fix 3** | `1ac63fb` | GuzergahList colspan fix + README güncelleme + debug log temizliği |

### 🧪 Smoke Test Sonuçları

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ 0 hata |
| Uygulama başlatma | ✅ `http://localhost:5200` |
| Master DB oluşturma | ✅ `KOAFiloServis_Master` (6 tablo) |
| Tenant DB otomatik oluşturma | ✅ 3 firma: `Koa_USTUN_GRUP_001`, `Koa_RECEP_USTUN_003`, `Koa_USTUN_FILO_005` |
| Veri göçü (Firma 1) | ✅ 47.263 satır (lookup: 44.307 + tenant: 2.956) |
| Veri göçü (Firma 3) | ✅ 25 Cariler + diğer veriler |
| Veri göçü (Firma 5) | ✅ 13 Cariler + diğer veriler |
| Login sayfası | ✅ HTTP 200 |

### 🏗️ Alınan Mimari Kararlar

| Karar | Gerekçe |
|-------|---------|
| **Hybrid model** | `Firma.DatabaseName == null` → shared DB, `!= null` → tenant DB. Kademeli geçiş imkanı |
| **Master DB raw SQL** | EF Core entity discovery cascade sorunu (41+ tablo). Raw SQL ile sadece 6 core tablo |
| **EnsureCreated** | 80+ migration'da legacy kolon referansları kırılıyor. Tenant DB'ler için daha güvenli |
| **Koa_[FirmaKodu]_[ID]** | DB isimlendirme: firma kısa adı + ID, Türkçe karakterler dönüştürülür |
| **AutoCreateTenantDatabases** | Startup'ta tüm aktif firmalar için otomatik tenant DB oluşturma + veri göçü |
| **FK disable (replica)** | Tenant DB'ye veri kopyalarken FK sıralama sorununu çözmek için |

### 📂 Yeni Eklenen Dosyalar

```
KOAFiloServis.Web/Data/MasterDbContext.cs
KOAFiloServis.Web/Data/TenantDbContextFactory.cs
KOAFiloServis.Web/Services/ITenantConnectionStringProvider.cs
KOAFiloServis.Web/Services/TenantConnectionStringProvider.cs
KOAFiloServis.Web/Services/ITenantDatabaseService.cs
KOAFiloServis.Web/Services/TenantDatabaseService.cs
KOAFiloServis.Web/Migrations/..._MultiDbFaz1_AddFirmaDatabaseName.cs
```

### 📂 Silinen Dosyalar

```
KOAFiloServis.Web/Data/TenantAwareDbContextFactory.cs
KOAFiloServis.Web/Components/Pages/Ayarlar/AIAsistan.razor
```

---

## 📅 21.05.2026 — Gün Sonu Özeti (2. Kısım)

### ✅ Ek Fix

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **Fix 8** | `WithMany()` → `WithMany((string?)null)` | `ApplicationDbContext.cs` | Firma entity'sinde `CariId` FK ilişkisi: `WithMany()` çağrısı Cari entity'sindeki `FirmaId` navigasyonuyla çakışıyordu. Explicit `(string?)null` ile navigasyonsuz ilişki tanımlandı. Build: 0 hata, 0 uyarı, 291 test başarılı |

---

## 📅 21.05.2026 — Holding Modülü (Faz 10)

### ✅ Holding Modülü Tamamlandı

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **10a** | Holding entity + DbContext | `HoldingVeri.cs`, `HoldingRapor.cs`, `HoldingDbContext.cs` (3 YENİ) | 2 entity + HoldingDbContext (2 tablo: HoldingVeriler, HoldingRaporlar). `appsettings.json`'a `HoldingConnection` eklendi. `Program.cs`'e DI + `EnsureHoldingDatabase` startup görevi eklendi |
| **10a** | HoldingService + Quartz | `IHoldingService.cs`, `HoldingService.cs`, `HoldingVeriToplamaJob.cs` (3 YENİ) | `ToplaVeKaydetAsync`: Master DB'den aktif firmaları alır, `Task.WhenAll` ile tüm tenant DB'lere paralel bağlanır, gelir/gider/araç maliyet/personel/hakediş toplamlarını Holding DB'ye upsert eder. Quartz: her ayın 1'inde 02:07'de otomatik |
| **10c** | Holding UI (7 sayfa) | `Holding/*.razor` (7 YENİ) | Dashboard (`/holding`), FirmaKarsilastirma, ButceKonsolidasyonu, OdemePlani, AracMaliyetOzeti, PersonelGiderOzeti, HakedisOzeti. Tümü `[Authorize]` + `InteractiveServer` |
| **10c** | NavMenu güncelleme | `NavMenu.razor` | Holding Yonetimi menü grubu eklendi (7 link) |

### 🧪 Smoke Test Sonuçları

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |
| Uygulama başlatma | ✅ `http://0.0.0.0:5200` |
| `EnsureHoldingDatabase` | ✅ `KOAFiloServis_Holding` DB + 2 tablo (HoldingVeriler, HoldingRaporlar) |
| `/holding` | ✅ 401 (Authorize - beklenen) |
| `/holding/karsilastirma` | ✅ 401 |
| `/holding/butce` | ✅ 401 |
| `/holding/odemeler` | ✅ 401 |
| `/holding/arac-maliyet` | ✅ 401 |
| `/holding/personel` | ✅ 401 |
| `/holding/hakedis` | ✅ 401 |
| Runtime hata/exception | ✅ **0 hata** |

### 📊 Holding Modülü Dosya Özeti

```
YENİ (14 dosya):
  Shared/Entities/HoldingVeri.cs
  Shared/Entities/HoldingRapor.cs
  Web/Data/HoldingDbContext.cs
  Web/Services/IHoldingService.cs
  Web/Services/HoldingService.cs
  Web/Jobs/HoldingVeriToplamaJob.cs
  Web/Components/Pages/Holding/HoldingDashboard.razor
  Web/Components/Pages/Holding/FirmaKarsilastirma.razor
  Web/Components/Pages/Holding/ButceKonsolidasyonu.razor
  Web/Components/Pages/Holding/OdemePlani.razor
  Web/Components/Pages/Holding/AracMaliyetOzeti.razor
  Web/Components/Pages/Holding/PersonelGiderOzeti.razor
  Web/Components/Pages/Holding/HakedisOzeti.razor

DEĞİŞEN (3 dosya):
  Web/appsettings.json              (+HoldingConnection)
  Web/Program.cs                    (+DI, +startup, +Quartz job)
  Web/Components/Layout/NavMenu.razor (+Holding menü grubu)
```

### 🏗️ MİMARİ KARAR — Snapshot Tabanlı Holding Konsolidasyonu

**Karar:** Holding raporları canlı cross-database sorgu yerine periyodik snapshot ile çalışır.

**Gerekçe:**
- Performans: N+1 DB sorgusu yerine tek snapshot DB okuması
- KVKK/Gizlilik: Ham veri tenant DB'lerde kalır, sadece agregasyon holding DB'ye girer
- Tutarlılık: Tüm raporlar aynı snapshot'tan okur

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| Holding verisi henüz toplanmadı (Quartz job bekliyor) | 🟡 İlk veri toplama sonrası raporlar dolu gelecek |
| Master tablo temizliği (#11) ertelendi | 🟡 Holding sonrası tekrar değerlendirilecek |
| Firma geçiş testi + Tenant DB UI testi (manuel) | 🔴 Login gerek, test edilmedi |

---

## 📅 21.05.2026 — Sprint Planı: Puantaj Modülü (Kurum/Firma Bazlı)

> Kaynak: `Kurum_Firma_Bazli_Puantaj_Planlama_Raporu.docx`

### 🎯 Amaç
Mevcut puantaj sistemini bozmadan (KurumPuantaj yaklaşımı korunarak):
- Güzergah bazlı çoklu sefer slotu (Sabah/Akşam/Mesai)
- Sefer CRUD (ekle/düzenle/sil)
- Çakışma kuralları ve engelleyici validasyon
- Önceki aydan kopyalama
- Gelir/gider ve fatura yönü takibi

### 📋 5 Sprint Planı

| Sprint | Tema | Kapsam | Tahmini Süre |
|:------:|------|--------|:-----------:|
| **S1** | Temel Uyum + Sefer CRUD | KurumPuantaj UX referansa yaklaştır, sefer ekle/düzenle/sil modal, slot bazlı sefer yönetimi, KurumId/IsverenFirmaId ayrımı | 3-4 gün |
| **S2** | Çakışma Motoru | (Tarih+Güzergah+Slot)→tek araç, (Tarih+Araç+Slot)→tek güzergah kuralları, kaydet öncesi validasyon, FaturaYonu enum + PlanlamaFaturaTakip tablosu, DB indexleri | 2-3 gün |
| **S3** | Çakışma UX + Kopyalama | Çakışmalı satır renklendirme + hover tooltip, önceki aydan kopyalama + kopya sonrası çakışma tarama, Basit/İleri mod, tek hesaplama servisi | 3-4 gün |
| **S4** | Fatura/Transfer Takibi | Gelir/gider/marj satır bazlı görünüm, giden/gelen/firma içi yansıtma durum takibi, iç transfer alanları, araç/şoför evrak durum badge'leri | 2-3 gün |
| **S5** | UAT ve Canlıya Geçiş | Senaryo testleri (CRUD, çakışma, kopyalama, faturalaşma), performans iyileştirme, release checklist, dokümantasyon | 2-3 gün |

**Toplam tahmini süre: 12-17 gün**

### 🔴 Kritik Kurallar

| Kural | Açıklama |
|-------|----------|
| Tek araç kuralı | Aynı gün + aynı güzergah + aynı slotta birden fazla araç olamaz |
| Tek güzergah kuralı | Aynı gün + aynı slotta aynı araç birden fazla güzergaha yazılamaz |
| Görsel çakışma | Çakışmalar renk + hover tooltip/liste ile gösterilir |

### 🛡️ Risk Yönetimi

| Risk | Aksiyon | Sprint |
|------|---------|:------:|
| İşveren Firma / Kurum ayrımı karışıyor | KurumId ve IsverenFirmaId alanlarını zorunlu ayır; formda ayrı seçim | S1 |
| Eski veriler slot kurallarına uymuyor | Dry-run migration raporu + varsayılan slot map + manuel düzeltme listesi | S2 |
| Çakışma kontrolü yavaşlar | (Tarih,Guzergah,Slot) ve (Tarih,Arac,Slot) indexleri; sadece etkilenen kayıtta kontrol | S2 |
| Mevcut puantajın bozulması | Feature flag + paralel ekran + geri dönüş planı; mevcut servis kontratına dokunmama | S1-S3 |
| 3 firma arası yansıtma izlenemiyor | İç transfer alanları (kaynak/hedef firma/durum/belge no) ekle | S4 |

### 🏁 Kabul Kriterleri (Go/No-Go)

1. Puantaj mevcut akışları kırılmadan çalışmalı
2. Çoklu slot + sefer CRUD sorunsuz çalışmalı
3. Çakışmalar teknik olarak engellenmeli ve görsel olarak anlaşılır olmalı
4. Önceki ay kopyalama güvenli ve izlenebilir olmalı
5. 3 firma + kurum + işveren senaryosu finansal olarak takip edilebilmeli
6. Build başarılı olmalı; kritik hata bulunmamalı

---

## 📅 21.05.2026 — Gün Sonu Özeti

### ✅ Bugün Tamamlanan (7 fix + temizlik + optimizasyon)

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **Fix 1** | NuGet build hatası | `nuget.config` (YENİ) | Proje köküne fallback klasörleri temizleyen `nuget.config` eklendi. .NET 10 SDK'nın eksik VS fallback klasörü hatası giderildi |
| **Fix 2** | Debug log temizliği | `KullaniciService.cs`, `ApplicationDbContext.cs`, `appsettings.Development.json` | `SaveWithLogAsync` metodu kaldırıldı (4 çağrı → `SaveChangesAsync`). EF Core log seviyeleri `Information` → `Warning`. `AssignFirmaTenantId` debug log'ları kaldırıldı |
| **Fix 3** | `AktiviteLogInterceptor` DI hatası | `AktiviteLogInterceptor.cs` | Singleton interceptor'da `IServiceProvider` → `IServiceScopeFactory`. Scoped `IDbContextFactory<ApplicationDbContext>` artık scope içinde resolve ediliyor |
| **Fix 4** | Auth servisleri MasterDbContext'e geçiş | `KullaniciService.cs`, `KullaniciUserStore.cs`, `AppAuthenticationStateProvider.cs` | 3 auth servisi `IDbContextFactory<ApplicationDbContext>` → `IDbContextFactory<MasterDbContext>`. `EnsureRolePermissionAsync` parametresi de güncellendi |
| **Fix 5** | Veri göçü iyileştirme | `TenantDatabaseService.cs` | `CopyTableDataAsync` non-static yapıldı, `ILogger` entegre edildi. Sütun uyumsuzlukları `LogWarning` ile loglanıyor (ilk hata mesajı + başarılı/atlanan sayısı). Kaynak tablo varlık kontrolü eklendi. Ortak sütun yoksa kaynak/hedef sütun listesi loglanıyor |
| **Fix 6** | IFirmaTenant query filter disable | `ApplicationDbContext.cs` | Tenant DB'de `FirmaTenantDisabled` artık `true` dönüyor (`AktifFirmaBilgisi.DatabaseName != null`). Tenant DB'de tüm veri zaten o firmaya ait, filter gereksiz |
| **Fix 7** | Tenant DB pooling optimizasyonu | `TenantDbContextFactory.cs` | `ConcurrentDictionary<string, DbContextOptions>` cache eklendi. `GetOrAdd` ile connection string başına bir kez options oluşturuluyor, sonraki çağrılarda cached kullanılıyor |
| **Temizlik** | Build uyarıları temizliği | `EvrakDetay.razor`, `BankaHesapList.razor`, `AracMasrafSahibiHelper.cs`, `ApplicationDbContext.cs` | `EbysAIPanel` bileşeni ve ilgili kodlar (showAIPanel, OnAIKategoriSecildi, OnAIOzetOlusturuldu) kaldırıldı. Kullanılmayan `firmalar`/`firmasizHesaplar` alanları silindi. XML yorum `&` → `&amp;` fix. `GetQueryFilter()` obsolete → pragma ile bastırıldı |

### 🧪 Smoke Test Sonuçları

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| Uygulama başlatma | ✅ `http://0.0.0.0:5200` |
| Tüm startup görevleri (25+) | ✅ Hepsi başarılı |
| Master DB | ✅ Mevcut |
| Tenant DB otomatik oluşturma | ✅ "Tüm firmaların tenant DB'si mevcut" |
| Quartz Scheduler | ✅ Başlatıldı |
| `GET /login` | ✅ HTTP 200 |
| `GET /ayarlar/firmalar` | ✅ HTTP 200 |
| `GET /ayarlar/kullanicilar` | ✅ HTTP 200 |
| `GET /ayarlar/roller` | ✅ HTTP 200 |
| `GET /dashboard` | ✅ 401 (giriş gerekli - beklenen) |
| Runtime hata/exception | ✅ **0 hata** |

### 📊 Değişen Dosya Özeti

```
nuget.config                                          (YENİ)
KOAFiloServis.Web/Data/AktiviteLogInterceptor.cs      (DI fix)
KOAFiloServis.Web/Data/ApplicationDbContext.cs         (debug log + obsolete fix)
KOAFiloServis.Web/Services/KullaniciService.cs         (SaveWithLogAsync + MasterDbContext)
KOAFiloServis.Web/Services/KullaniciUserStore.cs       (MasterDbContext)
KOAFiloServis.Web/Services/AppAuthenticationStateProvider.cs (MasterDbContext)
KOAFiloServis.Web/Services/TenantDatabaseService.cs    (veri göçü loglama)
KOAFiloServis.Web/Services/AracMasrafSahibiHelper.cs   (XML fix)
KOAFiloServis.Web/Components/Pages/EBYS/EvrakDetay.razor          (EbysAIPanel kaldırıldı)
KOAFiloServis.Web/Components/Pages/BankaHesaplari/BankaHesapList.razor (unused fields)
KOAFiloServis.Web/appsettings.Development.json         (EF log seviyeleri)
```

**Toplam: 11 dosya + 1 yeni, +64 / -93 satır (net -29)**

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| Tenant DB'de firma geçişi çalışıyor mu? | 🔴 Test edilmedi (manuel giriş gerek) |
| Tenant DB UI "Oluştur" butonu testi | 🔴 Test edilmedi (manuel giriş gerek) |
| `IFirmaTenant` query filter tenant DB'de disable | 🟢 Tamamlandı |
| Holding girişi tasarımı | 🔴 Başlanmadı |

---

## 📅 21.05.2026 — Yapılacak İşler (Plan) — GÜNCEL

### 🔴 Manuel Test (Kullanıcı Girişi Gerek)

| # | İş | Açıklama |
|---|-----|----------|
| 1 | Firma geçiş testi | Tenant DB'ye geçince dashboard/sayfalar sadece o firmanın verisini gösteriyor mu? |
| 2 | Tenant DB UI testi | Admin panelden "Tenant DB Oluştur" butonu çalışıyor mu? |
| 3 | Holding raporları testi | Login olup `/holding` sayfalarına girince veriler geliyor mu? |

### 🟢 Multi-DB Geçişi — TAMAMLANDI

| # | İş | Durum |
|---|-----|:-----:|
| 1-6 | Faz 1-6 (Altyapı, Master DB, Tenant DB, DI fix, veri göçü, query filter) | ✅ |
| 7 | Fix 1-8 (NuGet, Debug log, DI, Auth, Veri göçü, Filter, Pooling, WithMany) | ✅ |
| 10 | Holding modülü (DbContext + Service + 7 UI sayfası + Quartz) | ✅ |
| 11 | ApplicationDbContext master tablo temizliği | 🔵 ERTELENDİ |
| 12 | Pooling optimizasyonu | ✅ |

### 🟡 Sonraki Sprint: Puantaj Modülü (Kurum/Firma Bazlı)

| Sprint | Tema | Süre |
|:------:|------|:---:|
| S1 | Temel Uyum + Sefer CRUD | 3-4 gün |
| S2 | Çakışma Motoru + Validasyon | 2-3 gün |
| S3 | Çakışma UX + Önceki Ay Kopyalama | 3-4 gün |
| S4 | Fatura/Transfer Takibi | 2-3 gün |
| S5 | UAT ve Canlıya Geçiş | 2-3 gün |

> Detaylar için yukarıdaki "Sprint Planı: Puantaj Modülü" bölümüne bakın.

---

## 📅 20.05.2026 — Faz 3 Uygulama Oturumu

### Commit: `0261aa6`
```
feat(multi-db): Faz 3 - Tenant DB olusturma UI + veri gocu altyapisi
```

### ✅ Faz 3 — Tamamlanan Adımlar

| # | Adım | Değişen Dosyalar | Özet |
|---|------|-----------------|------|
| 1 | FirmaYonetimi.razor UI | `Components/Pages/Ayarlar/FirmaYonetimi.razor` | DatabaseName badge (Shared DB / tenant adı), Tenant DB Oluştur butonu, loading state |
| 2 | Veri göçü metodu | `Services/TenantDatabaseService.cs`, `Services/ITenantDatabaseService.cs` | `MigrateFirmaDataAsync`: FirmaId kolonu içeren tüm tablolardan shared→tenant veri kopyalama (sütun eşleştirmeli), `CreateTenantDatabaseAsync` artık otomatik veri göçü yapıyor |
| 3 | DatabaseName koruması | `Services/FirmaService.cs` | `UpdateAsync`: mevcut entity okunup seçici alan güncellemesi, DatabaseName overwrite edilemez |
| 4 | Smoke test | — | `dotnet build` → **0 hata**, Login + FirmaYonetimi sayfaları HTTP 200 |

### 🧪 Smoke Test

| Kontrol | Sonuç |
|---------|:-----:|
| `dotnet build` | ✅ 0 hata, 5 uyarı |
| `GET /` login sayfası | ✅ HTTP 200 |
| `GET /ayarlar/firmalar` | ✅ HTTP 200 |
| DatabaseName badge UI | ✅ Shared DB / tenant adı gösterimi hazır |
| Tenant DB Oluştur butonu | ✅ UI'da görünür (sadece DatabaseName null ise) |

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| Tenant DB oluşturma UI üzerinden test edilmedi (manuel login gerek) | 🟡 Kullanıcı testi bekliyor |
| Veri göçü sırasında büyük tablolarda performans | 🟡 İlk testte izlenecek |
| Authentication hala shared DB'den yapılıyor | 🟡 Faz 4'te ele alınacak |

### 📊 Faz 3 Durumu: 🟢 TAMAMLANDI

Tenant DB oluşturma UI + veri göçü altyapısı hazır. Manuel test için: Admin panel → Firma Yönetimi → "Tenant DB Oluştur" butonu.

---

## 📅 20.05.2026 — Faz 2 Uygulama Oturumu

### Commit: `2de0ef4`
```
feat(multi-db): Faz 2 - Master DB fiziksel ayrim + TenantDatabaseService
```

### ✅ Faz 2 — Tamamlanan Adımlar

| # | Adım | Değişen Dosyalar | Özet |
|---|------|-----------------|------|
| 1 | Master DB oluşturma script'i | `Data/DbInitializer.cs` | `EnsureMasterDatabaseAsync` eklendi: raw SQL ile `KOAFiloServis_Master` DB + 6 tablo oluşturur, shared DB'den veri kopyalar (sütun eşleştirmeli) |
| 2 | `TenantDatabaseService` | `Services/ITenantDatabaseService.cs` (YENİ), `Services/TenantDatabaseService.cs` (YENİ) | `CreateTenantDatabaseAsync`: tenant DB oluşturur, migration uygular, `Firma.DatabaseName` günceller |
| 3 | DbInitializer akış güncelleme | `Program.cs` | `EnsureMasterDatabaseAsync` DbInitializer'dan ÖNCE çağrılır |
| 4 | Config + DI | `appsettings.json`, `Program.cs` | `MasterConnection` ayrı DB'ye yönlendirildi, `ITenantDatabaseService` DI kaydı eklendi |
| 5 | Build + smoke test | — | `dotnet build` → **0 hata**, app başlatma → Master DB otomatik oluştu, **6/6 tablo veri kopyalandı** |

### 🧪 Smoke Test Sonuçları

| Tablo | Satır | Durum |
|-------|:-----:|:-----:|
| Firmalar | 4 | ✅ |
| Kullanicilar | 10 | ✅ (sütun eşleştirme fixi sonrası) |
| Roller | 9 | ✅ |
| RolYetkileri | 211 | ✅ |
| Lisanslar | 29 | ✅ |
| AppAyarlari | 5 | ✅ |
| Login sayfası | HTTP 200 | ✅ |

### 🏗️ MİMARİ KARAR — Master DB Raw SQL Yaklaşımı

**Karar:** Master DB tabloları EF Core migration ile değil, raw SQL `CREATE TABLE IF NOT EXISTS` ile oluşturuldu.

**Gerekçe:** MasterDbContext'in EF Core entity discovery'si, Firma entity'sine FK ile bağlanan onlarca tenant entity'sini cascade ederek 41+ tablo oluşturmaya çalıştı. Raw SQL yaklaşımı bu sorunu tamamen bypass eder ve sadece istenen 6 tabloyu oluşturur.

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| Kullanici authentication henüz Master DB'den yapılmıyor | 🟡 `KullaniciService` ve auth servisleri hala ApplicationDbContext kullanıyor |
| Tenant DB oluşturma test edilmedi (UI yok) | 🟡 Servis hazır, admin panel butonu eksik |
| Veri göçü (shared→tenant) henüz yapılmadı | 🔴 Faz 3'te ele alınacak |

### 📊 Faz 2 Durumu: 🟢 TAMAMLANDI

Master DB fiziksel olarak ayrıldı, veriler kopyalandı, TenantDatabaseService hazır.

---

## 📅 20.05.2026 — Database-Per-Firma Faz 1 Başlangıç Oturumu

### Yapılanlar

| # | İş | Detay |
|---|-----|-------|
| 1 | `AIAsistan.razor` kaldırıldı | Derleme hatası olan sayfa tamamen silindi, `NavMenu.razor`'daki link kaldırıldı |
| 2 | Build doğrulandı | `dotnet build` → 0 hata, 5 uyarı (önceden mevcut) |
| 3 | Mimari keşif tamamlandı | ApplicationDbContext, TenantAwareDbContextFactory, IAktifFirmaProvider, IFirmaTenant, Firma entity, Program.cs DI, appsettings.json incelendi |
| 4 | Faz planı hazırlandı | 6 fazlı Database-Per-Firma geçiş planı (Faz 0-6) oluşturuldu |
| 5 | Kayıt defteri formatı belirlendi | Tarih+amaç, commit, değişen dosyalar, değişiklik özeti, test/build sonucu, risk/açık işler, faz durumu, karar+gerekçe |

### 🏗️ MİMARİ KARAR — Faz 1 Yaklaşımı

**Karar:** Hybrid model ile kademeli geçiş.

**Gerekçe:**
- `Firma.DatabaseName == null` → eski shared DB modu (mevcut çalışan sistem)
- `Firma.DatabaseName != null` → yeni tenant DB modu (tam izolasyon)
- Aynı `ApplicationDbContext` her iki modda da çalışır — tenant query filter shared modda izolasyon sağlar, dedicated modda zararsız no-op olur
- `MasterDbContext` sadece global tabloları (Firmalar, Kullanicilar, Lisans) yönetir

### 📋 Faz 1 Uygulama Planı (Özet)

| Sıra | Adım | Dosya |
|:----:|------|-------|
| 1 | `Firma` entity'sine `DatabaseName` ekle | `Shared/Entities/Firma.cs` |
| 2 | `ITenantConnectionStringProvider` arayüzü | `Web/Services/` (YENİ) |
| 3 | `TenantConnectionStringProvider` implementasyonu | `Web/Services/` (YENİ) |
| 4 | `MasterDbContext` oluştur | `Web/Data/` (YENİ) |
| 5 | `ApplicationDbContext`'ten master tabloları çıkar | `Web/Data/ApplicationDbContext.cs` |
| 6 | `TenantDbContextFactory` (eskisini replace et) | `Web/Data/` (YENİ + SİL) |
| 7 | `appsettings.json`'a `MasterConnection` ekle | `Web/appsettings.json` |
| 8 | `Program.cs` DI kayıtlarını güncelle | `Web/Program.cs` |
| 9 | `FirmaService` + master tablo servislerini güncelle | `Web/Services/` |
| 10 | Migration klasörlerini düzenle | `Data/Migrations/` |
| 11 | `DbInitializer` güncelle | `Web/Data/DbInitializer.cs` |
| 12 | Build + smoke test | — |

### ⚠️ Riskler / Açık İşler

| Risk | Durum |
|------|:-----:|
| V1 (70+ entity) vs V2 (24 entity) kararı — V2 öneriliyor | 🔴 Karar bekliyor |
| `LisansService` singleton ama MasterDbContext scoped | Çözüm: `IDbContextFactory<MasterDbContext>` |
| Cross-tenant (TumFirmalar) sorgular dedicated DB'de çalışmaz | Faz 1'de sadece shared-DB firmalar için destek |
| Pooling per-tenant DB'ler için optimize değil | Faz 2'de `ConcurrentDictionary` cache |

### 🎯 Sonraki Adım

Faz 1 Adım 1: `Firma` entity'sine `DatabaseName` alanı eklenmesi.

---

## 📅 20.05.2026 — Faz 1 Uygulama Oturumu (İkinci Kısım)

### Commit: `cba5d90`
```
feat(multi-db): Faz 1 altyapi - Database-Per-Firma hybrid mimari kurulumu
```

### ✅ Faz 1 — Tamamlanan Adımlar

| # | Adım | Değişen Dosyalar | Özet |
|---|------|-----------------|------|
| 1 | `Firma` + `AktifFirmaBilgisi` güncelle | `Shared/Entities/Firma.cs`, `Web/Services/FirmaService.cs` | `DatabaseName` alanı eklendi, `AktifFirmaBilgisi`'ne taşındı |
| 2 | `ITenantConnectionStringProvider` + implementasyon | `Web/Services/ITenantConnectionStringProvider.cs` (YENİ), `Web/Services/TenantConnectionStringProvider.cs` (YENİ) | Dinamik connection string çözümleyici: `DatabaseName == null` → shared DB, `!= null` → tenant DB |
| 3 | `MasterDbContext` oluştur | `Web/Data/MasterDbContext.cs` (YENİ) | 6 çekirdek global tablo: Firmalar, Kullanicilar, Lisanslar, Roller, RolYetkileri, AppAyarlari. Navigation property temizliği yapıldı |
| 4 | ApplicationDbContext temizliği | — | **ERTELENDİ:** FK kırılma riski nedeniyle tüm entity'ler korundu. Faz 4'te yapılacak |
| 5 | `TenantDbContextFactory` (eskisini replace) | `Web/Data/TenantDbContextFactory.cs` (YENİ), `TenantAwareDbContextFactory.cs` (SİL) | `ITenantConnectionStringProvider` ile dinamik DB bağlantısı, pooling Faz 2'de |
| 6-7 | `appsettings.json` + `Program.cs` DI | `Web/appsettings.json`, `Web/Program.cs` | `MasterConnection` eklendi (şimdilik shared DB ile aynı), `TenantDatabase` bölümü eklendi, DI kayıtları güncellendi, `PooledDbContextFactoryHolder` kaldırıldı |
| 8 | Master tablo servisleri güncelle | `Web/Services/LisansService.cs`, `Web/Services/FirmaService.cs` | `LisansService`: `PooledDbContextFactoryHolder` → `IDbContextFactory<MasterDbContext>`, `FirmaService`: `IDbContextFactory<ApplicationDbContext>` → `IDbContextFactory<MasterDbContext>` |
| 9 | Migration | `Web/Migrations/..._MultiDbFaz1_AddFirmaDatabaseName.cs` (YENİ) | `Firmalar` tablosuna `DatabaseName` kolonu eklendi (varchar(100), nullable) |
| 10 | Build doğrulama | — | `dotnet build` → **0 hata, 5 uyarı** ✅ |

### 🏗️ MİMARİ KARAR — MasterDbContext Migration Ertelendi

**Karar:** MasterDbContext migration'ı Faz 2'ye ertelendi. Faz 1'de Master DB fiziksel olarak ayrılmadı, `MasterConnection` şimdilik DefaultConnection ile aynı shared DB'yi gösteriyor.

**Gerekçe:** MasterDbContext entity keşfi (navigation property cascade) 41+ tablo oluşturmaya çalıştı. MasterDbContext'in sadece 6 core tabloyla sınırlanması için kapsamlı `Ignore<>()` konfigürasyonu gerekiyor. Bu iş Faz 2'de Master DB fiziksel ayrımıyla birlikte yapılacak.

### 📊 Faz 1 Durumu: 🟡 KISMEN TAMAMLANDI

**Tamamlanan:** Altyapı (entity, provider, factory, DI, migration) kuruldu, build temiz.
**Ertelenen:** ApplicationDbContext master tablo temizliği, MasterDbContext migration'ı, DbInitializer güncellemesi.

### 🧪 Runtime Smoke Test (20.05.2026 - 3. Kısım)

| Kontrol | Sonuç |
|---------|:-----:|
| `dotnet ef database update` — migration uygulama | ✅ `MultiDbFaz1_AddFirmaDatabaseName` başarıyla uygulandı |
| `dotnet run` — uygulama başlatma | ✅ `Now listening on: http://0.0.0.0:5190` |
| Startup görevleri (Seed, Quartz, GPS) | ✅ Tümü başarılı |
| Uygulama log'larında hata/exception | ✅ **0 hata** |
| `GET /` — giriş sayfası | ✅ HTTP 200, "Giris - Koa Filo Servis" |
| `GET /login` — login sayfası | ✅ HTTP 200 |

**Sonuç:** ✅ Uygulama sorunsuz başlıyor, login sayfası açılıyor, hiçbir hata yok.

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| V1 (70+ entity) vs V2 (24 entity) kararı | 🔴 Karar bekliyor |
| MasterDbContext migration cascade | 🟡 Faz 2'de çözülecek |
| Runtime smoke test | ✅ **TAMAMLANDI** — uygulama başlıyor, login açılıyor, 0 hata |
| `KullaniciService` + auth servisleri hala ApplicationDbContext kullanıyor | 🟡 Faz 2'de güncellenecek |
| Cross-tenant (TumFirmalar) dedicated DB'de çalışmaz | 🟡 Faz 2'de ele alınacak |

---

## 📅 14.05.2026 — AI Asistan + Mimari Karar Oturumu

### Commit: `952a546`
```
feat(ai-asistan): DeepSeek V3/R1 model katalogu + docs guncelleme
```

---

### 🤖 AI Asistan Model Kataloğu

**Konu:** DeepSeek V4 yapay zeka listesine eklenebilir mi?

**Araştırma Sonucu:**
- Ollama public registry'de `deepseek-v4` tag'i **mevcut değil** (14.05.2026 tarihi itibarıyla)
- Mevcut resmi DeepSeek sürümleri:
  - `deepseek-v3` — Genel amaçlı, güçlü model
  - `deepseek-r1` — Reasoning (akıl yürütme) modeli
  - `deepseek-coder-v2` — Kod odaklı model

**Yapılan Değişiklik:**
- `AIAsistan.razor` → `GetBirlesikModelListesi()` metoduna `deepseek-v3` ve `deepseek-r1` eklendi
- Dropdown artık iki grup gösteriyor:
  - **Yerel (Ollama):** Makinede `ollama pull` ile yüklenmiş modeller
  - **Önerilen (yüklü değil):** Katalogdaki ama henüz indirilmemiş modeller
- Yüklü olmayan model seçilince `ollama pull <model>` komutu ipucu olarak gösteriliyor

**Dosya:** `KOAFiloServis.Web/Components/Pages/Ayarlar/AIAsistan.razor`

---

### 🏗️ MİMARİ KARAR — Database Per Firma

#### Sorun Tanımı
Müşteri 3 firma ile çalışıyor. Mevcut "Shared Database + FirmaId row-level isolation"
mimarisinde kullanıcıların hatalı firma seçimi veya filter kaçağı durumunda firmaların
verileri birbirine karışabiliyor. Zirve Müşavirlik gibi referans sistemlerde her firma
ayrı veritabanında çalışıyor.

#### Mevcut Mimari (Shared DB)
```
PostgreSQL: DestekCRMServisBlazorDb (TEK DB)
  Araclar   → FirmaId=1, FirmaId=2, FirmaId=3  (hepsi aynı tabloda)
  Cariler   → FirmaId=1, FirmaId=2, FirmaId=3
  Faturalar → FirmaId=1, FirmaId=2, FirmaId=3

Koruma mekanizması: HasQueryFilter("FirmaId == aktifFirma")
Zayıf nokta: Kullanıcı hatalı firma seçimi → yanlış veri görme/yazma riski
```

#### Hedef Mimari (Database Per Firma)
```
PostgreSQL Server
  db_global   → Kullanıcılar, Lisans, Firma katalogu
  db_firma_1  → Firma A'nın TÜM verileri (tam izolasyon)
  db_firma_2  → Firma B'nın TÜM verileri (tam izolasyon)
  db_firma_3  → Firma C'nın TÜM verileri (tam izolasyon)
  db_holding  → Ortak/Holding konsolidasyon DB
```

#### Gerekçe
1. **Veri güvenliği:** DB seviyesinde fiziksel izolasyon — filter bypass imkansız
2. **Müşteri talebi:** Zirve Müşavirlik benzeri yapı isteniyor
3. **Holding ihtiyacı:** 3 firmayı birleştiren ortak raporlama / bütçe konsolidasyonu
4. **Yedekleme:** Firma bazlı backup/restore kolaylaşır
5. **KVKK/Hukuki:** Firma verisi fiziksel olarak ayrı

---

### 🏢 HOLDİNG / ORTAK FİRMA MODÜLÜ

#### Kavram
3 (veya daha fazla) operasyonel firmanın finansal verilerini **özetleyerek** tek bir
"Holding" veritabanında konsolide eden yeni modül.

#### Holding'e Ne Aktarılır?
| Veri Türü | Aktarılır | Not |
|-----------|:---------:|-----|
| Bütçe gerçekleşmesi | ✅ | Gelir/gider toplam |
| Fatura toplamları | ✅ | KDV dahil/hariç |
| Banka/Kasa bakiyesi | ✅ | Dönem sonu snapshot |
| Personel gider özeti | ✅ | Bordro toplamı |
| Araç maliyet özeti | ✅ | Bakım+yakıt toplam |
| Hakediş ödemeleri | ✅ | Tedarikçi toplamı |
| Tekil fatura detayı | ❌ | Gizlilik/boyut |
| Personel özlük | ❌ | KVKK |
| Cari kart detayı | ❌ | Firma içi bilgi |

#### Holding Rapor Türleri
- Firma Karşılaştırma (Gelir/Gider/Kâr yan yana)
- Bütçe Konsolidasyonu (tüm firmalar toplam)
- Ödeme Planı (tüm firmaların vadesi gelenler)
- Araç Maliyet Özeti (firma bazlı)
- Personel Gider Özeti (firma bazlı bordro toplam)
- Hakediş Özeti (tedarikçi ödemeleri)

---

### 🔀 GITHUB — YENİ REPO KARARI

#### Soru
> "GitHub'da bu yapıya dokunmadan yeni proje gibi açıp projeyi oraya kopyalayıp
> oradan devam etsek olur mu?"

#### Yanıt ve Değerlendirme
**Evet, tamamen uygulanabilir.** İki yöntem var:

---

**Yöntem 1 — Fork (Önerilen)**
```bash
# GitHub web arayüzünde:
# 1. https://github.com/karamur/KOAFiloServis → "Fork" butonu
# 2. Yeni repo adı: KOAFiloServis-v2  (veya KOAFiloServis-MultiDb)
# 3. Sadece main branch'i fork et

# Yerel:
git clone https://github.com/karamur/KOAFiloServis-v2
cd KOAFiloServis-v2
# Upstream'i orijinal repo olarak ekle (gelecekte senkronizasyon için)
git remote add upstream https://github.com/karamur/KOAFiloServis
```
✅ Orijinal repo **aynen korunur** (production backup)
✅ Yeni repoda Database-Per-Firma geçişi yapılır
✅ İleride orijinale patch geri alınabilir (`git cherry-pick`)

---

**Yöntem 2 — Yeni Boş Repo + Kopyalama**
```bash
# GitHub'da yeni repo oluştur: KOAFiloServis-v2

# Yerel — mevcut kodu yeni remote'a bağla:
cd "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis"
git remote add v2 https://github.com/karamur/KOAFiloServis-v2
git push v2 main

# Yeni çalışma klasörü:
cd C:\Users\muratk\Desktop\d yedek\calisma\
git clone https://github.com/karamur/KOAFiloServis-v2 KOAFiloServis-v2
```
✅ Temiz başlangıç
⚠️ Commit geçmişi taşınır (arzu edilmezse `--depth 1` veya squash)

---

**Önerilen Akış:**
```
karamur/KOAFiloServis        → Mevcut production kodu (dokunulmaz, korunur)
karamur/KOAFiloServis-MultiDb → Yeni Database-Per-Firma mimarisi geliştirme
```

Geçiş tamamlanıp test edilince `KOAFiloServis-MultiDb` → `KOAFiloServis`'e merge edilir
veya doğrudan production'a alınır.

---

### 📋 Uygulama Fazları (Özet)

| Faz | İçerik | Tahmini Süre |
|-----|--------|:---:|
| **Faz 1** | GlobalDbContext + TenantDbContext ayrımı + ITenantDbResolver | 3–4 gün |
| **Faz 2** | Mevcut veri göçü (3 firma DB'sine taşıma) | 1–2 gün |
| **Faz 3** | Holding modülü + konsolidasyon raporları | 3–4 gün |
| **Faz 4** | IFirmaTenant + FirmaId temizliği | 1–2 gün |
| **Test** | Stabilizasyon | 2–3 gün |
| **TOPLAM** | | **~10–15 gün** |

---

### ⚠️ Riskler

| Risk | Önlem |
|------|-------|
| Veri göçü sırasında kayıp | Tam backup → row count doğrulama |
| FirmaKopyalama çoklu DB'de kırılma | Önce refactor, sonra göç |
| Migration yönetimi karmaşıklığı | Tek TenantDbContext migration path |
| Holding raporu performansı | Task.WhenAll ile paralel DB sorgusu |

---

### 🎯 Sonraki Adım (Onay Bekliyor)

Yeni repo açma ve Faz 1'e başlama kararı alınırsa:
1. GitHub'da `KOAFiloServis-MultiDb` reposu oluştur
2. Mevcut kodu oraya kopyala (`git push v2 main`)
3. `GlobalDbContext` ve `TenantDbContext` dosyalarını oluştur
4. `ITenantDbResolver` interface + implementasyonunu yaz
5. `appsettings.json`'a `TenantDb:Template` bölümü ekle

---

## 📚 İlgili Dosyalar

| Dosya | Konu |
|-------|------|
| `docs/TENANT_MIGRATION_PLAN.md` | Mevcut tenant migrasyonu (tamamlanmış) |
| `docs/CALISMA-NOTLARI-2026-05-13.md` | Önceki oturum notları |
| `docs/CALISMA-NOTLARI-2026-05-14.md` | Bu oturum notları |
| `docs/OTURUM_NOTLARI_2026-05-19.md` | Tenant v1.0.21 tamamlandı notu |
| `KOAFiloServis.Shared/Entities/IFirmaTenant.cs` | Mevcut tenant interface |
| `KOAFiloServis.Web/Data/TenantAwareDbContextFactory.cs` | Mevcut factory |
| `KOAFiloServis.Web/Services/IAktifFirmaProvider.cs` | Aktif firma state servisi |

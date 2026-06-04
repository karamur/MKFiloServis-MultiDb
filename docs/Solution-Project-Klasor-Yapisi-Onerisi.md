# KOAFiloServis Solution / Project Klasör Yapısı Önerisi

Bu doküman, KOAFiloServis'in Web + Desktop + Mobile hibrit mimaride ölçeklenebilir şekilde ayrıştırılması için önerilen solution yapısını içerir.

---

## 1. Hedef Solution Yapısı

Önerilen üst seviye solution:

- `KOAFiloServis.sln`

Önerilen proje dağılımı:

### Çekirdek Katman
- `KOAFiloServis.Shared`
  - Entity'ler
  - DTO'lar
  - enum'lar
  - ortak sabitler
  - validation model'leri

- `KOAFiloServis.Domain`
  - iş kuralları
  - domain servisleri
  - aggregate / policy yapıları
  - tenant davranış kuralları

- `KOAFiloServis.Application`
  - use-case servisleri
  - interface'ler
  - transfer orchestration
  - raporlama sözleşmeleri

### Altyapı Katmanı
- `KOAFiloServis.Infrastructure`
  - EF Core DbContext
  - repository implementasyonları
  - tenant connection çözümleme
  - logging
  - cache
  - dosya sistemi / object storage
  - migration / seed yardımcıları

- `KOAFiloServis.Infrastructure.Transfer`
  - legacy DB okuma
  - tenant DB yazma
  - transfer pipeline
  - dry-run altyapısı
  - conflict logging

### Sunum Katmanı
- `KOAFiloServis.Web`
  - Blazor Server UI
  - admin panelleri
  - ERP / muhasebe / bütçe / stok / rapor ekranları

- `KOAFiloServis.TransferDesktop`
  - WPF veya WinUI masaüstü aktarım uygulaması
  - dry-run ekranı
  - firma kodu eşleme ekranı
  - transfer raporu

- `KOAFiloServis.TerminalDesktop`
  - puantaj terminali
  - barkod operasyonları
  - hızlı saha veri girişi

- `KOAFiloServis.Mobile`
  - .NET MAUI
  - sevkiyat
  - saha checklist
  - mobil görevler

### Destek / Test Katmanı
- `KOAFiloServis.Tests`
  - unit test
  - integration test
  - tenant routing test
  - transfer pipeline test

- `KOAFiloServis.PlaywrightTests`
  - web UI smoke testleri

---

## 2. Önerilen Klasör Organizasyonu

### KOAFiloServis.Web
- `Components/Pages`
- `Components/Layout`
- `Components/Shared`
- `Controllers`
- `Services`
- `Data`
- `Jobs`
- `Middleware`
- `HealthChecks`
- `wwwroot/js`
- `wwwroot/css`
- `Docs`

### KOAFiloServis.TransferDesktop
- `Views`
- `ViewModels`
- `Services`
- `Transfer`
- `Validation`
- `Reports`
- `Logs`
- `Configuration`

### KOAFiloServis.TerminalDesktop
- `Views`
- `ViewModels`
- `Services`
- `Barcode`
- `Puantaj`
- `Offline`
- `Sync`

### KOAFiloServis.Mobile
- `Pages`
- `ViewModels`
- `Services`
- `Offline`
- `Sync`
- `Geo`

---

## 3. Sorumluluk Ayrımı

### Web
- yönetim
- muhasebe
- bütçe
- stok
- cari
- raporlama
- tenant yönetimi

### TransferDesktop
- legacy -> tenant veri aktarımı
- dry-run
- FK doğrulama
- transfer logları
- rapor üretimi

### TerminalDesktop
- puantaj terminali
- barkod işlemleri
- cihaz entegrasyonları
- offline işlem desteği

### Mobile
- sevkiyat
- saha aksiyonları
- mobil görevler
- konum bazlı süreçler

---

## 4. Tenant / DB Katmanı Eşlemesi

- `KOAFiloServis_Master`
  - firma
  - kullanıcı
  - lisans
  - rol

- `DestekCRMServisBlazorDb`
  - legacy veri kaynağı
  - sadece transfer amaçlı

- `Koa_[FirmaKodu]_[Id]`
  - firma bazlı runtime tenant DB

- `KOAFiloServis_Holding`
  - konsolidasyon
  - holding raporları

---

## 5. Teknik Ayırma İlkeleri

- Shared içinde yalnızca ortak model bulunmalı
- DbContext implementasyonları Infrastructure altında olmalı
- Web projesi doğrudan legacy transfer mantığı içermemeli
- TransferDesktop, legacy DB erişiminin ana yürütücüsü olmalı
- Tenant DB çözümleme merkezi servis üzerinden yapılmalı
- Runtime CRUD ile transfer pipeline karıştırılmamalı

---

## 6. Önerilen İlk Ayrıştırma Sırası

1. Tenant bağlantı çözümleme servislerini ayır
2. Transfer pipeline'ı ayrı projeye taşı
3. Web içindeki runtime-only servisleri netleştir
4. Desktop transfer uygulamasını bağımsızlaştır
5. Puantaj terminalini ayrı desktop projeye çıkar
6. Mobile senaryolarını MAUI projesinde başlat

---

## 7. Kısa Sonuç

En sağlıklı yapı:
- `Web` = yönetim ve kurumsal operasyon
- `TransferDesktop` = veri taşıma
- `TerminalDesktop` = saha terminali
- `Mobile` = sevkiyat ve mobil saha
- `Master / Tenant / Holding / Legacy` = net ayrılmış DB sorumlulukları

# KOAFiloServis Master Yol Haritası ve DeepSeek Promptu v2

Bu doküman, KOAFiloServis için tek parça, güçlendirilmiş strateji belgesidir.

Amaç:
- ürün mimarisini netleştirmek
- Web + Desktop + Mobile ayrımını kesinleştirmek
- multi-tenant veritabanı mimarisini sabitlemek
- legacy DB kullanım sınırlarını belirlemek
- veri aktarım stratejisini standardize etmek
- DeepSeek'e tek parça güçlü teknik prompt vermek

---

# 1. STRATEJİK VİZYON

KOAFiloServis, çok kiracılı (multi-tenant), modüler, hibrit bir operasyon platformu olarak konumlandırılmalıdır.

## Hedef çalışma modeli
- **Web / Blazor Server**: yönetim, ERP, muhasebe, bütçe, stok, cari, raporlama
- **Desktop / WPF veya WinUI**: puantaj terminali, barkod operasyonları, veri aktarım aracı, offline hızlı giriş
- **Mobile / .NET MAUI**: sevkiyat, saha görevleri, mobil checklist, konum bazlı operasyonlar

Bu mimaride:
- runtime işlemler tenant DB üzerinde çalışır
- legacy DB yalnızca kaynak olur
- veri taşıma kontrollü ve raporlu yapılır
- her firma veri olarak izole edilir

---

# 2. KESİN MİMARİ KURALLAR

## 2.1 Runtime veri erişim kuralı
Runtime sırasında aşağıdaki işlemler:
- SELECT
- INSERT
- UPDATE
- DELETE

yalnızca aktif firmanın tenant veritabanında çalışmalıdır.

## 2.2 Legacy DB kuralı
`DestekCRMServisBlazorDb`:
- runtime CRUD için kullanılmaz
- sadece migration kaynağıdır
- sadece transfer kaynağıdır
- sadece analiz / doğrulama kaynağıdır

## 2.3 Tenant çözümleme kuralı
Akış:
1. kullanıcı giriş yapar
2. aktif firma seçilir
3. firma kaydından `DatabaseName` alınır
4. tenant connection string çözülür
5. `ApplicationDbContext` tenant DB'ye bağlanır

## 2.4 Fallback yasağı
Aşağıdakiler runtime için yasak kabul edilir:
- aktif firma yokken `DefaultConnection` kullanımı
- `DatabaseName` boşken legacy DB fallback
- tenant bulunamazsa sessiz devam etme

## 2.5 Veri aktarımı kuralı
- kaynak DB değiştirilemez
- yalnızca read-only okunur
- hedef tenant DB'ye kontrollü yazılır
- dry-run zorunlu desteklenir
- ON CONFLICT davranışı loglanır
- hata raporu üretilir

---

# 3. VERİTABANI TOPOLOJİSİ

## 3.1 Master DB
Örnek:
- `KOAFiloServis_Master`

İçerik:
- Firmalar
- Kullanicilar
- Roller
- Lisanslar
- tenant eşleme bilgileri
- sistem ayarları

## 3.2 Legacy Source DB
Örnek:
- `DestekCRMServisBlazorDb`

İçerik:
- eski ortak veri havuzu
- taşınacak geçmiş kayıtlar

## 3.3 Tenant DB
Örnek:
- `Koa_USTUN_GRUP_001`
- `Koa_RECEP_USTUN_003`
- `Koa_USTUN_FILO_005`

İçerik:
- Cari
- Muhasebe
- Bütçe
- Stok
- Araç
- Şoför / Personel
- Puantaj
- Fatura
- Operasyon

## 3.4 Holding DB
Örnek:
- `KOAFiloServis_Holding`

İçerik:
- çok firma konsolidasyon özetleri
- yönetici raporları
- KPI ve trend verileri

---

# 4. MODÜL DAĞILIMI

| Modül | Kanal | Teknoloji | Veritabanı | Not |
|---|---|---|---|---|
| ERP | Web | Blazor Server | Tenant DB | Ana yönetim |
| Muhasebe | Web | Blazor Server | Tenant DB | Finansal işlemler |
| Bütçe | Web | Blazor Server | Tenant DB | Tenant bazlı bütçe |
| Stok | Web | Blazor Server | Tenant DB | Operasyon / envanter |
| Cari / CRM | Web | Blazor Server | Tenant DB | Müşteri / tedarikçi |
| Fatura / Proforma | Web | Blazor Server | Tenant DB | Gelir / gider / belge |
| Filo | Web | Blazor Server | Tenant DB | Araç / güzergah / bakım |
| Puantaj Yönetimi | Web | Blazor Server | Tenant DB | Onay / rapor / kontrol |
| Puantaj Terminali | Desktop | WPF / WinUI | Tenant DB | Hızlı saha veri girişi |
| Barkod Operasyonları | Desktop | WPF / WinUI | Tenant DB | Barkod / cihaz entegrasyonu |
| Veri Aktarımı | Desktop | WPF / WinUI | Legacy -> Tenant | Kontrollü veri taşıma |
| Sevkiyat | Mobile | .NET MAUI | Tenant API / Sync | Saha ve teslimat |
| Holding Raporlama | Web | Blazor Server | Holding DB | Konsolidasyon |
| Sistem Yönetimi | Web | Blazor Server | Master DB | Lisans / kullanıcı / firma |

---

# 5. HEDEF SOLUTION YAPISI

## Çekirdek
- `KOAFiloServis.Shared`
- `KOAFiloServis.Domain`
- `KOAFiloServis.Application`

## Altyapı
- `KOAFiloServis.Infrastructure`
- `KOAFiloServis.Infrastructure.Transfer`

## Sunum
- `KOAFiloServis.Web`
- `KOAFiloServis.TransferDesktop`
- `KOAFiloServis.TerminalDesktop`
- `KOAFiloServis.Mobile`

## Test
- `KOAFiloServis.Tests`
- `KOAFiloServis.PlaywrightTests`

---

# 6. FAZ BAZLI MASTER YOL HARİTASI

## Faz 1 — Runtime Tenant Sertleştirme
Amaç: runtime legacy bağımlılığını bitirmek

Teslimatlar:
- tenant connection fallback kaldırılması
- fail-fast davranışı
- DB trace logları
- tenant çözümleme raporu

Başarı kriteri:
- runtime CRUD legacy DB'ye düşmüyor

## Faz 2 — Tenant Health ve Doğrulama
Amaç: tenant DB sağlık durumu görünürlüğü

Teslimatlar:
- tenant validation service
- tablo/şema/seed doğrulama
- eksik firma bootstrap raporu

Başarı kriteri:
- problemli tenantlar raporlanabiliyor

## Faz 3 — TransferDesktop Altyapısı
Amaç: kontrollü veri taşıma aracı kurmak

Teslimatlar:
- firma seçimi ekranı
- dry-run motoru
- transfer log altyapısı
- report generator

Başarı kriteri:
- gerçek taşıma öncesi dry-run raporu alınabiliyor

## Faz 4 — Modül Bazlı Aktarım
Amaç: legacy verileri tenant DB’lere taşımak

Öncelik sırası:
1. lookup / tanım verileri
2. cari
3. personel
4. araç
5. bütçe
6. fatura
7. puantaj
8. operasyon

Başarı kriteri:
- her modül için conflict ve hata raporu üretiliyor

## Faz 5 — Desktop Operasyon Terminali
Amaç: saha hızını artırmak

Teslimatlar:
- puantaj terminali
- barkod ekranları
- offline cache
- retry/sync mekanizması

## Faz 6 — Mobile Saha Modülü
Amaç: sevkiyat ve mobil saha işlemleri

Teslimatlar:
- görev ekranları
- checklist
- konum bazlı akışlar
- offline sync

## Faz 7 — Holding ve Yönetici Konsolidasyonu
Amaç: çok firma yönetsel görünürlük

Teslimatlar:
- holding dashboard
- çok firma karşılaştırma raporları
- konsolidasyon veri pipeline

---

# 7. TRANSFERDESKTOP TEKNİK STANDARDI

## Zorunlu ekranlar
- bağlantı ve konfigürasyon ekranı
- firma eşleme ekranı
- modül seçimi ekranı
- dry-run önizleme ekranı
- transfer yürütme ekranı
- sonuç / hata raporu ekranı

## Zorunlu servisler
- `ITenantResolver`
- `ILegacySourceReader`
- `ITransferValidator`
- `ITransferExecutor`
- `ITransferReportService`
- `IConflictLogger`

## Zorunlu rapor alanları
- firma kodu
- hedef tenant DB
- kaynak satır sayısı
- hedef mevcut satır sayısı
- aktarılacak satır sayısı
- conflict tahmini
- eksik FK listesi
- eksik tablo listesi
- işlem süresi
- hata detayları

---

# 8. RİSK MATRİSİ

## Yüksek Risk
- runtime fallback ile legacy DB kullanımı
- tenant DB içinde eksik `Firmalar` kaydı
- personel / bütçe / fatura ilişkilerinde FK kopukluğu

## Orta Risk
- tekrar çalışan transferlerde duplicate/conflict
- büyük veri setlerinde uzun süreli batch işlemler
- offline sync tutarsızlıkları

## Düşük Risk
- UI kanal ayrımı
- dashboard / rapor modernizasyonu
- dokümantasyon eksikleri

---

# 9. KABUL KRİTERLERİ

Aşağıdakiler sağlanmadan mimari tamamlanmış sayılmayacaktır:
- runtime CRUD legacy DB kullanmıyor
- aktif firma -> tenant DB akışı zorunlu
- transfer desktop dry-run destekli
- source DB read-only kalıyor
- conflict logları alınabiliyor
- tenant health raporu üretilebiliyor
- modül bazlı aktarım raporlanabiliyor

---

# 10. DEEPSEEK İÇİN TEK PARÇA GÜÇLÜ PROMPT

Aşağıdaki metni tek parça prompt olarak DeepSeek'e ver:

---

KOAFiloServis için detaylı hibrit mimari, çözümleme stratejisi ve aşamalı uygulama planı hazırla.

Sistem şu gerçek hedef mimaride çalışacak:

## Platformlar
- Web: Blazor Server
- Desktop: WPF veya WinUI
- Mobile: .NET MAUI

## Veritabanı katmanları
- Master DB: firma, kullanıcı, lisans, rol, tenant eşleme
- Legacy DB: `DestekCRMServisBlazorDb`, sadece veri kaynağı
- Tenant DB: her firma için ayrı veritabanı
- Holding DB: çok firma konsolidasyon raporları

## Kesin mimari kurallar
- Runtime sırasında hiçbir CRUD işlemi `DestekCRMServisBlazorDb` kullanmayacak
- Tüm runtime ekran ve servisleri aktif firmanın tenant DB’sinde çalışacak
- Legacy DB yalnızca transfer / migration / analiz kaynağı olacak
- Veri aktarımı yalnızca Desktop uygulama üzerinden yapılacak
- Kaynak DB’ye yazılmayacak
- Sadece kopyalama yapılacak
- Firma koduna göre hedef tenant DB bulunacak
- Dry-run olacak
- ON CONFLICT davranışı loglanacak
- FK ve veri bütünlüğü korunacak
- Web tarafı Blazor Server öncelikli olacak

## Kanal bazlı modüller
- Web:
  - ERP
  - Muhasebe
  - Bütçe
  - Stok
  - Cari / CRM
  - Fatura / Proforma
  - Filo yönetimi
  - Puantaj yönetimi
  - Raporlama
  - Tenant ve sistem yönetimi
- Desktop:
  - Puantaj terminali
  - Barkod operasyonları
  - Veri aktarımı
  - Offline hızlı giriş
- Mobile:
  - Sevkiyat
  - saha görevleri
  - mobil checklist
  - konum bazlı süreçler

## Çıktı formatı
Aşağıdaki başlıklarla eksiksiz çıktı üret:
1. Hedef Mimari
2. Modül Dağılımı
3. Veritabanı Akışı
4. Tenant Connection Resolution Akışı
5. Web / Desktop / Mobile Sorumluluk Sınırları
6. Önerilen Solution Yapısı
7. Project Ayrımı
8. Servis Katmanı Tasarımı
9. Desktop Veri Aktarım Tasarımı
10. Transfer Pipeline
11. Dry-run Rapor Tasarımı
12. ON CONFLICT Log Modeli
13. Validation Rules
14. Retry / Failure Handling
15. Riskler
16. Rollback Stratejisi
17. Aşamalı Uygulama Planı

## Özellikle teknik ayrıntı ver
Aşağıdakileri açık liste halinde üret:
- interface listesi
- service listesi
- repository ayrımı
- tenant resolver tasarımı
- legacy source reader tasarımı
- transfer validator tasarımı
- transfer executor tasarımı
- rapor servisi tasarımı
- tablo bazlı veri aktarım sıralaması
- master -> tenant eşleme stratejisi
- offline cache stratejisi
- mobil sync stratejisi

## TransferDesktop için özel görev
`TransferDesktop` modülünü ayrıca detaylandır:
- ekran listesi
- viewmodel listesi
- servis listesi
- dry-run akışı
- gerçek transfer akışı
- conflict çözümleme seçenekleri
- audit trail yapısı
- kullanıcıya gösterilecek sonuç raporları

## Yasaklar
- runtime legacy DB fallback önermeyeceksin
- FK kaldırma önermeyeceksin
- veri bütünlüğünü bozacak çözüm önermeyeceksin
- kaynak DB’yi değiştiren çözüm önermeyeceksin
- tenant izolasyonunu bozan ortak runtime DB yaklaşımı önermeyeceksin

## Çıktı stili
- teknik
- uygulanabilir
- aşamalı
- net karar verici
- gerekiyorsa tablolar kullan
- belirsiz bırakma

---

# 11. KULLANIM TALİMATI

Bu doküman şu sırayla kullanılmalıdır:
1. DeepSeek'e master prompt verilir
2. çıkan sonuç fazlara bölünür
3. her faz için ayrı teknik görev listesi üretilir
4. uygulama, tenant runtime sertleştirmeden sonra transfer modülüne geçer

---

# 12. SONUÇ

Bu dokümanın özeti:
- Web yönetir
- Desktop aktarır ve terminal olur
- Mobile sahaya iner
- Master yönetir
- Tenant çalıştırır
- Legacy yalnızca kaynak olur
- Holding özetler

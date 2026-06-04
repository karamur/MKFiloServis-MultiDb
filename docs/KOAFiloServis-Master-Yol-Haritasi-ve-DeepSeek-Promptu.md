# KOAFiloServis Master Yol Haritası ve DeepSeek İçin Güçlü Prompt

Bu doküman iki amacı birleştirir:

1. KOAFiloServis için tek parça **master yol haritası** sunmak
2. DeepSeek'e verilebilecek **tek parça güçlü teknik prompt** üretmek

---

# BÖLÜM 1 — MASTER YOL HARİTASI

## 1. Vizyon

KOAFiloServis, çok kiracılı (multi-tenant) mimari üzerine kurulu, **Web + Desktop + Mobile** hibrit yapıda çalışan entegre operasyon platformu olarak konumlandırılmalıdır.

### Hedef platformlar
- **Web / Blazor Server**
  - ERP
  - Muhasebe
  - Bütçe
  - Stok
  - Cari / CRM
  - Fatura / Proforma
  - Yönetim ve raporlama
- **Desktop / WPF veya WinUI**
  - Puantaj terminali
  - Barkod operasyonları
  - Veri aktarım aracı
  - Offline giriş / hızlı operasyon ekranları
- **Mobile / .NET MAUI**
  - Sevkiyat
  - Saha görevleri
  - Mobil checklist
  - Konum tabanlı iş akışları

---

## 2. Veritabanı Mimarisi

### 2.1 Master DB
Amaç: sistem yönetimi

İçerik:
- Firmalar
- Kullanıcılar
- Roller
- Lisanslar
- Sistem ayarları
- Tenant eşleme bilgileri

Örnek:
- `KOAFiloServis_Master`

### 2.2 Legacy Source DB
Amaç: sadece eski sistem verisi ve kaynak havuz

İçerik:
- geçmiş ortak veriler
- ilk taşınacak kayıtlar
- migration / transfer kaynağı

Örnek:
- `DestekCRMServisBlazorDb`

Kural:
- Runtime CRUD burada çalışmaz
- Sadece okuma yapılır
- Sadece transfer / analiz / migration kaynağıdır

### 2.3 Tenant DB
Amaç: her firmanın izole çalışma veritabanı

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

Kural:
- Web runtime burada çalışır
- Desktop terminal burada çalışır
- Mobil uygulama burada çalışır

### 2.4 Holding DB
Amaç: konsolidasyon ve çok firma üst raporları

İçerik:
- bütçe konsolidasyonu
- filo özetleri
- personel gider özetleri
- nakit / KPI / trend özetleri

---

## 3. Modül Dağılımı

| Modül | Kanal | Teknoloji | DB Hedefi | Açıklama |
|---|---|---|---|---|
| ERP | Web | Blazor Server | Tenant DB | Ana yönetim |
| Muhasebe | Web | Blazor Server | Tenant DB | Firma bazlı finans |
| Bütçe | Web | Blazor Server | Tenant DB | Tenant bazlı bütçe |
| Stok | Web | Blazor Server | Tenant DB | Merkez operasyon |
| Cari / CRM | Web | Blazor Server | Tenant DB | Müşteri ve tedarikçi yönetimi |
| Filo | Web | Blazor Server | Tenant DB | Araç, şoför, güzergah |
| Puantaj Yönetimi | Web | Blazor Server | Tenant DB | Onay, rapor, hesap |
| Puantaj Terminali | Desktop | WPF / WinUI | Tenant DB / Offline Cache | Hızlı saha ekranı |
| Barkod Operasyonları | Desktop | WPF / WinUI | Tenant DB / Offline Cache | Barkod akışı |
| Veri Aktarımı | Desktop | WPF / WinUI veya Worker UI | Legacy -> Tenant | Kontrollü kopyalama |
| Sevkiyat | Mobile | MAUI | Tenant API / Offline Sync | Mobil teslimat |
| Holding Raporları | Web | Blazor Server | Holding DB | Konsolidasyon |
| Sistem Yönetimi | Web | Blazor Server | Master DB | Lisans, kullanıcı, firma |

---

## 4. Kritik Mimari Kurallar

### 4.1 Runtime veri kuralı
- Normal kullanıcı akışında hiçbir CRUD işlemi `DestekCRMServisBlazorDb` kullanmamalı
- Tüm runtime işlemleri aktif firmanın tenant DB’sinde çalışmalı

### 4.2 Tenant çözümleme kuralı
Akış:
- Login
- Aktif firma seçimi
- Firma kaydından `DatabaseName`
- `TenantConnectionStringProvider`
- `TenantDbContextFactory`
- `ApplicationDbContext`
- Tenant DB CRUD

### 4.3 Fallback yasağı
- Aktif firma yoksa exception
- `DatabaseName` boşsa exception
- `DefaultConnection` runtime fallback olarak kullanılmamalı

### 4.4 Transfer kuralı
- Kaynak DB’ye yazı yapılmaz
- Sadece kopyalama yapılır
- Firma kodu / firma eşleme ile hedef tenant DB bulunur
- Dry-run zorunlu desteklenir
- ON CONFLICT loglanır

---

## 5. Hedef Solution Yapısı

### Çekirdek
- `KOAFiloServis.Shared`
- `KOAFiloServis.Domain`
- `KOAFiloServis.Application`

### Altyapı
- `KOAFiloServis.Infrastructure`
- `KOAFiloServis.Infrastructure.Transfer`

### Sunum
- `KOAFiloServis.Web`
- `KOAFiloServis.TransferDesktop`
- `KOAFiloServis.TerminalDesktop`
- `KOAFiloServis.Mobile`

### Test
- `KOAFiloServis.Tests`
- `KOAFiloServis.PlaywrightTests`

---

## 6. Aşamalı Uygulama Planı

## Faz 1 — Runtime Tenant Sertleştirme
Amaç: sistemin runtime sırasında legacy DB’ye düşmesini engellemek

Yapılacaklar:
1. `TenantConnectionStringProvider` fallback’lerini kaldır
2. `TenantDbContextFactory` fallback’lerini kaldır
3. DbContext oluşturulurken tenant trace log ekle
4. Aktif firma / DatabaseName eksikse fail-fast davranışı uygula
5. Runtime modüllerde `DefaultConnection` bağımlılıklarını raporla

Çıktı:
- Runtime sadece tenant DB üzerinde çalışır

---

## Faz 2 — Tenant Doğrulama ve Sağlık Kontrolü
Amaç: tenant DB’lerin eksik/bozuk durumlarını görünür kılmak

Yapılacaklar:
1. Tenant doğrulama servisi oluştur
2. Aşağıdakileri kontrol et:
   - `Firmalar`
   - `BudgetOdemeler`
   - `BudgetHedefler`
   - `TekrarlayanOdemeler`
3. Tenant health raporu üret
4. Eksik tablo / eksik firma bootstrap / FK risklerini raporla

Çıktı:
- Tenant hazır mı, eksik mi net görünür

---

## Faz 3 — TransferDesktop Altyapısı
Amaç: kontrollü veri aktarım aracı kurmak

Yapılacaklar:
1. `KOAFiloServis.TransferDesktop` oluştur
2. Firma kodu bazlı tenant resolve ekranı ekle
3. Legacy bağlantı doğrulama ekle
4. Dry-run altyapısı kur
5. Transfer pipeline iskeletini hazırla
6. Log ve rapor servislerini ekle

Çıktı:
- masaüstü veri aktarım aracı hazır olur

---

## Faz 4 — Modül Bazlı Veri Aktarımı
Amaç: legacy verileri firma bazlı tenant DB’lere taşımak

Yapılacaklar:
1. Budget aktarımı
2. Cari aktarımı
3. Personel aktarımı
4. Stok aktarımı
5. Fatura aktarımı
6. Puantaj aktarımı
7. Operasyon aktarımı

Kurallar:
- Firma kodu / firmaId eşlemesi açık olacak
- FK ön doğrulama yapılacak
- ON CONFLICT davranışı raporlanacak
- Hatalı satırlar ayrıştırılacak

Çıktı:
- modül bazlı güvenli tenant veri taşıma akışı

---

## Faz 5 — Desktop Operasyon Modülleri
Amaç: saha ve hızlı operasyon akışlarını masaüstüne almak

Yapılacaklar:
1. Puantaj terminali
2. Barkod operasyon ekranları
3. Offline cache desteği
4. Senkronizasyon kuyruğu
5. cihaz entegrasyonları

Çıktı:
- operasyonel kullanım için hızlı ve stabil masaüstü ekranları

---

## Faz 6 — Mobile Saha Modülü
Amaç: mobil sevkiyat ve saha yönetimi

Yapılacaklar:
1. Sevkiyat görev ekranları
2. Saha checklist
3. Konum tabanlı doğrulama
4. Offline sync
5. API güvenliği

Çıktı:
- mobil saha akışı tenant bazlı çalışır hale gelir

---

## 7. TransferDesktop Teknik Sorumlulukları

### UI
- Firma seçimi
- Modül seçimi
- Dry-run önizleme
- Transfer başlatma
- Sonuç / hata raporu

### Servisler
- `ITenantResolver`
- `ILegacySourceReader`
- `ITransferValidator`
- `ITransferExecutor`
- `ITransferReportService`
- `IConflictLogger`

### Dry-run raporu
- kaynak satır sayısı
- hedef mevcut satır sayısı
- aktarılabilir satır sayısı
- eksik FK listesi
- conflict tahmini
- riskler

### ON CONFLICT logu
- tablo adı
- okunan satır sayısı
- eklenen satır sayısı
- conflict sayısı
- atlanan satır sayısı
- hata mesajı

---

## 8. Riskler

### Yüksek Risk
- runtime fallback ile legacy DB kullanılması
- tenant DB’de eksik `Firmalar` bootstrap kaydı
- modüller arası FK zincirinin tam çözülmemesi

### Orta Risk
- tekrar çalışan transferlerde conflict yönetimi
- büyük veri setlerinde uzun işlem süresi
- offline cache senkron hataları

### Düşük Risk
- raporlama ekranlarının holding DB’ye ayrılması
- desktop UX düzenlemeleri

---

## 9. Başarı Kriterleri

Aşağıdakiler sağlanmış olmalı:
- hiçbir runtime CRUD legacy DB kullanmıyor
- aktif firma -> tenant DB zinciri zorunlu
- desktop transfer dry-run destekli
- kaynak DB read-only kalıyor
- ON CONFLICT logları üretiliyor
- tenant DB health raporu alınabiliyor
- budget/cari/personel/stok aktarımı kontrollü çalışıyor

---

# BÖLÜM 2 — DEEPSEEK İÇİN TEK PARÇA GÜÇLÜ PROMPT

Aşağıdaki prompt, DeepSeek'e doğrudan verilecek tek parça ana girdidir.

---

## DeepSeek Master Prompt

KOAFiloServis için detaylı hibrit mimari ve uygulama planı hazırla.

Sistem gerçek hedefte şu yapıda çalışacak:

### Platformlar
- Web: Blazor Server
- Desktop: WPF veya WinUI
- Mobile: .NET MAUI

### Veritabanı katmanları
- Master DB: kullanıcı, firma, lisans, rol, tenant eşleme
- Legacy DB: `DestekCRMServisBlazorDb`, sadece veri kaynağı
- Tenant DB: her firma için ayrı veritabanı
- Holding DB: çok firma konsolidasyon raporları

### Temel mimari kurallar
- Runtime sırasında hiçbir CRUD işlemi `DestekCRMServisBlazorDb` kullanmayacak
- Tüm runtime ekran ve servisleri aktif firmanın tenant DB’sinde çalışacak
- Legacy DB yalnızca veri aktarımı / migration kaynağı olacak
- Veri aktarımı yalnızca Desktop uygulama üzerinden yapılacak
- Kaynak DB’ye yazılmayacak
- Sadece kopyalama yapılacak
- Firma koduna göre hedef tenant DB bulunacak
- Dry-run modu olacak
- ON CONFLICT davranışı loglanacak
- FK ve veri bütünlüğü korunacak
- Blazor Server web tarafında öncelikli teknoloji olacak

### Kanal bazlı modül dağılımı
- Web:
  - ERP
  - Muhasebe
  - Bütçe
  - Stok
  - Cari / CRM
  - Fatura / Proforma
  - Filo yönetimi
  - Puantaj yönetimi
  - Raporlar
  - Tenant ve sistem yönetimi
- Desktop:
  - Puantaj terminali
  - Barkod operasyonları
  - Veri aktarımı
  - Offline hızlı giriş ekranları
- Mobile:
  - Sevkiyat
  - Saha görevleri
  - Mobil checklist
  - Konum bazlı akışlar

### Senden beklenenler
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

### Özellikle teknik ayrıntı ver
Aşağıdaki konularda somut öneriler üret:
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

### Veri aktarımı için özel görev
`TransferDesktop` modülünü ayrıca detaylandır:
- ekran listesi
- viewmodel listesi
- servis listesi
- dry-run akışı
- gerçek transfer akışı
- conflict çözümleme seçenekleri
- audit trail yapısı
- kullanıcıya gösterilecek sonuç raporları

### Kısıtlar
- Runtime legacy DB fallback önermeyeceksin
- FK kaldırma önermeyeceksin
- veri bütünlüğünü bozacak çözümler önermeyeceksin
- kaynak DB’yi değiştiren yaklaşım önermeyeceksin
- tenant izolasyonunu zedeleyen ortak runtime DB kullanımını önermeyeceksin

### Çıktı stili
- Teknik
- Yapısal
- Aşamalı
- Uygulanabilir
- Gerektiğinde tablo ve madde listesi kullan
- Belirsiz bırakma, net yön ver

---

# BÖLÜM 3 — KISA KULLANIM TALİMATI

Bu doküman nasıl kullanılmalı:

1. Önce bu dosyanın master prompt bölümü DeepSeek’e verilir
2. Çıkan sonuca göre:
   - solution yapısı
   - transfer tasarımı
   - tenant doğrulama
   - desktop modül planı
   ayrı iş paketlerine bölünür
3. Ardından her faz bağımsız teknik görev olarak uygulanır

---

# BÖLÜM 4 — SON SÖZ

KOAFiloServis’in doğru hedef mimarisi:

- **Web** = yönetim ve kurumsal operasyon
- **Desktop** = terminal, barkod, veri aktarımı
- **Mobile** = saha ve sevkiyat
- **Master / Tenant / Holding / Legacy** = ayrık sorumluluklu DB yapısı

Bu dokümandaki en kritik üç ilke:

1. tenant izolasyonu zorunludur
2. legacy DB sadece kaynaktır
3. veri aktarımı kontrollü, loglu ve dry-run destekli olmalıdır

# KOAFiloServis Hibrit Mimari Raporu

## 1. Genel Mimari

KOAFiloServis için önerilen yapı **Web + Desktop + Mobile** hibrit mimarisidir.

- **Web**: merkezi yönetim, ERP, muhasebe, bütçe, stok, cari, raporlama
- **Desktop**: puantaj terminali, barkod operasyonları, veri aktarımı
- **Mobile**: sevkiyat ve saha operasyonları

Bu yapıda runtime iş yükü tenant veritabanlarına dağıtılır, legacy veritabanı yalnızca veri kaynağı olarak kullanılır.

---

## 2. Modül Dağılımı

| Modül | Kanal | Teknoloji | Veritabanı Modeli | Not |
|---|---|---|---|---|
| ERP / Ana Yönetim | Web | Blazor Server | Tenant DB | Merkez yönetim ekranları |
| Muhasebe | Web | Blazor Server | Tenant DB | Firma bazlı finans izolasyonu |
| Bütçe | Web | Blazor Server | Tenant DB | Her firma kendi DB’sinde çalışır |
| Stok / Envanter | Web | Blazor Server | Tenant DB | Merkez ofis ve operasyon yönetimi |
| Cari / CRM | Web | Blazor Server | Tenant DB | Müşteri/tedarikçi/firma bazlı ayrım |
| Fatura / Proforma | Web | Blazor Server | Tenant DB | Tenant bazlı finansal kayıt |
| Filo Yönetimi | Web | Blazor Server | Tenant DB | Araç, şoför, güzergah, bakım |
| Puantaj Yönetimi | Web | Blazor Server | Tenant DB | Onay, rapor, hesaplama |
| Puantaj Terminali | Desktop | WPF / WinUI | Tenant DB / Offline Cache | Saha/operasyon terminali |
| Barkod Operasyonları | Desktop | WPF / WinUI | Tenant DB / Offline Cache | Hızlı operasyon ekranları |
| Veri Aktarımı | Desktop | .NET Desktop Worker/Client | Legacy -> Tenant | Kontrollü veri taşıma |
| Sevkiyat | Mobile | .NET MAUI | Tenant API + Offline Sync | Mobil saha operasyonu |
| Holding Konsolidasyon | Web | Blazor Server | Holding DB | Çok firma özet raporları |
| Sistem Yönetimi | Web | Blazor Server | Master DB | Firma, kullanıcı, lisans, rol |

---

## 3. Veritabanı Katmanları

### 3.1 Master DB
Amaç: sistem yönetimi

İçerik:
- Firmalar
- Kullanıcılar
- Roller
- Lisanslar
- Tenant eşleme bilgileri
- Sistem ayarları

Örnek:
- `KOAFiloServis_Master`

### 3.2 Legacy Kaynak DB
Amaç: eski sistem verisi ve ilk veri kaynağı

İçerik:
- Ortak eski veri yapısı
- Geçmiş operasyon kayıtları
- İlk taşınacak veri havuzu

Örnek:
- `DestekCRMServisBlazorDb`

Kural:
- Runtime CRUD için kullanılmaz
- Sadece migration / analiz / veri taşıma kaynağıdır

### 3.3 Tenant DB
Amaç: her firmanın izole çalışma veritabanı

Örnekler:
- `Koa_USTUN_GRUP_001`
- `Koa_RECEP_USTUN_003`
- `Koa_USTUN_FILO_005`

İçerik:
- Cari
- Muhasebe
- Bütçe
- Stok
- Araç
- Şoför
- Puantaj
- Fatura
- Operasyon

Kural:
- Web runtime burada çalışır
- Desktop terminal burada çalışır
- Mobil entegrasyon burada çalışır

### 3.4 Holding DB
Amaç: konsolidasyon ve üst yönetim raporları

İçerik:
- Toplam bütçe
- Çok firma KPI
- Nakit / puantaj / filo özetleri

---

## 4. Kanal Bazlı Görevler

### Web
Blazor Server ile:
- ERP
- Muhasebe
- Bütçe
- Stok
- Cari
- Fatura
- Yönetim ekranları
- Raporlama
- Tenant yönetimi

### Desktop
WPF / WinUI ile:
- Puantaj terminali
- Barkod okutma
- Hızlı veri giriş ekranları
- Cihaz entegrasyonları
- Veri aktarımı / veri taşıma aracı
- Offline senaryolar

### Mobile
MAUI ile:
- Sevkiyat
- Araç teslim/alım
- Saha checklist
- Lokasyon tabanlı görev akışları

---

## 5. Veri Aktarımı İçin Desktop Uygulama Önerisi

### Uygulama Adı
`KOAFiloServis.TransferDesktop`

### Amaç
`DestekCRMServisBlazorDb` içindeki legacy verileri ilgili tenant DB’lere güvenli şekilde taşımak.

### Sorumluluklar
- Firma seçimi
- Firma koduna göre hedef tenant DB bulma
- Dry-run çalışma
- Satır sayısı analizi
- Eksik tablo kontrolü
- FK ön kontrolü
- ON CONFLICT loglama
- Transfer raporu üretme
- Yeniden çalıştırılabilir senaryo

### Çalışma Kuralı
- Kaynak DB’ye yazmaz
- Sadece okur
- Hedef tenant DB’ye yazar
- Log ve rapor üretir
- Hatalı satırları ayrı raporlar

---

## 6. Ana Teknik Kurallar

- Runtime hiçbir modülde `DestekCRMServisBlazorDb` kullanmamalı
- Tüm CRUD işlemleri aktif firmanın tenant DB’sinde çalışmalı
- Kaynak DB yalnızca veri kaynağı olmalı
- Veri aktarımı yalnızca Desktop aracıyla yapılmalı
- Kaynak DB’ye hiçbir yazma işlemi yapılmamalı
- Firma koduna göre tenant DB bulunmalı
- Dry-run desteklenmeli
- ON CONFLICT davranışı loglanmalı
- FK ve veri bütünlüğü korunmalı

---

## 7. Aşamalı Uygulama Planı

### Faz 1
- Runtime legacy bağımlılığı kaldır
- Tenant connection zorunlu yap
- DB trace logları ekle

### Faz 2
- Desktop Transfer uygulamasını oluştur
- Dry-run + raporlama yap
- Firma kodu eşleme ekranı ekle

### Faz 3
- Budget / Cari / Personel / Stok veri aktarım paketleri
- Conflict resolution politikası
- Eksik FK raporu

### Faz 4
- Puantaj terminali desktop
- Barkod operasyon desktop
- Offline cache

### Faz 5
- Mobile sevkiyat
- API / sync altyapısı
- Holding konsolidasyon ekranları

---

## 8. Kısa Nihai Öneri

En doğru ürün konumlandırması:

- **Web / Blazor Server**
  - ERP + Muhasebe + Bütçe + Stok + Cari + Rapor
- **Desktop / WPF veya WinUI**
  - Puantaj Terminali + Barkod + Veri Aktarımı
- **Mobile / MAUI**
  - Sevkiyat + Saha İşlemleri
- **DB Mimarisi**
  - Master + Legacy Source + Tenant DB + Holding DB
- **Veri Aktarımı**
  - Sadece Desktop aracıyla, firma kodu bazlı, dry-run destekli

# DeepSeek İçin KOAFiloServis Hibrit Mimari Plan ve Talimatları

Bu doküman, DeepSeek'e verilecek mimari görev tanımlarını ve teknik çerçeveyi içerir.

---

## 1. Sistem Tanımı

KOAFiloServis için hibrit mimari hedeflenmektedir:

- **Web**: Blazor Server
- **Desktop**: Puantaj terminali, barkod operasyonları, veri aktarımı
- **Mobile**: Sevkiyat ve saha işlemleri

Veritabanı katmanları:

- **Master DB**: firma, kullanıcı, lisans, rol
- **Legacy DB**: `DestekCRMServisBlazorDb` sadece veri kaynağı
- **Tenant DB**: her firma için ayrı veritabanı
- **Holding DB**: konsolidasyon

---

## 2. DeepSeek'e Verilecek Kritik Kurallar

- Runtime hiçbir modülde `DestekCRMServisBlazorDb` kullanmayacak
- Tüm CRUD işlemleri tenant DB üzerinde çalışacak
- Veri aktarımı yalnızca Desktop uygulama üzerinden yapılacak
- Kaynak DB’ye yazı yapılmayacak
- Sadece kopyalama olacak
- Firma koduna göre hedef tenant DB belirlenecek
- Dry-run desteklenecek
- ON CONFLICT davranışı loglanacak
- FK ve veri bütünlüğü korunacak
- Web tarafı Blazor Server öncelikli olacak

---

## 3. DeepSeek İçin Hazır Prompt

Aşağıdaki prompt doğrudan kullanılabilir:

---

KOAFiloServis için hibrit mimari planı hazırla.

Sistem şu mimaride çalışacak:

- Web: Blazor Server
- Desktop: Puantaj terminali, barkod operasyonları, veri aktarımı
- Mobile: Sevkiyat ve saha işlemleri
- Master DB: kullanıcı, firma, lisans, rol
- Legacy DB: DestekCRMServisBlazorDb, sadece veri kaynağı
- Tenant DB: her firma için ayrı veritabanı
- Holding DB: konsolidasyon

Amaç:
1. Modülleri Web / Desktop / Mobile olarak ayır
2. Tenant DB bazlı veri akışını çiz
3. Desktop veri aktarım modülü tasarla
4. Kaynak DB’den tenant DB’ye veri kopyalama stratejisi oluştur
5. Dry-run, loglama, hata yönetimi, ON CONFLICT davranışı belirle
6. Uygulama klasör yapısını öner
7. Servis katmanı, repository yapısı, transfer pipeline ve doğrulama mekanizmasını tasarla
8. Aşamalandırılmış implementasyon planı ver

Kurallar:
- Runtime legacy DB kullanmayacak
- Kaynak DB’ye yazılmayacak
- Firma koduna göre tenant DB bulunacak
- FK ve veri bütünlüğü korunacak
- Web tarafı Blazor Server öncelikli olacak

Çıktıyı şu başlıklarla ver:
1. Hedef Mimari
2. Modül Dağılımı
3. Veritabanı Akışı
4. Desktop Veri Aktarım Tasarımı
5. Servis Katmanı Tasarımı
6. Riskler
7. Aşamalı Uygulama Planı

---

## 4. DeepSeek'ten Özellikle İstenmesi Gereken Teknik Çıktılar

Aşağıdakiler özellikle talep edilmelidir:

- solution yapısı
- project ayrımı
- interface listesi
- service listesi
- transfer pipeline
- validation rules
- retry policy
- logging modeli
- audit trail yapısı
- dry-run raporu formatı
- mapping strategy
- tenant resolution strategy

---

## 5. Desktop Veri Aktarımı İçin Teknik İstem

DeepSeek’e ayrıca şu dar kapsamlı görev de verilebilir:

"KOAFiloServis için `TransferDesktop` modülü tasarla.

Kurallar:
- Kaynak DB: `DestekCRMServisBlazorDb`
- Hedef DB: `Koa_[FirmaKodu]_[Id]`
- Kaynak veritabanına yazılmayacak
- Sadece kopyalama yapılacak
- Firma kodu bazlı filtreleme olacak
- Dry-run olacak
- Transfer öncesi ve sonrası rapor üretilecek
- ON CONFLICT davranışı loglanacak
- Hatalı satırlar raporlanacak
- FK eksikleri önceden doğrulanacak

Aşağıdakileri üret:
1. ekran tasarımı
2. servis tasarımı
3. transfer pipeline
4. log formatı
5. dry-run rapor formatı
6. retry/failure handling
7. rollback stratejisi
8. tablo bazlı transfer sıralaması"

---

## 6. Aşamalı Teknik Yol Haritası

### Faz 1
- Runtime tenant zorunluluğu
- Legacy fallback kaldırma
- DB trace logging

### Faz 2
- TransferDesktop altyapısı
- tenant resolve servisi
- firma kodu eşleme

### Faz 3
- Budget / Cari / Personel / Stok veri aktarımı
- FK doğrulama ve conflict handling

### Faz 4
- Puantaj terminali desktop
- barkod operasyon desktop
- offline destek

### Faz 5
- Mobil sevkiyat ve saha iş akışları
- sync altyapısı
- holding konsolidasyon

---

## 7. Sonuç

DeepSeek'ten beklenen çıktı yalnızca kod değil, aynı zamanda:
- mimari yönlendirme
- modül ayrımı
- veritabanı stratejisi
- transfer güvenliği
- operasyonel hata yönetimi
olmalıdır.

Bu nedenle promptlarda her zaman şu üç vurgu bulunmalıdır:
1. tenant izolasyonu
2. legacy DB sadece kaynak
3. desktop transfer kontrollü ve raporlu olmalı

# Desktop Transfer Modülü Teknik Görev Listesi

Bu doküman, `KOAFiloServis.TransferDesktop` modülü için uygulanabilir görev listesini içerir.

---

## 1. Amaç

Legacy veritabanı `DestekCRMServisBlazorDb` içindeki verileri, firma koduna göre ilgili tenant veritabanına güvenli şekilde kopyalamak.

Kurallar:
- Kaynak DB değişmeyecek
- Sadece okuma yapılacak
- Hedef tenant DB’ye kopyalama yapılacak
- Dry-run desteklenecek
- ON CONFLICT davranışı loglanacak
- Hatalar raporlanacak

---

## 2. Ana Bileşenler

### UI Katmanı
- Firma seçim ekranı
- Transfer modül seçim ekranı
- Dry-run önizleme ekranı
- Transfer ilerleme ekranı
- Sonuç / rapor ekranı

### Servis Katmanı
- `ILegacySourceReader`
- `ITenantResolver`
- `ITransferValidator`
- `ITransferExecutor`
- `ITransferReportService`
- `IConflictLogger`

### Teknik Katman
- PostgreSQL bağlantı yönetimi
- batch işleme
- retry policy
- transaction yönetimi
- audit / transfer log

---

## 3. Önerilen Görevler

## Faz 1 — Temel İskelet
1. Desktop proje iskeletini oluştur
2. MVVM yapısını kur
3. Konfigürasyon okuma altyapısını ekle
4. Master / Legacy / Tenant bağlantı servislerini tanımla
5. Firma kodu çözümleme mekanizmasını ekle

## Faz 2 — Dry-Run
6. Dry-run servis kontratlarını yaz
7. Kaynak satır sayısı analizini ekle
8. Hedef tablo varlık kontrolünü ekle
9. FK bağımlılık doğrulamasını ekle
10. Dry-run rapor modelini oluştur

## Faz 3 — Transfer Pipeline
11. Transfer pipeline tasarla
12. Tablo bazlı sıralı aktarım stratejisi ekle
13. Batch insert mekanizması kur
14. ON CONFLICT davranışlarını logla
15. Hatalı satır toplama mekanizması ekle

## Faz 4 — Raporlama
16. Transfer sonuç ekranını oluştur
17. Başarılı/başarısız satır raporu üret
18. Dry-run vs gerçek çalışma farklarını raporla
19. CSV / Excel dışa aktarma ekle

## Faz 5 — Dayanıklılık
20. Retry policy ekle
21. İptal/yeniden başlat desteği ekle
22. Uzun işlem ilerleme bildirimi ekle
23. İşlem günlüğü arşivleme ekle

---

## 4. Önerilen Servisler

### ITenantResolver
Sorumluluk:
- Firma koduna göre tenant DB bulmak
- Master DB üzerinden firma -> DatabaseName çözmek

### ILegacySourceReader
Sorumluluk:
- Legacy DB’den veri okumak
- Filtreli veri almak
- Firma kodu / FirmaId bazlı sorgu yürütmek

### ITransferValidator
Sorumluluk:
- Hedef tablo mevcut mu
- Gerekli FK kayıtları var mı
- Firma bootstrap mevcut mu
- Kolon uyumu sağlanıyor mu

### ITransferExecutor
Sorumluluk:
- Dry-run veya gerçek transfer çalıştırmak
- Tablo sırası yönetmek
- Batch write yapmak
- Conflict ve hata loglarını toplamak

### ITransferReportService
Sorumluluk:
- Dry-run raporu
- Gerçek transfer raporu
- Hatalı satır listesi
- Özet dashboard üretimi

---

## 5. Transfer Sıralaması Önerisi

Önce:
- Firmalar bootstrap
- lookup / tanım tabloları
- muhasebe hesap planı
- masraf kalemleri
- marka/model/tanım kayıtları

Sonra:
- cari
- kurum
- araç
- şoför/personel
- banka hesapları
- budget hedef / ödeme
- fatura
- puantaj
- operasyon kayıtları

En son:
- ilişki / eşleştirme tabloları
- snapshot / audit tabloları

---

## 6. Dry-Run Çıktısı İçeriği

Dry-run raporunda şu alanlar olmalı:
- Firma kodu
- Hedef tenant DB
- Kaynak satır sayısı
- Hedef mevcut satır sayısı
- Aktarılabilir satır sayısı
- conflict tahmini
- eksik tablo listesi
- eksik FK listesi
- kritik riskler
- önerilen işlem sırası

---

## 7. ON CONFLICT Log Formatı

Her tablo için:
- tablo adı
- okunan satır sayısı
- insert edilen satır sayısı
- conflict olan satır sayısı
- atlanan satır sayısı
- ilk hata mesajı

Örnek alanlar:
- `TableName`
- `ReadCount`
- `InsertedCount`
- `ConflictCount`
- `SkippedCount`
- `ErrorMessage`
- `StartedAt`
- `CompletedAt`

---

## 8. Riskler

- Eksik firma bootstrap
- Eksik FK bağımlılığı
- Legacy veride null/bozuk firma ilişkileri
- Aynı kayıtların tekrarlı taşınması
- Büyük veri setinde uzun işlem süresi
- Hedef tenant şema farkları

---

## 9. Rollback Stratejisi

Öneri:
- Kaynak DB’ye zaten yazılmadığı için kaynak rollback gereksiz
- Hedefte işlem bazlı transaction kullanılmalı
- Büyük veri setlerinde tablo bazlı veya batch bazlı rollback uygulanmalı
- Dry-run öncesi gerçek transfer çalıştırılmamalı
- Transfer raporu arşivlenmeli

---

## 10. Kısa Sonuç

TransferDesktop modülü şu üç özelliği zorunlu taşımalıdır:
1. tenant resolve
2. dry-run doğrulama
3. loglu ve güvenli kopyalama

Bu modül, KOAFiloServis hibrit mimarisinde legacy bağımlılığı kontrollü şekilde sonlandıran ana araç olacaktır.

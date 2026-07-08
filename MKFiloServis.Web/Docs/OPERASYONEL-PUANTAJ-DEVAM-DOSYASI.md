# MKFiloServis — Operasyonel Puantaj Devam Dosyası

> **Amaç:** Bu dosya, operasyonel puantaj sayfası çalışmasına kaldığımız yerden devam edebilmek için
> tüm bağlamı, mimari kararları ve açık noktaları tek belgede saklar.
> Son güncelleme: Temmuz 2026

---

## 1. İŞLETME MODELİ (Değişmez Referans)

Firma; **kurumsal müşterilere personel taşımacılığı / personel servis hizmeti** vermektedir.

### Araç Sahiplik Tipleri

| Tip | Enum Değeri | Açıklama | Maliyet Sahibi | Fatura |
|-----|-------------|----------|----------------|--------|
| **Özmal** | `AracSahiplikTipi.Ozmal (1)` | Bize ait araç, bize ait şoför | Biz (yakıt + bakım + maaş + amortisman) | Kuruma 1 gelir fatura |
| **Kiralık** | `AracSahiplikTipi.Kiralik (2)` | C-plaka uzun dönem kira, bize ait şoför | Biz (plaka kirası + yakıt + maaş) | Kuruma 1 gelir fatura |
| **Tedarikçi** | `AracSahiplikTipi.Tedarikci (5)` | Taşeronun aracı + şoförü | Tedarikçi | Kuruma 1 gelir fatura + Tedarikçiye 1 gider fatura |

### İş Hiyerarşisi

```
Kurum (Müşteri Firma/Kurum)
  └─ Güzergah (örn: Organize Sanayi ↔ Şehir Merkezi)
       └─ ServisKontrat (Güzergah × Araç × Şoför × Tedarikçi × birim fiyat)
            └─ FiloGunlukPuantaj (her güne 1 kayıt — operasyonun kalbi)
                 └─ Hakediş (aylık toplama — kurum / tedarikçi / araç bazında)
                      └─ Fatura (gelir veya gider)
```

---

## 2. MEVCUT KOD DURUMU (Temmuz 2026 itibarı)

### 2.1 Entity Katmanı — Durum

| Entity | Dosya | Durum | Notlar |
|--------|-------|-------|--------|
| `FiloGunlukPuantaj` | `FiloKomisyonPuantaj.cs` | ✅ Tam | `ServisTuru`, `SeferSayisi`, `TahakkukEdenKurumUcreti`, `TahakkukEdenTaseronUcreti`, `MaliyetOzmalKiralik`, `TaksiKullanildiMi` var |
| `ServisKontrat` | `ServisKontrat.cs` | ✅ Tam | `ServisKontratTip` (Ozmal/Kiralik/Tedarikci), `GelirBirimFiyat`, `GiderBirimFiyat` |
| `Hakedis` + `HakedisDetay` | Shared/Entities | ✅ Tam | `HakedisTipi` (Kurum/Tedarikci/Arac), `HakedisDurum`, migration var |
| `HakedisPuantaj` | `HakedisPuantaj.cs` | ✅ Var (Legacy) | ESKİ hakediş modeli; aktif iş akışlarında kullanılmıyor, ağırlıklı DB/migration uyumluluğu için duruyor (bkz. §3.1 Sorun) |
| `AracMaliyetSnapshot` | Shared/Entities | ✅ Tam | Migration eklendi (önceki oturum), servis çalışıyor |
| `PersonelPuantaj` | `PersonelPuantaj.cs` | ✅ Tam | Maaş bazlı — operasyonel puantajdan AYRI |
| `OperasyonKaydi` | `OperasyonKaydi.cs` | ✅ Var | PuantajEngine ile oluşturulan kayıt — `FiloGunlukPuantaj` ile FARKLI tablo |
| `PuantajKayit` | `PuantajKayit.cs` | ✅ Var | Engine çıktısı |
| `PuantajAnomali` | `PuantajAnomali.cs` | ✅ Var | Job ile tespit ediliyor |

### 2.2 Servis Katmanı — Durum

| Servis | Implementasyon | Durum | Notlar |
|--------|---------------|-------|--------|
| `OperasyonelHakedisService` | `OperasyonelHakedisService.cs` | ✅ **ÇALIŞIYOR** | 632 satır, `FiloGunlukPuantaj` → `Hakedis` üretimi tam |
| `HakedisPuantajService` | `HakedisPuantajService.cs` | ❌ Kaldırıldı | Legacy servis/interface dosyaları ve DI kaydı kaldırıldı; kalan legacy kullanım doğrudan tablo/sync modüllerinde |
| `HakedisRaporService` | `HakedisRaporService.cs` | ✅ ÇALIŞIYOR | Yeni `Hakedis` modeli üzerinden raporlama |
| `HakedisMuhasebeService` | `HakedisMuhasebeService.cs` | ✅ ÇALIŞIYOR | Yeni `Hakedis` modeli üzerinden muhasebe fişi entegrasyonu |
| `AracMaliyetService` | `AracMaliyetService.cs` | ✅ **ÇALIŞIYOR** | `FullpetFaturaDagitAsync` dahil |
| `PuantajEngineService` | `PuantajEngineService.cs` | ✅ Var | `OperasyonKaydi` → `PuantajKayit` dönüşümü |
| `PuantajWorkflowService` | `PuantajWorkflowService.cs` | ✅ Var | Finans/Muhasebe/Kilit onay zinciri |
| `ServisKontratService` | `ServisKontratService.cs` | ✅ Var | Kontrat CRUD + birim fiyat |
| `FiloKomisyonService` | `FiloKomisyonService.cs` | ✅ Var | `FiloGunlukPuantaj` CRUD |

### 2.3 UI Sayfaları — Durum

| Sayfa | Rota | Satır | Durum | Notlar |
|-------|------|-------|-------|--------|
| `FiloGunlukPuantajPage.razor` | `/operasyon/filo-gunluk-puantaj` | 1887 | ❌ Silindi | FAZ 0 Adım 0.1 kapsamında kaldırıldı |
| `FiloHakedisPage.razor` | `/filo-hakedis` | 568 | ❌ Silindi | FAZ 0 Adım 0.1 kapsamında kaldırıldı |
| `FiloPuantaj.razor` | `/filo/puantaj` | ? | ❌ Silindi | FAZ 0 Adım 0.1 kapsamında kaldırıldı |
| `OperasyonelPuantajPage.razor` | `/operasyon/puantaj` | ~300 | ✅ Aktif | Yeni günlük operasyon puantaj ekranı (FAZ 1 başlangıç) |
| `OperasyonelHakedisPage.razor` | `/operasyonel-hakedis` | ~330 | ✅ Aktif | Yeni hakediş yönetim ekranı (toplu üret/onay/faturalama) |
| `PuantajDetay.razor` | `/servis-operasyon/puantaj/{id}` | 681 | ✅ Aktif | Kontrat bazlı detay, onay, fatura taslağı |
| `KontratList.razor` | `/servis-operasyon/kontratlar` | ? | ✅ Aktif | ServisKontrat listesi |
| `KontratForm.razor` | `/servis-operasyon/kontrat-form` | ? | ✅ Aktif | Kontrat oluşturma/düzenleme |
| `AracMaliyetSnapshotPage.razor` | `/araclar/maliyet-snapshot` | ? | ✅ Aktif | Önceki oturumda eklendi |
| `HaftalikPuantajMatrisi.razor` | Raporlar | ? | ✅ Var | Haftalık matris raporu |

---

## 3. KRİTİK SORUNLAR VE ÇÖZÜM DURUMLARI

### 3.1 🟡 ÜÇ PARALEL HAKEDİŞ SİSTEMİ ÇAKIŞMASI (GEÇİŞ DARALDI)

Projede **üç ayrı hakediş yolu** var, bunlar tam entegre değil:

```
YOL A — Klasik kontrat bazlı:
ServisKontrat → ServisPuantaj → HakedisPuantaj (tablo: HakedisPuantajlar)
Servis: Legacy tablo varlığı (`HakedisPuantaj`) + çoğunlukla `ApplicationDbContext` / migration geçmişinde kalan referanslar
Durum: Operasyonel yeni ekranlarda kullanılmıyor; ayrıca FinansDashboardService, DenetimService, HakedisRaporService ve HakedisMuhasebeService tarafında yeni Hakedis modeline geçiş yapıldı.
Kalan kullanım alanları ağırlıklı legacy/yardımcı modüllerle sınırlı (DI kaydı ile servis/interface dosyaları kaldırıldı, kod tabanı referansları kademeli temizlenecek). `PuantajFinansService` tarafındaki legacy `HakedisPuantaj` işleme overload'u kaldırıldı; `RebuildService` zinciri yalnız yeni `Hakedis` modelini kullanıyor. `PuantajExcelService` import hattı ve `PuantajHakedisSyncService` + `PuantajExcelGrid` senkronizasyon/validasyon akışı yeni `Hakedis` modeline geçirildi. Denetim tarafında SnapshotTransaction eşlemesi `HakedisPuantajId` yerine `FaturaId` bazına çekildi; ayrıca yalnız `HakedisFinans*` transactionları denetlenerek ve taslak/iptal kayıtlar hariç tutularak false-positive riskleri azaltıldı. TestSession backup/rollback akışı ve TopluFatura yönlendirme metni de yeni `Hakedis` terminolojisine hizalandı.

YOL B — Operasyonel (YENİ AKTİF YOL):
FiloGunlukPuantaj → Hakedis + HakedisDetay (tablo: Hakedisler)
Servis: OperasyonelHakedisService ✅ ÇALIŞIYOR
Kullanıldığı yerler: OperasyonelHakedisPage, operasyonel hakediş akışları, ilgili yeni ekranlar

YOL C — PuantajEngine:
OperasyonKaydi → PuantajKayit → (hakediş yok, sadece sync)
```

**Gerçek durum (güncel):**
`FiloHakedisPage.razor` kaldırıldı; operasyonel UI artık `OperasyonelHakedisPage.razor` üzerinden ilerliyor.
Buna rağmen `HakedisPuantaj` hattı kod tabanında tamamen temizlenmiş değil; kalan kullanım alanları ağırlıklı `ApplicationDbContext` + migration geçmişi gibi legacy tablo katmanında sınırlıdır.

**Sonuç:** Operasyon tarafında doğru yol Yol B'dir. FinansDashboard ve Denetim katmanı Yol B ile hizalanmıştır.
Sistem genelinde Yol A için geçiş/temizlik tamamen bitmediği için çift model riski azaltılmış ama teknik borç olarak kısmen devam etmektedir.

### 3.2 🟢 OPERASYONEL PUANTAJ SAYFASI (AKTİF VE İYİLEŞTİRİLMİŞ)

Eski `FiloGunlukPuantajPage.razor` kaldırıldı. Yerine `OperasyonelPuantajPage.razor` eklendi.

Mevcut durum:
- ✅ Kontrat/eşleştirme bazlı eksik günlük satır üretimi var
- ✅ Özmal/kiralık/tedarikçi/komisyon satır ayrımı (renk kodu) var
- ✅ Satır bazında `SeferSayisi` düzenleme + kaydet var
- ✅ Tahakkuk hesaplama otomatik (serviste):
  - `TahakkukEdenKurumUcreti` = `KurumaKesilecekUcret × SeferSayisi × PuantajCarpani × ServisCarpani`
  - `TahakkukEdenTaseronUcreti` = (Komisyon/Tedarikçi) `TaseronaOdenenUcret × SeferSayisi × PuantajCarpani × ServisCarpani`
- ✅ Maliyet snapshot (`MaliyetOzmalKiralik`) otomatik doldurma tamamlandı
- ✅ Toplu satır düzenleme / toplu kaydet eklendi (seçim + toplu sefer uygula + seçilenleri kaydet)
- ✅ Toplu kaydet servis tarafında batch güncelleme ile optimize edildi (`UpdateGunlukPuantajlarAsync`)
- ✅ Operasyonel puantaj listesinde hızlı arama eklendi (güzergah/kurum/araç/şoför)
- ✅ Araç sahiplik tipi ve servis türü filtreleri eklendi
- ✅ Tablo, toplu seçim ve toplu kaydet akışı filtreli liste ile uyumlu hale getirildi

Durum: ✅ **Çözüldü**

### 3.3 🟢 HAKEDİŞ SAYFASI YENİ SİSTEME BAĞLI

Eski `FiloHakedisPage.razor` kaldırıldı. Yerine `OperasyonelHakedisPage.razor` eklendi.

Mevcut durum:
- ✅ `IOperasyonelHakedisService` üzerinden listeleme var
- ✅ Toplu Hakediş Üret (Kurum/Tedarikçi/Araç) var
- ✅ Taslak → Onayla akışı var
- ✅ Onaylı → Faturaya Dönüştür akışı var (Araç tipi hariç)
- ✅ Taslak silme / Onaylı iptal var
- ✅ Referans adı çözümleme tamamlandı (Kurum/Tedarikçi/Araç adı gösteriliyor)
- ✅ Çoklu seçim + toplu onay/faturala/sil butonları eklendi
- ✅ PDF çıktısı eklendi (satır bazlı PDF indir)

Durum: ✅ **Çözüldü**

### 3.4 🟡 TAHAKKUK OTOMATİK HESAPLANMIYOR

`FiloGunlukPuantaj` kaydedilirken tahakkuk artık servis tarafında otomatik hesaplanıyor (`FiloKomisyonService.UygulaPuantajKurallariAsync`).

Mevcut formül:
- `TahakkukEdenKurumUcreti` = `KurumaKesilecekUcret × SeferSayisi × PuantajCarpani × ServisCarpani`
- `TahakkukEdenTaseronUcreti` = (Komisyon/Tedarikçi) `TaseronaOdenenUcret × SeferSayisi × PuantajCarpani × ServisCarpani`

Durum: ✅ **Çözüldü**

### 3.5 🟢 MALİYET SNAPSHOT → PUANTAJ BAĞLANTISI

`FiloGunlukPuantaj.MaliyetOzmalKiralik` alanı artık puantaj kaydı sırasında otomatik dolduruluyor.

Mevcut davranış (`FiloKomisyonService.UygulaPuantajKurallariAsync`):
- Araç sahiplik tipi **Özmal/Kiralık** ise,
  ilgili araç + dönem (`AracId`, `Tarih.Year`, `Tarih.Month`) için `AracMaliyetSnapshot` okunur.
- Snapshot'ta `ToplamSefer > 0` ise:
  - `MaliyetOzmalKiralik = ToplamMaliyet / ToplamSefer` (2 hane yuvarlanmış)
- Snapshot yoksa veya `ToplamSefer = 0` ise:
  - `MaliyetOzmalKiralik = null`
- Tedarikçi/Komisyon araçlarda:
  - `MaliyetOzmalKiralik = null`

Durum: ✅ **Çözüldü**

### 3.6 🟢 PERSONEL PUANTAJ ENTEGRASYONU (ÇÖZÜLDÜ)

`PersonelPuantaj` (maaş bazlı, aylık) verisi artık `AracMaliyetSnapshot.SoforMaasPayi` hesaplamasına dahil ediliyor.

Mevcut davranış (`AracMaliyetService.SnapshotUretAsync`):
- İlgili araç+dönem puantajından çalışan şoförler bulunur.
- Şoförün aynı dönemde birden fazla araçta çalışması durumunda maaş, sefer oranına göre dağıtılır.
- Maaş bazında öncelik: `NetOdeme`; `NetOdeme <= 0` ise `BrutMaas`.
- Hesaplanan toplam, snapshot'a `SoforMaasPayi` olarak yazılır (2 hane yuvarlama).

Durum: ✅ **Çözüldü**

---

### 3.7 🟢 ESKİ UI SAYFALARI KALDIRILDI, MENÜ TEMİZLENDİ

NavMenu ve bağlı sayfa akışları güncellendi:
- ✅ `FiloGunlukPuantajPage.razor` kaldırıldı
- ✅ `FiloHakedisPage.razor` kaldırıldı
- ✅ `FiloPuantaj.razor` kaldırıldı
- ✅ `GuzergahForm.razor` içindeki eski puantaj yönlendirmesi kaldırıldı
- ✅ `FiloKpiDashboard.razor` içindeki eski `filo/puantaj` linki kaldırıldı
- ✅ `NavMenu.razor` içindeki `"filo-gunluk-puantaj"` string referansı temizlendi

Durum: ✅ **Çözüldü**

Not:
- Entity / Servis / Migration / DB tablolarına dokunulmadı.
- Eski sayfalar yerine yeni ekranlar (`/operasyon/puantaj`, `/operasyonel-hakedis`) aktif kullanımdadır.

---

## 4. MİMARİ KARAR KAYITLARI

| # | Karar | Gerekçe | Tarih |
|---|-------|---------|-------|
| K1 | `FiloGunlukPuantaj` tek kayıt noktası olacak | Çift kayıt riskini ortadan kaldırır | Tasarım dokümanı |
| K2 | `Hakedis` tablosu `HakedisPuantaj` tablosunun yerini alır | Daha temiz; Kurum/Tedarikçi/Araç tipi tek yapıda | Tasarım dokümanı |
| K3 | `OperasyonelHakedisService` geçerli hakediş servisi | 632 satır, tam uygulanmış, `FiloGunlukPuantaj` → `Hakedis` dönüşümü çalışıyor | Temmuz 2026 analizi |
| K4 | `AracMaliyetSnapshot` korunacak ve Araç hakediş için kaynak olacak | `FullpetFaturaDagitAsync` dahil tam çalışıyor | Önceki oturum |
| K5 | Yeni alanlar `Required` yapılmayacak | Eski kayıtlar geçerli kalsın | Tasarım dokümanı |
| K6 | `ServisKontratTip` ile `AracSahiplikTipi` farkı korunacak | Kontrat tipi ≠ araç tipi (aynı araç farklı kontratta farklı tip olabilir) | Temmuz 2026 analizi |
| K7 | Mevcut 3 puantaj UI sayfası silinecek, sıfırdan yazılacak | Sayfalar menüde yok, 1887+568+? satır bakımsız kod, temiz başlamak daha iyi | Temmuz 2026 analizi |
| K8 | `HakedisPuantaj` hattı operasyonel UI için legacy kabul edilir | `HakedisPuantajService`/`IHakedisPuantajService` ve DI kaydı kaldırıldı; rapor/denetim/dashboard/muhasebe geçişleri büyük ölçüde yeni `Hakedis` modeline taşındı, kalan tablo/sync bağımlılıkları için kontrollü temizlik gerekir | Temmuz 2026 güncel analizi |

---

## 5. ÖNERİLEN UYGULAMA SIRALAMASI

### FAZ 0 — Temizlik (TAMAMLANDI)

**Hedef:** Eski UI sayfalarını sil, build temiz kalsın, yeni sayfa için alan aç.

#### Adım 0.1 — 3 UI sayfasını sil ✅ TAMAMLANDI
- `FiloGunlukPuantajPage.razor` silindi
- `FiloHakedisPage.razor` silindi
- `FiloPuantaj.razor` silindi

#### Adım 0.2 — Kalan referansları temizle ✅ TAMAMLANDI
- `GuzergahForm.razor` eski `NavigateTo("/filo-gunluk-puantaj")` referansı kaldırıldı
- `FiloKpiDashboard.razor` eski `filo/puantaj` linki kaldırıldı
- `NavMenu.razor` içinden `"filo-gunluk-puantaj"` string'i kaldırıldı

#### Adım 0.3 — Build doğrula ✅ TAMAMLANDI
- Son durum: Build başarılı

> ✅ FAZ 0 tamamlandı. Sistem temiz durumla FAZ 1'e geçti.

---

### FAZ 1 — Yeni Operasyonel Puantaj Sayfası (SIFIRDAN)

**Hedef:** Operatör günde 5 dakikada tüm güzergah satırlarını işleyebilsin.

#### Adım 1.1 — Kontrat → Günlük Satır Otomatik Üretimi
- Sayfa açıldığında seçilen tarih için aktif `ServisKontrat`'lar listelenir
- "Bugün için satırları oluştur" butonu ile `FiloGunlukPuantaj` kayıtları taslak olarak oluşturulur
- Her satır için kontrat tipine göre `TahakkukEdenKurumUcreti` otomatik hesaplanır

#### Adım 1.2 — Tahakkuk Otomatik Hesaplama
```csharp
// SaveAsync içinde:
kayit.TahakkukEdenKurumUcreti = kontrat.GelirBirimFiyat * kayit.SeferSayisi * kayit.PuantajCarpani;
if (kontrat.Tip == ServisKontratTip.Tedarikci)
    kayit.TahakkukEdenTaseronUcreti = kontrat.GiderBirimFiyat * kayit.SeferSayisi * kayit.PuantajCarpani;
```

#### Adım 1.3 — UI Araç Tipi Renk Kodlaması
- Özmal satırlar: `table-success` (yeşil)
- Kiralık satırlar: `table-info` (mavi)
- Tedarikçi satırlar: `table-warning` (sarı) + ekstra Tedarikçi sütunu

### FAZ 2 — Hakediş Sayfası Yeni Sisteme Bağlama (SONRA)

**Hedef:** `FiloHakedisPage.razor` → `IOperasyonelHakedisService` kullanacak.

#### Adım 2.1 — Sayfa Inject Güncelleme
```razor
@inject IOperasyonelHakedisService HakedisService
```

#### Adım 2.2 — "Hakediş Üret" Butonu
- Dönem (Yıl/Ay) + Tip (Kurum/Tedarikçi/Araç) seçimi
- `OperasyonelHakedisService.KurumHakedisiUretAsync(...)` çağrısı

#### Adım 2.3 — Onay ve Faturalama Akışı
```
Taslak → Onayla → Faturaya Dönüştür
                     ↓
              Fatura.HakedisId = hakedis.Id
              Fatura.FaturaYonu = Gelir (Kurum) | Gider (Tedarikçi)
```

### FAZ 3 — Maliyet → Hakediş Bağlantısı (EN SON)

#### Adım 3.1 — MaliyetOzmalKiralik Otomatik Doldurma
Kayıt kaydedilirken:
```csharp
var snapshot = await maliyetService.GetSnapshotlarAsync(aracId, tarih.Year, tarih.Month);
if (snapshot.FirstOrDefault() is { } s && s.ToplamSefer > 0)
    kayit.MaliyetOzmalKiralik = s.SeferBasiMaliyet;
```

#### Adım 3.2 — Kar/Zarar Raporu
- `TahakkukEdenKurumUcreti - MaliyetOzmalKiralik` = Günlük Kar/Zarar
- Aylık toplama → `FiloKpiDashboard` veya ayrı sayfa

---

## 6. DOSYA REFERANSLARİ (Kritik Dosyalar)

### Entity'ler
- `MKFiloServis.Shared/Entities/FiloKomisyonPuantaj.cs` → `FiloGunlukPuantaj` sınıfı (satır 68)
- `MKFiloServis.Shared/Entities/ServisKontrat.cs` → `ServisKontratTip`, `ServisKontrat`
- `MKFiloServis.Shared/Entities/HakedisPuantaj.cs` → ESKİ hakediş modeli
- `MKFiloServis.Shared/Entities/Hakedis.cs` → YENİ hakediş modeli (kullanılacak)

### Servisler
- `MKFiloServis.Web/Services/OperasyonelHakedisService.cs` → YENİ, ÇALIŞIYOR (632 satır)
- `MKFiloServis.Web/Services/HakedisMuhasebeService.cs` → YENİ `Hakedis` modeliyle muhasebe fiş entegrasyonu
- `MKFiloServis.Web/Services/PuantajHakedisSyncService.cs` → Legacy `HakedisPuantaj` sync hattı (kademeli temizlik adayı)
- `MKFiloServis.Web/Services/FiloKomisyonService.cs` → `FiloGunlukPuantaj` CRUD
- `MKFiloServis.Web/Services/AracMaliyetService.cs` → Snapshot ve Fullpet dağıtım
- `MKFiloServis.Web/Services/ServisKontratService.cs` → Kontrat ve birim fiyat

### Sayfalar
- `MKFiloServis.Web/Components/Pages/Filo/FiloGunlukPuantajPage.razor` → ❌ FAZ 0 Adım 0.1'de silindi
- `MKFiloServis.Web/Components/Pages/Filo/FiloHakedisPage.razor` → ❌ FAZ 0 Adım 0.1'de silindi
- `MKFiloServis.Web/Components/Pages/Filo/FiloPuantaj.razor` → ❌ FAZ 0 Adım 0.1'de silindi
- `MKFiloServis.Web/Components/Pages/ServisOperasyon/PuantajDetay.razor` → Detay/onay/fatura (aktif)

### Dokümanlar
- `MKFiloServis.Web/Docs/Puantaj-Hakedis-Tasarim.md` → Orijinal tasarım dokümanı
- `MKFiloServis.Web/Docs/OPERASYONEL-PUANTAJ-DEVAM-DOSYASI.md` → **BU DOSYA**

---

## 7. DEVAM EDİLDİĞİNDE YAPILACAK İLK ADIM

"Devam edelim" dediğinde şu konudan başlayacağız:

> **FAZ 1 sonrası iyileştirme adımları:**
> 1. ✅ `OperasyonelPuantajPage.razor` aktif ve günlük satır üretimi çalışıyor
> 2. ✅ Satır bazlı ve toplu düzenleme/kaydet (batch) tamamlandı
> 3. ✅ Tahakkuk ve maliyet snapshot entegrasyonu tamamlandı
> 4. ✅ `OperasyonelHakedisPage.razor` aktif (toplu işlem + PDF)
>
> **Sonraki öncelik (öneri):**
> 1. ✅ `PuantajFinansService` legacy `HakedisPuantaj` işleme overload'u kaldırıldı; `RebuildService` akışı yalnız `Hakedis` modeliyle çalışıyor.
> 2. ✅ DenetimService içinde SnapshotTransaction eşlemesi yeni modele uygun şekilde `FaturaId` bazına geçirildi.
> 3. ✅ `PuantajFinansService` + denetim/dashboard tarafında snapshot tutarlılık uç durumları sertleştirildi (`HakedisFinans*` filtreleme, taslak/iptal dışlama, `GenelToplam`→`Tutar` fallback).
> 4. ✅ `PuantajExcelService` import hattı `HakedisPuantaj` bağımlılığından çıkarıldı (yeni `Hakedis` modeline geçirildi).
> 5. ✅ `PuantajHakedisSyncService` ve `PuantajExcelGrid` senkronizasyon/validasyon akışı yeni `Hakedis` modeline taşındı.
> 6. ✅ TestSession backup/rollback ve TopluFatura yönlendirme metni yeni `Hakedis` modeliyle hizalandı.
> 7. ✅ Operasyonel puantaj ekranında arama + sahiplik/servis türü filtreleri eklendi; tablo, toplu seçim ve toplu kaydet akışı filtreli listeyle uyumlu hale getirildi.
> 8. ▶ Yeni faz önerisi: `HakedisPuantaj` tablo katmanını arşivleme/kaldırma için migration planı çıkar (şema etkisi + geri dönüş stratejisi + veri saklama kararı).
> 9. 🟡 Operasyonel puantaj/hakediş test fazı genişletildi: Playwright smoke kapsamında ekran açılışı, filtre temizleme, varsayılan pasif buton durumları, seçim sonrası buton aktifleşmeleri, click-to-result akışları ve opsiyonel state-transition (`CRMFILO_SMOKE_ALLOW_MUTATION=true`) kontrolleri eklendi (Program.cs). Sonraki adım: rebuild + dashboard + denetim tutarlılık senaryoları için servis/integration testleri.

---

## 8. BAĞLAM SORU-CEVAP (Hızlı Referans)

| Soru | Cevap |
|------|-------|
| Günlük puantaj tablosu hangisi? | `FiloGunlukPuantaj` (FiloKomisyonPuantaj.cs içinde) |
| Kontrat tipi enum nerede? | `ServisKontrat.cs` → `ServisKontratTip` (Ozmal/Kiralik/Tedarikci) |
| Araç tipi enum nerede? | `Arac.cs` → `AracSahiplikTipi` (Ozmal=1, Kiralik=2, Tedarikci=5) |
| Çalışan hakediş servisi hangisi? | `OperasyonelHakedisService` (IOperasyonelHakedisService) |
| Eski hakediş servisi hangisi? | `HakedisPuantajService` (IHakedisPuantajService) kaldırıldı; legacy hat ağırlıklı `HakedisPuantaj` tabloları (`ApplicationDbContext` + migration geçmişi) üzerinden devam ediyor |
| Maliyet snapshot nerede? | `AracMaliyetService` + `AracMaliyetSnapshot` entity |
| DB provider? | PostgreSQL (Host=localhost, Port=5432, DB=MKFiloServis) |
| Build durumu? | ✅ Başarılı (Temmuz 2026 — son geçiş adımları + PlaywrightSmoke deterministik modda Toplu Hakediş Üret `Hatalı=0` metrik doğrulaması sonrası doğrulandı) |
| Fullpet dağıtımı nasıl? | `IAracMaliyetService.FullpetFaturaDagitAsync(...)` |
| Opsiyonel mutasyonlu smoke nasıl açılır? | `CRMFILO_SMOKE_ALLOW_MUTATION=true` **veya** CLI'da `--allow-mutation` (hakediş state-transition adımını SKIP yerine aktif çalıştırır) |
| Demo veri hazırlıklı smoke nasıl açılır? | `CRMFILO_SMOKE_PREPARE_DEMO=true` **veya** CLI'da `--prepare-demo-data` (login sonrası `/admin/test` ekranında reset+seed adımı çalıştırılır; bu modda bazı hakediş kontrolleri SKIP yerine FAIL'e çevrilir ve dağılım ön-kontrolü yapılır) |
| Deterministik hakediş filtresi nasıl verilir? | CLI: `--hakedis-year=2026 --hakedis-month=7 --hakedis-tip=Kurum` veya env: `CRMFILO_SMOKE_HAKEDIS_YEAR`, `CRMFILO_SMOKE_HAKEDIS_MONTH`, `CRMFILO_SMOKE_HAKEDIS_TIP` |

---

## 9. YENİ FAZ ÖNERİSİ — LEGACY TABLO ARŞİVLEME + ENTEGRASYON TESTLERİ

### 9.1 `HakedisPuantaj` Tablo Katmanı için Güvenli Geçiş Planı

**Hedef:** Aktif iş akışlarını bozmadan, legacy `HakedisPuantaj*` tablolarını üretim akışından çıkarıp arşiv/uyumluluk seviyesine almak.

#### Aşama A — Envanter ve Donma
1. `ApplicationDbContext` ve migration geçmişi dışında kalan tüm `HakedisPuantaj*` kod referanslarını tekrar tara.
2. Yeni iş geliştirmelerinde `HakedisPuantaj*` kullanımını yasakla (yalnızca `Hakedis/HakedisDetay`).
3. Dokümanda “legacy donma tarihi” notu ekle.

#### Aşama B — Veri Saklama Kararı
1. **Arşivle ve tut** (önerilen): tablolar DB’de kalsın, uygulama yazmasın.
2. **Tam kaldır**: veri yedek/export sonrası migration ile drop.
3. Karar öncesi zorunlu çıktı:
   - Son 12 ay kayıt adedi
   - Denetim/muhasebe bağımlılık teyidi
   - Geri dönüş senaryosu (rollback)

#### Aşama C — Migration Stratejisi
1. `HakedisPuantaj*` tablolarına yazan kod yollarını kaldırdıktan sonra migration üret.
2. Seçime göre:
   - Arşiv modelinde: tablo kalır, DbSet kaldırılmazsa `[Obsolete]` notu + kullanım engeli.
   - Kaldırma modelinde: FK sırasına dikkat ederek kontrollü drop migration.
3. Migration sonrası smoke kontrol:
   - Operasyonel puantaj liste/kaydet
   - Operasyonel hakediş üret/onay/faturala
   - Rebuild + Denetim + Finans dashboard

### 9.2 Operasyonel Akış için Temel Entegrasyon Test Seti (Öneri)

#### Senaryo Grubu 1 — Üretim Akışı
- `FiloGunlukPuantaj` verisinden `Hakedis` üretimi
- Kurum/Tedarikçi/Araç tip ayrımı
- Toplam/GenelToplam fallback doğrulaması

#### Senaryo Grubu 2 — Finans Zinciri
- `PuantajFinansService.IsleAsync(Hakedis)` ile fatura oluşumu
- SnapshotTransaction (`HakedisFinans*`) yazımı
- Snapshot gelir/gider delta tutarlılığı

#### Senaryo Grubu 3 — Rebuild/Denetim/Dashboard
- Rebuild sonrası snapshot ve hakediş toplamlarının eşitliği
- Denetim skorunun false-positive üretmemesi (taslak/iptal hariç)
- Dashboard fallback’inde `GenelToplam -> Tutar` davranışı

#### Senaryo Grubu 4 — UI Kritik Akışları (Blazor)
- ✅ Playwright smoke: Operasyonel puantaj ekran açılışı (`/operasyon/puantaj`) doğrulandı
- ✅ Playwright smoke: Operasyonel puantaj filtre temizleme aksiyonu doğrulandı (`Arama yazın...` + `Temizle`)
- ✅ Playwright smoke: Operasyonel puantaj toplu aksiyon butonları başlangıçta pasif doğrulandı (`Toplu Sefer Uygula`, `Seçilenleri Kaydet`)
- ✅ Playwright smoke: Operasyonel puantajda seçim + sefer değeri sonrası `Toplu Sefer Uygula` click-to-result akışı doğrulandı (uygun veri yoksa kontrollü SKIP)
- ✅ Playwright smoke: Operasyonel puantajda satır seçimi sonrası `Seçilenleri Kaydet` butonu aktifleşmesi doğrulandı (uygun veri yoksa kontrollü SKIP)
- ✅ Playwright smoke: Operasyonel hakediş ekran açılışı (`/operasyonel-hakedis`) doğrulandı
- ✅ Playwright smoke: Operasyonel hakediş dönem validasyonu doğrulandı (`Ay=13` → `Geçersiz dönem seçimi.`)
- ✅ Playwright smoke: Operasyonel hakediş toplu aksiyon butonları başlangıçta pasif doğrulandı (`Toplu Onayla`, `Toplu Faturala`, `Toplu Sil`)
- ✅ Playwright smoke: Operasyonel hakedişte `Tümünü Seç` / `Seçimi Temizle` etkileşimi doğrulandı (kayıt yoksa kontrollü SKIP)
- ✅ Playwright smoke: Operasyonel hakedişte satır seçimi sonrası en az bir toplu aksiyon butonu aktifleşmesi doğrulandı (duruma göre kontrollü SKIP)
- ✅ Playwright smoke: Operasyonel hakedişte opsiyonel state-transition akışı eklendi (`CRMFILO_SMOKE_ALLOW_MUTATION=true` veya `--allow-mutation` ile toplu onay/faturalama/sil aksiyonlarından uygun olanı çalıştırıp sonuç mesajı doğrulanır; aksi durumda kontrollü SKIP)
- ✅ Playwright smoke: Opsiyonel demo veri hazırlık adımı eklendi (`CRMFILO_SMOKE_PREPARE_DEMO=true` veya `--prepare-demo-data` ile login sonrası `/admin/test` üzerinden reset+seed çalıştırılır)
- ✅ Playwright smoke: Deterministik veri beklenti kuralı eklendi (demo seed açıkken hakediş senaryolarındaki “kayıt yok/uygun aksiyon yok” durumları SKIP yerine FAIL olarak değerlendirilir)
- ✅ Playwright smoke: Demo seed sonrası hakediş listesinde deterministik durum dağılımı ön-kontrolü eklendi (`Taslak`/`Onaylandi` varlığı doğrulanır)
- ✅ Playwright smoke: Dağılım kontrolü metin taramadan çıkarılıp durum kolonu badge locator’ına (`td:nth-child(9) span.badge`) taşındı
- ✅ Playwright smoke: Deterministik dağılım kontrolü dönem/tip filtreleriyle daraltıldı (Kurum + seçili ay/yıl + Listele)
- ✅ Playwright smoke: Dağılım kontrolünden önce seçili dönem/tip için `Toplu Hakediş Üret` tetiklenerek test verisi daha deterministik hale getirildi
- ✅ Playwright smoke: `Toplu Hakediş Üret` başarı mesajı parse edilerek `Üretilen` adedinin `> 0` olması zorunlu kılındı (aksi durumda FAIL)
- ✅ Playwright smoke: `Toplu Hakediş Üret` özet metriklerinde `Hatalı=0` beklentisi eklendi (deterministik modda hata toleransı kaldırıldı; `Atlanan` metrikleri loglanır)
- ✅ Playwright smoke: Deterministik dağılım filtresi parametrik hale getirildi (CLI/env ile yıl/ay/tip verilebilir)
- ✅ Playwright smoke: Toplu üretim öncesi/sonrası hakediş ID kıyası eklendi (`Üretilen>0` iken yeni ID oluşumu zorunlu) ve run-bazlı izolasyon sinyali güçlendirildi
- ✅ Playwright smoke: Deterministik modda bulk-action/state-transition adımları yeni üretilen hakedişleri (`ID > oncekiMaxId`) hedefleyecek şekilde ID-eşikli seçime taşındı
- ✅ Playwright smoke: Hakediş bulk-action ve state-transition senaryoları da aynı deterministik dönem/tip filtresine bağlandı (demo seed modunda tek veri kümesi hedefleniyor)
- ⏳ Sonraki adım: gerçek fixture/marker (örn. test run id) alanı ile doğrudan etiketleme yapıp ID kıyasına ek olarak marker-bazlı filtre doğrulaması eklemek

**Başarı ölçütü:** Yukarıdaki test seti yeşil olmadan `HakedisPuantaj` tablo katmanında fiziksel kaldırma yapılmaz.

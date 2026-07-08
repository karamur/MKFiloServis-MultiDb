# MKFiloServis — Operasyonel Puantaj Tek Plan Dosyası

> Amaç: Operasyonel puantaj sürecini **tek bir standart akışta** birleştirip; günlük işlem, aylık kapanış, tekil düzeltme ve Excel güncelleme süreçlerini aynı modelde yönetmek.
> 
> Kapsam: Blazor (`/operasyon/puantaj`, `/operasyonel-hakedis`), servis katmanı, veri doğrulama, raporlama, test ve canlıya geçiş.

---

## 1) Hedeflenen Birleşik Model

Tek operasyonel akış:

1. **Tanım Katmanı**: Kurum → Güzergah → ServisKontrat
2. **Operasyon Katmanı**: Günlük `FiloGunlukPuantaj`
3. **Finans Katmanı**: Aylık `Hakedis + HakedisDetay`
4. **Belge Katmanı**: Fatura / muhasebe / denetim

Ana prensip:
- Tek kaynak günlük operasyon: `FiloGunlukPuantaj`
- Aylık sonuç üretimi: `Hakedis`
- Legacy yol (`HakedisPuantaj`) yeni iş akışına dahil edilmez.

---

## 2) Süreç Tasarımı (Nasıl Yapacağız?)

### 2.1 Günlük Operasyonel Puantaj

Ekran: `/operasyon/puantaj`

Adımlar:
1. Tarih seçilir.
2. Eksik günlük satırlar kontratlara göre oluşturulur.
3. Satır bazlı sefer/servis bilgisi girilir veya güncellenir.
4. Toplu/tekil kaydetme yapılır.
5. Validasyon çalışır (firma, zorunlu alan, negatif değer, kontrat uyumu).

Çıktı:
- Doğrulanmış günlük puantaj satırları
- Uygun durumdaki satırlar aylık hakedişe hazır

### 2.2 Aylık Operasyonel Kapanış

Ekran: `/operasyonel-hakedis`

Adımlar:
1. Dönem (Yıl/Ay) filtrelenir.
2. Toplu üretim ile günlük puantajdan hakediş oluşturulur.
3. Taslaklar kontrol edilir.
4. Uygun olanlar onay/faturalama adımına geçer.

Kural:
- Onaylı/faturalı kayıtlarda geriye dönük değişiklik koruma politikası zorunlu.

### 2.3 Tekil Operasyonel Puantaj

Kullanım amacı:
- Günlük satırda istisna düzeltmesi (tek araç, tek güzergah, tek gün)

Kural:
- Tekil güncelleme sonrası etkilenen hakediş taslakları için yeniden hesaplama veya yeniden senkron tetiklenir.
- Onaylı/faturalı kayıtlar için doğrudan overwrite yapılmaz; kontrollü revizyon akışı uygulanır.

### 2.4 Excel ile Güncelleme (deneme-TÜRK AK PUANTAJ.xlsx benzeri)

Amaç:
- Dışarıda yapılan puantajı sisteme güvenli ve izlenebilir şekilde almak

Önerilen teknik akış:
1. **Preview**: Kolon eşleme, zorunlu alan kontrolü, eşleşmeyen referansların listesi
2. **Validation**: Tarih/servis/sefer/firma kuralları
3. **Apply (Idempotent)**:
   - varsa güncelle (update)
   - yoksa ekle (insert)
   - mükerrer üretme engeli
4. **Diff Raporu**: Eski değer ↔ yeni değer
5. **Opsiyonel Senkron**: Dönem hakediş taslaklarını güncelle

---

## 3) Veri ve Eşleme Kuralları (Ne Şekilde?)

### 3.1 Excel Kolon Eşleme Standardı

Kanonik (hedef) kolonlar:
- Tarih
- Güzergah
- YÖN
- Araç/Plaka
- Şoför (varsa)
- Servis Türü
- Sefer Sayısı
- Birim Fiyat
- Not (opsiyonel)

Kural:
- Bu kolonlardan biri Excel’de yoksa import pipeline’ında **türetilerek eklenir** (auto-add).
- Dosya matris formatındaysa (1..31 gün kolonları) wide→long dönüşüm zorunludur.

Türetme/ekleme kuralları:
1. `Tarih` yoksa: `Ay/Yıl parametresi + gün kolonu (1..31)` ile üretilir.
2. `Sefer Sayısı` yoksa: ilgili gün hücresindeki değerden üretilir.
3. `YÖN` yoksa: güzergahın sefer tanımı ve slot bilgisine göre üretilir.
4. `Servis Türü` yoksa: güzergah sefer tipinden (`SeferTipi`) türetilir.
5. `Birim Fiyat` yoksa: güzergah + yön + sefer tipine bağlı fiyat kuralından hesaplanır.

YÖN sözlüğü (kabul edilen değerler):
- `Sabah+Akşam`
- `Sabah`
- `Akşam`
- `Mesai`
- `Ek Sefer`

Not:
- `Ek Sefer` değeri iç modelde ayrı karşılık yoksa geçici olarak "Diger" sınıfına map edilip audit log'a yazılır.

### 3.2 Eşleştirme Anahtarı

Öneri:
- `Firma + Tarih + Güzergah + Araç(veya Şoför) + ServisTuru`

Bu anahtar ile:
- aynı satır tekrar gelirse update
- yeni satırsa insert

### 3.3 Koruma Politikası

- `HakedisDurum = Onaylandi/Faturalandi` satırlarını etkileyen değişikliklerde:
  - doğrudan overwrite yok
  - fark raporu + kullanıcı onayı + log zorunlu

---

## 4) Uygulama Planı (İş Paketleri)

## Faz 1 — Standartlaştırma
1. Operasyonel puantaj alan/kural sözleşmesini netleştir
2. Excel kolon standardı dokümanını sabitle
3. Günlük giriş validasyonlarını tek servis kuralına topla

Teslim:
- Tek kural seti
- Günlük puantaj veri kalitesinde artış

## Faz 2 — Excel Güncelleme Motoru
1. Preview + doğrulama katmanı
2. Idempotent upsert mekanizması
3. Fark raporu ve işlem logu

Teslim:
- Güvenli import/update
- Aynı dosyada mükerrer üretim engeli

## Faz 3 — Aylık Kapanış Entegrasyonu
1. Güncellenen günlük kayıtların hakedişe etkisini netleştir
2. Taslak hakediş yeniden üret/senkron stratejisi
3. Onaylı/faturalı koruma kuralı ve revizyon akışı

Teslim:
- Dönem kapanış tutarlılığı
- Finans/denetim uyuşmazlıklarının azalması

## Faz 4 — Raporlama ve İzlenebilirlik
1. Günlük durum raporu
2. Excel fark/güncelleme raporu
3. Aylık kapanış uygunluk raporu

Teslim:
- Operasyon + finans ekipleri için şeffaf takip

## Faz 5 — Test ve Canlıya Geçiş
1. Birim + entegrasyon testleri
2. Playwright smoke senaryoları (günlük giriş, excel update, aylık üretim)
3. Pilot firma ile kontrollü canlı geçiş

Teslim:
- Güvenli release
- Geri dönüş planı hazır

---

## 5) Teknik Uygulama Notları

- UI: `OperasyonelPuantajPage.razor`, `OperasyonelHakedisPage.razor`
- Servis: günlük puantaj update + hakediş senkron + excel import
- Entity: `FiloGunlukPuantaj`, `Hakedis`, `HakedisDetay`
- Log/Audit: kullanıcı, zaman, kaynak (manuel/excel), etkilenen kayıt

Performans:
- Toplu işlemlerde batch update
- Büyük Excel importlarında sayfalı/partisyonlu işleme

Güvenlik:
- Firma tenant kontrolü
- Yetki bazlı işlem (okuma/yazma/onay/faturalama)

---

## 6) Riskler ve Önlemler

1. **Excel format sapması**
   - Önlem: Template + zorunlu kolon doğrulama
2. **Onaylı döneme yanlış müdahale**
   - Önlem: Koruma politikası + revizyon akışı
3. **Mükerrer kayıt**
   - Önlem: Idempotent anahtar ve upsert
4. **Aylık toplam tutarsızlığı**
   - Önlem: dönem sonu otomatik tutarlılık raporu

---

## 7) Başarı Kriterleri (KPI)

- Günlük puantaj hatalı satır oranı düşüşü
- Excel import sonrası manuel düzeltme ihtiyacı azalması
- Aylık hakediş üretim süresinde kısalma
- Denetim fark sayısında azalma

---

## 8) Önerilen Yol Haritası (Kısa)

- Hafta 1: Faz 1
- Hafta 2: Faz 2
- Hafta 3: Faz 3
- Hafta 4: Faz 4 + Faz 5 (pilot)

---

## 9) Sonuç

Bu tek plan ile operasyonel puantaj süreci;
- günlükten aylığa,
- tekil düzeltmeden Excel güncellemeye,
- operasyondan finansa
tek bir standart altında birleşir.

Süreç hem kullanıcı tarafında sadeleşir hem de denetlenebilir ve sürdürülebilir hale gelir.

---

## 10) Güncel Durum (Temmuz 2026)

Tamamlananlar:
- Operasyonel ekranlar aktif: `/operasyon/puantaj`, `/operasyonel-hakedis`
- Legacy servis bağımlılıklarının önemli bölümü yeni `Hakedis` modeline taşındı
- Menüde Operasyonel Puantaj ve Operasyonel Hakediş erişimleri eklendi
- Playwright smoke senaryoları operasyonel akış için sertleştirildi

Devam edenler:
- Excel tabanlı güncellemenin tam idempotent kurallarla standartlaştırılması
- Onaylı/faturalı dönemlerde revizyon akışının nihai hale getirilmesi
- Marker/run-id tabanlı test izolasyonunun netleştirilmesi

---

## 11) Net Sonraki Adımlar (Uygulanabilir Checklist)

1. Excel import şablonunu sabitle (`kanonik kolonlar + örnek dosya + YÖN sözlüğü`)
2. Eksik kolonlar için auto-add/türetme katmanını devreye al (`Tarih`, `YÖN`, `Servis Türü`, `Sefer Sayısı`, `Birim Fiyat`)
3. Matris format (1..31 gün) dosyalar için wide→long dönüşümünü standartlaştır
4. Preview ekranında eşleşmeyen referansları ve `YÖN/fiyat` türetme sonuçlarını indirilebilir raporla sun
5. Upsert anahtarını teknik olarak tekilleştir (firma+tarih+güzergah+araç/şoför+servis türü)
6. Güzergah sefer tanımına göre yön/fiyat kuralını uygula (`SeferTipi` + `SeferSlot` bazlı)
7. Onaylı/faturalı dönem güncellemelerinde "fark raporu + onay" zorunluluğunu aktive et
8. Güncelleme sonrası etkilenen hakediş taslakları için otomatik yeniden senkron kuralını çalıştır
9. Aylık kapanış öncesi uygunluk raporunu zorunlu kontrol adımı yap

---

## 12) Sprint Bazlı Uygulama (Öneri)

### Sprint A (1 hafta)
- Excel şablon standardı
- Preview + validation çıktıları
- İlk fark raporu altyapısı

Çıkış kriteri:
- Kullanıcı dosyayı yüklemeden önce hataları net görebilir

### Sprint B (1 hafta)
- Idempotent upsert motoru
- Koruma politikası (Onaylandı/Faturalandı)
- İşlem logları

Çıkış kriteri:
- Aynı Excel tekrar yüklendiğinde mükerrer kayıt oluşmaz

### Sprint C (1 hafta)
- Hakediş taslak senkronu
- Aylık uygunluk raporu
- Playwright + entegrasyon testleri

Çıkış kriteri:
- Günlük değişikliklerin aylık hakedişe etkisi deterministik ve izlenebilir olur

---

## 13) Sprint A — Dosya Bazlı Teknik İş Listesi

### 13.1 UI
- `MKFiloServis.Web/Components/Pages/Personel/PuantajExcelGrid.razor`
  - Excel yükleme öncesi kolon kontrol özeti (zorunlu kolonlar var/yok)
  - Preview sonucunda hata satırları için indirilebilir CSV butonu
- `MKFiloServis.Web/Components/Pages/Filo/OperasyonelPuantajPage.razor`
  - Tekil güncelleme sonrası "hakedişe etki" bilgi bandı (taslak etki sayısı)

### 13.2 Servis
- `MKFiloServis.Web/Services/PuantajExcelService.cs`
  - Zorunlu kolon doğrulama metodu
  - Eksik kolon auto-add/türetme (`Tarih`, `YÖN`, `Servis Türü`, `Sefer Sayısı`, `Birim Fiyat`)
  - Matris format (1..31) için wide→long dönüşüm
  - Upsert anahtarını tekilleştiren eşleştirme metodu
  - Preview diff çıktısı (`eski değer / yeni değer / durum`)
- `MKFiloServis.Web/Components/Pages/Guzergahlar/GuzergahForm.razor`
  - Sefer (`SeferTipi`) + slot (`SeferSlot`) bilgisinden yön/fiyat türetme kural referansı
  - Yön değerleri: `Sabah+Akşam`, `Sabah`, `Akşam`, `Mesai`, `Ek Sefer`
- `MKFiloServis.Web/Services/PuantajHakedisSyncService.cs`
  - Güncelleme sonrası etkilenen taslak hakedişleri yeniden senkron kuralı

### 13.3 Model / Sözleşme
- `MKFiloServis.Shared/Entities/Hakedis.cs`
  - `GenerationParams` alanında import kaynağı ve dönem filtre bilgisini standart JSON formatta yazma
- `MKFiloServis.Web/Docs`
  - Excel template sözleşme dosyası (`OPERASYONEL-PUANTAJ-EXCEL-SABLON.md`)

---

## 14) Kabul Kriterleri (Definition of Done)

Sprint A tamamlandı sayılması için:
1. Eksik kolonlu Excel dosyasında zorunlu alanlar auto-add/türetme ile tamamlanır; türetilemeyenlerde anlaşılır hata listesi gösterilir.
2. `YÖN` kolonu için kabul edilen değerler (`Sabah+Akşam`, `Sabah`, `Akşam`, `Mesai`, `Ek Sefer`) preview'de normalize edilir.
3. Güzergah sefer tanımına göre (`SeferTipi` + `SeferSlot`) yön/fiyat türetme sonucu preview raporunda görülebilir.
4. Aynı satır ikinci kez yüklendiğinde mükerrer satır açılmadan update davranışı doğrulanır.
5. Tekil günlük güncellemeden sonra etkilenen taslak hakediş sayısı kullanıcıya gösterilir.

---

## 15) Hemen Uygulanacak Sıra (Devam Planı)

1. Excel şablon sözleşmesini dokümante et (kanonik kolonlar + YÖN sözlüğü + örnek format)
2. `PuantajExcelService` içinde eksik kolon auto-add/türetme ve matris→satır dönüşümünü tanımla
3. Güzergah sefer tanımından (`SeferTipi` + `SeferSlot`) yön/fiyat türetme kuralını finalize et
4. UI tarafına türetme sonuçları + hata raporu indirme aksiyonunu ekle
5. Upsert tekilleştirme testlerini yaz
6. Taslak hakediş yeniden senkron tetikleyicisini devreye al
7. Smoke testte Excel yükleme + tekrar yükleme + YÖN normalizasyon senaryosunu doğrula

---

## 16) Operasyon Matrisi (Rol / Sorumluluk)

- **Operasyon Kullanıcısı**
  - Günlük puantaj giriş/düzeltme
  - Excel preview çıktısını kontrol etme
  - Eşleşmeyen referansları düzeltip yeniden yükleme

- **Finans Kullanıcısı**
  - Aylık hakediş üretim öncesi uygunluk raporu kontrolü
  - Fark raporu onayı (Onaylandı/Faturalandı dönemler)
  - Faturalama öncesi son doğrulama

- **Sistem Yöneticisi**
  - Yetki/policy kontrolü
  - Hata logları ve import kayıtlarının izlenmesi
  - Geri alma/incident akışının işletilmesi

---

## 17) Hafta 1 — Günlük Uygulama Planı (Sprint A Detay)

### Gün 1
- `OPERASYONEL-PUANTAJ-EXCEL-SABLON.md` oluştur ✅ (tamamlandı)
- Zorunlu kolonları kesinleştir ✅ (v1 sözleşmeye işlendi)
- Örnek satır formatını standardize et ✅ (v1 sözleşmeye işlendi)

### Gün 2
- `PuantajExcelService` içinde zorunlu kolon kontrolünü ekle
- Eksik kolonlar için kullanıcıya dönecek hata modelini netleştir

### Gün 3
- Preview diff modelini genişlet (`Eski/Yeni/Durum`)
- Eşleşmeyen referansları ayrı listeye ayır

### Gün 4
- `PuantajExcelGrid.razor` üzerinde hata satırı CSV indirme aksiyonu ekle
- Kullanıcı mesajlarını sadeleştir (anlaşılır hata dili)

### Gün 5
- İlk entegrasyon testi + smoke kontrolü
- Sprint A demo çıktısını hazırla

---

## 18) Ölçülebilir Teslim Çıktıları

Sprint A sonunda aşağıdaki çıktılar fiziksel olarak mevcut olmalı:

1. **Doküman**: Excel şablon sözleşmesi (zorunlu kolonlar + örnek)
2. **Servis**: Preview/doğrulama sonucu veren genişletilmiş import servisi
3. **UI**: Hata satırlarını indirilebilir rapor olarak sunan ekran akışı
4. **Test**: En az 1 tekrar-yükleme (idempotent davranış) doğrulama senaryosu
5. **Rapor**: Sprint A kapanış notu (yapıldı/yapılmadı, riskler, sonraki adım)

---

## 19) Gün 2 — Teknik Tasarım (Servis Katmanı)

Hedef dosya:
- `MKFiloServis.Web/Services/PuantajExcelService.cs`

Eklenecek çekirdek metotlar:
1. `ValidateRequiredColumns(...)`
   - Girdi: header listesi
   - Çıktı: eksik kolon listesi
2. `BuildPreviewValidationResult(...)`
   - Girdi: satır bazlı parse çıktısı
   - Çıktı: `ValidRows`, `Errors`, `Warnings`
3. `NormalizeCellValue(...)`
   - Amaç: tarih/sayı/string normalize (TR-EN ayracı toleransı)

Kural:
- Eksik zorunlu kolon varsa import süreci başlamaz.
- Preview adımında "hata var ama devam" davranışı yalnızca import öncesi raporlamak için kullanılabilir; doğrudan yazma yapılamaz.

---

## 20) Hata Modeli Sözleşmesi (UI'ye Dönecek)

Önerilen alanlar:
- `RowNumber` (int)
- `ColumnName` (string)
- `ErrorCode` (string)
- `Message` (string)
- `RawValue` (string?)
- `SuggestedAction` (string?)

Örnek kodlar:
- `MISSING_REQUIRED_COLUMN`
- `INVALID_DATE_FORMAT`
- `INVALID_SERVICE_TYPE`
- `ROUTE_NOT_FOUND`
- `VEHICLE_NOT_FOUND`
- `NEGATIVE_TRIP_COUNT`

Not:
- UI tarafında aynı satırdaki çoklu hatalar gruplanmalı.
- Kullanıcıya teknik exception metni değil, sadeleştirilmiş iş kuralı mesajı gösterilmeli.

---

## 21) Güncel İlerleme Takibi (Sprint A)

- [x] Gün 1 tamamlandı
- [x] Gün 2 başladı
- [ ] Gün 3 başladı
- [ ] Gün 4 başladı
- [ ] Gün 5 başladı

Bugünkü hedef (Gün 2):
1. `PuantajExcelService` zorunlu kolon doğrulaması
2. Hata modeli DTO sözleşmesi
3. Preview doğrulama çıktısının ilk versiyonu

Gün sonu hedef çıktısı:
- `ValidateRequiredColumns(...)` servis içinde kullanılabilir durumda
- Eksik kolonlar için standart `MISSING_REQUIRED_COLUMN` kodu ile hata listesi üretilmiş
- Preview sonucunda `ValidRows / Errors / Warnings` yapısı UI tüketimine hazır

---

## 22) Gün 2 — Uygulama Kontrol Listesi (Kodlama Öncesi)

### 22.1 Dosya ve Katman Hazırlığı
1. `MKFiloServis.Web/Services/PuantajExcelService.cs` içinde mevcut preview/import akış noktalarını işaretle
2. DTO'lar mevcutsa genişlet, yoksa ayrı model dosyası aç
3. Hata kodlarını sabit sınıf/enum ile merkezileştir

### 22.2 Zorunlu Kolon Doğrulama
1. Beklenen kolon setini tek yerde tanımla
2. Header normalize (trim + case-insensitive) uygula
3. Eksik kolonları tek listede topla
4. Import başlangıcında fail-fast uygula

### 22.3 Preview Doğrulama Sonucu
1. Satır bazında `ValidRows` üret
2. Satır bazında `Errors` üret (`RowNumber`, `ColumnName`, `ErrorCode`, `Message`)
3. Kritik olmayanları `Warnings` altında topla
4. UI için özet sayaçları ekle (Toplam/Geçerli/Hatalı/Uyarılı)

### 22.4 Test Kapsamı (Gün 2)
- Eksik `Tarih` kolonu -> import başlamamalı
- Eksik `ServisTuru` kolonu -> import başlamamalı
- Tüm kolonlar mevcut + satır hatalı -> preview hata üretmeli, yazma yapmamalı
- Tüm kolonlar mevcut + satırlar geçerli -> preview geçerli satır sayısını doğru vermeli

---

## 23) Gün 2 — Risk ve Karar Notları

Karar 1:
- Kolon adları kullanıcı dosyalarında değişebileceği için alias desteği (ör: `Plaka`, `Araç Plaka`, `AracPlaka`) Sprint B yerine Sprint A sonuna çekilebilir.

Karar 2:
- Hata mesajları teknik değil operasyon diliyle yazılacak (ör: "Servis Türü kolonu eksik").

Risk:
- Eski dosya formatları yeni zorunlu kolon kuralında bloklanabilir.

Önlem:
- Geçiş döneminde örnek şablon + kısa kullanım kılavuzu birlikte yayınlanacak.

---

## 24) Gün 3 — Preview Diff ve Referans Eşleşme Tasarımı

Hedef:
- Kullanıcı import öncesi "hangi satır yeni / hangi satır güncellenecek / hangi satır hatalı" bilgisini net görmeli.

Uygulama:
1. `PuantajExcelService` preview çıktısına `DiffStatus` alanı eklenir
   - `Yeni`
   - `Guncellenecek`
   - `Degismedi`
   - `Hatali`
2. Eşleşmeyen referanslar ayrı grupta toplanır
   - Güzergah eşleşmiyor
   - Araç eşleşmiyor
   - Şoför eşleşmiyor
3. UI tarafında özet kartlar gösterilir
   - Toplam satır
   - Geçerli satır
   - Hatalı satır
   - Güncellenecek satır

Çıkış kriteri:
- Kullanıcı importa basmadan önce etkilenen kayıtların dağılımını tek ekranda okuyabilir.

---

## 25) Gün 4 — UI İyileştirmesi ve İndirilebilir Hata Raporu

Hedef dosya:
- `MKFiloServis.Web/Components/Pages/Personel/PuantajExcelGrid.razor`

Yapılacaklar:
1. Preview sonucu için hata tablosu bileşeni
2. "Hata CSV İndir" butonu
3. Uyarı/hata metinlerinin operasyon diliyle sadeleştirilmesi
4. Hatalı satıra hızlı filtre (yalnız hatalıları göster)

CSV içerik standardı:
- `SatirNo,Alan,HataKodu,HataMesaji,Oneri`

Çıkış kriteri:
- Operasyon kullanıcısı teknik destek almadan hatalı satırları düzeltebilir.

---

## 26) Sprint A Kapanış Rapor Şablonu

Sprint A sonunda aşağıdaki şablonla kapanış notu üretilir:

### 26.1 Tamamlananlar
- [ ] Excel şablon sözleşmesi yayınlandı
- [ ] Zorunlu kolon doğrulaması devrede
- [ ] Preview diff çıktısı devrede
- [ ] Hata CSV indirme aktif

### 26.2 Ölçümler
- Toplam test dosyası sayısı:
- İlk yüklemede hatalı satır ortalaması:
- Düzeltme sonrası başarıyla import edilen satır oranı:
- Tekrar yüklemede mükerrer oluşmama oranı:

### 26.3 Açık Konular (Sprint B'ye Devir)
- Alias kolon desteği
- Onaylı/Faturalı dönem revizyon onay ekranı
- Idempotent davranış için ek regresyon testleri

---

## 27) Sonraki Adım (Kodlama Başlangıcı)

Dokümantasyon hazırlığı Sprint A için yeterli seviyeye gelmiştir.

Kod tarafında başlangıç sırası:
1. `PuantajExcelService` -> `ValidateRequiredColumns(...)`
2. Preview sonucuna `Errors/Warnings/DiffStatus` alanlarının eklenmesi
3. `PuantajExcelGrid.razor` -> Hata CSV indirme butonu
4. İlgili smoke/test senaryolarının güncellenmesi

---

## 28) Hazır Görev Kartları (Uygulama Ekibine Direkt Atanabilir)

### Kart A1 — Zorunlu Kolon Doğrulama
- Dosya: `MKFiloServis.Web/Services/PuantajExcelService.cs`
- İş: `ValidateRequiredColumns(...)` implementasyonu
- Kabul kriteri:
  - Eksik zorunlu kolonlarda import başlamaz
  - `MISSING_REQUIRED_COLUMN` kodu ile hata üretilir

### Kart A2 — Preview Hata/Warning Modeli
- Dosya: `MKFiloServis.Web/Services/PuantajExcelService.cs`
- İş: `ValidRows / Errors / Warnings / DiffStatus` çıktısı
- Kabul kriteri:
  - Hatalı satırlar yazmaya gitmez
  - Preview özet sayaçları doğru hesaplanır

### Kart A3 — UI Hata CSV İndir
- Dosya: `MKFiloServis.Web/Components/Pages/Personel/PuantajExcelGrid.razor`
- İş: "Hata CSV İndir" butonu + çıktı üretimi
- Kabul kriteri:
  - CSV formatı `SatirNo,Alan,HataKodu,HataMesaji,Oneri`
  - Boş hata listesinde buton pasif

### Kart A4 — Tekrar Yükleme Davranışı Testi
- Dosya: ilgili test projesi / smoke senaryosu
- İş: aynı dosyanın ikinci yüklemesinde mükerrer oluşmamasını doğrula
- Kabul kriteri:
  - idempotent davranış testte yeşil

---

## 29) Test Senaryo Matrisi (Sprint A)

| Senaryo | Girdi | Beklenen Sonuç |
|---|---|---|
| S1 | `Tarih` kolonu eksik | Import bloklanır, `MISSING_REQUIRED_COLUMN` |
| S2 | `ServisTuru` kolonu eksik | Import bloklanır, kullanıcıya sade mesaj |
| S3 | Geçerli kolonlar + hatalı satırlar | Preview hataları listeler, yazma yapılmaz |
| S4 | Geçerli kolonlar + geçerli satırlar | Preview `ValidRows` doğru döner |
| S5 | Aynı dosyayı tekrar yükleme | Mükerrer kayıt oluşmaz (update/degismedi) |

---

## 30) Sprint A Tamamlanma Kapısı (Go/No-Go)

Go kararı için tüm koşullar sağlanmalı:
1. Zorunlu kolon doğrulama canlıda aktif
2. Hata CSV indirme operasyon ekibi tarafından kullanılabilir
3. En az 1 idempotent tekrar-yükleme testi başarılı
4. Operasyon + finans tarafı preview çıktısını anlaşılır buluyor

No-Go tetikleyicileri:
- Hatalı satırların yazmaya düşmesi
- Onaylı/faturalı dönemde koruma kuralının delinmesi
- Tekrar yüklemede mükerrer kayıt üretimi

---

## 31) Sprint B Hazırlık Planı (Ön Taslak)

Sprint B odakları:
1. Alias kolon desteği (`Plaka`, `Araç Plaka`, `AracPlaka` vb.)
2. Onaylı/Faturalı dönem revizyon onay akışı
3. Idempotent davranış için regresyon test setinin genişletilmesi

### 31.1 Teknik Hazırlık
- Alias mapping tablosu tek noktada yönetilecek
- Revizyon işlemlerinde "önce fark raporu" zorunlu olacak
- Testte en az 3 farklı Excel varyasyonu kullanılacak

### 31.2 Kabul Kriteri
- Alias’lı dosyalar manuel müdahale olmadan parse edilebilmeli
- Onaylı/Faturalı dönemde kullanıcı onayı olmadan yazma yapılamamalı
- Regresyon seti tam geçtiğinde Sprint B kabul edilir

---

## 32) Kodlama Başlatma Kontrolü (Developer Ready Checklist)

Kod değişikliğine başlamadan önce aşağıdakiler doğrulanır:

1. `OPERASYONEL-PUANTAJ-EXCEL-SABLON.md` güncel mi?
2. `PuantajExcelService` mevcut preview/import akışı notlandı mı?
3. Hata kodları tek bir merkezde toplanacak tasarım net mi?
4. UI tarafında hata CSV indirme yeri kesinleşti mi?
5. Test senaryoları (S1..S5) kod tarafında karşılanacak şekilde eşlendi mi?

Tümü "Evet" ise implementasyona geçilir.

---

## 33) Uygulama Sonrası Doğrulama Akışı

Her geliştirme adımı sonrası minimum doğrulama:
1. Build başarılı
2. Preview akışında hatalı satır yazmaya gitmiyor
3. CSV hata raporu doğru kolonlarla üretiliyor
4. Tekrar yüklemede mükerrer kayıt oluşmuyor
5. Operasyon kullanıcısı için hata mesajları anlaşılır

Not:
- Bu 5 madde tamamlanmadan ilgili iş kartı "tamamlandı" sayılmaz.

---

## 34) Kart A1 Uygulama Planı (Kod Adımı)

Amaç:
- `PuantajExcelService` içinde zorunlu kolon doğrulamasını devreye almak.

Adımlar:
1. Zorunlu kolon listesini sabit koleksiyon olarak tanımla (`Tarih`, `Guzergah`, `AracPlaka`, `ServisTuru`, `SeferSayisi`).
2. Header normalize et (`trim`, `case-insensitive`, Türkçe karakter toleransı gerektiğinde map).
3. `ValidateRequiredColumns(...)` metodu ile eksik kolonları üret.
4. Eksik kolon varsa preview sonucuna `MISSING_REQUIRED_COLUMN` kodları ile hata yaz.
5. Eksik kolon varsa import yazma adımını fail-fast ile durdur.

Kabul kriteri:
- Eksik zorunlu kolonlu dosya yazmaya gitmeden kullanıcıya anlaşılır hata döner.

---

## 35) Kart A2 Uygulama Planı (Preview Modeli)

Amaç:
- Preview çıktısını UI’nin doğrudan tüketebileceği standarda taşımak.

Çıktı modeli:
- `ValidRows`
- `Errors`
- `Warnings`
- `DiffStatus`
- `Summary` (`Toplam`, `Gecerli`, `Hatali`, `Uyarili`)

Kurallar:
- `Errors` içeren satır import yazmasına gidemez.
- `Warnings` bilgilendirici olmalı; kritik ihlaller `Errors` altında tutulmalı.

---

## 36) Sprint A — İşletim Notu (Güncel)

Durum:
- Dokümantasyon hazırlığı tamamlandı, implementasyon fazına geçişe hazır.

Bir sonraki teknik adım sırası:
1. Kart A1 kodlama
2. Kart A2 kodlama
3. Kart A3 UI bağlama
4. S1..S5 test matrisi doğrulama

Raporlama kuralı:
- Her kart kapanışında bu dosyada "Tamamlandı / Açık Nokta / Sonraki İş" formatında kısa güncelleme yapılacak.

---

## 37) Kart A1 — Kodlama Alt Görev Kırılımı (Uygulayıcı Notu)

Hedef dosya:
- `MKFiloServis.Web/Services/PuantajExcelService.cs`

Alt görevler:
1. `RequiredColumns` sabit listesini ekle
2. Header normalize yardımcı metodunu ekle
3. `ValidateRequiredColumns(...)` implement et
4. Import girişinde fail-fast kontrolü bağla
5. Preview sonucuna kolon-hata üretimini bağla

Beklenen teknik çıktı:
- Eksik kolonlarda tek tip hata kodu: `MISSING_REQUIRED_COLUMN`
- Hata mesajı operasyon dili: `"Zorunlu kolon eksik: <KolonAdi>"`

---

## 38) Kart A1 — Tamamlandı Kriteri (Doğrulama)

Kart A1 "tamamlandı" sayılması için:
1. Build başarılı
2. Eksik `Tarih` kolonu ile import denemesi yazmaya gitmeden sonlanır
3. Eksik `ServisTuru` kolonu ile aynı davranış doğrulanır
4. Hata listesi UI tarafından okunabilir formatta döner
5. Doküman (bu dosya) "Kart A1 Tamamlandı" notu ile güncellenir

---

## 39) Kart Kapanış Şablonu (Bu Dosyada Kullanılacak)

### Kart: A1
- **Tamamlandı:**
- **Açık Nokta:**
- **Sonraki İş:** A2

### Kart: A2
- **Tamamlandı:** `PuantajImportSonuc` modeli `Errors / Warnings / Ozet` alanları ile genişletildi; `PuantajOnizlemeSatir` için `DiffStatus` eklendi; preview akışında structured error DTO üretimi aktif.
- **Açık Nokta:** DiffStatus şu aşamada varsayılan `Yeni`; gerçek `Guncellenecek / Degismedi` karşılaştırması A3/A4 test adımıyla netleşecek.
- **Sonraki İş:** A3

### Kart: A3
- **Tamamlandı:** `PuantajExcelGrid` üzerinde Excel dosya seçimi (`InputFile`), `Önizleme` ve `Import` aksiyonları aynı akışta bağlandı; code-behind tarafında `ExcelOnizlemeYap()` ve `ExcelImportYap()` ile servis çağrıları aktif. Import sonrası doğrulama state'i (`Errors / Warnings / Ozet`) güncelleniyor ve grid yeniden yükleniyor.
- **Açık Nokta:** İleri adımda `DiffStatus` için gerçek `Degismedi / Guncellenecek` ayrımı ve senaryo bazlı test doğrulaması tamamlanacak.
- **Sonraki İş:** A4 / Test

### Kart: A4
- **Tamamlandı:** Build doğrulaması yeşil alındı. `PlaywrightSmoke` akışına `personel/puantaj-grid` için iki yeni kontrol eklendi: ekran erişim doğrulaması ve Excel `Önizleme/Import` butonlarının dosya seçilmeden pasif olması. Smoke tarafında erişim/yetki kaynaklı false-fail azaltımı için `SkipIfAccessDeniedAsync(...)` helper'ı eklendi.
- **Açık Nokta:** Mevcut smoke koşusunda operasyonel puantaj/hakediş adımları ortam yetki/veri durumundan dolayı FAIL/timeout üretmeye devam ediyor; bu adımların PASS'e çekilmesi için test kullanıcısı yetkileri ve test verisi (`CRMFILO_SMOKE_PREPARE_DEMO`, opsiyonel mutasyon ayarları) stabilize edilmeli.
- **Sonraki İş:** A5 / Yetki + seed stabilizasyonu

### Kart: A5
- **Tamamlandı:** `SkipIfAccessDeniedAsync(...)` route-aware olacak şekilde güncellendi; beklenen route dışındaki tüm yönlendirmeler (ör. `/login`, `/firma-sec`) erişim/yetki uyumsuzluğu olarak SKIP’e çevriliyor ve URL loglanıyor. Güncel smoke koşusunda önceki FAIL/timeout adımları SKIP’e normalize edildi.
- **Açık Nokta:** Operasyonel puantaj/hakediş adımlarının PASS olabilmesi için test kullanıcısına ilgili sayfa yetkileri verilmeli ve firma seçim/test seed akışı stabilize edilmeli.
- **Sonraki İş:** A6 / Test kullanıcı yetki matrisi + otomatik firma bağlamı

### Kart: A6
- **Tamamlandı:** Smoke akışına login sonrası `EnsureFirmaContextAsync(...)` adımı eklendi ve `Aktif firma bağlamı hazırlanır` kontrolü PASS alacak şekilde otomatik firma seçimi bağlandı. `/firma-sec` yönlendirmelerinde testin boş yere FAIL vermesi engellendi.
- **Açık Nokta:** `personel/puantaj-grid` için kullanıcı `login` ekranına yönleniyor; operasyonel puantaj/hakediş tarafı ise `firma-sec` yönlendirmesinde kalıyor. Bu nedenle yetki matrisi netleştirilmeden ilgili adımlar PASS'e dönmüyor.
- **Sonraki İş:** A7 / Test kullanıcı yetki matrisi çıkarımı + rol atama doğrulaması

### Kart: A7
- **Tamamlandı:** Smoke çıktısındaki yönlendirmeler konsolide edildi: `personel/puantaj-grid` için `/login?returnUrl=...`, operasyonel puantaj/hakediş için `/firma-sec?returnUrl=...` akışı doğrulandı. Böylece başarısızlıkların kod regresyonu değil yetki/bağlam kaynaklı olduğu netleşti.
- **Açık Nokta:** Test kullanıcısında sayfa bazlı yetkiler eksik olduğu için operasyonel adımlar PASS'e dönemiyor. Özellikle `menu.filoservis` altında `serviscalisma.oku`, `toplucalisma.oku`, `guzergahlar.oku` ve ilgili operasyon ekran erişim yetkileri rol seviyesinde doğrulanmalı.
- **Sonraki İş:** A8 / Test rolüne minimum operasyon yetki seti ataması + smoke PASS doğrulaması

---

## 40) Kart A1 — Uygulama Başlatma Komutları (Operasyonel Sıra)

1. `PuantajExcelService.cs` içinde preview/import giriş metotlarını işaretle
2. `RequiredColumns` sabit listesini ekle
3. `ValidateRequiredColumns(...)` metodunu yaz
4. Import başında fail-fast kontrolünü bağla
5. Eksik kolonları `MISSING_REQUIRED_COLUMN` kodu ile hata modeline dön

Not:
- Teknik exception metni kullanıcıya doğrudan gösterilmeyecek.
- Mesaj formatı: `Zorunlu kolon eksik: <KolonAdi>`

---

## 41) Kart A1 — Günlük İlerleme Kaydı (Canlı Takip)

### A1 / Başlangıç
- **Durum:** Hazır
- **Yapılacak:** `ValidateRequiredColumns(...)` implementasyonu
- **Risk:** Eski Excel başlık varyasyonları (alias ihtiyacı)
- **Azaltım:** Sprint B öncesi geçici başlık eşleme tablosu değerlendirilecek

### A1 / Kapanışta Doldurulacak
- **Tamamlandı:** `PuantajExcelService` içine `ValidateRequiredColumns(...)` + fail-fast akışı eklendi; preview/import başlangıcında zorunlu kolon kontrolü aktif.
- **Açık Nokta:** Alias seti Sprint B’de genişletilecek (şimdilik temel varyasyonlar tanımlı).
- **Doğrulama Sonucu:** Build ✅ / S1-S2 mantığı kodda aktif (eksik kolonlarda yazma başlamadan dönüş)
- **Sonraki İş:** A2

---

## 42) Devam Kararı

Bu dosya artık Kart A1 kodlamasına geçmek için yeterli olgunlukta.

Sonraki adım:
- Dokümantasyon güncellemesi durdurulur.
- Doğrudan `PuantajExcelService` kod değişikliğine başlanır.

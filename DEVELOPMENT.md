# DEVELOPMENT

## Proje Adı
`Koa Filo Servis`

## Amaç
Filo yönetimi, muhasebe, bütçe, e-fatura, cari yönetimi, operasyon puantajı ve yardımcı CRM süreçlerini tek uygulamada toplamak.

---

## Kullanım Amacı
Bu dosya artık sadece genel özet değil, aynı zamanda proje için merkezi geliştirme kayıt dosyası olarak kullanılmalıdır.

Bu dosyada aşağıdakiler birlikte tutulur:
- kullanıcıdan gelen her yeni talep
- talep için yapılan işlemler
- tamamlanan işler
- bekleyen işler
- teknik borçlar
- sonraki adımlar

Her yeni geliştirme sonrasında bu dosya güncellenmelidir.

---

## Aktif Geliştirme Kayıt Yapısı

### 1. Gelen İstekler
Buraya kullanıcıdan gelen talepler tarih sırasıyla eklenir.

### 2. Yapılanlar
Tamamlanan geliştirmeler kısa ve net şekilde yazılır.

### 3. Yapılacaklar
Henüz tamamlanmamış ama planlanan işler burada tutulur.

### 4. Blokajlar / Riskler
Sorun çıkaran, tekrar kontrol edilmesi gereken veya teknik risk barındıran konular burada tutulur.

---

## Handoff Notu

### Son Durum
- Son tamamlanan geliştirme: `Kayıt 162 - Sahiplik UI Standardizasyonu + Tedarikçi Personel Ayrımı`
- Git durumu: commit edilecek
- Branch: `main`

### Yarım Devam İçin Önerilen Başlangıç
1. `ROADMAP.md` içindeki kalan tarihsel backlog bölümlerini gerekirse arşiv formatına dönüştür
2. `FAZ 8.5` altında bekleyen `Rakip / piyasa teklif benchmark alanları` işi için kapsam netleştir
3. Fatura çoklu firma/yön akışında UI + API regresyon kontrolü yap

### Kısa Teknik Özet
- `SahiplikHelper` ve `SahiplikBadge` ortak component'leri ile tüm araç sahiplik UI'ları tek noktadan yönetiliyor.
- Personel listelerinde tedarikçi personeli ile kendi personel arasındaki ayrım rozet ve filtre ile netleştirildi.
- Son güncel iş akışı `Fatura` çoklu firma/yön kuralları etrafında ilerledi:
  - `Kayıt 154`: veritabanı seviyesinde benzersizlik koruması
  - `Kayıt 155`: API doğrulama ve DTO uyumu
  - `Kayıt 156`: UI'da çakışma hatalarının görünür hale getirilmesi
  - `Kayıt 157`: listede firma/yön rozetlerinin güçlendirilmesi
- `Servis Çalışmaları` ekranları ve ilişkili dosyalarda Türkçe karakter / encoding temizliği yapıldı.
- `ROADMAP.md` ve `DEVELOPMENT.md` arasında üst seviye durum ile tarihsel backlog notları ayrıştırıldı.

### Not
- FAZ 4 Kurumsal Özellikler bölümü tamamlandı
- FAZ 4.1 (Çoklu Şirket Desteği) tam olarak hazır
- FAZ 4.2 (API & Entegrasyon) - REST API, Swagger, Webhook ✅
- FAZ 4.3 (Performans & Ölçekleme) - Redis Cache, Pagination ✅
- FAZ 8.5 ve 8.6 üst seviye teslimleri tamamlanmış durumda; alt sprint backlog bölümleri tarihsel referans olarak korunuyor

---

## İstek Kayıtları

### Kayıt 162 - Sahiplik UI Standardizasyonu + Tedarikçi Personel Ayrımı
**Talep:**
- Araç sahiplik (`Özmal / Kiralık / Komisyon / Tedarikçi / Diğer`) görsellerinin tüm ekranlarda tutarlı şekilde gösterilmesi.
- Personel listelerinde "kendi personelimiz" ile "tedarikçi personeli" ayrımının net olması.

**Yapılanlar:**
- `MKFiloServis.Web/Helpers/SahiplikHelper.cs` oluşturuldu:
  - `AracSahiplikTipi` (5 değer) ve `AracSahiplikKalem` (3 değer) için ortak `GetMetin`, `GetBadgeClass`, `GetIcon`, `GetAlertClass`, `GetAciklama` metodları.
  - Renk paleti standardize edildi: Özmal = success, Kiralık = warning, Komisyon = info, Tedarikçi = primary, Diğer = secondary.
- `MKFiloServis.Web/Components/Shared/SahiplikBadge.razor` shared component eklendi:
  - `Tip` veya `Kalem` parametresi ile çalışır.
  - `ShowIcon` ve `CssClass` opsiyonları ile esnek kullanım.
- Aşağıdaki ekranlardaki tekrar eden `GetSahiplikMetin/GetSahiplikBadge/SahiplikBadge` metodları `SahiplikHelper` çağrılarına yönlendirildi:
  - `IhaleHazirlik.razor`
  - `EslestirmeTanimlari.razor`
  - `TedarikciAraclari.razor`
  - `FiloGunlukPuantajPage.razor`
  - `AracMaliyetSnapshotPage.razor`
  - `LastikSezonTakip.razor`
- `SoforList.razor` güncellendi:
  - "Personel Kaynağı" filtresi eklendi (Kendi Personelimiz / Tedarikçi Personeli / Tümü).
  - "Tedarikçi" filtresi eklendi.
  - Liste kolon başlığı `Firma` → `Firma / Tedarikçi` olarak değiştirildi.
  - Tedarikçi personelleri mavi rozet (`bi-truck` ikon), kendi personelimiz gri rozet (`bi-building` ikon) ile gösteriliyor.
  - `LoadData` ve `FiltreleAsync` metodlarında `TasimaTedarikci` navigation property `Include` edildi.
  - `LoadTedarikcilerAsync` yardımcı metodu eklendi.
- `TedarikciPersonel.razor` içindeki SRC + yaygın eğitim karışık rozet düzeltildi: Sadece `YayginEgitimSertifikasiVarMi` üzerinden gösterim sağlandı.

**Doğrulama:**
- `run_build` başarılı (0 warning).
- Tüm refaktör edilen sayfalar derlemede başarıyla çalıştı.

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Helpers/SahiplikHelper.cs` (yeni)
- `MKFiloServis.Web/Components/Shared/SahiplikBadge.razor` (yeni)
- `MKFiloServis.Web/Components/Pages/Ihale/IhaleHazirlik.razor`
- `MKFiloServis.Web/Components/Pages/Filo/EslestirmeTanimlari.razor`
- `MKFiloServis.Web/Components/Pages/TedarikciServisOperasyon/TedarikciAraclari.razor`
- `MKFiloServis.Web/Components/Pages/TedarikciServisOperasyon/TedarikciPersonel.razor`
- `MKFiloServis.Web/Components/Pages/Filo/FiloGunlukPuantajPage.razor`
- `MKFiloServis.Web/Components/Pages/Araclar/AracMaliyetSnapshotPage.razor`
- `MKFiloServis.Web/Components/Pages/Lastik/LastikSezonTakip.razor`
- `MKFiloServis.Web/Components/Pages/Soforler/SoforList.razor`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 161 - Manuel Veritabanı Yedeği Oluşturulması
**Talep:**
- Mevcut veritabanı için yedeğin gerçekten alınması.

**Yapılanlar:**
- `MKFiloServis.Web/dbsettings.json` içindeki PostgreSQL ayarları kullanılarak manuel `pg_dump` çalıştırıldı.
- Zaman damgalı yedek klasörü oluşturuldu.
- `database.sql` dump dosyası üretildi.
- Aynı klasöre `dbsettings.json` kopyalandı.
- Yedek ayrıca `.zip` paketine sıkıştırıldı.

**Doğrulama:**
- `database.sql` dosyası oluşturuldu.
- `.zip` paketi oluşturuldu.

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Backups/Database/MKFiloServis_Backup_20260414_150331/database.sql`
- `MKFiloServis.Web/Backups/Database/MKFiloServis_Backup_20260414_150331/dbsettings.json`
- `MKFiloServis.Web/Backups/Database/MKFiloServis_Backup_20260414_150331.zip`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 160 - IIS Paket Publish ve `kur.bat` Kurulum Otomasyonu
**Talep:**
- IIS için publish paketi hazırlanması, kurulumun `kur.bat` ile yapılabilmesi ve veritabanı yedeğinin yoksa oluşturulup varsa güncellenmesi.

**Yapılanlar:**
- Web projesi publish çıktısına otomatik dahil edilen `kur.bat` ve `kur.ps1` kurulum scriptleri eklendi.
- `kur.bat` artık yayın paketinin içinden çalıştırılarak hedef klasöre kurulum/güncelleme yapabiliyor.
- Kurulum sırasında mevcut yayın klasörü zaman damgalı ve `latest` yedek mantığıyla kopyalanıyor.
- `dbsettings.json` üzerinden veritabanı ayarı okunarak yedek akışı eklendi:
  - `SQLite` için veritabanı dosyası kopyalanıyor
  - `PostgreSQL` için `pg_dump` varsa SQL dump alınıyor
  - diğer sağlayıcılarda en azından `dbsettings.json` yedeği korunuyor
- Mevcut sunucu `dbsettings.json` dosyası varsa ezilmemesi sağlandı; yoksa paket içinden ilk kurulum için kopyalanıyor.
- Yerelde IIS dağıtım paketi üretmek için kök dizine `publish-iis-package.bat` eklendi.

**Doğrulama:**
- Publish scriptleri ve proje publish içeriği manuel olarak kontrol edildi.

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/MKFiloServis.Web.csproj`
- `MKFiloServis.Web/Deploy/IIS/kur.bat`
- `MKFiloServis.Web/Deploy/IIS/kur.ps1`
- `publish-iis-package.bat`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 159 - ROADMAP / DEVELOPMENT Dokümantasyon Senkronizasyonu
**Talep:**
- `ROADMAP.md` ve `DEVELOPMENT.md` dosyalarının incelenmesi ve güncel durumla tutarsız özetlerin netleştirilmesi.

**Yapılanlar:**
- `ROADMAP.md` içinde `FAZ 8.5` altındaki sprint backlog bölümlerine tarihsel referans notları eklendi.
- `FAZ 8.5` üst seviye tamamlanma durumu ile alt backlog notları arasındaki fark dokümante edildi.
- `DEVELOPMENT.md` üst `Handoff Notu` bölümü güncellendi.
- Son durum, önerilen başlangıç ve kısa teknik özet bölümleri güncel proje akışına göre revize edildi.

**Doğrulama:**
- Dokümantasyon güncelliği manuel olarak kontrol edildi.

**Etkilenen Dosyalar:**
- `ROADMAP.md`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 158 - Türkçe Karakter / Encoding Temizliği
**Talep:**
- `/servis-calismalari` akışında görülen Türkçe karakter bozulmalarının düzeltilmesi ve çözüm genelinde benzer encoding sorunlarının temizlenmesi.

**Yapılanlar:**
- `ServisCalismaList.razor`, `ServisCalismaForm.razor` ve `TopluServisCalismasiEkle.razor` dosyaları UTF-8 uyumlu Türkçe içerikle yenilendi.
- Test dokümanları ve servis katmanındaki bozuk Türkçe sabit/metinler temizlendi.
- `ToastService`, `ServisKiralamaService`, `PersonelMaasIzinService`, `PythonScraperService`, `SatisService` ve `MultiUserSessionTests.md` içinde bozuk karakterler düzeltildi.
- Çözüm genelinde `*.razor`, `*.cs`, `*.md` dosyaları taranarak görünür bozuk karakter kaydı kalmadığı doğrulandı.

**Doğrulama:**
- Bozuk karakter taraması temiz sonuç verdi.
- `run_build` başarılı.

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Components/Pages/ServisCalismalari/ServisCalismaList.razor`
- `MKFiloServis.Web/Components/Pages/ServisCalismalari/ServisCalismaForm.razor`
- `MKFiloServis.Web/Components/Pages/ServisCalismalari/TopluServisCalismasiEkle.razor`
- `MKFiloServis.Web/Services/ToastService.cs`
- `MKFiloServis.Web/Services/ServisKiralamaService.cs`
- `MKFiloServis.Web/Services/PersonelMaasIzinService.cs`
- `MKFiloServis.Web/Services/PythonScraperService.cs`
- `MKFiloServis.Web/Services/SatisService.cs`
- `MKFiloServis.Web/Tests/MultiUserSessionTests.md`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 157 - Fatura Listesinde Firma/Yön Rozetlerinin Güçlendirilmesi
**Talep:**
- Aynı `FaturaNo` değerinin çoklu firma ve yön yapısında listede daha kolay ayırt edilebilmesi.

**Yapılanlar:**
- `FaturaList.razor` içinde `Fatura No` kolonu genişletildi.
- Fatura numarasının altına görünür rozetler eklendi:
  - firma adı
  - yön (`Kesilen` / `Gelen`)
  - firmalar arası rozet bilgisi
- Firma kolonunda karşı firma bilgisi badge olarak daha görünür hale getirildi.
- Cari kolonuna kısa kimlik satırı eklendi (`Firma • Yön`).

**Doğrulama:**
- `run_build` başarılı

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Components/Pages/Faturalar/FaturaList.razor`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 156 - Fatura UI'da Çakışma Hatalarının Kullanıcıya Gösterilmesi
**Talep:**
- Çoklu firma ve yön bazlı fatura kurallarında servis/veritabanı çakışmalarının kullanıcı ekranında daha anlaşılır görünmesi.

**Yapılanlar:**
- `FaturaService` içinde `DbUpdateException` yakalanarak benzersizlik hataları kullanıcı dostu `InvalidOperationException` mesajına çevrildi.
- `FirmaId + FaturaYonu + FaturaNo` unique index çakışmaları için özel mesaj üretimi eklendi.
- `FaturaForm.razor` içinde kayıt/güncelleme sırasında servis katmanından dönen doğrulama/çakışma mesajları form içine yansıtıldı.
- `GidenFaturalar.razor` manuel kayıt modalında servis hataları toast yerine modal içi uyarı olarak gösterilir hale getirildi.
- `GelenFaturalar.razor` manuel kayıt modalında servis hataları toast yerine modal içi uyarı olarak gösterilir hale getirildi.

**Doğrulama:**
- `run_build` başarılı

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Services/FaturaService.cs`
- `MKFiloServis.Web/Components/Pages/Faturalar/FaturaForm.razor`
- `MKFiloServis.Web/Components/Pages/EFatura/GidenFaturalar.razor`
- `MKFiloServis.Web/Components/Pages/EFatura/GelenFaturalar.razor`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 155 - Faturalar API Çoklu Firma/Yön Doğrulama Uyumu
**Talep:**
- UI tarafında tamamlanan çoklu firma ve yön bazlı fatura kurallarının `FaturalarController` API uçlarına da yansıtılması.

**Yapılanlar:**
- `FaturalarController.GetAll` içine `yon` ve `firmaId` query filtreleri eklendi.
- `GetByNo` endpoint'i firma ve yön bazlı aramayı destekleyecek şekilde genişletildi.
- `GetById` artık detaylı yükleme akışını kullanıyor (`GetByIdWithKalemlerAsync`).
- API dönüş DTO'larına eklendi:
  - `FaturaYonu`
  - `FirmaId`
  - `FirmaAdi`
  - `FirmalarArasiFatura`
  - `KarsiFirmaId`
  - `KarsiFirmaAdi`
- `FaturaCreateDto` çoklu firma akışını destekleyecek şekilde genişletildi:
  - `FaturaYonu`
  - `Durum`
  - `FirmaId`
  - `FirmalarArasiFatura`
  - `KarsiFirmaId`
- `Create` endpoint'ine eklendi:
  - firma zorunluluğu
  - firmalar arası karşı firma doğrulaması
  - tip + yön parse mantığı
  - servis katmanından gelen çakışma hatalarını `409 Conflict` olarak döndürme
- `UpdateDurum` endpoint'ine servis kaynaklı doğrulama/çakışma hataları için `409 Conflict` dönüşü eklendi.
- Controller içinde tekrar eden API dönüşümlerini sadeleştirmek için `MapFaturaDto` ve `MapFaturaDetayDto` yardımcı metodları eklendi.

**Doğrulama:**
- `run_build` başarılı

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Controllers/FaturalarController.cs`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 154 - Fatura Numara Tekilliği İçin Veritabanı Seviyesi Koruma
**Talep:**
- Çoklu firma fatura akışında `FaturaNo + FaturaYonu + FirmaId` kombinasyonunun veritabanı seviyesinde de korunması.

**Yapılanlar:**
- `ApplicationDbContext` içinde `Fatura` entity benzersizlik tanımı güncellendi.
- Eski global `FaturaNo` unique index kaldırılıp model tarafında `FirmaId + FaturaYonu + FaturaNo` benzersiz index tanımlandı.
- SQLite için `IsDeleted = 0` filtreli unique index tanımı eklendi.
- PostgreSQL için `"IsDeleted" = false` filtreli unique index tanımı eklendi.
- `DbInitializer` içine startup sırasında çalışan `EnsureFaturaFirmaYonUniqueIndexAsync` yardımcı akışı eklendi.
- Bu akış ile:
  - eski `IX_Faturalar_FaturaNo` index'i düşürülüyor
  - yeni `IX_Faturalar_FirmaId_FaturaYonu_FaturaNo` unique index'i oluşturuluyor

**Doğrulama:**
- `run_build` başarılı

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Data/ApplicationDbContext.cs`
- `MKFiloServis.Web/Data/DbInitializer.cs`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 153 - Fatura Detay Ekranında Firma/Yön Görünürlüğü
**Talep:**
- Çoklu firma fatura akışında `FaturaDetay` ekranında firma, yön ve karşı firma bilgilerinin görünür olması.

**Yapılanlar:**
- `FaturaService.GetByIdAsync` içine `Firma` ve `KarsiFirma` include'ları eklendi.
- `FaturaService.GetByIdWithKalemlerAsync` içine:
  - `Firma` include'u
  - `KarsiFirma` include'u
  - ödeme hareketleri için `BankaHesap` include'u
  eklendi.
- `FaturaDetay.razor` ekranında gösterime eklendi:
  - fatura yönü (`Kesilen` / `Gelen`)
  - firma adı
  - firmalar arası fatura rozeti
  - karşı firma adı

**Doğrulama:**
- `run_build` başarılı

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Services/FaturaService.cs`
- `MKFiloServis.Web/Components/Pages/Faturalar/FaturaDetay.razor`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 152 - Gelen/Kesilen Fatura Modallarında Tutarlılık İyileştirmesi
**Talep:**
- `GidenFaturalar` ve `GelenFaturalar` içindeki manuel fatura modallarında firma/cari/karşı firma seçimlerinin daha tutarlı çalışması.

**Yapılanlar:**
- `GidenFaturalar.razor` içinde manuel kayıt modalına form içi uyarı alanı eklendi.
- `GidenFaturalar.razor` içinde:
  - firma değişince geçersiz cari seçimi temizleniyor
  - karşı firma aynı firmaysa sıfırlanıyor
  - yeni kayıtta fatura numarası yeniden üretiliyor
  - firma seçilmeden numara üretimi engelleniyor
- `GelenFaturalar.razor` içinde manuel kayıt modalına form içi uyarı alanı eklendi.
- `GelenFaturalar.razor` içinde:
  - firma değişince geçersiz tedarikçi/cari seçimi temizleniyor
  - karşı firma aynı firmaysa sıfırlanıyor
  - yeni kayıtta fatura numarası yeniden üretiliyor
  - firma seçilmeden numara üretimi engelleniyor
- Her iki modalda da sessiz `return` yerine kullanıcıya görünür doğrulama mesajları eklendi.

**Doğrulama:**
- `run_build` başarılı

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Components/Pages/EFatura/GidenFaturalar.razor`
- `MKFiloServis.Web/Components/Pages/EFatura/GelenFaturalar.razor`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 151 - Fatura Formu Tutarlılık ve Form İçi Doğrulama İyileştirmesi
**Talep:**
- Çoklu firma desteği sonrası `FaturaForm` ekranında yön/firma/tip/cari ilişkilerinin daha tutarlı çalışması.

**Yapılanlar:**
- `FaturaForm.razor` içine form içi uyarı mesaj alanı eklendi.
- Fatura yönü değiştiğinde:
  - geçersiz fatura tipi otomatik düzeltiliyor
  - firma/yön ile uyumsuz cari seçimi temizleniyor
  - yeni kayıtta numara yeniden üretiliyor
- Firma değiştiğinde:
  - firma ile uyumsuz cari temizleniyor
  - karşı firma aynıysa sıfırlanıyor
  - yeni kayıtta numara yeniden üretiliyor
- Firmalar arası fatura checkbox durumu değiştiğinde karşı firma alanı güvenli şekilde temizleniyor.
- `Fatura Tipi` seçenekleri artık seçilen yöne göre filtreleniyor:
  - `Kesilen`: satış / satış iade / tevkifatlı
  - `Gelen`: alış / alış iade / tevkifatlı
- Sessiz `return` akışları yerine kullanıcıya görünür doğrulama mesajları eklendi.
- Firma seçilmeden numara üretimi engellendi ve kullanıcı bilgilendirildi.

**Doğrulama:**
- `run_build` başarılı

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Components/Pages/Faturalar/FaturaForm.razor`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 150 - Genel Fatura Ekranlarında Çoklu Firma Desteği
**Talep:**
- Çoklu firma fatura yönetimi geliştirmesinin genel `Faturalar` liste/form ekranlarına da yayılması.

**Yapılanlar:**
- `FaturaFilterParams` içine `FirmaId` filtresi eklendi.
- `FaturaService.GetPagedAsync` içine firma filtresi ve `Firma` include'u eklendi.
- `FaturaForm.razor` güncellendi:
  - firma seçimi
  - fatura yönü seçimi
  - firmalar arası fatura checkbox'ı
  - karşı firma seçimi
  - firma+yön bazlı numara üret butonu
  - firma/yön bazlı cari seçenek filtresi
- `FaturaList.razor` güncellendi:
  - firma filtresi
  - yön filtresi
  - firma kolonu
  - yön kolonu
  - firmalar arası kayıtlarda karşı firma görünürlüğü

**Doğrulama:**
- `run_build` başarılı

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Services/Interfaces/IFaturaService.cs`
- `MKFiloServis.Web/Services/FaturaService.cs`
- `MKFiloServis.Web/Components/Pages/Faturalar/FaturaForm.razor`
- `MKFiloServis.Web/Components/Pages/Faturalar/FaturaList.razor`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 149 - Çoklu Firma Fatura Yönetimi (Kesilen / Gelen)
**Talep:**
- Kesilen ve gelen faturaların birden fazla firma bazında yönetilebilmesi.
- Kesilen faturanın karşı firmada gelen fatura olarak değerlendirildiği akışta numara ve kayıt kontrollerinin buna göre yapılması.

**Yapılanlar:**
- `FaturaService` içinde fatura numarası üretimi `firma + yön` bazlı hale getirildi.
- Fatura tekillik kontrolü artık global değil; `FaturaNo + FaturaYonu + FirmaId` kombinasyonuna göre çalışıyor.
- Manuel kayıt ve import akışlarında aynı numaranın farklı firma veya karşı yönde kullanılabilmesi sağlandı.
- `PrepareFaturaForSaveAsync` içine firmalar arası fatura doğrulamaları eklendi:
  - kaynak firma zorunlu
  - karşı firma zorunlu
  - aynı firmanın karşı firma olarak seçilmesi engellendi
- `UpdateAsync` içinde `FirmaId`, `FirmalarArasiFatura` ve `KarsiFirmaId` güncellemesi eklendi.
- `CreateKarsiFirmaFaturasiAsync` içinde:
  - hedef firmada aynı numara + karşı yön fatura varsa tekrar oluşturmak yerine eşleştirme yapılıyor
  - karşı firmada oluşturulan cari artık hedef firma kapsamında aranıyor/oluşturuluyor
- Excel ve XML import akışlarında:
  - mevcut fatura kontrolü firma + yön bazlı yapılıyor
  - cari eşleşmeleri hedef firma kapsamında çalışıyor
  - otomatik oluşan cari kartlara ilgili `FirmaId` atanıyor
- `GidenFaturalar.razor` manuel kayıt modalı güncellendi:
  - firma seçimi
  - firmalar arası fatura checkbox'ı
  - karşı firma seçimi
  - firma bazlı müşteri/cari filtreleme
  - numara üret butonu
- `GelenFaturalar.razor` manuel kayıt modalı güncellendi:
  - firma seçimi
  - firmalar arası fatura checkbox'ı
  - karşı firma seçimi
  - firma bazlı tedarikçi/cari filtreleme
  - numara üret butonu

**Doğrulama:**
- `run_build` başarılı

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Services/Interfaces/IFaturaService.cs`
- `MKFiloServis.Web/Services/FaturaService.cs`
- `MKFiloServis.Web/Components/Pages/EFatura/GidenFaturalar.razor`
- `MKFiloServis.Web/Components/Pages/EFatura/GelenFaturalar.razor`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 148 - ROADMAP İhale Fazları Senkronizasyonu
**Talep:**
- İhale modülünde tamamlanan FAZ 8.5 ve 8.6 işlerinin yol haritasına işlenmesi.

**Yapılanlar:**
- `ROADMAP.md` içindeki `Son Güncellemeler` bölümü ihale teklif operasyonları ve gerçekleşen takip geliştirmeleriyle güncellendi.
- `FAZ 8.5` tablosunda tamamlanan maddeler `✅ Tamamlandı` olarak işlendi.
- `FAZ 8.6` tablosunda tamamlanan maddeler `✅ Tamamlandı` olarak işlendi.
- `İhale Operasyon Dashboard Excel Export` çıktısı yol haritasına eklendi.
- `FAZ 8'den Devam Önerisi` bölümü yeni operasyon detay raporları odağına göre güncellendi.

**Etkilenen Dosyalar:**
- `ROADMAP.md`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 147 - İhale Operasyon Dashboard Excel Export
**Talep:**
- İhale sonrası operasyon dashboard özetinin yönetim raporu olarak Excel'e indirilebilmesi.

**Yapılanlar:**
- `IhaleHazirlik.razor` içindeki `İhale Sonrası Operasyon Özeti` kartına `Excel` butonu eklendi.
- `OperasyonDashboardExcelExportAsync` metodu eklendi.
- Export içine özet metrikler dahil edildi:
  - kazanılan / analiz edilen proje sayısı
  - riskli proje sayısı
  - revizyonlu proje sayısı
  - süre uzatımlı proje sayısı
  - ortalama teklif doğruluk skoru
  - toplam revizyon bedel farkı
  - toplam ve en kötü kâr sapması
- Aynı export içine `Riskli Projeler` listesi de eklendi:
  - proje kodu / proje adı
  - risk seviyesi
  - aktif revizyon sayısı
  - teklif doğruluk skoru
  - maliyet sapma oranı
  - revizyon bedel farkı
  - kâr sapması

**Etkilenen Dosyalar:**
- `MKFiloServis.Web/Components/Pages/Ihale/IhaleHazirlik.razor`
- `DEVELOPMENT.md`

**Durum:** ✅ Tamamlandı

### Kayıt 133 - Redis Cache Entegrasyonu (FAZ 4.3)
**Talep:**
- Distributed cache sistemi ile sık kullanılan sorguları cache'leme

**Yapılanlar:**
- NuGet paketi eklendi:
  - `Microsoft.Extensions.Caching.StackExchangeRedis` (Redis cache provider)
- ICacheService interface oluşturuldu:
  - `GetAsync<T>` - Cache'den veri al
  - `SetAsync<T>` - Cache'e veri yaz (absolute expiration)
  - `SetWithSlidingAsync<T>` - Sliding expiration ile cache
  - `RemoveAsync` - Cache'den sil
  - `RemoveByPrefixAsync` - Prefix ile toplu silme
  - `ExistsAsync` - Key kontrolü
  - `GetOrSetAsync<T>` - Cache-aside pattern (al yoksa oluştur)
  - `RefreshAsync` - Cache süresini yenile
- CacheService implementasyonu:
  - IDistributedCache tabanlı (Memory veya Redis)
  - JSON serialization
  - Key tracking (prefix bazlı silme için)
  - Hata toleransı (cache hatası uygulamayı durdurmaz)
- CacheKeys static class:
  - Tutarlı key isimlendirme
  - Dashboard, Cari, Araç, Şoför, Güzergah, Fatura, Masraf, İstatistik key'leri
- CacheDurations static class:
  - Short (1 dk), Default (5 dk), Medium (15 dk), Long (1 saat), Daily (24 saat)
- appsettings.json güncellemeleri:
  - `Cache:Provider` (Memory/Redis seçimi)
  - `Cache:Redis:ConnectionString`
  - `Cache:Redis:InstanceName`
- Program.cs güncellemeleri:
  - AddStackExchangeRedisCache / AddDistributedMemoryCache kaydı
  - ICacheService servis kaydı
- DashboardGrafikService cache entegrasyonu:
  - `GetAylikGelirGiderAsync` cache'lendi
  - `GetAylikSeferSayisiAsync` cache'lendi
  - `GetAracPerformansAsync` cache'lendi
  - `GetCariPerformansAsync` cache'lendi
  - AsNoTracking eklendi

**Performans Kazancı:**
- Dashboard grafikleri 15 dakika cache'leniyor
- İlk yüklemeden sonraki çağrılar anında yanıt veriyor
- Veritabanı yükü azaldı
- FAZ 4.3 (Performans & Ölçekleme) tamamlandı

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/CRMFiloServis.Web.csproj` (güncellendi)
- `CRMFiloServis.Web/Services/Interfaces/ICacheService.cs` (yeni)
- `CRMFiloServis.Web/Services/CacheService.cs` (yeni)
- `CRMFiloServis.Web/Services/DashboardGrafikService.cs` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/appsettings.json` (güncellendi)
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 134 - Harita Entegrasyonu (Leaflet.js)
**Talep:**
- Güzergahları harita üzerinde görselleştirme
- Koordinat seçici ile konum belirleme

**Yapılanlar:**
- Leaflet.js Entegrasyonu:
  - App.razor'a Leaflet CSS ve JS CDN eklendi
  - `leaflet-interop.js` JavaScript dosyası oluşturuldu
  - Blazor JSInterop ile tam entegrasyon
- HaritaGosterici.razor Bileşeni:
  - Çoklu harita desteği (MapId ile)
  - Marker ekleme (başlangıç yeşil, bitiş kırmızı)
  - Rota çizimi (polyline)
  - Tıklama ile koordinat seçme
  - Haritayı marker'lara sığdırma (FitBounds)
  - GuzergahHaritaDto ile veri aktarımı
- Guzergah Entity Güncellemesi:
  - `BaslangicLatitude` (double?) eklendi
  - `BaslangicLongitude` (double?) eklendi
  - `BitisLatitude` (double?) eklendi
  - `BitisLongitude` (double?) eklendi
  - `RotaRengi` (string?) eklendi
- GuzergahList.razor Güncellemeleri:
  - Liste/Harita görünüm geçiş düğmeleri
  - Harita görünümünde tüm güzergahlar gösteriliyor
  - Koordinatlı güzergahlar badge ile işaretli
  - Kart görünümü ile özet liste
- GuzergahForm.razor Güncellemeleri:
  - Genişletilebilir harita paneli
  - Başlangıç/Bitiş koordinat input'ları
  - Haritadan tıklayarak koordinat seçme
  - Rota rengi seçici (color picker)
  - Koordinat temizleme butonu
  - Otomatik rota gösterimi
- Migration Helper:
  - SQLite için ALTER TABLE komutları
  - PostgreSQL için DO $$ bloğu
  - Program.cs'e migration çağrısı eklendi

**Teknik Özellikler:**
- OpenStreetMap tile katmanı (ücretsiz)
- Renkli özel marker'lar (divIcon)
- Türkiye merkezi varsayılan görünüm (Ankara)
- Responsive harita boyutu
- IAsyncDisposable ile temizlik

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/Guzergah.cs` (güncellendi)
- `CRMFiloServis.Web/Components/App.razor` (güncellendi)
- `CRMFiloServis.Web/wwwroot/js/leaflet-interop.js` (yeni)
- `CRMFiloServis.Web/Components/Shared/HaritaGosterici.razor` (yeni)
- `CRMFiloServis.Web/Components/Pages/Guzergahlar/GuzergahList.razor` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/Guzergahlar/GuzergahForm.razor` (güncellendi)
- `CRMFiloServis.Web/Data/Migrations/GuzergahKoordinatMigrationHelper.cs` (yeni)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 135 - Test Data Seeding (Demo Veri Oluşturma)
**Talep:**
- Demo ve test amaçlı örnek veri oluşturma sistemi
- FAZ 7.5 kapsamında Örnek Veri & Test

**Yapılanlar:**
- TestDataSeeder.cs Servisi (500+ satır):
  - `SeedAllAsync(bool silinenleriTemizle)` - Ana seed metodu
  - `TemizleAsync()` - [TEST] etiketli kayıtları temizleme
  - `SeedCarilerAsync()` - 10 müşteri + 5 tedarikçi
  - `SeedSoforlerAsync()` - 15 şoför (TC, ehliyet, maaş bilgileri)
  - `SeedAraclarAsync()` - 12 araç (plaka, şase, km, sahiplik tipi)
  - `SeedGuzergahlarAsync()` - 8 güzergah (İstanbul koordinatları ile)
  - `SeedFaturalarAsync()` - 45 fatura (30 satış + 15 alış)
  - `SeedServisCalismalarıAsync()` - Son 30 gün sefer kayıtları
  - `TestDataResult` DTO (sayılar ve mesajlar)
- [TEST] Etiketleme Sistemi:
  - Tüm demo veriler `[TEST]` prefix ile işaretleniyor
  - Notlar alanına veya Unvan/Ad alanına ekleniyor
  - Toplu temizleme ile kolay silinebilir
- İstanbul Koordinat Verisi:
  - 12 gerçek nokta (Kadıköy, Beşiktaş, Ataşehir, Taksim, vb.)
  - Güzergahlar için başlangıç/bitiş koordinatları
  - Renkli rota tanımları (#FF0000, #00FF00, vb.)
- DemoVeri.razor Yönetim Sayfası:
  - Demo veri oluştur butonu
  - Mevcut veriyi temizle seçeneği
  - Demo veriyi sil butonu
  - İstatistik kartları (Cari, Şoför, Araç, Güzergah, Fatura, Sefer sayıları)
  - Sonuç mesajları listesi
- Program.cs Servisi Kaydı:
  - `builder.Services.AddScoped<TestDataSeeder>();`
- NavMenu.razor Linki:
  - Ayarlar > Demo Veri menü öğesi

**Entity Uyumluluk Düzeltmeleri:**
- Cari: Unvan, Notlar alanları
- Sofor: TcKimlikNo, EhliyetGecerlilikTarihi, NetMaas, PersonelGorev
- Arac: SaseNo, AktifPlaka, Marka/Model string, KmDurumu, AracSahiplikTipi
- Guzergah: CariId (zorunlu), BirimFiyat, Notlar
- Fatura: FaturaTipi.SatisFaturasi/AlisFaturasi, FaturaDurum.Beklemede/Odendi
- ServisCalisma: CalismaTarihi, ServisTuru, Fiyat, Notlar

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Data/TestDataSeeder.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/DemoVeri.razor` (yeni)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 136 - Araç Takip Sistemi (GPS Entegrasyonu)
**Talep:**
- Araçların gerçek zamanlı GPS takibi
- Konum geçmişi ve raporlama
- FAZ 3.1 kapsamında Araç Takip Sistemi Entegrasyonu

**Yapılanlar:**
- AracTakip.cs Entity'leri (5 yeni entity):
  - `AracTakipCihaz` - GPS cihaz tanımları (IMEI, marka, model, SIM)
  - `AracKonum` - GPS koordinat kayıtları (lat/lng, hız, yön, kontak durumu)
  - `AracBolge` - Geofence bölge tanımları (daire/çokgen)
  - `AracBolgeAtama` - Bölge-Araç atamaları
  - `AracTakipAlarm` - Alarm kayıtları (hız aşımı, bölge giriş/çıkış)
  - `KonumOlayTipi`, `BolgeTipi`, `AlarmTipi` enum'ları
- IAracTakipService Interface:
  - Cihaz CRUD (Create, Read, Update, Delete)
  - Konum yönetimi (son konum, geçmiş, toplu kayıt)
  - Bölge (Geofence) yönetimi
  - Alarm yönetimi (okundu/işlendi işaretleme)
  - İstatistik ve raporlama
  - API entegrasyonu (test bağlantısı, senkronizasyon)
  - `AracKonumDto`, `AracTakipDurum`, `AracTakipIstatistik`, `DurakNoktasi` DTO'ları
- AracTakipService Implementasyonu (600+ satır):
  - Haversine formülü ile mesafe hesaplama
  - Araç durum belirleme (Hareket/Bekliyor/Park/Çevrimdışı)
  - Otomatik alarm kontrolü (hız aşımı, bölge)
  - Durak noktası analizi
  - İstatistik hesaplama (toplam mesafe, süre, ortalama hız)
- AracTakipCanli.razor (Canlı Takip Sayfası):
  - Sol panel: Araç listesi (durum badge'leri, hız, yakıt)
  - Sağ panel: Leaflet.js harita (tüm araçlar)
  - Otomatik yenileme (30 saniye)
  - Araç seçimi ile haritada yakınlaştırma
  - Özet kartlar (hareket/bekliyor/park/çevrimdışı)
  - Okunmamış alarm bildirimi
- TakipCihazYonetimi.razor (Cihaz Yönetim Sayfası):
  - Cihaz listesi (araç, IMEI, marka, SIM, son iletişim)
  - Yeni cihaz ekleme modal'ı
  - Cihaz düzenleme ve silme
  - Durum göstergeleri (aktif/pasif, batarya, sinyal)
- KonumGecmisi.razor (Geçmiş Sayfa):
  - Tarih aralığı filtresi (bugün/7 gün/30 gün)
  - İstatistik kartları (mesafe, süre, max hız, durak sayısı)
  - Durak noktaları listesi
  - Alarm listesi
  - Harita üzerinde rota çizimi
  - Başlangıç/bitiş marker'ları
- leaflet-interop.js Güncellemeleri:
  - `clearMarkers()` - Marker temizleme
  - `clearRoute()` - Rota temizleme
  - `destroyMap()` - Harita kaldırma
  - `setView()` - Görünüm ayarlama
  - `drawRoute()` - Çoklu nokta rota çizimi
  - `addCircle()` - Daire bölge çizimi
  - `addPolygon()` - Çokgen bölge çizimi
  - `addVehicleMarker()` - Yön oklu araç marker'ı
- ApplicationDbContext DbSet'ler:
  - `AracTakipCihazlar`, `AracKonumlar`, `AracBolgeler`, `AracBolgeAtamalar`, `AracTakipAlarmlar`
- NavMenu.razor Linkleri:
  - Filo Servis > Canlı Araç Takip
  - Ayarlar > Entegrasyon > Araç Takip Cihazları

**Teknik Özellikler:**
- Gerçek zamanlı konum takibi (SignalR hazır altyapı)
- Haversine formülü ile hassas mesafe hesaplama
- Geofence desteği (daire ve çokgen bölgeler)
- Otomatik alarm oluşturma (hız aşımı 120 km/s)
- Durak analizi (min 5 dakika bekleme)
- GPS cihaz marka desteği (Teltonika, Queclink, Concox, Meitrack, Coban)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/AracTakip.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IAracTakipService.cs` (yeni)
- `CRMFiloServis.Web/Services/AracTakipService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/AracTakip/AracTakipCanli.razor` (yeni)
- `CRMFiloServis.Web/Components/Pages/AracTakip/TakipCihazYonetimi.razor` (yeni)
- `CRMFiloServis.Web/Components/Pages/AracTakip/KonumGecmisi.razor` (yeni)
- `CRMFiloServis.Web/wwwroot/js/leaflet-interop.js` (güncellendi)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 137 - SignalR Gerçek Zamanlı Araç Takip
**Talep:**
- Araç konum güncellemelerini gerçek zamanlı olarak bildirme
- WebSocket tabanlı anlık iletişim

**Yapılanlar:**
- AracTakipHub.cs SignalR Hub oluşturuldu:
  - `JoinAracTakip()` - Takip grubuna katılma
  - `LeaveAracTakip()` - Takip grubundan ayrılma
  - `JoinAracOzel(int aracId)` - Tekil araç takibi
  - `LeaveAracOzel(int aracId)` - Tekil takipten çıkma
  - `AracKonumGuncelleme` - Client'a konum bildirimi
  - `AracAlarmBildirimi` - Alarm bildirimi
  - `YeniBolgeUyarisi` - Bölge giriş/çıkış uyarısı
  - `AracKonumGuncelleme` DTO sınıfı
- IAracTakipBildirimService Interface:
  - `KonumGuncellemesiGonderAsync()` - Tek araç güncellemesi
  - `TopluKonumGuncellemesiGonderAsync()` - Batch güncelleme
  - `AlarmBildirimiGonderAsync()` - Alarm bildirimi
  - `BolgeUyarisiGonderAsync()` - Geofence bildirimi
- AracTakipBildirimService Implementasyonu:
  - IHubContext<AracTakipHub> entegrasyonu
  - Grup bazlı mesaj gönderimi
  - Async/await pattern
- signalr-interop.js JavaScript Helper:
  - `startConnection(hubUrl)` - Bağlantı başlatma
  - `stopConnection()` - Bağlantı kapatma
  - `joinGroup(groupName)` - Gruba katılma
  - `leaveGroup(groupName)` - Gruptan ayrılma
  - `onKonumGuncelleme(callback)` - Konum event listener
  - `onAlarmBildirimi(callback)` - Alarm event listener
  - `getConnectionState()` - Bağlantı durumu
- AracTakipCanli.razor SignalR Entegrasyonu:
  - OnInitializedAsync'de bağlantı kurulumu
  - KonumGuncellemeAlindi handler
  - Otomatik reconnect
  - Bağlantı durumu göstergesi
- Program.cs Güncellemeleri:
  - `app.MapHub<AracTakipHub>("/aractakiphub")`
  - `builder.Services.AddSignalR()`

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Hubs/AracTakipHub.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IAracTakipBildirimService.cs` (yeni)
- `CRMFiloServis.Web/Services/AracTakipBildirimService.cs` (yeni)
- `CRMFiloServis.Web/wwwroot/js/signalr-interop.js` (yeni)
- `CRMFiloServis.Web/Components/Pages/AracTakip/AracTakipCanli.razor` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 138 - Araç Takip API Controller
**Talep:**
- GPS cihazlarından veri alımı için REST API endpoint'leri
- Mobil uygulama ve üçüncü parti entegrasyon desteği

**Yapılanlar:**
- AracTakipController.cs (~600 satır) oluşturuldu:
  - `POST /api/aractakip/konum` - Tek konum kaydı
  - `POST /api/aractakip/konum/toplu` - Toplu konum kaydı (batch)
  - `POST /api/aractakip/cihazlar` - Yeni cihaz ekleme
  - `GET /api/aractakip/cihazlar` - Cihaz listesi
  - `GET /api/aractakip/cihazlar/{id}` - Cihaz detayı
  - `PUT /api/aractakip/cihazlar/{id}` - Cihaz güncelleme
  - `DELETE /api/aractakip/cihazlar/{id}` - Cihaz silme
  - `GET /api/aractakip/konumlar` - Tüm araçların son konumları
  - `GET /api/aractakip/konumlar/{aracId}` - Araç konum geçmişi
  - `GET /api/aractakip/alarmlar` - Alarm listesi
  - `PUT /api/aractakip/alarmlar/{id}/okundu` - Alarm okundu işaretle
  - `GET /api/aractakip/istatistik` - Takip istatistikleri
- DTO Sınıfları:
  - `KonumKayitRequest` (SerialNumber, Lat, Lng, Hiz, Yon, Kontak, Motor, Yakit)
  - `TopluKonumKayitRequest` (Konumlar listesi)
  - `CihazOlusturRequest` (AracId, SerialNumber, Marka, Model, SimKart)
  - `CihazGuncelleRequest` (aktiflik, batarya, sinyal)
- JWT Authentication entegrasyonu
- SignalR bildirim entegrasyonu (konum kaydında otomatik broadcast)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Controllers/AracTakipController.cs` (yeni)

**Durum:** ✅ Tamamlandı

---

### Kayıt 139 - GPS Cihaz Simülasyon Servisi
**Talep:**
- Test ve demo amaçlı GPS verisi simülasyonu
- Gerçek cihaz olmadan sistemin test edilebilmesi

**Yapılanlar:**
- GpsSimulasyonService.cs BackgroundService oluşturuldu (~380 satır):
  - `ExecuteAsync()` - BackgroundService ana döngüsü
  - `SimulasyonDongusuAsync()` - Periyodik konum üretimi
  - `YeniSimulasyonDurumuOlustur()` - Başlangıç noktası belirleme
  - `GuncelleDurum()` - Durum geçişleri
  - `GuncelleHareketModu()` - Hareket halinde konum güncellemesi
  - `GuncelleBekliyorModu()` - Rölanti durumu
  - `GuncelleParkModu()` - Park durumu
  - `HesaplaMesafe()` - Haversine formülü
  - `HesaplaYon()` - İki nokta arası yön hesaplama
  - Public API: `Baslat()`, `Durdur()`, `Sifirla()`, `GuncellemeAraligiAyarla()`
- SimulasyonAracDurumu Sınıfı:
  - Enlem, Boylam, Hız, Yön
  - KontakAcik, MotorCalisiyor
  - YakitSeviyesi, BataryaSeviyesi, SinyalGucu
  - Mod (Hareket/Bekliyor/Park)
  - HedefEnlem, HedefBoylam (rota için)
- GpsSimulasyon.razor Yönetim Sayfası:
  - Başlat/Durdur kontrolü
  - Güncelleme aralığı ayarı (1-60 saniye)
  - Araç durumlarını sıfırlama
  - Bilgilendirme kartları
  - appsettings.json yapılandırma örneği
- Program.cs Güncellemeleri:
  - `builder.Services.AddSingleton<GpsSimulasyonService>()`
  - `builder.Services.AddHostedService()` kaydı
- NavMenu.razor Güncellemeleri:
  - Ayarlar > GPS Simülasyon linki eklendi
- appsettings.json Yapılandırması:
  - `GpsSimulasyon:Aktif` (bool)
  - `GpsSimulasyon:GuncellemeAraligiSaniye` (int)

**Teknik Özellikler:**
- BackgroundService pattern (uygulama ile başlar)
- Singleton servis (state tutar)
- 3 hareket modu: Hareket, Bekliyor, Park
- Rastgele mod geçişi (5-15 dakika)
- Gerçekçi GPS sapması (drift)
- SignalR ile anlık broadcast
- Haversine formülü ile mesafe/yön hesaplama
- İstanbul merkezi başlangıç koordinatları

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/GpsSimulasyonService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/AracTakip/GpsSimulasyon.razor` (yeni)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 140 - MAUI Blazor Mobil Uygulama Altyapısı
**Talep:**
- Şoförler için mobil uygulama (FAZ 3.3)
- Sefer başlat/bitir, masraf girişi, arıza bildirimi

**Yapılanlar:**
- CRMFiloServis.Mobile MAUI Blazor projesi oluşturuldu:
  - .NET 10 hedefli (Android, iOS, Windows, macOS)
  - Solution'a eklendi
  - CRMFiloServis.Shared projesine referans
- API İstemci Servisleri:
  - `IApiService` interface (~180 satır DTO dahil)
  - `ApiService` implementasyonu - JWT Bearer auth, HttpClient
  - `GirisYanit`, `KullaniciBilgisi`, `AracOzet`, `SeferOzet` DTO'ları
  - `SeferBaslatRequest`, `SeferBitirRequest` - sefer yönetimi
  - `KonumGonderRequest`, `ArizaBildirimRequest`, `MasrafKayitRequest`
- Konum Servisi:
  - `IKonumService` interface
  - `KonumService` - MAUI Geolocation API entegrasyonu
  - Konum izni yönetimi (request/check)
  - Arka plan konum takibi (10 saniye aralık)
  - `KonumBilgisi` DTO (enlem, boylam, hız, yön, yükseklik)
- Blazor Sayfaları:
  - `Home.razor` - Ana sayfa (hoşgeldin, aktif sefer, hızlı aksiyonlar, araç listesi)
  - `Giris.razor` - Kullanıcı giriş sayfası (form validation, şifre göster/gizle)
  - `SeferBaslat.razor` - Yeni sefer başlatma (araç/güzergah seçimi, KM, konum)
  - `MasrafGirisi.razor` - Masraf kaydı (tutar, fiş fotoğrafı, konum)
  - `ArizaBildir.razor` - Arıza bildirimi (tip seçimi, öncelik, çoklu fotoğraf)
- Layout:
  - `MainLayout.razor` - Alt navigasyon barı (Ana Sayfa, Seferler, Masraf, Ayarlar)
  - iOS safe area desteği
  - Responsive mobil tasarım
- MauiProgram.cs Güncellemeleri:
  - HttpClient yapılandırması (BaseAddress, timeout, headers)
  - Blazored.LocalStorage (token saklama)
  - Servis kayıtları (IApiService, IKonumService)
- wwwroot/index.html:
  - Bootstrap 5.3 CDN
  - Bootstrap Icons
  - iOS safe area CSS
  - Türkçe dil ayarı

**Teknik Özellikler:**
- MAUI Blazor Hybrid (native + web)
- JWT Bearer Authentication
- MAUI Geolocation API
- MAUI MediaPicker (kamera/galeri)
- Blazored.LocalStorage (token persistence)
- Bottom tab navigation pattern
- Form validation

**Etkilenen Dosyalar:**
- `CRMFiloServis.Mobile/CRMFiloServis.Mobile.csproj` (yeni)
- `CRMFiloServis.Mobile/MauiProgram.cs` (güncellendi)
- `CRMFiloServis.Mobile/Services/IApiService.cs` (yeni)
- `CRMFiloServis.Mobile/Services/ApiService.cs` (yeni)
- `CRMFiloServis.Mobile/Services/IKonumService.cs` (yeni)
- `CRMFiloServis.Mobile/Services/KonumService.cs` (yeni)
- `CRMFiloServis.Mobile/Components/Pages/Home.razor` (güncellendi)
- `CRMFiloServis.Mobile/Components/Pages/Giris.razor` (yeni)
- `CRMFiloServis.Mobile/Components/Pages/SeferBaslat.razor` (yeni)
- `CRMFiloServis.Mobile/Components/Pages/MasrafGirisi.razor` (yeni)
- `CRMFiloServis.Mobile/Components/Pages/ArizaBildir.razor` (yeni)
- `CRMFiloServis.Mobile/Components/Layout/MainLayout.razor` (güncellendi)
- `CRMFiloServis.Mobile/wwwroot/index.html` (güncellendi)
- `CRMFiloServis.slnx` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 141 - Mobil Uygulama Sayfaları ve API Endpoint'leri
**Talep:**
- Mobil uygulamada eksik sayfaların tamamlanması
- MobileController'a eksik endpoint'lerin eklenmesi

**Yapılanlar:**

**1. Yeni Mobil Sayfalar:**
- `SeferGecmisi.razor` - Şoför sefer geçmişi görüntüleme:
  - Ay/yıl filtreleme (dropdown seçici)
  - Özet kartları (toplam sefer, toplam KM, toplam çalışma süresi)
  - Sefer listesi (tarih, güzergah, araç, mesafe, süre)
  - Tamamlanan/iptal renk kodları
  - Detay görüntüleme navigasyonu

- `SeferBitir.razor` - Aktif seferi bitirme:
  - Sefer bilgileri kartı (güzergah, araç, başlangıç KM)
  - Bitiş KM girişi (validation: başlangıç KM'den büyük)
  - Mevcut konum alma butonu (GPS koordinatları)
  - Notlar alanı (isteğe bağlı)
  - Sefer tamamlama API çağrısı
  - Ana sayfaya yönlendirme

- `Ayarlar.razor` - Mobil uygulama ayarları:
  - Profil bilgileri kartı (ad, e-posta, telefon)
  - Konum takibi switch (on/off)
  - Bildirim ayarları switch (on/off)
  - Sunucu bağlantısı test butonu (health check API)
  - Çıkış yap butonu (token temizleme, login'e yönlendirme)

**2. API Service Güncellemeleri (IApiService/ApiService):**
- `GuzergahlariGetirAsync()` - Tüm güzergahları getir
- `SeferGecmisiniGetirAsync()` - Şoför sefer geçmişi (ay/yıl filtreli)
- `SeferGetirAsync(int id)` - Tekil sefer detayı
- `BaglantiyiTestEtAsync()` - Sunucu bağlantı testi (health check)
- `GuzergahOzet` DTO eklendi

**3. MobileController Endpoint'leri:**
- `GET /api/mobile/seferler` - Şoför sefer geçmişi listesi
- `GET /api/mobile/seferler/{id}` - Tekil sefer detayı
- `GET /api/health` - Sunucu sağlık kontrolü (health check)
- `MobileSeferGecmisOzet` DTO eklendi (Id, BaslangicZamani, BitisZamani, ToplamKm, Durum, GuzergahAdi, AracPlaka)

**Teknik Detaylar:**
- Nullable TimeSpan hesaplama düzeltmesi (ToplamSaat)
- Bootstrap 5 mobil uyumlu tasarım
- Form validation (DataAnnotations)
- JWT Bearer authentication
- Async/await pattern

**Etkilenen Dosyalar:**
- `CRMFiloServis.Mobile/Components/Pages/SeferGecmisi.razor` (yeni)
- `CRMFiloServis.Mobile/Components/Pages/SeferBitir.razor` (yeni)
- `CRMFiloServis.Mobile/Components/Pages/Ayarlar.razor` (yeni)
- `CRMFiloServis.Mobile/Services/IApiService.cs` (güncellendi)
- `CRMFiloServis.Mobile/Services/ApiService.cs` (güncellendi)
- `CRMFiloServis.Web/Controllers/MobileController.cs` (güncellendi)
- `ROADMAP.md` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 142 - Mobil API URL Platform Yapılandırması
**Talep:**
- Mobil uygulamada geliştirme ortamı için API bağlantı yapılandırması
- Windows masaüstü'nde login hatası çözümü (test/test123 çalışmıyordu)

**Sorun Analizi:**
- Windows masaüstünde `10.0.0.2` IP adresi erişilemez (localhost gerekli)
- Android emulator'de `localhost` erişilemez (`10.0.2.2` host IP gerekli)
- Fiziksel Android cihazlarda yerel ağ IP'si gerekli

**Yapılanlar:**

**1. MauiProgram.cs Platform Bazlı API URL:**
```csharp
#if DEBUG
    #if WINDOWS
        private const string ApiBaseUrl = "http://localhost:5190/";
    #elif ANDROID
        private const string ApiBaseUrl = "http://10.0.2.2:5190/";
    #else
        private const string ApiBaseUrl = "http://10.0.0.2:5190/";
    #endif
#else
    private const string ApiBaseUrl = "https://api.MKFiloServis.com/";
#endif
```

**2. AndroidManifest.xml Cleartext Traffic:**
- `android:usesCleartextTraffic="true"` eklendi (HTTP izni)

**3. SSL Bypass (Geliştirme Ortamı):**
- `ServerCertificateCustomValidationCallback` ile self-signed sertifika desteği

**Platform API URL Özeti:**
| Platform | API URL | Açıklama |
|----------|---------|----------|
| Windows Masaüstü | `localhost:5190` | Aynı makine |
| Android Emulator | `10.0.2.2:5190` | Host makine IP |
| Fiziksel Cihaz | `10.0.0.2:5190` | Yerel ağ IP |
| Production | `api.MKFiloServis.com` | HTTPS |

**Etkilenen Dosyalar:**
- `CRMFiloServis.Mobile/MauiProgram.cs` (güncellendi)
- `CRMFiloServis.Mobile/Platforms/Android/AndroidManifest.xml` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 143 - Multi-tenant Altyapısı ve Fatura Import Fix
**Talep:**
1. Multi-tenant (çoklu şirket) desteği altyapısı
2. Fatura XML+PDF import sorunu (2. firma aktarım yapamıyor)
3. Windows masaüstü exe publish

**Yapılanlar:**

**1. Multi-tenant Entity'ler:**
- `Sirket.cs` - Ana şirket entity'si (SirketKodu, Unvan, KisaAd, VergiDairesi, VergiNo, Adres, Il, Ilce, vb.)
- `TenantEntity` - Base class (SirketId FK ile tenant izolasyonu)
- `Kullanici.SirketId` - Kullanıcı-şirket ilişkisi eklendi

**2. Multi-tenant Servisler:**
- `ITenantService` interface (GetCurrentTenantId, GetCurrentTenantAsync, SetCurrentTenant)
- `TenantService` implementasyonu (HttpContext + Session bazlı tenant çözümleme)
- `ApplicationDbContext.Sirketler` DbSet eklendi
- `Program.cs` TenantService DI kaydı

**3. PostgreSQL Tablo Oluşturma:**
```sql
CREATE TABLE "Sirketler" (Id, SirketKodu, Unvan, KisaAd, VergiDairesi, VergiNo, 
                          Adres, Il, Ilce, PostaKodu, Telefon, Email, WebSitesi, 
                          LogoUrl, Aktif, ParaBirimi, AyarlarJson, LisansBitisTarihi,
                          MaxKullaniciSayisi, CreatedAt, UpdatedAt, IsDeleted);
ALTER TABLE "Kullanicilar" ADD COLUMN "SirketId" INTEGER REFERENCES "Sirketler"("Id");
INSERT INTO "Sirketler" (...) VALUES ('KOA', 'Koa Filo Servis A.S.', ...);
```

**4. Fatura XML+PDF Import Fix:**
- **Sorun:** InputFile `@key="importTipi"` - firma değiştiğinde component reset olmuyor
- **Çözüm:** `@key="@($"{importFirmaId}_{importTipi}")"` - firma+tip kombinasyonu
- `ImportFirmaDegisti()` handler eklendi - firma değiştiğinde dosya seçimlerini temizliyor

**5. Windows Masaüstü EXE Publish:**
```powershell
dotnet publish CRMFiloServis.Mobile.csproj -f net10.0-windows10.0.19041.0 -c Release -o ./publish/windows-desktop
```
- Boyut: 129.50 MB
- Çıktı: `publish/windows-desktop/CRMFiloServis.Mobile.exe`

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/Sirket.cs` (yeni)
- `CRMFiloServis.Shared/Entities/KullaniciVeLisans.cs` (güncellendi - SirketId)
- `CRMFiloServis.Web/Services/Interfaces/ITenantService.cs` (yeni)
- `CRMFiloServis.Web/Services/TenantService.cs` (yeni)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi - Sirketler DbSet)
- `CRMFiloServis.Web/Program.cs` (güncellendi - TenantService kaydı)
- `CRMFiloServis.Web/Components/Pages/EFatura/GelenFaturalar.razor` (güncellendi - InputFile @key fix)

**Durum:** ✅ Tamamlandı

---

### Kayıt 144 - Şirket Yönetimi UI (Multi-tenant)
**Talep:**
- Multi-tenant yönetim arayüzü oluşturma
- Şirket CRUD işlemleri UI

**Yapılanlar:**

**1. SirketYonetimi.razor Sayfası:**
- `/ayarlar/sirketler` route
- Şirket listesi (kart görünümü)
- İstatistik kartları (toplam/aktif şirket, kullanıcı sayısı, mevcut şirket)
- Yeni şirket ekleme modal
- Şirket düzenleme
- Şirket silme (kullanıcısı olmayan şirketler)
- Şirket geçişi (aktif şirketi değiştirme)

**2. TenantService Güncellemesi:**
- `IsSuperAdmin` property'sine Admin rolü desteği eklendi
- SuperAdmin, Admin veya IsSuperAdmin claim'i olan kullanıcılar yönetici sayılıyor

**3. NavMenu Güncellemesi:**
- Şirket Yönetimi linki eklendi (sadece Admin/SuperAdmin görebilir)
- `IsSuperAdmin()` yardımcı metodu eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Ayarlar/SirketYonetimi.razor` (yeni)
- `CRMFiloServis.Web/Services/TenantService.cs` (güncellendi - Admin rolü desteği)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi - Şirket Yönetimi linki)

**Durum:** ✅ Tamamlandı

---

### Kayıt 145 - Multi-tenant Veri İzolasyonu (Global Query Filter)
**Talep:**
- Şirket bazlı veri izolasyonu (FAZ 4.1)
- Her şirket sadece kendi verilerini görmeli

**Yapılanlar:**

**1. Entity'lere SirketId Eklendi:**
- `Cari.cs` - SirketId + Sirket navigation property
- `Sofor.cs` - SirketId + Sirket navigation property
- `Arac.cs` - SirketId + Sirket navigation property
- `Fatura.cs` - SirketId + Sirket navigation property
- `Guzergah.cs` - SirketId + Sirket navigation property
- `BankaHesap.cs` - SirketId + Sirket navigation property
- `BankaKasaHareket.cs` - SirketId + Sirket navigation property

**2. ApplicationDbContext Güncellemesi:**
- ITenantService constructor injection eklendi
- Her entity için Global Query Filter güncellendi:
  - `!e.IsDeleted` + Multi-tenant filtreleme
  - SuperAdmin/Admin bypass desteği
  - `SirketId == null` sistem geneli veri desteği
- Entity configuration'lara Sirket ilişkisi eklendi

**3. Global Query Filter Mantığı:**
```csharp
entity.HasQueryFilter(e => !e.IsDeleted && 
    (_tenantService == null || _tenantService.IsSuperAdmin || 
     e.SirketId == null || e.SirketId == _tenantService.CurrentSirketId));
```
- `_tenantService == null`: Migration/seeding sırasında bypass
- `_tenantService.IsSuperAdmin`: Admin kullanıcılar tüm veriyi görür
- `e.SirketId == null`: Sistem geneli veriler (tüm şirketlere açık)
- `e.SirketId == CurrentSirketId`: Şirkete özel veri filtreleme

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/Cari.cs` (SirketId eklendi)
- `CRMFiloServis.Shared/Entities/Sofor.cs` (SirketId eklendi)
- `CRMFiloServis.Shared/Entities/Arac.cs` (SirketId eklendi)
- `CRMFiloServis.Shared/Entities/Fatura.cs` (SirketId eklendi)
- `CRMFiloServis.Shared/Entities/Guzergah.cs` (SirketId eklendi)
- `CRMFiloServis.Shared/Entities/BankaHesap.cs` (SirketId eklendi)
- `CRMFiloServis.Shared/Entities/BankaKasaHareket.cs` (SirketId eklendi)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (ITenantService injection + Global Query Filter)

**Sonraki Adım:**
- EF Core Migration oluşturma: `dotnet ef migrations add AddMultiTenantSirketId`
- Migration uygulama: `dotnet ef database update`

**Durum:** ✅ Tamamlandı

---

### Kayıt 132 - EF Core Sorgu Optimizasyonu (FAZ 4.3)
**Talep:**
- Lazy loading optimizasyonu ve N+1 sorunu çözümü

**Yapılanlar:**
- CariService N+1 sorunu çözüldü:
  - `GetAllWithBakiyeAsync` metodu optimize edildi
  - `GetPagedAsync` metodu optimize edildi
  - Her cari için 4 ayrı SQL sorgusu yerine 2 toplu sorgu kullanılıyor
  - `GetBulkBakiyeVerileriAsync` helper metodu eklendi
  - `ApplyBakiyeFromBulkData` helper metodu eklendi
  - `BulkBakiyeData` yardımcı sınıfı eklendi
- AsNoTracking yaygınlaştırıldı:
  - CariService okuma sorgularına AsNoTracking eklendi
  - FaturaService okuma sorgularına AsNoTracking eklendi (GetAllAsync, GetPagedAsync, GetByCariIdAsync, GetByTipAsync, GetByDurumAsync, GetOdenmemisFaturalarAsync, GetOdenmisFaturalarAsync, GetByDateRangeAsync)
  - AracService okuma sorgularına AsNoTracking eklendi (GetAllAsync, GetActiveAsync)

**Performans Kazancı:**
- 100 cari için: ~400 SQL sorgusu → ~2 SQL sorgusu
- Memory kullanımı azaldı (AsNoTracking ile change tracking kapalı)
- Sorgu süresi önemli ölçüde düştü

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/CariService.cs` (güncellendi)
- `CRMFiloServis.Web/Services/FaturaService.cs` (güncellendi)
- `CRMFiloServis.Web/Services/AracService.cs` (güncellendi)
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 131 - Webhook Desteği (FAZ 4.2)
**Talep:**
- Dış sistemlere olay bildirimi için webhook sistemi

**Yapılanlar:**
- Webhook Entity'leri oluşturuldu:
  - `WebhookEndpoint` - Webhook endpoint tanımları (URL, secret, retry ayarları, olay filtresi)
  - `WebhookLog` - Webhook gönderim logları (durum, HTTP yanıt, süre, retry sayısı)
  - `WebhookLogDurum` enum (Bekliyor, Gonderiliyor, Basarili, Basarisiz, YenidenDeneniyor, Iptal)
  - `WebhookOlayTipleri` static class - 20 olay tipi (Fatura.*, Cari.*, Arac.*, Sofor.*, Guzergah.*, Odeme.*)
- `IWebhookService` interface oluşturuldu:
  - Endpoint CRUD
  - Webhook tetikleme (TriggerWebhookAsync)
  - Log işlemleri
  - İstatistikler
- `WebhookService` implementasyonu:
  - HMAC-SHA256 imza (X-Webhook-Signature header)
  - Retry mekanizması (configurable max retry, delay)
  - Asenkron gönderim (fire and forget)
  - HTTP status tracking
  - Olay filtresi desteği
- Webhook yönetim UI'ı oluşturuldu:
  - Endpoint listesi ve CRUD
  - Test butonu
  - Log görüntüleme
  - İstatistik kartları
- ApplicationDbContext'e DbSet'ler eklendi
- Program.cs'e servis kaydı eklendi
- NavMenu'ye Webhook Yönetimi linki eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/CRMEntities.cs` (güncellendi)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi)
- `CRMFiloServis.Web/Services/Interfaces/IWebhookService.cs` (yeni)
- `CRMFiloServis.Web/Services/WebhookService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/Webhooks.razor` (yeni)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 130 - REST API + Swagger (FAZ 4.2)
**Talep:**
- REST API oluşturma ve Swagger dokümantasyonu

**Yapılanlar:**
- NuGet paketleri eklendi:
  - `Swashbuckle.AspNetCore` (Swagger/OpenAPI)
  - `Microsoft.AspNetCore.Authentication.JwtBearer` (JWT Authentication)
- `Program.cs` güncellemeleri:
  - JWT Bearer Authentication konfigürasyonu
  - Swagger/OpenAPI servisi ve UI
  - Bearer token şeması ile güvenlik tanımı
- `appsettings.json` güncellemeleri:
  - `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience` ayarları
- 6 API Controller oluşturuldu:
  - `AuthController` - JWT token oluşturma (login, refresh, verify)
  - `CarilerController` - Cari CRUD (list, get, create, update, delete, bakiye)
  - `AraclarController` - Araç CRUD (list, get, create, update, delete)
  - `SoforlerController` - Şoför CRUD (list, get, create, update, delete, performans)
  - `FaturalarController` - Fatura CRUD (list, get, create, delete, durum güncelleme, vadesi geçmişler)
  - `GuzergahlarController` - Güzergah CRUD (list, get, create, update, delete)
- Her controller için DTO modelleri tanımlandı
- Entity model uyumsuzlukları düzeltildi (KdvTutar, FaturaKalemleri, Rol.RolAdi, PlakaIslemTipi.Alis)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Program.cs`
- `CRMFiloServis.Web/appsettings.json`
- `CRMFiloServis.Web/Controllers/AuthController.cs` (yeni)
- `CRMFiloServis.Web/Controllers/CarilerController.cs` (yeni)
- `CRMFiloServis.Web/Controllers/AraclarController.cs` (yeni)
- `CRMFiloServis.Web/Controllers/SoforlerController.cs` (yeni)
- `CRMFiloServis.Web/Controllers/FaturalarController.cs` (yeni)
- `CRMFiloServis.Web/Controllers/GuzergahlarController.cs` (yeni)
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 129 - SMS Entegrasyonu
**Talep:**
- SMS gönderim altyapısı (birden fazla SMS sağlayıcı desteği)
- SMS şablonları
- Bildirim sistemi ile entegrasyon

**Yapılanlar:**
- SMS Entity'leri oluşturuldu:
  - `SmsAyar` - SMS sağlayıcı ayarları (provider, API bilgileri, bakiye vb.)
  - `SmsLog` - SMS gönderim logları
  - `SmsSablon` - SMS şablonları
  - `SmsProvider` enum (NetGSM, İletimerkezi, Mutlucell, Twilio, JetSMS, Verimor)
  - `SmsGonderimDurum` enum (Bekliyor, Gonderildi, Iletildi, Basarisiz, Iptal)
  - `SmsTipi` enum (Bildirim, VadeHatirlatma, OdemeBildirimi, FaturaBildirimi, Duyuru, DogrulamaKodu, Pazarlama)
- `ISmsService` interface oluşturuldu:
  - SMS ayarları CRUD
  - Bakiye sorgulama
  - Tekli/toplu SMS gönderimi
  - Şablonlu gönderim
  - Log yönetimi
  - İstatistik
- `SmsService` implementasyonu:
  - NetGSM API entegrasyonu
  - İletimerkezi API entegrasyonu
  - Mutlucell API entegrasyonu
  - Twilio API entegrasyonu
  - Telefon numarası formatlama
  - Hata kodu açıklamaları
- `SmsMigrationHelper` oluşturuldu (runtime tablo oluşturma)
- `BildirimAyar` entity'sine SMS tercihleri eklendi:
  - `SmsAlsin`, `SmsTelefon`, `SmsVadeHatirlatma`, `SmsBelgeHatirlatma`
- `/ayarlar/sms` sayfası oluşturuldu:
  - SMS sağlayıcı ayarları yönetimi
  - SMS şablon yönetimi
  - Bakiye sorgulama
  - Test SMS gönderimi
  - İstatistik paneli
  - Desteklenen sağlayıcılar listesi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/CRMEntities.cs`
- `CRMFiloServis.Web/Services/Interfaces/ISmsService.cs` (yeni)
- `CRMFiloServis.Web/Services/SmsService.cs` (yeni)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs`
- `CRMFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/SmsAyarlari.razor` (yeni)
- `CRMFiloServis.Web/Program.cs`
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 128 - E-Fatura Entegrasyonu (GİB) Durum Takibi
**Talep:**
- E-Fatura tarafında GİB gönderim sürecinin hazırlanması ve durum takibi

**Yapılanlar:**
- `Fatura` entity'sine GİB durum alanları eklendi:
  - `GibDurumu`
  - `GibGonderimTarihi`
  - `GibDurumGuncellemeTarihi`
  - `GibDurumMesaji`
- `IEFaturaXmlService` içine `GibDurumGuncelleAsync` eklendi
- `EFaturaXmlService` içinde:
  - XML oluşturma sonrası faturayı otomatik `XmlHazirlandi` durumuna alma
  - gönderime hazır / gönderildi / kabul / red güncelleme akışı
  eklendi
- `EFaturaXml.razor` içinde:
  - GİB durumu filtresi
  - durum rozeti
  - gönderime hazırla / gönderildi / kabul / red aksiyonları
  eklendi
- `FaturaGibDurumMigrationHelper` oluşturuldu ve `Program.cs` içine bağlandı

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/Fatura.cs`
- `CRMFiloServis.Web/Services/Interfaces/IEFaturaXmlService.cs`
- `CRMFiloServis.Web/Services/EFaturaXmlService.cs`
- `CRMFiloServis.Web/Services/FaturaService.cs`
- `CRMFiloServis.Web/Components/Pages/Faturalar/EFaturaXml.razor`
- `CRMFiloServis.Web/Data/Migrations/FaturaGibDurumMigrationHelper.cs`
- `CRMFiloServis.Web/Program.cs`
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 127 - Puantaj Onay Sistemi
**Talep:**
- Personel puantaj modülüne onay sürecinin eklenmesi

**Yapılanlar:**
- `PersonelPuantaj` entity'sine onay alanları eklendi
- `PersonelPuantajOnayMigrationHelper` oluşturuldu
- `IPuantajService` ve `PuantajService` içine:
  - `OnayaGonderAsync`
  - `OnaylaAsync`
  - `ReddetAsync`
  akışları eklendi
- `CalismaPuantaji.razor` içinde:
  - onay özeti kartları
  - toplu onaya gönderme
  - satır bazlı onaya gönder / onayla / reddet butonları
  - onay durumu rozeti
  eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/PersonelPuantaj.cs`
- `CRMFiloServis.Web/Services/Interfaces/IPuantajService.cs`
- `CRMFiloServis.Web/Services/PuantajService.cs`
- `CRMFiloServis.Web/Components/Pages/Personel/CalismaPuantaji.razor`
- `CRMFiloServis.Web/Data/Migrations/PersonelPuantajOnayMigrationHelper.cs`
- `CRMFiloServis.Web/Program.cs`
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 126 - Maaşa Mahsup (Masraf/Ödeme)
**Talep:**
- Personel finans tarafındaki açık avansların maaşa kesinti olarak mahsup edilmesi

**Yapılanlar:**
- `IPersonelFinansService` içine `MaasaAcikAvansMahsupEtAsync` eklendi
- `PersonelFinansService` içinde:
  - açık avansları bulma
  - maaşın ödenebilir tutarı kadar mahsup etme
  - `PersonelAvansMahsup` kaydı oluşturma
  - maaş üzerindeki `Avans` kesintisini güncelleme
  akışı eklendi
- `MaasYonetimi.razor` içinde açık avans listesi ve mahsup butonu eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/IPersonelFinansService.cs`
- `CRMFiloServis.Web/Services/PersonelFinansService.cs`
- `CRMFiloServis.Web/Components/Pages/Personel/MaasYonetimi.razor`
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 125 - ASP.NET Core Identity Entegrasyonu
**Talep:**
- Kullanıcı yönetimi altyapısının gerçek `ASP.NET Core Identity` omurgasına bağlanması

**Yapılanlar:**
- `KullaniciUserStore` eklendi ve mevcut `Kullanici` tablosu `IUserPasswordStore` üzerinden Identity'ye bağlandı
- `KullaniciPasswordHasher` eklendi
  - yeni parolalar Identity formatında hashleniyor
  - eski SHA tabanlı parolalar doğrulanabiliyor
  - başarılı girişte otomatik rehash yapılabiliyor
- `Program.cs` içinde:
  - `IUserStore<Kullanici>`
  - `IPasswordHasher<Kullanici>`
  - `AddIdentityCore<Kullanici>().AddUserStore<KullaniciUserStore>()`
  kaydı eklendi
- `KullaniciService` içinde giriş ve parola değiştirme akışları `UserManager<Kullanici>` ile çalışacak şekilde güncellendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Program.cs`
- `CRMFiloServis.Web/Services/KullaniciService.cs`
- `CRMFiloServis.Web/Services/KullaniciPasswordHasher.cs`
- `CRMFiloServis.Web/Services/KullaniciUserStore.cs`
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 124 - Kullanıcı Kayıt/Giriş
**Talep:**
- Kullanıcı yönetimi fazında self-servis kayıt/giriş akışının tamamlanması

**Yapılanlar:**
- `IKullaniciService` içine `KayitOlAsync` eklendi
- `KullaniciService` içinde:
  - lisans kullanıcı limit kontrolü
  - varsayılan `Kullanici` rolü ile kayıt
  - benzersiz kullanıcı adı ve e-posta kontrolü
  akışı eklendi
- `Register.razor` sayfası oluşturuldu
- `Login.razor` içine kayıt ekranına yönlendiren link eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/IKullaniciService.cs`
- `CRMFiloServis.Web/Services/KullaniciService.cs`
- `CRMFiloServis.Web/Components/Pages/Login.razor`
- `CRMFiloServis.Web/Components/Pages/Register.razor`
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 123 - Şifre Sıfırlama
**Talep:**
- Login ekranındaki `Şifremi Unuttum` akışının gerçek şifre sıfırlama özelliğine dönüştürülmesi

**Yapılanlar:**
- `IKullaniciService` içine `SifremiUnuttumAsync` eklendi
- `KullaniciService` içinde:
  - kullanıcı adı/e-posta ile kullanıcı bulma
  - geçici şifre üretme
  - şifreyi güncelleme ve kilidi kaldırma
  - geçici şifreyi e-posta ile gönderme
  - e-posta başarısızsa eski şifre hash'ine geri dönme
  akışı eklendi
- `Login.razor` içinde açılır şifre sıfırlama paneli eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/IKullaniciService.cs`
- `CRMFiloServis.Web/Services/KullaniciService.cs`
- `CRMFiloServis.Web/Components/Pages/Login.razor`
- `ROADMAP.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 122 - Excel Export İyileştirme
**Talep:**
- FAZ 2.3 kapsamındaki mevcut Excel export yapısının iyileştirilmesi
- Rapor sayfalarındaki dağınık export mantığının ortak servis yapısına taşınması

**Yapılanlar:**
- `IExcelService` arayüzüne yeni export imzaları eklendi:
  - `ExportSoforPerformansRaporu`
  - `ExportSoforKarsilastirmaRaporu`
  - `ExportAracKarlilikRaporu`
  - `ExportAracKarlilikKarsilastirmaRaporu`
- `ExcelService` içinde şoför performans ve araç karlılık için servis tabanlı export üretimi eklendi
- `SoforPerformansRapor.razor` generic `ExportToExcel` yerine yeni servis metodlarını kullanacak şekilde güncellendi
- `AracKarlilikRapor.razor` generic `ExportToExcel` yerine yeni servis metodlarını kullanacak şekilde güncellendi
- `downloadFile` çağrıları tutarlı hale getirildi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/IExcelService.cs`
- `CRMFiloServis.Web/Services/ExcelService.cs`
- `CRMFiloServis.Web/Components/Pages/Raporlar/SoforPerformansRapor.razor`
- `CRMFiloServis.Web/Components/Pages/Raporlar/AracKarlilikRapor.razor`
- `ROADMAP.md`

**Doğrulama:**
- `run_build` başarılı
- `get_tests` ile ilgili Excel testi aranmış, eşleşen test bulunmamış

**Durum:** ✅ Tamamlandı

---

### Kayıt 102 - Menü Düzeni, Global Hata Sayfası ve Gelen Fatura Import İyileştirmesi
**Talep:**
- EBYS ekranlarının ayrı bir belge yönetimi başlığı altında toplanması
- Destek ve entegrasyon bağlantılarının ayarlar altına taşınması
- Programın hata anında kırılmak yerine rapor ekranı göstermesi ve önceki sayfaya dönebilmesi
- Gelen faturalar ekranında firma bazlı `Excel / XML / XML+PDF` aktarım butonunun çalışmasının düzeltilmesi
- İşletim kurallarının dosyaya kaydedilmesi

**Yapılanlar:**
- `NavMenu.razor` güncellendi
  - `EBYS Belge Yönetimi` başlığı oluşturuldu
  - `Destek` ve `Entegrasyon` menüleri `Ayarlar` altına taşındı
- Global hata yönetimi eklendi
  - `AppIssueStateService`
  - `AppRouteTracker`
  - `AppErrorBoundary`
  - `Error.razor` artık `ters-giden-bir-sey` rapor ekranı olarak çalışıyor
- `GelenFaturalar.razor` import modalı iyileştirildi
  - import butonu yalnızca firma ve uygun dosya seçildiğinde aktif oluyor
  - `InputFile` bileşeni aktarım tipine göre yeniden oluşturuluyor
  - `XML + PDF` seçiminde dosya sayısı bilgisi gösteriliyor
- İşletim kural dosyası oluşturuldu:
  - `docs/filo-servis-isletim-kurallari.md`

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor`
- `CRMFiloServis.Web/Components/App.razor`
- `CRMFiloServis.Web/Components/Pages/Error.razor`
- `CRMFiloServis.Web/Components/Pages/EFatura/GelenFaturalar.razor`
- `CRMFiloServis.Web/Program.cs`
- `CRMFiloServis.Web/Services/AppIssueStateService.cs`
- `CRMFiloServis.Web/Components/Shared/AppRouteTracker.razor`
- `CRMFiloServis.Web/Components/Shared/AppErrorBoundary.cs`
- `docs/filo-servis-isletim-kurallari.md`

**Durum:** ✅ Tamamlandı

---

### Kayıt 103 - Araç Masraf Servisinde Sahiplik Kurallarının Uygulanması
**Talep:** `Özmal / Kiralık / Komisyon` sahiplik düzenine göre araç masrafı akışının uygulanması.

**Yapılanlar:**
- `AracMasrafService` sorgularına `IsDeleted` filtresi eklendi.
- `Komisyon` sahiplik tipindeki araçlarda masraf kaydı oluşturulurken/güncellenirken:
  - `SoforId` otomatik temizleniyor
  - `CariId` komisyoncu cari üzerinden zorunlu hale getiriliyor
  - muhasebe karşı hesabı komisyoncu cari hesabına yönleniyor
- muhasebe açıklamaları sahiplik tipini gösterecek şekilde güncellendi.

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/AracMasrafService.cs`

**Durum:** ✅ Tamamlandı

---

### Kayıt 104 - Mali Analiz Servisinde Sahiplik Kurallarının Yaygınlaştırılması
**Talep:** `Özmal / Kiralık / Komisyon` sahiplik kurallarının mali analiz ve segment raporlarına uygulanması.

**Yapılanlar:**
- `MaliAnalizService` içinde `IsDeleted` kayıtları analizlerden çıkarıldı.
- `Komisyon` segmenti artık `KomisyonVar` yerine doğrudan `AracSahiplikTipi.Komisyon` ile hesaplanıyor.
- `Kiralık` segmentte firma üzerindeki araç masrafları da gider hesabına dahil edildi.
- `Yıllık trend`, `gider dağılımı` ve `araç bazlı kârlılık` hesapları sahiplik mantığına göre hizalandı.

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/MaliAnalizService.cs`

**Durum:** ✅ Tamamlandı

---

### Kayıt 105 - Puantaj ve Eşleştirme Servisinde Sahiplik Kurallarının Uygulanması
**Talep:** `Özmal / Kiralık / Komisyon` sahiplik kurallarının günlük operasyon puantajı ve eşleştirme şablonlarına uygulanması.

**Yapılanlar:**
- `FiloKomisyonService` içinde eşleştirme kaydetme/güncelleme sırasında sahiplik kuralları uygulanmaya başlandı.
- `Özmal` ve `Kiralık` araçlarda `TaseronaOdenenUcret` otomatik olarak `0` yapılıyor.
- `Komisyon` araçlarda günlük puantaj tahakkuku şablon ücreti ve puantaj çarpanına göre otomatik hesaplanıyor.
- `Mazeretli / mazeretsiz / kurum iptali` durumlarında tahakkuklar sıfırlanıyor.

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/FiloKomisyonService.cs`

**Durum:** ✅ Tamamlandı

---

### Kayıt 106 - Eşleştirme ve Operasyon Puantajı UI Tarafında Sahiplik Kuralları
**Talep:** Sahiplik kuralının kullanıcı ekranlarında görünür ve zorlayıcı hale getirilmesi.

**Yapılanlar:**
- `EslestirmeTanimlari.razor` içinde sahiplik badge'i eklendi.
- Araç seçimine göre `Taşerona/Şoföre Ödenen` alanı yalnızca `Komisyon` araçlarda aktif bırakıldı.
- Eşleştirme modalına sahiplik tipine göre açıklayıcı uyarı kutuları eklendi.
- `OperasyonPuantaji.razor` içinde satırlarda araç sahiplik badge'i gösterilmeye başlandı.
- Günlük puantaj modalında `Kurum Tarafından İptal` durumu eklendi.
- `Özmal` ve `Kiralık` araçlarda taşeron hakediş alanı pasifleştirildi ve UI tarafında sıfırlama uygulandı.

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Filo/EslestirmeTanimlari.razor`
- `CRMFiloServis.Web/Components/Pages/Filo/OperasyonPuantaji.razor`

**Durum:** ✅ Tamamlandı

---

### Kayıt 107 - Komisyonculuk İş Atamalarında Sahiplik Kuralları
**Talep:** `Özmal / Kiralık / Komisyon` düzeninin komisyonculuk iş atama akışına uygulanması.

**Yapılanlar:**
- `FiloOperasyonService` içinde `CreateIsAtamaAsync` ve `UpdateIsAtamaAsync` öncesine sahiplik kuralı normalizasyonu eklendi.
- `Özmal` araç atamalarında firma şoförü zorunlu hale getirildi ve dış kaynak alanları temizleniyor.
- `Kiralık` araç atamalarında firma şoförü zorunlu hale getirildi, kiralık cari varsayılanlanıyor ve kira bedeli araç kartından alınabiliyor.
- `Komisyon` araç atamalarında komisyoncu cari zorunlu hale getirildi, iç şoför kaldırılıyor ve dış kaynak alanları buna göre düzenleniyor.

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/FiloOperasyonService.cs`

**Durum:** ✅ Tamamlandı

---

### Kayıt 108 - Komisyonculuk Atama UI Tamamlama
**Talep:** Komisyonculuk iş atama ekranında sahiplik kurallarını kullanıcıya görünür ve yönetilebilir hale getirmek.

**Yapılanlar:**
- `KomisyonculukForm.razor` içine iş atama yönetim bölümü eklendi.
- Atama modalında araç seçimine göre sahiplik kuralı uyarısı ve otomatik atama tipi gösterimi eklendi.
- Bitiş tarihi, tedarikçi ödeme durumu ve ödeme tarihi alanları UI üzerinden yönetilebilir hale getirildi.
- Atama listesinde sahiplik tipi, atama tipi ve ödeme durumu görünür hale getirildi.

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/FiloOperasyon/KomisyonculukForm.razor`

**Durum:** ✅ Tamamlandı

---

### Kayıt 109 - Mali Analiz Trend Sekmesinde Aylık/Yıllık Karşılaştırma
**Talep:** Açık kalan `Aylık/Yıllık karşılaştırmalı raporlar` ihtiyacının mali analiz dashboard'ında tamamlanması.

**Yapılanlar:**
- `MaliAnalizDashboard.razor` içindeki `Trend Karşılaştırma` sekmesi genişletildi.
- Seçili yıl ve önceki yıl için toplam net kar kartları eklendi.
- Aynı ayın geçen yıl ile net kar, gelir ve gider karşılaştırması eklendi.
- 12 aylık net kar fark ve değişim oranı tablosu eklendi.
- Mevcut yıllık trend grafiği korunarak karşılaştırma akışıyla birlikte çalışır hale getirildi.

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Raporlar/MaliAnalizDashboard.razor`

**Durum:** ✅ Tamamlandı

---

### Kayıt 110 - EBYS Belge Versiyon Kontrolü Sistemi
**Talep:** ROADMAP'te açık kalan `EBYS - Versiyon kontrolü` başlığının tamamlanması.

**Yapılanlar:**

**Entity Katmanı:**
- `BelgeVersiyon.cs` dosyası oluşturuldu (3 versiyon entity + 1 DTO):
  - `EbysEvrakDosyaVersiyon` - EBYS evrak dosyası versiyonları
  - `AracEvrakDosyaVersiyon` - Araç evrak dosyası versiyonları
  - `PersonelOzlukEvrakVersiyon` - Personel özlük evrak versiyonları
  - `BelgeVersiyonKarsilastirma` - Versiyon karşılaştırma DTO
- `EbysEvrakDosya` entity'sine eklendi: `VersiyonNo`, `SonDegisiklikNotu`, `Versiyonlar` collection
- `AracEvrakDosya` entity'sine eklendi: `VersiyonNo`, `SonDegisiklikNotu`, `Versiyonlar` collection
- `PersonelOzlukEvrak` entity'sine eklendi: `VersiyonNo`, `SonDegisiklikNotu`, `DosyaAdi`, `DosyaTipi`, `DosyaBoyutu`, `Versiyonlar` collection

**Veritabanı:**
- `ApplicationDbContext.cs` - 3 yeni DbSet eklendi:
  - `EbysEvrakDosyaVersiyonlari`
  - `AracEvrakDosyaVersiyonlari`
  - `PersonelOzlukEvrakVersiyonlari`

**Servis Katmanı:**
- `IBelgeVersiyonService.cs` interface oluşturuldu - 9 metod
- `BelgeVersiyonService.cs` implementasyonu yazıldı:
  - `ArsivleEbysEvrakDosyaAsync` - Dosya güncellemesinde eski versiyonu arşivleme
  - `ArsivleAracEvrakDosyaAsync` - Araç evrak versiyonlama
  - `ArsivlePersonelOzlukEvrakAsync` - Personel özlük versiyonlama
  - `GeriYukleEbysVersiyonAsync` - Eski versiyonu aktif yapma
  - `GeriYukleAracVersiyonAsync` - Araç evrak geri yükleme
  - `GeriYuklePersonelOzlukVersiyonAsync` - Personel özlük geri yükleme
  - `GetEbysEvrakDosyaVersiyonlariAsync` - Versiyon geçmişi listesi
  - `GetAracEvrakDosyaVersiyonlariAsync` - Araç evrak versiyon listesi
  - `GetPersonelOzlukEvrakVersiyonlariAsync` - Personel özlük versiyon listesi
  - `KarsilastirEbysVersiyonlarAsync` - İki versiyon karşılaştırma
- `IEbysEvrakService.cs` interface'e `DosyaGuncelleAsync` eklendi
- `EbysEvrakService.cs` - Dosya güncelleme ve versiyonlama implementasyonu

**Program.cs:**
- `IBelgeVersiyonService` / `BelgeVersiyonService` Scoped olarak kaydedildi

**UI Katmanı (EvrakDetay.razor):**
- Dosya listesinde versiyon numarası badge'i (v2, v3 vb.)
- `Versiyon Geçmişi` butonu ve modal'ı
- Versiyon geçmişi listesi (mevcut ve önceki versiyonlar)
- `Dosya Güncelle` butonu ve modal'ı
- Yeni dosya yükleme ve değişiklik notu girişi
- `Geri Yükle` butonu ile eski versiyonu aktif yapma
- Geri yükleme onay diyaloğu

**Özellikler:**
- Her dosya güncellemesinde önceki versiyon otomatik arşivleniyor
- Versiyon numarası her güncellemede artıyor (v1 → v2 → v3)
- Geri yükleme işlemi mevcut versiyonu da arşivliyor
- Değişiklik notu ile her versiyon için açıklama kaydı
- 3 farklı belge tipini destekliyor: EBYS Evrak, Araç Evrak, Personel Özlük

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/BelgeVersiyon.cs` (YENİ)
- `CRMFiloServis.Shared/Entities/EbysEvrak.cs` (güncellendi)
- `CRMFiloServis.Shared/Entities/AracEvrak.cs` (güncellendi)
- `CRMFiloServis.Shared/Entities/PersonelOzlukEvrak.cs` (güncellendi)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi)
- `CRMFiloServis.Web/Services/Interfaces/IBelgeVersiyonService.cs` (YENİ)
- `CRMFiloServis.Web/Services/BelgeVersiyonService.cs` (YENİ)
- `CRMFiloServis.Web/Services/Interfaces/IEbysEvrakService.cs` (güncellendi)
- `CRMFiloServis.Web/Services/EbysEvrakService.cs` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/EBYS/EvrakDetay.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 111 - EBYS Gelişmiş Belge Arama Sistemi
**Talep:** ROADMAP'te açık kalan `EBYS - Belge arama (içerik + metadata)` başlığının tamamlanması.

**Yapılanlar:**

**Entity Katmanı (EbysBelgeArama.cs):**
- Arama filtreleri:
  - `EbysGelismisAramaFiltre` - Tüm arama parametrelerini içeren filtre DTO
- Arama sonuçları:
  - `EbysAramaSonuc` - Sayfalı arama sonuçları (sonuçlar, istatistikler, süre)
  - `EbysAramaSonucItem` - Tekil sonuç öğesi (kaynak, belge adı, kategori, durum, risk vb.)
- İstatistikler:
  - `EbysAramaIstatistik` - Kaynak ve kategori bazlı sayımlar
- Kullanıcı verileri:
  - `EbysAramaGecmisi` - Kullanıcı arama geçmişi
  - `EbysKayitliArama` - Kaydedilmiş arama filtreleri
  - `EbysAramaOnerisi` - Arama önerileri
- Enum'lar:
  - `EbysAramaTipi` - Arama türü (tümü, belge adı, dosya adı, açıklama, kategori, ilgili kayıt)
  - `EbysAramaKaynak` - Arama kaynağı (Personel Özlük, Araç Evrak, Gelen Evrak, Giden Evrak)
  - `EbysTarihAlani` - Tarih filtre alanı
  - `EbysAramaSiralama` - Sıralama seçenekleri
  - `EbysOneriTipi` - Öneri tipleri

**Servis Katmanı:**
- `IEbysBelgeAramaService.cs` interface - 15+ metod
- `EbysBelgeAramaService.cs` implementasyonu:
  - `AraAsync` - Ana gelişmiş arama metodu (4 kaynakta paralel arama)
  - `HizliAraAsync` - Basit metin araması
  - `KaynaktaAraAsync` - Tek kaynakta arama
  - `AraPersonelOzlukAsync` - Personel özlük evraklarında arama
  - `AraAracEvrakAsync` - Araç evraklarında arama
  - `AraGelenEvrakAsync` / `AraGidenEvrakAsync` - EBYS evraklarında arama
  - `GetAramaOnerileriAsync` - Arama önerileri
  - `GetPopulerAramalarAsync` - Popüler aramalar
  - `GetIlgiliAramalarAsync` - İlgili aramalar
  - `GetAramaGecmisiAsync` - Kullanıcı arama geçmişi
  - `KaydetAramaGecmisiAsync` - Arama geçmişi kaydetme
  - `TemizleAramaGecmisiAsync` - Geçmiş temizleme
  - `GetKayitliAramalarAsync` - Kaydedilmiş aramalar
  - `AramaKaydetAsync` - Arama kaydetme
  - `GetGenelIstatistiklerAsync` - Genel istatistikler
  - `GetTumKategorilerAsync` - Tüm kategoriler

**Veritabanı (ApplicationDbContext.cs):**
- `EbysAramaGecmisleri` DbSet
- `EbysKayitliAramalar` DbSet

**Program.cs:**
- `IEbysBelgeAramaService` / `EbysBelgeAramaService` Scoped olarak kaydedildi

**UI Katmanı (BelgeArama.razor):**
- Hızlı arama kutusu (autocomplete önerileri)
- Gelişmiş filtre paneli (collapsible):
  - Kaynak seçimi (checkbox)
  - Tarih aralığı ve alan seçimi
  - Kategori seçimi
  - Durum filtreleri
  - Dosya filtreleri
  - Sıralama seçenekleri
- Sonuç istatistik kartları (kaynak bazlı)
- Sayfalı sonuç listesi:
  - Kaynak badge'i (renk kodlu)
  - Belge adı, kategori, ilgili kayıt
  - Durum ve risk göstergeleri
  - Dosya bilgileri
  - Detay linki
- Sayfalama kontrolü
- Yükleniyor göstergesi

**NavMenu Güncellemesi:**
- EBYS Belge Yönetimi altına "Belge Arama" linki eklendi

**Özellikler:**
- 4 farklı kaynakta arama (Personel Özlük, Araç Evrak, Gelen/Giden Evrak)
- Paralel arama ile yüksek performans
- Alaka skoru hesaplama ve sıralama
- Metin eşleştirme (belge adı, dosya adı, açıklama, kategori, ilgili kayıt)
- Tarih bazlı filtreleme
- Durum ve risk filtreleri
- Süresi dolmuş/yaklaşan evrak vurgulama
- Arama geçmişi ve kaydedilmiş aramalar desteği
- Kategori bazlı istatistikler

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/EbysBelgeArama.cs` (YENİ)
- `CRMFiloServis.Web/Services/Interfaces/IEbysBelgeAramaService.cs` (YENİ)
- `CRMFiloServis.Web/Services/EbysBelgeAramaService.cs` (YENİ)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/EBYS/BelgeArama.razor` (YENİ)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 112 - EBYS AI Entegrasyonu (OCR ve Belge Sınıflandırma)
**Talep:** ROADMAP'te açık kalan `EBYS AI entegrasyonu (OCR, otomatik sınıflandırma)` başlığının tamamlanması.

**Yapılanlar:**

**Interface Katmanı (IEbysAIService.cs):**
- OCR işlemleri:
  - `MetinCikarAsync(Stream, dosyaAdi)` - Belge içeriğinden metin çıkarma (stream)
  - `MetinCikarAsync(dosyaYolu)` - Dosya yolundan metin çıkarma
- Belge sınıflandırma:
  - `BelgeSiniflandirAsync(metin, belgeGrubu)` - AI ile otomatik kategori tahmini
- Belge analizi:
  - `BelgeOzetiOlusturAsync(metin, maxKarakter)` - AI ile belge özeti
  - `AnahtarKelimelerCikarAsync(metin, maxKelime)` - Anahtar kelime çıkarma
  - `BelgeBenzerligiHesaplaAsync(metin1, metin2)` - İki belge benzerlik skoru
  - `OneriGetirAsync(metin, belgeGrubu)` - Belge için öneri ve tahminler
- Durum kontrolü:
  - `DurumKontrolAsync()` - Ollama ve OCR durumu

**DTO'lar:**
- `OcrSonuc` - OCR işlem sonucu (metin, güven skoru, sayfa sayısı, süre)
- `OcrDetayBilgi` - OCR detayları (karakter, kelime, satır sayısı)
- `OcrSayfaBilgi` - Sayfa bazlı OCR bilgisi
- `BelgeSiniflandirmaSonuc` - Sınıflandırma sonucu (kategori, güven, alternatifler)
- `KategoriTahmin` - Kategori tahmin detayı
- `BelgeOneriSonuc` - Belge önerileri (konu, öncelik, vade tarihi)
- `AIDurumBilgi` - AI servis durum bilgisi (Ollama, OCR aktif mi)

**Enum'lar:**
- `BelgeTipi` - Belge grubu (EbysEvrak, PersonelOzluk, AracEvrak, Genel)

**Servis Katmanı (EbysAIService.cs):**
- OCR İşlemleri:
  - Tesseract OCR entegrasyonu (offline çalışabilir)
  - PDF metin çıkarma (doğrudan + OCR yedek)
  - Görsel dosya OCR (JPG, PNG, BMP, TIFF)
  - Güven skoru hesaplama (Türkçe kelime yoğunluğu)
- Belge Sınıflandırma:
  - Ollama AI ile kategori tahmini
  - Belge tipine göre kategori setleri:
    - EbysEvrak: resmi_yazi, sozlesme, fatura, dilekce, rapor, duyuru
    - PersonelOzluk: kimlik, egitim, saglik, sofor, sgk
    - AracEvrak: ruhsat, sigorta, muayene, bakim
  - JSON format prompt ve parse
- Belge Analizi:
  - AI ile özet oluşturma
  - Anahtar kelime çıkarma
  - Jaccard benzerlik hesaplama (stop words filtrelemeli)
  - Belge öneri sistemi (konu, öncelik, vade tahmini)
- Durum Kontrolü:
  - Ollama bağlantı kontrolü
  - Tesseract versiyon kontrolü

**UI Katmanı (EbysAIPanel.razor):**
- Genişletilebilir/daraltılabilir AI panel
- Dosya yükleme (PDF, JPG, PNG, BMP, TIFF - max 10MB)
- Belge grubu seçimi
- Manuel metin girişi seçeneği
- OCR sonuç kartı (güven skoru, kelime/satır sayısı, süre)
- Sınıflandırma sonuç kartı (tahmin edilen kategori, güven, açıklama)
- Belge özeti kartı
- Anahtar kelimeler badge listesi
- Yükleniyor animasyonu ve durum mesajları
- Event callback'leri (kategori seçimi, özet oluşturma, metin çıkarma)

**Evrak Detay Entegrasyonu (EvrakDetay.razor):**
- Sağ kolona AI Panel eklendi
- Kategori önerisi evrak özetine uygulanabiliyor
- AI özeti evrak özet alanına aktarılabiliyor

**Konfigürasyon (appsettings.json):**
```json
"Ocr": {
  "Enabled": true,
  "TesseractPath": "tesseract",
  "TessDataPath": ""
}
```

**Program.cs:**
- `IEbysAIService` / `EbysAIService` Scoped olarak kaydedildi

**Özellikler:**
- Tesseract OCR ile offline çalışabilirlik
- Ollama AI ile internetsiz belge analizi
- Türkçe + İngilizce OCR desteği
- 4 farklı belge grubunu destekliyor
- AI kategori önerisi ve güven skoru
- Otomatik anahtar kelime çıkarma
- Belge özeti oluşturma
- Jaccard benzerlik algoritması

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/IEbysAIService.cs` (YENİ)
- `CRMFiloServis.Web/Services/EbysAIService.cs` (YENİ)
- `CRMFiloServis.Web/Components/Shared/EbysAIPanel.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/EBYS/EvrakDetay.razor` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/appsettings.json` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 113 - EBYS Semantic Search (Akıllı Belge Arama) Sistemi
**Talep:** ROADMAP'te açık kalan `Akıllı belge arama (semantic search)` başlığının tamamlanması.

**Yapılanlar:**

**OllamaService Embedding Desteği (OllamaService.cs):**
- Embedding metodları eklendi:
  - `EmbeddingOlusturAsync(metin)` - Tekil metin için vektör oluşturma
  - `TopluEmbeddingOlusturAsync(metinler)` - Toplu embedding işlemi
- `EmbeddingModelAdi` property - Kullanılan embedding modeli (nomic-embed-text)
- DTO'lar:
  - `OllamaEmbeddingRequest` - Embedding API isteği
  - `OllamaEmbeddingResponse` - Embedding API yanıtı

**Entity Katmanı (EbysBelgeArama.cs):**
- `EbysBelgeEmbedding` entity oluşturuldu:
  - `BelgeId`, `BelgeTipi`, `BelgeKodu`, `BelgeAdi`
  - `EmbeddingJson` - Vektör verisi JSON olarak saklanır
  - `Embedding` property - JSON↔float[] otomatik dönüşüm
  - `EmbeddingBoyutu` property - Vektör boyutu
  - `BelgeIcerigi` - Orijinal metin içeriği
  - `OlusturmaTarihi`, `GuncellemeTarihi`
- `SemanticAramaSonuc` DTO oluşturuldu:
  - Sonuç özellikleri: `BelgeId`, `BelgeTipi`, `BelgeAdi`, `BelgeKodu`
  - `BenzerlikSkoru` - Cosine similarity (0-1)
  - `Onizleme` - Metin önizlemesi

**Interface Katmanı (ISemanticSearchService.cs):**
- Embedding oluşturma:
  - `EmbeddingOlusturVeKaydetAsync(belgeId, belgeTipi, icerik)` - Tek belge indeksleme
  - `TumBelgeleriIndeksleAsync(belgeTipi?)` - Toplu indeksleme
- Semantic arama:
  - `SemanticAraAsync(sorgu, maksimumSonuc, minimumSkor)` - Vektör tabanlı arama
- Yardımcı:
  - `IndekslemeDurumuGetirAsync()` - İndekslenen belge sayıları
  - `EmbeddingSilAsync(belgeId, belgeTipi)` - Embedding silme

**Servis Katmanı (SemanticSearchService.cs):**
- Embedding oluşturma:
  - Ollama `/api/embeddings` API entegrasyonu
  - `nomic-embed-text` modeli kullanımı
  - JSON formatında vektör depolama
- Semantic arama algoritması:
  - Cosine Similarity hesaplama
  - Benzerlik skoru eşik filtreleme
  - Sonuç sıralama (en yüksek skor önce)
- Toplu indeksleme:
  - 4 belge kaynağı desteği:
    - PersonelOzlukEvrak (EvrakTanim)
    - AracEvrak (EvrakKategorisi)
    - EbysEvrak (Yon - Gelen/Giden)
    - EbysBelge (Baslik)
  - Paralel işleme desteği
- Durum takibi:
  - Belge tipine göre indekslenmiş sayı raporlama

**UI Katmanı (BelgeArama.razor):**
- "AI Akıllı Arama" toggle switch
- Semantic arama modu aktifleştirildiğinde:
  - Klasik arama yerine vektör tabanlı arama
  - Benzerlik skoru gösterimi
  - Minimum skor filtresi (varsayılan 0.5)
- "Indeksi Yenile" butonu - Tüm belgeleri yeniden indeksler
- Semantic sonuç listesi:
  - Belge tipi rozeti
  - Benzerlik skoru yüzdesi
  - Belge adı ve kodu
  - Önizleme metni

**Konfigürasyon (appsettings.json):**
```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "llama2",
  "EmbeddingModel": "nomic-embed-text"
}
```

**Program.cs:**
- `ISemanticSearchService` / `SemanticSearchService` Scoped olarak kaydedildi

**ApplicationDbContext.cs:**
- `EbysBelgeEmbeddingler` DbSet eklendi

**Özellikler:**
- Ollama embedding API ile vektör oluşturma
- Cosine Similarity algoritması ile benzerlik hesaplama
- JSON formatında vektör depolama (EF Core uyumlu)
- 4 farklı belge kaynağını destekliyor
- Minimum benzerlik skoru filtreleme
- Toplu indeksleme ve güncelleme
- UI toggle ile kolay aktivasyon

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/OllamaService.cs` (güncellendi - embedding metodları)
- `CRMFiloServis.Web/Services/Interfaces/IOllamaService.cs` (güncellendi - interface)
- `CRMFiloServis.Shared/Entities/EbysBelgeArama.cs` (güncellendi - EbysBelgeEmbedding entity)
- `CRMFiloServis.Web/Services/Interfaces/ISemanticSearchService.cs` (YENİ)
- `CRMFiloServis.Web/Services/SemanticSearchService.cs` (YENİ)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi - DbSet)
- `CRMFiloServis.Web/Program.cs` (güncellendi - DI)
- `CRMFiloServis.Web/appsettings.json` (güncellendi - EmbeddingModel)
- `CRMFiloServis.Web/Components/Pages/EBYS/BelgeArama.razor` (güncellendi - semantic search UI)

**Durum:** ✅ Tamamlandı

---

### Kayıt 114 - EBYS Örnek Veri ve Test Senaryoları
**Talep:** ROADMAP'te açık kalan `EBYS - Örnek veri ve test senaryoları` başlığının tamamlanması.

**Yapılanlar:**

**DbInitializer.cs Güncellemeleri:**
- `SeedEbysOrnekVerileriAsync()` metodu eklendi - EBYS modülü için kapsamlı örnek veri oluşturma
- `InitializeAsync` metodlarından yeni seed metodu çağrılıyor

**1. EBYS Evrak Kategorileri (10 kategori):**
- Resmi Yazışmalar (mavi, bi-envelope-paper)
- Sözleşmeler (yeşil, bi-file-earmark-text)
- Faturalar / Mali Belgeler (sarı, bi-receipt)
- Raporlar (mor, bi-file-earmark-bar-graph)
- Dilekçeler / Başvurular (turkuaz, bi-file-earmark-person)
- İhale Belgeleri (kırmızı, bi-briefcase)
- SGK / Vergi Belgeleri (turuncu, bi-building)
- Araç Belgeleri (teal, bi-truck)
- Personel Belgeleri (indigo, bi-person-badge)
- Diğer (gri, bi-folder)

**2. Özlük Evrak Tanımları (19 evrak tipi):**

*Kimlik Belgeleri:*
- Nüfus Cüzdanı Fotokopisi (zorunlu)
- İkametgah Belgesi (zorunlu)
- Vesikalık Fotoğraf (zorunlu)
- Sabıka Kaydı (zorunlu)

*Eğitim Belgeleri:*
- Diploma Fotokopisi (opsiyonel)
- Sertifika / Kurs Belgeleri (opsiyonel)

*Sağlık Belgeleri:*
- Sağlık Raporu (zorunlu)
- Kan Grubu Belgesi (opsiyonel)

*Şoför Belgeleri (GecerliGorevler: "1" - Şoför):*
- Ehliyet Fotokopisi (zorunlu)
- SRC Belgesi (zorunlu)
- Psikoteknik Belgesi (zorunlu)

*SGK Belgeleri:*
- SGK İşe Giriş Bildirgesi (zorunlu)
- SGK Hizmet Dökümü (opsiyonel)

*İşe Giriş Belgeleri:*
- İş Başvuru Formu (zorunlu)
- İş Sözleşmesi (zorunlu)
- İşe Giriş Bildirgesi (zorunlu)
- IBAN Bilgi Formu (zorunlu)
- Acil Durum İletişim Formu (opsiyonel)

**3. Örnek EBYS Evrakları (7 evrak):**

*Gelen Evraklar (4 adet):*
- GE-2025-00001: Personel Servis Hizmeti İhale Daveti (Belediye, İhale, Yüksek öncelik, İşleniyor)
- GE-2025-00002: SGK Denetim Bildirimi (SGK, Gizli, Acil öncelik, Tamamlandı)
- GE-2025-00003: Sözleşme Yenileme Talebi (ABC Sanayi, Sözleşme, Cevap Bekliyor)
- GE-2025-00004: Araç Muayene Hatırlatması (TÜVTÜRK, Resmi Yazışma, Beklemede)

*Giden Evraklar (3 adet):*
- GI-2025-00001: Personel Servis Hizmeti Teklif (KEP ile gönderildi, Tamamlandı)
- GI-2025-00002: Aylık Fatura Gönderimi - Ocak 2025 (E-mail ile gönderildi, Tamamlandı)
- GI-2025-00003: Sözleşme Yenileme Cevabı (Elden teslim, Tamamlandı)

**Özellikler:**
- ✅ Evrak kategorileri: 10 farklı kategori (renk kodlu, ikonlu)
- ✅ Özlük evrak tanımları: 19 evrak tipi (7 kategori, zorunlu/opsiyonel)
- ✅ Şoför görevine özel evraklar (GecerliGorevler filtreleme)
- ✅ Gelen evraklar: 4 örnek (farklı durumlar, öncelikler, gizlilik seviyeleri)
- ✅ Giden evraklar: 3 örnek (farklı gönderim yöntemleri)
- ✅ Cevap süresi takibi (CevapSuresi, CevapGerekli)
- ✅ Gerçekçi tarih ve belge numaraları

**Test Senaryoları Destekli:**
- Evrak arama testi (farklı kategorilerde evrak mevcut)
- Özlük checklist testi (zorunlu ve opsiyonel evraklar)
- Evrak durumu takip testi (Beklemede, İşleniyor, Cevap Bekliyor, Tamamlandı)
- Öncelik filtresi testi (Düşük, Normal, Yüksek, Acil)
- Gizlilik seviyesi testi (Normal, Gizli)
- Gönderim yöntemi testi (KEP, Email, Elden)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Data/DbInitializer.cs` (güncellendi - SeedEbysOrnekVerileriAsync eklendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 115 - Filo Sahiplik Kurallarının Raporlara Yayılması
**Talep:** Filo sahiplik kurallarının (Özmal/Kiralık/Komisyon) rapor servislerine ve rapor UI sayfalarına eklenmesi.

**Yapılanlar:**

**IRaporService.cs Güncellemeleri:**
- `GetServisCalismaRaporuAsync` metoduna `AracSahiplikTipi? sahiplikTipi` parametresi eklendi
- `GetAracMasrafRaporuAsync` metoduna `AracSahiplikTipi? sahiplikTipi` parametresi eklendi
- `GetSoforPerformansAsync` metoduna `AracSahiplikTipi? sahiplikTipi` parametresi eklendi
- `GetSoforKarsilastirmaAsync` metoduna `AracSahiplikTipi? sahiplikTipi` parametresi eklendi
- `GetAracKarsilastirmaAsync` metoduna `AracSahiplikTipi? sahiplikTipi` parametresi eklendi

**RaporService.cs Güncellemeleri:**
- Tüm rapor metodlarına sahiplik filtreleme eklendi
- LINQ sorgularında `Where(s => s.Arac.SahiplikTipi == sahiplikTipi.Value)` kullanıldı
- Araç, servis çalışması ve masraf sorgularında filtreleme uygulandı

**UI Sayfaları Güncellemeleri:**

1. **ServisCalismaRapor.razor:**
   - Sahiplik dropdown filtresi eklendi (Tümü, Özmal, Kiralık, Komisyon)
   - `secilenSahiplikTipi` değişkeni eklendi
   - `RaporGetir()` metodu sahiplik parametresi ile güncellendi
   - `Enum.TryParse` ile string-to-enum dönüşümü

2. **AracKarlilikRapor.razor:**
   - Sahiplik dropdown filtresi eklendi
   - `seciliSahiplikTipi` değişkeni eklendi
   - `filtreliAraclar` computed property ile araç listesi filtreleme
   - `SahiplikDegisti()` metodu ile araç seçimi sıfırlama

3. **SoforPerformansRapor.razor:**
   - Sahiplik dropdown filtresi eklendi
   - `secilenSahiplikTipi` değişkeni eklendi
   - `GetSahiplikTipi()` helper metodu eklendi
   - Performans ve karşılaştırma servis çağrıları güncellendi

4. **AracMasrafRapor.razor:**
   - Sahiplik dropdown filtresi eklendi
   - `seciliSahiplikTipi` değişkeni eklendi
   - `filtreliAraclar` computed property eklendi
   - `SahiplikDegisti()` metodu eklendi
   - `RaporGetir()` sahiplik parametresi ile güncellendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/IRaporService.cs` (güncellendi)
- `CRMFiloServis.Web/Services/RaporService.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/Raporlar/ServisCalismaRapor.razor` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/Raporlar/AracKarlilikRapor.razor` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/Raporlar/SoforPerformansRapor.razor` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/Raporlar/AracMasrafRapor.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 116 - Kullanıcı Profil Sayfası
**Talep:** ROADMAP'te belirtilen Kullanıcı Yönetimi & Yetkilendirme modülünden "Kullanıcı profil sayfası" özelliğinin eklenmesi.

**Mevcut Durum Analizi:**
- Proje özel kimlik doğrulama sistemi kullanıyor (ASP.NET Core Identity değil)
- `Kullanici` entity: Id, KullaniciAdi, SifreHash, AdSoyad, Email, Telefon, RolId, Tema, KompaktMod
- `KullaniciService`: Login, Logout, CRUD işlemleri, şifre sıfırlama
- `AppAuthenticationStateProvider`: Claims tabanlı kimlik doğrulama
- Login ve KullaniciYonetimi sayfaları mevcut

**Eksik Özellik:**
- Kullanıcının kendi profilini görüntüleyip düzenleyebileceği sayfa yoktu

**Yapılanlar:**

**Profil.razor Sayfası (YENİ):**
- `/ayarlar/profil` route'u ile erişilebilir
- 3 ana bölüm:
  1. **Profil Bilgileri:** Ad Soyad, E-posta, Telefon düzenleme
  2. **Şifre Değiştirme:** Mevcut şifre, yeni şifre, tekrar alanları
  3. **Tercihler:** Tema seçimi (Açık/Koyu/Sistem), Kompakt mod toggle

**Profil Özellikleri:**
- Aktif kullanıcı bilgilerini AuthenticationStateProvider'dan alır
- Avatar ikonu kullanıcı adının baş harflerini gösterir
- Kullanıcı adı, oluşturulma tarihi ve rol bilgisi görüntülenir
- Form doğrulama (Required, Email, Phone attributes)
- Şifre göster/gizle toggle butonları
- Responsive tasarım (card-based layout)
- Düzenleme modları ayrı (bilgi düzenleme, şifre değiştirme)

**NavMenu.razor Güncellemesi:**
- Ayarlar bölümüne "Profilim" linki eklendi
- `bi-person-circle` ikonu ile
- Kullanıcı Yönetimi'nden önce yerleştirildi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Ayarlar/Profil.razor` (YENİ)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 117 - Bildirim Sistemi (Vade ve Belge Süresi Uyarıları)
**Talep:** ROADMAP FAZ 2.2 - Bildirim Sistemi implementasyonu. Vade yaklaşan fatura bildirimleri, ehliyet/muayene/sigorta bitiş uyarıları ve uygulama içi bildirimler.

**Mevcut Durum Analizi:**
- `Bildirim` entity mevcut (CRMEntities.cs içinde)
- `BildirimTipi` enum mevcut ama sınırlı (Bilgi, Uyari, Hata, Basari, Sistem)
- Fatura entity: `VadeTarihi` alanı mevcut
- Araç entity: `TrafikSigortaBitisTarihi`, `KaskoBitisTarihi`, `MuayeneBitisTarihi` alanları mevcut
- Şoför entity: `EhliyetGecerlilikTarihi`, `SrcBelgesiGecerlilikTarihi`, `PsikoteknikGecerlilikTarihi`, `SaglikRaporuGecerlilikTarihi` alanları mevcut

**Yapılanlar:**

**1. Entity Güncellemeleri (CRMEntities.cs):**
- `BildirimTipi` enum genişletildi:
  - FaturaVade=8, EhliyetBitis=9, SrcBelgesi=10, Psikoteknik=11
  - SaglikRaporu=12, TrafikSigorta=13, Kasko=14, Muayene=15
  - DestekTalebi=16, Sistem=17
- `BildirimAyar` entity eklendi:
  - KullaniciId, BildirimTipi, Aktif, UyariGunSayisi
  - Kullanıcı bazlı bildirim tercihleri

**2. IBildirimService Interface (YENİ):**
- DTO'lar: BildirimOzet, BildirimDashboardDto
- CRUD: GetKullaniciBildirimlerAsync, GetOkunmamisSayisi, Okundu İşaretle, Sil
- Tarama: TaraVeBildirimOlusturAsync, VadeYaklasanFaturalariTaraAsync, SuresiDolanBelgeleriTaraAsync
- Ayarlar: GetKullaniciAyarlarAsync, AyarKaydetAsync
- Dashboard: GetDashboardOzetAsync

**3. BildirimService Implementation (YENİ):**
- **VadeYaklasanFaturalariTaraAsync:** Ödenmemiş faturaları tarar, vade yaklaşanları bildirim olarak kaydeder
- **SuresiDolanBelgeleriTaraAsync:** 
  - Araçlar: Trafik Sigortası, Kasko, Muayene bitiş tarihleri
  - Şoförler: Ehliyet, SRC, Psikoteknik, Sağlık Raporu geçerlilik tarihleri
- Öncelik hesaplama: Kalan güne göre Acil/Yüksek/Orta/Düşük
- Mükerrer bildirim engelleme (EntityId + BildirimTipi kontrolü)
- Dashboard istatistikleri (tip bazlı sayımlar)

**4. BildirimPanel.razor (Navbar Dropdown - YENİ):**
- Navbar'da zil ikonu ile bildirim göstergesi
- Okunmamış sayı badge'i
- Dropdown panel: Son 10 bildirim listesi
- 60 saniye otomatik yenileme (Timer)
- Bildirim tıklama: Okundu işaretle + ilgili sayfaya yönlendir
- Tip bazlı ikon ve renk kodlaması

**5. Bildirimler.razor (Yönetim Sayfası - YENİ):**
- `/ayarlar/bildirimler` route'u
- **Sol Panel:** Bildirim listesi
  - Filtreler: Tümü/Okunmamış/Okunmuş, Bildirim tipi
  - Sayfalama ve toplu işlemler
- **Sağ Panel:** 
  - Dashboard özet kartları (tip bazlı sayımlar)
  - Bildirim ayarları (her tip için toggle + uyarı gün sayısı)
  - "Yeniden Tara" butonu ile manuel tarama tetikleme

**6. NavMenu.razor Güncellemesi:**
- Ayarlar bölümüne "Bildirimlerim" linki eklendi
- `bi-bell` ikonu ile

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/CRMEntities.cs` (güncellendi - BildirimTipi extended, BildirimAyar added)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi - BildirimAyarlari DbSet)
- `CRMFiloServis.Web/Services/Interfaces/IBildirimService.cs` (YENİ)
- `CRMFiloServis.Web/Services/BildirimService.cs` (YENİ)
- `CRMFiloServis.Web/Components/Shared/BildirimPanel.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/Bildirimler.razor` (YENİ)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi - IBildirimService DI)

**Özellikler:**
- ✅ Vade yaklaşan fatura bildirimleri
- ✅ Ehliyet/SRC/Psikoteknik/Sağlık Raporu bitiş uyarıları
- ✅ Trafik Sigortası/Kasko/Muayene bitiş uyarıları
- ✅ Uygulama içi bildirimler (navbar dropdown)
- ✅ Kullanıcı bazlı bildirim ayarları
- ✅ Otomatik tarama ve bildirim oluşturma
- ✅ Dashboard istatistikleri
- ✅ Öncelik hesaplama (kalan güne göre)

**Durum:** ✅ Tamamlandı

---

### Kayıt 101 - AI Asistan Floating Widget ve İhale Örnek Veri Oluşturma
**Talep:** AI Asistan'ın her sayfada erişilebilir olması ve İhale Hazırlık modülüne test verisi oluşturma özelliği eklenmesi.

**Yapılanlar:**

**AI Asistan Floating Widget:**
- `AIAsistanFloating.razor` bileşeni oluşturuldu
- Sağ alt köşede yüzen ChatGPT benzeri chat butonu
- Modal içinde tam AI sohbet arayüzü
- Ollama API ile streaming yanıt desteği
- Sistem bağlamını anlayan prompt
- `MainLayout.razor`'a widget eklendi (tüm sayfalarda erişilebilir)
- `NavMenu.razor`'dan eski AI Asistan linki kaldırıldı
- CSS animasyonları (pulse efekti, smooth geçişler)

**İhale Örnek Veri Oluşturma:**
- `IIhaleHazirlikService` interface'e `OrnekProjeOlusturAsync()` eklendi
- `IhaleHazirlikService`'e kapsamlı örnek veri metodu yazıldı:
  - Örnek Güzergah (Merkez - Organize Sanayi, 45km)
  - Örnek Şoför (32.000 TL brüt maaş)
  - Örnek Araç (Mercedes Sprinter, 34 ORNEK 001)
  - İhale Projesi (12 ay süreli, %30 enflasyon, %35 yakıt zam)
  - Güzergah Kalemi (35 personel, tüm masraf kalemleri dolu)
  - Puantaj Kaydı (Gun01-31 alanları, hafta içi 2 sefer, hafta sonu 0)
- `HaftaIciMi()` helper metodu eklendi
- `IhaleHazirlik.razor`'a "Örnek Veri Oluştur" butonu eklendi
- Oluşturulan proje detay sayfasına yönlendirme

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Shared/AIAsistanFloating.razor` (YENİ)
- `CRMFiloServis.Web/Components/Layout/MainLayout.razor` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)
- `CRMFiloServis.Web/Services/Interfaces/IIhaleHazirlikService.cs` (güncellendi)
- `CRMFiloServis.Web/Services/IhaleHazirlikService.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/Ihale/IhaleHazirlik.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 102 - Personel Maaş Yönetimi SGK/Kalan Ayrımı ve Banka Ödeme Listesi Genişletme
**Talep:** Personel maaş yönetiminde SGK maaş ve kalan maaş ayrımının görünür hale getirilmesi, banka ödeme listesinin genişletilmesi, görev filtreleme ve Excel export'un güncellenmesi.

**Yapılanlar:**

**Banka Ödeme Listesi Genişletme:**
- `BankaOdemeListesi.razor` tamamen güncellendi
- Ana tablo kolonları genişletildi:
  - SGK Maaş (mavi arka plan, tooltip ile açıklama)
  - Kalan Maaş / Ek Ödeme (sarı arka plan)
  - Ara Toplam (SGK + Kalan)
  - Avans, Kesinti, Alacak (mevcut kolonlar)
  - Ödenecek (yeşil vurgulu)
- Footer toplam satırı SGK/Kalan ayrımı ile güncellendi
- Görev Dağılımı Özet Tablosu genişletildi (7 kolon)
- Ödeme Özeti kartı yeniden düzenlendi:
  - Toplam SGK Maaş (mavi satır)
  - Toplam Kalan Maaş / Ek Ödeme (sarı satır)
  - Ara Toplam (gri satır, formül gösterimi)
  - Avans/Kesinti/Alacak satırları
  - Net Ödenecek Tutar

**Excel Export Güncelleme:**
- Sütun sayısı 10'dan 12'ye genişletildi
- SGK Maaş ve Kalan Maaş ayrı kolonlarda
- Ara Toplam kolonu eklendi
- SGK Maaş (açık mavi) ve Kalan Maaş (açık sarı) header renklendirmesi
- Görev Özeti tablosu 7 kolona genişletildi

**Model Sınıfları Güncelleme:**
- `PersonelOdemeKalemi` sınıfına yeni alanlar eklendi:
  - `SgkMaas`: SGK'ya bildirilen maaş
  - `KalanMaas`: Ek ödeme (TopluMaaş - SGK Maaş)
  - `AraToplam`: SGK + Kalan
- `GorevOzet` sınıfına yeni alanlar eklendi:
  - `ToplamSgkMaas`
  - `ToplamKalanMaas`
  - `ToplamAraToplam`

**Hesaplama Mantığı:**
```
SGK Maaş = Personel.SgkMaasi
Kalan Maaş = Personel.EkOdeme (TopluMaas - SgkMaasi)
Ara Toplam = SGK Maaş + Kalan Maaş
Ödenecek = Ara Toplam - Avans - Kesinti + Alacak
```

**Mevcut Özellikler (Zaten Çalışan):**
- ✅ SGKBordroDahilMi flag (default false)
- ✅ Görev filtreleme (PersonelGorev enum)
- ✅ EFT/Banka ödeme dosyası export
- ✅ Avans/Borç/Alacak entegrasyonu (PersonelFinansService)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Personel/BankaOdemeListesi.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Bilinen Sorunlar ve Teknik Borçlar

#### EF Core Global Query Filter Uyarıları
- `Cari` → `CariHatirlatma` ve `CariIletisimNot` ilişkilerinde global query filter uyumsuzluğu
- `Firma` → `FaturaSablon` ilişkisinde aynı sorun
- **Çözüm:** Navigation'ları optional yapma veya ilişkili entity'lere matching query filter ekleme

#### PostgreSQL Migration Sorunu
- `Hatirlaticilar` tablosu veritabanında mevcut değil
- Migration, olmayan tabloda constraint silmeye çalışıyor
- **Geçici Çözüm:** `EnsureCreated` ile tablo oluşturma
- **Kalıcı Çözüm:** Yeni migration oluşturma:
  ```bash
  dotnet ef migrations add FixHatirlaticilarTable -p CRMFiloServis.Web
  dotnet ef database update -p CRMFiloServis.Web
  ```

---

### Kayıt 099 - osTicket Benzeri Destek Talebi Sistemi (Kullanıcı ve Yetkili Arayüzü)
**Talep:** Destek talebi modülüne osTicket benzeri 2 ana arayüz eklenmesi: kullanıcı talep girişi + yetkili yönetimi (Kanban board).

**Yapılanlar:**
- `DestekDurum` enum'una yeni durumlar eklendi: `Taslak=0`, `Gonderildi=7`, `Islemde=8`, `Bitti=9`, `Onaylandi=10`
- 4 yeni Razor sayfası oluşturuldu:
  - `TalepGiris.razor` (`/destek-talepleri/talep-giris`, `/destek-talepleri/talep-giris/{TalepId:int}`)
    - Kullanıcı talep oluşturma ve düzenleme
    - Taslak kaydetme ve Gönder aksiyonları
    - Departman/Kategori/Öncelik seçimi
    - Konu ve açıklama girişi
  - `Taleplerim.razor` (`/destek-talepleri/taleplerim`)
    - Kullanıcının kendi taleplerini listeleme
    - Durum bazlı filtreleme
    - Taslak düzenleme/silme aksiyonları
    - Bitti durumundaki talepler için onaylama aksiyonu
  - `TalepTakip.razor` (`/destek-talepleri/talep-takip/{TalepId:int}`)
    - Talep detay görüntüleme
    - Durum akışı görseli (Taslak → Gönderildi → İşlemde → Bitti → Onaylandı)
    - Mesajlaşma/yorum sistemi
    - Aktivite geçmişi timeline
  - `TalepYonetim.razor` (`/destek-talepleri/yonetim`)
    - 4 kolonlu Kanban board (Atama Bekleyen, İşlemde, Onay Bekleyen, Tamamlanan)
    - Yetkili atama modalı
    - Durum değiştirme aksiyonları
    - Talep özet kartları
- `NavMenu.razor` güncellendi - CRM modülü altına destek talebi linkleri eklendi:
  - Taleplerim (`destektalebi.oku`)
  - Yeni Talep (`destektalebi.oku`)
  - Talep Yönetimi (`destektalebi.yaz`)
  - Tüm Talepler (`destektalebi.yaz`)
  - Bilgi Bankası (`destektalebi.oku`)
- `StokService.cs` içindeki `CreateUretimRecetesiAsync` metodu girintileme hatası düzeltildi (CS1026, CS1002, CS1513)
- Tüm yeni sayfalarda `OlusturulmaTarihi` → `CreatedAt` düzeltildi (BaseEntity uyumu)

**Durum Akışı:**
```
Taslak → Gönderildi → İşlemde → Bitti → Onaylandı
  ↑        ↑           ↑         ↑         ↑
  │        │           │         │         └── Kullanıcı onayı
  │        │           │         └── Yetkili tamamlama
  │        │           └── Yetkili işleme alma
  │        └── Kullanıcı gönderme
  └── Kullanıcı taslak kaydetme
```

**Yetki Yapısı:**
- `destektalebi.oku`: Taleplerim, Yeni Talep, Talep Takip, Bilgi Bankası
- `destektalebi.yaz`: Talep Yönetimi, Tüm Talepler, Atama işlemleri

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/DestekTalebi.cs` (DestekDurum enum)
- `CRMFiloServis.Web/Components/Pages/DestekTalepleri/TalepGiris.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/DestekTalepleri/Taleplerim.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/DestekTalepleri/TalepTakip.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/DestekTalepleri/TalepYonetim.razor` (YENİ)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)
- `CRMFiloServis.Web/Services/StokService.cs` (girintileme düzeltmesi)

**Durum:** ✅ Tamamlandı

### Kayıt 100 - EBYS Gelen/Giden Evrak Yönetim Sistemi
**Talep:** EBYS modülüne gelen/giden evrak girişi, atama, işlem ve evrak takip sistemi eklenmesi.

**Yapılanlar:**
- **Entity Katmanı:**
  - `EbysEvrak.cs` oluşturuldu - Ana evrak entity (gelen/giden ortak)
  - `EbysEvrakKategori` - Evrak kategorileri
  - `EbysEvrakDosya` - Evrak ekleri/dosyaları
  - `EbysEvrakAtama` - Evrak atama kayıtları
  - `EbysEvrakHareket` - İşlem geçmişi/log
  - Enum'lar: `EvrakYonu`, `EvrakOncelik`, `EvrakGizlilik`, `GonderimYontemi`, `EbysEvrakDurum`, `AtamaDurum`, `EbysHareketTipi`

- **Servis Katmanı:**
  - `IEbysEvrakService.cs` interface oluşturuldu
  - `EbysEvrakService.cs` implementasyonu - CRUD, atama, dosya yükleme, durum değişikliği, istatistik metodları

- **Razor Sayfaları:**
  - `GelenEvraklar.razor` (`/ebys/gelen`) - Gelen evrak listesi, filtreleme, yeni evrak ekleme
  - `GidenEvraklar.razor` (`/ebys/giden`) - Giden evrak listesi, filtreleme, yeni evrak ekleme
  - `EvrakDetay.razor` (`/ebys/evrak/{Id:int}`) - Evrak detay, atama yapma, dosya yükleme, durum değiştirme
  - `EvrakTakip.razor` (`/ebys/takip`) - Kanban tarzı evrak takip paneli (Beklemede, İşlemde, Cevap Bekleyen)
  - `EvrakKategorileri.razor` (`/ebys/kategoriler`) - Kategori CRUD yönetimi

- **Veritabanı:**
  - `ApplicationDbContext.cs` - 5 yeni DbSet eklendi: `EbysEvraklar`, `EbysEvrakKategoriler`, `EbysEvrakDosyalar`, `EbysEvrakAtamalar`, `EbysEvrakHareketler`
  - OnModelCreating'de tüm entity konfigürasyonları eklendi

- **Program.cs:**
  - `IEbysEvrakService` / `EbysEvrakService` Scoped olarak kaydedildi

- **NavMenu.razor:**
  - EBYS bölümüne 5 yeni link eklendi: Gelen Evraklar, Giden Evraklar, Evrak Takip, Evrak Kategorileri

**Özellikler:**
- Gelen/Giden evrak ayrımı
- Otomatik evrak numaralandırma (GE-2025-00001, GI-2025-00001)
- Öncelik seviyeleri (Düşük, Normal, Yüksek, Acil)
- Gizlilik seviyeleri (Normal, Gizli, Çok Gizli)
- Cevap süresi takibi ve gecikme uyarıları
- Dosya yükleme (max 50MB)
- Kullanıcıya evrak atama
- Durum akışı: Taslak → Beklemede → İşleniyor → Atama Bekliyor → Cevap Bekliyor → Cevaplandı → Tamamlandı → Arşivlendi
- İşlem geçmişi/log kaydı
- Kategori bazlı organizasyon
- İstatistik dashboard'u

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/EbysEvrak.cs` (YENİ)
- `CRMFiloServis.Web/Services/Interfaces/IEbysEvrakService.cs` (YENİ)
- `CRMFiloServis.Web/Services/EbysEvrakService.cs` (YENİ)
- `CRMFiloServis.Web/Components/Pages/EBYS/GelenEvraklar.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/EBYS/GidenEvraklar.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/EBYS/EvrakDetay.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/EBYS/EvrakTakip.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/EBYS/EvrakKategorileri.razor` (YENİ)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 097 - Destek Talepleri E-posta Entegrasyonu
**Talep:** Destek talepleri modülüne e-posta bildirim entegrasyonu eklenmesi (ROADMAP Faz 2).

**Yapılanlar:**
- `EmailService.cs` → `IEmailService` interface'ine 4 yeni destek e-posta metodu eklendi:
  - `SendDestekYeniTalepEmailAsync` – yeni talep oluşturulduğunda müşteriye onay
  - `SendDestekYanitEmailAsync` – temsilci yanıtı eklendiğinde müşteriye bildirim
  - `SendDestekDurumEmailAsync` – durum değişikliğinde müşteriye bildirim
  - `SendDestekAtamaEmailAsync` – talep atandığında temsilciye bildirim
- Her metot için profesyonel HTML e-posta şablonu (`BuildDestekEmailBody` ortak builder) oluşturuldu.
- `DestekTalebiService.cs` → `IEmailService` inject edildi ve 5 kritik noktada e-posta tetikleme eklendi:
  - `CreateAsync` → yeni talep onay e-postası
  - `UpdateDurumAsync` → durum değişiklik bildirimi
  - `AtaAsync` → atama bildirimi
  - `KapatAsync` → kapatma bildirimi
  - `AddYanitAsync` → yanıt bildirimi (yalnızca temsilci yanıtı, dahili not hariç)
- `SendDestekEmailSafeAsync` yardımcı metodu ile fire-and-forget, hata-güvenli e-posta gönderimi sağlandı.
- E-posta göndermeden önce `EmailBildirimAktif` destek ayarı kontrol edilir; kapalıysa e-posta atlanır.
- `DestekAyarlar.razor` bilgi kartı güncellendi: entegrasyon aktif mesajı ve tetiklenen olay listesi eklendi.

**Durum:** ✅ Tamamlandı

### Kayıt 098 - Login Sayfası Lisans Süresi Kontrolü ve Aktivasyonu
**Talep:** Lisans süresi dolduğunda kullanıcının sisteme giriş yapamaması, lisans anahtarı girmesi istenmesi.

**Yapılanlar:**
- `Login.razor` içinde `OnInitializedAsync` başlangıcında lisans geçerlilik kontrolü eklendi.
- Lisans süresi dolmuşsa `lisansSuresiDoldu = true` yapılarak normal giriş formu yerine lisans süresi doldu kartı gösterilir hale getirildi.
- Lisans süresi doldu kartında makine kodu görüntüleme ve panoya kopyalama aksiyonu eklendi.
- Yeni lisans anahtarı giriş alanı ve `Lisansı Aktive Et` butonu eklendi.
- `GirisYapAsync` içinde kimlik doğrulama öncesi ek lisans kontrolü eklendi; süresi dolmuşsa giriş engellenir.
- `LisansAktiveEtAsync` metodu ile login sayfasından ayrılmadan lisans aktivasyonu yapılabilir hale getirildi.
- Aktivasyon başarılı olduğunda lisans kartı gizlenir ve normal giriş formu gösterilir.
- `MakineKoduKopyala` metodu ile `IJSRuntime` üzerinden clipboard API kullanımı eklendi.
- Lisans kartı için `.license-expired-card`, `.license-expired-icon`, `.license-detail-box`, `.license-machine-code`, `.license-key-input` CSS stilleri eklendi.
- `LisansService` cache davranışı doğrulandı; süresi dolmuş lisanslar cache'ten dönmediği onaylandı.

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Login.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 096 - Bütçe Analiz Hedef / Gerçekleşen Karşılaştırma
**Talep:** Yol haritasındaki açık başlıklardan `Bütçe Analiz - Hedef/Gerçekleşen karşılaştırma` adımına başlanması.

**Yapılanlar:**
- `BudgetAnaliz.razor` içine yeni `Hedef / Gerçekleşen Karşılaştırma` kartı eklendi.
- Karşılaştırma, seçili dönem için geçmiş dönem referanslı hedef mantığıyla görünür hale getirildi.
- Toplam hedef, gerçekleşen, sapma ve gerçekleşme oranı kartları eklendi.
- Kategori bazında hedef / gerçekleşen / sapma tablosu eklendi.
- Referans dönem etiketi ve sapma renkleri yardımcı metodlarla düzenlendi.

**Durum:** ✅ Tamamlandı

### Kayıt 095 - Destek Modülü İçin Playwright Smoke Test Altyapısı
**Talep:** Destek modülünün `Liste`, `Detay`, `Bilgi Bankası` ve `Ayarlar` ekran açılışlarını doğrulayacak gerçek smoke test altyapısının yeniden görünür hale getirilmesi.

**Yapılanlar:**
- `CRMFiloServis.Web/Tests/PlaywrightSmoke` altında çalıştırılabilir `CRMFiloServis.PlaywrightSmoke.csproj` oluşturuldu.
- `Program.cs` içinde Playwright tabanlı smoke akışı eklendi.
- Akış; anonim yönlendirme, login, destek listesi, destek detay, bilgi bankası ve ayarlar ekranlarını kontrol eder hale getirildi.
- Detay ekranı veri bağımlı olduğu için listede talep yoksa `skip` davranışı tanımlandı.
- `PlaywrightTestProcedures.md` yeni proje yolu ve destek modülü akışına göre güncellendi.
- Ana web projesinin test kaynaklarını derlememesi için `CRMFiloServis.Web.csproj` içine `Tests\**\*.cs` exclude kuralı eklendi.

**Durum:** ✅ Tamamlandı

### Kayıt 094 - Personel Özlük Checklist Eksik Evrak Görünürlüğü
**Talep:** Personel özlük checklist ekranında kullanıcı modal açmadan hangi evrakların eksik olduğunu daha görünür biçimde görebilsin.

**Yapılanlar:**
- `OzlukEvrakChecklist.razor` personel listesinde kişi satırına eksik evrak özeti eklendi."""""""""
- Zorunlu eksikler için ayrı kırmızı badge görünürlüğü eklendi.
- Detay modal üst kısmına `Eksik Evrak Özeti` uyarı alanı eklendi.
- Eksik evrak özeti zorunlu belgeleri önce gösterecek şekilde yardımcı metotlarla üretildi.
- Checklist export tarafında mevcut `Boş Personel Dosyası` çıktısı ekran üstüne görünür aksiyon olarak taşındı.
- `Excel İndir` aksiyonu `Tüm Checklist Excel` olarak netleştirildi.

**Durum:** ✅ Tamamlandı

### Kayıt 093 - DEVELOPMENT ve ROADMAP Sadeleştirme
**Talep:** Güncel durumu yansıtmayan eski açık maddelerin temizlenmesi ve sonraki adımların daha net hale getirilmesi.

**Yapılanlar:**
- `DEVELOPMENT.md` içindeki eski `aktif dikkat`, `yapılması gerekenler`, `kısa yol haritası` ve `güncel kısa durum` bölümleri güncel açık başlıklara indirgenerek sadeleştirildi.
- `ROADMAP.md` içindeki artık büyük ölçüde tamamlanmış `Hemen Başlanabilecek Öncelikli İşler` bölümü aktif kalan maddelere göre yeniden yazıldı.
- Destek modülü smoke test tarafında prosedür dokümanları bulunduğu, ancak repo içinde çalıştırılabilir Playwright smoke test kaynak dosyalarının görünmediği not edildi.

**Durum:** ✅ Tamamlandı

### Kayıt 092 - Bütçe Tarafında Cari Mahsup Belge No Görünürlüğü
**Talep:** Bütçe tarafındaki `Cari Mahsup` ödeme izinde hareket bağlantısı görünürken `Belge No` bilgisinin de kullanıcı tarafından görülebilmesi.

**Yapılanlar:**
- `BudgetOdeme` entity'sine UI gösterimi için `HareketBelgeNo` alanı eklendi.
- `BudgetService` içinde cari mahsup hareketinden gelen `BelgeNo` bilgisi ödeme satırına taşınır hale getirildi.
- `OdemeYonetimi.razor` ödeme listesinde açıklama alanı altında `Belge No` görünürlüğü eklendi.
- `BudgetAnaliz.razor` ödeme düzenleme modalındaki `Ödeme İzi` özetine `Belge No` alanı eklendi.

**Durum:** ✅ Tamamlandı

### Kayıt 091 - OdemeYonetimi Odeme Modalinin BudgetAnaliz ile Hizalanmasi
**Talep:** `OdemeYonetimi.razor` içindeki ödeme modalinin `BudgetAnaliz.razor` ile aynı `Cari Mahsup`, yön seçimi, hesap seçimi, muhasebe alanları ve ek masraf davranışına getirilmesi.

**Yapılanlar:**
- `OdemeYonetimi.razor` ödeme modalı `Cari Mahsup`, `Kredi Kartı`, `Hesap Mahsup`, `Kasa` ve `Banka` akışlarını kapsayacak şekilde genişletildi.
- `Cari Mahsup` için cari seçimi, hesap seçimi, işlem yönü ve muhasebe eşleştirme alanları eklendi.
- Hesap bakiyeleri ve aktif cariler ekrana taşındı; cari mahsup hesap seçiminde varsayılan muhasebe kodu ve kost merkezi otomatik doldurulur hale getirildi.
- Ek masraf kartı, net ödeme özeti, kaydetme loading durumu ve başarı mesajı `BudgetAnaliz` ile tutarlı hale getirildi.
- `OdemeYapAsync` validasyonları cari mahsup hesabı dahil olacak şekilde güçlendirildi.

**Durum:** ✅ Tamamlandı

### Kayıt 090 - Destek Modülü PostgreSQL Eksik Kolon Doğrulama Katmanı
**Talep:** Destek modülünde tablo var ama bazı kolonlar eksik kaldığında başlangıçta tekrar düşme riskinin azaltılması.

**Yapılanlar:**
- `DbInitializer` içine `EnsureDestekModuluColumnsAsync` eklendi.
- PostgreSQL için destek modülü tablolarındaki kritik kolonlar `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` ile tamamlanır hale getirildi.
- `DestekTalepleri`, `DestekKategorileri`, `DestekAyarlari`, `DestekHazirYanitlari`, `DestekBilgiBankasiMakaleleri` ve ilişkili destek tabloları için koruma eklendi.
- Güncel ve geriye dönük başlangıç akışlarında destek modülü kolon doğrulaması çalıştırılır hale getirildi.

**Durum:** ✅ Tamamlandı

### Kayıt 089 - Destek Ayarlar Ekranı Kontrollü Yükleme Hata Görünümü
**Talep:** Destek modülünde `DestekAyarlar` ekranının da PostgreSQL tablo/seed sorunlarında tamamen düşmeden kontrollü hata görünümü sunması.

**Yapılanlar:**
- `DestekAyarlar.razor` içine sayfa üstünde görünen uyarı alanı eklendi.
- Ayar yükleme akışında hata olması durumunda form alanları güvenli varsayılan değerlere döndürülür hale getirildi.
- Hata durumunda kullanıcıya `Tekrar Dene` aksiyonu sunuldu.
- İlk yükleme akışı ortak `SayfayiYenile` metodu altına toplandı.

**Durum:** ✅ Tamamlandı

### Kayıt 088 - Destek Talebi Detay Ekranı Kontrollü Yükleme Hata Görünümü
**Talep:** Destek modülünde `DestekTalebiDetay` ekranının da PostgreSQL tablo/seed sorunlarında tamamen düşmeden kontrollü hata görünümü sunması.

**Yapılanlar:**
- `DestekTalebiDetay.razor` içinde talep yükleme ve referans veri yükleme akışları ayrı hata yakalama ile güçlendirildi.
- Talep hiç yüklenemezse açıklayıcı uyarı ve `Tekrar Dene` aksiyonu gösterilir hale getirildi.
- Talep yüklenmiş fakat referans veriler eksik kalmışsa ekranın açılmaya devam etmesi ve üstte uyarı göstermesi sağlandı.
- İlk açılış akışı ortak `SayfayiYenile` metodu altında toplandı.

**Durum:** ✅ Tamamlandı

### Kayıt 087 - Destek Bilgi Bankası Kontrollü Yükleme Hata Görünümü
**Talep:** Destek modülünde `BilgiBankasi` ekranının da PostgreSQL tablo/seed sorunlarında tamamen düşmeden kontrollü hata görünümü sunması.

**Yapılanlar:**
- `BilgiBankasi.razor` içine sayfa üstünde görünen uyarı alanı eklendi.
- Kategori yükleme, makale listeleme ve özet istatistik yükleme akışlarında hatalar kontrollü şekilde yakalanır hale getirildi.
- Hata durumunda boş sonuç modeli ve sıfırlanmış özet değerleri ile ekranın açılmaya devam etmesi sağlandı.
- `Tekrar Dene` aksiyonu ve ortak `SayfayiYenile` akışı eklendi.

**Durum:** ✅ Tamamlandı

### Kayıt 086 - Destek Talebi Liste Ekranı Kontrollü Yükleme Hata Görünümü
**Talep:** Destek modülünde PostgreSQL tablo/seed sorunları sonrası liste ekranının ilk açılışta tamamen düşmemesi ve kullanıcıya kontrollü hata görünümü sunulması.

**Yapılanlar:**
- `DestekTalebiList.razor` içine sayfa üstünde görünen uyarı alanı eklendi.
- Referans veriler, talepler ve dashboard istatistikleri yüklenirken oluşan hatalar kontrollü şekilde yakalanır hale getirildi.
- Hata durumunda ekran boş/düşük state yerine kullanıcıya açıklayıcı mesaj ve `Tekrar Dene` aksiyonu gösterilir hale getirildi.
- İlk yükleme akışı ortak `SayfayiYenile` metodu altında toplandı.

**Durum:** ✅ Tamamlandı

### Kayıt 085 - Faz 1 BudgetAnaliz Ödenmiş Kayıtta Cari Mahsup İzi
**Talep:** Faz 1 bütçe + cari mahsup entegrasyonunda `BudgetAnaliz` ekranında da ödenmiş kaydın hareket izinin görünür hale getirilmesi.

**Yapılanlar:**
- `BudgetAnaliz.razor` ödeme düzenleme modalında `Ödeme İzi` özet alanı eklendi.
- Ödenmiş kayıt için `gerçek ödeme tarihi`, `ödenen tutar`, `hesap`, `iz`, `cari`, `yön` ve `hareket numarası` bilgileri görünür hale getirildi.
- Düzenleme akışında `BudgetOdeme` kopyasına cari mahsup izi alanları da taşındı.
- Seçilen hesap adını göstermek için yardımcı görünüm metodu eklendi.

**Durum:** ✅ Tamamlandı

### Kayıt 084 - Faz 1 Bütçe Ödenmiş Kayıtta Cari Mahsup İzi Görünürlüğü
**Talep:** Faz 1 bütçe + cari mahsup entegrasyonunda ödenmiş kayıtta cari mahsup izinin kullanıcı tarafından listede görülebilmesi.

**Yapılanlar:**
- `BudgetOdeme` entity'sine yalnızca UI gösterimi için `NotMapped` hareket iz alanları eklendi.
- `BudgetService.GetOdemelerAsync` içinde ilişkili `BankaKasaHareket` kaydı okunarak ödeme satırları `Cari Mahsup`, cari unvanı ve işlem yönü bilgisiyle zenginleştirildi.
- `OdemeYonetimi.razor` ödeme listesinde açıklama alanı altına `İz`, `Cari`, `Yön` ve `Hareket #` bilgileri görünür hale getirildi.

**Durum:** ✅ Tamamlandı

### Kayıt 083 - Faz 1 Bütçe Cari Mahsup Muhasebe Alanı Bağlantısı
**Talep:** Faz 1 bütçe + cari mahsup entegrasyonunda muhasebe eşleştirme alanlarının ekrandan servis katmanına gerçekten bağlanması.

**Yapılanlar:**
- `BudgetAnaliz.razor` içinde `Cari Mahsup` ödeme modalına `Muhasebe Hesap Kodu`, `Kost Merkezi` ve `Proje Kodu` alanları eklendi.
- Cari mahsup için seçilen hesaptan `VarsayilanMuhasebeKodu` ve `VarsayilanKostMerkezi` değerleri otomatik doldurulur hale getirildi.
- `Cari Mahsup` ekranındaki yinelenen tarih alanı temizlenerek modal akışı tutarlı hale getirildi.
- `BudgetService.OdemeYapAsync` içindeki `CariMahsupAsync` çağrısına muhasebe eşleştirme alanları aktarıldı.

**Durum:** ✅ Tamamlandı

### Kayıt 082 - Destek Modülü PostgreSQL Eksik Tablo Başlangıç Düzeltmesi
**Talep:** `Npgsql.PostgresException: 42P01 relation "DestekKategorileri" does not exist` hatasının giderilmesi.

**Yapılanlar:**
- `DbInitializer.InitializeAsync(context, configuration)` içine destek modülü tablolarını kontrol eden başlangıç adımı eklendi.
- PostgreSQL için `DestekDepartmanlari`, `DestekKategorileri`, `DestekTalepleri`, `DestekTalebiYanitlari`, `DestekTalebiEkleri`, `DestekTalebiAktiviteleri`, `DestekTalebiIliskileri`, `DestekDepartmanUyeleri`, `DestekHazirYanitlari`, `DestekSlaListesi`, `DestekAyarlari` ve `DestekBilgiBankasiMakaleleri` tablolarını `IF NOT EXISTS` ile oluşturan güvenli altyapı eklendi.
- Güncel başlangıç akışında destek modülü seed verileri yeniden devreye alındı.
- Geriye dönük `InitializeAsync(context)` akışı da aynı destek tablo kontrolünü çalıştıracak şekilde güncellendi.

**Durum:** ✅ Tamamlandı

### Kayıt 081 - Faz 1 Bütçe + Cari Mahsup Entegrasyonu Ekran Doğrulama
**Talep:** Faz 1 kapsamında bütçe ödeme ekranındaki `Cari Mahsup` akışının kullanıcı tarafından görünür ve doğrulanabilir hale getirilmesi.

**Yapılanlar:**
- `BudgetAnaliz.razor` ödeme modalında `Cari Mahsup` seçildiğinde artık işlem yapılacak hesap seçimi görünür hale getirildi.
- `Cari Mahsup` için `Ödeme (Hesap → Cari)` ve `Tahsilat (Cari → Hesap)` yön seçimi eklendi.
- Seçilen yönün ne anlama geldiğini açıklayan bilgi kutusu eklendi.
- Ekran tarafında `Cari Mahsup` için hesap seçimi validasyonu tamamlandı.
- Kayıt sonrası başarı mesajı, cari mahsup yönünü de içerecek şekilde daha görünür hale getirildi.

**Durum:** ✅ Tamamlandı

### Kayıt 080 - Faz 1 Mahsup Ekranı Muhasebe Fişi Görünürlüğü
**Talep:** Faz 1 mahsup ekranı doğrulamasında mahsup hareketlerinin ilişkili muhasebe fişi ve iptal fişi bağlantılarının kullanıcı tarafından görülebilmesi.

**Yapılanlar:**
- `BankaKasaHareket` entity'sine liste gösterimi için `NotMapped` muhasebe fişi alanları eklendi.
- `BankaKasaHareketService.GetMahsupHareketleriAsync` içinde ilişkili muhasebe fişi numarası, durum bilgisi ve varsa iptal fişi bilgisi doldurulur hale getirildi.
- Hesap transferlerinde tek fişin aynı mahsup grubu içindeki karşı harekete de görünür olması sağlandı.
- `MahsupIslemleri.razor` listesinde `Fiş`, `Durum` ve `İptal Fişi` bilgileri görünür hale getirildi.

**Durum:** ✅ Tamamlandı

### Kayıt 079 - Faz 1 Mahsup Ekranı Doğrulama Liste Görünürlüğü
**Talep:** Faz 1 mahsup ekranı doğrulamasında kaydedilen muhasebe alanlarının kullanıcı tarafından listede de doğrulanabilmesi.

**Yapılanlar:**
- `MahsupIslemleri.razor` işlem listesinde `Belge No` görünürlüğü eklendi.
- Kaydedilen `Muhasebe Hesap Kodu`, `Kost Merkezi` ve `Proje Kodu` bilgileri açıklama alanı altında gösterilir hale getirildi.
- Böylece mahsup ekranındaki veri girişinin sadece kayıt anında değil, liste üzerinden de hızlı doğrulanması sağlandı.

**Durum:** ✅ Tamamlandı

### Kayıt 078 - Faz 1 Mahsup Ekranı Doğrulama İlk Uçtan Uca Düzeltme
**Talep:** Faz 1 kapsamında mahsup ekranındaki muhasebe alanlarının gerçekten veritabanına yazıldığının doğrulanması ve eksik bağların tamamlanması.

**Yapılanlar:**
- `MahsupIslemleri.razor` içinde transfer ve cari mahsup ekranlarında girilen `Belge No`, `Muhasebe Hesap Kodu`, `Kost Merkezi` ve `Proje Kodu` alanları servis çağrılarına bağlandı.
- `IBankaKasaHareketService` içine mahsup metodları için opsiyonel muhasebe alanı parametreleri eklendi.
- `BankaKasaHareketService` içinde hesap transferi ve cari mahsup kayıtlarında bu alanlar doğrudan `BankaKasaHareket` kaydına yazılır hale getirildi.
- Kullanıcı alan bırakırsa hesap üzerindeki varsayılan muhasebe kodu ve varsayılan kost merkezi değerleri fallback olarak kullanılacak şekilde akış genişletildi.

**Durum:** ✅ Tamamlandı

### Kayıt 077 - Faz 1 Login Stabilizasyonu Erişim Reddedildi Görünümü
**Talep:** Faz 1 login/yetki zincirinde giriş yapmış ama yetkisi olmayan kullanıcı için daha anlaşılır bir deneyim sağlanması.

**Yapılanlar:**
- `Routes.razor` içindeki authenticated ancak yetkisiz kullanıcı akışı düz metin yerine kart yapısına taşındı.
- `Erişim Reddedildi` başlığı ve açıklayıcı metin eklendi.
- Kullanıcıya `Dashboard` ve `Giriş Ekranı` aksiyonları eklendi.
- Yetkisiz deneyim login yönlendirme akışından ayrıştırılarak daha anlaşılır hale getirildi.

**Durum:** ✅ Tamamlandı

### Kayıt 076 - Faz 1 Login Stabilizasyonu Remember Me Dayanıklılığı
**Talep:** Faz 1 login elden geçirmesinde `beni hatırla` davranışının bozuk/eski kayıtlar karşısında daha dayanıklı hale getirilmesi.

**Yapılanlar:**
- `Login.razor` içinde `remember me` için yeni saklama modeli eklendi.
- Eski `string` formatındaki saklama kaydıyla geriye uyumluluk korunarak yeni formata otomatik geçiş eklendi.
- Boş / bozuk `remember me` kayıtları otomatik temizlenir hale getirildi.
- Hatırlama yükleme ve kaydetme akışı yardımcı metodlara ayrılarak daha okunabilir hale getirildi.

**Durum:** ✅ Tamamlandı

### Kayıt 075 - Faz 1 Login Stabilizasyonu Yetkisiz Yönlendirme Temizliği
**Talep:** Faz 1 login elden geçirmesinde yetkisiz kullanıcı yönlendirmesinin render sırasında doğrudan navigation çağrısı yapmadan güvenli hale getirilmesi.

**Yapılanlar:**
- `MainLayout.razor` içindeki `NotAuthorized` bloğunda yer alan doğrudan `NavigateTo` çağrısı kaldırıldı.
- Yetkisiz kullanıcı akışı mevcut `RedirectToLogin` komponenti üzerinden çalışır hale getirildi.
- Render sırasında yönlendirme yan etkisi azaltılarak login stabilizasyonu daha güvenli hale getirildi.

**Durum:** ✅ Tamamlandı

### Kayıt 074 - Faz 1 Login Stabilizasyonu Logout Akışı Düzeltmesi
**Talep:** Faz 1 login elden geçirmesinde çıkış işleminin yalnızca sayfa yönlendirmesi değil, gerçek oturum kapatma ile çalışması.

**Yapılanlar:**
- `MainLayout.razor` içindeki çıkış butonları doğrudan `/login` JavaScript yönlendirmesi yerine ortak `CikisYapAsync` metoduna bağlandı.
- `KullaniciService.CikisYapAsync` çağrısı ile auth provider temizlenir hale getirildi.
- Çıkış sonrası kullanıcı kontrollü biçimde login ekranına yönlendirilir hale getirildi.
- Lisans geçersiz ekranındaki çıkış aksiyonu da aynı oturum kapatma akışına bağlandı.

**Durum:** ✅ Tamamlandı

### Kayıt 073 - Faz 1 Login Stabilizasyonu İlk Sağlamlaştırma
**Talep:** Faz 1 başlangıcında login akışının yeniden elden geçirilmesi ve kırılgan noktalarının azaltılması.

**Yapılanlar:**
- `Login.razor` içinde oturum açık kullanıcı login ekranına gelirse otomatik olarak `dashboard` yönlendirmesi eklendi.
- Çift tıklama / art arda `Enter` ile oluşabilecek tekrar giriş denemelerine karşı `isLoading` koruması eklendi.
- Başarılı giriş sonrası parola alanı temizlenir hale getirildi.
- `KullaniciService` içinde kullanıcı adı eşleşmesi `trim + büyük/küçük harf toleranslı` hale getirildi.
- Giriş ve kullanıcı adı sorgularında `IsDeleted` kayıtları dışlanır hale getirildi.
- `RedirectToLogin.razor` içine `returnUrl` desteği eklendi.
- Yetkisiz sayfadan login ekranına düşen kullanıcı için başarılı giriş sonrası güvenli geri yönlendirme eklendi.

**Durum:** ✅ Tamamlandı

### Kayıt 072 - EBYS Belge Merkezinde Arama İpuçları ve Filtre Temizleme
**Talep:** EBYS belge merkezindeki metadata aramanın kullanıcı tarafından daha görünür ve pratik kullanılabilir hale getirilmesi.

**Yapılanlar:**
- `BelgeMerkezi.razor` filtre alanına `Temizle` aksiyonu eklendi.
- Aktif filtre varsa çalışan `FiltreVarMi` kontrolü eklendi.
- Ekrana örnek arama kelimeleri içeren ipucu kutusu eklendi.
- Tek tıkla arama, kaynak, kategori, risk ve dosya filtresini sıfırlayan akış eklendi.

**Durum:** ✅ Tamamlandı

### Kayıt 071 - EBYS Belge Merkezinde Metadata Arama Genişletmesi
**Talep:** EBYS belge merkezindeki aramanın günlük kullanımda daha fazla metadata alanını kapsaması.

**Yapılanlar:**
- `EbysService` içinde belge araması servis tarafında tek yardımcı yapı altında genişletildi.
- Arama kapsamına `kaynak`, `durum`, `risk durumu`, `dosya var/yok`, `dosya adı`, `belge tarihi` ve `bitiş tarihi` metinleri dahil edildi.
- Türkçe karakterlerden bağımsız normalize arama desteği eklendi.
- Metadata arama altyapısı güçlendirilirken içerik bazlı arama sonraki adım için açık bırakıldı.

**Durum:** ✅ Tamamlandı

### Kayıt 070 - Personel Özlük Dosyasında Detay Arama ve Eksik Evrak Filtresi
**Talep:** Personel özlük dosyası ekranında çok sayıda belge arasında daha hızlı çalışma imkanı sağlamak.

**Yapılanlar:**
- `OzlukEvrakChecklist.razor` detay modalına evrak içi arama alanı eklendi.
- `Sadece eksikler` filtresi eklendi.
- Arama; evrak adı, kategori adı ve açıklama alanlarında çalışır hale getirildi.
- Detay modal kapatıldığında arama ve eksik filtre state temizliği eklendi.

**Durum:** ✅ Tamamlandı

### Kayıt 069 - İş Sözleşmesi Yönetimi İçin Hızlı Erişim ve Tarih Bağlantısı
**Talep:** EBYS personel dosyalarında `İş sözleşmesi` evrakının ayrı ve yönetilebilir hale getirilmesi.

**Yapılanlar:**
- `OzlukEvrakChecklist.razor` detay modalına `İş Sözleşmesi` hızlı filtresi eklendi.
- `İş Sözleşmesi` evrakı tarih yönetimi akışına dahil edildi.
- Sözleşme tarihi alanı mevcut `IseBaslamaTarihi` değeriyle eşlenir hale getirildi.
- Tarih değiştirildiğinde ilgili personel kaydına anında kalıcı yazım eklendi.

**Durum:** ✅ Tamamlandı

### Kayıt 068 - Özlük Evraklarında Geçerlilik Tarihi Destekli Yükleme
**Talep:** `Sağlık raporu` ve şoför belge yüklemelerinde dosya ile birlikte geçerlilik tarihinin de yönetilmesi.

**Yapılanlar:**
- `OzlukEvrakChecklist.razor` içinde desteklenen evrak satırlarına `geçerlilik tarihi` alanı eklendi.
- `Ehliyet`, `SRC`, `Psikoteknik` ve `Sağlık Raporu` için mevcut personel tarih alanları modal açılışında ekrana taşındı.
- Dosya yükleme sonrası seçilmiş geçerlilik tarihi varsa ilgili `Sofor` kayıt alanına otomatik yazım eklendi.
- Geçerlilik tarihi değiştirildiğinde ilgili personel belge tarihi anında kalıcı kaydedilir hale getirildi.
- Detay modal kapanırken tarih state temizliği eklendi.

**Durum:** ✅ Tamamlandı

### Kayıt 067 - Personel Özlük Evraklarında Satır Bazlı Dosya Yükleme
**Talep:** EBYS personel dosyaları kapsamında `ehliyet / diploma / sertifika` gibi özlük belgelerinin doğrudan yüklenebilir hale getirilmesi.

**Yapılanlar:**
- `OzlukEvrakChecklist.razor` içine `InputFile` tabanlı satır bazlı dosya yükleme alanı eklendi.
- Mevcut dosyası olan evrak satırlarına indirme aksiyonu eklendi.
- Yüklenen dosyalar `ISecureFileService` ile şifreli olarak saklanır hale getirildi.
- Aynı evrak için eski dosya varsa yükleme öncesi güvenli şekilde silinmesi eklendi.
- Yükleme sonrası evrak durumu ve liste anlık yenilenir hale getirildi.
- Detay modalına `Ehliyet / Diploma / Sertifika / Sağlık Raporu` için hızlı filtre butonları eklendi.
- Hedef belge türlerine doğrudan erişim kolaylaştırılarak yükleme akışı pratik hale getirildi.

**Durum:** ✅ Tamamlandı

### Kayıt 066 - Belge Uyarıları Filtre Tutarlılığı ve Filtreli Export
**Talep:** Belge uyarıları ekranındaki filtrelerin görünüm ve export akışlarıyla tutarlı çalışmasının tamamlanması.

**Yapılanlar:**
- `BelgeUyarilari.razor` içinde filtreler sadece birleşik tabloyu değil, personel ve araç tablolarını da etkiler hale getirildi.
- Özet kartları aktif filtre sonucuna göre hesaplanır hale getirildi.
- Excel ve PDF export işlemleri artık tüm kayıtlar yerine görünür filtrelenmiş listeyi dışa aktarır hale getirildi.
- Filtre temizleme aksiyonu eklendi.
- Ortak filtreleme mantığı tek yardımcı metod altında toplandı.

**Durum:** ✅ Tamamlandı

### Kayıt 065 - Belge Uyarıları Birleşik Liste ve Filtreleme
**Talep:** Belge uyarıları ekranının günlük kullanımda daha hızlı aksiyon alınabilir hale getirilmesi.

**Yapılanlar:**
- `BelgeUyari` modeline kaynak bilgisi eklendi.
- `BelgeUyariService` içinde tüm uyarı kayıtları `Personel` / `Araç` kaynağı ile etiketlendi.
- `BelgeUyarilari.razor` ekranına birleşik `Tüm Uyarılar` tablosu eklendi.
- Ekrana kaynak filtresi, seviye filtresi ve serbest metin arama eklendi.
- Birleşik listeden ilgili personel / araç detay ekranına hızlı geçiş aksiyonu eklendi.

**Durum:** 🔄 Devam Ediyor

### Kayıt 064 - EBYS Belge Bitiş Tarihi Uyarıları Genişletmesi
**Talep:** Yol haritasındaki `Belge bitiş tarihi uyarıları` başlığı kapsamında mevcut uyarı ekranının EBYS belge akışına daha yakın hale getirilmesi.

**Yapılanlar:**
- `IBelgeUyariService` modellerine satır bazlı detay URL bilgisi eklendi.
- Personel belge uyarılarındaki bağlantılar çalışan `personel` düzenleme sayfasına yönlendirilecek şekilde düzeltildi.
- Araç belge uyarılarındaki bağlantılar araç evrak yönetimi sayfasına yönlendirilecek şekilde düzeltildi.
- `BelgeUyariService` içinde sadece sabit araç alanları değil, `AracEvraklari` tablosundaki bitiş tarihli diğer evrak kategorileri de uyarı kapsamına alındı.
- `BelgeUyarilari.razor` ekranında diğer araç evrak uyarıları liste ve özet kartlarına dahil edildi.

**Durum:** 🔄 Devam Ediyor

### Kayıt 063 - EBYS Kategori Özeti ve Hızlı Filtreleme
**Talep:** Yol haritasındaki EBYS başlığında belge kategorilerini daha görünür ve yönetilebilir hale getirmek.

**Yapılanlar:**
- `IEbysService` ve `EbysService` içine kategori bazlı özet modeli ve özet listeleme metodu eklendi.
- `BelgeMerkezi.razor` içine kategori özeti kartları eklendi.
- Kategori kartlarından tek tıkla hızlı filtreleme desteği eklendi.
- Aktif kategori filtresini temizleme aksiyonu eklendi.
- Liste yenilendiğinde kategori listesi ve özetler de güncellenir hale getirildi.

**Durum:** 🔄 Devam Ediyor

### Kayıt 062 - EBYS Merkez Ekrandan Yeni Belge Kaydı Oluşturma
**Talep:** Yol haritasındaki EBYS başlığında belge kayıt oluşturma akışını merkez ekran üzerinden ilerletmek.

**Yapılanlar:**
- `IEbysService` ve `EbysService` içine yeni belge oluşturma seçenekleri ve kayıt oluşturma metotları eklendi.
- Personel için aktif personel + aktif evrak tanımı seçilerek merkez ekrandan yeni özlük evrak kaydı açılabilir hale getirildi.
- Araç için araç, kategori, belge adı, durum ve tarih bilgileriyle merkez ekrandan yeni araç evrakı oluşturulabilir hale getirildi.
- Oluşturma modalında opsiyonel ilk dosya yükleme desteği eklendi.
- `BelgeMerkezi.razor` içine `Yeni Belge` butonu ve ortak oluşturma modalı eklendi.

**Durum:** 🔄 Devam Ediyor

### Kayıt 061 - EBYS Merkez Ekrandan Dosya Yükleme
**Talep:** Yol haritasındaki EBYS başlığında dosya yükleme/indirme akışını merkez ekran üzerinden ilerletmek.

**Yapılanlar:**
- `IEbysService` ve `EbysService` içine ortak belge dosyası yükleme metodu eklendi.
- Personel belgeleri için güvenli dosya saklama servisi kullanılarak merkez ekrandan dosya yükleme desteği eklendi.
- Araç belgeleri için mevcut `IAracService.UploadEvrakDosyaAsync` akışı EBYS merkez ekrana bağlandı.
- `BelgeMerkezi.razor` içine `Yükle` aksiyonu ve ortak dosya yükleme modalı eklendi.
- Yükleme sonrası liste yenilenir ve belge dosya durumu merkez ekranda güncellenir hale getirildi.

**Durum:** 🔄 Devam Ediyor

### Kayıt 060 - EBYS Merkez Ekrandan Belge Metadata Düzenleme
**Talep:** Yol haritasındaki EBYS başlığında belge yönetimini bir adım daha ilerletmek.

**Yapılanlar:**
- `IPersonelOzlukService` ve `PersonelOzlukService` içine kayıt bazlı personel özlük evrakı getirme ve güncelleme metotları eklendi.
- `IEbysService` ve `EbysService` içine ortak belge düzenleme modeli ve metadata güncelleme akışı eklendi.
- Personel belgeleri için merkez ekrandan `tamamlandı`, `tamamlanma tarihi` ve `açıklama` düzenlenebilir hale getirildi.
- Araç belgeleri için merkez ekrandan `belge adı`, `kategori`, `durum`, `başlangıç/bitiş tarihi` ve `açıklama` düzenlenebilir hale getirildi.
- `BelgeMerkezi.razor` içine `Düzenle` aksiyonu ve kaynak tipine göre çalışan ortak düzenleme modalı eklendi.

**Durum:** 🔄 Devam Ediyor

### Kayıt 059 - Resmi Raporlar İkinci Eksik Adım: Yıllık İzin Takip Raporu
**Talep:** Yol haritasındaki resmi raporlar başlığında eksik kalan raporların tamamlanmasına devam edilmesi.

**Yapılanlar:**
- `YillikIzinTakipRaporu.razor` oluşturuldu.
- Mevcut `IPersonelMaasIzinService.GetIzinRaporuAsync` altyapısı kullanılarak resmi rapor görünümü eklendi.
- Ekranda yıl seçimi, personel arama, kalan izin durum filtresi, özet kartlar, izin tipi özeti ve personel bazlı izin tablosu eklendi.
- Personel bazlı izin geçmişi detay modalı eklendi.
- Excel export eklendi.
- `ResmiRaporlar.razor` içine yeni rapora yönlendiren kart eklendi.

**Durum:** 🔄 Devam Ediyor

### Kayıt 058 - Resmi Raporlar İlk Eksik Adım: İşe Giriş / Çıkış Bildirge
**Talep:** Yol haritasındaki resmi raporlar başlığından başlanabilir bir adım seçilerek geliştirmeye devam edilmesi.

**Yapılanlar:**
- `IseGirisCikisRaporModels.cs` oluşturuldu.
- `IRaporService` ve `RaporService` içine işe giriş / çıkış bildirge raporu metotları eklendi.
- `Sofor` kayıtlarındaki `IseBaslamaTarihi`, `IstenAyrilmaTarihi` ve `SgkCikisTarihi` alanlarından resmi rapor satırları üretilir hale getirildi.
- `IseGirisCikisBildirge.razor` oluşturuldu.
- Ekranda tarih aralığı, kayıt tipi ve görev filtresi, özet kartlar, tablo görünümü, yazdırma ve Excel indirme eklendi.
- `ResmiRaporlar.razor` içine yeni sayfaya yönlendiren kart eklendi.

**Durum:** 🔄 Devam Ediyor

### Kayıt 057 - EBYS İlk Merkezi Ekran
**Talep:** Yol haritasındaki EBYS başlığı için başlanabilir bir adım seçilerek geliştirmeye devam edilmesi.

**Yapılanlar:**
- `IEbysService` ve `EbysService` eklendi.
- Mevcut `PersonelOzlukEvrak` ve `AracEvrak` kayıtları tek merkezde toplanarak sorgulanabilir hale getirildi.
- `EBYS/BelgeMerkezi.razor` oluşturuldu.
- Ekranda arama, kaynak filtreleme, sadece dosyalı kayıt filtresi, özet kartlar ve indirme aksiyonu eklendi.
- Personel güvenli dosyaları `ISecureFileService`, araç evrak dosyaları mevcut araç servis akışı üzerinden indirilebilir hale getirildi.
- Sol menüye `EBYS Belge Merkezi` bağlantısı eklendi.
- `EBYS Belge Merkezi` ekranına kategori filtresi ve risk filtresi eklendi.
- Araç evrakları için `Yaklaşan`, `Süresi Dolmuş`, `Dosya Eksik` risk hesaplaması eklendi.
- Belge satırlarına detay görüntüleme akışı eklendi; belge detay modalında metadata, risk, dosya ve kaynak bağlantısı gösterilir hale getirildi.
- Kategori listesi filtre sonucuna göre daralmayacak şekilde servis tarafında ayrı kaynak listeleme ile sabitlendi.

**Durum:** 🔄 Devam Ediyor

### Kayıt 056 - Araç Kartı Açılış Hatası İlk Stabilizasyon
**Talep:** Araç kartı açılırken oluşan hatanın incelenmesi ve başlanması. Ayrıca destek talepleri modülünün güncel durumunun kontrol edilmesi.

**Yapılanlar:**
- `BilgiBankasi.razor` içindeki yanlış `IDestekTalebiService` namespace kullanımı düzeltildi, destek talepleri modülündeki build hatası giderildi.
- `AracForm.razor` açılış akışı sertleştirildi:
  - cari listesi yüklenemezse sayfa tamamen düşmek yerine uyarı gösteriyor,
  - araç kaydı yüklenirken hata oluşursa toast ile hata gösterilip listeye dönülüyor.
- `AracForm.razor` içinde araç kartı açılırken gereksiz tüm cari listesi yerine yalnızca tedarikçi / müşteri+tedarikçi cariler yüklenir hale getirildi.
- `CariService.FillMuhasebeBilgisiAsync` içinde ünvan-hesap adı eşleştirmesi null/boş değerler için daha güvenli hale getirildi.
- `DestekTalepleri/BilgiBankasi.razor` geçici örnek veri modelinden çıkarıldı ve gerçek `IDestekTalebiService` bilgi bankası metodlarına bağlandı.
- `DestekTalepleri/DestekAyarlar.razor` oluşturuldu; NavMenu'deki kırık rota için çalışan destek ayarları sayfası eklendi.
- Destek modülünde `Destek Ayarları` sol menüden kaldırıldı; ilgili erişimler destek sayfalarının sağ üst aksiyon alanına taşındı.
- `DestekTalebiList.razor` yeni talep formundaki cari seçimi gerçek cari verisine bağlandı ve seçilen cari müşteri bilgilerini otomatik doldurur hale getirildi.
- `AIAsistan.razor` ve `OllamaAIChatService` içinde 404/model bulunamadı senaryoları iyileştirildi; yüklü model yoksa daha yönlendirici hata mesajı gösteriliyor ve uygun model otomatik seçiliyor.
- `Cari` / `CariHatirlatma` ilişkisi için navigation ve `ApplicationDbContext` konfigürasyonu eklendi; `Model.Validation[10622]` query filter uyarısı giderildi.

**Durum:** 🔄 Devam Ediyor

### Kayıt 055 - Destek Talepleri Modülü (osTicket Benzeri)
**Talep:** Müşteri destek talepleri yönetimi için kapsamlı bir biletleme sistemi. Departman, kategori, öncelik, SLA, hazır yanıtlar, dosya ekleri, aktivite takibi, performans raporlaması özellikleri.

**Yapılanlar:**

#### A) Entity'ler (`CRMFiloServis.Shared/Entities/DestekTalebi.cs`)
- `DestekTalebi`: Ana talep entity (TalepNo otomatik, Konu, Aciklama, Oncelik, Durum, SLA, Müşteri bilgileri, Etiketler)
- `DestekTalebiYanit`: Konuşma yanıtları (Icerik, DahiliNot flag, MusteriYaniti flag, HazirYanitId bağlantısı)
- `DestekTalebiEk`: Dosya ekleri (OrijinalDosyaAdi, DosyaYolu, MimeTipi, DosyaBoyutu)
- `DestekTalebiAktivite`: Aktivite geçmişi (AktiviteTuru enum, EskiDeger/YeniDeger)
- `DestekDepartman`: Departman tanımları (Ad, Email, Simge, Aktif, VarsayilanSla)
- `DestekKategori`: Kategori tanımları (Ad, DepartmanId, UstKategoriId, Renk, Simge, VarsayilanSla)
- `DestekHazirYanit`: Hazır yanıt şablonları (Ad, Icerik, DepartmanId, KategoriId, KullanimSayisi)
- `DestekSla`: SLA tanımları (Ad, Oncelik, YanitSuresiSaat, CozumSuresiSaat)
- `DestekAyar`: Sistem ayarları (Anahtar, Deger, Grup, Aciklama)
- **Enum'lar**: DestekDurum (Yeni, Acik, Beklemede, YanitBekleniyor, Cozuldu, Kapali), DestekOncelik (Dusuk, Normal, Yuksek, Acil, Kritik), DestekKaynak (Web, Email, Telefon, Canlı Destek, Sosyal Medya, API), YanitTuru (Personel, Musteri, Sistem), AktiviteTuru (12 farklı aktivite tipi)

#### B) Service Interface (`CRMFiloServis.Web/Services/Interfaces/IDestekTalebiService.cs`)
- 50+ metod tanımı
- **Talep CRUD**: CreateAsync, UpdateAsync, DeleteAsync, GetByIdAsync, GetByIdWithDetailsAsync, GetAllAsync, GetPagedAsync
- **Filtreleme**: GetByDurumAsync, GetByOncelikAsync, GetByDepartmanAsync, GetByKullaniciAsync, AramaYapAsync
- **Durum İşlemleri**: UpdateDurumAsync, UpdateOncelikAsync, AtaAsync, TransferEtAsync, BirleştirAsync, YenidenAcAsync
- **Yanıt İşlemleri**: AddYanitAsync, UpdateYanitAsync, DeleteYanitAsync, GetYanitlarAsync
- **Dosya İşlemleri**: AddEkAsync, DeleteEkAsync, GetEkAsync, DosyaIndirAsync
- **Departman/Kategori CRUD**: 12 metod
- **Hazır Yanıt CRUD**: 6 metod
- **SLA Yönetimi**: GetSlaListesiAsync, GetSlaByIdAsync, GetSlaByOncelikAsync, CreateSlaAsync, UpdateSlaAsync, DeleteSlaAsync, CheckAndUpdateSlaViolationsAsync
- **Dashboard ve Raporlama**: GetDashboardStatsAsync, GetRaporStatsAsync, GetPersonelPerformansRaporuAsync
- **Ayarlar**: GetAyarAsync, SetAyarAsync, GetAyarlarByGrupAsync
- **Model Sınıfları** (interface içinde): DestekDashboardStats (13 property), DestekRaporStats (10 property + 2 liste), GunlukTalepTrend, KategoriTalepStats, DestekPerformansRapor (9 property), DestekTalebiFilterParams (13 filtre alanı)

#### C) Service Implementation (`CRMFiloServis.Web/Services/DestekTalebiService.cs`)
- ~1565 satır tam implementasyon
- IDbContextFactory pattern ile context yönetimi
- **Talep Numarası Üretimi**: `GenerateTalepNoAsync()` - YYYY-MMNNNN formatında unique numara
- **SLA Hesaplama**: Önceliğe göre SLA süresi belirleme, SlaBitisTarihi otomatik set
- **Dosya Yükleme**: IWebHostEnvironment ile `wwwroot/uploads/destek/{talepId}/` klasör yapısı
- **Aktivite Loglama**: LogAktiviteInternalAsync ile tüm işlemlerin kaydı
- **Dashboard Stats**: Toplam/Açık/Bugün/SLA aşımı sayıları, Öncelik/Durum/Departman/Kaynak dağılımları, Ortalama çözüm süresi, Memnuniyet puanı
- **Rapor Stats**: Tarih aralığı filtreleme, Günlük trend hesaplama, Kategori bazlı istatistikler
- **Personel Performans**: Kullanıcı bazlı atanan/çözülen talep, SLA aşım, ortalama yanıt/çözüm süreleri, yanıt sayısı

#### D) DbContext Güncellemesi (`CRMFiloServis.Web/Data/ApplicationDbContext.cs`)
- 9 yeni DbSet eklendi: DestekTalepleri, DestekTalebiYanitlari, DestekTalebiEkleri, DestekTalebiAktiviteleri, DestekDepartmanlari, DestekKategorileri, DestekHazirYanitlari, DestekSlaListesi, DestekAyarlari
- OnModelCreating'de tam konfigürasyon: Index'ler (TalepNo unique, Durum+Oncelik composite), relationship'ler, enum conversion

#### E) Seed Data (`CRMFiloServis.Web/Data/DbInitializer.cs`)
- `EnsureDestekModuluSeedDataAsync()` metodu
- 3 varsayılan departman: Teknik Destek, Satış, Muhasebe
- 6 varsayılan kategori: Yazılım Hatası, Donanım, Ağ/Bağlantı, Kurulum, Soru, Şikayet
- 4 SLA tanımı: Kritik (2s yanıt/8s çözüm), Acil (4/24), Yüksek (8/48), Normal (24/72)
- 5 hazır yanıt şablonu

#### F) Razor Sayfaları

**DestekTalebiList.razor** (`/destek-talepleri`):
- Dashboard kartları: Toplam, Açık, Bugün Açılan, Bugün Kapanan, SLA Aşımı, Ort. Çözüm Süresi
- Gelişmiş filtre paneli: Arama, Durum, Öncelik, Departman, Atanan, Sadece Açık, SLA Aşımı
- Tablo görünümü: Talep No (link), Konu (etiketler), Müşteri, Departman, Öncelik badge, Durum badge, Atanan, SLA durumu (kalan süre), Son Aktivite (relative time)
- Yeni Talep Modal: Müşteri bilgileri, Departman, Kategori, Öncelik, Kaynak, Konu, Açıklama, Etiketler
- Atama Modal: Kullanıcı seçimi
- Durum Değiştir Modal: Durum seçimi
- Sayfalama: Önceki/Sonraki, sayfa numaraları
- Helper metodlar: GetDurumText, GetDurumBadgeClass, GetOncelikText, GetOncelikBadgeClass, GetRelativeTime

**DestekTalebiDetay.razor** (`/destek-talepleri/{TalepId:int}`):
- Üst bar: Geri butonu, Talep No, Durum/Öncelik/SLA badge'leri, Çözüldü/Kapat/Yeniden Aç butonları, İşlemler dropdown
- Sol panel (col-md-8):
  - Talep detay kartı: Konu, Açıklama, Etiketler, Oluşturma tarihi
  - Dosya ekleri kartı: İkon, dosya adı, boyut
  - Konuşma kartı: Yanıt listesi (müşteri/personel/dahili not renklendirmesi), Yanıt Ekle butonu
  - Aktivite geçmişi kartı: AktiviteTuru ikonu, açıklama, tarih, kullanıcı
- Sağ panel (col-md-4):
  - Müşteri bilgileri: Ad, Email, Telefon, Cari link
  - Talep bilgileri: Departman, Kategori, Kaynak, Oluşturma/Kapatılma tarihi, Çözüm süresi
  - Atama kartı: Atanan kullanıcı, düzenleme butonu
  - SLA kartı: Süre, Bitiş tarihi, Durum (kalan süre/aşıldı)
  - İlk yanıt süresi kartı
  - Dahili notlar kartı
- Modallar: Yanıt Ekle (hazır yanıt seçimi, dahili not, müşteri adına), Atama, Öncelik Değiştir, Departman Transfer
- Helper metodlar: Tüm badge/ikon/format metodları

#### G) NavMenu Güncellemesi
- "Destek Talebi" ana başlık (bi-ticket-detailed)
- Alt linkler: Tüm Talepler, Açık Talepler (filtre ile), Yeni Talep

#### H) Program.cs Service Registration
- `builder.Services.AddScoped<IDestekTalebiService, DestekTalebiService>();`

**Özellikler:**
- ✅ osTicket benzeri profesyonel biletleme sistemi
- ✅ Departman ve kategori bazlı organizasyon
- ✅ 5 seviyeli öncelik sistemi (Düşük → Kritik)
- ✅ 6 durum akışı (Yeni → Kapalı)
- ✅ SLA yönetimi ve aşım takibi
- ✅ Otomatik talep numarası üretimi (YYYY-MMNNNN)
- ✅ Hazır yanıt şablonları
- ✅ Dosya eki desteği
- ✅ Dahili not özelliği (müşteriye görünmez)
- ✅ Müşteri adına yanıt yazma
- ✅ Aktivite geçmişi takibi (12 aktivite türü)
- ✅ Dashboard istatistikleri
- ✅ Performans raporlaması (personel bazlı)
- ✅ Departman transferi
- ✅ Talep birleştirme altyapısı
- ✅ Gelişmiş filtreleme ve sayfalama
- ✅ Relative time gösterimi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/DestekTalebi.cs` (yeni - tüm entity'ler)
- `CRMFiloServis.Web/Services/Interfaces/IDestekTalebiService.cs` (yeni - 50+ metod, model sınıfları)
- `CRMFiloServis.Web/Services/DestekTalebiService.cs` (yeni - 1565 satır implementasyon)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi - 9 DbSet, konfigürasyon)
- `CRMFiloServis.Web/Data/DbInitializer.cs` (güncellendi - seed data)
- `CRMFiloServis.Web/Components/Pages/DestekTalepleri/DestekTalebiList.razor` (yeni - 680 satır)
- `CRMFiloServis.Web/Components/Pages/DestekTalepleri/DestekTalebiDetay.razor` (yeni - 833 satır)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Models/PagedResult.cs` (güncellendi - PageNumber property)

**Durum:** ✅ Tamamlandı

### Kayıt 054 - Puantaj Excel Import Sistemi + Hata Düzeltmeleri
**Talep:** Puantaj verilerini Excel şablonundan import etme sistemi. Bölge, sıra no, semt (güzergah), sefer fiyatı, S/A servis tipi, plaka, şoför, telefon, firma adı, günlük puantaj (ayın günleri), sefer günü toplamı, toplam, KDV %20, KDV %10, kesinti, ödenecek. Araç/şoför/güzergah otomatik oluşturma. Ayrıca 5 kritik hata düzeltmesi.

**Yapılanlar:**

#### A) Hata Düzeltmeleri

**1. Dashboard SiralamaNo Hatası:**
- `DbInitializer.cs` güncellendi - `Soforler` tablosuna `SiralamaNo INTEGER` kolon ekleme

**2. AI Analiz KalanTutar LINQ Hatası:**
- `CariRiskService.cs` güncellendi - 4 LINQ sorgusu düzeltildi (client-side evaluation hatası)
- `CariHareketTakipService.cs` güncellendi - 1 LINQ sorgusu düzeltildi
- PostgreSQL'de çalışmayan `.Sum()` ifadeleri `AsEnumerable()` ile client-side'a taşındı

**3. Bütçe Ödeme DB'de Ödendi Olmuyor:**
- `BudgetService.cs` güncellendi - `ExecuteUpdateAsync` ile doğrudan DB güncelleme
- `OdemeYonetimi.razor` güncellendi - İşlemNo'ya milisaniye eklenerek uniqueness sağlandı

**4. Beyaz Ekran Port Çakışması:**
- Port 5190 çakışması çözüldü (process kill)

**5. Kasa/Banka Hareketler Pagination Hatası:**
- `BankaHareketList.razor` güncellendi - `OnPageSizeChanged` → `PageSizeChanged` event adı düzeltildi

#### B) Puantaj Excel Import Sistemi

**PuantajKayit Entity Güncellendi** (`CRMFiloServis.Shared/Entities/PuantajKayit.cs`):
- `Bolge` (string?) - Bölge bilgisi
- `SiraNo` (int) - Sıra numarası
- `AitFirmaAdi` (string?) - Ait olduğu firma adı
- `Gun01`-`Gun31` (int) - 31 günlük puantaj alanları (0/1/2 değer)
- `SeferGunuToplami` [NotMapped] - Gun01+...+Gun31 computed property
- `GetGunDeger(int gun)` / `SetGunDeger(int gun, int deger)` - Switch-based accessor metodları
- `HesaplaPuantajToplam()` - Gun→SeferGunuToplami, ToplamGider, Odenecek hesaplama

**HakedisService Genişletildi** (`CRMFiloServis.Web/Services/HakedisService.cs`):
- `PuantajSablonSatiri` modeli eklendi:
  - Bolge, SiraNo, Semt, SeferFiyati, ServisTipi, Plaka, SoforAdi, SoforTelefon, FirmaAdi
  - Gunler[31] dizisi, SeferGunuToplami, Toplam, Kdv20, Kdv10, Kesinti, Odenecek
  - Eşleştirme alanları: GuzergahId, AracId, SoforId, KurumCariId, SahiplikTipi
- `IHakedisService` interface'e 3 yeni metod eklendi:
  - `PuantajSablonOnizlemeAsync(Stream, yil, ay, baslangicSatiri)` - Excel'den satır okuma
    - Kolon düzeni: Bölge(1)|SıraNo(2)|Semt(3)|Fiyat(4)|S/A(5)|Plaka(6)|Şoför(7)|Tel(8)|Firma(9)|Gün1-N(10+)|TopSef|Toplam|KDV20|KDV10|Kesinti|Ödenecek
    - Cache'li Güzergah/Araç/Şoför otomatik eşleştirme
    - Plaka normalize, SahiplikTipi tespiti (Öz Mal/Kiralama/Komisyon)
  - `PuantajSablonImportAsync(satirlar, yil, ay, dosyaAdi, kullanici, kurumCariId, otomatikOlustur)` - Kaydetme
    - Otomatik Güzergah oluşturma (BirimFiyat, SeferTipi eşleştirme)
    - Otomatik Şoför oluşturma (SGKBordroDahilMi=false, Telefon)
    - PuantajKayit günlük puantaj kayıt (SetGunDeger loop, BirimGider=SeferFiyati)
  - `PuantajSablonIndirAsync(yil, ay)` - EPPlus ile şablon Excel oluşturma
    - Mavi header, hafta sonu kırmızı kolon, SUM formülleri, para formatı
    - Mevcut güzergah verileri ile ön doldurma
- `ParseServisTipiToPuantajYon(tip)` ve `ParseServisTipiToSeferTipi(tip)` yardımcı metodlar (S/A/S-A format desteği)

**ApplicationDbContext Güncellendi** (`CRMFiloServis.Web/Data/ApplicationDbContext.cs`):
- PuantajKayit config'e `Bolge` HasMaxLength(100), `AitFirmaAdi` HasMaxLength(200) eklendi

**DbInitializer Güncellendi** (`CRMFiloServis.Web/Data/DbInitializer.cs`):
- `EnsurePuantajKayitlarColumnsAsync` metodu eklendi (34 kolon):
  - Bolge VARCHAR(100) NULL, SiraNo INTEGER DEFAULT 0, AitFirmaAdi VARCHAR(200) NULL
  - Gun01-Gun31 INTEGER NOT NULL DEFAULT 0
- `InitializeWithConfigurationAsync`'den çağrı eklendi
- `BudgetOdemeler` tablosuna 11 kolon ekleme
- `BankaKasaHareketleri` tablosuna 7 kolon ekleme

**Özellikler:**
- ✅ Excel'den puantaj şablon okuma (EPPlus ile önizleme)
- ✅ Güzergah otomatik eşleştirme (isim benzerliği)
- ✅ Araç otomatik eşleştirme (plaka normalize)
- ✅ Şoför otomatik eşleştirme (ad-soyad)
- ✅ SahiplikTipi tespiti (Öz Mal/Kiralama/Komisyon)
- ✅ Otomatik güzergah oluşturma (BirimFiyat, SeferTipi)
- ✅ Otomatik şoför oluşturma (SGK'sız)
- ✅ Günlük puantaj kayıt (Gun01-Gun31)
- ✅ Şablon Excel indirme (hafta sonu renklendirme, formüller)
- ✅ S/A servis tipi parse (Sabah/Akşam/SabahAkşam)
- ✅ Dashboard SiralamaNo hata düzeltmesi
- ✅ AI analiz LINQ sorgu düzeltmeleri
- ✅ Bütçe ödeme kayıt düzeltmesi
- ✅ Pagination event adı düzeltmesi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/PuantajKayit.cs` (güncellendi - Bolge, SiraNo, AitFirmaAdi, Gun01-31, helper metodlar)
- `CRMFiloServis.Web/Services/HakedisService.cs` (güncellendi - PuantajSablonSatiri model, 3 yeni metod, 2 helper)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi - PuantajKayit config)
- `CRMFiloServis.Web/Data/DbInitializer.cs` (güncellendi - EnsurePuantajKayitlarColumnsAsync + BudgetOdemeler + BankaKasaHareketleri kolonlar)
- `CRMFiloServis.Web/Services/CariRiskService.cs` (güncellendi - 4 LINQ sorgu düzeltmesi)
- `CRMFiloServis.Web/Services/CariHareketTakipService.cs` (güncellendi - 1 LINQ sorgu düzeltmesi)
- `CRMFiloServis.Web/Services/BudgetService.cs` (güncellendi - ExecuteUpdateAsync)
- `CRMFiloServis.Web/Components/Pages/Budget/OdemeYonetimi.razor` (güncellendi - İşlemNo milisaniye)
- `CRMFiloServis.Web/Components/Pages/BankaHareketleri/BankaHareketList.razor` (güncellendi - PageSizeChanged)

**Durum:** ✅ Tamamlandı

### Kayıt 049 - Toplu Fatura Oluşturma
**Talep:** Puantaj kayıtlarından toplu fatura kesimi, cari bazlı dönemsel faturalama.

**Yapılanlar:**
- `TopluFaturaModels.cs` oluşturuldu (~190 satır):
  - `TopluFaturaKaynak` enum: Puantaj, Sozlesme, Manuel
  - `TopluFaturaDurum` enum: Hazir, EksikBilgi, FaturaKesildi
  - `TopluFaturaFiltre`: Yıl/Ay/Kaynak/FaturaYönü/Cari filtreleri
  - `TopluFaturaOnizleme`: Cari bazlı fatura önizleme (kalemler, toplamlar, tevkifat)
  - `TopluFaturaKalemOnizleme`: Kalem detayı (miktar, birim fiyat, KDV)
  - `TopluFaturaSonuc`: Oluşturma sonucu (başarılı/başarısız sayı, hatalar)
  - `OlusturulanFaturaBilgi`: Oluşturulan fatura özeti
  - `TopluFaturaOzet`: Dönem özeti (dashboard için)
  - `CariFaturaAyar`: Cari bazlı varsayılan ayarlar
- `ITopluFaturaService.cs` interface oluşturuldu (8 metot):
  - `GetDonemOzetiAsync(yil, ay)` - Dönem özeti (kesilecek/kesilmiş)
  - `GetOnizlemeAsync(filtre)` - Fatura önizleme listesi
  - `FaturaOlusturAsync(onizlemeler)` - Toplu fatura oluşturma
  - `TekFaturaOlusturAsync(onizleme)` - Tek fatura oluşturma
  - `PuantajFaturaEslestirAsync` - Puantaj-Fatura eşleştirme
  - `GetCariFaturaAyarAsync` - Cari fatura ayarları
  - `GetFaturaKesilmemisPuantajlarAsync` - Bekleyen puantajlar
  - `GetMevcutDonemlerAsync` - Mevcut dönem listesi
- `TopluFaturaService.cs` implementasyon oluşturuldu (~430 satır):
  - **Dönem Özeti**: Gelir/Gider fatura durumu, kesilecek tutar, kesilmiş tutar
  - **Gelir Fatura Önizleme**: KurumCari bazlı gruplama, satış faturası oluşturma
  - **Gider Fatura Önizleme**: OdemeYapilacakCari bazlı gruplama, alış faturası oluşturma
  - **Kalem Oluşturma**: Puantaj bilgilerinden otomatik açıklama (dönem, güzergah, yön)
  - **Toplu Faturalama**: Seçilen önizlemeleri toplu faturaya dönüştürme
  - **Puantaj Eşleştirme**: Oluşturulan faturayı puantaj kayıtlarına işleme
- `TopluFatura.razor` oluşturuldu (~450 satır):
  - **Route**: `/faturalar/toplu-fatura`
  - **Dönem Özet Kartları**: Kesilecek satış/alış fatura sayı ve tutarları
  - **Filtre Paneli**: Yıl, Ay, Fatura Yönü (Satış/Alış), Kaynak seçimi
  - **Önizleme Listesi**: Cari bazlı gruplu tablo, seçim checkbox'ları
  - **Detay Modal**: Fatura tarihi/vade düzenleme, kalem düzenleme, tevkifat ayarı
  - **Toplu İşlem**: "Seçilenleri Faturala" butonu, işlem sonuç bildirimi
- `Program.cs` güncellendi - DI kaydı eklendi
- `NavMenu.razor` güncellendi - Fatura menüsüne "Toplu Fatura" linki eklendi

**Özellikler:**
- ✅ Puantaj kayıtlarından otomatik fatura oluşturma
- ✅ Cari bazlı gruplama (her cari için tek fatura)
- ✅ Satış (Giden) ve Alış (Gelen) fatura desteği
- ✅ Kalem bazlı düzenleme (miktar, birim fiyat, açıklama)
- ✅ Tevkifat desteği (oran ve kod)
- ✅ Dönem özeti (kesilecek vs kesilmiş karşılaştırma)
- ✅ Puantaj-Fatura otomatik eşleştirme

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/TopluFaturaModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/ITopluFaturaService.cs` (yeni)
- `CRMFiloServis.Web/Services/TopluFaturaService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Faturalar/TopluFatura.razor` (yeni)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 050 - E-Fatura XML Oluşturma (GİB UBL-TR 1.2)
**Talep:** Faturalardan GİB uyumlu UBL-TR 1.2 formatında e-fatura XML dosyası oluşturma.

**Yapılanlar:**
- `EFaturaXmlModels.cs` oluşturuldu (~650 satır):
  - **Ana Modeller**:
    - `EFaturaXmlRequest`: XML oluşturma isteği (FaturaId, Senaryo, ETTN)
    - `EFaturaXmlSonuc`: XML oluşturma sonucu (XML içerik, ETTN, dosya yolu, hatalar)
    - `EFaturaDogrulamaRapor`: Doğrulama sonucu (geçerli, hatalar, uyarılar)
    - `EFaturaSenaryo` enum: Temel, Ticari, İhracat, Kamu
  - **UBL-TR Yapıları** (GİB uyumlu):
    - `UblInvoice`: Fatura kök elementi (ID, UUID, IssueDate, InvoiceTypeCode, vb.)
    - `UblAccountingParty`, `UblParty`: Satıcı/Alıcı taraf bilgileri
    - `UblPartyIdentification`: VKN/TCKN kimlik tanımlayıcı (schemeID ile)
    - `UblAddress`, `UblCountry`: Adres ve ülke bilgileri
    - `UblPartyTaxScheme`, `UblTaxScheme`: Vergi dairesi bilgisi
    - `UblTaxTotal`, `UblTaxSubtotal`, `UblTaxCategory`: Vergi hesaplamaları
    - `UblInvoiceLine`, `UblItem`, `UblPrice`: Fatura kalemi detayları
    - `UblQuantity`, `UblAmount`: Miktar ve tutar (unitCode/currencyID ile)
    - `UblMonetaryTotal`: Toplam tutarlar (LineExtension, TaxExclusive, PayableAmount)
    - `UblAllowanceCharge`: İskonto/Ek ücret bilgileri
    - `UblPaymentMeans`, `UblPaymentTerms`: Ödeme koşulları
  - **GİB Sabit Kodları**:
    - `GibVergiKodlari`: KDV (0015), ÖTV, Damga Vergisi vb.
    - `GibTevkifatKodlari`: 40+ tevkifat kodu (601-699)
    - `UblBirimKodlari`: Türkçe birim → UN/ECE kodu eşleştirme (Adet→C62, Kg→KGM, Lt→LTR, vb.)
    - `EFaturaTipKodlari`: SATIS, IADE, TEVKIFAT, ISTISNA
    - `EFaturaProfilIdleri`: TEMELFATURA, TICARIFATURA, IHRACATKAYITLI
- `IEFaturaXmlService.cs` interface oluşturuldu (8 metot):
  - `XmlOlusturAsync(request)` - E-Fatura XML oluşturma
  - `UblDonusturAsync(faturaId)` - Fatura → UBL-TR dönüşümü
  - `DogrulaAsync(xmlIcerik)` - XML doğrulama (zorunlu alan kontrolü)
  - `DosyayaKaydetAsync(faturaId, xml)` - XML dosya kaydetme
  - `XmlOkuAsync(faturaId)` - Mevcut XML okuma
  - `TopluXmlOlusturAsync(faturaIdler, senaryo)` - Toplu XML oluşturma
  - `YeniEttnOlustur()` - GUID formatında ETTN oluşturma
  - `BirimKoduDonustur(birim)` - Birim → UBL kodu dönüşümü
- `EFaturaXmlService.cs` implementasyon oluşturuldu (~550 satır):
  - **XML Oluşturma**:
    - Fatura → UblInvoice dönüşümü
    - Profil ID senaryoya göre ayarlama
    - XML serialization (namespace'ler ile)
    - Doğrulama ve dosya kaydetme
    - Fatura ETTN/XmlDosyaYolu güncelleme
  - **Taraf Bilgileri**:
    - `OlusturSatici(Firma)`: Firma bilgilerinden UBL satıcı oluşturma
    - `OlusturAlici(Cari)`: Cari bilgilerinden UBL alıcı oluşturma
    - VKN/TCKN otomatik tanımlama (11 haneli = TCKN)
    - Şahıs firması için Person elementi
  - **Fatura Kalemleri**:
    - `OlusturFaturaKalemi`: Kalem bazlı KDV ve tevkifat hesaplama
    - İskonto (AllowanceCharge) desteği
    - Ürün kodu (SellersItemIdentification) desteği
  - **Vergi Hesaplamaları**:
    - `OlusturVergiToplamlari`: KDV oranlarına göre gruplama
    - `OlusturTevkifatToplamlari`: Tevkifat toplamı
    - `OlusturMonetaryTotal`: Toplam tutarlar
  - **Doğrulama**:
    - UUID, ID, Satıcı VKN, Fatura kalemleri, PayableAmount kontrolü
    - XPath ile XML element kontrolü
    - Hata ve uyarı listeleme
  - **Dosya Yönetimi**:
    - `wwwroot/efatura/YYYY/MM/` klasör yapısı
    - UTF-8 encoding ile kaydetme
- `EFaturaXml.razor` oluşturuldu (~400 satır):
  - **Route**: `/faturalar/efatura-xml`
  - **Filtre Paneli**:
    - Tarih aralığı (başlangıç/bitiş)
    - Fatura tipi (Satış, Alış, Tevkifatlı)
    - XML durumu (Oluşturulmamış, Mevcut)
    - Senaryo seçimi (Temel, Ticari, İhracat)
  - **Fatura Listesi**:
    - Toplu seçim (checkbox)
    - Fatura no, tarih, cari, tip, tutar gösterimi
    - ETTN gösterimi (kısaltılmış)
    - XML durumu (Mevcut/Yok badge)
    - İşlem butonları: Oluştur, Önizle, İndir, Yeniden Oluştur
  - **Toplu İşlem**: Seçili faturalar için toplu XML oluşturma
  - **XML Önizleme Modal**: Tam XML içeriği görüntüleme, panoya kopyalama
  - **Doğrulama Raporu Modal**: Hatalar ve uyarılar listesi
- `Program.cs` güncellendi - `IEFaturaXmlService` DI kaydı eklendi
- `NavMenu.razor` güncellendi - "E-Fatura XML" linki eklendi

**Özellikler:**
- ✅ GİB UBL-TR 1.2 uyumlu XML oluşturma
- ✅ Temel/Ticari/İhracat senaryo desteği
- ✅ Satış ve iade fatura desteği
- ✅ Tevkifatlı fatura desteği (WithholdingTaxTotal)
- ✅ Türkçe birim → UN/ECE kod dönüşümü
- ✅ VKN/TCKN otomatik tanımlama
- ✅ XML doğrulama (zorunlu alan kontrolü)
- ✅ Toplu XML oluşturma
- ✅ XML önizleme ve indirme
- ✅ ETTN (UUID) otomatik oluşturma
- ✅ Fatura kaydına XML yolu ve ETTN kaydetme

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/EFaturaXmlModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IEFaturaXmlService.cs` (yeni)
- `CRMFiloServis.Web/Services/EFaturaXmlService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Faturalar/EFaturaXml.razor` (yeni)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 051 - Luca Portal Entegrasyonu
**Talep:** Luca e-dönüşüm portalı ile entegrasyon. Ayarlar sayfasından kullanıcı adı/şifre girişi, E-Fatura gelen/kesilen belgelerinin çekilmesi, E-Arşiv XML ve PDF dosyalarının indirilmesi.

**Yapılanlar:**
- `LucaPortalSettings.cs` entity oluşturuldu (~130 satır):
  - `LucaPortalSettings`: Ayarlar modeli (kullanıcı adı, şifre, portal URL, token bilgileri, senkron ayarları, firma bağlantısı)
  - `LucaBelge`: Portal'dan çekilen belge modeli (ETTN, fatura no, tarih, gönderici/alıcı VKN-unvan, tutarlar, durum, XML/PDF URL'leri)
  - `LucaBelgeTipi` enum: EFatura, EArsiv, EMustahsil, EIrsaliye
  - `LucaBelgeYonu` enum: Gelen, Giden
  - `LucaSorguFiltre`: Sorgu parametreleri (tarih aralığı, belge tipi/yönü, VKN arama, sayfalama)
  - `LucaSorguSonuc`: Sorgu sonucu (belgeler listesi, toplam kayıt, sayfalama)
  - `LucaLoginSonuc`: Giriş sonucu (başarı, token, firma kodu/unvan)
  - `LucaBelgeIndirmeSonuc`: İndirme sonucu (içerik bytes, dosya adı, content type)
- `ILucaPortalService.cs` interface oluşturuldu (15 metot):
  - **Ayarlar**: `GetAyarlarAsync(firmaId?)`, `AyarKaydetAsync(ayarlar)`
  - **Kimlik Doğrulama**: `GirisYapAsync(kullaniciAdi, sifre)`, `TokenYenileAsync(refreshToken)`, `CikisYapAsync()`, `BaglantiTestiAsync()`
  - **Belge Sorgulama**: `EFaturaListeleAsync(filtre)`, `EArsivListeleAsync(filtre)`, `BelgeDetayGetirAsync(belgeId, belgeTipi)`
  - **Belge İndirme**: `XmlIndirAsync(belgeId, belgeTipi)`, `PdfIndirAsync(belgeId, belgeTipi)`, `TopluXmlIndirAsync(idler, belgeTipi)`, `TopluPdfIndirAsync(idler, belgeTipi)`
  - **Sisteme Aktarma**: `BelgeleriSistemeAktarAsync(belgeler, xmlIndir, pdfIndir)`, `TumBelgeleriSenkronizeEtAsync(baslangic, bitis, progress)`
- `LucaPortalService.cs` implementasyon oluşturuldu (~700 satır):
  - **Ayar Yönetimi**: JSON dosyası ile firma bazlı ayar saklama (`Data/LucaSettings/lucasettings_{firmaId}.json`)
  - **Kimlik Doğrulama**: Web scraping ile login (CSRF token, cookie yönetimi), oturum cache
  - **Belge Listeleme**: HTML parse ile belge tablosu çözümleme (Regex ile TR/TD içerik okuma)
  - **Belge İndirme**: HttpClient ile XML/PDF indirme, rate limiting (200ms delay)
  - **Sisteme Aktarma**: 
    - ETTN kontrolü ile mükerrer kayıt engelleme
    - VKN ile cari eşleştirme veya otomatik cari oluşturma
    - Fatura entity oluşturma (ImportKaynak = "Luca")
    - XML/PDF dosya kaydetme (`wwwroot/belgeler/efatura/`)
  - **Senkronizasyon**: Tüm belge tiplerini (E-Fatura Gelen/Giden, E-Arşiv Gelen/Giden) topluca çekme
- `LucaPortalAyarlari.razor` oluşturuldu (~300 satır):
  - **Route**: `/ayarlar/luca-portal`
  - **Giriş Bilgileri**: Portal URL, kullanıcı adı, şifre (göster/gizle)
  - **Bağlantı Testi**: Kimlik doğrulama ve durum gösterimi
  - **Senkron Ayarları**: Otomatik senkron toggle, aralık seçimi (1-24 saat)
  - **Bağlantı Durumu**: Token geçerliliği, firma kodu, son senkron tarihi
  - **Hızlı İşlemler**: Belgeleri listele, şimdi senkronize et, oturumu kapat
  - **Senkron Logu**: Terminal görünümünde işlem mesajları
- `LucaPortalBelgeleri.razor` oluşturuldu (~400 satır):
  - **Route**: `/faturalar/luca-portal`
  - **Filtre Paneli**: Belge tipi (E-Fatura/E-Arşiv), yön (Gelen/Giden), tarih aralığı, VKN arama
  - **Belge Listesi**: Tablo görünümü (fatura no, ETTN, tarih, gönderici/alıcı, VKN, tutar, durum, XML/PDF ikonları)
  - **Toplu Seçim**: Checkbox ile çoklu seçim, tümünü seç
  - **Tekil İşlemler**: Her satırda XML indir, PDF indir, sisteme aktar butonları
  - **Toplu İşlemler**: Seçilenleri aktar, toplu XML indir, toplu PDF indir
  - **Sayfalama**: Sayfa navigasyonu
  - **Base64 İndirme**: JS ile tarayıcıya dosya indirme
- `Program.cs` güncellendi - `ILucaPortalService` DI kaydı eklendi
- `NavMenu.razor` güncellendi:
  - Cari menüsüne "Luca Portal" linki eklendi
  - Ayarlar menüsüne "Luca Portal" linki eklendi

**Özellikler:**
- ✅ Luca e-dönüşüm portal entegrasyonu
- ✅ Kullanıcı adı/şifre ile kimlik doğrulama
- ✅ E-Fatura gelen/kesilen belgeler listeleme
- ✅ E-Arşiv gelen/kesilen belgeler listeleme
- ✅ XML ve PDF dosya indirme (tekil ve toplu)
- ✅ Belgeleri sisteme fatura olarak aktarma
- ✅ Otomatik cari oluşturma (VKN ile eşleşme yoksa)
- ✅ ETTN ile mükerrer kayıt engelleme
- ✅ Firma bazlı ayar saklama (JSON dosya)
- ✅ Otomatik senkronizasyon ayarı
- ✅ Bağlantı testi ve durum gösterimi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/LucaPortalSettings.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/ILucaPortalService.cs` (yeni)
- `CRMFiloServis.Web/Services/LucaPortalService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/LucaPortalAyarlari.razor` (yeni)
- `CRMFiloServis.Web/Components/Pages/Faturalar/LucaPortalBelgeleri.razor` (yeni)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 052 - Cari Otomatik Hatırlatmalar
**Talep:** Cariler için vadesi yaklaşan/geçmiş fatura hatırlatmaları, borç/alacak eşik uyarıları, hareketsiz cari bildirimleri, otomatik e-posta ve sistem bildirimi gönderimi.

**Yapılanlar:**
- `CariHatirlatmaSettings.cs` entity oluşturuldu (~120 satır):
  - `CariHatirlatmaSettings`: Hatırlatma ayarları (aktif/pasif, kontrol saati, vade günleri, eşik tutarları, email ayarları)
  - `CariHatirlatma`: Hatırlatma geçmişi entity (cari, fatura, tip, tutar, gönderim durumu)
  - `CariHatirlatmaTipi` enum: VadeYaklasan, VadeGecmis, BorcEsikAsildi, AlacakEsikAsildi, TahsilatHatirlatma, HareketsizCari, OdemeAlindi, FaturaOdendi
  - `CariHatirlatmaRapor`: Kontrol raporu (toplam uyarı, tutarlar, detay listesi)
  - `CariHatirlatmaDetay`: Uyarı detayı (cari bilgisi, tutar, vade, öncelik)
- `ICariHatirlatmaService.cs` interface oluşturuldu (12 metot):
  - **Ayarlar**: `GetAyarlarAsync`, `SaveAyarlarAsync`
  - **Manuel Kontrol**: `HatirlatmaKontroluYapAsync`
  - **Vade Kontrolleri**: `VadeYaklasanFaturalariGetirAsync`, `VadeGecmisFaturalariGetirAsync`
  - **Eşik Kontrolleri**: `BorcEsikAsilanCarileriGetirAsync`, `AlacakEsikAsilanCarileriGetirAsync`
  - **Hareketsiz Cari**: `HareketsizCarileriGetirAsync`
  - **Hatırlatma Geçmişi**: `GetHatirlatmaGecmisiAsync`
  - **Tek Cari**: `TekCariHatirlatmaGonderAsync`
  - **E-posta**: `VadeHatirlatmaEmailiGonderAsync`, `TopluHatirlatmaEmailiGonderAsync`
  - **Özet**: `GetHatirlatmaOzetiAsync`
- `CariHatirlatmaService.cs` implementasyon oluşturuldu (~650 satır):
  - **Ayar Yönetimi**: JSON dosya ile firma bazlı ayar saklama (`Data/HatirlatmaAyarlari/`)
  - **Vade Yaklaşan**: Belirlenen günlerde hatırlatma (7, 3, 1 gün önce)
  - **Vade Geçmiş**: Minimum tutar filtreli geçmiş fatura tespiti
  - **Borç/Alacak Eşik**: Fatura bazlı bakiye hesaplama, eşik aşımı kontrolü
  - **Hareketsiz Cari**: Son fatura/hareket tarihine göre tespit
  - **Bildirim Oluşturma**: Admin kullanıcılara sistem bildirimi
  - **E-posta Gönderimi**: Tek fatura ve toplu rapor email şablonları
  - **Özet İstatistikler**: Dashboard için güncel özet bilgileri
- `CariHatirlatmaBackgroundService.cs` arka plan servisi oluşturuldu (~100 satır):
  - Belirlenen saatte günlük otomatik kontrol
  - Firma bazlı ayar kontrolü
  - Son kontrol tarihi takibi
- `CariHatirlatmaAyarlari.razor` sayfa oluşturuldu (~350 satır):
  - **Route**: `/ayarlar/cari-hatirlatma`
  - **Özet Kartları**: Vadesi geçmiş/yaklaşan sayısı, eşik aşımı, haftalık gönderim
  - **Genel Ayarlar**: Aktif/pasif, kontrol saati, son kontrol bilgisi
  - **Vade Hatırlatmaları**: Yaklaşan/geçmiş gün ayarları, minimum tutar
  - **Eşik Uyarıları**: Borç/alacak eşik tutarları
  - **Diğer**: Tahsilat özeti, hareketsiz cari kontrolü
  - **Bildirim Ayarları**: E-posta gönderimi, admin/müşteri, ek adresler
  - **Manuel Kontrol**: Test butonu, kontrol raporu görüntüleme
- `ApplicationDbContext.cs` güncellendi - `CariHatirlatmalar` DbSet eklendi
- `Program.cs` güncellendi - `ICariHatirlatmaService`, `CariHatirlatmaBackgroundService` DI kaydı eklendi
- `NavMenu.razor` güncellendi - CRM menüsüne "Cari Hatırlatma" linki eklendi

**Özellikler:**
- ✅ Vadesi yaklaşan fatura hatırlatmaları (özelleştirilebilir gün sayıları)
- ✅ Vadesi geçmiş fatura hatırlatmaları (minimum tutar filtreli)
- ✅ Borç/alacak eşik aşımı uyarıları
- ✅ Hareketsiz cari tespiti (belirlenen gün sonra)
- ✅ Günlük otomatik kontrol (arka plan servisi)
- ✅ Sistem bildirimi oluşturma (öncelik seviyeleri)
- ✅ Toplu e-posta raporu (admin kullanıcılara)
- ✅ Müşteriye direkt e-posta gönderimi (opsiyonel)
- ✅ Firma bazlı ayar yönetimi (JSON)
- ✅ Manuel kontrol ve rapor görüntüleme
- ✅ Hatırlatma geçmişi kaydı

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/CariHatirlatmaSettings.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/ICariHatirlatmaService.cs` (yeni)
- `CRMFiloServis.Web/Services/CariHatirlatmaService.cs` (yeni)
- `CRMFiloServis.Web/Services/CariHatirlatmaBackgroundService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/CariHatirlatmaAyarlari.razor` (yeni)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 053 - Fatura Şablonları
**Talep:** Özelleştirilebilir fatura şablonları, PDF oluşturma, yazdırma, e-posta ile gönderme. Firma logosu, renk temaları, sayfa düzeni ayarları.

**Yapılanlar:**
- `FaturaSablon.cs` entity oluşturuldu (~180 satır):
  - `FaturaSablon`: Şablon ayarları (ad, varsayılan, firma bağlantısı)
  - **Sayfa Ayarları**: Boyut (A4/A5/Letter), yönelim (Dikey/Yatay), kenar boşlukları
  - **Logo Ayarları**: Logo görüntüsü (Base64), konum (Sol/Orta/Sağ), boyutlar
  - **Başlık Ayarları**: Fatura başlığı metni, konum, yazı boyutu
  - **Renk Ayarları**: Primary renk, tablo başlık/satır/zebra renkleri, toplam arka plan
  - **Tablo Ayarları**: Sütun görünürlükleri (sıra no, KDV, iskonto), zebra deseni
  - **Toplam Ayarları**: Konum (Sol/Orta/Sağ), genişlik, ara toplam/KDV/ödenen/kalan gösterimi
  - **Ek Alanlar**: Banka bilgileri, notlar, kaşe/imza alanları, QR kod
  - `SayfaBoyutu` enum: A4, A5, Letter
  - `SayfaYonelimi` enum: Dikey, Yatay
  - `LogoKonumu` enum: Sol, Orta, Sag
  - `BaslikKonumu` enum: Sol, Orta, Sag
  - `ToplamKonumu` enum: Sol, Orta, Sag
  - `QrKodIcerik` enum: Yok, FaturaBilgisi, OdemeLinki, WebSitesi
  - `FaturaYazdirRequest`: Yazdırma isteği (fatura ID, şablon ID, kopya sayısı)
  - `FaturaPdfResult`: PDF sonucu (bytes, dosya adı, başarı, hata)
- `IFaturaSablonService.cs` interface oluşturuldu (12 metot):
  - **Şablon CRUD**: `TumSablonlariGetirAsync`, `SablonGetirAsync(id)`, `VarsayilanSablonGetirAsync`, `SablonKaydetAsync`, `SablonSilAsync`
  - **PDF Oluşturma**: `FaturaPdfOlusturAsync(faturaId, sablonId?)`, `TopluFaturaPdfAsync(faturaIdler)`, `OnizlemePdfOlusturAsync(sablon)` - örnek fatura ile önizleme
  - **Yazdırma/E-posta**: `FaturaYazdirAsync(request)`, `FaturaEmailGonderAsync(faturaId, email, sablonId?)`
  - **Yardımcı**: `VarsayilanSablonOlusturAsync`, `SablonKopyalaAsync(id, yeniAd)`
- `FaturaSablonService.cs` implementasyon oluşturuldu (~1050 satır):
  - **QuestPDF Entegrasyonu**: Fluent API ile PDF oluşturma
  - **Şablon Yönetimi**: CRUD işlemleri, varsayılan şablon kontrolü
  - **PDF Oluşturma**:
    - `ComposeHeader`: Firma logosu, fatura başlığı, fatura/vade tarihi, fatura no, cari bilgileri
    - `ComposeContent`: Fatura kalemleri tablosu, toplam bölümü, banka bilgileri, notlar, kaşe/imza alanları
    - `ComposeFooter`: Sayfa numarası
  - **Dinamik Düzen**:
    - Logo konumu ayarına göre hizalama
    - Sayfa boyutu ve yönelim desteği
    - Özelleştirilebilir renk temaları
    - Zebra desen, sütun görünürlükleri
  - **E-posta Gönderimi**: PDF eki ile SMTP üzerinden gönderim (System.Net.Mail)
  - **Önizleme**: Örnek fatura verisi ile şablon önizlemesi
  - **Renk Dönüşümü**: Hex → QuestPDF Color (`ParseColor` metodu)
- `FaturaSablonlari.razor` sayfa oluşturuldu (~550 satır):
  - **Route**: `/ayarlar/fatura-sablonlari`
  - **Şablon Listesi**: Mevcut şablonlar, varsayılan işareti, düzenle/sil butonları
  - **Sekmeli Düzenleme**:
    - **Genel**: Şablon adı, varsayılan seçimi, sayfa boyutu/yönelimi, kenar boşlukları
    - **Logo & Başlık**: Logo yükleme (Base64), konum/boyut, başlık metni/konum
    - **Renkler**: Primary renk, tablo başlık/satır/zebra renkleri, toplam arka plan
    - **Tablo**: Sütun görünürlükleri, zebra deseni, cari bilgi kutusu
    - **Diğer**: Toplam konumu/genişliği, banka bilgileri, kaşe/imza alanları
  - **PDF Önizleme**: Modal içinde örnek fatura görüntüleme
  - **Logo/Kaşe Yükleme**: Dosya seçim ve Base64 dönüşümü
  - **Renk Seçiciler**: HTML5 color input ile renk seçimi
- `ApplicationDbContext.cs` güncellendi - `FaturaSablonlari` DbSet eklendi
- `Program.cs` güncellendi - `IFaturaSablonService` DI kaydı eklendi
- `NavMenu.razor` güncellendi - Ayarlar menüsüne "Fatura Şablonları" linki eklendi

**Özellikler:**
- ✅ Özelleştirilebilir fatura şablonları (çoklu şablon desteği)
- ✅ Varsayılan şablon belirleme
- ✅ QuestPDF ile profesyonel PDF oluşturma
- ✅ Firma logosu desteği (Base64)
- ✅ Sayfa boyutu (A4/A5/Letter) ve yönelim (Dikey/Yatay)
- ✅ Renk teması özelleştirme
- ✅ Tablo düzeni ayarları (sütunlar, zebra desen)
- ✅ Toplam bölümü konumlandırma
- ✅ Banka bilgileri, notlar, kaşe/imza alanları
- ✅ PDF önizleme (örnek fatura ile)
- ✅ E-posta ile fatura gönderimi (PDF eki)
- ✅ Şablon kopyalama

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/FaturaSablon.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IFaturaSablonService.cs` (yeni)
- `CRMFiloServis.Web/Services/FaturaSablonService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/FaturaSablonlari.razor` (yeni)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 048 - Kolay Muhasebe Girişi
**Talep:** Muhasebe bilgisi olmayan kullanıcılar için tek sayfadan gelir-gider fatura, masraf, fiş, tahsilat, ödeme, mahsup, avans girişleri yapılabilecek sayfa. Girilen bilgilere göre altta muhasebe kaydı (borç-alacak) otomatik gösterilecek, kullanıcı manuel düzeltme yapabilecek, "Muhasebeleştir" butonu ile kayıt oluşturulacak.

**Yapılanlar:**
- `KolayMuhasebeModels.cs` oluşturuldu (~130 satır):
  - `KolayIslemTuru` enum: GelirFatura, GiderFatura, MasrafGirisi, TahsilatGirisi, OdemeGirisi, MahsupKaydi, AvansGirisi (7 tür)
  - `KolayMuhasebeGiris`: Form modeli (işlem türü, tarih, belge no, cari, tutar, KDV, tevkifat, banka hesap, masraf kalemi, araç)
  - `MuhasebeOnizleme`: Fiş önizleme (fiş no, tarih, tür, kalemler, toplam borç/alacak, denge kontrolü)
  - `MuhasebeKalemOnizleme`: Satır önizleme (hesap kodu/adı, borç/alacak, cari bilgisi, düzenlenme flag'i)
  - `KolayMuhasebeSonuc`: Kaydetme sonucu (başarı, mesaj, oluşturulan ID'ler, uyarılar)
  - `MasrafKalemiBasit`, `BankaHesapBasit`: Dropdown için basit modeller
  - `OdemeYontemi` enum: Nakit, BankaHavalesi, Cek, KrediKarti, Diger
- `IKolayMuhasebeService.cs` interface oluşturuldu (9 metot):
  - `OnizlemeOlusturAsync(KolayMuhasebeGiris)` - İşlem türüne göre muhasebe kaydı önizlemesi
  - `KaydetAsync(KolayMuhasebeGiris, MuhasebeOnizleme?)` - Fatura/Masraf/BankaHareket + Muhasebe fişi kaydetme
  - `GetCarilerAsync(tip, arama)` - Hızlı cari seçimi
  - `GetMasrafKalemleriAsync()` - Masraf kalemi listesi
  - `GetBankaHesaplariAsync()` - Banka/kasa hesapları
  - `GetAraclarAsync()` - Araç listesi (araç masrafı için)
  - `HizliCariOlusturAsync(unvan, tip)` - Yeni cari hızlı oluşturma
  - `GetMuhasebeHesaplariAsync()` - Manuel düzenleme için hesap listesi
  - `GetMuhasebeAyarAsync()` - Varsayılan hesap kodları
- `KolayMuhasebeService.cs` implementasyon oluşturuldu (~900 satır):
  - **Muhasebe Kaydı Oluşturma** (7 işlem türü için):
    - `OlusturGelirFaturaKalemleri`: 120 Alıcılar BORÇ → 600 Satışlar + 391 KDV ALACAK (tevkifat destekli)
    - `OlusturGiderFaturaKalemleri`: 770 Gider + 191 KDV BORÇ → 320 Satıcılar ALACAK (tevkifat destekli)
    - `OlusturMasrafKalemleri`: 7xx Gider + 191 KDV BORÇ → 100/102 Kasa/Banka ALACAK
    - `OlusturTahsilatKalemleri`: 100/102 BORÇ → 120 Alıcılar ALACAK
    - `OlusturOdemeKalemleri`: 320 Satıcılar BORÇ → 100/102 ALACAK
    - `OlusturAvansKalemleri`: 195 Avanslar BORÇ → 100/102 ALACAK
    - `OlusturMahsupKalemleri`: Kullanıcı manuel düzenleyecek
  - **Kaydetme İşlemleri** (işlem türüne göre):
    - `KaydetGelirFatura`: Fatura + MuhasebeFis oluşturma
    - `KaydetGiderFatura`: Fatura + MuhasebeFis oluşturma
    - `KaydetMasraf`: AracMasraf + MuhasebeFis + BankaKasaHareket oluşturma
    - `KaydetTahsilat`: BankaKasaHareket + MuhasebeFis oluşturma
    - `KaydetOdeme`: BankaKasaHareket + MuhasebeFis oluşturma
    - `KaydetAvans`: BankaKasaHareket + MuhasebeFis oluşturma
    - `KaydetMuhasebeFisi`: MuhasebeFis + MuhasebeFisKalem kaydetme
  - **Yardımcı Metodlar**:
    - `GetMasrafHesapKodu(MasrafKategori)`: Kategori bazlı muhasebe hesap kodu (Yakıt→770.01, Bakım→770.02, vb.)
    - `GetBankaHesapKodu(HesapTipi)`: Hesap tipi bazlı muhasebe kodu (Kasa→100.01, Banka→102.01, vb.)
    - `GenerateFaturaNo(prefix)`: Fatura no oluşturma (SF2025000001, AF2025000001)
    - `GenerateIslemNo()`: Banka hareket işlem no oluşturma (ISL202506XXXX)
- `KolayGiris.razor` oluşturuldu (~500 satır):
  - **Route**: `/muhasebe/kolay-giris`
  - **Sol Panel - İşlem Formu**:
    - İşlem türü butonları (7 tür, renkli badge'ler)
    - Tarih/Belge No/Vade girişleri
    - Cari seçimi (autocomplete + hızlı ekleme)
    - Tutar girişleri (ara toplam, KDV oranı/tutar, genel toplam)
    - Tevkifat seçeneği (oran/kod/tutar)
    - Banka/Kasa hesap seçimi
    - Masraf kalemi ve araç seçimi (masraf girişi için)
    - Açıklama/Notlar
  - **Sağ Panel - Muhasebe Önizleme**:
    - Fiş bilgileri (no, tarih, tür)
    - Muhasebe kalemleri tablosu (hesap kodu/adı, borç, alacak)
    - Toplam borç/alacak gösterimi
    - Denge durumu (✅ Dengeli / ⚠️ Dengesiz)
    - Manuel hesap düzenleme (dropdown ile hesap seçimi)
  - **Alt Kısım**:
    - "Muhasebeleştir ve Kaydet" butonu (sadece dengeli ise aktif)
    - Son girilen işlemler listesi
- `Program.cs` güncellendi - DI kaydı: `builder.Services.AddScoped<IKolayMuhasebeService, KolayMuhasebeService>();`

**Muhasebe Kuralları:**
- Satış Faturası: Alıcılar(120) BORÇ → Satışlar(600) + Hesaplanan KDV(391) ALACAK
- Alış Faturası: Gider(770) + İndirilecek KDV(191) BORÇ → Satıcılar(320) ALACAK
- Masraf: Gider(7xx) + KDV(191) BORÇ → Kasa/Banka(100/102) ALACAK
- Tahsilat: Kasa/Banka(100/102) BORÇ → Alıcılar(120) ALACAK
- Ödeme: Satıcılar(320) BORÇ → Kasa/Banka(100/102) ALACAK
- Avans: Personel Avansları(195) BORÇ → Kasa/Banka(100/102) ALACAK
- Tevkifatlı Satış: Tevkifat Alacağı(136) ayrı satır BORÇ
- Tevkifatlı Alış: Sorumlu KDV(360) ayrı satır ALACAK

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/KolayMuhasebeModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IKolayMuhasebeService.cs` (yeni)
- `CRMFiloServis.Web/Services/KolayMuhasebeService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Muhasebe/KolayGiris.razor` (yeni)
- `CRMFiloServis.Web/Program.cs` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 047 - Excel'den Personel Import
**Talep:** Excel dosyasından toplu personel yükleme özelliği.

**Yapılanlar:**
- `ISoforService.cs` interface güncellendi:
  - `GetImportSablonAsync()` - Import şablonu oluşturma
  - `ImportFromExcelAsync(byte[] excelData, bool mevcutGuncelle)` - Excel'den import
  - `ExportToExcelAsync()` - Mevcut personelleri Excel'e export
  - `PersonelImportSonuc` sınıfı - Import sonuç bilgileri
  - `PersonelImportHata` sınıfı - Import hata detayları
- `SoforService.cs` güncellendi (~350 satır eklendi):
  - **Import Şablonu** (2 sayfa):
    - Personel Import sayfası: 17 kolon (Ad*, Soyad*, TC, Telefon, Email, Adres, Görev, Departman, Pozisyon, İşe Başlama, Brüt Maaş, Net Maaş, SGK, Bordro Tipi, Banka, IBAN, Notlar)
    - Açıklamalar sayfası: Kullanım kılavuzu
    - Zorunlu alanlar kırmızı arka planla işaretli
    - Örnek veri satırı mevcut
  - **Import İşlemi**:
    - TC Kimlik No ile mevcut personel kontrolü
    - `mevcutGuncelle` flag'i ile güncelleme seçeneği
    - Satır satır hata yakalama (kritik/uyarı ayrımı)
    - Görev otomatik parse (Sofor, OfisCalisani, Muhasebe, vb.)
    - Tarih formatı desteği (dd.MM.yyyy, dd/MM/yyyy, yyyy-MM-dd)
    - Bordro tipi otomatik ayırma (Normal/Arge)
    - SGK flag'i otomatik set
  - **Export İşlemi**: Mevcut personel listesini Excel'e aktarma
- `SoforList.razor` güncellendi:
  - Header'a buton grubu eklendi:
    - 📥 Şablon İndir butonu
    - 📤 Excel Import butonu
    - 📊 Excel Export butonu
  - **Import Modal** (~120 satır):
    - Dosya seçici (InputFile, max 5MB)
    - "Mevcut personelleri güncelle" checkbox
    - İşlem sonuç kartları (toplam, eklenen, güncellenen, atlanan)
    - Hata/uyarı listesi (max 20 gösterim)
  - Import değişkenleri ve metodlar eklendi

**Özellikler:**
- ✅ TC Kimlik ile duplikasyon kontrolü
- ✅ Mevcut personel güncelleme seçeneği
- ✅ SGK'lı normal/AR-GE otomatik ayırma
- ✅ Detaylı hata raporlama
- ✅ Şablon indirme (örnek verili)
- ✅ Mevcut liste export

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/ISoforService.cs` (güncellendi)
- `CRMFiloServis.Web/Services/SoforService.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/Soforler/SoforList.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 046 - Yevmiye Kayıtları Yazdır ve Excel Export
**Talep:** Tarih bazlı yevmiye kayıtlarını yazdırma ve Excel'e export etme özelliği.

**Yapılanlar:**
- `YevmiyeRaporu.razor` oluşturuldu (~320 satır):
  - **Route**: `/muhasebe/raporlar/yevmiye`
  - **URL Parametreleri**: `baslangic`, `bitis`, `yazdir` (otomatik yazdırma için)
  - **Özet Kartları**: Kayıt sayısı, toplam borç, toplam alacak, bakiye farkı
  - **İki Görünüm Modu**:
    - Fiş Bazlı: Her fiş ayrı kart, kalemler tablo
    - Satır Bazlı: Tüm kayıtlar tek tabloda
  - **Yazdır Butonu**: `window.print()` ile browser yazdırma
  - **Excel Export**: `ExportYevmiyeToExcelAsync` metodu kullanılıyor
  - **Zirve Export**: Zirve Muhasebe Programı formatında export
  - **Print CSS**: `@media print` ile yazdırma optimizasyonu
- `MuhasebeFisler.razor` güncellendi:
  - Header'a Yazdır ve Excel butonları eklendi
  - `YevmiyeYazdir()` metodu eklendi - seçili yıl/aya göre yazdırma sayfasına yönlendirme
  - `YevmiyeExcelExport()` metodu eklendi - seçili yıl/aya göre Excel export
- **Mevcut Servis Metodları** (zaten implemente):
  - `GetYevmiyeRaporuAsync(baslangic, bitis)` - Yevmiye raporu oluşturma
  - `ExportYevmiyeToExcelAsync(baslangic, bitis)` - Excel export
  - `ExportZirveFormatAsync(baslangic, bitis)` - Zirve formatı export
  - `GetYevmiyeYazdirDataAsync(baslangic, bitis)` - JSON yazdırma verisi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Muhasebe/YevmiyeRaporu.razor` (yeni)
- `CRMFiloServis.Web/Components/Pages/Muhasebe/MuhasebeFisler.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 045 - Personel Düzenleme BUG Fix
**Talep:** Personel düzenleme işlemi veritabanına kaydedilmiyor hatası.

**Yapılanlar:**
- **Sorun Tespit:**
  - `Program.cs` satır 93: `options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);` global ayar
  - Bu ayar nedeniyle Entity Framework değişiklikleri takip etmiyordu
  - `SoforService.UpdateAsync` metodu `FindAsync` ile entity alıyordu ama NoTracking nedeniyle değişiklikler kaydedilmiyordu
- **Çözüm:**
  - `SoforService.UpdateAsync` metodu düzeltildi:
    - `FindAsync` yerine `FirstOrDefaultAsync` kullanıldı
    - Tüm alan güncellemeleri sonrası `_context.Soforler.Update(existing)` çağrısı eklendi
    - Bu sayede global NoTracking ayarına rağmen entity explicit olarak güncelleniyor
  - `SoforService.DeleteAsync` metodu da aynı şekilde düzeltildi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/SoforService.cs` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 044 - Cari Risk Analizi Modülü (AI Destekli Tahsilat Risk Takibi)
**Talep:** Cari hesap bazlı risk analizi: vadesi geçmiş alacak takibi, yaşlandırma analizi (0-30, 31-60, 61-90, 90+ gün), risk skorlaması, AI destekli tahsilat stratejisi önerileri.

**Yapılanlar:**
- `CariRiskModels.cs` oluşturuldu - Risk analizi DTO'ları:
  - `CariRiskOzet`: Toplam cari sayısı, vadesi geçmiş cari/toplam, riskli cari sayısı, ortalama tahsilat süresi, açık alacak/borç
  - `CariRiskKarti`: Cari risk kartı (skor, seviye, borc/alacak, yaşlandırma 4 dönem, son 1 yıl ciro, tahsilat süresi, vade uyum oranı, son işlem tarihleri, risk faktörleri listesi)
  - `RiskSeviyesi` enum: DusukRisk(0-25), OrtaRisk(26-50), YuksekRisk(51-75), KritikRisk(76+)
  - `VadesiGecmisFatura`: Vadesi geçmiş fatura detayları (gecikme gün sayısı dahil)
  - `RiskTrendItem`: Aylık risk trend verisi
  - `CariRiskFilterParams`: Filtreleme parametreleri (cari tipi, risk seviyesi, min gecikme, min tutar, sıralama)
- `ICariRiskService.cs` interface oluşturuldu - 10 metot:
  - `GetRiskOzetAsync`: Genel risk özeti
  - `GetRiskKartlariAsync`: Filtrelenebilir risk kartları listesi
  - `GetCariRiskKartiAsync`: Tek cari risk kartı
  - `GetVadesiGecmisFaturalarAsync`: Vadesi geçmiş fatura listesi
  - `GetToplamVadesiGecmisBorcAsync`: Toplam vadesi geçmiş borç
  - `GetRiskTrendAsync`: Aylık trend verisi
  - `HesaplaRiskSkoruAsync`: Tek cari risk skoru
  - `RecalculateAllRiskScoresAsync`: Toplu yeniden hesaplama
  - `GetAIRiskAnaliziAsync`: Tek cari AI analizi
  - `GetTopluAIRiskAnaliziAsync`: Firma geneli AI analizi
- `CariRiskService.cs` implementasyon oluşturuldu (~460 satır):
  - **Risk Skoru Algoritması** (0-100):
    - Vadesi geçmiş borç etkisi (0-40 puan): >100K=40, >50K=30, >20K=20, >5K=10, <5K=5
    - Gecikme süresi etkisi (0-30 puan): >90 gün=30, >60=20, >30=10, >0=5
    - Ortalama tahsilat süresi (0-15 puan): >60 gün=15, >45=10, >30=5
    - Vade uyum oranı (0-15 puan): <%30=15, <%50=10, <%70=5
    - Düşük ciro bonusu: <10K ciro + vadesi geçmiş = +5 puan
  - **Yaşlandırma Analizi**: FaturaYonu.Giden + VadeTarihi nullable kontrol
  - **Risk Seviyesi**: <=25 Düşük, <=50 Orta, <=75 Yüksek, >75 Kritik
  - **AI Risk Analizi**: Ollama ile bireysel/toplu risk değerlendirmesi ve strateji önerileri
  - **Trend Analizi**: Son 12 ay vadesi geçmiş borç ve riskli cari trend
- `CariRiskAnalizi.razor` oluşturuldu (~500 satır):
  - **Özet Kartları**: Toplam cari, vadesi geçmiş, riskli cari, toplam vadesi geçmiş borç (renkli kartlar)
  - **Risk Dağılımı**: 4 seviye yüzde bar grafiği
  - **Yaşlandırma Özeti**: 4 dönem (0-30, 31-60, 61-90, 90+) tutarları
  - **AI Genel Analiz**: Toplu risk analizi butonu
  - **Risk Listesi**: Filtrelenebilir/sıralanabilir cari tablosu (seviye badge, yaşlandırma bar)
  - **Cari Detay Modal**: Risk kartı, yaşlandırma dağılımı, risk faktörleri, AI analiz
- `Program.cs` güncellendi - ICariRiskService DI kaydı eklendi
- `NavMenu.razor` güncellendi - Cari modülüne "Risk Analizi" linki eklendi
- **Build Hataları Düzeltildi**:
  - FaturaYonu.Gelir → FaturaYonu.Giden (Giden=Kesilen=Gelir faturası)
  - Cari.ToplamBorc/ToplamAlacak → Cari.Borc/Alacak
  - Cari.Bakiye → hesaplanmış (Borc - Alacak)
  - BankaHareketleri → OdemeEslestirmeleri (doğru tablo)
  - CariIletisimNotlari → CariIletisimNotlar (doğru isim)
  - Fatura.ToplamTutar → Fatura.GenelToplam
  - OdemeEslestirme.Tarih → EslestirmeTarihi
  - VadeTarihi nullable (DateTime?) → .Value kontrolü
  - AnalizYapAsync(prompt) → AnalizYapAsync(prompt, sistemPrompt)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/CariRiskModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/ICariRiskService.cs` (yeni)
- `CRMFiloServis.Web/Services/CariRiskService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Cariler/CariRiskAnalizi.razor` (yeni)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 043 - İhale Hazırlık Modülü (AI Destekli Maliyet Analizi)
**Talep:** Personel servisi firmasının süresine göre, güzergah/mesafe, özmal/kiralık/komisyon durumlarında, araç model ve yakıt ortalamasına göre araç masrafları AI tahmini (kullanıcı değiştirebilsin), şoför maaş AI tahmini (geriye dönük ve enflasyon), aylık/saatlik/sefer başı birim fiyatlar, kâr/zarar/masraf tablosu. Sınırsız proje oluşturma.

**Yapılanlar:**
- `IhaleHazirlik.cs` entity oluşturuldu:
  - `IhaleProje`: ProjeKodu (IHL-YYYY-NNNN), ProjeAdi, CariId, FirmaId, BaslangicTarihi, BitisTarihi, SozlesmeSuresiAy, EnflasyonOrani, YakitZamOrani, AylikCalismGunu, GunlukCalismaSaati, Durum, AIAnaliz
  - `IhaleGuzergahKalem`: Hat bilgileri (ad/başlangıç/bitiş/mesafe/süre/sefer), araç bilgileri (sahiplik/model/koltuk/yakıt), 7 masraf kategorisi, kira/komisyon, şoför (brüt/net/SGK %22.5), amortisman, maliyet/kâr/teklif hesaplamaları, birim fiyatlar (aylık/sefer/saat/km)
  - `AylikProjeksiyon`: Enflasyonlu aylık maliyet projeksiyon detayları
  - `IhaleProjeDurum` enum: Taslak/Hazirlaniyor/TeklifVerildi/Kazanildi/Kaybedildi/IptalEdildi
  - `AracSahiplikKalem` enum: Ozmal/Kiralik/Komisyon
- `IhaleHazirlikModels.cs` oluşturuldu - DTO'lar:
  - `IhaleMaliyetTahminIstek/Sonuc`: AI masraf tahmin request/response
  - `IhaleSoforMaasTahmin`: Brüt/net/SGK/toplam/enflasyonlu maaş tahmini
  - `IhaleProjeOzet`: Proje toplamları + kalem özetleri + aylık projeksiyon
- `IIhaleHazirlikService.cs` interface oluşturuldu - 17 metot
- `IhaleHazirlikService.cs` implementasyon oluşturuldu (~470 satır):
  - **Proje CRUD**: Auto ProjeKodu (IHL-2025-0001), deep copy kopyalama
  - **Kalem CRUD**: Güzergah/araç/şoför bilgi otomatik aktarımı
  - **Maliyet Hesaplama**: Yakıt (mesafe×sefer×tüketim×fiyat), komisyon, SGK %22.5, amortisman, toplam, kâr, teklif, birim fiyatlar
  - **Enflasyonlu Projeksiyon**: Bileşik faiz formülü, yakıt ayrı zam oranı, amortisman sabit
  - **AI Araç Masraf Tahmini**: Gerçek masraf DB ortalaması + Ollama JSON prompt → 7 masraf kalemi
  - **AI Şoför Maaş Tahmini**: Mevcut şoför ortalaması + asgari ücret + Ollama → brüt/net/SGK/enflasyonlu
  - **AI Proje Analizi**: Proje özet → Ollama stratejik analiz (kâr marjı, risk, rekabet, öneri)
- `IhaleHazirlik.razor` oluşturuldu (~700 satır):
  - **Proje Listesi**: Kart grid, durum badge'leri (renk kodlu), düzenle/kopyala/sil
  - **Proje Detay**: Bilgi kartları, hat tablosu, birim fiyat kartları
  - **Proje Modal**: Tüm proje bilgileri CRUD formu
  - **Kalem Modal**: Güzergah + araç + masraflar + şoför + kâr marjı, AI Tahmin butonları
  - **Rapor**: Enflasyonlu projeksiyon tablosu, kümülatif hesap, toplam kartları
- `ApplicationDbContext.cs` güncellendi - IhaleProjeleri, IhaleGuzergahKalemleri DbSet eklendi
- `Program.cs` güncellendi - IIhaleHazirlikService DI kaydı eklendi
- `NavMenu.razor` güncellendi - İhale Hazırlık menü bölümü eklendi
- EF Core migration oluşturuldu (IhaleHazirlikModulu)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/IhaleHazirlik.cs` (yeni)
- `CRMFiloServis.Web/Models/IhaleHazirlikModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IIhaleHazirlikService.cs` (yeni)
- `CRMFiloServis.Web/Services/IhaleHazirlikService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Ihale/IhaleHazirlik.razor` (yeni)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 042 - AI Destekli Fatura Import ve Cari Geliştirme
**Talep:** Cari modülden kesilen/gelen faturaları XML yüklerken yapay zeka ile cari kart kontrolü, güzergah eşleştirme, stok kartı kontrolü, kalem sınıflandırma ve puantaj entegrasyonu.

**Yapılanlar:**
- `FaturaAIImportModels.cs` oluşturuldu - AI fatura import DTO'ları:
  - `FaturaAIAnalizSonuc`: Fatura bilgileri, satıcı/alıcı, cari eşleşme, kalemler, AI yorum
  - `FaturaAICariBilgi`: Unvan, VergiNo, TcKimlikNo, VergiDairesi, Adres, İl/İlçe
  - `CariEslesmeSonuc`: Mevcut/yeni cari, eşleşme yöntemi (VergiNo/TcKimlikNo/Unvan)
  - `FaturaAIKalem`: AI kalem tipi, alt tipi, güven skoru, kullanıcı düzeltme, güzergah/stok eşleşme
  - `GuzergahEslesmeSonuc`, `StokEslesmeSonuc`: Benzer kayıtlar, otomatik eşleşme
- `IFaturaAIImportService.cs` oluşturuldu - 7 metot interface
- `FaturaAIImportService.cs` oluşturuldu (~550 satır):
  - **XML Parse**: UBL 2.1 e-fatura formatı (cbc/cac namespace), satıcı/alıcı party, kalemler, tevkifat, vade
  - **Cari Eşleştirme**: VergiNo → TcKimlikNo → Unvan tam → Unvan kısmi → yeni oluştur
  - **AI Kalem Sınıflandırma**: Ollama ile JSON format sınıflandırma (Hizmet/Mal/Kiralama/Servis)
  - **Güzergah Eşleştirme**: Kelime benzerlik skoru, cari bonus +20%, >70% otomatik eşleşme
  - **Stok Eşleştirme**: Ürün kodu tam eşleşme, açıklama benzerlik
  - **Kaydet**: Transaction (cari → güzergah → fatura+kalemler → güzergah FaturaKalemId güncelle)
  - UBL birim kodları normalizasyonu (C62→Adet, KGM→Kg, LTR→Lt, HUR→Saat vb.)
- `FaturaAIImport.razor` oluşturuldu (~550 satır) - 4 adımlı wizard:
  - Adım 1: XML dosya yükleme (boyut kontrolü, AI bağlantı uyarısı)
  - Adım 2: Cari kontrol (mevcut eşleşme/yeni oluşturma/farklı cari seçme)
  - Adım 3: Kalem analizi tablosu (AI tipi, kullanıcı düzeltme, güzergah/stok eşleşme dropdown)
  - Adım 4: Kaydet sonucu ve yönlendirme
- `CariService.cs` güncellendi - İletişim notu, hatırlatıcı ve vade uyarı implementasyonları:
  - `GetIletisimNotlariAsync`, `AddIletisimNotuAsync`, `UpdateIletisimNotuAsync`, `DeleteIletisimNotuAsync`
  - `GetCariHatirlaticilariAsync`, `AddCariHatirlaticiAsync`
  - `GetVadeUyarilariAsync` (kritik/gecikmiş/bugün/yaklaşan vade sınıflandırma)
- `NavMenu.razor` güncellendi - Fatura menüsüne "AI Fatura Import" linki eklendi
- `Program.cs` güncellendi - `IFaturaAIImportService` DI kaydı eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/FaturaAIImportModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IFaturaAIImportService.cs` (yeni)
- `CRMFiloServis.Web/Services/FaturaAIImportService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Cariler/FaturaAIImport.razor` (yeni)
- `CRMFiloServis.Web/Services/CariService.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncellendi)
- `CRMFiloServis.Web/Program.cs` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 040 - AI Destekli Muhasebeleştirme ve Puantaj Analizi
**Talep:** Muhasebeleştirme ve puantaj kısımlarında yapay zeka desteği ile öneri, tahmin, kontrol bulguları sunma ve kullanıcıya aksiyon alma imkanı.

**Yapılanlar:**
- `OllamaService.cs` güncellendi - `AnalizYapAsync(prompt, sistemPrompt)` metodu eklendi:
  - Özelleştirilebilir sistem prompt desteği
  - 2048 token yanıt limiti (rapor yorumlamadan daha uzun)
  - `IOllamaService` interface'e yeni metot eklendi
- `MuhasebeleştirmeModels.cs` güncellendi - `AIAksiyon` ve `PuantajAIAksiyon` sınıfları eklendi
- `Muhasbelestirme.razor` - AI muhasebe analizi eklendi:
  - **AI Analiz butonu** fatura ve masraf sekmelerinde (mor renkli, robot ikonu)
  - **AI Analiz Modalı**: Ollama model adı göstergesi, analiz süresi, temizleme
  - **Fatura AI Analizi**: Kontrol bulgularını + fatura detaylarını AI'ya gönderir
    - Tutarlılık analizi (aynı cariye birden fazla fatura, olağandışı tutarlar)
    - KDV ve tevkifat doğruluğu kontrolü
    - Vergisel risk uyarıları
    - Muhasebe kaydı oluşturma önerileri
  - **Masraf AI Analizi**: Kategori dağılımı + araç bazlı dağılım + kontrol bulgularını AI'ya gönderir
    - Anomali tespiti (olağandışı tutarlar, sık tekrarlar)
    - Gider hesap eşleştirme önerileri (770.06, 770.07 vb.)
    - Maliyet optimizasyonu önerileri
  - **Aksiyon Listesi**: AI yanıtından otomatik parse edilen YÜKSEK/ORTA/DÜŞÜK öncelikli aksiyonlar
    - Checkbox ile seçilebilir aksiyonlar
    - Renkli öncelik badge'leri
- `CalismaPuantaji.razor` - AI puantaj analizi eklendi:
  - **AI Analiz butonu** toolbar'da (mor renkli)
  - **AI Puantaj Analiz Modalı**: Ay/yıl bilgisi, personel sayısı göstergesi
  - **Puantaj AI Analizi**: Personel bazlı detay + günlük dağılım + fazla mesai detayı gönderir
    - Devamsızlık pattern analizi (sık izin/mazeret, ardışık günler)
    - Fazla mesai analizi (İş Kanunu uygunluğu: haftalık 45 saat, aylık 270 saat)
    - Anomali tespiti (belirli günlerde toplu izin, olağandışı çalışma düzeni)
    - Verimlilik değerlendirmesi
    - Gelecek ay tahmini (trend bazlı devamsızlık/fazla mesai beklentisi)
  - **Aksiyon Listesi**: Aynı parse mekanizması ile öncelikli aksiyonlar

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/OllamaService.cs` (güncellendi - AnalizYapAsync eklendi)
- `CRMFiloServis.Web/Models/MuhasebeleştirmeModels.cs` (güncellendi - AIAksiyon, PuantajAIAksiyon)
- `CRMFiloServis.Web/Components/Pages/Muhasebe/Muhasbelestirme.razor` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/Personel/CalismaPuantaji.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 039 - Fatura/Masraf Muhasebeleştirme Geliştirme
**Talep:** Fatura ve masraf muhasebeleştirme sayfasına kontrol listesi, işlenmiş kayıtlar görüntüleme, geri alma ve Excel export özellikleri ekle.

**Yapılanlar:**
- `MuhasebeleştirmeModels.cs` güncellendi - 3 yeni model + 1 enum eklendi:
  - `MuhasbelestirmeKontrol`: Kontrol sonucu (HazirMi, Maddeler, UyariSayisi, HataSayisi, BilgiSayisi)
  - `KontrolMaddesi`: Kontrol maddesi detayı (Baslik, Aciklama, Seviye, IlgiliKayit)
  - `KontrolSeviye` enum: Bilgi, Uyari, Hata
  - `MuhasbelestirilmisKayit`: İşlenmiş kayıt DTO (KaynakId, KaynakTip, Tutar, FisId, FisNo, Secildi)
- `IMuhasebeService.cs` güncellendi - 4 yeni metot:
  - `KontrolYapAsync`: Muhasebeleştirme öncesi kontrol (hesap planı, ayarlar, dönem, cari, tevkifat, hesap eşleşme)
  - `GetMuhasbelestirilmisKayitlarAsync`: İşlenmiş fatura+masraf birleşik liste (fiş bilgileriyle)
  - `TopluGeriAlAsync`: Fiş silme + fatura/masraf muhasebeleştirme durumu geri alma
  - `ExportMuhasbelestirmeKontrolExcelAsync`: 3 sayfalı Excel export (Faturalar/Masraflar/Kontrol Listesi)
- `MuhasebeService.cs` güncellendi (~300 satır yeni kod):
  - `KontrolYapAsync`: Hesap planı boş mu, muhasebe ayarları var mı, aktif dönem var mı, fatura cari eksik mi, tevkifatlı fatura var mı, 120/320/770 hesapları tanımlı mı
  - `GetMuhasbelestirilmisKayitlarAsync`: Fatura (MuhasebeFisiOlusturuldu=true) ve masraf (MuhasebeFisId!=null) birleşik listesi, fiş numaraları dahil
  - `TopluGeriAlAsync`: Fiş kalemleri + fiş silme, ilişkili fatura/masraf muhasebe bağlantısı temizleme
  - `ExportMuhasbelestirmeKontrolExcelAsync`: ClosedXML ile 3 sayfalı Excel (koşullu renklendirme)
- `Muhasbelestirme.razor` tamamen güncellendi:
  - **İşlenmiş Kayıtlar Sekmesi** (yeni): Fatura/Masraf filtre, fiş detayına link, toplu geri alma
  - **Kontrol Listesi Modalı** (yeni): Hata/Uyarı/Bilgi kartları, madde listesi, kontrol sonrası muhasebeleştirmeye devam
  - **Excel Export butonları**: Fatura/masraf kontrol listesi Excel indirme
  - **Geri Alma**: Seçili işlenmiş kayıtların muhasebe fişlerini silip durumu geri alma
  - **Gelişmiş buton grubu**: "Kontrol Et", "Excel", "Muhasebeleştir" butonları her sekmede
- ROADMAP: #10 ve #11 tamamlandı olarak işaretlendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/MuhasebeleştirmeModels.cs` (güncellendi)
- `CRMFiloServis.Web/Services/Interfaces/IMuhasebeService.cs` (güncellendi)
- `CRMFiloServis.Web/Services/MuhasebeService.cs` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/Muhasebe/Muhasbelestirme.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

### Kayıt 035 - Bordro Personel Bazlı Düzenleme
**Talep:** Bordro detaylarında personel bazlı maaş/kesinti/ek ödeme düzenleme özelliği. Normal ve AR-GE bordrolarda ayrı ayrı.

**Yapılanlar:**
- `NormalBordro.razor`: Detay tablosuna "Düzenle" butonu ve tam düzenleme modalı eklendi
  - Maaş bilgileri: Brüt, Net, SGK Maaşı, Toplu Maaş, Ek Ödeme (fark)
  - Kesintiler: SGK+İşsizlik, Gelir Vergisi, Damga Vergisi
  - Ek Ödemeler: Yemek, Yol, Prim, Diğer
  - Notlar alanı, canlı toplam hesaplama (Toplam Kesinti, Toplam Ek Ödeme, Toplam Ödenecek)
  - Eski JS `prompt` düzenleme kaldırıldı, modal ile değiştirildi
  - Kalan Ödeme sekmesindeki düzenleme butonu da modalı kullanıyor
  - Onaylı bordrolarda düzenleme engellendi
- `ArgeBordro.razor`: Aynı düzenleme modalı ve buton yapısı eklendi (AR-GE etiketli)
- ROADMAP: #18 tamamlandı olarak işaretlendi

**Durum:** ✅ Tamamlandı

### Kayıt 036 - Bordro Hesap Pusulası
**Talep:** Bordro hesap pusulası - Personel bazlı yazdırılabilir maaş makbuzu (pay slip).

**Yapılanlar:**
- `HesapPusulasi.razor` oluşturuldu (`/personel/bordro/hesap-pusulasi`)
  - Filtre: Yıl, Firma, Bordro Tipi (Normal/AR-GE), Dönem seçimi
  - Personel listesi tablosu: checkbox seçim, maaş özet bilgileri
  - Tek personel / seçili personeller / tüm personeller yazdırma
  - A4 print-ready pusula formatı (CSS @media print):
    - Firma bilgileri (ünvan, adres, vergi dairesi/no)
    - Personel bilgileri (sicil no, TC, görev/departman, işe başlama, banka/IBAN)
    - Kazançlar: Brüt maaş, SGK matrah, net maaş, toplu maaş, ek ödeme farkı
    - Kesintiler: SGK+İşsizlik, gelir vergisi, damga vergisi, toplam
    - Ek ödemeler: Yemek, yol, prim, diğer, toplam
    - Ödeme durumu: Banka/Ek ödeme yapıldı/bekliyor
    - Toplam ödenecek tutar (büyük font, vurgulu)
    - İmza alanları (İşveren + Personel)
    - Onay bilgisi ve düzenlenme tarihi
  - Her personel ayrı sayfada (page-break-after) - toplu yazdırmada
- NavMenu'ya "Hesap Pusulası" linki eklendi (Bordro altına)
- ROADMAP: #19 tamamlandı olarak işaretlendi

**Durum:** ✅ Tamamlandı

### Kayıt 038 - Personel Excel Import
**Talep:** Excel dosyasından toplu personel yükleme (import) özelliği.

**Yapılanlar:**
- `PersonelImport.razor` oluşturuldu - Toplu personel Excel import sayfası
  - **Excel Şablon İndirme**: ClosedXML ile hazır şablon (16 kolon), örnek veriler ve açıklama sayfası
  - **Dosya Yükleme**: InputFile ile .xlsx yükleme (10MB limit)
  - **Önizleme**: Excel parse → satır bazlı durum tespiti (Yeni/Güncelleme/Atla/Hata)
  - **Mevcut Personel Kontrolü**: TC Kimlik No veya Ad+Soyad ile eşleşme tespiti
  - **Güncelleme Modu**: Checkbox ile mevcut personeli güncelleme opsiyonu
  - **Toplu Kaydetme**: ISoforService.CreateAsync/UpdateAsync ile yeni ekleme veya güncelleme
  - **Otomatik Kod Üretimi**: GenerateNextKodAsync(gorev) ile görev bazlı personel kodu
  - **Bordro Tipi Otomatik Ayarma**: Yok/Normal/Arge → SGKBordroDahilMi senkronizasyonu
  - **Durum Filtreleme**: Yeni/Güncelleme/Atlanan/Hatalı filtre dropdown
  - **Özet Kartları**: Toplam/Yeni/Güncelleme/Atla/Hata/İşlenecek sayıları
  - **Tarih/Para Birimi Parse**: Türkçe format desteği (GG.AA.YYYY, virgül/nokta)
  - **Görev Parse**: Şoför/Ofis/Muhasebe/Yönetici/Teknik/Diğer (case-insensitive, alias destekli)
- NavMenu'ya "Excel Import" linki eklendi (Personel bölümü)
- ROADMAP: #20 tamamlandı olarak işaretlendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Personel/PersonelImport.razor` (yeni)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor`

**Durum:** ✅ Tamamlandı

---

### Kayıt 037 - Bütçe Analiz Geliştirme + AI Rapor Yorumlama (Ollama)
**Talep:** Bütçe Analiz sayfasına kategori bazlı analiz, aylık trend grafikleri ve Ollama (internetsiz AI) ile akıllı rapor yorumlama ekle.

**Yapılanlar:**
- `OllamaService.cs` oluşturuldu - Local LLM entegrasyonu (Ollama REST API)
  - `IOllamaService` interface: `RaporYorumlaAsync`, `BaglantiKontrolAsync`
  - Ollama `/api/generate` endpoint kullanımı
  - Configurable model (appsettings: `Ollama:Model`, default: `llama3.2`)
  - Configurable base URL (default: `http://localhost:11434`)
  - Sistem promptu: Türk mali müşavir rolü, kısa/öz/aksiyona yönelik
  - 3 dakika timeout, hata yönetimi
- `appsettings.json`: Ollama konfigürasyonu eklendi
- `Program.cs`: HttpClient("Ollama") + IOllamaService DI kaydı
- `BudgetAnaliz.razor` güncellemeleri:
  - **Kategori Dağılımı paneli**: Progress bar'lı kategori listesi, yüzde oranları, toplam
  - **Aylık Harcama Trendi paneli**: Tablo + stacked progress bar (ödenen/bekleyen oranı), ortalama/en yüksek/en düşük ay istatistikleri
  - **AI Bütçe Analizi paneli**: Ollama bağlantı durumu göstergesi, 5 analiz türü (Genel/Kategori/Trend/Tasarruf/Anomali), analiz süresi göstergesi, sonuç temizleme
  - Dinamik prompt oluşturma: Bütçe özeti + Kategori dağılımı + Aylık trend + Kredi/taksit + Gecikmiş ödemeler otomatik derleniyor
  - OnInitializedAsync'te kategori/trend/Ollama bağlantı kontrolü
  - YenileDataAsync'te kategori/trend otomatik güncelleme
- ROADMAP: #12, #25, #26, #27 tamamlandı olarak işaretlendi

**Durum:** ✅ Tamamlandı

---

### Kayıt 034 - Toplu Ödeme Listesi Banka EFT Export
**Talep:** Banka ödeme listesine banka portalına yüklenebilir toplu ödeme (EFT/Havale) dosyası export özelliği.

**Yapılanlar:**
- `BankaOdemeListesi.razor`: Mevcut sayfaya "EFT Dosyası" butonu eklendi
  - Semicolon-delimited CSV formatı (Türk bankaları genel uyumu)
  - Header satırı: Tarih, toplam adet, toplam tutar, para birimi, açıklama
  - Veri satırları: IBAN, Ad Soyad, Tutar, Açıklama, Personel Kodu
  - IBAN’sız personel uyarısı (eksik IBAN bildirimi)
  - UTF-8 BOM encoding (Türkçe karakter desteği)
- ROADMAP: #17 tamamlandı olarak işaretlendi

**Durum:** ✅ Tamamlandı

---

### Kayıt 033 - Maaş Hareket Listesi
**Talep:** Personel maaş ödeme geçmişi görüntüleme sayfası. Tüm aylara ait maaş kayıtlarının filtrelenerek listelenmesi, detay görüntüleme ve Excel export.

**Yapılanlar:**
- `MaasHareketleri.razor`: Tam sayfa oluşturuldu (`/personel/maas-hareketleri`)
  - Personel, Yıl, Ay, Ödeme Durumu filtreleri
  - Özet kartları: Toplam Kayıt, Toplam Ödenen, Bekleyen, Genel Toplam
  - Detaylı hareket tablosu: Brüt/Net maaş, eklemeler, kesintiler, ödenecek, çalışma günü, ödeme durumu
  - Personel bazlı özet tablosu (tüm personel görünümünde)
  - Detay modalı: Dönem bilgisi, ödeme bilgisi, ek ödemeler, kesintiler detayı
  - Excel export (ClosedXML)
  - Toast bildirim sistemi
- NavMenu: "Maaş Hareketleri" linki eklendi (Personel menüsü altına)
- ROADMAP: #16 tamamlandı olarak işaretlendi

**Durum:** ✅ Tamamlandı

---

### Kayıt 032 - Personel Servis Çalışma Puantajı
**Talep:** Personel bazlı günlük/aylık çalışma puantaj takibi sayfası. Hangi gün çalıştı, izinli, mazeretli olduğu takip edilecek.

**Yapılanlar:**
- `CalismaPuantaji.razor`: Tam sayfa oluşturuldu (`/personel/puantaj`)
  - Firma + Yıl + Ay filtreleri
  - Personel arama (ad soyad / şoför kodu)
  - Özet kartlar: Personel sayısı, Ort. çalışılan gün, Toplam izin/mazeret, F. mesai, Net ödeme
  - Takvim grid tablosu (personel × gün matrisi):
    - Satırlar: Personel adı ve kodu
    - Sütunlar: Ayın günleri (1-28/30/31) + Ç/İ/M/FM özet sütunları
    - Haftasonu renklendirme (Cumartesi sarı, Pazar kırmızı)
    - Durum badge'leri: Ç (Çalıştı), İ (İzinli), M (Mazeret), FM (Fazla Mesai)
    - Sticky header ve sol sütun (kaydırma desteği)
  - Footer toplam satırı (günlük çalışan personel sayısı)
  - Hücre tıklama ile günlük düzenleme modalı:
    - Çalıştı/İzinli/Mazeret toggle (karşılıklı exclusive)
    - Fazla mesai saat girişi
    - Not alanı
  - "Ay Puantajı Oluştur" butonu (aktif şoförlere otomatik puantaj ve günlük kayıt oluşturma)
  - Günlük verilerden aylık özet otomatik hesaplama
  - "Hesapla" butonu (maaş kesintileri hesaplama)
  - Excel export (mevcut PuantajService.ExportPuantajListesiAsync kullanarak)
  - Toast bildirim sistemi, IDisposable implementasyonu
- `NavMenu.razor`: "Çalışma Puantajı" linki eklendi (Personel menüsü altına, İzin Yönetimi sonrası)
- `ROADMAP.md`: "Personel Servis Çalışma Puantajı" tamamlandı olarak işaretlendi

**Mevcut Altyapı Kullanımı:**
- `PersonelPuantaj` entity: Aylık özet (CalisilanGun, IzinGunu, MazeretGunu, FazlaMesaiSaat, maaş alanları)
- `GunlukPuantaj` entity: Günlük detay (Calisti, Izinli, Mazeret, FazlaMesaiSaat, ServisCalismaId)
- `IPuantajService / PuantajService`: CRUD, günlük puantaj, otomatik oluşturma, hesaplama, Excel export

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Personel/CalismaPuantaji.razor` (yeni)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncelleme)
- `ROADMAP.md` (güncelleme)

**Durum:** Tamamlandı

### Kayıt 031 - Fatura/Masraf Resmi Muhasebe Kaydı (Toplu Muhasebeleştirme)
**Talep:** Girilen fatura ve masrafların toplu olarak resmi yevmiye kaydı (muhasebe fişi) oluşturulması.

**Yapılanlar:**
- `MuhasebeleştirmeModels.cs`: DTO modeller oluşturuldu
  - `MuhasebeFaturaOzet`: Fatura özet bilgileri (seçim desteği ile)
  - `MuhasebeMasrafOzet`: Masraf özet bilgileri (seçim desteği ile)
  - `MuhasbelestirmeSonuc`: Toplu işlem sonucu (başarılı/hatalı sayısı, hatalar)
  - `MuhasbelestirmeDurum`: Genel durum özeti (bekleyen/işlenmiş sayıları)
- `IMuhasebeService.cs`: Yeni metotlar eklendi
  - GetMuhasbelestirmeDurumuAsync: Durum özeti
  - GetMuhasbelestirilmemisFaturalarAsync: Bekleyen fatura listesi (filtre desteği)
  - GetMuhasbelestirilmemisMasraflarAsync: Bekleyen masraf listesi (filtre desteği)
  - TopluFaturaMuhasbelestirAsync: Toplu fatura muhasebeleştirme
  - TopluMasrafMuhasbelestirAsync: Toplu masraf muhasebeleştirme
- `MuhasebeService.cs`: Implementasyon
  - Toplu fatura muhasebeleştirme (mevcut CreateFaturaFisiAsync kullanılarak)
  - Toplu masraf muhasebeleştirme (yeni CreateMasrafMuhasebeFisiAsync)
  - Masraf kategorisine göre gider hesabı eşleme (770.06-770.09)
  - Karşı hesap otomatik belirleme (cari/personel/kasa)
- `Muhasbelestirme.razor`: Tam sayfa oluşturuldu
  - Özet kartları (bekleyen/işlenmiş fatura ve masraf sayıları)
  - Tarih ve fatura yönü filtreleri
  - Fatura/Masraf sekme navigasyonu
  - Tümünü seç/kaldır, tekli seçim
  - Seçili toplamlar (footer)
  - Toplu muhasebeleştir butonu (loading state)
  - Sonuç modalı (başarılı/hatalı ayrıntılı gösterim)
  - Toast bildirimleri
- `NavMenu.razor`: "Muhasebeleştirme" linki eklendi (Muhasebe menüsü altına)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/MuhasebeleştirmeModels.cs` (güncelleme)
- `CRMFiloServis.Web/Services/Interfaces/IMuhasebeService.cs` (güncelleme)
- `CRMFiloServis.Web/Services/MuhasebeService.cs` (güncelleme)
- `CRMFiloServis.Web/Components/Pages/Muhasebe/Muhasbelestirme.razor` (yeni)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor` (güncelleme)

**Durum:** Tamamlandı

### Kayıt 030 - Maaş Yönetimi "Ödeme Yap" Butonu Bug Fix
**Talep:** Maaş yönetimi sayfasındaki "Ödeme Yap" butonu pasif durumda, düzgün çalışmıyordu.

**Yapılanlar:**
- `MaasYonetimi.razor`: Ödeme Yap butonu tamamen yeniden tasarlandı
  - Onay modalı eklendi (tarih seçimi, açıklama girişi)
  - Loading durumu eklendi (işlem sırasında spinner gösterimi)
  - Başarı/hata toast bildirimleri eklendi (3 sn otomatik kapanma)
  - Ödeme iptal etme özelliği eklendi (ödendi → bekliyor geri alma)
  - Ödendi durumunda ödeme tarihi tooltip olarak gösteriliyor
  - Toplu maaş oluşturma sonrası bildirim eklendi
  - Maaş kaydetme sonrası bildirim eklendi
  - `IDisposable` implementasyonu eklendi (timer temizliği)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Personel/MaasYonetimi.razor` (güncelleme)

**Durum:** Tamamlandı

### Kayıt 029 - Cari Borç/Alacak Detaylı Takip
**Talep:** Cari hesaplar için detaylı borç/alacak analizi, risk skorlaması ve tahsilat planlaması.

**Yapılanlar:**
- `CariHareketTakipModels.cs`: Yeni modeller oluşturuldu
  - `CariHareketTakipRapor`: Tek cari detaylı rapor (bakiye, vade analizi, risk skoru, trend)
  - `CariHareketDetay`: Hareket listesi (fatura + ödeme birleşik)
  - `CariAcikFatura`: Açık fatura detayı (vade durumu, öncelik)
  - `CariAylikTrend`: Aylık trend verisi
  - `TahsilatPlanItem`: Tahsilat planı öğesi
  - `CariBorcAlacakOzet`: Tüm cariler özet raporu
  - `CariHareketTakipOzet`: Cari özet satırı
  - `CariTipiBakiyeDagilimi`, `GenelAylikTrend`
- `ICariHareketTakipService.cs`: Interface oluşturuldu
  - GetBorcAlacakOzetAsync: Tüm cariler özet
  - GetCariDetayAsync: Tek cari detay
  - GetCariHareketlerAsync: Hareket listesi
  - GetAcikFaturalarAsync: Açık faturalar
  - GetAylikTrendAsync: Aylık trend
  - HesaplaRiskSkoruAsync: Risk skoru hesaplama
  - OlusturTahsilatPlaniAsync: Tahsilat planı
  - ExportToExcelAsync: Excel export
- `CariHareketTakipService.cs`: Tam implementasyon
  - Risk skoru hesaplama (0-100 arası)
  - Vade analizi (0-30, 31-60, 61-90, 90+ gün)
  - Ortalama ödeme süresi hesaplama
  - Tahsilat planı öneri sistemi
- `CariHareketTakip.razor`: Ana sayfa oluşturuldu
  - Tüm cariler özet görünümü (filtreler, vade analizi, cari tipi dağılımı)
  - Tek cari detay görünümü (bilgiler, bakiye, risk, hareket listesi, açık faturalar, tahsilat planı)
  - Hareket detay modal
  - Excel export
- `Program.cs`: Servis DI kaydı eklendi
- `NavMenu.razor`: "Borç/Alacak Takip" linki eklendi (Cari Modülü altına)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/CariHareketTakipModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/ICariHareketTakipService.cs` (yeni)
- `CRMFiloServis.Web/Services/CariHareketTakipService.cs` (yeni)
- `CRMFiloServis.Web/Components/Pages/Cariler/CariHareketTakip.razor` (yeni)
- `CRMFiloServis.Web/Program.cs`
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor`

**Durum:** Tamamlandı

### Kayıt 028 - Proforma Fatura Sistemi
**Talep:** Fatura kesilmeden önce müşteriye proforma fatura gönderme sistemi.

**Yapılanlar:**
- `ProformaFatura.cs`: Yeni entity oluşturuldu (ProformaFatura, ProformaFaturaKalem sınıfları)
- `ProformaDurum` enum eklendi (Taslak, Gonderildi, Onaylandi, Reddedildi, FaturayaDonusturuldu, SuresiDoldu)
- `ApplicationDbContext.cs`: DbSet ve OnModelCreating konfigürasyonları eklendi
- `IProformaFaturaService.cs`: Interface oluşturuldu
- `ProformaFaturaService.cs`: Tam implementasyon
  - CRUD işlemleri
  - Numara otomatik üretimi (PRF-YYYYMM-XXXX)
  - Faturaya dönüştürme (FaturayaDonusturAsync)
  - Süresi dolan proformaları güncelleme
  - Excel export desteği
- `Program.cs`: Servis DI kaydı eklendi
- `ProformaList.razor`: Liste sayfası oluşturuldu
  - Arama, filtreleme (durum, tarih aralığı)
  - İstatistik kartları (toplam, onaylanan, bekleyen, reddedilen)
  - Hızlı işlem butonları (faturaya dönüştür, onayla, reddet)
- `ProformaForm.razor`: Form sayfası oluşturuldu
  - Cari seçimi
  - Kalem ekleme/silme
  - KDV ve toplam otomatik hesaplama
  - Geçerlilik tarihi
- `ProformaDetay.razor`: Detay sayfası oluşturuldu
  - Durum badge'leri
  - Kalem listesi
  - İşlem butonları (onayla, reddet, faturaya dönüştür, Excel)
- `NavMenu.razor`: Proforma Faturalar linki eklendi
- Migration oluşturuldu: `AddProformaFatura`

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/ProformaFatura.cs` (yeni)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs`
- `CRMFiloServis.Web/Services/Interfaces/IProformaFaturaService.cs` (yeni)
- `CRMFiloServis.Web/Services/ProformaFaturaService.cs` (yeni)
- `CRMFiloServis.Web/Program.cs`
- `CRMFiloServis.Web/Components/Pages/ProformaFaturalar/ProformaList.razor` (yeni)
- `CRMFiloServis.Web/Components/Pages/ProformaFaturalar/ProformaForm.razor` (yeni)
- `CRMFiloServis.Web/Components/Pages/ProformaFaturalar/ProformaDetay.razor` (yeni)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor`

**Durum:** Tamamlandı

### Kayıt 024 - Şoför Performans Raporu
**Talep:** Şoförlerin performansını analiz eden detaylı rapor sayfası oluşturulması.

**Yapılanlar:**
- `SoforPerformansRaporModels.cs`: Yeni rapor modelleri oluşturuldu (SoforPerformansOzet, SoforAracPerformansi, SoforGuzergahPerformansi, SoforAylikPerformans, SoforKarsilastirmaOzeti)
- `IRaporService.cs`: 2 yeni metod eklendi (GetSoforPerformansAsync, GetSoforKarsilastirmaAsync)
- `RaporService.cs`: Şoför performans metodları implementasyonu eklendi
- `SoforPerformansRapor.razor`: Şoför performans raporu sayfası oluşturuldu
  - Bireysel şoför detaylı performans özeti
  - Tüm şoförler karşılaştırma tablosu
  - Özet kartları (toplam sefer, kazanç, çalışılan gün, arıza oranı)
  - Aylık performans grafiği (Chart.js entegrasyonu)
  - Araç ve güzergah bazlı analiz tabloları
  - Excel export desteği
- `NavMenu.razor`: Raporlar menüsüne "Şoför Performans" linki eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/SoforPerformansRaporModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IRaporService.cs`
- `CRMFiloServis.Web/Services/RaporService.cs`
- `CRMFiloServis.Web/Components/Pages/Raporlar/SoforPerformansRapor.razor` (yeni)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor`

**Durum:** Tamamlandı

### Kayıt 025 - Araç Karlılık Raporu
**Talep:** Araç bazlı karlılık analizi raporu - gelir/gider/kar hesaplama, masraf dağılımı, aylık trend grafiği.

**Yapılanlar:**
- `AracKarlilikRaporModels.cs`: Yeni rapor modelleri oluşturuldu (AracKarlilikOzet, AracMasrafDetay, AracAylikKarlilik, AracGuzergahPerformansi, AracKarsilastirmaOzeti)
- `IRaporService.cs`: 2 yeni metod eklendi (GetAracKarlilikAsync, GetAracKarsilastirmaAsync)
- `RaporService.cs`: Araç karlılık metodları implementasyonu eklendi
  - Gelir hesaplama: ServisCalisma.HesaplananFiyat
  - Gider hesaplama: AracMasraf.Tutar + KiraBedeli + Komisyon
  - Kiralık araçlar için aylık kira bedeli hesaplama
  - Komisyonlu araçlar için oran veya sabit komisyon hesaplama
- `AracKarlilikRapor.razor`: Araç karlılık raporu sayfası oluşturuldu
  - Tekil araç detaylı karlılık analizi
  - Tüm araçlar karşılaştırma tablosu
  - Özet kartları (gelir, gider, net kar, arıza oranı)
  - Aylık karlılık grafiği (multi-bar chart - gelir/gider/kar)
  - Masraf dağılımı doughnut chart
  - Güzergah bazlı performans tablosu
  - Masraf detay tablosu
  - Excel export desteği
- `NavMenu.razor`: Raporlar menüsüne "Araç Karlılık" linki eklendi
- `dashboard-charts.js`: createMultiBarChart fonksiyonu eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/AracKarlilikRaporModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IRaporService.cs`
- `CRMFiloServis.Web/Services/RaporService.cs`
- `CRMFiloServis.Web/Components/Pages/Raporlar/AracKarlilikRapor.razor` (yeni)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor`
- `CRMFiloServis.Web/wwwroot/js/dashboard-charts.js`

**Durum:** Tamamlandı

### Kayıt 026 - Cari Bakiye Yaşlandırma Raporu
**Talep:** Cari bakiyelerin vade tarihine göre yaşlandırma analizi - 0-30, 31-60, 61-90, 90+ gün bantları.

**Yapılanlar:**
- `CariYaslandirmaRaporModels.cs`: Yeni rapor modelleri oluşturuldu
  - `CariYaslandirmaRapor`: Genel rapor özeti (toplam bakiye, bant toplamları, cari sayıları)
  - `CariYaslandirmaOzet`: Cari bazlı yaşlandırma özeti (bakiye bantları, risk seviyesi)
  - `YaslandirmaBandi`: Yaşlandırma bandı özeti (tutar, fatura/cari sayısı, oran)
  - `CariTipiDagilimi`: Cari tipi bazlı dağılım
  - `YaslandirmaFaturaDetay`: Fatura bazlı yaşlandırma detayı
- `IRaporService.cs`: 2 yeni metod eklendi
  - `GetCariYaslandirmaAsync`: Genel yaşlandırma raporu
  - `GetCariYaslandirmaDetayAsync`: Tek cari detaylı yaşlandırma
- `RaporService.cs`: Yaşlandırma metodları implementasyonu eklendi
  - Vade tarihine göre gecikme günü hesaplama
  - Yaşlandırma bantlarına dağıtım (0-30, 31-60, 61-90, 90+ gün)
  - Risk seviyesi hesaplama (Normal, Düşük, Orta, Yüksek)
  - Cari tipi ve fatura bazlı gruplama
- `CariYaslandirmaRapor.razor`: Cari yaşlandırma raporu sayfası oluşturuldu
  - Filtre alanı (rapor tarihi, cari tipi, cari seçimi, sadece borçlu cariler)
  - Özet kartları (toplam bakiye, güncel, vadesi geçmiş, kritik)
  - Pie chart: Yaşlandırma dağılımı
  - Horizontal bar chart: Yaşlandırma bantları
  - Bant özet tablosu (tutar, fatura/cari sayısı, oran, progress bar)
  - Cari bazlı detay tablosu (risk seviyesi renklendirmeli)
  - Fatura detay modal (cari tıklandığında)
  - Excel export desteği
- `NavMenu.razor`: Raporlar menüsüne "Cari Yaşlandırma" linki eklendi
- `dashboard-charts.js`: 2 yeni fonksiyon eklendi
  - `createPieChart`: Pasta grafik
  - `createYaslandirmaBarChart`: Horizontal bar chart

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/CariYaslandirmaRaporModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IRaporService.cs`
- `CRMFiloServis.Web/Services/RaporService.cs`
- `CRMFiloServis.Web/Components/Pages/Raporlar/CariYaslandirmaRapor.razor` (yeni)
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor`
- `CRMFiloServis.Web/wwwroot/js/dashboard-charts.js`

**Durum:** Tamamlandı

### Kayıt 023 - Dashboard Grafikleri (Chart.js)
**Talep:** Dashboard'a görsel grafikler eklenmesi - Aylık gelir/gider, masraf dağılımı, bütçe takibi.

**Yapılanlar:**
- `ChartDataModels.cs`: Yeni grafik veri modelleri oluşturuldu (AylikGelirGiderVeri, CariTipDagilimi, MasrafKategoriDagilimi, AylikButceVeri)
- `IDashboardGrafikService.cs`: 3 yeni metod eklendi (GetMasrafKategoriDagilimiAsync, GetCariTipDagilimiAsync, GetAylikButceAsync)
- `DashboardGrafikService.cs`: Yeni metodların implementasyonu eklendi
- `Home.razor`: Chart.js entegrasyonu yapıldı (IJSRuntime ile JS interop)
- `Home.razor`: 4 grafik kartı eklendi (Aylık Gelir/Gider bar chart, Masraf Dağılımı doughnut chart, Bütçe Takibi line chart, Cari Dağılımı tablosu)
- `Home.razor`: OnAfterRenderAsync ve RenderChartsAsync metodları eklendi
- Mevcut `dashboard-charts.js` fonksiyonları kullanıldı (createBarChart, createLineChart, createDoughnutChart)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Models/ChartDataModels.cs` (yeni)
- `CRMFiloServis.Web/Services/Interfaces/IDashboardGrafikService.cs`
- `CRMFiloServis.Web/Services/DashboardGrafikService.cs`
- `CRMFiloServis.Web/Components/Pages/Home.razor`

**Durum:** Tamamlandı

### Kayıt 022 - Sayfalama (Pagination) altyapısı
**Talep:** Liste sayfalarında performans iyileştirmesi için server-side sayfalama altyapısının eklenmesi.

**Yapılanlar:**
- `Pagination.razor`: Yeniden kullanılabilir sayfalama komponenti oluşturuldu
- `PagedResult.cs`: Generic `PagedResult<T>` ve `PagingParameters` modelleri oluşturuldu
- `ICariService.cs`: `GetPagedAsync` metodu ve `CariFilterParams` sınıfı eklendi
- `CariService.cs`: Sayfalama implementasyonu ve bakiye hesaplama eklendi
- `CariList.razor`: Server-side sayfalama entegrasyonu ve debounce arama eklendi
- `IFaturaService.cs`: `GetPagedAsync` metodu ve `FaturaFilterParams` sınıfı eklendi
- `FaturaService.cs`: Sayfalama implementasyonu eklendi
- `FaturaList.razor`: Sayfalama destekli yeniden oluşturuldu (tip, durum, tarih filtreleri)
- `IBankaKasaHareketService.cs`: `GetPagedAsync` metodu ve `BankaHareketFilterParams` sınıfı eklendi
- `BankaKasaHareketService.cs`: Sayfalama implementasyonu eklendi
- `BankaHareketList.razor`: Sayfalama destekli güncellendi (hesap, tip, tarih filtreleri, toplam özeti)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Shared/Pagination.razor`
- `CRMFiloServis.Web/Models/PagedResult.cs`
- `CRMFiloServis.Web/Services/Interfaces/ICariService.cs`
- `CRMFiloServis.Web/Services/CariService.cs`
- `CRMFiloServis.Web/Components/Pages/Cariler/CariList.razor`
- `CRMFiloServis.Web/Services/Interfaces/IFaturaService.cs`
- `CRMFiloServis.Web/Services/FaturaService.cs`
- `CRMFiloServis.Web/Components/Pages/Faturalar/FaturaList.razor`
- `CRMFiloServis.Web/Services/Interfaces/IBankaKasaHareketService.cs`
- `CRMFiloServis.Web/Services/BankaKasaHareketService.cs`
- `CRMFiloServis.Web/Components/Pages/BankaHareketleri/BankaHareketList.razor`

**Durum:** Tamamlandı

### Kayıt 021 - Çalışma zamanı klasör disiplininin genişletilmesi
**Talep:** Sadece `uploads` değil, diğer çalışma zamanı klasörlerinin de git takibinden ayrılması.

**Yapılanlar:**
- `.gitignore`: `CRMFiloServis.Web/Backups/**` ignore kuralı eklendi
- `.gitignore`: `deploy/Backups/**`, `deploy/Logs/**`, `deploy/Uploads/**` ignore kapsamına alındı
- runtime klasörlerini repo içinde korumak için `.gitkeep` istisnaları tanımlandı
- `010` kaydı ile açık iş özeti tutarlı hale getirildi

**Etkilenen Dosyalar:**
- `.gitignore`
- `DEVELOPMENT.md`
- `CRMFiloServis.Web/Backups/.gitkeep`
- `CRMFiloServis.Web/wwwroot/uploads/.gitkeep`
- `deploy/Backups/.gitkeep`
- `deploy/Logs/.gitkeep`
- `deploy/Uploads/.gitkeep`

**Durum:** Tamamlandı

### Kayıt 020 - Servis katmanında okuma ve soft delete tutarlılığı
**Talep:** Servis katmanındaki güvenli refaktör işlerinin tamamlanması.

**Yapılanlar:**
- `GuzergahService.cs`: soft delete işleminde `UpdatedAt` güncellemesi eklendi
- `GuzergahService.cs`: kod üretiminde okuma sorguları `AsNoTracking()` ile güvenli hale getirildi
- `MasrafKalemiService.cs`: soft delete işleminde `UpdatedAt` güncellemesi eklendi
- `PiyasaKaynakService.cs`: `DateTime.Now` yerine `DateTime.UtcNow` standardı uygulandı
- `PiyasaKaynakService.cs`: kod kontrolü ve seed sayım sorgularında `AsNoTracking()` eklendi
- `SoforService.cs`: aktif kayıt sayımı `AsNoTracking()` ile güncellendi
- `SoforService.cs`: soft delete işleminde `UpdatedAt` güncellemesi eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/GuzergahService.cs`
- `CRMFiloServis.Web/Services/MasrafKalemiService.cs`
- `CRMFiloServis.Web/Services/PiyasaKaynakService.cs`
- `CRMFiloServis.Web/Services/SoforService.cs`

**Durum:** Tamamlandı

### Kayıt 019 - Marka adı görünür metin taraması
**Talep:** Proje genelinde görünür marka adlarının taranması ve `Koa Filo Servis` ile tutarlı hale getirilmesi.

**Yapılanlar:**
- `Login.razor`: footer içindeki GitHub bağlantı etiketi `Koa Filo Servis` olarak güncellendi
- `README.md`: ilgili projeler tablosundaki eski marka adı güncellendi
- `ROADMAP.md`: doküman başlığındaki eski marka adı güncellendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Login.razor`
- `README.md`
- `ROADMAP.md`

**Durum:** Tamamlandı

### Kayıt 018 - Muhasebe eşleştirme yönetim ekranları
**Talep:** Banka/kasa hesap ve hareketlerinde muhasebe eşleştirme alanlarının yönetilebilir hale getirilmesi.

**Sorun:**
Muhasebe eşleştirme alanları entity ve servis katmanında vardı; ancak banka hesap ve banka hareket ekranlarında bu alanlar yönetilemiyordu.

**Yapılanlar:**
- `BankaHesapForm.razor`: varsayılan muhasebe kodu ve kost merkezi alanları eklendi
- `BankaHesapForm.razor`: hesap tipine göre önerilen varsayılan muhasebe kodu ataması eklendi (`100` / `102` / `300`)
- `BankaHesapList.razor`: hesap kartlarında varsayılan muhasebe kodu ve kost merkezi görünür hale getirildi
- `BankaHareketForm.razor`: hareket bazlı muhasebe hesap kodu, alt hesap, kost merkezi, proje kodu ve muhasebe açıklama alanları eklendi
- `BankaHareketForm.razor`: seçilen hesaptan varsayılan muhasebe değerlerini doldurma desteği eklendi
- `BankaHareketList.razor`: hareket listesine muhasebe özeti kolonu eklendi
- `BankaKasaHareketService.cs`: create/update sırasında hesap varsayılanlarını servis katmanında otomatik uygulama eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/BankaHesaplari/BankaHesapForm.razor`
- `CRMFiloServis.Web/Components/Pages/BankaHesaplari/BankaHesapList.razor`
- `CRMFiloServis.Web/Components/Pages/BankaHareketleri/BankaHareketForm.razor`
- `CRMFiloServis.Web/Components/Pages/BankaHareketleri/BankaHareketList.razor`
- `CRMFiloServis.Web/Services/BankaKasaHareketService.cs`

**Durum:** Tamamlandı

### Kayıt 017 - Servis Puantaj firma filtresi düzeltmesi
**Talep:** Servis puantaj ekranında "Tüm Firmalar" filtrelemesi ve toplu puantaj akışı düzeltmesi.

**Sorunlar:**
1. `YenileAsync` metodunda `firmaId = 0` olduğunda hard-coded `firmaId = 1` kullanılıyordu
2. Tüm firmalar seçildiğinde sadece FirmaId=1 olan eşleştirmeler geliyordu
3. Toplu puantaj üretiminde firma seçimi zorunlu değildi

**Yapılanlar:**
- `IFiloKomisyonService.cs`: `GetEslestirmelerAsync` ve `GetPuantajlarByTarihAraligiAsync` parametreleri nullable yapıldı
- `FiloKomisyonService.cs`: firmaId null veya 0 ise tüm firmaları getir
- `ServisPuantaj.razor`:
  - `YenileAsync`: Hard-coded değer kaldırıldı, nullable int kullanımı
  - `TopluPuantajOlustur`: Firma seçimi zorunlu hale getirildi (toplu üretim için)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/IFiloKomisyonService.cs`
- `CRMFiloServis.Web/Services/FiloKomisyonService.cs`
- `CRMFiloServis.Web/Components/Pages/Filo/ServisPuantaj.razor`

**Durum:** Tamamlandı

### Kayıt 016 - Bütçe ödemelerinde cari mahsup entegrasyonu
**Talep:** Bütçe ödemelerinde CariMahsup tipi seçildiğinde otomatik hareket ve muhasebe fişi üretimi.

**Sorun:**
BudgetService.OdemeYapAsync metodunda sadece `OdemeTipi.Mahsup` kontrol ediliyordu, `OdemeTipi.CariMahsup` için ayrı işlem yapılmıyordu. Bu durumda:
- Cari mahsup seçildiğinde BankaKasaHareket oluşturulmuyordu
- Muhasebe fişi üretilmiyordu

**Yapılanlar:**
- `BudgetService.cs`: IBankaKasaHareketService dependency injection eklendi
- `BudgetService.OdemeYapAsync`: CariMahsup tipi için ayrı branch eklendi
  - CariId ve BankaHesapId validasyonu
  - BankaKasaHareketService.CariMahsupAsync çağrısı
  - CaridenTahsilat yönü desteği
  - Otomatik muhasebe fişi zinciri (CariMahsupAsync → CreateCariMahsupFisiAsync)
  - Hareket ID'sini BudgetOdeme kaydına bağlama

**Akış:**
```
BudgetAnaliz → OdemeTipi.CariMahsup seç → OdemeYapAsync
  → BankaKasaHareketService.CariMahsupAsync
    → BankaKasaHareket oluştur
    → MuhasebeService.CreateCariMahsupFisiAsync
      → MuhasebeFis + MuhasebeFisKalem kayıtları
```

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/BudgetService.cs`

**Durum:** Tamamlandı

### Kayıt 015 - Taşıma → Güzergah akışı doğrulama ve düzeltme
**Talep:** Fatura kaleminden güzergah oluşturma akışının doğrulanması ve düzeltilmesi.

**Sorunlar:**
1. CariId yanlış atanıyordu (Firma.Id yerine Fatura.CariId olmalı)
2. Aynı fatura kaleminden tekrar güzergah oluşturulabiliyordu (kontrol yoktu)
3. Aynı firma + güzergah adı kombinasyonu için benzersizlik kontrolü eksikti

**Yapılanlar:**
- `IGuzergahService.cs`: 3 yeni doğrulama metodu eklendi
  - FaturaKalemdenGuzergahVarMiAsync: Fatura kaleminden daha önce güzergah oluşturulmuş mu
  - GetByFaturaKalemIdAsync: Fatura kaleminden oluşturulan güzergahı getir
  - BenzersizGuzergahMiAsync: Firma + güzergah adı benzersizlik kontrolü
- `GuzergahService.cs`: Doğrulama region'ı ile metodlar implemente edildi
- `StokTuruEslestir.razor`: Güzergah oluşturma akışı düzeltildi
  - GuzergahOnizlemeItem'a CariId ve ZatenMevcut alanları eklendi
  - MevcutGuzergahKontrolEtAsync: Modal açıldığında mevcut kontrol
  - GuzergahlariOlustur: Doğrulama kontrolleri ve bilgilendirme mesajları

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/IGuzergahService.cs`
- `CRMFiloServis.Web/Services/GuzergahService.cs`
- `CRMFiloServis.Web/Components/Pages/EFatura/StokTuruEslestir.razor`

**Durum:** Tamamlandı

### Kayıt 014 - Mahsup işlemleri muhasebe fişi entegrasyonu
**Talep:** Mahsup işlemlerinde otomatik muhasebe fişi üretimi ve iptal kaydı.

**Yapılanlar:**
- `IMuhasebeService.cs`: 3 yeni metod eklendi (CreateHesapTransferFisiAsync, CreateCariMahsupFisiAsync, IptalFisiOlusturAsync)
- `MuhasebeService.cs`: Mahsup fişi oluşturma implementasyonu eklendi
  - Hesaplar arası transfer için çift taraflı fiş (kaynak ALACAK, hedef BORÇ)
  - Cari mahsup için tahsilat/ödeme fişi (Kasa/Banka vs Alıcılar/Satıcılar)
  - İptal için ters kayıt (storno) fişi oluşturma
- `BankaKasaHareketService.cs`: IMuhasebeService bağımlılığı eklendi
  - HesaplarArasiTransferAsync: Transfer sonrası otomatik fiş üretimi
  - CariMahsupAsync: Cari mahsup sonrası otomatik fiş üretimi
  - MahsupIptalAsync: İptal öncesi ters kayıt fişi oluşturma
- Hesap tipine göre varsayılan muhasebe kodu eşleştirmesi (Kasa:100, Banka:102, Kredi:300)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/IMuhasebeService.cs`
- `CRMFiloServis.Web/Services/MuhasebeService.cs`
- `CRMFiloServis.Web/Services/BankaKasaHareketService.cs`

**Durum:** Tamamlandı

### Kayıt 013 - Login stabilizasyonu ve Servis Puantaj Excel export
**Talep:** Login akışı stabilitesi artırımı ve Servis Puantaj ekranı Excel export özelliği.

**Yapılanlar:**
- `RedirectToLogin.razor`: `forceLoad: true` yerine `false` yapıldı - circuit korunarak auth state kaybı önlendi
- `Login.razor`: Input değerleri trim edilerek gereksiz boşluk temizliği eklendi
- `Login.razor`: Auth state propagation bekleme süresi 100ms'den 150ms'ye artırıldı
- `ServisPuantaj.razor`: Excel export özelliği tamamlandı (ClosedXML ile puantaj tablosu export)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Shared/RedirectToLogin.razor`
- `CRMFiloServis.Web/Components/Pages/Login.razor`
- `CRMFiloServis.Web/Components/Pages/Filo/ServisPuantaj.razor`

**Durum:** Tamamlandı

### Kayıt 012 - Bütçe Analiz ödeme ve hareket düzeltmeleri
**Talep:** Bütçe analizde ödeme yaparken kesinti/ek masraf hesaplama ve listeden kaldırma sorunları.

**Sorunlar:**
1. 791 TL ceza + 3,90 TL masraf kesintisi = Net 794,90 TL olması gerekirken (-) işaretsiz net rakam gelmiyordu
2. Kredi kartı listesinde net rakama eklenmesi gerekirken eksiltme yapılıyordu
3. Ödeme yapıldıktan sonra sağ taraftaki bekleyen ödemeler tablosundan kayıt kaldırılmıyordu
4. Kasa/banka hareketi silindiğinde ilişkili bütçe ödeme durumu geri alınmıyordu

**Yapılanlar:**
- Ek masraf değerleri için `Math.Abs()` ile mutlak değer alınması sağlandı (BudgetAnaliz.razor, BudgetService.cs, IBudgetService.cs)
- Net ödeme tutarı hesaplaması düzeltildi (her zaman tutar + ek masraf)
- Ödeme yapıldıktan sonra `bekleyenOdemeler` listesinden kayıt kaldırma eklendi
- `BankaKasaHareketService.DeleteAsync()` metodunda ilişkili bütçe ödeme durumunu "Bekliyor"a geri alma eklendi

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Budget/BudgetAnaliz.razor`
- `CRMFiloServis.Web/Services/BudgetService.cs`
- `CRMFiloServis.Web/Services/Interfaces/IBudgetService.cs`
- `CRMFiloServis.Web/Services/BankaKasaHareketService.cs`

**Durum:** Tamamlandı

### Kayıt 001 - E-Fatura PDF eşleştirme sorunu
**Talep:** PDF dosyasına sürekli aynı faturanın PDF'inin eklenmesi sorununun çözülmesi.

**Yapılanlar:**
- XML + PDF eşleştirme mantığı düzeltildi.
- import sırasında yanlış PDF eşleşmesi engellendi.
- PDF dosya adı üretimi benzersiz hale getirildi.

**Durum:** Tamamlandı

### Kayıt 011 - Servis katmanında takip/izleme güvenliği refaktörü
**Talep:** Bekleyen servis değişikliklerinin sınıflandırılması ve güvenli hale getirilmesi.

**Yapılanlar:**
- bekleyen servis değişikliklerinin büyük ölçüde aynı refaktör grubunda olduğu tespit edildi
- sorgu tarafında `AsNoTracking()` kullanımı yaygınlaştırıldı
- güncelleme işlemlerinde doğrudan `Update(entity)` yerine mevcut kaydı bulup alan bazlı güncelleme yaklaşımı uygulanmaya başlandı
- çözüm dosyasına yanlışlıkla eklenen harici proje referansı geri alındı

**Yapılacaklar:**
- davranış değişikliği riski düşük tutularak refaktör tamamlandı; kritik akışlarda manuel doğrulama önerilir

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/BankaHesapService.cs`
- `CRMFiloServis.Web/Services/BankaKasaHareketService.cs`
- `CRMFiloServis.Web/Services/GuzergahService.cs`
- `CRMFiloServis.Web/Services/MasrafKalemiService.cs`
- `CRMFiloServis.Web/Services/PiyasaKaynakService.cs`
- `CRMFiloServis.Web/Services/SoforService.cs`
- `CRMFiloServis.slnx`

**Durum:** Tamamlandı

### Kayıt 002 - Taşıma hizmetlerinden güzergah üretimi
**Talep:** Stok türü eşleştirmede `Hizmet > Taşıma` seçildiğinde güzergah listesi hazırlanması ve kullanıcı onayı sonrası firma bazlı güzergah açılması.

**Yapılanlar:**
- güzergah için yeni alanlar eklendi
- sefer tipi ve personel sayısı alanları genişletildi
- önizleme ve oluşturma akışı başlatıldı
- CariId düzeltildi, doğrulama ve benzersizlik kontrolü eklendi (Kayıt 015)

**Durum:** Tamamlandı

### Kayıt 010 - Repo temizliği ve uploads takibinin kapatılması
**Talep:** Çalışma zamanında oluşan yükleme dosyalarının tekrar git takibine girmesinin engellenmesi.

**Yapılanlar:**
- kök `.gitignore` dosyasına `CRMFiloServis.Web/wwwroot/uploads/**` kuralı eklendi
- yükleme çıktıları için repo temizliği maddesi aktif takip listesine işlendi
- git takibine girmiş upload dosyaları index'ten çıkarıldı
- klasörü korumak için `CRMFiloServis.Web/wwwroot/uploads/.gitkeep` eklendi
- `CRMFiloServis.Web/Backups`, `deploy/Backups`, `deploy/Logs`, `deploy/Uploads` çalışma zamanı klasörleri ignore kapsamına alındı
- runtime klasörleri için `.gitkeep` istisna yaklaşımı genişletildi

**Yapılacaklar:**
- ek çalışma zamanı klasörü oluşursa aynı ignore + `.gitkeep` standardı uygulanmalı

**Etkilenen Dosyalar:**
- `.gitignore`
- `DEVELOPMENT.md`

**Durum:** Tamamlandı

### Kayıt 009 - Proje geneli marka adı tutarlılığı
**Talep:** Görünür ekranlar ve dokümantasyonda eski marka adlarının taranıp güncellenmesi.

**Yapılanlar:**
- login ekranı `Koa Filo Servis` olarak güncellendi
- sol menü marka adı güncellendi
- `README.md` başlığı güncellendi
- `KURULUM.md` başlığı güncellendi
- deploy dokümantasyonu ve build script başlıkları güncellendi
- lisans masaüstü uygulama başlıkları güncellendi
- lisans kodu ön eki `KOA-` olarak güncellendi
- login footer GitHub etiketi `Koa Filo Servis` olarak güncellendi
- `README.md` proje tablosundaki eski marka adı güncellendi
- `ROADMAP.md` başlığı güncellendi

**Yapılacaklar:**
- teknik namespace, repo adı ve veritabanı adı gibi ürün adı olmayan teknik referanslar korunarak ayrıştırma yaklaşımı sürdürülmeli

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Login.razor`
- `CRMFiloServis.Web/Components/Layout/NavMenu.razor`
- `README.md`
- `KURULUM.md`
- `ROADMAP.md`

**Durum:** Tamamlandı

### Kayıt 003 - Servis puantaj sistemi
**Talep:** Güzergah / araç / şoför eşleştirme ve günlük puantaj tablosu oluşturulması.

**Yapılanlar:**
- `FiloGuzergahEslestirme` ve `FiloGunlukPuantaj` yapıları devreye alındı
- puantaj ekranı için temel sayfa oluşturuldu
- Firma filtresi düzeltildi, toplu kayıt akışı tamamlandı (Kayıt 017)
- Excel export özelliği eklendi (Kayıt 013)

**Durum:** Tamamlandı

### Kayıt 004 - Mahsup işlemleri
**Talep:** Kasa / banka / kredi kartı ve cari hesaplar arası mahsup işlemleri.

**Yapılanlar:**
- hesaplar arası transfer mantığı geliştirildi
- cari mahsup yapısı eklendi
- mahsup ekranı oluşturuldu
- Fiş üretimi ve iptal kaydı eklendi (Kayıt 014)

**Durum:** Tamamlandı

### Kayıt 005 - Muhasebe eşleştirme kodları
**Talep:** Girilen banka hareketlerinde kullanıcı tarafından muhasebe eşleştirme kodlarının girilebilmesi.

**Yapılanlar:**
- banka/kasa hareketlerine muhasebe alanları eklendi
- hesap bazlı varsayılan muhasebe kodu alanları eklendi
- kost merkezi ve proje tanımları için altyapı eklendi
- banka hesap form ve liste ekranlarına muhasebe eşleştirme alanları eklendi
- banka hareket form ve liste ekranlarına muhasebe eşleştirme yönetimi eklendi
- hesap varsayılanlarının hareket kayıtlarında otomatik uygulanması eklendi

**Durum:** Tamamlandı

### Kayıt 006 - Bütçe ödeme kesintileri
**Talep:** Bütçe analiz ödeme ekranında masraf/ceza kesintileri ve ödeme şekline göre kayıt yapılması.

**Yapılanlar:**
- kesinti alanları eklendi
- net ödeme hesabı eklendi
- ödeme tipi alanları genişletildi
- CariMahsup entegrasyonu ve muhasebe fişi zinciri eklendi (Kayıt 016)
- Net tutar hesaplama düzeltildi, bekleyen ödemeler listesinden kaldırma eklendi (Kayıt 012)

**Durum:** Tamamlandı

### Kayıt 007 - Login ekranı sorunları
**Talep:** Şifre göster, beni hatırla ve giriş yap butonunun çalışmaması sorunlarının giderilmesi.

**Yapılanlar:**
- login sayfası birkaç kez düzenlendi
- route çakışması giderildi
- input binding ve giriş akışı üzerinde düzeltmeler yapıldı
- sayfa yeniden yapılandırıldı
- forceLoad düzeltildi, input trim eklendi, delay artırıldı (Kayıt 013)

**Durum:** Tamamlandı

### Kayıt 008 - Marka adı güncellemesi
**Talep:** Proje görünen adının `Koa Filo Servis` olarak düzenlenmesi.

**Yapılanlar:**
- login ekranı başlığı güncellendi
- menü marka adı güncellendi
- bazı görünür başlıklarda düzenleme yapıldı
- Görünür marka metinleri ve doküman başlıkları güncellendi (Kayıt 009, 019)

**Durum:** Tamamlandı

---

## Açık İş Özeti

| No | Konu | Durum | Öncelik | Not |
|---|---|---|---|---|
| 001 | Login akışı | Tamamlandı | Yüksek | forceLoad düzeltildi, input trim eklendi, delay artırıldı |
| 002 | Taşıma → Güzergah akışı | Tamamlandı | Yüksek | CariId düzeltildi, doğrulama ve benzersizlik kontrolü eklendi |
| 003 | Servis puantaj sistemi | Tamamlandı | Yüksek | Firma filtresi düzeltildi, toplu kayıt akışı tamamlandı |
| 004 | Mahsup işlemleri | Tamamlandı | Yüksek | Fiş üretimi ve iptal kaydı eklendi |
| 005 | Muhasebe eşleştirme ekranları | Tamamlandı | Orta | Hesap ve hareket ekranlarında yönetim alanları eklendi |
| 006 | Bütçe + cari mahsup | Tamamlandı | Yüksek | CariMahsup entegrasyonu ve muhasebe fişi zinciri eklendi |
| 007 | Marka adı tutarlılığı | Tamamlandı | Orta | Görünür marka metinleri ve doküman başlıkları güncellendi |
| 008 | Repo temizliği / uploads | Tamamlandı | Orta | Ignore + cached dosya temizliği yapıldı |
| 009 | Dokümantasyon marka güncellemesi | Tamamlandı | Düşük | README, kurulum, roadmap ve deploy başlıkları güncellendi |
| 010 | Çalışma zamanı dosya disiplini | Tamamlandı | Düşük | Upload, backup, log ve deploy runtime klasörleri ignore edildi |
| 011 | Servis refaktörlerinin sınıflandırılması | Tamamlandı | Orta | AsNoTracking, UTC ve soft delete audit tutarlılığı tamamlandı |
| 015 | Proforma Fatura Sistemi | Tamamlandı | Yüksek | Entity, servis, 3 sayfa, faturaya dönüştürme, Excel export |
| 016 | Cari Borç/Alacak Takip | Tamamlandı | Yüksek | Risk skorlama, vade analizi, tahsilat planı, detaylı rapor |
| 012 | Sayfalama altyapısı | Tamamlandı | Yüksek | CariList, FaturaList, BankaHareketList sayfalama destekli |
| 013 | Şoför Performans Raporu | Tamamlandı | Orta | Bireysel/karşılaştırma, grafik, Excel export |
| 014 | Araç Karlılık Raporu | Tamamlandı | Orta | Gelir/gider/kar analizi, masraf dağılımı, trend grafikleri |

---

## Çözüm Yapısı

### Projeler
- `CRMFiloServis.Web`
  - Blazor Server ana uygulama
  - sayfalar, servisler, veri erişimi, migrationlar
- `CRMFiloServis.Shared`
  - ortak entity modelleri
  - enumlar, yardımcı sınıflar
- `CRMFiloServis.LisansDesktop`
  - lisans yönetimi / masaüstü yardımcı araç

### Teknoloji
- `.NET 10`
- `Blazor Server`
- `Entity Framework Core`
- `SQLite` / `PostgreSQL` desteği
- `Bootstrap` + `Bootstrap Icons`

---

## Şu Ana Kadar Yapılanlar

## 1. Temel Modüller
Aşağıdaki ana modüller projede mevcut durumda:
- Kullanıcı / rol / yetki yönetimi
- Cari yönetimi
- Filo servis yönetimi
- Araç, şoför, güzergah, servis çalışma kayıtları
- Bütçe ve ödeme takibi
- Muhasebe hesap planı / fiş yapısı
- Banka / kasa hareketleri
- E-fatura / XML import altyapısı
- Stok / envanter mantığı
- CRM yardımcı modülleri
  - bildirim
  - mesaj
  - WhatsApp
  - randevu / hatırlatıcı

## 2. Son Dönemde Tamamlanan İşler

### Login ekranı
- giriş ekranı düzenlendi
- şifre göster/gizle davranışı iyileştirildi
- “beni hatırla” yaklaşımı üzerinde düzenleme yapıldı
- login route tekrarları temizlendi
- login sayfası birkaç kez sadeleştirilip yeniden düzenlendi
- marka adı giriş ekranında `Koa Filo Servis` olarak güncellendi

### Marka / Görünüm
- giriş ekranı başlığı `Koa Filo Servis` yapıldı
- sol menü üst marka adı güncellendi
- giriş ekranı daha kurumsal görünecek şekilde elden geçirildi

### E-Fatura / XML + PDF import
- XML ile birlikte PDF yükleme desteği eklendi
- tek PDF’in tüm faturalara bağlanması sorunu düzeltildi
- XML-PDF eşleştirme mantığı iyileştirildi
- dosya adı benzersizleştirildi

### Stok Türü Eşleştirme / Güzergah hazırlığı
- `Hizmet` + `Taşıma` tipi için güzergah üretim hazırlığı yapıldı
- fatura kalemlerinden güzergah önizleme mantığı eklendi
- güzergah için sefer tipi ve personel sayısı alanları genişletildi

### Filo operasyon / puantaj
- `FiloGuzergahEslestirme` ve `FiloGunlukPuantaj` yapıları kullanılmaya başlandı
- servis puantaj ekranı için temel sayfa oluşturuldu
- güzergah / araç / şoför eşleştirme akışı geliştirildi

### Mahsup işlemleri
- hesaplar arası transfer mantığı eklendi
- cari mahsup mantığı eklendi
- mahsup hareketlerini gruplamak için alanlar eklendi:
  - `MahsupGrupId`
  - `MahsupHareketId`
- `Mahsup İşlemleri` sayfası oluşturuldu
- kasa / banka / kredi kartı hesapları arası transfer altyapısı geliştirildi

### Muhasebe eşleştirme alanları
- banka / kasa hareketlerine kullanıcı tarafından girilebilecek muhasebe alanları eklendi:
  - `MuhasebeHesapKodu`
  - `MuhasebeAltHesapKodu`
  - `KostMerkeziKodu`
  - `ProjeKodu`
  - `MuhasebeAciklama`
- hesap bazında varsayılan muhasebe kodu alanları eklendi
- `KostMerkezi` ve `MuhasebeProje` tanımları eklendi

### Bütçe modülü
- ödeme yaparken kesinti alanları eklendi:
  - masraf kesintisi
  - ceza kesintisi
  - diğer kesinti
- net ödeme hesabı yapıldı
- ödeme tipi seçenekleri genişletildi:
  - kasa
  - banka
  - kredi kartı
  - mahsup
  - cari mahsup hazırlığı

### Migrationlar
Yakın dönemde eklenen migrationlar:
- `GuzergahGenisletme`
- `BudgetOdemeKesintiler`
- `MahsupMuhasebeKodlari`

---

## Aktif Olarak Dikkat Edilmesi Gerekenler

### 1. Destek modülü smoke test altyapısı
- `PlaywrightTestProcedures.md` mevcut ancak repo içinde çalıştırılabilir smoke test kaynak dosyaları görünmüyor.
- Destek modülü için otomatik ekran açılış testi istenirse önce test projesi kaynakları netleştirilmeli veya yeniden oluşturulmalı.

### 2. EBYS tamamlama başlıkları
- `BelgeMerkezi` etrafındaki dosya, metadata ve arama akışları büyük ölçüde hazır.
- Versiyon kontrolü, içerik bazlı/akıllı arama ve örnek veri/test tarafı hâlâ açık.

### 3. Yol haritası temizliği sonrası aktif başlıklar
- Bütçe `Hedef/Gerçekleşen` karşılaştırması
- Destek talepleri `E-posta Entegrasyonu`
- EBYS kalan altyapı ve test işleri

---

## Yapılması Gerekenler

### A. Yakın Geliştirme Sırası
1. Destek modülü smoke test altyapısını netleştir veya yeniden kur.
2. EBYS için görünür iyileştirmeleri sürdür:
   - eksik evrak görünürlüğü
   - checklist/export doğrulaması
   - versiyon kontrolü
3. Bütçe için `Hedef / Gerçekleşen` karşılaştırma ekranını tamamla.

### B. Teknik Borç
1. Otomatik UI smoke test altyapısını yeniden görünür hale getir.
2. Dokümantasyonda sadece aktif açık maddeleri bırak.
3. Kritik modüller için seçilmiş integration test seti oluştur.

---

## Önerilen Kısa Yol Haritası

### Faz 1
- destek modülü smoke test altyapısını çalışır hale getirme
- `DEVELOPMENT.md` / `ROADMAP.md` aktif başlıklar odaklı tutma

### Faz 2
- EBYS açık işleri: versiyon kontrolü, içerik arama, örnek veri ve test
- Personel özlük tarafında görünür kullanım iyileştirmeleri

### Faz 3
- bütçe `Hedef / Gerçekleşen` karşılaştırması
- seçilmiş modüller için otomatik test kapsaması

---

## Son Commitlerden Özet
- login ekranı yeniden yazıldı
- login route düzeltildi
- marka adı `Koa Filo Servis` olarak güncellendi
- mahsup işlemleri eklendi
- muhasebe eşleştirme kodları eklendi
- bütçe kesinti alanları eklendi
- XML + PDF import geliştirildi
- servis puantaj / güzergah tarafında temel altyapı geliştirildi

---

## Güncel Kısa Durum Özeti

### Tamamlanan Ana Başlıklar
- bütçe + cari mahsup görünürlüğü ve ödeme izi
- destek modülü kontrollü yükleme ve PostgreSQL dayanıklılığı
- e-fatura, mahsup, muhasebe ve puantaj ana akışları
- marka ve runtime klasör temizliği

### Devam Edenler
- EBYS merkez ekranının ileri seviye tamamlama işleri
- destek modülü smoke test altyapısının netleştirilmesi

### Kritik Açık Başlıklar
- destek modülü için çalıştırılabilir smoke test altyapısı
- EBYS versiyon kontrolü ve test senaryoları
- bütçe `Hedef / Gerçekleşen` karşılaştırması

---

## Yeni Kayıt Şablonu

Yeni bir kullanıcı talebi geldiğinde aşağıdaki format kullanılmalıdır:

### Kayıt 00X - Talep başlığı
**Talep:**
- kullanıcı isteğinin kısa özeti

**Yapılanlar:**
- yapılan adım 1
- yapılan adım 2

**Yapılacaklar:**
- eksik adım 1
- eksik adım 2

**Etkilenen Dosyalar:**
- `dosya/yolu`
- `dosya/yolu`

**Durum:** Bekliyor / Devam ediyor / Kısmen tamamlandı / Tamamlandı

**Not:**
- varsa risk, karar veya bağımlılık

---

## Güncelleme Kuralları

- Her yeni kullanıcı isteğinde önce `İstek Kayıtları` güncellenmeli.
- İş tamamlandıysa `Açık İş Özeti` tablosundaki durum da güncellenmeli.
- Teknik olarak önemli değişiklikler `Şu Ana Kadar Yapılanlar` bölümüne eklenmeli.
- Büyük eksikler `Yapılması Gerekenler` altında ilgili başlığa taşınmalı.
- İş bittiğinde mümkünse ilgili commit mesajı ayrıca not edilmeli.

---

## Login Doğrulama Checklist

### Fonksiyonel Kontroller
- [ ] `/login` sayfası tek endpoint olarak açılıyor mu
- [ ] kullanıcı adı alanı veri alıyor mu
- [ ] şifre alanı veri alıyor mu
- [ ] `Giriş Yap` butonu tıklanınca servis çağrısı çalışıyor mu
- [ ] hatalı kullanıcı adı doğru hata mesajı veriyor mu
- [ ] hatalı şifre doğru hata mesajı veriyor mu
- [ ] başarılı giriş sonrası ana sayfaya yönleniyor mu
- [ ] giriş sonrası kullanıcı yetkili sayfalara erişebiliyor mu
- [ ] tarayıcı yenilendiğinde oturum davranışı beklenen şekilde mi

### UI Kontrolleri
- [ ] şifre göster / gizle çalışıyor mu
- [ ] `Beni Hatırla` seçimi kalıyor mu
- [ ] başarı ve hata mesajları görünüyor mu
- [ ] mobil görünümde form bozulmuyor mu

### Teknik Kontroller
- [ ] `AuthProvider` giriş sonrası state yayıyor mu
- [ ] `KullaniciService.GirisYapAsync` sonucu beklenen kullanıcıyı döndürüyor mu
- [ ] yönlendirme sonrası `AuthorizeRouteView` kullanıcıyı anonim görmüyor mu
- [ ] local storage erişiminde hata oluşursa login akışı kırılmıyor mu

---

## Not
Bu dosya canlı durum özeti olarak kullanılmalı.
Yeni modül veya önemli değişikliklerden sonra güncellenmesi önerilir.

Her yeni kullanıcı talebinde aşağıdaki 3 başlık mutlaka güncellenmelidir:
- `İstek Kayıtları`
- `Yapılanlar`
- `Yapılması Gerekenler`

---

### Kayıt 120 - Fatura PDF Oluşturma ve E-posta Gönderimi
**Talep:** FAZ 2.3 - Doküman Yönetimi kapsamında Fatura PDF oluşturma özelliğinin FaturaDetay sayfasına entegrasyonu.

**Yapılanlar:**

**1. FaturaDetay.razor UI Entegrasyonu:**
- `@inject IFaturaSablonService FaturaSablonService` eklendi
- `@inject IJSRuntime JS` eklendi
- Header bölümüne PDF İndir ve E-posta Gönder butonları eklendi
- İşlem durumu göstergesi (islemMesaji, islemBasarili) eklendi
- E-posta gönderimi için modal popup eklendi:
  - E-posta adresi input alanı
  - Fatura no ve tutar bilgisi (GenelToplam)
  - Gönderim onay/iptal butonları
  - Yükleniyor spinner

**2. Kod Tarafı (@code section):**
- State değişkenleri: `pdfYukleniyor`, `emailGonderiliyor`, `emailModalGoster`, `emailAdresi`
- `PdfIndir()` metodu:
  - `FaturaSablonService.FaturaPdfOlusturAsync()` çağrısı
  - `FaturaPdfResult.PdfData` byte array'i Base64'e dönüşüm
  - `JS.InvokeVoidAsync("downloadFile")` ile tarayıcıya indirme
- `EmailModalAc()` / `EmailModalKapat()` modal yönetimi
- `EmailGonder()` metodu:
  - `FaturaYazdirRequest` DTO hazırlama
  - `FaturaSablonService.FaturaEmailGonderAsync()` çağrısı
  - bool dönüş değerine göre başarı/hata mesajı

**Kullanılan Model Yapısı:**
```csharp
FaturaPdfResult:
  - Basarili (bool)
  - Mesaj (string?)
  - PdfData (byte[]?)
  - DosyaAdi (string?)
  - Base64Data (string?)

FaturaYazdirRequest:
  - FaturaId (int)
  - SablonId (int?)
  - EmailGonder (bool)
  - EmailAdresi (string?)
  - EmailKonu (string?)
  - EmailMesaj (string?)
```

**Özellikler:**
- ✅ PDF indirme (JavaScript interop ile downloadFile)
- ✅ E-posta ile PDF gönderimi
- ✅ Yükleniyor durumu göstergesi
- ✅ İşlem sonucu mesajı (alert)
- ✅ Responsive modal tasarımı

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Pages/Faturalar/FaturaDetay.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 119 - E-posta Bildirimleri (Kullanıcı Bazlı Otomatik Gönderim)
**Talep:** FAZ 2.2 - Bildirim Sistemi kapsamında e-posta bildirimleri özelliğinin tamamlanması. Kullanıcı bazlı e-posta tercihleri, test e-postası ve günlük otomatik gönderim.

**Yapılanlar:**

**1. IBildirimService Interface Güncellendi:**
- `EpostaBildirimGonderAsync()` metodu eklendi - Tüm kullanıcılara bildirim e-postası gönderir
- `TestEpostaGonderAsync(int kullaniciId)` metodu eklendi - Test amaçlı örnek e-posta gönderir

**2. BildirimService Implementasyonu Genişletildi:**
- `IEmailService` dependency injection eklendi
- `EpostaBildirimGonderAsync()`: 
  - `BildirimAyarlari` tablosundan `EpostaAlsin=true` olan kullanıcıları sorgular
  - Her kullanıcının tercihlerine göre uyarıları toplar (fatura vade, araç/şoför belge süreleri)
  - `IEmailService.SendBelgeUyariEmailAsync` ile HTML formatında e-posta gönderir
  - `EpostaBildirimLog` tablosuna gönderim kaydı oluşturur (24 saat mükerrer gönderim engeli)
- `TestEpostaGonderAsync()`:
  - 3 örnek `BelgeUyariEmail` oluşturur (Fatura, Ehliyet, Trafik Sigortası)
  - Kullanıcının e-posta adresine test e-postası gönderir
- `GetBelgeTipiAdi()` helper metodu: BildirimTipi enum → Türkçe string dönüşümü

**3. EpostaBildirimLog Entity Eklendi (CRMEntities.cs):**
- `KullaniciId`: Gönderilen kullanıcı
- `EpostaAdresi`: Gönderilen e-posta adresi
- `UyariSayisi`: E-postadaki uyarı sayısı
- `GonderimTarihi`: Gönderim tarihi
- `Basarili`: Gönderim başarı durumu
- `HataMesaji`: Varsa hata mesajı

**4. ApplicationDbContext Güncellendi:**
- `DbSet<EpostaBildirimLog> EpostaBildirimLoglari` eklendi

**5. Bildirimler.razor E-posta Ayarları UI:**
- "E-posta Bildirimleri" bölümü eklendi
- `EpostaAlsin` checkbox toggle
- `EpostaAdresi` input alanı (EpostaAlsin=true olduğunda görünür)
- "Test E-postası Gönder" butonu
- Bilgi alertı: "E-posta bildirimleri her gün sabah 09:00'da otomatik olarak gönderilir"
- `testGonderiliyor` state değişkeni (loading durumu)

**6. BelgeUyariBackgroundService Entegrasyonu:**
- `KontrolVeGonderAsync()` metodu güncellendi
- `IBildirimService` scope'dan resolve ediliyor
- `bildirimService.TaraVeBildirimOlusturAsync()` çağrılıyor (uygulama içi bildirimler)
- `bildirimService.EpostaBildirimGonderAsync()` çağrılıyor (kullanıcı bazlı e-postalar)
- Mevcut admin e-posta mantığı korunuyor

**Mevcut Altyapı Kullanımı:**
- `EmailService.cs` içindeki `SendBelgeUyariEmailAsync` metodu (HTML şablonlu e-posta)
- `BildirimAyar` entity'sindeki `EpostaAlsin` ve `EpostaAdresi` alanları
- `BildirimTipi` enum (FaturaVade, EhliyetBitis, TrafikSigorta, Kasko, Muayene, SrcBelgesi, vb.)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Services/Interfaces/IBildirimService.cs` (güncellendi - 2 yeni metod)
- `CRMFiloServis.Web/Services/BildirimService.cs` (güncellendi - IEmailService entegrasyonu, e-posta metodları)
- `CRMFiloServis.Shared/Entities/CRMEntities.cs` (güncellendi - EpostaBildirimLog entity)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (güncellendi - EpostaBildirimLoglari DbSet)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/Bildirimler.razor` (güncellendi - e-posta ayarları UI)
- `CRMFiloServis.Web/Services/BelgeUyariBackgroundService.cs` (güncellendi - IBildirimService entegrasyonu)

**Özellikler:**
- ✅ Kullanıcı bazlı e-posta tercihleri (EpostaAlsin, EpostaAdresi)
- ✅ Test e-postası gönderme
- ✅ Günlük otomatik e-posta gönderimi (BelgeUyariBackgroundService üzerinden)
- ✅ 24 saat mükerrer gönderim engeli (EpostaBildirimLog kontrolü)
- ✅ HTML formatında profesyonel e-posta şablonu
- ✅ Bildirim tipine göre uyarı filtreleme

**Durum:** ✅ Tamamlandı

---

### Kayıt 118 - Rol Tabanlı Yetkilendirme (Sayfa Seviyesi Yetki Kontrolü)
**Talep:** FAZ 2.1 - Kullanıcı Yönetimi & Yetkilendirme kapsamında sayfa seviyesinde rol tabanlı yetki kontrolü eklenmesi.

**Yapılanlar:**
- **YetkiKontrol.razor bileşeni oluşturuldu** (`/Components/Shared/YetkiKontrol.razor`):
  - `Yetki` parametresi: Tek yetki kodu kontrolü (örn: `kullanici.oku`)
  - `Yetkiler[]` parametresi: Çoklu yetki kontrolü (OR mantığı - herhangi biri yeterliyse geçer)
  - `Rol` parametresi: Tek rol kontrolü
  - `Roller[]` parametresi: Çoklu rol kontrolü (OR mantığı)
  - `Yetkili` RenderFragment: Yetkili kullanıcıya gösterilecek içerik
  - `Yetkisiz` RenderFragment: Yetkisiz kullanıcıya gösterilecek opsiyonel içerik
  - `YetkisizYonlendir` parametresi: Otomatik yönlendirme URL'si
  - Admin rolü her zaman yetkili kabul edilir
  - `KullaniciService.GetKullaniciYetkileriAsync` ile dinamik yetki kontrolü
- **KullaniciYonetimi.razor güncellendi**:
  - Sayfa içeriği `<YetkiKontrol Yetki="kullanici.oku">` ile sarmalandı
  - Yetkisiz kullanıcılara bilgilendirme mesajı gösteriliyor
- **RolYonetimi.razor güncellendi**:
  - Sayfa içeriği `<YetkiKontrol Yetki="rol.oku">` ile sarmalandı
  - Yetkisiz kullanıcılara bilgilendirme mesajı gösteriliyor

**Mevcut Yetkilendirme Altyapısı:**
Projede zaten kapsamlı bir rol/yetki sistemi mevcuttu:
- `Kullanici` entity'si (RolId ile Rol bağlantısı)
- `Rol` entity'si (yetkiler collection'ı)
- `RolYetki` entity'si (yetki tanımları)
- `SistemRolleri` statik sınıfı (Admin, Muhasebeci, Operasyon, SatisTemsilcisi, Sofor, Kullanici)
- `Yetkiler` statik sınıfı (tüm yetki kodları)
- `NavMenu.razor` içinde `HasYetki/HasMenuYetki` kontrolleri (menü görünürlüğü)
- `KullaniciService.GetKullaniciYetkileriAsync` ve `YetkiVarMiAsync` metodları
- `AppAuthenticationStateProvider` (claims-based auth, Role claim)

**Etkilenen Dosyalar:**
- `CRMFiloServis.Web/Components/Shared/YetkiKontrol.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/KullaniciYonetimi.razor` (güncellendi)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/RolYonetimi.razor` (güncellendi)

**Durum:** ✅ Tamamlandı

---

### Kayıt 145 - Multi-tenant Veri İzolasyonu (FAZ 4.1)
**Talep:** FAZ 4.1 - Çoklu Şirket Desteği kapsamında şirket bazlı veri izolasyonu.

**Yapılanlar:**

**1. Entity Güncellemeleri (SirketId FK Ekleme):**
- `Cari` entity'sine `SirketId` (int?, FK) eklendi
- `Sofor` entity'sine `SirketId` (int?, FK) eklendi
- `Arac` entity'sine `SirketId` (int?, FK) eklendi
- `Fatura` entity'sine `SirketId` (int?, FK) eklendi
- `Guzergah` entity'sine `SirketId` (int?, FK) eklendi
- `BankaHesap` entity'sine `SirketId` (int?, FK) eklendi
- `BankaKasaHareket` entity'sine `SirketId` (int?, FK) eklendi
- Tüm entity'lere `Sirket` navigation property eklendi

**2. ApplicationDbContext Global Query Filter:**
- 7 entity için `HasQueryFilter` ile otomatik şirket filtreleme
- `_currentSirketId` private field (aktif şirket ID)
- `SetTenantService()` metodu ile runtime tenant değişikliği
- SuperAdmin bypass desteği (`_isSuperAdmin = true` olduğunda filtre devre dışı)
- `IgnoreQueryFilters()` ile manuel bypass imkanı

**3. ITenantService Interface:**
- `GetCurrentSirketIdAsync()` - aktif şirket ID
- `SetCurrentSirketAsync(int sirketId)` - şirket değiştirme
- `IsSuperAdminAsync()` - SuperAdmin kontrolü
- `GetKullaniciSirketleriAsync()` - kullanıcının erişebildiği şirketler

**4. TenantService Implementasyonu:**
- Session/Cookie tabanlı aktif şirket yönetimi
- Kullanıcı-şirket ilişkisi kontrolü
- SuperAdmin için tüm şirketlere erişim
- DbContext'e tenant bilgisi aktarımı

**5. Servis Güncellemeleri:**
- `CariService`, `SoforService`, `AracService` güncellendi
- Yeni kayıtlara otomatik `SirketId` atama
- `ITenantService` injection

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/CRMEntities.cs` (7 entity güncellendi)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (Global Query Filter)
- `CRMFiloServis.Web/Services/Interfaces/ITenantService.cs` (YENİ)
- `CRMFiloServis.Web/Services/TenantService.cs` (YENİ)
- `CRMFiloServis.Web/Services/CariService.cs` (tenant entegrasyonu)
- `CRMFiloServis.Web/Services/SoforService.cs` (tenant entegrasyonu)
- `CRMFiloServis.Web/Services/AracService.cs` (tenant entegrasyonu)

**Durum:** ✅ Tamamlandı

---

### Kayıt 146 - Şirketler Arası Transfer (FAZ 4.1)
**Talep:** FAZ 4.1 - Çoklu Şirket Desteği kapsamında şirketler arası veri transferi.

**Yapılanlar:**

**1. SirketTransferLog Entity:**
- `EntityTuru` (string) - transfer edilen entity türü
- `EntityId` (int) - transfer edilen kayıt ID
- `KaynakSirketId` (int) - kaynak şirket
- `HedefSirketId` (int) - hedef şirket
- `TransferEden` (string) - işlemi yapan kullanıcı
- `TransferTarihi` (DateTime) - transfer zamanı
- `Aciklama` (string) - opsiyonel not

**2. ITenantService Transfer Metodları:**
- `TransferAsync(SirketTransferRequest)` - toplu transfer
- `GetTransferOnizlemeAsync()` - transfer önizleme (entity sayıları)
- `GetTransferLoglariAsync()` - transfer geçmişi

**3. TenantService Transfer Implementasyonu:**
- `TransferCariAsync()` - Cari transferi (alt kayıtlarıyla birlikte)
- `TransferAracAsync()` - Araç transferi (masraflar, belgeler dahil)
- `TransferSoforAsync()` - Şoför transferi (puantaj, maaş dahil)
- `TransferFaturaAsync()` - Fatura transferi (detaylar dahil)
- `TransferGuzergahAsync()` - Güzergah transferi
- `TransferBankaHesapAsync()` - Banka hesabı transferi
- `TransferBankaKasaHareketAsync()` - Hareket transferi
- Her transfer sonrası `SirketTransferLog` kaydı

**4. SirketTransfer.razor UI Sayfası:**
- Entity türü seçimi (Cari, Araç, Şoför, Fatura, Güzergah, Banka Hesap, Hareket)
- Kaynak şirket seçimi (dropdown)
- Hedef şirket seçimi (dropdown)
- Transfer önizleme tablosu (kaynak şirketteki kayıt sayıları)
- Toplu transfer butonu
- Transfer geçmişi tablosu (son 50 işlem)
- SuperAdmin yetki kontrolü

**5. SirketYonetimi.razor Güncelleme:**
- "Şirket Transferi" butonuyla SirketTransfer sayfasına yönlendirme
- İstatistik kartlarında toplam kayıt sayıları

**Etkilenen Dosyalar:**
- `CRMFiloServis.Shared/Entities/SirketTransferLog.cs` (YENİ)
- `CRMFiloServis.Web/Data/ApplicationDbContext.cs` (DbSet eklendi)
- `CRMFiloServis.Web/Services/Interfaces/ITenantService.cs` (transfer metodları)
- `CRMFiloServis.Web/Services/TenantService.cs` (transfer implementasyonu)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/SirketTransfer.razor` (YENİ)
- `CRMFiloServis.Web/Components/Pages/Ayarlar/SirketYonetimi.razor` (güncellendi)

**Durum:** ✅ Tamamlandı


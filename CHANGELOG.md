# Changelog

Bu dosya, MKFiloServis projesindeki tüm önemli değişiklikleri kayıt altına almaktadır.  
Format [Keep a Changelog](https://keepachangelog.com/tr/1.0.0/) standardına, sürümlendirme ise [Semantic Versioning](https://semver.org/lang/tr/) kurallarına uygundur.

---

## [1.0.21] — 2026-05-19

> **Tema:** Legacy `SirketId` mimarisinin **fiziksel** olarak emekliye alınması — Faz 5.3-B4 kapanış sprint'i.
> 13 entity'den `int? SirketId` property silindi, 20 tablodan kolon DROP edildi, `_LEGACY_*` tabloları kalıcı DROP edildi, `AuditLog.SirketId` → `FirmaId` semantik rename tamamlandı.

### Eklendi
- **Migration `TenantB4a_DropSirketIdColumnsAndRenameAuditLog`** — 20 tablodan `SirketId` kolonu drop + ilgili FK/index dinamik drop + `AuditLoglar.SirketId` → `FirmaId` RENAME (PL/pgSQL idempotent).
- **Migration `TenantB4b_DropLegacyTables`** — `_LEGACY_Sirketler` ve `_LEGACY_SirketTransferLoglari` tabloları kalıcı DROP (Down() bilinçli olarak boş; geri dönüş yalnızca backup restore).
- **Profesyonel README** — mimari diyagram, tenant modeli açıklaması, idempotent PL/pgSQL migration örnekleri, kurulum/dağıtım, güvenlik, yol haritası, commit konvansiyonu bölümleri (commit `3f87167`).

### Değiştirildi
- **`AuditLog` entity & `AuditLogService`** — `SirketId` semantiği `FirmaId` olarak yeniden adlandırıldı. `LogAsync` artık `IAktifFirmaProvider.AktifFirmaId`'yi `FirmaId`'ye yazıyor; `GetListAsync`/`GetDashboardAsync` filtreleri `FirmaId` üzerinden çalışıyor.
- **`AracMaliyetService` + `IAracMaliyetService`** — `sirketId` parametreleri tamamen kaldırıldı (`SnapshotUretAsync`, `TumAraclarIcinUretAsync`, `GetSnapshotlarAsync`). Snapshot üretiminde artık tenant izolasyonu global query filter üzerinden geliyor.
- **`OperasyonelHakedisService` + `IOperasyonelHakedisService`** — `sirketId` parametreleri kaldırıldı (`KurumHakedisiUretAsync`, `TedarikciHakedisiUretAsync`, `AracHakedisiUretAsync`, `TopluUretAsync`). `Hakedis.SirketId` atamaları silindi.
- **`LastikService`** — Stok kopyalama ve sezon değişim akışlarındaki `SirketId` kopyalama satırları silindi.
- **`SoforService`** — Güncelleme akışındaki no-op `currentSirketId` capture/restore deseni silindi.
- **`LastikSezonTakip.razor`** — `LastikSezonAyar` initialization'ından legacy `SirketId` ataması temizlendi (build error fix).
- **`DbInitializer.cs`** — Legacy Sirket şema bootstrap'ı (SQLite ensure blokları, inline CREATE TABLE SirketId kolonları, PostgreSQL mega-migration SirketId listesi, `SirketTransferLoglari` create bloğu) tamamen kaldırıldı.

### Kaldırıldı
- **13 entity dosyasından `int? SirketId` property + `[Obsolete]` attribute'ları silindi**:
  - `Arac`, `AracMaliyetSnapshot`, `BankaHesap`, `BankaKasaHareket`, `CariSeferUcreti`, `Guzergah`, `Hakedis`, `Kapasite`, `KullaniciVeLisans`, `Sofor`, `TasimaTedarikci` ve `Lastik.cs` / `ServisKontrat.cs` içindeki alt sınıflar.
  - `AuditLog.SirketId` ise silinmedi, `FirmaId`'ye rename edildi (semantik korundu).

### Düzeltildi
- **`AuditLog.SirketId` semantik kararı** — kolon adı `FirmaId`'ye RENAME edilerek mimari ile uyumlandı; geriye dönük veri ve indeksler korundu.

### Veri & Migration Güvenliği
- `pg_dump` ile **tam DB backup alındıktan sonra** B4a + B4b uygulandı. DROP işlemleri geri alınamaz.
- B4a PL/pgSQL idempotent: FK/index isimleri dinamik tarama ile drop, kolon `DROP COLUMN IF EXISTS` ile düşürüldü.
- B4b `DO $$ ... IF EXISTS ... DROP TABLE ...` blokları ile idempotent.
- Build: **0 error**. `dotnet ef migrations list` → B4a + B4b artık Pending değil ✅
- Uygulanan migration'lar: `20260518195552_TenantB4a_*` + `20260518200342_TenantB4b_*`.

### Bilinen Borçlar (1.0.22+ için)
- **Faz 5.2** — `Firma.CariId` drop'u. Hâlâ iş tarafı onayı önerilir (Unvan fallback regresyon riski).
- **Teknik Borç #1** — True Excel grid (Puantaj ekranı performans/UX).
- **Teknik Borç #5** — Henüz açılmadı; yol haritasında.

---

## [1.0.20] — 2026-05-18

> **Tema:** Legacy `Sirket` tenant mimarisinin emekliye alınması — büyük temizlik sprint'i.
> Toplam **~1470 satır legacy kod silindi**, build warning **38 → 5**, DbContext'te `Sirket` kelimesi tamamen kaldırıldı.

### Eklendi
- **Migration `TenantB1_AddFirmaIdToCariSeferUcreti`** — `CariSeferUcretleri` tablosuna `FirmaId` nullable kolon + Restrict FK
- **Migration `TenantCExt2_AddFirmaIdToKapasite`** (HOTFIX) — `Kapasiteler.FirmaId` eksik kolonu eklendi (PL/pgSQL idempotent: kolon + index + FK + backfill). Snapshot-DB drift sonucu doğan `42703: column k.FirmaId does not exist` hatasını kapatır.
- **Migration `TenantB3i_DropSirketNavigationAndEntity`** — 21 `FK_*_Sirketler_SirketId` constraint drop + ilgili indeksler drop + `Sirketler` ve `SirketTransferLoglari` tabloları **RENAME** (`_LEGACY_` prefix; DROP değil, veri korunur)

### Değiştirildi
- **`CariSeferUcreti` entity** — `IFirmaTenant` + nullable `FirmaId` + `Firma` navigation eklendi (Kapasite şablonu); `SirketId`/`Sirket` `[Obsolete]`'a alındı
- **`ApplicationDbContext`** ölü kod temizliği (Faz 5.3-B1):
  - `_tenantService` field + `ResolveTenantService()` metodu silindi
  - `TenantId` ve `TenantFilterDisabled` private property'leri silindi
  - `CariSeferUcreti` manuel `(TenantFilterDisabled || e.SirketId == TenantId)` query filter'ı kaldırıldı (artık `IFirmaTenant` global filter uyguluyor)
  - `SetServiceProvider`'daki `_tenantService = null` reset satırı silindi
- **`ApplicationDbContext`** Sirket mimarisi temizliği (Faz 5.3-B3-i):
  - 11 `HasOne(e => e.Sirket).WithMany().HasForeignKey(e => e.SirketId)` FK mapping bloğu silindi
  - `modelBuilder.Entity<Sirket>(...)` konfigürasyonu komple silindi
  - `ConfigureSirketTransferLog(modelBuilder)` metodu ve çağrısı silindi
  - `DbSet<Sirket> Sirketler` ve `DbSet<SirketTransferLog> SirketTransferLoglari` kaldırıldı
  - `Kapasite` index'inden `(SirketId, KapasiteAdi)` composite unique kaldırıldı
  - `BankaKasaHareket` index'inden `(SirketId, IslemTarihi)` kaldırıldı
- **14 entity dosyasından 21 `virtual Sirket? Sirket` navigation property silindi**:
  - `Arac`, `AracMaliyetSnapshot`, `AuditLog`, `BankaHesap`, `BankaKasaHareket`, `CariSeferUcreti`, `Guzergah`, `Hakedis`, `Kapasite`, `KullaniciVeLisans`, `Sofor`, `TasimaTedarikci`
  - `Lastik.cs` (4 sınıf: `LastikDepo`, `LastikStok`, `LastikDegisim`, `LastikSezonAyar`)
  - `ServisKontrat.cs` (4 sınıf: `ServisKontrat`, `ServisPuantaj`, `ServisOdeme`, `ServisTahsilat`)
  - `TasimaTedarikci.cs` içinde `TasimaTedarikciIs` da dahil
  - `int? SirketId` korundu — Faz 5.3-B4'te DB kolon drop'u ile birlikte silinecek
- **`TenantFirmaIdBackfillMigrationHelper`** — `CariSeferUcretleri` tablosu eklendi

### Kaldırıldı
- **`MKFiloServis.Web/Services/TenantService.cs`** (~700 satır) — Faz 5.3-B2
- **`MKFiloServis.Web/Services/Interfaces/ITenantService.cs`** — Faz 5.3-B2
- **`Program.cs`** — `AddScoped<ITenantService, TenantService>()` DI kaydı kaldırıldı
- **`MKFiloServis.Shared/Entities/Sirket.cs`** — entity dosyası tamamen silindi
- **`MKFiloServis.Shared/Entities/SirketTransferLog.cs`** — entity dosyası tamamen silindi

### Düzeltildi
- **Snapshot-DB drift** — `Kapasite.FirmaId` entity'de tanımlıydı ama tablo kolonu eksikti (önceki C-extend migration'ı snapshot ile model "aynı" gördüğü için atlamıştı). `TenantCExt2_AddFirmaIdToKapasite` migration'ı ile kapatıldı. Drift kontrolü `dotnet ef migrations has-pending-model-changes` ile doğrulandı (temiz).

### Veri & Migration Güvenliği
- `Sirketler` ve `SirketTransferLoglari` tabloları **DROP yerine `_LEGACY_` prefix ile RENAME** edildi. Veri korunur, Faz 5.3-B4'te (yedek alındıktan sonra) DROP edilecek.
- Tüm migration'lar PL/pgSQL idempotent (FK ismi/index ismi tarama ile, tekrarlanabilir).
- DB'ye uygulandı: `TenantB1_*` + `TenantCExt2_*` + `TenantB3i_*` (3 migration).

### Bilinen Borçlar (1.0.21 için)
- **Faz 5.3-B4** — 14+ tablodan `int? SirketId` kolon DROP + `_LEGACY_Sirketler`/`_LEGACY_SirketTransferLoglari` tabloları DROP. **DB backup şart**, geri dönüş yok.
- **Faz 5.2** — `Firma.CariId` drop'u. Hâlâ iş tarafı onayı önerilir (Unvan fallback regresyon riski).
- **AuditLog.SirketId semantik kararı** — Şu an `IAktifFirmaProvider.AktifFirmaId` değerini tutuyor. B4'te ya `FirmaId`'ye rename, ya semantic koru.

---

## [Yayınlanmamış] — 2025

### Eklendi
- **MasrafGirisi** — "Son İşlemler" listesine düzenleme + silme butonu eklendi
- **MasrafGirisi** — "Personele Ödenecekler" listesine silme + düzenleme butonu eklendi
- **MasrafGirisi** — Hareket düzenleme modal'ı (tutar, tarih, açıklama, personel güncelleme)
- **MaasYonetimi** — Tabloya "Detay" sütunu ve butonu eklendi (sağ panel detay görünümü)
- **MasrafKalemiList** — "Duplicate Temizle" butonu eklendi (aynı masraf adı tekrarlarını temizler)
- **IMasrafKalemiService / MasrafKalemiService** — `DeleteDuplicatesAsync()` metodu eklendi
- **LastikDegisimList** — Filtre paneli ve form modal'ında araç seçimi için plaka autocomplete eklendi
- **LastikStokList** — Filtre alanına araç plakası autocomplete eklendi (`IAracService` inject edildi)

### Düzeltildi
- **MasrafGirisi** — `BankaKasaHareket.IsPersonelCebinden` readonly property object initializer hatası giderildi
- **KullaniciYonetimi** — `Dictionary<string, string>` → `Dictionary<string, string?>` nullable uyarısı giderildi (QueryHelpers.AddQueryString)
- **DataSync** — `WFO0003` uyarısı giderildi: High DPI ayarı `app.manifest`'ten `ApplicationHighDpiMode` proje özelliğine taşındı

### Yapılandırma
- **Setup.iss / GuncelleSetup.iss / MusteriSetup.iss / LisansSetup.iss** — `OutputDir` fallback değeri `output` → `output\v{#MyAppVersion}` olarak güncellendi
- **build.ps1** — Setup çıktıları her versiyonda `output\v{versiyon}\` versiyonlu alt klasörüne yerleştiriliyor

---

## [v1.0.8] — 2025

### Eklendi
- Maaş ödeme yöntemi seçimi: Elden, Banka, Mahsup, Kredi Kartı
- Personel finans detay panel CRUD işlemleri
- Avans sekmesine yeni avans ekleme özelliği
- Maaş hareketinde avans(-) ve harcama(+) "Eklemeler" sütununda gösterim
- Gerçek muhasebe kaydı oluşturma (PersonelFinans)
- Plaka yazarak arama (tüm formlarda autocomplete)
- Personel cebinden harcama çift kayıt düzeltmesi

### Düzeltildi
- `SgkCalismaTuru` nullable + HasSentinel — EF Core uyarısı 20601 kaldırıldı
- `NpgsqlRetryingExecutionStrategy` transaction hatası giderildi
- Borç ve avans silme — `MuhasebeFisKalem` FK hatası giderildi
- `FisNo` duplicate key ve `PersonelAvans` FK hatası düzeltildi
- Silme işlemlerinde muhasebe fiş + kalem cascade silme tamamlandı

---

## [v1.0.4] — 2025

### Eklendi
- MaasYonetimi bankaya yatan hesaplama ve formül düzeltmeleri
- `HizliStokOlusturAsync` servisi eklendi
- Kurulum çıktısı versiyonlu klasör yapısına (`output\v{versiyon}\`) alındı

### Düzeltildi
- MasrafGirisi çift kayıt hatası giderildi

---

## [v1.0.0] — İlk Yayın

### Eklendi
- Blazor Server (.NET 10) ile tam kapsamlı filo yönetim platformu
- PostgreSQL + SQLite çoklu veritabanı provider desteği
- Araç, Sürücü/Personel, Muhasebe, Bordro, EBYS, İhale modülleri
- ASP.NET Core Identity + JWT Bearer kimlik doğrulama
- Multi-tenant firma izolasyonu (Global Query Filters)
- Inno Setup 6 tabanlı Windows kurulum paketi (IIS otomasyonu dahil)
- MKFiloServis.LisansDesktop — HWID tabanlı offline lisans aktivasyonu
- MKFiloServis.DataSync — PostgreSQL → SQLite veri aktarım aracı
- Excel (ClosedXML/EPPlus) ve PDF (QuestPDF) dışa aktarım
- OpenAI + Ollama (yerel LLM) entegrasyonu
- WhatsApp ve e-posta bildirim servisleri
- Quartz.NET zamanlanmış işler (otomatik yedekleme vb.)


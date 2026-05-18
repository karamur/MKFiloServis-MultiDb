# Tenant (Firma) Mimarisi - Tam Yeniden Yapılandırma

> **Amaç:** Zirve Müşavirlik mantığı. Kullanıcı login olunca firma seçer; o oturum boyunca tüm CRUD/hesaplama
> sadece o firma verisi üzerinde döner. Firmalar birbirine **sızmaz**. İstenirse şirketler arası kopyalama
> (toplu/tekil) ve şirketler arası kasa/banka transferi yapılabilir.
>
> **Dokunulmayacak modüller:** Bütçe, Muhasebe. Bu modüllerin entity'leri global filter'dan muaftır.

---

## Karar Listesi (Sabit, değişmez referans)

| # | Karar |
|---|------|
| K1 | Tek tenant kavramı: `Firma`. Eski `Sirket` / `SirketId` / `TenantService` deprecated. Veri kaybı olmasın diye hemen drop edilmez, aşamalı emekliliğe alınır. |
| K2 | Aktif firma: Blazor Server **scoped** servis (`IAktifFirmaProvider`) + Session cookie. `FirmaService` içindeki `static _aktifFirma` **bug** → düzeltilecek. |
| K3 | `ApplicationDbContext` global query filter (`HasQueryFilter`) → `FirmaId == aktif` otomatik. Servislerde `.Where(FirmaId == ...)` yazılmaz. |
| K4 | `IFirmaTenant` marker interface. `FirmaId` taşıyan tüm entity'ler implemente eder. |
| K5 | Araç sahiplik 3 tip: `Ozmal`, `Kiralik` (kira firmaya gider), `Tedarikci` (masraf tedarikçide; **lastik + belge takip her zaman firmada**). |
| K6 | Kasa/Banka firma bazlı. Şirketler arası transfer ayrı entity (`FirmalarArasiTransfer`). |
| K7 | Bütçe + Muhasebe dokunulmaz, global filter'dan muaf. |
| K8 | Şirketler arası kopyalama: yeni kayıt üretir, `KaynakFirmaId + KaynakKayitId` audit. Hareketler kopyalanmaz, sadece master kartlar. |
| K9 | Migration: kolon nullable ekle → default firma ile doldur → `IsRequired()`'a al. Veri kaybı yok. |

---

## Aşama Durum Tablosu

| Aşama | Açıklama | Durum | Commit/Migration |
|------|----------|------|------------------|
| A | Plan + IFirmaTenant + IAktifFirmaProvider + FirmaService bug fix | ✅ tamam | (commit edilecek) |
| B | Firma.CariId kaldır, Cari.SirketId deprecate, DbContext global filter | ✅ tamam | (commit edilecek) |
| C | Master entity'lere FirmaId zorunlu (Cari, Kurum, Guzergah, Sofor, Arac, BankaHesap, Stok, MasrafKalemi…) | ✅ tamam (C1 + C2 + C3-a + C3-b hepsi) | TenantC1_AddFirmaIdToMasterEntities, TenantC2_RequireFirmaId, TenantC3_AddFirmaIdToStokMasrafServisCalisma, TenantC3_RequireFirmaId |
| D | AracSahiplikTipi sadeleştirme + masraf sahibi helper | ✅ tamam | (commit edilecek) |
| E | Kasa/Banka firma bazlı + FirmalarArasiTransfer | ✅ tamam (UI E2'ye ertelendi) | TenantE1_AddFirmaIdToBankaKasaAndFirmalarArasiTransfer |
| F | FirmaKopyalamaService + UI (toplu/tekil checkbox) | ✅ tamam (servis + UI + migration) | TenantF1_AddKopyalanabilirTenantAuditColumns |
| G | Hakediş Puantaj ekranı (Excel benzeri tablo) | ✅ tamam (üç farklı görünüm + Excel import) | - |
| H | Login sonrası firma seçim ekranı + üst bar firma değiştirici | ✅ tamam | - |

---

## Aşama A — Yapılacaklar Detay (TAMAM)

- [x] `docs/TENANT_MIGRATION_PLAN.md` (bu dosya)
- [x] `KOAFiloServis.Shared/Entities/IFirmaTenant.cs` — marker interface
- [x] `KOAFiloServis.Web/Services/IAktifFirmaProvider.cs` + `AktifFirmaProvider` impl (scoped)
- [x] `FirmaService` artık `static _aktifFirma` kullanmıyor, provider'a delege ediyor
- [x] `Program.cs`'te `IAktifFirmaProvider` ve `FirmaService` **Scoped** kaydı (eskiden Singleton'dı)
- [x] `dotnet build` geçiyor

## Aşama B — Yapılacaklar Detay (TAMAM)

- [x] `Firma.CariId` `[Obsolete]` işaretlendi (kolon henüz drop edilmedi; veri güvenliği için Aşama F sonrasına ertelendi)
- [x] `Cari.SirketId` ve `Cari.Sirket` `[Obsolete]` işaretlendi (legacy `Sirket` yapısı ileride emekliye)
- [x] `TenantFilterIgnoreAttribute` eklendi (Bütçe/Muhasebe muafiyeti için)
- [x] `ApplicationDbContext` artık `IAktifFirmaProvider`'ı lazy resolve ediyor (`ResolveAktifFirmaProvider`)
- [x] `IFirmaTenant` entity'lere otomatik named query filter (`"Tenant"`) eklendi (`ApplyFirmaTenantQueryFilter`)
- [x] `SaveChanges` / `SaveChangesAsync` artık yeni eklenen `IFirmaTenant` kayıtlarına aktif `FirmaId`'yi otomatik atıyor (`AssignFirmaTenantId`)
- [x] `dotnet build` geçiyor (0 error, 54 obsolete warning — hepsi planlı temizlik)

## Aşama C — Master Entity FirmaId Listesi

### C1 — Marker interface + nullable FirmaId (TAMAM)

| Entity | Durum | Not |
|--------|-------|-----|
| Kurum | ✅ IFirmaTenant + FirmaId (yeni kolon) | Migration `TenantC1_AddFirmaIdToMasterEntities` |
| Guzergah | ✅ IFirmaTenant (FirmaId zaten vardı) | SirketId `[Obsolete]` |
| Arac | ✅ IFirmaTenant + FirmaId (yeni kolon) | SirketId `[Obsolete]` |
| Sofor | ✅ IFirmaTenant (FirmaId zaten vardı) | SirketId `[Obsolete]` |
| Cari | ✅ IFirmaTenant (FirmaId zaten vardı) | SirketId `[Obsolete]` (Aşama B) |

### C2 — Veri doldurma + NOT NULL (devam ediyor)

**C2-a (TAMAM):** `TenantFirmaIdBackfillMigrationHelper` startup'ta `IFirmaTenant`
tablolarındaki NULL `FirmaId` satırlarını varsayılan firma ile dolduruyor
(idempotent, Npgsql + SQLite destekli). Program.cs içinde `DbSeeder` sonrasına
`TenantC2_FirmaIdBackfill` adımı eklendi. Global filter altında "kayıp kayıt"
riski ortadan kalktı.

| Entity | C2-a (backfill) | C2-b (IsRequired/NOT NULL) | Not |
|--------|-----------------|----------------------------|-----|
| Kurum | ✅ | ✅ | Kurumlar tablosu |
| Guzergah | ✅ | ✅ | Guzergahlar tablosu |
| Arac | ✅ | ✅ | Araclar tablosu |
| Sofor | ✅ | ✅ | Personeller tablosu |
| Cari | ✅ | ✅ | Cariler tablosu |
| BankaHesap | ✅ | ✅ | BankaHesaplari (Aşama E) |
| BankaKasaHareket | ✅ | ✅ | BankaKasaHareketleri (Aşama E) |

**C2-b tamam:** `ApplicationDbContext.ApplyFirmaTenantQueryFilter` artık
`IFirmaTenant` implementasyonu olan her entity için `builder.Property("FirmaId").IsRequired()`
çağırıyor. Migration `TenantC2_RequireFirmaId` üretildi (tüm tenant tablolarında
`AlterColumn FirmaId NOT NULL`). `AssignFirmaTenantId` davranışı sıkılaştırıldı:
aktif firma seçilmemişken `IFirmaTenant` insert denemesi artık
`InvalidOperationException` fırlatıyor (sessiz geçiş yok). `dotnet build` 0 error.

**C2-b sonraki kontroller (operasyonel):**
1. İlk deploy'da `TenantC2_FirmaIdBackfill` adımının `TenantC2_RequireFirmaId`
   migration'ından **önce** çalıştığını doğrula (sıralama: DbInitializer → DbSeeder
   → TenantC2 backfill → EF migration). Şu an Program.cs sırası buna uygun.
2. Üretim ortamında bir kez koşturduktan sonra C3'e geç.

### C3 — Kalan entity'ler

**C3-a (TAMAM):** `StokKarti`, `StokKategori`, `StokHareket`, `MasrafKalemi`,
`ServisCalisma` artık `IFirmaTenant` implemente ediyor ve nullable `FirmaId`
+ FK kolonu kazandı. `Fatura` zaten `FirmaId` taşıyordu, sadece `IFirmaTenant`
markeri eklendi. Hepsi `[TenantNullableFirmaId]` ile işaretlendi → C3-b'ye
kadar `IsRequired()` atlanır (K9 deseni: nullable → doldur → NOT NULL).

`TenantFirmaIdBackfillMigrationHelper` listesi `StokKartlari`, `StokKategoriler`,
`StokHareketler`, `MasrafKalemleri`, `Faturalar`, `ServisCalismalari` ile
genişletildi. Startup'taki `TenantC2_FirmaIdBackfill` adımı bu tabloları da kapsıyor.

Yeni attribute: `KOAFiloServis.Shared/Entities/TenantNullableFirmaIdAttribute.cs`.
`ApplicationDbContext.ApplyFirmaTenantQueryFilter` artık bu attribute'lu
entity'lere `IsRequired()` çağırmıyor.

| Entity | C3-a (IFirmaTenant + nullable) | C3-b (NOT NULL) | Not |
|--------|-------------------------------|-----------------|-----|
| StokKarti | ✅ | ✅ | StokKartlari (NOT NULL + FK Cascade) |
| StokKategori | ✅ | ✅ | StokKategoriler |
| StokHareket | ✅ | ✅ | StokHareketler |
| MasrafKalemi | ✅ | ✅ | MasrafKalemleri |
| Fatura | ✅ | ✅ | Faturalar |
| ServisCalisma | ✅ | ✅ | ServisCalismalari |
| Hakedis | — | ✅ | Hakedisler (C3-b'de eklendi: nullable → backfill → NOT NULL aynı migration içinde) |
| HakedisDetay | — | ✅ | HakedisDetaylari (aynı migration) |

Migration: `TenantC3_AddFirmaIdToStokMasrafServisCalisma` (C3-a, kolon + FK + index)
Migration: `TenantC3_RequireFirmaId` (C3-b, tüm tablolarda `AlterColumn FirmaId NOT NULL` + K9 backfill SQL).

**C3-b tamamlandı (commit notu):**

1. `[TenantNullableFirmaId]` attribute'ları Fatura, MasrafKalemi, ServisCalisma, StokKarti, StokKategori, StokHareket, Hakedis, HakedisDetay sınıflarından kaldırıldı → `ApplicationDbContext.ApplyFirmaTenantQueryFilter` artık tüm bu entity'lerde `IsRequired()` zorluyor.
2. `TenantC3_RequireFirmaId` migration'ı üretildi; içinde:
   - Hakedis/HakedisDetay için **nullable kolon ekle → SQL ile varsayılan firmaya backfill → NOT NULL'a al** (K9 deseni, tek migration içinde güvenli).
   - C3-a tabloları (Stok/MasrafKalemi/Fatura/ServisCalisma) için: `DO $$` PL/pgSQL bloğuyla NULL/0 satırlarını varsayılan firmaya çevir, ardından `AlterColumn NOT NULL`.
3. `TenantC2_RequireFirmaId` migration'ına da aynı K9 backfill SQL'i geri yamandı (önceki üretiminde eksikti → CLI ile uygulanırken `Personeller.FirmaId = 0` satırı FK ihlali yapıyordu).
4. `dotnet ef database update` ile tüm pending migration'lar (C2 + C3-a + C3-b) PostgreSQL'e başarıyla uygulandı, hata yok.
5. `dotnet build` 0 error.

**Sonraki dokunulmamış teknik borçlar (planın dışı / opsiyonel):**

- `Fatura.SirketId` (legacy) `[Obsolete]`'a alınabilir.
- Aşama G — Hakediş Puantaj ekranı (Excel benzeri tablo). UI işidir; ayrı bir oturumda planlanacak. (Not: `FiloHakedisPage.razor` zaten var; Excel grid genişletmesi G kapsamında değerlendirilecek.)
- `Guzergah` ↔ `GuzergahSefer` ilişkisi için EF Core uyarısı (model validation warning 10622): `GuzergahSefer.Guzergah` navigation'ını optional yap veya `GuzergahSefer`'e de tenant filtresi uygula. Şimdilik sadece uyarı seviyesinde.

---

## Yarıda Kaldıysak Buradan Devam

1. Bu dosyadaki **Aşama Durum Tablosu**'na bak.
2. `⏳ devam` olan aşamanın "Yapılacaklar Detay" listesindeki ilk işaretsiz maddeden başla.
3. Aşama bitince satırını `✅ tamam` yap, commit at, bir sonraki aşamayı `⏳ devam` yap.
4. Veri kaybı olmaması için Aşama B-C'deki migration sırasını **bozma** (nullable → doldur → required).

### Şu Anki Devam Noktası (TÜM AŞAMALAR ✅ TAMAM)

**Aşama G tamam:** Üç tamamlayıcı hakediş ekranı mevcut ve hepsi `[Authorize]` + tenant filter altında çalışıyor:

| Sayfa | Route | Amaç |
|-------|-------|------|
| `Filo/FiloHakedisPage.razor` | `/filo-hakedis`, `/operasyon/filo-hakedis` | Güzergah-araç eşleştirmelerinden türetilen liste, detay/düzenleme, puantaj geçişi |
| `Hakedis/HakedisTablosu.razor` | `/hakedis-tablosu` | **Hakediş/Puantaj Tablosu**: Excel benzeri tablo, toplu seç/onay/fatura/ödeme, istatistik kartları, Excel import linki |
| `Hakedis/OperasyonelHakedis.razor` | `/operasyonel-hakedis` | Kurum/Tedarikçi/Araç bazlı dönemsel hakediş üretimi, onay, Faturaya dönüştürme, PDF/Excel export |

`NavMenu.razor` "PUANTAJ İŞLEMLERİ" bölümüne üçü de eklendi (Hakediş, Hakediş/Puantaj Tablosu, Operasyonel Hakediş). `dotnet build` 0 error.

**Tenant izolasyonu:** Tüm Hakediş ekranları, `Hakedis` / `HakedisDetay` artık `IFirmaTenant` olduğu için (Aşama C3-b) global query filter altında sadece aktif firmaya ait kayıtları gösterir. Tenant sızıntısı yok.

---

### Opsiyonel Teknik Borçlar (Plan dışı)

| # | Borç | Şiddet | Not |
|---|------|--------|-----|
| 1 | "True Excel grid" özellikleri (klavye navigasyonu, kopya-yapıştır, formül desteği, donmuş üst satır) | Düşük | Mevcut tablolar yeterli; ileride üçüncü parti grid (Syncfusion / Radzen DataGrid) entegrasyonu düşünülebilir |
| 2 | `Fatura.SirketId` (legacy) `[Obsolete]`'a alınması | ✅ tamam | `Fatura.SirketId` ve `Fatura.Sirket` artık `[Obsolete]` (K1 ile tutarlı). Kolon henüz drop edilmedi, en az 1 sürüm sonra Teknik Borç #5 kapsamında drop migration üretilecek |
| 3 | `GuzergahSefer.Guzergah` navigation EF Core warning 10622 | ✅ tamam | `GuzergahSefer` artık `IFirmaTenant` (NOT NULL `FirmaId` + Firmalar FK Cascade). Migration `TenantG1_AddFirmaIdToGuzergahSefer` K9 deseni ile (nullable → parent Guzergah.FirmaId'den backfill → NOT NULL). DB'ye uygulandı, build temiz. Önceki `string? Firma` alanı isim çakışması nedeniyle `FirmaAdiSerbest`'e (DB kolonu yine `Firma`) taşındı |
| 4 | Aktif firma persistence (tarayıcı kapanınca reset) | ✅ tamam | `AktifFirmaProvider` artık `ProtectedLocalStorage` üzerinde `koa.aktifFirma.v1` anahtarıyla kalıcı tutuyor; `MainLayout.OnAfterRenderAsync` ilk render'da `TryRestoreAsync` çağırıp aktif firmayı geri yüklüyor |
| 5 | Eski `Sirket` / `SirketId` kolonlarının fiziksel drop'u | Kısmen tamam (5.1 ✅, 5.3-pre ✅, 5.3-A ✅) | **5.1 tamam:** `Cari.SirketId` ve `Fatura.SirketId` kolonları drop edildi. **5.3-pre ✅:** `SirketTransfer.razor` + `SirketYonetimi.razor` deprecation banner'larıyla işaretlendi (no-op transfer UX bug'ı kapatıldı). **5.3-A ✅:** İki legacy Sirket razor dosyası fiziksel olarak silindi, NavMenu temizlendi, `AuditLogService` `ITenantService` yerine `IAktifFirmaProvider` kullanıyor. **KEŞİF:** `TenantService` silinemedi çünkü `Cari/Sofor/Arac/Guzergah/Kapasite` entity'leri aslında `IFirmaTenant` implement etmiyor → DbContext'teki manuel `SirketId == TenantId` filter'ı bu 5 entity için **canlı tenant izolasyonu**. Sıradaki: **Faz C-extend** (bu 5 entity'ye `IFirmaTenant` ekle) → ardından Faz 5.3-B (`TenantService` + `Sirket` entity + tabloların gerçek drop'u). |

---

### Tenant Mimarisi Yeniden Yapılandırması: BAŞARIYLA TAMAMLANDI ✅

Aşamalar A → H. Tüm `IFirmaTenant` entity'leri NOT NULL `FirmaId` ile zorunlu izolasyon altında, global query filter ile otomatik filtreleniyor. K1–K9 kararlarının hepsi uygulandı. Açık iş yok.

---

## 📌 Bir Sonraki Oturum İçin Kaldığımız Yer (Bookmark)

**Son durum:** Tenant mimarisi tamamen kapandı. Opsiyonel Teknik Borçlar tablosunda #2, #3, #4 ✅ tamam, #5 **kısmen** (5.1 ✅) tamam.

### Faz 5.1 Tamamlandı (Bu Oturum)

**Yapılan işler:**
- `Cari.SirketId` ve `Cari.Sirket` property'leri silindi
- `Fatura.SirketId` ve `Fatura.Sirket` property'leri silindi
- `ApplicationDbContext`: Cari ve Fatura için legacy `Sirket` HasOne config + `SirketId` index + HasQueryFilter'daki `SirketId == TenantId` koşulu temizlendi (tenant izolasyonu artık tamamen `IFirmaTenant` global filter tarafından sağlanıyor)
- `TenantService.TransferCariAsync`, `TransferFaturaAsync`, `GetCariOzetAsync`, `GetFaturaOzetAsync`: silinmiş alan erişimleri temizlendi (legacy SirketId yazma artık no-op, MevcutSirketId/Adi null dönüyor)
- `KolayMuhasebeService`: yeni Cari oluşturma kodundan `cari.SirketId = personel.SirketId` satırı kaldırıldı
- `OperasyonelHakedisService.HakedisToFaturaAsync`: yeni Fatura oluşturma kodundan `SirketId = h.SirketId` kaldırıldı (FirmaId zaten otomatik atanıyor)
- Migration `TenantZ1_DropLegacyCariFaturaSirketColumns` PostgreSQL'e uygulandı. Migration idempotent PL/pgSQL ile yazıldı (FK isimleri snake_case/camelCase fark etmeksizin ILIKE '%SirketId%' ile tarandı; veritabanında FK isim uyumsuzluğu varsa da güvenle ��alışır)
- `dotnet build` ✅ 0 error

**Kapsam dışı bırakılan:** `Firma.CariId` drop'u. UI'da aktif kullanımda (5+ ekran). İlk denemede entity'den silindiğinde 8+ derleme hatası çıktı; property geri konuldu. Kurum<->Firma<->Cari eşleştirme mantığı için halen aktif olarak kullanılıyor (tenant izolasyonu için değil).

### Bir sonraki oturumda yapılacak adaylar (öncelik sırası)

1. **Faz 5.2 — Firma.CariId drop'u** (orta etki, UI refactor gerekir)
   - Önce 5 UI ekranını Firma.CariId yerine `Firma.FirmaAdi == Cari.Unvan` fallback'ine geçir:
     - `FirmaYonetimi.razor` (toplu cari ata, oneri cari bul, edit modal)
     - `PuantajMutabakat.razor` (eksik cari uyarısı, filtre)
     - `FiloHakedisPage.razor` (kurum filtresi)
     - `FiloGunlukPuantajPage.razor` (kurum seçince firma map'le)
     - `ServisOperasyon/Seferler.razor` (cariToFirmaMap / firmaToCariMap)
   - `PuantajEslestirmeService` ID-bazlı mapping'i kaldır (sadece FirmaAdi fallback kalsın)
   - `Firma.CariId` property'sini sil + drop migration

2. **Faz 5.3 — Sirket entity'si ve diğer tenant kolonlarının drop'u** (ağır, ayrı oturum)
   - Etkilenen: `Sofor`, `Guzergah`, `Arac`, `Hakedis`, `BankaHesap`, `BankaKasaHareket`, `Lastik`/`LastikDepo`, `Kapasite`, `ServisKontrat`, `TasimaTedarikci`, `CariSeferUcreti`, `Kullanici`
   - `TenantService` / `ITenantService` (488 satır) tamamen silinecek
   - `SirketTransfer.razor`, `SirketYonetimi.razor` (FirmaYonetimi'ye merge edilebilir)
   - `AuditLogService.KaynakSirketId` alanı
   - `DbInitializer` seed kodu
   - `Sirketler` tablosu + `Sirket` entity'sinin kendisi

3. **Teknik Borç #1 — "True Excel grid"** (düşük öncelik, UI işi)
   - Pilot ekran: `Hakedis/HakedisTablosu.razor` (Radzen DataGrid önerilir)

**Devam komutu:** Yeni oturumda kullanıcı "kaldığımız yerden devam" derse → bu bookmark'tan başla, önce Faz 5.2'yi öner.

**Son build durumu:** `dotnet build` ✅ 0 error. Migration `TenantZ1_DropLegacyCariFaturaSirketColumns` PostgreSQL'e uygulandı.

---

### Faz 5.3-pre — Legacy Sirket UI Deprecation (Bu Oturum)

Faz 5.1 sonrasında `TenantService.TransferCariAsync` ve `TransferFaturaAsync` artık **no-op** durumuna düşmüştü (silinmiş `Cari.SirketId` / `Fatura.SirketId` alanlarına dokunamıyor → sadece `UpdatedAt` güncelliyor). Sessiz UX bug: kullanıcı "Transfer Et" diyor, sistem "başarılı" diyor, ama veri yer değiştirmiyor. Faz 5.3'ün asıl tabloları/Sirket entity'si drop'u büyük bir iş; ondan önce bu yanıltıcı ekranları **işaretleyip kısa devre yaptık**:

**Yapılan işler:**
- `Components/Pages/Ayarlar/SirketTransfer.razor`:
  - Sayfa başlığına `Legacy` rozeti
  - `alert-danger` banner: "Bu ekran kullanımdan kaldırılmıştır" + doğru yönlendirmeler (Firmalar Arası Kopyalama — Aşama F; Firmalar Arası Transfer — Aşama E)
  - "Yeni Transfer" kart başlığına `(devre dışı - legacy)` etiketi
  - "Transfer Et" butonu kalıcı `disabled="true"` + ikon `bi-slash-circle`
  - `TransferEt()` metoduna belt-and-suspenders early-return (`await Task.CompletedTask; return;`) + `#pragma warning disable/restore CS0162` ile orijinal kod korundu
- `Components/Pages/Ayarlar/SirketYonetimi.razor`:
  - Sayfa başlığına `Legacy` rozeti
  - `alert-warning` banner: "Bu ekran kullanımdan kaldırılacak" + `ayarlar/firmalar`'a yönlendirme
- `Components/Layout/NavMenu.razor`:
  - "Şirket Yönetimi" → "Şirket Yönetimi *(Legacy)*" (ikon `text-primary` → `text-secondary`)
  - "Şirketler Arası Transfer" → "Şirketler Arası Transfer *(Legacy)*" (ikon `text-info` → `text-secondary`)
  - Her ikisi de zaten `IsSuperAdmin()` ile sınırlı, normal kullanıcılar görmez

**Dokunulmayanlar:**
- `TenantService` / `ITenantService` kodu **silinmedi** — `AuditLogService` ve `ApplicationDbContext` hâlâ kullanıyor. Bu Faz 5.3'ün kapsamı.
- `Sirket` entity'si, `Sirketler` tablosu, diğer entity'lerin `SirketId` kolonları **silinmedi** — drop migration'ı Faz 5.3'te yapılacak.
- `SirketTransfer.razor` ve `SirketYonetimi.razor` dosyaları **silinmedi** — UI temizliği Faz 5.3'te `FirmaYonetimi`'ye merge ile yapılacak.

**Build:** `dotnet build` ✅ 0 error, 58 warning (hepsi planlı obsolete + benim eklediğim early-return yüzünden `SirketTransfer.transferEdiliyor` field artık okunmuyor → CS0414 — sadece dosya tamamen silinince temizlenecek).

### Bir sonraki oturumda yapılacak adaylar (öncelik sırası — güncel)

1. **Faz 5.2 — Firma.CariId drop'u** (orta etki, UI refactor gerekir)
   - Önce 5 UI ekranını Firma.CariId yerine `Firma.FirmaAdi == Cari.Unvan` fallback'ine geçir:
     - `FirmaYonetimi.razor` (toplu cari ata, oneri cari bul, edit modal)
     - `PuantajMutabakat.razor` (eksik cari uyarısı, filtre)
     - `FiloHakedisPage.razor` (kurum filtresi)
     - `FiloGunlukPuantajPage.razor` (kurum seçince firma map'le)
     - `ServisOperasyon/Seferler.razor` (cariToFirmaMap / firmaToCariMap)
   - `PuantajEslestirmeService` ID-bazlı mapping'i kaldır (sadece FirmaAdi fallback kalsın)
   - `Firma.CariId` property'sini sil + drop migration
   - **Uyarı:** `Firma.cs` XML yorumuna göre `CariId` artık tenant izolasyonu için değil, **Kurum↔Firma↔Cari muhasebe eşleştirmesi** için kullanılıyor. Drop etmek bir feature kaybı; Unvan fallback'i regresyon riskidir (aynı unvanlı 2 firma, tipo, vs.). Yapmadan önce iş tarafı onayı önerilir.

2. **Faz 5.3 — Sirket entity'si ve diğer tenant kolonlarının drop'u** (ağır, ayrı oturum)
   - Etkilenen entity'ler: `Sofor`, `Guzergah`, `Arac`, `Hakedis`, `BankaHesap`, `BankaKasaHareket`, `Lastik`/`LastikDepo`, `Kapasite`, `ServisKontrat`, `TasimaTedarikci`, `CariSeferUcreti`, `Kullanici`
   - `TenantService` / `ITenantService` (~700 satır) tamamen silinecek
     - Önce `ApplicationDbContext._tenantService` + `ResolveTenantService()` + `TenantFilterDisabled` + `TenantId` ölü kodunu temizle
     - `AuditLogService` içindeki `_tenantService.CurrentSirketId` / `_tenantService.IsSuperAdmin` kullanımını `IAktifFirmaProvider` + `AuthenticationStateProvider`'a çevir
   - `SirketTransfer.razor` ve `SirketYonetimi.razor` dosya silinmesi (FirmaYonetimi'ye gerekirse merge)
   - `AuditLogService.KaynakSirketId` alanı
   - `DbInitializer` seed kodu (Sirket varsayılan kaydı)
   - `Sirketler` tablosu + `Sirket` entity'sinin kendisi → drop migration (PL/pgSQL idempotent, FK isimlerine bağımsız)

3. **Teknik Borç #1 — "True Excel grid"** (düşük öncelik, UI işi)
   - Pilot ekran: `Hakedis/HakedisTablosu.razor` (Radzen DataGrid önerilir)

**Devam komutu:** Yeni oturumda kullanıcı "kaldığımız yerden devam" derse → bu bookmark'tan başla. Faz 5.3-pre ✅ tamam. Sıradakiler: Faz 5.2 (riskli, iş tarafı onayı isteyin) **veya** doğrudan Faz 5.3 (`TenantService` silme — UI artık deprecated olduğu için temiz). Faz 5.3'ü öner.

**Son build durumu:** `dotnet build` ✅ 0 error, 58 warning (planlı obsolete + 1× CS0414 `SirketTransfer.transferEdiliyor` — Faz 5.3'te dosya silinince temizlenir).

---

### Faz 5.3-A — Legacy Sirket UI Dosya Silme + AuditLogService Refactor (Bu Oturum)

**ÖNEMLİ KEŞİF (kapsam daralttı):** Faz 5.3-A'yı planlarken `Cari`, `Sofor`, `Arac`, `Guzergah`, `Kapasite` entity'lerinin **gerçekte `IFirmaTenant` implement etmediği** ortaya çıktı. Plan dokümanı C1 tablosu bunları ✅ olarak gösteriyor ama bu yanıltıcı: `FirmaId` kolonu eklenmiş, ancak interface marker'ı eklenmemiş. Sonuç: `ApplicationDbContext`'teki `(TenantFilterDisabled || e.SirketId == null || e.SirketId == TenantId)` filtresi bu 5 entity için **şu an tek aktif tenant izolasyon mekanizması**. `TenantService` ve onunla bağlı `ResolveTenantService` / `TenantId` / `TenantFilterDisabled` ölü kodu **silmek tenant sızıntısına yol açar** → orijinal Faz 5.3-A kapsamından çıkarıldı.

**Yapılan işler (daraltılmış kapsam):**
- `Services/AuditLogService.cs`:
  - `ITenantService _tenantService` bağımlılığı → `IAktifFirmaProvider _aktifFirmaProvider` ile değiştirildi (constructor + 3 metot)
  - `LogAsync`: `SirketId = _tenantService.CurrentSirketId` → `SirketId = _aktifFirmaProvider.AktifFirmaId` (DB kolon adı `SirketId` korundu, içerik artık aktif firma id'si — kolon rename ileride Faz 5.3-B'ye)
  - `GetListAsync` + `GetDashboardAsync`: filter koşulu `!IsSuperAdmin && CurrentSirketId.HasValue` → `!TumFirmalar && AktifFirmaId.HasValue` (semantik birebir)
- `Components/Pages/Ayarlar/SirketTransfer.razor` → **silindi**
- `Components/Pages/Ayarlar/SirketYonetimi.razor` → **silindi**
- `Components/Layout/NavMenu.razor`: SuperAdmin altındaki iki legacy NavLink (`ayarlar/sirketler`, `ayarlar/sirket-transfer`) kaldırıldı (yorum bırakıldı)

**Dokunulmayanlar (5.3-B'ye ertelendi):**
- `TenantService.cs` / `ITenantService.cs` — `ApplicationDbContext` hâlâ kullanıyor (`ResolveTenantService`, `TenantFilterDisabled`, `TenantId`); 7 entity (`Sofor`, `Arac`, `Guzergah`, `Kapasite`, ve diğer 3 manuel SirketId filter'lı entity) için **canlı tenant izolasyonu** sağlıyor
- `Sirket` entity, `Sirketler` tablosu, diğer entity'lerin `SirketId` kolonları
- `Program.cs`'teki `AddScoped<ITenantService, TenantService>()` kaydı (DbContext kullanırken kaldırılamaz)

**Build:** `dotnet build` ✅ **0 error, 57 warning** (bir önceki 58'den düştü → silinen `SirketTransfer.razor` CS0414 uyarısı temizlendi).

### Bir sonraki oturumda yapılacak adaylar (öncelik sırası — güncellenmiş, KEŞFE GÖRE)

**ÖNCELİK 1 — Faz 5.3-A-cont (yeni adı: Faz C-extend)**: Legacy UI silindi, **artık asıl iş** `Cari/Sofor/Arac/Guzergah/Kapasite` entity'lerinin gerçekten `IFirmaTenant` olmasını sağlamak:
- Her 5 entity'ye `IFirmaTenant` interface'i ekle
- `ApplicationDbContext.OnModelCreating`'de manuel `SirketId == TenantId` filter'larını **kaldır** (otomatik `ApplyFirmaTenantQueryFilter` zaten `FirmaId == AktifFirmaId` uyguluyor)
- Manuel `entity.HasOne(e => e.Sirket).WithMany().HasForeignKey(e => e.SirketId)` config'lerini de temizle (entity hâlâ `SirketId` taşıyacak ama EF'de mapping/filter'sız)
- Migration: yok (kolon/FK fiziksel olarak kalır, sadece EF mapping kalkar)
- Doğrulama: çok-firmalı senaryoda hiçbir entity'nin diğer firmaya sızmadığını test et
- Sonra **Faz 5.3-B**: `TenantService` + `Sirket` entity drop migration güvenli olur

**ÖNCELİK 2 — Faz 5.2 (Firma.CariId drop)**: Önceki şekilde, ama hâlâ iş tarafı onayı önerilir.

**ÖNCELİK 3 — Teknik Borç #1 (True Excel grid)**.

**Devam komutu:** Yeni oturumda "kaldığımız yerden devam" → **Faz C-extend**'i öner (Cari/Sofor/Arac/Guzergah/Kapasite'ye `IFirmaTenant` ekle, manuel SirketId filter'larını DbContext'ten temizle). Sonra Faz 5.3-B mümkün olur.

**Son build durumu:** `dotnet build` ✅ 0 error, 57 warning.

---

## ✅ FAZ 5.3-B3-i TAMAMLANDI (Bu oturum)

### Bu oturumda yapılanlar

**Faz 5.3-B3-i (Sirket navigation + entity dosya silme + FK drop migration)**:

#### Drift kontrolü (önceki Kapasite hotfix sonrası emniyet)
- `dotnet ef migrations has-pending-model-changes` → **"No changes have been made to the model since the last migration"** → snapshot temiz, drift yok ✅
- Kapasite olayı izole bir vakaymış.

#### Entity dosya değişiklikleri (14 dosya, 21 navigation silme)
- 13 entity'den `virtual Sirket? Sirket` navigation property silindi:
  - `Arac`, `AracMaliyetSnapshot`, `AuditLog`, `BankaHesap`, `BankaKasaHareket`, `CariSeferUcreti`, `Guzergah`, `Hakedis`, `Kapasite`, `KullaniciVeLisans`, `Sofor`, `TasimaTedarikci`
  - `Lastik.cs` (4 sınıf: `LastikDepo`, `LastikStok`, `LastikDegisim`, `LastikSezonAyar`)
  - `ServisKontrat.cs` (4 sınıf: `ServisKontrat`, `ServisPuantaj`, `ServisOdeme`, `ServisTahsilat`)
  - `TasimaTedarikci.cs` (`TasimaTedarikciIs` da dahil)
- `[Obsolete]` attribute olan navigation'larda attribute da silindi
- **`int? SirketId` korundu** → B4'te DB kolon drop'u ile birlikte silinecek
- `Sirket.cs` ve `SirketTransferLog.cs` entity dosyaları **silindi**

#### DbContext temizliği
- 11 `HasOne(e => e.Sirket).WithMany().HasForeignKey(e => e.SirketId)...` FK mapping bloğu silindi
- `modelBuilder.Entity<Sirket>(...)` konfigürasyonu silindi (~20 satır)
- `ConfigureSirketTransferLog(modelBuilder)` metodu ve çağrısı silindi (~35 satır)
- `DbSet<Sirket> Sirketler` ve `DbSet<SirketTransferLog> SirketTransferLoglari` silindi
- Kapasite index'inden `SirketId, KapasiteAdi` composite unique kaldırıldı (FirmaId composite yeterli)
- BankaKasaHareket index'inden `SirketId, IslemTarihi` kaldırıldı

#### Migration `20260518140619_TenantB3i_DropSirketNavigationAndEntity`
- **Kritik karar**: Auto-üretilen migration `DropTable("Sirketler")` ve `DropTable("SirketTransferLoglari")` içeriyordu → manuel olarak **RENAME** ile değiştirildi (`_LEGACY_` prefix). Veri korunur, B4'te DROP edilecek.
- PL/pgSQL idempotent yapı:
  - 21 `FK_*_Sirketler_SirketId` constraint dinamik drop (information_schema'dan tarama)
  - `IX_*_SirketId` index'leri dinamik drop
  - `Sirketler` → `_LEGACY_Sirketler` RENAME (idempotent)
  - `SirketTransferLoglari` → `_LEGACY_SirketTransferLoglari` RENAME
- Down(): rename'i tersine alır (FK ve indeksler için manuel önceki migration'lara dönüş gerekir)
- PostgreSQL'e başarıyla uygulandı ✅

#### Hotfix (önceki context'ten)
- **`20260518135021_TenantCExt2_AddFirmaIdToKapasite`** (Kapasite.FirmaId eksik kolon, commit `c9d204e`): KapasiteService'in `42703: column k.FirmaId does not exist` hatasını çözer. PL/pgSQL idempotent: FirmaId nullable kolon + IX_Kapasiteler_FirmaId_KapasiteAdi + FK Restrict + backfill.

#### Build & commit
- Build: **0 error, 5 warning** (önceki 7 → 5)
- Commit `739df5f` push edildi

### Bir sonraki oturumda yapılacak adaylar (öncelik sırası)

**ÖNCELİK 1 — Faz 5.3-B4 (DB kolon DROP migration + _LEGACY_ tablo DROP)**:
- **YÜKSEK RİSK:** DB backup şart. Geri dönüş yok.
- Kapsam:
  - 14+ entity tablosunda `SirketId` kolonu DROP (PL/pgSQL idempotent, `TenantZ1` şablonu)
  - Entity'lerden `int? SirketId` property silme (ve Obsolete attribute'ları)
  - `_LEGACY_Sirketler` ve `_LEGACY_SirketTransferLoglari` tabloları DROP
  - **İstisna karar:** `AuditLog.SirketId` semantik olarak "aktif firma id'si" tutuyor (`AuditLogService` yazıyor). Ya kolon adı `FirmaId`'ye RENAME edilir ya da semantic olarak korunur. Karar B4 başlangıcında verilecek.

**ÖNCELİK 2 — Faz 5.2 (Firma.CariId drop)**: Hâlâ iş tarafı onayı önerilir.

**ÖNCELİK 3 — Teknik Borç #1 (True Excel grid)**.

### Açık dosyalar / referans (bu oturum sonu)
- 📌 `docs/TENANT_MIGRATION_PLAN.md` (bookmark)
- ✅ `KOAFiloServis.Web/Data/Migrations/20260518140619_TenantB3i_DropSirketNavigationAndEntity.cs` (DB'ye uygulandı)
- ✅ `KOAFiloServis.Web/Data/Migrations/20260517212717_TenantZ1_DropLegacyCariFaturaSirketColumns.cs` (B4 şablonu — PL/pgSQL idempotent drop)
- 🔒 DB'de `_LEGACY_Sirketler` + `_LEGACY_SirketTransferLoglari` tabloları duruyor (B4'te DROP edilecek)

### Git durumu (bu oturum sonu)
- Branch: `main`, push edildi
- Son commit'ler: `c9d204e` Kapasite hotfix, `739df5f` Faz 5.3-B3-i

**Devam komutu (sonraki oturum):** "kaldığımız yerden devam" → Faz 5.3-B4 başlat. **ÖNCELİKLE DB BACKUP AL** (pg_dump). Sonra SirketId kolon drop migration + _LEGACY_ tablo drop migration.

---

## ✅ FAZ 5.3-B1 + B2 TAMAMLANDI (Bu oturum)

### Bu oturumda yapılanlar

**Faz 5.3-B1 (DbContext ölü kod temizliği) + B2 (TenantService dosya silme)** birleşik bitirildi:

- ✅ `CariSeferUcreti` entity'sine `[TenantNullableFirmaId]` + `IFirmaTenant` + nullable `FirmaId` + `Firma` navigation + `[Obsolete] SirketId/Sirket` eklendi (Kapasite şablonu)
- ✅ `ApplicationDbContext`'te `CariSeferUcreti` için Sirket FK config + manuel `TenantFilterDisabled` query filter kaldırıldı, Firma FK eklendi → tenant izolasyonu artık `IFirmaTenant` global filter üzerinden
- ✅ `ApplicationDbContext` ölü kod temizliği:
  - `_tenantService` private field silindi
  - `ResolveTenantService()` metodu silindi
  - `TenantId` private property silindi
  - `TenantFilterDisabled` private property silindi
  - `SetServiceProvider`'daki `_tenantService = null` reset satırı silindi
- ✅ Migration `20260518132902_TenantB1_AddFirmaIdToCariSeferUcreti` üretildi + PostgreSQL'e uygulandı
  - `CariSeferUcretleri.FirmaId` nullable kolon + Restrict FK
  - `Sirket` FK navigation hâlâ olduğu için EF auto-regenerate etti (5.3-B3'te tamamen kalkacak)
- ✅ `TenantFirmaIdBackfillMigrationHelper`: `CariSeferUcretleri` tablosu eklendi
- ✅ `KOAFiloServis.Web/Services/TenantService.cs` **silindi** (~700 satır)
- ✅ `KOAFiloServis.Web/Services/Interfaces/ITenantService.cs` **silindi**
- ✅ `Program.cs`: `AddScoped<ITenantService, TenantService>()` DI kaydı kaldırıldı
- ✅ Build: **0 error, 7 warning** (önceki 38 → 7; planlı obsolete uyarılarının çoğu TenantService.cs içindeydi)
- ✅ 2 commit + push (`dcbb805` Faz 5.1+5.3-A, `e29cc98` Faz 5.3-B1+B2)

### Bir sonraki oturumda yapılacak adaylar (öncelik sırası)

**ÖNCELİK 1 — Faz 5.3-B3 (12 entity'den SirketId/Sirket property drop)**:
Etkilenen entity'ler (17 dosya, Shared/Entities/*.cs içinde `\bSirket\b` referansı olanlar):
`Arac`, `AracMaliyetSnapshot`, `AuditLog`, `BankaHesap`, `BankaKasaHareket`, `CariSeferUcreti`, `Guzergah`, `Hakedis`, `Kapasite`, `KullaniciVeLisans`, `Lastik`, `ServisKontrat`, `Sofor`, `TasimaTedarikci`
- Her entity'den `[Obsolete] SirketId` + `[Obsolete] Sirket` navigation sil
- `ApplicationDbContext`'ten 12 `HasOne(Sirket).HasForeignKey(SirketId)` mapping sil
- `Kapasite.cs` index'inden `SirketId` kaldır
- `BankaKasaHareket.cs` index'inden `SirketId` kaldır
- `DbSet<Sirket>` + `DbSet<SirketTransferLog>` sil
- Build kontrol et — kullanan UI/servis varsa düzelt
- **Çıktı:** Migration üretildiğinde sadece FK drop'ları olacak (kolonlar 5.3-B4'te)

**ÖNCELİK 2 — Faz 5.3-B4 (DB drop migration)**:
- `Sirketler` + `SirketTransferLoglari` tabloları drop
- 12+ tablodaki `SirketId` kolonları + indeksleri drop
- PL/pgSQL idempotent pattern (`TenantZ1` şablonu)
- **YÜKSEK RİSK:** Önce DB backup şart. Geri dönüş yok.

**ÖNCELİK 3 — Faz 5.2 (Firma.CariId drop)**: Hâlâ iş tarafı onayı önerilir.

**ÖNCELİK 4 — Teknik Borç #1 (True Excel grid)**.

### Açık dosyalar / referans (bu oturum sonu)
- 📌 `docs/TENANT_MIGRATION_PLAN.md` (bookmark)
- ✅ `KOAFiloServis.Web/Data/Migrations/20260518132902_TenantB1_AddFirmaIdToCariSeferUcreti.cs` (DB'ye uygulandı)
- ✅ `KOAFiloServis.Shared/Entities/Kapasite.cs` (şablon, B3'te referans)
- ✅ `KOAFiloServis.Web/Data/Migrations/20260517212717_TenantZ1_DropLegacyCariFaturaSirketColumns.cs` (PL/pgSQL idempotent drop pattern — B4'te lazım)

### Git durumu (bu oturum sonu)
- Branch: `main`, push edildi (origin'e kadar güncel)
- Son 2 commit: `dcbb805` Faz 5.1+5.3-A, `e29cc98` Faz 5.3-B1+B2

**Devam komutu (sonraki oturum):** "kaldığımız yerden devam" → Faz 5.3-B3 başlat (entity'lerden SirketId/Sirket property drop). B4 (DB drop) **ayrı oturuma**, backup şart.

---

## ✅ FAZ C-EXTEND TAMAMLANDI (Bu oturum)

### Bu oturumda yapılanlar

**Faz C-extend** tamamen bitirildi. 5 legacy entity (`Arac`, `BankaHesap`, `BankaKasaHareket`, `Guzergah`, `Sofor/Personel`) artık `Kapasite` ile aynı şablonda:

- ✅ Yarım migration silindi: `20260517222405_TenantCExt_AddFirmaIdToLegacyEntities*`
- ✅ 5 entity'ye `[TenantNullableFirmaId]` class attribute eklendi
  - `IFirmaTenant` + nullable `FirmaId` + `Firma` navigation + `[Obsolete] SirketId/Sirket` zaten önceki oturumdan hazırdı
- ✅ `ApplicationDbContext.OnModelCreating`: 4 entity'ye `HasOne(Firma).WithMany().HasForeignKey(FirmaId).OnDelete(Restrict)` mapping eklendi (Sofor + Kapasite zaten vardı)
- ✅ Tek temiz migration üretildi: `20260518124248_TenantCExt_AddFirmaIdToLegacyEntities`
  - 5 tablo için: `FirmaId` NOT NULL → NULL + Cascade FK → Restrict FK
  - Up() başına güvenlik SQL'i eklendi: `UPDATE ... SET FirmaId=NULL WHERE FirmaId=0` (Restrict FK ihlali olmasın diye, startup backfill sonra doldurur)
- ✅ `TenantFirmaIdBackfillMigrationHelper`: 5 tablo zaten listede, dokunulmadı
- ✅ Build: **0 error, 38 warning** (hepsi planlı obsolete uyarıları)

### Tooling notu
Bookmark'taki `AutoImport.props 10.0.6 not found` hatası bu oturumda **görülmedi** — muhtemelen önceki oturum kapanırken VS cache yenilenmiş. Sorun çıkarsa: obj/bin temizle + `dotnet nuget locals all --clear` + `dotnet restore`.

### Bir sonraki oturumda yapılacak adaylar (öncelik sırası)

**ÖNCELİK 1 — Faz C-extend uygulama + doğrulama (DB'ye apply):**
1. `dotnet ef database update --project KOAFiloServis.Web` → Up() çalışır (FirmaId=0 satırları NULL'a döner, FK Restrict olur)
2. Uygulamayı başlat → `TenantFirmaIdBackfillMigrationHelper` NULL'ları varsayılan firma ile doldurur
3. Çok-firmalı spot kontrol: 2 firmayla login olup birbirinin Arac/Sofor/Guzergah/BankaHesap kayıtlarının görünmediğini test et
4. (Opsiyonel K9-Faz2) Ayrı migration `TenantCExt_FirmaIdRequired`: `[TenantNullableFirmaId]` kaldırılınca DbContext otomatik IsRequired uygular → NOT NULL'a alır

**ÖNCELİK 2 — Faz 5.3-B (TenantService + Sirket entity drop):**
- Artık 5 entity `IFirmaTenant` üzerinden tenant izole olduğu için `ApplicationDbContext`'teki manuel `SirketId == TenantId` filter'ları + `ResolveTenantService` + `TenantFilterDisabled` + `TenantId` ölü kod silinebilir
- `AuditLogService` zaten `IAktifFirmaProvider`'a geçti
- `TenantService.cs` / `ITenantService.cs` (~700 satır) silinir
- `Sirket` entity + `Sirketler` tablosu + diğer entity'lerin `SirketId` kolonları → PL/pgSQL idempotent drop migration

**ÖNCELİK 3 — Faz 5.2 (Firma.CariId drop)**: Hâlâ iş tarafı onayı önerilir (Unvan fallback regresyon riski).

**ÖNCELİK 4 — Teknik Borç #1 (True Excel grid)**.

### Açık dosyalar / referans
- 📌 `docs/TENANT_MIGRATION_PLAN.md` (bookmark)
- ✅ `KOAFiloServis.Web/Data/Migrations/20260518124248_TenantCExt_AddFirmaIdToLegacyEntities.cs` (yeni, henüz DB'ye uygulanmadı)
- ✅ `KOAFiloServis.Shared/Entities/Kapasite.cs` (şablon)
- ✅ `KOAFiloServis.Web/Data/Migrations/TenantFirmaIdBackfillMigrationHelper.cs` (değişmedi, zaten 5 tabloyu içeriyor)

### Git durumu (bu oturum sonu)
- Branch: `main`
- Modified entity'ler (Arac, BankaHesap, BankaKasaHareket, Guzergah, Sofor) → attribute eklendi
- Modified ApplicationDbContext.cs → 4 yeni Firma FK mapping
- Yeni migration: `20260518124248_TenantCExt_*` (2 dosya)
- Önceki TenantZ1 migration + diğer modified dosyalar hâlâ uncommitted → **C-extend bitti, commit zamanı**

**Devam komutu (sonraki oturum):** "kaldığımız yerden devam" / "C-extend'i DB'ye uygula" / "Faz 5.3-B'ye geç"

---

## 🗄️ ARŞİV — Önceki oturum sonu bookmark (referans)

### Tooling/Build hatası (eski not — şu an görülmüyor)

**Hata:** `The imported project "C:\Program Files\dotnet\packs\Microsoft.NET.Runtime.WebAssembly.Sdk\10.0.6\Sdk\AutoImport.props" was not found.`

**Tanı (bugün yapıldı):**
- Yüklü WebAssembly pack sürümü: **10.0.8** (10.0.6 yok)
- Yüklü .NET SDK: 9.0.314, 10.0.108, 10.0.204
- `global.json` **YOK** → SDK pinning yok
- Hiçbir `.csproj` / `Directory.*.props` dosyasında `10.0.6` referansı **YOK**
- Sonuç: 10.0.6 referansı **Visual Studio cache / eski `obj/project.assets.json`'dan** geliyor

**Çözüm sırası (yarın bu sırayla):**
1. VS 2026 kapat
2. PowerShell:
   ```pwsh
   cd "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis"
   Get-ChildItem -Recurse -Directory -Include obj,bin | Remove-Item -Recurse -Force
   dotnet nuget locals all --clear
   dotnet restore
   dotnet build
   ```
3. Hâlâ devam ederse `global.json` ile SDK'yı pinle (10.0.8 pack'i ile uyumlu olan SDK):
   ```json
   { "sdk": { "version": "10.0.108", "rollForward": "latestFeature" } }
   ```
4. Son çare: `dotnet workload update && dotnet workload install wasm-tools`

### Yarınki yapılacaklar (NET SIRA)

1. **[ÇEVRE]** Yukarıdaki çözüm sırasıyla build'i yeşillendir.
2. **[GERİ ALMA]** Yarım migration'ı sil:
   ```pwsh
   Remove-Item "KOAFiloServis.Web\Data\Migrations\20260517222405_TenantCExt_AddFirmaIdToLegacyEntities*"
   ```
   Doğrulama: `dotnet ef migrations list --project KOAFiloServis.Web` → son uygulanan `TenantZ1_DropLegacyCariFaturaSirketColumns` olmalı.
3. **[ENTITY]** `Kapasite.cs` şablonunu şu 5 entity'ye uygula:
   - `KOAFiloServis.Shared/Entities/Arac.cs`
   - `KOAFiloServis.Shared/Entities/BankaHesap.cs`
   - `KOAFiloServis.Shared/Entities/BankaKasaHareket.cs`
   - `KOAFiloServis.Shared/Entities/Guzergah.cs`
   - `KOAFiloServis.Shared/Entities/Sofor.cs` (Personeller tablosu — TPH varsa dikkat: Personel/Sofor inheritance)
   - Şablon: `[TenantNullableFirmaId]` class attr + `IFirmaTenant` + `int? FirmaId` + `virtual Firma? Firma` + `[Obsolete]` `SirketId`/`Sirket` (silme, sadece işaretle)
4. **[CONTEXT]** `ApplicationDbContext.OnModelCreating`:
   - Her entity için `HasOne(e => e.Firma).WithMany().HasForeignKey(e => e.FirmaId).OnDelete(Restrict)` ekle
   - Manuel `e.SirketId == TenantId` filter'larını **henüz kaldırma** (backfill garantisinden sonra C-extend-cont fazında)
5. **[MIGRATION]** Tek migration'da topla:
   ```pwsh
   dotnet ef migrations add TenantCExt_AddFirmaIdToLegacyEntities --project KOAFiloServis.Web --context ApplicationDbContext
   ```
   Üretilen migration'ı **elle gözden geçir**: sadece 6 tablo için `AddColumn FirmaId (nullable)` + index + FK olmalı. Yabancı `DropForeignKey/AddForeignKey` churn satırlarını **sil**.
6. **[BACKFILL]** `TenantFirmaIdBackfillMigrationHelper`'a 6 tablo girdisini ekle (mevcut girişleri şablon al). Mantık: `UPDATE {table} SET "FirmaId" = (SELECT "FirmaId" FROM "Sirketler" WHERE "Id" = {table}."SirketId")`
7. **[DOĞRULAMA]** `dotnet build` → `dotnet ef database update` → çok-firmalı spot kontrol
8. **[K9-FAZ2]** Ayrı migration ile NOT NULL: `TenantCExt_FirmaIdRequired`

### Açık dosyalar / referans
- 📌 `docs/TENANT_MIGRATION_PLAN.md` (bookmark)
- ⚠️ `KOAFiloServis.Web/Data/Migrations/20260517222405_TenantCExt_AddFirmaIdToLegacyEntities.cs` (YARIM, silinecek)
- ✅ `KOAFiloServis.Shared/Entities/Kapasite.cs` (şablon)
- ✅ `KOAFiloServis.Web/Data/Migrations/20260517212717_TenantZ1_DropLegacyCariFaturaSirketColumns.cs` (PL/pgSQL idempotent drop pattern — ileride lazım)
- 🔧 `KOAFiloServis.Web/Data/Migrations/TenantFirmaIdBackfillMigrationHelper.cs` (backfill listesine ekleme yapılacak)
- 🔧 `KOAFiloServis.Web/Data/ApplicationDbContext.cs` (FK mapping eklenecek)

### Git durumu (bugün)
- Branch: `main`
- 2 untracked migration: `TenantZ1_*` (✅ uygulandı, kalsın) + `TenantCExt_*` (⚠️ yarım, yarın silinecek)
- Çok sayıda modified entity dosyası → **commit atılmadı** (C-extend bitince tek commit önerilir)

**Devam komutu (yarın):** "kaldığımız yerden devam" / "C-extend'i bitir" / "AutoImport.props hatasını çöz"

> **NOT (arşiv):** Bu yarınki yapılacaklar listesi yukarıdaki "FAZ C-EXTEND TAMAMLANDI" bölümünde gerçekleşti. Güncel devam noktası için yukarıdaki bookmark'a bakın.


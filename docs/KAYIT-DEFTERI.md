# 📒 KOAFiloServis — Kayıt Defteri

> Bu dosya, geliştirme sürecinde alınan kararları, yapılan tartışmaları ve hazırlanan raporları
> kronolojik olarak kayıt altına alır. Her oturum sonunda güncellenir.

---

## 📅 22.05.2026 — Gün Sonu Özeti (Final)

### ✅ Bugün Tamamlanan

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **Item 1** | Güzergah listesi temizliği | `GuzergahList.razor` | VarsayılanAraç, Şoför, Sefer Durumu sütunları + inline sefer paneli + ~480 satır code-behind kaldırıldı |
| **Item 2** | Güzergah form sefer persist | `GuzergahForm.razor` | Sefer alt tablosu `IGuzergahSeferService` ile persist ediliyor, edit'te geri yükleniyor |
| **Item 3** | Puantaj-GuzergahSefer entegrasyonu | `KurumPuantajService.cs` | `SablonOlusturAsync` önce GuzergahSefer'den araç/şoför atıyor, `SeferTipindenSlotlara` helper |
| **Item 4** | Dashboard aktif firma gösterimi | `Home.razor` | Dashboard'da firma adı, kodu, dönem bilgisi gösteren kart |
| **Item 5** | Kapasite çakışma kuralı | `KurumPuantajService.cs` | `CheckConflictsAsync`'e 5. kural: Kapasite (Blocking) |
| **Item 6** | PlanlamaEditModal dropdown | `PlanlamaEditModal.razor` | Boş dropdown'lar dolduruldu, GuzergahSefer'den araç filtresi |
| **Yedek** | Tüm firma DB'leri yedekleme | `BackupService.cs` | PostgreSQL'de Master + Holding + tüm tenant DB'ler pg_dump ile ayrı ayrı yedekleniyor |
| **Fix 9** | Sefer kayıt hatası | `GuzergahForm.razor` | `FirmaId` eksikliği giderildi, inner exception gösterimi eklendi, sefer hatası guzergah kaydını bozmuyor |
| **Fix 10** | Duplicate key sequence | DB (manuel) | 3 tenant DB'de 172 sequence `MAX(Id)+1`'e sıfırlandı (veri göçü sonrası senkronizasyon) |
| **Fix 11** | Tenant DB eksik kolonlar | DB (manuel) | `PuantajKayitlar` tablosuna `Slot`, `KurumId`, `IsverenFirmaId`, `KaynakTipi`, `FinansYonu`, `BelgeNo`, `TransferDurum`, `FirmaId` eklendi |
| **Fix 12** | SablonOlustur plaka eksik | `KurumPuantajService.cs` | GuzergahSefer sorgusuna `.Include(s => s.Arac)` eklendi, plaka bilgisi dolduruluyor |

### 📊 Toplam: 7 dosya + 3 DB fix, 7 commit

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |
| `/swagger` | ✅ HTTP 200 |
| `/guzergahlar` | ✅ HTTP 200 (10 sütun) |
| `/login` | ✅ HTTP 200 |
| `/dashboard` | ✅ 401 (Authorize) |
| `/planlama` | ✅ 401 (Authorize) |
| `/api/auth/login` | ✅ JWT token alındı |
| `/api/guzergahlar` | ✅ 16 güzergah döndü |
| Runtime hata/exception | ✅ **0 hata** |

### 🏗️ Alınan Kararlar

| Karar | Gerekçe |
|-------|---------|
| Sefer yönetimi GuzergahList'ten GuzergahForm'a taşındı | Tek sorumluluk: listeleme ve düzenleme ayrıldı |
| GuzergahSefer puantaja öncelikli kaynak | Kullanıcının güzergah altında tanımladığı araç/şoför puantaja otomatik yansır |
| Kapasite kuralı Blocking seviyesinde | Güzergah kapasite aşımı operasyonel risk, engel olmalı |
| Tüm tenant DB'ler yedekleniyor | Firma bazlı fiziksel izolasyonda her DB ayrı yedeklenmeli |
| Güzergah formu: VarsayılanAraç/Şoför sabit, puantaj: seferdeki araç/şoför | İki seviyeli atama: varsayılan (default) + operasyonel (sefer bazlı) |

### 📋 Commit Geçmişi

```
6476b6d fix(puantaj): SablonOlustur - GuzergahSefer'den plaka bilgisi eklendi
df0516f chore: .gitignore guncellendi
19c146d fix(guzergah): Sefer kayit hatasi giderildi - FirmaId + inner exception
1cca984 feat(backup): Tum tenant DB yedekleme + kayit defteri
644e50c feat(guzergah-planlama): 6 is paketi
```

---

## 📅 22.05.2026 — Gün Sonu Özeti (Ek Oturum)

### ✅ Tamamlanan (10 iş paketi, 4 commit)

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **Item 6** | Puantaj gün atama UI | `PlanlamaEditModal.razor` | Gun01-Gun31 grid (0-1-2 döngü, hafta içi seçimi, auto hesapla) |
| **Item 7** | Modal veri yükleme optimizasyonu | `PlanlamaEditModal.razor`, `Planlama.razor` | 5 servis çağrısı paralel + parent'tan parametre geçişi |
| **Item 8** | Tenant DB migration otomasyonu | `TenantDatabaseService.cs` | `BaselineMigrationsAsync`: yeni DB'lerde migration history baseline |
| **Item 9** | Holding verisi manuel toplama | `HoldingDashboard.razor` | Yıl/Ay seçimi + "Veri Topla" butonu |
| **Item 10** | Planlama mini gün göstergesi | `Planlama.razor` | Ana tabloda 31 günlük renkli çubuk + tooltip |
| **Item 11** | Holding Yöneticisi rolü + auth | `KullaniciVeLisans.cs`, `NavMenu.razor`, 7 Holding sayfası | Rol, 8 yetki, menü kontrolü, sayfa `[Authorize(Roles)]` |
| **Bonus 1** | Planlama inline gün toggle | `Planlama.razor` | Mini gün çubukları tıklanabilir, `degisenSatirlar` otomatik takip |
| **Bonus 2** | Holding ilk veri toplama | `Program.cs` | `EnsureHoldingInitialData`: boş tabloya otomatik ilk veri |
| **Bonus 3** | Planlama rol bazlı auth | `KullaniciVeLisans.cs`, `NavMenu.razor`, 2 Planlama sayfası | 6 Planlama yetkisi, menü kontrolü, sayfa `[Authorize(Roles)]` |
| **Fix** | Setup.iss Boolean fix | `setup/Setup.iss` | `Boolean()` → `Boolean` |

### 📊 Toplam: 10 dosya, 4 commit

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |

### 🏗️ Alınan Kararlar

| Karar | Gerekçe |
|-------|---------|
| Tenant DB migration baseline | `EnsureCreated` sonrası tüm migration'lar `__EFMigrationsHistory`'ye kaydedilir, gelecek `Migrate()` çağrıları sadece yeni migration'ları uygular |
| Holding ve Planlama sayfaları rol bazlı yetkilendirme | Admin + ilgili rol dışındakiler sayfalara erişemez, menüde göremez |
| PlanlamaPermission seed | Operasyon (tam yetki), Muhasebeci (okuma), Admin (tümü) |

### 📋 Commit Geçmişi

```
ad89039 feat(planlama): Planlama modulu rol bazli yetkilendirme
2a52c1d feat(holding-planlama): Holding ilk veri toplama + Planlama inline gun toggle
5d5ca6f feat(holding): Holding Yoneticisi rolu + yetkilendirme
08a4256 feat(puantaj-planlama): Gun bazli puantaj grid + modal optimizasyonu + holding veri toplama + tenant DB migration baseline
```

---

## 📅 23.05.2026 — Ana Oturum (52 commit, ~79 iyileştirme)

### ✅ Tamamlanan

| # | İş | Modül | Açıklama |
|---|-----|-------|----------|
| 1 | SeferSlot genişletme | Puantaj | Enum: Diger1-5 eklendi (toplam 8 slot) |
| 2 | SlotAdi alanı | Puantaj | `PuantajKayit.SlotAdi` — özel isim ("Gece Vardiyası" vb) |
| 3 | GuzergahSefer.Slot | Güzergah | Güzergah sefer tanımında Slot alanı |
| 4 | Slot otomatik önerme | Planlama | Yeni seferde ilk boş slot otomatik seçilir |
| 5 | Kullanılan slot göstergesi | Planlama | Güzergah seçince dolu slotlar badge ile gösterilir |
| 6 | Hızlı sefer ekleme | Planlama | `+` butonu ile aynı güzergaha yeni slot ekleme |
| 7 | Slot/Güzergah toggle | Planlama | Gruplandırma modu: Slot bazlı / Güzergah bazlı |
| 8 | Daraltılabilir gruplar | Planlama | Slot ve Güzergah grupları tıklanarak daraltılabilir |
| 9 | Finans özet satırı | Planlama | Her slot grubu altında Gelir/Gider/Net tfoot |
| 10 | Genel toplam kartı | Planlama | Tüm slotların toplam Sefer/Gün/Gelir/Gider/Kar |
| 11 | FinansYonu renk kodlaması | Planlama | Satır sol border: Gelen=yeşil, Giden=kırmızı, İçDağıtım=mavi |
| 12 | OnayDurum sütunu+filtresi | Planlama | Tabloda OnayDurum badge + filtre dropdown |
| 13 | Toplu Onayla / Onay Kaldır | Planlama | Tüm değişen/seçili kayıtları toplu onayla |
| 14 | Hızlı onay toggle | Planlama | OnayDurum badge'ine tıklayarak Onaylandi/Taslak geçişi |
| 15 | Birim fiyat yayma | Planlama | Birim fiyatı aynı güzergahın diğer slotlarına uygulama |
| 16 | Satır kopyala | Planlama | Her satırda Kopyala butonu |
| 17 | Hafta sonu butonu | Planlama | Edit modal'da "Sadece Hafta Sonu" hızlı doldurma |
| 18 | Bugün vurgusu | Planlama | Gün grid'de bugünün tarihi kırmızı border ile |
| 19 | Hafta sonu çubukları | Planlama | Mini gün çubuklarında hafta sonu farklı ton |
| 20 | Kurum gösterme | Planlama | "Tüm Kurumlar" filtresinde güzergah altında Kurum adı |
| 21 | Tablo sıralama | Planlama | Sütun başlıkları tıklanabilir (Güzergah, Araç, Gelir, Gider) |
| 22 | Dönem Temizle onay | Planlama | Çift tıklama ile onay dialog'u |
| 23 | Slot dropdown | KurumPuantaj | Edit modal'a Slot enum dropdown eklendi |
| 24 | Güzergah/Araç dropdown | KurumPuantaj | Edit modal'a Güzergah ve Araç seçimi eklendi |
| 25 | SlotAdi + OnayDurum | KurumPuantaj | Tabloda SlotAdi gösterimi + OnayDurum badge + istatistik kartları |
| 26 | Dashboard yıl/ay seçici | Dashboard | Dönem değiştirilebilir, veriler yeniden yüklenir |
| 27 | Önceki ay trend okları | Dashboard | Toplam Sefer ve Çakışma için önceki aya göre ▲/▼ |
| 28 | Gelir/Gider/Kar KPI | Dashboard | Toplam Gelir, Gider, Net Kar kartları |
| 29 | En Karlı 5 Güzergah | Dashboard | Kâr sıralaması ile ilk 5 güzergah tablosu |
| 30 | Toplam Sefer Günü KPI | Dashboard | Toplam iş günü sayısı kartı |
| 31 | Sayfalama + Excel | Güzergah | 25 kayıt sayfalama + CSV/Excel export |
| 32 | Sütun sıralama | Güzergah | GüzergahList tablosunda sıralanabilir sütunlar |
| 33 | Güzergah kopyalama | Güzergah | GüzergahList'te Kopyala butonu + form desteği |
| 34 | Harita rota çizgileri | Güzergah | Harita görünümünde tüm güzergahlar rotalarıyla |
| 35 | İstatistik kartları | Güzergah | Toplam/Aktif/Pasif sayıları |
| 36 | Koordinat validasyonu | Güzergah | Enlem -90..90, Boylam -180..180 kontrolü |
| 37 | FirmaId kod üretimi | Güzergah | Güzergah kodu firma bazlı üretiliyor |
| 38 | SeferTipi fiyat uyarısı | Güzergah | SeferTipi değişince fiyat güncellemesi uyarısı |
| 39 | GiderFiyat uyarısı | Güzergah | Gider > Gelir ise görsel uyarı |
| 40 | Modüller arası geçiş | Genel | Planlama→GüzergahForm + GüzergahForm→Planlama linkleri |

### 🔧 Bug Fix (Önceki oturumdan kalan)

| # | İş | Açıklama |
|---|-----|----------|
| F1 | Tum Kurumlar filtresi | `GetPuantajlarAsync` parametresi `int?` yapıldı, null=tümü |
| F2 | Gereksiz slot üretimi | `SablonOlusturAsync` → `SeferTipindenSlotlara` kullanılıyor |
| F3 | N+1 sorgu fix | `TopluSavePuantajAsync` tek sorguda tüm kayıtları yüklüyor |
| F4 | Finansal auto-hesapla | `SavePuantajAsync` + `TopluSavePuantajAsync` → `HesaplaPuantajToplam/Gelir/Gider` |
| F5 | Benzersizlik DB sorgusu | `BenzersizGuzergahMiAsync` → client-side yerine DB `AnyAsync` |
| F6 | Dashboard kurumId=0 fix | PlanlamaDashboard `GetPuantajlarAsync` null ile çağrılıyor |
| F7 | CopyPreviousMonth eksik alanlar | KDV, kesinti, finans, transfer alanları kopyalanıyor |
| F8 | Setup.iss Boolean fix | `Boolean()` → `Boolean` |

### 🔐 Yetkilendirme

| # | Modül | Açıklama |
|---|-------|----------|
| A1 | Holding | `HoldingYoneticisi` rolü + 8 yetki + menü kontrolü + `[Authorize(Roles)]` |
| A2 | Planlama | 6 Planlama yetkisi + menü kontrolü + `[Authorize(Roles)]` |
| A3 | KurumPuantaj | `[Authorize(Roles)]` eklendi |

### 🏗️ Altyapı

| # | İş | Açıklama |
|---|-----|----------|
| I1 | Tenant DB migration baseline | Yeni DB'lerde `__EFMigrationsHistory` baseline kaydı |
| I2 | Holding ilk veri toplama | `EnsureHoldingInitialData` startup görevi |
| I3 | Planlama veri cache | Modal'a veriler parent'tan parametre ile geçiliyor |

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |

### 🏗️ Alınan Kararlar

| Karar | Gerekçe |
|-------|---------|
| Dinamik slot (Diger1-5 + SlotAdi) | Enum genişletme + serbest metin ile esnek slot tanımı |
| GüzergahSefer.Slot öncelikli | Şablon oluşturmada SeferTipi yerine spesifik Slot kullanılır |
| OnayDurum her yerde | Planlama, KurumPuantaj, Dashboard — tutarlı onay akışı |
| Modüller arası link | Güzergah ↔ Planlama hızlı geçiş ile operasyonel akıcılık |

### 📋 Commit Geçmişi (son 10)

```
1867f42 feat(planlama): Dashboard Toplam Sefer Gunu KPI karti eklendi
373dc80 feat(guzergah-planlama): Moduller arasi hizli gecis
573597a feat(planlama): OnayDurum badge tiklayarak hizli onay/taslak toggle
ee7f6e5 feat(planlama): Birim fiyati ayni guzergahin diger slotlarina da uygulama
9e3be61 feat(puantaj): Hizli sefer eklemede guzergah varsayilan arac/sofor otomatik doldurma
ba114cc feat(guzergah): Harita gorunumunde tum guzergahlar rota cizgileriyle
70fd670 feat(planlama): Dashboard En Karli 5 Guzergah listesi
0069167 feat(planlama): Guzergah gruplari da daraltilabilir yapildi
c2d115a feat(planlama): Slot/Guzergah gruplandirma toggle eklendi
782de84 feat(guzergah-puantaj): GuzergahSefer Slot alani + SablonOlustur Slot destegi
```

---

## 📅 23.05.2026 — Yapılacak İşler

### 🔴 Manuel Test (Login Gerek)

| # | İş | Açıklama | Durum |
|---|-----|----------|:-----:|
| 1 | Güzergah sefer persist testi | Yeni güzergah oluştur → sefer ekle → kaydet → tekrar düzenle, seferler gelmeli | 🔴 |
| 2 | Planlama kurum seçimi testi | Login → Planlama → Kurum seç → sayfa kırılmadan açılmalı | 🔴 |
| 3 | Planlama Şablon Oluştur testi | Kurum seç → Şablon Oluştur → GuzergahSefer'deki araç/şoför/plaka puantajda görünmeli | 🔴 |
| 4 | Kapasite çakışma testi | PersonelSayisi=1 olan güzergaha aynı slotta 2 araç → Kapasite çakışması çıkmalı | 🔴 |
| 5 | Dashboard firma kartı testi | Login → Dashboard → aktif firma adı/kodu/dönemi görünmeli | 🔴 |
| 6 | Dinamik slot (Diger1-5) testi | Planlama → Yeni Sefer → Diger slot seç → SlotAdi yaz → kaydet | 🔴 |
| 7 | Role atama testi | Admin → Rol Yonetimi → Kullanıcıya HoldingYoneticisi ata → login ol | 🔴 |

### 🟡 Geliştirme

| # | İş | Açıklama | Durum |
|---|-----|----------|:-----:|
| 6 | Puantaj gün atama (Gun01-Gun31) | Planlama tablosunda gün bazlı işaretleme UI'ı | ✅ |
| 7 | PlanlamaEditModal veri yükleme optimizasyonu | Modal her açıldığında servis çağrıları yapılıyor, cache'lenebilir | ✅ |
| 8 | Tenant DB migration otomasyonu | Yeni tenant DB oluşturulurken tüm migration'ların otomatik uygulanması | ✅ |
| 9 | Holding verisi manuel toplama | `ToplaVeKaydetAsync` çağrısı yapıp raporları doldur | ✅ |

### ⚪ Uzun Vadeli

| # | İş | Durum |
|---|-----|:-----:|
| 10 | Planlama sayfası gün grid UI (Gun01-Gun31 checkbox/matrix) | ✅ |
| 11 | Holding girişi — Holding Yöneticisi rolü + auth | ✅ |
| 12 | Firma geçiş testi (tenant DB izolasyonu) | 🔴 |
| 13 | Puantaj Excel import sayfası | 🔵 Yeni |

### 🆕 23.05.2026 Ana Oturumda Eklenenler (özet)

| Kategori | Sayı |
|---|---|
| Yeni özellik | 40+ |
| Bug fix | 8 |
| Yetkilendirme | 3 modül |
| Altyapı | 3 |
| **Toplam commit** | **52** |
| **Build** | 0 hata, 0 uyarı |
| **Test** | 291/291 |

---

## 📅 21.05.2026 — Gün Sonu Özeti (Final)

### ✅ Bugün Tamamlanan

| Modül | Sprint | Commit | İçerik |
|-------|:------:|--------|--------|
| **MultiDb Fix** | — | `5155be1` | WithMany null parametre - Cari navigasyon çakışması giderildi |
| **Holding** | Faz 10 | `6d28e11` | HoldingDbContext + Service + 7 UI sayfası + Quartz job |
| **Planlama S1** | Altyapı | `35f9534` `1a6f294` | SeferSlot enum + entity genişletme + migration + Planlama.razor + EditModal |
| **Planlama S2** | Çakışma | `1bc7bfa` | ConflictResult modeli + 3 kural motoru + UI görsel uyarılar |
| **Planlama S3** | Kopyalama | `77cc93b` | CopyPreviousMonthModal + çakışma simülasyonu |
| **Planlama S4** | Finans | `c4735b4` | FinansYonu + KaynakTipi + Tedarikçi izolasyonu |
| **Planlama S5** | Dashboard | `4e565f6` | PlanlamaDashboard (KPI + slot/finans/kaynak dağılımı) |
| **Guzergah** | — | `e9c1448` | Eski projedeki sefer alt tablosu GuzergahForm'a taşındı |

### 📊 Toplam: 9 commit, 27 dosya, +2500/-30 satır

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |
| Uygulama başlatma | ✅ `http://0.0.0.0:5200` |
| Tüm startup görevleri (30+) | ✅ Hepsi başarılı |
| PuantajSlotMigration | ✅ Tüm kolonlar mevcut |
| EnsureHoldingDatabase | ✅ KOAFiloServis_Holding + 2 tablo |
| `/planlama` route | ✅ 401 (Authorize) |
| `/planlama/dashboard` route | ✅ 401 (Authorize) |
| `/holding` + 6 alt sayfa | ✅ 401 (Authorize) |
| Runtime hata/exception | ✅ **0 hata** |

### 🏗️ Alınan Kararlar

| Karar | Gerekçe |
|-------|---------|
| Planlama modülü PuantajKayit üzerine inşa edildi | Ayrı PlanlamaKayit entity'si yerine mevcut entity genişletildi — veri tekrarı önlendi |
| KurumPuantaj korundu | Kırıcı değişiklik yok, feature flag gerekmedi |
| Holding snapshot mimarisi | Canlı cross-db sorgu yerine periyodik snapshot — performans + KVKK |
| #11 Master tablo temizliği ertelendi | 40+ FK değişikliği riski, düşük getiri |
| SeferSlot per-day Gun01..Gun31 üzerinden çakışma kontrolü | PuantajKayit aylık tablo, günlük çakışma analizi için tek yol |

---

## 📅 22.05.2026 — Yapılacak İşler

### 🔴 Öncelikli (Manuel Test - Login Gerek)

| # | İş | Açıklama |
|---|-----|----------|
| 1 | Firma geçiş testi | Tenant DB'ye geçince dashboard sadece o firmanın verisini gösteriyor mu? |
| 2 | Tenant DB UI testi | "Tenant DB Oluştur" butonu çalışıyor mu? |
| 3 | Planlama sayfaları testi | Login olup `/planlama` ve `/planlama/dashboard` testi |
| 4 | Holding sayfaları testi | Login olup `/holding` dashboard + 6 rapor sayfası testi |
| 5 | KurumPuantaj regresyon | Mevcut puantaj ekranı kırılmamış olmalı |

### 🟡 Geliştirme

| # | İş | Açıklama |
|---|-----|----------|
| 6 | #11 Master tablo temizliği | Uygun zamanda, düşük öncelik |
| 7 | Holding verisi toplama | İlk kez manuel `ToplaVeKaydetAsync` çağrısı yapıp raporları doldur |

### ⚪ Uzun Vadeli

| # | İş |
|---|-----|
| 8 | Holding girişi — Holding Yoneticisi rolü + auth |
| 9 | Yeni modül talepleri |

---

## 📅 20.05.2026 — Gün Sonu Özeti

### ✅ Bugün Tamamlanan (5 faz + 3 fix)

| Faz | Commit | İçerik |
|-----|--------|--------|
| **Faz 1** | `cba5d90` | Altyapı: `Firma.DatabaseName`, `ITenantConnectionStringProvider`, `MasterDbContext`, `TenantDbContextFactory`, migration |
| **Faz 2** | `2de0ef4` | Master DB fiziksel ayrım: `EnsureMasterDatabaseAsync` (raw SQL), veri kopyalama (sütun eşleştirmeli) |
| **Faz 3** | `0261aa6` | Tenant DB UI: `FirmaYonetimi` sayfasında DatabaseName badge + "Tenant DB Oluştur" butonu, `MigrateFirmaDataAsync` |
| **AutoCreate** | `ba98b03` | Startup'ta otomatik tenant DB oluşturma (`AutoCreateTenantDatabases` görevi) |
| **İsimlendirme** | `9492616` | DB isimleri `Koa_[FirmaKodu]_[ID]` formatı (Türkçe karakter dönüşümü) |
| **Fix 1** | `b833fbf` | `EnableLegacyTimestampBehavior` Program.cs başına taşındı + `TenantDbContextFactory` eksik EF konfigürasyonları |
| **Fix 2** | `8f9b8aa` | Veri göçü FK kısıtlama fix (`session_replication_role=replica`) + 2 adımlı göç (lookup→tenant) |
| **Fix 3** | `1ac63fb` | GuzergahList colspan fix + README güncelleme + debug log temizliği |

### 🧪 Smoke Test Sonuçları

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ 0 hata |
| Uygulama başlatma | ✅ `http://localhost:5200` |
| Master DB oluşturma | ✅ `KOAFiloServis_Master` (6 tablo) |
| Tenant DB otomatik oluşturma | ✅ 3 firma: `Koa_USTUN_GRUP_001`, `Koa_RECEP_USTUN_003`, `Koa_USTUN_FILO_005` |
| Veri göçü (Firma 1) | ✅ 47.263 satır (lookup: 44.307 + tenant: 2.956) |
| Veri göçü (Firma 3) | ✅ 25 Cariler + diğer veriler |
| Veri göçü (Firma 5) | ✅ 13 Cariler + diğer veriler |
| Login sayfası | ✅ HTTP 200 |

### 🏗️ Alınan Mimari Kararlar

| Karar | Gerekçe |
|-------|---------|
| **Hybrid model** | `Firma.DatabaseName == null` → shared DB, `!= null` → tenant DB. Kademeli geçiş imkanı |
| **Master DB raw SQL** | EF Core entity discovery cascade sorunu (41+ tablo). Raw SQL ile sadece 6 core tablo |
| **EnsureCreated** | 80+ migration'da legacy kolon referansları kırılıyor. Tenant DB'ler için daha güvenli |
| **Koa_[FirmaKodu]_[ID]** | DB isimlendirme: firma kısa adı + ID, Türkçe karakterler dönüştürülür |
| **AutoCreateTenantDatabases** | Startup'ta tüm aktif firmalar için otomatik tenant DB oluşturma + veri göçü |
| **FK disable (replica)** | Tenant DB'ye veri kopyalarken FK sıralama sorununu çözmek için |

### 📂 Yeni Eklenen Dosyalar

```
KOAFiloServis.Web/Data/MasterDbContext.cs
KOAFiloServis.Web/Data/TenantDbContextFactory.cs
KOAFiloServis.Web/Services/ITenantConnectionStringProvider.cs
KOAFiloServis.Web/Services/TenantConnectionStringProvider.cs
KOAFiloServis.Web/Services/ITenantDatabaseService.cs
KOAFiloServis.Web/Services/TenantDatabaseService.cs
KOAFiloServis.Web/Migrations/..._MultiDbFaz1_AddFirmaDatabaseName.cs
```

### 📂 Silinen Dosyalar

```
KOAFiloServis.Web/Data/TenantAwareDbContextFactory.cs
KOAFiloServis.Web/Components/Pages/Ayarlar/AIAsistan.razor
```

---

## 📅 21.05.2026 — Gün Sonu Özeti (2. Kısım)

### ✅ Ek Fix

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **Fix 8** | `WithMany()` → `WithMany((string?)null)` | `ApplicationDbContext.cs` | Firma entity'sinde `CariId` FK ilişkisi: `WithMany()` çağrısı Cari entity'sindeki `FirmaId` navigasyonuyla çakışıyordu. Explicit `(string?)null` ile navigasyonsuz ilişki tanımlandı. Build: 0 hata, 0 uyarı, 291 test başarılı |

---

## 📅 21.05.2026 — Holding Modülü (Faz 10)

### ✅ Holding Modülü Tamamlandı

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **10a** | Holding entity + DbContext | `HoldingVeri.cs`, `HoldingRapor.cs`, `HoldingDbContext.cs` (3 YENİ) | 2 entity + HoldingDbContext (2 tablo: HoldingVeriler, HoldingRaporlar). `appsettings.json`'a `HoldingConnection` eklendi. `Program.cs`'e DI + `EnsureHoldingDatabase` startup görevi eklendi |
| **10a** | HoldingService + Quartz | `IHoldingService.cs`, `HoldingService.cs`, `HoldingVeriToplamaJob.cs` (3 YENİ) | `ToplaVeKaydetAsync`: Master DB'den aktif firmaları alır, `Task.WhenAll` ile tüm tenant DB'lere paralel bağlanır, gelir/gider/araç maliyet/personel/hakediş toplamlarını Holding DB'ye upsert eder. Quartz: her ayın 1'inde 02:07'de otomatik |
| **10c** | Holding UI (7 sayfa) | `Holding/*.razor` (7 YENİ) | Dashboard (`/holding`), FirmaKarsilastirma, ButceKonsolidasyonu, OdemePlani, AracMaliyetOzeti, PersonelGiderOzeti, HakedisOzeti. Tümü `[Authorize]` + `InteractiveServer` |
| **10c** | NavMenu güncelleme | `NavMenu.razor` | Holding Yonetimi menü grubu eklendi (7 link) |

### 🧪 Smoke Test Sonuçları

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |
| Uygulama başlatma | ✅ `http://0.0.0.0:5200` |
| `EnsureHoldingDatabase` | ✅ `KOAFiloServis_Holding` DB + 2 tablo (HoldingVeriler, HoldingRaporlar) |
| `/holding` | ✅ 401 (Authorize - beklenen) |
| `/holding/karsilastirma` | ✅ 401 |
| `/holding/butce` | ✅ 401 |
| `/holding/odemeler` | ✅ 401 |
| `/holding/arac-maliyet` | ✅ 401 |
| `/holding/personel` | ✅ 401 |
| `/holding/hakedis` | ✅ 401 |
| Runtime hata/exception | ✅ **0 hata** |

### 📊 Holding Modülü Dosya Özeti

```
YENİ (14 dosya):
  Shared/Entities/HoldingVeri.cs
  Shared/Entities/HoldingRapor.cs
  Web/Data/HoldingDbContext.cs
  Web/Services/IHoldingService.cs
  Web/Services/HoldingService.cs
  Web/Jobs/HoldingVeriToplamaJob.cs
  Web/Components/Pages/Holding/HoldingDashboard.razor
  Web/Components/Pages/Holding/FirmaKarsilastirma.razor
  Web/Components/Pages/Holding/ButceKonsolidasyonu.razor
  Web/Components/Pages/Holding/OdemePlani.razor
  Web/Components/Pages/Holding/AracMaliyetOzeti.razor
  Web/Components/Pages/Holding/PersonelGiderOzeti.razor
  Web/Components/Pages/Holding/HakedisOzeti.razor

DEĞİŞEN (3 dosya):
  Web/appsettings.json              (+HoldingConnection)
  Web/Program.cs                    (+DI, +startup, +Quartz job)
  Web/Components/Layout/NavMenu.razor (+Holding menü grubu)
```

### 🏗️ MİMARİ KARAR — Snapshot Tabanlı Holding Konsolidasyonu

**Karar:** Holding raporları canlı cross-database sorgu yerine periyodik snapshot ile çalışır.

**Gerekçe:**
- Performans: N+1 DB sorgusu yerine tek snapshot DB okuması
- KVKK/Gizlilik: Ham veri tenant DB'lerde kalır, sadece agregasyon holding DB'ye girer
- Tutarlılık: Tüm raporlar aynı snapshot'tan okur

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| Holding verisi henüz toplanmadı (Quartz job bekliyor) | 🟡 İlk veri toplama sonrası raporlar dolu gelecek |
| Master tablo temizliği (#11) ertelendi | 🟡 Holding sonrası tekrar değerlendirilecek |
| Firma geçiş testi + Tenant DB UI testi (manuel) | 🔴 Login gerek, test edilmedi |

---

## 📅 21.05.2026 — Sprint Planı: Puantaj Modülü (Kurum/Firma Bazlı)

> Kaynak: `Kurum_Firma_Bazli_Puantaj_Planlama_Raporu.docx`

### 🎯 Amaç
Mevcut puantaj sistemini bozmadan (KurumPuantaj yaklaşımı korunarak):
- Güzergah bazlı çoklu sefer slotu (Sabah/Akşam/Mesai)
- Sefer CRUD (ekle/düzenle/sil)
- Çakışma kuralları ve engelleyici validasyon
- Önceki aydan kopyalama
- Gelir/gider ve fatura yönü takibi

### 📋 5 Sprint Planı

| Sprint | Tema | Kapsam | Tahmini Süre |
|:------:|------|--------|:-----------:|
| **S1** | Temel Uyum + Sefer CRUD | KurumPuantaj UX referansa yaklaştır, sefer ekle/düzenle/sil modal, slot bazlı sefer yönetimi, KurumId/IsverenFirmaId ayrımı | 3-4 gün |
| **S2** | Çakışma Motoru | (Tarih+Güzergah+Slot)→tek araç, (Tarih+Araç+Slot)→tek güzergah kuralları, kaydet öncesi validasyon, FaturaYonu enum + PlanlamaFaturaTakip tablosu, DB indexleri | 2-3 gün |
| **S3** | Çakışma UX + Kopyalama | Çakışmalı satır renklendirme + hover tooltip, önceki aydan kopyalama + kopya sonrası çakışma tarama, Basit/İleri mod, tek hesaplama servisi | 3-4 gün |
| **S4** | Fatura/Transfer Takibi | Gelir/gider/marj satır bazlı görünüm, giden/gelen/firma içi yansıtma durum takibi, iç transfer alanları, araç/şoför evrak durum badge'leri | 2-3 gün |
| **S5** | UAT ve Canlıya Geçiş | Senaryo testleri (CRUD, çakışma, kopyalama, faturalaşma), performans iyileştirme, release checklist, dokümantasyon | 2-3 gün |

**Toplam tahmini süre: 12-17 gün**

### 🔴 Kritik Kurallar

| Kural | Açıklama |
|-------|----------|
| Tek araç kuralı | Aynı gün + aynı güzergah + aynı slotta birden fazla araç olamaz |
| Tek güzergah kuralı | Aynı gün + aynı slotta aynı araç birden fazla güzergaha yazılamaz |
| Görsel çakışma | Çakışmalar renk + hover tooltip/liste ile gösterilir |

### 🛡️ Risk Yönetimi

| Risk | Aksiyon | Sprint |
|------|---------|:------:|
| İşveren Firma / Kurum ayrımı karışıyor | KurumId ve IsverenFirmaId alanlarını zorunlu ayır; formda ayrı seçim | S1 |
| Eski veriler slot kurallarına uymuyor | Dry-run migration raporu + varsayılan slot map + manuel düzeltme listesi | S2 |
| Çakışma kontrolü yavaşlar | (Tarih,Guzergah,Slot) ve (Tarih,Arac,Slot) indexleri; sadece etkilenen kayıtta kontrol | S2 |
| Mevcut puantajın bozulması | Feature flag + paralel ekran + geri dönüş planı; mevcut servis kontratına dokunmama | S1-S3 |
| 3 firma arası yansıtma izlenemiyor | İç transfer alanları (kaynak/hedef firma/durum/belge no) ekle | S4 |

### 🏁 Kabul Kriterleri (Go/No-Go)

1. Puantaj mevcut akışları kırılmadan çalışmalı
2. Çoklu slot + sefer CRUD sorunsuz çalışmalı
3. Çakışmalar teknik olarak engellenmeli ve görsel olarak anlaşılır olmalı
4. Önceki ay kopyalama güvenli ve izlenebilir olmalı
5. 3 firma + kurum + işveren senaryosu finansal olarak takip edilebilmeli
6. Build başarılı olmalı; kritik hata bulunmamalı

---

## 📅 21.05.2026 — Gün Sonu Özeti

### ✅ Bugün Tamamlanan (7 fix + temizlik + optimizasyon)

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **Fix 1** | NuGet build hatası | `nuget.config` (YENİ) | Proje köküne fallback klasörleri temizleyen `nuget.config` eklendi. .NET 10 SDK'nın eksik VS fallback klasörü hatası giderildi |
| **Fix 2** | Debug log temizliği | `KullaniciService.cs`, `ApplicationDbContext.cs`, `appsettings.Development.json` | `SaveWithLogAsync` metodu kaldırıldı (4 çağrı → `SaveChangesAsync`). EF Core log seviyeleri `Information` → `Warning`. `AssignFirmaTenantId` debug log'ları kaldırıldı |
| **Fix 3** | `AktiviteLogInterceptor` DI hatası | `AktiviteLogInterceptor.cs` | Singleton interceptor'da `IServiceProvider` → `IServiceScopeFactory`. Scoped `IDbContextFactory<ApplicationDbContext>` artık scope içinde resolve ediliyor |
| **Fix 4** | Auth servisleri MasterDbContext'e geçiş | `KullaniciService.cs`, `KullaniciUserStore.cs`, `AppAuthenticationStateProvider.cs` | 3 auth servisi `IDbContextFactory<ApplicationDbContext>` → `IDbContextFactory<MasterDbContext>`. `EnsureRolePermissionAsync` parametresi de güncellendi |
| **Fix 5** | Veri göçü iyileştirme | `TenantDatabaseService.cs` | `CopyTableDataAsync` non-static yapıldı, `ILogger` entegre edildi. Sütun uyumsuzlukları `LogWarning` ile loglanıyor (ilk hata mesajı + başarılı/atlanan sayısı). Kaynak tablo varlık kontrolü eklendi. Ortak sütun yoksa kaynak/hedef sütun listesi loglanıyor |
| **Fix 6** | IFirmaTenant query filter disable | `ApplicationDbContext.cs` | Tenant DB'de `FirmaTenantDisabled` artık `true` dönüyor (`AktifFirmaBilgisi.DatabaseName != null`). Tenant DB'de tüm veri zaten o firmaya ait, filter gereksiz |
| **Fix 7** | Tenant DB pooling optimizasyonu | `TenantDbContextFactory.cs` | `ConcurrentDictionary<string, DbContextOptions>` cache eklendi. `GetOrAdd` ile connection string başına bir kez options oluşturuluyor, sonraki çağrılarda cached kullanılıyor |
| **Temizlik** | Build uyarıları temizliği | `EvrakDetay.razor`, `BankaHesapList.razor`, `AracMasrafSahibiHelper.cs`, `ApplicationDbContext.cs` | `EbysAIPanel` bileşeni ve ilgili kodlar (showAIPanel, OnAIKategoriSecildi, OnAIOzetOlusturuldu) kaldırıldı. Kullanılmayan `firmalar`/`firmasizHesaplar` alanları silindi. XML yorum `&` → `&amp;` fix. `GetQueryFilter()` obsolete → pragma ile bastırıldı |

### 🧪 Smoke Test Sonuçları

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| Uygulama başlatma | ✅ `http://0.0.0.0:5200` |
| Tüm startup görevleri (25+) | ✅ Hepsi başarılı |
| Master DB | ✅ Mevcut |
| Tenant DB otomatik oluşturma | ✅ "Tüm firmaların tenant DB'si mevcut" |
| Quartz Scheduler | ✅ Başlatıldı |
| `GET /login` | ✅ HTTP 200 |
| `GET /ayarlar/firmalar` | ✅ HTTP 200 |
| `GET /ayarlar/kullanicilar` | ✅ HTTP 200 |
| `GET /ayarlar/roller` | ✅ HTTP 200 |
| `GET /dashboard` | ✅ 401 (giriş gerekli - beklenen) |
| Runtime hata/exception | ✅ **0 hata** |

### 📊 Değişen Dosya Özeti

```
nuget.config                                          (YENİ)
KOAFiloServis.Web/Data/AktiviteLogInterceptor.cs      (DI fix)
KOAFiloServis.Web/Data/ApplicationDbContext.cs         (debug log + obsolete fix)
KOAFiloServis.Web/Services/KullaniciService.cs         (SaveWithLogAsync + MasterDbContext)
KOAFiloServis.Web/Services/KullaniciUserStore.cs       (MasterDbContext)
KOAFiloServis.Web/Services/AppAuthenticationStateProvider.cs (MasterDbContext)
KOAFiloServis.Web/Services/TenantDatabaseService.cs    (veri göçü loglama)
KOAFiloServis.Web/Services/AracMasrafSahibiHelper.cs   (XML fix)
KOAFiloServis.Web/Components/Pages/EBYS/EvrakDetay.razor          (EbysAIPanel kaldırıldı)
KOAFiloServis.Web/Components/Pages/BankaHesaplari/BankaHesapList.razor (unused fields)
KOAFiloServis.Web/appsettings.Development.json         (EF log seviyeleri)
```

**Toplam: 11 dosya + 1 yeni, +64 / -93 satır (net -29)**

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| Tenant DB'de firma geçişi çalışıyor mu? | 🔴 Test edilmedi (manuel giriş gerek) |
| Tenant DB UI "Oluştur" butonu testi | 🔴 Test edilmedi (manuel giriş gerek) |
| `IFirmaTenant` query filter tenant DB'de disable | 🟢 Tamamlandı |
| Holding girişi tasarımı | 🔴 Başlanmadı |

---

## 📅 21.05.2026 — Yapılacak İşler (Plan) — GÜNCEL

### 🔴 Manuel Test (Kullanıcı Girişi Gerek)

| # | İş | Açıklama |
|---|-----|----------|
| 1 | Firma geçiş testi | Tenant DB'ye geçince dashboard/sayfalar sadece o firmanın verisini gösteriyor mu? |
| 2 | Tenant DB UI testi | Admin panelden "Tenant DB Oluştur" butonu çalışıyor mu? |
| 3 | Holding raporları testi | Login olup `/holding` sayfalarına girince veriler geliyor mu? |

### 🟢 Multi-DB Geçişi — TAMAMLANDI

| # | İş | Durum |
|---|-----|:-----:|
| 1-6 | Faz 1-6 (Altyapı, Master DB, Tenant DB, DI fix, veri göçü, query filter) | ✅ |
| 7 | Fix 1-8 (NuGet, Debug log, DI, Auth, Veri göçü, Filter, Pooling, WithMany) | ✅ |
| 10 | Holding modülü (DbContext + Service + 7 UI sayfası + Quartz) | ✅ |
| 11 | ApplicationDbContext master tablo temizliği | 🔵 ERTELENDİ |
| 12 | Pooling optimizasyonu | ✅ |

### 🟡 Sonraki Sprint: Puantaj Modülü (Kurum/Firma Bazlı)

| Sprint | Tema | Süre |
|:------:|------|:---:|
| S1 | Temel Uyum + Sefer CRUD | 3-4 gün |
| S2 | Çakışma Motoru + Validasyon | 2-3 gün |
| S3 | Çakışma UX + Önceki Ay Kopyalama | 3-4 gün |
| S4 | Fatura/Transfer Takibi | 2-3 gün |
| S5 | UAT ve Canlıya Geçiş | 2-3 gün |

> Detaylar için yukarıdaki "Sprint Planı: Puantaj Modülü" bölümüne bakın.

---

## 📅 20.05.2026 — Faz 3 Uygulama Oturumu

### Commit: `0261aa6`
```
feat(multi-db): Faz 3 - Tenant DB olusturma UI + veri gocu altyapisi
```

### ✅ Faz 3 — Tamamlanan Adımlar

| # | Adım | Değişen Dosyalar | Özet |
|---|------|-----------------|------|
| 1 | FirmaYonetimi.razor UI | `Components/Pages/Ayarlar/FirmaYonetimi.razor` | DatabaseName badge (Shared DB / tenant adı), Tenant DB Oluştur butonu, loading state |
| 2 | Veri göçü metodu | `Services/TenantDatabaseService.cs`, `Services/ITenantDatabaseService.cs` | `MigrateFirmaDataAsync`: FirmaId kolonu içeren tüm tablolardan shared→tenant veri kopyalama (sütun eşleştirmeli), `CreateTenantDatabaseAsync` artık otomatik veri göçü yapıyor |
| 3 | DatabaseName koruması | `Services/FirmaService.cs` | `UpdateAsync`: mevcut entity okunup seçici alan güncellemesi, DatabaseName overwrite edilemez |
| 4 | Smoke test | — | `dotnet build` → **0 hata**, Login + FirmaYonetimi sayfaları HTTP 200 |

### 🧪 Smoke Test

| Kontrol | Sonuç |
|---------|:-----:|
| `dotnet build` | ✅ 0 hata, 5 uyarı |
| `GET /` login sayfası | ✅ HTTP 200 |
| `GET /ayarlar/firmalar` | ✅ HTTP 200 |
| DatabaseName badge UI | ✅ Shared DB / tenant adı gösterimi hazır |
| Tenant DB Oluştur butonu | ✅ UI'da görünür (sadece DatabaseName null ise) |

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| Tenant DB oluşturma UI üzerinden test edilmedi (manuel login gerek) | 🟡 Kullanıcı testi bekliyor |
| Veri göçü sırasında büyük tablolarda performans | 🟡 İlk testte izlenecek |
| Authentication hala shared DB'den yapılıyor | 🟡 Faz 4'te ele alınacak |

### 📊 Faz 3 Durumu: 🟢 TAMAMLANDI

Tenant DB oluşturma UI + veri göçü altyapısı hazır. Manuel test için: Admin panel → Firma Yönetimi → "Tenant DB Oluştur" butonu.

---

## 📅 20.05.2026 — Faz 2 Uygulama Oturumu

### Commit: `2de0ef4`
```
feat(multi-db): Faz 2 - Master DB fiziksel ayrim + TenantDatabaseService
```

### ✅ Faz 2 — Tamamlanan Adımlar

| # | Adım | Değişen Dosyalar | Özet |
|---|------|-----------------|------|
| 1 | Master DB oluşturma script'i | `Data/DbInitializer.cs` | `EnsureMasterDatabaseAsync` eklendi: raw SQL ile `KOAFiloServis_Master` DB + 6 tablo oluşturur, shared DB'den veri kopyalar (sütun eşleştirmeli) |
| 2 | `TenantDatabaseService` | `Services/ITenantDatabaseService.cs` (YENİ), `Services/TenantDatabaseService.cs` (YENİ) | `CreateTenantDatabaseAsync`: tenant DB oluşturur, migration uygular, `Firma.DatabaseName` günceller |
| 3 | DbInitializer akış güncelleme | `Program.cs` | `EnsureMasterDatabaseAsync` DbInitializer'dan ÖNCE çağrılır |
| 4 | Config + DI | `appsettings.json`, `Program.cs` | `MasterConnection` ayrı DB'ye yönlendirildi, `ITenantDatabaseService` DI kaydı eklendi |
| 5 | Build + smoke test | — | `dotnet build` → **0 hata**, app başlatma → Master DB otomatik oluştu, **6/6 tablo veri kopyalandı** |

### 🧪 Smoke Test Sonuçları

| Tablo | Satır | Durum |
|-------|:-----:|:-----:|
| Firmalar | 4 | ✅ |
| Kullanicilar | 10 | ✅ (sütun eşleştirme fixi sonrası) |
| Roller | 9 | ✅ |
| RolYetkileri | 211 | ✅ |
| Lisanslar | 29 | ✅ |
| AppAyarlari | 5 | ✅ |
| Login sayfası | HTTP 200 | ✅ |

### 🏗️ MİMARİ KARAR — Master DB Raw SQL Yaklaşımı

**Karar:** Master DB tabloları EF Core migration ile değil, raw SQL `CREATE TABLE IF NOT EXISTS` ile oluşturuldu.

**Gerekçe:** MasterDbContext'in EF Core entity discovery'si, Firma entity'sine FK ile bağlanan onlarca tenant entity'sini cascade ederek 41+ tablo oluşturmaya çalıştı. Raw SQL yaklaşımı bu sorunu tamamen bypass eder ve sadece istenen 6 tabloyu oluşturur.

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| Kullanici authentication henüz Master DB'den yapılmıyor | 🟡 `KullaniciService` ve auth servisleri hala ApplicationDbContext kullanıyor |
| Tenant DB oluşturma test edilmedi (UI yok) | 🟡 Servis hazır, admin panel butonu eksik |
| Veri göçü (shared→tenant) henüz yapılmadı | 🔴 Faz 3'te ele alınacak |

### 📊 Faz 2 Durumu: 🟢 TAMAMLANDI

Master DB fiziksel olarak ayrıldı, veriler kopyalandı, TenantDatabaseService hazır.

---

## 📅 20.05.2026 — Database-Per-Firma Faz 1 Başlangıç Oturumu

### Yapılanlar

| # | İş | Detay |
|---|-----|-------|
| 1 | `AIAsistan.razor` kaldırıldı | Derleme hatası olan sayfa tamamen silindi, `NavMenu.razor`'daki link kaldırıldı |
| 2 | Build doğrulandı | `dotnet build` → 0 hata, 5 uyarı (önceden mevcut) |
| 3 | Mimari keşif tamamlandı | ApplicationDbContext, TenantAwareDbContextFactory, IAktifFirmaProvider, IFirmaTenant, Firma entity, Program.cs DI, appsettings.json incelendi |
| 4 | Faz planı hazırlandı | 6 fazlı Database-Per-Firma geçiş planı (Faz 0-6) oluşturuldu |
| 5 | Kayıt defteri formatı belirlendi | Tarih+amaç, commit, değişen dosyalar, değişiklik özeti, test/build sonucu, risk/açık işler, faz durumu, karar+gerekçe |

### 🏗️ MİMARİ KARAR — Faz 1 Yaklaşımı

**Karar:** Hybrid model ile kademeli geçiş.

**Gerekçe:**
- `Firma.DatabaseName == null` → eski shared DB modu (mevcut çalışan sistem)
- `Firma.DatabaseName != null` → yeni tenant DB modu (tam izolasyon)
- Aynı `ApplicationDbContext` her iki modda da çalışır — tenant query filter shared modda izolasyon sağlar, dedicated modda zararsız no-op olur
- `MasterDbContext` sadece global tabloları (Firmalar, Kullanicilar, Lisans) yönetir

### 📋 Faz 1 Uygulama Planı (Özet)

| Sıra | Adım | Dosya |
|:----:|------|-------|
| 1 | `Firma` entity'sine `DatabaseName` ekle | `Shared/Entities/Firma.cs` |
| 2 | `ITenantConnectionStringProvider` arayüzü | `Web/Services/` (YENİ) |
| 3 | `TenantConnectionStringProvider` implementasyonu | `Web/Services/` (YENİ) |
| 4 | `MasterDbContext` oluştur | `Web/Data/` (YENİ) |
| 5 | `ApplicationDbContext`'ten master tabloları çıkar | `Web/Data/ApplicationDbContext.cs` |
| 6 | `TenantDbContextFactory` (eskisini replace et) | `Web/Data/` (YENİ + SİL) |
| 7 | `appsettings.json`'a `MasterConnection` ekle | `Web/appsettings.json` |
| 8 | `Program.cs` DI kayıtlarını güncelle | `Web/Program.cs` |
| 9 | `FirmaService` + master tablo servislerini güncelle | `Web/Services/` |
| 10 | Migration klasörlerini düzenle | `Data/Migrations/` |
| 11 | `DbInitializer` güncelle | `Web/Data/DbInitializer.cs` |
| 12 | Build + smoke test | — |

### ⚠️ Riskler / Açık İşler

| Risk | Durum |
|------|:-----:|
| V1 (70+ entity) vs V2 (24 entity) kararı — V2 öneriliyor | 🔴 Karar bekliyor |
| `LisansService` singleton ama MasterDbContext scoped | Çözüm: `IDbContextFactory<MasterDbContext>` |
| Cross-tenant (TumFirmalar) sorgular dedicated DB'de çalışmaz | Faz 1'de sadece shared-DB firmalar için destek |
| Pooling per-tenant DB'ler için optimize değil | Faz 2'de `ConcurrentDictionary` cache |

### 🎯 Sonraki Adım

Faz 1 Adım 1: `Firma` entity'sine `DatabaseName` alanı eklenmesi.

---

## 📅 20.05.2026 — Faz 1 Uygulama Oturumu (İkinci Kısım)

### Commit: `cba5d90`
```
feat(multi-db): Faz 1 altyapi - Database-Per-Firma hybrid mimari kurulumu
```

### ✅ Faz 1 — Tamamlanan Adımlar

| # | Adım | Değişen Dosyalar | Özet |
|---|------|-----------------|------|
| 1 | `Firma` + `AktifFirmaBilgisi` güncelle | `Shared/Entities/Firma.cs`, `Web/Services/FirmaService.cs` | `DatabaseName` alanı eklendi, `AktifFirmaBilgisi`'ne taşındı |
| 2 | `ITenantConnectionStringProvider` + implementasyon | `Web/Services/ITenantConnectionStringProvider.cs` (YENİ), `Web/Services/TenantConnectionStringProvider.cs` (YENİ) | Dinamik connection string çözümleyici: `DatabaseName == null` → shared DB, `!= null` → tenant DB |
| 3 | `MasterDbContext` oluştur | `Web/Data/MasterDbContext.cs` (YENİ) | 6 çekirdek global tablo: Firmalar, Kullanicilar, Lisanslar, Roller, RolYetkileri, AppAyarlari. Navigation property temizliği yapıldı |
| 4 | ApplicationDbContext temizliği | — | **ERTELENDİ:** FK kırılma riski nedeniyle tüm entity'ler korundu. Faz 4'te yapılacak |
| 5 | `TenantDbContextFactory` (eskisini replace) | `Web/Data/TenantDbContextFactory.cs` (YENİ), `TenantAwareDbContextFactory.cs` (SİL) | `ITenantConnectionStringProvider` ile dinamik DB bağlantısı, pooling Faz 2'de |
| 6-7 | `appsettings.json` + `Program.cs` DI | `Web/appsettings.json`, `Web/Program.cs` | `MasterConnection` eklendi (şimdilik shared DB ile aynı), `TenantDatabase` bölümü eklendi, DI kayıtları güncellendi, `PooledDbContextFactoryHolder` kaldırıldı |
| 8 | Master tablo servisleri güncelle | `Web/Services/LisansService.cs`, `Web/Services/FirmaService.cs` | `LisansService`: `PooledDbContextFactoryHolder` → `IDbContextFactory<MasterDbContext>`, `FirmaService`: `IDbContextFactory<ApplicationDbContext>` → `IDbContextFactory<MasterDbContext>` |
| 9 | Migration | `Web/Migrations/..._MultiDbFaz1_AddFirmaDatabaseName.cs` (YENİ) | `Firmalar` tablosuna `DatabaseName` kolonu eklendi (varchar(100), nullable) |
| 10 | Build doğrulama | — | `dotnet build` → **0 hata, 5 uyarı** ✅ |

### 🏗️ MİMARİ KARAR — MasterDbContext Migration Ertelendi

**Karar:** MasterDbContext migration'ı Faz 2'ye ertelendi. Faz 1'de Master DB fiziksel olarak ayrılmadı, `MasterConnection` şimdilik DefaultConnection ile aynı shared DB'yi gösteriyor.

**Gerekçe:** MasterDbContext entity keşfi (navigation property cascade) 41+ tablo oluşturmaya çalıştı. MasterDbContext'in sadece 6 core tabloyla sınırlanması için kapsamlı `Ignore<>()` konfigürasyonu gerekiyor. Bu iş Faz 2'de Master DB fiziksel ayrımıyla birlikte yapılacak.

### 📊 Faz 1 Durumu: 🟡 KISMEN TAMAMLANDI

**Tamamlanan:** Altyapı (entity, provider, factory, DI, migration) kuruldu, build temiz.
**Ertelenen:** ApplicationDbContext master tablo temizliği, MasterDbContext migration'ı, DbInitializer güncellemesi.

### 🧪 Runtime Smoke Test (20.05.2026 - 3. Kısım)

| Kontrol | Sonuç |
|---------|:-----:|
| `dotnet ef database update` — migration uygulama | ✅ `MultiDbFaz1_AddFirmaDatabaseName` başarıyla uygulandı |
| `dotnet run` — uygulama başlatma | ✅ `Now listening on: http://0.0.0.0:5190` |
| Startup görevleri (Seed, Quartz, GPS) | ✅ Tümü başarılı |
| Uygulama log'larında hata/exception | ✅ **0 hata** |
| `GET /` — giriş sayfası | ✅ HTTP 200, "Giris - Koa Filo Servis" |
| `GET /login` — login sayfası | ✅ HTTP 200 |

**Sonuç:** ✅ Uygulama sorunsuz başlıyor, login sayfası açılıyor, hiçbir hata yok.

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| V1 (70+ entity) vs V2 (24 entity) kararı | 🔴 Karar bekliyor |
| MasterDbContext migration cascade | 🟡 Faz 2'de çözülecek |
| Runtime smoke test | ✅ **TAMAMLANDI** — uygulama başlıyor, login açılıyor, 0 hata |
| `KullaniciService` + auth servisleri hala ApplicationDbContext kullanıyor | 🟡 Faz 2'de güncellenecek |
| Cross-tenant (TumFirmalar) dedicated DB'de çalışmaz | 🟡 Faz 2'de ele alınacak |

---

## 📅 14.05.2026 — AI Asistan + Mimari Karar Oturumu

### Commit: `952a546`
```
feat(ai-asistan): DeepSeek V3/R1 model katalogu + docs guncelleme
```

---

### 🤖 AI Asistan Model Kataloğu

**Konu:** DeepSeek V4 yapay zeka listesine eklenebilir mi?

**Araştırma Sonucu:**
- Ollama public registry'de `deepseek-v4` tag'i **mevcut değil** (14.05.2026 tarihi itibarıyla)
- Mevcut resmi DeepSeek sürümleri:
  - `deepseek-v3` — Genel amaçlı, güçlü model
  - `deepseek-r1` — Reasoning (akıl yürütme) modeli
  - `deepseek-coder-v2` — Kod odaklı model

**Yapılan Değişiklik:**
- `AIAsistan.razor` → `GetBirlesikModelListesi()` metoduna `deepseek-v3` ve `deepseek-r1` eklendi
- Dropdown artık iki grup gösteriyor:
  - **Yerel (Ollama):** Makinede `ollama pull` ile yüklenmiş modeller
  - **Önerilen (yüklü değil):** Katalogdaki ama henüz indirilmemiş modeller
- Yüklü olmayan model seçilince `ollama pull <model>` komutu ipucu olarak gösteriliyor

**Dosya:** `KOAFiloServis.Web/Components/Pages/Ayarlar/AIAsistan.razor`

---

### 🏗️ MİMARİ KARAR — Database Per Firma

#### Sorun Tanımı
Müşteri 3 firma ile çalışıyor. Mevcut "Shared Database + FirmaId row-level isolation"
mimarisinde kullanıcıların hatalı firma seçimi veya filter kaçağı durumunda firmaların
verileri birbirine karışabiliyor. Zirve Müşavirlik gibi referans sistemlerde her firma
ayrı veritabanında çalışıyor.

#### Mevcut Mimari (Shared DB)
```
PostgreSQL: DestekCRMServisBlazorDb (TEK DB)
  Araclar   → FirmaId=1, FirmaId=2, FirmaId=3  (hepsi aynı tabloda)
  Cariler   → FirmaId=1, FirmaId=2, FirmaId=3
  Faturalar → FirmaId=1, FirmaId=2, FirmaId=3

Koruma mekanizması: HasQueryFilter("FirmaId == aktifFirma")
Zayıf nokta: Kullanıcı hatalı firma seçimi → yanlış veri görme/yazma riski
```

#### Hedef Mimari (Database Per Firma)
```
PostgreSQL Server
  db_global   → Kullanıcılar, Lisans, Firma katalogu
  db_firma_1  → Firma A'nın TÜM verileri (tam izolasyon)
  db_firma_2  → Firma B'nın TÜM verileri (tam izolasyon)
  db_firma_3  → Firma C'nın TÜM verileri (tam izolasyon)
  db_holding  → Ortak/Holding konsolidasyon DB
```

#### Gerekçe
1. **Veri güvenliği:** DB seviyesinde fiziksel izolasyon — filter bypass imkansız
2. **Müşteri talebi:** Zirve Müşavirlik benzeri yapı isteniyor
3. **Holding ihtiyacı:** 3 firmayı birleştiren ortak raporlama / bütçe konsolidasyonu
4. **Yedekleme:** Firma bazlı backup/restore kolaylaşır
5. **KVKK/Hukuki:** Firma verisi fiziksel olarak ayrı

---

### 🏢 HOLDİNG / ORTAK FİRMA MODÜLÜ

#### Kavram
3 (veya daha fazla) operasyonel firmanın finansal verilerini **özetleyerek** tek bir
"Holding" veritabanında konsolide eden yeni modül.

#### Holding'e Ne Aktarılır?
| Veri Türü | Aktarılır | Not |
|-----------|:---------:|-----|
| Bütçe gerçekleşmesi | ✅ | Gelir/gider toplam |
| Fatura toplamları | ✅ | KDV dahil/hariç |
| Banka/Kasa bakiyesi | ✅ | Dönem sonu snapshot |
| Personel gider özeti | ✅ | Bordro toplamı |
| Araç maliyet özeti | ✅ | Bakım+yakıt toplam |
| Hakediş ödemeleri | ✅ | Tedarikçi toplamı |
| Tekil fatura detayı | ❌ | Gizlilik/boyut |
| Personel özlük | ❌ | KVKK |
| Cari kart detayı | ❌ | Firma içi bilgi |

#### Holding Rapor Türleri
- Firma Karşılaştırma (Gelir/Gider/Kâr yan yana)
- Bütçe Konsolidasyonu (tüm firmalar toplam)
- Ödeme Planı (tüm firmaların vadesi gelenler)
- Araç Maliyet Özeti (firma bazlı)
- Personel Gider Özeti (firma bazlı bordro toplam)
- Hakediş Özeti (tedarikçi ödemeleri)

---

### 🔀 GITHUB — YENİ REPO KARARI

#### Soru
> "GitHub'da bu yapıya dokunmadan yeni proje gibi açıp projeyi oraya kopyalayıp
> oradan devam etsek olur mu?"

#### Yanıt ve Değerlendirme
**Evet, tamamen uygulanabilir.** İki yöntem var:

---

**Yöntem 1 — Fork (Önerilen)**
```bash
# GitHub web arayüzünde:
# 1. https://github.com/karamur/KOAFiloServis → "Fork" butonu
# 2. Yeni repo adı: KOAFiloServis-v2  (veya KOAFiloServis-MultiDb)
# 3. Sadece main branch'i fork et

# Yerel:
git clone https://github.com/karamur/KOAFiloServis-v2
cd KOAFiloServis-v2
# Upstream'i orijinal repo olarak ekle (gelecekte senkronizasyon için)
git remote add upstream https://github.com/karamur/KOAFiloServis
```
✅ Orijinal repo **aynen korunur** (production backup)
✅ Yeni repoda Database-Per-Firma geçişi yapılır
✅ İleride orijinale patch geri alınabilir (`git cherry-pick`)

---

**Yöntem 2 — Yeni Boş Repo + Kopyalama**
```bash
# GitHub'da yeni repo oluştur: KOAFiloServis-v2

# Yerel — mevcut kodu yeni remote'a bağla:
cd "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis"
git remote add v2 https://github.com/karamur/KOAFiloServis-v2
git push v2 main

# Yeni çalışma klasörü:
cd C:\Users\muratk\Desktop\d yedek\calisma\
git clone https://github.com/karamur/KOAFiloServis-v2 KOAFiloServis-v2
```
✅ Temiz başlangıç
⚠️ Commit geçmişi taşınır (arzu edilmezse `--depth 1` veya squash)

---

**Önerilen Akış:**
```
karamur/KOAFiloServis        → Mevcut production kodu (dokunulmaz, korunur)
karamur/KOAFiloServis-MultiDb → Yeni Database-Per-Firma mimarisi geliştirme
```

Geçiş tamamlanıp test edilince `KOAFiloServis-MultiDb` → `KOAFiloServis`'e merge edilir
veya doğrudan production'a alınır.

---

### 📋 Uygulama Fazları (Özet)

| Faz | İçerik | Tahmini Süre |
|-----|--------|:---:|
| **Faz 1** | GlobalDbContext + TenantDbContext ayrımı + ITenantDbResolver | 3–4 gün |
| **Faz 2** | Mevcut veri göçü (3 firma DB'sine taşıma) | 1–2 gün |
| **Faz 3** | Holding modülü + konsolidasyon raporları | 3–4 gün |
| **Faz 4** | IFirmaTenant + FirmaId temizliği | 1–2 gün |
| **Test** | Stabilizasyon | 2–3 gün |
| **TOPLAM** | | **~10–15 gün** |

---

### ⚠️ Riskler

| Risk | Önlem |
|------|-------|
| Veri göçü sırasında kayıp | Tam backup → row count doğrulama |
| FirmaKopyalama çoklu DB'de kırılma | Önce refactor, sonra göç |
| Migration yönetimi karmaşıklığı | Tek TenantDbContext migration path |
| Holding raporu performansı | Task.WhenAll ile paralel DB sorgusu |

---

### 🎯 Sonraki Adım (Onay Bekliyor)

Yeni repo açma ve Faz 1'e başlama kararı alınırsa:
1. GitHub'da `KOAFiloServis-MultiDb` reposu oluştur
2. Mevcut kodu oraya kopyala (`git push v2 main`)
3. `GlobalDbContext` ve `TenantDbContext` dosyalarını oluştur
4. `ITenantDbResolver` interface + implementasyonunu yaz
5. `appsettings.json`'a `TenantDb:Template` bölümü ekle

---

## 📚 İlgili Dosyalar

| Dosya | Konu |
|-------|------|
| `docs/TENANT_MIGRATION_PLAN.md` | Mevcut tenant migrasyonu (tamamlanmış) |
| `docs/CALISMA-NOTLARI-2026-05-13.md` | Önceki oturum notları |
| `docs/CALISMA-NOTLARI-2026-05-14.md` | Bu oturum notları |
| `docs/OTURUM_NOTLARI_2026-05-19.md` | Tenant v1.0.21 tamamlandı notu |
| `KOAFiloServis.Shared/Entities/IFirmaTenant.cs` | Mevcut tenant interface |
| `KOAFiloServis.Web/Data/TenantAwareDbContextFactory.cs` | Mevcut factory |
| `KOAFiloServis.Web/Services/IAktifFirmaProvider.cs` | Aktif firma state servisi |

---

## 📅 23.05.2026 — Kiralık C Plaka Takibi Geliştirme Planı

> **Talep:** Kiralık C Plaka Takibi modülünde eşleştirme, fatura, ödeme takibi ve raporlama özellikleri.

### 🔴 ACİL BUG — Güzergah Düzenle

| Sorun | Açıklama |
|-------|----------|
| Kapasite, araç, şoför bilgileri gelmiyor | `GuzergahForm` edit modunda `AracService.GetActiveAsync()`, `SoforService.GetActiveAsync()` tenant DB'den veri çekiyor olabilir. Multi-tenant izolasyonunda araç/şoför listesi boş geliyor. |

**Çözüm:** `GuzergahForm.OnInitializedAsync` içinde servis çağrılarının doğru DB'ye yönlendiğinden emin olunacak.

---

### 📋 FAZ PLANI — Kiralık C Plaka Takibi

#### Faz 1: Eşleştirme Derinliği (1-2 gün)
| # | İş | Açıklama |
|---|-----|----------|
| 1.1 | 1. Eşleştirme - Araç detay | `KiralikPlakaList` tablosunda eşleşen aracın detay bilgileri gösterilecek: Marka, Model, Yıl, Şasi No, Yakıt Tipi, Plaka |
| 1.2 | 2. Eşleştirme - Tam bilgi | Eşleşen araca tıklandığında modal/panel ile aracın TÜM bilgileri: Sigorta, Muayene, Koltuk Sigortası, Ruhsat, Evraklar, Bağlı olduğu güzergah, Atandığı şoför |
| 1.3 | Güzergah Düzenle BUG FIX | Kapasite, araç, şoför dropdown'larının tenant DB'de düzgün yüklenmesi |

#### Faz 2: Fatura Entegrasyonu (2-3 gün)
| # | İş | Açıklama |
|---|-----|----------|
| 2.1 | Fatura sütunu | `KiralikPlakaList` tablosuna "Kesilen Fatura" sütunu: fatura no, tarih, tutar |
| 2.2 | Kesilecek fatura takibi | `KiralikPlakaTakip` entity'sine fatura alanları: `KesilenFaturaId`, `KesilenFaturaTutar`, `KalanFaturaTutar` |
| 2.3 | Fatura listesi/raporu | Plaka bazlı fatura dökümü: PDF/Excel export, hesap pusulası görünümü |
| 2.4 | Fatura eşleştirme | Gelen faturalar (Alış Faturası) ile plaka takip kaydı eşleştirme: `KiralikPlakaTakip.GelenFaturaId` |

#### Faz 3: Ödeme Takibi (2-3 gün)
| # | İş | Açıklama |
|---|-----|----------|
| 3.1 | Ödeme planı | `KiralikPlakaTakip` entity'sine ödeme alanları: `ToplamOdeme`, `OdenenTutar`, `KalanOdeme`, `SonOdemeTarihi` |
| 3.2 | Aylık/kısmi ödeme | Periyot="AYLIK" ise aylık ödeme takvimi otomatik oluşturma. Kısmi ödeme girişi |
| 3.3 | Ödeme listesi/önizleme | Plaka bazlı ödeme takvimi: bu ay, gelecek aylar, geçmiş ödemeler |
| 3.4 | Ödeme dökümü | Ödeme geçmişi PDF/Excel export |

#### Faz 4: Raporlama ve Hesap Pusulası (1-2 gün)
| # | İş | Açıklama |
|---|-----|----------|
| 4.1 | Hesap Pusulası | Plaka bazlı: Toplam borç, ödenen, kalan, vade takvimi — tek sayfa özet |
| 4.2 | Fatura + Ödeme özet raporu | Tüm plakalar için konsolide fatura/ödeme durumu |
| 4.3 | Aylık önizleme | Aylık kesilecek fatura ve yapılacak ödemelerin toplu önizlemesi + Excel export |

### 🏗️ Mimari Kararlar

| Karar | Gerekçe |
|-------|---------|
| `KiralikPlakaTakip` entity'si genişletilecek | Yeni alanlar: fatura ve ödeme takibi için FK'lar eklenecek |
| Fatura eşleştirme çift yönlü | Hem kesilen hem gelen fatura ile eşleştirme |
| Ödeme takvimi otomatik | Periyot="AYLIK" ise başlangıç/bitiş tarihleri arası aylık ödeme satırları otomatik oluşturulur |

### 📊 Tahmini Süre

| Faz | İçerik | Süre |
|:---:|--------|:---:|
| Faz 1 | Eşleştirme Derinliği + Bug Fix | 1-2 gün |
| Faz 2 | Fatura Entegrasyonu | 2-3 gün |
| Faz 3 | Ödeme Takibi | 2-3 gün |
| Faz 4 | Raporlama ve Hesap Pusulası | 1-2 gün |
| **Toplam** | | **6-10 gün** |

---

## 📅 23.05.2026 — İkinci Oturum (6 commit, ~12 iyileştirme)

### ✅ Tamamlanan

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **Fix 1** | Bütçe FK hatası | `BudgetAnaliz.razor`, `BudgetService.cs` | `FirmaId=0` → FK constraint violation. 3 yerde `(TumFirmalar \|\| FirmaId <= 0) ? null` koruması |
| **Fix 2** | Layout firma/kullanıcı bilgisi | `NavMenu.razor` | Top bar'a aktif firma adı/kodu, dönem, kullanıcı adı eklendi |
| **Fix 3** | Araç FK hatası | `AracForm.razor`, `AracService.cs` | Tenant modunda firma dropdown filtrelendi, CreateAsync/UpdateAsync'te FirmaId validasyonu |
| **Feat 1** | Kiralık C Plaka Faz 1.1 | `KiralikPlakaList.razor` | Tabloda eşleşen araç detay: Marka, Model, Yıl |
| **Feat 2** | Kiralık C Plaka Faz 1.2 | `KiralikPlakaList.razor` | Araç detay modalı: sigorta, muayene, koltuk sigortası, bağlı güzergah, şoför |
| **Feat 3** | Kiralık C Plaka güzergah linki | `KiralikPlakaList.razor` | Modalda güzergah adı tıklanabilir → GüzergahForm'a link |

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |

### 📋 Commit Geçmişi

```
2a2bb68 fix(arac, kiralikplaka): Arac FK hatasi + Faz 1.1/1.2 KiralikPlaka eslestirme derinligi
51b7fac fix(budget): FirmaId=0 FK constraint hatasi + Layout'a aktif firma/donem/kullanici bilgisi
```

### 🏗️ Alınan Kararlar

| Karar | Gerekçe |
|-------|---------|
| FirmaId=0 her yerde null'a çevrildi | `AktifFirmaBilgisi.FirmaId` tipi `int` (null değil). `TumFirmalar=false` iken `FirmaId=0` → FK violation |
| Tenant modunda firma dropdown'ı filtrelendi | Tenant DB'de sadece tek firma var, dropdown'dan başka firma seçilince FK hatası |
| Kiralık C Plaka'da güzergah bilgisi DB'den direkt sorgulanıyor | `Guzergah` entity'sinde `GuzergahSeferleri` navigation'ı yok, `GuzergahSefer` entity'si ayrı |

### 🔴 Yapılacaklar (Sonraki Oturum)

| # | İş | Modül |
|---|-----|-------|
| 1 | Manuel testler (login gerek) | Genel |
| 2 | Kiralık C Plaka Excel/PDF export güncelleme | FiloOperasyon |
| 3 | Kiralık C Plaka Faz 5 - UAT ve Canlıya Geçiş | FiloOperasyon |

---

## 📅 23.05.2026 — Üçüncü Oturum (4 commit)

### ✅ Tamamlanan

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **Faz 2** | Fatura Entegrasyonu | `KiralikPlakaTakip.cs`, `KiralikPlakaForm.razor`, `KiralikPlakaList.razor` | 8 yeni alan: fatura/ödeme takibi, form/liste güncelleme |
| **Faz 3** | Ödeme Planı | `KiralikPlakaForm.razor` | Periyot=AYLIK → aylık ödeme takvimi tablosu |
| **Faz 4** | Hesap Pusulası | `KiralikPlakaList.razor` | Plaka bazlı özet modal: borç/ödenen/kalan/vade |
| **Layout** | Sidebar düzenleme | `NavMenu.razor` | Firma/kullanıcı bilgisi sidebar'a kart olarak taşındı |
| **Mig** | Fatura kolonları | `KiralikPlakaFaturaMigrationHelper.cs` | PostgreSQL kolon ekleme helper'ı |

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |

### 📋 Commit Geçmişi

```
4574090 fix(cache): JsonSerializer ReferenceHandler.IgnoreCycles eklendi
a9f227d feat(navmenu): Puantaj Excel Aktar menu linki eklendi
3672972 feat(puantaj): Excel'den puantaj import sayfasi
072ba66 feat(kiralikplaka): PDF export'a fatura/odeme sutunlari eklendi
f5395f5 feat(kiralikplaka): Excel export'a fatura/odeme sutunlari eklendi
eb3c29e fix(genel): Proje geneli FirmaId=0 FK korumasi (5 nokta)
cfa6ab0 feat(kiralikplaka): Faz 4 - Hesap Pusulasi modali
59e2ae2 fix(layout): NavMenu - Firma/kullanici bilgisi sidebar'a tasindi
f697192 feat(kiralikplaka): Faz 3 - Aylik odeme plani tablosu
c004a3d feat(kiralikplaka): Faz 2 - Fatura ve odeme takip alanlari
```

---

## 📅 23.05.2026 — Dördüncü Oturum (3 commit)

### ✅ Tamamlanan

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **Fix 1** | Tenant DB migration | `Program.cs` | `ApplyMigrationsToTenantDatabases`: tüm tenant DB'lere Slot/Fatura kolonları eklendi |
| **Fix 2** | Layout düzenleme | `MainLayout.razor`, `NavMenu.razor` | Firma/dönem bilgisi üst bara taşındı, sidebar kart kaldırıldı |
| **Feat 1** | Puantajı Güncelle | `Planlama.razor`, `KurumPuantajService.cs` | Güzergah/sefer değişikliklerini puantaja yansıtan buton |
| **Feat 2** | SeferTipi.Mesai | `Guzergah.cs`, `KurumPuantajService.cs` | SeferTipi enum'a Mesai eklendi, slot mantığı güncellendi |
| **Feat 3** | Gruplandırma varsayılan | `Planlama.razor` | Varsayılan gruplandırma "güzergah" olarak değiştirildi |

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 291/291 başarılı |

### 📋 Commit Geçmişi

```
d4ab624 feat(planlama): Güzergah gruplandirma varsayilan + SeferTipi.Mesai
b24eef4 feat(planlama): Puantaji Guncelle butonu + GuncellePuantajAsync
119bd25 fix(db): Tenant DB'lere migration helper uygulama task'i
2f9eadc fix(layout): Firma/donem bilgisi MainLayout ust bara tasindi
```

### 🔴 Yapılacaklar (Sonraki Oturum)

| # | İş | Modül |
|---|-----|-------|
| 1 | Araç firma değişikliği — tenant izolasyonu korunarak taşıma | Araçlar |
| 2 | PlanlamaEditModal — sefer başı/günlük/saatlik seçim ve şoför değişikliği | Planlama |
| 3 | Puantajda güzergah için ek sefer ekleme (hızlı sefer) | Planlama |
| 4 | Manuel testler (login gerek) | Genel |
| 5 | Kiralık C Plaka Excel/PDF export — özet sayfası güncelleme | FiloOperasyon |

---

## 📅 23.05.2026 — BUG RAPORU: Güzergah ve Puantaj Kırılma Sorunları

### 🔴 Tespit Edilen Hatalar

#### 1. EKSİK DB KOLONLARI (KRİTİK)

| Tablo | Eksik Kolon | Etki |
|-------|-------------|------|
| `PuantajKayitlar` | `SlotAdi` | Puantaj kaydı okuma/yazma hatası |
| `GuzergahSeferleri` | `Slot` | Güzergah sefer okuma/yazma hatası |

**Neden:** Yeni entity alanları eklendi ancak DB migration helper'ları güncellenmedi.
**Çözüm:** `PuantajSlotMigrationHelper` ve `GuzergahKoordinatMigrationHelper` güncellendi.

#### 2. KONTROL EDİLMESİ GEREKENLER

| # | Risk | Açıklama |
|---|------|----------|
| 2a | Planlama Slot/Güzergah toggle | Yeni gruplandırma kodunda null reference riski |
| 2b | Planlama OnayDurum filtresi | Filtre + VeriYukle metodunda slot filtresi çakışması |
| 2c | KurumPuantaj Slot dropdown | Edit modal'da Slot enum değişikliği |
| 2d | CopyPreviousMonthModal | Yeni eklenen alanların kopyalama sırasında hata verme riski |
| 2e | GuzergahSefer.Slot kolonu | DB'de kolon yoksa SablonOlusturAsync patlar |
| 2f | PuantajKayit.SlotAdi kolonu | DB'de kolon yoksa kaydetme işlemleri patlar |

#### 3. ACİL YAPILACAKLAR (Yarın)

| # | İş |
|---|-----|
| 3a | Uygulama başlatılıp `/guzergahlar` ve `/planlama` sayfaları test edilecek |
| 3b | Yeni güzergah oluşturma + düzenleme testi (kapasite/araç/şoför dropdown) |
| 3c | Planlama - Slot/Güzergah toggle + yeni sefer ekleme testi |
| 3d | Planlama - OnayDurum filtresi + hızlı onay toggle testi |
| 3e | KurumPuantaj - Slot dropdown + SlotAdi testi |
| 3f | Runtime exception log'ları kontrol edilecek |

### 🛠️ Yapılan Fix'ler

| Fix | Dosya |
|-----|-------|
| `PuantajKayitlar.SlotAdi` kolonu eklendi | `PuantajSlotMigrationHelper.cs` |
| `GuzergahSeferleri.Slot` kolonu eklendi | `GuzergahKoordinatMigrationHelper.cs` |
| Güzergah Düzenle - seçili araç/şoför listede yoksa ekleme | `GuzergahForm.razor` |
---

## 📅 24.05.2026 — Oturum (12 commit)

### Yapilanlar
- Tenant DB migration fix (Slot/Fatura kolonlari)
- FK constraint fix (Guzergah + GuzergahSefer)
- Layout: firma/donem bilgisi ust bara
- GuzergahForm: firma secimi + tasima modali
- FirmaTransferService: veri tasima/kopyalama
- Puantaj: IsverenFirmaId=0 FK fix, Yon fix, Sefer=1 satir, BirimFiyat×2
- Kurum Puantaj: ozet kartlari, Guncelle butonu

### Yapilacaklar
1. Kurum Puantaj - Gun grid, arac/sofor ekleme, cakisma, onay

---

## 📅 24.05.2026 — İkinci Oturum (KurumPuantaj Bug Fix)

### 🔴 Tespit Edilen Hatalar

| # | Dosya | Satır | Sorun | Şiddet |
|---|-------|:-----:|-------|:------:|
| **B1** | `KurumPuantaj.razor` | 882 | `degisikSatirlar.Remove(kayitlar[idx])` — atama sonrası *yeni* nesneyi set'ten silmeye çalışıyor. `BaseEntity`'de `Equals` override yok, referans eşitliği ile çalışır. Eski nesne yerine yeni nesne aranır → bulunamaz → silme başarısız, kirli satır set'te kalır. | 🔴 |
| **B2** | `KurumPuantaj.razor` | 843 | `YeniKayitEkle()` metodunda `FinansYonu = PlanlamaFinansYonu.Gelen` yazılmış, entity default'u `Giden`. Tutarsızlık. | 🟡 |
| **B3** | `KurumPuantaj.razor` | 805-827 | `TekKayitDuzenle()` deep copy'de `OnayDurum` alanı kopyalanmamış. Entity default'u `Taslak` olduğu için, mevcut `Onaylandi` bir kaydı düzenleyip kaydedince OnayDurum sıfırlanır (veri kaybı). | 🔴 |
| **B4** | `KurumPuantaj.razor` | 904 | `KayitSil()` — yeni kayıt (`Id==0`) silinince `degisikSatirlar`'dan kaldırılmıyor. Kullanıcı eklediği kaydı silse bile kirli set'te kalır, "Tümünü Kaydet"te hata oluşabilir. | 🟡 |

### ✅ Yapılan Fix'ler

| Fix | Dosya | Açıklama |
|-----|-------|----------|
| **B1** | `KurumPuantaj.razor:881-883` | `var eski = kayitlar[idx]` ile eski referans saklandı, atama sonrası `degisikSatirlar.Remove(eski)` ile doğru nesne siliniyor |
| **B2** | `KurumPuantaj.razor:843` | `FinansYonu = PlanlamaFinansYonu.Giden` olarak düzeltildi (entity default'u ile tutarlı) |
| **B3** | `KurumPuantaj.razor:827` | `OnayDurum = kayit.OnayDurum` deep copy'e eklendi |
| **B4** | `KurumPuantaj.razor:904` | `degisikSatirlar.Remove(kayit)` eklendi |

---

## 📅 25.05.2026 — Oturum (5 commit)

### ✅ Tamamlanan

| # | İş | Açıklama |
|---|-----|----------|
| **1** | SeferTipi.Vardiya eklendi | `Guzergah.cs`: `Vardiya = 6` |
| **2** | Sabah+Aksam merge | `ApplyMergeAndPricing`: aynı araç/şoför Sabah+Aksam → tek satır (Yon=SabahAksam, BirimFiyat×2). Farklı araç → ayrı satır |
| **3** | EkleEksikSatir fiyat düzeltme | `*2` çarpanı kaldırıldı, baz fiyat kullanılıyor, merge sonrası uygulanıyor |
| **4** | PuantajKaldirAsync | Kurum+dönem için tüm puantaj kayıtlarını soft-delete |
| **5** | TopluSavePuantajAsync yetim temizliği | Merge sonrası eski Aksam slot'lu satırlar soft-delete |
| **6** | KurumPuantaj "Puantajı Kaldır" butonu | JS confirm + servis çağrısı |
| **7** | Dashboard kurum filtresi | PlanlamaDashboard'a Kurum dropdown'ı, GetPuantajlarAsync'e kurumId parametresi |
| **8** | GüzergahForm otomatik çarpan iptali | `GetSeferTipiCarpani` + `SeferTipiDegistiAsync` fiyat güncelleme kodu kaldırıldı, manuel fiyat bilgi mesajı |
| **9** | Planlama modülü SİLİNDİ | 4 dosya (Planlama.razor, PlanlamaDashboard.razor, PlanlamaEditModal.razor, CopyPreviousMonthModal.razor) + NavMenu linkleri + KullaniciVeLisans Planlama yetkileri temizlendi |

### 📋 Commit Geçmişi

```
6e4fc03 feat(puantaj): Sabah+Aksam merge, Vardiya enum, Puantaj Kaldir, Dashboard kurum filtresi
0b1e13d refactor(puantaj): Planlama modulu kaldirildi, tek puantaj = Kurum Puantaj
```

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |

### 🔴 YARIN DEVAM EDİLECEK — OperasyonKaydi Entity

Kullanıcının son görevi: Operasyon çekirdeğini oluşturacak **OperasyonKaydi** entity'si.

**İstenenler:**
- Entity: `OperasyonKaydi` (Id, Tarih, KurumId, GuzergahId, SeferTipi, AracId, SoforId, FirmaId, KurumFiyati, TedarikciFiyati, Durum, Aciklama)
- `SeferTipi` enum (zaten mevcut)
- FluentValidation / DataAnnotations
- EntityTypeConfiguration
- Service interface + implementation
- Migration
- Clean architecture uyumlu

**Kurallar:**
- Sabah ve akşam farklı araç olabilir
- Sabah ve akşam farklı şoför olabilir
- Kuruma kesilecek fiyat tutulmalı
- Tedarikçiye ödenecek fiyat tutulmalı
- İptal edilen sefer puantaja dahil edilmemeli
- Aynı araç aynı saat aralığında ikinci sefere atanamamalı

**Açık sorular (yarın sorulacak):**
1. OperasyonKaydi vs mevcut PuantajKayit ilişkisi (yerine mi geçecek, birlikte mi çalışacak?)
2. Validation yaklaşımı (DataAnnotations mi, FluentValidation mi?)
3. Generic repository isteniyor mu?
4. Günlük kayıt mı (Tarih bazlı) yoksa aylık mı (Gun01..Gun31)?

---

## 📅 25.05.2026 — Sprint 1: OperasyonKaydi Entity Mimarisi

### ✅ Sprint 1 — Tamamlanan

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **S1.1** | OperasyonKaydi entity | `Shared/Entities/OperasyonKaydi.cs` (YENİ) | Günlük ham operasyon kaydı. `IFirmaTenant` destekli, audit (CreatedBy/UpdatedBy), soft delete (DeletedAt/DeletedBy), 30+ alan |
| **S1.2** | PuantajKayit güncelleme | `Shared/Entities/PuantajKayit.cs` | `OperasyonKayitlari` koleksiyonu eklendi |
| **S1.3** | DbContext güncelleme | `Data/ApplicationDbContext.cs` | `DbSet<OperasyonKaydi>`, fluent config: 8 FK (tümü Restrict), 13 index, string limits, decimal precision |
| **S1.4** | OperasyonKaydiService | `Services/Interfaces/IOperasyonKaydiService.cs`, `Services/OperasyonKaydiService.cs` (YENİ) | CRUD + şablon + PuantajKayit→OperasyonKaydi migrasyon |
| **S1.5** | PuantajEngineService | `Services/Interfaces/IPuantajEngineService.cs`, `Services/PuantajEngineService.cs` (YENİ) | OperasyonKaydi→PuantajKayit dönüşüm motoru |
| **S1.6** | Validator + BusinessRules | `Services/OperasyonKaydiValidator.cs`, `Services/OperasyonKaydiBusinessRules.cs` (YENİ) | Input validasyon + domain kuralları + çakışma kontrolü |
| **S1.7** | Migration | `Data/Migrations/*_AddOperasyonKaydi.cs` (YENİ) | OperasyonKayitlari tablosu |
| **S1.8** | DI kayıtları | `Program.cs` | `IOperasyonKaydiService`, `IPuantajEngineService`, `OperasyonKaydiBusinessRules` |
| **S1.9** | Duplicate check SQL | `docs/sql/migration-duplicate-check.sql` (YENİ) | Migration öncesi unique constraint kontrolü |

### 🏗️ Mimari Kararlar

| Karar | Gerekçe |
|-------|---------|
| OperasyonKaydi günlük (normalize) kayıt | Her satır = bir gün × bir araç × bir güzergah × bir slot. PuantajEngine gruplayıp aylık PuantajKayit üretir |
| Tüm FK'lar Restrict | Kurum/Araç/Şoför silinince operasyon silinmez, FK hatası alınır |
| OperasyonKaydiService 3 katmanlı | Validator (statik) → BusinessRules (DI) → Service (data access) |
| PuantajKayit korundu | Mevcut yapı bozulmadı, OperasyonKaydi yeni merkez entity olarak eklendi |

### 📊 Toplam: 10 yeni dosya, 3 değişiklik

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata, 0 uyarı** |
| `dotnet test` | ✅ 305/305 başarılı |
| `dotnet ef migrations add AddOperasyonKaydi` | ✅ Başarılı |

---

## 📅 25.05.2026 — Sprint 2: Operasyon Giriş Ekranı

### ✅ Sprint 2 — Tamamlanan

| # | İş | Dosyalar | Açıklama |
|---|-----|----------|----------|
| **S2.1** | OperasyonGiris sayfası | `Components/Pages/Operasyon/OperasyonGiris.razor` (YENİ) | Pure Bootstrap 5, Excel benzeri grid, inline edit |
| **S2.2** | Code-behind | `Components/Pages/Operasyon/OperasyonGiris.razor.cs` (YENİ) | Tarih/kurum/güzergah filtresi, autocomplete, dirty tracking, toplu kaydet |

### 🎯 Özellikler

- **Filtre barı**: Tarih (bugün/ileri/geri), Kurum (autocomplete), Güzergah (cascade dropdown)
- **Grid**: Günlük operasyon listesi, slot/sefer sayısı/durum inline edit
- **Yeni kayıt**: Inline form — araç/şoför autocomplete, slot toggle (Sabah/Akşam/Mesai)
- **Toplu kaydet**: Çakışma kontrolü + `TopluSaveAsync`
- **Dirty tracking**: Değişen satırlar yeşil vurgulu, "Tümünü Kaydet" butonu

### 📊 Toplam: 2 yeni dosya

### 🧪 Smoke Test

| Test | Sonuç |
|------|:-----:|
| `dotnet build` | ✅ **0 hata** |
| Route: `/operasyon-giris` | ✅ Sayfa hazır |

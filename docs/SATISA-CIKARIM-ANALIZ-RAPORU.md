# MKFiloServis — Satışa Çıkarım Öncesi Analiz ve Düzeltme Raporu

**Tarih:** 2026-10-01
**Kapsam:** Tüm çözüm (Web, Shared, DataSync, LisansDesktop, Client, Testler, CI/CD, Docker)
**Yöntem:** Statik kod analizi, yapılandırma incelemesi, dosya/konum bazlı doğrulama

---

## 1. Yönetici Özeti

Proje işlevsel açıdan zengin ve modüler bir filo-ERP çözümü; ancak **satışa çıkarım öncesi kapatılması zorunlu olan 6 kritik**, **yüksek öncelikli 11**, **orta öncelikli 14** ve **düşük öncelikli 8** konu tespit edilmiştir.

**En kritik 3 sorun:**

| # | Sorun | Etki |
|---|---|---|
| 1 | Lisans imzalama anahtarı kaynak koda gömülü ve dağıtım aracında açık | Her müşteri sınırsız lisans üretebilir — gelir modelini çökertir |
| 2 | ~192 sayfa `[Authorize]` olmadan, yedek yetki politikası yok | Giriş yapmadan doğrudan URL ile tüm sayfalara erişim |
| 3 | `DefaultScheme = "Cookies"` tanımlı ama `AddCookie()` hiç çağrılmamış | Kimlik doğrulama yolunda çalışma zamanı hatası / beklenmeyen davranış |

**Genel değerlendirme:** Mimari ve modülerlik olarak sağlam, ancak **güvenlik katmanı ürün seviyesine çıkarılmamış** ve **test altyapısı pratikte yok**. Tavsiye: Bu 6 kritik madde kapanmadan müşteriye kurulum/demo yapılmamalı.

---

## 2. Proje Profili

| Proje | Gerçek Durum | Hedef FW | Boyut |
|---|---|---|---|
| `MKFiloServis.Web` | Faal — Blazor Server, ~255 sayfa, ~173 servis, ~97 arayüz, 288 EF migration | net10.0 | ~106k satır |
| `MKFiloServis.Shared` | Faal — ~200 entity, `BaseEntity` + soft delete | net10.0 | ~14k satır |
| `MKFiloServis.DataSync` | Faal — SQLite ↔ PostgreSQL toplu tablo kopyalayıcı | net10.0-windows | 936 satır |
| `MKFiloServis.LisansDesktop` | Faal — WinForms lisans üretim aracı | net8.0-windows | 1.685 satır |
| `MKFiloServis.Client` | Faal — MAUI WebView2 ince istemci | net10.0-android | ~400 satır |
| `MKFiloServis.Infrastructure` | **BOŞ** — `ApplicationDbContext.cs` 0 bayt, `.csproj` yok | — | 0 |
| `MKFiloServis.Service` | **YOK** — README'de belgelenmiş ama diskte yok | — | — |
| `MKFiloServis.Tests` | **YOK** — CI workflow bu projeyi referanslıyor | — | — |

> **Tespit:** `MKFiloServis.slnx:1-9` yalnızca 6 proje içeriyor. `README.md:68-69` ise `Infrastructure` ve `Service` projelerini belgeliyor. **README gerçek durumu yansıtmıyor.** `Infrastructure` klasörü silinmeli veya doldurulmalı; `Service` dokümandan çıkarılmalı.

### Mimari Özeti
- **Çoklu DB:** `DatabaseRuntimeResolver` (`Services/DatabaseRuntimeResolver.cs:24-70`) sağlayıcıyı belirliyor; `Program.cs:104-178` içinde 4 sağlayıcı (SQLite/SQL Server/MySQL/PostgreSQL) kayıt ediliyor. **Ancak `CanonicalProvider` PostgreSQL'e sabitlenmiş** (`:43`, `:81`) ve migration'lar yalnızca Npgsql ile üretilmiş. SQL Server/MySQL için migration yolu **yok**.
- **Çoklu müşterilik (tenant):** `ApplicationDbContext.ApplyFirmaTenantQueryFilter` (`:3231-3283`) reflection ile tüm `IFirmaTenant` entity'lere filtre ekliyor. `FirmaTenantDisabled` yalnızca "TumFirmalar" yetkisi varsa true (`:33-43`). **Fail-closed tasarım — doğru.**
- **Arka plan işleri:** Quartz, 10 job (`Program.cs:444-531`).
- **Dosya güvenliği:** AES-GCM şifreli depolama (`SecureFileService.cs:9-59`), anahtar dosya sistemi, anahtar rotasyonu (`FileRecoveryService.cs:204-246`).

---

## 3. KRİTİK SORUNLAR (Satışa Çıkarımı Engeller)

### K-1. Lisans İmzalama Anahtarı Kaynak Kodda Gömülü — LİSANS SİSTEMİ TÜMÜYLE KORUMASIZ
**Konum:**
- `MKFiloServis.LisansDesktop/MainForm.cs:15` → `const string SECRET = "[MASKELENDI]"`
- `MKFiloServis.Web/Services/LicenseService.cs:35` → **aynı değer, birebir aynı**

**Nasıl çalışıyor:**
İmza şeması (`LicenseService.cs:132`):
```
raw = "{firmaKodu}|{machineId}|{expire}|{durationDays}|{isDemo}|{allowedVersion}|{created}|{phone}|{SecretKey}"
imza = SHA256(raw) → Base64 → JSON → Base64 lisans anahtarı
```

**Sorun:** Anahtar, lisans üretim aracının (`LisansDesktop`) **paketlenmiş binary'sinin içinde düz metin olarak** bulunuyor. Herhangi bir müşteri:
1. `MainForm.cs` içeriğini illegal olarak okur (veya ILSpy/string araması yapar),
2. `LicenseService.cs:132` şemasını kopyalar,
3. İstediği firma kodu + makine kodu + sınırsız süre ile **kendi lisansını üretir**.

`LicenseService.cs:143` doğrulaması düz string karşılaştırması. `AllowedVersion` ayrıca `"1.0.99"`'a sabitlenmiş (`MainForm.cs:16`) — sürüm kısıtı da dolanılabilir.

**Ayrıca zayıf noktalar:**
- `LicenseService.cs:188-194`: hash dosyası yoksa **kendini kendine onarır** (fail-open)
- `LicenseService.cs:209-213`: disk okuma hatasında **doğrulama `true` dönüyor** (fail-open)
- `LicenseService.cs:143`: `==` ile karşılaştırma — timing attack'e açık, `CryptographicOperations.FixedTimeEquals` kullanılmalı
- **Çevrimdışı tolerans (grace period) yok** — lisans bitişinde uygulama açılmıyor. Saha tarafında "lisans bitti, yazıcı çalışmıyor" şeklinde geri dönüşsüz destek talebi üretir.
- `LisansAesKey = "[MASKELENDI]"` (`LicenseService.cs:920`) — lisans dosyası şifreleme anahtarı da gömülü

**Düzeltme:**
1. **Asimetrik imzaya geç (RSA/ECDSA).** Gizli anahtar (private key) sadece imzalayan tarafta (LisansDesktop, şirket içinde) tutulmalı; uygulama yalnızca **public key** ile doğrulamalı. Public key güvenle gömülebilir.
2. `LicenseService.cs:188-213` fail-open durumlarını **fail-closed** yap (doğrulanamıyorsa `false`).
3. `==` yerine `CryptographicOperations.FixedTimeEquals`.
4. Lisans dosyası şifrelemesini makineye bağlı anahtara (DPAPI/DPAPI-NG) taşı.
5. **Grace period ekle** (örn. 14 gün) ve saat manipülasyonuna karşı son başarılı çalışma zamanını sunucu/veri tabanında tut.
6. Lisans üretim aracını **müşteriye teslim etmeyin** — yalnızca şirket içinde kullanılmalı, dağıtım paketinden çıkarılmalı.
7. **Acil:** Mevcut sürüm satışa çıkmadan önce anahtar rotasyonu yapılmalı; şu ana kadar üretilmiş tüm anahtarlar geçersiz sayılmalı.

---

### K-2. Yetkisiz Erişim — 255 Sayfanın 192'sinde `[Authorize]` Yok
**Konum:** `Components/Pages/**` — 255 `.razor` dosyası, yalnızca **63'ünde** `@attribute [Authorize]` var.

**Neden kritik:** Yetkilendirme kontrolü **sadece UI tarafında** yapılıyor:
- `Components/Layout/NavMenu.razor:1339-1370` → `HasYetki` / `HasAnyYetki` / `HasMenuYetki`
- `Components/Pages/Home.razor:1358` → aynı kontrol, **kopya kod**

Menüde link görünmemesi ≠ erişim engeli. Kullanıcı `https://sunucu/firmalar`, `https://sunucu/ayarlar/veritabani` gibi URL'leri doğrudan yazarsa sayfa render edilir. `Program.cs:210` içinde `AddAuthorizationCore()` çağrılıyor ancak **fallback policy tanımlı değil**.

**Risk altındaki sayfalar (örnekler):** `Pages/Admin/*`, `Pages/Ayarlar/VeritabaniAyarlari.razor`, `Pages/Cari/*`, `Pages/Personel/*`, tüm finans sayfaları.

**Düzeltme:**
1. `Program.cs` içinde **fallback policy** tanımla:
```csharp
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser().Build();
```
2. Giriş/çıkış gibi istisna sayfalara `[AllowAnonymous]` ekle.
3. Yetki kontrollerini sayfaya taşı: yetki gerektiren her sayfaya policy bazlı `[Authorize(Policy = "Yetki.X")]` ekle.
4. `NavMenu.razor` ve `Home.razor` içindeki kopya yetki kodunu merkezi bir `YetkiCozumleyici` servisine taşı.

---

### K-3. `DefaultScheme = "Cookies"` Tanımlı, `AddCookie()` Hiç Çağrılmamış
**Konum:** `Program.cs:557`
```csharp
options.DefaultScheme = "Cookies";
options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
```
Çözüm genelinde `AddCookie(...)` çağrısı **sıfır**. Doğrulandı.

**Sonuç:** `HttpContext.User`, `[Authorize]` attribute'ı, Blazor `AuthorizeRouteView` gibi herhangi bir yol `Cookies` şemasını tetiklerse:
```
InvalidOperationException: Scheme 'Cookies' was not found.
```
`DefaultChallengeScheme` JWT'ye çevrildiği için API tarafı çalışır; **Blazor/cookie tarafı patlar.** Bazı akışlarda (Blazor `AuthorizeRouteView`, `SignInAsync`, `[Authorize]` sayfalarının ilk render'ı) çalışma zamanı hatası olarak yüzeye çıkar.

**Düzeltme:** Seçeneklerden birini netleştir:
- **A)** Kullanılacaksa: `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)` ile gerçek cookie handler kaydet.
- **B)** Kullanılmayacaksa (mevcut Blazor oturum yaklaşımı korunacaksa): `DefaultScheme`'i kaldır, yalnızca JWT scheme tanımla.

**Öneri:** Mevcut mimari (JWT + `ProtectedSessionStorage`) korunacaksa **B** daha uygundur; ancak `AuthController` üretimi JWT'ye gidiyorsa ikisinin birlikte çalışması kararı önce netleştirilmeli.

---

### K-4. Üretim Ortamında JWT Anahtarı Kaynak Kodda
**Konum:** `MKFiloServis.Web/appsettings.json:8`
```json
"Secret": "[MASKELENDI]"
```
`appsettings.PreProduction.json:10` içinde de gerçek görünümlü bir PreProd sırrı var.

**Düzeltme:**
1. `appsettings.json` içindeki sırrı boşalt (`""`) veya yalnızca geliştirme placeholder'ı bırak.
2. Sırrı `Jwt__Secret` ortam değişkeni veya Azure Key Vault / ortam sırları deposu üzerinden sağla.
3. **Bu anahtarla üretilmiş tüm JWT'leri geçersiz kıl** (anahtarı değiştir).
4. Git geçmişinden temizle (git-filter-repo / BFG ile) veya en azından anahtarı kalıcı olarak rotate et.

---

### K-5. Varsayılan Yönetici Şifresi `[MASKELENDI]` — Kaynak Kodda Sabit
**Konum:**
- `MKFiloServis.Web/Data/DbSeeder.cs:58` → `SifreHash = "[MASKELENDI]"`
- `MKFiloServis.Web/Services/KullaniciService.cs:677` → admin `[MASKELENDI]` ile seed ediliyor
- `MKFiloServis.Web/Tests/PlaywrightSmoke/Program.cs:12` → test varsayılan şifresi `[MASKELENDI]`

**Düzeltme:**
1. Seed şifresini ortam değişkeninden al (`INITIAL_ADMIN_PASSWORD`), yoksa kurulumda zorlu değiştirme uyarısı göster ve **ilk girişte değişiklik zorunlu** kıl (zaten zorunluysa doğrula).
2. `KullaniciService.cs:677` içindeki **ikinci** seed noktasını kaldır — seed mantığı tek yerde olmalı.
3. Sıfırlamayı otomatik yapma; `DbSeeder` yalnızca kullanıcı yoksa çalışsın.

---

### K-6. Parola Politikası Kuralsız + Eski SHA256 Doğrulama Yolu Açık
**Konum:**
- `Program.cs:201-205` → parola politikası: **rakam/harf büyüklüğü/özel karakter zorunluluğu yok**, minimum uzunluk **6**, kilit 5 deneme
- `MKFiloServis.Web/Services/.../KullaniciPasswordHasher.cs:25-31` → legacy SHA256 + salt doğrulama hala aktif

**Düzeltme:**
1. Minimum uzunluğu 8'e (tercihen 10-12) çıkar.
2. Rakam, büyük/küçük harf ve özel karakter gereksinimleri ekle.
3. Legacy SHA256 yolunu **kaldır**; şifreler `PasswordHasher<T>` ile yeniden hash'lensin ve kullanıcı ilk girişte upgrade edilsin.
4. Kilit sayısını ve kilit süresini (5 dk) ayarla.
5. 2FA altyapısı zaten mevcut (`KullaniciService.cs:231-290`) — **yönetici ve muhasebe rollerine zorunlu** yap.

---

## 4. YÜKSEK ÖNCELİKLİ SORUNLAR

### Y-1. 40 Adet Boş `catch` Bloğu — Hatalar Sessizce Yutuluyor
En kötü dosyalar:
- `Services/PlaywrightScraperService.cs` — **17 adet** (`:62, :270, :313, :323, :337, :347, :430, :487, :506, :582, :640, :647, :689, :754, :851`)
- `Services/SeleniumScraperService.cs` — **14 adet** (`:76, :265, :275, :298, :308, :432, :478, :732, :808, :837, :868, :968, :985, :998`)
- `Services/HttpScraperService.cs` (`:708, :800, :953`)
- `Services/KolayMuhasebeService.cs` (`:745, :815, :908`) ← **muhasebe modülü**
- `Services/FaturaSablonService.cs` (`:969, :1017`) ← **fatura modülü**
- `Services/WebhookService.cs` (`:304, :323`) ← **dış sistem entegrasyonu**
- `Services/BakimPeriyotService.cs:198`

Muhasebe ve fatura yollarındaki yutulan hatalar **veri bütünlüğü riski** doğurur. `LicenseService.cs:66-68, :108-111, :116-118` ise doğrulama yerine `false`/`"UNKNOWN"` dönerek sessizce başarısız oluyor.

**Düzeltme:** Boş catch'leri `ILogger` ile `LogWarning`/`LogError` seviyesine bağla. Muhasebe/fatura akışlarındakiler **mutlaka** loglanmalı.

### Y-2. `NotImplementedException` — Genel Kullanıma Açık Yolda
- `Services/ProformaFaturaService.cs:536` → `throw new NotImplementedException("PDF export henüz implemente edilmedi.")`
- `Services/LucaPortalService.cs:899` → `return null; // TODO: Implementasyon`

**Düzeltme:** Ya implement et ya da özelliği feature flag arkasına alıp UI'dan gizle.

### Y-3. Luca Portal Parolası Düz Metin Saklanıyor
**Konum:** `Services/LucaPortalService.cs:211`
```csharp
ayarlar.Sifre = sifre; // Sifreyi sifrelenmis olarak sakla (TODO: Encryption)
```
**Düzeltme:** DPAPI veya AES-GCM ile şifrele. Düz metin saklama.

### Y-4. Denetim İzinde Sabit Kullanıcı ID
**Konum:** `Services/EbysEvrakService.cs:326` → `AtayanKullaniciId = 1` (sabit)
**Etki:** EBYS evrak işlemlerinin kim tarafından yaptığı bilgisi yanlış kaydedilir. **Yasal denetim izi bozulur.**

**Düzeltme:** `HttpContext.User` üzerinden gerçek kullanıcıyı al (Blazor'da `AuthenticationStateProvider`).

### Y-5. Swagger Tüm Ortamlarda Açık (Production dahil)
**Konum:** `Program.cs:1197-1213`
**Düzeltme:** `if (app.Environment.IsDevelopment())` ile sınırla.

### Y-6. Sahte "Yedek Policy Yokluğu" — `PendingModelChangesWarning` Susturulmuş
**Konum:** `Program.cs:171-176` → 3 EF uyarısı bastırılıyor, bunlardan biri `PendingModelChangesWarning`
**Etki:** Model ile migration arasındaki **gerçek** tutarsızlıklar görünmez hale gelir. Üretimde şema hatası olarak patlar.

**Düzeltme:** Susturmayı kaldır, gerçek model-migration farklarını gider. 288 migration birikmiş — production veritabanına uygulama öncesi **tam yedek + geri dönüş planı** zorunlu.

### Y-7. CI Test İş Akışı Kırık — Test Projesi Yok
**Konum:** `.github/workflows/tests.yml:63, :67, :74`
```yaml
run: dotnet restore MKFiloServis.Tests/MKFiloServis.Tests.csproj
```
`MKFiloServis.Tests` **projesi diskte yok** (`Test-Path` → False). Yorum satırı "xUnit + Coverlet" diyor ama projede **hiçbir test framework'ü yok** (xunit/NUnit/MSTest/Moq/FluentAssertions → 0 eşleşme, `[Fact]`/`[Test]` → 0).

Mevcut "testler" console uygulaması:
- `Tests/RentACar/Program.cs` (300 satır) — ~40 elle yazılmış assertion, gerçek EF/SQLite
- `Tests/PlaywrightSmoke/Program.cs` (989 satır) — ~24 senaryo, uygulamayı kendi barındırıyor

**Düzeltme:**
1. `MKFiloServis.Tests` projesini xUnit ile oluştur, mevcut assertion'ları teste çevir.
2. Workflow'da `MKFiloServis.slnx` restore/build etsin.
3. `codecov.yml:37-49` `Tests/**` klasörünü ignore ediyor — yeni proje için düzeltilmeli. Ayrıca `patch` hedefi `%70` ama `informational: true` olduğu için **asla başarısız olmuyor**.

### Y-8. Sync-over-Async — Ölümkül Kilit Riski
- `Services/ArchiveMigrationService.cs:172, :284` → `.ExistsAsync(...).Result`
- `Services/LisansService.cs:206` → `GetMakineKoduAsync().Result`
- `Services/PlaywrightScraperService.cs:59` → `.GetAwaiter().GetResult()`
- `MKFiloServis.DataSync/Program.cs:15`

**Düzeltme:** Hepsi `await`'a çevrilmeli. Özellikle `LisansService` lisans kontrolünde — Blazor devre thread'i ile kilitlenme riski.

### Y-9. Yazılım/Sağlayıcı Gizlilik İhlali — Luca/EBYS Dış Servislerine Düz Bağlantı
`EfaturaXmlService`, `LucaPortalService`, `EbysService`, `HareketImportService` dış sistemlere bağlanıyor. `Program.cs:295-300` Ollama proxy'si devre dışı bırakılmış durumda.

**Düzeltme:** Tüm dış bağlantılar için TLS zorunluluğu, timeout ve retry (Polly — paket mevcut, kullanımı doğrulanmalı) uygulanmalı. Müşteri verisi içeren tüm entegrasyonlar KVKK kapsamında değerlendirilmeli.

### Y-10. `Npgsql.EnableLegacyTimestampBehavior` Global Olarak Açık
**Konum:** `Program.cs:46`
**Etki:** PostgreSQL `timestamp without time zone` davranışı — saat dilimi karışıklığı ve **hakediş/maaş hesaplarında tutarsızlık** riski. Türkiye saati (Europe/Istanbul) için ciddi sonuç doğurabilir.

**Düzeltme:** Kaldır, `timestamptz` kullan.

### Y-11. NuGet Güvenlik Uyarısı Bastırılmış
**Konum:** `MKFiloServis.Web/MKFiloServis.Web.csproj:90` ve `DataSync.csproj:28` → `NuGetAuditSuppress` ile `GHSA-2m69-gcr7-jv3q` bastırılmış
**Düzeltme:** Paketi yükselt veya bastırmayı kaldır — CI'daki `nuget-audit.yml` zaten var, bastırma onu işlevsiz kılıyor.

---

## 5. ORTA ÖNCELİKLİ SORUNLAR

| # | Sorun | Konum |
|---|---|---|
| O-1 | **SQL Server ve MySQL için migration yolu yok.** Sağlayıcılar DI'da kayıtlı ama şema oluşturma/upgrade mekanizması yok. `CanonicalProvider` PostgreSQL'e sabit. | `DatabaseRuntimeResolver.cs:43, :81` |
| O-2 | **Arka plan işleri/servisler firma 1'e yazıyor.** Tenant sağlayıcı yoksa `FirmaId = 1` atanıyor — çoklu müşterilikte yanlış firmaya veri yazar. | `ApplicationDbContext.cs:3316` |
| O-3 | **Denetim kaydı eksik.** Yeni eklenen entity'lerde `AtayanKullaniciId` doldurulmuyor; DB hatasında denetim kaydı sessizce atlanıyor. | `ApplicationDbContext.cs:3343-3348, :3415-3419` |
| O-4 | **Başlatma seed/migration bloklarında geniş `catch`** — 25 ardışık blok (`Program.cs:680-1185`), hatalar yutuluyor. DB kurulumu yarım kalabilir, fark edilmez. | `Program.cs:691-696, :1178-1181` |
| O-5 | **Lisans eşleşmemesi sessizce "varsayılan firma"ya bağlanıyor** — yalnızca uyarı. Çoklu müşteride veri karışması. | `LicenseService.cs:641-684` |
| O-6 | **`DatabaseBackupService` geri yükleme uygulanmamış** (TODO). Yedek alınıyor ama geri yüklenemiyorsa felaket kurtarma senaryosu test edilemez. | `DatabaseBackupService.cs:387` |
| O-7 | **DataSync şema taşımıyor.** Hedef tablonun önceden var olması gerekiyor; içe aktarma yorumu bunu doğruluyor. Ayrıca `session_replication_role = replica` ile FK devre dışı bırakılıyor → bütünlük garantisi yok. | `PostgresToSqliteExporter.cs:33-38`, `SqliteToPostgresImporter.cs:70` |
| O-8 | **Kod tekrarı:** lisans normalizasyonu 2 yerde, imza formatı 2 yerde birebir kopyalanmış (satır 131'deki yorum bağımlılığı kabul ediyor), tablo-introspeksiyon SQL'i 2 yerde, DB seed 2 yerde. | `MainForm.cs:802-817, :826` / `LicenseService.cs:132` / `DbSeeder.cs` / `KullaniciService.cs` |
| O-9 | **Ölü kod:** `MasterDbContext.cs` ve `HoldingDbContext.cs` `[Obsolete]`, hiçbir yerde kayıtlı değil. | `Data/MasterDbContext.cs:11`, `Data/HoldingDbContext.cs:10` |
| O-10 | **README yanlış** — `Infrastructure` ve `Service` projelerini belgeliyor, diskte yok. Ayrıca test komutu (`dotnet run --project ...`) yanlış formatta. | `README.md:68-69, :123` |
| O-11 | **Docker varsayılan sırları zayıf:** `[MASKELENDI]`, `local-dev-secret-...` — `.env` zorunlu değil. | `docker-compose.yml:33, :35, :67` |
| O-12 | **`DataSync` yardım metninde gerçek görünümlü parola:** `Password=[MASKELENDI]` | `DataSync/CliRunner.cs:101` |
| O-13 | **`bin/` ve `obj/` dosyaları Git'e commit edilmiş** (LisansDesktop, DataSync, iki test projesi). Depo şişer, gizli dosya riski artar. | `.gitignore` kontrol edilmeli |
| O-14 | **Sürüm sabitlemesi** `AllowedVersion = "1.0.99"` — her sürümde güncellemek gerekir, otomatik değil. | `MainForm.cs:16` |

---

## 6. DÜŞÜK ÖNCELİKLİ / İYİLEŞTİRME

| # | Konu | Not |
|---|---|---|
| D-1 | PWA offline desteklenmiyor | README'de belirtiliyor; kurulan uygulama sunucuya bağlı. Ürün vaadiyle uyumlu ama mobilde ağ kesintisi riski |
| D-2 | `IKopyalanabilirTenant` arayüzü tanımlı, kullanımı belirsiz | Shared'de yalnızca tanım |
| D-3 | Shared katmanında DTO doğrulama neredeyse yok | Doğrulama Web'e dağılmış durumda (`RentACar/IRentACarRezervasyonServisi.cs:6-84` gibi) |
| D-4 | Blazor render mode tekrarları | `Components/App.razor:26, :33` — tüm uygulama tek sunucu devresi; alt bileşenlerdeki `@rendermode` dekoratif |
| D-5 | Veri koruma anahtarları dosya sisteminde | `Program.cs:212-220` — sunucu/veri kaybında tüm şifreli belgeler okunamaz hale gelir |
| D-6 | S3/local nesne depolama ikilisi | Hangisinin kullanılacağı yapılandırmaya bağlı; belgelerde netleştirilmemiş |
| D-7 | `_tmpbuild/`, `artifacts/`, `temp/` klasörleri repoda | `Directory.Build.props:16` bunları dışladığı için derlemeyi etkiliyor; temizlenmeli |
| D-8 | Kök dizinde 4 ham log dosyası | `build.log` (1 KB), `build2.log` (33 KB), `client-build.log` (107 KB), `setup-build.log` (378 KB), `test_all.txt` (41 KB) — repodan çıkarılmalı |

---

## 7. Satışa Çıkarım Kontrol LİSTESİ

### AŞAMA 1 — ZORUNLU (Bloklayıcı, müşteriye kurulum yapılmaz)
- [ ] **K-1** Lisans sistemini asimetrik imzaya (RSA/ECDSA) geçir, anahtarı dağıtımdan kaldır
- [ ] **K-2** Fallback authorization policy ekle, 192 sayfayı yetkilendir
- [ ] **K-3** `DefaultScheme`/cookie karmaşasını çöz
- [ ] **K-4** JWT sırrını ortam değişkenine taşı, anahtarı rotate et, git geçmişinden temizle
- [ ] **K-5** Varsayılan admin şifresini kaldır, ilk girişte değişiklik zorunlu kıl
- [ ] **K-6** Parola politikasını güçlendir, legacy SHA256 yolunu kaldır

### AŞAMA 2 — YÜKSEK (kurulum öncesi tamamlanmalı)
- [ ] **Y-7** Gerçek test projesi kur (xUnit), CI iş akışını düzelt
- [ ] **Y-1** Boş catch'leri logla (muhasebe/fatura/webhook öncelikli)
- [ ] **Y-3** Luca parolasını şifrele
- [ ] **Y-4** Denetim izindeki sabit kullanıcı ID'yi kaldır
- [ ] **Y-5** Swagger'ı üretimde kapat
- [ ] **Y-6** EF uyarı bastırmasını kaldır, model-migration farklarını gider
- [ ] **Y-9** Dış servis bağlantılarında TLS + timeout + retry
- [ ] **Y-2, Y-8, Y-10, Y-11** Diğer yüksek öncelikli maddeler

### AŞAMA 3 — ORTA (ilk sürüm sonrası 2-4 hafta)
- [ ] **O-1** SQL Server/MySQL migration stratejisi belirle veya bu sağlayıcıları kaldır
- [ ] **O-2** Arka plan işlerinde tenant bağlamı (firma 1 varsayılanı kaldır)
- [ ] **O-3** Denetim kaydı eksikliğini gider
- [ ] **O-6** Yedek **geri yükleme** yolunu uygula ve **uçtan uca test et**
- [ ] **O-10** README'yi gerçek yapıyla güncelle
- [ ] **O-13** `bin/`, `obj/`, log dosyalarını Git'ten çıkar

### AŞAMA 4 — TESLİMAT ÖNCESİ DOĞRULAMA
- [ ] Üretim benzeri ortamda **tam yedek al**, geri yükleme provası yap
- [ ] **288 EF migration**'ını boş veritabanına baştan sona uygula
- [ ] Performans testi: 2 firmalı, 10 kullanıcılı eşzamanlı senaryo
- [ ] Lisans akışının uçtan uca testi (üretim → imzala → doğrula → süre bitişi)
- [ ] Çoklu firma izolasyonu testi (K-2 düzeltmesi sonrası)
- [ ] KVKK / veri saklama politikası gözden geçirilmesi
- [ ] Rent a Car belge çıktıları hukuk danışmanı onayı (README:36 uyarısı)
- [ ] **LisansDesktop aracı teslim paketinden çıkarılıyor mu?** (K-1)
- [ ] `SECURITY.md` ve geri dönüş planı müşteriye sunuldu mu?

---

## 8. Önerilen Çalışma Sırası

```
1. Lisans (K-1)      → 2-3 gün   ·  En yüksek ticari risk
2. Yetkilendirme(K-2)+ K-3      → 2 gün  · En yüksek güvenlik riski
3. Sırlar + parola (K-4/5/6)     → 1 gün   · Hızlı kazanım
4. Test projesi (Y-7)            → 3-5 gün · Sürdürülebilirlik
5. Yüksek öncelikliler (Y-1..11) → 1 hafta · Kalite
6. Orta öncelikliler (O-1..14)   → 2 hafta · Sağlamlaştırma
                                         ─────────────
                                  ~5 hafta
```

---

## 9. Toparlanan Tablo

| Öncelik | Adet |
|---|---|
| Kritik | 6 |
| Yüksek | 11 |
| Orta | 14 |
| Düşük | 8 |
| **Toplam** | **39** |

---

*Bu rapor statik kod analizi ve yapılandırma incelemesine dayanır. Belirtilen konumların tamamı kaynak kodda doğrulanmıştır. Aşama 4 doğrulama adımları (yüksek yüklü test, geri yükleme provası, migration uygulaması) statik analizle doğrulanamaz ve canlı ortamda yapılmalıdır.*
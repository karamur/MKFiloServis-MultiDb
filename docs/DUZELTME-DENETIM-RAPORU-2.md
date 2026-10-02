# MKFiloServis — İkinci Düzeltme Denetim Raporu

**Denetim tarihi:** 2026-10-02 (önceki denetimin ~2 saat sonrası)
**Önceki denetim:** [DUZELTME-DENETIM-RAPORU.md](DUZELTME-DENETIM-RAPORU.md)
**Kapsam:** İlk denetimden sonra yapılan tüm değişiklikler (27 dosya, +297 / −828 satır)
**Yöntem:** Git diff incelemesi, satır bazlı doğrulama, **canlı `dotnet build` çalıştırması**

---

## 1. Yönetici Özeti

**Ciddi ilerleme var.** Önceki denetimde 39 maddenin 1'i kapanmışken, bu denetimde **6 madde tamamen, 3 madde kısmen kapatıldı.** Güvenlik açığı olan dosyalara odaklanılmış.

| Durum | Önceki | Şimdi | Değişim |
|---|---:|---:|---|
| ✅ Tamamen düzeltildi | 1 | **6** | +5 |
| 🟡 Kısmen düzeltildi | 5 | **8** | +3 |
| ⚪ Değişiklik yok | 22 | **21** | −1 |
| ⬜ Yalnızca planlanan | 8 | 8 | — |
| 🔴 Açılan yeni risk | 1 | **1** | — |

**Derleme durumu:** ✅ **`dotnet build` başarılı — 0 hata, 2 uyarı** (2 uyarı da `LisansDesktop/MainForm.cs` içinde kullanılmayan alan, ilgisiz).

**Hâlâ dokunulmamış en kritik madde: K-1 (lisans imza anahtarı).** Bu, raporun ilk günden beri bir numaralı riski ve ürünün gelir modelini doğrudan tehdit ediyor. Diğer tüm güvenlik düzeltmeleri yapılmasına rağmen bu madde hâlâ açık.

---

## 2. Bu Turda Yapılan İşler

### 2.1 Kapatılan maddeler

#### ✅ K-2 — Yetkisiz erişim (Blazor tarafı) — **ÇÖZÜLDÜ**
**Nasıl:** `Components/_Imports.razor:10` → `@attribute [Authorize]`

Bu, Pages ağacındaki **tüm bileşenlere** tek satırla yetki zorunluluğu getiriyor. Daha önce 283 `@page` yönlendirmesinden yalnızca 63'ü korumalıydı; artık 6 açık istisna dışında hepsi korumalı:
- `Login.razor:3`, `Portal/Landing.razor:3`, `Setup/SetupWizard.razor:3`, `Setup/LisansSetup.razor:3`, `Error.razor:3`, `NotFound.razor:2`

15 sayfa ek olarak `Roles="Admin"` veya çoklu rol şartı kullanıyor (`Admin/Denetim.razor:9`, `AdminSystemHealth.razor:2`).

**API tarafı da korundu:** `AuthController.cs:16` sınıf seviyesinde `[Authorize]`, `HealthController.cs:10`, `SystemHealthController.cs:15` (`Roles="Admin"`), `FaturalarController`, `FaturaGrupSablonuController`, `AraclarController`, `CarilerController`, `SoforlerController`, `AnalitikController` — hepsi `[Authorize(AuthenticationSchemes="Bearer")]`. `Hubs/EvrakHub.cs:6` da korundu.

**Not:** `GuzergahlarController` sınıf seviyesinde değil, 6 action'ın tek tek hepsi `[Authorize]` alıyor. Şu an eşdeğer ama yeni action eklenirse varsayılan korumasız olur — tutarsızlık, düzeltilmeli.

#### ✅ K-3 — Kimlik doğrulama şeması — **ÇÖZÜLDÜ**
`Program.cs:571-576`:
```csharp
options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
```
Kayıtsız `"Cookies"` şeması kaldırıldı. Artık `Cookies` handler'ı olmayan bir duruma atıflar.

**Blazor için güvenli olduğu doğrulandı:** `AppAuthenticationStateProvider.cs:16-45` kullanıcıyı yalnızca bellekte tutuyor, `GetAuthenticationStateAsync()` doğrudan döndürüyor; oturum geri yükleme `ProtectedSessionStorage` üzerinden. Yani Blazor tarafı scheme'e bağlı değil, JWT-only varsayılan ona dokunmuyor.

#### ✅ K-4 — Sırlar kaynak koddan çıkarıldı — **ÇÖZÜLDÜ**
| Dosya | Değişiklik |
|---|---|
| `appsettings.json:8` | `"Secret": ""` |
| `appsettings.PreProduction.json:10` | `"Secret": ""` |
| `appsettings.Production.json:8` | `"Secret": ""` |
| `appsettings.*.json` | `DataProtection:SecretKey`, `Redis:Password`, `Mail:Password` → boş |
| `.env.example` | Tüm sırlar placeholder (`JWT_SECRET=`, `POSTGRES_PASSWORD=`) |
| `docker-compose.yml:33-38` | `${JWT_SECRET:?Set_JWT_SECRET_in_.env}` — **zorunlu interpolasyon**, compose başlamayı reddediyor |

`ChangeMe!2025` ve `local-dev-secret-...` artık repo genelinde **sıfır eşleşme**.

**Ek olarak iki akıllı önlem eklendi (`Program.cs:549-568`):**
1. Development ortamında process ömrüne özel rastgele anahtar üretiliyor — geliştiricide bile kalıcı anahtar kalmıyor
2. Sızıntı olmuş eski anahtarın **parmak izi karşılaştırması** yapılıyor: `F96583977D82...` → başlatma reddediliyor. Anahtar döndürme unutulsa bile sızıntılı anahtarın kullanılması engelleniyor.

`setup/build.ps1:117-118` müşteri paketinden JWT sırrını açıkça hariç tutuyor.

#### ✅ K-5 — Varsayılan admin parolası — **DOĞRULANDI VE GENİŞLETİLDİ**
Önceki turda seed blokları silinmişti. Bu turda kalan **son boşluk kapatıldı:**
- `KullaniciService.cs:677-703` → `SeedAdminAsync` artık `INITIAL_ADMIN_USERNAME` / `INITIAL_ADMIN_PASSWORD` / `INITIAL_ADMIN_NAME` ortam değişkenlerinden okuyor
- Eksikse `LogCritical` atıp çıkıyor (`:679-683`)
- Kullanıcı adı > 50 veya şifre < 12 ise reddediyor (`:685-689`)

**Kod tabanında artık hiçbir yerde `admin123`/`test123` yok** (yalnız `PlaywrightSmoke/Program.cs` env fallback — test aracı, kaynak değil).

#### ✅ R-1 — Kurulum sihirbazı kilitlenme riski — **ÇÖZÜLDÜ**
Bu, önceki denetimimde raporladığım yeni riskti. Tam kapatılmış:

| Sorun | Düzeltme |
|---|---|
| `Firmalar.AnyAsync()` kontrolü yanlış tablodaydı | `Kullanicilar.AnyAsync()` oldu (`:447`) — doğru tablo |
| `catch { }` DB hatasını yutuyordu | `LogError` + **fail-closed** (`:453-457`) — kurulum kapanıyor |
| `/setup` herkese açıktı | `MKFILOSERVIS_SETUP_BOOTSTRAP_TOKEN` şartı (≥32 bayt, `:436-441`) |
| Token karşılaştırması normal `==` idi | `CryptographicOperations.FixedTimeEquals` (`:469-471`) |
| Şifre kontrolü 6 karakter | 12 karakter (`:502`, `Program.cs:205` ile tutarlı) |
| Yarım kalan kurulumda veri tutarsızlığı | `Serializable` transaction + rollback (`:509-519`, `:582-586`) |

**Bu turun en değerli güvenlik işi.** Önceki denetimde "sistem kilitlenir" diye raporladığım senaryo artık mümkün değil.

#### ✅ O-11 — Docker zayıf varsayılan sırları — **ÇÖZÜLDÜ**
`docker-compose.yml`: 13 satır değişiklik. Tüm sırlar zorunlu interpolasyona çevrildi:
```yaml
Jwt__Secret: "${JWT_SECRET:?Set_JWT_SECRET_in_.env}"
```
`.env` dosyası yoksa **docker compose başlamayı reddediyor** — sessizce zayıf anahtarla çalışma riski kalmadı.

#### ✅ O-13 — Git'te izlenen dosyalar — **TEMİZ (doğrulandı)**
`git ls-files`: bin/obj izlenmiyor, log dosyaları izlenmiyor. **Yalnız `test_all.txt` izleniyor** — bu da silinebilir.

#### ✅ Y-7 (kısmen) — Test aracı sertleştirildi
`Tests/PlaywrightSmoke/Program.cs`: `admin123` fallback kaldırıldı; `CRMFILO_TEST_USER`/`CRMFILO_TEST_PASSWORD` yoksa `throw` ediyor (`:11-12`). Doğru, ama bu **test kapsamı değil** — CI iş akışı hâlâ ölü yolu referanslıyor.

### 2.2 Kısmen ilerleyen maddeler

| ID | Ne yapıldı | Kalan |
|---|---|---|
| **K-6** | Min uzunluk 6 → **12**; kilit 5 deneme / **15 dakika** | Rakam/büyük/küçük/özel karakter şartı hâlâ `false` (`:201-204`) → `aaaaaaaaaaaa` kabul ediliyor. Legacy SHA256 erişilebilir |
| **Y-9** | 6 yere timeout eklendi (`WebhookService.cs:267,364` dahil) | **Retry yok, TLS hardening yok.** Polly paketi referanslı ama **hiç kullanılmıyor** — ölü bağımlılık |
| **O-4/O-5** | Kullanıcı/firma bağlamı açıklamaları eklendi | Kritik/opsiyonel ayrımı hâlâ yorum düzeyinde |

**Y-9'daki ölü Polly bağımlılığı dikkat çekici:** `MKFiloServis.Web.csproj:13` Polly'yi referanslıyor, kodda tek bir kullanımı yok. Ya kullanılmalı ya da paket kaldırılmalı — şu an yanıltıcı bir "dayanıklılık var" izlenimi yaratıyor.

### 2.3 Diğer değişiklikler (rapor maddesi dışı)

| Dosya | Değişiklik | Değerlendirme |
|---|---|---|
| `HealthController.cs` | `/api/health` anonim ama sadece `Status`+`Timestamp` döndürüyor; `/api/health/details` → `Roles="Admin"` | ✅ İyi tasarım — liveness herkese açık, ayrıntı yalnız yöneticiye |
| `AdminSystemHealth.razor` | `[Authorize(Roles="Admin")]` eklendi | ✅ |
| `AdminSystemHealth.razor.cs` | Ana kurtarma anahtarı hex doğrulaması + 32 bayt kontrolü, `finally` içinde `ZeroMemory` | ✅ İyi — anahtar bellekte bırakılmıyor |
| `Error.razor`, `NotFound.razor` | `[AllowAnonymous]` eklendi | ✅ `_Imports` değişikliğinin zorunlu sonucu |
| `PlaywrightSmoke/Program.cs` | CLI argümanı, env zorunluluğu | ✅ |
| `setup/build.ps1` | JWT sırrı paketten çıkarıldı | ✅ |
| `docs/analiz/Rent-a-Car-...md` | 601 satır silindi | ⚠️ Neden silindiği belirtilmemiş — kontrol edilmeli |
| Test rehberleri (4 dosya) | `admin123` referansları temizlendi | ✅ Tutarlılık |

---

## 3. Madde Bazında Güncel Durum

### Kritik
| ID | Durum | Kanıt |
|---|---|---|
| K-1 | 🔴 **DEĞİŞMEDİ** | `MainForm.cs:15` ve `LicenseService.cs:35` aynı düz metin anahtar. RSA/ECDSA yok. Grace period yok. |
| K-2 | ✅ Çözüldü | `_Imports.razor:10` global `[Authorize]` + 6 istisna. API controller'ları korundu. |
| K-3 | ✅ Çözüldü | `Program.cs:571-576` üç şema da JWT. Blazor etkilenmedi (doğrulandı). |
| K-4 | ✅ Çözüldü | 4 appsettings + `.env.example` + docker-compose temiz. Parmak izi kontrolü eklendi. |
| K-5 | ✅ Çözüldü | Config-driven seed + kurulum sihirbazı. Kaynak kodda bilinen parola yok. |
| K-6 | 🟡 Kısmi | Min 12 + 15dk kilit ✅ / karmaşıklık kuralları yok ❌ |

### Yüksek
| ID | Durum | Kanıt |
|---|---|---|
| Y-1 | ⚪ Yok | 39 boş catch — sıfırı loglandı. KolayMuhasebe ve FaturaSablon'da 5 adet. |
| Y-2 | ⚪ Yok | `ProformaFaturaService.cs:536`, `LucaPortalService.cs:899` |
| Y-3 | ⚪ Yok | `LucaPortalService.cs:211` düz metin parola |
| Y-4 | ⚪ Yok | `EbysEvrakService.cs:326` `AtayanKullaniciId = 1` |
| Y-5 | ⚪ Yok | `Program.cs:1214-1230` Swagger korumasız |
| Y-6 | ⚪ Yok | `Program.cs:171-176` 3 uyarı bastırılıyor |
| Y-7 | ⚪ Yok | `MKFiloServis.Tests` yok, `tests.yml:63,67,74` ölü yol |
| Y-8 | ⚪ Yok | `ArchiveMigrationService.cs:172,284`, `LisansService.cs:206`, `PlaywrightScraperService.cs:59` |
| Y-9 | 🟡 Kısmi | Timeout ✅ / retry ❌ / TLS ❌ / Polly ölü bağımlılık |
| Y-10 | ⚪ Yok | `Program.cs:44-46` |
| Y-11 | ⚪ Yok | `Web.csproj:90`, `DataSync.csproj:28` |

### Orta
| ID | Durum | Kanıt |
|---|---|---|
| O-1 | ⚪ Yok | `DatabaseRuntimeResolver.cs:9,43,66,81` PostgreSQL sabit |
| O-2 | ⚪ Yok | `ApplicationDbContext.cs:3311-3316` `firmaId = 1` |
| O-3 | ⚪ Yok | `:3343-3348`, `:3415-3419` |
| O-4 | 🟡 Kısmi | Açıklama eklendi, sınıflandırma yok |
| O-5 | 🟡 Kısmi | Log eklendi, hata fırlatmıyor |
| O-6 | ⚪ Yok | `DatabaseBackupService.cs:385-387` TODO |
| O-7 | ⚪ Yok | DataSync şema ön koşulu yok |
| O-10 | ⚪ Yok | `README.md:68-69` hâlâ var olmayan projeleri belgeliyor |
| O-11 | ✅ Çözüldü | `docker-compose.yml` zorunlu interpolasyon |
| O-12 | ⚪ Yok | `CliRunner.cs:101` `Password=Fast123` |
| O-13 | ✅ Temiz | `git ls-files` doğrulandı; yalnız `test_all.txt` kaldı |

---

## 4. Açık Riskler ve Tespitler

### 🔴 R-3 (yeni) — Lisans aracı hâlâ müşteri paketine giriyor
**Konum:** `setup/build.ps1:48, :121-125, :172, :184`

`build.ps1` hâlâ `MKFiloServis.LisansDesktop.csproj` projeyini publish ediyor ve `LisansSetup.iss` ile `MKLisansArac-<version>.exe` üretiyor. Bu binary, K-1'deki imza anahtarını (`MainForm.cs:15`) içinde barındırıyor.

**Senaryo:** Müşteri paketini alan kişi `MKLisansArac-*.exe`'yi açar, `MainForm.cs` içeriğini okur, `LicenseService.cs:132` şemasını kopyalar, kendi lisansını üretir.

**K-1 tek başına zaten Go/No-Go nedeniyken, paketleme tarafı da kapatılmalı.** Lisans üretim aracı yalnızca şirket içinde kalmalı.

### 🟡 R-4 (yeni) — Kilitlenen hesap zaman aşımıyla açılmıyor
**Konum:** `KullaniciService.cs:331-336`, `Program.cs:206-207`

`Program.cs`'de `Lockout.DefaultLockoutTimeSpan = 15 dakika` ayarlandı — **ama** giriş kilidi servis seviyesinde, kendi sayacıyla uygulanıyor:
```csharp
// BasarisizGirisSayisi 5'e ulaşınca kilitli
```
Zaman kontrolü yok. Kilitli kullanıcı **sistem yöneticisi elle sıfırlamayana kadar** kilitli kalıyor; 15 dakika ayarı ölü kod.

**Etki:** 5 yanlış şifre denemesi sonrası kullanıcı kendi hesabına erişemez. Yönetici müdahalesi gerekir. Saha desteği yükü ve kullanıcı memnuniyeti riski. Identity'nin kendi kilit mekanizması yerine özel sayaç kullanıldığı için bu boşluk oluştu.

### 🟡 R-5 (yeni) — `GuzergahlarController` sınıf düzeyinde korunmamış
**Konum:** `GuzergahlarController.cs:27, :65, :98, :152, :235, :254`

Diğer tüm controller'lar sınıf seviyesinde `[Authorize]` alırken bu controller'da 6 action'a tek tek eklenmiş. **Şu an eşdeğer**, ancak yeni bir action yazıldığında unutulma riski yüksek — bu, tam olarak K-2'nin çözümüyle kapatılan "UI kontrolüne güvenme" hatasının kendisi.

Ayrıca `AuthenticationSchemes = "Bearer"` string literal'i yerine `JwtBearerDefaults.AuthenticationScheme` kullanılmalı — şu an çalışıyor ama tip güvenliği yok.

### 🟡 R-6 (yeni) — `_Imports.razor` global `[Authorize]` API tarafını kapsamıyor
**Konum:** `Components/_Imports.razor:10`

Global `[Authorize]` yalnızca Blazor bileşenlerine uygulanıyor. ASP.NET Core tarafında hâlâ `FallbackPolicy` yok — `Program.cs`'de `AddAuthorization` fallback'i tanımlı değil.

**Sonuç:** Blazor sayfaları korumalı, ama yeni bir minimal API endpoint'i eklenirse veya `[AllowAnonymous]` eklenmezse kontrolsüz kalır. Mevcut controller'lar manuel korunduğu için **şu an açık risk yok**, ama bu, uzun vadede aynı sınıf hatayı yeniden üretme eğilimi.

### ⚠️ R-7 — `App_Data/db-transitions/*.json` dosyalarında parola kalıntısı
`App_Data` altındaki geçiş dosyalarında `Password=Fast123` içeren PostgreSQL bağlantı dizeleri bulundu. Git'e izlenmiyor ama diskte duruyor. Temizlenmeli ve `.gitignore`'a eklenmeli.

### ⚠️ R-8 — `docs/analiz/Rent-a-Car-Modulu-Analiz-Raporu.md` silindi
601 satırlık doküman sessizce silinmiş (git status `D`). Bu rapor faz planının referans verdiği "değerlendirme raporu" kaynaklarından biri olabilir. Silme gerekçesi kayıt altında değil. Yanlışlıkla silinmiş olabilir.

---

## 5. Derleme Doğrulaması

`dotnet build MKFiloServis.slnx -c Debug` çalıştırıldı:

```
Oluşturma başarılı oldu.
    2 Uyarı
    0 Hata
Geçen Süre 00:00:47.23
```

Başarıyla derlenen projeler: `MKFiloServis.Shared`, `MKFiloServis.Web`, `MKFiloServis.DataSync`, `MKFiloServis.LisansDesktop`, `MKFiloServis.Client` (android + windows), `MKFiloServis.PlaywrightSmoke`.

**2 uyarı** (her ikisi de ilgisiz):
- `LisansDesktop/MainForm.cs:103` — `CS0169`: `_suppressHistoryRefresh` alanı hiç kullanılmıyor
- `LisansDesktop/MainForm.cs:102` — `CS0649`: `_isLoadingSelection` alanı hiç atanmıyor

**`_Imports.razor` global `[Authorize]` + sayfa seviyesindeki 64 `[Authorize]` çakışması derlemeyi bozmadı.** Blazor aynı attribute'un birden fazla uygulanmasına izin veriyor. Bu, kod tabanında daha önce belirsiz olan bir riskti — şimdi önlendi.

**Uyarı:** Log dosyaları (`build.log`, `build2.log`) eski ve yanıltıcı. `build.log` "0 hata" diyor ama 1,28 saniyelik no-op artımlı derleme. `build2.log` "2 hata" diyor ama ikisi de dosya kilidi (MSB3021/MSB3027), kod hatası değil. Güvenilir tek kaynak bu denetimin canlı derlemesi.

---

## 6. Değerlendirme

### Bu turda iyi yapılanlar

1. **Odak doğruydu.** Güvenlik açığı olan dosyalara (seed, auth, appsettings, setup) odaklanılmış; doküman/UI dosyaları öncelik almamış.

2. **Kurulum sihirbazı çözümü örnek niteliğinde.** Önceki denetimde raporladığım kilitlenme riski, sadece "sorunu düzelt" değil, **saldırı yüzeyini de kapatacak** şekilde çözülmüş: bootstrap token, `FixedTimeEquals`, fail-closed, transaction, 12 karakter şifre. Bu, planın Faz 1 kabul ölçütlerini ("anonim doğrudan URL reddedilir") karşılıyor.

3. **Sır temizliği sistematik yapıldı.** Tek bir `appsettings.json` düzeltmesi değil, 4 appsettings + `.env.example` + docker-compose + kurulum betiği birlikte ele alınmış. Parmak izi kontrolü gibi "anahtar döndürme unutulursa" senaryosunu düşünen ek önlem eklenmiş.

4. **API tarafı ihmal edilmemiş.** Controller'lar, SignalR hub ve minimal API endpoint'leri (admin backfill) ayrıca korunmuş.

5. **Derlenebilirlik korunmuş.** 27 dosyalık değişiklik seti derleniyor.

### Bu turda eksik kalanlar

1. **K-1'e yine dokunulmadı.** Artık "kaçırılabilir bir madde" değil — iki turdur, diğer tüm kritikler kapanırken tek duran madde. Anahtar hâlâ iki yerde birebir aynı, paketleme de hâlâ yapılıyor (R-3).

2. **Parola karmaşıklığı hâlâ yok.** Min 12 karakter iyi bir hamle ama `aaaaaaaaaaaa` geçerli. Gerçek güvenlik artışı sınırlı.

3. **Kilit mekanizması tutarsız.** 15 dakikalık süre ayarı ölü kod (R-4).

4. **Yüksek öncelikli 21 madde hiç dokunulmadı.** Özellikle muhasebe/fatura yollarındaki 5 boş `catch` (veri bütünlüğü) ve Swagger'ın açık olması.

5. **Test altyapısı hâlâ yok.** Test aracı sertleştirildi ama hâlâ `MKFiloServis.Tests` projesi yok, CI hâlâ ölü yolu restore etmeye çalışıyor. **Şu an hiçbir otomatik kalite güvencesi yok.**

### Öncelik sırası önerisi

```
1. K-1 + R-3   → Lisans anahtarı + paketten aracı çıkar  (EN YÜKSEK)
2. K-6 + R-4   → Karmaşıklık kuralları + kilit zaman aşımı
3. Y-7         → Test projesi ve CI'ı ayağa kaldır
4. Y-1 (muhasebe/fatura 5 catch) + Y-5 Swagger
5. R-5, R-6, R-7, R-8  → Tutarlılık ve temizlik
```

---

## 7. Güncel Özet

| Ölçüt | Önceki denetim | Şimdi |
|---|---:|---:|
| Toplam madde | 39 | 39 |
| Tamamen düzeltilen | 1 | **6** |
| Kısmen ilerleyen | 5 | **8** |
| Değişiklik olmayan | 22 | 21 |
| Tamamlanan faz | 0 / 7 | **0 / 7** (Faz 1 büyük ölçüde tamamlandı, kabul kanıtı bekliyor) |
| Derleme | Doğrulanmamıştı | ✅ **0 hata** |
| Açık yeni risk | 1 | 1 (R-3) + 3 uyarı düzeyi |

**Faz 1 durumu:** K-2, K-3, K-4, K-5 kapandı; K-1 açık; K-6 yarım. Faz planının Faz 1 kabul ölçütlerinden "üretim seed'inde test hesabı yoktur" ve "anonim doğrudan URL reddedilir" maddeleri karşılandı. **Faz 1 kapatılabilir, ancak K-1 hariç tutularak kapatılırsa lisans riski Faz 2'ye sızar ve bu doğru olmaz.**

---

*Bu denetimde `dotnet build` canlı olarak çalıştırıldı ve 0 hata ile başarıyla derlendiği doğrulandı. K-1, K-6 ve kilit mekanizması gibi maddeler statik kod okumasıyla doğrulandı; çalışma zamanı davranışları (özellikle kurulum sihirbazının gerçek akışı ve kilitlenme senaryosu) canlı ortamda test edilmemiştir.*
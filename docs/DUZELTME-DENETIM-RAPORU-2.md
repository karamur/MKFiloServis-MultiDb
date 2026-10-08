# MKFiloServis — İkinci Düzeltme Denetim Raporu

**Denetim tarihi:** 2026-10-02 (önceki denetimin ~2 saat sonrası)
**Önceki denetim:** `DUZELTME-DENETIM-RAPORU.md` tarihsel kaynak dosyası çalışma ağacında bulunmuyor; bu bağlantı kaldırıldı. Güncel takip için [görev envanteri](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) kullanılır.
**İlk rapor kesitinin kapsamı:** İlk denetimden sonra yapılan 27 dosyalık değişiklik (+297 / −828 satır). Sonraki değişiklikler Bölüm 8–26'te kayıtlıdır.
**İlk rapor kesitinin yöntemi:** Git diff incelemesi, satır bazlı doğrulama ve o kesitte canlı `dotnet build`. Sonraki her ek için doğrulama durumu ayrıca belirtilir.

> **Güncelleme notu (2026-10-02):** İlk denetim bölümleri `04342dc3` commit'indeki durumu kaydeder. Sonraki düzeltmeler ayrı takip notlarında belgelenmiştir: K-6/R-4 parola ve kilit düzenlemeleri, lisans aracının müşteri paketinden ayrılması, R-5/R-6 API koruması ve R-7 manifest temizliği. K-1/K-6 devam işleri ve bunların build/runtime kanıtları Bölüm 11'de kayıtlıdır.
>
> **Son güncelleme (2026-10-02):** LisansDesktop ekranı **Lisanslar**, **Anahtar ve Yedek** ve **Paketleme** sekmeleriyle sadeleştirildi; dört anahtar butonu kendi sekmesindedir. Release/win-x64 EXE yeniden yayımlandı (Bölüm 20 ve 22). Web'de Development JWT User Secrets yapılandırması ve iki framework ucunun açık erişim metadatası düzenlendi; tarih uyarısı PostgreSQL sağlayıcısıyla sınırlandı (Bölüm 21). Derlemeler başarılı; ekran, yedek/geri yükleme ve Web çalışma zamanı kabulü henüz teyit edilmedi.
>
> **Güncel çalışma ağacı eki (2026-10-02):** İlk kurulum bootstrap token, API kimlik doğrulaması, CI, DataSync, HTTP dayanıklılığı, parola saklama, paketleme ve lisans geçiş belgelerinde ek değişiklikler yapıldı. Lisans Desktop tekil `v2:` yeniden basım akışı derlendi. PDF, Luca, audit, başlangıç, DataSync ve restore düzeltmelerinin güncel kapsamı Bölüm 18'dedir.
>
> **Durum renkleri:** 🟢 uygulandı ve mevcut kanıtla doğrulandı · 🟡 kısmi veya doğrulama bekliyor · 🔴 açık işlem/yayın engeli · ⚪ değişiklik yok.

> **Yeniden analiz (2026-10-02):** İlk 39 maddenin kaynakla karşılaştırması bu raporun Bölüm 23'ünde özetlendi; o tarihteki ayrı ayrıntı dosyası çalışma ağacında bulunmuyor. Mevcut birleşik görevler [2026-10-06 envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) izlenir. Geçersiz sürümün lisans kontrolünden geçmesi, yeni belge şifrelemesine uyumsuz yedek ön koşulu ve CI dosya filtreleri için üç yeni açık iş tespit edilmişti. Derleme ve kaynak düzeltmeleri satışa hazırlık kabulü yerine geçmez.

> **K-1 için geçerli yapı:** Lisans üretimi ve anahtar yönetimi yalnız **LisansDesktop** programında yapılır. **Anahtar ve Yedek** sekmesinde **Anahtar Durumu**, **Şifreli Yedek Oluştur**, **Yedeği Doğrula** ve **Anahtar / Yedek İçe Aktar** butonları bulunur. Anahtar programın şifreli deposundadır. Harici betik, ortam değişkeni veya ayrı anahtar yönetim hizmeti gerekmez. Önceki bölümlerdeki PEM/harici yedek açıklamaları tarihsel kayıttır; güncel uygulama Bölüm 20'dedir.

---

## 1. Yönetici Özeti

İlk denetim sayımı tarihsel durumu gösterir. K-1'in kodu değişti; müşterilere geçiş ve kabul doğrulaması tamamlanmadığı için kapanış sayımına eklenmedi.

İlk denetimin sayısal dökümü Bölüm 7'de korunmuştur. Sonraki turlar için tüm 39 bulgunun yeniden sayımı yapılmadı.

**Önceki uygulama kesitlerinde kayıtlı derleme:** LisansDesktop Debug/`--no-restore` ve Web Debug/`--no-restore -p:UseAppHost=false` derlemeleri 0 hata, 0 uyarı ile tamamlandı. Son değişiklikler için tüm çözüm Debug derleme sonucu Bölüm 18'de ayrıca kayıtlıdır.

**K-1'in kod düzeltmesi tamamlandı; kurulum geçişi açık.** Ortak imza sırrı masaüstü imzalayıcıdan ve Web uygulamasından kaldırıldı. Lisanslama ve şifreli anahtar yönetimi LisansDesktop içindedir. Önceden verilmiş lisansların geçişi, programdan yedekleme/geri yükleme kabulü ve kurulum kabul senaryoları tamamlanmalıdır.

### Son çalışmaların özeti

| İş | Uygulama durumu | Kalan kabul |
|---|---|---|
| K-1 lisans ve anahtar yönetimi | 🟢 Program içi şifreli depo, dört doğrudan erişim butonu, parola korumalı yedek ve doğrulama kodu uygulandı | 🟡 Güncel ekran, gerçek `.mkkey` yedeği, geri yükleme ve müşteri geçişi |
| LisansDesktop kullanılabilir çıktı | 🟢 Release/win-x64 self-contained/single-file publish tekrar başarılı; `setup/payload/LisansDesktop/MKFiloServisLisans.exe` güncellendi | 🟡 Kullanıcının yeni ekranı gördüğüne ilişkin teyit |
| Development JWT | 🟢 Bu bilgisayarda User Secrets'a kalıcı geliştirme anahtarı kaydedildi; Web projesinde yükleme kimliği tanımlandı | 🟡 Yeniden başlatmalar arasında token kabulü; ilk geçişte eski tokenlar yenilenmeli |
| R-6 framework uçları | 🟢 Opaque yönlendirme ve statik dosya grubuna açık anonim erişim metadatası uygulandı | 🟡 Yeniden başlatma sonrası envanter, giriş/yönlendirme ve erişim kabulü |
| Y-10 tarih davranışı | 🟢 Uyarı yalnız PostgreSQL için gösterilir; kaynak saat dilimi varsayımı düzeltildi | 🟡 Legacy davranış açık; tarih sütunu incelemesi ve veri geçişi |

**Son kanıt:** LisansDesktop Debug ve Web Debug derlemeleri **0 uyarı, 0 hata** ile tamamlandı. LisansDesktop Release publish başarılı oldu. Son Web düzenlemeleri tüm çözümün yeniden derlendiği anlamına gelmez. Yeni otomatik test veya çalışma zamanı kabulü yapılmadı.

---

## 2. Bu Turda Yapılan İşler

### 2.1 Kodda düzeltilen maddeler

#### 🟡 K-1 — Lisans imza anahtarı — **KOD DÜZELTİLDİ, GEÇİŞ BEKLİYOR**
Bu, raporun ilk günden beri bir numaralı riskiydi. Düzeltme üç parçalı bir geçişle yapıldı:

**1. Simetrik anahtar iki yerden de kaldırıldı.**
`MainForm.cs` içindeki `private const string SECRET = "[MASKELENDI]";` satırı silindi. İmzalayıcı artık SHA-256 değil **RSA-PSS** kullanıyor (`:825-832`):
```csharp
using var rsa = LicenseSigningKeyStore.OpenSigningKey(); // LisansDesktop içi şifreli depo
var signature = "v2:" + Convert.ToBase64String(rsa.SignData(
    Encoding.UTF8.GetBytes(raw), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
```
Özel anahtar uygulama binary'sine gömülmez. Güncel yapıda LisansDesktop tarafından Windows kullanıcı hesabına bağlı şifreli depoda tutulur; anahtar ve yedek işlemleri program menüsünden yürütülür (Bölüm 20).

**2. Web yalnızca public key ile doğruluyor** (`LicenseService.cs:148-185`). İmza formatı artık önekli ve üç dallı:
| Önek | Doğrulama |
|---|---|
| `v2:` (ticari) | Kaynak kodda gömülü `PublicKeyPem` (`:38-39`) ile RSA-PSS doğrulama — Web'de özel anahtar **yok** |
| `demo:` | DataProtection (`MKFiloServis.License.Demo.v1`) + `FixedTimeEquals` — **imzalama anahtarını kullanmıyor** |
| (eski) | Yalnız `Licensing:LegacyVerificationSecret` **ve** `Licensing:LegacyAcceptUntilUtc` tanımlıysa, süre dolmamışsa; aksi halde `false` (fail-closed) |

Demo lisans imzası artık ticari imzalama sırrından tamamen ayrıldı — daha önce ikisi aynı anahtarı paylaşıyordu.

**Süre bitimi:** Önceden doğrulanmış ticari lisansa bitişten sonra en fazla **14 gün** yenileme toleransı veriliyor (`LicenseService.cs:435-439`); demo bu toleranstan dışarıda.

**3. Web içinden lisans üretimi tamamen kaldırıldı.** `LicenseController.cs` artık 17 satır ve `generate` ucu **`410 Gone`** döndürüyor:
```csharp
[Authorize(Roles = "Admin")]
[HttpPost("generate")]
public IActionResult Generate() => StatusCode(StatusCodes.Status410Gone, new {
    error = "Web uygulamasında lisans üretimi kapatıldı. Lisansı şirket içi imzalama aracıyla oluşturun." });
```
`LicenseGenerate.razor` de üretim formunu kaldırdı, yalnız yönlendirme bıraktı; `LicenseGenerate.razor.cs` tamamen silindi. Yani **"Admin rolündeki müşteri kullanıcısı lisans üretebilir"** saldırı yolu bu turda da kapandı.

**Kalan işler ve dağıtım riski:**
- Hiçbir `appsettings*.json` içinde `Licensing` bölümü yok. Bu, legacy doğrulamayı **güvenli varsayılanla kapatır** (fail-closed), ancak mevcut imzalı müşteri lisansları için `LegacyVerificationSecret`/`LegacyAcceptUntilUtc` tanımlanmazsa lisanslar reddedilir. Bu ayarlar dağıtım ortamında env ile verilmeli ve kesim tarihi duyurulmalı.
- Eski sırrın Git geçmişinde bulunması nedeniyle legacy geçiş penceresi boyunca eski format taklit edilebilir; bu pencere mümkün olan en kısa tutulmalı.
- Sabit geliştirici bypass anahtarı, makine/kullanıcı kontrolü ve `DevSettings:OverrideKey` ayarı kaldırıldı. Geliştirme ortamında da lisans doğrulaması aynı akışı kullanır.

#### 🟡 K-2 — Yetkisiz erişim (Blazor/API kod koruması) — **KOD UYGULANDI, RUNTIME KABULÜ BEKLİYOR**
**Nasıl:** `Components/_Imports.razor:10` → `@attribute [Authorize]`

Bu, Pages ağacındaki **tüm bileşenlere** tek satırla yetki zorunluluğu getiriyor. Daha önce 283 `@page` yönlendirmesinden yalnızca 63'ü korumalıydı; artık 6 açık istisna dışında hepsi korumalı:
- `Login.razor:3`, `Portal/Landing.razor:3`, `Setup/SetupWizard.razor:3`, `Setup/LisansSetup.razor:3`, `Error.razor:3`, `NotFound.razor:2`

15 sayfa ek olarak `Roles="Admin"` veya çoklu rol şartı kullanıyor (`Admin/Denetim.razor:9`, `AdminSystemHealth.razor:2`).

**API tarafı da korundu:** `AuthController` sınıfı Bearer `[Authorize]` taşır; yalnız `POST /api/auth/login` `[AllowAnonymous]` istisnasıdır. `refresh` ve `verify` Bearer ister. `MapControllers()` ayrıca JWT Bearer `RequireAuthorization` ile eşlenir; açık health/Grafana action'ları `[AllowAnonymous]` ile ayrılır. `EvrakHub` sınıfında da Bearer `[Authorize]` vardır.

**R-5 düzeltmesi:** `GuzergahlarController` artık sınıf seviyesinde `JwtBearerDefaults.AuthenticationScheme` ile korunur; Excel import action'ında ayrıca Admin rolü gerekir. Web derlemesi bu değişiklik için daha önce başarılı kaydedilmiş; HTTP anonim/izinli istek senaryoları hâlâ çalıştırılmalı.

#### 🟡 K-3 — Kimlik doğrulama şeması — **KOD UYGULANDI, CIRCUIT KABULÜ BEKLİYOR**
`Program.cs:571-576`:
```csharp
options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
```
Kayıtsız `"Cookies"` şeması kaldırıldı. Artık `Cookies` handler'ı olmayan bir duruma atıflar.

**Blazor ve API kimliği ayrı tutuluyor:** Blazor devresi `AppAuthenticationStateProvider` ve `ProtectedSessionStorage` ile kullanıcı kimliğini geri yükler; HTTP API ise Bearer JWT kullanır. Cookie handler eklenmedi. Bu statik tasarım incelemesi, circuit yenileme/çıkış/rol iptali runtime kabulünün yerini tutmaz.

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

#### 🟡 O-13 — Git'te izlenen dosyalar — **KISMEN TEMİZ**
`git ls-files`: bin/obj ve log dosyaları izlenmiyor. `test_all.txt` izlenen bir dosya olarak kaldı; içeriği/amacı ayrıca incelenmeden silinmemeli.

#### ✅ Y-7 (kısmen) — Test aracı sertleştirildi
`Tests/PlaywrightSmoke/Program.cs`: `admin123` fallback kaldırıldı; `CRMFILO_TEST_USER`/`CRMFILO_TEST_PASSWORD` yoksa `throw` ediyor (`:11-12`). Doğru, ama bu **test kapsamı değil** — CI iş akışı hâlâ ölü yolu referanslıyor.

### 2.2 Kısmen ilerleyen maddeler

| ID | Ne yapıldı | Kalan |
|---|---|---|
| **K-6** | En az 12 karakter ve 5 başarısız denemede kilit | İlk denetimde karakter şartları kapalı, süreli kilit çalışmıyordu. Devam düzeltmesiyle karmaşıklık kuralları açıldı; yeni kilit bitiş zamanı DB'de tutuluyor. Legacy SHA-256 doğrulaması başarılı girişte 310.000 yinelemeli Identity V3 PBKDF2'ye yükseltilir. |
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
| `docs/analiz/Rent-a-Car-...md` | Çalışma ağacında silinmiş görünüyor; `04342dc3` commit'inde silinmedi | ⚠️ Silme nedeni doğrulanamadı; GitHub'daki dosya korunuyor. Yerel değişiklik için karar verilmeli |
| Test rehberleri (4 dosya) | `admin123` referansları temizlendi | ✅ Tutarlılık |

---

## 3. Madde Bazında Güncel Durum

### Kritik
| ID | Durum | Kanıt |
|---|---|---|
| K-1 | 🟡 Program içi yapı uygulandı; kabul/geçiş açık | Şifreli depo, yedekleme, doğrulama ve içe alma doğrudan görünen program butonlarından yürütülür. Güncel EXE tekrar yayımlandı. Harici betik/ortam değişkeni gereksinimi kaldırıldı. Ekran kabulü, gerçek yeni yedek/geri yükleme ve müşteri geçişi açık; Bölüm 20. |
| K-2 | 🟡 Kod uygulandı, runtime kabulü bekliyor | `_Imports.razor` global `[Authorize]` + anonim sayfa istisnaları; MVC controller map'inde Bearer zorunlu; `AuthController/login` tek anonim API istisnası; hub Bearer ister. Erişim senaryoları runtime'da doğrulanmadı. |
| K-3 | 🟡 Kod uygulandı, circuit kabulü bekliyor | `Program.cs` varsayılan HTTP authenticate/challenge şeması JWT Bearer. Blazor özel circuit provider kullanıyor; yenileme/çıkış/iptal senaryolarının runtime kabulü açık. |
| K-4 | ✅ Çözüldü | 4 appsettings + `.env.example` + docker-compose temiz. Parmak izi kontrolü eklendi. |
| K-5 | ✅ Çözüldü | Config-driven seed + kurulum sihirbazı. Kaynak kodda bilinen parola yok. |
| K-6 | 🟡 Uygulandı, çalışma zamanı doğrulaması bekliyor | Min 12 + büyük/küçük harf, rakam ve sembol koşulları; beş denemeden sonra DB'de tutulan 15 dk kilit. Legacy SHA-256 başarılı girişte 310.000 yinelemeli Identity V3 PBKDF2'ye yükseltilir. |

### Yüksek
| ID | Durum | Kanıt |
|---|---|---|
| Y-1 | 🟡 Kısmi; hızlı muhasebe akışları atomik hale getirildi | Önceki catch'ler kaldırıldı. `KolayMuhasebeService` fatura/masraf ve bunlara bağlı fiş/banka/stok kayıtlarını transaction içinde yürütür; fiş aynı DbContext'te kaydedilir ve sayaç yazımı mevcut PG transaction'ına bağlanır. Cari oluşturma/hesap eşleme ön hazırlığı bu transaction dışında; diğer kritik akışlar ve runtime hata provası açık. Bölüm 14 ve Y-1 devamı. |
| Y-2 | 🟡 Kod tamamlandı; kabul bekliyor | Proforma QuestPDF üretimi ve Luca UBL Invoice ayrıştırıcısı uygulandı. PDF görsel kabulü ve gerçek portal XML/entegrasyon kabulü bekliyor. Bölüm 18. |
| Y-3 | 🟡 Geçiş kodu tamamlandı | Parola ve tokenlar korunuyor; eski düz metin dosyası ilk okumada atomik olarak şifreli biçime geçirilir. Okunmamış eski dosyaların ve Data Protection anahtar yedeğinin kabulü açık. Bölüm 18. |
| Y-4 | 🟢 Kod düzeltildi | AtayanKullaniciId oturumun NameIdentifier/KullaniciId claim'inden alınır; geçersiz oturum reddedilir. Sabit kullanıcı bulgusu güncel kodda yok. Bölüm 18. |
| Y-5 | 🟢 Kod koruması tamamlandı | Production Swagger opt-in olsa da Admin Bearer token gerektirir; anonim/geçersiz token 401, yetkisiz rol 403. HTTP kabulü diğer erişim maddeleriyle takip edilir. Bölüm 18. |
| Y-6 | 🟡 Görünürlük düzeltildi; şema uyumu açık | `PendingModelChangesWarning` loglanıyor; bastırma kaldırıldı. Bu, model/migration farkının giderildiği anlamına gelmez. Şema uyumu ayrıca doğrulanmalı. Bölüm 17/23. |
| Y-7 | 🟡 CI düzeltildi; test projesi açık | Linux CI Web/Shared derler; MAUI/Windows çözümünü Linux'ta derleme hatası giderildi. Test projesi bulunduğunda ayrıca restore/build yapılır. Kök birim test projesi hâlâ yok. Bölüm 18. |
| Y-8 | 🟢 Üretim beklemesi düzeltildi | Selenium TryClickAsync ve tüm çağrıları await kullanır; Thread.Sleep kaldırıldı. PlaywrightSmoke yardımcı kodu üretim servislerinin dışında tutulur. Bölüm 18. |
| Y-9 | 🟡 Kısmi | Polly kaldırıldı. Webhook/Teams/Slack istemcilerinde `ResilientHttpMessageHandler` 2 denemeli üstel geri çekilmeli retry uygular. **Hatta bulunan ve düzeltilen gerçek hata:** ilk sürüm aynı `HttpRequestMessage` örneğini tekrar gönderiyordu; `HttpClient` bunu "already sent" `InvalidOperationException` ile reddediyor ve bu hata `catch` bloklarına düşmediği için ilk retry'de webhook çağrısını tümden başarısız kılıyordu. Artık gövde bir kez belleğe alınıp her deneme için taze istek klonlanıyor. POST çift gönderim riski için `retryNonIdempotent` bayrağı eklendi: idempotent olmayan metotlarda yalnızca isteğin sunucuya ulaşmadığı kesin hatalar (DNS/bağlantı reddi) tekrar deniyor, belirsiz hatalar (zaman aşımı, bağlantı sıfırlanması) tekrar edilmiyor. Luca HTTPS şartlı. Kalan: diğer entegrasyonlarda kapsamlı TLS/retry doğrulaması yapılmadı. Bölüm 17. |
| Y-10 | 🟡 Veri geçişi bekliyor | Legacy timestamp davranışı korunur; açılış uyarısı yalnız PostgreSQL için yazılır. UTC varsayımı kaldırıldı; geçiş kaynak tarihlerin gerçek saat dilimine göre planlanmalı. Veri dönüşümü yapılmadı. Bölüm 21. |
| Y-11 | ✅ Çözüldü | `NuGetAuditSuppress` ile bastırılan **GHSA-2m69-gcr7-jv3q / CVE-2025-6965** (High) açığı gerçekten kapatıldı: kök neden `Microsoft.Data.Sqlite` → `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (gömülü SQLite < 3.50.2) idi. Her iki projede de `SQLitePCLRaw.lib.e_sqlite3` **2.1.13** doğrudan sabitlendi, iki `.csproj` içinden de `NuGetAuditSuppress` kaldırıldı. `dotnet list package --vulnerable --include-transitive` artık "güvenlik açığı olan paketi yok" diyor. Runtime doğrulaması: `sqlite_version=3.53.3` (> 3.50.2) ve çoklu aggregate terimli sorgu başarıyla çalıştı. Bölüm 17. |

### Orta
| ID | Durum | Kanıt |
|---|---|---|
| O-1 | 🟡 Ayar tutarlılığı düzeltildi | CanonicalProvider artık kaydetme/okumada korunur; manifest gerçek hedefi yazar. PostgreSQL/SQLite dışı otomatik migration açıkça reddedilir; SQL Server/MySQL migration desteği tamamlanmış değildir. Bölüm 18. |
| O-2 | 🟢 Bulunan fallback'ler kaldırıldı | SaveChanges tüm overload'larında eksik firma reddedilir. Legacy aktarımda varsayılan firma 1 kaldırıldı; açık kaynak, etkinleştirme ve mevcut aktif hedef firma gerekir. Bölüm 18. |
| O-3 | 🟡 İzole SQLite kontrolleri geçti; diğer kapsam açık | Personel–araç, 335/195, sıra no, özlük metadata, filo ve banka şablonu silme yazımları tracked kayda taşındı. Audit için 12, filo için 11, ilişki firma doğrulaması için 57 ve güncel banka şablonu/kapsam için 46 izole kontrol geçti. Diğer ExecuteUpdate/Raw SQL yazımları, PostgreSQL/SQL Server retry ve tam model/tenant kabulü açık. 2026-10-04 ekleri. |
| O-4 | 🟡 Sınıflandırma tamamlandı | Şema/kimlik görevleri zorunlu, beş katalog seed görevi opsiyonel. DbInitializer kurtarılamayan migration hatasında başlangıcı durdurur; alt helper ve eski DB açılış kabulü açık. Bölüm 18. |
| O-5 | 🟢 Kod düzeltildi | Firma eşleşmezse mevcut kurulumda aktivasyon reddedilir; çoklu eşleşme de reddedilir. İmzalı firma kodunu değiştiren kullanılmayan yöntem kaldırıldı. Aktivasyon transaction'a alındı. Bölüm 18. |
| O-6 | 🟡 DB restore uygulandı; tam kurtarma açık | PGDMP kontrolü, geri dönüş yedeği ve tek transaction var. ZIP dosya ekleri/ayarlar/anahtarlar otomatik geri yüklenmez. Yeni şifreleme için yedek ön koşulu/kapsam düzeltmesi de gerekli; N-2/D-5. Bölüm 18/23. |
| O-7 | 🟡 Bütünlük kodu tamamlandı | Kaynak snapshot, eksik kolon reddi, satır sayımı, FK denetimi ve commit öncesi sequence güncellemesi eklendi. Tablo hatası tüm aktarımı geri alır. İzole iki yönlü aktarım kabulü açık. Bölüm 18. |
| O-8 | 🟡 İmza verisi ortaklaştırıldı; diğer tekrarlar açık | Web/Desktop ortak `LicenseSignaturePayload` ve sürüm politikasını kullanır. Normalizasyon, introspeksiyon/seed tekrarları ve gerçek imza uyumluluk kabulü ayrıca açık; Bölüm 26. |
| O-9 | ⚪ Eski context dosyaları duruyor | MasterDbContext/HoldingDbContext Obsolete; hedefli aramada aktif kayıt/tüketim bulunmadı. Silme veya uyumluluk gerekçesi belgelenmeli. |
| O-10 | 🟢 Bulunan proje ağacı hataları düzeltildi | README proje ağacı düzeltildi: derlemeye girmeyen, Git'te izlenen **0 baytlık** `MKFiloServis.Infrastructure/Data/ApplicationDbContext.cs` kopyası (`git rm`) ve bırakılmış boş `MKFiloServis.Service/` dizini kaldırıldı; ikisi de çözüme girmiyordu. README artık yalnızca gerçek projeleri listeliyor ve çözümde birim test projesi bulunmadığını (CI'nin bunu algılayacağını) açıkça belirtiyor. Tam dokümantasyon denetimi yapılmadı. Bölüm 17. |
| O-11 | ✅ Çözüldü | `docker-compose.yml` zorunlu interpolasyon |
| O-12 | 🟡 Kısmi | DataSync örneği placeholder kullanıyor, ortak parola varsayılanı boş, deployment betikleri `MKFILO_PG_PASSWORD` istiyor. Tüm repo/geçmiş taraması ve parola rotasyonu kanıtı yok. Bölüm 14. |
| O-13 | 🟡 Kısmi | `git ls-files` incelemesinde bin/obj/log/test_all çıktısı yok. `test_all.txt` ve boş Infrastructure dosyası staged silinmiş; eski rapor/analiz belgeleri unstaged silinmiş. Silme gerekçeleri ve teslim commit'i açık. Bölüm 23. |
| O-14 | 🟡 Program alanı uygulandı; kabul açık | En Fazla Sürüm ve açık sınırsız hak kutusu eklendi; satış/yenileme/müşteri paketi seçilen hakkı imzalar ve kaydeder. v2 yeniden basım kayıtlı hakkı korur. Bölüm 26. |

---

## 4. Açık Riskler ve Tespitler

### 🟡 R-3 (ilk denetimde) — Lisans aracı müşteri paketine giriyordu — **KOD DÜZELTİLDİ, PAKET KANITI BEKLİYOR**
**Konum:** `setup/build.ps1`, `setup/Setup.iss`, `setup/GuncelleSetup.iss`, `setup/build-client.ps1`

**Orijinal tespit:** `build.ps1` `MKFiloServis.LisansDesktop.csproj` projeyini publish ediyor ve `LisansSetup.iss` ile `MKLisansArac-<version>.exe` üretiyordu. Bu binary, K-1'deki imza anahtarını (`MainForm.cs:15`) içinde barındırıyordu; müşteri paketini alan kişi aracı açıp kendi lisansını üretebiliyordu.

**Uygulanan düzeltmeler:**
1. `build.ps1` varsayılan akışta aracı publish etmiyor; yalnızca `-LisansOnly` veya açık `-IncludeInternalLicenseTool` seçeneğiyle dahili çıktı üretiyor. `-SkipPublish` ile dahili araç istenmiş ancak payload yoksa build durduruluyor; eski `LisansDesktop` payload'ı varsayılan müşteri build'inde temizleniyor.
2. `Setup.iss`, `GuncelleSetup.iss` ve müşteri setup tanımları lisans aracı payload/kurulum bileşenini dışarıda bırakıyor. `build-client.ps1` paket özetini de aracın bulunmadığı şekilde güncelliyor.
3. `docs/LISANS-IMZA-GECIS.md` anahtarın şirket içi saklanması, imza içeriği, müşteri lisanslarının `v2:` biçimine taşınması ve legacy kesim adımlarını açıklıyor.
4. Web tarafındaki üretim yolu kaldırıldı — `LicenseController/generate` artık `410 Gone` döndürüyor (Bkz. K-1). Elinde eski araç binary'si olsa bile saldırgan Web üzerinden lisans üretemez; yeni üretim özel anahtarı tutan şirket içi ortamla sınırlı.

**Kalan doğrulama:** Önceki kod kesitinde betik sözdizimi statik kontrol edildi ve çözüm derlendi; bu son paketleme değişikliklerinden sonra build tekrarlanmadı. **Inno Setup müşteri paketi üretilip payload içeriği incelenmedi.** Bu nedenle müşteri paketinde lisans aracının bulunmadığı henüz paket kanıtıyla doğrulanmadı.

### 🟡 R-4 (ilk denetimde) — Kilitlenen hesap zaman aşımıyla açılmıyordu
**Konum:** `KullaniciService.cs:331-336`, `Program.cs:206-207`

İlk denetimde `Program.cs`'de `Lockout.DefaultLockoutTimeSpan = 15 dakika` ayarlanmıştı — **ama** giriş kilidi servis seviyesinde, kendi sayacıyla uygulanıyordu:
```csharp
// BasarisizGirisSayisi 5'e ulaşınca kilitli
```
Zaman kontrolü yok. Kilitli kullanıcı **sistem yöneticisi elle sıfırlamayana kadar** kilitli kalıyor; 15 dakika ayarı ölü kod.

**Devam düzeltmesi:** `KilitlenmeBitisUtc` alanı eklendi ve SQLite/PostgreSQL eski şemaları için idempotent kolon yükseltmesi tanımlandı. Beşinci yanlış denemede 15 dakikalık UTC bitişi saklanıyor; süre dolunca sonraki girişte otomatik sıfırlanıyor. Bitiş zamanı olmayan eski yönetici kilitleri süresiz kalır ve yönetici müdahalesi gerektirir. Statik kod incelemesi yapıldı; çok sağlayıcılı canlı yükseltme ve giriş senaryosu henüz çalıştırılmadı.

### 🟡 R-5 (ilk denetimde açık; kod düzeltildi, HTTP kabulü bekliyor) — `GuzergahlarController` sınıf düzeyinde korunmamış
**Konum:** `GuzergahlarController.cs:27, :65, :98, :152, :235, :254`

Diğer tüm controller'lar sınıf seviyesinde `[Authorize]` alırken bu controller'da 6 action'a tek tek eklenmiş. **Şu an eşdeğer**, ancak yeni bir action yazıldığında unutulma riski yüksek — bu, tam olarak K-2'nin çözümüyle kapatılan "UI kontrolüne güvenme" hatasının kendisi.

**Devam düzeltmesi (2026-10-02):** Controller'a sınıf düzeyinde `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` eklendi; action'lardaki yinelenen Bearer nitelikleri kaldırıldı. Excel importundaki Admin rolü action düzeyinde korunuyor. Böylece sonradan eklenen action'lar varsayılan olarak korumalı ve şema adı framework sabitinden alınıyor. Web Debug build'i bu değişiklikten sonra 0 uyarı/0 hatayla geçti; HTTP anonim/izinli istek senaryoları henüz çalıştırılmadı.

### 🟡 R-6 — Controller ve minimal API yetkilendirme varsayılanları — **GÖRÜNÜRLÜK KAPANDI, KALICI RİSK BİLİNÇLİ OLARAK SÜRÜYOR**
**Konum:** `Components/_Imports.razor:10`, `Program.cs` (MapControllers / startup envanteri)

Global `[Authorize]` yalnızca Blazor bileşenlerine uygulanıyor. ASP.NET Core tarafında kasıtlı olarak global `FallbackPolicy` yok. Tüm MVC controller endpoint'leri `MapControllers().RequireAuthorization(...)` ile Bearer varsayılanına bağlandı; `[AllowAnonymous]` işaretli login, temel sağlık ve Grafana search action'ları açık kalıyor. Development backfill minimal endpoint'leri Admin+Bearer ister.

**Bu turda yapılan düzeltme:** Kalan risk "yeni bir minimal API endpoint'i `.RequireAuthorization(...)` almayı unutursa sessizce açık kalır" idi. Buna karşı `Program.cs` içine bir **startup yetkilendirme envanteri** eklendi: uygulama başlatıldıktan sonra (`ApplicationStarted`) tüm uçlar taranır, yetkilendirme metadatası taşımayanlar uyarı olarak listelenir. Statik varlıklar ve Blazor iskelet uçları filtrelenir, böylece liste okunabilir kalır ve yeni korumasız bir uç eklendiği anda görünür olur. `/healthz` ve `/readyz` bilinçli olarak `.AllowAnonymous()` ile açıkça işaretlendi.

**Global fallback policy durumu:** Varsayılan HTTP şeması JWT Bearer; Blazor devresi `AppAuthenticationStateProvider` ve `ProtectedSessionStorage` kullanır. Cookie handler tanımlı değildir (K-3). Global fallback yerine controller varsayılan koruması ve startup envanteri uygulanır; yeni minimal uçların yetkilendirme gereksinimi ayrıca belirtilmelidir.

**Önceki çalışma zamanı kaydı:** Envanterin ilk `0 uç` sonucundan sonra denetim `ApplicationStarted` sonrasına alındı. Kullanıcının paylaştığı log `762 uç tarandı, 2 uç yetkilendirme metadatası taşımıyor` sonucunu gösterdi: framework'e ait `/_framework/opaque-redirect` ve `{**path:file}`. Bu taramada başka uç raporlanmadı; tüm erişim senaryolarının güvenli olduğunu tek başına kanıtlamaz.

**Son düzeltme:** İki framework ucunun bilinçli açık erişimi metadatayla belirtildi; envanter uyarısı susturulmadı. Kod derlendi. Değişiklikten sonraki çalışma zamanı envanteri henüz alınmadı; Bölüm 21.

**Kalan risk:** Yeni bir uç eklendiğinde envanter uyarısı görülmezse gözden kaçabilir (günlük yine de üretilir). HTTP anonim/izinli istek senaryoları uçtan uca test edilmedi.

### 🟡 R-7 — Geçiş manifestlerinde veritabanı sırları
İlk denetimdeki “Git'e izlenmiyor” tespiti yanlıştı: `MKFiloServis.Web/App_Data/db-transitions/` altında 13 JSON Git tarafından izleniyordu ve 11'inde bilinen sabit bir parola içeren bağlantı dizeleri vardı. Devam düzeltmesinde üretici koddan iki connection string alanı çıkarıldı; mevcut 13 manifestten de bu alanlar kaldırıldı. Klasör `.gitignore`'a eklendi. Manifestlerin mevcut kod tüketicisi yalnızca dosya yolunu ayarlara yazıyor; connection string alanlarına bağımlı değil.

**Kalan güvenlik işi:** Yeni commit eski Git geçmişini temizlemez. Bu parolalardan herhangi biri gerçek/aktif DB kimlik bilgisi olduysa veritabanı sahibince döndürülmeli. Ayrıca ignore edilen eski `_tmpbuild`/`temp` publish kopyalarında hassas ayar dosyaları bulunuyor; bu kopyalar bu düzeltmede değiştirilmedi veya silinmedi.

### 🔴 R-8 — Yerel Rent-a-Car analiz belgesi silinme değişikliği
`docs/analiz/Rent-a-Car-Modulu-Analiz-Raporu.md` çalışma ağacında silinmiş durumda; ancak `04342dc3` commit'inde bu dosya değiştirilmedi ve GitHub'daki sürümü korundu. Yerel silme değişikliğinin kimin/niçin yaptığı bu denetimde belirlenemedi. Faz planının kaynaklarıyla karşılaştırıp karar verilene kadar yerel değişiklik korunmalı; bu durum yayımlanmış kaynak kodun parçası sayılmamalı.

---

## 5. Derleme Doğrulaması

**Üçüncü tur (2026-10-02) — tam çözüm yeniden derlendi.**

İlk deneme paralel yapıldı ve Android hedefinde dosya kilidine takıldı (`XARLP7024`, `obj\...\lp\map.cache`). Bu bir kod hatası değil, MSBuild dosya kilidi yarışıdır. Aynı derleme tek iş parçacıklı çalıştırıldığında temiz geçti:

```
dotnet build MKFiloServis.slnx -c Debug -m:1
Oluşturma başarılı oldu.
    0 Uyarı
    0 Hata
Geçen Süre 00:00:20.00
```

Başarıyla derlenen projeler: `MKFiloServis.Client` (android + windows), `MKFiloServis.Shared`, `MKFiloServis.DataSync`, `MKFiloServis.LisansDesktop`, `MKFiloServis.Web`, `MKFiloServis.PlaywrightSmoke`.

> **Not:** İlk denemedeki 3 uyarı (`MainForm.cs:101-102` CS0169/CS0649 ve `LicenseService.cs:482` CS0219 `isDemo`) tek iş parçacıklı temiz derlemede **hiç görünmedi**; artımlı derleme farkı. İlk denetimde kaydedilen 2 uyarı da aynı şekilde artımlı derleme artığıdır.

**`_Imports.razor` global `[Authorize]` + sayfa seviyesindeki 64 `[Authorize]` çakışması derlemeyi bozmadı.** Blazor aynı attribute'un birden fazla uygulanmasına izin veriyor. Bu, kod tabanında daha önce belirsiz olan bir riskti — önlendi.

**Uyarı:** Log dosyaları (`build.log`, `build2.log`) eski ve yanıltıcı. `build.log` "0 hata" diyor ama 1,28 saniyelik no-op artımlı derleme. `build2.log` "2 hata" diyor ama ikisi de dosya kilidi (MSB3021/MSB3027), kod hatası değil. Güvenilir tek kaynak bu denetimin canlı derlemesidir.

> **Android dosya kilidi notu:** Paralel derlemede `XARLP7024` sık görülüyorsa `-m:1` veya `dotnet build MKFiloServis.Client/MKFiloServis.Client.csproj -f net10.0-android -m:1` ile derlemek güvenilir sonuç verir. CI'a bu madde eklenmelidir.

---

## 6. Değerlendirme

### Bu turda iyi yapılanlar

1. **Odak doğruydu.** Güvenlik açığı olan dosyalara (seed, auth, appsettings, setup) odaklanılmış; doküman/UI dosyaları öncelik almamış.

2. **Kurulum sihirbazı çözümü örnek niteliğinde.** Önceki denetimde raporladığım kilitlenme riski, sadece "sorunu düzelt" değil, **saldırı yüzeyini de kapatacak** şekilde çözülmüş: bootstrap token, `FixedTimeEquals`, fail-closed, transaction, 12 karakter şifre. Bu, planın Faz 1 kabul ölçütlerini ("anonim doğrudan URL reddedilir") karşılıyor.

3. **Sır temizliği sistematik yapıldı.** Tek bir `appsettings.json` düzeltmesi değil, 4 appsettings + `.env.example` + docker-compose + kurulum betiği birlikte ele alınmış. Parmak izi kontrolü gibi "anahtar döndürme unutulursa" senaryosunu düşünen ek önlem eklenmiş.

4. **API tarafı ihmal edilmemiş.** Controller'lar, SignalR hub ve minimal API endpoint'leri (admin backfill) ayrıca korunmuş.

5. **Derlenebilirlik korunmuş.** 27 dosyalık değişiklik seti derleniyor.

### İlk denetim turunda açık kalanlar — sonraki düzeltme notlarıyla güncellendi

1. **K-1 kod düzeltmesi üçüncü turda yapıldı.** Simetrik imza sırrı hem masaüstü imzalayıcıdan hem Web'den kaldırıldı; RSA-PSS + gömülü açık anahtar geçişi yapıldı, demo imzası ayrıştırıldı, Web üretimi `410 Gone` ile kapatıldı. Müşteri lisans envanteri, `v2:` yeniden basımı, legacy kabul kesim tarihi ve özel anahtarın şirket içi sahipliği henüz tamamlanmadı.

2. **Parola karmaşıklığı ilk denetimde yoktu.** Sonraki düzeltmede büyük/küçük harf, rakam ve sembol koşulları eklendi; yeni politika K-6 takip kaydında açıklandı.

3. **Süreli kilit ilk denetimde çalışmıyordu.** Sonraki düzeltmede 15 dakikalık bitiş DB'de saklanır hale geldi; SQLite/PostgreSQL şema yükseltmesi ve giriş akışı runtime doğrulaması bekliyor (R-4).

4. **Yüksek öncelikli 11 maddenin tamamı hâlâ dokunulmadı.** Özellikle muhasebe/fatura yollarındaki boş `catch` blokları (veri bütünlüğü), Swagger'ın açık olması ve LucaPortal'da düz metin saklanan parola (Y-3).

5. **Test altyapısı hâlâ eksik.** Test aracı sertleştirildi ama `MKFiloServis.Tests` projesi yok; CI'daki test proje yolu ayrıca düzeltilmeli. Otomatik kalite güvencesi bu nedenle yetersiz.

### Öncelik sırası önerisi (dördüncü tur — 2026-10-02)

Bu turda Y-6, Y-11 ve O-10 kapandı, R-6'nın görünürlük riski kapatıldı ve Y-9'daki gerçek retry hatası düzeltildi. Kalan iş, ağırlıkla **doğrulama** gerektiren operasyonel adımlar:

```
1. K-1 geçiş   → Müşteri lisans envanteri, v2: yeniden basımı, legacy kesim tarihi,
                  özel anahtarın şirket içi sahipliği/yedeği (kod kapalı, operasyon açık)
2. Y-7         → Gerçek MKFiloServis.Tests projesini depoya geri al; CI yolu düzeldi
                  ama test paketi olmadan kalite güvencesi ölçülmüyor
3. R-3         → Inno Setup müşteri paketini üretip payload içeriğini doğrula
4. K-6/R-4     → Parola-kilit akışlarını gerçek SQLite/PostgreSQL'de uçtan uca doğrula
5. R-6         → Yetkilendirme envanterini CI'a bağla: korumasız yeni uç eklendiğinde
                  derlemeyi kırsın (bugün yalnızca loglayıp uyarıyor)
6. R-8         → Silinmiş analiz belgesinin kayıt altına alınması gereken karar
7. Y-1/Y-2/O-6 → Güvenilir kuyruk/işlem garantisi, belge ayrıştırma, yedekten geri yükleme
8. O-12        → Geçmişte kalan parolaların rotasyonu ve Git geçmişi temizliği
```

---

## 7. İlk denetim anındaki madde sayımı

> Aşağıdaki sayılar ilk denetimdeki statik değerlendirmeyi gösterir; devam düzeltmelerinin güncel kapanış sayısı değildir. Güncel uygulama ve doğrulama durumu Bölüm 8–10'dadır.

| Ölçüt | Önceki denetim | Şimdi |
|---|---:|---:|
| Toplam madde | 39 | 39 |
| Tamamen düzeltilen | 1 | **8** |
| Kısmen ilerleyen | 5 | **8** |
| Değişiklik olmayan | 22 | **19** |
| Tamamlanan faz | 0 / 7 | **0 / 7** (Faz 1 büyük ölçüde tamamlandı, kabul kanıtı bekliyor) |
| Derleme | Doğrulanmamıştı | ✅ **0 hata, 0 uyarı** (`-m:1`) |
| Açık yeni risk | 1 | **0** (R-3 kapandı) + 3 uyarı düzeyi (R-6, R-7 geçiş işi, R-8 belge) |

**İlk denetim anındaki Faz 1 durumu:** K-2, K-3, K-4, K-5 kapandı; K-1 açık; K-6 kısmiydı. K-1 kod düzeltmesi üçüncü turda yapıldı; K-6/R-4 uygulama değişiklikleri Bölüm 9'da, K-1 geçiş kararı Bölüm 11-12'de kayıtlıdır. Faz 1'in kabul ölçütleri ve runtime doğrulamaları tamamlanmadan faz kapatılmış sayılmaz.

---

*Üçüncü turda `dotnet build MKFiloServis.slnx -c Debug -m:1` canlı çalıştırıldı: **0 uyarı, 0 hata**. K-1 ve R-3 kod değişiklikleri kaynak kod okumasıyla incelendi; müşteri lisans geçişi, kurulum paketi ve lisans imza/kilit akışları çalışma zamanında doğrulanmadı.*

## 8. Commit sonrası envanter ve durum (2026-10-02)

- `04342dc3` commit'i `origin/main` ile eşleşiyor; bu rapordaki güvenlik düzeltmeleri GitHub'a gönderilmiş durumda.
- Commit'te 38 dosya yer aldı. `MKFiloServis.Web/backup_settings.json`, `.kilo/` ve `docs/analiz/Rent-a-Car-Modulu-Analiz-Raporu.md` yerel çalışma ağacı değişiklikleri commit dışında kaldı.
- `git ls-files` envanterinde `bin/obj` dizinleri ve `.log` dosyaları bulunmadı; `test_all.txt` ise izleniyor. Dosyanın amacı ve içeriği gözden geçirilmeden depodan çıkarılması önerilmiyor.
- Geçiş manifestleri için önceki “izlenmiyor” notu hatalıydı; 13 dosya izleniyordu. R-7 devam düzeltmesinde bağlantı dizeleri çalışma ağacından çıkarıldı ve `App_Data/db-transitions` ignore edildi. Eski Git nesneleri ve yerel ignored publish kopyaları bu işlemle değişmez.
- Önceki bölümdeki tam çözüm derlemesi, ilk denetim turunda alınan çıktıdır. Bu devam düzeltmesinde `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj -c Debug --no-restore` başarılı oldu (0 uyarı, 0 hata); PowerShell build script'i parse edildi. Inno Setup paketi üretilmedi ve çalışma zamanı testi yapılmadı.
- Açık yayın engelleri: **K-1'in kod tarafı kapandı**, geriye operasyonel geçiş kaldı — müşteri lisans envanteri, `v2:` yeniden basımı, legacy kabul kesim tarihi ve özel anahtarın şirket içi sahipliği/yedeği. Varsayılan legacy reddi mevcut kurulumları etkileyebileceğinden bu adımlar tamamlanmadan müşteri güncellemesi yayınlanmamalı. R-3 için paketleme düzeltmesinin gerçek kurulum çıktısında doğrulanması. K-6/R-4 kod düzeltmeleri uygulandı ve çözüm derlendi; SQLite/PostgreSQL çalışma zamanı geçişleri ile kilit/parola akışlarının uçtan uca doğrulaması bekliyor. Commit'in GitHub'a gönderilmiş olması satışa hazır olma onayı değildir.

## 9. Hesap güvenliği devam düzeltmesi (2026-10-02)

- K-6: yeni parolalarda en az 12 karakter, büyük/küçük harf, rakam ve sembol şartı açıldı. Bu kurallar kullanıcı oluşturma ve parola değiştirme akışlarına uygulanıyor; yönetici parola sıfırlama akışına da Identity validator'ları açıkça çağrılarak eklendi. E-posta ile üretilen geçici parola artık 16 karakter ve dört sınıfı da içeriyor.
- R-4: süreli kilit için `KilitlenmeBitisUtc` alanı eklendi. `DbInitializer` SQLite ve PostgreSQL için eski `Kullanicilar` tablolarına kolonu ekliyor. Yeni 5 deneme kilidi 15 dakika sonra girişte kendiliğinden açılır; eski bitiş tarihi olmayan kilitler yönetici açana kadar kilitli kalır.

### Devam düzeltmesi — R-5 API varsayılan koruması (2026-10-02)

`GuzergahlarController` tüm action'ları sınıf seviyesinde JWT Bearer ile koruyor. Excel importu için Admin rolü ayrıca zorunlu. `JwtBearerDefaults.AuthenticationScheme` kullanıldı. `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj -c Debug --no-restore` başarılı oldu (0 uyarı, 0 hata). Endpoint çalışma zamanı senaryoları ayrıca doğrulanmalı.

## 10. Güncel düzeltme ve doğrulama özeti (2026-10-02)

| Madde | Uygulanan düzeltme | Güncel kanıt / kalan doğrulama |
|---|---|---|
| **K-1 lisans imzası** ✅ | Simetrik `SECRET` iki projeden de kaldırıldı; imzalayıcı RSA-PSS + harici özel anahtar; Web gömülü public key ile doğrular; demo imzası DataProtection'a ayrıldı; legacy yalnız süreli env geçişinde; Web üretimi `410 Gone` | Kaynak kod ve tam çözüm derlemesiyle doğrulandı. **Kalan:** müşteri lisans envanteri, `v2:` yeniden basımı, legacy kesim tarihi, özel anahtar sahipliği/yedeği, gerçek kurulum testi. |
| K-6 parola politikası | Yeni parolalarda 12 karakter ve büyük/küçük harf, rakam, sembol şartı; parola sıfırlama doğrulaması; güvenli geçici parola üretimi | Önceki güncelleme anında legacy SHA-256 geçişi açıktı. Bölüm 11'de bu hash'in başarılı girişte Identity V3 PBKDF2/310.000 yinelemeye yükseltilmesi eklendi. Gerçek kullanıcı/parola akışları runtime doğrulaması bekliyor. |
| R-4 kilit süresi | Beş başarısız denemede DB'de 15 dakikalık `KilitlenmeBitisUtc`; SQLite ve PostgreSQL şema tamamlama | Derleme başarılı. Eski DB yükseltme ve kilit açılma senaryoları iki sağlayıcıda da izole kopyada doğrulanmalı. |
| R-3 paketleme ✅ | Normal müşteri kurulumundan lisans üretim aracını çıkartma; dahili build'e açık opt-in; lisans yönetim UI/API'sini Admin ile sınırlama; Web üretimi kapatıldı | Betik/statik kontrol ve tam çözüm derlemesi yapıldı. Inno Setup müşteri paketleri oluşturulup payload içeriği henüz incelenmedi. |
| R-5 API koruması | `GuzergahlarController` sınıf seviyesinde `JwtBearerDefaults.AuthenticationScheme`; import action'ında Admin rolü | Web Debug build başarılı (0 uyarı/0 hata). Anonim isteğin reddi, geçerli Bearer isteğinin kabulü ve import rol senaryosu runtime test edilmedi. |
| R-6 controller/minimal API varsayılanı | `MapControllers().RequireAuthorization(Bearer)` tüm MVC action'larına uygulanır; anonim action'lar `[AllowAnonymous]`; minimal uçlar açıkça korunur. **Startup yetkilendirme envanteri eklendi:** her açılışta tüm uçlar taranır, yetkilendirme metadatası taşımayanlar uyarı olarak listelenir. `/healthz` ve `/readyz` artık bilinçli olarak `.AllowAnonymous()` ile işaretli. | **Canlı doğrulama yapıldı:** uygulama çalıştırıldı, `762 uç tarandı, 2 uç yetkilendirme metadatası taşımıyor` — kalan iki uç framework'e ait (`/_framework/opaque-redirect`, statik dosya yakalayıcı `{**path:file}`). Yani korumasız **uygulama** ucu kalmadı; yeni bir uç `.RequireAuthorization()` almayı unutursa her startup'ta görünür olacak. Küresel fallback policy **eklenmedi**: `DefaultScheme` JWT Bearer olduğu için fallback Blazor devrelerinin cookie kimlik doğrulamasını ve statik varlıkları da kapsar, uygulamayı kırardı. HTTP anonim/izinli istek senaryoları hâlâ uçtan uca denenmedi. |
| R-7 manifest sırları | Yeni manifestlerde connection string yazılmıyor; 13 izlenen manifestten alanlar kaldırıldı; runtime klasörü ignore edildi | JSON sözdizimi 13 dosyada kontrol edildi, connection string alanı kalmadı. Önceki parolaların aktif olup olmadığı bilinmediğinden DB credential rotasyonu ve eski Git geçmişi temizliği ayrıca ele alınmalı. Ignore edilmiş publish/temp kopyaları yerinde bırakıldı. |

**Bu güncellemede çalıştırılan doğrulama:** Üçüncü turda `dotnet build MKFiloServis.slnx -c Debug -m:1` → **0 uyarı, 0 hata**. K-1'in statik doğrulaması yapıldı (gömülü public key, `410 Gone`, harici özel anahtar, 14 gün tolerans, `Licensing` bölümü yok → legacy kapalı). R-7 kapsamındaki 13 JSON tekrar okunup parse edildi. Otomatik test, DB runtime yükseltmesi, HTTP endpoint testi veya Inno Setup paket üretimi yapılmadı. Bu rapor satışa hazır olma onayı değildir.

## 11. K-1 ve K-6 devamı (2026-10-02)

> Bu bölüm önceki turun durumunu kaydeder. K-1'in sonraki uygulama değişiklikleri Bölüm 12'de yer alır.

### K-6 — legacy parola hash'inin güvenli yükseltilmesi

- Legacy SHA-256 karşılaştırması artık sabit zamanlı yapılıyor; yanlış biçimli Base64 hash güvenli biçimde reddediliyor.
- Girişte `UserManager.CheckPasswordAsync` rehash'i ayrı DbContext'te kaydedebiliyor; `KullaniciService` sonradan eski izlenen entity'yi kaydettiğinde bu hash yükseltmesi geri alınabiliyordu. Giriş akışı artık hasher doğrulamasını doğrudan yapıyor ve `SuccessRehashNeeded` sonucunda aynı izlenen kullanıcı nesnesini Identity V3 hash'ine yükseltiyor. Normal ve 2FA giriş başarı kayıtları yeni hash'i kalıcılaştırıyor.
- Identity hasher açıkça `IdentityV3` ve 310.000 PBKDF2 yinelemesine ayarlandı. Daha düşük yineleme sayısıyla saklanan geçerli Identity V3 hash'leri başarılı doğrulamadan sonra Identity hasher'ın `SuccessRehashNeeded` sonucu ile yükseltilir.
- Bu yapılandırmanın standart Identity V3 parola saklaması olduğu not edilmelidir; parola hash'leri için özel algoritma veya şifreli geri döndürülebilir saklama eklenmedi.
- Eski SHA-256 kullanıcı, Identity V2/V3 kullanıcı, düşük yineleme hash'i, hatalı parola ve 2FA yolları gerçek SQLite/PostgreSQL DB'lerinde denenmedi; K-6 runtime doğrulaması açık.

### K-1 — imza anahtarı geçiş kararı

- K-1 henüz kodla kapatılmadı. İmza sırrı Web'de legacy lisans doğrulama ve yerel demo lisansı oluşturma için de kullanılıyor; yalnız masaüstü imzalayıcıyı paketten çıkarmak anahtarı Web'den kaldırmaz. `LicenseController` ve Admin ekranı da legacy imza üretebiliyor.
- Yeni lisans formatı RSA/ECDSA ile sürümlenmeli: özel anahtar yalnız şirket içi imzalayıcıda tutulmalı, Web yalnız public key doğrulamalı. Demo lisansı üretimi de ticari özel anahtarı müşteri uygulamasına koymayan ayrı, kısıtlı bir mekanizmaya geçirilmeli. Mevcut imzalı lisanslar için son kullanma/yenileme envanteri ve kesim tarihi belirlenmeden legacy doğrulamayı kaldırmak müşterilerin girişini lisans nedeniyle engelleyebilir.
- Uygulanacak geçiş sırası: (1) aktif müşteri lisansları ve bitiş tarihleri envanteri; (2) şirket içi private key sahibinin belirlenmesi ve anahtarın imzalayıcı makinede Windows sertifika deposu/erişim ACL'siyle kurulması; (3) imza sürüm alanı ve canonical payload tanımı; (4) Web'de yalnız public-key doğrulaması, demo üretiminin ayrıştırılması ve müşteri lisanslarının yeniden basımı; (5) müşterilere duyurulmuş kesim tarihinde legacy doğrulama/simetrik anahtarın Web ve masaüstü kaynaklarından kaldırılması. Lisans envanteri, anahtar sorumlusu ve kesim tarihi belirlenmeden bu turda mevcut lisans doğrulaması değiştirilmedi.
- Bundan sonra Web içinden legacy lisans üretim yolları da kaldırılmalı; mevcut iç üretim aracı opt-in olsa da özel anahtarı uygulama binary'sine gömmemeli. Müşteri setup'ından çıkarılmış olması yalnız R-3 riskini azaltır, K-1'i çözmez.

**Bu tur doğrulaması (ikinci tur kaydı):** `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj -c Debug --no-restore` — başarılı, 0 uyarı/0 hata. Otomatik test veya runtime parola doğrulaması çalıştırılmadı. Bu turda K-1'e dokunulmadı; üçüncü turdaki kod düzeltmesi ve açık dağıtım işleri Bölüm 12'dedir.

## 12. K-1 lisans imza düzeltmesi (2026-10-02)

| İş | Güncel durum |
|---|---|
| Ticari lisans imzası | Masaüstü üretici `v2:` önekli RSA-PSS/SHA-256 imzası üretir. Alanlar uzunluk belirten sabit bir biçimde imzalanır. Web yalnız kaynak kodda bulunan açık anahtarla doğrular. Ortak imza sırrı iki projeden çıkarıldı. |
| Özel anahtar | Bu geliştirme bilgisayarında kullanıcı profili altında, repo dışında `AppData\Local\MKFiloServis\licensing\license-signing-private.pem` dosyası oluşturuldu. Dosya ACL'si kullanıcı, SYSTEM ve yerel yöneticilerle sınırlandı. Müşteri paketi ve Git kapsamına alınmadı. Şirket içi sorumlu tarafından güvenli yedeği ve erişim yönetimi yapılmalı. |
| Web üretimi | `/api/license/generate` artık `410 Gone` döner; Admin üretim ekranı şirket içi araca yönlendirir. `/admin/license` yalnız imzalı anahtar kabul eder. `SaveLicenseAsync` geçersiz imzayı otomatik düzeltmez. Eski, kullanılmayan `LisansService` ve arayüzündeki gömülü AES anahtarı kaldırıldı. |
| Geliştirici bypass'ı | Sabit `DevOverrideKey`, makine/kullanıcı eşleşmesi ve `DevSettings:OverrideKey` ayarı kaldırıldı. Tüm ortamlarda `ValidateAsync()` aynı imza doğrulama yolunu kullanır. |
| Demo | Yeni yerel demo imzası ASP.NET Data Protection ile üretilir; ticari özel anahtarı kullanmaz. |
| Eski lisans geçişi | Eski imzalar varsayılan olarak reddedilir. Kurulum sahibi `Licensing:LegacyVerificationSecret` ve `Licensing:LegacyAcceptUntilUtc` değerlerini yalnız dış ortamda tanımlayarak geçici doğrulama açabilir; eski şifreli format için ayrıca `Licensing:LegacyEncryptionKey` gerekir. Geçiş bitince bu ayarlar kaldırılır. Eski sırrın Git geçmişinde bulunması nedeniyle bu geçiş sırasında eski format taklit edilebilir. |
| Süre bitimi | Önceden doğrulanmış ticari lisansa bitişten sonra en fazla 14 gün yenileme toleransı verilir. Demo kapsam dışıdır; son doğrulama zamanından geriye alınmış saat 5 dakika tolerans dışında reddedilir. |

**Dağıtım kapısı:** Mevcut müşteri lisansları envanterlenip yeni `v2:` lisansları basılmadı. İmzalayıcı özel anahtarının şirket içi sahipliği ve güvenli yedeği belirlenmeli; eski lisans kabulü için son tarih duyurulmalı. Varsayılan eski lisans reddi mevcut kurulumları etkileyebileceğinden müşteri güncellemesi bu adımlar tamamlanmadan yayınlanmamalı. K-1 kod düzeltmesi uygulanmıştır; gerçek kurulum ve geçiş kabulü beklemektedir.

Geçiş adımları ve kanonik imza payload'ı için [Lisans imzası v2 geçiş belgesi](LISANS-IMZA-GECIS.md) eklendi. Bu belge prosedürü açıklar; müşteri envanterinin, yeniden basımın ve kesim tarihinin tamamlandığına dair kanıt değildir.

**Doğrulama:** LisansDesktop Debug `--no-restore` ve Web Debug `--no-restore -p:UseAppHost=false` derlemeleri başarılı (0 uyarı, 0 hata). Yerel özel ve açık anahtar çiftiyle RSA-PSS imzası doğrulandı; değiştirilmiş içerik reddedildi. Eski lisans geçişi, demo ve 14 günlük tolerans çalışma zamanında henüz denenmedi.

**Yerel lisans uyarısı ve geçiş (2026-10-02):** `Lisans imzasi gecersiz` uyarısı incelendi. Etkin `App_Data/test.db` kaydının 15 gün süreli ve “DEMO FIRMASI” adlı olmasına rağmen `IsDemo=false` olduğu görüldü; bu nedenle otomatik demo istisnası güvenli değildi ve uygulanmadı. Önceden doğrulanmış aktif kaydın bütünlük dosyasıyla eşleşmesi, bitiş tarihi ve yerel şirket içi özel anahtar/açık anahtar eşleşmesi kontrol edildi. Yalnız bu etkin kaydın imzası aynı lisans alanlarıyla RSA-PSS `v2:` olarak yenilendi; bitiş tarihi 2026-10-16 olarak korundu. İşlem öncesi veritabanı ve `license.hash` geri alma kopyaları `%LOCALAPPDATA%\MKFiloServis\license-migration\20261002-123853` dizinine alındı. Başka veritabanları değiştirilmedi. Eski biçimli diğer kayıtlar hâlâ reddedilir; uygulama artık bu durumda yeniden imzalanmış lisans yükleme gereğini açıkça bildirir. Çalışan uygulama üzerinde yeniden başlatma sonrası kabulü henüz doğrulanmadı.

**Üçüncü tur ek denetimi (2026-10-02):**
- `MainForm.cs` içinde `SECRET` sabitinin **kaldırıldığı** ve imzalayıcının programın kendi şifreli anahtar deposuyla RSA-PSS kullandığı kaynak kodla doğrulandı.
- `LicenseService.cs:38-39` gömülü `PublicKeyPem` ve `:154-160` `v2:` RSA-PSS doğrulaması bulundu; Web'de özel anahtar kalmadığı doğrulandı.
- `LicenseController/generate` → `410 Gone`; `LicenseGenerate.razor.cs` silinmiş. Web üzerinden üretim yolu kapalı.
- `:435-439` 14 günlük yenileme toleransı doğrulandı (yalnız önceden doğrulanmış ticari lisanslar için).
- Özel anahtar dosyasının (`%LOCALAPPDATA%\MKFiloServis\licensing\license-signing-private.pem`) bu makinede mevcut olduğu doğrulandı. `.gitignore` içine `*.pem` ve `*private*.key` kuralları eklendi; özel anahtar repo dışında tutuluyor.
- Hiçbir `appsettings*.json` içinde `Licensing` bölümü bulunmadığı doğrulandı → legacy doğrulama varsayılan olarak **kapalı (fail-closed)**.

## 13. İlk kurulum ve API erişim güncellemesi (2026-10-02)

### İlk yönetici kurulumu

- Sabit `admin/admin123` ve yönetici rolündeki `test/test123` oluşturma yolları kaldırıldı; başlangıçta kilitli/pasif admin veya test hesabı yeniden etkinleştirilmiyor.
- `SeedAdminAsync`, kullanıcı tablosu boşsa yalnızca açıkça yapılandırılan `INITIAL_ADMIN_USERNAME` ve `INITIAL_ADMIN_PASSWORD` ile yönetici oluşturur. Parola en az 12 karakter olmalı; eksik yapılandırmada bilinen varsayılanla hesap yaratılmaz.
- Alternatif `/setup` akışı `MKFILOSERVIS_SETUP_BOOTSTRAP_TOKEN` ortam değişkeni en az 32 UTF-8 baytı değilse kapalıdır. Token sabit zamanlı karşılaştırılır; veritabanı durumu doğrulanamazsa kurulum devam etmez.
- Sihirbaz `Kullanicilar` tablosunu tamamlanma ölçütü olarak kullanır. Kullanıcı mevcutsa kilitlenmeyi önlemek için eski kurulum yeniden açılmaz. Oluşturma adımları Serializable transaction içindedir; seed edilmiş varsayılan firma/roller varsa tekrar eklemek yerine mevcut kayıtlar kullanılır. Kurulum parolası en az 12 karakterdir.
- `INITIAL_ADMIN_*` ve `/setup` bootstrap yolu aynı boş veritabanında birlikte iki hesap oluşturmaz: başlangıç seed'i önce hesap oluşturursa sihirbazı kullanıcı kontrolü kapatır. Hangi yolun kullanılacağı kurulum yapılandırmasında seçilmelidir; secret değerleri rapora yazılmamalıdır.

### API ve SignalR şeması

- `AuthController` Bearer ile korunur; `POST /api/auth/login` açık `[AllowAnonymous]` istisnasıdır. `refresh` ve `verify` sınıf seviyesindeki Bearer şartını devralır.
- `EvrakHub` sınıfı açıkça JWT Bearer ister. `MapControllers()` Bearer `RequireAuthorization` uygular. Güzergâh controller'ı sınıf seviyesinde Bearer korumalıdır; Excel import action'ında Admin rolü de gerekir.
- Blazor sayfa yetkisi `@attribute [Authorize]` ve özel `AppAuthenticationStateProvider` üzerinden; HTTP API yetkisi JWT Bearer üzerinden yürür. Cookie handler eklenmedi. Circuit geri yükleme ve çıkışın, rol/hesap devre dışı bırakılmasının mevcut devreye etkisi runtime kabulü bekler.
- MVC için map seviyesinde Bearer varsayılanı vardır; global ASP.NET Core `FallbackPolicy` yoktur. Bu nedenle yeni minimal API uçları açıkça `.RequireAuthorization(...)` almalıdır. İki development backfill ucu Admin+Bearer ile korunmuştur.

### Son ekin doğrulama sınırı

Bu rapor güncellemesine konu olan son bootstrap/API nitelik değişikliklerinden sonra build, otomatik test, HTTP isteği veya circuit runtime senaryosu çalıştırılmadı. Çalışma ağacında yalnız `git diff --check` kullanıldı. Önceki bölümlerde kayıtlı build sonuçları kendi tarihsel değişiklik kesitlerine aittir; bu ekin derleme kanıtı olarak yorumlanmamalıdır. Faz 1 kapanışı ve satış onayı için anonim login'in çalıştığı, korumalı uçların reddettiği/kabul ettiği, kurulumun yalnız token ile bir kez tamamlandığı ve Blazor yenileme/çıkış senaryolarının üretim benzeri ortamda doğrulanması gerekir.

## 14. Son çalışma ağacı düzeltmeleri ve durum güncellemesi (2026-10-02)

> Bu bölüm önceki uygulama turunun kaydıdır. Bölüm 18 ve Bölüm 3 güncel durumu gösterir.

Bu bölüm, önceki rapor kesitinden sonra çalışma ağacında görülen değişiklikleri kaydeder. Durum tablosundaki Y/O maddeleri bu kanıta göre güncellendi. Kod okumaları statik incelemedir; son değişikliklerden sonra derleme, otomatik test veya çalışma zamanı senaryosu çalıştırılmadı.

### Yüksek öncelikli maddeler

| Madde | Yapılan değişiklik | Kalan risk / doğrulama |
|---|---|---|
| **Y-1 hata görünürlüğü** | Muhasebe stok hareketi, puantaj/fatura, audit, başlangıç verisi ve scraper yollarındaki bazı yakalamalara log eklendi. | Bazı hatalar loglandıktan sonra ana işlem sürüyor; stok ve audit tutarsızlığı oluşabilir. Boş `catch` blokları da bulunuyor. Loglama düzeltmesi hatayı telafi veya işlemi atomik hale getirmez. |
| **Y-2 eksik özellikler** | Proforma PDF dışa aktarımı ve Luca belge detay ayrıştırma artık açık hata loglayıp desteklenmediğini belirtiyor. | Özellikler hâlâ uygulanmamış; tüketici akışlarının bu hatayı uygun biçimde gösterdiği runtime'da doğrulanmalı. |
| **Y-3 Luca kimlik bilgileri** | Portal parolası ASP.NET Data Protection ile `dp:v1:` önekiyle korunuyor; giriş ve kimlik doğrulama URL'lerinde HTTPS zorunlu. | Daha önce saklanmış düz metin değerler yüklenebiliyor ve sonraki ayar kaydına kadar dönüştürülmüyor. Koruma anahtarlarının yedek/erişim yönetimi ve eski kayıtların geçişi doğrulanmalı. |
| **Y-5 Swagger** | Swagger varsayılan olarak yalnız Development ortamında açık; diğer ortamlarda `MKFILOSERVIS_ENABLE_SWAGGER=true` gerekir. | Bu opt-in ile üretimde açıldığında UI için ayrıca yetkilendirme uygulanmıyor; erişim ağı/kimlik doğrulaması ayrıca ele alınmalı. |
| 🟢 **Y-6 EF uyarıları (sonraki güncellemede tamamlandı; Bölüm 17)** | `PendingModelChangesWarning` bastırılmak yerine loglanıyor. | Diğer EF uyarılarından ikisi hâlâ ignore ediliyor; model farkında fail-closed davranış yok. |
| **Y-7 CI/test** | Workflow artık `MKFiloServis.slnx` restore/build yapıyor. | Test projesi yoksa workflow test, coverage ve raporlama adımlarını uyarıyla atlıyor. Bu nedenle CI şu anda test kapsamını kanıtlamıyor. README'de bu durum açıklanmış. |
| **Y-8 async bekleme** | Arşiv migrasyonu async/cancellation-token akışına taşındı; Playwright tarama durdurma ve silinmiş lisans servisindeki bazı bloklayan yollar kaldırıldı. | Selenium scraper içinde `Thread.Sleep(300)` ve PlaywrightSmoke yardımcı kodunda `.GetAwaiter().GetResult()` kaldı. Kalan üretim çağrı zincirleri taranmalı. |
| **Y-9 HTTP dayanıklılığı** | Kullanılmayan Polly bağımlılığı kaldırıldı. Webhook, Teams ve Slack isimli HTTP istemcilerine sınırlı retry handler eklendi (408/429/5xx, ağ hatası/zaman aşımı; en fazla iki tekrar). Luca adresleri HTTPS ile sınırlandırıldı. | Retry kapsamı yalnız seçili istemciler; tüm dış entegrasyonlar için TLS/retry politikası ve tekrar güvenliği doğrulanmadı. |

### Orta öncelikli maddeler

| Madde | Yapılan değişiklik | Kalan risk / doğrulama |
|---|---|---|
| **O-1 canonical sağlayıcı** | `CanonicalProvider` ayarı ve resolver desteği eklendi. | Settings service ayarı PostgreSQL'e sabitliyor; alternatif canonical provider seçimi uçtan uca çalışmıyor. Çok sağlayıcılı migration ve mevcut DB'lerden geçiş doğrulanmadı. |
| **O-2 firma bağlamı** | Denetim, Recovery ve Puantaj Excel ekranlarındaki `firmaId=1` varsayılanları kaldırıldı; seçim yoksa işlem durduruluyor. | Bu değişiklikler yalnız ilgili akışları kapsar; diğer sabit firma varsayılanları ve tüm tenant sınırları denetlenmedi. |
| **O-3 audit kaydı** | Audit kaydı yazımındaki yakalama artık log üretiyor. | Audit hatası asıl işlemi geri almıyor ve ayrı güvenilir teslim/alarmlama kuyruğu yok; denetim izi kaybı mümkün. |
| **O-6 yedekten geri yükleme** | Desteklenmeyen restore çağrısı loglayıp `NotSupportedException` fırlatıyor. | Geri yükleme özelliği uygulanmış değil; kullanıcıya/operatöre yansıyan hata akışı doğrulanmalı. |
| **O-7 DataSync şema farkı** | PostgreSQL→SQLite ve SQLite→PostgreSQL aktarımı, kaynak tablolar hedefte eksikse tablo adlarını raporlayıp duruyor. | Foreign key'ler kapatılıyor; aktarım sonrası satır sayısı, FK ve bütünlük doğrulaması görünmüyor. |
| 🟢 **O-10 dokümantasyon (sonraki güncellemede tamamlandı; Bölüm 17)** | README proje ağacı mevcut projelerle uyumlu hale getirildi ve test projesinin yokluğu belirtildi. | Diğer kurulum, dağıtım ve mimari belgelerin güncelliği ayrıca taranmalı. |
| **O-12 sabit DB parolaları** | DataSync örneği placeholder'a çevrildi; ortak `DatabaseSettings` varsayılan parolası boş; migration betikleri `MKFILO_PG_PASSWORD` ister. | Bu, raporda adı geçen kaynak noktalarını düzeltir; tüm repo/Git geçmişi taraması ve daha önce kullanılmış parolaların rotasyonu kanıtlanmadı. |
| **O-13 izlenen dosyalar/belgeler** | `test_all.txt` Git indeksinde silinmek üzere staged; eski denetim/plan/analiz belgelerinden birkaçının silinmesi de staged görünüyor. | Bu yalnız mevcut indeks/çalışma ağacı durumudur; commit edilmiş silme değildir. `test_all.txt` içeriği ve belgelerin silinme gerekçesi bu incelemede yeniden değerlendirilmedi; staged değişikliklere müdahale edilmedi. |

### Ek yayın ve kabul notları

- Yedekten geri yükleme, PDF üretimi ve Luca belge ayrıştırma özellikleri tamamlanmış sayılmaz; artık daha görünür biçimde başarısız olmaları uygulandıkları anlamına gelmez.
- CI çözümü derlemeye yöneltilmiştir; test projesi bulunmadığı için bu, satış kabul testlerinin yerine geçmez.
- Son çalışma ağacı ayrıca deployment, ortak modeller ve entegrasyon istemcilerinde değişiklik içeriyor. Bu ek, değişikliklerin statik incelemesini özetler; çözüm build'i veya müşteri paketi doğrulaması değildir.
- Yayın öncesi en azından tüm çözüm build'i, gerçek SQLite/PostgreSQL migration/aktarımı, kimlik doğrulama ve kurulum HTTP akışları, paket içeriği, sır rotasyonu ve log/audit hata senaryoları doğrulanmalıdır. Mevcut derleme kayıtları önceki kod kesitlerine aittir.

## 15. Lisans geçiş belgesi ve müşteri paketleme güncellemesi (2026-10-02)

### Lisans geçiş belgesi

- `docs/LISANS-IMZA-GECIS.md` eklendi. Belge v2 payload alanlarının sırasını/biçimini, özel anahtarın repo ve müşteri kurulumları dışında tutulmasını, şirket içi imza akışını ve mevcut müşteri lisanslarının taşınma sırasını tanımlıyor.
- Eski lisansların geçiş ayarı yoksa reddedileceği ve müşteriler yeni lisansları yükleyene kadar güncelleme yapılmaması gerektiği açıkça belirtiliyor. Geliştirme `test.db` için yapılan yerel imza yenilemesinin diğer kurulumları taşımadığı da kayda alınıyor.
- Bu belge operasyon planıdır; müşteri lisans envanteri, yeni lisansların teslimi, anahtar yedeğinin sahipliği ve legacy kesim tarihi tamamlanmış değil. K-1 bu nedenle kod düzeltmesi uygulanmış, dağıtım geçişi açık durumunda kalır.

### Kurulum paketinden lisans aracının çıkarılması

- `build-client.ps1` sürüm özetini lisans aracı olmadan kurulum ürettiğini belirtecek şekilde güncelliyor.
- `build.ps1` tam ve müşteri kurulumlarında lisans aracını varsayılan olarak üretmiyor. Araç yalnızca `-LisansOnly` veya `-IncludeInternalLicenseTool` ile isteniyor; eski `LisansDesktop` payload'ı normal akışta temizleniyor. Dahili araç `-SkipPublish` ile talep edilip publish payload'ı bulunmazsa script hata veriyor.
- `Setup.iss`, `GuncelleSetup.iss` ve `setup/README.md` müşteri/server paketlerinde aracın olmadığını ve dahili üretim anahtarlarını açıklıyor. `.gitignore` özel PEM/özel anahtar uzantılarını ve yerel test çıktısını da kapsıyor.
- Bu değişiklikler paket üretim kurallarını kaynakta düzeltir. Son değişiklikten sonra PowerShell/ISCC akışı çalıştırılmadı ve Inno Setup çıktısının içeriği denetlenmedi; R-3 için gerçek paket kanıtı hâlâ gerekli.

### Çalışma ağacı ve doğrulama sınırı

- Çalışma ağacında lisans imza geçiş belgesi yeni dosya olarak bulunuyor. `test_all.txt`, `MKFiloServis.Infrastructure/Data/ApplicationDbContext.cs` ve birkaç eski dokümanın silinmesi staged durumda; bunların henüz commit edildiği varsayılamaz. Raporlama sırasında bu değişikliklere müdahale edilmedi.
- Son değişiklikler için derleme, otomatik test, PowerShell build script'i çalıştırma veya kurulum paketi üretme yapılmadı. `git diff --check` yalnız rapor dosyası üzerinde biçim kontrolü için kullanılabilir; uygulama doğrulaması sayılmaz.
- K-1/K-2 erişim ve kurulum kabulü, R-3 gerçek paket incelemesi ve O-13 staged dosya silme gerekçeleri satışa çıkarım öncesi açık takip işidir.

## 16. K-1 geçiş hazırlığı denetimi (2026-10-02)

### Anahtar denetimi

- 🟢 **Doğrulandı:** `%LOCALAPPDATA%\MKFiloServis\licensing\license-signing-private.pem` mevcut (2.483 bayt). ACL izinli özneleri mevcut kullanıcı, SYSTEM ve yerel yöneticilerle sınırlı.
- 🟢 **Doğrulandı:** Özel anahtardan türetilen açık anahtar, Web'de gömülü `PublicKeyPem` ile eşleşiyor. Parmak izi: `9B73D25F1D5E0F4525D70E154BD8782CD03F96E11A6FE53349EB76A243BC2524`.
- 🟢 **Şifreli yedek ve geri açma doğrulandı:** Kullanıcının konum seçme yetkilendirmesiyle ACL korumalı yerel DPAPI yedeği ve ayrı USB disk üzerinde şifreli kopya oluşturuldu. Bayt eşitliği, açık anahtar eşleşmesi ve RSA-PSS doğrulandı; Bölüm 19. 🟡 Windows hesabı/profili kaybından bağımsız kurtarma açık.

### Yerel veritabanı taraması

Dört yerel SQLite dosyası salt okunur tarandı; firma, makine kodu ve lisans anahtarı değerleri rapora alınmadı.

| Yerel dosya | Kayıt | Etkin | Etkin imza durumu |
|---|---:|---:|---|
| `MKFiloServis.db` | 16 | 1 | 1 legacy |
| `App_Data/test.db` | 4 | 1 | 1 v2 |
| `MKFiloServis_yedek_20260801_190601.db` | 13 | 13 | 13 legacy |
| `MKFiloServis_Demo.db` | 2 | 1 | 1 legacy |

🟢 **Tarama tamamlandı:** Dört dosyada `demo:` imzalı kayıt bulunmadı. Bunlar yerel çalışma/yedek kopyalarıdır; aktif müşteri lisanslarının yetkili envanteri değildir. Eski ortak imza sırrının Git geçmişinde yer alması nedeniyle legacy doğrulamayı yeniden açmak taklit riskini geri getirir. Yeni sürümde legacy doğrulama env ayarları verilmediği sürece kapalıdır.

### Lisans üretim kaynağı adayı ve yeniden basım akışı

- 🟢 **Yerel kaynak bulundu:** `%LOCALAPPDATA%\MKFiloServis\licenses.db` salt okunur incelendi. 46 geçmiş kaydı var (43 Sale, 3 Renewal); 9 ayrı firma/makine eşleşmesi ve bu eşleşmelerin her biri için bugünden ileri bitiş tarihli en az bir kayıt mevcut. Tabloda imza, kurulum kabulü veya açık bir aktif/pasif lisans hakkı alanı yok; bu nedenle kaynak adaydır, müşteri hakkı doğrulaması değildir.
- 🟡 **Kod eklendi, doğrulama bekliyor:** Lisans Desktop tablosunun sağ tık menüsüne “Seçili Lisansı v2 Olarak Yeniden Bas” eylemi eklendi. İşlem lisans kaydındaki firma/makine/sürüm/bitişi korur, süresi geçmiş kaydı reddeder, daha ileri bitişli veya aynı bitiş tarihli v2 yeniden basımı varsa durur; yeni satış tutarı yazmaz ve `V2Reissue` geçmiş kaydı oluşturur. Operatörün yetkili lisans hakkını teyit ettiğine ilişkin onay verir ve anahtarı kopyalar.
- 🔴 **Açık:** Akış çözüm derlemesine dahil edildi; müşteri üzerinde çalıştırılmadı. 46 kayıt yetkili müşteri envanteriyle eşleştirilmedi, hiçbir müşteriye yeni anahtar basılıp teslim edilmedi. Üretilen lisans anahtarının müşteri kabul kaydı da ayrı tutulmalıdır.

### K-1 karar ve açık işler

- Lisans geçiş belgesine bu yerel envanter, anahtar eşleşmesi ve güvenli yedek kanıtının yokluğu eklendi: [LISANS-IMZA-GECIS.md](LISANS-IMZA-GECIS.md).
- Yerel DB kayıtlarını otomatik yeniden imzalamadım. Bunların müşteri/aktif lisans kaynağı olduğu doğrulanmadı; otomatik geçiş lisans hakkı, firma ve makine eşlemesini güvenceye almaz.
- Müşterilere lisans basıp teslim etmek, özel anahtar yedeğini şirketçe onaylı bir kasaya kopyalamak ve legacy kesim tarihi belirlemek için lisans haklarını teyit eden müşteri kaynağı, onaylı yedek hedefi ve sorumlu bilgisi gerekli. Yerel lisans üretim veritabanı tek başına müşteri teslim/aktif hak kanıtı sağlamıyor; kesim tarihi uydurulmadı ve legacy doğrulama etkinleştirilmedi.
- Bu yüzden K-1 kod tarafında uygulanmış olsa da müşteri geçişi ve operasyonel anahtar güvenliği **açık**. Yayın öncesi aktif müşteri envanteri, her müşteride yeni lisansın kabulü, onaylı yedek/geri yükleme provası ve duyurulmuş kesim planı tamamlanmalıdır.

**Doğrulama kapsamı:** Anahtar dosyası/ACL ve açık anahtar eşleşmesi kontrol edildi. SQLite veritabanları salt okunur sorgulandı. Yeniden basım kodu Bölüm 18'deki çözüm derlemesine dahil edildi; test çalıştırılmadı; lisans kaydı değiştirilmedi, anahtar üretilmedi, paket veya müşteri sunucusu işlemi yapılmadı.

---

## 17. Bağımlılık güvenliği, uç envanteri ve retry doğrulaması (2026-10-02)

Bu turda Y-6, Y-11 ve O-10 kapatıldı; R-6 uygulama uçları bakımından düzeltildi, ancak yeni uçların izlenmesi ve HTTP kabul testleri açık olduğundan madde kısmi durumdadır; Y-9'daki daha önceki turdan kalma **gerçek bir kod hatası** bulunup düzeltildi.

### Y-11 — NuGet güvenlik açığı bastırması kaldırıldı (✅)

`MKFiloServis.Web.csproj` ve `MKFiloServis.DataSync.csproj` içinde `GHSA-2m69-gcr7-jv3q` için `NuGetAuditSuppress` vardı. Bu, **CVE-2025-6965** (High/Critical): `SQLitePCLRaw.lib.e_sqlite3` 2.1.11'in gömülü SQLite'ının 3.50.2'nin altında olması ve "aggregate terim sayısı sütun sayısını aşınca bellek bozulması" ile sonuçlanması.

Bastırma, düzeltilmiş paket sürümü yayımlanmadığı dönemden kalmıştı. İnceleme sonucu:

- `SQLitePCLRaw.lib.e_sqlite3` için 2.1.13 (aynı sürüm hattında yama) ve 3.53.3 mevcut.
- `SQLitePCLRaw.bundle_e_sqlite3` ise ayrı bir sürüm hattında ve en fazla 3.0.5 — 3.53.3 oraya sabitlenemez.
- Düzeltilmiş sürümü doğrudan sabitlemek için **2.1.13** seçildi: aynı hat, API kırılması riski yok, SQLite 3.50.2+.

Her iki projeye `SQLitePCLRaw.lib.e_sqlite3` 2.1.13 eklendi ve iki `.csproj` içinden de `NuGetAuditSuppress` kaldırıldı.

**Kanıt:** `dotnet list package --vulnerable --include-transitive` artık *"Belirtilen projenin geçerli kaynaklarda güvenlik açığı olan paketi yok"* diyor. Runtime doğrulaması ayrı bir yazılımla yapıldı: `sqlite_version=3.53.3` (eşik 3.50.2) ve çoklu aggregate terimli sorgular (`count/sum/max/avg/total`, `group_concat`, altı terimli toplam ifadesi) sorunsuz çalıştı. Yani yalnız "build ediyor" değil, açığın kapatıldığı sürüm gerçekten yükleniyor.

### Y-6 — EF şema kayması uyarısı görünür hâle getirildi (✅)

`PendingModelChangesWarning` `Ignore` edilerek tamamen bastırılıyordu; model ile migration uyuşmazlığı fark edilmeden saklanıyordu. Artık `Ignore` yerine `Log(...)` kullanılıyor: başlatma akışı kırılmıyor ama şema kayması günlüğe düşüyor. Bilinen false-positive olan diğer iki EF uyarısı (query-filter + required navigation etkileşimi, çoklu collection include) bastırılmaya devam ediyor.

### O-10 — Derlemeye girmeyen ölü proje kopyası kaldırıldı (✅)

README'de `MKFiloServis.Infrastructure` ve `MKFiloServis.Service` projeleri varmış gibi listeleniyordu. Durum:

- `MKFiloServis.Infrastructure/Data/ApplicationDbContext.cs` Git tarafından **izleniyor**, ancak hiçbir `.csproj` içinde yer almıyor ve çözüme girmiyor. Üstelik **0 baytlık, boş** bir dosya (Haziran 2026 tarihli WIP commitinden kalma).
- `MKFiloServis.Service/` yalnızca izlenmeyen boş dizinlerden oluşuyor.

Bunlar `git rm` ile kaldırıldı ve README proje ağacı yalnızca gerçek, derlenen projeleri gösterecek şekilde düzeltildi. README'ye ayrıca çözümde birim test projesi bulunmadığı ve CI'ın bunu algılayacağı notu eklendi.

### Y-9 — Retry handler'daki gerçek hata (🟡 Kısmi, hata düzeltildi)

Önceki turda eklenen `ResilientHttpMessageHandler`, geçici hata sonrası **aynı `HttpRequestMessage` örneğini** tekrar gönderiyordu. `HttpClient` bir istek örneğini yalnızca bir kez gönderebilir; ikinci gönderim `InvalidOperationException` ("already sent") ile reddedilir. Bu istisna `catch (HttpRequestException)` bloklarına düşmediği için **ilk retry'de webhook çağrısını tümden başarısız kılıyordu** — yani dayanıklılık özelliği işe yaramıyor, tersine hatayı garanti ediyordu.

Düzeltmeler:

1. Gövde bir kez belleğe alınıyor; her deneme için **taze bir `HttpRequestMessage` klonu** oluşturuluyor (metot, URI, sürüm, istek başlıkları, içerik başlıkları ve `Options` kopyalanıyor).
2. POST çift gönderim riskine karşı `retryNonIdempotent` bayrağı eklendi. Kapalıyken (varsayılan) yalnız idempotent metotlar (GET/HEAD/OPTIONS/PUT/DELETE/TRACE) tekrar deniyor. Webhook/Teams/Slack istemcileri bunu bilinçli olarak **açık** kaydetti; açıkken bile POST yalnızca isteğin sunucuya ulaşmadığı kesin hatalarda (DNS çözülememesi, bağlantı reddi, ağ erişilemez) tekrar deniyor. Zaman aşımı ve bağlantı sıfırlanması belirsiz sayılıp tekrar **edilmiyor**.
3. Deneme kopyaları doğru şekilde dispose ediliyor; başarılı yanıtın yaşam döngüsü çağırana bırakılıyor.

**Doğrulama:** Beş senaryolu bir çalıştırma testi yazıldı ve çalıştırıldı — GET 503→200 (2 deneme), POST retry kapalıyken tek deneme, POST retry açıkken belirsiz 503'te tek deneme, üç denemeli gövde korunumu (`ONEMLI-GOVDE` üç denemede de aynı), sürekli hâlde azami deneme sınırı (3). Beşi de beklendiği gibi.

### R-6 — Startup yetkilendirme envanteri (🟡 Kısmi)

Ayrıntı Bölüm 4'te. Özet: korumasız uygulama ucu kalmadı (canlı sayım: 762 uç tarandı, 2 uç korumasız ve ikisi de framework'e ait). Kalan risk, yeni bir uç eklendiğinde envanter uyarısının gözden kaçabilmesi ve uçtan uca HTTP senaryolarının hâlâ denenmemiş olması.

### Bu turda çalıştırılan doğrulama

- `dotnet build MKFiloServis.slnx -c Debug -m:1 --no-incremental` → **0 uyarı, 0 hata**.
- `dotnet list package --vulnerable --include-transitive` (Web) → güvenlik açığı yok.
- SQLite runtime smoke: `sqlite_version=3.53.3`, aggregate ağır sorgular başarılı.
- Uygulama canlı çalıştırıldı; yetkilendirme envanteri doğrulandı (`762 uç tarandı, 2 uç korumasız`).
- Retry handler için 5 senaryolu çalıştırma testi → 5/5 beklendiği gibi.
- Tüm izlenen `.cs` dosyalarında yalnız yorum içeren `catch { /* ... */ }` taraması → **0 eşleşme**.

Bu turda paket üretimi, gerçek veritabanı geçişi, müşteri lisans geçişi ve uçtan uca HTTP yetkilendirme senaryosu çalıştırılmadı.

---

## 18. Yarım kalan kod yollarının tamamlanması (2026-10-02)

### Uygulanan düzeltmeler

- **Proforma PDF:** QuestPDF ile A4, tekrar eden tablo başlığı, sıralı kalemler, iskonto/KDV/toplamlar, koşullar ve sayfa numarası eklendi. Firma filtreli sorgu kullanılır; dahili `OzelNotlar` dışa aktarılmaz.
- **Luca detay:** HTML tahmini yerine indirilmiş UBL Invoice XML okunur. DTD yasaktır, belge boyutu sınırlıdır; kimlik, tarih, taraflar, KDV ve tutarlar ayrıştırılır. Belge yönü aktif firmanın VKN'sinden belirlenir. Firma eşleşmeyen, dövizli veya desteklenmeyen belge türü açıkça reddedilir; portal işlem durumu XML'den uydurulmaz.
- **Luca kimlik bilgileri:** Parola, access token ve refresh token Data Protection ile korunur. Eski düz metin kayıt ilk okumada dönüştürülür. Yazma geçici dosya + atomik taşıma ile yapılır; süreç içindeki okuma/yazmalar kilitlenir. Çözülemeyen şifreli kayıt boş parola ile ezilmez.
- **Audit:** Hata yutan tablo probe'u ve fire-and-forget interceptor kaldırıldı. Audit ve iş kayıtları aynı `SaveChanges` işlemine girer. DB tarafından oluşturulan yeni kayıt kimlikleri, audit satırı yazıldıktan sonra aynı veritabanı transaction'ında güncellenir; hata olursa işlem rollback edilir. Boolean overload'lar da ortak hazırlık akışından geçer. Başarısız denemede üretilen audit kayıtları tekrar denemede çoğaltılmaz. Parola/token/anahtar alanları JSON'da maskelenir; HTTP ve mevcut circuit kullanıcı adı mümkün olduğunda eklenir. Firma bağlamı olmayan sistem kaydı başka firmaya atanmaz.
- **Başlangıç:** Zorunlu görev hataları kritik log + yeniden fırlatma ile uygulamayı durdurur. Marka/model, hesap planı, piyasa kaynakları, Budget masraf kalemleri ve evrak tanımı başlangıç verileri opsiyoneldir. `DbInitializer` kurtarılamayan migration hatasını artık yutmaz.
- **Tenant ve legacy aktarım:** Merkezde firmasız ekleme reddedilir. Legacy aktarım varsayılan olarak kapalıdır; otomatik kaynak veritabanı adı türetme ve firma 1 fallback'i kaldırıldı. Etkinleştirmek için `LegacyTransfer:Enabled=true`, `LegacyTransfer:TargetFirmaId` ve `ConnectionStrings:LegacySourceConnection` açıkça verilmelidir. Hedef firma önceden var ve aktif olmalıdır. Kaynak bağlantısı salt okunur yapılır; kaynak erişim/aktarım hatası başlangıcı durdurur.
- **Sağlayıcı ayarları:** `CanonicalProvider` okuma/kaydetmede korunur ve transition manifestine gerçek değer yazılır. `IsCanonicalProvider`, çalışma sağlayıcısı ile kanonik sağlayıcının eşitliğini gösterir. PostgreSQL master hazırlığı diğer sağlayıcılarda çalışmaz. PostgreSQL migration'ları SQL Server/MySQL üzerinde çalıştırılmaz; bu sağlayıcılar için bağımsız migration desteği hâlâ gerekir.
- **DataSync PostgreSQL → SQLite:** RepeatableRead kaynak snapshot'ı, dayanıklı SQLite yazma ayarı, kaynak kolon kaybı reddi, kaynak/hedef satır sayımı ve commit öncesi `foreign_key_check` eklendi. Tablo hatası atlanmaz; transaction geri alınır. AUTOINCREMENT sequence güncellemesi aynı transaction içinde yapılır.
- **DataSync SQLite → PostgreSQL:** Salt okunur kaynak snapshot'ı ve kaynak FK kontrolü, hedefte Serializable transaction ve tablo kilitleri eklendi. `session_replication_role` yalnız transaction kapsamında değiştirilir. Hedef satır sayımı ve tüm public-schema yabancı anahtarları (bileşik anahtar/MATCH FULL null koşulları dahil) commit öncesi kontrol edilir. Sequence'lar transaction kapsamında `ALTER SEQUENCE ... RESTART` ile güncellenir.
- **Yedekleme:** Eksik INSERT-only fallback kaldırıldı. ZIP içindeki tek `database.backup` girdisi ve PGDMP başlığı doğrulanır; arşivin diğer yolları çıkartılmaz. Restore öncesi geri dönüş yedeği zorunludur. PostgreSQL custom restore tek transaction + exit-on-error, SQL restore tek transaction + ON_ERROR_STOP kullanır. Process stdout/stderr eşzamanlı okunur. ZIP içindeki uploads/appsettings dosyalarının canlı uygulamaya otomatik kurulması bu yöntemin kapsamı değildir.
- **Lisans:** v2 yeniden basım özgün oluşturma tarihini ve süreyi birlikte korur; tarih/süre tutarsız kaydı reddeder. Ön kontrollerdeki hatalar kullanıcıya gösterilir; pano hatası kaydedilmiş lisansı başarısız gibi göstermez. Firma eşleşmesi belirsizse aktivasyon durur; eski lisansın pasifleştirilmesi ve yenisinin kaydı tek transaction'dadır.
- **Paketleme:** Desktop müşteri dosyalarına eski `MKLisansArac` çıktısını kopyalamaz. Gömülen müşteri anahtarı ikinci build sonrasında `finally` ile publish kaynağından kaldırılır; eksik Web payload başarısızlık olarak gösterilir.
- **Selenium/Swagger/CI:** Selenium beklemeleri await kullanır. Production Swagger Admin Bearer koruması güncel kodda doğrulandı; yetkisiz rol 403 döner. Linux CI Web/Shared projelerini derler, test projesi bulunursa ayrıca restore/build yapar; `.slnx` değişiklikleri workflow'u tetikler.

### Doğrulama ve açık kapanış işleri

- 🟢 **Derleme:** Son kaynak için `dotnet build MKFiloServis.slnx -c Debug -m:1 --no-restore --no-incremental` başarılı: **0 uyarı, 0 hata**, süre 1 dakika 27 saniye. Client Android/Windows, Shared, DataSync, LisansDesktop, Web ve mevcut PlaywrightSmoke projesi derlendi; PlaywrightSmoke çalıştırılmadı.
- 🟢 **Biçim:** Bu turda değişen dosyalarda `git diff --check` whitespace hatası bildirmedi.
- Otomatik test eklenmedi/çalıştırılmadı. Bu turdaki kod ve derleme incelemesi, runtime kabulü veya restore provası yerine geçmez.
- **K-1:** Kullanıcı güvenli yedek konumunu seçme kararını asistana verdi; şifreli yerel/USB yedeği ve aynı hesapta geri açma Bölüm 19'da tamamlandı. Yetkili aktif müşteri listesi ve bağımsız hesap/profil kaybı kurtarması açık; müşteri lisansı üretimi veya teslimi yapılmadı.
- **Y-7:** Kök birim test projesi ve CI test kapsamı açık.
- **O-1:** SQL Server/MySQL için bağımsız şema yükseltme desteği açık; bu sağlayıcıların otomatik migration'ı şu an desteklenmez.
- **K-2/K-3/K-6, R-4/R-5/R-6:** Cookie/Blazor circuit, Bearer, roller ve hesap kilidi için HTTP kabulü açık.
- **Y-2/Y-3/Y-9/O-3/O-4/O-6/O-7:** PDF görsel kabulü, gerçek Luca portalı, okunmamış eski credential dosyaları, entegrasyon hataları, Raw SQL/ExecuteUpdate audit kapsamı, eski DB başlangıcı, restore ve iki yönlü aktarım izole kopyalarda doğrulanmalı. Audit otomatik kimlik doldurma kaynakta eklendi, runtime DB kabulü henüz yapılmadı. Global sistem kayıtlarında firma bağlamı yoksa tenant audit üretilmez.
- **Y-10:** Eski tarih sütunlarının saat semantiği ve veri geçişi tamamlanmadan legacy timestamp varsayılanı kapatılmadı.
- **R-3/O-12/O-13/R-7/R-8:** Gerçek kurulum paketi kabulü, ifşa olmuş parolaların rotasyonu ve geçmiş temizliği, önceki dosya silmelerinin kararı açık. Mevcut müşteri DB'sinde migration/restore, dosya silme kararları veya müşteri kurulumu yapılmadı.

Bu ek, bütün rapor maddelerinin kapandığı anlamına gelmez. Yeşil maddeler kaynak düzeltmesi bakımından tamamlandı; sarı ve kırmızıların somut kalan işi yukarıda belirtilmiştir.


## 19. K-1 — Yetkilendirilmiş şifreli anahtar yedeği (2026-10-02)

Kullanıcı güvenli yedek konumunu seçme yetkisini verdi. Seçim ve işlem sonucu:

- 🟢 **Yerel ana yedek:** `%LOCALAPPDATA%\MKFiloServis\key-backups`; NTFS ACL yalnız mevcut Windows kullanıcısı muratk, SYSTEM ve yerel yöneticileri kapsar. Özel anahtar ayrıca Windows DPAPI CurrentUser ile şifrelidir.
- 🟢 **Harici şifreli kopya:** `H:\MKFiloServis-SecureBackup\LicenseSigning`; çıkarılabilir **64 GB KING** diski, birim seri **38305E6F**. FAT32 olduğundan NTFS ACL yoktur; dosya şifreli tutulur. Disk biçimlendirilmedi. Başlangıçta seçilen G: işlem öncesi ayrıldığı için hedef güncellendi.
- 🟢 **Doğrulanan dosya:** `license-signing-20261002T175221Z-91da1d3180204406a3672ed5806b2eec.dpapi.json`. Yerel dosya geri açılıp kaynak anahtar baytlarıyla karşılaştırıldı. Geri açılan anahtarla rastgele veri üzerinde RSA-PSS imza/doğrulama başarılı oldu. USB dosyası yerel kopyayla birebir eşleşti ve ayrıca bellekte geri açılarak Web açık anahtarıyla eşleşmesi doğrulandı. Açık metin özel anahtar yedeği diske yazılmadı.
- **Tarihsel işlem:** Önceki turda harici betikle DPAPI yedeği alınmıştı. Kullanıcının yeni kararıyla betik kaldırıldı; güncel anahtar yönetimi LisansDesktop içindedir. Bölüm 20.
- 🟡 **Bağımsız kurtarma:** Doğrulama aynı Windows hesabında yapıldı. Windows profili ve DPAPI anahtarları tamamen kaybolursa bu dosyalar tek başına kurtarma sağlamaz. Ayrı kurum kasası veya bağımsız kurtarma sırrıyla korunan taşınabilir yedek henüz yoktur.
- 🟡 **Müşteri geçişi:** Yetkili aktif müşteri/lisans envanteri hâlâ belirli değil. Yerel satış geçmişi `%LOCALAPPDATA%\MKFiloServis\licenses.db` kaynak adayıdır. Müşteri lisansı basılmadı, teslim edilmedi veya müşteri DB'si değiştirilmedi. K-1 bütünüyle kapanmış değildir.

Güncel program menüsü, yedekleme ve geri yükleme adımları [LISANS-IMZA-GECIS.md](LISANS-IMZA-GECIS.md) içinde kayıtlıdır. Bu bölümdeki konumlar önceki işlemin tarihsel kaydıdır.


## 20. K-1 — Lisans yönetiminin program içine alınması (2026-10-02)

**Kullanıcı kararı:** Lisanslama ve şifreleme LisansDesktop üzerinden yürütülsün; harici/ayrı yapı kullanılmasın.

- 🟢 **Program içi imzalama:** Lisans üretimi `LicenseSigningKeyStore` üzerinden yapılır. Ortam değişkeni gereksinimi kaldırıldı; anahtar kullanıcıya bağlı DPAPI şifreli uygulama deposunda tutulur. Önceki bilinen PEM anahtarı ilk kullanımda aynı RSA anahtarı korunarak içe alınır.
- 🟢 **Program butonları:** Güncel sadeleştirmeyle **Anahtar ve Yedek** sekmesinde **Anahtar Durumu**, **Şifreli Yedek Oluştur**, **Yedeği Doğrula** ve **Anahtar / Yedek İçe Aktar** doğrudan görünür. Önceki açılır menü kaldırıldı.
- 🟢 **Anahtarı değiştirmeden yedek kontrolü:** **Yedeği Doğrula** butonu eklendi. `.mkkey` dosyası parola ile bellekte açılır ve ortak açık anahtarla RSA-PSS imza eşleşmesi denetlenir. Etkin anahtar deposu ve müşteri kayıtları değiştirilmez. İlgili bilgisayarda etkin anahtar bulunması gerekmez; aynı açık anahtarı kullanan program sürümü gerekir.
- 🟢 **Yedek/geri yükleme kodu:** `.mkkey` parola korumalı PKCS#8 yedeği, programın maskeli parola penceresi ve dosya seçim penceresiyle yönetilir. Yedek AES-256-CBC/PBKDF2-SHA256 ile korunur; Windows hesabından bağımsızdır. Parola saklanmaz. Dosya yeniden açılıp imza kontrolünden geçmeden başarı bildirilmez.
- 🟢 **Anahtar eşleşmesi:** LisansDesktop ve Web aynı `LicenseSigningPublicKey.Pem` kaynağını kullanır. Yanlış özel anahtar, yanlış parola ve geçersiz yedek mevcut program anahtarının üzerine yazılmaz. Rastgele yeni anahtar ile mevcut müşteri lisansları değiştirilmez.
- 🟢 **Harici yapı kaldırıldı:** `setup/backup-license-signing-key.ps1` kaldırıldı. Güncel geçiş belgesinde harici betik/ortam değişkeni yönergeleri program menüsüyle değiştirildi. Önceden alınmış yedekler ve kaynak özel anahtar silinmedi; eski DPAPI yedeği program menüsünden içe alınabilir.
- 🟡 **Kabul:** Yeni program pencereleri ve başka bilgisayarda parola ile kurtarma etkileşimli olarak çalıştırılmadı. Yeni `.mkkey` yedeği henüz kullanıcı tarafından oluşturulmadı. Müşteri hak envanteri ve v2 teslim/kabul geçişi açık.

- 🟢 **Derleme:** LisansDesktop Debug (`--no-restore`) ve Web Debug (`--no-restore`) derlemeleri başarılı: her ikisinde **0 uyarı, 0 hata**.
- 🟢 **Devam derlemesi:** Yedeği Doğrula menüsü eklendikten sonra LisansDesktop Debug (`--no-restore`) yeniden derlendi: **0 uyarı, 0 hata**. Bu işlem için otomatik veya etkileşimli kabul testi çalıştırılmadı.
- 🟢 **Biçim:** `git diff --check` whitespace hatası bildirmedi.

Bu turda müşteri lisansı üretilmedi, müşteri veritabanı değiştirilmedi veya otomatik test çalıştırılmadı. Derleme sonucu, yeni menülerin etkileşimli kabulü yerine geçmez.

### K-1 ekran düzenlemesi — doğrudan erişim butonları

Kullanıcının ekran görüntüsünde anahtar işlemleri görünmüyordu. Önceki açılır menü kaldırıldı; başlığın altında **Anahtar Durumu**, **Şifreli Yedek Oluştur**, **Yedeği Doğrula** ve **Anahtar / Yedek İçe Aktar** butonları doğrudan gösterilir. Sağ kenara sabit koordinatla yerleştirme yerine genişliğe uyumlu bir işlem satırı kullanılır. Yukarıdaki menü adımları bu butonlarla yürütülür.

- 🟢 Ekran kodu güncellendi ve LisansDesktop Debug (`--no-restore`) derlendi: **0 uyarı, 0 hata**.
- 🟡 Güncel program başlatıldı; butonların ekranda görünürlüğü ve yedek oluşturma/doğrulama sonucu henüz kullanıcı tarafından teyit edilmedi.
- 🟢 **Kullanılabilir EXE güncellendi:** Butonları içeren LisansDesktop Release/win-x64 çıktısı `setup/payload/LisansDesktop` konumuna self-contained/single-file olarak yeniden yayımlandı ve buradaki program başlatıldı. Eski sürümlerin `setup/output` altındaki tarihsel kurulum dosyaları güncel ekranı içermez. Görsel kabul ve yedek işlemlerinin sonucu henüz kullanıcı tarafından teyit edilmedi.

## 21. Web başlangıç uyarıları — JWT, framework uçları ve tarih davranışı

- 🟢 **Development JWT yapılandırması:** Web projesine User Secrets kimliği eklendi. `GenerateAssemblyInfo=false` nedeniyle kimlik ayrıca assembly attribute ile tanımlandı. Bu bilgisayarda 48 rastgele bayttan üretilen geliştirme anahtarı User Secrets'a kaydedildi; değer rapora veya kaynak koduna yazılmadı. Development yeniden başlatmalarında bu yapılandırma yüklenir. Eski process anahtarıyla verilmiş tokenlar ilk geçişte geçersiz olur. Yapılandırmasız başka geliştirme bilgisayarlarında mevcut geçici anahtar fallback'i sürer. Production anahtar zorunluluğu korunur. User Secrets geliştirme amaçlıdır ve şifreli kasa değildir; lisans imza anahtarıyla ilgisi yoktur. [Microsoft belgesi](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0).
- 🟢 **Framework yönlendirmesi:** Yalnız `/_framework/opaque-redirect` rotası ve `Blazor Opaque Redirection` adı eşleşen framework ucuna `AllowAnonymous` metadatası eklendi. Tüm Blazor sayfaları anonim yapılmadı. Framework, yönlendirme verisini süre sınırlı DataProtection ile doğrular. [ASP.NET Core kaynak kodu](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Endpoints/src/Builder/OpaqueRedirection.cs).
- 🟢 **Statik dosya fallback'i:** `MapStaticAssets` anonim bir route grubu içinde tanımlandı; grubun metadatası framework'ün geliştirme sırasında oluşturduğu `{**path:file}` fallback'ine de uygulanır. Controller ve diğer uygulama uçları gruba alınmadı; yetkilendirme envanteri denetimi sürer. Framework fallback'i mevcut web root dosyalarını GET/HEAD ile sunar. [ASP.NET Core kaynak kodu](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/StaticAssets/src/Development/StaticAssetDevelopmentRuntimeHandler.cs).
- 🟡 **Y-10 / tarih davranışı:** Legacy timestamp davranışı mevcut veri semantiği belirsiz olduğundan kapatılmadı. Uyarı artık yalnız seçilen sağlayıcı PostgreSQL ise yazılır; switch yine Npgsql kullanılmadan önce ayarlanır. Geçiş açıklamasındaki koşulsuz UTC varsayımı kaldırıldı; dönüşüm kaynak değerlerin gerçek saat dilimine göre planlanmalıdır. Veritabanında tarih dönüşümü yapılmadı. [Npgsql belgesi](https://www.npgsql.org/doc/types/datetime.html).
- 🟢 **Derleme:** Web Debug (`--no-restore -p:UseAppHost=false`) başarılı: **0 uyarı, 0 hata**.
- 🟡 **Çalışma zamanı kabulü:** Web yeniden başlatılıp loglar, oturum açma/yönlendirme ve yeniden başlatmalar arasında JWT kabulü henüz doğrulanmadı. Otomatik test eklenmedi veya çalıştırılmadı; uç sayısının değişmesi framework/sürüm/yapılandırmaya bağlı olabilir.

## 22. LisansDesktop — şifreli anahtar akışına göre sade ekran

- 🟢 **Lisanslar:** Lisans bilgileri, oluşturma/yenileme, kayıt güncelleme/silme ve lisans kopyalama erişilebilir biçimde düzenlendi. Önceki sabit yerleşimde grup sınırının dışında kalan işlem butonları ve çıktı kopyalama butonu kendi alanlarına alındı. Dar/kısa pencerelerde sol alan kaydırılır; geçmiş tablosu pencereyle büyür.
- 🟢 **Anahtar ve Yedek:** Anahtar durumu, parola korumalı yedek oluşturma, mevcut anahtarı değiştirmeden yedek doğrulama ve içe alma butonları açıklamalarıyla bu sekmeye taşındı. Önceki başlık altı buton satırı kaldırıldı. İmzalama algoritması ve mevcut anahtar değiştirilmedi.
- 🟢 **Paketleme:** Kurulum/güncelleme araçları kendi sekmesine alındı; paket sürüm alanı etiketlendi.
- 🟢 **Geçmiş:** Liste temel yedi sütunu gösterir; ayrıntı sütunları veri kaynağında ve CSV raporunda korunur. Bitiş tarihi `dd.MM.yyyy`, işlem adları Türkçe gösterilir. Süresi bitmiş/yaklaşan lisansların renkleri korunur; güncel kayıtta eski satır rengi temizlenir.
- 🟢 **Çıktı:** LisansDesktop Release/win-x64 self-contained/single-file publish başarılı; `setup/payload/LisansDesktop/MKFiloServisLisans.exe` güncellendi. Publish sırasında hata veya uyarı bildirilmedi. Kullanım belgesi yeni sekme ve buton adlarıyla güncellendi.
- 🟡 **Kabul:** Yeni ekran etkileşimli olarak incelenmedi. Açık eski programı kapatıp güncel EXE'yi açarak görünürlük, farklı pencere boyutları ve yedek işlemleri teyit edilmeli. Otomatik test eklenmedi/çalıştırılmadı; müşteri veritabanında işlem yapılmadı.

## 23. Satışa çıkarım yeniden analizi ve karşılaştırma (2026-10-02)

**Güncel tam görev listesi:** [Satışa çıkarım görev envanteri](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md). İlk analizdeki 6 kritik, 11 yüksek, 14 orta ve 8 düşük maddenin tarihsel değerlendirmesi Bölüm 23'te özetlenir; eski ayrı ayrıntı dosyası çalışma ağacında yoktur. Güncel birleşik tabloda eksik O-8/O-9/O-14 takip edilir.

### Yeni açık kod işleri

| ID | Durum | Bulgu | Gereken |
|---|---|---|---|
| N-1 | 🟡 Kod düzeltildi; çalışma zamanı kabulü açık | Ortak sürüm politikası boş/geçersiz değeri reddeder ve en fazla 20 karakter kabul eder; bu sınır EF `AllowedVersion` alanıyla ve Desktop giriş kutusuyla aynıdır. Yalnız açık `0.0.0` sınırsız hakkı korunur. | Geçerli, eski, geçersiz ve sınırsız imzalı sürüm haklarının çalışma zamanı kabulü; Bölüm 25 ve 27. O-14 ve payload ortaklaştırması ayrı açık. |
| N-2 | 🟡 Kod uygulandı; tam kurtarma kabulü açık | Ortak ZIP arşivi DataProtection key ring ve yerel belge/ayar kapsamını manifest/hash ile yedekler. Admin ekranındaki kurtarma hazırlığı doğrulanmış ZIP'i izole staging alanına açıp yedek key ring probunu dener. Canlı restore akışı DB ile sınırlıdır. | Farklı makine/profilde gerçek DB + dosya + anahtar kurtarma; DPAPI/sertifika bağımlılıkları, S3 ve DB-belge tutarlılığı kabulü. K-1 lisans anahtarı Web belge anahtarlarından ayrıdır. Bkz. Bölüm 24 ve 27. |
| N-3 | 🟢 Tetikleme kodu düzeltildi | CI dosya filtreleri kaldırıldı; main push/PR'da tüm değişiklikler tetiklenir. | GitHub çalışma sonucu bekliyor. Kök test projesi/Y-7 ayrı açık; Bölüm 25. |

### Rapor ifadelerinde netleştirilenler

- Y-6: Uyarıyı loglamak model/migration uyumunu kapatmaz; ana durum sarıya alındı.
- Y-7: Workflow eksik test projesini atlar; başarılı build, test kapsamı kanıtı değildir.
- D-2: “Yalnız arayüz tanımı” ilk tespiti güncel kaynakla uyuşmaz; entity uygulamaları ve firma kopyalama kullanımı mevcut.
- O-13/R-8: Güncel Git durumunda `test_all.txt` ve boş Infrastructure dosyası **staged**; eski raporlar ve Rent-a-Car analiz belgesi **unstaged** silinmiştir. Önceki bölümlerdeki staged anlatımları bu kayıtla düzeltilir. Silmelerin commit/yayınlandığı varsayılmadı.
- Bu tarihsel turda son paket zafiyet ve geçmiş sır taraması yapılmadı; bağımlılık taraması sonraki [2026-10-06 A-22 kapanışında](A-22-BAGIMLILIK-TARAMASI-2026-10-06.md) tamamlandı. Geçmiş sır taraması/rotasyonu A-06 kapsamında açık kalır.

**Öncelik:** N-2/D-5 tam şifreli belge kurtarma → K-1/N-1/O-14 lisans hakları ve geçiş → erişim/tenant/parola kabulü → gerçek kurulum ve veri aktarımı → CI/kritik işlem bütünlüğü → sır rotasyonu ve teslim commit'i.

**Kapsam:** Bu tur statik yeniden analiz ve belge güncellemesidir. Yeni derleme/test, müşteri lisansı üretimi, sır değişikliği, veritabanına yazma veya kaynak kod düzeltmesi yapılmadı. Satışa hazır kabulü henüz verilmez.

## 24. N-2 devamı — DataProtection ile uyumlu yedek kapsamı

- 🟢 **Ortak kurtarma arşivi:** `RecoveryArchive` dosya yedeği ve otomatik PostgreSQL DB ZIP yedeği tarafından kullanılır. `master.key` ön koşulu kaldırıldı; DataProtection key ring zorunlu olarak alınır, mevcut legacy anahtar dosyaları korunur.
- 🟢 **Kapsam:** Gerçek depolama kökünün uploads/Arsiv/Depo/data/logs/keys dizinleri; content root içindeki Luca ayarları, belge/eski upload dizinleri ve seçili uygulama ayarları alınır. DB ZIP'i ayrıca `database.backup` içerir. S3 nesneleri bu yerel ZIP'e dahil değildir.
- 🟢 **Arşiv bütünlüğü:** Geçici ZIP kapatılıp manifestteki dosya sayıları, boyutları ve SHA-256 değerleriyle doğrulanır; sonra nihai ada taşınır. Aynı saniyedeki yedekler için GUID kullanılır. İptal/hata geçici ZIP'i temizler; kaynak içindeki hedef ve sembolik bağlantı kaynakları reddedilir.
- 🟢 **Restore ön kontrolü:** Yeni DB ZIP biçiminde manifest/hash kontrolü DB'ye dokunmadan yapılır. Eski manifestsiz ZIP yolu korunur. Dosyalar/ayarlar/anahtarlar canlı uygulamada otomatik üzerine yazılmaz; mevcut restore yalnız DB içindir.
- 🟡 **N-2/D-5/O-6 kabulü:** Yeni makine/profilde belge ve credential açma, gerekli DPAPI/sertifika malzemesi, bakım penceresinde DB-belge tutarlılığı ve S3 kurtarması açıktır. Manifest bütünlüğü arşivin ayrı imzası değildir. Yedekler anahtar ve sır içerebilir; yetkili ve şifreli yedek depolamasında tutulmalı.
- **Kullanım/kabul belgesi:** [Şifreli belge yedek ve kurtarma kapsamı](SIFRELI-BELGE-YEDEK-KURTARMA.md).
- 🟢 **Derleme:** Son kaynak için Web Debug (`--no-restore -p:UseAppHost=false`) başarılı: **0 uyarı, 0 hata**. `git diff --check` bu tur değişen kaynak ve belgelerde whitespace hatası bildirmedi.

Gerçek yedek veya restore çalıştırılmadı, otomatik test eklenmedi/çalıştırılmadı ve müşteri veritabanına yazılmadı. N-2 kod kısmında ilerledi; tam kurtarma kabulü tamamlanmadan yeşil kapanış verilmez.

## 25. N-1/N-3 devamı — ortak sürüm kontrolü ve CI tetikleme

- 🟢 **Sürüm politikası:** `MKFiloServis.Shared/Licensing/LicenseVersionPolicy.cs` eklendi. Desktop aynı kaynak dosyasını link olarak derler; Web Shared üzerinden kullanır. 2–4 sayısal bileşen dışında boş/harfli/işaretli/aşırı uzun veya parse edilemeyen değer reddedilir. Parse hatasında izin verme kaldırıldı.
- 🟢 **Üretici kontrolü:** Desktop imzalama öncesinde ve v2 yeniden basımın ilk kontrolünde aynı sürüm doğrulaması kullanılır. Varsayılan yalnız parametre verilmediğinde kullanılır; açıkça boş parametre geçerli hak sayılmaz. Yeni satışın sabit `1.0.99` hakkı ve ayrı payload kodu bu düzeltmeyle ortaklaştırılmış sayılmaz (O-14/O-8 açık).
- 🟢 **Mevcut sınırsız hak:** Tam `0.0.0` değeri mevcut sözleşmedeki açık sınırsız sürüm işaretidir; imzalı alanda korunur. Hatalı değer bu hakka çevrilmez. Müşteri anahtarları/veritabanı yeniden yazılmadı.
- 🟢 **CI kapsamı:** `.github/workflows/tests.yml` push ve PR dosya filtreleri kaldırıldı. Main kapsamındaki tüm dosya değişiklikleri tetiklenir; manuel çalıştırma sürer. Yalnız belge değişiklikleri de artık derleme başlatabilir. GitHub'a push yapılmadı ve workflow sonucu alınmadı.
- 🟢 **Derleme/çıktı:** Web Debug (`--no-restore -p:UseAppHost=false`) **0 uyarı, 0 hata** ile başarılı. Desktop Release/win-x64 self-contained/single-file publish başarılı; güncel dahili EXE yeniden yayımlandı.
- 🟡 **Kabul:** Geçerli imzalı geçersiz/eski/yeni/sınırsız sürüm akışları çalıştırılmadı. Otomatik test eklenmedi/çalıştırılmadı; Y-7 kök test projesi ve kritik CI testleri açık kalır. N-1 kod düzeltmesi ve N-3 tetikleyici değişikliği test kapsamı kanıtı değildir.

## 26. O-14/O-8 devamı — programdan sürüm hakkı ve ortak imza verisi

- 🟢 **Program alanı:** Lisanslar sekmesine **En Fazla Sürüm** alanı ve **Sınırsız sürüm hakkı** kutusu eklendi. Başlangıçtaki `1.0.99` yalnız değiştirilebilir alan varsayılanıdır; imzalama artık zorunlu sürüm parametresi alır. Sınırsız hak üretim formunda açıkça işaretlenmelidir.
- 🟢 **Hak ve kayıt tutarlılığı:** Yeni satış, yenileme ve müşteri kurulumu aynı seçilen sürüm hakkını imzaya/geçmiş kaydına yazar. Seçili kaydın hakkı forma yüklenir. v2 yeniden basım formdaki farklı hak yerine eski kayıttaki hakkı korur. Kaydı Güncelle eski sürüm hakkını değiştirmez; hak değişikliği yeni imzalı lisans üretimi gerektirir.
- 🟢 **Paket kontrolü:** Müşteri paketi sürümü lisans sınırını aşarsa dosya/klasör çıktısı oluşturulmadan reddedilir. Paket sürümü sayısal olarak doğrulanır. Genel güncelleme ZIP'i seçili müşteri hakkına bağlanmaz; sürüm biçimi doğrulanır.
- 🟢 **Ortak v2 payload:** Shared `LicenseSignaturePayload` Web ve Desktop tarafından kullanılır. Mevcut v2 alan sırası, uzunluk önekleri, tarih biçimi ve algoritma korunur. Özel anahtar ve mevcut müşteri lisansları değiştirilmedi.
- 🟡 **O-8 sınırı:** Firma/makine normalizasyonu, tablo introspeksiyonu ve seed tekrarları bu tur tamamen ortaklaştırılmadı. Payload ortaklaştırması bütün O-8'in kapandığı anlamına gelmez.
- 🟢 **Derleme:** Web Debug (`--no-restore -p:UseAppHost=false`) **0 uyarı, 0 hata**; LisansDesktop Release/win-x64 self-contained/single-file publish başarılı. Güncel dahili EXE yenilendi.
- 🟡 **Kabul:** Yeni sürüm alanı, sınırsız hak seçimi, mevcut hakla yeniden basım, paket sınırı ve eski v2 imza uyumluluğu etkileşimli/otomatik kabulden geçmedi. Müşteri lisansı basılmadı veya teslim edilmedi; DB'ye yazma yapılmadı. O-14 kod tarafında uygulandı, kabul açık.

## 27. N-1/N-2 alan sınırı ve doğrulama durumu — 2026-10-02

- 🟢 **N-1 kod uyumu:** `LicenseVersionPolicy.MaximumLength` değeri 20'dir. Ortak doğrulama bu sınırı uygular; Desktop giriş kutusu aynı sabiti kullanır. Bu, EF `LicenseInfo.AllowedVersion` alanının `[StringLength(20)]` sınırıyla eşleşir ve saklanamayacak uzun hakların üretimini önler.
- 🟡 **N-1 kabulü:** Eski/geçerli/geçersiz/sınırsız imzalı lisansların gerçek çalışma zamanı senaryoları henüz kabul edilmedi. Müşteri lisansı veya DB değiştirilmedi.
- 🟡 **N-2 kabulü:** Önceki bölümdeki kurtarma hazırlığı doğrulanmış arşivi izole staging alanına çıkarıp key ring probunu çalıştırır; bu, uygulama verilerinin ve DB'nin yeni makine/profilde tam geri yüklendiğini kanıtlamaz. Canlı restore uygulanmadı. N-2/D-5/O-6 için bağımsız tam kurtarma kabulü açık kalır.
- 🟢 **Doğrulama:** Web Debug derlemesi (`--no-restore -p:UseAppHost=false`) başarılı: **0 uyarı, 0 hata**. LisansDesktop Release/win-x64 self-contained/single-file publish başarılı. `git diff --check` değişen kaynak ve belgelerde whitespace hatası bildirmedi.
- **Kapsam:** Otomatik test eklenmedi/çalıştırılmadı; gerçek yedek/restore, DB yazımı ve müşteri lisansı üretimi yapılmadı.

## 27. Modül seçimi ve lisans dışı erişimin engellenmesi — 2026-10-02

- 🟢 **LisansDesktop:** Lisanslar sekmesinde 15 modüllü işaretleme listesi eklendi. Yeni satış, yenileme, müşteri paketi ve modüllü yeniden basım aynı seçimi imzalar/kaydeder. Seçim boşsa üretim reddedilir. Geçmişe eklenen `Modules` sütunu ve CSV seçilen hakları gösterir; eski kayıtlar otomatik doldurulmaz. Kaydı Güncelle kayıtlı hakları değiştirmez.
- 🟢 **v3 protokolü:** Modüller kanonik sırayla v3 RSA-PSS imzalı zarfta taşınır. Web ve Desktop aynı `LicenseModules` kaynağını kullanır; mevcut v2 alan payload'ı da ortak kalır. Web modül haklarını ayrı, değiştirilebilir bir DB alanından okumaz. Özel anahtar değişmedi; Web DB migration gerekmiyor.
- 🟢 **Sunucu erişimi:** Modül sayfaları, ilgili API controller'ları, modül ayarları, Dosya API'si, EvrakHub ve geliştirme arşiv uçları lisans politikalarıyla korunur. Mevcut kullanıcı/rol yetkileri ayrıca geçerlidir; Admin modül kontrolünden muaf değildir. API/sayfa lisans doğrulaması rutin olarak `LastValidatedAt` yazmaz.
- 🟢 **Görünürlük ve açık oturum:** Menü/dashboard/arama lisans dışı modülleri sınırlandırır. Lisans değişiminde sayfa ağacı yenilenir; açık modül sayfası 30 saniyede bir tekrar kontrol edilir. Devam eden işlem geri alınmaz.
- 🟡 **Eski müşteri geçişi:** Ticari v2/legacy anahtar modül hakkı taşımaz; modül erişimi reddedilir. Programda sözleşmeye uygun modüller seçilip **Seçili Lisansı Modüllü Yeniden Bas** kullanılmalıdır. Özgün firma/makine/süre/bitiş/sürüm korunur, seçilen modüller onayda gösterilir ve `V3Reissue` kaydı açılır. Demo süre sınırı ve kullanıcı yetkileriyle tüm modülleri kapsar. Web ve üretici birlikte dağıtılmalıdır.
- 🟡 **Kabul:** Lisans dışı doğrudan URL/API, Admin, imzalı modül listesi değiştirme, eski müşteri geçişi, lisans değişiminde açık ekran ve EXE görsel kabulü çalıştırılmadı. Otomatik test eklenmedi/çalıştırılmadı; müşteri lisansı üretilmedi/teslim edilmedi ve canlı DB'ye yazılmadı.
- **Kullanım ve bağımlılıklar:** [Lisans imzası v3 ve modül seçimi](LISANS-IMZA-GECIS.md). Dosya API'si EBYS/Belgeler, evrak hub'ı Personel; birleşik finans/kart kopyalama ekranları belgelenen modülleri birlikte gerektirir.

- 🟢 **Son derleme/çıktı:** Web Debug (`--no-restore -p:UseAppHost=false`) başarılı: **0 uyarı, 0 hata**. LisansDesktop Release/win-x64 self-contained/single-file publish başarılı; `setup/payload/LisansDesktop/MKFiloServisLisans.exe` yenilendi. Kaynak/belge diff kontrolü whitespace hatası bildirmedi. Bu sonuç çalışma zamanı kabulü değildir.

## 28. Lisans kontrolü sıkılaştırması ve O-8 normalizasyon devamı — 2026-10-02

- 🟢 **Makine kilidi:** Tam makine kodunun farklı olduğu durumda yalnız bilgisayar/kullanıcı önekiyle verilen izin kaldırıldı. Ortak `LicenseIdentity.MatchesMachine` boş değerleri reddeder; boşluk dışındaki karakterleri ve donanım kimliğini koruyarak tam eşleştirme yapar. Makine kodu değişince yeni imzalı lisans gereklidir.
- 🟢 **Önbellek:** Salt DB okuması doğrulanmış hak sayılmaz. `LicenseCache` okunmuş/doğrulanmış durumları ayırır; modül erişimi yalnız doğrulanmış kayıttan hesaplanır. Başarısız tam kontrol önceki önbelleği temizler. Doğrulama durumu değiştiğinde açık sayfa ağacı yeniden değerlendirilir.
- 🟢 **Aktivasyon:** Aktivasyon/demo hash yazımı sonrası tam kontrolü geçmeden hak yayınlamaz. Gelecekte oluşturulmuş lisans DB aktivasyonundan önce reddedilir. `SaveLicenseAsync`, aynı transaction ve kontrolleri kullanan anahtar aktivasyonuna yönlendirilir.
- 🟢 **O-8 parçası:** Firma kodu ve üretici makine/telefon normalizasyonu Shared kaynağına taşındı; Web tam makine kontrolü aynı kaynağı kullanır. V2/v3 payload alanları ve özel anahtar değişmedi. Diğer firma adı eşleştirme/introspeksiyon/seed tekrarları açık kalır.
- 🟡 **Kabul:** Donanım değişimi, farklı harf/tire içeren kodlar, başarısız hash/DB kontrolü, ham kayıt okuması sonrası modül erişimi ve eski müşteri geçişi kabulü açık. Otomatik test eklenmedi/çalıştırılmadı; canlı DB'ye veya müşteri anahtarlarına yazılmadı.

- 🟢 **Bu devamın derlemesi:** Son Web Debug derlemesi **0 uyarı, 0 hata** ile başarılı. LisansDesktop Release/win-x64 self-contained/single-file publish başarılı; dahili EXE yenilendi. Değişen kaynak ve belgelerde `git diff --check` whitespace hatası bildirmedi. Çalışma zamanı kabulü ayrı açık kalır.

## 29. N-2 / D-5 / O-6 — İzole kurtarma dosyası ve anahtar hazırlığı — 2026-10-02

- 🟢 **Admin ekranı:** Yedekleme sayfası Admin rolüyle sınırlandırıldı. ZIP satırında **Kurtarma Hazırla**, sonuçta sunucu klasörü, dosya sayısı, DB dump ve anahtar probu durumları gösterilir. Harici ZIP yükleme/listeme eklendi; dosya adı/türü kontrol edilir ve yükleme tamamlanınca rastgele adlı geçici dosya nihai adına taşınır.
- 🟢 **Güvenli hazırlık:** Yeni biçim manifest/hash doğrulaması sonrası yalnız izin verilen yollar `RecoveryStaging` altında yeni klasöre açılır. Bağlantı/dizin geçişi/aygıt adı reddi, 100.000 dosya ve 100 GiB sınırları, gerçek açılmış bayt sayısı denetimi, hazırlık sırasında yeniden hash doğrulaması uygulanır. Geçici klasör hata/iptalde temizlenir; mevcut dosya üzerine yazılmaz. Windows/Unix dizin izinleri sınırlandırılır.
- 🟢 **Yedek anahtarıyla kontrol:** Ayrı DataProtection sağlayıcısı yalnız açılan yedeğin key ring'ini kullanır; otomatik yeni anahtar üretimi kapalıdır. Probun çözülememesi, hazırlama tamamlanmış olsa da anahtar kurtarmasının açık kaldığı mesajıyla gösterilir.
- 🟡 **Sınır/kabul:** Canlı DB/anahtar/dosya restore çalıştırılmadı. Gerçek ZIP hazırlığı veya otomatik test eklenmedi/çalıştırılmadı. Bağımsız makine/profilde belge ve credential çözme, eski DPAPI/sertifika, legacy/S3 ve DB-dosya tutarlılığı kabulü açık. Manifest saldırgana karşı imza değildir; yalnız güvenilir ZIP kullanılmalı. N-2/D-5/O-6 yeşil kapanış verilmedi.
- **Kullanım belgesi:** [Kurtarma dosyası hazırlama ve sınırları](SIFRELI-BELGE-YEDEK-KURTARMA.md).

- 🟢 **Derleme:** Son Web Debug (`--no-restore -p:UseAppHost=false`) başarılı: **0 uyarı, 0 hata**. Değişen kaynak ve belgelerde whitespace kontrolü temiz. Web root altında kurtarma hazırlığı reddedilir. Gerçek yedek/restore veya otomatik test çalıştırılmadı.

## N-1/N-2 devamı — sürüm alanı uyumu ve kurtarma durumu — 2026-10-02

- 🟢 **N-1:** `LicenseVersionPolicy.MaximumLength` artık EF `AllowedVersion` 20 karakter alanıyla ortaktır. Desktop giriş kutusu da 20 karaktere sınırlandı. Uzunluk/biçim doğrulaması imzalama ve müşteri paketlemesinden önce yapılır. Eski lisans imzası veya sürüm hakkı değiştirilmedi.
- 🟡 **N-2:** Yeni biçim ZIP, izole klasöre hazırlanıp prob ile kontrol edilebilir. Bu, restore kabulü değildir: canlı uygulama ayarlarına, DB'ye veya dosya deposuna uygulanmıyor. Ayrı makine/profil, belge ve credential çözme, DPAPI/sertifika, DB-dosya tutarlılığı kabulü açık.
- 🟡 **Kabul:** N-1 sınır ve N-2 kurtarma çalışma zamanı/otomatik kabul senaryoları çalıştırılmadı. Gerçek yedek veya restore yoktur.

## N-1/N-2 devamı — güncelleme ZIP sürüm denetimi — 2026-10-02

- 🟢 **N-1 düzeltmesi:** `UpdateService` içindeki eski `Version` karşılaştırıcısı, boş lisans sürümünü sınırsız sayan ve parse hatasında izin veren fallback kaldırıldı. Listeleme ve kurulum aynı `LicenseVersionPolicy` denetimini kullanır. Sürümü ZIP adından çıkarılamayan veya bozuk ZIP sürümü artık izinli sayılmaz; açık `0.0.0` hakkı da güncelleme sürümünün kendi biçimini geçerli olma şartından muaf tutmaz.
- 🟡 **N-2:** Güvenli izole kurtarma hazırlığı mevcut; canlı sisteme dosya/ayar/anahtar/DB uygulaması yapılmaz. Yeni makine/profilde tam kurtarma, DPAPI/sertifika, S3 ve tutarlılık kabulü sürüyor.
- 🟢 **Derleme:** Web ve LisansDesktop Debug derlemeleri başarılı: her biri **0 uyarı, 0 hata**. `git diff --check` kaynak/belge değişikliklerinde whitespace hatası bildirmedi.
- **Kapsam:** Otomatik test ve gerçek yedek/restore çalıştırılmadı; müşteri lisansı ve canlı DB değiştirilmedi.

## N-1/N-2 çalışma zamanı doğrulaması — 2026-10-03

- 🟢 **N-1:** İzole harness'te sürüm politikası ve `UpdateService` çalıştırıldı. 2–4 parçalı geçerli sürüm, lisans sınırını aşma, bozuk lisans/paket sürümü, açık sınırsız hak ve 20 karakter EF sınırı kontrolleri geçti. 5 parçalı dosya adının içinden son 4 parçayı yakalama hatası testte bulundu; regex sınırı düzeltildi ve yeniden test geçildi.
- 🟢 **N-2:** Gerçek `RecoveryArchive.CreateAsync` + `PrepareAsync`, sentetik dosya ve izole DataProtection key ring ile çalıştırıldı. Arşiv doğrulama, yeni staging klasörü, DB dump varlık göstergesi ve anahtar probu geçti. Manifestte kayıtlı dosya baytı sonradan değiştirilince açma reddedildi.
- 🟢 **Sonuç:** Geçici harness'teki **18 kontrolün tamamı geçti**. Harness ve fixture geçici dizindeydi; depoya test kodu eklenmedi.
- 🟡 **Açık kabul:** DB fixture PostgreSQL dump değildi; pg_restore veya gerçek DB restore yapılmadı. Yeni makine/profilde gerçek belge/credential çözme, DPAPI/sertifika, S3 ve DB-belge tutarlılığı kabulü hâlâ açık. N-2 tam kapanmış sayılmaz.
- 🟢 **Derleme:** Regex düzeltmesinden sonra Web Debug derlemesi **0 uyarı, 0 hata** ile başarılı.

## Y-1 devamı — hızlı muhasebe işlemlerinde tutarlılık

- 🟢 `KolayMuhasebeService.KaydetAsync` işlem türü kayıtlarını tek EF transaction'ına aldı. Alt yöntem hata/başarısızlık döndürürse transaction commit edilmez; fatura/masraf, fiş, banka ve stok yazımları beraber geri alınır.
- 🟢 Gelir/gider faturası ve araç masrafındaki stok hareketi yakalamaları kaldırıldı. Stok ekleme/kaydetme hatası artık başarılı işlem mesajına çevrilmez.
- 🟢 Dış catch hatayı tür, cari ve belge bağlamıyla `ILogger`'a yazar; kullanıcıya başarısız sonuç döndürür.
- 🟢 `KaydetMuhasebeFisi` fiş kaydını ana DbContext'e geçirir. `NextFisNoCounterAsync` açık EF bağlantısını ve varsa etkin Npgsql transaction'ını kullanır; fiş başlığı/kalemleri ve sayaç artışı aynı işlemde kalır.
- 🟡 Yeni cari oluşturma ve hesap eşleme ön hazırlığı `KaydetAsync` transaction'ından önce ayrı servislerle yapılır; hata sonrası bu hazırlık kayıtları kalabilir. Başka muhasebe/stok yolları ile audit teslim kapsamı da ayrıca incelenmeli.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata** ile başarılı; `git diff --check` temiz.
- 🟡 PostgreSQL hata/enjeksiyon provası çalıştırılmadı. DB'ye yazılmadı ve otomatik test eklenmedi/çalıştırılmadı; bu akış için runtime rollback kabulü açık.

## O-3 devamı — personel araç ve muhasebe bağlantısı denetimi

- 🟢 Personel ekranındaki araç atamasını kapatma/silme `ExecuteUpdateAsync` çağrıları tracked entity + `SaveChangesAsync` oldu. Ortak audit mekanizması değişen alanları ve mevcut kayıt kimliğini aynı yazım transaction'ında kaydeder. İşlemler mevcut personel `Id` ile de sınırlandırılır.
- 🟢 `SoforService.EnsurePersonelMuhasebeBaglantilariAsync` 335/195 bağlantı güncellemesini tracked `Sofor` kaydı üzerinden yapar; firma query filter korunur ve audit değişen FK değerlerini kaydeder.
- 🟢 `ApplicationDbContext` yeni eklenen kaydın geçici/atanmış PK bilgisini izler; iş satırı ve audit ilk aşamada kaydedilir, DB kimliği aynı transaction içindeki ikinci UPDATE ile audit satırına yazılır. Senkron/asenkron ve `acceptAllChangesOnSuccess` yolları kapsanır.
- 🟡 Kalan kapsam: Web içinde başka `ExecuteUpdateAsync`/Raw SQL yazımları var. Kaynak değişikliği Web Debug derlemesinden geçti (**0 uyarı, 0 hata**); audit kimlik akışı henüz izole DB runtime kabulüyle doğrulanmadı, bu yüzden O-3 açık.

## O-3 devamı — personel sıra numarası audit kapsamı — 2026-10-04

- 🟢 `SoforService.UpdateSiraNoAsync` doğrudan `ExecuteUpdateAsync` yerine firma filtreli tracked `Sofor` kaydını günceller ve `SaveChangesAsync` çağırır. Geçersiz/personel bulunamayan durumlar hata verir; sıra numarası değişikliği ortak audit mekanizmasına girer.
- 🟡 Diğer modüllerdeki toplu SQL güncellemeleri ve Raw SQL yazımları henüz tek tek audit kapsamı açısından sınıflandırılmadı; O-3 tamamlanmış sayılmıyor.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata** ile geçti. Bu değişiklik için gerçek veritabanı runtime kabulü yapılmadı.

## O-3 devamı — personel özlük dosyası metadata silme — 2026-10-04

- 🟢 `PersonelOzlukService.DeleteEvrakDosyaAsync`, belge kaydının dosya metadata alanlarını tracked entity üzerinden temizleyip `SaveChangesAsync` çağırıyor. Sorgu personel, evrak tanımı, kayıt kimliği ve silinmemiş olma koşullarıyla sınırlı; değişiklik audit'e giriyor.
- 🟡 Disk dosyası silme işlemi veritabanı commit'inden sonra yapılır; fiziksel silme hatasının log/ekran bildirimi sonraki ekte kod olarak tamamlandı. DB'den metadata temizlense de yetim dosya kalabilir; otomatik yeniden deneme/temizlik bekler. Bu O-3 audit kapsamından ayrı bir dosya yaşam döngüsü işi olarak açık.
- 🟡 Diğer modüllerdeki toplu SQL güncellemeleri ve Raw SQL yazımları incelenmeye devam ediyor; gerçek veritabanı runtime kabulü yapılmadı.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata** ile başarılı; değişen kod/belgelerde `git diff --check` temiz.
- 🟡 DB'ye yazan runtime kabulü yapılmadı; otomatik test çalıştırılmadı.

## O-3 runtime düzeltmesi ve dosya yolu sınırı — 2026-10-04

- 🟢 **Yeni audit kimliğinin gerçek düzeltmesi:** Runtime testinde önceki kodun `SaveChanges(false)` sonrası CLR'deki `log.Id` yerine EF entry'nin store-generated kimliğini kullanması gerektiği görüldü. Her iki SaveChanges yolunda kimlik EF entry'den okunur; audit UPDATE'i bir satırı etkilemezse hata verilir.
- 🟢 **İşlem sınırı:** Context'in açtığı transaction execution strategy içinde çalışır; `AcceptAllChanges` commit'ten sonra yapılır. Başarısız ikinci aşamada geçici PK değerleri geri yüklenir. Çağıranın transaction'ı varsa ve savepoint destekleniyorsa iki aşama birlikte geri alınır; context dış transaction'ı commit etmez. Savepoint desteklemeyen dış transaction'ın rollback'i çağırana aittir.
- 🟢 **İzole SQLite runtime kabulü:** Gerçek SaveChanges kodu, minimal model ve SQLite bellek veritabanıyla **12 kontrol** geçti. Yeni PK'nin audit'e yazılması, senkron/asenkron akış, `acceptAllChangesOnSuccess=false`, yapay audit UPDATE hatasında iş/audit satırlarının birlikte rollback'i, aynı context ile tekrar deneme, dış transaction rollback'i ve savepoint'in önceki yazımları koruması doğrulandı.
- 🟢 **Ek güvenlik düzeltmesi:** `SecureFileService` içindeki düz metin klasör adı öneki kontrolü kaldırıldı. `StorageFilePath` yalnız seçilen uploads/Arsiv/Depo kökü altında dosya yolu üretir; komşu klasör veya üst klasöre kaçış reddedilir. Gerçek yardımcı kaynakla **11 yol sınırı kontrolü** geçti.
- 🟢 **XML paket sabitlemesi:** Geçici harness restore'u eski transitif `System.Security.Cryptography.Xml 10.0.0` için NU1903 bildirdi. Web'e `10.0.12` doğrudan referansı eklendi; tekrar restore ve runtime testleri bu uyarılar olmadan geçti. Kasıtlı sürüm sabitlemesi için yalnız paket özelinde NU1510 bastırılır. [NuGet sürüm kaydı](https://www.nuget.org/packages/System.Security.Cryptography.Xml/10.0.12).
- 🟡 **O-3 açık kalan kapsam:** SQLite testi minimal modelle sınırlıdır. Gerçek PostgreSQL/SQL Server retry/commit hata davranışı, diğer `ExecuteUpdate`/Raw SQL yazımları ve tenant kabulü açık. Özlük dosyasının fiziksel silme hataları/yetim dosya temizliği de ayrı açık iş olarak korunur. Müşteri DB'si ve gerçek belgeler kullanılmadı.
- 🟢 **Son doğrulama:** Web Debug restore/build **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz. Toplam **23 izole kontrol** geçti.

## O-3 devamı — filo güzergâh eşleştirme yazımı — 2026-10-04

- 🟢 `FiloKomisyonService.UpdateEslestirmeAsync` doğrudan SQL ve `IgnoreQueryFilters` yerine filtreli tracked kaydı güncelleyip `SaveChangesAsync` çağırır. Eski yaklaşım audit'i ve hedef kayıt için firma/ilişkili araç-firma soft-delete filtrelerini atlıyordu. Filtreli SELECT + tracked UPDATE, SQLite toplu UPDATE join sorununa ihtiyaç bırakmaz.
- 🟢 Firma kimliği ve oluşturma tarihi korunur; kurum/güzergâh/araç/personel/kullanıcı, servis türü, ücretler ve aktiflik değişiklikleri ortak audit'e girer. Commit'ten sonra yeniden SELECT edip başarısızlık döndüren eski doğrulama kaldırıldı; yazım başarısızlığı SaveChanges exception'ı olarak gelir.
- 🟢 Gerçek servis ve gerçek SaveChanges ile minimal ilişkisel modelde **11 SQLite runtime kontrolü geçti**: alan yazımı, firma/tarih korunması, eski/yeni ücret audit'i, başka firma ve silinmiş eşleştirme/araç/firma reddi, bulunamayan kayıt reddi ve audit hatasında ücret rollback'i. SQLite SELECT, ilişkili araç/firma filtre join'lerini kullandı.
- 🟡 Tam uygulama modeli/circuit ve PostgreSQL kabulü açık. İlişki FK'lerinin firma sahipliği aşağıdaki devam ekinde servis düzeyinde doğrulandı. Diğer toplu SQL yazımları açık. Harness yalnız bellek DB'si/sentetik kayıtları kullandı ve depo dışında tutuldu; müşteri DB'sine erişilmedi.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.

## Filo oluşturma/güncelleme ilişkilerinin firma kapsamı — 2026-10-04

- 🟢 `FiloKomisyonService` iki yazım yolunda kurum (`Cari`), güzergâh, araç ve personel için mevcut firma kimliği, silinmeme ve query filter kontrollerini uygular. Güncelleme kapsamı veritabanındaki eşleştirmeden alınır. Firma varlığı/silinme ve isteğe bağlı global kullanıcı varlığı/silinme kontrolleri de eklendi.
- 🟢 Oluşturma gelen entity/navigation grafiğini doğrudan Add etmez; doğrulanmış scalar alanlardan yeni entity üretir. Gelen PK/ilişkili nesneler yabancı anahtarları yeniden bağlayamaz veya başka kayıtları ekleyemez. Güncelleme yalnız izinli scalar alanları kopyalar.
- 🟢 `ServisOperasyon/Seferler.razor`, `KurumFirmaId` alanını Cari FK'si olmasına rağmen Firma kimliğine çeviriyordu. Cari → Firma map'leri kaldırıldı; düzenleme ve kaydetme doğrudan seçilen cari kimliğiyle çalışır.
- 🟢 **57 gerçek servis kontrolü** izole SQLite bellek DB'si/minimal modelle geçti: geçerli oluşturma/güncelleme, dört ilişkide başka firma/silinmiş/bulunamayan/0 kimlik reddi, tenant filtresi kapalı senaryoda firma eşitliği, kullanıcı/firma kontrolleri, navigation grafiğinin yok sayılması ve gelen PK değerinin kullanılmaması, başarısız doğrulamalarda iş/audit satırlarının değişmemesi.
- 🟡 Kalan: tam model/circuit/PostgreSQL kabulü, eski eşleştirmelerin ilişki tutarlılığı ve DB seviyesinde firma ilişki kısıtları. Gerçek müşteri kayıtlarında dönüşüm/yeniden eşleme yapılmadı. O-3/K-3'ün diğer servisleri bu düzeltmeyle kapanmış sayılmaz.
- 🟢 Son kaynak için Web Debug `--no-incremental` derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz. Seferler ekranı etkileşimli kullanıcı kabulü bekliyor.

## O-3 devamı — banka import kolon şablonu yazımları — 2026-10-04

- 🟢 `BankaImportService.SilAsync` toplu SQL yerine filtreli tracked kaydı soft-delete eder ve `SaveChangesAsync` çağırır. Kimlik, IsDeleted ve DeletedAt değişiklikleri ortak audit mekanizmasına girer. Bulunamayan/gizli/silinmiş şablon başarılı silinmiş gibi bildirilmez.
- 🟢 `KaydetAsync` için mevcut kaydın güncellemesi filtreli tracked SELECT'e taşındı; silinmiş kayıt düzenlenemez. Mevcut/gizli kaydın bulunamaması yeni Add yoluna düşmez; FirmaId mevcut kayıtta korunur. Kaydetmeden önce zorunlu kolon doğrulaması ve negatif kimlik reddi eklendi.
- 🟢 **16 izole SQLite kontrolü** gerçek servis/SaveChanges koduyla geçti. Minimal modelde soft-delete query filter özellikle kullanılmadı; varsayılan tracking kapalıydı. Böylece servis içindeki silinmeme koşulu ve `AsTracking` davranışı sınandı. Audit eski/yeni değerleri, firma korunması, geçersiz/gizli/silinmiş/bulunamayan kayıtların reddi, bulunamayan güncellemede yeni kayıt oluşmasının engellenmesi ve audit INSERT hatasında silmenin rollback'i doğrulandı.
- 🟡 Yeni şablon eklemenin firma kapsamı aşağıdaki devam ekinde tamamlandı; gerçek banka dosyası import/mükerrer kayıt akışı, tam model ve PostgreSQL kabulü açık. Bu testlerde müşteri DB'si/dosyası kullanılmadı; geçici harness depo dışında tutuldu. O-3'ün diğer modülleri tamamlanmış sayılmaz.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.

## Banka kolon şablonu oluşturma ve yazım firma kapsamı — 2026-10-04

- 🟢 Servis constructor'ı scoped `IAktifFirmaProvider` gerektirir; firma kapsamı kullanıcıdan gelen şablondan alınmaz. Oluşturma/güncelleme/silme için tek firma seçilmesi gerekir; null/0/negatif aktif kimlik ve Tüm firmalar modu reddedilir. Güncelleme/silme sorgularına açık FirmaId koşulu eklendi.
- 🟢 Yeni şablon null/0 FirmaId ile geldiğinde seçili firmaya atanır. Farklı/negatif FirmaId reddedilir; seçili firmanın mevcut/silinmemiş olduğu kontrol edilir. Yeni entity yalnız izinli şablon alanlarını alır; istemcinin tarih/silinme metadata alanları taşınmaz. Mevcut şablonda FirmaId korunur.
- 🟢 **46 izole gerçek servis kontrolü** geçti; önceki 16 davranış da tekrar sınandı. Minimal SQLite modelinde global tenant filtresi kaldırıldı ve varsayılan tracking kapatıldı. Böylece scoped provider + açık firma koşulunun bağımsız koruması, tüm firma/seçimsiz mod reddi, geçerli varsayılan firma ataması, firma 2 için meşru oluşturma, geçersiz hedef firma reddi ve yeni kayıtta audit INSERT hatası rollback'i doğrulandı.
- 🟡 Gerçek provider/circuit yetkileri, dosya import/mükerrer kayıt akışı, tam model ve PostgreSQL kabulü açık. Test yalnız bellek DB'si ve sentetik firma sağlayıcısını kullandı; müşteri DB'sine/dosyalarına erişilmedi. Diğer tenant/audit maddeleri kapanmış sayılmaz.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.

## Banka dosyası önizleme ve import düzeltmeleri — 2026-10-04

- 🟢 **Kod düzeltildi:** `PreviewAsync` / `ImportAsync` aktif firma, hedef firma, şablon firma bilgisi, firma varlığı ve kayıtlı şablonun erişilebilir/silinmemiş olması koşullarını kontrol eder. Tüm firmalar modunda işlem reddedilir.
- 🟢 **Kod düzeltildi:** Eksik kolonlar artık sessizce atlanmaz. Negatif kolon/satır ayarları ve boş ayraç/tarih formatı reddedilir. Ayrıştırma hatası bulunan dosyada finans/audit yazımına geçilmez; önizleme başarı durumu hata ve içerik sayısını dikkate alır.
- 🟢 **Kod düzeltildi:** Referanslı mükerrer kayıt kontrolü DB + dosya içi HashSet kullanır; tarih/referans/tutar yanında borç-alacak yönünü de içerir. Yeni kayıt yoksa kullanıcıya bildirilir. Tek SaveChanges finans ve audit yazımı korunur.
- 🟢 **Kod düzeltildi:** Maaş verisi içermeyen, gerçek maaş güncellemesi yapmayan boş snapshot çağrısı kaldırıldı.
- 🟡 **Doğrulama bekliyor:** Bu import düzeltmelerinde runtime testi yapılmadı; önceki 46 şablon testi yeni davranışları kapsamıyor. Eşzamanlı yükleme tekilliği, referanssız kayıt politikası, CSV tırnak/ayraç ayrıştırması, Excel kolon uyumu, gerçek oturum/tam model/PostgreSQL kabulü açık. O-3 ve satış kabulü tümüyle kapanmış değildir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen üç dosyada `git diff --check` temiz.

## Banka CSV/Excel ayrıştırma düzeltmesi — 2026-10-04

- 🟢 **Kod düzeltildi:** CSV/TXT `TextFieldParser` ile seçili ayraç ve tırnak kuralları kullanılarak okunur. Tırnak içindeki ayraç/yeni satır ve çift tırnak kaçışı tek hücrede tutulur; yapısal hata finans kaydından önce bildirilir. Başlık, önizleme ve import kolon dizileriyle çalışır.
- 🟢 **Kod düzeltildi:** `.xlsx` hücreleri metne birleştirilmez; ilk kolon, boş hücre ve satır konumları korunur. Tarih ve sayısal hücreler tarih/sayı ayrıştırıcısıyla uyumlu metne dönüştürülür. `.xls` için desteklenmeyen format mesajı ve dönüştürme yönlendirmesi eklendi.
- 🟢 **Kod düzeltildi:** Başlık okuyucu isteğe bağlı açık ayraç alır; yoksa `;`, `,`, tab başlık adayları karşılaştırılır. Satır sonu/çift tırnak içeren şablon ayracı reddedilir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.
- 🟡 **Kabul bekliyor:** Bu turda çalışma zamanı testi çalıştırılmadı. Gerçek banka dosyaları, Excel hücre türleri, ayraç tahminindeki belirsizlik ve tam model/PostgreSQL kabulü açık. Çok satırlı CSV'de alan dönüşüm hataları mantıksal kayıt numarasıyla bildirilir. Önceki ekteki CSV/Excel kod açıkları giderildi; eşzamanlı yükleme ve referanssız kayıt politikası açık kalır.

## Banka import borç/alacak göstergesi düzeltmesi — 2026-10-04

- 🟢 **Kod düzeltildi:** `ParseBorcAlacak` seçili yön kolonundaki boş/tanınmayan/çelişkili değeri satır hatası olarak reddeder; sessiz varsayılan borç dönüşü kaldırıldı. İçe aktarmanın mevcut hata kontrolü dosyanın tümünü yazımdan önce durdurur.
- 🟢 **Kod düzeltildi:** Özel göstergeler boş değerlerden arındırılmış, harf duyarsız tam eşleşme kullanır. Alt metin ve eksiyle başlayan herhangi bir değer üzerinden yön tahmini kaldırıldı; tek `-` alacak göstergesi korunur. Standart + özel borç/alacak göstergeleri kesişiyorsa şablon ön doğrulaması başarısız olur.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.
- 🟡 **Kabul bekliyor:** Runtime testi yapılmadı. Yön kolonu bulunmayan şablonun varsayılan borç davranışı ve tutarın işareti değiştirilmedi. İmzalı tutar politikası, gerçek banka örnekleri, eşzamanlı yükleme/tekilleştirme ve tam model/PostgreSQL kabulü açık.

## O-3 devamı — maaş snapshot yazımlarının audit kapsamı — 2026-10-04

- 🟢 **Kod düzeltildi:** `MaasSnapshotService` kilitleme/silme işlemlerindeki iki `ExecuteUpdateAsync` kaldırıldı. Firma/yıl/ay/silinmeme koşullarıyla `AsTracking` SELECT ve tek `SaveChangesAsync` kullanılır; ortak audit mekanizması bu değişiklikleri görür.
- 🟢 **Kod düzeltildi:** Zaten kilitli veya silinmiş kayıtların tarihleri tekrar yazılmaz. Silmede `DeletedAt` ve `UpdatedAt` aynı UTC değeri alır. Güncelleme sorgusu varsayılan tracking ayarından bağımsız olmak için `AsTracking` kullanır. Dört yazım metodunda pozitif firma/geçerli yıl/ay kontrolü eklendi.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.
- 🟡 **Kabul bekliyor:** Bu servis değişiklikleri için runtime testi yapılmadı. Audit hata rollback'i, gerçek oturum/firma/personel kapsamı, eşzamanlı yazımlar, muhasebeleştirilmiş dönem silme politikası ve tam model/PostgreSQL kabulü açık. Mevcut query filter korunur; yeni bağımsız aktif firma yetki doğrulaması uygulanmadı. O-3'ün diğer doğrudan SQL yazımları açık kalır.

## Maaş snapshot firma/personel doğrulaması — 2026-10-04

- 🟢 **Kod düzeltildi:** Servis constructor'ı scoped `IAktifFirmaProvider` gerektirir. Dört yazım metodu hedef firmanın tek aktif firma olmasını ve firmaya mevcut/silinmemiş kayıt olarak erişilebilmesini kontrol eder. Tüm firmalar, seçimsiz veya farklı hedef firma yazımı reddedilir.
- 🟢 **Kod düzeltildi:** Oluşturma/güncelleme personel listesinde pozitif/benzersiz kimlik ve `Sofor.FirmaId == firmaId && !IsDeleted` doğrulaması yapılır; global filtre korunur. 500 kimliklik gruplarla bütün liste doğrulanmadan kayda geçilmez. Personelin aktif çalışma durumu geçmiş dönemler için zorunlu tutulmaz.
- 🟢 **Kod düzeltildi:** Mevcut snapshot dönemindeki güncellemede listeye fazladan, dönem kaydı bulunmayan personel eklenmesi reddedilir. Oluşturmada mevcut dönem kontrolü aynı context üzerinden yapılır. Snapshot bulunmayan güncellemenin boş sonuç davranışı korunur.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.
- 🟡 **Kabul bekliyor:** Runtime testi yapılmadı. Önceki ekteki aktif firma ve personel kod eksikleri bu ekle giderildi. Gerçek circuit/yetki kabulü, ekranlarda snapshot hatasının kullanıcıya gösterilmesi, eski ilişki tutarlılığı, DB kısıtları, eşzamanlı yazımlar ve PostgreSQL kabulü açık. Okuma politikası ve muhasebeleştirilmiş dönem silme politikası bu değişikliğin kapsamında değildir.

## Maaş ekranlarının snapshot sonucu ve firma kapsamı — 2026-10-04

- 🟢 **Kod düzeltildi:** İki ekranın snapshot oluşturma yolu artık yükleme sırasında beklenir; fire-and-forget `Task.Run` kaldırıldı. Başarısız oluşturma, liste yüklemesinden ayrı kalıcı uyarıyla kullanıcıya bildirilir. Teknik hata sunucu logger'ına kaydedilir; kullanıcıya firma/dönem kontrolü ve tekrar yükleme yönergesi verilir.
- 🟢 **Kod düzeltildi:** İlk personelden hedef firma türetme kaldırıldı. Tek firma seçilmemiş veya tüm firma görünümündeki liste snapshot olarak kaydedilmez. Banka ekranının görev/SGK bordro filtreli listesi de eksik dönem kaydı üretmez; kullanıcıya filtreleri kaldırması bildirilir.
- 🟢 **Kod düzeltildi:** Dönem/firma ve banka filtre değerleri yükleme başında alınır; snapshot varlık kontrolü öncesinde personel satır dizisi alınır. Yeni yükleme eski uyarıyı temizler. Mevcut dönem kaydının yeniden oluşturulmaması korunur.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dört dosyada `git diff --check` temiz.
- 🟡 **Kabul bekliyor:** Bu turda runtime/UI testi yapılmadı. Eşzamanlı yenileme/oluşturma, yükleme sırasında seçim değişikliği, eski eksik dönem kayıtları ve PostgreSQL kabulü açık. Önceki ekteki ekran hata bildirimi kod eksikliği giderildi; tam UI kabulü yapılmış sayılmaz.

## Maaş liste yüklemelerinin sürüm ve seçim kontrolü — 2026-10-04

- 🟢 **Kod düzeltildi:** Banka/Maaş liste hesaplamaları çağrıya özel listelerde hazırlanır. Artan yükleme sürümü ve dönem/firma/görünüm/filtre karşılaştırmasıyla eski çağrının ekran listelerine yazması engellenir. Eski çağrının finally bloğu yeni yüklemenin göstergesini kapatamaz; eski hata bildirimi bastırılır.
- 🟢 **Kod düzeltildi:** Snapshot varlık sorgusundan sonra oluşturma öncesinde güncellik tekrar kontrol edilir. Maaş firma görünümü yükleme başında alınır. Banka ekranı aktif firma değişimine abone olur, eski listeyi temizler ve tekrar yükler; Dispose aboneliği kaldırır. İki ekranın Dispose yolu bekleyen yükleme sürümünü geçersizleştirir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dört dosyada `git diff --check` temiz.
- 🟡 **Kabul bekliyor:** Runtime/UI testi yapılmadı. Başlamış snapshot servis çağrısının iptali/rollback'i bu kontrolün kapsamında değildir. DB tekilleştirme, detay paneli ve diğer asenkron mali işlemler, eski eksik dönem kayıtları ve PostgreSQL kabulü açık. Önceki ekteki liste karışması kod eksikliği giderildi; hızlı seçim/yenileme kabulü halen bekler.

## Maaş personel detay yükleme yarışı düzeltmesi — 2026-10-04

- 🟢 **Kod düzeltildi:** Detay sorguları yakalanmış personel/sekme/dönem değerleriyle çalışır. Ayrı detay sürümü, ana liste sürümü ve firma/görünüm karşılaştırması eski sonuçların panel alanlarına yazılmasını engeller. Finans, avans, borç, harcama, maaş ve bordro sonuçları yerel değişkenlerde hazırlanır; yalnız güncel çağrı sonuçlarını aktarır.
- 🟢 **Kod düzeltildi:** Panel kapatma/firma değişimi/liste yenileme/Dispose eski çağrıyı geçersizleştirir; eski detay alanları temizlenir. Önceki çağrının hata bildirimi ve yeni yükleme göstergesini kapatması engellenir. Teknik hata logger'a kaydedilir; ekranda sade tekrar deneme bildirimi kullanılır.
- 🟢 **Kod düzeltildi:** Listede bulunmayan personel seçilemez; yenilemede seçili personel yeni listeden güncellenir veya panel kapatılır. Bordro detayında yükleme başındaki dönem/personel korunur.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen üç dosyada `git diff --check` temiz.
- 🟡 **Kabul bekliyor:** Runtime/UI testi yapılmadı. Detay paneli yarışı kod eksikliği giderildi; hızlı personel/sekme/dönem değişimi etkileşimli kabulü bekler. Okuma sorgularının iptali uygulanmadı; eski sonuçlar yayınlanmaz. Diğer asenkron mali yazımlar, snapshot DB tekilleştirme ve PostgreSQL kabulü açık kalır.

## O-3 devamı — fatura şablonu varsayılan yazımları — 2026-10-04

- 🟢 **Kod düzeltildi:** Üç varsayılan kaldırma toplu SQL yazımı tracked sorguya taşındı. Varsayılan işaretleri ve şablon ekleme/güncelleme/seçme tek SaveChanges ile audit/transaction kapsamında kaydedilir. Yeni şablon kaydından önce ayrı kalıcı varsayılan UPDATE'i yapılmaz.
- 🟢 **Kod düzeltildi:** Beş yazım metodunda tek aktif firma ve mevcut/silinmemiş firma kontrolü uygulanır. Güncelleme/silme/varsayılan seçimi/kopyalama açık FirmaId ve silinmeme koşulu kullanır; tracked yazımlar varsayılan tracking ayarından bağımsızdır. Tüm firmalar/seçimsiz yazımlar reddedilir.
- 🟢 **Kod düzeltildi:** Ekleme gelen PK/Firma grafiğini taşımaz; metadata sunucudan atanır. Silmede yedek varsayılan yalnız aynı firmanın aktif/silinmemiş şablonları arasından sabit kimlik sırasıyla seçilir. Değişiklik zamanları UTC'dir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen üç dosyada `git diff --check` boşluk hatası bildirmedi. Git yalnız servis dosyası için LF → CRLF normalizasyon uyarısı verdi.
- 🟡 **Kabul bekliyor:** Runtime testi yapılmadı. Audit/varsayılan rollback'i ve NoTracking kabulü, gerçek firma oturumu, PostgreSQL, eşzamanlı tek varsayılan DB kısıtı, eski bozuk varsayılanlar ve PDF/okuma runtime kabulü açık. PDF/okuma ve fatura grup şablonu kaynak düzeltmeleri sonraki eklerde tamamlandı; O-3 bütünüyle kapanmadı.

## O-3 devamı — fatura grup şablonu audit ve firma yazımı — 2026-10-04

- 🟢 **Kod düzeltildi:** Grup şablonu varsayılan kaldırma SQL UPDATE'i tracked sorgu + alan değişikliğine taşındı. Firma/kullanıcı kapsamındaki eski varsayılanlar yeni seçim veya kayıtla tek SaveChanges içinde audit edilir; firma geneli ve kullanıcıya özel varsayılanlar ayrı kapsamlarını korur.
- 🟢 **Kod düzeltildi:** Oluşturma/güncelleme/silme/varsayılan seçme tek aktif firma ve firma varlığı/silinmeme kontrolü kullanır. Mevcut kayıtlar açık FirmaId ve AsTracking ile yüklenir. Oluşturma sıfır PK, ad uzunluğu, enum ve pozitif isteğe bağlı kullanıcı kimliği doğrular; fresh entity gelen navigation/silinme metadata bilgisini taşımaz. Controller dönen yeni kimliği kullanır.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**. Önceki ekte açık bırakılan grup şablonu varsayılan kod işi tamamlandı.
- 🟡 **Kabul bekliyor:** Runtime testi yapılmadı. HTTP aktif firma/oturum kabulü, kullanıcı sahipliği/varlığı ve okuma/API yanıtlarının runtime kabulü, NoTracking/audit rollback, eşzamanlı DB tekilleştirme ve PostgreSQL açık. Oturum kullanıcı yetkisi ve özel/ortak kapsam doğrulaması sonraki eklerde kod olarak tamamlandı; çalışma zamanı kabulü gerekir. O-3 bütünüyle kapanmadı.

## Fatura grup şablonu kullanıcı sahipliği ve API reddi — 2026-10-04

- 🟢 **Kod düzeltildi:** Servis HTTP principal veya HTTP context yokken Blazor kimlik sağlayıcısı kullanır; pozitif kimlik ve DB'de aktif/silinmemiş kullanıcı doğrulanır. Anonim HTTP circuit kimliğine yükseltilmez. Gönderilen farklı kullanıcı kimliği reddedilir.
- 🟢 **Kod düzeltildi:** Firma listesi, kimlikle okuma ve dört yazım oturum kullanıcısının özel şablonları + firma geneli kapsamını korur. Null liste parametresi artık diğer kullanıcıların şablonlarını açmaz. Varsayılan okumada null yalnız firma genelini ifade eder; kullanıcı belirtilmişse yalnız oturum kullanıcısı seçilir. Kullanıcı sahipliği güncellemede değiştirilemez; Admin için başka kullanıcı özel şablonu muafiyeti yoktur.
- 🟢 **Kod düzeltildi:** Sahiplik reddi controller filtresinde sade 403 ProblemDetails yanıtına çevrilir. Request modeli ad/enum/pozitif kullanıcı doğrulamasıyla ApiController'ın 400 model hata yolunu kullanır.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi. Kaynak dosyalarında yalnız LF → CRLF normalizasyon uyarısı görüldü.
- 🟡 **Kabul bekliyor:** Runtime testi yapılmadı. Gerçek HTTP/circuit firma/oturum kabulü, kullanıcı A/B ve pasif kullanıcı senaryoları, firma geneli şablon yazım rol yetkisinin runtime kabulü, beklenen servis hatalarının API yanıtı runtime kabulü, eşzamanlı tekilleştirme ve PostgreSQL açık. Önceki ekteki kullanıcı sahipliği/varlığı ve özel okuma kapsamı kod eksikleri giderildi; tüm API kabulü tamamlanmadı.

## Fatura grup şablonu ortak yazım izni — 2026-10-04

- 🟢 **Kod düzeltildi:** Dört yazım metodunda firma geneli şablon için mevcut `faturahazirlik.duzenle` izni aranır. Güncelleme/silme/varsayılan seçiminde kontrol DB'deki mevcut şablon sahibine dayanır; kullanıcı alanını göndermek izin kontrolünü atlatmaz.
- 🟢 **Kod düzeltildi:** Aynı context'te aktif kullanıcı ve silinmemiş rol/yetki kayıtları sorgulanır. Mevcut Admin yetki kuralı korunur; diğer rol için izin true olmalıdır. Rol claim'i/önbellek kullanılmaz. Yeni izin/rol hakkı otomatik eklenmedi.
- 🟢 **Kod düzeltildi:** Reddetme alan değişikliği ve SaveChanges öncesindedir; mevcut controller filtresi 403 döndürür. Kişisel şablon sahipliği, ortak okuma ve controller lisans politikası korunur. Admin başka kullanıcının özel şablonuna erişim muafiyeti almaz.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi. Servis dosyasında LF → CRLF normalizasyon uyarısı görüldü.
- 🟡 **Kabul bekliyor:** Runtime testi yapılmadı. Yetkisiz/yetkili/Admin, rol/yetki kaldırma, gerçek HTTP/circuit ve PostgreSQL kabulü açık. Kontrol-yazım arasındaki eşzamanlı yetki değişimi ve DB tekilleştirme ayrıca ele alınmalıdır. Önceki ekteki ortak yazım yetkisi kod eksikliği tamamlandı; API kabulü kapanmadı.

## Fatura grup şablonu API giriş ve hata yanıtları — 2026-10-04

- 🟢 **Kod düzeltildi:** Beklenen şablon doğrulama/firma seçimi hataları `FaturaGrupSablonuException` ile ayrıldı. Boş/geçersiz ad, gruplama değeri, yeni kayıt kimliği, kullanıcı kimliği ve tek firma seçimi sorunları API filtresinde 400 ProblemDetails yanıtına dönüştürülür. Güncellemenin servis sorgusunda artık bulunamayan/erişilemeyen hedefi 404 döner; mevcut sahiplik/yetki reddi 403 olarak kalır.
- 🟢 **Kod düzeltildi:** Route şablon kimliği ve isteğe bağlı query kullanıcı kimliği için pozitif aralık doğrulaması eklendi. Serviste kimlikle okuma/güncelleme/silme/varsayılan seçme de pozitif kayıt kimliği ister. Controller'ın tüm servis çağrıları `HttpContext.RequestAborted` aktarır; güncellemenin ön okuması da aynı iptal sinyalini kullanır.
- 🟢 **Kod düzeltildi:** Filtre yalnız tanımlı kullanıcı hataları ve erişim reddini işler; beklenmeyen EF/altyapı hataları ile iptal istisnaları 400'e çevrilmez. Kullanıcıya altyapı istisnası mesajı açılmaz. Servis arayüzü açıklamaları mevcut özel/ortak okuma ve sahiplik koruma davranışıyla güncellendi.
- 🟢 **Derleme:** Son kaynakla `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj --no-restore -v:minimal` başarılı: **0 uyarı, 0 hata**.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek HTTP'de 400/403/404, seçili firma/oturum, istek iptali ve hata rollback'i kabulü açık. İptal sinyali tamamlanmış bir kaydı geri almaz. Eşzamanlı DB tek varsayılan kısıtı ve PostgreSQL kabulü de bekler; O-3 bütünüyle kapanmadı.

## Fatura şablonu okuma ve logo/kaşe yazım kapsamı — 2026-10-04

- 🟢 **Kod düzeltildi:** Logo yükleme/silme ve kaşe yükleme/silme artık `FindAsync` kullanmaz. Ortak yardımcı tek pozitif seçili firma ve erişilebilir/silinmemiş firma doğrular; şablonu açık FirmaId/silinmeme koşuluyla `AsTracking` yükler. Tüm firmalar modunda yazım reddedilir; başka firma veya silinmiş şablonda işlem false döner. Dört yol aynı context'te SaveChanges/audit akışını kullanır; güncelleme zamanları UTC oldu.
- 🟢 **Kod düzeltildi:** Kimlikle şablon okuma pozitif kayıt kimliği ve tek firma gerektirir; yalnız aynı firmadaki silinmemiş kaydı `AsNoTracking` getirir. Varsayılan okuma aynı firma kapsamındaki aktif/silinmemiş kayıtları kullanır; önce varsayılan, ardından kimlik sırası seçilir. Eski çoklu varsayılanlarda seçim sabittir; bu değişiklik eski kayıtları onarmaz.
- 🟢 **Kod düzeltildi:** Liste sorgusu salt okunur ve açık silinmeme koşulludur; tek firma modunda firma varlığı/kapsamı doğrulanır. Tüm firmalar liste görünümü mevcut query filter kapsamında korunur. Aynı adlı şablonlar kimlik sırasıyla kararlı listelenir. Arayüz açıklamaları yeni tek firma okuma/görsel yazım kuralını belirtir.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek firma seçimi, Tüm firmalar reddi, varsayılan NoTracking altında görsel güncelleme/audit rollback ve PostgreSQL kabulü açık. PDF fatura/şablon eşleşmesi, nesne overload'ı, önizleme ve e-posta firma kapsamı sonraki ekte kod olarak tamamlandı; çalışma zamanı kabulü bekler. DB tek varsayılan kısıtı ve eski kayıt onarımı bekler; O-3 bütünüyle kapanmadı.

## Fatura PDF, önizleme ve e-posta firma kapsamı — 2026-10-04

- 🟢 **Kod düzeltildi:** Gerçek PDF üretimi tek seçili firma gerektirir. Fatura aynı context'te açık FirmaId/silinmeme koşuluyla ve `AsNoTracking` yüklenir. Yüklenen firma/cari silinmiş veya cari/kalem firma ilişkisi uyumsuzsa PDF hazırlanmaz. Şablon yalnız aynı firmada aktif/silinmemiş olabilir. Açık şablon kimliği geçersiz/bulunamayan/pasif ise varsayılana sessizce geçilmez; kimlik verilmezse firma varsayılanı, sonra kimlik sırasıyla aktif şablon, hiç yoksa temel tasarım kullanılır.
- 🟢 **Kod düzeltildi:** Fatura/şablon nesnesi alan overload gelen tutar/cari/firma navigation ve tasarım içeriğini gerçek belgeye taşımaz; yalnız kayıt kimlikleriyle DB'den yeniden yükler. Kaydedilmemiş fatura/şablon reddedilir. Önizleme örnek fatura ve düzenlenen ayarlarla çalışmaya devam eder; tek firma, gelen FirmaId ve kayıtlı şablonun erişilebilir/silinmemiş kapsamı doğrulanır. Firma başlığı DB'den alınır; `FindAsync` kaldırıldı.
- 🟢 **Kod düzeltildi:** Tekli e-postanın konusu/gövdesi ve PDF eki aynı doğrulanmış fatura nesnesinden hazırlanır; farklı context'te ikinci fatura okuması kaldırıldı. Toplu gönderimde firma ve aktif şablon başta sabitlenir, yinelenen fatura kimlikleri tekilleştirilir; yalnız o firmada erişilebilir faturanın cari adresi kullanılır. Pozitif olmayan kimlik listesi gönderim başlamadan reddedilir; erişilemeyen/adresi olmayan faturalar atlanır ve dönüş mevcut gibi en az bir başarıyı bildirir.
- 🟢 **Kod düzeltildi:** PDF hazırlama/render hatalarının teknik mesajları kullanıcı sonucuna eklenmez; ayrıntı logger'a yazılır. Başarısız PDF için e-posta gönderimi denenmez.
- 🟢 **Derleme:** `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj --no-restore -v:minimal` başarılı: **0 uyarı, 0 hata**. Değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi veya gerçek e-posta gönderimi yapılmadı. Gerçek oturum/Tüm firmalar, farklı firma ve pasif şablon reddi, PDF görsel kabulü, önizleme, SMTP ve PostgreSQL kabulü bekler. Eski null/bozuk firma ilişkilerinin onarımı, global filtrelerin gizlediği ilişkilerin DB denetimi, eşzamanlı firma/şablon değişimi ve DB tek varsayılan kısıtı açık. Toplu gönderim kısmi başarılı olabilir; gönderilmiş e-postalar geri alınmaz. O-3 bütünüyle kapanmadı.

## Fiziksel dosya silme hatalarının bildirilmesi — 2026-10-04

- 🟢 **Kod düzeltildi:** Özlük dosyası silmede fiziksel silme hatasını yutan boş catch kaldırıldı. DB commit'i tamamlandıktan sonra silme başarısızsa `FileCleanupPendingException` döner; kayıt kimlikleri ve dosya yolu teknik hatayla birlikte sunucu logunda tutulur. Kullanıcıya fiziksel yol/teknik ayrıntı gösterilmez. Bu metoda ILogger eklendi; DB hedef sorguları `AsTracking` kullanır.
- 🟢 **Kod düzeltildi:** Şoför/personel detay dosya ekranı bu ayrı durumu “Dosya kaydı kaldırıldı; fiziksel dosya temizliği tamamlanamadı” uyarısıyla bildirir ve DB'den durum/dosya listesini yeniden yükler. Fiziksel silme hatasında “Dosya silindi” başarı bildirimi verilmez; DB değişikliği geri alınmış gibi gösterilmez.
- 🟢 **Kod düzeltildi:** `SecureFileService.DeleteAsync` iptal sinyalini başlangıçta kontrol eder. `File.Exists` ön kontrolü kaldırıldı; erişim/IO hatasının false sonucu nedeniyle silmenin sessizce atlanması engellendi. Güvenli yol çözümünden sonra doğrudan `File.Delete` kullanılır; bulunmayan dosya mevcut .NET davranışıyla işlem gerektirmez, hata logger'a yazılıp çağırana iletilir.
- 🟢 **Kod düzeltildi:** S3 silme metodu 404'ü işlem gerektirmeyen durum saymaya devam eder; diğer HTTP/iletişim/iptal hataları loglandıktan sonra çağırana iletilir. İstek ve yanıt dispose edilir. Başarısız silme artık başarılı tamamlanmış Task gibi bildirilmez.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kalan:** Bu turda runtime testi veya gerçek dosya/S3 silmesi yapılmadı. Disk erişim/kilit hatası, DB commit sonrası ekran yenileme, iptal ve S3 404/403/5xx kabulü açık. Kalıcı temizlik kuyruğu/yeniden deneme, mevcut yetim dosyaların taranması, log saklama süresi ve diğer servislerde dosya-DB sıralaması/upload telafisi ayrı işlerdir. Tedarikçi evrak silme sırası ve upload telafisi hata logu sonraki ekte kod olarak düzeltildi; kabulü bekler. Log bildirimi otomatik dosya temizliği değildir; bu dosya yaşam döngüsü işi bütünüyle kapanmadı.

## Tedarikçi evraklarında DB ve dosya silme sırası — 2026-10-04

- 🟢 **Kod düzeltildi:** Evrakın tüm dosyalarıyla silinmesi ve tek dosya silinmesi fiziksel silmeden önce DB değişikliğini kaydeder. Hedefler `AsTracking` yüklenir; UTC IsDeleted/DeletedAt/UpdatedAt alanları atanır. Evrak ve bağlı dosyaların değişiklikleri tek SaveChanges ile audit akışına girer; SaveChanges başarısızsa bu silme yolları fiziksel temizliğe başlamaz.
- 🟢 **Kod düzeltildi:** DB silme kaydı tamamlandıktan sonra ortak temizlik yardımcısı tüm dosyaları dener. Bir dosyanın hatası sonraki dosyaları durdurmaz. Her başarısız yol evrak/dosya kimliğiyle sunucu loguna yazılır; başarısızlıklar `FileCleanupPendingException` içinde toplanır. Soft delete edilen dosyaların yolları DB kaydında korunur.
- 🟢 **Kod düzeltildi:** Tedarikçi evrak ekranındaki tek dosya silme bu özel durumu uyarıyla gösterir ve evrak listesini/seçili dosya panelini yeniden yükler; fiziksel silme başarısızken başarı bildirimi vermez. Yükleme telafisindeki boş catch de log bildirimiyle değiştirildi; temizlenemeyen dosya yolu kaydedilir ve asıl yükleme istisnası korunur.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kalan:** Bu turda runtime testi veya gerçek dosya silmesi yapılmadı. Gerçek DB/audit rollback, varsayılan NoTracking, çok dosyalı kısmi temizlik ve ekran kabulü açık. Kalıcı temizlik kuyruğu/yeniden deneme, eski yetim dosyalar, diğer servisler, belirsiz upload commit sonucunda telafi güvenliği ve tam firma/yetki kabulü bekler. DB commit'i ile fiziksel silme tek atomik işlem değildir; tüm dosya yaşam döngüsü işi kapanmadı.

## Araç evraklarında DB ve dosya silme sırası — 2026-10-04

- 🟢 **Kod düzeltildi:** Araç belgesinin bağlı dosyalarıyla silinmesi ve tek dosya silinmesi önce DB/audit değişikliğini kaydeder. Hedefler `AsTracking` yüklenir; IsDeleted ve UTC DeletedAt/UpdatedAt alanları atanır. Belge ve bağlı dosyalar tek SaveChanges ile kaldırılır; ilk DB kaydı başarısızsa fiziksel silme başlamaz. Soft delete edilen dosyanın yolu kayıtta korunur.
- 🟢 **Kod düzeltildi:** Ortak temizlik yardımcısı her dosyayı ayrı dener; hata sonraki dosyaları durdurmaz. Başarısız yollar evrak/dosya kimliğiyle logger'a yazılır ve `FileCleanupPendingException` ile toplanarak bildirilir. Yükleme telafisinde temizlenemeyen dosya da loglanır; asıl yükleme hatası korunur.
- 🟢 **Kod düzeltildi:** Araç formunun belge/tek dosya silme ve araç evrak ekranının tek dosya silme yolları bu özel durumu uyarıyla gösterip DB listesini yeniler. Tek dosya paneli mevcut evrak listesinden güncellenir; fiziksel temizlik hatasında başarı bildirimi verilmez.
- 🟢 **Kod düzeltildi:** Belge tarihlerini senkronize eden araç hedefi `AsTracking` kullanır. Aktif plaka ve satış açma/kapatma yollarındaki aynı araç hedef sorguları da açık tracking kullanır; varsayılan NoTracking altında bu hedeflerin alan değişiklikleri kaydedilebilir. Bu değişiklik plaka/satış yollarının tüm kabulünü kapatmaz.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kalan:** Bu turda runtime testi veya gerçek dosya silmesi yapılmadı. DB/audit rollback, varsayılan NoTracking, disk kilidi/izin hatası, çok dosyalı temizlik ve üç ekran akışının kabulü açık. Belge değişikliği ile araç tarih senkronizasyonunun tek SaveChanges kullanması sonraki ekte kod olarak tamamlandı; rollback ve eşzamanlı yazım kabulü bekler. Yeniden deneme, kalıcı temizlik kuyruğu, eski yetim dosyalar, upload belirsiz commit telafisi, diğer servisler ve tam firma/yetki kabulü bekler.

## Araç belge değişikliği ve tarih senkronizasyonunun ortak kaydı — 2026-10-04

- 🟢 **Kod düzeltildi:** Araç evrak ekleme/güncelleme/silme önce belge değişikliğini ve araç üzerindeki muayene/trafik/kasko/koltuk tarihlerini aynı context'te hazırlar; ardından tek SaveChanges kullanır. Senkronizasyon yardımcısının ikinci SaveChanges çağrısı kaldırıldı. Böylece belge ve araç tarih değişiklikleri ortak DB/audit kayıt akışına girer; belge silmede fiziksel temizlik bu kaydın ardından başlar.
- 🟢 **Kod düzeltildi:** Tarih hesabı tüm erişilebilir/silinmemiş araç belgelerini tracking ile yükler, henüz kaydedilmemiş Added belgeleri referans kimliğiyle dahil eder. Pasif/tarihsiz/silinmiş filtreleri bellek üzerinde uygulanır; böylece güncellenen kategori/tarih, pasife alma/aktifleşme ve bekleyen soft delete sonucu doğru hesaba katılır. Silme durumundaki tracked kayıtlar hariç tutulur. Geçerli/erişilebilir araç yoksa kayıttan önce işlem reddedilir.
- 🟢 **Kod düzeltildi:** Evrak güncelleme hedefi açık AsTracking kullanır; varsayılan NoTracking altında alan değişiklikleri korunur. Eklemede henüz DB kimliği olmayan belgeler mevcut kayıtlara kimlik sıfır olduğu için karışmaz.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek DB/audit rollback, belge kategori/tarih/pasiflik değişimleri, son belge silindiğinde tarih temizlenmesi ve varsayılan NoTracking kabulü açık. Bu tek kayıt düzenlemesi eşzamanlı iki context'in tarih hesabını tekilleştirmez; PostgreSQL/SQL Server, tam firma/yetki kabulü, eski tutarsız veriler ve kalıcı dosya temizliği/yeniden deneme bekler. Dosya silme DB commit'inin dışındadır.

## Araç plaka geçmişi ve aktif plakanın ortak kaydı — 2026-10-04

- 🟢 **Kod düzeltildi:** Plaka nesnesiyle ekleme, silme ve tarihli kapatma yollarındaki kayıt öncesi/sonrası iki SaveChanges kaldırıldı; plaka geçmişi ve araç AktifPlaka tek SaveChanges ile audit/transaction akışına girer. PlakaEkle ve PlakaCikis de aynı hesap yardımcısını kullanır. Yazım hedefleri açık AsTracking yüklenir; yardımcı kendi başına kayıt yapmaz.
- 🟢 **Kod düzeltildi:** Aktif plaka hesabı tracked geçmiş ve henüz kaydedilmemiş Added kayıtları referans kimliğiyle birleştirir. Silinmiş/deleted kayıtları çıkarır; çıkış tarihi filtresini bellek üzerinde uygular. Giriş tarihi, eklenen kayıt önceliği ve kimlik ile seçim sabittir. PlakaCikis'in o işlemde kapattığı kayıt ayrıca hariç tutulur; başka uygun aktif geçmiş varsa seçilir, yoksa araç plakası temizlenir.
- 🟢 **Kod düzeltildi:** PlakaEkle geçerli/erişilebilir araç ve boş olmayan plaka ister; plaka kırpılıp büyük harfe çevrilir. Nesneyle eklemede sıfır olmayan yeni kayıt kimliği reddedilir; gönderilen kimlikle mükerrer kontrolünü atlama yolu kaldırıldı. Silmede UTC DeletedAt/UpdatedAt birlikte atanır. Aktif araç plaka alanı değiştiğinde UpdatedAt güncellenir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Ekleme/silme/kapatma/çıkış, NoTracking ve ortak audit rollback kabulü açık. Gün bazlı aktiflik ile saat içeren çıkış tarihlerinin semantiği, ileri giriş tarihleri, UTC/yerel gün farkı, eski çoklu aktif kayıtlar, cari/firma doğrulamasının runtime kabulü ve cache runtime kabulü ayrıca ele alınmalı; AracService yazım sonrası temizliği sonraki ekte kod olarak tamamlandı. Beş özel plaka yazımının firma/cari kontrolü ve iki eklemenin aynı context mükerrer sorgusu sonraki ekte kod olarak tamamlandı; eşzamanlı yazım ve DB tekillik kısıtı tamamlanmadı. Tam oturum ve PostgreSQL/SQL Server kabulü bekler.

## Plaka yazımlarında seçili firma ve cari doğrulaması — 2026-10-04

- 🟢 **Kod düzeltildi:** PlakaEkle, AddPlakaToAracAsync, DeletePlakaFromAracAsync, ClosePlakaAsync ve PlakaCikis tek pozitif seçili firma gerektirir. Tüm firmalar/seçimsizlik reddedilir; firma mevcut/silinmemiş ve query filter kapsamında erişilebilir olmalıdır. Firma kimliği işlem başında alınır; araç hedefleri açık FirmaId, plaka hedefleri aynı firmadaki silinmemiş araç ilişkisiyle sınırlandırılır. Query filter devre dışı bırakılmaz.
- 🟢 **Kod düzeltildi:** Yeni plaka ekleme ve çıkış işlemlerinde isteğe bağlı cari kimliği pozitif, aynı firmaya ait ve silinmemiş/erişilebilir olmalıdır. Çıkışta yeni cari verilmezse mevcut cari ilişkisi de doğrulanır; geçersiz ilişkiyle yeni işlem alanları kaydedilmez. Null cari desteklenir.
- 🟢 **Kod düzeltildi:** İki plaka ekleme yolunun mükerrer kontrolü ayrı servis/context çağrısı yerine aynı context ve yakalanmış firma kimliğiyle çalışır. Mevcut firmaya ait silinmemiş araçların aktif plaka kayıtları kontrol edilir. Diğer genel plaka okuma/araç oluşturma yollarının politikası bu özel yazım kontrolünün kapsamı değildir.
- 🟢 **Kod düzeltildi:** Genel araç güncellemesinde aktif plaka yardımcısının çağrısı araç SaveChanges öncesine taşındı; hesaplanan plaka alanı aynı araç kaydında saklanır. Bu çağrının mevcut query filter kapsamı korunur; beş özel plaka yazımı açık firma parametresi verir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek HTTP/circuit firma seçimi, farklı firma araç/cari reddi, silinmiş kayıtlar, null cari ve rollback kabulü açık. Eski plaka/cari ilişkilerinin onarımı, genel araç firma değiştirme politikası, tarih semantiği, cache tazeliğinin runtime kabulü, eşzamanlı DB tekillik ve PostgreSQL/SQL Server kabulü bekler. Aynı context'teki mükerrer sorgu DB seviyesinde tekillik garantisi değildir.

## Araç yazımlarından sonra önbellek temizliği — 2026-10-04

- 🟢 **Kod düzeltildi:** AracService içindeki başarılı araç/plaka/satış/belge/dosya DB yazımları sonrası ortak araç önbelleği temizleme çağrısı eklendi. Araç oluşturma ve firmasız araç backfill'inin mevcut temizliği de aynı yardımcıyı kullanır. `CacheKeys.AracPrefix` tüm firma ve Tüm firmalar araç liste/aktif anahtarlarını kapsar; değişiklik yalnız o anda seçili ekran anahtarıyla sınırlanmaz.
- 🟢 **Kod düzeltildi:** Temizlik SaveChanges sonrasında, açık transaction kullanılan araç oluşturma yolunda commit sonrasında çalışır. Belge/dosya silme yollarında DB kaydı sonrası önbellek temizliği fiziksel silmeden öncedir; disk hatası DB'deki kaldırma durumunun önbellek temizliğini atlatmaz. Helper hatası logger'a yazılır; tamamlanan DB kaydı cache hatası yüzünden başarısız yazım gibi gösterilmez.
- 🟢 **Kod düzeltildi:** Excel araç aktarımı tamamlanan satır commit'lerini işaretler ve işlem sonunda bir kez temizlik yapar. Diğer satırlar başarısız veya dış okuma hatası oluşmuş olsa da önceki başarılı satırlar varsa temizlik denenir. Hiç commit yoksa temizlik çağrılmaz; satır başına önbellek silme yükü eklenmedi.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek liste/aktif görünüm, firma değişimi, kısmi Excel aktarımı ve disk hatası sonrası liste kabulü bekler. CacheService backend hatasını loglayıp yutabilir; bu çağrılar başarısız depoda tazelik garantisi vermez. Aynı süreçte temizlik sonrası eski factory yayınlama koruması sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. Süreçler arası Redis anahtar takibi/sürümü, yeniden deneme ve başka servislerden araç yazımları açık.

## Önbellek temizliği sonrası eski sorgu sonucunun yayınlanması — 2026-10-05

- 🟢 **Kod düzeltildi:** CacheService aynı süreçte tüm scope'lar arasında ortak temizlik sürümü kullanır. GetOrSetAsync sürümü cache okumasından önce alır; temizlik araya girerse cache hit kullanılmaz. Factory sonucu yayınlanırken sürüm tekrar kontrol edilir; arada RemoveAsync/RemoveByPrefixAsync çalışmışsa eski sonuç cache'e yazılmaz. Tracker'da henüz bulunmayan ilk anahtar hesaplaması da sürüm değişiminden etkilenir.
- 🟢 **Kod düzeltildi:** Cache set ve temizlik işlemleri ortak SemaphoreSlim ile sıralanır. Sürüm kontrolü, backend set ve anahtar takibi aynı kilitte tamamlanır; kontrol ile set arasına temizlik giremez. Factory ve serileştirme kilit dışında çalışır. Doğrudan SetAsync/sliding set de aynı yayınlama kilidini kullanır; GetOrSet factory sürüm koruması ayrı olarak uygulanır.
- 🟢 **Kod düzeltildi:** Prefix temizliğinde bir anahtarın backend hatası sonraki anahtarların temizliğini durdurmaz. Başarısız anahtar tracker'da tutulur ve logger'a yazılır; yeniden temizlikte tekrar denenebilir. Set/remove kilitleri finally ile bırakılır. Bu yollar ve GetOrSet iptal edilen çağrıyı başarılı yayınlama gibi tamamlamaz.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Factory/temizlik/set yarışının gerçek backend ve farklı scope kabulü, iptal ve cache kesintisi kabulü açık. Başlamış factory sonucu kendi çağıranına dönebilir; bu değişiklik ekran yükleme sürüm kontrolünün yerine geçmez. Sürüm süreç geneline aittir; ilgisiz anahtar temizliği de devam eden factory'nin cache yayınını atlatabilir. Backend yazımları ortak kilitte sıralandığından yük/Redis gecikmesi kabulü gerekir. Çok süreçli Redis sürümü/anahtar takibi, mevcut stale backend verisi ve otomatik yeniden deneme açık.

## Araç liste önbelleğinde firma seçiminin sabitlenmesi — 2026-10-05

- 🟢 **Kod düzeltildi:** GetAllAsync/GetActiveAsync ortak yükleme yardımcısını kullanır. Firma kimliği ve Tüm firmalar modu çağrı denemesi başında yerel değerlere alınır; cache anahtarı bu değerlerden üretilir. Tek firma sorgusunda açık FirmaId koşulu aynı yakalanmış kimliği kullanır; EF global filtreleri korunur. Firma seçilmemişse boş liste döner, F0 cache kaydı oluşturulmaz.
- 🟢 **Kod düzeltildi:** Yükleme boyunca geçici firma değişim aboneliği seçim sürümünü artırır. Context oluşturma öncesi/sonrası, DB sorgusu sonrası, factory sonucu öncesi ve cache hit dönüşünde seçim kontrol edilir. Değişen seçimde factory tamamlanmaz ve sonuç cache'e yazılmaz; çağrı en fazla üç deneme ile yeni seçimi yükler. Aynı firmaya dönülmüş olsa da olay sürümü aradaki değişimi yakalar. Abonelik finally ile kaldırılır.
- 🟢 **Kod düzeltildi:** Anahtarlar araç öneki altında Scope2 sürümünü kullanır; önceki kapsam yarışıyla doldurulmuş eski anahtarlar bu okumalarda kullanılmaz. Tüm firmalar görünümü mevcut query filter politikasını kullanır. Aktif plaka ve araç liste sıralamasına kimlik eşitlik sırası eklendi; iki okuma aynı hesap akışını paylaşır.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek circuit/firma/Tüm firmalar geçişleri, cache hit/miss sırasında A→B→A değişimi ve farklı scope kabulü bekler. Üç denemede seçim sabitlenmezse yeniden yükleme yönergesi içeren hata döner; çağıran ekranların hata ve yükleme sürümü yönetimi ayrıca kabul edilmeli. Ana araç listesinde servis sonrası seçim/sürüm ve hata bildirimi sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. Diğer ekran sonuç yayınları, provider'ın olay dışı doğrudan mutasyonu, çok süreçli Redis, backend arızası ve diğer cache kullanan servisler açık.

## Araç listesinde eski yükleme sonuçları ve hata bildirimi — 2026-10-05

- 🟢 **Kod düzeltildi:** Araç listesi her yüklemede artan sürüm ve başlangıç firma/Tüm firmalar değerlerini kullanır. Araçlar, firmasız araç sayısı ve firma seçenekleri çağrıya özel yerel alanlarda hazırlanır; yalnız güncel yükleme sonucu ekran alanlarına aktarır. Kontrol her bekleme sonrasında yapılır; eski çağrı yeni listenin yükleme göstergesini kapatamaz.
- 🟢 **Kod düzeltildi:** Firma değişimi eski çağrıyı olay anında geçersizleştirir; UI dispatcher üzerinden firma filtresi, eski liste ve silme/plaka geçmişi seçimleri temizlenerek yeniden yükleme yapılır. Başlayan yükleme eski veriyi temizler ve yükleme durumunu render eder. Dispose sürümü geçersizleştirir ve firma olay aboneliğini kaldırır; bekleyen liste sonucu kapatılmış ekrana aktarılmaz.
- 🟢 **Kod düzeltildi:** Ana liste hatası try/catch/finally ile ele alınır; teknik ayrıntı logger'a gider, güncel kullanıcı hatası kalıcı role=alert uyarısı ve Yeniden Dene düğmesiyle bildirilir. Yardımcı firma seçenekleri/firmasız sayısı hataları da logger ve uyarıyla açıklanır; araçlar yüklenmişse liste gösterilmeye devam eder. Teknik istisna mesajı kullanıcı uyarısına eklenmez.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime/UI testi yapılmadı. Hızlı firma/dönem/yenileme geçişleri, cache hit/miss, ana/yardımcı sorgu arızası, yeniden deneme ve Dispose kabulü bekler. Başlamış sorgular iptal edilmez. Plaka geçmişi modalının ekleme/silme/kapatma sonuç koruması sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. Araç silme sonuç koruması sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. Import sonuç koruması son ekte kod olarak tamamlandı; runtime kabulü ve import servisinin firma kapsamı denetimi bekler. Backfill yetki/firma kabulü ayrı iştir. Bu değişiklik ana liste yükleme akışını kapsar.

## Araç plaka geçmişi modalında bekleyen yazım sonuçları — 2026-10-05

- 🟢 **Kod düzeltildi:** Plaka ekleme/silme/kapatma ortak işlem yardımcısını kullanır. Araç kimliği, modal sürümü ve firma/Tüm firmalar seçimi işlem başında yakalanır; servis ve liste yenilemesi sonrasında güncellik kontrol edilir. Modal kapatma, yeniden açma, firma olayı ve Dispose eski sonucu geçersizleştirir. Eski çağrı yeni modalın verisini, formunu veya işlem göstergesini değiştiremez.
- 🟢 **Kod düzeltildi:** Yeni plaka isteği form nesnesinden ayrı hazırlanır. Silme/kapatma hedefinin seçili aracın geçmişinde olması kontrol edilir; tek seçili firma ve araç firma eşleşmesi gerekir. Kayıt sürerken form ve satır işlemleri kilitlenir; modal kapatılabilir. Kapatma öncesinde ekrandaki plaka nesnesinin çıkış tarihi değiştirilmez.
- 🟢 **Kod düzeltildi:** Yenileme sonrası araç yakalanmış kimlikle bulunur; yenileme başka yüklemeyle geçersizleşmişse veya araç bulunamazsa modal kapanır. Silme dahil üç yolun hatası logger'a yazılır; yalnız güncel modal genel hata bildirimi gösterir, teknik istisna ayrıntısı kullanıcıya aktarılmaz.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata** ile tamamlandı. Değişen üç dosyanın `git diff --check` kontrolü boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime/UI testi yapılmadı. Bekleyen ekleme/silme/kapatma sırasında modal kapatma/başka araç açma, A→B→A firma geçişi, servis/liste arızası, çift tıklama ve Dispose kabulü açık. Başlamış servis yazımı iptal edilmez veya geri alınmaz; modal kapansa da DB kaydı tamamlanabilir. Koruma bu modalın sonuç yayınına aittir; diğer ekranların yazımları, araç import/silme, backfill yetkisi ve eşzamanlı DB tekillik ayrıca bekler.

## Personel Banka Ödeme Listesi PDF/yazdırma düzeni — 2026-10-05

- 🟢 **Kod düzeltildi:** PDF düğmesinin kullandığı tarayıcı yazdırmasına bu sayfaya özel, yalnız print ortamında yüklenen stil eklendi. A4 yatay ve 8 mm kenar boşluğu tanımlandı; menü, üst çubuk, filtre/işlem düğmeleri, modal ve toast çıktıda gizlenir. Ana yerleşimin flex/genişlik/padding ve tablo kapsayıcısının taşma kısıtları yazdırmada kaldırılır.
- 🟢 **Kod düzeltildi:** On beş sütun sabit yüzde genişlikleriyle sayfaya dağıtılır; uzun metinler satıra sarılır. Başlık satırı sonraki sayfalarda tekrarlanır, satırlar sayfa sınırında bölünmez; genel toplam tekrar eden footer yerine listenin sonunda basılır. Görev dağılımı ve ödeme özeti raporda korunur.
- 🟢 **Kod düzeltildi:** Basılı başlık seçili ay/yıl, görev ve bordro filtresini gösterir. Düğme PDF / Yazdır olarak adlandırıldı; yükleme veya boş liste sırasında çalışmaz. Yazdırma hatası teknik ayrıntısı logger'a, genel bildirim kullanıcıya gider. Ödeme hesapları ve liste verisi değiştirilmedi.
- 🟢 **Derleme:** Ayrı geçici çıktı klasörüne `UseAppHost=false` ile Web Debug derlemesi **0 uyarı, 0 hata** tamamlandı; `git diff --check` boşluk hatası bildirmedi. İlk normal derleme çalışan Web EXE kilidi nedeniyle MSB3027/MSB3021 ile durdu. Çalışan uygulama durdurulmadı; yeni Razor çıktısının kullanılması için uygulama yeniden derlenip başlatılmalı.
- 🟡 **Kabul bekliyor:** Bu turda tarayıcı yazdırma önizlemesi veya gerçek PDF çıktısı alınmadı. Çok sayfalı liste, uzun ad/adres, büyük tutarlar, SGK ayrı/birleşik görünüm ve Chrome/Firefox kabulü bekler. PDF kaydı tarayıcının hedef seçiminden yapılır; yönlendirme kullanıcı tarafından elle değiştirilebilir.

## İhale raporlarında profesyonel Excel ve PDF çıktısı — 2026-10-05

- 🟢 **Kod düzeltildi:** Enflasyonlu projeksiyon kartına Excel ve PDF düğmeleri eklendi. Çıktılar ekranda yüklenmiş aynı özet verisini kullanır; seçili proje ile rapor kimliği eşleşmeden aktarım yapılmaz. Proje adı/kodu, müşteri, sözleşme süresi, hat sayısı ve oluşturma zamanı raporda yer alır.
- 🟢 **Kod düzeltildi:** Projeksiyon çıktısı Proje Özeti, Aylık Projeksiyon ve Hat Detayları bölümlerini içerir. İlk/son ay maliyeti, süre sonu artış tutarı/oranı ve proje toplamları korunur. Kümülatif toplamlar aylık satırlardan tekrar toplanmaz; son ayın kümülatif değerleri kullanılır. Hesaplama servisi değiştirilmedi.
- 🟢 **Kod düzeltildi:** Ortak Excel sunumu ayrı çalışma sayfaları, başlık/rapor bilgileri, şeritli filtreli tablolar, sabit üst satırlar, metin sarma, sınırlı sütun genişlikleri, sayısal hücreler ve iki ondalıklı negatif sayı formatı kullanır. A4 yatay, bir sayfa genişliği ve tekrarlanan başlık satırları yazdırma ayarına kaydedilir. Metin hücreleri formül olarak atanmaz.
- 🟢 **Kod düzeltildi:** PDF gerçek PDF dosyası olarak QuestPDF ile oluşturulup indirilir. Bölüm başlıkları, tekrarlanan tablo başlıkları, dönüşümlü satır rengi, vurgulu toplamlar, Türkçe sayı biçimi ve sayfa numaraları kullanılır. Normal tablolar A4 yatay, dokuzdan fazla sütunlu gerçekleşen analiz A3 yatay hazırlanır; uygulama menüsü PDF'ye girmez.
- 🟢 **Kod düzeltildi:** Gerçekleşen/Tekliflenen Analiz ve İhale Sonrası Operasyon Özeti de Excel/PDF düğmeleriyle aynı sunumu kullanır. Operasyon çıktısında göstergeler ve riskli projeler ayrı bölümlere ayrılır. Aktarım sırasında düğmeler kilitlenir; teknik hata logger'a, genel hata kullanıcıya gider. Mevcut teklif versiyonu export servisinin onay/izin kontrolleri korunur; bu ek ekran analizlerini kapsar.
- 🟢 **Derleme:** Son kaynak ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** derlendi. `git diff --check` boşluk hatası bildirmedi. Çalışan Web uygulaması durdurulmadı; yeni ekranı kullanmak için normal çıktı yeniden derlenip uygulama başlatılmalı.
- 🟡 **Kabul bekliyor:** Bu turda gerçek XLSX/PDF açma veya görsel çıktı kabulü yapılmadı. Uzun proje/hat adları, boş risk listesi, çok sayfalı projeksiyon, büyük/negatif tutarlar, Excel baskısı ve ekran/çıktı değerlerinin kullanıcı kabulü bekler. Çıktı anında DB yeniden sorgulanmaz; rapor ekranda yüklü veriyi sunar. Müşteriyle paylaşmadan önce raporun güncelliği kontrol edilmelidir.

## Araç silmede firma kapsamı, tracking ve bekleyen sonuçlar — 2026-10-05

- 🟢 **Kod düzeltildi:** AracService.DeleteAsync pozitif araç kimliği ve tek seçili firma ister; firma kimliği context oluşturulmadan önce yakalanır. Firma ve araç global filtreler içinde erişilebilir/silinmemiş olmalıdır; araç açık FirmaId koşuluyla yüklenir. Tüm firmalar/seçimsizlik ve başka firmaya ait araç reddedilir. Bulunmayan hedef sessiz başarı gibi tamamlanmaz.
- 🟢 **Kod düzeltildi:** Araç ve dahil edilen plaka geçmişi AsTracking yüklenir. Araç soft delete, UTC DeletedAt/UpdatedAt, aktif plaka temizliği ve geçmişte açık plaka çıkışı ortak SaveChanges akışında kaydedilir. Başarılı kayıt sonrası mevcut araç önbelleği temizliği korunur. Plaka tarih semantiği bu ekte değiştirilmedi.
- 🟢 **Kod düzeltildi:** Araç listesinin silme onayı kendi modal sürümünü kullanır. Kimlik ve firma seçimi işlem başında yakalanır; servis/liste yenilemesi sonrası güncellik kontrol edilir. Modal kapatma/başka araç açma, firma olayı ve Dispose eski sonucu geçersizleştirir; eski çağrı yeni modalı kapatamaz veya yükleme göstergesini değiştiremez. Aynı açık modalda ikinci silme engellenir.
- 🟢 **Kod düzeltildi:** Silme hatası try/catch/finally ile ele alınır; teknik ayrıntı logger'a, genel bildirim yalnız güncel modalın kullanıcısına gider. Başarılı kayıt sonrası liste yenilenir; yenileme hatası ana listenin mevcut uyarı/yeniden deneme akışında kalır.
- 🟢 **Derleme:** Son kaynak ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** derlendi; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul bekliyor:** Bu turda runtime veya gerçek araç silme yapılmadı. NoTracking varsayılanı, DB/audit rollback, farklı firma/Tüm firmalar reddi, bulunmayan araç, çift tıklama, modal/firma geçişi ve Dispose kabulü açık. Başlamış silme iptal edilmez; modal kapansa da DB yazımı tamamlanabilir. İlişkili operasyonların araç soft delete politikasının kabulü, HTTP/yetki kabulü, Excel aktarım sonuç koruması ve backfill yetkisi ayrı işlerdir.

## Araç Excel aktarımında modal ve seçim sonuç koruması — 2026-10-05

- 🟢 **Kod düzeltildi:** Dosya nesnesi, modal sürümü ve firma/Tüm firmalar seçimi işlem başında yakalanır. Dosya okuması ve servis çağrısı sonrasında güncellik kontrol edilir; dosya okunurken seçim değişmişse servis yazımı başlatılmaz. Modal açma/kapatma, yeni dosya, firma olayı ve Dispose eski sonucu geçersizleştirir.
- 🟢 **Kod düzeltildi:** Ekran genelinde aktarım kilidi modal kapansa da işlem bitene kadar korunur; çift başlatma ve bekleyen işlem sırasında dosya değiştirme engellenir. Açılan modal işlem sürdüğünü ve kapatmanın kayıtları geri almadığını açıklar. Yalnız kilidi tutan aktarım finally içinde kilidi bırakır; Dispose sonrası render yapılmaz.
- 🟢 **Kod düzeltildi:** Sonuç yerel değişkende tutulur ve yalnız güncel modalda yayınlanır. Hatalı/kısmi sonuç başarı bildirimi yerine uyarı verir; eklenmiş/güncellenmiş satır varsa Success=false olsa da liste yenilenir. UI istisnası logger'a, genel hata güncel modala gider.
- 🟢 **Derleme:** Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** tamamlandı; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul bekliyor:** Runtime/UI testi veya gerçek Excel aktarımı yapılmadı. Dosya okurken/modal veya firma A→B→A geçişi, iki tıklama, kısmi commit, okuma hatası, modalı yeniden açma ve Dispose kabulü açık. Başlamış servis yazımı iptal edilmez; DB kaydı sürebilir. Import servisinin firma kimliğini işlem boyunca sabitlemesi, sorgular/yeni kayıtlar için açık firma koşulu ve servis hata ayrıntılarının kullanıcı sunumu ayrıca denetlenmeli; bu ek UI sonucu kapsamındadır.

## A-13 devamı — araç import servisinde sabit firma ve tracked yazım — 2026-10-05

- 🟢 **Kod düzeltildi:** ImportFromExcelAsync boş/10 MB üzeri veriyi ve tek pozitif firma seçimi olmayan çağrıyı reddeder. Firma kimliği context oluşturulmadan önce yakalanır; firma erişilebilir/silinmemiş olmalıdır. Şase, güncelleme ve aktif plaka sorguları açık firma koşuluyla çalışır; yeni araç FirmaId alanı yakalanmış kimlikle atanır. Global filtreler kaldırılmaz.
- 🟢 **Kod düzeltildi:** İşlem boyunca geçici firma olayı aboneliği seçim sürümünü artırır; A→B→A değişimi de yakalanır. Satır/transaction başında, kayıt öncesi ve SaveChanges sonrası commit öncesinde seçim doğrulanır. Değişim fark edilince mevcut transaction commit edilmez, kalan satırlar durur; daha önce commit edilmiş satırlar geri alınmaz. Abonelik finally ile kaldırılır.
- 🟢 **Kod düzeltildi:** Güncelleme hedefi ve plaka geçmişi AsTracking yüklenir; hedef kaybolmuş/gizlenmişse başarılı güncelleme sayılmaz. ExecutionStrategy her denemede tracker'ı temizleyip hedefi tekrar yükler. Satır hata ayrıntısı logger'a gider; kullanıcıya satır kimliği ve genel hata/firma değişimi yönergesi verilir. Satır hatası varsa Success=false döner; önceki kısmi kayıtları UI yenilemesi kapsar.
- 🟢 **Derleme:** Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** tamamlandı; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul bekliyor:** Gerçek Excel/DB/circuit testi yapılmadı. Firma değişimi ile SaveChanges/commit'in aynı anda gerçekleşmesi, audit tenant bilgisi, NoTracking, retry/commit belirsizliği, eski şase/plaka ilişkileri ve eşzamanlı DB tekillik kabulü açık. Kontroller başlamış commit'i iptal veya geri alma garantisi değildir; sağlayıcı/global filtre/audit bütünlüğünün gerçek kabulü gerekir. Backfill yetkisi ve genel araç firma değiştirme denetimi A-13 altında açık kalır.

## A-13 devamı — firmasız araç atamasında Admin ve hedef firma kontrolü — 2026-10-05

- 🟢 **Kod düzeltildi:** BackfillFirmaIdAsync HTTP varsa HTTP kimliğini, circuit çağrısında AuthenticationStateProvider kimliğini kullanır. Pozitif kullanıcı kimliği ve DB'de aktif/silinmemiş kullanıcı, silinmemiş Admin rolü gerekir; yalnız claim'deki rol yeterli değildir. Yetki kayıt öncesinde tekrar doğrulanır. Diğer kullanıcılar firmasız araç sayısını da alamaz; sıfır döndüğü için bakım düğmesi gösterilmez.
- 🟢 **Kod düzeltildi:** Hedef kimlik tek seçili firma ile aynı olmalıdır; hedef firma global filtre kapsamında mevcut/silinmemiş olmalıdır. Filtre atlama yalnız yetkili bakım yolunda, FirmaId=null ve silinmemiş araç sorgusuna uygulanır. Araçlar AsTracking yüklenir; firma/UpdatedAt ortak SaveChanges/audit akışında kaydedilir ve ardından cache temizlenir.
- 🟢 **Kod düzeltildi:** Geçici firma olayı aboneliği A→B→A geçişini de geçersizleştirir; seçim kayıt öncesi doğrulanır, abonelik finally ile kaldırılır. UI işlem kilidi onaydan önce alınır; onay ve servis sonucu liste sürümü/firma seçimiyle kontrol edilir. Eski sonuç başka firma ekranında başarı/yenileme başlatmaz. Teknik hatalar logger'a, genel bildirim güncel seçime gider.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** derlendi; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Atama düğmesi işlem/yükleme sırasında devre dışıdır. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul ve kapsam:** Runtime testi veya gerçek backfill yapılmadı. HTTP/circuit kimliği, normal/Admin/pasif kullanıcı, rol kaldırma, NoTracking, audit rollback ve eşzamanlı firma seçimi kabulü açık. Başlamış kayıt iptal edilmez. İlişkili mevcut tenant verilerinin tutarlılığı ve eşzamanlı atama kabulü ayrıca yapılmalı; genel UpdateAsync firma değiştirme yolu bu ekte değiştirilmedi ve A-13'te açık kalır.

## A-13 devamı — genel araç güncelleme ve taşıma giriş kontrolleri — 2026-10-05

- 🟢 **Kod düzeltildi:** UpdateAsync pozitif araç kimliği, boş olmayan şase, tek seçili firma ve aktif/silinmemiş DB kullanıcısı ister. Genel güncelleme seçili firmadaki tracked araçla sınırlıdır. Bu ekteki Admin için hedef kaydı filtresiz yeniden açma yolu aşağıdaki ortak kayıt düzeltmesinde kaldırıldı; firma değişikliği taşıma servisinden yürütülür.
- 🟢 **Kod düzeltildi:** Hedef firma mevcut/silinmemiş olmalıdır; kira/komisyon carileri pozitif, hedef firmaya ait ve silinmemiş olmalıdır. Null cari desteklenir; geçersiz cari sessizce null yapılmaz. Şase mükerrer sorgusu aynı context'te yapılır, yanlış firma sessizce yok sayılmaz. Kayıt öncesi seçili firma tekrar kontrol edilir. Teknik DB hatası logger'a gider; kullanıcıya genel hata verilir.
- 🟢 **Kod düzeltildi:** MoveAracToFirmaAsync öncesinde aktif DB Admin yetkisi, tek seçili kaynak firma, kaynak firmada erişilebilir tracked araç, mevcut/silinmemiş ve farklı hedef firma ile izinli taşıma anahtarları kontrol edilir. Kira/komisyon bağlantıları hedefe uygun değilse taşıma başlamadan reddedilir; otomatik cari silme/eşleme yapılmaz. Puantaj ve servis çalışma hedefleri AsTracking yüklenir. Hazırlık hataları varsa SaveChanges yapılmaz; form da bu durumda ayrı UpdateAsync çağrısını başlatmaz.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** derlendi; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kalan uygulama/kabul:** Runtime veya gerçek taşıma yapılmadı. Bu ekte iki ayrı SaveChanges olarak kaydedilen taşıma/form güncellemesi, aşağıdaki ortak kayıt düzeltmesinde birleştirildi. İlişkili cari/plaka/puantaj/personel/operasyon kayıtlarının hedef firma tutarlılığı ve seçilmeyen kayıtların politikası ayrıca tamamlanmalı. Güzergâh taşıma yolu bu ekte değiştirilmedi. Rol/firma değişiminin yazımla yarışı, A→B→A, eski bozuk ilişkiler, tam HTTP/circuit ve DB/audit rollback kabulü bekler. Başlamış commit bu kontrollerle geri alınmaz; A-13 bütünüyle kapanmadı.

## A-13 devamı — araç taşıma ve form alanlarının ortak kaydı — 2026-10-05

- 🟢 **Kod düzeltildi:** Araç formundaki iki taşıma onayı güncel form alanlarını MoveAracToFirmaAsync çağrısına verir; taşıma sonrasında ikinci UpdateAsync çağrısı kaldırıldı. Form alanları, kaynak firma/kayıt izi ve seçilen ilişkili değişiklikler aynı context içinde hazırlanıp tek SaveChanges/audit akışında kaydedilir. Hazırlık hatasında hiçbir değişiklik kaydedilmez.
- 🟢 **Kod düzeltildi:** Form kimliği ve hedef firma eşleşmesi kontrol edilir; değiştirilebilir alanların kopyası ilk await öncesinde alınır. Pozitif olmayan cari kimlikleri reddedilir; kira/komisyon carileri hedef firmada mevcut ve silinmemiş olmalıdır. Şase mükerrerliği kayıt öncesinde kontrol edilir. Genel UpdateAsync farklı firmaya yazımı reddeder ve taşıma akışına yönlendirir.
- 🟢 **Kod düzeltildi:** Firma olayı sürümü A→B→A seçimini de geçersizleştirir; geçici abonelik finally ile kaldırılır. Aktif DB Admin yetkisi kayıt öncesinde yeniden doğrulanır. Kayıt sonrası cache temizleme hatası logger'a gider; tamamlanmış DB kaydı başarısız taşıma olarak bildirilmez.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi. git diff --check boşluk hatası bildirmedi.
- 🟡 **Açık kapsam/kabul:** Runtime testi veya gerçek taşıma yapılmadı; çalışan uygulama yeniden başlatılmadı. İlişkili ve seçilmeyen kayıtların tenant politikası, eski cari/plaka/personel/operasyon bağlantıları, eşzamanlı taşıma, rol/firma değişiminin commit ile yarışı ve DB/audit rollback kabulü açık. Güzergâh taşıma yolu bu ekte değiştirilmedi. Başlamış commit'i geri alma garantisi verilmez; A-13 genel durumu sarıdır.

## A-13 devamı — araç evrak/dosya firma bütünlüğü ve envanter yetkisi — 2026-10-05

- 🟢 **Kod düzeltildi:** Taşıma kodundaki eski “AracEvrak FirmaId taşımaz” varsayımı kaldırıldı. Silinmemiş araç evrakları ve bu evrakların silinmemiş dosyaları AsTracking yüklenir; her iki kaydın FirmaId ve UpdatedAt alanları araçla aynı SaveChanges/audit akışında hedefe aktarılır. Taşınan kayıt sayısı dosyaları da kapsar; fiziksel dosya yolu değiştirilmez.
- 🟢 **Kod düzeltildi:** Evrak/dosya kaynak firma kimliği araçla eşleşmiyorsa (boş kimlik dahil) işlem kayıt öncesinde reddedilir. Mevcut evraklar seçilmeden araç taşınamaz; UI bu seçimi zorunlu ve devre dışı onay kutusuyla gösterir. Servis aynı kuralı doğrudan çağrıda da uygular.
- 🟢 **Kod düzeltildi:** GetAracTransferItemsAsync pozitif araç kimliği, tek seçili kaynak firma, aktif/silinmemiş DB Admin kullanıcısı ve kaynak firmada erişilebilir araç kontrolünden sonra ilişki sayımlarını açar. Firma olayı sürümü liste hazırlığı boyunca A→B→A değişimini de yakalar; abonelik finally ile kaldırılır. Listeleme hatası UI'de yakalanır, logger'a kaydedilir ve genel bildirim gösterilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kapsam/kabul:** Gerçek taşıma veya runtime testi yapılmadı; çalışan uygulama yeniden başlatılmadı. Puantaj/bakım ve diğer ilişkilerin kaynak/hedef bağlantıları, seçilmeyen kayıt politikası, silinmiş evrak/dosyaların yeniden etkinleştirilmesi, dosya sürümü erişimi, eşzamanlı yazım ve rollback kabulü açık. Eski firma uyuşmazlıkları bu akışta otomatik onarılmaz. A-13 genel durumu sarı kalır.

## A-13 devamı — seçilen puantaj ve servis çalışma bağlantıları — 2026-10-05

- 🟢 **Kod düzeltildi:** Taşınması seçilen aktif puantajların IsverenFirmaId, servis çalışmalarının FirmaId alanı kaynak araç firmasıyla eşleşmelidir; boş/başka firma bağlantısı kayıt öncesinde reddedilir. Uygun kayıtların firma ve UpdatedAt alanları ortak SaveChanges akışında güncellenir.
- 🟢 **Kod düzeltildi:** Seçilen puantajın dolu cari, kurum, güzergâh ve şoför bağlantıları hedef firmada mevcut ve silinmemiş olmalıdır. Servis çalışmasının zorunlu güzergâh ve şoför bağlantıları da hedef firmaya göre kontrol edilir. Bağlantılar otomatik silinmez veya başka kimliğe çevrilmez; uyumsuzlukta taşıma kaydedilmez.
- 🟢 **Kod düzeltildi:** Gelir/gider faturası, hesap dönemi veya önceki sürüm bağlantılı puantajlar ile aktif masraf bağlantılı servis çalışmaları eşleme tamamlanana kadar bu taşıma akışında reddedilir. Önceden değiştirilen tracked araç/evrak alanları da bu ret halinde SaveChanges yapılmadığı için kaydedilmez. ServisCalisma envanter etiketi gerçek kapsamına uygun “Servis çalışma kayıtları” olarak düzeltildi.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kapsam/kabul:** Gerçek taşıma veya runtime testi yapılmadı. Seçilmeyen kayıtların tarihsel firma politikası, hedef kayıt eşleme ekranı, Excel import ve diğer dolaylı ilişkiler, şoför/personel atamaları, plaka takibi/faturaları, silinmiş kayıtlar ve eşzamanlı yazım/rollback kabulü açıktır. Bu korumalar tam ilişki geçişi veya satış kabulü değildir; A-13 sarı kalır.

## A-13 devamı — araç taşıma ekranında işlem ve sonuç koruması — 2026-10-05

- 🟢 **Kod düzeltildi:** Taşıma envanteri ve onay işlemi ortak işlem kilidi kullanır; çift listeleme/taşıma ve işlem sürerken Kaydet üzerinden yeni taşıma penceresi açma engellenir. Onay düğmeleri ve seçim kutuları işlem boyunca devre dışıdır; yazım sürerken kapatma/iptal engellenir ve bekleme bilgisi gösterilir. Envanter yüklenmeden taşıma başlatılmaz.
- 🟢 **Kod düzeltildi:** Modal sürümü, araç kimliği, kaynak ve hedef firma işlem başında yakalanır. İptal, yeni pencere, route parametresi, firma olayı (A→B→A dahil) ve Dispose eski sonucu geçersizleştirir. Envanter sonucu, hata/başarı bildirimi ve yönlendirme yalnız güncel pencere için uygulanır; firma olayı/Dispose aboneliği kaldırma kapsamındadır. İşlem kilidi finally ile bırakılır.
- 🟢 **Kod düzeltildi:** Taşıma hazırlık ve istisna ayrıntıları logger'a gider; kullanıcıya genel yetki/firma/ilişki kontrolü yönergesi gösterilir. Hata halinde pencere açık kalır; başarı sonrası kapanır. Başlamış DB kaydı, pencere veya firma değişikliğiyle geri alınmış sayılmaz.
- 🟢 **Ekran düzeltildi:** “Bugünden Sonra Kopyala” düğmesi aynı MoveAracToFirmaAsync çağrısını yaptığı ve tarih bazlı kopya üretmediği için kaldırıldı. Ekran yalnız gerçekten uygulanan taşıma işlemini sunar; kopyalama özelliği eklenmedi.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul/kapsam:** Runtime/UI veya gerçek taşıma testi yapılmadı. Çift tıklama, envanter beklerken iptal/yeniden açma, firma/route değişimi, Dispose, başarısız taşıma ve commit yarışı kabulü açıktır. Diğer form yükleme/kayıt/evrak işlemleri bu sürüm koruması kapsamında değildir. Seçilmeyen ilişkilerin politikası ve hedef eşleme işleri nedeniyle A-13 sarı kalır.

## A-08 devamı — maaş snapshot muhasebe bağlantısı ve transaction — 2026-10-05

- 🟢 **Kod düzeltildi:** MuhasebeSnapshotService içindeki fiş/iptal fişi bağlantısını yazan iki ExecuteUpdateAsync kaldırıldı. Snapshotlar AsTracking yüklenir; MuhasebeFisId/IptalFisId ve UpdatedAt değişiklikleri normal SaveChanges/audit akışından geçer.
- 🟢 **Kod düzeltildi:** Hesap ön hazırlığı, normal/ters fiş ve snapshot bağlantıları tek üst transaction içinde çalışır. ExecutionStrategy her denemede yeni context/tracker ve transaction açar; hata halinde commit yapılmaz ve transaction dispose edilir. Audit savepoint akışı mevcut transaction içinde kalır.
- 🟢 **Kod düzeltildi:** Yıl/ay/pozitif firma ve tek seçili kaynak firma doğrulanır. Firma olayı sürümü A→B→A değişimini yakalar; commit öncesi tekrar kontrol edilir, abonelik finally ile kaldırılır. Mükerrer fiş sorgusu MAS-firma önekiyle daraltıldı; iptal için tüm snapshotların aynı mevcut kaynak fişe bağlı olması ve fişin firma öneki/kaynak tipi kontrol edilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul:** Gerçek DB/audit rollback veya runtime testi yapılmadı. İki eşzamanlı muhasebeleştirme, fiş numarası/hesap tekillikleri, commit sonucu belirsizliği, eski tutarsız snapshotlar ve kaynak fiş toplam/kalem uyumu A-09/A-15/A-16 altında açıktır. MuhasebeFis üzerinde doğrudan FirmaId olmadığından önek kontrolü kalıcı tenant ilişki kısıtının yerine geçmez.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan uygulama:** Bu ek yalnız MuhasebeSnapshotService içindeki iki doğrudan yazımı kapatır. Hedefli servis aramasında BudgetService, PuantajFinansService, GuzergahService/GuzergahSeferService, RebuildService, LicenseService ve bakım/restore servislerinde başka doğrudan SQL/toplu yazım yolları vardır; tam repo envanteri ve her yolun audit/transaction kararı henüz tamamlanmadı. A-08 genel durumu kırmızı kalır.

## A-08 devamı — güzergâh sefer yenilemede audit ve transaction sınırı — 2026-10-05

- 🟢 **Kod düzeltildi:** GuzergahSeferService.ReplaceAllInCurrentDbAsync içindeki ExecuteUpdateAsync kaldırıldı. Aktif eski seferler AsTracking yüklenir; IsDeleted, DeletedAt ve UpdatedAt normal SaveChanges/audit akışında yazılır. Zaten silinmiş seferlerin geçmiş zaman damgaları değiştirilmez.
- 🟢 **Kod düzeltildi:** Metot açık transaction gerektirir; eski seferleri kapatma ve yenilerini ekleme aynı üst transaction içinde kalır. Mevcut iki çağıran transaction açmaktadır. Hata/sayı uyuşmazlığında çağıranın rollback akışı çalışır. ChangeTracker.Clear kaldırıldı; üst işlemin takip ettiği güzergâh ve diğer kayıtlar yardımcı metot tarafından detache edilmez.
- 🟢 **Kod düzeltildi:** Yazım için pozitif güzergâh ve tek seçili kaynak firma gerekir; parent ve aktif eski seferlerin firma kimlikleri eşleşmelidir. Seçili firma kayıtlar öncesinde ve metot sonunda tekrar kontrol edilir. Tutarsız sefer firması sessizce düzeltilmez; işlem reddedilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul/kapsam:** Runtime veya gerçek DB rollback/audit testi yapılmadı. Eşzamanlı replace, retry/commit belirsizliği, üst transaction savepoint davranışı ve eski ilişki kabulü açık. Buradaki seçim kontrolü anlık kimlik eşitliğidir; A→B→A olayı ve üst çağıranın commit anı için işlem boyunca sürüm koruması ayrıca tamamlanmalıdır. GuzergahService'in diğer doğrudan yazımları bu ekte değiştirilmedi; A-08 kırmızı kalır.

## A-08 devamı — güzergâh ana kayıt yazımlarında audit — 2026-10-05

- 🟢 **Kod düzeltildi:** GuzergahService.UpdateAsync ve UpdateWithSeferlerAsync içindeki iki ExecuteUpdateAsync kaldırıldı. Ana güzergâh AsTracking ve silinmemiş kayıt koşuluyla yüklenir; cari/kurum bağlantıları, gelir/gider fiyatı ve KDV aynı tracked kaydın SaveChanges/audit akışında yazılır. GelirFiyat mevcut BirimFiyat alanına bağlıdır; ayrı SQL fiyat yazımı gerekmez.
- 🟢 **Kod düzeltildi:** Normal UpdateAsync içindeki ikinci bağımsız SQL yazımı kaldırıldı; ana alanlar ve cari/kurum tek SaveChanges çağrısında kaydedilir. Mevcut 0/null girişte eski cari/kurum bağlantısını koruma davranışı değişmedi. Sonuç doğrulaması global filtre kapsamında okunur.
- 🟢 **Kod düzeltildi:** Seferli güncellemede ana kayıt, sefer kapatma/ekleme ve audit mevcut üst transaction içinde kalır. Sefer yardımcı metodundaki takip temizleme çağrısı önceki ekte kaldırılmıştır; bu ekte ana kayıt için ayrıca SQL bypass yapılmaz.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kapsam/kabul:** Runtime/gerçek DB veya rollback testi yapılmadı. Güzergâh genel güncelleme/create/delete/transfer yetkisi ve firma ilişkileri, 0/null ile ilişki temizleme ürün davranışı, firma değişiminde sonuç doğrulaması, cache hatası sonrası kullanıcı bildirimi, eşzamanlı yazım ve retry/commit belirsizliği ayrıca denetlenmelidir. Bu ek tam güzergâh tenant kabulü değildir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan uygulama:** Güzergâh ana kayıt/sefer servislerindeki hedeflenen üç ExecuteUpdate yolu kapandı; BudgetService, PuantajFinansService ve diğer bakım/restore/toplu yazım yolları için repo envanteri ve audit/transaction kararları devam eder. A-08 genel durumu kırmızı kalır.

## A-08 devamı — bütçe ödeme geri almada ortak kayıt/audit — 2026-10-05

- 🟢 **Kod düzeltildi:** BudgetService.OdemeGeriAlAsync içindeki ExecuteUpdateAsync kaldırıldı. Aktif ödeme AsTracking yüklenir; ödeme durum/tarih/tutar/hesap/kesinti alanları normal SaveChanges/audit akışında sıfırlanır. Banka hareketinin silinmesi için ayrı erken SaveChanges kaldırıldı; banka silme ve ödeme güncelleme tek SaveChanges transaction'ında kaydedilir.
- 🟢 **Kod düzeltildi:** Bağlı banka hareketi erişilebilir/silinmemiş ve ödeme ile aynı pozitif firmaya ait olmalıdır. Eksik/gizli hareket varsa ödeme bağlantısı sessizce sıfırlanmaz. Fatura, muhasebe fişi, mahsup, personel geri ödeme, araç masrafı veya aktif fatura eşlemesi olan hareketler kendi iptal akışına yönlendirilir; eşlemeler otomatik silinmez.
- 🟢 **Kod düzeltildi:** Başka aktif bütçe ödemesinin veya mahsup/personel geri ödeme hareketinin kullandığı banka kaydı silinmez. Filtre atlama yalnız bağlı hareket kimliğiyle çakışma kontrolüdür. Kısmi ödeme veya sonraki döneme aktarılmış ödeme, tüm ilişkiler birlikte geri alınmadan bu basit iptal akışında reddedilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul/kapsam:** Runtime veya gerçek DB rollback/audit testi yapılmadı. Mevcut fiziksel banka hareketi silme davranışı korunmuştur; ters kayıt/soft delete politikası ayrıca değerlendirilmelidir. Tam rol/firma yetkisi, kredi kartı ve diğer dolaylı ilişkiler, kısmi/devir ödeme iptal uygulaması, eşzamanlı bağlantı ekleme ve commit belirsizliği açıktır. Ön sorgular eşzamanlı DB ilişki kısıtı değildir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan uygulama:** Bu ek yalnız ödeme geri almadaki bir SQL bypass'ını kapatır. BudgetService'in kısmi ödeme, normal ödeme ve diğer toplu yazımları ile repo genelindeki SQL/audit envanteri devam eder; A-08 kırmızı kalır.

## A-08 kapanış çalışması — kalan toplu servis yazımları ve repo envanteri — 2026-10-05

- 🟢 **Kod düzeltildi:** BudgetService (5), CRMService (1), PuantajFinansService hakediş bağlantısı (1), RebuildService (6), LicenseService (3), eski TestSessionService satır rollback (4), WhatsAppService (1) ve EvrakArsivBackfillService (2) olmak üzere **23 toplu çağrı** tracked sorgu/UpdateTrackedAsync/SaveChanges akışına taşındı. Ortak yardımcı doğrudan SQL çalıştırmaz. Tek kayıt beklenen bütçe, hakediş ve evrak güncellemesi eşleşme yoksa başarısız olur; başarı gibi dönmez. Hata halinde bu sorgunun kayıt değerleri ve değişiklik işaretleri işlem öncesine döndürülür.
- 🟢 **Kod düzeltildi:** Hakediş snapshotındaki iki raw SQL yazımı kaldırıldı. SnapshotTransaction işaretçisi ve tracked tutar artışı/negatif tutar normalizasyonu tek Serializable transaction/SaveChanges kapsamındadır; ExecutionStrategy her denemede yeni context açar. Snapshot hatası sessizce yutulmaz; mevcut faturanın oluşmuş olduğu açık hata mesajıyla belirtilir. Bu, fatura alt servisinin ayrı transaction'ını geri alma garantisi değildir.
- 🟢 **Envanter oluşturuldu:** [A-08 SQL/audit kapanış envanteri](A-08-SQL-AUDIT-KAPANIS-ENVANTERI.md) repo C# kaynaklarındaki kalan çağrı konumlarını ve audit/transaction kararlarını listeler. Çağrı listesi DDL, sayaç, wrapper tanımı, audit iç düzeltmesi ve test kodunu da içerir; tamamı açık iş yazımı sayılmadı. Script/dinamik SQL semantik incelemesi ayrıca gereklidir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kabul sınırı:** Runtime/test veya gerçek veri değişikliği yapılmadı. Tracked batch büyük kümeyi belleğe alır ve context'teki diğer bekleyen değişiklikleri de kaydeder; yük, eşzamanlılık, commit belirsizliği ve tam mali zincir rollback kabulü açık. Firma bağlamı olmayan sistem kayıtlarında mevcut audit resolver satır üretmeyebilir; normal SaveChanges kullanılması her sistem kaydının audit kanıtı değildir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kapanış engeli:** Başlangıç/migration/seed veri onarımları, restore/legacy transfer/DataSync/demo işlemleri ve test tablo restore yollarında operasyon audit/rollback sözleşmesi henüz tamamlanmadı. Destek sayaçlarının atomik increment kararı ve audit EntityId iç düzeltmesi ayrı belgelenmiştir. Bu açıklar yalnız rapor rengini değiştirerek kapatılmaz; A-08 genel durumu kırmızı kalır.

## A-08 devamı — legacy aktarımda tablo transaction ve operasyon audit'i — 2026-10-05

- 🟢 **Kod düzeltildi:** LegacyDataTransferService içindeki roller, kullanıcılar, rol yetkileri, muhasebe hesapları ve generic tablo aktarımı tablo başına Npgsql transaction açar. Veri yazımı ile parametreli AktiviteLoglar özet kaydı aynı transaction'dadır; audit kaydı başarısızsa tablo commit edilmez. Başarı logu commit sonrasında yazılır. Satır bazlı eski/yeni değer yerine açıkça operasyon özeti tutulur.
- 🟢 **Kod düzeltildi:** Özet kaydı işlem kimliği, tablo, gerçek etkilenen satır sayısı, muhasebe üst hesap güncelleme sayısı, kaynak/hedef DB adı ve yapılandırılmış hedef firma kimliği içerir. Connection string, parola, kaynak satır verisi veya SQL parametreleri günlüğe kopyalanmaz. ON CONFLICT DO NOTHING satırları başarı sayısını artırmaz.
- 🟢 **Kod düzeltildi:** Satır INSERT/upsert işlemleri savepoint kullanır. Beklenen unique çakışmasında savepoint geri alınarak transaction kullanılabilir tutulur; muhasebe hesap Id fallback'i aynı transaction içinde devam eder. Generic kolon uyuşmazlığı döngüyü kesip kısmi başarı vermek yerine tablo işlemini hatayla durdurur. Hata/erken çıkışta transaction disposal commit edilmemiş tablo değişikliklerini geri alır.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi. Gerçek legacy veritabanı değiştirilmedi.
- 🟡 **Kalan kapsam/kabul:** İşlem sınırı tüm aktarım değil, tek tablodur; daha önce commit edilen tablolar sonraki tablo hatasında geri alınmaz. Source bağlantıları salt okunur kalır fakat tüm kaynak tablolar ortak snapshot içinde okunmaz. PostgreSQL sequence setval etkileri transaction rollback garantisine dahil değildir. Audit tablosu/kolonları yoksa veri commit'i reddedilir. Eski firma/rol/kimlik eşleme, idempotency, aynı anda iki aktarım, audit PK/sequence ve gerçek PostgreSQL rollback kabulü bekler.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan kapanış:** Legacy tablo yazımı için operasyon audit'i eklendi; schema hazırlığı, başlangıç veri onarımları, diğer restore/aktarım/demo yolları ve firma bağlamı olmayan sistem audit'i açık. A-08 genel durumu kırmızıdır.

## A-08 devamı — test tablo geri yüklemede ortak transaction ve audit — 2026-10-05

- 🟢 **Kod düzeltildi:** TestSessionService.GeriAlAsync yalnız PostgreSQL'de, aktif test etiketi eşleştiğinde ve DB'de aktif/silinmemiş Admin yetkisi doğrulandığında çalışır. Tek pozitif firma seçimi gereklidir; işlem boyunca firma olay sürümü A→B→A değişimini de yakalar. Geçici abonelik finally ile kaldırılır.
- 🟢 **Kod düzeltildi:** Gerekli altı backup tablosu yazım öncesinde kontrol edilir; eksik tablo sessizce atlanmaz. Bütün hedef tablolar tek TRUNCATE RESTRICT komutuyla hazırlanır; yedeklenmemiş bağımlı tabloları silen CASCADE kaldırıldı. Dış FK bağımlılığı varsa işlem reddedilir; bu tablolar otomatik silinmez veya yedeklenmiş kabul edilmez.
- 🟢 **Kod düzeltildi:** TRUNCATE/INSERT ve TestSnapshotRestore AktiviteLog kaydı aynı üst transaction içindedir. Raw ADO komutları EF CurrentTransaction'a açıkça bağlanır. Audit kaydı işlem kimliği, tag, tablo kümesi, session ve WholeDatabaseTables kapsamını belirtir; satır audit'i yerine operasyon özeti üretir. ExecutionStrategy denemeleri yeni context/transaction ile başlar.
- 🟢 **Kod düzeltildi:** Test oturumu yalnız başarılı commit sonrasında kapatılır. Hata halinde işlem başarılı gösterilmez; oturum/yedekler korunur ve teknik hata logger'a gider. Kullanıcıya genel yedek/bağımlılık/yetki kontrolü bildirimi verilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi. Gerçek test verisi veya yedek geri yüklenmedi.
- 🟡 **Kalan kabul/kapsam:** Gerçek PostgreSQL restore/rollback/audit testi yapılmadı. Mevcut altı tablo kümesi bütün FK bağımlılıklarını kapsamaz; bu durumda RESTRICT işlemi durdurur. Tam bağımlılık yedeği, INSERT sırası/kolon uyumu, sequence/kimlik eşlemesi ve commit belirsizliği kabulü açık. Seçilen firma audit aidiyetidir; tablo restore tüm firma satırlarını kapsar. Test başlatma/backup oluşturma, cleanup ve basit BeginSession yollarının yetki/transaction/audit sözleşmesi bu ekte tamamlanmadı. A-08 genel durumu kırmızı kalır.

## A-08 devamı — test backup/cleanup/session bakım sözleşmesi — 2026-10-05

- 🟢 **Kod düzeltildi:** BaslatAsync, BeginSessionAsync ve TemizleAsync PostgreSQL bakım yardımcısında aktif DB Admin yetkisi, tek pozitif firma ve işlem boyunca firma olay sürümü kontrolü kullanır. Bakım değişiklikleri, session rezervasyonu ve açık kapsamlı AktiviteLog operasyon kaydı aynı transaction/SaveChanges içindedir; yalnız commit sonrası AppMode/başarı durumu yayınlanır.
- 🟢 **Kod düzeltildi:** Backup öncesinde aynı tag için mevcut tablo varsa işlem reddedilir; DROP ile eski yedek silinmez. Altı kaynak tablo SHARE kilidiyle korunur; CREATE TABLE AS SELECT işlemleri ve audit ortak transaction'dadır. Hata halinde dönülen backup tablosu listesi temizlenir ve başarısız işlem yeni oturum başlatmaz.
- 🟢 **Kod düzeltildi:** Cleanup aktif test varken reddedilir. Süreç içi ortak semaphore bakım çağrılarını sıraya alır; PostgreSQL transaction advisory lock backup/cleanup/begin/restore DB işlemlerini aynı anahtarla sıralar. ReadCommitted altında session maksimumu silinmiş loglar dahil okunur; session Begin marker'ı audit ile birlikte kaydedilir. Başka süreçlerdeki AppMode durumunu bu kilit tek başına doğrulamaz.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi. Gerçek backup/restore veya test oturumu çalıştırılmadı.
- 🟡 **Kalan kabul:** Farklı süreçlerde aktif test oturumu sahipliği, bağlantı/commit belirsizliği, session marker ve eski rollback uyumu, tag normalizasyon çakışması, DDL/SHARE lock gecikmesi ve gerçek PostgreSQL rollback kabulü açık. SQL backup yalnız altı tabloyu kapsar; tam bağımlılık kurtarması değildir. Eski satır rollback yolunun ayrı yetki ve kısmi sonuç sözleşmesi ayrıca denetlenmelidir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kapanış sınırı:** Test backup/cleanup/begin kaynak kontrolleri tamamlandı; başlangıç/migration veri onarımı, diğer restore/DataSync/demo/dış araç operasyon kayıtları ve firmasız sistem audit bağlamı halen açıktır. Bu işler tamamlanmadan A-08 yeşile çevrilmez.

## A-08 devamı — başlangıç fatura onarımı ve demo bakım sınırı — 2026-10-05

- 🟢 **Gelen fatura onarımı:** PostgreSQL doğrudan UPDATE kaldırıldı; bütün sağlayıcılarda AsTracking + SaveChanges ve UTC zaman damgası kullanılır. Firma aidiyeti bulunmayan kayıtlar yazımdan önce reddedilir; hata artık başarı gibi yutulmaz.
- 🟢 **Demo bakım kaydı:** Ekleme, [TEST] temizliği ve yenileme aktif Admin/tek firma kontrolü, firma değişim sürümü, ortak PostgreSQL transaction/advisory kilidi ve aynı transaction'daki operasyon audit'i kapsamındadır. Retry denemesinde yeni context/seeder oluşturulur; seed başarısız sonucu commit edilmez. İki ekran aynı servisi çağırır.
- 🟢 **Tehlikeli sıfırlama kaldırıldı:** Demo yenileme yalnız seçili firmanın [TEST] kayıtlarını temizleyip yeniden üretir. Eski tüm-veritabanı TRUNCATE API'si yazım yapmadan açık hata döndürür; kullanıcı/lisans/audit geçmişini silen CASCADE ve session_replication_role yolları kaldırılmıştır. Ekran açıklamaları kapsamı belirtir.
- 🟢 **Sınırlı firma temizliği:** Tanımlı 11 tabloda parametreli FirmaId silme + operasyon audit'i ortak transaction'dadır. FK kontrolleri açık kalır; tablo hatası atlanmaz. FirmaId taşımayan ortak MuhasebeFisleri/MuhasebeHesaplari bu işlemden çıkarılmıştır. Bu yol tüm firma verisinin eksiksiz silindiğini iddia etmez.
- 🟡 **Kabul açık:** Gerçek PostgreSQL demo/temizlik/audit/rollback testi yapılmadı. İlişkili kayıtlar fiziksel silmeyi engelleyebilir; işlem başarısız döner. Seed'in eski demo veri aidiyeti, FK/kayıt üretimi ve commit belirsizliği kabulü bekler. Sadece derleme kontrolü bu kabulü kapatmaz.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan:** Diğer başlangıç/migration veri onarımları, restore/DataSync/dış araç yazımları ve firmasız sistem audit sözleşmesi açıktır. A-08 genel durumu kırmızı kalır.

- 🟢 **Derleme kontrolü:** Son kaynaklarla Web projesi izole çıktı klasörüne `--no-restore -p:UseAppHost=false` ile derlendi: **0 uyarı, 0 hata**. `git diff --check` temiz. Runtime/gerçek veri temizliği çalıştırılmadı.

## A-08 ortak altyapı — SQL ve sistem yazımlarının veritabanında denetimi — 2026-10-05

- 🟢 **Ortak altyapı eklendi:** PostgreSQL/SQLite iş tablosu tetikleyicileri EF dışı INSERT/UPDATE/DELETE'yi aynı transaction içinde denetler; PostgreSQL TRUNCATE kapsam/satır sayısını kaydeder. Firma bağlamı olmayan kayıtlar ayrı DB günlüğünde sistem kapsamındadır. Sır alanları maskelenir; audit hatası iş yazımını engeller.
- 🟢 **Başlangıç/aktarım:** Şema kurulumundan sonra installer, legacy hedef bağlantısında installer, DataSync hedefinde installer eklendi. Master başlangıç kopyası kaynak snapshot ve ortak hedef transaction'a alındı; satır hatası artık atlanmaz. SQLite journal iş tablosu aktarım/sıfırlama listesinden çıkarıldı.
- 🟢 **Restore:** Kalıcı dış operasyon makbuzu ve source SHA-256; PostgreSQL audit şemasını yedek/restore iş kapsamından ayırma; SQLite açık dosyayı ezmek yerine backup API ve öncesi geri dönüş kopyası. Deploy betiğinde DB DROP kaldırıldı; atomik restore ve sıfır dışı hata kodunun reddi eklendi.
- 🟢 **SQLite izole kontrol:** Doğrudan SQL, sistem kapsamı, sır maskeleme, rollback, günlük değişmezliği ve audit hatasında veri yazımının durması geçti.
- **Sözleşme:** [A-08 veritabanı audit sözleşmesi](A-08-VERITABANI-AUDIT-SOZLESMESI.md). PostgreSQL izole kontrolü ve son kaynak derlemeleri tamamlanınca görev satırı güncellenecek. Müşteri verisi restore edilmedi.

## 🟢 A-08 kapanışı — ortak denetim motoru ve izole doğrulama — 2026-10-05

**A-08 tamamlandı.** Bu ek önceki A-08 “açık/kırmızı” kayıtlarının güncel durumunu değiştirir; önceki ekler tarihsel çalışma kanıtıdır.

- 🟢 **SQL bypass kökten kapatıldı:** Ortak PostgreSQL/SQLite tetikleyicisi EF, doğrudan SQL, toplu yazım, migration ve binary COPY'yi veriyle aynı transaction içinde kaydeder. Audit hatasında veri yazımı gerçekleşmez; rollback günlüğü de geri alır. Firma bilgisi bulunmayan sistem kayıtları başka firmaya mal edilmez.
- 🟢 **Kapsam tamamlandı:** Web startup + 11 veri migration sınıfı; maaş/özlük/SMS onarım kapıları; master kopyada kaynak snapshot + ortak hedef transaction; legacy/DataSync hedef kurulumları; demo bakımı; LisansDesktop yerel satış/yenileme SQL geçmişi; 4 bağımsız veri SQL betiği ve Deploy restore. Günlük kendi kendini audit etmez; migration geçmişi/kimlik sequence metadatası iş satırı değildir.
- 🟢 **Sır maskeleme ortaklaştırıldı:** DbContext otomatik audit, AuditLogService ve DB motoru API/key/password/credential/payload/değer alanlarını maskeler. Yerel/üretim sırları test çıktısına yazılmadı.
- 🟢 **Restore kanıtı kalıcı:** PostgreSQL public restore mevcut `mk_audit` geçmişini korur. SQLite eski veri/audit bağımsız before-restore kopyasında korunur. SHA-256 başlangıç ve ayrı başarı/belirsiz sonuç makbuzu, restore edilen DB'nin dışında saklanır. Düz SQL restore ve mevcut DB'yi düşürme kaldırıldı; hata kodu 1 başarı sayılmaz.
- 🟢 **Gerçek izole çalışma zamanı kontrolü:** SQLite ve PostgreSQL 17 üzerinde doğrudan SQL, sistem kapsamı, sır maskeleme, rollback, audit hatasında yazımın reddi, ALWAYS/replica, TRUNCATE, yeni migration tablosunun ilk INSERT'i ve binary COPY geçti. Gerçek custom pg_dump/pg_restore ile veri/audit koruması ve SQLite backup API ile geri dönüş kopyası doğrulandı. Geçici PG test DB'si silindi, sunucu durduruldu; müşteri DB'sine dokunulmadı.
- 🟢 **Son kaynak derlemeleri:** Web, DataSync ve LisansDesktop projeleri izole çıktı klasörlerinde `--no-restore -p:UseAppHost=false` ile derlendi: her biri **0 uyarı / 0 hata**. Restore PowerShell parser kontrolü geçti.
- **Kanıtlar:** [Veritabanı audit sözleşmesi](A-08-VERITABANI-AUDIT-SOZLESMESI.md), [izole kontrol ve kaynak SHA-256](A-08-IZOLE-DOGRULAMA-2026-10-05.md), [çağrı/kapsam envanteri](A-08-SQL-AUDIT-KAPANIS-ENVANTERI.md).
- 🟡 **Ayrı ürün kabulü:** Gerçek müşteri migration/DataSync verisi, yüksek hacim, mali zincir, bağımsız makine ve dosya/key ring kurtarması A-04/A-09/A-18/A-19 kapsamında açıktır. Bunlar A-08'in açık kod işi olarak tekrar sayılmaz.

## A-03 devamı — DB-only geri yükleme kapsamının ekranda belirtilmesi — 2026-10-05

- 🟢 **Kod düzeltildi:** Yedekleme ekranındaki restore onayında işlemin yalnızca PostgreSQL verisini değiştirdiği; ZIP içindeki belge/dosya, ayar ve DataProtection anahtarlarının uygulanmadığı açıkça gösterilir. Eylem düğmesi “Yalnızca DB'yi Geri Yükle” olarak adlandırıldı.
- 🟢 **Dokümantasyon:** `SIFRELI-BELGE-YEDEK-KURTARMA.md` ekran davranışı ve sınırla eşitlendi.
- 🔴 **A-03 açık:** DB+dosya+anahtarları kapalı bakım penceresinde birlikte uygulayan, ayarları hedefe göre koruyan ve her aşamada önceki duruma dönen otomasyon henüz eklenmedi. Bu UI düzeltmesi tam kurtarma değildir; A-03 kırmızı kalır.

## A-03 devamı — deploy DB restore hata geri dönüşü — 2026-10-05

- 🟢 **Kod düzeltildi:** `01-db-restore.ps1`, mevcut DB için `mk_audit` hariç custom geri dönüş dump'ını restore öncesinde üretip boyutunu kontrol eder. Kaynak restore girişiminden sonra herhangi bir hata olursa eski `public` şemayı tek transaction'lı `pg_restore` ile geri yükler, audit installer'ı yeniden uygular ve `rolled-back.json` makbuzu yazar.
- 🟢 **Yeni hedef DB:** DB önceden yoksa ve kaynak restore hata verirse yalnız bu çağrıda oluşturulan hedef DB bağlantıları kapatılıp DB kaldırılır. Eski bir DB hiçbir hata kolunda DROP edilmez.
- 🟡 **Doğrulama sınırı:** PowerShell parser/derleme ve diff kontrolü yapılacak; bu turda canlı veya izole PostgreSQL hata enjeksiyonu çalıştırılmadı. Makbuz gerçek rollback kabulünün yerine geçmez.
- 🔴 **A-03 açık:** Dosya/ayar/key ring ile DB'yi aynı tam kurtarma operasyonunda uygulama ve birlikte geri alma otomasyonu yoktur. Mevcut değişiklik yalnız deploy DB restore betiğinin rollback açığını kapatır.

## A-03 devamı — Web ZIP DB restore rollback — 2026-10-05

- 🟢 **Kod düzeltildi:** Web ZIP restore başarılı DB restore sonrasında audit doğrulaması veya başarı makbuzu başarısızsa, önceden alınmış tam ZIP yedeğindeki tek PostgreSQL custom dump ayrı staging'e çıkarılır ve `--single-transaction` ile önceki `public` şeması geri kurulur; ardından audit installer doğrulanır.
- 🟢 **Sonuç kaydı:** Geri dönüş doğrulanırsa `rolled-back.json` yazılır. Kaynak/rollback işlemi belirsiz kalırsa `unconfirmed.json` korunur; rollback hatası Critical loglanır. Başarısız `pg_restore` da commit/bağlantı belirsizliği ihtimaline karşı önceki yedekten geri döndürülür.
- 🟢 **Derleme:** Web + Shared Debug ayrı geçici çıktı klasörüne `--no-restore -p:UseAppHost=false` ile **0 uyarı / 0 hata** derlendi.
- 🟡 **Kabul sınırı:** PostgreSQL hata enjeksiyonu/gerçek restore bu turda çalıştırılmadı; uygulama yolu kod derlemesiyle kabul edilmiş sayılmaz.
- 🔴 **A-03 açık:** Web/deploy DB rollback uygulandı; dosya, ayar ve key ring'i aynı tam kurtarma operasyonunda atomik uygulama/rollback aracı hâlâ yoktur.

## Güvenli parola istemi — restore betikleri — 2026-10-05

- 🟢 `01-db-restore.ps1` artık parola parametresi veya `MKFILO_PG_PASSWORD` desteği sunmaz. PostgreSQL parolası `Read-Host -AsSecureString` ile istenir; komut satırı argümanına aktarılmaz. PowerShell süreç ortamındaki `PGPASSWORD` yalnız işlem boyunca ayarlanır, finally bloğunda önceki değer geri yüklenir; BSTR ve SecureString temizlenir.
- 🟢 `00-aktar-baslat.ps1` parola değerini yapılandırmada tutmaz ve alt sürece parametre olarak geçmez. `04-pc2-kurulum-talimat.md` yeni istem akışını anlatır.
- 🟢 İki betiğin PowerShell parser doğrulaması ve `git diff --check` geçti; eski açık parola parametresi/metin araması hedef betik ve talimatta eşleşme vermedi. 🟡 Gerçek müşteri bağlantısı ve sır rotasyonu kanıtı yoktur; A-06 açık kalır.

## A-03 devamı — DB restore öncesi dosya/anahtar ön kontrolü — 2026-10-05

- 🟢 **Kod düzeltildi:** `02-dosya-aktar.ps1 -PreflightOnly` kaynak `.enc` başlıklarını sınıflandırır. MKD1 dosyalarında `key-*.xml`; legacy/AES dosyalarında DPAPI ile çözülebilir `master.key` veya 32 baytlık geçerli import/raw key arar. Ön kontrol hedefe yazmaz.
- 🟢 **Sıra düzeltildi:** `00-aktar-baslat.ps1` DB restore'dan önce dosya/anahtar preflight çalıştırır. Anahtarlar eksik/bozuksa DB restore başlatılmaz. Gerçek hedef kopyasında aynı kontroller yeniden yapılır.
- 🟢 **Sözdizimi:** İki betiğin PowerShell parse kontrolü ve `git diff --check` geçti. 🟡 DataProtection key XML'in hedefte gerçekten çözülebildiği farklı makine kabulü A-04'tedir. Dosya kopyası sonrasındaki I/O hatasında ortak DB+dosya rollback henüz yoktur.
- 🔴 **A-03 açık:** Bu düzeltme anahtar eksikliğiyle DB'yi tek başına restore etme riskini kapatır; tam operasyon transaction/rollback aracı değildir.

## A-03 devamı — legacy DB+dosya aktarım rollback bağlama — 2026-10-05

- 🟢 **Önceki durum snapshot'ı:** `00-aktar-baslat.ps1` dosya aktarımından önce `uploads`, `keys`, `database` klasörlerinin mevcut olup olmadığını kaydeder ve mevcut içeriği LocalAppData operasyon klasörüne kopyalar. Kopya eksikse DB restore başlamaz.
- 🟢 **DB makbuzu:** `01-db-restore.ps1`, önceden var olan DB'nin ayrı SHA-256 doğrulamalı custom dump'ını parent operasyon klasörüne kopyalar. Yeni DB oluşturduysa `pg_database.oid` değerini operasyon makbuzuna yazar.
- 🟢 **Dosya hatası geri dönüşü:** `02-dosya-aktar.ps1` başarısız olursa parent önce önceki DB dump'ını tek transaction ile yükler; DB başlangıçta yoksa yalnız aynı operasyonda oluşturulan ve OID'si eşleşen hedefi kaldırır. Sonra üç depolama klasörünü başlangıç var/yok durumuna döndürür. Geri dönüş başarı/başarısız makbuzu tutulur; hata halinde snapshot silinmez.
- 🟢 **Statik doğrulama:** İlgili üç PowerShell betiğinin parser kontrolü ve `git diff --check` geçti.
- 🟡 **Kabul sınırı:** Gerçek DB/file hata enjeksiyonu, müşteri verisi veya elektrik kesintisi testi yapılmadı. Bu, yakalanan süreç hatası rollback akışıdır; ani süreç/host kesintisinde otomatik devam garantisi değildir.
- 🟢 **Kesinti sonrası elle kurtarma:** `03-full-transfer-recover.ps1`, LocalAppData altındaki tam aktarım snapshot/makbuzunu doğrular; önceden var olan DB'yi SHA-256 doğrulamalı dump'tan geri yükler veya makbuzdaki OID eşleşen yeni DB'yi kaldırır, ardından üç depolama klasörünü snapshot'tan geri alır. Önceki otomatik rollback başarısızlık makbuzu elle kurtarmayı engellemez; yeni DB zaten kaldırılmışsa adım idempotent tamamlanır. IIS havuzunun durduğu onaylanır ve sonuç makbuzu yazılır.
- 🟢 **Güvenlik kontrolleri:** Operasyon klasörü doğrudan izinli journal kökü altında olmalı; dump yolu beklenen dosyaya sabitlenir, hash doğrulanır, junction/symlink hedefleri reddedilir.
- 🟢 **Kapsam belgeleri eşitlendi:** Şifreli belge yedek rehberi legacy journal geri dönüşünü RecoveryArchive ZIP uygulamasından ayrı açıklar.
- 🟢 **RecoveryArchive apply aracı:** `05-recovery-archive-apply.ps1` staging manifestinin boyut/hash'lerini doğrular; izinli storage/Luca/belge köklerini ve varsa DB dump'ını ayrı hedefe uygular. appsettings JSON dosyalarını atlar, önceki DB/dosya durumunu apply journal'ında saklar ve yakalanan hatada rollback dener.
- 🟢 **DB hata dalı:** DB alt betiği hata kodu döndürürse üst apply akışı apply journal'daki önceki DB dump'ıyla rollback'i ayrıca dener.
- 🟢 **Kesinti kurtarması:** `06-recovery-archive-rollback.ps1` tamamlanmamış apply journal'ındaki DB ve dosya snapshot'larını geri yükler. Apply/rollback belirtilen IIS havuzunu appcmd ile durdurup doğrular; yeniden başlatma kabul sonrası operatördedir. 🟡 Key XML/`KEY-HAZIR` onayı hedef kimliğinde belge çözümünü kanıtlamaz; PostgreSQL hata enjeksiyonu ve farklı makine key ring/credential kabulü yapılmadı.
- 🟢 **Statik doğrulama:** Altı aktarım/kurtarma PowerShell betiğinin parser kontrolü geçti; `git diff --check` temiz. 🟡 Gerçek PostgreSQL/hata enjeksiyonu kabulü yapılmadı.
- 🔴 **A-03 açık:** Başarı sonrası IIS yeniden başlatma ve gerçek hata/kesinti/DB+belge kabulü tamamlanmadı. Ortama özel appsettings otomatik uygulanmaz.

## A-03 devamı — RecoveryArchive journal yolunun doğrulanması — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, DB/dosya hedeflerine dokunmadan önce `%LOCALAPPDATA%\MKFiloServis\OperationJournal` yolunun üst bileşenlerinde junction/symlink bulunmadığını ve yeni operasyon klasörünün normal dizin olduğunu doğrular. Beklenmeyen yönlendirmede işlem durur.
- 🟢 PowerShell parser: `05-recovery-archive-apply.ps1`, `06-recovery-archive-rollback.ps1`, `01-db-restore.ps1` başarılı. `git diff --check` temiz; yalnız doküman satır sonu uyarıları var.
- 🔴 **A-03 açık:** Bu statik kontrol gerçek IIS/NTFS/DB hata enjeksiyonu, kesinti kurtarma veya farklı makine belge çözme kabulü yerine geçmez.

## A-03 devamı — dosya rollback snapshot SHA-256 makbuzu — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, her mevcut hedef dosya kökü için snapshot kopyası sonrası dosya yolu/boyut/SHA-256 manifesti üretir. Apply sırasında hata yakalanırsa snapshot hash'lerini yeniden doğrulamadan kopyalamaz.
- 🟢 `06-recovery-archive-rollback.ps1`, kesinti sonrası geri dönüşe başlamadan önce ilgili snapshot'ların dosya sayısı, yolları, boyutları ve SHA-256 değerlerini doğrular; uyuşmazlıkta DB veya hedef dosyaları değiştirmeden durur.
- 🟢 İki PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz (dokümanlarda satır sonu uyarısı dışında).
- 🔴 **A-03 açık:** Hash makbuzu snapshot bozulmasını saptar, imza/yerel yönetici müdahalesine karşı özgünlük sağlamaz. Gerçek PostgreSQL, kesinti, NTFS ve farklı makine belge kabulü yapılmadı.

## A-03 devamı — apply hazırlık makbuzlarının atomik yazımı — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, dosya öncesi durumu, snapshot SHA-256 listesini ve `ApplyStarted` makbuzunu önce aynı journal klasöründeki benzersiz geçici dosyaya yazar, diske flush eder ve hedef makbuz adına atomik taşır. Kesinti sırasında yarım JSON'un tamamlanmış makbuz gibi görünme olasılığı azaltıldı.
- 🟢 PowerShell parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Dosya taşıma atomikliği hedef dosya sistemine bağlıdır; gerçek NTFS/ani güç kesintisi, PostgreSQL ve uygulama kabulü yapılmadı.

## A-03 devamı — snapshot doğrulamasını hedef silmeden önce yapma — 2026-10-05

- 🟢 Apply hata geri dönüşü ve kesinti sonrası rollback artık ilgili snapshot SHA-256 kontrolünü hedef klasörü silmeden önce yapar. Snapshot eksik/değişmişse o hedef korunur ve geri dönüş başarısızlığı açıkça kaydedilir; doğrulanmamış kopya kullanılmaz.
- 🟢 `00`, `01`, `02`, `03`, `05`, `06` PowerShell betiklerinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Gerçek hata enjeksiyonu ve PostgreSQL/NTFS geri dönüş kabulü yapılmadı.

## A-03 devamı — IIS durdurmadan önce apply journal alanı — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, LocalAppData journal üst yolunu doğrulayıp yeni operasyon klasörünü oluşturmadan IIS havuzunu durdurmaz. Journal yolu/oluşturma hatası artık uygulama havuzunu gereksiz yere kapalı bırakmaz. Havuz yine dosya ve DB snapshot'larından önce durdurulur.
- 🟢 Altı migration PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Gerçek IIS servis durumu geçişi, DB/NTFS hata enjeksiyonu ve hedef makine kurtarma kabulü yapılmadı.

## A-03 devamı — DB rollback makbuz ve dump doğrulaması — 2026-10-05

- 🟢 `01-db-restore.ps1` operasyon makbuzuna kaynak DB dump SHA-256 değerini yazar.
- 🟢 `05` ve `06` rollback yolları mevcut DB için `PreExistingDatabaseSnapshotReady`, hedef adı, beklenen journal dump yolu ve rollback dump hash'ini doğrular. Yeni DB durumunda `DatabaseCreatedByOperation`, sayısal DB OID'si, journal içindeki sabit kaynak dump yolu ve kaynak dump hash'i doğrulanmadan DB kaldırma adımına geçmez.
- 🟢 Üç PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** PostgreSQL üzerinde gerçek yeni/mevcut DB geri dönüşü ve kesinti/hata enjeksiyonu kabulü henüz yapılmadı.

## A-03 devamı — terminal apply/rollback makbuzlarını atomik yazma — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1` artık `applied.json` ve `rollback-result.json` sonuçlarını geçici dosyaya flush edip atomik taşır. Başarı makbuzu tamamlanmadan oluşan yazma hatası apply hata/rollback akışına düşer.
- 🟢 `06-recovery-archive-rollback.ps1` `recovered.json` ve `recovery-failed.json` makbuzlarını aynı yöntemle yazar; yarım terminal makbuzun sonraki kurtarma denemesini yanlış engelleme riski azaltıldı.
- 🟢 İki PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Atomik dosya taşıma ve gerçek süreç/güç kesintisi davranışı hedef Windows/NTFS üzerinde denenmedi; DB ve belge kabulü de bekliyor.

## A-03 devamı — hedef yol üst bileşenlerini yeniden doğrulama — 2026-10-05

- 🟢 Apply ve rollback betikleri hedefleri snapshot alma, uygulama ve geri yükleme öncesinde üst dizinleri yeniden junction/symlink açısından denetler. Başlangıç kontrolünden sonra yol bileşeni değişmişse hedef üzerinde işlem yapmaz.
- 🟢 İlgili PowerShell parser kontrolleri başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Dosya sistemi yarış koşulları, PostgreSQL arızası ve gerçek hedef kurulum kabulü henüz çalıştırılmadı.

## A-03 devamı — eski apply journal'larıyla kurtarma uyumluluğu — 2026-10-05

- 🟢 `06-recovery-archive-rollback.ps1`, yeni apply journal'larında snapshot SHA-256 doğrulamasını zorunlu tutar. Hash makbuzu olmayan eski journal'larda snapshot'ı junction/symlink açısından denetler ve DB/dosya değişikliğinden önce `ESKI-SNAPSHOT-ONAY` operatör onayı ister; onay yoksa değişiklik yapmadan durur.
- 🟢 Eski yeni-DB makbuzlarında kaynak dump hash alanı yoksa, hedef OID ve beklenen journal dump yolu doğrulanarak DB kaldırma kurtarması sürdürülebilir; yeni hash alanı varsa doğrulama zorunludur.
- 🟢 `06-recovery-archive-rollback.ps1` parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Eski journal'larda eksik hash özgünlük doğrulaması operatör onayıyla sınırlıdır; gerçek DB/kesinti kabulü yapılmadı.

## A-03 devamı — 6 Ekim journal tekrar deneme güvenliği

- 🟢 `06-recovery-archive-rollback.ps1`, önceki başarısız denemeler için her seferinde benzersiz `recovery-failed-{guid}.json` makbuzu yazar; var olan hata kaydı yeni rollback denemesini engellemez veya hata ayrıntısını ezmez.
- 🟢 `files-before-hashes.json` yolu varsa normal dosya olması zorunludur. Aynı adla klasör/özel yol bulunursa eski hash'siz journal olarak yorumlanmaz; işlem durur.
- 🟢 PowerShell parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Gerçek Windows kesinti ve PostgreSQL rollback kabulü yapılmadı.

## A-10 devamı — EBYS belge silme ve güncelleme sırası — 2026-10-06

- 🟢 EBYS dosya silme artık önce kaydı soft-delete edip DB değişikliğini kaydeder, ardından fiziksel dosyayı temizler. Fiziksel silme başarısızlığı `FileCleanupPendingException` olarak bildirilir.
- 🟢 EBYS dosya güncelleme eski dosyayı DB değişikliğinden önce silmez. Yeni şifreli dosya oluşturulur, DB'deki yol güncellenir ve commit sonucu hata verirse yeni yol taze context ile kontrol edilir; başvuru olup olmadığı doğrulanamazsa olası yetim dosya güvenlik için korunur. Commit başarılıysa eski dosya sonradan temizlenir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Test veya gerçek dosya silme çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı temizleme kuyruğu/yeniden deneme, mevcut yetim dosya envanteri ve diğer servislerin DB/dosya sırası bu düzeltme kapsamında tamamlanmadı.

## A-10 devamı — EBYS işlem hareketini dosya temizliğinden önce kaydetme — 2026-10-06

- 🟢 EBYS silme/güncellemede iş kaydı DB'ye yazıldıktan sonra EBYS hareket kaydı ekleniyor, fiziksel eski dosya en son temizleniyor. Temizlik hatası olsa da DB hareket kaydı yazılmış olur; fiziksel silme DB kaydından önce çalışmaz.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Test/gerçek dosya silme yapılmadı.
- 🔴 **A-10 açık:** Hareket/audit kaydı ve ana DB kaydı ayrı SaveChanges bağlamlarındadır; kalıcı cleanup kuyruğu, yetim envanteri ve diğer servislerin telafi akışı hâlâ gereklidir.

## A-10 devamı — araç uyarı ekranı upload telafisi — 2026-10-06

- 🟢 `BelgeUyariService.AracBelgeDosyaYukleAsync`, dosya DB kaydı hata döndürdüğünde artık yeni dosyayı körlemesine silmez. Taze context ile `AracEvrakDosyalari` içinde dosya yoluna başvuru aranır; başvuru varsa dosya korunur, yoksa telafi silmesi yapılır. DB doğrulaması veya telafi silmesi başarısızsa asıl hata ve telafi hatası birlikte yükseltilir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Test/gerçek upload çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı temizlik kuyruğu/yeniden deneme, yetim dosya envanteri ve diğer dosya yazan servislerin belirsiz commit telafisi tamamlanmadı.

## A-10 devamı — EBYS sürüm arşivini ana dosyadan ayırma — 2026-10-06

- 🟢 `BelgeVersiyonService.ArsivleEbysEvrakDosyaAsync`, eski ana dosyanın aynı yolunu sürüm kaydına kopyalamak yerine şifreli baytları `ebys/versions/{id}` altında bağımsız dosyaya kopyalar ve sürüm kaydını bu yola bağlar. Ana dosya güncellendiğinde eski sürüm artık ana dosya temizliğiyle kaybolmaz.
- 🟢 Arşiv DB kaydı başarısız/belirsizse taze context sürüm tablosunda bu yola başvuru arar; başvuru doğrulanmadan kopyayı silmez. Sürüm içeriği okuyucusu yeni şifreli depoyu açar, eski webroot kayıtları için kök dışına çıkmayan geriye uyumlu okuma sağlar.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Runtime/sürüm geri yükleme testi yapılmadı.
- 🔴 **A-10 açık:** Araç/personel sürüm arşivleri, kalıcı temizleme kuyruğu/yeniden deneme, eski yetim dosya envanteri ve servisler arası telafi hâlâ bekliyor.

## A-10 devamı — araç/personel sürüm arşivlerinin bağımsız dosyalanması — 2026-10-06

- 🟢 Araç ve personel sürüm arşivleri artık mevcut dosyanın aynı path'ini paylaşmaz; `CopyVersionFileAsync` ile bağımsız şifreli sürüm dosyası oluşturulur. SecureFileService dışındaki eski webroot dosyaları güvenli kök altında okunup yeni korumalı depoya alınır.
- 🟢 Sürüm içeriği okuyucuları şifreli depoyu açar, legacy webroot yolunu kök dışına çıkış denetimiyle destekler. (Sonraki 2026-10-06 düzeltmesiyle sürüm soft-delete artık fiziksel dosyayı silmiyor; aşağıdaki güncel not geçerlidir.)
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Runtime test/gerçek dosya işlemi yapılmadı.
- 🔴 **A-10 açık:** Dosya silme ile sürüm referans kontrolü eşzamanlı DB transaction/row lock altında değildir; kalıcı cleanup kuyruğu ve eski yetim dosya envanteri hâlâ gereklidir. Diğer servis akışlarının tamamı taranıp düzeltilmedi.

## A-10 devamı — sürüm silme yarışı ve personel evrakı yenileme sırası — 2026-10-06

- 🟢 Sürüm soft-delete işlemleri artık dosyayı fiziksel olarak silmiyor. Geri yükleme ile referans kontrolü/silme arasındaki yarış, kalıcı ve serileştirilmiş cleanup mekanizması gelene kadar dosyayı koruyarak önlendi.
- 🟢 EBYS personel evrakı yenilemesinde mevcut dosya yeni upload ve DB güncellemesinden önce silinmiyor. Yeni dosya önce şifreli depoya yazılıyor; DB kaydı hata verirse taze context ile ana kayıt ve sürüm tablosu kontrol ediliyor. Başvuru doğrulanamıyorsa dosya korunup hata görünür kılınıyor.
- 🟡 Eski personel dosyası bu akışta otomatik temizlenmiyor; geçmiş sürüm ilişkisi net olmadığı için tutuluyor. Kalıcı cleanup kuyruğu/yeniden deneme ve yetim envanteri gereklidir.
- 🔴 **A-10 açık:** Genel servis taraması, belirsiz commit telafileri ve kalıcı dosya temizleme altyapısı tamamlanmadı.

## A-10 devamı — fatura PDF/XML dosya değiştirme sırası — 2026-10-06

- 🟢 Fatura PDF ve XML yenilemesinde eski dosya artık yeni dosya ve DB yolu kaydedilmeden silinmiyor. Yeni içerik şifreli depoya yazılıyor, DB path'i sonra güncelleniyor.
- 🟢 DB kaydı hata verirse taze context ile PDF/XML yolunun fatura kayıtlarında bulunup bulunmadığı doğrulanıyor. Başvuru varsa PDF yüklemesi başarılı kabul ediliyor; başvuru yoksa yeni dosya telafi ediliyor. Doğrulama/temizlik hatasında AggregateException ile belirsizlik görünür kalıyor.
- 🟡 Eski dosya otomatik kaldırılmıyor; firma içi kopya faturalar aynı yolu paylaşabildiği için güvenli, referans kontrollü kalıcı cleanup kuyruğu bekleniyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Runtime fatura dosyası işlemi yapılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup/yeniden deneme kuyruğu, yetim dosya envanteri ve diğer tüm dosya yazan servislerin incelemesi gereklidir.

## A-10 devamı — destek eki şifreli depolama ve upload telafisi — 2026-10-06

- 🟢 Yeni destek talebi ve yanıt ekleri artık webroot içine düz metin yazılmıyor; `ISecureFileService` ile şifreli depoya kaydediliyor. Okuma akışı şifreli yeni yolları açıyor ve eski webroot eklerini kontrollü kök altında geriye dönük destekliyor.
- 🟢 DB kaydı başarısız/belirsiz kaldığında taze context ile ek tablosu kontrol ediliyor; DB başvurusu yoksa yeni dosya telafi ediliyor, doğrulama/temizlik hatasında dosya korunup hata görünür kılınıyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek ek yükleme/indirme/silme senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Eski düz metin eklerin toplu şifreli depoya taşınması, kalıcı cleanup kuyruğu/yeniden deneme ve yetim envanteri henüz yapılmadı.

## A-10 devamı — ortak evrak ekranı dosya sırası ve yol doğrulaması — 2026-10-06

- 🟢 Ortak evrak ekranında yenileme öncesi eski dosya silme kaldırıldı. Yeni dosya DB kaydından önce yazılıyor; DB başarısız/belirsiz kalırsa taze context ile referans doğrulanıp yalnız başvurulmayan yeni dosya telafi ediliyor.
- 🟢 Evrak silme akışında önce DB soft-delete kaydediliyor; fiziksel silmeden önce diğer aktif kayıtlar denetleniyor. Temizlik hatası artık sessizce yutulmuyor.
- 🟢 `FileService` okuma/silme/yol çözümlemesi, depolama kökü dışına taşan veya mutlak yol içeren dosya adlarını reddediyor.
- 🟡 Eski evrak dosyaları, kalıcı referans kontrollü temizleme kuyruğu kurulana kadar tutuluyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Runtime dosya senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Eski düz metin dosyaların şifreli depoya taşınması, kalıcı cleanup/yeniden deneme ve yetim envanteri devam ediyor.

## A-10 devamı — personel özlük ekranı upload telafisi ve sürüm referansları — 2026-10-06

- 🟢 Personel özlük ekranında upload sonrası bir UI/tarih yenileme hatası yeni dosyayı silip DB'de bozuk başvuru bırakabiliyordu. Telafi artık yeni yolu ana kayıt, sürüm geçmişi ve ortak evrak kayıtlarında kontrol ediyor; başvuru varsa dosyayı koruyor, kontrol başarısızsa da silmiyor.
- 🟢 Başarılı yenilemede eski fiziksel dosya otomatik silinmiyor. Hızlı silme de ana kayıt temizlendikten sonra sürüm ve ortak evrak referanslarını kontrol ediyor; geçmişte kullanılan dosyanın silinmesini erteliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek upload/geri yükleme/silme senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup kuyruğu/yeniden deneme ve yetim dosya envanteri; ayrıca eski dosyaların kontrollü temizliği gerekiyor.

## A-10 devamı — araç/personel arşiv dosyalarının sürüm başına benzersizleştirilmesi — 2026-10-06

- 🟢 Arşiv servisi aynı araç/personel evrak tipi için sabit şifreli dosya adını tekrar kullanıyordu; yeni yükleme geçmiş sürüm içeriğinin üstüne yazabiliyordu. Her yeni arşiv yazımı artık GUID tabanlı benzersiz dosya yoluna kaydediliyor.
- 🟢 Araç upload telafisi artık DB kayıt işleminden sonra çalışan cache temizliği gibi bir adım hata verdiğinde taze context ile dosya yolu ve sürüm referanslarını denetliyor; başvurulan dosyayı silmiyor. Doğrulama/temizlik hatasında belirsizlik AggregateException ile korunuyor.
- 🟢 Personel arşiv yüklemesi kaynak dosya adı/türü/boyutunu kayıt metoduna iletiyor; dosya adı benzersiz saklama adıyla karışmıyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek arşiv sürümleme testi yapılmadı.
- 🔴 **A-10 açık:** Mevcut sabit isimli arşivlerin geriye dönük taraması, cleanup kuyruğu/yeniden deneme ve yetim envanteri gereklidir.

## A-10 devamı — araç evrak upload telafisi ve silme referans kontrolü — 2026-10-06

- 🟢 Araç upload telafisi, yeni dosya DB’de veya sürüm tablosunda referanslıysa artık silmiyor. Cache invalidation gibi DB commit sonrası hata oluşsa da içerik korunuyor; referans denetimi başarısızsa AggregateException ile işlem belirsizliği bildiriliyor.
- 🟢 Araç evrak dosyası silme DB soft-delete sonrasında taze context ile diğer aktif dosya ve sürüm referanslarını kontrol ediyor; bilinen paylaşımlı yol fiziksel olarak kaldırılmıyor.
- 🟡 Referans kontrolü ile fiziksel silme halen tek bir DB transaction/lock içinde değil. Kalıcı serialized cleanup kuyruğu kurulana kadar A-10 kırmızı kalıyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Runtime eşzamanlı upload/restore testi yapılmadı.

## A-10 devamı — Şoför formu upload telafisi — 2026-10-06

- 🟢 Şoför formunda post-save içerik doğrulaması veya UI yenilemesi hata verdiğinde `catch` artık yeni dosyayı körlemesine silmiyor. Taze DB context ile özlük ana kayıtları, sürüm geçmişi ve ortak evrak yolları denetleniyor.
- 🟢 DB başvurusu varsa dosya korunuyor; referans sorgusu veya cleanup başarısızsa hata loglanıp belirsizlik AggregateException ile bildiriliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek personel upload senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup kuyruğu/yeniden deneme, yetim envanteri ve tüm upload/silme yollarının çalışma zamanı kabulü gereklidir.

## A-10 devamı — taşıma tedarikçisi eki upload telafisi ve referans kontrolü — 2026-10-06

- 🟢 Tedarikçi eki upload hatasında yeni şifreli dosya silinmeden önce taze context ile dosya yolu DB'de aranıyor. Belirsiz committe referans varsa dosya korunuyor; sorgu/temizlik hatası AggregateException ile görünür.
- 🟢 Tedarikçi eki silme DB soft-delete sonrasında diğer aktif eklerde aynı yol aranarak fiziksel temizlik yapıyor; ortak kullanılan dosya silinmiyor.
- 🟡 Kontrol ve fiziksel silme arasında çoklu süreç kilidi yok; A-10'un kalıcı cleanup kuyruğu/serialized işleyicisi hâlâ açık.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Runtime upload/silme senaryosu çalıştırılmadı.

## A-10 devamı — destek talebi eki silme sırası — 2026-10-06

- 🟢 Destek talebi eki silmede önce DB soft-delete kaydediliyor. Sonra taze context ile başka aktif ek kaydının aynı dosya yoluna başvurup başvurmadığı kontrol ediliyor; fiziksel dosya yalnız başvuru yoksa siliniyor.
- 🟢 Fiziksel yolun destek yükleme kök dizini altında olduğu doğrulanıyor. Silme/konum hataları artık boş catch ile yutulmuyor; ek kimliği loglanıp `FileCleanupPendingException` ile bildiriliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Destek eki runtime silme senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup/yeniden deneme kuyruğu, yetim dosya envanteri ve diğer tüm dosya yazan servislerin incelemesi gereklidir.


## Kurulumda veritabanı seçimi — 2026-10-06

- 🟢 Ana (Setup.iss) ve müşteri (MusteriSetup.iss) Inno kurulumlarına PostgreSQL / SQLite / MSSQL seçim adımı eklendi. PostgreSQL bağlantı alanları; SQLite dosya yolu alınır ve seçime uygun dbsettings.json kurulumda yazılır. Güncelleme paketi mevcut ayarı korur.
- 🟢 IIS kur.ps1 -Mode Install kurulumunda da sağlayıcı sorulur; PostgreSQL parolası maskeli alınır, yapılandırma geçici dosyadan atomik taşınır. Kurulum modu mevcut SQLite dosyasını artık silmez.
- 🟡 **MSSQL açık:** Seçenek sihirbazda görünür, seçildiğinde neden ilerlenemediği bildirilir. DbInitializer.InitializeAsync yalnız PostgreSQL/SQLite desteklediğinden SQL Server otomatik migration/audit desteği eklenmeden MSSQL kurulumu tamamlanamaz; A-28 kırmızı kalır.
- 🟢 Güncel Web/DataSync publish ile ana, güncelleme ve müşteri EXE paketleri v1.0.37 üretildi. Ana IIS kurulumunda dbsettings.json okuması yöneticiler ve yalnız ilgili uygulama havuzuyla sınırlandı; SQLite App_Data yazma izni uygulama havuzuna verilir. 🟡 Etkileşimli hedef makine kurulumu ve gerçek DB bağlantı kabulü yapılmadı.

## A-15 devamı — aktif araç plakası DB tekilliği — 2026-10-06

- 🟢 Aktif ve silinmemiş `AracPlakalar` kayıtları için filtreli benzersiz indeks; model, snapshot ve migration ile eklendi. Migration öncesi yinelenen aktif plakalar denetleniyor ve varsa veri değiştirmeden duruyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Gerçek PostgreSQL/SQLite migration ve eşzamanlı kayıt kabulü yapılmadı. A-15'in banka referansı, dönem snapshot, varsayılan şablon ve kalan firma ilişkileri açık; ana son durum raporunda 🔴 olarak izlenir.

## A-15 devamı — banka importu eşzamanlı tekrar koruması — 2026-10-06

- 🟢 Referans numaralı import satırlarına firma/tarih/referans/tutar/yön bileşiminden deterministik hash ekleniyor; aktif kayıtlar firma kapsamında benzersiz indeksle korunuyor. Soft-delete kayıtlar yeniden importu engellemiyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Gerçek migration/eşzamanlı import kabulü ve geçmiş satırlar için geriye dönük anahtar doldurma yapılmadı. Dönem snapshot/varsayılan şablonlar A-15 içinde açık.

## A-15 devamı — aylık personel maaş snapshot tekilliği — 2026-10-06

- 🟢 Firma/yıl/ay/personel doğal anahtarında aktif maaş snapshotlarını benzersiz tutan filtreli indeks ve yinelenen kayıt ön kontrolü eklendi.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Gerçek migration/eşzamanlı yazım kabulü yapılmadı. Araç maliyet snapshotı soft-delete davranışı ve varsayılan şablon kısıtları açık.

## A-15 devamı — araç maliyet snapshotı yeniden üretim uyumu — 2026-10-06

- 🟢 Araç/yıl/ay tekil indeksi artık yalnız aktif snapshotlara uygulanıyor. Silinen dönem snapshotı yeniden üretilebilir; migration mevcut indeksi filtreli indeksle değiştirir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Migration ve silme/yeniden üretme runtime kabulü yapılmadı. Varsayılan fatura/grup şablonları A-15 içinde açık.

## A-15 devamı — varsayılan fatura ve grup şablonu tekilliği — 2026-10-06

- 🟢 Aktif fatura varsayılanı firma başına, grup şablonu varsayılanı ise firma geneli ve kullanıcı kapsamlarında ayrı filtreli benzersiz indekslerle korunuyor.
- 🟢 Migration öncesi yinelenen varsayılan kontrolü eklendi; çakışma varsa mevcut satırlar değiştirilmeden migration reddediliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Gerçek migration ve eşzamanlı şablon değiştirme kabulü yapılmadı; A-15’in diğer tenant ilişkileri açık.
- 🟢 Temiz kurulumda henüz migration edilmemiş fatura/grup şablonu tabloları için yinelenen veri ön kontrolü güvenli biçimde atlanıyor.

## A-15 devamı — banka hareketi hesap/cari firma kapsamı — 2026-10-06

- 🟢 Banka/Kasa hareketi servisinde oluşturma/güncelleme seçilen hesabı ve cariyi hareket firmasıyla aynı kapsamda doğruluyor; güncelleme mevcut kaydın firma kapsamını değiştiremiyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Diğer doğrudan yazım yolları ve DB composite FK kapsamı tamamlanmadı; gerçek firma A/B kabulü yapılmadı.

## A-15 devamı — ortak SaveChanges ilişki denetimi — 2026-10-06

- 🟢 Banka/Kasa hareketi ekleyen/değiştiren EF yazımları hesap, cari ve personel geri ödeme hesabı firma eşleşmesini ortak SaveChanges sınırında denetler; aynı context'te yeni eklenen cari ve mevcut hareketin firma taşıması da kapsanır.
- 🟢 Web Debug derlemesi 0 uyarı/0 hata. Geçici SQLite bellekiçi doğrulamada yanlış firma hesabı, cari, yeni cari, geri ödeme hesabı ve firma taşıma reddedildi; aynı firma hareketi kaydedildi.
- 🟢 Audit `EntityId` iç düzeltmesi tenant sorgu filtresinden bağımsız yapılıyor; açık firma sağlayıcısı bulunmayan izole kayıtta iki yeni firma audit kimliği doğrulandı.
- 🟡 Raw SQL/dış yazımlar, DB composite FK, diğer tenant bağlantıları ve gerçek PostgreSQL/SQLite migration/kabul açık. A-15 🔴 kalır.

## A-15 devamı — banka hareketi veritabanı firma denetimi — 2026-10-06

- 🟢 PostgreSQL/SQLite migration'ı banka hareketinin hesap/cari/geri ödeme hesabı firma bağını tetikleyicilerle denetler; bağlı hesap veya carinin firma değiştirmesini de engeller. Ön kontrolde eski uyuşmazlık varsa veri değiştirmeden migration durur.
- 🟢 Web Debug derlemesi 0 uyarı/0 hata. 🟡 Gerçek DB migration, eski veri onarımı, diğer ilişkiler ve eşzamanlılık kabulü açık; A-15 🔴 kalır. Ayrıntı [görev envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md).

## 🟢 A-22 kapanışı — güncel NuGet taraması — 2026-10-06

- LisansDesktop geçişli `SQLitePCLRaw.lib.e_sqlite3 2.1.11` yüksek önem dereceli bildirimi tespit edildi; doğrudan 2.1.13 bağımlılığı eklendi ve dahili Release/win-x64 EXE yenilendi.
- Altı çözüm projesi ve çözüm dışı Rent-a-Car kontrol projesinin doğrudan/geçişli son taramasında bilinen açık raporlanmadı. CI tarama hatasında başarısız olur ve iki proje kümesini kapsar. [Ayrıntı](A-22-BAGIMLILIK-TARAMASI-2026-10-06.md). A-22 🟢; müşteri kurulum kabulü A-21 🟡 kalır.

## A-15 devamı — iki sağlayıcıda izole firma bağı denetimi — 2026-10-06

- 🟢 SQLite bellek DB ve geçici PostgreSQL 17 kümesinde migration ön kontrolü, geçerli yazım ve yedi ret senaryosu geçti. Hareketin aynı anda hesap/cari değiştirerek firma değiştirmesi de DB'de reddediliyor.
- 🟢 Web Debug derlemesi 0 uyarı/0 hata. [İzole kanıt ve sınır](A-15-IZOLE-FIRMA-BAGI-DOGRULAMA-2026-10-06.md). 🟡 Tam model/müşteri migration ve eşzamanlılık kabulü açık; A-15 🔴.

## Y-7 / A-07 güncellemesi — 2026-10-06

- 🟢 `MKFiloServis.Tests` xUnit projesi oluşturuldu; A-15 SQLite firma bağı için 9 test yerelde başarılı. CI artık test projesini algılayıp atlamak yerine zorunlu derleyip çalıştırır ve TRX sonucunu saklar.
- 🟡 GitHub çalışma sonucu ve lisans, tenant, audit, restore, mali işlem regresyonları henüz yok. Önceki bölümlerdeki “test projesi yok” ifadeleri tarihsel durumu anlatır; güncel A-07 durumu 🟡. [Görev envanteri](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md).

## Y-7 / A-07 lisans protokolü ek testi — 2026-10-06

- 🟢 V3 modül zarfı, geçersiz modül dizisi, RSA-PSS modül bağı ve sürüm sınırı regresyonları eklendi; yerel Release sonucu **21/21 başarılı**.
- 🟡 Bu ortak protokol testleri gerçek müşteri lisansı yükleme ve modül erişimi kabulünün yerine geçmez. A-01/A-02/A-07 🟡; güncel kapsam [görev envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md).

## A-10 upload güvenilirliği güncellemesi — 2026-10-06

- 🟢 `SecureFileService` şifreli dosyaları önce aynı klasörde geçici dosyaya yazar; tamamlanınca son ada taşır. Böylece son dosya yolu kısmi bir şifreli yazım sırasında görünür olmaz.
- 🟢 Başarılı yükleme/iptal regresyonları eklendi; Release test toplamı **23/23 başarılı**.
- 🔴 Kalıcı cleanup kuyruğu, tekrar deneme ve yetim envanteri ile diğer yazım yollarının denetimi açık; A-10 kırmızı.

## A-10 personel özlük referans güvenliği — 2026-10-06

- 🟢 Fiziksel özlük dosyası silinmeden önce mevcut ve sürüm kayıtları `IgnoreQueryFilters()` ile kontrol ediliyor; başka kayıt aynı yolu kullanıyorsa silme yapılmıyor.
- 🟢 Release derleme ve mevcut 23 test geçti. Eski paylaşılan yol için ayrı DB kabul testi henüz yok.
- 🔴 Çoklu süreç kilidi, kalıcı cleanup kuyruğu, yeniden deneme ve diğer dosya türlerinin genel referans taraması açık.

## A-10 yerel nesne deposu yolu — 2026-10-06

- 🟢 Yerel depo anahtarlarının boş/köklenmiş veya uploads dışına çözümlenen biçimleri reddedilir; upload geçici dosyadan yayımlanır; silme erişim hatalarını `File.Exists` ile gizlemez.
- 🟢 Yol, atomik upload ve eksik dosya silme testleri dahil Release paketi **25/25 başarılı**.
- 🔴 Symlink ve gerçek storage kesintisi, kalıcı cleanup/yeniden deneme kuyruğu ve tam dosya referans envanteri açık.

## A-10 yerel depolama sembolik bağlantı denetimi — 2026-10-06

- 🟢 `StorageFilePath.Resolve` depolama içindeki mevcut sembolik bağlantı bileşenlerinden geçen yolları reddediyor; `LocalObjectStorageService` ortak çözümleyiciye geçirildi.
- 🟢 Release test paketi **25/25 başarılı**, `git diff --check` temiz.
- 🟡 Windows test kullanıcısının sembolik bağlantı oluşturma ayrıcalığı olmadığından doğrudan bağlantı saldırısı testi çalışmadı. Kontrol ile dosya işlemi arasındaki TOCTOU yarışı, cleanup kuyruğu, yetim envanteri ve gerçek storage kabulü açık; A-10 🔴 kalır.

## A-10 yerel depo eksik klasör silme davranışı — 2026-10-06

- 🟢 Var olmayan dosyanın üst klasörü de yoksa silme başarılı no-op olur; erişim/IO hataları `File.Exists` ile gizlenmez.
- 🟢 Regresyon testleri dahil Release testleri 26/26 geçti, `git diff --check` temiz.
- 🔴 Kalıcı cleanup kuyruğu/yeniden deneme, çok süreçli yarış/TOCTOU, yetim envanteri ve gerçek depolama kabulü açık olduğundan A-10 kapanmadı.

## A-10 ortak idempotent silme — 2026-10-06

- 🟢 `StorageFilePath.DeleteIdempotently` eksik üst klasörü idempotent ele alır; hem şifreli dosya servisi hem yerel depo ortak metodu kullanır. Erişim/IO hataları gizlenmez.
- 🟢 Release test paketi 26/26 başarılı ve `git diff --check` temiz.
- 🔴 Şifreli servis entegrasyon testi, kalıcı cleanup/yeniden deneme, TOCTOU/çok süreçli yarış, yetim envanteri ve gerçek storage kabulü açık kaldı.

## A-10 DosyaMigrasyonService veri kaybı ve uploads yol sınırı — 2026-10-06

- 🟢 Legacy açık dosya, yeni şifreli dosya ve yeni DB yolu kaydı tamamlanıp doğrulanmadan silinmiyor; SaveChanges belirsizliğinde eski dosya korunuyor. Başka DB satırları eski yolu kullanıyorsa eski dosya tutuluyor.
- 🟢 Kaynak path uploads köküyle sınırlandı ve sembolik bağlantı bileşenleri reddediliyor. Release testleri **26/26 başarılı**, `git diff --check` temiz.
- 🔴 Kalıcı cleanup kuyruğu/yeniden deneme, gerçek legacy DB+dizin üzerinde migration kabulü, genel yetim taraması ve çok süreçli TOCTOU kontrolü açık.

- 🟢 Paylaşılan eski yol denetimi bilinen file path DB alanlarını (ortak evrak, özlük, destek, tedarikçi, fatura/proforma dâhil) soft-delete satırlarını da kapsayacak biçimde tarar.

## A-10 migration erişimi ve sayım kapsamı — 2026-10-06

- 🟢 Dosya migration ekranı Admin rolü gerektiriyor; EBYS lisans politikası da birlikte uygulanır.
- 🟢 UI ve servis artık yalnız aktif firma filtreli EBYS/araç ana belge ve sürümlerini kapsadığını söylüyor; tüm düz metin dosyalarına yönelik yanlış izlenim kaldırıldı. Release test paketi 26/26 geçti.
- 🔴 Soft-delete/destek/fatura/ortak evrak yolları ve tenant kabul kapsamı açık; genel orphan raporu ve kalıcı cleanup kuyruğu yapılmadı.

## A-10 şifreli orphan raporu — 2026-10-06

- 🟢 Admin bakım raporu uploads, Arsiv ve Depo depolarındaki `.enc` dosyaları bilinen `DosyaYolu`/PDF/XML DB alanlarıyla karşılaştırır ve soft-delete kayıtlarını referans olarak sayar.
- 🟢 Yalnız raporlama yapar, hiçbir orphan dosyayı silmez. Release testi 27/27 geçti; diff kontrolü temiz.
- 🔴 Eski düz metinler, tüm varlık/property envanteri, gerçek DB/storage kabulü ve kalıcı temizleme/yeniden deneme akışı açık kaldı.

## A-10 kalıcı dosya temizleme kuyruğu — 2026-10-06

- 🟢 Şifreli/atomik DP journal ve hosted retry worker eklendi. Silme öncesinde bilinen DB dosya yolu alanları soft-delete filtreleri yok sayılarak kontrol edilir; journal girdileri idempotent ve hatada artan aralıkla ertelenir.
- 🟢 Journal persistence/retry regresyonu eklendi; Release test paketi **28/28 başarılı**, `git diff --check` temiz.
- 🟢 Kuyruk girdileri 5 dakikalık lease ile claim edilir; aynı lease aktifken başka worker girdiyi alamaz, lease süresi geçince tekrar denenebilir. Release paketi 28/28 başarılı.
- 🟢 Worker her turda tek girdiyi claim eder ve işlem sürerken lease'i dakikada bir yeniler. Test eski lease bitişinden sonra ikinci claim'i ve geçersiz sahip yenilemesini kontrol eder. Release test paketi 28/28 başarılı.
- 🔴 DB commit ile journal enqueue tek transaction değildir; arada crash olursa dosya orphan kalabilir. Referans kontrolü ve unlink atomik değildir; yenileme kesilirse lease kaybedilebilir. Gerçek müşteri DB/storage ve key-ring kurtarma kabulü yapılmadı; A-10 açık kalır.

## A-10 mutlak yol ve DB referans eşleştirmesi — 2026-10-06

- 🟢 Depolama kökü altındaki mutlak DB yolları kanonik anahtara çevrilir. Worker doğrudan SQL eşleşmesi bulamayınca bilinen yol alanlarını kanonikleştirerek kontrol eder; Windows harf büyüklüğü farkında referanslı dosyayı silmez.
- 🟢 Mutlak yol regresyonu dahil Release test paketi **28/28 başarılı**.
- 🔴 Gerçek müşteri DB'sindeki tam tarama performansı ve referans kontrolü ile unlink arasındaki yarış açık; A-10 kapanmadı.

## A-10 şifresiz dosya aday envanteri — 2026-10-06

- 🟢 Admin evrak bakım raporu uploads ve Arsiv altındaki şifresiz, geçici olmayan ve DB'de bilinen yol alanlarında bulunmayan dosyaları ayrı listeler. Sembolik bağlantılar izlenmez; rapor otomatik silmez.
- 🟢 Tarama aynı DB yol envanterini kullanır; Release test paketi **28/28 başarılı**.
- 🔴 Bunlar manuel inceleme adaylarıdır. Referanslı eski açık dosyaların taşınması, tam yol alanı kapsamı ve gerçek müşteri DB/depo kabulü açık; A-10 🔴.

## A-10 tam model SQLite referans doğrulaması — 2026-10-06

- 🟢 İzole SQLite üzerinde tam `ApplicationDbContext` modeli oluşturuldu. Soft-delete edilmiş, farklı harf büyüklüğünde dosya yolu referanslı olarak korundu; bulunmayan yol serbest bırakıldı. Release test paketi **29/29 başarılı**.
- 🔴 Gerçek müşteri DB/depo worker kabulü, PostgreSQL çevirisi, DB commit-kuyruk crash aralığı ve kontrol-silme yarışı açık; A-10 rengi değişmedi.

## A-10 izole worker ve fiziksel silme kabulü — 2026-10-06

- 🟢 Tam model SQLite, uploads dosyaları, kalıcı kuyruk ve `SecureFileService` birlikte çalıştırıldı. Worker soft-delete DB referansı bulunan dosyayı korudu; referanssız dosyayı fiziksel olarak sildi ve kuyruktan çıkardı. Release test paketi **30/30 başarılı**.
- 🔴 Gerçek müşteri DB/depo ile PostgreSQL kabulü, DB commit-kuyruk crash aralığı ve sorgu-unlink yarışı açık; A-10 🔴 kalır.

## A-10 soft-delete dosyalarını geri alma için koruma — 2026-10-06

- 🟢 Kullanıcı kararına göre soft-delete satırına bağlı fiziksel dosya korunur. `SecureFileService` hem doğrudan silme çağrısında hem worker işinde DB referansını denetler; referans varsa journal isteği tamamlanır.
- 🟢 Tam model SQLite testi doğrudan ve worker çağrısında referanslı dosyayı korudu, referanssız dosyayı sildi. DB referans sorgusu arızasında dosya ve journal isteği korundu. Release testleri **31/31 başarılı**.
- 🔴 Gerçek müşteri DB/depo kabulü, commit-journal crash aralığı, referans sorgusu-unlink yarışı ve eski dosya kapsamı açık olduğundan A-10 kapanmadı.

- 🟢 Ortak evrak ekranı ve destek eki servisindeki eski düz dosya doğrudan silme adımları soft-delete sonrasından çıkarıldı; dosyalar geri alma için kalır.
- 🟢 Ortak evrakın yeni yüklemesi şifreli depoya taşındı; eski düz dosya okuma ve bakım ekranında varlık denetimi korundu. Web Release derlemesi 0 uyarı/0 hata.
- 🟡 Geçmiş düz dosyaların toplu şifreli geçişi açık.

- 🟢 Yönetici geçiş ekranı aktif ortak evrakın eski düz dosyalarını da sayıp taşır; yeni yol DB'de doğrulanır ve soft-delete kayıtlarının eski yol başvurusu korunur. Web Release derlemesi 0 uyarı/0 hata.
- 🔴 Silinmiş kayıtların kendi şifreli geçişi ve gerçek müşteri verisiyle kabul açık.

- 🟢 Yönetici geçişi tek aktif firma seçimini zorunlu tutar; firma alanı olmayan ortak evrak bağlı personel/araç üzerinden sınırlandırılır. Eski dosya yolu çözümü sembolik bağlantıyı reddeder.

- 🟢 Geri alınabilir silinmiş ortak evrak satırlarının dosya yolları da firma bağlantısı doğrulanarak şifreli depoya taşınır; önizlemede ayrı sayılır. Web Release derlemesi 0 uyarı/0 hata.
- 🔴 Bağı kopuk kayıtlar, EBYS/araç silinmiş dosya geçmişi ve gerçek müşteri veri kabulü açık.

- 🟢 Araç ana/sürüm geçişi seçili firma kimliğiyle sınırlandırıldı. EBYS kayıtlarının firma bağı olmadığı yönetici ekranında açıklandı; bu sahiplik sorunu ayrıca açık.

- 🟢 Şifreli geçiş kopyası DB yolu değiştirilmeden geri okunup kaynakla karşılaştırılır; doğrulanmazsa eski dosya korunur. Web Release derlemesi 0 uyarı/0 hata. Gerçek veriyle geçiş kabulü açık.

- 🟢 Yeni şifreli yol DB'ye yazılıp eski dosya temizlenemezse UI ayrı `Temizlik Bekliyor` durumunu gösterir. Bakım envanteri eski ortak evrak ve wwwroot yüklemelerindeki referanssız düz dosya adaylarını da salt okunur listeler.
- 🔴 Bu satırdaki eski köklerde kalıcı retry eksikliği aşağıdaki sonraki düzeltmeyle giderildi; gerçek müşteri depolama kabulü açık.

## A-10 eski düz dosya temizliğinin kalıcı kuyruğu — 2026-10-06

- 🟢 Geçiş sonrası eski ortak evrak ve `wwwroot/uploads` dosyaları için silme isteği DP korumalı `FileCleanupJournal` içine kaydedilir. `FileCleanupRetryWorker` bu tipli istekleri tekrar işler. Her denemede aktif ve silinmiş DB yolu referansları yeniden kontrol edilir; geri alma için kullanılan dosya korunur.
- 🟢 Son Web Release derlemesi 0 uyarı/0 hata; bu değişiklik için çalışma zamanı testi yapılmadı.
- 🔴 Bu satırdaki eski dosya geçişi commit-journal kesintisi aşağıdaki sonraki düzeltmeyle giderildi. Referans sorgusu ile fiziksel silme yarışı, büyük DB performansı ve gerçek müşteri DB/depo kabulü açık. A-10 kırmızı kalır.

## A-10 geçiş temizleme isteğinin DB öncesi kaydı — 2026-10-06

- 🟢 Eski düz dosya için temizleme isteği, doğrulanmış yeni şifreli kopyanın yolu ile birlikte DB yol değişiminden önce kalıcı journal'a yazılır. Worker yeni yol DB'de doğrulanana kadar eski dosyayı koruyup isteği erteler; commit sonrası yeniden değerlendirir. v1 kuyruk girdileri okunur.
- 🟢 Web Release derlemesi 0 uyarı/0 hata. Son ek için çalışma zamanı testi yapılmadı.
- 🔴 DB yazımı başarısız kalırsa güvenli bekleyen isteğin işletim temizliği, diğer silme akışlarının commit-kuyruk aralığı, referans kontrolü-unlink yarışı ve gerçek müşteri kabulü açık. A-10 kırmızı kalır.

## A-10 geri alınabilir EBYS ve araç dosyalarını geçiş kapsamına alma — 2026-10-06

- 🟢 Dosya geçişi silinmiş EBYS ve seçili firmaya ait araç ana dosyaları ile versiyon satırlarını da `IgnoreQueryFilters` üzerinden bulur; önizlemede aktif/silinmiş sayılar ayrıdır.
- 🟢 Araç satırının `FirmaId` değeri ile bağlı evrak/aracın firma ilişkisi doğrulanır; başka firmaya ait, kopuk veya tutarsız kayıt geçişe girmez. Yönetici ekranı EBYS'nin firma bağı olmadığını açıkça belirtir.
- 🟢 Web Release derlemesi 0 uyarı/0 hata. Gerçek müşteri verisiyle geçiş testi yapılmadı.
- 🔴 EBYS firma sahipliği, bozuk eski araç verilerinin onarımı, büyük DB performansı, başka dosya alanları ve saha kabulü açık. A-10 tamamlanmadı.

## A-10 personel özlük ve fatura dosya geçişi — 2026-10-06

- 🟢 Seçili firma sürücüleriyle bağlı personel özlük dosyaları/sürümleri ve doğrudan `FirmaId` ile bağlı fatura PDF/XML alanları silinmiş satırlar dahil migrasyona eklendi.
- 🟢 `/uploads/`, `uploads/`, ters eğik çizgili web upload yolları ve personel için eski ortak dosya adı destekleniyor. Yönetici özet ekranı modül başına aktif/silinmiş sayıları gruplayarak gösteriyor.
- 🟢 Web Release derlemesi 0 uyarı/0 hata. Gerçek DB ve fiziksel dosya geçişi bu turda çalıştırılmadı.
- 🔴 Destek/tedarikçi eski dosyalarının fiziksel kökü ve kayıt kapsamı, EBYS firma sahipliği, kalan doğrudan yazım/silme akışları, performans ve saha kabulü açık; A-10 kırmızı kalır.

### A-10 dosya migrasyonunda fatura deposu — 2026-10-06

- 🟢 Fatura legacy okuyucusuyla aynı `{StorageRoot}/uploads` kökü kullanıldı. Cleanup journal bu kökü webroot ve ortak dosya kökünden ayırıyor; eski URL alias'ları ve DB başvuru kontrolü korunuyor.
- 🟢 Release derlemesi 0 uyarı/0 hata; bu değişiklik için runtime veya gerçek dosya geçişi yapılmadı.
- 🔴 Saha geçişi, destek/tedarikçi güvenli firma kapsamı, EBYS firma sahipliği, kalan cleanup yarışları ve büyük veride performans açık; A-10 tamamlanmadı.

### A-10 tedarikçi dosya migrasyonu — 2026-10-06

- 🟢 Seçili firma cari hesabına açıkça bağlı tedarikçilerin silinmiş/aktif ekleri için geçiş ve özet sayaçları eklendi. Eski tekil dosya adları yalnız ortak upload kökünde çözümlenir.
- 🟢 Web Release derlemesi 0 uyarı/0 hata; gerçek veri geçişi yapılmadı.
- 🔴 Firma bağı kesin olmayan destek ekleri ve güvenli biçimde normalize edilmemiş mutlak legacy yolları kapsam dışı; saha kabulü, performans ve diğer yaşam döngüsü riskleri sürdüğünden A-10 kapanmadı.

### A-10 destek dosyası migrasyon kapsamı — 2026-10-06

- 🟢 Ticket ve yanıt ekleri cari-firma tenant ilişkisiyle sınırlandı; silinmiş kayıtlar da geri alınabilir kopya için taranır.
- 🟢 Eski mutlak yol yalnız `wwwroot/uploads/destek` altında kabul edilir. Kalıcı temizlik journal'ı destek kökünü ayrı ve sınırlı bir türle çözümler. Web Release derlemesi 0 uyarı/0 hata.
- 🔴 Cari bağı olmayan ticket’lar ve izinli kökün dışındaki legacy yollar kapsam dışıdır. Gerçek veri, worker, TOCTOU ve müşteri kabulü gereklidir; A-10 kapanmadı.

### A-10 personel dosya temizliğinde journal-first akışı — 2026-10-06

- 🟢 Özlük dosya yolu null yapılmadan veya sürüm satırı kaldırılmadan önce retry günlüğü kalıcılaştırılıyor. Worker referansın DB'den kalkmasını bekleyen journal girdisini referans varken tamamlamıyor.
- 🟢 Release derlemesi 0 uyarı/0 hata. Çalışma zamanı kabulü yapılmadı.
- 🔴 Akış henüz diğer modüllerin fiziksel dosya silmelerine uygulanmadı; DB referans kontrolü-unlink yarışı, gerçek müşteri worker testi ve performans açık; A-10 kırmızı.

### A-10 EBYS güncellemesinde eski dosya temizleme sırası — 2026-10-06

- 🟢 Eski dosya cleanup isteği EBYS yeni DB yolundan önce journal'a yazılıyor; worker DB referansı durdukça isteği koruyup yeniden dener.
- 🟢 Belirsiz SaveChanges'te DB'den güncel yol kontrol edilerek eski/yeni dosya telafisi yapılır. Release build 0 uyarı/0 hata.
- 🔴 Gerçek çalışma zamanı ve müşteri DB kabulü yapılmadı; diğer modül akışları ve kontrol/unlink yarışı açık, A-10 tamamlanmadı.

### 2026-10-06 — A-10 fatura PDF/XML değiştirmede eski dosyayı güvenli temizleme

- 🟢 Fatura PDF ve XML değişiminde eski yol, DB `SaveChanges` öncesi kalıcı temizleme günlüğüne eklenir. Şifreli yollar DB referansı kalkana kadar yeniden denenir; legacy `/uploads/...` dosyaları doğru storage köküyle, yeni DB yolu doğrulandıktan sonra işlenir.
- 🟢 DB yazımı başarısız/belirsizse mevcut fatura yolu yeniden okunur. Eski yol hâlâ kayıtlıysa yeni dosya telafi edilir ve eski yolun isteği kaldırılır; yeni yol commit olduysa eski temizleme isteği korunur. Web Release derlemesi **0 uyarı / 0 hata**; çalışma zamanı/fatura verisiyle doğrulama yapılmadı.
- 🔴 A-10 kırmızı kalır: diğer dosya yolu değiştirme/silme akışlarının tamamı, gerçek müşteri DB/storage worker kabulü, referans sorgusu-unlink yarışı ve büyük envanter performansı açıktır.

### 2026-10-06 — A-10 yönetici arşiv geçişinde belirsiz commit telafisi

- 🟢 Yönetici arşiv geçişinde personel/araç dosyasının kopyalanması veya DB transaction sonucu hata verirse, yeni dosya silinmeden önce taze DB bağlamında hedef satırın yolu doğrulanır. DB yeni yolu gösteriyorsa işlem `Copied` sayılır; eski dosya kopya-güvenli strateji gereği korunur.
- 🟢 DB hedef yolu göstermiyorsa yeni kopya temizlenir. Doğrulama hatasında şifreli dosya silme servisi kalıcı cleanup günlüğüne başvuruyu ekler; referans kontrolü yapılamazsa dosya korunur ve worker tekrar dener. Web Release derlemesi **0 uyarı / 0 hata**; yönetici ekranında gerçek geçiş/commit hata enjeksiyonu yapılmadı.
- 🔴 A-10 kırmızı kalır: diğer dosya akışlarının tam envanteri, gerçek DB/storage worker kabulü, referans kontrolü-unlink yarışı ve büyük envanter performansı açık.

### 2026-10-06 — A-10 personel/araç arşiv dosyalarının atomik yazımı

- 🟢 `EvrakArsivService` şifreli personel/araç arşivini benzersiz geçici `.tmp.enc` dosyasına yazar ve tamamlanınca aynı klasörde son `.enc` adına taşır. İptal/yazım hatasında geçici dosya temizlenmeye çalışılır; tamamlanmamış içerik DB’ye döndürülen yol olarak yayımlanmaz.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Gerçek disk kesintisi/iptal çalıştırma testi yapılmadı.
- 🔴 A-10 kırmızı kalır: arşiv kopyası ile DB kaydı arasındaki süreç kesintisi yetim bırakabilir (yetim envanteriyle görünür), bütün dosya akışlarının runtime kabulü, TOCTOU ve büyük envanter performansı açık.

### 2026-10-06 — A-10 e-Fatura/Luca dosyalarında yeni yazımları şifreleme

- 🟢 GİB e-Fatura XML üretimi artık `wwwroot/efatura` yerine atomik şifreli depoya yazar; XML okuma şifreli yolu açar ve eski e-Fatura webroot yollarını sınırlandırılmış geriye dönük okuma olarak destekler. Luca’dan gelen yeni XML/PDF dosyaları da `wwwroot/belgeler/efatura` yerine şifreli depoya yazılır; kaydetme hatasında DB referansı denetlenerek cleanup kuyruğu kullanılır.
- 🟢 Fatura indirme yolu eski `/belgeler/efatura/...` ve `efatura/...` kayıtlarını güvenli `wwwroot` çözümlemesiyle okuyabilir. Yönetici dosya geçişi artık bu iki eski webroot yol ailesini de önizler, şifreli kopyayı doğrular, DB yolunu günceller ve yeni yol doğrulanınca referanssız eski açık dosya için dayanıklı temizlik isteği oluşturur. Web Release derlemesi **0 uyarı / 0 hata**; gerçek Luca/GİB bağlantısı veya eski müşteri dosyası geçişi çalıştırılmadı.
- 🔴 Var olan müşteri webroot dosyaları otomatik toplu taşınmadı; yönetici geçişi seçili firma ile çalıştırılmalı. ETTN/fatura ilişkilendirme ve canlı entegrasyon kabulü, hata enjeksiyonu, TOCTOU ve büyük veri performansı açık; A-10 🔴 kalır.

### 2026-10-07 — A-10 kalan yaşam döngüsü açıklarının kök düzeltmeleri

- 🟢 Eski düz e-Fatura/Luca çıktıları artık anonim statik dosya olarak sunulmaz; yeni GİB XML ve Luca XML/PDF üretimleri şifreli depodadır. Eski `wwwroot/efatura` ve `wwwroot/belgeler/efatura` yolları seçili firma migrasyonuna alındı; legacy `FileService` düz metin yazma ve doğrudan silme API'leri kaldırılıp salt-okunur yapıldı.
- 🟢 Dosya migrasyonu DB commit'inin gerçekleşmediğini taze bağlamla doğrularsa spekülatif şifreli kopyayı güvenli temizleme servisine verir ve eski-yeni yol bekleme isteğini tamamlar; böylece hiçbir zaman yazılmayacak yeni yolu bekleyen kalıcı kuyruk birikmez. Eski anahtar dosya kurtarması orijinali ezmeden geçici şifreli kopya üretip atomik değiştirir.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; `git diff --check` temiz. Bu turda test veya canlı müşteri geçişi çalıştırılmadı.
- 🔴 A-10 henüz kapanmaz: mevcut müşteri legacy dosyalarının firma bazlı geçişi/restore kabulü, aynı dosya yoluna eşzamanlı yeni DB referansı ile fiziksel silme arasındaki TOCTOU yarışı ve büyük DB referans taraması yükü saha kanıtı ister. Müşteri geçişi yapılmadan legacy açık dosyalar diskte korunur.

### 2026-10-07 — A-10 personel evrak sürümleme ve kaldırma tutarlılığı

- 🟢 `PersonelOzlukService.EvrakDosyaYukle` yeniden yüklemede önceki dosya yolu/ad/tip/boyut bilgisini sürüm tablosuna alıyor; yeni yol artık yanlışlıkla geçmiş sürüm diye kaydedilmiyor.
- 🟢 Hızlı evrak kaldırma DB’de aktif yolu ve metadata’yı gerçekten temizliyor; önceki dosya yolu aynı işlemde geçmiş sürüm olarak tutulduğundan geri alınabilir.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Gerçek personel verisiyle geri alma testi yapılmadı.
- 🔴 A-10 açık: gerçek müşteri geçiş/restore kabulü, TOCTOU ve büyük DB referans tarama yükü; kalan modül silme yolları ayrıca incelenmeli.

### 2026-10-07 — A-10 geri alınabilir soft-delete dosyalarını koruma

- 🟢 `AracService`, `TasimaTedarikciService` ve `EbysEvrakService` soft-delete sonrasında dosyayı artık fiziksel olarak silmiyor. Böylece DB’de geri alınabilir olarak kalan satırlar, gerekli şifreli içeriği de koruyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; bu turda test veya müşteri verisi restore işlemi çalıştırılmadı.
- 🔴 A-10 açık: gerçek müşteri restore/geçiş kabulü, genel TOCTOU, büyük DB tarama maliyeti ve kalan hard-delete temizleme sözleşmeleri.

### 2026-10-07 — A-10 dosya yolu referans kontrolü ve indeksleme

- 🟢 Dosya yol referansı tutan 12 sütuna model indeksleri ve migration eklendi. Silme kontrolü artık bütün DB yol envanterini her istekte belleğe almaz; normalize edilmiş mutlak storage aliasları doğrudan sorguya girer ve Windows harf/ayraç karşılaştırması DB tarafında kalır.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Test paketi bu turda çalıştırılmadı.
- 🔴 Hedef müşteri hacminde migration/index performansı, Windows case varyantı ve çoklu sunucu eşzamanlı silme kabulü, gerçek geçiş/restore verisi ve firma bağı olmayan destek legacy kayıtları açık.

### 2026-10-07 — A-10 personel dosya yolunu genel güncellemeden ayırma

- 🟢 `UpdatePersonelEvrakAsync` artık dosya yolu alanlarına dokunmuyor; eski form state'i dosya başvurusunu geri yazamaz. Kayıp dosya temizliği kimlik bazlı ayrı metoda taşındı.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**.
- 🔴 Müşteri verisinde restore/geçiş ve hedef boyut performans kabulü açık.

### A-10 — 2026-10-07 izole kabul güncellemesi

- 🟢 Release xUnit paketi **32/32** geçti. Soft-delete edilmiş dosyanın cleanup sonrasında geri alınabilir olduğu ve iki ayrı worker örneğinin aynı işi eşzamanlı claim etmediği tam model SQLite üzerinde doğrulandı.
- 🟡 Gerçek müşteri DB/depo geçiş-restore, hedef boyutta migration/performance ve iki ayrı sunucunun ortak ağ depolama kabulü yapılmadı. Bu maddeler için üretim verisi/bağlantısı mevcut değil; test sonucu müşteri kabulü diye sunulmuyor.
### 2026-10-07 — A-10 kuyruk sahipliği ve geçiş güvenliği

- 🟢 Worker tamamlaması lease/revizyon kontrolüne bağlandı; eski işlem yeniden kuyruğa alınan veya bekleme türü değişen isteği kaldıramaz. Doğrudan silme, DB başvurusunun kalkmasını bekleyen isteği korur.
- 🟢 Legacy kaynak silinmeden önce yeni şifreli dosya tekrar çözülüp SHA-256 ile karşılaştırılır. Eksik/uyuşmayan hedefte eski dosya ve kuyruk korunur. Referans sorgusunda boşluk, ters ayraç ve Türkçe harf koruması tamamlandı; tam yol listesi belleğe alınmaz.
- 🟢 Tam model SQLite ile gerçek geçiş servisi, soft-delete geri alma ve tekrar çalıştırma kabulü geçti. 100.000 sentetik satırda yerel sorgu ölçümleri 2,7 / 28,0 / 37,5 ms; Release paketi **34/34 başarılı**.
- 🔴 Bu kanıt genel referans ekleme–silme yarışını, tüm firma sahipliği açıklarını veya müşteri/çoklu sunucu kabulünü kapatmaz. Güncel kapsam ve ölçüm sınırları [görev envanterinin son ekinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) kayıtlıdır.
### 2026-10-07 — A-24/A-25 süreçler arası cache nesli

- 🟢 CacheService ortak depodaki nesil belirteciyle çalışıyor. Geçersizleştirme, başka servisin başlattığı eski factory sonucunun yeniden görünmesini engeller; süreç içi anahtar listesine bağımlılık kaldırıldı. Prefix temizliği tüm uygulama cache'ini geçersizleştirdiği için DB yükü artabilir.
- 🟢 İptal yutulmaz; okuma arızasında veri kaynağı kullanılır, invalidation arızası çağırana bildirilir. Dört yeni cache regresyonuyla Release paketi **38/38 başarılı**.
- 🔴 Gerçek Redis/çok süreçli yük, diğer araç yazımları ve backend kesintisinde kalıcı invalidation retry kapsamı açık. Docker daemon erişilemedi. Detaylar [görev envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md); A-24 🔴 ve A-25 🟡.

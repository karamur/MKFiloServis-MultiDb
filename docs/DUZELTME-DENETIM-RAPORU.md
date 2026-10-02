# MKFiloServis — Düzeltme Denetim Raporu

**Denetim tarihi:** 2026-10-02
**Denetlenen plan:** [SATISA-CIKARIM-DUZELTME-FAZ-PLANI.md](SATISA-CIKARIM-DUZELTME-FAZ-PLANI.md)
**Kapsam:** K-1…K-6, Y-1…Y-11, O-1…O-13 maddelerinin kaynak kod üzerinden doğrulanmış durumu
**Yöntem:** Statik kod okuması, git diff incelemesi, satır bazlı doğrulama

---

## 1. Yönetici Özeti

**Yapılan iş, planlama ve iki kaynak dosya düzeltmesinden oluşuyor. 39 maddenin yalnızca 1'i tamamen kapatılmış, 5'i kısmi ilerlemiş, 33'ünde değişiklik yok.**

| Durum | Adet | Maddeler |
|---|---:|---|
| ✅ Tamamen düzeltildi | **1** | K-5 |
| 🟡 Kısmen düzeltildi | **5** | K-2, K-4, Y-9, O-4, O-5 |
| ⚪ Değişiklik yok | **22** | K-1, K-3, K-6, Y-1…Y-8, Y-10, Y-11, O-1…O-3, O-6, O-7, O-10…O-12 |
| ⬜ Yalnızca yazıldı, uygulanmadı | **8** | O-8, O-9, O-13, D-1…D-8 |
| ⚠️ Yeni risk | **1** | K-5 içinde kurtarma açığı |

**Kritik gerçek:** **K-1 (lisans anahtarı) hiç dokunulmadı.** Bu, rapordaki en yüksek ticari riskli madde ve ürünün gelir modelini doğrudan tehdit ediyor.

**Faz durumu:** Plan 7 faz tanımlamıştı. **Hiçbir faz için kod teslimi ve kabul ölçütü kanıtı yok.** Faz 0 (envanter) ve Faz 1 (güvenlik) aynı anda başlatılmış görünüyor; halbuki plan "Faz 0 çıkmadan Faz 1'e geçilmez" kuralı içeriyor.

---

## 2. Yapılan İşlerin Listesi

### 2.1 Dokümantasyon (3 belge)
| Belge | Durum | İçerik |
|---|---|---|
| `SATISA-CIKARIM-ANALIZ-RAPORU.md` | ✅ Tamamlandı | 39 maddelik analiz (K/Y/O/D), 4 aşamalı kontrol listesi |
| `SATISA-CIKARIM-RAPORU-DEGERLENDIRME.md` | ✅ Tamamlandı | Kodla doğrulama ve düzeltme önerileri |
| `SATISA-CIKARIM-DUZELTME-FAZ-PLANI.md` | ✅ Tamamlandı | 7 faz, kabul ölçütleri, bağımlılıklar, karar gerektiren 5 nokta |

**Faz planının kalitesi:** İçerik güçlü. Özellikle şu noktalar olumlu:
- Her faz için **kabul ölçütleri** tanımlanmış (boş kalmamış)
- **Düzeltme bekleyen maddeler** bölümü (§135-140) analiz raporundaki 4 iddiayı dürüstçe çürütmüş:
  - K-2'deki "192 sayfa" sayısının hepsinin veri verdiğinin kanıtlanmadığı
  - K-3'te Blazor'un özel provider kullandığı için devrenin tamamının çökmediği
  - O-13/D-8'de `bin/obj` iddiasının doğrulanmadığı
  - Y-1/Y-6/O-4 için "log eklemek" değil **fail-closed** yaklaşım gerektiği

**Bu kendi kendini düzeltme yaklaşımı doğru ve farkındalık gösteriyor.** Ancak §137-140'taki düzeltmeler **analiz raporuna geri yansıtılmamış** — analiz raporunda hâlâ "192 sayfa", "kritik", "fail-open" gibi eski ifadeler duruyor. İki belge artık birbiriyle çelişiyor.

### 2.2 Kaynak Kod Değişiklikleri (2 dosya)
`git diff --stat`:
```
MKFiloServis.Web/Data/DbSeeder.cs           | 25 --------
MKFiloServis.Web/Services/KullaniciService.cs | 86 +----------------
2 files changed, 2 insertions(+), 109 deletions(-)
```

**Değişiklik 1 — `DbSeeder.cs` (K-5)**
Silinen: `admin` kullanıcısını `SifreHash = "admin123"` ile oluşturan blok (`:45-69`).
Geride kalan: `SeedAdminAsync` (`Program.cs:830`) yalnızca rol/yetki oluşturuyor.

**Değişiklik 2 — `KullaniciService.cs` (K-5)**
Silinen 86 satır:
- `admin` kullanıcısını `HashPassword(..., "admin123")` ile oluşturan blok
- **`test` kullanıcısını `HashPassword(..., "test123")` ile oluşturan blok** ← bu bir **ek güvenlik kazancı**, raporda yoktu
- Devre dışı bırakılmış kullanıcıları otomatik yeniden etkinleştiren bloklar

Eklenen:
```csharp
// Üretim başlangıcında bilinen parolalı yönetici/test hesabı oluşturulmaz
// ve devre dışı bırakılmış kullanıcılar yeniden etkinleştirilmez.
```

**Bu değişikmenin değerlendirmesi — olumlu:**
- K-5'in özü (bilinen parolalı hesap üretimi) gerçekten kapatıldı
- `test` hesabının kaldırılması, değerlendirme raporunda ek olarak işaretlenen bir riski kapattı
- "Devre dışı bırakılmış kullanıcıyı otomatik etkinleştirme" kaldırılması doğru — aksi halde bir yönetici güvenlik önlemi olarak hesabı pasifleştirdiğinde uygulama onu geri açardı
- Kullanıcı girdiği şifreye artık dokunulmuyor (parola korunuyor)

**K-5'in devamı olarak doğrulanan mevcut durum — `SetupWizard.razor`:**
- `Program.cs:544-552` JWT sırrı doğrulaması eklendi (boş/`REPLACE_`/32 karakter altı → başlatma reddi) — **K-4'teki iyileşmenin kaynağı**
- `Components/Pages/Setup/SetupWizard.razor:1-3` → `@page "/setup"`, `[AllowAnonymous]` — ilk kurulum sihirbazı mevcut
- `:476-484` → admin kullanıcısı operatörün girdiği şifreyle `PasswordHasher.HashPassword` ile oluşturuluyor
- `:408-411` → herhangi bir `Firma` varsa `/login`'e yönlendiriyor

**Yani: sıfırdan kurulumda bilinen parola artık oluşturulmuyor. K-5'in amacı gerçekleşti.**

---

## 3. Madde Bazında Denetim Tablosu

### 3.1 Kritik maddeler

| ID | Madde | Önceki durum | Şimdiki durum | Kanıt |
|---|---|---|---|---|
| **K-1** | Lisans anahtarı kodda gömülü | Kritik | ⚪ **DEĞİŞMEDİ** | `MainForm.cs:15` ve `LicenseService.cs:35` hâlâ aynı düz metin anahtarı. RSA/ECDSA yok. Fail-open duruyor (`:188-194`, `:209-213`). `FixedTimeEquals` yok. Grace period yok. `LisansAesKey` (`:920`) hâlâ gömülü. |
| **K-2** | 192 sayfada `[Authorize]` yok | Kritik | 🟡 **KISMEN** | 63 sayfa `[Authorize]`'lı. **`FallbackPolicy` hâlâ yok** — `Program.cs:210` yalnızca `AddAuthorizationCore()`. ~180 yönlendirilebilir sayfa açık. |
| **K-3** | `DefaultScheme="Cookies"`, `AddCookie()` yok | Kritik | ⚪ **DEĞİŞMEDİ** | `Program.cs:557` aynen duruyor. Çözümde hâlâ sıfır `AddCookie` çağrısı. |
| **K-4** | JWT sırrı repoda | Kritik | 🟡 **KISMEN** | `appsettings.json:8` hâlâ gerçek değer (57 karakter, doğrulamayı geçiyor). `Program.cs:545-552` doğrulama eklendi ama mevcut anahtarı geçiyor. `appsettings.json:155` ikinci anahtar boşaltıldı. |
| **K-5** | Varsayılan admin parolası | Kritik | ✅ **DÜZELTİLDİ** | `DbSeeder.cs` ve `KullaniciService.cs` seed blokları silindi. Kurulum sihirbazı güvenli. `admin123`/`test123` kaynakta kalmadı (yalnız test projesinde env fallback olarak var). |
| **K-6** | Parola politikası kuralsız | Kritik | ⚪ **DEĞİŞMEDİ** | `Program.cs:199-209`: min 6, rakam/büyük/küçük/özel karakter şartı yok, kilit 5. `KullaniciPasswordHasher.cs:25-31` legacy SHA256 hâlâ erişilebilir. SetupWizard `:436` de "en az 6 karakter" diyor. |

### 3.2 Yüksek öncelikli maddeler

| ID | Madde | Durum | Kanıt |
|---|---|---|---|
| Y-1 | Boş `catch` blokları (40 adet) | ⚪ Değişiklik yok | Playwright 19, Selenium 17, Http 3, KolayMuhasebe 3, FaturaSablon 2, Webhook 2 — hiçbiri loglanmadı |
| Y-2 | `NotImplementedException` / TODO | ⚪ Değişiklik yok | `ProformaFaturaService.cs:536`, `LucaPortalService.cs:899` duruyor |
| Y-3 | Luca parolası düz metin | ⚪ Değişiklik yok | `LucaPortalService.cs:211` TODO yorumu duruyor |
| Y-4 | `AtayanKullaniciId = 1` | ⚪ Değişiklik yok | `EbysEvrakService.cs:326` |
| Y-5 | Swagger tüm ortamlarda | ⚪ Değişiklik yok | `Program.cs:1197-1213`, `IsDevelopment()` koruması yok |
| Y-6 | EF uyarıları bastırılmış | ⚪ Değişiklik yok | `Program.cs:171-176`, 3 uyarı hâlâ bastırılıyor |
| Y-7 | CI test iş akışı kırık | ⚪ Değişiklik yok | `MKFiloServis.Tests` yok; `tests.yml:63,67,74` ölü yol; hiçbir csproj'da test framework'ü yok |
| Y-8 | Sync-over-async | ⚪ Değişiklik yok | `ArchiveMigrationService.cs:172,284`, `LisansService.cs:206`, `PlaywrightScraperService.cs:59` |
| Y-9 | Dış servis TLS/timeout/retry | 🟡 **KISMEN** | Timeout 2/4 yere eklendi (`LucaPortalService.cs:748`, `HttpScraperService.cs:31`). Retry yok, TLS yapılandırması yok, Polly kullanılmıyor |
| Y-10 | Legacy timestamp | ⚪ Değişiklik yok | `Program.cs:46` |
| Y-11 | NuGet advisory bastırılmış | ⚪ Değişiklik yok | `Web.csproj:90`, `DataSync.csproj:28` |

### 3.3 Orta öncelikli maddeler

| ID | Madde | Durum | Kanıt |
|---|---|---|---|
| O-1 | SQL Server/MySQL migration yolu yok | ⚪ Değişiklik yok | `DatabaseRuntimeResolver.cs:43` PostgreSQL'e sabit |
| O-2 | `firmaId = 1` varsayılanı | ⚪ Değişiklik yok | `ApplicationDbContext.cs:3316` |
| O-3 | Denetim kaydı eksik | ⚪ Değişiklik yok | `:3343-3348`, `:3415-3419` |
| O-4 | Başlangıçta geniş `catch` | 🟡 **KISMEN** | `Program.cs:680-697` artık `LogWarning` atıyor ve yorum eklendi, **ama kritik/opsiyonel sınıflandırması sadece yorum — kod yok** |
| O-5 | Lisans sessizce varsayılan firmaya bağlanıyor | 🟡 **KISMEN** | `LicenseService.cs:641-684` artık `LogWarning` atıyor ve demo yolu eklendi, **ama hâlâ sessiz atıyor — hata fırlatmıyor** |
| O-6 | Yedek geri yükleme TODO | ⚪ Değişiklik yok | `DatabaseBackupService.cs:387-389` |
| O-7 | DataSync şema/FK kontrolü yok | ⚪ Değişiklik yok | `PostgresToSqliteExporter.cs:31-38`, `SqliteToPostgresImporter.cs:64-96` |
| O-10 | README yanlış | ⚪ Değişiklik yok | `README.md:68-69` hâlâ var olmayan projeleri belgeliyor |
| O-11 | Docker zayıf varsayılan sırlar | ⚪ Değişiklik yok | `docker-compose.yml:33,35` |
| O-12 | CliRunner parolası | ⚪ Değişiklik yok | `CliRunner.cs:101` |
| O-13 | Git'te izlenen bin/obj/log | ✅ **DÜZELTİLMİŞ (zaten doğruymuş)** | `git ls-files`: bin/obj izlenmiyor, log dosyaları izlenmiyor. **Yalnız `test_all.txt` izleniyor** |

**O-13 düzeltilmedi, düzeltilmesi de gerekmiyordu** — analiz raporundaki iddia yanlıştı ve faz planı bunu doğru tespit etmişti.

---

## 4. Tespit Edilen Yeni Risk

### R-1. Kurulum sihirbazı kurtarma açığı oluşturdu
**Konum:** `SetupWizard.razor:408-413`
```csharp
if (await ctx.Firmalar.IgnoreQueryFilters().AnyAsync())
{
    Navigation.NavigateTo("/login", replace: true);
}
...
catch { /* DB bağlantı hatası → setup devam et */ }
```

**Sorun:** K-5 düzeltmesi seed'den admin oluşturmayı kaldırdı. Şimdi **admin oluşturmanın tek yolu** bu sihirbaz. Ama koruma `Firmalar` tablosuna bakıyor, `Kullanicilar`'a değil. `catch` bloğu da DB hatasını yutuyor.

**Somut risk senaryosu:**
1. Kurulum yarıda kesilir → `Firmalar` tablosunda kayıt var, `Kullanicilar` boş
2. Kullanıcı `/setup` adresine gider → `/login`'e yönlendirilir
3. Giriş yapamaz (hesap yok) → `/setup` da açılmıyor
4. **Sistem kilitli. Manuel veritabanı müdahalesi gerekir.**

**Ayrıca:** `Firma` kaydı olan ama kullanıcısız bir veritabanında kurulum sihirbazı hiç açılmaz — bu, seed'in kaldırılmasından sonra **yeni** bir kilitlenme senaryosudur.

**Düzeltme önerisi:** Koruma `Firmalar.Any() && Kullanicilar.Any()` üzerinden kurulmalı. Yalnız `Firmalar` varsa sihirbaz "hesap oluştur" moduna geçmeli. DB hatası `catch` yerine hata ekranı göstermeli — sessizce devam etmek, sihirbazın veri oluşturma adımlarında daha büyük veri tutarsızlığı üretir.

### R-2. İki belge birbiriyle çelişiyor
`SATISA-CIKARIM-ANALIZ-RAPORU.md` §3'te hâlâ "192 sayfa", "`DefaultScheme` kritik", "hash dosyası yoksa fail-open" gibi ifadelerle suçlamalar var. Faz planı §135-140 bu iddiaların bir kısmını geri çekiyor.

**Risk:** Geliştirici hangi belgeyi esas alacağını bilemez; faz planı "plan niteliğindedir, kod değişikliği yapılmadı" derken analiz raporunun kesin dili "satışı engeller" diyor.

**Düzeltme:** Analiz raporuna üst bölümde bir düzeltme notu, veya iki belgeyi tek dosyada birleştirmek.

---

## 5. Faz Bazında Durum

| Faz | Hedef | Beklenen süre | Gerçek durum |
|---|---|---:|---|
| **Faz 0** | Envanter, geri dönüş planı | 1–3 gün | 🔄 Kısmen — belgeler var, envanter/sorumlu/yedek kanıtı yok |
| **Faz 1** | K-1…K-6 güvenlik | 1–3 hafta | ❌ **Başlanmadı** — K-5 dışında hiçbir maddeye dokunulmadı |
| **Faz 2** | Firma izolasyonu, denetim | 1–2 hafta | ⬜ Başlanmadı |
| **Faz 3** | Şema, kurtarma | 1–3 hafta | ⬜ Başlanmadı |
| **Faz 4** | Kod kalitesi, CI | 1–3 hafta | ⬜ Başlanmadı |
| **Faz 5** | Paketleme, doküman | 3–10 gün | ⬜ Başlanmadı |
| **Faz 6** | Yayın kapısı | 1–2 hafta | ⬜ Başlanmadı |

**Planın kendi kuralı ihlal edildi:** Faz planı §13 "Her fazın kod teslimi ve doğrulama sonucu kaydedilmeden sonraki faza geçilmez" diyor. Oysa Faz 1'in 6 maddesinden 5'i bitmeden K-5'e başlanmış.

**Bu bir sorun değil, tek başına doğal bir gelişme** — K-5 en kolay ve en yüksek getirili maddiydi, iyi seçilmiş. Ancak **Faz 0 kanıtı olmadan Faz 1'e başlanması**, Faz 0'ın amacını (mevcut müşteri verisini ve lisansları envanterlemek) atlamak demek. K-1'e geçildiğinde **hangi müşterilerde lisans üretilmiş, hangi anahtarlar dağıtılmış** bilgisi şart — çünkü anahtar döndürme bunları etkileyecek.

---

## 6. Değerlendirme

### Doğru yapılanlar
1. **K-5'in seçimi isabetli.** Bilinen parolalı hesap üretimi, güvenlik açısından en görünür ve en ucuz kapatılan risk. Kaldırılan `test`/`test123` hesabı ek bir kazanç sağladı.
2. **Devre dışı bırakılan kullanıcıların otomatik etkinleştirilmesinin kaldırılması** — bu, seed'de fark edilmemiş bir güvenlik açığıydı; kapatılması doğru.
3. **Kurulum sihirbazı mevcut ve doğru tasarlanmış** — operatör şifresi belirliyor, `PasswordHasher` kullanıyor, `[AllowAnonymous]` doğru.
4. **JWT doğrulamasının eklenmesi** (`Program.cs:545-552`) — boş/placeholder sırlarla başlatmayı engelliyor.
5. **Faz planının "düzeltme bekleyen maddeler" bölümü** — analizdeki abartılı ifadeleri kendi kendine düzeltmiş. Bu, kendi işini eleştirme kapasitesi göstergesi.
6. **Kabul ölçütlerinin tanımlanması** — her fazın neyle biteceği belli.

### Eksik kalanlar
1. **K-1 hiç dokunulmadı.** Lis anahtarının kodda olması, tek başına ürünü satılamaz hale getiren bir sorundur — müşteri binary'den anahtarı okuyup kendi lisansını üretir. Diğer tüm maddeler bu kadar önemli olmasa da bu **tek başına Go/No-Go nedenidir.**
2. **K-3 hâlâ bozuk.** `DefaultScheme="Cookies"` olup handler'ı olmayan bir yapı, hangi akışın patlayacağı belli değil — bu belirsizlik de risktir.
3. **K-4 yarım.** Doğrulama eklendi ama repodaki gerçek anahtar hâlâ dosyada ve doğrulamayı geçiyor. Anahtar hâlâ kamuya açık.
4. **K-6 hiç dokunulmadı.** Kurulum sihirbazı hâlâ "en az 6 karakter" kabul ediyor — K-5 kapatıldı ama parola kalitesi kapısı açık.
5. **Faz 0 atlandı.** K-1'e başlamadan önce lisans envanteri gerekliydi.
6. **8 düşük öncelikli madde hiç ele alınmadı** — kabul edilebilir, ancak hiç dokunulmadığı belirtilmeli.

---

## 7. Önerilen Sonraki Adımlar

### Hemen (bu hafta)
1. **Faz 0'u tamamla:** Kaç müşteride lisans üretildi, hangi anahtarlar dağıtıldı, hangi sürümler kullanılıyor? Bu liste K-1'in planlanabilmesi için ön koşul.
2. **K-1'e başla:** RSA/ECDSA imzaya geçiş. LisansDesktop'ta özel anahtar, Web'de yalnızca public key. Eski lisanslar için geçiş süresi tanımla.
3. **K-3'ü kararla kapat:** Ya `AddCookie()` ekle ya da `DefaultScheme`'i kaldır. İki seçenekten biri 5 dakikalık iş — belirsiz bırakılması risk.
4. **R-1'i düzelt:** SetupWizard korumasını `Kullanicilar.AnyAsync()` ile tamamla.

### Kısa vade (2 hafta)
5. **K-2:** `FallbackPolicy` ekle — tek satır, en büyük güvenlik kazancı.
6. **K-4:** JWT anahtarını `appsettings.json`'dan çıkar, ortam değişkenine taşı, döndür.
7. **K-6:** Parola politikasını yükselt (min 8-12, karmaşıklık şartları), legacy SHA256'yı kapat.
8. **K-5 artık risk değil, tamamlandı olarak işaretle.**

### Orta vade
9. Y-7 (test projesi) — CI kırık olduğu için hiçbir kalite güvencesi yok.
10. Y-1 (muhasebe/fatura boş catch'leri) — veri bütünlüğü riski.

### Belge
11. Analiz raporuna düzeltme notu ekle veya iki belgeyi birleştir (R-2).

---

## 8. Özet Tablo

| Ölçüt | Değer |
|---|---:|
| Toplam madde | 39 |
| Tamamen düzeltilen | **1** (%3) |
| Kısmen ilerleyen | 5 (%13) |
| Değişiklik olmayan | 22 (%56) |
| Yalnızca planlanan | 8 (%21) |
| Yeni risk | 1 |
| Değiştirilen kaynak dosya | 2 |
| Eklenen satır / silinen satır | +2 / −109 |
| Tamamlanan faz | **0 / 7** |
| K-1 (en yüksek riskli madde) | **Dokunulmadı** |

---

*Bu denetim kaynak kodun okunmasıyla yapılmıştır. K-5'in çalışma zamanı davranışı (kurulum sihirbazının sıfırdan kurulumda admin oluşturması) statik incelemeyle doğrulandı ancak canlı ortamda test edilmemiştir. Faz 0 envanteri ve Faz 6 uçtan uca doğrulama adımları statik analizle doğrulanamaz.*
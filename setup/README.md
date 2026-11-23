# MKFiloServis-MultiDb — Setup / Kurulum Paketleri

Kurulum paketleri **WiX Toolset v6 + Burn** ile üretilir (1.0.48 ve sonrası).
Inno Setup betikleri (`*.iss`) yalnızca geçiş referansı olarak tutulur; yeni paket
üretiminde kullanılmaz.

## Gereksinimler

- **.NET 10 SDK** — https://dotnet.microsoft.com/download/dotnet/10.0
- **WiX Toolset 6.0.2** (CLI) — `dotnet tool install --global wix --version 6.0.2`
  - WiX 7 ayrı bir lisans onayı (OSMF EULA) istediği için 6.x kullanılır.
  - Uzantılar betik tarafından otomatik eklenir: `WixToolset.UI.wixext`,
    `WixToolset.Util.wixext`, `WixToolset.BootstrapperApplications.wixext` (6.0.2).
- **Windows PowerShell 5.1+** (paket üretimi için yönetici gerekmez)

## Hizli Baslangic

```powershell
cd setup
.\build-wix.ps1 -Version 1.0.48
```

Seçenekler: `-SkipPublish` (mevcut `payload/` kullanılır), `-LisansOnly`
(yalnız lisans aracı MSI'ı), `-SkipLisans`.

## Uretilen Paketler (`setup/output/v<sürüm>/`)

| Dosya | Aciklama |
|-------|----------|
| `MKFiloServisKurulum-<sürüm>.exe` | **Burn bundle** — yeni sunucu kurulumu: .NET 10 Hosting Bundle (IIS/ANCM) + Web MSI. Mevcut PostgreSQL'e bağlanır |
| `MKFiloServisGuncelle-<sürüm>.exe` | **Burn bundle** — yalnızca Web MSI; mevcut kurulum şarttır, `dbsettings.json` korunur |
| `MKFiloServis.Web-<sürüm>.msi` | Bundle'ların içindeki Web + DataSync MSI'ı. **Tek başına kurulursa Hosting Bundle (ANCM) kurulmaz**; IIS'te `500.19 / 0x8007000d` alınırsa Kurulum bundle'ı çalıştırın |
| `MKLisansArac-<sürüm>.msi` | Lisans üretim aracı; **kullanıcı bazlı** kurulum (`%LocalAppData%\Programs\MKLisans`), yönetici gerekmez |
| `MKFiloServisMasaustu-<sürüm>.exe` | Windows masaüstü istemcisi (ayrı build, `build-client.ps1`) |
| `MKFiloServisAndroid-<sürüm>.apk` | Android istemcisi (ayrı build) |

## Klasor Yapisi

```
setup/
  build-wix.ps1      — Ana paket üretim betiği (publish + WiX + Burn)
  build-client.ps1   — Windows masaustu ve Android istemcileri
  wix/
    MKFiloServis.Web.Setup/     — Web MSI (Package.wxs, WixUI_MKFilo.wxs, tr-TR .wxl)
    MKFiloServis.Lisans.Setup/  — Lisans aracı MSI (kullanıcı bazlı)
    MKFiloServis.Bundle.Shared/ — Kurulum/Güncelle bundle'ının ortak kaynağı (IncludeHosting=1/0)
    scripts/install-web.ps1     — MSI custom action'larının çalıştırdığı IIS/dbsettings betiği
    obj/                        — Ara dosyalar (.gitignore'da)
  scripts/           — IIS yapilandirma PowerShell script'leri (configure-iis-startup.ps1 pakete dahil edilir)
  build.ps1, *.iss   — Eski Inno Setup akışı (referans; yeni üretimde kullanılmaz)
  payload/           — Publish ciktilari (gecici, .gitignore'da)
  redist/            — dotnet-hosting-10.0-win.exe (.gitignore'da; yoksa betik indirir)
  output/            — Uretilen MSI/EXE'ler (.gitignore'da)
```

## Kurulum akışı (Kurulum bundle)

1. Ön koşullar: 64-bit Windows, yönetici hakkı.
2. Web MSI: Hoş geldiniz → **PostgreSQL bağlantı** sayfası (sunucu, port, veritabanı,
   kullanıcı, parola) → özet → dosyalar `C:\MKFiloServis\app` ve `C:\MKFiloServis\tools`
   altına kopyalanır → `install-web.ps1 -Mode Install` çalışır: IIS yoksa `dism` ile
   etkinleştirir, `MKFiloServis` uygulama havuzu ve 5050 portlu siteyi oluşturur,
   `dbsettings.json` dosyasını yazar ve ACL'sini SYSTEM/Administrators/uygulama havuzu
   ile sınırlar, `configure-iis-startup.ps1` ile `Jwt__Secret` ve depolama klasörlerini hazırlar.
3. Hosting Bundle: IIS + ANCM + ASP.NET Core 10 çalışma zamanı yoksa kurulur; çalışma
   zamanı IIS'ten önce kurulmuşsa ANCM'nin kaydolması için `/repair` ile çalıştırılır.
4. Son sayfadaki onay kutusu `http://localhost:5050/` adresini tarayıcıda açar.

Kurulum depolama kökünde erişimi sınırlandırılmış `Kurtarma` dizinini oluşturur
(varsayılan `C:\MKFiloServis_yedekleme\Kurtarma`). Kurulum sonunda gösterilen
uyarıya göre **Ayarlar → Yedekleme ve Kurtarma** ekranından ilk yedeği alın ve
dizini ayrı, şifreli bir ortamda saklayın. Kurulum kendi başına yedek almaz.

Kurulum **PostgreSQL kurmaz, veritabanı oluşturmaz ve yedek yüklemez**; yalnızca
önceden hazırlanmış PostgreSQL veritabanına bağlanır. Boş bir veritabanı verilirse
şema ilk açılışta uygulama tarafından (EF baseline + migration'lar) üretilir.
Sunucuya TCP erişimi
kurulum sırasında denenir, erişilemezse bağlantı bilgileri yine de yazılır ve günlüğe
uyarı düşülür (`C:\MKFiloServis\app\logs\install-web.log`). Parola komut satırıyla
aktarıldığından `"` içeremez ve `\` ile bitemez.

MSI custom action'ları `install-web.ps1` betiğini çalıştırır; yalnızca site/havuzu
durduran `StopWeb` eylemi dosyadan bağımsız olarak doğrudan `appcmd` çağırır (güncellemede
eski ürünün dosyaları silinmiş, yenileri henüz kopyalanmamış olur). Betikte `-Db` gibi
PowerShell ortak parametre takma adlarıyla (`-Debug` → `db`) çakışan parametre adı
kullanılamaz; aksi halde betik hiçbir modda yüklenmez ve MSI 1603 ile geri alınır.

## Güncelleme akışı (Güncelle bundle)

- Mevcut bir kurulum (WiX MSI ya da eski Inno kurulumu) ve ANCM + ASP.NET Core 10
  çalışma zamanı bulunmazsa bundle çalışmayı reddeder.
- Site/uygulama havuzu durdurulur, dosyalar yenilenir, `install-web.ps1 -Mode Upgrade`
  IIS ayarlarını yeniden uygular, site başlatılır.
- `dbsettings.json`, `portalsettings.json`, `backup_settings.json` MSI bileşeni
  değildir; güncelleme ve kaldırmada korunur. Mevcut `dbsettings.json` varsa
  PostgreSQL sayfası gösterilmez.
- Güncelleme IIS havuzundaki mevcut `CRMFILO_STORAGE_ROOT` değerini korur ve
  arşiv dosyalarını veya anahtarlarını yeniden yazmaz. Yapılandırılmış dizin
  erişilemiyorsa güncelleme başka bir depoya geçmek yerine hata verir.
- Aynı sürümün üzerine yeniden kurulum (onarım amaçlı) desteklenir; sürüm düşürme
  engellenir.

## Kaldırma

IIS sitesi ve uygulama havuzu silinir; `C:\MKFiloServis\app` içindeki ayar/günlük
dosyaları, `C:\MKFiloServis_yedekleme` ve PostgreSQL veritabanı yerinde bırakılır.

## IIS kurulumunda ilk giriş

Lisansın etkin olması bir kullanıcı hesabı oluşturmaz. Kullanıcı tablosu boş olan yeni
bir veritabanında uygulama ilk açılışta Admin rolünde varsayılan hesabı oluşturur:
kullanıcı adı `admin`, parola `Admin123`. **İlk girişten sonra parolayı
Ayarlar → Profil → Şifre Değiştir bölümünden değiştirin**; değiştirilene kadar uygulamaya
erişebilen herkes bu bilinen parolayla yönetici olarak girebilir. Yeni parola en az
8 karakter olmalı ve büyük harf, küçük harf, rakam ve özel karakter içermelidir.
`test` hesabı yalnızca yerel Development SQLite ortamında otomatik oluşturulur.
Mevcut veritabanını bağladıysanız mevcut hesabınızla giriş yapın; silinmiş hesaplar
dahil herhangi bir kullanıcı kaydı varsa varsayılan hesap oluşturulmaz. Eski hesaplar
bir SQLite dosyasındaysa Veri Aktarım aracını kullanarak hesaplar dahil verileri
planlanan PostgreSQL hedefine taşıyın.

Varsayılan hesap yerine kendi ilk yöneticinizi istiyorsanız, uygulama ilk kez açılmadan
önce yönetici olarak IIS
yapılandırma düzenleyicisinden `system.applicationHost/applicationPools` altında
`MKFiloServis` havuzunun `environmentVariables` koleksiyonuna `INITIAL_ADMIN_USERNAME`
ve `INITIAL_ADMIN_PASSWORD` ekleyin. Kullanıcı adı en fazla 50 karakter; parola en az
8 karakter olmalı ve büyük harf, küçük harf, rakam ve özel karakter içermelidir.
Havuzu yeniden başlatıp uygulamayı açın. İlk giriş başarılı olduktan sonra iki
değişkeni kaldırın ve havuzu yeniden başlatın. Başarısız olursa sunucu başlangıç
günlüğünde `SeedAdmin` çıktısını kontrol edin.

## Veri Aktarım (DataSync) ile SQLite → PostgreSQL

`C:\MKFiloServis\tools` altındaki DataSync aracı SQLite verisini PostgreSQL'e kopyalar.
Hedefte tablo yoksa aracı **kendisi üretir**: `MKFiloServis.Web.exe --ensure-schema`
komutunu (bağlantı dizesi `MKFILO_SCHEMA_CONNECTION` ortam değişkeniyle) alt süreç
olarak çalıştırır; bu komut Web uygulamasının açılışta kullandığı aynı yolu izler
(boş veritabanında EF baseline + migration geçmişi, aksi halde bekleyen migration'lar,
`FisNoCounters` şeması). Web uygulaması klasörü otomatik bulunur (kayıt defterindeki
`InstallPath`, `..\app`, `C:\MKFiloServis\app`); bulunamazsa arayüz klasör sorar, komut
satırında `import --web-app <klasör>` verilebilir. Şema üretiminden sonra hâlâ karşılığı
olmayan tablolar (EF modelinde bulunmayan eski SQLite tabloları) kaynakta boşsa atlanır,
satır içeriyorsa aktarım veri kaybını önlemek için durdurulur.
Mevcut hedefte yalnız `AracMasrafKalemleri` ve/veya `RolYetkiler` eksikse şema
hazırlama yeniden çalıştırılmaz; bu SQLite kalıntıları ancak boşsa atlanır.
Aktarımdan önce hedefi yedekleyin ve IIS uygulama havuzunu ve hedefe erişen diğer
işlemleri durdurun. Şema hazırlama veya tablo kilidi zaman aşımına uğrarsa önce
hedef şemayı ve açık bağlantıları kontrol edin, sonra aktarımı yeniden başlatın.

## Veritabanı ve yerel ayarlar

Ana kurulumun hedefi `C:\MKFiloServis`; IIS uygulama kökü
`C:\MKFiloServis\app` ve stdout log klasörü `C:\MKFiloServis\app\logs` olur.
Tam kurulum `C:\MKFiloServis_yedekleme` altındaki `uploads`, `Arsiv`, `Depo`,
`data`, `database`, `keys` ve `logs` klasörlerini oluşturur. IIS sitesinin fiziksel
yolu `C:\MKFiloServis\app`, portu 5050 olarak ayarlanır. `MKFiloServis` uygulama
havuzuna `CRMFILO_STORAGE_ROOT=C:\MKFiloServis_yedekleme` ortam değişkeni ve
depolama klasörüne yazma izni verilir. Güncelleme paketi aynı yol ve izinleri
yeniden uygular. Mevcut şifreli dosyalar ve anahtarlar kurulum paketine eklenmez;
eski makineden taşınırken birlikte korunmalıdır.

Uygulama seçilen sağlayıcıda ortak veritabanı kullanır; firma kapsamı kayıtların
`FirmaId` bağıyla korunur. Her firma için otomatik ayrı veritabanı oluşturulmaz.

Kurulum sihirbazı PostgreSQL bağlantısını alıp `dbsettings.json` dosyasını hedefte
oluşturur (Provider=PostgreSQL). Dosyanın ACL'si SYSTEM/Administrators ve uygulama
havuzu ile sınırlandırılır. SQL Server/MSSQL desteklenmez; SQLite yalnızca
`dbsettings.json` elle düzenlenerek kullanılabilir.
Güncelleme paketi mevcut kurulumun üzerine yazar ve `dbsettings.json` ayarını korur.

`dbsettings.json`, `portalsettings.json` ve `backup_settings.json` kurulumun
çalışma zamanı dosyalarıdır. Kaynak depoya ve publish/kurulum paketine alınmazlar;
kurulum sihirbazı bağlantı ayarını yazar, diğer iki ayar uygulama varsayılanlarından
başlar ve yönetim ekranından kaydedilebilir. Git ile eski bir çalışma kopyasını
güncelleyen operatör, çekmeden önce yerel ayarlarının yedeğini almalıdır.
`-SkipPublish` ile eski payload kullanılırsa paket betiği bu dosyaları bulduğunda
işlemi durdurur.
Ortama özel `appsettings.*.json` dosyaları da Web publish/kurulum paketine alınmaz;
eski payload'da bulunurlarsa `setup/build-wix.ps1` paketlemeyi reddeder. Pakette yalnız
sır içermeyen temel `appsettings.json` yer alır.

`Jwt__Secret` en az 32 baytlık rastgele üretim sırrıdır. Tam kurulum sihirbazı bunu
hedef bilgisayarda üretip yalnız `MKFiloServis` IIS uygulama havuzunun ortam
değişkenine yazar; mevcut geçerli değeri korur. Sır EXE paketine, `appsettings`
dosyasına veya komut satırına konmaz. Kurulum, IIS stdout log klasörünü de
uygulama havuzuna yazılabilir şekilde hazırlar. Daha önce kurulmuş bir bilgisayarı
düzeltmek için `setup/scripts/configure-iis-startup.ps1` betiğini hedefe kopyalayıp
yönetici PowerShell'de `powershell -NoProfile -ExecutionPolicy Bypass -File .\configure-iis-startup.ps1 -AppPath 'C:\MKFiloServis\app' -Port 5050 -RestartAppPool`
komutuyla çalıştırın. Betik mevcut IIS sitesinin HTTP bağını 5050 olarak ayarlar.
Güncelleme paketi de bu ayarı uygular. Dağıtım klasörüne ayrıca kopyalanır. IIS yapılandırmasına
erişimi yalnız yetkili yöneticilerle sınırlandırın.

Güncel PC2 geçişi için önce sağlayıcı/bağlantı ayarını oluşturun, onaylı DB yedeğini
izole hedefe geri yükleyin ve `Jwt__Secret` değerini sağlayın. Boş PostgreSQL ve
SQLite başlangıç baseline'ı izole ortamda geçti; eski müşteri şeması yükseltmesi,
parity ve rollback kabulü A-18/A-21'de açıktır.

## Masaustu ve Android istemcileri

Bu istemciler web uygulamasini WebView icinde acar. Web sunucusu ayrica kurulu ve
agdan erisilebilir olmalidir. Windows istemcisinde varsayilan adres
`http://localhost:5050` olarak gelir. Android uygulamasinda sunucunun LAN IP'si
veya HTTPS adresi girilir; telefondaki `localhost` bilgisayari gostermez.
Girilen adres cihazda saklanir. APK gelistirme anahtariyla imzalanir; herkese
dagitilacak kalici surum icin kuruma ait Android imzalama anahtari gereklidir.


## Şirket içi lisans programı

Lisans üretimi ve anahtar yedek/geri yükleme işlemleri LisansDesktop programından yürütülür.
Programın **İmza Anahtarı** menüsünde durum kontrolü, parola korumalı `.mkkey` yedeği
ve geri yükleme bulunur. Harici anahtar yönetim betiği veya ortam değişkeni gerekmez.
İmzalama anahtarı ve LisansDesktop müşteri kurulumuna eklenmez.
Ayrıntılar: [Lisans geçişi](../docs/LISANS-IMZA-GECIS.md).

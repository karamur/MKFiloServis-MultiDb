# MKFiloServis-MultiDb — Setup / Kurulum Paketleri

## Gereksinimler

- **.NET 10 SDK** — https://dotnet.microsoft.com/download/dotnet/10.0
- **Inno Setup 6** — `winget install JRSoftware.InnoSetup`
- **PowerShell 7+** (Admin olarak calistirilmali)

## Hizli Baslangic

```cmd
cd setup
make.cmd <surum>
```

veya PowerShell ile:

```powershell
.\build.ps1 -Version <surum>
```

## Uretilen Paketler

| Dosya | Aciklama |
|-------|----------|
| `MKFiloServisKurulum-<sürüm>.exe` | Yeni müşteri için IIS sunucu kurulumu (Web + DataSync); lisans üretim aracı içermez |
| `MKFiloServisGuncelle-<sürüm>.exe` | Güncelleme paketi |
| `MKFiloServisKurulumMusteri-<sürüm>.exe` | Eski doğrudan çalıştırma varyantı; ACL koruması tamamlanana kadar yeni satışta üretilmez/dağıtılmaz |
| `MKLisansArac-<sürüm>.exe` | Yalnızca dahili lisans üretim akışında oluşturulur |
| `MKFiloServisMasaustu-<sürüm>.exe` | Windows masaüstü istemcisi (ayrı build) |
| `MKFiloServisAndroid-<sürüm>.apk` | Android istemcisi (ayrı build) |

## Parametreler

| Parametre | Aciklama |
|-----------|----------|
| `-Version` | Dağıtım sürümü (belirtilmesi önerilir; script varsayılanı 1.0.37) |
| `-SkipPublish` | Publish atla, sadece Inno Setup calistir |
| `-LisansOnly` | Sadece lisans araci EXE'si uret |
| `-IncludeInternalLicenseTool` | Sunucu paketleriyle birlikte ayrıca dahili lisans aracını üret; müşteri kurulumuna eklemez |

## Klasor Yapisi

```
setup/
  build.ps1          — Ana build script'i
  build-client.ps1   — Windows masaustu ve Android istemcileri
  make.cmd           — Cift tikla calistirma
  Setup.iss          — Tam kurulum (Inno Setup)
  GuncelleSetup.iss  — Guncelleme paketi
  MusteriSetup.iss   — Eski dogrudan calistirma varyanti (yeni satis build'inde yok)
  LisansSetup.iss    — Lisans araci
  DesktopSetup.iss   — Windows masaustu istemcisi
  scripts/           — IIS yapilandirma PowerShell script'leri
  assets/            — Gorsel dosyalari (ikon, banner)
  payload/           — Publish ciktilari (gecici, .gitignore'da)
  output/            — Uretilen EXE'ler (.gitignore'da)
```

## Veritabanı ve yerel ayarlar

Uygulama seçilen sağlayıcıda ortak veritabanı kullanır; firma kapsamı kayıtların
`FirmaId` bağıyla korunur. Her firma için otomatik ayrı veritabanı oluşturulmaz.

Ana ve müşteri kurulum sihirbazları PostgreSQL veya SQLite bağlantısını alıp
`dbsettings.json` dosyasını hedefte oluşturur. Ana IIS paketinde bu dosyanın ACL'si
SYSTEM/Administrators ve uygulama havuzu ile sınırlandırılır; SQLite veri klasörüne
uygulama havuzunun yazma izni verilir. Müşteri doğrudan çalıştırma paketinde aynı ACL
koruması henüz yoktur; hedef makinede dosya erişimi doğrulanmadan bu varyant
dağıtıma kabul edilmez. SQL Server/MSSQL bu satış sürümünde desteklenmez.
Temiz kurulum paketi mevcut uygulama veya DB ayarı olan hedefi reddeder.
Güncelleme paketi kurulu ana/müşteri varyantının gerçek dizinini bulur ve
`dbsettings.json` ayarını korur; iki varyant aynı makinede kuruluysa hedef
belirsizliği nedeniyle durur.

`dbsettings.json`, `portalsettings.json` ve `backup_settings.json` kurulumun
çalışma zamanı dosyalarıdır. Kaynak depoya ve publish/kurulum paketine alınmazlar;
kurulum sihirbazı bağlantı ayarını yazar, diğer iki ayar uygulama varsayılanlarından
başlar ve yönetim ekranından kaydedilebilir. Git ile eski bir çalışma kopyasını
güncelleyen operatör, çekmeden önce yerel ayarlarının yedeğini almalıdır.
`-SkipPublish` ile eski payload kullanılırsa paket betiği bu dosyaları bulduğunda
işlemi durdurur.
Ortama özel `appsettings.*.json` dosyaları da Web publish/kurulum paketine alınmaz;
eski payload'da bulunurlarsa `setup/build.ps1` paketlemeyi reddeder. Pakette yalnız
sır içermeyen temel `appsettings.json` yer alır.

`Jwt__Secret` en az 32 karakterli rastgele üretim sırrıdır. Kurulum paketi bu sırrı
oluşturmaz veya saklamaz. İlk uygulama başlangıcından önce hedef Windows/IIS
ortamının onaylı gizli ayar deposunda tanımlanmalıdır. Değeri ZIP'e, `appsettings`
dosyasına, kaynak depoya veya komut satırına koymayın. Sır yapılandırılmamışsa
Production uygulaması başlamayı reddeder.

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

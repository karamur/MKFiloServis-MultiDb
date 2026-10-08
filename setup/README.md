# MKFiloServis-MultiDb — Setup / Kurulum Paketleri

## Gereksinimler

- **.NET 10 SDK** — https://dotnet.microsoft.com/download/dotnet/10.0
- **Inno Setup 6** — `winget install JRSoftware.InnoSetup`
- **PowerShell 7+** (Admin olarak calistirilmali)

## Hizli Baslangic

```cmd
cd setup
make.cmd 1.0.37
```

veya PowerShell ile:

```powershell
.\build.ps1 -Version 1.0.37
.\build-client.ps1 -Version 1.0.37
```

## Uretilen Paketler

| Dosya | Aciklama |
|-------|----------|
| `MKFiloServisKurulum-1.0.37.exe` | Sunucu kurulumu (Web + DataSync); lisans üretim aracı içermez |
| `MKFiloServisGuncelle-1.0.37.exe` | Guncelleme paketi |
| `MKFiloServisKurulumMusteri-1.0.37.exe` | Musteri paketi (lisans araci haric) |
| `MKLisansArac-1.0.37.exe` | Yalnızca dahili lisans üretim akışında oluşturulur |
| `MKFiloServisMasaustu-1.0.37.exe` | Windows masaustu istemcisi |
| `MKFiloServisAndroid-1.0.37.apk` | Android istemcisi (imzali APK) |

## Parametreler

| Parametre | Aciklama |
|-----------|----------|
| `-Version` | Versiyon numarasi (varsayilan: 1.0.37) |
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
  MusteriSetup.iss   — Musteri paketi
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

Ana ve müşteri kurulum sihirbazı PostgreSQL, SQLite veya MSSQL seçimini sorar.
PostgreSQL için bağlantı bilgileri, SQLite için dosya yolu alınır ve uygulama
ayar dosyası kurulumda üretilir. SQL Server seçeneği bugün bilgilendirme amacıyla
gösterilir ve ilerlemeyi durdurur; uygulamanın otomatik migration/audit altyapısı
SQL Server'ı henüz desteklemiyor. PostgreSQL veya SQLite seçilmelidir.
Ana IIS kurulumunda bağlantı ayar dosyası yalnız yöneticiler ve uygulama havuzu
tarafından okunabilir; SQLite veri klasörüne yazma izni uygulama havuzuna verilir.
Güncelleme paketi mevcut `dbsettings.json` ayarını korur.

`dbsettings.json`, `portalsettings.json` ve `backup_settings.json` kurulumun
çalışma zamanı dosyalarıdır. Kaynak depoya ve publish/kurulum paketine alınmazlar;
kurulum sihirbazı bağlantı ayarını yazar, diğer iki ayar uygulama varsayılanlarından
başlar ve yönetim ekranından kaydedilebilir. Git ile eski bir çalışma kopyasını
güncelleyen operatör, çekmeden önce yerel ayarlarının yedeğini almalıdır.
`-SkipPublish` ile eski payload kullanılırsa paket betiği bu dosyaları bulduğunda
işlemi durdurur.

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

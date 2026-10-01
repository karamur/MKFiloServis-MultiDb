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
| `MKFiloServisKurulum-1.0.37.exe` | Tam kurulum (Web + Lisans + DataSync) |
| `MKFiloServisGuncelle-1.0.37.exe` | Guncelleme paketi |
| `MKFiloServisKurulumMusteri-1.0.37.exe` | Musteri paketi (lisans araci haric) |
| `MKLisansArac-1.0.37.exe` | Bagimsiz lisans yonetim araci |
| `MKFiloServisMasaustu-1.0.37.exe` | Windows masaustu istemcisi |
| `MKFiloServisAndroid-1.0.37.apk` | Android istemcisi (imzali APK) |

## Parametreler

| Parametre | Aciklama |
|-----------|----------|
| `-Version` | Versiyon numarasi (varsayilan: 1.0.37) |
| `-SkipPublish` | Publish atla, sadece Inno Setup calistir |
| `-LisansOnly` | Sadece lisans araci EXE'si uret |

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

## MultiDb Notlari

Bu surum **Database-Per-Firma** mimarisini kullanir:
- `MKFiloServis_Master` — Kullanici, lisans, firma katalogu
- `MK_[FirmaKodu]_[ID]` — Her firma icin ayri tenant DB
- `MKFiloServis_Holding` — Konsolidasyon raporlari

Kurulum sonrasi aktif veritabani `dbsettings.json` uzerinden yapilandirilir.

## Masaustu ve Android istemcileri

Bu istemciler web uygulamasini WebView icinde acar. Web sunucusu ayrica kurulu ve
agdan erisilebilir olmalidir. Windows istemcisinde varsayilan adres
`http://localhost:5050` olarak gelir. Android uygulamasinda sunucunun LAN IP'si
veya HTTPS adresi girilir; telefondaki `localhost` bilgisayari gostermez.
Girilen adres cihazda saklanir. APK gelistirme anahtariyla imzalanir; herkese
dagitilacak kalici surum icin kuruma ait Android imzalama anahtari gereklidir.

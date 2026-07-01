# MKFiloServis-MultiDb — Setup / Kurulum Paketleri

## Gereksinimler

- **.NET 10 SDK** — https://dotnet.microsoft.com/download/dotnet/10.0
- **Inno Setup 6** — `winget install JRSoftware.InnoSetup`
- **PowerShell 7+** (Admin olarak calistirilmali)

## Hizli Baslangic

```cmd
cd setup
make.cmd 1.0.22
```

veya PowerShell ile:

```powershell
.\build.ps1 -Version 1.0.22
```

## Uretilen Paketler

| Dosya | Aciklama |
|-------|----------|
| `MKFiloServisKurulum-1.0.22.exe` | Tam kurulum (Web + Lisans + DataSync) |
| `MKFiloServisGuncelle-1.0.22.exe` | Guncelleme paketi (sadece dosya degistirir) |
| `MKFiloServisKurulumMusteri-1.0.22.exe` | Musteri paketi (Lisans araci haric) |
| `MKLisansArac-1.0.22.exe` | Bagimsiz lisans yonetim araci |

## Parametreler

| Parametre | Aciklama |
|-----------|----------|
| `-Version` | Versiyon numarasi (varsayilan: 1.0.22) |
| `-SkipPublish` | Publish atla, sadece Inno Setup calistir |
| `-LisansOnly` | Sadece lisans araci EXE'si uret |

## Klasor Yapisi

```
setup/
  build.ps1          — Ana build script'i
  make.cmd           — Cift tikla calistirma
  Setup.iss          — Tam kurulum (Inno Setup)
  GuncelleSetup.iss  — Guncelleme paketi
  MusteriSetup.iss   — Musteri paketi
  LisansSetup.iss    — Lisans araci
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

Kurulum sonrasi PostgreSQL baglantisi `dbsettings.json` uzerinden yapilandirilir.

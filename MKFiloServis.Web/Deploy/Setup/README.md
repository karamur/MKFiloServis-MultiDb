# MKFiloServis Setup

Bu klasor, uygulamanin publish edilip kurulum paket klasorune hazirlamnasi icin kullanilir.

**Surum: 1.0.27**

## Hizli Kullanim

### PowerShell
```powershell
pwsh .\\setup.ps1
```

Versiyon belirtmek icin:
```powershell
pwsh .\\setup.ps1 -Version 1.0.27
```

### Batch
```bat
setup.bat
```

Versiyon belirtmek icin:
```bat
setup.bat "" "" "" 1.0.27
```

## Parametreler

- `Configuration` (varsayilan: `Release`)
- `Runtime` (varsayilan: `win-x64`)
- `OutputRoot` (varsayilan: `./artifacts/setup`)
- `Version` (varsayilan: `1.0.27`)

Ornek:
```powershell
pwsh .\\setup.ps1 -Configuration Release -Runtime win-x64 -OutputRoot .\\artifacts\\setup -Version 1.0.27
```

## Ciktılar

- `artifacts/setup/publish` : dotnet publish ciktisi
- `artifacts/setup/package` : dagilim icin paket klasoru

Paket klasorune varsa mevcut IIS kurulum scriptleri (`kur.ps1`, `kur.bat`) da kopyalanir.
Ayrica `version.txt` dosyasi paket klasorune eklenir ve kurulum bilgilerini iceri.

## Versiyon Bilgisi

Her kurulum setup paketi asagidaki bilgileri iceren bir `version.txt` dosyasi olusturur:
- Versiyon numarasi
- Olusturulma tarihi ve saati
- Kurulum konfigurasyonu
- Runtime bilgisi

Ornek `version.txt` icerigi:
```
MKFiloServis Setup Package
Version: 1.0.27
Build Date: 2024-01-15 10:30:45
Configuration: Release
Runtime: win-x64
```

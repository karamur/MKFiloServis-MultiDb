# KOAFiloServis Setup

Bu klasör, uygulamanın publish edilip kurulum paket klasörüne hazırlanması için kullanılır.

## Hızlı Kullanım

### PowerShell
```powershell
pwsh .\setup.ps1
```

### Batch
```bat
setup.bat
```

## Parametreler

- `Configuration` (varsayılan: `Release`)
- `Runtime` (varsayılan: `win-x64`)
- `OutputRoot` (varsayılan: `./artifacts/setup`)

Örnek:
```powershell
pwsh .\setup.ps1 -Configuration Release -Runtime win-x64 -OutputRoot .\artifacts\setup
```

## Çıktılar

- `artifacts/setup/publish` : dotnet publish çıktısı
- `artifacts/setup/package` : dağıtım için paket klasörü

Paket klasörüne varsa mevcut IIS kurulum scriptleri (`kur.ps1`, `kur.bat`) da kopyalanır.

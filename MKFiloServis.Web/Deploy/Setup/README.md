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
- `UseDemoSqlite` (varsayilan: kapali; acildiginda dagitim paketini ayri `App_Data/MKFiloServis_Demo.db` SQLite veritabanini kullanacak sekilde ayarlar)

Ornek:
```powershell
pwsh .\\setup.ps1 -Configuration Release -Runtime win-x64 -OutputRoot .\\artifacts\\setup -Version 1.0.27
```

15 gunluk demo icin yalnizca yeni dagitim paketini SQLite demo veritabanina yonlendirmek:
```powershell
pwsh .\\setup.ps1 -UseDemoSqlite
```
Bu secenek kaynak `dbsettings.json` dosyasini ve kurulu veritabanlarini degistirmez; yalnizca olusturulan paket ayarini yazar. SQLite dosyasi ilk uygulama baslangicinda olusturulur.

## Ciktılar

- `artifacts/setup/publish` : dotnet publish ciktisi
- `artifacts/setup/package` : dagilim icin paket klasoru

Paket klasorune varsa mevcut IIS kurulum scriptleri (`kur.ps1`, `kur.bat`) da kopyalanir.
Ayrica `version.txt` dosyasi paket klasorune eklenir ve kurulum bilgilerini iceri.

## Sunucuya Evrak ve Anahtar Aktarımı

Yeni yüklenen evraklar ASP.NET Core Data Protection ile korunur; kullanıcı
tarafından yönetilen `master.key` gerekmez. `keys` klasöründeki otomatik Data
Protection anahtarları düzenli yedeklenmelidir.

Eski KOA1/AES şifreli evraklar açılacaksa, yalnız bu eski dosyalar için kaynak
makinede `Deploy\Migrate\02-dosya-aktar.ps1` çalıştırılabilir. Orijinal anahtarı
olmayan eski şifreli dosyalar listelenir ancak kriptografik olarak açılamaz.

Admin arşiv görüntüleyicinin taradığı depo kökü:
`C:\MKFiloServis_yedekleme\Depo`

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

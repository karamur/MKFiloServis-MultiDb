# MKFiloServis 1.0.27 - Windows Installer (.exe) Oluşturma Rehberi

## Seçenek 1: Inno Setup ile (Tavsiyeli - Türkçe Desteği)

### Adım 1: Inno Setup Yükleyin
1. https://jrsoftware.org/isdl.php adresinden Inno Setup indirin
2. İnstalleri çalıştırın ve varsayılan ayarlarla kurun
3. PowerShell veya Command Prompt yeniden başlatın

### Adım 2: Kurulum Scriptini Hazırla
```powershell
cd C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb
```

### Adım 3: Publish Dosyalarını Hazırla
Önce dotnet publish çalıştırılmalı:
```bash
cd setup\output
pwsh setup.ps1 -Configuration Release -Runtime win-x64 -Version 1.0.27
```

Bu komut aşağıdaki yapıyı oluşturacak:
```
artifacts/setup/
├── publish/     (dotnet publish çıktısı)
└── package/     (kurulum paketi)
```

### Adım 4: Inno Setup Scriptini Çalıştır
```batch
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup\MKFiloServis-Setup.iss
```

**Sonuç**: `setup\output\v1.0.27\MKFiloServis-1.0.27-Setup.exe` oluşturulacak

---

## Seçenek 2: NSIS ile (Hafif - Küçük Boyut)

### Adım 1: NSIS Yükleyin
1. https://nsis.sourceforge.io adresinden NSIS indirin
2. İnstalleri çalıştırın ve varsayılan ayarlarla kurun

### Adım 2: Setup Dosyalarını Hazırla
```cmd
cd C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb\setup
```

### Adım 3: NSIS Compiler'ı Çalıştır
```batch
"C:\Program Files (x86)\NSIS\makensis.exe" MKFiloServis-Setup.nsi
```

**Sonuç**: `setup\output\v1.0.27\MKFiloServis-1.0.27-Setup.exe` oluşturulacak

---

## PowerShell ile Otomatik Oluşturma

### Tek Komutla Seçenek 1 (Inno Setup):
```powershell
$innoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (Test-Path $innoSetup) {
    & $innoSetup "setup\MKFiloServis-Setup.iss"
} else {
    Write-Host "Inno Setup yüklü değildir!"
}
```

### Tek Komutla Seçenek 2 (NSIS):
```powershell
$nsis = "C:\Program Files (x86)\NSIS\makensis.exe"
if (Test-Path $nsis) {
    . $nsis "setup\MKFiloServis-Setup.nsi"
} else {
    Write-Host "NSIS yüklü değildir!"
}
```

---

## Sistem Gereksinimleri

| Tool | Boyut | İndirilme | Avantaj |
|------|-------|-----------|---------|
| **Inno Setup** | ~5 MB | https://jrsoftware.org | Türkçe UI, Kolay |
| **NSIS** | ~700 KB | https://nsis.sourceforge.io | Küçük, Hafif |

---

## Kurulum Dosyası Özellikleri

Oluşturulacak .exe dosyası aşağıdaki özelliklere sahip olacak:

✅ **Sürüm**: 1.0.27
✅ **64-bit**: Windows x64 için optimize edilmiş
✅ **Admin Hakları**: Gerekli
✅ **İşletim Sistemi**: Windows 10 / Server 2019+
✅ **Dil**: Türkçe ve İngilizce
✅ **Bileşenler**:
   - Ana Uygulama (zorunlu)
   - IIS Integration Tools
   - Documentation
   - Configuration Examples

✅ **Kurulum Sonrası**:
   - Başlat Menüsüne Kısayol
   - Masaüstüne Kısayol
   - Kaldır programı
   - Registry kaydı

---

## Kurulum Dosyası Boyutu Tahminı

- Inno Setup ile: 150-250 MB
- NSIS ile: 100-150 MB

(Dotnet publish çıktısına bağlı)

---

## Ek: Kurulum Dosyasını İmzala (İsteğe Bağlı)

Windows SmartScreen uyarısını önlemek için dosyayı imzala:
```powershell
# Sertifika oluştur
$cert = New-SelfSignedCertificate -CertStoreLocation cert:\CurrentUser\My -Subject "MKFiloServis"

# .exe dosyasını imzala
$signingParams = @{
    FilePath = "setup\output\v1.0.27\MKFiloServis-1.0.27-Setup.exe"
    Certificate = $cert
    TimestampServer = "http://timestamp.digicert.com"
}
Set-AuthenticodeSignature @signingParams
```

---

## Sorun Giderme

### Inno Setup Hatası: "File not found"
- Publish dosyalarının `artifacts\setup\package\` klasöründe olup olmadığını kontrol edin
- Yolları kontrol edin

### NSIS Hatası: "Installer created successfully"
- Kurulum başarılı demektir!

### .exe çalışmıyor
- .NET 8.0 SDK yüklü olup olmadığını kontrol edin
- Windows Defender tarafından engellenmemiş olup olmadığını kontrol edin

---

## Manual Onay İşlemleri

```powershell
# Oluşturulan dosyayı kontrol et
ls -lh "setup\output\v1.0.27\MKFiloServis-1.0.27-Setup.exe"

# Dosya imzasını kontrol et
Get-AuthenticodeSignature "setup\output\v1.0.27\MKFiloServis-1.0.27-Setup.exe"

# Kurulum programını çalıştır
& "setup\output\v1.0.27\MKFiloServis-1.0.27-Setup.exe"
```

---

**Tavsiye**: Kurulum dosyasını dağıtmadan önce test ortamında çalıştırıp kontrol edin!


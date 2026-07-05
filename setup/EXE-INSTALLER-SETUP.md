# 📦 MKFiloServis 1.0.27 - Windows Installer (.exe) Oluşturma

## 🎯 Hızlı Başlangıç

### 1️⃣ Seçeneği 1: Otomatik Oluşturma (Tavsiyeli)

```powershell
cd C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb
pwsh setup\build-exe.ps1
```

**Bu komut:**
- Inno Setup'ı otomatik olarak algılar
- Setup dosyalarını hazırlar
- .exe installer'ı oluşturur
- Sonucu `setup\output\v1.0.27\` klasörüne koyar

---

## 📋 Alternatif Seçenekler

### 2️⃣ Seçeneği 2: PowerShell ile (Gelişmiş)

```powershell
# Inno Setup ile
pwsh setup\build-installer.ps1 -InstallerType inno -OutputVersion 1.0.27

# veya NSIS ile
pwsh setup\build-installer.ps1 -InstallerType nsis -OutputVersion 1.0.27

# Build + Installer oluştur
pwsh setup\build-installer.ps1 -InstallerType inno -BuildBefore
```

### 3️⃣ Seçeneği 3: Batch ile (Basit)

```batch
cd C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb\setup
build-installer.bat inno
```

---

## 🛠️ Araç Gereksinimleri

### Seçenek A: Inno Setup (Tavsiyeli)

**Adım 1: İndir ve Kur**
```powershell
# 1. https://jrsoftware.org/isdl.php adresine git
# 2. "Inno Setup 6" indirin (64-bit)
# 3. Setup executable'ı çalıştırın
# 4. Varsayılan kurulum ayarlarını kabul et
# 5. PowerShell/CMD yeniden başlat
```

**Adım 2: Installer Oluştur**
```powershell
pwsh setup\build-exe.ps1
```

### Seçenek B: NSIS

**Adım 1: İndir ve Kur**
```
# 1. https://nsis.sourceforge.io adresine git
# 2. NSIS 3.x indirin
# 3. Setup executable'ı çalıştırın
# 4. Varsayılan kurulum ayarlarını kabul et
```

**Adım 2: Installer Oluştur**
```powershell
pwsh setup\build-installer.ps1 -InstallerType nsis
```

---

## 📊 Installer Karşılaştırması

| Özellik | Inno Setup | NSIS |
|---------|-----------|------|
| **Ölçeklenebilirlik** | Büyük bileşenler | Hafif |
| **UI Dili** | Türkçe ✓ | Sınırlı |
| **Dosya Boyutu** | 150-250 MB | 100-150 MB |
| **Kurulum Süresi** | ~1 dakika | ~30 saniye |
| **Uyumluluk** | En iyi | Mükemmel |
| **Tavsiye** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |

---

## 📁 Oluşturulacak Dosya Yapısı

```
setup/
├── build-exe.ps1                 # Otomatik .exe oluşturucu
├── build-installer.ps1           # Gelişmiş oluşturucu
├── build-installer.bat           # Batch wrapper
├── MKFiloServis-Setup.iss        # Inno Setup scripti
├── MKFiloServis-Setup.nsi        # NSIS scripti
├── INSTALLER-GUIDE.md            # Ekstra rehber
│
└── output/
    └── v1.0.27/
        └── MKFiloServis-1.0.27-Setup.exe  ← FINAL DOSYA
```

---

## ✨ Oluşturulacak .exe Özellikleri

✅ **Versiyon**: 1.0.27
✅ **Mimarı**: 64-bit (x64)
✅ **İşletim Sistemi**: Windows 10 / Server 2019+
✅ **İzin**: Admin hakları gerekli
✅ **Dil**: Türkçe & İngilizce
✅ **Boyut**: 150-250 MB (kompres edilmiş)

### Kurulum Bileşenleri

- 🔹 **Ana Uygulama** (Zorunlu)
  - MKFiloServis.Web.exe
  - Bağımlılıklar ve runtime
  - Konfigürasyon dosyaları

- 🔹 **IIS Entegrasyon** (Opsiyonel)
  - kur.ps1
  - kur.bat
  - Deploy scripti

- 🔹 **Dokümantasyon** (Opsiyonel)
  - README.md
  - INSTALL.md
  - QUICKSTART.md
  - Deploy rehberleri

### Kurulum Sonrası

✅ **Başlat Menüsü** → MKFiloServis
✅ **Masaüstü Kısayolu** → MKFiloServis
✅ **Kaldır Programı** → Kontrol Masası
✅ **Registry** → Kurulum bilgileri
✅ **Çalıştırabilir** → Hemen başlatılabilir

---

## 🚀 Kurulum Talimatları

### Kullanıcılar için:

```
1. MKFiloServis-1.0.27-Setup.exe çift tıkla
2. Dil seç (Türkçe/English)
3. Kurulum sihirbazını takip et
4. Bileşenleri seç (ya da varsayılan kabulEt)
5. Kurulum konumunu seç
6. Kurulma işleminin bitmesini bekle
7. Seçenek: "Start MKFiloServis" işaretle ve bitir
```

### Kurulumdan Sonra:

```
- Masaüstünde MKFiloServis kısayolu oluşur
- Başlat Menüsünde MKFiloServis grubu oluşur
- Program çalışmaya hazır
```

---

## 🔧 Sorun Giderme

### ❌ "Inno Setup Not Found"

```powershell
# Çözüm 1: Yüklü mü kontrol et
Test-Path "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

# Çözüm 2: Farklı dizinde ara
dir "C:\Program Files\Inno Setup*" -Recurse

# Çözüm 3: Yeniden kur
# https://jrsoftware.org/isdl.php adresinde indir
```

### ❌ ".exe Dosyası Başlamamıyor"

```powershell
# Çözüm 1: .NET SDK kontrol et
dotnet --version

# Çözüm 2: Windows Defender kontrol et
# Dosyaya sağ tıkla → Özellikler → "Engellemeyi Kaldır" işaretle

# Çözüm 3: Dosyayı imzala
# (Admin PowerShell)
$cert = New-SelfSignedCertificate -CertStoreLocation cert:\CurrentUser\My -Subject "MKFiloServis"
Set-AuthenticodeSignature -FilePath "setup\output\v1.0.27\MKFiloServis-1.0.27-Setup.exe" -Certificate $cert
```

### ❌ Kurulum Başarısız

```powershell
# Adım 1: Publish dosyaları kontrol et
Test-Path "artifacts\setup\package"

# Adım 2: Disk alanını kontrol et
(Get-Volume C:).SizeRemaining / 1GB

# Adım 3: İzin kontrol et
[Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
```

---

## 📞 Destek

| Konu | İletişim |
|------|----------|
| **Hata Bildirimi** | https://github.com/karamur/MKFiloServis-MultiDb/issues |
| **Proje** | https://github.com/karamur/MKFiloServis-MultiDb |
| **Release'ler** | https://github.com/karamur/MKFiloServis-MultiDb/releases |

---

## 📝 Adım Adım Tam Örnek

### Senaryö: Tamamen Yeni Kurulum

```powershell
# 1. Proje klasörüne git
cd "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb"

# 2. Kurulum setup dosyalarını hazırla
pwsh setup\output\setup.ps1 -Configuration Release -Runtime win-x64 -Version 1.0.27

# 3. .exe installer'ı oluştur
pwsh setup\build-exe.ps1

# 4. Sonucu kontrol et
ls -lh setup\output\v1.0.27\*.exe

# 5. Installer'ı test et
& "setup\output\v1.0.27\MKFiloServis-1.0.27-Setup.exe" /?"
```

---

## ✅ Kontrol Listesi

- [ ] Inno Setup veya NSIS yüklü
- [ ] .NET 8.0 SDK yüklü
- [ ] PowerShell 7.0+ var
- [ ] Publish dosyaları hazır (`artifacts/setup/package`)
- [ ] Admin hakları mevcut
- [ ] Disk alanı yeterli (2+ GB)
- [ ] Setup scripti hazır

---

**Sonraki Adım**: `pwsh setup\build-exe.ps1` komutunu çalıştırın! 🚀


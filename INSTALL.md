# 🚀 Koa Filo Servis - Kurulum Kılavuzu

## 📋 Sistem Gereksinimleri

### Minimum Gereksinimler
- **İşletim Sistemi:** Windows Server 2019+ veya Windows 10/11
- **RAM:** 4 GB (8 GB önerilir)
- **Disk:** 2 GB boş alan
- **.NET Runtime:** .NET 10.0 Runtime
- **Veritabanı:** PostgreSQL 14+
- **Tarayıcı:** Chrome, Firefox, Edge (güncel sürümler)

### Önerilen Gereksinimler
- **RAM:** 16 GB
- **Disk:** SSD, 10 GB boş alan
- **Ollama:** AI özellikleri için (opsiyonel)

---

## 📦 Kurulum Adımları

### 1. .NET 10 Runtime Kurulumu

```powershell
# Windows için .NET 10 Runtime indirme
# https://dotnet.microsoft.com/download/dotnet/10.0
winget install Microsoft.DotNet.AspNetCore.10
```

### 2. PostgreSQL Kurulumu

```powershell
# PostgreSQL indirme: https://www.postgresql.org/download/windows/
# Kurulum sırasında:
# - Port: 5432 (varsayılan)
# - Şifre: güçlü bir şifre belirleyin
# - pgAdmin kurulumu önerilir
```

### 3. Veritabanı Oluşturma

```sql
-- pgAdmin veya psql ile:
CREATE DATABASE crm_filo_servis;
CREATE USER crm_user WITH ENCRYPTED PASSWORD 'GuvenliSifre123!';
GRANT ALL PRIVILEGES ON DATABASE crm_filo_servis TO crm_user;
```

### 4. Uygulama Kurulumu

```powershell
# 1. Release dosyasını indirin ve çıkarın
Expand-Archive -Path CRMFiloServis-Release.zip -DestinationPath C:\Apps\CRMFiloServis

# 2. Uygulama dizinine gidin
cd C:\Apps\CRMFiloServis

# 3. appsettings.json dosyasını düzenleyin
```

### 5. Yapılandırma (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=crm_filo_servis;Username=crm_user;Password=GuvenliSifre123!"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      },
      "Https": {
        "Url": "https://0.0.0.0:5001"
      }
    }
  },
  "OllamaSettings": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.2:3b"
  }
}
```

### 6. Veritabanı Migration

```powershell
# İlk çalıştırmada otomatik migration yapılır
# Veya manuel olarak:
dotnet CRMFiloServis.Web.dll --migrate
```

### 7. Uygulamayı Başlatma

```powershell
# Doğrudan çalıştırma
dotnet CRMFiloServis.Web.dll

# Veya kurulum script'i ile
.\install.ps1
```

---

## 🖥️ Windows Servis Kurulumu

### NSSM ile Servis Oluşturma

```powershell
# NSSM indirin: https://nssm.cc/download
# veya
choco install nssm

# Servis oluşturma
nssm install CRMFiloServis "C:\Apps\CRMFiloServis\CRMFiloServis.Web.exe"
nssm set CRMFiloServis AppDirectory "C:\Apps\CRMFiloServis"
nssm set CRMFiloServis Description "Koa Filo Servis Yönetim Sistemi"
nssm set CRMFiloServis Start SERVICE_AUTO_START

# Servisi başlatma
nssm start CRMFiloServis
```

### sc.exe ile Servis Oluşturma

```powershell
sc.exe create CRMFiloServis binPath="C:\Apps\CRMFiloServis\CRMFiloServis.Web.exe" start=auto
sc.exe description CRMFiloServis "Koa Filo Servis Yönetim Sistemi"
sc.exe start CRMFiloServis
```

---

## 🤖 Ollama Kurulumu (AI Özellikleri İçin)

```powershell
# 1. Ollama indirin: https://ollama.ai/download
winget install Ollama.Ollama

# 2. Model indirin
ollama pull llama3.2:3b

# 3. Ollama servisini başlatın
ollama serve
```

---

## 🔒 SSL/HTTPS Yapılandırması

### Geliştirme Sertifikası

```powershell
dotnet dev-certs https --trust
```

### Üretim Sertifikası (Let's Encrypt)

```powershell
# Certbot kullanarak
certbot certonly --standalone -d yourdomain.com

# appsettings.json'a ekleyin:
"Kestrel": {
  "Endpoints": {
    "Https": {
      "Url": "https://0.0.0.0:443",
      "Certificate": {
        "Path": "/etc/letsencrypt/live/yourdomain.com/fullchain.pem",
        "KeyPath": "/etc/letsencrypt/live/yourdomain.com/privkey.pem"
      }
    }
  }
}
```

---

## 🔥 Firewall Yapılandırması

```powershell
# HTTP port
netsh advfirewall firewall add rule name="CRMFiloServis HTTP" dir=in action=allow protocol=TCP localport=5000

# HTTPS port
netsh advfirewall firewall add rule name="CRMFiloServis HTTPS" dir=in action=allow protocol=TCP localport=5001
```

---

## 📊 İlk Kullanım

1. Tarayıcıda `http://localhost:5000` adresine gidin
2. Varsayılan admin hesabı:
   - **Kullanıcı:** admin
   - **Şifre:** Admin123!
3. Şifrenizi hemen değiştirin
4. Sistem ayarlarını yapılandırın

---

## 🔧 Sorun Giderme

### Port Kullanımda Hatası

```powershell
# Portu kullanan uygulamayı bulun
netstat -ano | findstr :5000

# Alternatif port kullanın
dotnet CRMFiloServis.Web.dll --urls "http://0.0.0.0:8080"
```

### Veritabanı Bağlantı Hatası

```powershell
# PostgreSQL servisini kontrol edin
Get-Service postgresql*

# Bağlantıyı test edin
psql -h localhost -U crm_user -d crm_filo_servis
```

### Log Dosyaları

```
C:\Apps\CRMFiloServis\logs\
├── app-{date}.log
├── error-{date}.log
└── access-{date}.log
```

### IIS 500.30 Tanilama (Kurulum Yapilan PC)

```powershell
# IIS / Hosting Bundle / web.config / stdout / Event Viewer tek raporda toplanir
powershell -ExecutionPolicy Bypass -File .\setup\scripts\collect-iis-diagnostics.ps1 \
  -InstallPath "C:\MKFiloServis\IIS" \
  -SiteName "MKFiloServis"
```

Rapor dosyasi varsayilan olarak su klasore yazilir:

```
C:\MKFiloServis\IIS\logs\diag-YYYYMMDD-HHMMSS.txt
```

---

## 📞 Destek

- **GitHub Issues:** https://github.com/karamur/CRMFiloServis/issues
- **Dokümantasyon:** DEVELOPMENT.md, ROADMAP.md dosyalarına bakın

---

*Son güncelleme: Haziran 2025*


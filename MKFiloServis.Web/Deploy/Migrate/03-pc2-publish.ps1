# =============================================================================
# 03-pc2-publish.ps1
# 2. PC kurulumu için uygulama paketi oluşturur.
# Çıktı: artifacts\pc2-paket\  (zip ve klasör)
#
# KULLANIM (Bu PC'de, Geliştirme ortamında çalıştırın):
#   pwsh -ExecutionPolicy Bypass -File 03-pc2-publish.ps1
#   Oluşan paketi 2. PC'ye taşıyın ve orada kur.bat'ı çalıştırın.
# =============================================================================

param(
    [string]$Configuration  = "Release",
    [string]$Runtime        = "win-x64",
    [string]$OutputRoot     = ".\artifacts\pc2-paket",

    # 2. PC'nin adı (bilgi amaçlı klasör adı)
    [string]$PC2Adi         = "PC2"
)

$ErrorActionPreference = "Stop"
$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ScriptDir "..\..\KOAFiloServis.Web.csproj"
$DeployIis   = Join-Path $ScriptDir "..\IIS"

function Write-Step([string]$msg) { Write-Host "[PUBLISH] $msg" -ForegroundColor Cyan }
function Write-OK([string]$msg)   { Write-Host "[OK] $msg"       -ForegroundColor Green }
function Write-Err([string]$msg)  { Write-Host "[HATA] $msg"     -ForegroundColor Red; exit 1 }

# --- Proje dosyası kontrolü ---
if (-not (Test-Path $ProjectFile)) {
    Write-Err "Proje dosyası bulunamadı: $ProjectFile"
}

$publishDir = Join-Path $OutputRoot "publish"
$packageDir = Join-Path $OutputRoot "package"
$zipPath    = Join-Path $OutputRoot "KOAFiloServis-$PC2Adi-$(Get-Date -Format 'yyyyMMdd_HHmm').zip"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  KOA FİLO SERVİS — 2. PC PAKET HAZIRLIĞI" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  Konfigürasyon : $Configuration" -ForegroundColor White
Write-Host "  Runtime       : $Runtime" -ForegroundColor White
Write-Host "  Çıktı         : $OutputRoot" -ForegroundColor White
Write-Host ""

# ---------------------------------------------------------------------------
# 1. dotnet publish
# ---------------------------------------------------------------------------
Write-Step "[1/5] Uygulama derleniyor ve yayınlanıyor..."

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish $ProjectFile -c $Configuration -r $Runtime --self-contained false -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Err "dotnet publish başarısız oldu (kod: $LASTEXITCODE)"
}
Write-OK "Publish tamamlandı: $publishDir"

# ---------------------------------------------------------------------------
# 2. Paket klasörü hazırla
# ---------------------------------------------------------------------------
Write-Step "[2/5] Paket klasörü hazırlanıyor..."

if (Test-Path $packageDir) { Remove-Item $packageDir -Recurse -Force }
New-Item -ItemType Directory -Path $packageDir | Out-Null
Copy-Item "$publishDir\*" $packageDir -Recurse -Force
Write-OK "Paket kopyalandı: $packageDir"

# ---------------------------------------------------------------------------
# 3. IIS scriptlerini ekle (kur.bat, kur.ps1)
# ---------------------------------------------------------------------------
Write-Step "[3/5] Kurulum scriptleri ekleniyor..."

if (Test-Path $DeployIis) {
    $kurBat = Join-Path $DeployIis "kur.bat"
    $kurPs1 = Join-Path $DeployIis "kur.ps1"
    if (Test-Path $kurBat) { Copy-Item $kurBat $packageDir -Force; Write-OK "kur.bat eklendi." }
    if (Test-Path $kurPs1) { Copy-Item $kurPs1 $packageDir -Force; Write-OK "kur.ps1 eklendi." }
} else {
    Write-Host "  [UYARI] Deploy\IIS klasörü bulunamadı: $DeployIis" -ForegroundColor Yellow
}

# ---------------------------------------------------------------------------
# 4. 2. PC için appsettings.PC2.json şablonu oluştur
# ---------------------------------------------------------------------------
Write-Step "[4/5] 2. PC için appsettings şablonu oluşturuluyor..."

$pc2AppSettings = @'
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=BURAYA_SIFRE;Pooling=true;MinPoolSize=2;MaxPoolSize=50;"
  },
  "Jwt": {
    "Secret": "BURAYA_MIN_32_KARAKTER_GIZLI_ANAHTAR_YAZIN",
    "Issuer": "KOAFiloServis",
    "Audience": "KOAFiloServis-API",
    "ExpirationHours": 8
  },
  "Backup": {
    "Enabled": true,
    "Path": "C:\\KOAFiloServis_yedekleme\\database",
    "RetentionDays": 30,
    "ScheduleHour": 3
  },
  "Storage": {
    "Provider": "Local"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
'@

$pc2SettingsPath = Join-Path $packageDir "appsettings.PC2.json"
$pc2AppSettings | Set-Content $pc2SettingsPath -Encoding UTF8
Write-OK "appsettings.PC2.json şablonu oluşturuldu."
Write-Host "  !! 2. PC'de bu dosyayı appsettings.Production.json olarak kaydedin !!" -ForegroundColor Yellow

# ---------------------------------------------------------------------------
# 5. ZIP oluştur (opsiyonel — ZIP araçları varsa)
# ---------------------------------------------------------------------------
Write-Step "[5/5] ZIP paketi oluşturuluyor..."

try {
    Compress-Archive -Path "$packageDir\*" -DestinationPath $zipPath -Force
    $zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-OK "ZIP hazır: $zipPath ($zipSize MB)"
} catch {
    Write-Host "  [UYARI] ZIP oluşturulamadı: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "  Paket klasörünü elle taşıyın: $packageDir" -ForegroundColor Yellow
}

# ---------------------------------------------------------------------------
# Özet
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  PAKET HAZIR — 2. PC KURULUM TALİMATLARI" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "  ZIP dosyası : $zipPath" -ForegroundColor Green
Write-Host "  veya klasör : $packageDir" -ForegroundColor Green
Write-Host ""
Write-Host "2. PC'de yapılacaklar:" -ForegroundColor Yellow
Write-Host "  1. PostgreSQL 16 kur (henüz kurulu değilse)" -ForegroundColor White
Write-Host "  2. .NET 10 Hosting Bundle kur (aka.ms/dotnet/download)" -ForegroundColor White
Write-Host "  3. Paketi C:\KOAFiloServis\IIS klasörüne çıkar" -ForegroundColor White
Write-Host "  4. Şifreli evrakları C:\KOAFiloServis_yedekleme\uploads klasörüne kopyala" -ForegroundColor White
Write-Host "  5. Data protection key'leri C:\KOAFiloServis_yedekleme\keys klasörüne kopyala" -ForegroundColor White
Write-Host "  6. DB restore: 01-db-restore.ps1 ile PostgreSQL'e aktar" -ForegroundColor White
Write-Host "  7. appsettings.PC2.json içeriğini appsettings.Production.json olarak kaydet" -ForegroundColor White
Write-Host "  8. IIS'de site ekle → kur.bat ile kur (Mode=Install)" -ForegroundColor White
Write-Host "  9. Lisansı app üzerinden aktiflestir" -ForegroundColor White
Write-Host ""
Write-Host "  Detaylar için: Deploy\Migrate\04-pc2-kurulum-talimat.md" -ForegroundColor Cyan
Write-Host ""

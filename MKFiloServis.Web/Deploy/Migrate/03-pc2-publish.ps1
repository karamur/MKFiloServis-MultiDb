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
$ProjectFile = Join-Path $ScriptDir "..\..\MKFiloServis.Web.csproj"
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
$zipPath    = Join-Path $OutputRoot "MKFiloServis-$PC2Adi-$(Get-Date -Format 'yyyyMMdd_HHmm').zip"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  MK FİLO SERVİS — 2. PC PAKET HAZIRLIĞI" -ForegroundColor Magenta
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
# 4. Güvenli ayar hatırlatması
# ---------------------------------------------------------------------------
Write-Step "[4/5] Üretim sırları pakete yazılmıyor."
Write-Host "  Bu legacy paket appsettings.PC2.json veya üretim sırrı üretmez." -ForegroundColor Yellow
Write-Host "  Kurulumu güncel setup/Setup.iss sihirbazıyla yapın; JWT sırrını" -ForegroundColor Yellow
Write-Host "  hedef sunucunun korumalı ortam değişkeni deposunda Jwt__Secret olarak sağlayın." -ForegroundColor Yellow

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
Write-Host "  3. Paketi C:\MKFiloServis\IIS klasörüne çıkar" -ForegroundColor White
Write-Host "  4. Şifreli evrakları C:\MKFiloServis_yedekleme\uploads klasörüne kopyala" -ForegroundColor White
Write-Host "  5. Anahtar ve evrak aktarımı için kaynak makinede 02-dosya-aktar.ps1 çalıştır" -ForegroundColor White
Write-Host "     (master.key dosyasını başka makineye doğrudan kopyalama)" -ForegroundColor Yellow
Write-Host "  6. DB restore: 01-db-restore.ps1 ile PostgreSQL'e aktar" -ForegroundColor White
Write-Host "  7. Güncel setup\MKFiloServisKurulum-<sürüm>.exe paketini kullan; bu legacy ZIP tek başına kurulum paketi değildir" -ForegroundColor White
Write-Host "  8. dbsettings.json dosyasını güncel kurulum sihirbazıyla üret; Jwt__Secret değerini sunucunun korumalı secret deposundan sağla" -ForegroundColor White
Write-Host "  9. IIS uygulama havuzunu başlatmadan önce DB restore ve ayar/ACL doğrulamasını tamamla" -ForegroundColor White
Write-Host " 10. Lisansı uygulama üzerinden etkinleştir" -ForegroundColor White
Write-Host ""
Write-Host "  Detaylar için: Deploy\Migrate\04-pc2-kurulum-talimat.md" -ForegroundColor Cyan
Write-Host ""

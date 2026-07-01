# ============================================
# MK Filo Servis - Windows Kurulum Script'i
# ============================================

param(
    [string]$InstallPath = "C:\Apps\MKFiloServis",
    [string]$ServiceName = "MKFiloServis",
    [int]$HttpPort = 5000,
    [int]$HttpsPort = 5001,
    [switch]$InstallService,
    [switch]$StartService
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MK Filo Servis Kurulum Aracı" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# .NET Runtime kontrolü
Write-Host "[1/6] .NET Runtime kontrol ediliyor..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "HATA: .NET Runtime bulunamadı!" -ForegroundColor Red
    Write-Host "Lütfen .NET 10 Runtime yükleyin: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Yellow
    exit 1
}
Write-Host "  ✓ .NET $dotnetVersion bulundu" -ForegroundColor Green

# Kurulum dizini oluşturma
Write-Host "[2/6] Kurulum dizini hazırlanıyor..." -ForegroundColor Yellow
if (!(Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    Write-Host "  ✓ Dizin oluşturuldu: $InstallPath" -ForegroundColor Green
} else {
    Write-Host "  ✓ Dizin mevcut: $InstallPath" -ForegroundColor Green
}

# Dosyaları kopyalama
Write-Host "[3/6] Uygulama dosyaları kopyalanıyor..." -ForegroundColor Yellow
$sourcePath = Split-Path -Parent $MyInvocation.MyCommand.Path
if (Test-Path "$sourcePath\MKFiloServis.Web.dll") {
    Copy-Item -Path "$sourcePath\*" -Destination $InstallPath -Recurse -Force
    Write-Host "  ✓ Dosyalar kopyalandı" -ForegroundColor Green
} else {
    Write-Host "  ! Script publish klasöründe çalıştırılmalı" -ForegroundColor Yellow
}

# appsettings.json kontrolü
Write-Host "[4/6] Yapılandırma kontrol ediliyor..." -ForegroundColor Yellow
$configFile = "$InstallPath\appsettings.json"
if (Test-Path $configFile) {
    Write-Host "  ✓ appsettings.json mevcut" -ForegroundColor Green
    Write-Host "  ! Veritabanı bağlantı ayarlarını kontrol edin" -ForegroundColor Yellow
} else {
    Write-Host "  ✗ appsettings.json bulunamadı!" -ForegroundColor Red
}

# Firewall kuralları
Write-Host "[5/6] Firewall kuralları ekleniyor..." -ForegroundColor Yellow
try {
    $existingHttp = Get-NetFirewallRule -DisplayName "MKFiloServis HTTP" -ErrorAction SilentlyContinue
    if (!$existingHttp) {
        New-NetFirewallRule -DisplayName "MKFiloServis HTTP" -Direction Inbound -Action Allow -Protocol TCP -LocalPort $HttpPort | Out-Null
        Write-Host "  ✓ HTTP port $HttpPort açıldı" -ForegroundColor Green
    } else {
        Write-Host "  ✓ HTTP kuralı mevcut" -ForegroundColor Green
    }
    
    $existingHttps = Get-NetFirewallRule -DisplayName "MKFiloServis HTTPS" -ErrorAction SilentlyContinue
    if (!$existingHttps) {
        New-NetFirewallRule -DisplayName "MKFiloServis HTTPS" -Direction Inbound -Action Allow -Protocol TCP -LocalPort $HttpsPort | Out-Null
        Write-Host "  ✓ HTTPS port $HttpsPort açıldı" -ForegroundColor Green
    } else {
        Write-Host "  ✓ HTTPS kuralı mevcut" -ForegroundColor Green
    }
} catch {
    Write-Host "  ! Firewall kuralları eklenemedi (Admin yetkisi gerekebilir)" -ForegroundColor Yellow
}

# Windows Servis kurulumu
if ($InstallService) {
    Write-Host "[6/6] Windows Servisi kuruluyor..." -ForegroundColor Yellow

    $exePath = "$InstallPath\MKFiloServis.Web.exe"
    
    # Mevcut servisi kontrol et
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "  ! Mevcut servis durduruluyor..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        sc.exe delete $ServiceName | Out-Null
        Start-Sleep -Seconds 2
    }
    
    # Yeni servis oluştur
    sc.exe create $ServiceName binPath="$exePath" start=auto | Out-Null
    sc.exe description $ServiceName "MK Filo Servis Yönetim Sistemi" | Out-Null
    Write-Host "  ✓ Servis oluşturuldu: $ServiceName" -ForegroundColor Green
    
    if ($StartService) {
        Start-Service -Name $ServiceName
        Write-Host "  ✓ Servis başlatıldı" -ForegroundColor Green
    }
} else {
    Write-Host "[6/6] Servis kurulumu atlandı (-InstallService parametresi kullanın)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Kurulum Tamamlandı!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Sonraki Adımlar:" -ForegroundColor Yellow
Write-Host "  1. appsettings.json dosyasını düzenleyin (veritabanı bağlantısı)" -ForegroundColor White
Write-Host "  2. PostgreSQL veritabanını oluşturun" -ForegroundColor White
Write-Host "  3. Uygulamayı başlatın:" -ForegroundColor White
Write-Host "     cd $InstallPath" -ForegroundColor Gray
Write-Host "     dotnet MKFiloServis.Web.dll" -ForegroundColor Gray
Write-Host ""
Write-Host "Web Adresi: http://localhost:$HttpPort" -ForegroundColor Cyan
Write-Host ""

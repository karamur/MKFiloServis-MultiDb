#!/usr/bin/env pwsh
# MKFiloServis Setup Installer Generator
# Versiyon: 1.0.27
# Bu script .exe installer dosyasını otomatik olarak oluşturur

param(
    [string]$InstallerType = "inno",  # "inno" veya "nsis"
    [string]$OutputVersion = "1.0.27",
    [switch]$BuildBefore,
    [switch]$SkipDialogs
)

$ErrorActionPreference = "Stop"

# Renkli çıktı fonksiyonları
function Write-Title { Write-Host "`n$args" -ForegroundColor Cyan -BackgroundColor Black }
function Write-Success { Write-Host "$args" -ForegroundColor Green }
function Write-Error_ { Write-Host "ERROR: $args" -ForegroundColor Red }
function Write-Warning_ { Write-Host "UYARI: $args" -ForegroundColor Yellow }
function Write-Info { Write-Host "INFO: $args" -ForegroundColor Blue }

Write-Title "╔════════════════════════════════════════════════════════════╗"
Write-Title "║        MKFiloServis Setup Installer Generator v1.0        ║"
Write-Title "║                    Sürüm: $OutputVersion                            ║"
Write-Title "╚════════════════════════════════════════════════════════════╝"

# 1. Sistem Kontrolü
Write-Title "[1/5] Sistem Kontrol Ediliyor..."

# .NET SDK kontrol
Write-Info "Checking .NET SDK..."
$dotnetVersion = dotnet --version
Write-Success "✓ .NET SDK bulundu: $dotnetVersion"

# Admin hakları kontrol
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Warning_ "Admin hakları olmadan çalışıyor, uyarılar olabilir"
}

# 2. Setup Dosyalarını Hazırla
Write-Title "[2/5] Setup Dosyaları Hazırlanıyor..."

if ($BuildBefore) {
    Write-Info "Publishing application..."
    $setupScript = Join-Path $PSScriptRoot "output\setup.ps1"
    if (Test-Path $setupScript) {
        & pwsh $setupScript -Configuration Release -Runtime win-x64 -Version $OutputVersion
        Write-Success "✓ Publish tamamlandı"
    } else {
        Write-Error_ "setup.ps1 bulunamadı!"
        exit 1
    }
} else {
    Write-Info "Setup dosyaları kullanılıyor (mevcut olanlar)"
    $packagePath = Join-Path $PSScriptRoot "output\artifacts\setup\package"
    if (-not (Test-Path $packagePath)) {
        Write-Error_ "Kurulum paketi bulunamadı: $packagePath"
        Write-Warning_ "Önce 'setup.ps1' çalıştırın veya '-BuildBefore' parametresini kullanın"
        exit 1
    }
    Write-Success "✓ Kurulum paketi bulundu"
}

# 3. İnstaller Türünü Seç
Write-Title "[3/5] İnstaller Türü: $InstallerType"

$installerPath = $null
$setupScript = $null

switch ($InstallerType.ToLower()) {
    "inno" {
        Write-Info "Inno Setup'ın kontrol edildiği..."
        $innoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

        if (-not (Test-Path $innoSetup)) {
            $innoSetup = "C:\Program Files\Inno Setup 6\ISCC.exe"
        }

        if (-not (Test-Path $innoSetup)) {
            Write-Error_ "Inno Setup bulunamadı!"
            Write-Warning_ "Lütfen https://jrsoftware.org/isdl.php adresinden Inno Setup indirin"
            exit 1
        }

        $setupScript = Join-Path $PSScriptRoot "MKFiloServis-Setup.iss"
        if (-not (Test-Path $setupScript)) {
            Write-Error_ "Inno Setup script bulunamadı: $setupScript"
            exit 1
        }

        $installerPath = $innoSetup
        Write-Success "✓ Inno Setup bulundu: $innoSetup"
    }
    "nsis" {
        Write-Info "NSIS'in kontrol edildiği..."
        $nsis = "C:\Program Files (x86)\NSIS\makensis.exe"

        if (-not (Test-Path $nsis)) {
            $nsis = "C:\Program Files\NSIS\makensis.exe"
        }

        if (-not (Test-Path $nsis)) {
            Write-Error_ "NSIS bulunamadı!"
            Write-Warning_ "Lütfen https://nsis.sourceforge.io adresinden NSIS indirin"
            exit 1
        }

        $setupScript = Join-Path $PSScriptRoot "MKFiloServis-Setup.nsi"
        if (-not (Test-Path $setupScript)) {
            Write-Error_ "NSIS script bulunamadı: $setupScript"
            exit 1
        }

        $installerPath = $nsis
        Write-Success "✓ NSIS bulundu: $nsis"
    }
    default {
        Write-Error_ "Bilinmeyen installer türü: $InstallerType"
        exit 1
    }
}

# 4. Output Klasörünü Hazırla
Write-Title "[4/5] Output Klasörü Hazırlanıyor..."

$outputDir = Join-Path $PSScriptRoot "output\v$OutputVersion"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Success "✓ Output klasörü oluşturuldu: $outputDir"
} else {
    Write-Success "✓ Output klasörü mevcut: $outputDir"
}

# 5. İnstaller Oluştur
Write-Title "[5/5] İnstaller Oluşturuluyor..."

try {
    if ($InstallerType -eq "inno") {
        Write-Info "Inno Setup çalıştırılıyor..."
        & $installerPath $setupScript
    } else {
        Write-Info "NSIS çalıştırılıyor..."
        & $installerPath $setupScript
    }

    if ($LASTEXITCODE -eq 0) {
        $exePath = Join-Path $outputDir "MKFiloServis-$OutputVersion-Setup.exe"
        if (Test-Path $exePath) {
            $size = (Get-Item $exePath).Length / 1MB
            Write-Success "✓ İnstaller başarıyla oluşturuldu!"
            Write-Success "  Yol: $exePath"
            Write-Success "  Boyut: $([Math]::Round($size, 2)) MB"
        } else {
            Write-Warning_ "İnstaller dosyası beklenen yerde bulunamadı"
        }
    } else {
        Write-Error_ "İnstaller oluşturma başarısız oldu (Exit Code: $LASTEXITCODE)"
        exit 1
    }
}
catch {
    Write-Error_ "Hata oluştu: $_"
    exit 1
}

Write-Title "╔════════════════════════════════════════════════════════════╗"
Write-Success "║              ✓ İnstaller Başarıyla Oluşturuldu!          ║"
Write-Title "╚════════════════════════════════════════════════════════════╝"

Write-Host ""
Write-Host "Sonraki Adımlar:" -ForegroundColor Yellow
Write-Host "1. İnstalleri test edin: $exePath /?" -ForegroundColor White
Write-Host "2. Kurulum dosyasını dağıtın" -ForegroundColor White
Write-Host "3. Kullanıcılarınıza www.github.com/karamur/MKFiloServis-MultiDb adresini verin" -ForegroundColor White
Write-Host ""

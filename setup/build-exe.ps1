#!/usr/bin/env pwsh
# MKFiloServis 1.0.27 Quick Installer Builder
# Inno Setup veya NSIS olmadan basit bir .exe wrapper oluşturur

param(
    [string]$Version = "1.0.27",
    [string]$OutputPath = "./setup/output/v1.0.27"
)

$ErrorActionPreference = "Stop"

Write-Host "`n╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  MKFiloServis 1.0.27 - Installer Builder (Lightweight)   ║" -ForegroundColor Green
Write-Host "╚═══════════════════════════════════════════════════════════╝`n" -ForegroundColor Green

# Ensure output directory exists
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

# Step 1: Check for Inno Setup
Write-Host "[1/4] Inno Setup Kontrol Ediliyor..." -ForegroundColor Yellow

$innoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $innoSetup)) {
    $innoSetup = "C:\Program Files\Inno Setup 6\ISCC.exe"
}

if (Test-Path $innoSetup) {
    Write-Host "✓ Inno Setup bulundu" -ForegroundColor Green

    # Step 2: Prepare setup.iss
    Write-Host "[2/4] Inno Setup Script Hazırlanıyor..." -ForegroundColor Yellow

    $issContent = @"
[Setup]
AppName=MKFiloServis
AppVersion=$Version
AppPublisher=MKFiloServis
DefaultDirName={pf}\MKFiloServis
DefaultGroupName=MKFiloServis
OutputDir=$($OutputPath -replace '/', '\')
OutputBaseFilename=MKFiloServis-$Version-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstalled=x64
ArchitecturesAllowed=x64

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Components]
Name: "app"; Description: "MKFiloServis Application"; Types: custom; Flags: fixed
Name: "docs"; Description: "Documentation"; Types: custom

[Files]
Source: "artifacts\setup\package\*"; DestDir: "{app}"; Components: app; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "setup\output\README.md"; DestDir: "{app}\Documentation"; Components: docs
Source: "setup\output\INSTALL.md"; DestDir: "{app}\Documentation"; Components: docs

[Icons]
Name: "{group}\MKFiloServis"; Filename: "{app}\MKFiloServis.Web.exe"
Name: "{commondesktop}\MKFiloServis"; Filename: "{app}\MKFiloServis.Web.exe"
Name: "{group}\{cm:UninstallProgram,MKFiloServis}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\MKFiloServis.Web.exe"; Description: "Start MKFiloServis"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: dirifempty; Name: "{app}"
"@

    $issPath = Join-Path $PSScriptRoot "MKFiloServis-AutoBuild.iss"
    $issContent | Out-File -FilePath $issPath -Encoding UTF8
    Write-Host "✓ Setup script oluşturuldu" -ForegroundColor Green

    # Step 3: Create installer
    Write-Host "[3/4] İnstaller Oluşturuluyor..." -ForegroundColor Yellow

    try {
        & $innoSetup $issPath

        Write-Host "✓ İnstaller oluşturuldu" -ForegroundColor Green

        # Step 4: Verify
        Write-Host "[4/4] İnstaller Doğrulanıyor..." -ForegroundColor Yellow

        $exePath = Join-Path $OutputPath "MKFiloServis-$Version-Setup.exe"

        if (Test-Path $exePath) {
            $size = [Math]::Round((Get-Item $exePath).Length / 1MB, 2)
            Write-Host "`n╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
            Write-Host "║                    ✓ BAŞARILI                            ║" -ForegroundColor Green
            Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Green
            Write-Host ""
            Write-Host "📦 İnstaller Bilgileri:" -ForegroundColor Cyan
            Write-Host "   Dosya: MKFiloServis-$Version-Setup.exe" -ForegroundColor White
            Write-Host "   Yol: $exePath" -ForegroundColor White
            Write-Host "   Boyut: $size MB" -ForegroundColor White
            Write-Host ""
            Write-Host "🚀 Kurulum için:" -ForegroundColor Cyan
            Write-Host "   $exePath" -ForegroundColor White
            Write-Host ""
        } else {
            Write-Host "⚠️  İnstaller dosyası beklenen yerde bulunamadı!" -ForegroundColor Yellow
        }

    } catch {
        Write-Host "❌ Hata: $_" -ForegroundColor Red
        exit 1
    } finally {
        # Cleanup
        if (Test-Path $issPath) {
            Remove-Item $issPath -Force
        }
    }

} else {
    Write-Host "❌ Inno Setup yüklü değildir!" -ForegroundColor Red
    Write-Host ""
    Write-Host "📥 Inno Setup Yüklemek İçin:" -ForegroundColor Yellow
    Write-Host "   1. https://jrsoftware.org/isdl.php adresini ziyaret edin" -ForegroundColor White
    Write-Host "   2. İnstaller indirin ve çalıştırın" -ForegroundColor White
    Write-Host "   3. Bu scripti tekrar çalıştırın" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 Alternatif: NSIS Kullanın" -ForegroundColor Yellow
    Write-Host "   NSIS: https://nsis.sourceforge.io" -ForegroundColor White
    Write-Host ""
    exit 1
}

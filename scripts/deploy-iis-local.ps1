<#
.SYNOPSIS
    MKFiloServis - IIS yerel test sunucusuna deploy scripti.
    Git pre-push hook tarafindan otomatik cagirilir.

.DESCRIPTION
    1) Release publish yapar
    2) IIS'i durdurur
    3) C:\inetpub\wwwroot\UstunFilo dizinine kopyalar
    4) IIS'i baslatir

.PARAMETER SkipPublish
    dotnet publish adimini atlar (zaten publish yapildiysa).
#>
[CmdletBinding()]
param(
    [switch] $SkipPublish
)

$ErrorActionPreference = 'Stop'

# --- Admin yetkisi kontrolu; yoksa yeniden baslat ---
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Admin yetkisi gerekli, yeniden baslatiliyor..." -ForegroundColor Yellow
    $startCmd = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $PSCommandPath)
    if ($SkipPublish) { $startCmd += "-SkipPublish" }
    Start-Process powershell -ArgumentList $startCmd -Verb RunAs -Wait
    exit 0
}

$RepoRoot   = Split-Path $PSScriptRoot -Parent
$WebProject = Join-Path $RepoRoot "MKFiloServis.Web\MKFiloServis.Web.csproj"
$PublishDir = Join-Path $RepoRoot "MKFiloServis.Web\bin\publish"
$IISTarget  = "C:\inetpub\wwwroot\UstunFilo"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MKFiloServis - IIS Lokal Deploy" -ForegroundColor Cyan
Write-Host "  $(Get-Date -Format 'dd.MM.yyyy HH:mm:ss')" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1) Publish
if (-not $SkipPublish) {
    Write-Host "[1/4] dotnet publish (Release)..." -ForegroundColor Yellow
    dotnet publish $WebProject -c Release -o $PublishDir --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish basarisiz oldu."; exit 1 }
    Write-Host "      Publish tamam." -ForegroundColor Green
} else {
    Write-Host "[1/4] Publish atlandi (-SkipPublish)." -ForegroundColor DarkGray
}

# 2) IIS durdur
Write-Host "[2/4] IIS durduruluyor..." -ForegroundColor Yellow
iisreset /stop 2>&1 | Out-Null
Write-Host "      IIS durduruldu." -ForegroundColor Green

# 3) Kopyala
Write-Host "[3/4] $IISTarget dizinine kopyalaniyor..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $IISTarget -Force | Out-Null
robocopy $PublishDir $IISTarget /E /IS /IT /NFL /NDL /NJH /NJS | Out-Null
Write-Host "      Kopyalama tamam." -ForegroundColor Green

# 4) IIS baslatir
Write-Host "[4/4] IIS baslatiliyor..." -ForegroundColor Yellow
iisreset /start 2>&1 | Out-Null
Write-Host "      IIS baslatildi." -ForegroundColor Green

Write-Host ""
Write-Host "Deploy tamamlandi! -> $IISTarget" -ForegroundColor Green
Write-Host ""

<#
.SYNOPSIS
    KOAFiloServis-MultiDb kurulum paketleri uretir.

.DESCRIPTION
    1) KOAFiloServis.Web           -> publish (framework-dependent, IIS)
    2) KOAFiloServis.LisansDesktop -> publish (self-contained, win-x64, SingleFile)
    3) KOAFiloServis.DataSync      -> publish (self-contained, win-x64, SingleFile)
    4) Inno Setup - Setup.iss      -> KOAFiloServisKurulum-<version>.exe (tam paket)
    5) Inno Setup - GuncelleSetup.iss-> KOAFiloServisGuncelle-<version>.exe
    6) Inno Setup - MusteriSetup.iss-> KOAFiloServisKurulumMusteri-<version>.exe
    7) Inno Setup - LisansSetup.iss-> KOALisansArac-<version>.exe

.PARAMETER Version
    Paket versiyon numarasi. Varsayilan 1.0.25

.PARAMETER SkipPublish
    Publish atlanir, sadece Inno Setup calistirilir.

.PARAMETER LisansOnly
    Sadece LisansDesktop publish + LisansSetup.iss EXE uretir.

.EXAMPLE
    .\build.ps1 -Version 1.0.22
    .\build.ps1 -Version 1.0.22 -LisansOnly
#>
[CmdletBinding()]
param(
    [string] $Version = '1.0.25',
    [switch] $SkipPublish,
    [switch] $LisansOnly
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$Root      = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot  = Split-Path -Parent $Root
$Payload   = Join-Path $Root 'payload'
$Output    = Join-Path $Root "output\v$Version"

$Web       = Join-Path $RepoRoot 'KOAFiloServis.Web\KOAFiloServis.Web.csproj'
$Lisans    = Join-Path $RepoRoot 'KOAFiloServis.LisansDesktop\KOAFiloServis.LisansDesktop.csproj'
$DataSync  = Join-Path $RepoRoot 'KOAFiloServis.DataSync\KOAFiloServis.DataSync.csproj'

$IsccExe = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $IsccExe) {
    throw "Inno Setup (ISCC.exe) bulunamadi. 'winget install JRSoftware.InnoSetup' ile kurun."
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "KOAFiloServis-MultiDb Paket Uretim - v$Version" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Kaynak  : $RepoRoot"
Write-Host "Payload : $Payload"
Write-Host "Output  : $Output"
Write-Host "ISCC    : $IsccExe"
Write-Host ""

if (-not $SkipPublish) {
    if ($LisansOnly) {
        $lPayload = Join-Path $Payload 'LisansDesktop'
        if (Test-Path $lPayload) { Remove-Item $lPayload -Recurse -Force }
        New-Item -ItemType Directory -Force $lPayload, $Output | Out-Null
    } else {
        if (Test-Path $Payload) { Remove-Item $Payload -Recurse -Force }
        New-Item -ItemType Directory -Force $Payload, $Output | Out-Null
    }

    if (-not $LisansOnly) {
        Write-Host "[1/5] Web publish..." -ForegroundColor Green
        dotnet publish $Web -c Release -o "$Payload\Web" /p:Version=$Version /p:UseAppHost=true --nologo | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Web publish basarisiz." }

        $webConfigPath = Join-Path $Payload 'Web\web.config'
        if (Test-Path $webConfigPath) {
            $wc = Get-Content $webConfigPath -Raw
            $wc2 = $wc -replace 'stdoutLogEnabled="false"', 'stdoutLogEnabled="true"'
            if ($wc -ne $wc2) {
                Set-Content -Path $webConfigPath -Value $wc2 -Encoding UTF8 -NoNewline
                Write-Host "       web.config: stdoutLogEnabled=true" -ForegroundColor DarkGray
            }
        }

        $dbSettingsSrc = Join-Path $RepoRoot 'KOAFiloServis.Web\dbsettings.json'
        if (Test-Path $dbSettingsSrc) {
            Copy-Item $dbSettingsSrc "$Payload\Web\dbsettings.json" -Force
            Write-Host "       dbsettings.json payload'a kopyalandi" -ForegroundColor DarkGray
        }
    }

    Write-Host "[2/5] LisansDesktop publish..." -ForegroundColor Green
    dotnet publish $Lisans -c Release -r win-x64 --self-contained `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        /p:Version=$Version -o "$Payload\LisansDesktop" --nologo | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "LisansDesktop publish basarisiz." }

    if (-not $LisansOnly) {
        Write-Host "[3/5] DataSync publish..." -ForegroundColor Green
        dotnet publish $DataSync -c Release -r win-x64 --self-contained `
            -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
            /p:Version=$Version -o "$Payload\DataSync" --nologo | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "DataSync publish basarisiz." }
    }
} else {
    Write-Host "[PUBLISH ATLANDI] -SkipPublish" -ForegroundColor Yellow
}

New-Item -ItemType Directory -Force $Output | Out-Null
Write-Host "Output klasoru : $Output" -ForegroundColor DarkGray

if (-not $LisansOnly) {
    Write-Host "[4/7] Inno Setup - Ana paket..." -ForegroundColor Green
    & $IsccExe "/DMyAppVersion=$Version" "/DOutputDir=$Output" "/DMyInstallDirBase=C:\KOAFiloServis_ustun" "/DMyBackupDirBase=C:\KOAFiloServis_yedekleme_ustun" (Join-Path $Root 'Setup.iss')
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup (Setup.iss) basarisiz." }

    Write-Host "[5/7] Inno Setup - Guncelleme paketi..." -ForegroundColor Green
    & $IsccExe "/DMyAppVersion=$Version" "/DOutputDir=$Output" (Join-Path $Root 'GuncelleSetup.iss')
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup (GuncelleSetup.iss) basarisiz." }

    Write-Host "[6/7] Inno Setup - Musteri paketi..." -ForegroundColor Green
    & $IsccExe "/DMyAppVersion=$Version" "/DOutputDir=$Output" "/DMyInstallDirBase=C:\KOAFiloServis_ustun" (Join-Path $Root 'MusteriSetup.iss')
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup (MusteriSetup.iss) basarisiz." }
}

Write-Host "[7/7] Inno Setup - Lisans araci..." -ForegroundColor Green
& $IsccExe "/DLisansAppVersion=$Version" "/DOutputDir=$Output" (Join-Path $Root 'LisansSetup.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup (LisansSetup.iss) basarisiz." }

$sonuclar = @()
if (-not $LisansOnly) {
    $p1 = Join-Path $Output "KOAFiloServisKurulum-$Version.exe"
    if (Test-Path $p1) { $s = [math]::Round((Get-Item $p1).Length/1MB,2); $sonuclar += "  Ana paket : $p1 ($s MB)" }
    $p2 = Join-Path $Output "KOAFiloServisGuncelle-$Version.exe"
    if (Test-Path $p2) { $s = [math]::Round((Get-Item $p2).Length/1MB,2); $sonuclar += "  Guncelleme: $p2 ($s MB)" }
    $p3 = Join-Path $Output "KOAFiloServisKurulumMusteri-$Version.exe"
    if (Test-Path $p3) { $s = [math]::Round((Get-Item $p3).Length/1MB,2); $sonuclar += "  Musteri    : $p3 ($s MB)" }
}
$p4 = Join-Path $Output "KOALisansArac-$Version.exe"
if (Test-Path $p4) { $s = [math]::Round((Get-Item $p4).Length/1MB,2); $sonuclar += "  Lisans     : $p4 ($s MB)" }

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "BASARILI!" -ForegroundColor Green
$sonuclar | ForEach-Object { Write-Host $_ -ForegroundColor Green }
Write-Host "==================================================" -ForegroundColor Cyan

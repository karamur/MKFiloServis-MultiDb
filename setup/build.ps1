<#
.SYNOPSIS
    MKFiloServis-MultiDb kurulum paketleri uretir.

.DESCRIPTION
    1) MKFiloServis.Web           -> publish (framework-dependent, IIS)
    2) MKFiloServis.DataSync      -> publish (self-contained, win-x64, SingleFile)
    4) Inno Setup - Setup.iss      -> MKFiloServisKurulum-<version>.exe (tam paket)
    5) Inno Setup - GuncelleSetup.iss-> MKFiloServisGuncelle-<version>.exe
    Eski MusteriSetup.iss varyanti ACL korumasi eksik oldugu icin satis paketinde uretilmez.
    Internal license utility is built only with -LisansOnly or -IncludeInternalLicenseTool.

.PARAMETER Version
    Paket versiyon numarasi. Varsayilan 1.0.25

.PARAMETER SkipPublish
    Publish atlanir, sadece Inno Setup calistirilir.

.PARAMETER LisansOnly
    Sadece dahili LisansDesktop publish + LisansSetup.iss EXE uretir.

.PARAMETER IncludeInternalLicenseTool
    Dahili lisans aracini ayrıca üretir. Müşteri kurulumlarına eklemez.

.EXAMPLE
    .\build.ps1 -Version 1.0.22
    .\build.ps1 -Version 1.0.22 -LisansOnly
#>
[CmdletBinding()]
param(
    [string] $Version = '1.0.37',
    [switch] $SkipPublish,
    [switch] $LisansOnly,
    [switch] $IncludeInternalLicenseTool
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$Root      = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot  = Split-Path -Parent $Root
$Payload   = Join-Path $Root 'payload'
$Output    = Join-Path $Root "output\v$Version"
$BuildLicenseTool = $LisansOnly -or $IncludeInternalLicenseTool
$LegacyCustomerPackage = Join-Path $Output "MKFiloServisKurulumMusteri-$Version.exe"

if (-not $LisansOnly -and (Test-Path -LiteralPath $LegacyCustomerPackage)) {
    throw "Eski MusteriSetup paketi ayni surum cikti klasorunde bulundu; ACL korumasi olmadigi icin yayin klasorunu temizleyip yeni surumle tekrar olusturun: $LegacyCustomerPackage"
}

if ($LisansOnly -and $IncludeInternalLicenseTool) {
    throw "-LisansOnly ile -IncludeInternalLicenseTool birlikte kullanilamaz."
}
if ($SkipPublish -and $BuildLicenseTool -and -not (Test-Path (Join-Path $Payload 'LisansDesktop'))) {
    throw "Dahili lisans araci publish cikisi yok. -SkipPublish kaldirip yeniden deneyin."
}

$expectedPayload = [System.IO.Path]::GetFullPath((Join-Path $Root 'payload'))
if (-not [System.IO.Path]::GetFullPath($Payload).Equals($expectedPayload, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Payload yolu setup klasoru disinda: $Payload"
}

$Web       = Join-Path $RepoRoot 'MKFiloServis.Web\MKFiloServis.Web.csproj'
$Lisans    = Join-Path $RepoRoot 'MKFiloServis.LisansDesktop\MKFiloServis.LisansDesktop.csproj'
$DataSync  = Join-Path $RepoRoot 'MKFiloServis.DataSync\MKFiloServis.DataSync.csproj'

$IsccExe = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $IsccExe) {
    throw "Inno Setup (ISCC.exe) bulunamadi. 'winget install JRSoftware.InnoSetup' ile kurun."
}

# ASP.NET Core Hosting Bundle (pakete gomulur; hedef makinede internet gerekmez)
$HostingBundleUrl  = 'https://aka.ms/dotnet/10.0/dotnet-hosting-win.exe'
$RedistDir         = Join-Path $Root 'redist'
$HostingBundleExe  = Join-Path $RedistDir 'dotnet-hosting-10.0-win.exe'

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "MKFiloServis-MultiDb Paket Uretim - v$Version" -ForegroundColor Cyan
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

        foreach ($relativePath in @('Tests', 'App_Data', 'Backups', 'artifacts', '.artifacts', 'wwwroot\uploads')) {
            if (Test-Path (Join-Path $Payload "Web\$relativePath")) {
                throw "Yerel veri/test klasoru publish ciktisinda bulundu: $relativePath"
            }
        }

        $webConfigPath = Join-Path $Payload 'Web\web.config'
        if (Test-Path $webConfigPath) {
            # 500.37 fix: startupTimeLimit=600 + stdout log ayarlarini garanti et
            [xml]$wcXml = Get-Content $webConfigPath -Raw
            $ancm = $wcXml.SelectSingleNode('//aspNetCore')
            if ($ancm) {
                $ancm.SetAttribute('startupTimeLimit', '600')
                $ancm.SetAttribute('requestTimeout', '00:10:00')
                $ancm.SetAttribute('stdoutLogEnabled', 'true')
                $ancm.SetAttribute('stdoutLogFile', '.\logs\stdout')
                $wcXml.Save($webConfigPath)
                Write-Host "       web.config: startupTimeLimit=600, stdoutLogEnabled=true" -ForegroundColor DarkGray
            }
        }

        # JWT özel anahtarını ortak kurulum paketine koyma. Her müşteri kurulumunda
        # Jwt__Secret dağıtım ortamının gizli ayar deposundan ayrıca sağlanmalıdır.
    }

    if ($BuildLicenseTool) {
        Write-Host "[2/5] Dahili LisansDesktop publish..." -ForegroundColor Green
        dotnet publish $Lisans -c Release -r win-x64 --self-contained `
            -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
            /p:Version=$Version -o "$Payload\LisansDesktop" --nologo | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "LisansDesktop publish basarisiz." }
    } elseif (Test-Path (Join-Path $Payload 'LisansDesktop')) {
        Remove-Item (Join-Path $Payload 'LisansDesktop') -Recurse -Force
        Write-Host "Eski LisansDesktop payload'i temizlendi; musteri paketine alinmayacak." -ForegroundColor DarkGray
    }

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

if (-not $LisansOnly) {
    $webPayload = Join-Path $Payload 'Web'
    if (-not (Test-Path $webPayload -PathType Container)) {
        throw 'Web payload bulunamadı; kurulum paketi oluşturulamaz.'
    }

    # -SkipPublish kullanıldığında da eski payload içindeki yerel ayarları paketleme.
    foreach ($fileName in @('dbsettings.json', 'portalsettings.json', 'backup_settings.json', 'appsettings.Production.json', 'cookies.txt')) {
        if (Test-Path (Join-Path $webPayload $fileName)) {
            throw "Yerel ayar/oturum dosyası Web payload'ında bulundu: $fileName"
        }
    }
    $environmentConfigs = @(Get-ChildItem -LiteralPath $webPayload -Filter 'appsettings.*.json' -File)
    if ($environmentConfigs.Count -gt 0) {
        throw "Ortama özel appsettings dosyası Web payload'ında bulundu: $($environmentConfigs.Name -join ', ')"
    }
}

if (-not $LisansOnly) {
    # Hosting Bundle'i bir kez indir (redist icinde onbellekte tutulur), payload'a kopyala.
    if (-not (Test-Path $HostingBundleExe)) {
        New-Item -ItemType Directory -Force $RedistDir | Out-Null
        Write-Host "Hosting Bundle indiriliyor: $HostingBundleUrl" -ForegroundColor Green
        Invoke-WebRequest -Uri $HostingBundleUrl -OutFile $HostingBundleExe -UseBasicParsing
    } else {
        Write-Host "Hosting Bundle onbellekten kullaniliyor: $HostingBundleExe" -ForegroundColor DarkGray
    }

    $payloadRedist = Join-Path $Payload 'redist'
    New-Item -ItemType Directory -Force $payloadRedist | Out-Null
    Copy-Item $HostingBundleExe (Join-Path $payloadRedist 'dotnet-hosting-win.exe') -Force
    Write-Host "Hosting Bundle payload'a eklendi." -ForegroundColor DarkGray
}

New-Item -ItemType Directory -Force $Output | Out-Null
Write-Host "Output klasoru : $Output" -ForegroundColor DarkGray

if (-not $LisansOnly) {
    Write-Host "[4/7] Inno Setup - Ana paket..." -ForegroundColor Green
    & $IsccExe "/DMyAppVersion=$Version" "/DOutputDir=$Output" "/DMyInstallDirBase=C:\MKFiloServis_ustun" "/DMyBackupDirBase=C:\MKFiloServis_yedekleme_ustun" (Join-Path $Root 'Setup.iss')
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup (Setup.iss) basarisiz." }

    Write-Host "[5/7] Inno Setup - Guncelleme paketi..." -ForegroundColor Green
    & $IsccExe "/DMyAppVersion=$Version" "/DOutputDir=$Output" (Join-Path $Root 'GuncelleSetup.iss')
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup (GuncelleSetup.iss) basarisiz." }

}

if ($BuildLicenseTool) {
    Write-Host "[Dahili] Inno Setup - Lisans araci..." -ForegroundColor Green
    & $IsccExe "/DLisansAppVersion=$Version" "/DOutputDir=$Output" (Join-Path $Root 'LisansSetup.iss')
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup (LisansSetup.iss) basarisiz." }
}

$sonuclar = @()
if (-not $LisansOnly) {
    $p1 = Join-Path $Output "MKFiloServisKurulum-$Version.exe"
    if (Test-Path $p1) { $s = [math]::Round((Get-Item $p1).Length/1MB,2); $sonuclar += "  Ana paket : $p1 ($s MB)" }
    $p2 = Join-Path $Output "MKFiloServisGuncelle-$Version.exe"
    if (Test-Path $p2) { $s = [math]::Round((Get-Item $p2).Length/1MB,2); $sonuclar += "  Guncelleme: $p2 ($s MB)" }
}
if ($BuildLicenseTool) {
    $p4 = Join-Path $Output "MKLisansArac-$Version.exe"
    if (Test-Path $p4) { $s = [math]::Round((Get-Item $p4).Length/1MB,2); $sonuclar += "  Dahili lisans araci: $p4 ($s MB)" }
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "BASARILI!" -ForegroundColor Green
$sonuclar | ForEach-Object { Write-Host $_ -ForegroundColor Green }
Write-Host "==================================================" -ForegroundColor Cyan

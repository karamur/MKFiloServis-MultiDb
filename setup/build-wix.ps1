<#
.SYNOPSIS
    MKFiloServis-MultiDb kurulum paketlerini WiX Toolset v6 + Burn ile uretir.

.DESCRIPTION
    1) MKFiloServis.Web          -> publish (framework-dependent, IIS)
    2) MKFiloServis.DataSync     -> publish (self-contained, win-x64, SingleFile)
    3) MKFiloServis.LisansDesktop-> publish (self-contained, win-x64, SingleFile)
    4) WiX MSI  : MKFiloServis.Web-<ver>.msi   (IIS sitesi, yalnizca MEVCUT PostgreSQL'e baglanti)
    5) WiX MSI  : MKLisansArac-<ver>.msi       (kullanici bazli, yonetici gerekmez)
    6) Burn     : MKFiloServisKurulum-<ver>.exe (Hosting Bundle 10.0 + Web MSI)
    7) Burn     : MKFiloServisGuncelle-<ver>.exe (yalnizca Web MSI; mevcut kurulum sart)

    Gereksinimler: dotnet SDK 10, `dotnet tool install --global wix --version 6.0.2` ve
    `wix extension add -g WixToolset.UI.wixext/6.0.2 WixToolset.Util.wixext/6.0.2 WixToolset.BootstrapperApplications.wixext/6.0.2`
    (betik eksik uzantilari kendisi ekler).

.PARAMETER Version
    Paket surumu. Varsayilan 1.0.48

.PARAMETER SkipPublish
    Publish adimlarini atlar; setup\payload icindeki mevcut ciktilar kullanilir.

.PARAMETER LisansOnly
    Yalnizca LisansDesktop publish + MKLisansArac MSI uretir.

.PARAMETER SkipLisans
    LisansDesktop paketini uretmez.

.EXAMPLE
    .\build-wix.ps1 -Version 1.0.48
    .\build-wix.ps1 -Version 1.0.48 -SkipPublish
    .\build-wix.ps1 -Version 1.0.48 -LisansOnly
#>
[CmdletBinding()]
param(
    [string] $Version = '1.0.48',
    [switch] $SkipPublish,
    [switch] $LisansOnly,
    [switch] $SkipLisans
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

if ($LisansOnly -and $SkipLisans) { throw '-LisansOnly ile -SkipLisans birlikte kullanilamaz.' }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Surum x.y.z bicimde olmali: $Version" }

$Root     = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Split-Path -Parent $Root
$Payload  = Join-Path $Root 'payload'
$Output   = Join-Path $Root "output\v$Version"
$WixDir   = Join-Path $Root 'wix'
$ObjDir   = Join-Path $WixDir 'obj'
$RedistDir = Join-Path $Root 'redist'
$HostingBundleUrl = 'https://aka.ms/dotnet/10.0/dotnet-hosting-win.exe'
$HostingBundleExe = Join-Path $RedistDir 'dotnet-hosting-10.0-win.exe'

$Web      = Join-Path $RepoRoot 'MKFiloServis.Web\MKFiloServis.Web.csproj'
$DataSync = Join-Path $RepoRoot 'MKFiloServis.DataSync\MKFiloServis.DataSync.csproj'
$Lisans   = Join-Path $RepoRoot 'MKFiloServis.LisansDesktop\MKFiloServis.LisansDesktop.csproj'

$BuildWeb = -not $LisansOnly
$BuildLisans = -not $SkipLisans

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "MKFiloServis WiX/Burn Paket Uretim - v$Version" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Kaynak  : $RepoRoot"
Write-Host "Payload : $Payload"
Write-Host "Output  : $Output"

# --- WiX araci ve uzantilari -------------------------------------------------
$wix = Get-Command wix -ErrorAction SilentlyContinue
if (-not $wix) {
    throw "wix CLI bulunamadi. Kurmak icin: dotnet tool install --global wix --version 6.0.2"
}
$wixVersion = (& wix --version) 2>$null
Write-Host "WiX     : $wixVersion"
if ($wixVersion -notmatch '^6\.') {
    throw "WiX 6.x gerekli (bulunan: $wixVersion). WiX 7 OSMF EULA ister; 'dotnet tool update --global wix --version 6.0.2' calistirin."
}
$installedExtensions = (& wix extension list -g) 2>$null | Out-String
foreach ($ext in @('WixToolset.UI.wixext', 'WixToolset.Util.wixext', 'WixToolset.BootstrapperApplications.wixext')) {
    if ($installedExtensions -notmatch [regex]::Escape($ext)) {
        Write-Host "WiX uzantisi ekleniyor: $ext" -ForegroundColor Yellow
        & wix extension add -g "$ext/6.0.2" | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "WiX uzantisi eklenemedi: $ext" }
    }
}

function Invoke-Wix([string[]] $Arguments, [string] $Step) {
    Write-Host "  wix $($Arguments[0]) ..." -ForegroundColor DarkGray
    & wix @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$Step basarisiz (wix cikis kodu $LASTEXITCODE)." }
}

# --- Publish -----------------------------------------------------------------
New-Item -ItemType Directory -Force $Output, $ObjDir | Out-Null
if (-not $SkipPublish) {
    if ($BuildWeb) {
        foreach ($sub in @('Web', 'DataSync')) {
            $p = Join-Path $Payload $sub
            if (Test-Path $p) { Remove-Item $p -Recurse -Force }
        }
        Write-Host "[1/7] Web publish (v$Version)..." -ForegroundColor Green
        dotnet publish $Web -c Release -o "$Payload\Web" /p:Version=$Version /p:UseAppHost=true --nologo | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Web publish basarisiz.' }

        foreach ($relativePath in @('Tests', 'App_Data', 'Backups', 'artifacts', '.artifacts', 'setup\output', 'wwwroot\uploads')) {
            if (Test-Path (Join-Path $Payload "Web\$relativePath")) { throw "Yerel veri/test klasoru publish ciktisinda bulundu: $relativePath" }
        }

        $webConfigPath = Join-Path $Payload 'Web\web.config'
        if (Test-Path $webConfigPath) {
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

        Write-Host "[2/7] DataSync publish..." -ForegroundColor Green
        dotnet publish $DataSync -c Release -r win-x64 --self-contained `
            -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
            /p:Version=$Version -o "$Payload\DataSync" --nologo | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'DataSync publish basarisiz.' }
    }

    if ($BuildLisans) {
        $lPayload = Join-Path $Payload 'LisansDesktop'
        if (Test-Path $lPayload) { Remove-Item $lPayload -Recurse -Force }
        Write-Host "[3/7] LisansDesktop publish..." -ForegroundColor Green
        dotnet publish $Lisans -c Release -r win-x64 --self-contained `
            -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
            /p:Version=$Version -o $lPayload --nologo | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'LisansDesktop publish basarisiz.' }
    }
} else {
    Write-Host "[PUBLISH ATLANDI] -SkipPublish" -ForegroundColor Yellow
}

# --- Payload denetimleri -----------------------------------------------------
if ($BuildWeb) {
    $webPayload = Join-Path $Payload 'Web'
    if (-not (Test-Path $webPayload -PathType Container)) { throw 'Web payload bulunamadi.' }
    if (-not (Test-Path (Join-Path $Payload 'DataSync') -PathType Container)) { throw 'DataSync payload bulunamadi.' }
    foreach ($fileName in @('dbsettings.json', 'portalsettings.json', 'backup_settings.json', 'appsettings.Production.json', 'cookies.txt')) {
        if (Test-Path (Join-Path $webPayload $fileName)) { throw "Yerel ayar/oturum dosyasi Web payload'inda bulundu: $fileName" }
    }
    $environmentConfigs = @(Get-ChildItem -LiteralPath $webPayload -Filter 'appsettings.*.json' -File)
    if ($environmentConfigs.Count -gt 0) { throw "Ortama ozel appsettings dosyasi Web payload'inda bulundu: $($environmentConfigs.Name -join ', ')" }

    $webDll = Join-Path $webPayload 'MKFiloServis.Web.dll'
    $webAsmVersion = [Reflection.AssemblyName]::GetAssemblyName($webDll).Version
    if ($webAsmVersion.ToString(3) -ne $Version) {
        throw "Web assembly surumu ($webAsmVersion) paket surumuyle ($Version) eslesmiyor. Publish'i /p:Version ile yeniden calistirin."
    }
    Write-Host "Web assembly surumu: $webAsmVersion" -ForegroundColor DarkGray

    if (-not (Test-Path $HostingBundleExe)) {
        New-Item -ItemType Directory -Force $RedistDir | Out-Null
        Write-Host "Hosting Bundle indiriliyor: $HostingBundleUrl" -ForegroundColor Green
        Invoke-WebRequest -Uri $HostingBundleUrl -OutFile $HostingBundleExe -UseBasicParsing
    }
    $hostingVersion = (Get-Item $HostingBundleExe).VersionInfo.ProductVersion
    Write-Host "Hosting Bundle: $hostingVersion ($HostingBundleExe)" -ForegroundColor DarkGray
    # Bundle.wxs icindeki AspNetCoreVersion, redist paketinin ilk uc bileseniyle eslesmeli.
    $hostingShort = ($hostingVersion -split '\.')[0..2] -join '.'
    $bundleSource = Get-Content (Join-Path $WixDir 'MKFiloServis.Bundle.Shared\Bundle.wxs') -Raw
    if ($bundleSource -notmatch [regex]::Escape("AspNetCoreVersion = `"$hostingShort`"")) {
        throw "Bundle.wxs icindeki AspNetCoreVersion, redist Hosting Bundle surumu ($hostingShort) ile eslesmiyor."
    }
}
if ($BuildLisans -and -not (Test-Path (Join-Path $Payload 'LisansDesktop\MKFiloServisLisans.exe'))) {
    throw 'LisansDesktop payload bulunamadi (MKFiloServisLisans.exe).'
}

# --- WiX derlemeleri ---------------------------------------------------------
$common = @('-arch', 'x64', '-culture', 'tr-TR', '-d', "ProductVersion=$Version", '-d', "PayloadDir=$Payload")

if ($BuildWeb) {
    $webMsi = Join-Path $Output "MKFiloServis.Web-$Version.msi"
    Write-Host "[4/7] WiX MSI - Web..." -ForegroundColor Green
    Invoke-Wix (@('build') + $common + @(
        '-ext', 'WixToolset.UI.wixext', '-ext', 'WixToolset.Util.wixext',
        '-d', "ScriptsDir=$WixDir\scripts", '-d', "SetupScriptsDir=$Root\scripts",
        '-loc', "$WixDir\MKFiloServis.Web.Setup\Package.tr-TR.wxl",
        '-intermediateFolder', "$ObjDir\Web",
        '-o', $webMsi,
        "$WixDir\MKFiloServis.Web.Setup\Package.wxs", "$WixDir\MKFiloServis.Web.Setup\WixUI_MKFilo.wxs")) 'Web MSI derlemesi'
    Invoke-Wix @('msi', 'validate', '-intermediateFolder', "$ObjDir\WebVal", '-sice', 'ICE61', '-sice', 'ICE60', $webMsi) 'Web MSI ICE dogrulamasi'

    Write-Host "[5/7] Burn - Kurulum paketi (Hosting Bundle + Web MSI)..." -ForegroundColor Green
    $bundleCommon = @('-arch', 'x64', '-ext', 'WixToolset.BootstrapperApplications.wixext', '-ext', 'WixToolset.Util.wixext',
        '-d', "ProductVersion=$Version", '-d', "PayloadDir=$Payload", '-d', "RedistDir=$RedistDir", '-d', "MsiDir=$Output")
    Invoke-Wix (@('build') + $bundleCommon + @('-d', 'IncludeHosting=1', '-intermediateFolder', "$ObjDir\Kurulum",
        '-o', (Join-Path $Output "MKFiloServisKurulum-$Version.exe"), "$WixDir\MKFiloServis.Bundle.Shared\Bundle.wxs")) 'Kurulum bundle derlemesi'

    Write-Host "[6/7] Burn - Guncelleme paketi (yalnizca Web MSI)..." -ForegroundColor Green
    Invoke-Wix (@('build') + $bundleCommon + @('-d', 'IncludeHosting=0', '-intermediateFolder', "$ObjDir\Guncelle",
        '-o', (Join-Path $Output "MKFiloServisGuncelle-$Version.exe"), "$WixDir\MKFiloServis.Bundle.Shared\Bundle.wxs")) 'Guncelleme bundle derlemesi'
}

if ($BuildLisans) {
    $lisansMsi = Join-Path $Output "MKLisansArac-$Version.msi"
    Write-Host "[7/7] WiX MSI - Lisans araci (kullanici bazli)..." -ForegroundColor Green
    Invoke-Wix (@('build') + $common + @(
        '-ext', 'WixToolset.UI.wixext', '-ext', 'WixToolset.Util.wixext',
        '-loc', "$WixDir\MKFiloServis.Lisans.Setup\Package.tr-TR.wxl",
        '-intermediateFolder', "$ObjDir\Lisans",
        '-o', $lisansMsi,
        "$WixDir\MKFiloServis.Lisans.Setup\Package.wxs")) 'Lisans MSI derlemesi'
    Invoke-Wix @('msi', 'validate', '-intermediateFolder', "$ObjDir\LisansVal", '-sice', 'ICE61', '-sice', 'ICE60', '-sice', 'ICE38', '-sice', 'ICE64', '-sice', 'ICE91', $lisansMsi) 'Lisans MSI ICE dogrulamasi'
}

# Dogrulama yan urunleri temizlenir; obj klasoru git tarafindan izlenmez.
Remove-Item "$ObjDir\WebVal", "$ObjDir\LisansVal" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $Output '*.wixpdb') -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "TAMAMLANDI - v$Version" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Get-ChildItem $Output -File | Where-Object { $_.Extension -in '.msi', '.exe' } | ForEach-Object {
    '  {0,-40} {1,8:N1} MB' -f $_.Name, ($_.Length / 1MB)
}
Write-Host ""
Write-Host "Kurulum  : MKFiloServisKurulum-$Version.exe  -> IIS + Hosting Bundle + mevcut PostgreSQL'e baglanti"
Write-Host "Guncelle : MKFiloServisGuncelle-$Version.exe -> dosyalar yenilenir, dbsettings.json korunur"
Write-Host "Lisans   : MKLisansArac-$Version.msi         -> kullanici profiline, yonetici gerekmez"

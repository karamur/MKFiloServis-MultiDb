# =====================================================================
# MK Filo Servis - IIS paketi olusturma scripti
# Cikti (OutputDir altinda):
#   <OutputDir>\IIS\                                                    (publish ciktisi)
#   <OutputDir>\MKFiloServis_IIS_<Mode>_<Version>_<stamp>.zip          (kurulum zip)
#
# Modlar:
#   Update  : Guncelleme. Mevcut dbsettings.json korunur.
#   Install : Yeni kurulum. dbsettings.json paket icindekiyle DEGISTIRILIR,
#             SQLite ise eski DB silinir (yedek alindiktan sonra).
#   All     : Update + Install paketlerini tek seferde uretir.
#
# Kullanim:
#   pwsh .\scripts\paketle.ps1 -Mode Update  -Version 1.2.3
#   pwsh .\scripts\paketle.ps1 -Mode Install -Version 1.2.3
#   pwsh .\scripts\paketle.ps1 -Mode All     -Version 1.2.3
#   pwsh .\scripts\paketle.ps1 -Mode Update  -Version 1.2.3 -SkipBuild
# =====================================================================
param(
    [ValidateSet('Install','Update','All')]
    [string]$Mode          = 'Update',
    [Parameter(Mandatory=$true)]
    [string]$Version,
    [string]$Configuration = 'Release',
    [string]$Project       = 'MKFiloServis.Web\MKFiloServis.Web.csproj',
    [string]$OutputDir,
    [string]$PublishDir,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $Root (Join-Path 'setup\output' "v$Version")
}
if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $PublishDir = Join-Path $OutputDir 'IIS'
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

function Step($m) { Write-Host "[PAKET] $m" -ForegroundColor Cyan }

# Mode = All ise once Update (build dahil) sonra Install (-SkipBuild) olarak iki kez calistir
if ($Mode -eq 'All') {
    Step "MOD: All -> Update + Install paketleri uretilecek"

    # 1) Update (build dahil, kullanici SkipBuild verdiyse o da gecirilir)
    if ($SkipBuild) {
        & $PSCommandPath -Mode 'Update' -Version $Version -Configuration $Configuration -Project $Project `
            -OutputDir $OutputDir -PublishDir $PublishDir -SkipBuild
    } else {
        & $PSCommandPath -Mode 'Update' -Version $Version -Configuration $Configuration -Project $Project `
            -OutputDir $OutputDir -PublishDir $PublishDir
    }
    if ($LASTEXITCODE -ne 0) { throw "Update paketi olusturulamadi (ExitCode=$LASTEXITCODE)" }

    # 2) Install (publish hazir oldugu icin her zaman -SkipBuild)
    & $PSCommandPath -Mode 'Install' -Version $Version -Configuration $Configuration -Project $Project `
        -OutputDir $OutputDir -PublishDir $PublishDir -SkipBuild
    if ($LASTEXITCODE -ne 0) { throw "Install paketi olusturulamadi (ExitCode=$LASTEXITCODE)" }

    Write-Host ""
    Write-Host "--- TUM PAKETLER (v$Version) ---" -ForegroundColor Green
    Get-ChildItem $OutputDir -File | Where-Object { $_.Name -like 'MKFiloServis_IIS_*' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object Name, @{n='MB';e={[math]::Round($_.Length/1MB,2)}}, LastWriteTime |
        Format-Table -AutoSize
    return
}

$ModeLabel = if ($Mode -eq 'Install') { 'Kurulum' } else { 'Guncelleme' }
$ModeTitle = if ($Mode -eq 'Install') { "MK Filo Servis IIS YENI KURULUM v$Version" } else { "MK Filo Servis IIS GUNCELLEME v$Version" }
Step "MOD     : $Mode ($ModeLabel)"
Step "VERSION : $Version"
Step "OUTPUT  : $OutputDir"
Step "PUBLISH : $PublishDir"

# 1) Publish
if (-not $SkipBuild) {
    Step "Publish hazirlaniyor: $Project -> $PublishDir"
    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
    dotnet publish $Project -c $Configuration -o $PublishDir `
        --self-contained false `
        /p:EnvironmentName=Production `
        /p:UseAppHost=true `
        /p:Version=$Version `
        /p:AssemblyVersion=$Version.0 `
        /p:FileVersion=$Version.0 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish basarisiz oldu." }
}
if (-not (Test-Path (Join-Path $PublishDir 'web.config'))) {
    throw "web.config olusmadi, publish kontrol edin."
}

# version.json yaz (publish icine + OutputDir koklerine)
$Stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$VersionInfo = [pscustomobject]@{
    Version     = $Version
    BuildDate   = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    BuildNumber = $Stamp
    Framework   = 'net10.0'
    Description = "MK Filo Servis IIS paketi ($ModeLabel) - v$Version"
}
$VersionJson = $VersionInfo | ConvertTo-Json -Depth 5
$ArtifactsInPublish = Join-Path $PublishDir 'artifacts'
New-Item -ItemType Directory -Force -Path $ArtifactsInPublish | Out-Null
[System.IO.File]::WriteAllText((Join-Path $ArtifactsInPublish 'version.json'), $VersionJson, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $OutputDir 'version.json'),         $VersionJson, [System.Text.UTF8Encoding]::new($false))

# 2) Manuel kurulum icin zip
$ZipName = "MKFiloServis_IIS_${ModeLabel}_${Version}_$Stamp.zip"
$ZipPath = Join-Path $OutputDir $ZipName
Step "Zip olusturuluyor: $ZipPath"
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    (Resolve-Path $PublishDir).Path,
    $ZipPath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false
)

# Ozet
Write-Host ""
Write-Host "--- Olusturulan paketler ($ModeLabel v$Version) ---" -ForegroundColor Green
Get-ChildItem $OutputDir -File | Where-Object { $_.Name -like 'MKFiloServis_IIS_*' } |
    Sort-Object LastWriteTime -Descending |
    Select-Object Name, @{n='MB';e={[math]::Round($_.Length/1MB,2)}}, LastWriteTime |
    Format-Table -AutoSize

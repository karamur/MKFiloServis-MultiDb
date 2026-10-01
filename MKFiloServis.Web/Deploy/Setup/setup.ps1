param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputRoot = "./artifacts/setup",
    [string]$Version = "1.0.27",
    [switch]$UseDemoSqlite
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "..\\..\\MKFiloServis.Web.csproj"
$publishDir = Join-Path $OutputRoot "publish"
$packageDir = Join-Path $OutputRoot "package"

Write-Host "[1/5] Publish aliniyor..."
dotnet publish $project -c $Configuration -r $Runtime --self-contained false -o $publishDir

Write-Host "[2/5] Paket klasoru hazirlaniyor..."
if (Test-Path $packageDir) { Remove-Item $packageDir -Recurse -Force }
New-Item -ItemType Directory -Path $packageDir | Out-Null
Copy-Item "$publishDir\*" $packageDir -Recurse -Force

if ($UseDemoSqlite) {
    $dbSettingsPath = Join-Path $packageDir "dbsettings.json"
    if (-not (Test-Path $dbSettingsPath)) {
        throw "dbsettings.json publish paketinde bulunamadi; demo SQLite ayari yazilamadi."
    }

    $dbSettings = Get-Content $dbSettingsPath -Raw | ConvertFrom-Json
    $dbSettings.Provider = 1
    $dbSettings.CanonicalProvider = 2
    $dbSettings.Host = ""
    $dbSettings.Port = 0
    $dbSettings.DatabaseName = "App_Data/MKFiloServis_Demo.db"
    $dbSettings.Username = ""
    $dbSettings.Password = ""
    $dbSettings.UseIntegratedSecurity = $false
    $dbSettings.IsCanonicalProvider = $true
    $dbSettings.LastUpdated = [DateTime]::UtcNow.ToString("o")
    $dbSettings | ConvertTo-Json -Depth 20 | Set-Content $dbSettingsPath -Encoding UTF8
    New-Item -ItemType Directory -Path (Join-Path $packageDir "App_Data") -Force | Out-Null
    Write-Host "Demo SQLite ayarlandi: App_Data/MKFiloServis_Demo.db (yalniz paket klasorunde)."
}

Write-Host "[3/5] Kurulum scriptleri kopyalaniyor..."
$deployIis = Join-Path $PSScriptRoot "..\\IIS"
if (Test-Path $deployIis) {
    Copy-Item "$deployIis\\kur.ps1" $packageDir -Force -ErrorAction SilentlyContinue
    Copy-Item "$deployIis\\kur.bat" $packageDir -Force -ErrorAction SilentlyContinue
}

Write-Host "[4/5] Versiyon bilgisi ekleniyor..."
$versionFile = Join-Path $packageDir "version.txt"
$buildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
@"
MKFiloServis Setup Package
Version: $Version
Build Date: $buildDate
Configuration: $Configuration
Runtime: $Runtime
"@ | Set-Content $versionFile

Write-Host "[5/5] Tamamlandi."
Write-Host "Paket klasoru: $packageDir"
Write-Host "Versiyon: $Version"

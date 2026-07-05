param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputRoot = "./artifacts/setup",
    [string]$Version = "1.0.27"
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

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputRoot = "./artifacts/setup"
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "..\..\KOAFiloServis.Web.csproj"
$publishDir = Join-Path $OutputRoot "publish"
$packageDir = Join-Path $OutputRoot "package"

Write-Host "[1/4] Publish alınıyor..."
dotnet publish $project -c $Configuration -r $Runtime --self-contained false -o $publishDir

Write-Host "[2/4] Paket klasörü hazırlanıyor..."
if (Test-Path $packageDir) { Remove-Item $packageDir -Recurse -Force }
New-Item -ItemType Directory -Path $packageDir | Out-Null
Copy-Item "$publishDir\*" $packageDir -Recurse -Force

Write-Host "[3/4] Kurulum scriptleri kopyalanıyor..."
$deployIis = Join-Path $PSScriptRoot "..\IIS"
if (Test-Path $deployIis) {
    Copy-Item "$deployIis\kur.ps1" $packageDir -Force -ErrorAction SilentlyContinue
    Copy-Item "$deployIis\kur.bat" $packageDir -Force -ErrorAction SilentlyContinue
}

Write-Host "[4/4] Tamamlandı."
Write-Host "Paket klasörü: $packageDir"

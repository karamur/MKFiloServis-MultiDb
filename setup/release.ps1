<#
.SYNOPSIS
    MKFiloServis surum paketleme yardimcisi.

.DESCRIPTION
    - Paket dosyasinin SHA256'sini hesaplar
    - RELEASE-NOTES-vX.Y.Z.md dosyasindaki <sha256-hash-buraya> yer tutucusunu gercek hash ile doldurur
    - git tag vX.Y.Z olusturur ve origin'e pusher
    - 'gh' CLI varsa otomatik GitHub release olusturur; yoksa manuel adimlari ekrana basar

.EXAMPLE
    .\release.ps1 -Version 1.0.2
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$SetupDir = $PSScriptRoot,
    [switch]$SkipTag,
    [switch]$SkipPush
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $SetupDir
$pkgPath  = Join-Path $SetupDir "output\v$Version\MKFiloServisKurulum-$Version.exe"
$notesSrc = Join-Path $SetupDir "RELEASE-NOTES-v$Version.md"

if (-not (Test-Path $pkgPath))  { throw "Paket bulunamadi: $pkgPath (once .\build.ps1 -Version $Version ile olustur)" }
if (-not (Test-Path $notesSrc)) { throw "Release notes bulunamadi: $notesSrc" }

Write-Host "=== MKFiloServis v$Version release ===" -ForegroundColor Cyan
$pkgInfo = Get-Item $pkgPath
Write-Host ("Paket : {0}" -f $pkgInfo.FullName)
Write-Host ("Boyut : {0:N2} MB" -f ($pkgInfo.Length / 1MB))

Write-Host "[1/4] SHA256 hesaplaniyor..." -ForegroundColor Green
$hash = (Get-FileHash -Algorithm SHA256 -Path $pkgPath).Hash
Write-Host "       $hash"

Write-Host "[2/4] Release notes guncelleniyor..." -ForegroundColor Green
$notesFinal = Join-Path $SetupDir "output\v$Version\RELEASE-NOTES-v$Version.md"
$notesText  = (Get-Content $notesSrc -Raw) -replace '<sha256-hash-buraya>', $hash
Set-Content -Path $notesFinal -Value $notesText -Encoding UTF8
Write-Host "       $notesFinal"

Write-Host "[3/4] Git tag..." -ForegroundColor Green
if ($SkipTag) {
    Write-Host "       (atlandi: -SkipTag)"
} else {
    Push-Location $repoRoot
    try {
        $existing = git tag --list "v$Version"
        if ($existing) {
            Write-Host "       v$Version zaten var, atlandi"
        } else {
            git tag -a "v$Version" -m "v$Version"
            Write-Host "       v$Version tag olusturuldu"
        }
        if (-not $SkipPush) {
            git push origin "v$Version"
            Write-Host "       origin'e pushlandi"
        }
    } finally {
        Pop-Location
    }
}

Write-Host "[4/4] GitHub release..." -ForegroundColor Green
$gh = Get-Command gh -ErrorAction SilentlyContinue
if ($gh) {
    Push-Location $repoRoot
    try {
        gh release create "v$Version" $pkgPath `
            --title "v$Version" `
            --notes-file $notesFinal
    } finally {
        Pop-Location
    }
    Write-Host "       Release olusturuldu" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "       gh CLI yok - manuel adimlar:" -ForegroundColor Yellow
    Write-Host "       1) https://github.com/karamur/MKFiloServis-MultiDb/releases/new?tag=v$Version" -ForegroundColor White
    Write-Host "       2) 'Release title' = v$Version" -ForegroundColor White
    Write-Host "       3) Description = $notesFinal icerigi" -ForegroundColor White
    Write-Host "       4) 'Attach binaries' = $pkgPath" -ForegroundColor White
    Write-Host "       5) 'Publish release'" -ForegroundColor White
}

Write-Host ""
Write-Host "SHA256: $hash" -ForegroundColor Cyan

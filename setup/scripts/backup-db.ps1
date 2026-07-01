<#
.SYNOPSIS
    MKFiloServis SQLite veritabani yedeği alir.

.DESCRIPTION
    InstallPath altindaki MKFiloServis (SQLite) dosyasini
    Backups\db-YYYYMMDD-HHMMSS klasorune kopyalar.
    Inno Setup [Code] blogunun disinda da (manuel veya zamanlanmis gorev) calistirilabilir.

.PARAMETER InstallPath
    Uygulama kurulu dizin. Varsayilan: C:\MKFiloServis

.PARAMETER BackupRoot
    Yedek klasoru ana dizini. Varsayilan: <InstallPath>\Backups

.PARAMETER MaxYedekSayisi
    En fazla kac yedek tutulacak (eskiler silinir). Varsayilan: 30

.EXAMPLE
    .\backup-db.ps1
    .\backup-db.ps1 -InstallPath "D:\MKFiloServis" -MaxYedekSayisi 14
#>
[CmdletBinding()]
param(
    [string] $InstallPath    = 'C:\MKFiloServis',
    [string] $BackupRoot     = '',
    [int]    $MaxYedekSayisi = 30
)

$ErrorActionPreference = 'Stop'

if (-not $BackupRoot) {
    $BackupRoot = Join-Path $InstallPath 'Backups'
}

$DbFile  = Join-Path $InstallPath 'MKFiloServis'
$ShmFile = Join-Path $InstallPath 'MKFiloServis-shm'
$WalFile = Join-Path $InstallPath 'MKFiloServis-wal'

if (-not (Test-Path $DbFile)) {
    Write-Host "UYARI: Veritabani dosyasi bulunamadi: $DbFile" -ForegroundColor Yellow
    Write-Host "       Yedekleme atlanıyor." -ForegroundColor Yellow
    exit 0
}

# Yedek dizini
$stamp     = Get-Date -Format 'yyyyMMdd-HHmmss'
$BackupDir = Join-Path $BackupRoot "db-$stamp"
New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null

# Kopyala
Copy-Item -Path $DbFile  -Destination (Join-Path $BackupDir 'MKFiloServis') -Force
Write-Host "Yedeklendi: $DbFile -> $BackupDir\MKFiloServis"

if (Test-Path $ShmFile) {
    Copy-Item -Path $ShmFile -Destination (Join-Path $BackupDir 'MKFiloServis-shm') -Force
    Write-Host "Yedeklendi: MKFiloServis-shm"
}
if (Test-Path $WalFile) {
    Copy-Item -Path $WalFile -Destination (Join-Path $BackupDir 'MKFiloServis-wal') -Force
    Write-Host "Yedeklendi: MKFiloServis-wal"
}

$boyut = [math]::Round((Get-Item (Join-Path $BackupDir 'MKFiloServis')).Length / 1KB, 1)
Write-Host "OK: Yedek alindi -> $BackupDir  ($boyut KB)" -ForegroundColor Green

# Eski yedekleri temizle (sadece db-* klasorleri)
$tumYedekler = Get-ChildItem -Path $BackupRoot -Directory -Filter 'db-*' |
               Sort-Object Name -Descending

if ($tumYedekler.Count -gt $MaxYedekSayisi) {
    $silinecekler = $tumYedekler | Select-Object -Skip $MaxYedekSayisi
    foreach ($klasor in $silinecekler) {
        Remove-Item -Path $klasor.FullName -Recurse -Force
        Write-Host "Eski yedek silindi: $($klasor.Name)" -ForegroundColor DarkGray
    }
    Write-Host "Temizlik: $($silinecekler.Count) eski yedek silindi, $MaxYedekSayisi yedek korundu." -ForegroundColor DarkGray
}

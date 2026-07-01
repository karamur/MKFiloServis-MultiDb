<#
.SYNOPSIS
    MKFiloServis sunucu guncelleme scripti (ZIP tabanli, IIS).

.DESCRIPTION
    Yayinlanan Web publish ZIP'ini mevcut IIS kurulumunun uzerine uygular.
    Adimlar:
      1) Guncelleme ZIP'i dogrula
      2) Veritabani yedekle (Backups\db-<tarih>)
      3) IIS AppPool'u durdur (dosya kilidi onleme)
      4) Eski dosyalari temizle (konfigurasyonlara DOKUNMA)
      5) Yeni dosyalari kopyala
      6) IIS AppPool'u baslat
      7) Smoke test (HTTP 200 kontrolu)

.PARAMETER ZipPath
    Web publish ZIP dosyasinin tam yolu.
    (build.ps1 -ZipOutput ile olusturulur veya manuel publish ZIP'i)

.PARAMETER InstallPath
    Uygulama kurulu dizin. Varsayilan: C:\MKFiloServis

.PARAMETER SiteName
    IIS site ve AppPool adi. Varsayilan: MKFiloServis

.PARAMETER Port
    Smoke test icin port. Varsayilan: 5190

.PARAMETER NoBackup
    Veritabani yedegini atla (tavsiye edilmez).

.PARAMETER NoSmokeTest
    HTTP smoke testini atla.

.EXAMPLE
    .\update-server.ps1 -ZipPath "C:\dist\MKFiloServisWeb-1.0.4.zip"
    .\update-server.ps1 -ZipPath "...\web.zip" -InstallPath "D:\MKFiloServis"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ZipPath,
    [string] $InstallPath = 'C:\MKFiloServis',
    [string] $SiteName    = 'MKFiloServis',
    [int]    $Port        = 5190,
    [switch] $NoBackup,
    [switch] $NoSmokeTest
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

$stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  MKFiloServis — Sunucu Guncelleme" -ForegroundColor Cyan
Write-Host "  $stamp" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# ---- 1) ZIP dogrula ----
Write-Host "[1/7] Guncelleme paketi kontrol ediliyor..." -ForegroundColor Yellow
if (-not (Test-Path $ZipPath)) {
    Write-Host "HATA: ZIP bulunamadi: $ZipPath" -ForegroundColor Red
    exit 1
}
$zipBoyut = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
Write-Host "      OK: $ZipPath ($zipBoyut MB)" -ForegroundColor Green

if (-not (Test-Path $InstallPath)) {
    Write-Host "HATA: Kurulum dizini bulunamadi: $InstallPath" -ForegroundColor Red
    Write-Host "      Once ana kurulum paketini (MKFiloServisKurulum-*.exe) calistirin." -ForegroundColor Red
    exit 1
}

# ---- 2) Veritabani yedekle ----
if (-not $NoBackup) {
    Write-Host "[2/7] Veritabani yedekleniyor..." -ForegroundColor Yellow
    $dbScript = Join-Path $InstallPath 'scripts\backup-db.ps1'
    if (Test-Path $dbScript) {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $dbScript `
            -InstallPath $InstallPath
    } else {
        # Script yoksa manuel yedekle
        $DbFile = Join-Path $InstallPath 'MKFiloServis'
        if (Test-Path $DbFile) {
            $BackupDir = Join-Path $InstallPath "Backups\db-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
            New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
            Copy-Item $DbFile (Join-Path $BackupDir 'MKFiloServis') -Force
            @('MKFiloServis-shm', 'MKFiloServis-wal') | ForEach-Object {
                $f = Join-Path $InstallPath $_
                if (Test-Path $f) { Copy-Item $f (Join-Path $BackupDir $_) -Force }
            }
            Write-Host "      Yedek: $BackupDir" -ForegroundColor Green
        } else {
            Write-Host "      Veritabani dosyasi bulunamadi, yedekleme atlandi." -ForegroundColor DarkGray
        }
    }
} else {
    Write-Host "[2/7] Veritabani yedekleme ATLANDI (-NoBackup)" -ForegroundColor DarkGray
}

# ---- 3) IIS AppPool durdur ----
Write-Host "[3/7] IIS AppPool durduruluyor: $SiteName..." -ForegroundColor Yellow
$poolDurduruldu = $false
try {
    Import-Module WebAdministration -ErrorAction Stop
    if (Test-Path "IIS:\AppPools\$SiteName") {
        $pool = Get-WebAppPoolState -Name $SiteName
        if ($pool.Value -ne 'Stopped') {
            Stop-WebAppPool -Name $SiteName
            # AppPool durmasini bekle (max 10s)
            $bekle = 0
            while ((Get-WebAppPoolState -Name $SiteName).Value -ne 'Stopped' -and $bekle -lt 10) {
                Start-Sleep -Milliseconds 500
                $bekle++
            }
        }
        $poolDurduruldu = $true
        Write-Host "      AppPool durduruldu." -ForegroundColor Green
    } else {
        Write-Host "      AppPool bulunamadi: $SiteName (atlandi)" -ForegroundColor DarkGray
    }
} catch {
    Write-Host "      IIS modulu yuklenemedi veya AppPool durdurulamadi: $($_.Exception.Message)" -ForegroundColor Yellow
}

# ---- 4) ZIP'i gecici klasore ac ----
Write-Host "[4/7] Guncelleme paketi aciliyor..." -ForegroundColor Yellow
$tempDir = Join-Path $env:TEMP "MKFiloServis_update_$(Get-Date -Format 'yyyyMMddHHmmss')"
Expand-Archive -Path $ZipPath -DestinationPath $tempDir -Force
Write-Host "      Acildi: $tempDir" -ForegroundColor Green

# ---- 5) Korunacak dosya/klasorleri belirle ----
$korunanlar = @(
    'dbsettings.json',
    'appsettings.json',
    'appsettings.Production.json',
    'appsettings.Development.json',
    'portalsettings.json',
    'backup_settings.json',
    'MKFiloServis',      # SQLite DB
    'MKFiloServis-shm',
    'MKFiloServis-wal',
    'logs',
    'uploads',
    'Backups',
    'scripts'
)

# ---- 6) Eski uygulama dosyalarini temizle (korunanlar haric) ----
Write-Host "[5/7] Eski dosyalar temizleniyor..." -ForegroundColor Yellow
Get-ChildItem -Path $InstallPath | Where-Object {
    $korunanlar -notcontains $_.Name
} | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "      Temizlik tamamlandi." -ForegroundColor Green

# ---- 7) Yeni dosyalari kopyala ----
Write-Host "[6/7] Yeni dosyalar kopyalaniyor..." -ForegroundColor Yellow
$kaynaklar = Get-ChildItem -Path $tempDir | Where-Object {
    $korunanlar -notcontains $_.Name
}
foreach ($kaynak in $kaynaklar) {
    $hedef = Join-Path $InstallPath $kaynak.Name
    Copy-Item $kaynak.FullName $hedef -Recurse -Force
}
# Gecici klasoru temizle
Remove-Item $tempDir -Recurse -Force
Write-Host "      Kopyalama tamamlandi." -ForegroundColor Green

# ---- 8) IIS AppPool baslat ----
Write-Host "[7/7] IIS AppPool baslatiliyor..." -ForegroundColor Yellow
if ($poolDurduruldu) {
    try {
        Start-WebAppPool -Name $SiteName
        Write-Host "      AppPool baslatildi." -ForegroundColor Green
    } catch {
        Write-Host "      AppPool baslatma hatasi: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "      AppPool zaten durdurulmustu, atlandi." -ForegroundColor DarkGray
}

# ---- Smoke test ----
if (-not $NoSmokeTest) {
    Write-Host ""
    Write-Host "Smoke test: http://localhost:$Port ..." -ForegroundColor Cyan
    $ok = $false
    for ($i = 1; $i -le 6; $i++) {
        Start-Sleep -Milliseconds 2000
        try {
            $resp = Invoke-WebRequest -Uri "http://localhost:$Port" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($resp.StatusCode -lt 500) {
                Write-Host "      HTTP $($resp.StatusCode) — BASARILI!" -ForegroundColor Green
                $ok = $true
                break
            }
        } catch {
            Write-Host "      Deneme $i/6 — bekleniyor..." -ForegroundColor DarkGray
        }
    }
    if (-not $ok) {
        Write-Host "UYARI: Smoke test basarisiz. Uygulama henuz ayaga kalkmamis olabilir." -ForegroundColor Yellow
        Write-Host "       Loglari kontrol edin: $InstallPath\logs\" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  GUNCELLEME TAMAMLANDI" -ForegroundColor Green
Write-Host "  http://localhost:$Port" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""


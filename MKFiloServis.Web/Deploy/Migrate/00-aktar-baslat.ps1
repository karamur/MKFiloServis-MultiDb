# =============================================================================
# 00-aktar-baslat.ps1  —  ANA TRANSFER SCRIPTİ
# Eski "DestekCRMServisBlazorDb" veritabanını ve şifreli evrak arşivini
# yeni "MKFiloServis" sistemine taşır.
#
# KULLANIM:
#   pwsh -ExecutionPolicy Bypass -File 00-aktar-baslat.ps1
#
# GEREKSİNİMLER:
#   - PostgreSQL 14+ kurulu ve pg_restore PATH'de veya varsayılan konumda
#   - robocopy (Windows ile gelir)
#   - PowerShell 7+
# =============================================================================

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# ---------------------------------------------------------------------------
# AYARLAR — İhtiyaca göre düzenleyin
# ---------------------------------------------------------------------------
$cfg = @{
    # Eski yedeklerin bulunduğu kök klasör (içinde database/, uploads/, keys/ var)
    EskiYedekKok   = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\ustunfiloservis_yedekler\MKFiloServis_yedekleme"

    # Restore edilecek en son backup dosyası
    BackupFile     = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\ustunfiloservis_yedekler\MKFiloServis_yedekleme\database\2026\06\MKFiloServis_PostgreSQL_20260626_164011.backup"

    # PostgreSQL bağlantı bilgileri
    PgHost         = "localhost"
    PgPort         = "5432"
    PgUser         = "postgres"
    PgPassword     = "Fast123"

    # Yeni veritabanı adı
    NewDbName      = "MKFiloServis"

    # Yeni sistemin depolama kökü
    YeniDepolamaKok = "C:\MKFiloServis_yedekleme"
}
# ---------------------------------------------------------------------------

function Write-Banner([string]$txt) {
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Magenta
    Write-Host "  $txt" -ForegroundColor Magenta
    Write-Host ("=" * 60) -ForegroundColor Magenta
    Write-Host ""
}

function Write-Step([string]$msg) { Write-Host "[ANA] $msg" -ForegroundColor Cyan }
function Write-OK([string]$msg)   { Write-Host "[OK] $msg"   -ForegroundColor Green }
function Write-Err([string]$msg)  { Write-Host "[HATA] $msg" -ForegroundColor Red }

# Süre takibi
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Banner "MK FİLO SERVİS — VERİ AKTARIM BAŞLADI"
Write-Host "  Tarih/Saat : $(Get-Date -Format 'dd.MM.yyyy HH:mm:ss')" -ForegroundColor White
Write-Host "  Bu PC      : $($env:COMPUTERNAME)" -ForegroundColor White
Write-Host ""

# ---------------------------------------------------------------------------
# ADIM 1 — Veritabanı restore
# ---------------------------------------------------------------------------
Write-Banner "ADIM 1/2 — VERİTABANI RESTORE"

$dbScript = Join-Path $ScriptDir "01-db-restore.ps1"
if (-not (Test-Path $dbScript)) {
    Write-Err "01-db-restore.ps1 bulunamadı: $dbScript"
    exit 1
}

& pwsh -NoProfile -ExecutionPolicy Bypass -File $dbScript `
    -BackupFile   $cfg.BackupFile `
    -PgHost       $cfg.PgHost `
    -PgPort       $cfg.PgPort `
    -PgUser       $cfg.PgUser `
    -PgPassword   $cfg.PgPassword `
    -NewDbName    $cfg.NewDbName

if ($LASTEXITCODE -ne 0) {
    Write-Err "Veritabanı restore adımı başarısız oldu (kod: $LASTEXITCODE). Aktarım durdu."
    exit 1
}

Write-OK "Veritabanı adımı tamamlandı."

# ---------------------------------------------------------------------------
# ADIM 2 — Dosya aktarımı
# ---------------------------------------------------------------------------
Write-Banner "ADIM 2/2 — DOSYA AKTARIMI (ŞİFRELİ EVRAKLAR + KEYS)"

$dosyaScript = Join-Path $ScriptDir "02-dosya-aktar.ps1"
if (-not (Test-Path $dosyaScript)) {
    Write-Err "02-dosya-aktar.ps1 bulunamadı: $dosyaScript"
    exit 1
}

& pwsh -NoProfile -ExecutionPolicy Bypass -File $dosyaScript `
    -EskiYedekKok    $cfg.EskiYedekKok `
    -YeniDepolamaKok $cfg.YeniDepolamaKok

if ($LASTEXITCODE -ne 0) {
    Write-Err "Dosya aktarım adımı başarısız oldu (kod: $LASTEXITCODE). Aktarım durdu."
    exit 1
}

Write-OK "Dosya aktarımı adımı tamamlandı."

# ---------------------------------------------------------------------------
# SONUÇ
# ---------------------------------------------------------------------------
$stopwatch.Stop()
$elapsed = $stopwatch.Elapsed

Write-Banner "AKTARIM BAŞARIYLA TAMAMLANDI"
Write-Host "  Toplam süre  : $($elapsed.Minutes) dk $($elapsed.Seconds) sn" -ForegroundColor White
Write-Host "  Veritabanı  : $($cfg.NewDbName) @ $($cfg.PgHost):$($cfg.PgPort)" -ForegroundColor Green
Write-Host "  Dosyalar    : $($cfg.YeniDepolamaKok)" -ForegroundColor Green
Write-Host ""
Write-Host "SONRAKI ADIMLAR:" -ForegroundColor Yellow
Write-Host "  1. Uygulamayı IIS'de başlatın veya 'kur.bat' ile güncelleyin" -ForegroundColor White
Write-Host "  2. Giriş yapın → 'Sistem > Arşiv Göçü' ile eski uploads/ yollarını dönüştürün" -ForegroundColor White
Write-Host "  3. Lisansı aktivasyon ekranından girin" -ForegroundColor White
Write-Host "  4. 2. PC için: Deploy\Migrate\03-pc2-publish.ps1 çalıştırın" -ForegroundColor White
Write-Host ""

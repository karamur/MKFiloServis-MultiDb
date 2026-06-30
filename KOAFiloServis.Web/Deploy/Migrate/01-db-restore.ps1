# =============================================================================
# 01-db-restore.ps1
# Eski "DestekCRMServisBlazorDb" yedeğini yeni "KOAFiloServis" adıyla restore eder.
# KULLANIM: pwsh -ExecutionPolicy Bypass -File 01-db-restore.ps1
# =============================================================================

param(
    [string]$BackupFile  = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\ustunfiloservis_yedekler\KOAFiloServis_yedekleme\database\2026\06\KOAFiloServis_PostgreSQL_20260626_164011.backup",
    [string]$PgHost      = "localhost",
    [string]$PgPort      = "5432",
    [string]$PgUser      = "postgres",
    [string]$PgPassword  = "Fast123",
    [string]$NewDbName   = "KOAFiloServis"
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$msg) { Write-Host "[DB-RESTORE] $msg" -ForegroundColor Cyan }
function Write-OK([string]$msg)   { Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Err([string]$msg)  { Write-Host "[HATA] $msg" -ForegroundColor Red }

# --- pg_restore yolunu bul ---
function Get-PgToolPath([string]$tool) {
    $cmd = Get-Command $tool -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $fallbacks = @(
        "C:\Program Files\PostgreSQL\17\bin\$tool",
        "C:\Program Files\PostgreSQL\16\bin\$tool",
        "C:\Program Files\PostgreSQL\15\bin\$tool",
        "C:\Program Files\PostgreSQL\14\bin\$tool"
    )
    return $fallbacks | Where-Object { Test-Path $_ } | Select-Object -First 1
}

Write-Step "Kontroller yapılıyor..."

if (-not (Test-Path $BackupFile)) {
    Write-Err "Backup dosyası bulunamadı: $BackupFile"
    exit 1
}

$psql     = Get-PgToolPath "psql.exe"
$pgrestore = Get-PgToolPath "pg_restore.exe"

if (-not $psql)      { Write-Err "psql.exe bulunamadı. PostgreSQL kurulu mu?"; exit 1 }
if (-not $pgrestore) { Write-Err "pg_restore.exe bulunamadı. PostgreSQL kurulu mu?"; exit 1 }

Write-OK "psql      : $psql"
Write-OK "pg_restore: $pgrestore"
Write-OK "Backup    : $BackupFile"

$env:PGPASSWORD = $PgPassword

# --- Hedef DB var mı kontrol et ---
Write-Step "Hedef veritabanı kontrol ediliyor: $NewDbName"
$dbExists = & $psql -h $PgHost -p $PgPort -U $PgUser -tAc "SELECT 1 FROM pg_database WHERE datname='$NewDbName'" 2>&1
if ($dbExists -eq "1") {
    Write-Step "UYARI: '$NewDbName' zaten mevcut."
    Write-Host ""
    Write-Host "Seçenekler:" -ForegroundColor Yellow
    Write-Host "  [1] Mevcut DB'yi SİL ve yeniden oluştur (tüm veriler kaybedilir!)" -ForegroundColor Red
    Write-Host "  [2] İptal et" -ForegroundColor Yellow
    $choice = Read-Host "Seçiminiz (1/2)"
    if ($choice -ne "1") {
        Write-Host "İşlem iptal edildi." -ForegroundColor Yellow
        $env:PGPASSWORD = ""
        exit 0
    }
    Write-Step "Mevcut '$NewDbName' siliniyor..."
    & $psql -h $PgHost -p $PgPort -U $PgUser -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='$NewDbName' AND pid <> pg_backend_pid();" postgres | Out-Null
    & $psql -h $PgHost -p $PgPort -U $PgUser -c "DROP DATABASE IF EXISTS `"$NewDbName`";" postgres
    if ($LASTEXITCODE -ne 0) { Write-Err "DB silinemedi."; exit 1 }
    Write-OK "Eski DB silindi."
}

# --- Yeni DB oluştur ---
Write-Step "'$NewDbName' veritabanı oluşturuluyor..."
& $psql -h $PgHost -p $PgPort -U $PgUser -c "CREATE DATABASE `"$NewDbName`" ENCODING 'UTF8';" postgres
if ($LASTEXITCODE -ne 0) { Write-Err "DB oluşturulamadı."; exit 1 }
Write-OK "Veritabanı oluşturuldu."

# --- Restore ---
Write-Step "Veri restore ediliyor... (büyük backup'ta birkaç dakika sürebilir)"
& $pgrestore -h $PgHost -p $PgPort -U $PgUser -d $NewDbName --no-owner --no-acl --role=$PgUser -v $BackupFile 2>&1 | ForEach-Object {
    if ($_ -match "error" -and $_ -notmatch "pg_restore: warning") {
        Write-Host "  $_" -ForegroundColor Red
    } elseif ($_ -match "warning") {
        Write-Host "  $_" -ForegroundColor Yellow
    }
    # Bilgi satırları: sessiz
}

# pg_restore bazı uyarılarla 1 döner (özellikle owner/extension), 0 veya 1 kabul et
if ($LASTEXITCODE -gt 1) {
    Write-Err "pg_restore beklenmedik hata kodu: $LASTEXITCODE"
    $env:PGPASSWORD = ""
    exit 1
}

# --- Satır sayısı doğrulama ---
Write-Step "Tablo satır sayıları kontrol ediliyor..."
$rowCount = & $psql -h $PgHost -p $PgPort -U $PgUser -d $NewDbName -tAc `
    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE';" 2>&1
Write-OK "Public şemada $rowCount tablo mevcut."

$env:PGPASSWORD = ""

Write-Host ""
Write-OK "DB restore tamamlandı! Veritabanı: $NewDbName @ ${PgHost}:${PgPort}"
Write-Host ""

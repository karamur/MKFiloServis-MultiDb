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

function Assert-SafeStorageTarget([string]$Root, [string]$Name) {
    if ($Name -notin @('uploads', 'keys', 'database')) { throw "İzin verilmeyen geri dönüş dizini adı." }
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $targetFull = [IO.Path]::GetFullPath((Join-Path $rootFull $Name))
    if (-not $targetFull.StartsWith($rootFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Geri dönüş hedefi depolama kökü dışında."
    }
    if ((Test-Path -LiteralPath $targetFull) -and ((Get-Item -LiteralPath $targetFull -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Geri dönüş hedefi junction/symlink olamaz: $targetFull"
    }
    return $targetFull
}

function Restore-StorageFolders([string]$SnapshotRoot, [string]$TargetRoot, [hashtable]$PriorState) {
    foreach ($name in @('uploads', 'keys', 'database')) {
        $target = Assert-SafeStorageTarget $TargetRoot $name
        $snapshot = Join-Path $SnapshotRoot $name
        if (Test-Path $target) { Remove-Item -LiteralPath $target -Recurse -Force }
        if ($PriorState[$name]) {
            if (-not (Test-Path -LiteralPath $snapshot -PathType Container)) { throw "Geri dönüş anlık görüntüsü eksik: $name" }
            New-Item -ItemType Directory -Path $target -Force | Out-Null
            & robocopy $snapshot $target /E /COPY:DAT /DCOPY:DAT /R:2 /W:2 /NP /NDL /NJH /NJS
            if ($LASTEXITCODE -gt 7) { throw "Dosya geri dönüşü başarısız ($name), robocopy=$LASTEXITCODE" }
        }
    }
}

# Süre takibi
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Banner "MK FİLO SERVİS — VERİ AKTARIM BAŞLADI"
Write-Host "  Tarih/Saat : $(Get-Date -Format 'dd.MM.yyyy HH:mm:ss')" -ForegroundColor White
Write-Host "  Bu PC      : $($env:COMPUTERNAME)" -ForegroundColor White
Write-Host ""

# ---------------------------------------------------------------------------
# ADIM 0 — Dosya ve anahtar ön kontrolü (hedefe yazmadan)
# ---------------------------------------------------------------------------
Write-Banner "ADIM 0/2 — DOSYA VE ANAHTAR ÖN KONTROLÜ"

$dosyaScript = Join-Path $ScriptDir "02-dosya-aktar.ps1"
if (-not (Test-Path $dosyaScript)) {
    Write-Err "02-dosya-aktar.ps1 bulunamadı: $dosyaScript"
    exit 1
}
& pwsh -NoProfile -ExecutionPolicy Bypass -File $dosyaScript `
    -EskiYedekKok    $cfg.EskiYedekKok `
    -YeniDepolamaKok $cfg.YeniDepolamaKok `
    -PreflightOnly
if ($LASTEXITCODE -ne 0) {
    Write-Err "Dosya/anahtar ön kontrolü başarısız (kod: $LASTEXITCODE). DB restore başlatılmadı."
    exit 1
}
Write-OK "Dosya ve anahtarlar hedefe yazmadan doğrulandı."

# DB veya dosya aşaması hata verirse önceki DB ve depolama durumunu geri döndürmek için
# ayrı bir operasyon kopyası oluştur. Hata halinde bu klasör otomatik silinmez.
$journalRoot = Join-Path $env:LOCALAPPDATA 'MKFiloServis/OperationJournal'
$operationId = 'full-transfer-' + [guid]::NewGuid().ToString('N')
$operationRoot = Join-Path $journalRoot $operationId
$snapshotRoot = Join-Path $operationRoot 'storage-before'
New-Item -ItemType Directory -Path $snapshotRoot -Force | Out-Null
$priorState = @{}
foreach ($name in @('uploads', 'keys', 'database')) {
    $target = Assert-SafeStorageTarget $cfg.YeniDepolamaKok $name
    $priorState[$name] = Test-Path -LiteralPath $target -PathType Container
    if ($priorState[$name]) {
        $snapshot = Join-Path $snapshotRoot $name
        New-Item -ItemType Directory -Path $snapshot -Force | Out-Null
        & robocopy $target $snapshot /E /COPY:DAT /DCOPY:DAT /R:2 /W:2 /NP /NDL /NJH /NJS
        if ($LASTEXITCODE -gt 7) { throw "Mevcut dosya geri dönüş kopyası alınamadı ($name); DB restore başlatılmadı." }
    }
}
$priorState | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $operationRoot 'storage-before.json') -Encoding UTF8
$rollbackDatabase = Join-Path $operationRoot 'database-before.backup'
$databaseManifest = Join-Path $operationRoot 'database-operation.json'
Write-Step "Önceki dosya/key snapshot hazır: $operationRoot"

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
    -RollbackBackupPath $rollbackDatabase `
    -OperationManifestPath $databaseManifest `
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

& pwsh -NoProfile -ExecutionPolicy Bypass -File $dosyaScript `
    -EskiYedekKok    $cfg.EskiYedekKok `
    -YeniDepolamaKok $cfg.YeniDepolamaKok

if ($LASTEXITCODE -ne 0) {
    $fileErrorCode = $LASTEXITCODE
    Write-Err "Dosya aktarım adımı başarısız oldu (kod: $fileErrorCode); önceki DB ve dosya durumu geri alınıyor."
    $rollbackDbExit = 1
    try {
        if (-not (Test-Path -LiteralPath $databaseManifest -PathType Leaf)) { throw "DB operasyon makbuzu yok." }
        $databaseOperation = Get-Content -LiteralPath $databaseManifest -Raw | ConvertFrom-Json
        if ($databaseOperation.Target -cne $cfg.NewDbName) { throw "DB operasyon makbuzu hedefi uyuşmuyor." }
        if ($databaseOperation.DatabaseExistedBefore) {
            if (-not (Test-Path -LiteralPath $rollbackDatabase -PathType Leaf)) { throw "Önceki DB dump'ı yok." }
            & pwsh -NoProfile -ExecutionPolicy Bypass -File $dbScript `
                -BackupFile $rollbackDatabase `
                -PgHost $cfg.PgHost -PgPort $cfg.PgPort -PgUser $cfg.PgUser -NewDbName $cfg.NewDbName -ConfirmedRollback
        } elseif ($databaseOperation.Status -eq 'DatabaseCreatedByOperation') {
            & pwsh -NoProfile -ExecutionPolicy Bypass -File $dbScript `
                -BackupFile $cfg.BackupFile `
                -PgHost $cfg.PgHost -PgPort $cfg.PgPort -PgUser $cfg.PgUser -NewDbName $cfg.NewDbName `
                -OperationManifestPath $databaseManifest -DropCreatedDatabase -ConfirmedRollback
        } else {
            throw "DB makbuzu güvenli bir geri dönüş yolu göstermiyor."
        }
        $rollbackDbExit = $LASTEXITCODE
    } catch {
        Write-Err "Veritabanı geri dönüşü başlatılamadı: $($_.Exception.Message)"
    }
    try {
        Restore-StorageFolders $snapshotRoot $cfg.YeniDepolamaKok $priorState
        if ($rollbackDbExit -ne 0) { throw "Veritabanı rollback betiği başarısız (kod: $rollbackDbExit)." }
        @{ Status='RolledBack'; OperationId=$operationId; CompletedAtUtc=[DateTime]::UtcNow.ToString('o') } |
            ConvertTo-Json | Set-Content -LiteralPath (Join-Path $operationRoot 'rolled-back.json') -Encoding UTF8
        Write-OK "DB ve depolama önceki durumuna döndürüldü. Snapshot: $operationRoot"
    } catch {
        @{ Status='RollbackFailedOrUnconfirmed'; OperationId=$operationId; Error=$_.Exception.Message; RecordedAtUtc=[DateTime]::UtcNow.ToString('o') } |
            ConvertTo-Json | Set-Content -LiteralPath (Join-Path $operationRoot 'rollback-failed.json') -Encoding UTF8
        Write-Err "Kritik: tam geri dönüş doğrulanamadı. Uygulamayı başlatmayın; snapshot korunuyor: $operationRoot"
    }
    exit 1
}

Write-OK "Dosya aktarımı adımı tamamlandı."

# Başarılı tam aktarım sonrası operasyonel rollback kopyası artık gerekmiyor.
$journalRootFull = [IO.Path]::GetFullPath($journalRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
$operationRootFull = [IO.Path]::GetFullPath($operationRoot)
if ($operationRootFull.StartsWith($journalRootFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and
    (Split-Path -Leaf $operationRootFull) -match '^full-transfer-[0-9a-f]{32}$') {
    try { Remove-Item -LiteralPath $operationRootFull -Recurse -Force }
    catch { Write-Host "[UYARI] Başarılı aktarım snapshot'ı temizlenemedi; hassas yedek kalıntısı: $operationRootFull" -ForegroundColor Yellow }
} else {
    Write-Host "[UYARI] Başarılı aktarım snapshot'ı otomatik temizlenmedi; yolu doğrulayın: $operationRootFull" -ForegroundColor Yellow
}

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

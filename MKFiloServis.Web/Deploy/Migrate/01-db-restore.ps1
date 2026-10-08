# =============================================================================
# 01-db-restore.ps1
# Eski "DestekCRMServisBlazorDb" yedeğini yeni "MKFiloServis" adıyla restore eder.
# KULLANIM: pwsh -ExecutionPolicy Bypass -File 01-db-restore.ps1
# =============================================================================

param(
    [string]$BackupFile  = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\ustunfiloservis_yedekler\MKFiloServis_yedekleme\database\2026\06\MKFiloServis_PostgreSQL_20260626_164011.backup",
    [string]$PgHost      = "localhost",
    [string]$PgPort      = "5432",
    [string]$PgUser      = "postgres",
    [string]$NewDbName   = "MKFiloServis",
    [string]$RollbackBackupPath,
    [string]$OperationManifestPath,
    [switch]$ConfirmedRestore,
    [switch]$ConfirmedRollback,
    [switch]$DropCreatedDatabase
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

if ($NewDbName -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') { throw "Geçersiz hedef DB adı." }
if (-not (Test-Path -LiteralPath $BackupFile -PathType Leaf)) { throw "Backup dosyası bulunamadı." }
$psql = Get-PgToolPath "psql.exe"
$pgrestore = Get-PgToolPath "pg_restore.exe"
$pgdump = Get-PgToolPath "pg_dump.exe"
if (-not $psql -or -not $pgrestore -or -not $pgdump) { throw "PostgreSQL client araçları eksik." }
$auditSql = Join-Path $PSScriptRoot 'postgres-write-audit.sql'
if (-not (Test-Path -LiteralPath $auditSql)) {
    $auditSql = Join-Path $PSScriptRoot '../../../MKFiloServis.Shared/Auditing/postgres-write-audit.sql'
}
if (-not (Test-Path -LiteralPath $auditSql)) { throw "Write audit kurulum dosyası eksik; restore başlatılmadı." }
$journalDir = Join-Path $env:LOCALAPPDATA ('MKFiloServis/OperationJournal/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $journalDir -Force | Out-Null
function Write-Receipt([string]$name, [hashtable]$record) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes(($record | ConvertTo-Json -Depth 5))
    $stream = [System.IO.FileStream]::new((Join-Path $journalDir $name), [System.IO.FileMode]::CreateNew)
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
}
$previousPassword = $env:PGPASSWORD
$securePassword = $null
$passwordBstr = [IntPtr]::Zero
$completed = $false
$rollbackConfirmed = $false
$restoreAttempted = $false
$databaseExistedBefore = $false
$databaseCreatedByOperation = $false
$beforeRestoreFile = Join-Path $journalDir 'before-restore.backup'
try {
    $securePassword = Read-Host "PostgreSQL parolası ($PgUser@$PgHost/$NewDbName)" -AsSecureString
    if ($securePassword.Length -eq 0) { throw "Veritabanı parolası boş olamaz." }
    $passwordBstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $env:PGPASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordBstr)
    $dbExists = & $psql -h $PgHost -p $PgPort -U $PgUser -d postgres -v ON_ERROR_STOP=1 -tAc "SELECT 1 FROM pg_database WHERE datname='$NewDbName'"
    if ($LASTEXITCODE -ne 0) { throw "Hedef DB kontrolü başarısız." }
    if ($DropCreatedDatabase) {
        if (-not $ConfirmedRollback -or [string]::IsNullOrWhiteSpace($OperationManifestPath)) {
            throw "Oluşturulan DB'yi kaldırmak için onaylı tam aktarım makbuzu gerekir."
        }
        $manifestFull = [IO.Path]::GetFullPath($OperationManifestPath)
        $allowedManifestRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'MKFiloServis/OperationJournal')).TrimEnd([IO.Path]::DirectorySeparatorChar)
        if (-not $manifestFull.StartsWith($allowedManifestRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Tam aktarım makbuzu izinli operasyon günlüğü dışında."
        }
        if (-not (Test-Path -LiteralPath $manifestFull -PathType Leaf) -or
            ((Get-Item -LiteralPath $manifestFull -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Tam aktarım makbuzu yok veya junction/symlink."
        }
        $manifest = Get-Content -LiteralPath $manifestFull -Raw | ConvertFrom-Json
        if ($manifest.Target -cne $NewDbName -or $manifest.Status -cne 'DatabaseCreatedByOperation' -or [string]::IsNullOrWhiteSpace([string]$manifest.DatabaseOid)) {
            throw "Makbuz bu DB'nin mevcut tam aktarımda oluşturulduğunu kanıtlamıyor. DB korunuyor."
        }
        $currentOidOutput = & $psql -h $PgHost -p $PgPort -U $PgUser -d postgres -v ON_ERROR_STOP=1 -tAc "SELECT oid FROM pg_database WHERE datname='$NewDbName'"
        if ($LASTEXITCODE -ne 0) { throw "DB kimliği okunamadı; DB korunuyor." }
        $currentOid = ("$currentOidOutput").Trim()
        if ([string]::IsNullOrWhiteSpace($currentOid)) {
            Write-Receipt 'created-db-removed.json' @{ Status='AlreadyAbsent'; Target=$NewDbName; DatabaseOid=$manifest.DatabaseOid; RecordedAtUtc=[DateTime]::UtcNow.ToString('o') }
            Write-OK "Operasyon makbuzundaki yeni hedef DB zaten yok; kaldırma işlemi tamamlanmış kabul edildi."
            return
        }
        if ($currentOid -cne [string]$manifest.DatabaseOid) {
            throw "DB kimliği makbuzla eşleşmiyor; DB korunuyor."
        }
        & $psql -h $PgHost -p $PgPort -U $PgUser -d postgres -v ON_ERROR_STOP=1 -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='$NewDbName' AND pid <> pg_backend_pid();"
        if ($LASTEXITCODE -ne 0) { throw "İşlemde oluşturulan DB bağlantıları kapatılamadı." }
        & $psql -h $PgHost -p $PgPort -U $PgUser -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE `"$NewDbName`";"
        if ($LASTEXITCODE -ne 0) { throw "İşlemde oluşturulan DB kaldırılamadı." }
        Write-Receipt 'created-db-removed.json' @{ Status='RolledBack'; Target=$NewDbName; DatabaseOid=$currentOid; RecordedAtUtc=[DateTime]::UtcNow.ToString('o') }
        Write-OK "Yalnızca bu aktarımda oluşturulan DB kaldırıldı."
        return
    }
    if ($dbExists -eq '1') {
        Write-Host "Public şemasındaki iş verileri yedekten değiştirilecek. Audit şeması korunacak." -ForegroundColor Yellow
        if (-not $ConfirmedRollback -and -not $ConfirmedRestore -and (Read-Host 'Devam etmek için EVET yazın') -ne 'EVET') {
            throw 'Restore kullanıcı tarafından iptal edildi.'
        }
    }
    Write-Receipt 'started.json' @{
        Operation='DatabaseRestore'; StartedAtUtc=[DateTime]::UtcNow.ToString('o'); Target=$NewDbName
        Source=[System.IO.Path]::GetFileName($BackupFile); SourceSha256=(Get-FileHash -LiteralPath $BackupFile -Algorithm SHA256).Hash
        Actor=$env:USERNAME; Provider='PostgreSQL'; Status='Started'
    }
    if ($dbExists -eq '1') {
        $databaseExistedBefore = $true
        & $pgdump -h $PgHost -p $PgPort -U $PgUser -d $NewDbName --format=custom --no-owner --no-privileges --exclude-schema=mk_audit -f $beforeRestoreFile
        if ($LASTEXITCODE -ne 0) { throw "Geri dönüş yedeği alınamadı; restore başlatılmadı." }
        if (-not (Test-Path -LiteralPath $beforeRestoreFile) -or (Get-Item -LiteralPath $beforeRestoreFile).Length -le 5) {
            throw "Geri dönüş yedeği boş veya eksik; restore başlatılmadı."
        }
        if (-not [string]::IsNullOrWhiteSpace($RollbackBackupPath)) {
            $externalRollbackPath = [System.IO.Path]::GetFullPath($RollbackBackupPath)
            if (Test-Path -LiteralPath $externalRollbackPath) { throw "Tam aktarım rollback dosyası zaten var; üzerine yazılmadı." }
            $externalRollbackParent = Split-Path -Parent $externalRollbackPath
            New-Item -ItemType Directory -Path $externalRollbackParent -Force | Out-Null
            Copy-Item -LiteralPath $beforeRestoreFile -Destination $externalRollbackPath
            if ((Get-FileHash -LiteralPath $beforeRestoreFile -Algorithm SHA256).Hash -ne
                (Get-FileHash -LiteralPath $externalRollbackPath -Algorithm SHA256).Hash) {
                Remove-Item -LiteralPath $externalRollbackPath -Force -ErrorAction SilentlyContinue
                throw "Tam aktarım rollback kopyasının SHA-256 doğrulaması başarısız."
            }
            Write-Receipt 'external-rollback.json' @{
                RecordedAtUtc=[DateTime]::UtcNow.ToString('o'); Status='Ready'
                Path=$externalRollbackPath; Sha256=(Get-FileHash -LiteralPath $externalRollbackPath -Algorithm SHA256).Hash
            }
        }
    } else {
        & $psql -h $PgHost -p $PgPort -U $PgUser -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE `"$NewDbName`" ENCODING 'UTF8';"
        if ($LASTEXITCODE -ne 0) { throw "DB oluşturulamadı." }
        $databaseCreatedByOperation = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($OperationManifestPath)) {
        $manifestFull = [IO.Path]::GetFullPath($OperationManifestPath)
        if (Test-Path -LiteralPath $manifestFull) { throw "DB aktarım makbuzu zaten mevcut; üzerine yazılmadı." }
        if ($databaseExistedBefore -and [string]::IsNullOrWhiteSpace($RollbackBackupPath)) {
            throw "Önceden var olan DB için makbuz üretilecekse dış geri dönüş yedeği zorunludur."
        }
        $manifestParent = Split-Path -Parent $manifestFull
        New-Item -ItemType Directory -Path $manifestParent -Force | Out-Null
        $databaseOid = $null
        if ($databaseCreatedByOperation) {
            $databaseOidOutput = & $psql -h $PgHost -p $PgPort -U $PgUser -d postgres -v ON_ERROR_STOP=1 -tAc "SELECT oid FROM pg_database WHERE datname='$NewDbName'"
            if ($LASTEXITCODE -ne 0) { throw "Yeni DB kimliği alınamadı; DB restore başlatılmadı." }
            $databaseOid = ("$databaseOidOutput").Trim()
            if ([string]::IsNullOrWhiteSpace($databaseOid)) { throw "Yeni DB kimliği boş; DB restore başlatılmadı." }
        }
        @{
            Target=$NewDbName; DatabaseExistedBefore=$databaseExistedBefore
            SourceBackupFile=[IO.Path]::GetFullPath($BackupFile)
            SourceBackupSha256=(Get-FileHash -LiteralPath $BackupFile -Algorithm SHA256).Hash
            DatabaseOid=$databaseOid
            RollbackBackupPath=$(if ($databaseExistedBefore) { [IO.Path]::GetFullPath($RollbackBackupPath) } else { $null })
            RollbackBackupSha256=$(if ($databaseExistedBefore) { (Get-FileHash -LiteralPath $RollbackBackupPath -Algorithm SHA256).Hash } else { $null })
            Status=$(if ($databaseCreatedByOperation) { 'DatabaseCreatedByOperation' } else { 'PreExistingDatabaseSnapshotReady' })
            RecordedAtUtc=[DateTime]::UtcNow.ToString('o')
        } | ConvertTo-Json | Set-Content -LiteralPath $manifestFull -Encoding UTF8
    }
    & $psql -h $PgHost -p $PgPort -U $PgUser -d $NewDbName -v ON_ERROR_STOP=1 --single-transaction -f $auditSql
    if ($LASTEXITCODE -ne 0) { throw "Audit kurulamadı; restore başlatılmadı." }
    $restoreAttempted = $true
    & $pgrestore -h $PgHost -p $PgPort -U $PgUser -d $NewDbName --single-transaction --exit-on-error --clean --if-exists --schema=public --no-owner --no-privileges $BackupFile
    if ($LASTEXITCODE -ne 0) { throw "Restore başarısız; hata kodu $LASTEXITCODE. İşlem başarılı sayılmadı." }
    & $psql -h $PgHost -p $PgPort -U $PgUser -d $NewDbName -v ON_ERROR_STOP=1 --single-transaction -f $auditSql
    if ($LASTEXITCODE -ne 0) { throw "Veri restore edildi ancak audit kurulumu doğrulanamadı; işlem tamamlanmış sayılmadı." }
    Write-Receipt 'completed.json' @{ CompletedAtUtc=[DateTime]::UtcNow.ToString('o'); Status='Succeeded' }
    $completed = $true
    Write-OK "Restore tamamlandı. Operasyon kaydı: $journalDir"
} catch {
    $restoreError = $_
    if ($restoreAttempted -or $databaseCreatedByOperation) {
        try {
            if ($databaseExistedBefore -and (Test-Path -LiteralPath $beforeRestoreFile)) {
                Write-Step "Yeni restore başarısız; önceki public şema geri yükleniyor..."
                & $pgrestore -h $PgHost -p $PgPort -U $PgUser -d $NewDbName --single-transaction --exit-on-error --clean --if-exists --schema=public --no-owner --no-privileges $beforeRestoreFile
                if ($LASTEXITCODE -ne 0) { throw "Geri dönüş pg_restore hata kodu $LASTEXITCODE." }
                & $psql -h $PgHost -p $PgPort -U $PgUser -d $NewDbName -v ON_ERROR_STOP=1 --single-transaction -f $auditSql
                if ($LASTEXITCODE -ne 0) { throw "Geri dönüş sonrası audit doğrulaması başarısız." }
            } elseif ($databaseCreatedByOperation -and -not $databaseExistedBefore) {
                Write-Step "Yeni restore başarısız; bu işlemde oluşturulan hedef DB kaldırılıyor..."
                & $psql -h $PgHost -p $PgPort -U $PgUser -d postgres -v ON_ERROR_STOP=1 -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='$NewDbName' AND pid <> pg_backend_pid();"
                if ($LASTEXITCODE -ne 0) { throw "Yeni hedef DB bağlantıları kapatılamadı." }
                & $psql -h $PgHost -p $PgPort -U $PgUser -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE IF EXISTS `"$NewDbName`";"
                if ($LASTEXITCODE -ne 0) { throw "Bu işlemde oluşturulan DB kaldırılamadı." }
            } else {
                throw "Önceki DB geri dönüş yedeği yok; güvenli rollback yapılamaz."
            }
            Write-Receipt 'rolled-back.json' @{
                RecordedAtUtc=[DateTime]::UtcNow.ToString('o'); Status='RolledBack'
                Target=$NewDbName; RestoredFrom=$(if ($databaseExistedBefore) { [System.IO.Path]::GetFileName($beforeRestoreFile) } else { 'DatabaseCreatedByThisOperationRemoved' })
            }
            $rollbackConfirmed = $true
            Write-Err "Kaynak restore başarısız oldu; önceki DB geri yüklendi. Operasyon: $journalDir"
        } catch {
            Write-Receipt 'rollback-failed.json' @{
                RecordedAtUtc=[DateTime]::UtcNow.ToString('o'); Status='RollbackFailed'
                Target=$NewDbName; Error=$_.Exception.Message
            }
            throw "Kaynak restore başarısız oldu ve DB geri dönüşü de doğrulanamadı. Özgün hata: $($restoreError.Exception.Message). Geri dönüş hatası: $($_.Exception.Message). Operasyon: $journalDir"
        }
    }
    throw $restoreError
} finally {
    if ($passwordBstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordBstr)
    }
    if ($securePassword) { $securePassword.Dispose() }
    $env:PGPASSWORD = $previousPassword
    if (-not $completed -and -not $rollbackConfirmed -and (Test-Path -LiteralPath (Join-Path $journalDir 'started.json'))) {
        Write-Receipt 'unconfirmed.json' @{ RecordedAtUtc=[DateTime]::UtcNow.ToString('o'); Status='FailedOrUnconfirmed' }
    }
}

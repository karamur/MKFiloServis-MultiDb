param(
    [Parameter(Mandatory)][string]$OperationFolder,
    [Parameter(Mandatory)][string]$StorageRoot,
    [string]$PgHost = 'localhost',
    [string]$PgPort = '5432',
    [string]$PgUser = 'postgres'
)

$ErrorActionPreference = 'Stop'

function Get-StorageTarget([string]$Root, [string]$Name) {
    if ($Name -notin @('uploads', 'keys', 'database')) { throw 'İzin verilmeyen storage klasörü.' }
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $target = [IO.Path]::GetFullPath((Join-Path $rootFull $Name))
    if (-not $target.StartsWith($rootFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Hedef depolama kökü dışında.'
    }
    if ((Test-Path -LiteralPath $target) -and ((Get-Item -LiteralPath $target -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Hedef junction/symlink; işlem reddedildi: $target"
    }
    return $target
}

$allowedRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'MKFiloServis/OperationJournal')).TrimEnd([IO.Path]::DirectorySeparatorChar)
$operationFull = [IO.Path]::GetFullPath($OperationFolder)
if (-not [string]::Equals((Split-Path -Parent $operationFull), $allowedRoot, [StringComparison]::OrdinalIgnoreCase) -or
    (Split-Path -Leaf $operationFull) -notmatch '^full-transfer-[0-9a-f]{32}$') {
    throw 'Yalnızca MKFiloServis full-transfer operasyon snapshot klasörü kabul edilir.'
}
if (-not (Test-Path -LiteralPath $operationFull -PathType Container) -or
    ((Get-Item -LiteralPath $operationFull -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw 'Operasyon klasörü yok veya junction/symlink.'
}
foreach ($marker in @('rolled-back.json', 'recovered.json')) {
    if (Test-Path -LiteralPath (Join-Path $operationFull $marker)) { throw "Operasyon zaten sonuç makbuzu içeriyor: $marker" }
}

$statePath = Join-Path $operationFull 'storage-before.json'
$databaseManifestPath = Join-Path $operationFull 'database-operation.json'
if (-not (Test-Path -LiteralPath $statePath -PathType Leaf) -or -not (Test-Path -LiteralPath $databaseManifestPath -PathType Leaf)) {
    throw 'Depolama veya veritabanı operasyon makbuzu eksik; otomatik kurtarma güvenli değil.'
}
$priorState = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
$db = Get-Content -LiteralPath $databaseManifestPath -Raw | ConvertFrom-Json
if ($db.Target -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') { throw 'Makbuzdaki veritabanı adı geçersiz.' }

$snapshotRoot = Join-Path $operationFull 'storage-before'
foreach ($name in @('uploads', 'keys', 'database')) {
    $target = Get-StorageTarget $StorageRoot $name
    if ($priorState.$name -and -not (Test-Path -LiteralPath (Join-Path $snapshotRoot $name) -PathType Container)) {
        throw "Geri dönüş snapshot'ı eksik: $name"
    }
}

if ((Read-Host 'IIS uygulama havuzu durduruldu mu? Onay için UYGULAMA-DURDU yazın') -cne 'UYGULAMA-DURDU') {
    throw 'Uygulama durdurulma onayı verilmedi; hiçbir değişiklik yapılmadı.'
}

$dbScript = Join-Path $PSScriptRoot '01-db-restore.ps1'
if (-not (Test-Path -LiteralPath $dbScript -PathType Leaf)) { throw '01-db-restore.ps1 bulunamadı.' }
if ($db.DatabaseExistedBefore) {
    $rollbackDb = [IO.Path]::GetFullPath([string]$db.RollbackBackupPath)
    $expectedRollbackDb = [IO.Path]::GetFullPath((Join-Path $operationFull 'database-before.backup'))
    if (-not [string]::Equals($rollbackDb, $expectedRollbackDb, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Geri dönüş yedeği bu operasyon klasöründeki database-before.backup değil.'
    }
    if (-not (Test-Path -LiteralPath $rollbackDb -PathType Leaf)) { throw 'Önceki DB geri dönüş yedeği yok.' }
    if ((Get-Item -LiteralPath $rollbackDb -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Geri dönüş yedeği junction/symlink olamaz.' }
    $actualHash = (Get-FileHash -LiteralPath $rollbackDb -Algorithm SHA256).Hash
    if ($actualHash -cne [string]$db.RollbackBackupSha256) { throw 'Önceki DB geri dönüş yedeği SHA-256 ile eşleşmiyor.' }
    & pwsh -NoProfile -ExecutionPolicy Bypass -File $dbScript -BackupFile $rollbackDb `
        -PgHost $PgHost -PgPort $PgPort -PgUser $PgUser -NewDbName $db.Target -ConfirmedRollback
} elseif ($db.Status -eq 'DatabaseCreatedByOperation' -and -not [string]::IsNullOrWhiteSpace([string]$db.SourceBackupFile)) {
    $sourceBackup = [IO.Path]::GetFullPath([string]$db.SourceBackupFile)
    if (-not (Test-Path -LiteralPath $sourceBackup -PathType Leaf) -or
        ((Get-Item -LiteralPath $sourceBackup -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Makbuzdaki kaynak yedek yok veya junction/symlink.'
    }
    & pwsh -NoProfile -ExecutionPolicy Bypass -File $dbScript -BackupFile $sourceBackup `
        -PgHost $PgHost -PgPort $PgPort -PgUser $PgUser -NewDbName $db.Target `
        -OperationManifestPath $databaseManifestPath -DropCreatedDatabase -ConfirmedRollback
} else {
    throw 'DB operasyon makbuzu güvenli bir geri dönüş eylemi tanımlamıyor.'
}
if ($LASTEXITCODE -ne 0) { throw "DB geri dönüş adımı başarısız ($LASTEXITCODE); depolama değiştirilmedi." }

foreach ($name in @('uploads', 'keys', 'database')) {
    $target = Get-StorageTarget $StorageRoot $name
    if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
    if ($priorState.$name) {
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        $snapshot = Join-Path $snapshotRoot $name
        & robocopy $snapshot $target /E /COPY:DAT /DCOPY:DAT /R:2 /W:2 /XJ /NP /NDL /NJH /NJS
        if ($LASTEXITCODE -gt 7) { throw "DB geri döndü, ancak storage/$name kopyası tamamlanmadı. Snapshot korunuyor." }
    }
}

@{ Status='Recovered'; OperationId=(Split-Path -Leaf $operationFull); RecoveredAtUtc=[DateTime]::UtcNow.ToString('o') } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $operationFull 'recovered.json') -Encoding UTF8
Write-Host "Önceki DB ve depolama geri yüklendi. Makbuz: $operationFull\recovered.json" -ForegroundColor Green

param(
    [Parameter(Mandatory)][string]$OperationFolder,
    [Parameter(Mandatory)][string]$StorageRoot,
    [Parameter(Mandatory)][string]$ContentRoot,
    [Parameter(Mandatory)][string]$IisAppPoolName,
    [string]$PgHost = 'localhost',
    [string]$PgPort = '5432',
    [string]$PgUser = 'postgres'
)

$ErrorActionPreference = 'Stop'

function Get-ApplyTarget([string]$Name) {
    if ($Name -match '^storage-(uploads|Arsiv|Depo|data|logs|keys)$') {
        return [IO.Path]::GetFullPath((Join-Path $StorageRoot $Name.Substring(8)))
    }
    switch ($Name) {
        'app-LucaSettings' { return [IO.Path]::GetFullPath((Join-Path $ContentRoot 'Data/LucaSettings')) }
        'app-belgeler' { return [IO.Path]::GetFullPath((Join-Path $ContentRoot 'wwwroot/belgeler')) }
        'app-uploads' { return [IO.Path]::GetFullPath((Join-Path $ContentRoot 'wwwroot/uploads')) }
        default { throw "İzin verilmeyen geri yükleme kökü: $Name" }
    }
}

function Assert-NoReparseTree([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    $item = Get-Item -LiteralPath $Path -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Junction/symlink reddedildi: $Path" }
    if ($item.PSIsContainer) { foreach ($child in Get-ChildItem -LiteralPath $Path -Force) { Assert-NoReparseTree $child.FullName } }
}

function Copy-ApplyTree([string]$Source, [string]$Destination) {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    & robocopy $Source $Destination /E /COPY:DAT /DCOPY:DAT /R:2 /W:2 /XJ /NP /NDL /NJH /NJS
    if ($LASTEXITCODE -gt 7) { throw "Snapshot kopyası başarısız: $Source (robocopy=$LASTEXITCODE)" }
}

function Get-TreeFileManifest([string]$Root) {
    $base = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    @(Get-ChildItem -LiteralPath $Root -File -Force -Recurse | ForEach-Object {
        [pscustomobject]@{
            Path=$_.FullName.Substring($base.Length).Replace([IO.Path]::DirectorySeparatorChar, '/')
            Length=[long]$_.Length
            Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    } | Sort-Object Path)
}

function Assert-TreeMatchesManifest([string]$Root, [object[]]$Expected) {
    Assert-NoReparseTree $Root
    $actual = Get-TreeFileManifest $Root
    if ($actual.Count -ne $Expected.Count) { throw "Snapshot dosya sayısı değişti: $Root" }
    $expectedByPath = @{}
    foreach ($entry in $Expected) { $expectedByPath[[string]$entry.Path] = $entry }
    foreach ($entry in $actual) {
        if (-not $expectedByPath.ContainsKey($entry.Path)) { throw "Snapshot'ta beklenmeyen dosya: $($entry.Path)" }
        $expectedEntry = $expectedByPath[$entry.Path]
        if ([long]$expectedEntry.Length -ne $entry.Length -or [string]$expectedEntry.Sha256 -ine $entry.Sha256) {
            throw "Snapshot hash/uzunluk uyuşmazlığı: $($entry.Path)"
        }
    }
}

function Write-AtomicJson([string]$Path, [object]$Value, [int]$Depth = 8) {
    $directory = Split-Path -Parent $Path
    $temporary = Join-Path $directory ('.receipt-' + [guid]::NewGuid().ToString('N') + '.tmp')
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Value | ConvertTo-Json -Depth $Depth))
    $stream = [IO.FileStream]::new($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
    try { [IO.File]::Move($temporary, $Path) }
    catch { Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue; throw }
}

function Stop-TargetAppPool([string]$Name) {
    if ($Name -notmatch '^[A-Za-z0-9_. -]{1,128}$') { throw 'IIS uygulama havuzu adı geçersiz.' }
    $appcmd = Join-Path $env:windir 'System32/inetsrv/appcmd.exe'
    if (-not (Test-Path -LiteralPath $appcmd -PathType Leaf)) { throw 'IIS appcmd.exe bulunamadı.' }
    $state = (& $appcmd list apppool "/apppool.name:$Name" /text:state)
    if ($LASTEXITCODE -ne 0) { throw "IIS havuzu bulunamadı veya durumu okunamadı: $Name" }
    $state = ("$state").Trim()
    if ($state -eq 'Started') {
        & $appcmd stop apppool "/apppool.name:$Name" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "IIS havuzu durdurulamadı: $Name" }
        $state = (& $appcmd list apppool "/apppool.name:$Name" /text:state)
        if ($LASTEXITCODE -ne 0 -or ("$state").Trim() -ne 'Stopped') { throw "IIS havuzunun durduğu doğrulanamadı: $Name" }
    } elseif ($state -ne 'Stopped') { throw "IIS havuzu kararlı Started/Stopped durumunda değil: $state" }
}

function Assert-NoReparseAncestors([string]$Path) {
    for ($current = [IO.Path]::GetFullPath($Path); -not [string]::IsNullOrEmpty($current); $current = Split-Path -Parent $current) {
        if ((Test-Path -LiteralPath $current) -and ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Junction/symlink içeren yol reddedildi: $current"
        }
        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrEmpty($parent) -or $parent -ceq $current) { break }
    }
}

$StorageRoot = [IO.Path]::GetFullPath($StorageRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
$ContentRoot = [IO.Path]::GetFullPath($ContentRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
Assert-NoReparseAncestors $StorageRoot
Assert-NoReparseAncestors $ContentRoot
foreach ($root in @($StorageRoot, $ContentRoot)) {
    if ([string]::Equals($root, [IO.Path]::GetPathRoot($root).TrimEnd([IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'StorageRoot/ContentRoot disk kökü olamaz.'
    }
}
if ($StorageRoot.StartsWith($ContentRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    $ContentRoot.StartsWith($StorageRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    [string]::Equals($StorageRoot, $ContentRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'StorageRoot ve ContentRoot ayrı ve iç içe olmayan klasörler olmalıdır.'
}
$journalRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'MKFiloServis/OperationJournal')).TrimEnd([IO.Path]::DirectorySeparatorChar)
Assert-NoReparseAncestors $journalRoot
$operation = [IO.Path]::GetFullPath($OperationFolder)
if (-not [string]::Equals((Split-Path -Parent $operation), $journalRoot, [StringComparison]::OrdinalIgnoreCase) -or
    (Split-Path -Leaf $operation) -notmatch '^recovery-apply-[0-9a-f]{32}$') {
    throw 'Yalnızca doğrudan LocalAppData recovery-apply journal kabul edilir.'
}
if (-not (Test-Path -LiteralPath $operation -PathType Container)) { throw 'Apply journal klasörü bulunamadı.' }
Assert-NoReparseTree $operation
foreach ($marker in @('applied.json', 'recovered.json')) {
    if (Test-Path -LiteralPath (Join-Path $operation $marker)) { throw "Journal tamamlanmış; işlem tekrarlanmayacak: $marker" }
}
$statePath = Join-Path $operation 'apply-state.json'
$priorPath = Join-Path $operation 'files-before.json'
$hashesPath = Join-Path $operation 'files-before-hashes.json'
if (-not (Test-Path -LiteralPath $statePath -PathType Leaf) -or -not (Test-Path -LiteralPath $priorPath -PathType Leaf) -or
    ((Get-Item -LiteralPath $statePath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or
    ((Get-Item -LiteralPath $priorPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw 'Apply başlangıç makbuzu veya önceki dosya snapshot makbuzu eksik/geçersiz.'
}
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
$prior = Get-Content -LiteralPath $priorPath -Raw | ConvertFrom-Json
$hashesPathExists = Test-Path -LiteralPath $hashesPath
if ($hashesPathExists -and -not (Test-Path -LiteralPath $hashesPath -PathType Leaf)) {
    throw 'Snapshot hash makbuzu mevcut fakat normal dosya değil; journal reddedildi.'
}
$legacySnapshotJournal = -not $hashesPathExists
if (-not $legacySnapshotJournal -and ((Get-Item -LiteralPath $hashesPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw 'Snapshot hash makbuzu junction/symlink.'
}
$snapshotFiles = if ($legacySnapshotJournal) { $null } else { Get-Content -LiteralPath $hashesPath -Raw | ConvertFrom-Json }
if ($state.Status -cne 'ApplyStarted' -or $state.TargetDatabase -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') {
    throw 'Apply journal durumu veya DB adı geçersiz.'
}
$roots = @($state.FileRoots | ForEach-Object { [string]$_ } | Select-Object -Unique)
$snapshotRoot = Join-Path $operation 'files-before'
foreach ($name in $roots) {
    $target = Get-ApplyTarget $name
    Assert-NoReparseAncestors $target
    Assert-NoReparseTree $target
    if ($prior.$name -and -not (Test-Path -LiteralPath (Join-Path $snapshotRoot $name) -PathType Container)) {
        throw "Önceki dosya snapshot'ı eksik: $name"
    }
    if ($prior.$name) {
        $snapshot = Join-Path $snapshotRoot $name
        if ($legacySnapshotJournal) { Assert-NoReparseTree $snapshot }
        else {
            $expected = @($snapshotFiles.$name)
            if (-not $snapshotFiles.PSObject.Properties[$name]) { throw "Snapshot SHA-256 makbuzu eksik: $name" }
            Assert-TreeMatchesManifest $snapshot $expected
        }
    }
}

if ($legacySnapshotJournal) {
    Write-Warning 'Bu eski apply journal''ında snapshot SHA-256 makbuzu yok. Dosya snapshot''ı hash ile doğrulanamıyor.'
    if ((Read-Host 'Eski snapshot''ın doğru olduğunu ayrıca doğruladınız mı? Onay için ESKI-SNAPSHOT-ONAY yazın') -cne 'ESKI-SNAPSHOT-ONAY') {
        throw 'Eski snapshot doğrulama onayı verilmedi; hiçbir DB/dosya geri dönüşü yapılmadı.'
    }
}

if ((Read-Host 'Diğer arka plan yazımları ve entegrasyonlar durduruldu mu? Onay için ARKAPLAN-DURDU yazın') -cne 'ARKAPLAN-DURDU') {
    throw 'Arka plan yazımı onayı yok; hiçbir geri yükleme yapılmadı.'
}
Stop-TargetAppPool $IisAppPoolName

try {
    $dbManifestPath = Join-Path $operation 'database-operation.json'
    if ($state.DatabaseBackupIncluded) {
        if (-not (Test-Path -LiteralPath $dbManifestPath -PathType Leaf)) {
            throw 'DB restore başladı olarak journal edilmedi veya DB makbuzu oluşmadı; otomatik DB işlemi güvenli değil.'
        }
        $db = Get-Content -LiteralPath $dbManifestPath -Raw | ConvertFrom-Json
        if ($db.Target -cne [string]$state.TargetDatabase) { throw 'DB makbuzu apply journal hedefiyle uyuşmuyor.' }
        $dbScript = Join-Path $PSScriptRoot '01-db-restore.ps1'
        if (-not (Test-Path -LiteralPath $dbScript -PathType Leaf)) { throw '01-db-restore.ps1 bulunamadı.' }
        if ($db.DatabaseExistedBefore) {
            if ($db.Status -cne 'PreExistingDatabaseSnapshotReady' -or [string]$db.RollbackBackupSha256 -notmatch '^[A-Fa-f0-9]{64}$') {
                throw 'Önceden var olan DB için rollback makbuzu durumu/hash geçersiz.'
            }
            $rollbackDb = [IO.Path]::GetFullPath([string]$db.RollbackBackupPath)
            $expectedDb = [IO.Path]::GetFullPath((Join-Path $operation 'database-before.backup'))
            if (-not [string]::Equals($rollbackDb, $expectedDb, [StringComparison]::OrdinalIgnoreCase) -or
                -not (Test-Path -LiteralPath $rollbackDb -PathType Leaf) -or
                ((Get-Item -LiteralPath $rollbackDb -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw 'Önceki DB dump yolu geçersiz.'
            }
            if ((Get-FileHash -LiteralPath $rollbackDb -Algorithm SHA256).Hash -cne [string]$db.RollbackBackupSha256) {
                throw 'Önceki DB dump SHA-256 doğrulaması başarısız.'
            }
            & pwsh -NoProfile -ExecutionPolicy Bypass -File $dbScript -BackupFile $rollbackDb `
                -PgHost $PgHost -PgPort $PgPort -PgUser $PgUser -NewDbName $db.Target -ConfirmedRollback
        } elseif ($db.Status -eq 'DatabaseCreatedByOperation' -and [string]$db.DatabaseOid -match '^\d+$' -and
            ([string]::IsNullOrWhiteSpace([string]$db.SourceBackupSha256) -or [string]$db.SourceBackupSha256 -match '^[A-Fa-f0-9]{64}$')) {
            $sourceDb = [IO.Path]::GetFullPath([string]$db.SourceBackupFile)
            $expectedSource = [IO.Path]::GetFullPath((Join-Path $operation 'database-source.backup'))
            if (-not [string]::Equals($sourceDb, $expectedSource, [StringComparison]::OrdinalIgnoreCase) -or
                -not (Test-Path -LiteralPath $sourceDb -PathType Leaf) -or
                ((Get-Item -LiteralPath $sourceDb -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw 'Kaynak DB dump yolu geçersiz.'
            }
            if (-not [string]::IsNullOrWhiteSpace([string]$db.SourceBackupSha256) -and
                (Get-FileHash -LiteralPath $sourceDb -Algorithm SHA256).Hash -ine [string]$db.SourceBackupSha256) {
                throw 'Kaynak DB dump SHA-256 doğrulaması başarısız.'
            }
            & pwsh -NoProfile -ExecutionPolicy Bypass -File $dbScript -BackupFile $sourceDb `
                -PgHost $PgHost -PgPort $PgPort -PgUser $PgUser -NewDbName $db.Target `
                -OperationManifestPath $dbManifestPath -DropCreatedDatabase -ConfirmedRollback
        } else { throw 'DB makbuzu güvenli DB geri dönüşünü kanıtlamıyor.' }
        if ($LASTEXITCODE -ne 0) { throw "DB geri dönüş adımı başarısız ($LASTEXITCODE)." }
    }

    foreach ($name in $roots) {
        $target = Get-ApplyTarget $name
        Assert-NoReparseAncestors $target
        Assert-NoReparseTree $target
        if ($prior.$name) {
            $snapshot = Join-Path $snapshotRoot $name
            if ($legacySnapshotJournal) { Assert-NoReparseTree $snapshot }
            else { Assert-TreeMatchesManifest $snapshot @($snapshotFiles.$name) }
        }
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
        if ($prior.$name) {
            Copy-ApplyTree $snapshot $target
        }
    }
    Write-AtomicJson (Join-Path $operation 'recovered.json') @{
        Status='Recovered'; Operation=(Split-Path -Leaf $operation); RecoveredAtUtc=[DateTime]::UtcNow.ToString('o')
    }
    Write-Host "Önceki DB ve dosya durumu geri yüklendi. Journal: $operation" -ForegroundColor Green
} catch {
    $failureReceipt = 'recovery-failed-' + [guid]::NewGuid().ToString('N') + '.json'
    Write-AtomicJson (Join-Path $operation $failureReceipt) @{
        Status='RecoveryFailed'; Error=$_.Exception.Message; RecordedAtUtc=[DateTime]::UtcNow.ToString('o')
    }
    throw "Apply journal geri dönüşü doğrulanamadı; IIS'i kapalı tutun ve journal'ı koruyun: $operation. $($_.Exception.Message)"
}

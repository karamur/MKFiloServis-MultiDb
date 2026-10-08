param(
    [Parameter(Mandatory)][string]$PreparedFolder,
    [Parameter(Mandatory)][string]$StorageRoot,
    [Parameter(Mandatory)][string]$ContentRoot,
    [Parameter(Mandatory)][string]$NewDbName,
    [Parameter(Mandatory)][string]$IisAppPoolName,
    [string]$PgHost = 'localhost',
    [string]$PgPort = '5432',
    [string]$PgUser = 'postgres'
)

$ErrorActionPreference = 'Stop'
$maxExpandedBytes = 100GB

function Get-RootName([string]$ArchivePath) {
    switch -Regex ($ArchivePath) {
        '^storage/(uploads|Arsiv|Depo|data|logs|keys)/' { return "storage-$($Matches[1])" }
        '^application/Data/LucaSettings/' { return 'app-LucaSettings' }
        '^application/wwwroot/belgeler/' { return 'app-belgeler' }
        '^application/wwwroot/uploads/' { return 'app-uploads' }
        default { return $null }
    }
}

function Get-Target([string]$Name) {
    if ($Name.StartsWith('storage-')) { return [IO.Path]::GetFullPath((Join-Path $StorageRoot $Name.Substring(8))) }
    switch ($Name) {
        'app-LucaSettings' { return [IO.Path]::GetFullPath((Join-Path $ContentRoot 'Data/LucaSettings')) }
        'app-belgeler' { return [IO.Path]::GetFullPath((Join-Path $ContentRoot 'wwwroot/belgeler')) }
        'app-uploads' { return [IO.Path]::GetFullPath((Join-Path $ContentRoot 'wwwroot/uploads')) }
        default { throw "İzin verilmeyen hedef kök: $Name" }
    }
}

function Get-ArchivePrefix([string]$Name) {
    if ($Name.StartsWith('storage-')) { return "storage/$($Name.Substring(8))/" }
    switch ($Name) {
        'app-LucaSettings' { return 'application/Data/LucaSettings/' }
        'app-belgeler' { return 'application/wwwroot/belgeler/' }
        'app-uploads' { return 'application/wwwroot/uploads/' }
        default { throw "İzin verilmeyen kaynak kök: $Name" }
    }
}

function Assert-NoReparseTree([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    $item = Get-Item -LiteralPath $Path -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Junction/symlink reddedildi: $Path" }
    if ($item.PSIsContainer) {
        foreach ($child in Get-ChildItem -LiteralPath $Path -Force) { Assert-NoReparseTree $child.FullName }
    }
}

function Assert-NoReparseAncestors([string]$Path) {
    for ($current = [IO.Path]::GetFullPath($Path); -not [string]::IsNullOrEmpty($current); $current = Split-Path -Parent $current) {
        if (Test-Path -LiteralPath $current) {
            if ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Junction/symlink içeren yol reddedildi: $current"
            }
        }
        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrEmpty($parent) -or $parent -ceq $current) { break }
    }
}

function Copy-Tree([string]$Source, [string]$Destination) {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    & robocopy $Source $Destination /E /COPY:DAT /DCOPY:DAT /R:2 /W:2 /XJ /NP /NDL /NJH /NJS
    if ($LASTEXITCODE -gt 7) { throw "Klasör kopyası tamamlanmadı: $Source (robocopy=$LASTEXITCODE)" }
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
    if (-not (Test-Path -LiteralPath $appcmd -PathType Leaf)) { throw 'IIS appcmd.exe bulunamadı; değişiklik yapılmadı.' }
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

if ($NewDbName -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') { throw 'Hedef DB adı geçersiz.' }
$StorageRoot = [IO.Path]::GetFullPath($StorageRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
$ContentRoot = [IO.Path]::GetFullPath($ContentRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
Assert-NoReparseAncestors $StorageRoot
Assert-NoReparseAncestors $ContentRoot
foreach ($root in @($StorageRoot, $ContentRoot)) {
    $volumeRoot = [IO.Path]::GetPathRoot($root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ([string]::Equals($root, $volumeRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'StorageRoot/ContentRoot disk kökü olamaz.' }
}
if ($StorageRoot.StartsWith($ContentRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    $ContentRoot.StartsWith($StorageRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    [string]::Equals($StorageRoot, $ContentRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'StorageRoot ve ContentRoot ayrı ve iç içe olmayan klasörler olmalıdır.'
}
$prepared = [IO.Path]::GetFullPath($PreparedFolder).TrimEnd([IO.Path]::DirectorySeparatorChar)
Assert-NoReparseAncestors $prepared
if (-not (Test-Path -LiteralPath $prepared -PathType Container) -or (Get-Item -LiteralPath $prepared -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw 'Hazırlanmış kurtarma klasörü yok veya junction/symlink.'
}
$manifestPath = Join-Path $prepared 'recovery-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or (Get-Item -LiteralPath $manifestPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw 'Kurtarma manifesti yok veya junction/symlink.'
}
Assert-NoReparseTree $prepared
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.Format -cne 'MKFiloServis-RecoveryArchive-v1' -or @($manifest.Files).Count -gt 100000) { throw 'Kurtarma manifest biçimi/sayısı geçersiz.' }

$fileRoots = @{}
$databaseBackup = $null
$databaseBackupHash = $null
$totalBytes = [long]0
$seenPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($record in $manifest.Files) {
    $relative = [string]$record.Path
    if ([string]::IsNullOrWhiteSpace($relative) -or $relative.Contains('\') -or $relative.StartsWith('/') -or $relative.Split('/') -contains '..') {
        throw 'Manifestte geçersiz dosya yolu var.'
    }
    foreach ($segment in $relative.Split('/')) {
        $stem = ($segment -split '\.', 2)[0]
        if ([string]::IsNullOrWhiteSpace($segment) -or $segment -in @('.', '..') -or $segment.EndsWith('.') -or $segment.EndsWith(' ') -or
            $segment -match '[<>:"|?*]') {
            throw "Manifestte geçersiz yol bileşeni var: $relative"
        }
        if ($segment.ToCharArray() | Where-Object { [int]$_ -lt 32 } | Select-Object -First 1) { throw "Manifest yolu kontrol karakteri içeriyor: $relative" }
        if ($stem -match '^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9]|COM[¹²³]|LPT[¹²³])$') { throw "Windows aygıt adı manifestte reddedildi: $relative" }
    }
    if (-not $seenPaths.Add($relative) -or [string]$record.Sha256 -notmatch '^[A-Fa-f0-9]{64}$') { throw "Manifest yinelenen yol veya hatalı SHA-256 içeriyor: $relative" }
    if ($record.Length -lt 0 -or $record.Length -gt ($maxExpandedBytes - $totalBytes)) { throw 'Manifest toplam boyut sınırını aşıyor.' }
    $totalBytes += [long]$record.Length
    $source = [IO.Path]::GetFullPath((Join-Path $prepared $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)))
    if (-not $source.StartsWith($prepared + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Manifest girdisi yok/hedef dışı: $relative" }
    if ((Get-Item -LiteralPath $source -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Manifest girdisi junction/symlink: $relative" }
    $actual = Get-FileHash -LiteralPath $source -Algorithm SHA256
    if ((Get-Item -LiteralPath $source).Length -ne [long]$record.Length -or $actual.Hash -ine [string]$record.Sha256) {
        throw "Manifest boyutu/hash uyuşmuyor: $relative"
    }
    if ($relative -ceq 'database.backup') { $databaseBackup = $source; $databaseBackupHash = $actual.Hash; continue }
    if ($relative -match '^application/[^/]+\.json$') { continue } # Ortama özel ayarlar korunur.
    $rootName = Get-RootName $relative
    if (-not $rootName) { throw "Arşiv yolu bu uygulama aracı için izinli değil: $relative" }
    if (-not $fileRoots.ContainsKey($rootName)) { $fileRoots[$rootName] = @() }
    $fileRoots[$rootName] += [pscustomobject]@{ Relative=$relative; Source=$source; Length=[long]$record.Length; Sha256=[string]$record.Sha256 }
}
if (-not $databaseBackup -and $fileRoots.Count -eq 0) { throw 'Uygulanabilir DB veya kurtarma dosyası bulunamadı.' }
$hasDataProtectionKeys = @($manifest.Files | Where-Object { $_.Path -match '^storage/keys/key-[^/]+\.xml$' }).Count -gt 0
if (-not $hasDataProtectionKeys) { throw 'DataProtection key-*.xml dosyaları manifestte yok; kurtarma uygulanmadı.' }

Write-Host "Hazırlanan dosya sayısı: $($manifest.Files.Count); DB dump: $(if ($databaseBackup) {'var'} else {'yok'}); hedefler: $($fileRoots.Keys -join ', ')" -ForegroundColor Yellow
Write-Host 'application/*.json arşivden uygulanmayacak; hedef makinenin Production bağlantı ve sır ayarları korunacak.' -ForegroundColor Yellow
Write-Host 'Yedekteki key XML tek başına yeterli olmayabilir. Sertifika/DPAPI gereksinimini ayrı doğrulayın.' -ForegroundColor Yellow
if ((Read-Host 'Diğer arka plan yazımları ve entegrasyonlar durduruldu mu? Onay için ARKAPLAN-DURDU yazın') -cne 'ARKAPLAN-DURDU') {
    throw 'Arka plan yazımı onayı verilmedi; hiçbir değişiklik yapılmadı.'
}
if ((Read-Host 'Bu hedef için key ring sertifika/DPAPI koruması ve kurtarma malzemesi hazır mı? KEY-HAZIR yazın') -cne 'KEY-HAZIR') {
    throw 'Anahtar kurtarma onayı verilmedi; hiçbir değişiklik yapılmadı.'
}
$operationParent = Join-Path $env:LOCALAPPDATA 'MKFiloServis/OperationJournal'
New-Item -ItemType Directory -Path $operationParent -Force | Out-Null
$operationParent = [IO.Path]::GetFullPath($operationParent)
Assert-NoReparseAncestors $operationParent
if (-not (Test-Path -LiteralPath $operationParent -PathType Container) -or
    (Get-Item -LiteralPath $operationParent -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw 'Operasyon günlüğü klasörü yok veya junction/symlink; hedeflere dokunulmadı.'
}
$operation = Join-Path $operationParent ('recovery-apply-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $operation -ErrorAction Stop | Out-Null
Assert-NoReparseAncestors $operation
if ((Get-Item -LiteralPath $operation -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw 'Yeni operasyon günlüğü beklenmeyen junction/symlink; hedeflere dokunulmadı.'
}
Stop-TargetAppPool $IisAppPoolName
$snapshotRoot = Join-Path $operation 'files-before'
New-Item -ItemType Directory -Path $snapshotRoot -Force | Out-Null
$dbManifest = Join-Path $operation 'database-operation.json'
$dbRollback = Join-Path $operation 'database-before.backup'
if ($databaseBackup) {
    $stableDatabaseBackup = Join-Path $operation 'database-source.backup'
    Copy-Item -LiteralPath $databaseBackup -Destination $stableDatabaseBackup
    if ((Get-FileHash -LiteralPath $stableDatabaseBackup -Algorithm SHA256).Hash -ine $databaseBackupHash) {
        Remove-Item -LiteralPath $stableDatabaseBackup -Force -ErrorAction SilentlyContinue
        throw 'DB dump doğrulama ile kullanım arasında değişti; restore başlatılmadı.'
    }
    $databaseBackup = $stableDatabaseBackup
}
$prior = @{}
$snapshotFiles = @{}
foreach ($name in $fileRoots.Keys) {
    $target = Get-Target $name
    Assert-NoReparseAncestors $target
    Assert-NoReparseTree $target
    $prior[$name] = Test-Path -LiteralPath $target -PathType Container
    if ($prior[$name]) {
        $snapshot = Join-Path $snapshotRoot $name
        Copy-Tree $target $snapshot
        Assert-NoReparseTree $snapshot
        $snapshotFiles[$name] = @(Get-TreeFileManifest $snapshot)
    }
}
$priorPath = Join-Path $operation 'files-before.json'
$hashesPath = Join-Path $operation 'files-before-hashes.json'
$statePath = Join-Path $operation 'apply-state.json'
Write-AtomicJson $priorPath $prior
Write-AtomicJson $hashesPath $snapshotFiles 5
Write-AtomicJson $statePath @{
    Status='ApplyStarted'; DatabaseBackupIncluded=[bool]$databaseBackup
    TargetDatabase=$NewDbName; FileRoots=@($fileRoots.Keys); PreparedFolder=$prepared
    StartedAtUtc=[DateTime]::UtcNow.ToString('o')
} 4

$dbAttempted = $false
try {
    if ($databaseBackup) {
        $dbScript = Join-Path $PSScriptRoot '01-db-restore.ps1'
        $dbAttempted = $true
        & pwsh -NoProfile -ExecutionPolicy Bypass -File $dbScript -BackupFile $databaseBackup `
            -PgHost $PgHost -PgPort $PgPort -PgUser $PgUser -NewDbName $NewDbName `
            -RollbackBackupPath $dbRollback -OperationManifestPath $dbManifest -ConfirmedRestore
        if ($LASTEXITCODE -ne 0) { throw "DB restore başarısız ($LASTEXITCODE)." }
    }
    foreach ($name in $fileRoots.Keys) {
        $target = Get-Target $name
        Assert-NoReparseAncestors $target
        Assert-NoReparseTree $target
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        $prefix = Get-ArchivePrefix $name
        foreach ($entry in $fileRoots[$name]) {
            $suffix = $entry.Relative.Substring($prefix.Length).Replace('/', [IO.Path]::DirectorySeparatorChar)
            $targetFile = [IO.Path]::GetFullPath((Join-Path $target $suffix))
            if (-not $targetFile.StartsWith($target + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Manifest hedefi kök dışına çıkıyor: $($entry.Relative)"
            }
            $targetDirectory = Split-Path -Parent $targetFile
            New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
            Assert-NoReparseTree $targetDirectory
            Copy-Item -LiteralPath $entry.Source -Destination $targetFile
            if ((Get-Item -LiteralPath $targetFile).Length -ne $entry.Length -or
                (Get-FileHash -LiteralPath $targetFile -Algorithm SHA256).Hash -ine $entry.Sha256) {
                throw "Yazılan kurtarma dosyası manifestle uyuşmuyor: $($entry.Relative)"
            }
        }
    }
    Write-AtomicJson (Join-Path $operation 'applied.json') @{
        Status='Applied'; PreparedFolder=$prepared; DatabaseApplied=[bool]$databaseBackup
        CompletedAtUtc=[DateTime]::UtcNow.ToString('o')
    }
    Write-Host "Kurtarma paketi uygulandı. IIS'i başlatmadan önce uygulama yapılandırmasını ve dosya/DB ilişkilerini doğrulayın. Makbuz: $operation" -ForegroundColor Green
} catch {
    $failure = $_
    $rollbackErrors = [Collections.Generic.List[string]]::new()
    foreach ($name in $fileRoots.Keys) {
        try {
            $target = Get-Target $name
            Assert-NoReparseAncestors $target
            Assert-NoReparseTree $target
            if ($prior[$name]) {
                $snapshot = Join-Path $snapshotRoot $name
                Assert-TreeMatchesManifest $snapshot @($snapshotFiles[$name])
            }
            if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
            if ($prior[$name]) {
                Copy-Tree $snapshot $target
            }
        } catch { $rollbackErrors.Add("Dosya/$name geri dönüşü: $($_.Exception.Message)") }
    }
    if ($dbAttempted) {
        try {
            if (-not (Test-Path -LiteralPath $dbManifest -PathType Leaf)) {
                throw 'DB alt işlem makbuzu yok; güvenli DB geri dönüşü kanıtlanamıyor.'
            }
            $dbState = Get-Content -LiteralPath $dbManifest -Raw | ConvertFrom-Json
            if ($dbState.DatabaseExistedBefore) {
                $expectedRollback = [IO.Path]::GetFullPath($dbRollback)
                if ($dbState.Target -cne $NewDbName -or $dbState.Status -cne 'PreExistingDatabaseSnapshotReady' -or
                    [string]$dbState.RollbackBackupPath -cne $expectedRollback -or
                    [string]$dbState.RollbackBackupSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
                    -not (Test-Path -LiteralPath $dbRollback -PathType Leaf) -or
                    (Get-Item -LiteralPath $dbRollback -Force).Attributes -band [IO.FileAttributes]::ReparsePoint -or
                    (Get-FileHash -LiteralPath $dbRollback -Algorithm SHA256).Hash -ine [string]$dbState.RollbackBackupSha256) {
                    throw 'Önceki DB rollback dump makbuz/yol/hash doğrulaması başarısız.'
                }
                & pwsh -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot '01-db-restore.ps1') `
                    -BackupFile $dbRollback -PgHost $PgHost -PgPort $PgPort -PgUser $PgUser -NewDbName $NewDbName -ConfirmedRollback
            } elseif ($dbState.Target -ceq $NewDbName -and $dbState.Status -eq 'DatabaseCreatedByOperation' -and
                [string]$dbState.DatabaseOid -match '^\d+$' -and [string]$dbState.SourceBackupSha256 -match '^[A-Fa-f0-9]{64}$') {
                $expectedSource = [IO.Path]::GetFullPath((Join-Path $operation 'database-source.backup'))
                if (-not [string]::Equals([IO.Path]::GetFullPath([string]$dbState.SourceBackupFile), $expectedSource, [StringComparison]::OrdinalIgnoreCase) -or
                    -not (Test-Path -LiteralPath $expectedSource -PathType Leaf) -or
                    (Get-Item -LiteralPath $expectedSource -Force).Attributes -band [IO.FileAttributes]::ReparsePoint -or
                    (Get-FileHash -LiteralPath $expectedSource -Algorithm SHA256).Hash -ine [string]$dbState.SourceBackupSha256) {
                    throw 'Yeni DB kaynak dump SHA-256 doğrulaması başarısız.'
                }
                & pwsh -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot '01-db-restore.ps1') `
                    -BackupFile $expectedSource -PgHost $PgHost -PgPort $PgPort -PgUser $PgUser -NewDbName $NewDbName `
                    -OperationManifestPath $dbManifest -DropCreatedDatabase -ConfirmedRollback
            } else { throw 'DB makbuzu geri dönüş eylemini kanıtlamıyor.' }
            if ($LASTEXITCODE -ne 0) { throw "DB geri dönüş başarısız ($LASTEXITCODE)." }
        } catch { $rollbackErrors.Add("DB geri dönüşü: $($_.Exception.Message)") }
    }
    $status = if ($rollbackErrors.Count) { 'RollbackFailed' } else { 'RolledBack' }
    Write-AtomicJson (Join-Path $operation 'rollback-result.json') @{
        Status=$status; Error=$failure.Exception.Message; RollbackErrors=@($rollbackErrors)
        RecordedAtUtc=[DateTime]::UtcNow.ToString('o')
    } 5
    if ($rollbackErrors.Count) { throw "Uygulama başarısız ve geri dönüş doğrulanamadı. Uygulamayı kapalı tutun. Snapshot: $operation. $($rollbackErrors -join '; ')" }
    throw "Uygulama başarısız; eski DB ve dosyalar geri yüklendi. Snapshot: $operation. Hata: $($failure.Exception.Message)"
}

param(
    [string]$TargetDir = 'C:\MKFiloServis\IIS',
    [string]$BackupRoot = 'C:\MKFiloServis_yedekleme\deploy',
    [string]$SiteName = '',
    [ValidateSet('Install','Update')]
    [string]$Mode = 'Update'
)

Write-Host "[MK] Calisma modu: $Mode" -ForegroundColor Yellow

$ErrorActionPreference = 'Stop'

$PackageDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$ReleaseBackupRoot = Join-Path $BackupRoot $Timestamp
$LatestBackupRoot = Join-Path $BackupRoot 'latest'
$AppOfflinePath = Join-Path $TargetDir 'app_offline.htm'

function Write-Step([string]$Message) {
    Write-Host "[MK] $Message" -ForegroundColor Cyan
}

function Invoke-Robocopy([string]$Source, [string]$Destination, [string[]]$ExtraArgs) {
    if (-not (Test-Path $Source)) {
        return
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    $arguments = @($Source, $Destination, '/E', '/R:1', '/W:1', '/NFL', '/NDL', '/NJH', '/NJS', '/NP') + $ExtraArgs
    & robocopy @arguments | Out-Null
    if ($LASTEXITCODE -gt 7) {
        throw "Robocopy hatası. Çıkış kodu: $LASTEXITCODE"
    }
}

function Get-PgDumpPath {
    $command = Get-Command 'pg_dump.exe' -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $fallbacks = @(
        'C:\Program Files\PostgreSQL\16\bin\pg_dump.exe',
        'C:\Program Files\PostgreSQL\15\bin\pg_dump.exe',
        'C:\Program Files\PostgreSQL\14\bin\pg_dump.exe'
    )

    return $fallbacks | Where-Object { Test-Path $_ } | Select-Object -First 1
}

function Read-DatabaseSettingsForInstall {
    Write-Host ''
    Write-Host 'Kullanilacak veritabani turunu secin:' -ForegroundColor Yellow
    Write-Host '  1) SQLite'
    Write-Host '  2) PostgreSQL'
    Write-Host '  3) Microsoft SQL Server (MSSQL)'
    $selection = Read-Host 'Secim (1/2/3)'

    switch ($selection.Trim()) {
        '1' {
            $databaseName = Read-Host 'SQLite DB dosya yolu (varsayilan: App_Data/MKFiloServis.db)'
            if ([string]::IsNullOrWhiteSpace($databaseName)) { $databaseName = 'App_Data/MKFiloServis.db' }
            return [ordered]@{
                Id = 0; Provider = 1; CanonicalProvider = 2; Host = ''; Port = 0
                DatabaseName = $databaseName.Trim(); Username = ''; Password = ''
                UseIntegratedSecurity = $false; LastUpdated = [DateTime]::UtcNow.ToString('o')
            }
        }
        '2' {
            $hostName = Read-Host 'PostgreSQL sunucu adresi'
            if ([string]::IsNullOrWhiteSpace($hostName)) { throw 'PostgreSQL sunucu adresi bos olamaz.' }
            $portText = Read-Host 'PostgreSQL portu (varsayilan: 5432)'
            if ([string]::IsNullOrWhiteSpace($portText)) { $portText = '5432' }
            $port = 0
            if (-not [int]::TryParse($portText, [ref]$port) -or $port -lt 1 -or $port -gt 65535) { throw 'PostgreSQL portu 1-65535 arasinda olmalidir.' }
            $databaseName = Read-Host 'PostgreSQL veritabani adi'
            $username = Read-Host 'PostgreSQL kullanici adi'
            if ([string]::IsNullOrWhiteSpace($databaseName) -or [string]::IsNullOrWhiteSpace($username)) { throw 'Veritabani adi ve kullanici adi bos olamaz.' }
            $securePassword = Read-Host 'PostgreSQL parolasi' -AsSecureString
            $passwordPointer = [IntPtr]::Zero
            try {
                $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
                $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
            }
            finally {
                if ($passwordPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer) }
                if ($securePassword) { $securePassword.Dispose() }
            }
            return [ordered]@{
                Id = 0; Provider = 2; CanonicalProvider = 2; Host = $hostName.Trim(); Port = $port
                DatabaseName = $databaseName.Trim(); Username = $username.Trim(); Password = $password
                UseIntegratedSecurity = $false; LastUpdated = [DateTime]::UtcNow.ToString('o')
            }
        }
        '3' { throw 'MSSQL secildi; ancak otomatik sema/migration ve audit kurulumu henuz SQL Server icin desteklenmiyor. PostgreSQL veya SQLite secin.' }
        default { throw 'Gecersiz veritabani secimi. Kurulum degisiklik yapmadan durduruldu.' }
    }
}

function Write-DatabaseSettings([System.Collections.IDictionary]$Settings, [string]$Path) {
    $temporaryPath = "$Path.tmp"
    $json = $Settings | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($temporaryPath, $json, [System.Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temporaryPath -Destination $Path -Force
}

function Backup-Database([string]$SettingsPath, [string]$BackupDir) {
    if (-not (Test-Path $SettingsPath)) {
        Write-Step "dbsettings.json bulunamadı, veritabanı bağlantı ayarı yedeklenemedi."
        return
    }

    New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
    Copy-Item $SettingsPath (Join-Path $BackupDir 'dbsettings.json') -Force

    $dbSettings = Get-Content $SettingsPath -Raw | ConvertFrom-Json
    $provider = [int]$dbSettings.Provider
    $databaseName = [string]$dbSettings.DatabaseName

    switch ($provider) {
        1 {
            if ([string]::IsNullOrWhiteSpace($databaseName)) {
                Write-Step 'SQLite veritabanı adı boş, SQLite yedeği atlandı.'
                return
            }

            $databasePath = if ([System.IO.Path]::IsPathRooted($databaseName)) {
                $databaseName
            } else {
                Join-Path $TargetDir $databaseName
            }

            if (Test-Path $databasePath) {
                Copy-Item $databasePath (Join-Path $BackupDir ([System.IO.Path]::GetFileName($databasePath))) -Force
                Write-Step "SQLite veritabanı yedeği alındı: $databasePath"
            } else {
                Write-Step "SQLite veritabanı dosyası bulunamadı: $databasePath"
            }
        }
        2 {
            $pgDumpPath = Get-PgDumpPath
            if (-not $pgDumpPath) {
                Write-Step 'pg_dump bulunamadı, PostgreSQL için yalnızca dbsettings.json yedeği alındı.'
                return
            }

            $dumpPath = Join-Path $BackupDir 'database.sql'
            $env:PGPASSWORD = [string]$dbSettings.Password
            try {
                & $pgDumpPath -h $dbSettings.Host -p $dbSettings.Port -U $dbSettings.Username -d $dbSettings.DatabaseName -f $dumpPath --no-password
                if ($LASTEXITCODE -ne 0) {
                    throw "pg_dump başarısız oldu. Çıkış kodu: $LASTEXITCODE"
                }
                Write-Step "PostgreSQL yedeği oluşturuldu: $dumpPath"
            }
            finally {
                Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
            }
        }
        Default {
            Write-Step 'Bu sağlayıcı için otomatik veritabanı dump işlemi tanımlı değil. dbsettings.json yedeği alındı.'
        }
    }
}

Write-Step 'Kurulum klasörleri hazırlanıyor.'
$selectedDatabaseSettings = if ($Mode -eq 'Install') { Read-DatabaseSettingsForInstall } else { $null }
New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null
New-Item -ItemType Directory -Force -Path $ReleaseBackupRoot | Out-Null

$existingDbSettings = Join-Path $TargetDir 'dbsettings.json'
$packageDbSettings = Join-Path $PackageDir 'dbsettings.json'
$dbSettingsForBackup = if (Test-Path $existingDbSettings) { $existingDbSettings } elseif (Test-Path $packageDbSettings) { $packageDbSettings } else { $null }

if (Get-ChildItem -Path $TargetDir -Force -ErrorAction SilentlyContinue | Select-Object -First 1) {
    Write-Step 'Mevcut yayın klasörü yedekleniyor.'
    Invoke-Robocopy -Source $TargetDir -Destination (Join-Path $ReleaseBackupRoot 'site') -ExtraArgs @('/XF', 'app_offline.htm')
    if (Test-Path $LatestBackupRoot) {
        Remove-Item $LatestBackupRoot -Recurse -Force
    }
    Invoke-Robocopy -Source $TargetDir -Destination (Join-Path $LatestBackupRoot 'site') -ExtraArgs @('/XF', 'app_offline.htm')
}

if ($dbSettingsForBackup) {
    Write-Step 'Veritabanı yedeği alınıyor/güncelleniyor.'
    Backup-Database -SettingsPath $dbSettingsForBackup -BackupDir (Join-Path $ReleaseBackupRoot 'database')

    $latestDatabaseBackup = Join-Path $LatestBackupRoot 'database'
    if (Test-Path $latestDatabaseBackup) {
        Remove-Item $latestDatabaseBackup -Recurse -Force
    }
    Backup-Database -SettingsPath $dbSettingsForBackup -BackupDir $latestDatabaseBackup
}

Write-Step 'Uygulama çevrimdışı dosyası oluşturuluyor.'
Set-Content -Path $AppOfflinePath -Value '<html><body><h2>MK Filo Servis guncelleniyor...</h2></body></html>' -Encoding UTF8

try {
    Write-Step 'Paket dosyaları hedef klasöre kopyalanıyor.'
    if ($Mode -eq 'Install') {
        Write-Step 'KURULUM modu: secilen veritabani ayari yazilacak; mevcut veritabani dosyalari korunacak.'
        $excludeFiles = @('/XF', 'dbsettings.json')
    }
    else {
        $excludeFiles = if (Test-Path $existingDbSettings) { @('/XF', 'dbsettings.json') } else { @() }
    }

    Invoke-Robocopy -Source $PackageDir -Destination $TargetDir -ExtraArgs $excludeFiles

    if ($Mode -eq 'Install') {
        Write-DatabaseSettings -Settings $selectedDatabaseSettings -Path $existingDbSettings
        Write-Step "Veritabani ayari kaydedildi: $($selectedDatabaseSettings.DatabaseName) (Provider $($selectedDatabaseSettings.Provider))"
    }

    if (-not (Test-Path $existingDbSettings) -and (Test-Path $packageDbSettings)) {
        Copy-Item $packageDbSettings $existingDbSettings -Force
        Write-Step 'dbsettings.json ilk kurulum için hedef klasöre kopyalandı.'
    }

    if (-not [string]::IsNullOrWhiteSpace($SiteName)) {
        Write-Step "IIS site adı alındı: $SiteName"
    }
}
finally {
    if (Test-Path $AppOfflinePath) {
        Remove-Item $AppOfflinePath -Force
    }
}

Write-Step "Kurulum/güncelleme tamamlandı. Hedef: $TargetDir"
Write-Step "Zaman damgalı yedek: $ReleaseBackupRoot"
Write-Step "Güncel yedek: $LatestBackupRoot"

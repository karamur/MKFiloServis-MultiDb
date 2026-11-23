# MKFiloServis Web MSI custom action yardimcisi.
# WixQuietExec64 ile yonetici (LocalSystem) baglaminda calisir.
# Modlar:
#   Stop      : IIS havuzu/sitesini durdurur (dosya kilidi olmasin diye InstallValidate oncesi).
#   Install   : IIS ozelliklerini etkinlestirir, havuz/site olusturur, dbsettings.json yazar
#               (yalnizca mevcut PostgreSQL'e baglanti; CREATE DATABASE / yedek yukleme YOK),
#               configure-iis-startup.ps1 calistirir ve siteyi baslatir.
#   Upgrade   : Install ile ayni; dbsettings.json'a dokunmaz.
#   Uninstall : Site ve havuzu kaldirir; dosyalara ve veritabanina dokunmaz.
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidateSet('Stop', 'Install', 'Upgrade', 'Uninstall')] [string]$Mode,
    [Parameter(Mandatory)] [string]$AppPath,
    [string]$StorageRoot = 'C:\MKFiloServis_yedekleme',
    [string]$AppPoolName = 'MKFiloServis',
    [string]$SiteName = 'MKFiloServis',
    [int]$Port = 5050,
    [string]$Server = 'localhost',
    [int]$DbPort = 5432,
    # 'Db' adi kullanilamaz: ortak -Debug parametresinin 'db' takma adiyla cakisir.
    [string]$DbName = 'MKFiloServis',
    [string]$User = 'postgres',
    [string]$Pwd = ''
)

$ErrorActionPreference = 'Stop'
$AppPath = [IO.Path]::GetFullPath($AppPath)
$toolsPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$logDir = Join-Path $AppPath 'logs'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$logFile = Join-Path $logDir 'install-web.log'
$appCmd = Join-Path $env:windir 'System32\inetsrv\appcmd.exe'
$dism = Join-Path $env:windir 'System32\dism.exe'
$icacls = Join-Path $env:windir 'System32\icacls.exe'

function Write-Log([string]$Message) {
    $line = '{0:u} [{1}] {2}' -f (Get-Date), $Mode, $Message
    Write-Host $line
    try { Add-Content -LiteralPath $logFile -Value $line -Encoding UTF8 } catch { }
}

function Invoke-Tool([string]$FilePath, [string[]]$Arguments, [int[]]$AllowedExitCodes = @(0)) {
    Write-Log ("> {0} {1}" -f (Split-Path -Leaf $FilePath), ($Arguments -join ' '))
    $output = & $FilePath @Arguments 2>&1
    $code = $LASTEXITCODE
    foreach ($item in $output) { Write-Log ("  " + [string]$item) }
    if ($AllowedExitCodes -notcontains $code) {
        throw "Komut basarisiz oldu ($code): $(Split-Path -Leaf $FilePath) $($Arguments -join ' ')"
    }
    return $code
}

function Stop-Web {
    if (-not (Test-Path -LiteralPath $appCmd)) { Write-Log 'IIS yok; durdurma atlandi.'; return }
    foreach ($cmdArgs in @(@('stop', 'site', "/site.name:$SiteName"), @('stop', 'apppool', "/apppool.name:$AppPoolName"))) {
        try { Invoke-Tool $appCmd $cmdArgs @(0, 1, 1062, 183) | Out-Null } catch { Write-Log "Uyari: $($_.Exception.Message)" }
    }
    # w3wp'nin dosya kilidini birakmasi icin kisa bekleme.
    Start-Sleep -Seconds 3
}

function Enable-Iis {
    if (Test-Path -LiteralPath $appCmd) { Write-Log 'IIS zaten etkin.'; return }
    Write-Log 'IIS ozellikleri etkinlestiriliyor (DISM)...'
    $features = @('IIS-WebServerRole', 'IIS-WebServer', 'IIS-CommonHttpFeatures', 'IIS-StaticContent',
        'IIS-DefaultDocument', 'IIS-HttpErrors', 'IIS-HttpLogging', 'IIS-RequestFiltering',
        'IIS-ApplicationInit', 'IIS-WebSockets', 'IIS-ManagementConsole')
    $cmdArgs = @('/online', '/enable-feature') + ($features | ForEach-Object { "/featurename:$_" }) + @('/all', '/norestart')
    # 3010: yeniden baslatma gerekiyor; kurulum devam edebilir.
    $code = Invoke-Tool $dism $cmdArgs @(0, 3010)
    if ($code -eq 3010) { Write-Log 'DISM yeniden baslatma istiyor; kurulum sonrasi bilgisayari yeniden baslatin.' }
    if (-not (Test-Path -LiteralPath $appCmd)) { throw 'IIS etkinlestirildi ancak appcmd.exe bulunamadi.' }
}

function Set-IisSite {
    Invoke-Tool $appCmd @('add', 'apppool', "/name:$AppPoolName", '/managedRuntimeVersion:', '/startMode:AlwaysRunning') @(0, 183) | Out-Null
    Invoke-Tool $appCmd @('set', 'apppool', "/apppool.name:$AppPoolName", '/managedRuntimeVersion:', '/startMode:AlwaysRunning',
        '/processModel.identityType:ApplicationPoolIdentity', '/enable32BitAppOnWin64:false') | Out-Null
    Invoke-Tool $appCmd @('add', 'site', "/name:$SiteName", "/physicalPath:$AppPath", "/bindings:http/*:${Port}:") @(0, 183) | Out-Null
    Invoke-Tool $appCmd @('set', 'site', "/site.name:$SiteName", "/[path='/'].applicationPool:$AppPoolName") | Out-Null
    Invoke-Tool $appCmd @('set', 'vdir', "$SiteName/", "/physicalPath:$AppPath") | Out-Null
}

function Test-PostgreSqlEndpoint {
    Write-Log "PostgreSQL sunucusuna erisim deneniyor: ${Server}:$DbPort"
    $client = New-Object Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect($Server, $DbPort, $null, $null)
        if (-not $async.AsyncWaitHandle.WaitOne(8000) -or -not $client.Connected) {
            throw "PostgreSQL sunucusuna ulasilamadi (${Server}:$DbPort). Sunucunun calistigini ve portun acik oldugunu kontrol edin."
        }
        $client.EndConnect($async)
    }
    finally { $client.Dispose() }
    Write-Log 'PostgreSQL portu erisilebilir.'
}

function Write-DbSettings {
    $settingsPath = Join-Path $AppPath 'dbsettings.json'
    if (Test-Path -LiteralPath $settingsPath) { Write-Log 'dbsettings.json zaten var; korunuyor.'; return }
    if ([string]::IsNullOrWhiteSpace($Server) -or [string]::IsNullOrWhiteSpace($DbName) -or [string]::IsNullOrWhiteSpace($User)) {
        throw 'PostgreSQL sunucu, veritabani adi ve kullanici adi bos olamaz.'
    }
    Test-PostgreSqlEndpoint
    $settings = [ordered]@{
        Id                    = 0
        Provider              = 2
        CanonicalProvider     = 2
        Host                  = $Server.Trim()
        Port                  = $DbPort
        DatabaseName          = $DbName.Trim()
        Username              = $User.Trim()
        Password              = $Pwd
        UseIntegratedSecurity = $false
        LastUpdated           = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    }
    $json = $settings | ConvertTo-Json
    [IO.File]::WriteAllText($settingsPath, $json, (New-Object Text.UTF8Encoding($false)))
    Write-Log "dbsettings.json yazildi (mevcut veritabanina baglanti: $($settings.Host):$($settings.Port)/$($settings.DatabaseName))."
}

function Protect-DbSettings {
    $settingsPath = Join-Path $AppPath 'dbsettings.json'
    if (-not (Test-Path -LiteralPath $settingsPath)) { return }
    Invoke-Tool $icacls @($settingsPath, '/inheritance:r', '/grant', '*S-1-5-18:F', '*S-1-5-32-544:F', "IIS AppPool\${AppPoolName}:R") | Out-Null
}

function Start-Web {
    Invoke-Tool $appCmd @('start', 'apppool', "/apppool.name:$AppPoolName") @(0, 183, 1062) | Out-Null
    Invoke-Tool $appCmd @('start', 'site', "/site.name:$SiteName") @(0, 183, 1062) | Out-Null
}

try {
    Write-Log "Baslangic. AppPath=$AppPath StorageRoot=$StorageRoot Port=$Port"
    switch ($Mode) {
        'Stop' { Stop-Web }
        'Uninstall' {
            if (Test-Path -LiteralPath $appCmd) {
                Stop-Web
                try { Invoke-Tool $appCmd @('delete', 'site', "/site.name:$SiteName") @(0, 1, 183) | Out-Null } catch { Write-Log "Uyari: $($_.Exception.Message)" }
                try { Invoke-Tool $appCmd @('delete', 'apppool', "/apppool.name:$AppPoolName") @(0, 1, 183) | Out-Null } catch { Write-Log "Uyari: $($_.Exception.Message)" }
            }
            Write-Log 'Veritabani, dbsettings.json ve depolama klasoru korunuyor.'
        }
        default {
            Enable-Iis
            Set-IisSite
            if ($Mode -eq 'Install') { Write-DbSettings }
            Protect-DbSettings
            $startupScript = Join-Path $toolsPath 'configure-iis-startup.ps1'
            Write-Log 'configure-iis-startup.ps1 calistiriliyor...'
            & $startupScript -AppPoolName $AppPoolName -SiteName $SiteName -Port $Port -AppPath $AppPath -StorageRoot $StorageRoot -PreserveStorageRoot:($Mode -eq 'Upgrade') 2>&1 |
                ForEach-Object { Write-Log ("  " + [string]$_) }
            Start-Web
        }
    }
    Write-Log 'Tamamlandi.'
    exit 0
}
catch {
    Write-Log ("HATA: " + $_.Exception.Message)
    exit 1
}

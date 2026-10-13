# Run from an elevated Windows PowerShell session on the IIS computer.
# The generated JWT secret is stored only in this machine's IIS application pool configuration.
[CmdletBinding()]
param(
    [string]$AppPoolName = 'MKFiloServis',
    [string]$SiteName = 'MKFiloServis',
    [ValidateRange(1, 65535)] [int]$Port = 5050,
    [string]$AppPath = 'C:\MKFiloServis\app',
    [string]$StorageRoot = 'C:\MKFiloServis_yedekleme',
    [switch]$RestartAppPool
)

$ErrorActionPreference = 'Stop'

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Bu betik yonetici yetkili PowerShell oturumunda calistirilmalidir.'
}

# IIS cannot bind the configured port while a local Kestrel/development process
# owns it. Detect that before changing IIS settings so setup can report the cause.
$portOwners = @(Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue |
    Where-Object { $_.OwningProcess -ne 4 } |
    Select-Object -ExpandProperty OwningProcess -Unique)
if ($portOwners.Count -gt 0) {
    $ownerDetails = foreach ($processId in $portOwners) {
        $owner = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($owner) { '{0} (PID {1})' -f $owner.ProcessName, $processId }
        else { 'PID {0}' -f $processId }
    }
    throw "HTTP $Port portu baska bir uygulama tarafindan kullaniliyor: $($ownerDetails -join ', '). Yerel uygulamayi kapatin ve kurulumu yeniden deneyin."
}

if (-not (Test-Path -LiteralPath $AppPath -PathType Container)) {
    throw "Uygulama klasoru bulunamadi: $AppPath"
}
$AppPath = [IO.Path]::GetFullPath($AppPath)
$StorageRoot = [IO.Path]::GetFullPath($StorageRoot)
$logPath = Join-Path $AppPath 'logs'
New-Item -ItemType Directory -Path $logPath -Force | Out-Null
$diagnosticPath = Join-Path $logPath 'configure-iis-startup.log'
trap {
    try {
        $details = ($_ | Format-List * -Force | Out-String)
        Add-Content -LiteralPath $diagnosticPath -Value ("`r`n--- {0:u} ---`r`n{1}" -f (Get-Date), $details) -Encoding UTF8
    }
    catch { }
    throw
}
foreach ($relative in @('', 'uploads', 'Arsiv', 'Depo', 'data', 'database', 'keys', 'logs')) {
    $directory = if ($relative -eq '') { $StorageRoot } else { Join-Path $StorageRoot $relative }
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$administrationDll = Join-Path $env:windir 'System32\inetsrv\Microsoft.Web.Administration.dll'
if (-not (Test-Path -LiteralPath $administrationDll)) {
    throw 'IIS yonetim kutuphanesi bulunamadi. IIS kurulumu tamamlanmamis olabilir.'
}
Add-Type -Path $administrationDll

$manager = New-Object Microsoft.Web.Administration.ServerManager
try {
    $pools = $manager.GetApplicationHostConfiguration().GetSection('system.applicationHost/applicationPools').GetCollection()
    $pool = $null
    foreach ($item in $pools) {
        if ($item.ElementTagName -eq 'add' -and [string]$item.GetAttributeValue('name') -eq $AppPoolName) {
            $pool = $item
            break
        }
    }
    if ($null -eq $pool) {
        throw "IIS uygulama havuzu bulunamadi: $AppPoolName"
    }

    # Once persist the startup-critical variables. Site/binding configuration can
    # fail independently; it must never leave Production without its JWT secret.
    $variables = $pool.GetCollection('environmentVariables')
    $jwtEntry = $null
    foreach ($item in $variables) {
        if ([string]$item.GetAttributeValue('name') -eq 'Jwt__Secret') {
            $jwtEntry = $item
            break
        }
    }

    $existingSecret = if ($null -eq $jwtEntry) { '' } else { [string]$jwtEntry.GetAttributeValue('value') }
    $secretInvalid = [string]::IsNullOrWhiteSpace($existingSecret) -or
        [Text.Encoding]::UTF8.GetByteCount($existingSecret) -lt 32 -or
        $existingSecret.StartsWith('REPLACE_', [StringComparison]::OrdinalIgnoreCase)
    if ($secretInvalid) {
        $bytes = New-Object byte[] 48
        $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        try {
            $random.GetBytes($bytes)
        }
        finally {
            $random.Dispose()
        }
        $secret = [Convert]::ToBase64String($bytes)
        if ($null -eq $jwtEntry) {
            $jwtEntry = $variables.CreateElement('add')
            $jwtEntry.SetAttributeValue('name', 'Jwt__Secret')
            $jwtEntry.SetAttributeValue('value', $secret)
            $variables.Add($jwtEntry)
        }
        else {
            $jwtEntry.SetAttributeValue('value', $secret)
        }
        Write-Host 'IIS uygulama havuzu icin gecerli rastgele JWT secret olusturuldu.'
    }
    else {
        Write-Host 'IIS uygulama havuzundaki mevcut JWT secret korundu.'
    }

    $storageEntry = $null
    foreach ($item in $variables) {
        if ([string]$item.GetAttributeValue('name') -eq 'CRMFILO_STORAGE_ROOT') {
            $storageEntry = $item
            break
        }
    }
    if ($null -eq $storageEntry) {
        $storageEntry = $variables.CreateElement('add')
        $storageEntry.SetAttributeValue('name', 'CRMFILO_STORAGE_ROOT')
        $storageEntry.SetAttributeValue('value', $StorageRoot)
        $variables.Add($storageEntry)
    }
    else {
        $storageEntry.SetAttributeValue('value', $StorageRoot)
    }

    $manager.CommitChanges()
    Write-Host 'IIS uygulama havuzunun baslangic degiskenleri kaydedildi.'

    $site = $manager.Sites[$SiteName]
    if ($null -eq $site) {
        throw "IIS sitesi bulunamadi: $SiteName"
    }
    $rootApplication = $site.Applications['/']
    if ($null -eq $rootApplication -or $null -eq $rootApplication.VirtualDirectories['/']) {
        throw "IIS kok uygulamasi bulunamadi: $SiteName"
    }
    $targetBinding = "*:$($Port):"
    foreach ($otherSite in $manager.Sites) {
        if ($otherSite.Name -eq $SiteName) { continue }
        foreach ($binding in $otherSite.Bindings) {
            if ($binding.Protocol -eq 'http' -and $binding.BindingInformation -eq $targetBinding) {
                throw "HTTP $Port portu baska bir IIS sitesinde kullaniliyor: $($otherSite.Name)"
            }
        }
    }
    $rootApplication.ApplicationPoolName = $AppPoolName
    $rootApplication.VirtualDirectories['/'].PhysicalPath = $AppPath

    $hasTargetBinding = $false
    foreach ($binding in @($site.Bindings)) {
        if ($binding.Protocol -ne 'http') { continue }
        if ($binding.BindingInformation -eq $targetBinding) {
            $hasTargetBinding = $true
        }
        else {
            $site.Bindings.Remove($binding)
        }
    }
    if (-not $hasTargetBinding) {
        $site.Bindings.Add($targetBinding, 'http') | Out-Null
    }
    $manager.CommitChanges()
    Write-Host "IIS sitesi: $AppPath; depolama: $StorageRoot; HTTP portu: $Port"
}
finally {
    $manager.Dispose()
}

New-Item -ItemType Directory -Path $logPath -Force | Out-Null
$icacls = Join-Path $env:windir 'System32\icacls.exe'
& $icacls $StorageRoot /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' "IIS AppPool\${AppPoolName}:(OI)(CI)M" | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Depolama klasorunun erisim izinleri ayarlanamadi: $StorageRoot"
}
& $icacls $logPath /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' "IIS AppPool\${AppPoolName}:(OI)(CI)M" | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "IIS stdout log klasorunun erisim izinleri ayarlanamadi: $logPath"
}
Write-Host "IIS stdout log klasoru hazir: $logPath"

if ($RestartAppPool) {
    Import-Module WebAdministration -ErrorAction Stop
    if ((Get-WebAppPoolState -Name $AppPoolName).Value -eq 'Started') {
        Restart-WebAppPool -Name $AppPoolName
    }
    else {
        Start-WebAppPool -Name $AppPoolName
    }
    Write-Host "IIS uygulama havuzu yeniden baslatildi: $AppPoolName"
    if ((Get-WebsiteState -Name $SiteName).Value -ne 'Started') {
        Start-Website -Name $SiteName
    }
}

# Run from an elevated Windows PowerShell session on the IIS computer.
# The generated JWT secret is stored only in this machine's IIS application pool configuration.
[CmdletBinding()]
param(
    [string]$AppPoolName = 'MKFiloServis',
    [string]$SiteName = 'MKFiloServis',
    [ValidateRange(1, 65535)] [int]$Port = 5050,
    [string]$AppPath = 'C:\MKFiloServis\app',
    [switch]$RestartAppPool
)

$ErrorActionPreference = 'Stop'

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Bu betik yonetici yetkili PowerShell oturumunda calistirilmalidir.'
}

if (-not (Test-Path -LiteralPath $AppPath -PathType Container)) {
    throw "Uygulama klasoru bulunamadi: $AppPath"
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

    $site = $manager.Sites[$SiteName]
    if ($null -eq $site) {
        throw "IIS sitesi bulunamadi: $SiteName"
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

    $variables = $pool.GetCollection('environmentVariables')
    $jwtEntry = $null
    foreach ($item in $variables) {
        if ([string]$item.GetAttributeValue('name') -eq 'Jwt__Secret') {
            $jwtEntry = $item
            break
        }
    }

    if ($null -eq $jwtEntry) {
        $bytes = New-Object byte[] 48
        $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        try {
            $random.GetBytes($bytes)
        }
        finally {
            $random.Dispose()
        }
        $secret = [Convert]::ToBase64String($bytes)
        $jwtEntry = $variables.CreateElement('add')
        $jwtEntry.SetAttributeValue('name', 'Jwt__Secret')
        $jwtEntry.SetAttributeValue('value', $secret)
        $variables.Add($jwtEntry)
        Write-Host 'IIS uygulama havuzu icin rastgele JWT secret olusturuldu.'
    }
    else {
        $existingSecret = [string]$jwtEntry.GetAttributeValue('value')
        if ([string]::IsNullOrWhiteSpace($existingSecret) -or
            [Text.Encoding]::UTF8.GetByteCount($existingSecret) -lt 32 -or
            $existingSecret.StartsWith('REPLACE_', [StringComparison]::OrdinalIgnoreCase)) {
            throw 'IIS uygulama havuzundaki Jwt__Secret gecersiz. En az 32 baytlik gecerli bir deger saglayin.'
        }
        Write-Host 'IIS uygulama havuzundaki mevcut JWT secret korundu.'
    }

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
    Write-Host "IIS HTTP portu ayarlandi: $Port"
}
finally {
    $manager.Dispose()
}

$logPath = Join-Path $AppPath 'logs'
New-Item -ItemType Directory -Path $logPath -Force | Out-Null
$icacls = Join-Path $env:windir 'System32\icacls.exe'
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

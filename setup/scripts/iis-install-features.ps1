<#
    MKFiloServis - IIS rolu/ozelliklerini otomatik yukler (idempotent)
    Sunucu (Windows Server) ve istemci (Windows 10/11) icin ayri yollar denenir.
    Ek olarak ASP.NET Core 10 Hosting Bundle yoksa indirip sessizce kurar.

    Cagrim:
        powershell.exe -NoProfile -ExecutionPolicy Bypass -File iis-install-features.ps1

    Cikis kodlari:
        0 = OK (IIS hazir, Hosting Bundle hazir)
        1 = Kritik hata (IIS yuklenemedi)
        2 = IIS hazir ama Hosting Bundle indirilemedi
#>
[CmdletBinding()]
param(
    [switch] $SkipHostingBundle,
    [string] $HostingBundleUrl = 'https://aka.ms/dotnet/10.0/dotnet-hosting-win.exe'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

function Write-Info ($m) { Write-Host "[IIS] $m" -ForegroundColor Cyan }
function Write-OK   ($m) { Write-Host "[IIS] $m" -ForegroundColor Green }
function Write-Warn ($m) { Write-Host "[IIS] $m" -ForegroundColor Yellow }
function Write-Err  ($m) { Write-Host "[IIS] $m" -ForegroundColor Red }

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return ([Security.Principal.WindowsPrincipal]::new($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    Write-Err "Bu betik yonetici (admin) yetkisi ile calistirilmalidir."
    exit 1
}

function Test-IIS {
    return [bool] (Get-Service -Name W3SVC -ErrorAction SilentlyContinue)
}

function Install-IIS-Server {
    Write-Info "Windows Server tespit edildi. ServerManager / Install-WindowsFeature kullaniliyor..."
    $features = @(
        'Web-Server',
        'Web-WebServer',
        'Web-Common-Http',
        'Web-Default-Doc',
        'Web-Dir-Browsing',
        'Web-Http-Errors',
        'Web-Static-Content',
        'Web-Http-Logging',
        'Web-Stat-Compression',
        'Web-Filtering',
        'Web-Mgmt-Console',
        'Web-Net-Ext45',
        'Web-AppInit',
        'Web-ISAPI-Ext',
        'Web-ISAPI-Filter'
    )
    Import-Module ServerManager -ErrorAction Stop
    Install-WindowsFeature -Name $features -IncludeManagementTools -ErrorAction Stop | Out-Null
}

function Install-IIS-Client {
    Write-Info "Windows 10/11 (istemci) tespit edildi. DISM / Enable-WindowsOptionalFeature kullaniliyor..."
    $features = @(
        'IIS-WebServerRole',
        'IIS-WebServer',
        'IIS-CommonHttpFeatures',
        'IIS-StaticContent',
        'IIS-DefaultDocument',
        'IIS-DirectoryBrowsing',
        'IIS-HttpErrors',
        'IIS-HttpLogging',
        'IIS-RequestFiltering',
        'IIS-ManagementConsole',
        'IIS-NetFxExtensibility45',
        'IIS-ISAPIExtensions',
        'IIS-ISAPIFilter',
        'IIS-ApplicationInit'
    )
    foreach ($f in $features) {
        try {
            $state = (Get-WindowsOptionalFeature -Online -FeatureName $f -ErrorAction Stop).State
            if ($state -ne 'Enabled') {
                Write-Info "  + $f"
                Enable-WindowsOptionalFeature -Online -FeatureName $f -All -NoRestart -ErrorAction Stop | Out-Null
            }
        } catch {
            Write-Warn "  ! $f atlandi: $($_.Exception.Message)"
        }
    }
}

function Test-HostingBundle {
    $regPaths = @(
        "HKLM:\SOFTWARE\Microsoft\IIS Extensions\IIS AspNetCore Module V2",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\IIS Extensions\IIS AspNetCore Module V2"
    )
    foreach ($p in $regPaths) { if (Test-Path $p) { return $true } }
    $dlls = @(
        "$env:windir\System32\inetsrv\aspnetcorev2.dll",
        "$env:SystemDrive\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
    )
    foreach ($d in $dlls) { if (Test-Path $d) { return $true } }
    return $false
}

function Install-HostingBundle {
    param([string] $Url)
    $tmp = Join-Path $env:TEMP "dotnet-hosting-bundle.exe"
    Write-Info "Hosting Bundle indiriliyor: $Url"
    try {
        Invoke-WebRequest -Uri $Url -OutFile $tmp -UseBasicParsing -ErrorAction Stop
    } catch {
        Write-Warn "Indirme basarisiz: $($_.Exception.Message)"
        return $false
    }
    if (-not (Test-Path $tmp)) { Write-Warn "Yukleyici dosyasi olusmadi."; return $false }

    Write-Info "Hosting Bundle sessizce kuruluyor..."
    $p = Start-Process -FilePath $tmp -ArgumentList '/install','/quiet','/norestart' -Wait -PassThru
    Remove-Item $tmp -ErrorAction SilentlyContinue
    # 0 = OK, 3010 = restart pending (ikisi de kabul)
    if ($p.ExitCode -eq 0 -or $p.ExitCode -eq 3010) {
        Write-OK "Hosting Bundle kuruldu (ExitCode=$($p.ExitCode))."
        return $true
    } else {
        Write-Warn "Hosting Bundle yukleyici ExitCode=$($p.ExitCode)."
        return $false
    }
}

# ---- Akis ----

if (Test-IIS) {
    Write-OK "IIS zaten yuklu (W3SVC bulundu)."
} else {
    try {
        $os = Get-CimInstance Win32_OperatingSystem
        # ProductType: 1=workstation, 2=DC, 3=server
        if ($os.ProductType -eq 1) {
            Install-IIS-Client
        } else {
            Install-IIS-Server
        }
    } catch {
        Write-Err "IIS yukleme basarisiz: $($_.Exception.Message)"
        exit 1
    }

    if (-not (Test-IIS)) {
        Write-Err "IIS yukleme sonrasi W3SVC servisi hala bulunamadi."
        exit 1
    }
    Write-OK "IIS yuklendi."
}

# ---- Hosting Bundle ----
if ($SkipHostingBundle) {
    Write-Warn "Hosting Bundle kontrolu atlandi (-SkipHostingBundle)."
    exit 0
}

if (Test-HostingBundle) {
    Write-OK ".NET ASP.NET Core Module V2 (Hosting Bundle) zaten yuklu."
    exit 0
}

if (Install-HostingBundle -Url $HostingBundleUrl) {
    # iisreset gerekebilir - cagiran tarafa birak
    Write-OK "Hosting Bundle hazir. (Gerekirse iisreset cagirilmali.)"
    exit 0
} else {
    Write-Warn "Hosting Bundle kurulamadi. Lutfen manuel kurun: $HostingBundleUrl"
    exit 2
}

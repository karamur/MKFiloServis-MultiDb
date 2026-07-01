<#
    MKFiloServis — IIS otomatik yapilandirma (v1.0.2)
    - IIS site + AppPool olusturur/gunceller (idempotent)
    - ACL: IIS AppPool\<SiteName> -> Modify (data/uploads/logs/Backups)
    - iisreset (AspNetCoreModule cache yenilemesi icin)
    - Port dolu mu kontrol + 3 deneme ile alternatif port (5191, 5192)
    - Smoke test: http://localhost:<port>
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $InstallPath,
    [Parameter(Mandatory)] [string] $SiteName,
    [Parameter(Mandatory)] [int]    $Port
)

$ErrorActionPreference = 'Stop'

function Ensure-Module {
    if (-not (Get-Module -ListAvailable -Name WebAdministration)) {
        Write-Host "IIS WebAdministration modulu bulunamadi. IIS kurulu mu?" -ForegroundColor Yellow
        exit 1
    }
    Import-Module WebAdministration -ErrorAction Stop
}

function Ensure-AppPool {
    param([string] $Name)
    if (-not (Test-Path "IIS:\AppPools\$Name")) {
        New-WebAppPool -Name $Name | Out-Null
        Write-Host "AppPool olusturuldu: $Name"
    } else {
        Write-Host "AppPool zaten var: $Name"
    }
    Set-ItemProperty "IIS:\AppPools\$Name" -Name managedRuntimeVersion -Value ''
    Set-ItemProperty "IIS:\AppPools\$Name" -Name processModel.identityType -Value 'ApplicationPoolIdentity'
    Set-ItemProperty "IIS:\AppPools\$Name" -Name startMode -Value 'AlwaysRunning'
    Set-ItemProperty "IIS:\AppPools\$Name" -Name autoStart -Value $true
    Set-ItemProperty "IIS:\AppPools\$Name" -Name enable32BitAppOnWin64 -Value $false
}

function Test-PortFree {
    param([int] $P)
    try {
        $c = Get-NetTCPConnection -LocalPort $P -State Listen -ErrorAction SilentlyContinue
        # IIS kendi site'imize aitse (kendi dinliyorsa) "boş" say
        return (-not $c)
    } catch { return $true }
}

function Find-FreePort {
    param([int] $Start, [int] $Tries = 5)
    for ($i = 0; $i -lt $Tries; $i++) {
        $candidate = $Start + $i
        if (Test-PortFree -P $candidate) { return $candidate }
    }
    throw "Bos port bulunamadi ($Start ile $($Start + $Tries - 1) arasinda)."
}

function Ensure-Site {
    param(
        [string] $Name,
        [string] $Path,
        [int]    $Port,
        [string] $AppPool
    )
    if (-not (Test-Path "IIS:\Sites\$Name")) {
        New-Website -Name $Name -PhysicalPath $Path -Port $Port -ApplicationPool $AppPool -Force | Out-Null
        Write-Host "Site olusturuldu: $Name (:$Port)"
    } else {
        Set-ItemProperty "IIS:\Sites\$Name" -Name physicalPath -Value $Path
        Set-ItemProperty "IIS:\Sites\$Name" -Name applicationPool -Value $AppPool
        $bindings = Get-WebBinding -Name $Name
        if (-not ($bindings | Where-Object { $_.bindingInformation -like "*:$($Port):*" })) {
            # Eski port binding'lerini temizle (yalnizca HTTP)
            $bindings | Where-Object { $_.protocol -eq 'http' } | ForEach-Object {
                Remove-WebBinding -Name $Name -BindingInformation $_.bindingInformation -ErrorAction SilentlyContinue
            }
            New-WebBinding -Name $Name -Protocol http -Port $Port | Out-Null
        }
        Write-Host "Site guncellendi: $Name (:$Port)"
    }
}

function Grant-Acl {
    param([string] $Path, [string] $AppPool)
    $ident = "IIS AppPool\$AppPool"
    try {
        $acl = Get-Acl $Path
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $ident, 'Modify', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
        $acl.SetAccessRule($rule)
        Set-Acl $Path $acl
        Write-Host "ACL: $ident icin Modify izni verildi -> $Path"
    } catch {
        Write-Host "ACL atanamadi ($Path): $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Test-Site {
    param([int] $P, [int] $TimeoutSec = 15)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $r = Invoke-WebRequest -Uri "http://localhost:$P" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500) {
                Write-Host "Smoke test OK: http://localhost:$P ($($r.StatusCode))" -ForegroundColor Green
                return $true
            }
        } catch {
            Start-Sleep -Seconds 2
        }
    }
    Write-Host "Smoke test basarisiz: http://localhost:$P" -ForegroundColor Yellow
    return $false
}

try {
    Write-Host "=== MKFiloServis IIS yapilandirma ==="
    Write-Host "Klasor : $InstallPath"
    Write-Host "Site   : $SiteName"
    Write-Host "Port   : $Port (istenen)"

    Ensure-Module

    # Port kullanilabilir mi? Mevcut site'imiza aitse gec, degilse alternatif bul
    $siteVar = Test-Path "IIS:\Sites\$SiteName"
    $gerçekPort = $Port
    if (-not $siteVar) {
        if (-not (Test-PortFree -P $Port)) {
            Write-Host "Port $Port dolu; alternatif araniyor..." -ForegroundColor Yellow
            $gerçekPort = Find-FreePort -Start $Port -Tries 5
            Write-Host "Alternatif port: $gerçekPort" -ForegroundColor Yellow
        }
    }

    Ensure-AppPool -Name $SiteName
    Ensure-Site -Name $SiteName -Path $InstallPath -Port $gerçekPort -AppPool $SiteName

    foreach ($sub in @($InstallPath, "$InstallPath\data", "$InstallPath\uploads", "$InstallPath\logs", "$InstallPath\Backups", "$InstallPath\keys")) {
        if (Test-Path $sub) { Grant-Acl -Path $sub -AppPool $SiteName }
    }

    # Hosting Bundle yeni yuklendiyse module kaydi icin iisreset gerekli
    Write-Host "iisreset /noforce ..."
    & iisreset.exe /noforce | Out-Host

    Start-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
    Start-Website    -Name $SiteName -ErrorAction SilentlyContinue

    # Kullanilan portu diska yaz (postinstall mesaji icin)
    Set-Content -Path (Join-Path $InstallPath 'active-port.txt') -Value $gerçekPort -Encoding ASCII

    Write-Host "Smoke test baslatiliyor..."
    $ok = Test-Site -P $gerçekPort -TimeoutSec 20
    if (-not $ok) {
        Write-Host "IIS site acildi ama HTTP cevap vermedi. Logs:" -ForegroundColor Yellow
        Get-ChildItem "$InstallPath\logs\stdout*.log" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1 |
            ForEach-Object { Get-Content $_.FullName -Tail 30 | Write-Host }
    }

    Write-Host "=== IIS yapilandirma tamam (port: $gerçekPort) ===" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace
    exit 1
}

<#
    MKFiloServis - IIS 500.30 tanilama raporu
    - IIS, AppPool, Site, web.config aspNetCore ayarlari
    - Hosting Bundle / ANCM (AspNetCoreModuleV2)
    - dotnet bilgisi
    - Son stdout log ozeti
    - Event Viewer (Application) son hata kayitlari
#>
[CmdletBinding()]
param(
    [string] $InstallPath = "C:\MKFiloServis\IIS",
    [string] $SiteName = "MKFiloServis",
    [int] $StdoutTail = 200,
    [int] $EventCount = 30,
    [string] $OutputPath
)

$ErrorActionPreference = 'Continue'

function Add-Section {
    param(
        [Parameter(Mandatory)] [string] $Title,
        [Parameter(Mandatory)] [scriptblock] $Body
    )

    Add-Content -Path $script:ReportPath -Value ""
    Add-Content -Path $script:ReportPath -Value ("=" * 80)
    Add-Content -Path $script:ReportPath -Value $Title
    Add-Content -Path $script:ReportPath -Value ("=" * 80)

    try {
        & $Body | Out-String | Add-Content -Path $script:ReportPath
    }
    catch {
        Add-Content -Path $script:ReportPath -Value ("[HATA] " + $_.Exception.Message)
    }
}

try {
    $logsDir = Join-Path $InstallPath 'logs'
    if (-not $OutputPath) {
        if (-not (Test-Path $logsDir)) {
            New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
        }
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $OutputPath = Join-Path $logsDir ("diag-{0}.txt" -f $stamp)
    }

    $script:ReportPath = $OutputPath
    Set-Content -Path $script:ReportPath -Value "MKFiloServis IIS Diagnostic Report" -Encoding UTF8
    Add-Content -Path $script:ReportPath -Value ("Generated: " + (Get-Date).ToString('yyyy-MM-dd HH:mm:ss'))

    Add-Section -Title 'SYSTEM INFO' -Body {
        [PSCustomObject]@{
            MachineName = $env:COMPUTERNAME
            UserName    = $env:USERNAME
            IsAdmin     = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
            OSVersion   = [Environment]::OSVersion.VersionString
            InstallPath = $InstallPath
            SiteName    = $SiteName
        } | Format-List
    }

    Add-Section -Title 'DOTNET INFO' -Body {
        $dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
        if (-not $dotnetCmd) {
            'dotnet komutu bulunamadi.'
        }
        else {
            "dotnet path: $($dotnetCmd.Source)"
            & dotnet --info 2>&1
        }
    }

    Add-Section -Title 'HOSTING BUNDLE / ANCM CHECK' -Body {
        $ancmRegPaths = @(
            'HKLM:\SOFTWARE\Microsoft\IIS Extensions\IIS AspNetCore Module V2',
            'HKLM:\SOFTWARE\WOW6432Node\Microsoft\IIS Extensions\IIS AspNetCore Module V2'
        )

        $foundReg = $ancmRegPaths | Where-Object { Test-Path $_ }
        $dllCandidates = @(
            "$env:windir\System32\inetsrv\aspnetcorev2.dll",
            "$env:SystemDrive\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
        )
        $foundDll = $dllCandidates | Where-Object { Test-Path $_ }

        "Registry found: "
        if ($foundReg) { $foundReg } else { 'Yok' }
        ""
        "ANCM DLL found: "
        if ($foundDll) { $foundDll } else { 'Yok' }
    }

    Add-Section -Title 'IIS SITE / APPPOOL' -Body {
        if (-not (Get-Module -ListAvailable -Name WebAdministration)) {
            'WebAdministration modulu bulunamadi.'
            return
        }

        Import-Module WebAdministration -ErrorAction Stop

        if (Test-Path "IIS:\Sites\$SiteName") {
            Get-Item "IIS:\Sites\$SiteName" | Select-Object Name, Id, State, PhysicalPath, ApplicationPool, Bindings | Format-List
            ""
            "Bindings:"
            Get-WebBinding -Name $SiteName | Select-Object protocol, bindingInformation, sslFlags | Format-Table -AutoSize
        }
        else {
            "Site bulunamadi: $SiteName"
        }

        ""
        if (Test-Path "IIS:\AppPools\$SiteName") {
            Get-Item "IIS:\AppPools\$SiteName" |
                Select-Object Name, State,
                    @{Name='ManagedRuntimeVersion';Expression={ $_.managedRuntimeVersion }},
                    @{Name='IdentityType';Expression={ $_.processModel.identityType }},
                    @{Name='StartMode';Expression={ $_.startMode }} | Format-List
        }
        else {
            "AppPool bulunamadi: $SiteName"
        }
    }

    Add-Section -Title 'WEB.CONFIG ASPNETCORE' -Body {
        $webConfigPath = Join-Path $InstallPath 'web.config'
        if (-not (Test-Path $webConfigPath)) {
            "web.config bulunamadi: $webConfigPath"
            return
        }

        "Path: $webConfigPath"
        ""
        try {
            [xml] $xml = Get-Content $webConfigPath -Raw
            $node = $xml.configuration.'system.webServer'.aspNetCore
            if ($null -eq $node) {
                'aspNetCore node bulunamadi.'
            }
            else {
                [PSCustomObject]@{
                    processPath      = $node.processPath
                    arguments        = $node.arguments
                    hostingModel     = $node.hostingModel
                    stdoutLogEnabled = $node.stdoutLogEnabled
                    stdoutLogFile    = $node.stdoutLogFile
                } | Format-List
            }
        }
        catch {
            "web.config parse hatasi: $($_.Exception.Message)"
        }
    }

    Add-Section -Title 'LATEST STDOUT LOG' -Body {
        if (-not (Test-Path $logsDir)) {
            "logs klasoru bulunamadi: $logsDir"
            return
        }

        $latestStdout = Get-ChildItem -Path $logsDir -Filter 'stdout*.log' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if (-not $latestStdout) {
            'stdout log bulunamadi.'
            return
        }

        "File: $($latestStdout.FullName)"
        "Size: $([Math]::Round($latestStdout.Length / 1KB, 2)) KB"
        "LastWriteTime: $($latestStdout.LastWriteTime)"
        ""
        Get-Content -Path $latestStdout.FullName -Tail $StdoutTail -ErrorAction SilentlyContinue
    }

    Add-Section -Title 'EVENT VIEWER (APPLICATION)' -Body {
        $providers = @(
            'IIS AspNetCore Module V2',
            'IIS AspNetCore Module',
            'IIS-W3SVC-WP',
            '.NET Runtime',
            'Application Error'
        )

        $events = Get-WinEvent -FilterHashtable @{
            LogName = 'Application'
            StartTime = (Get-Date).AddDays(-2)
        } -ErrorAction SilentlyContinue |
            Where-Object {
                ($providers -contains $_.ProviderName) -or
                ($_.Message -match '500\\.30|ANCM|AspNetCore|Failed to start|MKFiloServis|KOAFiloServis')
            } |
            Select-Object -First $EventCount TimeCreated, Id, LevelDisplayName, ProviderName, Message

        if (-not $events) {
            'Filtreye uyan event bulunamadi (son 2 gun).'
            return
        }

        foreach ($e in $events) {
            "Time: $($e.TimeCreated)"
            "Id: $($e.Id) | Level: $($e.LevelDisplayName) | Provider: $($e.ProviderName)"
            "Message:"
            $e.Message
            ("-" * 80)
        }
    }

    Add-Section -Title 'TOP 20 PROCESSES (MEMORY)' -Body {
        Get-Process | Sort-Object WorkingSet64 -Descending | Select-Object -First 20 Name, Id,
            @{Name='WorkingSetMB';Expression={ [Math]::Round($_.WorkingSet64 / 1MB, 2) }},
            @{Name='CPU';Expression={ $_.CPU }} | Format-Table -AutoSize
    }

    Write-Host "Tanilama raporu olusturuldu: $script:ReportPath" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "Rapor olusturulamadi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

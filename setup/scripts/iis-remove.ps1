<#
    MKFiloServis — IIS site ve app pool kaldirma (uninstall adimi)
    Kullanici verilerine (db, uploads, logs, Backups) DOKUNMAZ.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $SiteName
)

$ErrorActionPreference = 'SilentlyContinue'

try {
    Import-Module WebAdministration -ErrorAction Stop

    if (Test-Path "IIS:\Sites\$SiteName") {
        Stop-Website -Name $SiteName
        Remove-Website -Name $SiteName
        Write-Host "Site silindi: $SiteName"
    }

    if (Test-Path "IIS:\AppPools\$SiteName") {
        Stop-WebAppPool -Name $SiteName
        Remove-WebAppPool -Name $SiteName
        Write-Host "AppPool silindi: $SiteName"
    }

    exit 0
} catch {
    Write-Host "Uninstall IIS temizligi atlandi: $($_.Exception.Message)"
    exit 0   # uninstall hata verse bile ilerle
}

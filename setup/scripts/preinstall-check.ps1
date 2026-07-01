<#
    MKFiloServis — Kurulum oncesi gereksinim kontrolu (v1.0.2)
    - IIS yuklu mu?
    - ASP.NET Core 10 Hosting Bundle yuklu mu (AspNetCoreModuleV2)?
    Eksik varsa KURULUMU DURDURUR (exit 1).
#>
[CmdletBinding()]
param(
    [switch] $AllowMissingHostingBundle
)

$ErrorActionPreference = 'SilentlyContinue'
$kritik = @()
$uyari  = @()

# ---- 1) IIS kurulu mu? ----
$iisSvc = Get-Service -Name W3SVC -ErrorAction SilentlyContinue
if (-not $iisSvc) {
    $uyari += "IIS (W3SVC) yuklu degil veya servis bulunamadi."
} else {
    Write-Host "OK: IIS (W3SVC) servisi mevcut ($($iisSvc.Status))."
}

# ---- 2) ASP.NET Core Module V2 (Hosting Bundle) ----
$ancmRegPaths = @(
    "HKLM:\SOFTWARE\Microsoft\IIS Extensions\IIS AspNetCore Module V2",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\IIS Extensions\IIS AspNetCore Module V2"
)
$ancmBulundu = $false
foreach ($p in $ancmRegPaths) { if (Test-Path $p) { $ancmBulundu = $true; break } }

# Alternatif kontrol: aspnetcorev2.dll var mi?
$ancmDll = @(
    "$env:windir\System32\inetsrv\aspnetcorev2.dll",
    "$env:SystemDrive\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($ancmBulundu -or $ancmDll) {
    Write-Host "OK: ASP.NET Core Module V2 yuklu." -ForegroundColor Green
} else {
    $mesaj = '.NET 10 Hosting Bundle bulunamadi. https://dotnet.microsoft.com/download/dotnet/10.0 -> "Hosting Bundle" indirip kurun, sonra kurulumu tekrar baslatin.'
    if ($AllowMissingHostingBundle) {
        $uyari += $mesaj
    } else {
        $kritik += $mesaj
    }
}

# ---- Sonuc ----
if ($kritik.Count -eq 0 -and $uyari.Count -eq 0) {
    Write-Host "Gereksinimler tamam." -ForegroundColor Green
    exit 0
}

if ($uyari.Count -gt 0) {
    Write-Host "-- UYARILAR --" -ForegroundColor Yellow
    $uyari | ForEach-Object { Write-Host " * $_" -ForegroundColor Yellow }
}

if ($kritik.Count -gt 0) {
    Write-Host "-- EKSIK (KRITIK) --" -ForegroundColor Red
    $kritik | ForEach-Object { Write-Host " * $_" -ForegroundColor Red }
    Write-Host "Kurulum durduruluyor." -ForegroundColor Red
    exit 1
}

exit 0

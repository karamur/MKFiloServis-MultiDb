<#
.SYNOPSIS
    Mevcut output klasöründeki setup dosyalarını versiyonlu alt klasörlere taşır.

.DESCRIPTION
    output\MKFiloServisKurulum-1.0.3.exe  →  output\v1.0.3\MKFiloServisKurulum-1.0.3.exe

    Desen: output\<UrunAdi>-<Major>.<Minor>.<Patch>.exe

.PARAMETER Move
    Varsayilan: sadece kopyalar. Bu parametre ile taşır (orijinali siler).

.EXAMPLE
    .\organize-output.ps1           # Simule et (ne yapacagini goster)
    .\organize-output.ps1 -Move     # Gercekten tasi
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [switch] $Move
)

$ErrorActionPreference = 'Stop'
$Root   = Split-Path -Parent $MyInvocation.MyCommand.Definition
$Output = Join-Path $Root 'output'

# Versiyonlu klasör deseni dışındaki .exe ve .md dosyalarını al
$dosyalar = Get-ChildItem -Path $Output -File | Where-Object {
    $_.Name -match '-(\d+\.\d+\.\d+)\.'
}

if (-not $dosyalar) {
    Write-Host "Tasinacak dosya bulunamadi." -ForegroundColor Yellow
    return
}

$gruplar = $dosyalar | Group-Object { $_.Name -replace '.*-(\d+\.\d+\.\d+)\..*','$1' }

foreach ($grup in $gruplar) {
    $versiyon = $grup.Name
    $hedefKlasor = Join-Path $Output "v$versiyon"

    Write-Host ""
    Write-Host "--- v$versiyon ($hedefKlasor) ---" -ForegroundColor Cyan

    New-Item -ItemType Directory -Force -Path $hedefKlasor | Out-Null

    foreach ($dosya in $grup.Group) {
        $hedef = Join-Path $hedefKlasor $dosya.Name
        if ($PSCmdlet.ShouldProcess($dosya.FullName, "-> $hedef")) {
            if ($Move) {
                Move-Item -Path $dosya.FullName -Destination $hedef -Force
                Write-Host "  TASINDU : $($dosya.Name)" -ForegroundColor Green
            } else {
                Copy-Item -Path $dosya.FullName -Destination $hedef -Force
                Write-Host "  KOPYALANDI : $($dosya.Name)" -ForegroundColor Green
            }
        }
    }
}

Write-Host ""
Write-Host "Tamamlandi." -ForegroundColor Cyan
Write-Host "  output klasoru yapisi:" -ForegroundColor DarkGray
Get-ChildItem -Path $Output -Directory | Sort-Object Name | ForEach-Object {
    $alt = Get-ChildItem $_.FullName -File
    Write-Host ("  {0,-12} ({1} dosya)" -f $_.Name, $alt.Count)
}

$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis-MultiDb\MKFiloServis.Web\Data\Migrations"

$fixed = 0
Get-ChildItem -Path $root -Filter "*.cs" | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw

    # namespace ve using'i düzelt
    $newContent = $content -replace "namespace KOAFiloServis\.Web\.Data\.Migrations;", "namespace MKFiloServis.Web.Data.Migrations;"
    $newContent = $newContent -replace "using KOAFiloServis", "using MKFiloServis"

    # ApplicationDbContext using'i ekle (yoksa)
    if ($newContent -NotMatch "using MKFiloServis\.Web\.Data;") {
        $newContent = $newContent -replace "using Microsoft\.EntityFrameworkCore;", "using MKFiloServis.Web.Data;`nusing Microsoft.EntityFrameworkCore;"
    }

    if ($content -ne $newContent) {
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
        $fixed++
    }
}

Write-Host "✓ $fixed Migrations dosyası KOA → MK namespace'ine çevrildi"

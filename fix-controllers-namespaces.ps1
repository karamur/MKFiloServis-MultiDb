$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis-MultiDb\MKFiloServis.Web\Controllers"

$fixed = 0
Get-ChildItem -Path $root -Filter "*.cs" | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw

    # namespace'i düzelt
    $newContent = $content -replace "namespace KOAFiloServis\.Web\.Controllers;", "namespace MKFiloServis.Web.Controllers;"
    $newContent = $newContent -replace "using KOAFiloServis", "using MKFiloServis"

    # Using ekleme
    if ($newContent -NotMatch "using MKFiloServis\.Web\.Services\.Interfaces;") {
        $newContent = $newContent -replace "(using MKFiloServis\.Web\.Services;)", "`$1`nusing MKFiloServis.Web.Services.Interfaces;"
    }

    if ($content -ne $newContent) {
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
        $fixed++
        Write-Host "✓ $($file.Name)"
    }
}

Write-Host "`n✓ $fixed Controller dosyası KOA → MK namespace'ine çevrildi"

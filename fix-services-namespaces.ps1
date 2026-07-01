$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb\MKFiloServis.Web\Services"

$fixed = 0
Get-ChildItem -Path $root -Filter "*.cs" -Recurse | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw
    $newContent = $content

    # namespace'i düzelt
    $newContent = $newContent -replace "namespace KOAFiloServis\.Web\.Services", "namespace MKFiloServis.Web.Services"

    # using'leri düzelt
    $newContent = $newContent -replace "using KOAFiloServis", "using MKFiloServis"

    # Namespace references
    $newContent = $newContent -replace "KOAFiloServis\.", "MKFiloServis."

    if ($content -ne $newContent) {
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
        $fixed++
    }
}

Write-Host "✓ $fixed Services dosyası KOA → MK namespace'ine çevrildi"

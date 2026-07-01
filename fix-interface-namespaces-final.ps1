$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb\MKFiloServis.Web\Services\Interfaces"

$fixed = 0
Get-ChildItem -Path $root -Filter "*.cs" | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw

    # Eğer hala KOA var mı kontrol et
    if ($content -match "namespace KOAFiloServis") {
        $newContent = $content -replace "namespace KOAFiloServis\.Web\.Services\.Interfaces;", "namespace MKFiloServis.Web.Services.Interfaces;"
        $newContent = $newContent -replace "using KOAFiloServis", "using MKFiloServis"

        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
        $fixed++
        Write-Host "✓ $($file.Name)"
    }
}

Write-Host "`n✓ $fixed Interface dosyası KOA → MK namespace'ine çevrildi"

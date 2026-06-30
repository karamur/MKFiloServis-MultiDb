$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis-MultiDb"

# Proje klasörlerini yeniden adlandır
$folders = @(
    "KOAFiloServis.DataSync",
    "KOAFiloServis.Infrastructure",
    "KOAFiloServis.LisansDesktop",
    "KOAFiloServis.Shared",
    "KOAFiloServis.Web"
)

foreach ($folder in $folders) {
    $oldPath = Join-Path $root $folder
    if (Test-Path $oldPath) {
        $newFolder = $folder -replace "KOAFiloServis", "MKFiloServis"
        $newPath = Join-Path $root $newFolder
        Move-Item -Path $oldPath -Destination $newPath -Force
        Write-Host "✓ $folder → $newFolder"
    }
}

Write-Host "`n✓ Proje klasörleri yeniden adlandırıldı"

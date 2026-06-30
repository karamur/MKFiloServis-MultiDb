$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis-MultiDb"

# .csproj dosyalarını yeniden adlandır
$csprojFiles = @(
    "MKFiloServis.DataSync\KOAFiloServis.DataSync.csproj",
    "MKFiloServis.Infrastructure\KOAFiloServis.Infrastructure.csproj",
    "MKFiloServis.LisansDesktop\KOAFiloServis.LisansDesktop.csproj",
    "MKFiloServis.Shared\KOAFiloServis.Shared.csproj",
    "MKFiloServis.Web\KOAFiloServis.Web.csproj"
)

foreach ($csproj in $csprojFiles) {
    $oldPath = Join-Path $root $csproj
    if (Test-Path $oldPath) {
        $dir = Split-Path $oldPath
        $filename = Split-Path $oldPath -Leaf
        $newFilename = $filename -replace "KOAFiloServis", "MKFiloServis"
        $newPath = Join-Path $dir $newFilename
        Move-Item -Path $oldPath -Destination $newPath -Force
        Write-Host "✓ $filename → $newFilename"
    }
}

# .csproj dosyalarının içeriğini de güncelle (assembly name vb)
Get-ChildItem -Path $root -Recurse -Filter "*.csproj" | ForEach-Object {
    $content = Get-Content -Path $_.FullName -Raw
    if ($content -match "KOAFiloServis") {
        $newContent = $content -replace "KOAFiloServis", "MKFiloServis"
        Set-Content -Path $_.FullName -Value $newContent -Encoding UTF8 -Force
        Write-Host "✓ Updated content in: $($_.Name)"
    }
}

Write-Host "`n✓ .csproj dosyaları güncellendi"

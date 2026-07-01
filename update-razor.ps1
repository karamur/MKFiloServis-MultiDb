$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb"

# Razor dosyaları da güncelle
Get-ChildItem -Path $root -Recurse -Filter "*.razor" | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { return }

    if ($content -like "*KOAFiloServis*") {
        $newContent = $content -replace "KOAFiloServis", "MKFiloServis"
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
        Write-Host "✓ $($file.Name)"
    }
}

# HTML ve Blazor layout dosyaları
Get-ChildItem -Path $root -Recurse -Include "*.html", "*.htm", "*.cshtml" | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { return }

    if ($content -like "*KOAFiloServis*") {
        $newContent = $content -replace "KOAFiloServis", "MKFiloServis"
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
        Write-Host "✓ $($file.Name)"
    }
}

Write-Host "`n✓ Razor ve HTML dosyaları güncellendi"

$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis-MultiDb\MKFiloServis.Web\Models"

Get-ChildItem -Path $root -Filter "*.cs" | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw

    # Namespace'i düzelt
    $newContent = $content -replace "namespace KOAFiloServis\.Web\.Models;", "namespace MKFiloServis.Web.Models;"
    $newContent = $newContent -replace "using KOAFiloServis", "using MKFiloServis"

    Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
    Write-Host "✓ $($file.Name)"
}

Write-Host "`n✓ Models namespace'leri güncellendi"

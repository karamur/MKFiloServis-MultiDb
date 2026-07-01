$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb\MKFiloServis.Web\Services\AI"

Get-ChildItem -Path $root -Filter "*.cs" | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw

    # Namespace'i düzelt
    $newContent = $content -replace "namespace KOAFiloServis\.Web\.Services\.AI;", "namespace MKFiloServis.Web.Services.AI;"
    $newContent = $newContent -replace "using KOAFiloServis", "using MKFiloServis"

    Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
    Write-Host "✓ $($file.Name)"
}

Write-Host "`n✓ AI namespace'leri güncellendi"

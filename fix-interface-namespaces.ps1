$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb\MKFiloServis.Web\Services\Interfaces"

Get-ChildItem -Path $root -Filter "*.cs" | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw

    # Namespace'i düzelt
    $newContent = $content -replace "namespace MKFiloServis\.Web\.Services;", "namespace MKFiloServis.Web.Services.Interfaces;"

    Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
    Write-Host "✓ $($file.Name)"
}

Write-Host "`n✓ Interface namespace'leri güncellendi"

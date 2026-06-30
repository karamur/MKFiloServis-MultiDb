$files = @(
    "MKFiloServis.Web/Components/Pages/Admin/Denetim.razor.cs",
    "MKFiloServis.Web/Components/Pages/Admin/ErrorLogs.razor.cs",
    "MKFiloServis.Web/Components/Pages/Admin/LicensePage.razor.cs",
    "MKFiloServis.Web/Components/Pages/LicenseBlock.razor.cs",
    "MKFiloServis.Web/Components/Pages/Admin/Recovery.razor.cs",
    "MKFiloServis.Web/Components/Pages/Personel/PuantajExcelGrid.razor.cs"
)

foreach ($file in $files) {
    $path = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis-MultiDb\$file"
    if (Test-Path $path) {
        $content = Get-Content -Path $path -Raw
        $newContent = $content -replace "namespace KOAFiloServis\.Web\.Components", "namespace MKFiloServis.Web.Components"
        Set-Content -Path $path -Value $newContent -Encoding UTF8 -Force
        Write-Host "✓ $file"
    }
}

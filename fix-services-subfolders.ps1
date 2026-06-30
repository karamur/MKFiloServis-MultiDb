$servicesRoot = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis-MultiDb\MKFiloServis.Web\Services"
$subfolders = @("Calculation", "Common", "Security")

$totalFixed = 0

foreach ($folder in $subfolders) {
    $folderPath = Join-Path $servicesRoot $folder

    if (Test-Path $folderPath) {
        Write-Host "`n📁 $folder klasörünü işleniyor..."
        $fixed = 0

        Get-ChildItem -Path $folderPath -Filter "*.cs" -Recurse | ForEach-Object {
            $file = $_
            $content = Get-Content -Path $file.FullName -Raw

            # KOA → MK değişimi
            if ($content -match "namespace KOAFiloServis") {
                $newContent = $content -replace "namespace KOAFiloServis\.Web\.Services\.([^;]+);", "namespace MKFiloServis.Web.Services.`$1;"
                $newContent = $newContent -replace "using KOAFiloServis", "using MKFiloServis"

                Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
                $fixed++
            }
        }

        if ($fixed -gt 0) {
            Write-Host "✓ $fixed dosya ($folder)"
            $totalFixed += $fixed
        }
    }
}

Write-Host "`n✓ TOPLAM: $totalFixed dosya Services alt-klasörlerinde güncellendi"

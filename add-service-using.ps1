$servicesDir = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb\MKFiloServis.Web\Services"

Get-ChildItem -Path $servicesDir -Filter "*Service.cs" -Depth 0 | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw

    # Eğer interface implementation ediyorsa, using ekle
    if ($content -match ":\s*I\w+Service") {
        if ($content -notmatch "using MKFiloServis\.Web\.Services\.Interfaces;") {
            # using'i ekle
            $lines = $content -split "`n"
            $lastUsingIdx = -1

            for ($i = 0; $i -lt $lines.Length; $i++) {
                if ($lines[$i] -match "^using ") {
                    $lastUsingIdx = $i
                }
                elseif ($lines[$i] -notmatch "^using " -and $lastUsingIdx -ge 0) {
                    break
                }
            }

            if ($lastUsingIdx -ge 0) {
                $lines[$lastUsingIdx] += "`nusing MKFiloServis.Web.Services.Interfaces;"
                $newContent = $lines -join "`n"
                Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
                Write-Host "✓ $($file.Name) - using eklendi"
            }
        }
    }
}

Write-Host "`n✓ Service classes Interfaces using'i eklendi"

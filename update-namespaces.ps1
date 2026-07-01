
$root = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb"
$replacements = @(
    "namespace KOAFiloServis.Web.Services;",
    "namespace KOAFiloServis.Web.Jobs;",
    "namespace KOAFiloServis.Web.Data;",
    "namespace KOAFiloServis.Web.Hubs;",
    "namespace KOAFiloServis.Web.Helpers;",
    "namespace KOAFiloServis.Shared.Entities;",
    "using KOAFiloServis.Web.Services;",
    "using KOAFiloServis.Web.Services.Interfaces;",
    "using KOAFiloServis.Web.Data;",
    "using KOAFiloServis.Web.Hubs;",
    "using KOAFiloServis.Web.Helpers;",
    "using KOAFiloServis.Shared.Entities;",
    "using KOAFiloServis.Infrastructure;",
    "using KOAFiloServis"
)

$count = 0
Get-ChildItem -Path $root -Recurse -Filter "*.cs" -Exclude "*.user*" | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { return }

    foreach ($old in $replacements) {
        if ($content -like "*$old*") {
            $new = $old -replace "KOAFiloServis", "MKFiloServis"
            $content = $content -replace [regex]::Escape($old), $new
            $count++
        }
    }

    Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -Force
}

Write-Host "✓ $count dosyada namespace güncellemeleri tamamlandı"

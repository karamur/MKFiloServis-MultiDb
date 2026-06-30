# MKFiloServis Proje Yeniden Adlandırma Script
# MKFiloServis → MKFiloServis, C:\MKFiloServis → C:\MKFiloServis

$workspaceRoot  = 'C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\MKFiloServis-MultiDb'
$replacements = @(
    # Proje adları
    @{ Old = 'MKFiloServis'; New = 'MKFiloServis' },
    # Path'ler
    @{ Old = 'C:\MKFiloServis_yedekleme'; New = 'C:\MKFiloServis_yedekleme' },
    @{ Old = 'C:\MKFiloServis'; New = 'C:\MKFiloServis' }
)

$exclusions = @('bin', 'obj', '.git', 'node_modules', '.vs', '.playwright')

function Scan-AndReplace {
    param(
        [string]$Path,
        [array]$Replacements
    )

    Get-ChildItem -Path $Path -Recurse -File | Where-Object {
        $excluded = $false
        foreach ($ex in $exclusions) {
            if ($_.FullName -match [regex]::Escape($ex)) {
                $excluded = $true
                break
            }
        }
        -not $excluded
    } | ForEach-Object {
        $file = $_
        $originalContent = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
        if ($null -eq $originalContent) { return }

        $modifiedContent = $originalContent
        $changed = $false

        foreach ($replacement in $Replacements) {
            if ($modifiedContent -like "*$($replacement.Old)*") {
                $modifiedContent = $modifiedContent -replace [regex]::Escape($replacement.Old), $replacement.New
                $changed = $true
                Write-Host "  ✓ $($file.FullName)" -ForegroundColor Green
            }
        }

        if ($changed) {
            Set-Content -Path $file.FullName -Value $modifiedContent -Encoding UTF8 -Force
        }
    }
}

Write-Host "🔄 Tüm dosyalarda MKFiloServis → MKFiloServis değişiklikleri başlatılıyor..." -ForegroundColor Cyan
Scan-AndReplace -Path $workspaceRoot -Replacements $replacements
Write-Host "✓ Search/replace tamamlandı" -ForegroundColor Green


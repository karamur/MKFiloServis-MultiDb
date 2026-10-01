$ErrorActionPreference = 'Continue'

$ollamaPaths = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Ollama\ollama.exe'),
    (Join-Path $env:ProgramFiles 'Ollama\ollama.exe')
)
foreach ($candidate in $ollamaPaths) {
    if (Test-Path -LiteralPath $candidate) {
        Write-Host "Ollama calistiricisi: $candidate"
        Write-Host "SHA256: $((Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash)"
    }
}

$roots = @(
    (Join-Path $env:USERPROFILE 'Downloads'),
    (Join-Path $env:USERPROFILE 'Documents'),
    (Join-Path $env:USERPROFILE '.ollama\models'),
    (Join-Path $env:ProgramData 'MKFiloServis\LocalAI')
)
foreach ($root in $roots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    Get-ChildItem -LiteralPath $root -Filter '*.gguf' -File -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object {
            Write-Host "Model: $($_.FullName) ($([math]::Round($_.Length / 1GB, 2)) GB)"
            Write-Host "SHA256: $((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
        }
}

$manifestRoot = Join-Path $env:USERPROFILE '.ollama\models\manifests'
if (Test-Path -LiteralPath $manifestRoot) {
    Get-ChildItem -LiteralPath $manifestRoot -File -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object { Write-Host "Ollama modeli: $($_.FullName)" }
}

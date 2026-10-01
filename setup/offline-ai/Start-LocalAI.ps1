param([Parameter(Mandatory = $true)][string]$OllamaExe)

$ErrorActionPreference = 'Stop'
$env:OLLAMA_HOST = '127.0.0.1:11434'
$env:OLLAMA_NO_CLOUD = '1'
$env:OLLAMA_MODELS = Join-Path $env:ProgramData 'MKFiloServis\LocalAI\models'
& $OllamaExe serve

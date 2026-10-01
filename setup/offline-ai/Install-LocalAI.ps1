param(
    [Parameter(Mandatory = $true)][string]$OllamaExe,
    [Parameter(Mandatory = $true)][string]$ModelGguf,
    [Parameter(Mandatory = $true)][string]$OllamaSha256,
    [Parameter(Mandatory = $true)][string]$ModelSha256,
    [string]$ModelName = 'mkfiloservis-local'
)

$ErrorActionPreference = 'Stop'
$ollamaPath = (Resolve-Path -LiteralPath $OllamaExe).Path
$modelPath = (Resolve-Path -LiteralPath $ModelGguf).Path
if ([IO.Path]::GetFileName($ollamaPath) -like '*Setup*.exe') {
    throw 'OllamaExe olarak kurulum dosyasi degil, kurulmus ollama.exe yolu verilmelidir.'
}

 $principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Yerel AI icin dis ag engeli kurmak uzere PowerShell yonetici olarak acilmalidir.'
}
[Environment]::SetEnvironmentVariable('Ollama__Enabled', 'false', 'Machine')
if (Get-NetFirewallProfile | Where-Object { -not $_.Enabled }) {
    throw 'Windows Guvenlik Duvari tum profillerde acik olmadan yerel AI kurulamaz.'
}

foreach ($item in @(@($ollamaPath, $OllamaSha256), @($modelPath, $ModelSha256))) {
    if ((Get-FileHash -LiteralPath $item[0] -Algorithm SHA256).Hash -ne $item[1].ToUpperInvariant()) {
        throw "SHA256 uyusmazligi: $($item[0])"
    }
}
$signature = Get-AuthenticodeSignature -LiteralPath $ollamaPath
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Ollama Inc') {
    throw 'Ollama calistiricisinin resmi dijital imzasi dogrulanamadi.'
}

# Bilgisayar disina cikis isletim sistemi seviyesinde engellenir. Mevcut kural varsa tekrar olusturulmaz.
$ruleName = 'MKFiloServis Yerel AI - Ollama dis ag engeli'
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if ($existing) {
    $applicationFilter = $existing | Get-NetFirewallApplicationFilter
    if (@($existing).Count -ne 1 -or $existing.Action -ne 'Block' -or
        $existing.Direction -ne 'Outbound' -or $existing.Enabled -ne 'True' -or
        $applicationFilter.Program -ne $ollamaPath) {
        throw 'Mevcut AI guvenlik duvari kurali beklenen uygulamayi engellemiyor. Kurulum durduruldu.'
    }
} else {
    New-NetFirewallRule -DisplayName $ruleName -Direction Outbound -Action Block -Program $ollamaPath -Profile Any | Out-Null
}

$env:OLLAMA_HOST = '127.0.0.1:11434'
$env:OLLAMA_NO_CLOUD = '1'
$env:OLLAMA_MODELS = Join-Path $env:ProgramData 'MKFiloServis\LocalAI\models'
New-Item -ItemType Directory -Path $env:OLLAMA_MODELS -Force | Out-Null

$modelFile = Join-Path $env:TEMP 'mkfiloservis-local.Modelfile'
try {
    Set-Content -LiteralPath $modelFile -Encoding UTF8 -Value "FROM `"$modelPath`""
    if (Get-NetTCPConnection -LocalPort 11434 -State Listen -ErrorAction SilentlyContinue) {
        throw '11434 portunda baska bir servis calisiyor. Yerel AI kurulumu durduruldu.'
    }
    Start-Process -FilePath $ollamaPath -ArgumentList 'serve' -WindowStyle Hidden | Out-Null
    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        try {
            $request = [System.Net.HttpWebRequest][System.Net.WebRequest]::Create('http://127.0.0.1:11434/api/tags')
            $request.Proxy = $null
            $request.Timeout = 2000
            $response = $request.GetResponse()
            $response.Dispose()
            $ready = $true
            break
        } catch { Start-Sleep -Seconds 1 }
    }
    if (-not $ready) { throw 'Yerel Ollama baslatilamadi.' }
    $listeners = @(Get-NetTCPConnection -LocalPort 11434 -State Listen -ErrorAction Stop)
    if ($listeners | Where-Object { $_.LocalAddress -notin @('127.0.0.1', '::1') }) {
        throw 'Ollama yalnizca loopback adresinde dinlemiyor. Kurulum durduruldu.'
    }
    & $ollamaPath create $ModelName -f $modelFile
    if ($LASTEXITCODE -ne 0) { throw 'Yerel model yuklenemedi.' }
    $installedModels = & $ollamaPath list
    if ($LASTEXITCODE -ne 0 -or ($installedModels -join "`n") -notmatch [regex]::Escape($ModelName)) {
        throw 'Model kurulum sonrasi yerel listede bulunamadi.'
    }
    $installedLauncher = Join-Path $env:ProgramData 'MKFiloServis\LocalAI\Start-LocalAI.ps1'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Start-LocalAI.ps1') -Destination $installedLauncher -Force
    $taskAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument ('-NoProfile -File "' + $installedLauncher + '" -OllamaExe "' + $ollamaPath + '"')
    $taskTrigger = New-ScheduledTaskTrigger -AtStartup
    $taskPrincipal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
    Register-ScheduledTask -TaskName 'MKFiloServis Yerel AI' -Action $taskAction -Trigger $taskTrigger -Principal $taskPrincipal -Force | Out-Null
    [Environment]::SetEnvironmentVariable('Ollama__BaseUrl', 'http://127.0.0.1:11434', 'Machine')
    [Environment]::SetEnvironmentVariable('Ollama__Model', $ModelName, 'Machine')
    [Environment]::SetEnvironmentVariable('Ollama__EmbeddingModel', $ModelName, 'Machine')
    [Environment]::SetEnvironmentVariable('Ollama__Enabled', 'true', 'Machine')
    Write-Host "Yerel AI modeli hazir: $ModelName"
}
finally {
    Remove-Item -LiteralPath $modelFile -ErrorAction SilentlyContinue
}

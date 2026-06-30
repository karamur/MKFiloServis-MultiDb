$baseUrl = 'http://localhost:5000'
$oldKey = 'EE4461E47C5DDF6C5750EE9403735642171A5399FD9292F0C7631B2F181B8415'
$request = @{
    'oldMasterKeyHex' = $oldKey
    'targetDirectory' = $null
} | ConvertTo-Json

Write-Host "Recovery baslatiliyor..." -ForegroundColor Cyan
Write-Host "Endpoint: $baseUrl/api/system/recover-encrypted-files" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/system/recover-encrypted-files" `
        -Method POST `
        -Headers @{ 'Content-Type' = 'application/json' } `
        -Body $request `
        -TimeoutSec 3600

    Write-Host "`nRecovery sonucu:" -ForegroundColor Green
    Write-Host "  Basarili: $($response.successCount)" -ForegroundColor Green
    Write-Host "  Basarisiz: $($response.failedCount)" -ForegroundColor Red
    Write-Host "  Skip: $($response.skippedCount)" -ForegroundColor Yellow

    if ($response.recoveredFiles.Count -gt 0) {
        Write-Host "`nKurtarilan dosyalar (ilk 10):" -ForegroundColor Cyan
        $response.recoveredFiles | Select-Object -First 10 | ForEach-Object { Write-Host "    - $_" }
        if ($response.recoveredFiles.Count -gt 10) {
            Write-Host "    ... ve $($response.recoveredFiles.Count - 10) daha" -ForegroundColor Gray
        }
    }

    if ($response.failedFiles.Count -gt 0) {
        Write-Host "`nBasarisiz dosyalar (ilk 5):" -ForegroundColor Red
        $response.failedFiles | Select-Object -First 5 | ForEach-Object { Write-Host "    - $_" }
    }
}
catch {
    Write-Host "Hata: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

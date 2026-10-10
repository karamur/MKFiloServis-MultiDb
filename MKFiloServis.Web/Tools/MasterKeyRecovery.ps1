#!/usr/bin/env pwsh
<#
.SYNOPSIS
    DPAPI Master Key Recovery Diagnostic Tool
.DESCRIPTION
    Master key dosyası çöz ve veri kurtarma durumunu analiz et.
#>

param(
    [string]$MasterKeyPath = "C:\MKFiloServis_yedekleme\keys\master.key",
    [string]$EncryptedFilesDir = "C:\MKFiloServis_yedekleme\Arsiv\Sifreli\Araclar"
)

Write-Host "🔐 DPAPI Master Key Recovery Diagnostic" -ForegroundColor Cyan
Write-Host ("=" * 60)

# 1. Master key var mı?
if (-not (Test-Path $MasterKeyPath)) {
    Write-Host "❌ Master key dosyası bulunamadı: $MasterKeyPath" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Master key dosyası bulundu: $MasterKeyPath" -ForegroundColor Green

# 2. Dosya boyutu
$fileSize = (Get-Item $MasterKeyPath).Length
Write-Host "  Dosya boyutu: $fileSize bytes"

# 3. Dosya tarihi
$fileTime = (Get-Item $MasterKeyPath).LastWriteTime
Write-Host "  Son değiştirilme: $fileTime"

# 4. Mevcut kullanıcı
$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$currentSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
Write-Host "  Mevcut ortam kullanıcısı: $currentUser"
Write-Host "  SID: $currentSid"

# 5. Ana uyarılar
Write-Host ""
Write-Host "🔍 Sorun Analizi:" -ForegroundColor Yellow

$warnings = @{
    "Farklı Hesap Kullanımı" = "-"
    "Farklı Makine" = "-"
    "Registry Yok" = "-"
}

# LocalMachine Registry kontrol (opsiyonel, DPAPI-related)
try {
    $dpapival = Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa' -ErrorAction Stop
    Write-Host "  ✓ LocalMachine DPAPI Registry erişimi OK"
} catch {
    Write-Host "  ⚠ LocalMachine DPAPI Registry erişim hatası (Admin gerekli)" -ForegroundColor Yellow
}

# 6. Şifreli dosyalar
if (Test-Path $EncryptedFilesDir) {
    $encFiles = Get-ChildItem -Path $EncryptedFilesDir -Filter "*.enc" -Recurse -ErrorAction SilentlyContinue
    Write-Host ""
    Write-Host "📁 Şifreli Dosya Envanteli:" -ForegroundColor Cyan
    Write-Host "  Toplam: $($encFiles.Count) dosya"

    if ($encFiles.Count -gt 0) {
        Write-Host "  En eski: $($encFiles | Sort-Object LastWriteTime | Select-Object -First 1 | ForEach-Object { $_.LastWriteTime })"
        Write-Host "  En yeni: $($encFiles | Sort-Object LastWriteTime | Select-Object -Last 1 | ForEach-Object { $_.LastWriteTime })"
    }
} else {
    Write-Host ""
    Write-Host "⚠ Şifreli dosya dizini bulunamadı: $EncryptedFilesDir" -ForegroundColor Yellow
}

# 7. Güvenli sonraki adımlar — bu betik tanılama amaçlı salt okunurdur.
Write-Host ""
Write-Host "Sonraki adımlar (bu betik hiçbir anahtarı veya dosyayı değiştirmez):" -ForegroundColor Green
Write-Host "  - Bu çıktıyı ve dosya sayısını kaydedin; anahtar değerini hiçbir rapora eklemeyin."
Write-Host "  - Mevcut master.key'i silmeyin/değiştirmeyin; şifreli evrakları taşımayın veya yeniden şifrelemeyin."
Write-Host "  - Üretim kurtarması için güncel DOSYA_RECOVERY_KILAVUZU.md ve doğrulanmış recovery archive akışını kullanın."
Write-Host "  - DataProtection key ring başka makine/profilde çözülmüyorsa kaynak sertifika/DPAPI hesabı ve yetkili anahtar yedeğiyle kurtarın."
Write-Host "  - Legacy KOA1/master-key dosyaları söz konusuysa önce izole kopya ve yetkili key sahibiyle ayrı kurtarma planı hazırlayın."
Write-Host ""

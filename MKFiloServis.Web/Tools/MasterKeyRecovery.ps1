#!/usr/bin/env pwsh
<#
.SYNOPSIS
    DPAPI Master Key Recovery Diagnostic Tool
.DESCRIPTION
    Master key dosyası çöz ve veri kurtarma durumunu analiz et.
#>

param(
    [string]$MasterKeyPath = "C:\KOAFiloServis_yedekleme\keys\master.key",
    [string]$EncryptedFilesDir = "C:\KOAFiloServis_yedekleme\Arsiv\Sifreli\Araclar"
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

# 7. Çözüm önerileri
Write-Host ""
Write-Host "💡 Çözüm Önerileri:" -ForegroundColor Green
Write-Host ""
Write-Host "OPSIYON 1: Yedekten master.key Restore Et"
Write-Host "  - Eski makinedeki/yedeklemedeki master.key bul"
Write-Host "  - Mevcut dosyayı sil: Remove-Item '$MasterKeyPath' -Force"
Write-Host "  - Eski dosyayı kopyala: Copy-Item <eski-path>/master.key '$MasterKeyPath'"
Write-Host "  - Uygulamayı yeniden başlat"
Write-Host ""
Write-Host "OPSIYON 2: Eski Dosyaları Ignore Et"
Write-Host "  - Mevcut master.key'i koru (yeni dosyalar için olacak)"
Write-Host "  - Eski şifreli dosyaları: Move-Item '$EncryptedFilesDir' '$EncryptedFilesDir.bak'"
Write-Host "  - Yeni dosyalar normal şekilde şifrele"
Write-Host ""
Write-Host "OPSIYON 3: Environment Variable ile Key Sağla"
Write-Host "  - Production: \$env:KOA_MASTER_KEY_HEX or \$env:KOA_MASTER_KEY_BASE64"
Write-Host "  - Code: check DpapiMasterKeyProvider alternatifleri"
Write-Host ""

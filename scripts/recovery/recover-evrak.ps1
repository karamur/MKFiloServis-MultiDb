# =============================================================================
# MKFiloServis - Evrak Kurtarma ve Re-Encrypt Aracı (MEVCUT PC)
# =============================================================================
# Bu script mevcut PC'de (canlı sistem) çalıştırılır.
# Diğer PC'den alınan raw key ile eski evrakları decrypt eder,
# mevcut master.key ile yeniden şifreler.
#
# Kullanım:
#   PowerShell -ExecutionPolicy Bypass -File recover-evrak.ps1 -OldKeyHex "AABBCC..."
#   veya
#   PowerShell -ExecutionPolicy Bypass -File recover-evrak.ps1 -OldKeyFile "raw-key.txt"
#
# Parametreler:
#   -OldKeyHex       : Diğer PC'den alınan raw key (HEX string, 64 karakter)
#   -OldKeyFile      : raw-key.txt dosyasının yolu
#   -UploadsRoot     : Uploads kök dizini (varsayılan: C:\MKFiloServis_yedekleme\uploads)
#   -KeysDir         : Keys dizini (varsayılan: C:\MKFiloServis_yedekleme\keys)
#   -WhatIf          : Sadece simülasyon yap, dosyaları değiştirme
#   -DryRun          : Sadece decrypt testi yap, dosya başına ilk 1KB'yi göster
# =============================================================================

param(
    [string]$OldKeyHex,
    [string]$OldKeyFile,
    [string]$UploadsRoot = "C:\MKFiloServis_yedekleme\uploads",
    [string]$KeysDir = "C:\MKFiloServis_yedekleme\keys",
    [switch]$WhatIf,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Security

Write-Host "=== MKFiloServis Evrak Kurtarma Aracı (Mevcut PC) ===" -ForegroundColor Cyan
Write-Host ""

# ---------------------------------------------------------------------------
# 1. Eski raw key'i al
# ---------------------------------------------------------------------------
$oldRawKey = $null

if ($OldKeyHex) {
    if ($OldKeyHex.Length -ne 64) {
        Write-Host "HATA: OldKeyHex 64 karakter olmalıdır (32 byte = 64 hex). Geçerli: $($OldKeyHex.Length)" -ForegroundColor Red
        exit 1
    }
    $oldRawKey = New-Object byte[] 32
    for ($i = 0; $i -lt 32; $i++) {
        $oldRawKey[$i] = [Convert]::ToByte($OldKeyHex.Substring($i * 2, 2), 16)
    }
} elseif ($OldKeyFile) {
    if (-not (Test-Path $OldKeyFile)) {
        Write-Host "HATA: OldKeyFile bulunamadı: $OldKeyFile" -ForegroundColor Red
        exit 1
    }
    $hex = (Get-Content $OldKeyFile -Raw).Trim()
    if ($hex.Length -ne 64) {
        Write-Host "HATA: raw-key.txt 64 karakter olmalıdır. Geçerli: $($hex.Length)" -ForegroundColor Red
        exit 1
    }
    $oldRawKey = New-Object byte[] 32
    for ($i = 0; $i -lt 32; $i++) {
        $oldRawKey[$i] = [Convert]::ToByte($hex.Substring($i * 2, 2), 16)
    }
} else {
    Write-Host "HATA: -OldKeyHex veya -OldKeyFile parametresi zorunludur." -ForegroundColor Red
    Write-Host ""
    Write-Host "Örnek kullanım:"
    Write-Host "  .\recover-evrak.ps1 -OldKeyFile .\raw-key.txt"
    Write-Host "  .\recover-evrak.ps1 -OldKeyHex AABBCCDD..."
    Write-Host "  .\recover-evrak.ps1 -OldKeyFile .\raw-key.txt -DryRun"
    exit 1
}

Write-Host "Eski master key yüklendi." -ForegroundColor Green
Write-Host "  HEX: $(([System.BitConverter]::ToString($oldRawKey) -replace '-', ''))"
Write-Host ""

# ---------------------------------------------------------------------------
# 2. Mevcut master key'i DPAPI ile çöz
# ---------------------------------------------------------------------------
$masterKeyPath = Join-Path $KeysDir "master.key"

if (-not (Test-Path $masterKeyPath)) {
    Write-Host "HATA: Mevcut master.key bulunamadı: $masterKeyPath" -ForegroundColor Red
    exit 1
}

$protectedBytes = [System.IO.File]::ReadAllBytes($masterKeyPath)
$entropy = [System.Text.Encoding]::UTF8.GetBytes("KOAFiloServis.MasterKey.v1")

$newRawKey = $null
try {
    $newRawKey = [System.Security.Cryptography.ProtectedData]::Unprotect(
        $protectedBytes, $entropy, [System.Security.Cryptography.DataProtectionScope]::LocalMachine)
} catch {
    try {
        $newRawKey = [System.Security.Cryptography.ProtectedData]::Unprotect(
            $protectedBytes, $entropy, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
    } catch {
        Write-Host "HATA: Mevcut master.key DPAPI ile çözülemedi!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Mevcut master key yüklendi." -ForegroundColor Green
Write-Host "  HEX: $(([System.BitConverter]::ToString($newRawKey) -replace '-', ''))"
Write-Host ""

if ($WhatIf) {
    Write-Host "=== WHATIF MODU === (dosyalar değiştirilmeyecek)" -ForegroundColor Yellow
    Write-Host ""
}

if ($DryRun) {
    Write-Host "=== DRY RUN MODU === (ilk 1KB gösterilecek)" -ForegroundColor Yellow
    Write-Host ""
}

# ---------------------------------------------------------------------------
# 3. Evrakları tara ve kurtar
# ---------------------------------------------------------------------------
if (-not (Test-Path $UploadsRoot)) {
    Write-Host "HATA: Uploads root bulunamadı: $UploadsRoot" -ForegroundColor Red
    exit 1
}

$encFiles = Get-ChildItem -Path $UploadsRoot -Recurse -File -Filter "*.enc" -ErrorAction SilentlyContinue
$total = $encFiles.Count
$success = 0
$fail = 0
$skipped = 0
$backupDir = Join-Path (Split-Path -Parent $UploadsRoot) "RecoveredBackup"

Write-Host "=== Evraklar taranıyor ===" -ForegroundColor Cyan
Write-Host "Uploads root: $UploadsRoot"
Write-Host "Toplam .enc dosyası: $total"
Write-Host ""

$results = @()

foreach ($file in $encFiles) {
    $relativePath = $file.FullName.Substring($UploadsRoot.Length).TrimStart('\', '/')

    try {
        $encBytes = [System.IO.File]::ReadAllBytes($file.FullName)

        # KOA1 header kontrolü
        $isKoa1 = $encBytes.Length -ge 33 -and
                  $encBytes[0] -eq 75 -and $encBytes[1] -eq 79 -and
                  $encBytes[2] -eq 65 -and $encBytes[3] -eq 49

        if (-not $isKoa1) {
            Write-Host "ATLANDI (KOA1 değil): $relativePath" -ForegroundColor Yellow
            $skipped++
            $results += [PSCustomObject]@{ File = $relativePath; Status = "SKIPPED"; Reason = "KOA1 değil"; Size = $file.Length }
            continue
        }

        # KOA1 formatını çöz: KOA1 | VER(1) | NONCE(12) | TAG(16) | CIPHER
        $ver = $encBytes[4]
        $nonce = $encBytes[5..16]
        $tag = $encBytes[17..32]
        $cipher = $encBytes[33..($encBytes.Length-1)]

        # ESKİ key ile decrypt
        $plain = New-Object byte[] $cipher.Length
        $aesOld = New-Object System.Security.Cryptography.AesGcm($oldRawKey, 16)
        $aesOld.Decrypt($nonce, $cipher, $tag, $plain)
        $aesOld.Dispose()

        # Plain header kontrolü
        $plainHeader = [System.Text.Encoding]::ASCII.GetString($plain[0..([Math]::Min(19, $plain.Length-1))])
        $isPdf = $plain.Length -ge 4 -and $plain[0] -eq 0x25 -and $plain[1] -eq 0x50 -and $plain[2] -eq 0x44 -and $plain[3] -eq 0x46

        if ($DryRun) {
            Write-Host "--- $relativePath ---"
            Write-Host "  Size: $($file.Length) -> Plain: $($plain.Length)"
            Write-Host "  Header: $plainHeader"
            Write-Host "  PDF: $isPdf"
            Write-Host "  First 80 chars: $([System.Text.Encoding]::ASCII.GetString($plain[0..([Math]::Min(79, $plain.Length-1))]))"
            Write-Host ""
            $success++
            continue
        }

        if (-not $WhatIf) {
            # Backups klasörüne orijinali yedekle
            $backupPath = Join-Path $backupDir $relativePath
            $backupParent = Split-Path -Parent $backupPath
            New-Item -ItemType Directory -Force -Path $backupParent | Out-Null
            [System.IO.File]::Copy($file.FullName, $backupPath, $true)

            # YENİ key ile re-encrypt
            $newNonce = New-Object byte[] 12
            [System.Security.Cryptography.RandomNumberGenerator]::Fill($newNonce)
            $newTag = New-Object byte[] 16
            $newCipher = New-Object byte[] $plain.Length

            $aesNew = New-Object System.Security.Cryptography.AesGcm($newRawKey, 16)
            $aesNew.Encrypt($newNonce, $plain, $newCipher, $newTag)
            $aesNew.Dispose()

            # Yeni KOA1 dosyasını oluştur
            $newEnc = New-Object byte[] (5 + 12 + 16 + $newCipher.Length)
            $newEnc[0] = 75; $newEnc[1] = 79; $newEnc[2] = 65; $newEnc[3] = 49  # KOA1
            $newEnc[4] = 1  # Version
            [Array]::Copy($newNonce, 0, $newEnc, 5, 12)
            [Array]::Copy($newTag, 0, $newEnc, 17, 16)
            [Array]::Copy($newCipher, 0, $newEnc, 33, $newCipher.Length)
            [System.IO.File]::WriteAllBytes($file.FullName, $newEnc)

            Write-Host "KURTARILDI: $relativePath ($($file.Length) -> $($newEnc.Length) bytes) [PDF: $isPdf]" -ForegroundColor Green
        } else {
            Write-Host "KURTARILABİLİR: $relativePath [PDF: $isPdf, Plain: $($plain.Length) bytes]" -ForegroundColor Green
        }

        $success++
        $results += [PSCustomObject]@{ File = $relativePath; Status = "RECOVERED"; Reason = "OK"; Size = $file.Length; PdfHeader = $isPdf }
    } catch {
        $fail++
        $errMsg = $_.Exception.Message
        Write-Host "HATA: $relativePath -> $errMsg" -ForegroundColor Red
        $results += [PSCustomObject]@{ File = $relativePath; Status = "FAILED"; Reason = $errMsg; Size = $file.Length }
    }
}

# ---------------------------------------------------------------------------
# 4. Özet
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "          KURTARMA TAMAMLANDI" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Toplam .enc dosyası : $total"
Write-Host "Kurtarılan          : $success" -ForegroundColor Green
Write-Host "Başarısız           : $fail" -ForegroundColor Red
Write-Host "Atlanan (legacy)    : $skipped" -ForegroundColor Yellow

if (-not $WhatIf -and -not $DryRun -and $success -gt 0) {
    Write-Host ""
    Write-Host "Orijinal yedekler: $backupDir" -ForegroundColor Yellow
    Write-Host "Kurtarılan dosyalar uploads altında güncellendi." -ForegroundColor Green
}

if ($fail -gt 0) {
    Write-Host ""
    Write-Host "BAŞARISIZ DOSYALAR:" -ForegroundColor Red
    $results | Where-Object { $_.Status -eq "FAILED" } | ForEach-Object {
        Write-Host "  $($_.File): $($_.Reason)" -ForegroundColor Red
    }
}

# Sonuçları CSV'ye kaydet
$resultsFile = Join-Path $env:TEMP "evrak-recovery-results.csv"
$results | Export-Csv -Path $resultsFile -NoTypeInformation -Encoding UTF8
Write-Host ""
Write-Host "Detaylı sonuç: $resultsFile"

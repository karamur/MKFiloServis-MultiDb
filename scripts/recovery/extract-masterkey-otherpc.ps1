# =============================================================================
# KOAFiloServis - Diğer PC'den Master Key Çıkarma Aracı
# =============================================================================
# BU SCRIPT DİĞER PC'DE (evrakların açıldığı PC) ÇALIŞTIRILMALIDIR!
#
# Kullanım:
#   PowerShell -ExecutionPolicy Bypass -File extract-masterkey-otherpc.ps1
#
# Çıktı:
#   - Konsola raw key (hex) yazdırır
#   - raw-key.txt dosyasına kaydeder
#   - İsteğe bağlı: tüm evrakları decrypt edip plain dosya olarak kaydeder
# =============================================================================

param(
    [string]$UploadsRoot = "C:\KOAFiloServis_yedekleme\uploads",
    [string]$KeysDir = "C:\KOAFiloServis_yedekleme\keys",
    [switch]$DecryptAll,
    [string]$OutputDir = "C:\KOAFiloServis_yedekleme\RecoveredPlain"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Security

# ---------------------------------------------------------------------------
# 1. Master key'i bul ve DPAPI ile çöz
# ---------------------------------------------------------------------------
$masterKeyPath = Join-Path $KeysDir "master.key"

if (-not (Test-Path $masterKeyPath)) {
    # Alternatif path'leri dene
    $altPaths = @(
        "C:\KOAFiloServis_yedekleme\keys\master.key",
        "C:\ProgramData\KOAFiloServis\keys\master.key",
        "$env:ProgramData\KOAFiloServis\keys\master.key"
    )
    $found = $false
    foreach ($p in $altPaths) {
        if (Test-Path $p) {
            $masterKeyPath = $p
            $found = $true
            break
        }
    }
    if (-not $found) {
        Write-Host "HATA: master.key bulunamadı!" -ForegroundColor Red
        Write-Host "Aranan path'ler: $($altPaths -join ', ')"
        exit 1
    }
}

Write-Host "=== KOAFiloServis Master Key Çıkarma Aracı ===" -ForegroundColor Cyan
Write-Host "Master key path: $masterKeyPath"
Write-Host ""

$protectedBytes = [System.IO.File]::ReadAllBytes($masterKeyPath)
$entropy = [System.Text.Encoding]::UTF8.GetBytes("KOAFiloServis.MasterKey.v1")

$rawKey = $null
$scope = $null

# LocalMachine dene
try {
    $rawKey = [System.Security.Cryptography.ProtectedData]::Unprotect(
        $protectedBytes, $entropy, [System.Security.Cryptography.DataProtectionScope]::LocalMachine)
    $scope = "LocalMachine"
} catch {
    # CurrentUser dene
    try {
        $rawKey = [System.Security.Cryptography.ProtectedData]::Unprotect(
            $protectedBytes, $entropy, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
        $scope = "CurrentUser"
    } catch {
        Write-Host "HATA: master.key DPAPI ile çözülemedi!" -ForegroundColor Red
        Write-Host "LocalMachine: $_"
        Write-Host ""
        Write-Host "Bu script sadece evrakların AÇILDIĞI PC'de çalıştırılmalıdır." -ForegroundColor Yellow
        exit 1
    }
}

if ($rawKey.Length -ne 32) {
    Write-Host "UYARI: Raw key beklenmeyen uzunlukta: $($rawKey.Length) byte (32 bekleniyordu)" -ForegroundColor Yellow
}

$rawKeyHex = [System.BitConverter]::ToString($rawKey) -replace '-', ''
Write-Host "Master key başarıyla çıkarıldı!" -ForegroundColor Green
Write-Host "  DPAPI Scope: $scope"
Write-Host "  Key uzunluk: $($rawKey.Length) byte"
Write-Host "  Key (HEX)  : $rawKeyHex"
Write-Host ""

# Raw key'i dosyaya kaydet
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = "." }
$keyOutputFile = Join-Path $scriptDir "raw-key.txt"
$rawKeyHex | Out-File -FilePath $keyOutputFile -Encoding ASCII -NoNewline
Write-Host "Raw key kaydedildi: $keyOutputFile" -ForegroundColor Green

# ---------------------------------------------------------------------------
# 2. İsteğe bağlı: Tüm evrakları decrypt et
# ---------------------------------------------------------------------------
if ($DecryptAll) {
    Write-Host ""
    Write-Host "=== Tüm evraklar decrypt ediliyor ===" -ForegroundColor Cyan

    if (-not (Test-Path $UploadsRoot)) {
        Write-Host "HATA: Uploads root bulunamadı: $UploadsRoot" -ForegroundColor Red
        exit 1
    }

    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

    $encFiles = Get-ChildItem -Path $UploadsRoot -Recurse -File -Filter "*.enc" -ErrorAction SilentlyContinue
    $total = $encFiles.Count
    $success = 0
    $fail = 0

    Write-Host "Toplam .enc dosyası: $total"
    Write-Host "Çıktı dizini: $OutputDir"
    Write-Host ""

    foreach ($file in $encFiles) {
        try {
            $encBytes = [System.IO.File]::ReadAllBytes($file.FullName)

            # KOA1 header kontrolü
            $isKoa1 = $encBytes.Length -ge 33 -and
                      $encBytes[0] -eq 75 -and $encBytes[1] -eq 79 -and
                      $encBytes[2] -eq 65 -and $encBytes[3] -eq 49

            if (-not $isKoa1) {
                Write-Host "ATLANDI (KOA1 değil): $($file.Name)" -ForegroundColor Yellow
                continue
            }

            # KOA1 format: KOA1 | VER(1) | NONCE(12) | TAG(16) | CIPHER
            $ver = $encBytes[4]
            $nonce = $encBytes[5..16]
            $tag = $encBytes[17..32]
            $cipher = $encBytes[33..($encBytes.Length-1)]

            $plain = New-Object byte[] $cipher.Length
            $aes = New-Object System.Security.Cryptography.AesGcm($rawKey, 16)
            $aes.Decrypt($nonce, $cipher, $tag, $plain)
            $aes.Dispose()

            # Orijinal path yapısını koru
            $relativePath = $file.FullName.Substring($UploadsRoot.Length).TrimStart('\', '/')
            $plainPath = Join-Path $OutputDir ($relativePath -replace '\.enc$', '')
            $plainDir = Split-Path -Parent $plainPath
            New-Item -ItemType Directory -Force -Path $plainDir | Out-Null
            [System.IO.File]::WriteAllBytes($plainPath, $plain)

            $success++
            Write-Host "OK: $relativePath -> $plainPath" -ForegroundColor Green
        } catch {
            $fail++
            Write-Host "HATA: $($file.Name) -> $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    Write-Host ""
    Write-Host "=== Decrypt tamamlandı ===" -ForegroundColor Cyan
    Write-Host "Başarılı: $success"
    Write-Host "Başarısız: $fail"
    Write-Host "Toplam: $total"
    Write-Host ""
    Write-Host "Plain dosyalar: $OutputDir" -ForegroundColor Yellow
    Write-Host "DİKKAT: Plain dosyalar şifresizdir! Taşıma sonrası güvenli şekilde silinmelidir." -ForegroundColor Red
}

Write-Host ""
Write-Host "=== İşlem tamam ===" -ForegroundColor Cyan
Write-Host "Bu raw key'i güvenli bir şekilde mevcut PC'ye taşıyın." -ForegroundColor Yellow
Write-Host "Mevcut PC'de recover-evrak.ps1 scriptini bu key ile çalıştırın."

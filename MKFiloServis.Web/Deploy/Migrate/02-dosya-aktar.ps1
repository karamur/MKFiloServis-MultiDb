# =============================================================================
# 02-dosya-aktar.ps1
# Eski şifreli evrak arşivini + data protection key'lerini yeni sisteme kopyalar.
# KULLANIM: pwsh -ExecutionPolicy Bypass -File 02-dosya-aktar.ps1
# =============================================================================

param(
    # Eski kurulumun yedekleme kökü (içinde uploads/ ve keys/ olan yer)
    [string]$EskiYedekKok   = "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\ustunfiloservis_yedekler\MKFiloServis_yedekleme",

    # Yeni sistemin depolama kökü (AppStoragePaths.DefaultStorageRoot)
    [string]$YeniDepolamaKok = "C:\MKFiloServis_yedekleme",

    [switch]$PreflightOnly
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$msg) { Write-Host "[DOSYA] $msg" -ForegroundColor Cyan }
function Write-OK([string]$msg)   { Write-Host "[OK] $msg"    -ForegroundColor Green }
function Write-Warn([string]$msg) { Write-Host "[UYARI] $msg" -ForegroundColor Yellow }
function Write-Err([string]$msg)  { Write-Host "[HATA] $msg"  -ForegroundColor Red }

function Export-PortableMasterKey([string]$sourcePath, [string]$targetPath) {
    Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
    $entropy = [System.Text.Encoding]::UTF8.GetBytes("MKFiloServis.MasterKey.v1")
    $protected = [System.IO.File]::ReadAllBytes($sourcePath)

    foreach ($scope in @(
        [System.Security.Cryptography.DataProtectionScope]::LocalMachine,
        [System.Security.Cryptography.DataProtectionScope]::CurrentUser)) {
        try {
            $plain = [System.Security.Cryptography.ProtectedData]::Unprotect($protected, $entropy, $scope)
            if ($plain.Length -ne 32) { throw "Anahtar uzunluğu 32 byte değil." }

            [System.IO.File]::WriteAllText($targetPath, [Convert]::ToBase64String($plain))
            [Array]::Clear($plain, 0, $plain.Length)
            Write-OK "master.key $scope kapsamında çözüldü."
            return $true
        } catch {
            Write-Warn "master.key $scope kapsamında çözülemedi: $($_.Exception.Message)"
        }
    }

    Write-Err "master.key bu Windows makinesi/kullanıcısı ile çözülemedi."
    return $false
}

function Test-PortableMasterKey([string]$sourcePath) {
    Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
    $entropy = [System.Text.Encoding]::UTF8.GetBytes("MKFiloServis.MasterKey.v1")
    $protected = [System.IO.File]::ReadAllBytes($sourcePath)
    foreach ($scope in @(
        [System.Security.Cryptography.DataProtectionScope]::LocalMachine,
        [System.Security.Cryptography.DataProtectionScope]::CurrentUser)) {
        $plain = $null
        try {
            $plain = [System.Security.Cryptography.ProtectedData]::Unprotect($protected, $entropy, $scope)
            if ($plain.Length -eq 32) { return $true }
        } catch { }
        finally { if ($plain) { [Array]::Clear($plain, 0, $plain.Length) } }
    }
    return $false
}

function Test-RawMasterKey([string]$sourcePath) {
    try {
        $compact = [regex]::Replace([System.IO.File]::ReadAllText($sourcePath), '\s', '')
        if ($compact.Length -eq 64) {
            try { $bytes = [Convert]::FromHexString($compact) } catch { $bytes = [Convert]::FromBase64String($compact) }
        } else { $bytes = [Convert]::FromBase64String($compact) }
        $valid = $bytes.Length -eq 32
        if ($bytes) { [Array]::Clear($bytes, 0, $bytes.Length) }
        return $valid
    } catch { return $false }
}

# ---------------------------------------------------------------------------
# 1. Kaynak doğrulama
# ---------------------------------------------------------------------------
Write-Step "Kaynak klasörler kontrol ediliyor..."

$eskiUploads = Join-Path $EskiYedekKok "uploads"
$eskiKeys    = Join-Path $EskiYedekKok "keys"

if (-not (Test-Path $EskiYedekKok)) { Write-Err "Eski yedek kök bulunamadı: $EskiYedekKok"; exit 1 }
if (-not (Test-Path $eskiUploads))  { Write-Err "Eski uploads klasörü bulunamadı: $eskiUploads"; exit 1 }
if (-not (Test-Path $eskiKeys))     { Write-Warn "Eski keys klasörü bulunamadı: $eskiKeys (keys taşınmayacak)" }

# Şifreli dosyaların gerektirdiği anahtarları hedefe herhangi bir dosya yazmadan önce doğrula.
$encryptedFiles = @(Get-ChildItem -LiteralPath $eskiUploads -Recurse -File -Filter "*.enc" -ErrorAction Stop)
$hasDataProtectionFiles = $false
$hasLegacyEncryptedFiles = $false
foreach ($file in $encryptedFiles) {
    $header = [byte[]]::new(4)
    $stream = [System.IO.File]::OpenRead($file.FullName)
    try { $read = $stream.Read($header, 0, 4) } finally { $stream.Dispose() }
    if ($read -eq 4 -and [System.Text.Encoding]::ASCII.GetString($header) -eq "MKD1") {
        $hasDataProtectionFiles = $true
    } else {
        $hasLegacyEncryptedFiles = $true
    }
}
$keyFiles = if (Test-Path $eskiKeys) { @(Get-ChildItem -LiteralPath $eskiKeys -File -Filter "key-*.xml" -ErrorAction Stop) } else { @() }
$rawKeyCandidates = @()
if (Test-Path $eskiKeys) {
    foreach ($name in @("master.key.import", "raw-key.txt", "raw-key.txt.bak", "master.key.raw", "master.key.txt")) {
        $candidate = Join-Path $eskiKeys $name
        if ((Test-Path -LiteralPath $candidate -PathType Leaf) -and (Test-RawMasterKey $candidate)) { $rawKeyCandidates += $candidate }
    }
}
$masterKey = Join-Path $eskiKeys "master.key"
$protectedMasterKeyUsable = (Test-Path -LiteralPath $masterKey -PathType Leaf) -and (Test-PortableMasterKey $masterKey)
if ($hasDataProtectionFiles -and $keyFiles.Count -eq 0) {
    throw "MKD1 şifreli dosyalar bulundu ancak kaynakta key-*.xml DataProtection anahtarı yok. Dosya aktarımı durduruldu."
}
if ($hasLegacyEncryptedFiles -and -not $protectedMasterKeyUsable -and $rawKeyCandidates.Count -eq 0) {
    throw "Legacy/AES şifreli dosyalar bulundu ancak kaynakta çözülebilen master.key veya geçerli ham anahtar yok. Dosya aktarımı durduruldu."
}
if ($protectedMasterKeyUsable) { Write-OK "Legacy master.key kaynak makinede çözülebiliyor." }
elseif ($rawKeyCandidates.Count -gt 0) { Write-OK "Geçerli taşınabilir legacy master key bulundu: $(Split-Path $rawKeyCandidates[0] -Leaf)" }
elseif ($hasLegacyEncryptedFiles) { throw "Legacy dosyalar için anahtar doğrulanamadı." }

if ($PreflightOnly) {
    Write-OK "Dosya/anahtar ön kontrolü tamamlandı; hedefe hiçbir dosya yazılmadı."
    return
}

# ---------------------------------------------------------------------------
# 2. Hedef klasörleri oluştur
# ---------------------------------------------------------------------------
$yeniUploads = Join-Path $YeniDepolamaKok "uploads"
$yeniKeys    = Join-Path $YeniDepolamaKok "keys"

Write-Step "Hedef klasörler oluşturuluyor..."
New-Item -ItemType Directory -Force -Path $yeniUploads | Out-Null
New-Item -ItemType Directory -Force -Path $yeniKeys    | Out-Null
Write-OK "Hedef hazır: $YeniDepolamaKok"

# ---------------------------------------------------------------------------
# 3. Şifreli evrakları robocopy ile kopyala
# ---------------------------------------------------------------------------
Write-Step "Şifreli evraklar kopyalanıyor: $eskiUploads → $yeniUploads"
Write-Step "(Büyük arşivde bu adım birkaç dakika sürebilir)"

$encCount = (Get-ChildItem $eskiUploads -Recurse -File -Filter "*.enc" -ErrorAction SilentlyContinue).Count
$allCount = (Get-ChildItem $eskiUploads -Recurse -File -ErrorAction SilentlyContinue).Count
Write-Step "Kaynak: $allCount dosya ($encCount şifreli .enc)"

& robocopy $eskiUploads $yeniUploads /E /R:2 /W:2 /NP /NDL /NJH /NJS
if ($LASTEXITCODE -gt 7) {
    Write-Err "Robocopy hata kodu: $LASTEXITCODE"
    exit 1
}

$kopyalanan = (Get-ChildItem $yeniUploads -Recurse -File -ErrorAction SilentlyContinue).Count
Write-OK "Kopyalanan dosya: $kopyalanan"

# ---------------------------------------------------------------------------
# 4. Data protection key'lerini kopyala
# ---------------------------------------------------------------------------
if (Test-Path $eskiKeys) {
    Write-Step "Data protection key'leri kopyalanıyor: $eskiKeys → $yeniKeys"

    if ($keyFiles.Count -eq 0) {
        Write-Warn "key-*.xml dosyası bulunamadı; kaynakta MKD1 dosya gereksinimi yok."
    } else {
        foreach ($kf in $keyFiles) {
            $dest = Join-Path $yeniKeys $kf.Name
            Copy-Item $kf.FullName $dest -Force
            Write-OK "Key kopyalandı: $($kf.Name)"
        }
    }

    # DPAPI master.key makineye bağlıdır; başka sunucuya doğrudan kopyalanamaz.
    # Kaynak makinede çözülüp tek kullanımlık import dosyası olarak aktarılır.
    if (Test-Path $masterKey) {
        $portableKey = Join-Path $yeniKeys "master.key.import"
        if ($protectedMasterKeyUsable -and (Export-PortableMasterKey $masterKey $portableKey)) {
            Write-OK "Master key taşınabilir import dosyasına dönüştürüldü."
            Write-Warn "Uygulama ilk açılışta anahtarı sunucu DPAPI'si ile koruyup import dosyasını silecek."
        } elseif ($rawKeyCandidates.Count -gt 0) {
            Copy-Item -LiteralPath $rawKeyCandidates[0] -Destination $portableKey -Force
            Write-OK "Geçerli taşınabilir master key hedef içe aktarma dosyasına aktarıldı."
        } else {
            Write-Err "Anahtar aktarılmadan evraklar hedef sunucuda açılamaz. İşlem durduruldu."
            exit 1
        }
    } else {
        if ($rawKeyCandidates.Count -gt 0) {
            Copy-Item -LiteralPath $rawKeyCandidates[0] -Destination (Join-Path $yeniKeys "master.key.import") -Force
            Write-OK "Taşınabilir master key hedef içe aktarma dosyasına aktarıldı."
        } else {
            Write-Warn "Şifreli legacy dosya yok; kaynak master.key bulunmadığından taşıma gerekmiyor."
        }
    }

    # Legacy raw key yalnızca kurtarma yedeği olarak aktarılır.
    $rawKeyTxt = Join-Path $eskiKeys "raw-key.txt"
    if (Test-Path $rawKeyTxt) {
        Copy-Item $rawKeyTxt (Join-Path $yeniKeys "raw-key.txt.bak") -Force
        Write-OK "raw-key.txt referans kopyası alındı (raw-key.txt.bak)."
    }
}

# ---------------------------------------------------------------------------
# 5. Eski database yedeklerini yeni yapıya kopyala (bilgi amaçlı)
# ---------------------------------------------------------------------------
$eskiDatabase = Join-Path $EskiYedekKok "database"
$yeniDatabase = Join-Path $YeniDepolamaKok "database"

if (Test-Path $eskiDatabase) {
    Write-Step "Eski DB yedekleri kopyalanıyor (tarihsel arşiv): $eskiDatabase → $yeniDatabase"
    New-Item -ItemType Directory -Force -Path $yeniDatabase | Out-Null
    & robocopy $eskiDatabase $yeniDatabase /E /R:2 /W:2 /NP /NDL /NJH /NJS
    if ($LASTEXITCODE -le 7) {
        $dbCount = (Get-ChildItem $yeniDatabase -Recurse -File -ErrorAction SilentlyContinue).Count
        Write-OK "DB yedekleri kopyalandı: $dbCount dosya"
    } else {
        Write-Warn "DB yedek kopyasında uyarı (kod: $LASTEXITCODE) - devam ediliyor."
    }
}

# ---------------------------------------------------------------------------
# 6. Özet
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  DOSYA AKTARIM ÖZETI" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Kaynak : $EskiYedekKok" -ForegroundColor White
Write-Host "  Hedef  : $YeniDepolamaKok" -ForegroundColor White
Write-Host ""
Write-Host "  Uploads  : $yeniUploads" -ForegroundColor Green
Write-Host "  Keys     : $yeniKeys" -ForegroundColor Green
Write-Host "  Database : $yeniDatabase" -ForegroundColor Green
Write-Host ""
Write-OK "Dosya aktarımı tamamlandı!"
Write-Host ""
Write-Host "SONRAKI ADIM: Uygulamayı başlatın ve 'Arşiv Göçü' menüsünden" -ForegroundColor Yellow
Write-Host "eski uploads/ yollarını yeni Arsiv/ yapısına dönüştürün." -ForegroundColor Yellow
Write-Host ""

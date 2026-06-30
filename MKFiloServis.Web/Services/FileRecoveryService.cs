using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Security;
using System.Security.Cryptography;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Eski master key problemi sonrasında şifrelenmiş dosyaları kurtarma servisi.
/// - Eski anahtarla decrypt (raw-key.txt.bak veya backup'tan)
/// - Yeni anahtarla re-encrypt
/// - Uygulamanın recovery modunda çalıştırılması için tasarlanmış
/// </summary>
public sealed class FileRecoveryService
{
    private readonly IFileProtector _fileProtector;
    private readonly string _baseStorageRoot;
    private readonly ILogger<FileRecoveryService> _logger;

    public FileRecoveryService(
        IFileProtector fileProtector,
        IWebHostEnvironment environment,
        ILogger<FileRecoveryService> logger)
    {
        _fileProtector = fileProtector;
        _baseStorageRoot = AppStoragePaths.GetStorageRoot(environment.ContentRootPath);
        _logger = logger;
    }

    /// <summary>
    /// Eski anahtarla şifrelenmiş dosyaları, yeni anahtarla re-encrypt eder.
    /// Arşiv/Sifreli/ klasöründe bozuk dosyaları tarar ve kurtarmaya çalışır.
    /// </summary>
    public async Task<RecoveryResult> RecoverEncryptedFilesAsync(
        byte[] oldMasterKey,
        string? targetDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var result = new RecoveryResult();

        if (oldMasterKey.Length != 32)
        {
            _logger.LogError("Eski master key geçersiz uzunluk: {Len} (32 bekleniyor)", oldMasterKey.Length);
            result.ErrorMessage = $"Eski master key geçersiz: {oldMasterKey.Length} byte (32 bekleniyor)";
            return result;
        }

        // Taranacak dizinler
        var scanRoots = targetDirectory != null
            ? new[] { Path.Combine(_baseStorageRoot, targetDirectory) }
            : new[]
            {
                Path.Combine(_baseStorageRoot, AppStoragePaths.PersonelEvrakRelativeRoot),
                Path.Combine(_baseStorageRoot, AppStoragePaths.AracEvrakRelativeRoot)
            };

        foreach (var root in scanRoots)
        {
            if (!Directory.Exists(root))
            {
                _logger.LogWarning("Tarama dizini bulunamadı: {Dir}", root);
                continue;
            }

            await ScanAndRecoverDirectoryAsync(root, oldMasterKey, result, cancellationToken);
        }

        _logger.LogInformation(
            "📋 Recovery tamamlandı: {Success} başarı, {Failed} hata, {Skipped} skip",
            result.SuccessCount, result.FailedCount, result.SkippedCount);

        return result;
    }

    private async Task ScanAndRecoverDirectoryAsync(
        string directoryPath,
        byte[] oldMasterKey,
        RecoveryResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var encFiles = Directory.GetFiles(directoryPath, "*.enc", SearchOption.AllDirectories);
            _logger.LogInformation("📁 {Count} .enc dosyası tarandı: {Dir}", encFiles.Length, directoryPath);

            foreach (var filePath in encFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var relativeFromBase = Path.GetRelativePath(_baseStorageRoot, filePath)
                        .Replace(Path.DirectorySeparatorChar, '/');

                    _logger.LogDebug("🔄 Kurtarma denemesi: {File}", relativeFromBase);

                    if (await TryRecoverFileAsync(filePath, oldMasterKey, cancellationToken))
                    {
                        result.SuccessCount++;
                        result.RecoveredFiles.Add(relativeFromBase);
                        _logger.LogInformation("✅ Kurtarıldı: {File}", relativeFromBase);
                    }
                    else
                    {
                        result.SkippedCount++;
                        _logger.LogWarning("⏭️ Atlandı (yeni key ile açılamadı): {File}", relativeFromBase);
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedFiles.Add(new FailedFileInfo
                    {
                        Path = Path.GetRelativePath(_baseStorageRoot, filePath),
                        Error = ex.Message
                    });
                    _logger.LogError(ex, "❌ Kurtarma hatası: {File}", filePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Dizin taraması hatası: {Dir}", directoryPath);
        }
    }

    private async Task<bool> TryRecoverFileAsync(
        string encryptedFilePath,
        byte[] oldMasterKey,
        CancellationToken cancellationToken)
    {
        var encryptedData = await File.ReadAllBytesAsync(encryptedFilePath, cancellationToken);

        // 1) Yeni key ile açılabilir mi? (skip, sorun yok)
        try
        {
            var decrypted = _fileProtector.Unprotect(encryptedData);
            _logger.LogDebug("✓ Dosya zaten yeni key ile açılabiliyor: {Path}", encryptedFilePath);
            return true; // Sorun yok, skip
        }
        catch (CryptographicException)
        {
            // Devam et, eski key ile dene
        }

        // 2) Eski key ile decrypt edebilelim mi?
        byte[]? decryptedPlain;
        try
        {
            decryptedPlain = DecryptWithOldKey(encryptedData, oldMasterKey);
            if (decryptedPlain == null || decryptedPlain.Length == 0)
            {
                _logger.LogDebug("⚠️ Eski key ile decrypt başarısız: {Path}", encryptedFilePath);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("⚠️ Eski key dekriptasyon hatası: {Msg}", ex.Message);
            return false;
        }

        // 3) Yeni key ile re-encrypt et
        try
        {
            var reEncrypted = _fileProtector.Protect(decryptedPlain);
            await File.WriteAllBytesAsync(encryptedFilePath, reEncrypted, CancellationToken.None);
            _logger.LogInformation("🔄 Yeniden şifrelendi (old→new): {Path}", encryptedFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Re-encryption hatası: {Path}", encryptedFilePath);
            return false;
        }
    }

    /// <summary>
    /// Eski master key ile AES-GCM decrypt denemeleri (legacy formatlar dahil).
    /// Null dönersa format tanınamadı.
    /// </summary>
    private byte[]? DecryptWithOldKey(byte[] cipherData, byte[] oldMasterKey)
    {
        const int NonceSize = 12;
        const int TagSize = 16;
        const int HeaderSize = 4 + 1 + NonceSize + TagSize; // MAGIC + VER + NONCE + TAG

        if (cipherData.Length < 32)
            return null;

        // 1) Yeni format: KOA1 | VER | NONCE | TAG | CIPHER
        if (cipherData.Length >= HeaderSize &&
            cipherData[0] == (byte)'K' && cipherData[1] == (byte)'O' &&
            cipherData[2] == (byte)'A' && cipherData[3] == (byte)'1')
        {
            if (cipherData[4] == 0x01)
            {
                try
                {
                    var nonce = cipherData.AsSpan(5, NonceSize);
                    var tag = cipherData.AsSpan(5 + NonceSize, TagSize);
                    var body = cipherData.AsSpan(HeaderSize);

                    var plain = new byte[body.Length];
                    using var aes = new AesGcm(oldMasterKey, TagSize);
                    aes.Decrypt(nonce, body, tag, plain);
                    return plain;
                }
                catch (CryptographicException)
                {
                    // Devam
                }
            }
        }

        // 2) Legacy KOA1 (versiyonsuz): KOA1 | NONCE | TAG | CIPHER
        if (cipherData.Length >= 4 + NonceSize + TagSize)
        {
            try
            {
                var nonce = cipherData.AsSpan(4, NonceSize);
                var tag = cipherData.AsSpan(4 + NonceSize, TagSize);
                var body = cipherData.AsSpan(4 + NonceSize + TagSize);

                var plain = new byte[body.Length];
                using var aes = new AesGcm(oldMasterKey, TagSize);
                aes.Decrypt(nonce, body, tag, plain);
                return plain;
            }
            catch (CryptographicException)
            {
                // Devam
            }
        }

        // 3) Legacy KOA1: KOA1 | NONCE | CIPHER | TAG
        if (cipherData.Length >= 4 + NonceSize + TagSize)
        {
            try
            {
                var nonce = cipherData.AsSpan(4, NonceSize);
                var tag = cipherData.AsSpan(cipherData.Length - TagSize, TagSize);
                var body = cipherData.AsSpan(4 + NonceSize, cipherData.Length - (4 + NonceSize + TagSize));

                var plain = new byte[body.Length];
                using var aes = new AesGcm(oldMasterKey, TagSize);
                aes.Decrypt(nonce, body, tag, plain);
                return plain;
            }
            catch (CryptographicException)
            {
                // Devam
            }
        }

        return null;
    }

    public class RecoveryResult
    {
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> RecoveredFiles { get; } = new();
        public List<FailedFileInfo> FailedFiles { get; } = new();
        public string? ErrorMessage { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage) && FailedCount == 0;
    }

    public class FailedFileInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}



using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Security;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MKFiloServis.Web.Services;

public sealed class SecureFileService : ISecureFileService
{
    private readonly IFileProtector _fileProtector;
    private readonly IFileProtector _legacyAesProtector;
    // Eski IDataProtector ile sifrelenmis dosyalar icin fallback (gecis donemi)
    private readonly IDataProtector _legacyProtector;
    private readonly string _storageRoot;        // C:\KOAFiloServis_yedekleme\uploads
    private readonly string _baseStorageRoot;    // C:\KOAFiloServis_yedekleme (uploads üst dizini)
    private readonly ILogger<SecureFileService> _logger;
    private readonly IDecryptionRecoveryTracker _recoveryTracker;
    private readonly FileCleanupJournal _cleanupJournal;
    private readonly IServiceScopeFactory _scopeFactory;

    public SecureFileService(
        IFileProtector fileProtector,
        IMasterKeyProvider legacyMasterKeyProvider,
        IDataProtectionProvider dataProtectionProvider,
        IWebHostEnvironment environment,
        ILogger<SecureFileService> logger,
        IDecryptionRecoveryTracker recoveryTracker,
        FileCleanupJournal cleanupJournal,
        IServiceScopeFactory scopeFactory)
    {
        _fileProtector = fileProtector;
        _legacyAesProtector = new AesGcmFileProtector(legacyMasterKeyProvider);
        _legacyProtector = dataProtectionProvider.CreateProtector("MKFiloServis.SecureFileStorage.v1");
        _storageRoot = AppStoragePaths.GetUploadsRoot(environment.ContentRootPath);
        _baseStorageRoot = AppStoragePaths.GetStorageRoot(environment.ContentRootPath);
        _logger = logger;
        _recoveryTracker = recoveryTracker;
        _cleanupJournal = cleanupJournal;
        _scopeFactory = scopeFactory;
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> SaveEncryptedAsync(
        string relativeDirectory,
        string originalFileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var safeName = string.Concat(Path.GetFileNameWithoutExtension(originalFileName)
            .Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));

        var fileName = $"{safeName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}.enc";
        var relativePath = NormalizeRelativePath(Path.Combine(relativeDirectory, fileName));
        var fullPath = ResolveFullPath(relativePath);

        var encrypted = _fileProtector.Protect(content);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + ".uploading-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            // Publish only a complete encrypted file. The temporary file is on the same
            // volume so the final move is atomic and the database never receives a path
            // to a partially written payload.
            await File.WriteAllBytesAsync(temporaryPath, encrypted, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: false);
        }
        catch (Exception writeException)
        {
            try
            {
                // File.Delete is idempotent for a missing file and avoids a File.Exists
                // check that can hide access/IO errors.
                File.Delete(temporaryPath);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Şifreli dosya yüklemesi tamamlanamadı ve geçici dosya temizlenemedi.",
                    writeException, cleanupException);
            }

            throw;
        }

        _logger.LogInformation("Dosya kaydedildi: {RelativePath} ({Size} bytes)", relativePath, content.Length);
        return relativePath;
    }

    public async Task<byte[]?> ReadDecryptedAsync(
        string? relativePath,
        CancellationToken cancellationToken = default)
    {
        return await ReadDecryptedAsync(relativePath, overrideMasterKey: null, cancellationToken);
    }

    /// <summary>
    /// Recovery modunda eski anahtarla dekriptasyon destekler.
    /// </summary>
    public async Task<byte[]?> ReadDecryptedAsync(
        string? relativePath,
        byte[]? overrideMasterKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var rawContent = await ReadRawAsync(relativePath, cancellationToken);
        if (rawContent == null)
            return null;

        var dataProtectionFormat = DataProtectionFileProtector.IsProtectedFormat(rawContent);
        var koa1Format = rawContent.Length >= 5 &&
                         rawContent[0] == (byte)'K' && rawContent[1] == (byte)'O' &&
                         rawContent[2] == (byte)'A' && rawContent[3] == (byte)'1';

        // 1) Yeni format: MKD1 + ASP.NET Core Data Protection
        if (dataProtectionFormat)
        {
            try
            {
                var result = _fileProtector.Unprotect(rawContent);
                _logger.LogDebug("Dosya okundu (Data Protection): {RelativePath}", relativePath);
                return result;
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex,
                    "Data Protection dosyası çözülemedi. Key ring yedeği gerekli olabilir. Path={Path}",
                    relativePath);
            }
        }

        // 2) Eski format: KOA1 magic ile sifreli (AES-256-GCM)
        if (koa1Format)
        {
            try
            {
                var result = _legacyAesProtector.Unprotect(rawContent);
                _logger.LogDebug("Dosya okundu (AES-256-GCM): {RelativePath}", relativePath);
                return result;
            }
            catch (Exception ex) when (ex is CryptographicException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Eski AES dosyası çözülemedi; orijinal master key gerekli. Path={Path}", relativePath);
            }
        }

        // 3) Eski format: IDataProtector ile sifreli (geriye uyumluluk)
        try
        {
            var result = _legacyProtector.Unprotect(rawContent);
            _logger.LogInformation("✓ Dosya legacy formatla çözüldü (eski master key ile). Path={Path}", relativePath);
            return result;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex,
                "Dosya çözülemedi (Data Protection + AES + Legacy). " +
                "Muhtemel Sebepler:\n" +
                "  - Master key değişti (eski key ile şifrelenmiş)\n" +
                "  - Dosya başka makinede şifrelenmiş\n" +
                "  - Dosya bozulmuş veya kesilebilir\n" +
                "Path={Path}\n" +
                "Detay: {DetailMessage}", relativePath, ex.Message);

            // Diagnostic bilgisi ekle (üretim ortamında sensitive değil)
            _logger.LogInformation(
                "Diagnostic: DataProtectionFormat={IsDataProtection}, KOA1Format={IsKoa1}, RawLength={Length}",
                dataProtectionFormat, koa1Format, rawContent.Length);

            // Recovery tracker'a kaydet
            _recoveryTracker.TrackDecryptionFailure(
                relativePath,
                ex.InnerException?.Message ?? ex.Message);

            return null;
        }
    }

    public async Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        try
        {
            var pendingPath = relativePath.Trim().Replace('\\', '/');
            var fullPath = ResolveFullPath(NormalizeRelativePath(pendingPath));
            var request = await _cleanupJournal.EnqueueAsync(pendingPath, cancellationToken);
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var checker = scope.ServiceProvider.GetRequiredService<ISecureFileReferenceChecker>();
                if (await checker.IsReferencedAsync(pendingPath, cancellationToken))
                {
                    // Soft-delete geçmişindeki dosyalar geri alınabilir kalmalıdır.
                    // Referans devam ettiği sürece bu isteği tekrar denemek gerekmez.
                    if (!request.WaitForReferenceRemoval)
                        await _cleanupJournal.CompleteRequestAsync(request, cancellationToken);
                    _logger.LogInformation("Dosya DB kaydı tarafından tutulduğu için fiziksel olarak korundu: {RelativePath}", pendingPath);
                    return;
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            QuarantineIfPresent(fullPath);
            _logger.LogInformation("Referanssız şifreli dosya geri alınabilir karantinaya taşındı: {RelativePath}", relativePath);
            await _cleanupJournal.CompleteRequestAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fiziksel dosya silinemedi: {RelativePath}", relativePath);
            throw;
        }
    }

    /// <summary>Worker path for journal entries that must wait until the DB reference disappears.</summary>
    public async Task<bool> TryDeleteIfUnreferencedAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(relativePath))
            return true;

        var pendingPath = relativePath.Trim().Replace('\\', '/');
        var fullPath = ResolveFullPath(NormalizeRelativePath(pendingPath));
        await using var scope = _scopeFactory.CreateAsyncScope();
        var checker = scope.ServiceProvider.GetRequiredService<ISecureFileReferenceChecker>();
        if (await checker.IsReferencedAsync(pendingPath, cancellationToken))
            return false;

        cancellationToken.ThrowIfCancellationRequested();
        QuarantineIfPresent(fullPath);
        return true;
    }

    public async Task<string> CopyEncryptedAsync(
        string sourceRelativePath,
        string targetDirectory,
        string targetFileName,
        CancellationToken cancellationToken = default)
    {
        var originalSourceFull = ResolveFullPath(NormalizeRelativePath(sourceRelativePath));
        var sourceFull = ResolveReadableFullPath(originalSourceFull);
        if (!File.Exists(sourceFull))
            throw new FileNotFoundException($"Kaynak dosya bulunamadı: {sourceRelativePath}");

        var targetDir = NormalizeRelativePath(targetDirectory);
        var safeName = string.Concat(Path.GetFileNameWithoutExtension(targetFileName)
            .Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        var extension = Path.GetExtension(targetFileName);
        var fileName = $"{safeName}{extension}";

        var relativeResult = NormalizeRelativePath(Path.Combine(targetDir, fileName));
        var targetFull = ResolveFullPath(relativeResult);

        Directory.CreateDirectory(Path.GetDirectoryName(targetFull)!);

        // Çakışma çöz
        var finalTargetFull = targetFull;
        var finalRelative = relativeResult;
        var counter = 1;
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var fileExt = Path.GetExtension(fileName);
        while (File.Exists(finalTargetFull))
        {
            var collisionName = $"{nameWithoutExt}_{counter:D3}{fileExt}";
            finalRelative = NormalizeRelativePath(Path.Combine(targetDir, collisionName));
            finalTargetFull = ResolveFullPath(finalRelative);
            counter++;
        }

        try
        {
            File.Copy(sourceFull, finalTargetFull, overwrite: false);
        }
        catch (FileNotFoundException) when (sourceFull == originalSourceFull)
        {
            // Cleanup may have moved the source after the existence check.
            File.Copy(GetQuarantinePath(originalSourceFull), finalTargetFull, overwrite: false);
        }
        catch (DirectoryNotFoundException) when (sourceFull == originalSourceFull)
        {
            File.Copy(GetQuarantinePath(originalSourceFull), finalTargetFull, overwrite: false);
        }

        _logger.LogInformation("Dosya kopyalandı: {Source} -> {Target}", sourceRelativePath, finalRelative);
        return finalRelative;
    }

    public Task<bool> ExistsAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.FromResult(false);

        var fullPath = ResolveFullPath(NormalizeRelativePath(relativePath));
        return Task.FromResult(File.Exists(fullPath) || File.Exists(GetQuarantinePath(fullPath)));
    }

    private async Task<byte[]?> ReadRawAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = ResolveFullPath(NormalizeRelativePath(relativePath));
        try
        {
            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }

        try
        {
            return await File.ReadAllBytesAsync(GetQuarantinePath(fullPath), cancellationToken);
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    // Kalıcı DB yoluna yeni bir referans, referans kontrolü ile silme arasına girebilir.
    // Dosya bu nedenle geri alınabilir biçimde saklanır. RecoveryArchive uploads
    // ağacını yedeklediği için karantina da tam dosya yedeğine dahil olur.
    private string GetQuarantinePath(string fullPath)
    {
        // Preserve the logical key under quarantine so an operator can recover
        // an orphan even after its DB row is gone, on a different machine/root.
        var storageKey = Path.GetRelativePath(_baseStorageRoot, Path.GetFullPath(fullPath))
            .Replace('\\', '/');
        if (storageKey == ".." || storageKey.StartsWith("../", StringComparison.Ordinal))
            throw new InvalidOperationException("Karantina yolu depolama kökü dışında.");
        return StorageFilePath.Resolve(_storageRoot,
            Path.Combine(".deleted-file-quarantine-v1", storageKey.Replace('/', Path.DirectorySeparatorChar)));
    }

    private string ResolveReadableFullPath(string fullPath) =>
        File.Exists(fullPath) ? fullPath : GetQuarantinePath(fullPath);

    private void QuarantineIfPresent(string fullPath)
    {
        if (!fullPath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(fullPath))
                throw new InvalidOperationException("Düz dosya SecureFileService karantinasına alınamaz; eski dosya geçişi ve LegacyFileCleanupService kullanılmalıdır.");
            return;
        }
        var quarantinePath = GetQuarantinePath(fullPath);
        Directory.CreateDirectory(Path.GetDirectoryName(quarantinePath)!);
        try
        {
            // Same-volume rename publishes the whole encrypted file atomically.
            // Never overwrite a previous quarantined copy of the same path.
            File.Move(fullPath, quarantinePath, overwrite: false);
        }
        catch (FileNotFoundException) when (File.Exists(quarantinePath)) { }
        catch (DirectoryNotFoundException) when (File.Exists(quarantinePath)) { }
        catch (FileNotFoundException) when (!File.Exists(fullPath)) { }
        catch (DirectoryNotFoundException) when (!File.Exists(fullPath)) { }
    }

    private string ResolveFullPath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');

        string rootToUse;

        // Arşiv ve Depo kendi klasörleriyle sınırlıdır; base storage içindeki
        // anahtar/yedek dosyalarına ../ ile erişim verilmez.
        if (normalized.StartsWith("Arsiv/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("Depo/", StringComparison.OrdinalIgnoreCase))
        {
            var separatorIndex = normalized.IndexOf('/');
            rootToUse = Path.Combine(_baseStorageRoot, normalized[..separatorIndex]);
            normalized = normalized[(separatorIndex + 1)..];
        }
        else
        {
            // Eski path: uploads/ ön ekini temizle, uploads root altına yerleştir
            if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("uploads/".Length);

            rootToUse = _storageRoot;
        }

        // uploads yolları base storage root'a da kaçamamalı; yalnız seçilen köke izin ver.
        return StorageFilePath.Resolve(rootToUse, normalized);
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("uploads/".Length);

        return normalized;
    }
}





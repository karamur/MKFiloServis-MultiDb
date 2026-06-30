using KOAFiloServis.Web.Helpers;
using KOAFiloServis.Web.Services.Security;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace KOAFiloServis.Web.Services;

public sealed class SecureFileService : ISecureFileService
{
    private readonly IFileProtector _fileProtector;
    // Eski IDataProtector ile sifrelenmis dosyalar icin fallback (gecis donemi)
    private readonly IDataProtector _legacyProtector;
    private readonly string _storageRoot;        // C:\KOAFiloServis_yedekleme\uploads
    private readonly string _baseStorageRoot;    // C:\KOAFiloServis_yedekleme (uploads üst dizini)
    private readonly ILogger<SecureFileService> _logger;
    private readonly IDecryptionRecoveryTracker _recoveryTracker;

    public SecureFileService(
        IFileProtector fileProtector,
        IDataProtectionProvider dataProtectionProvider,
        IWebHostEnvironment environment,
        ILogger<SecureFileService> logger,
        IDecryptionRecoveryTracker recoveryTracker)
    {
        _fileProtector = fileProtector;
        _legacyProtector = dataProtectionProvider.CreateProtector("KOAFiloServis.SecureFileStorage.v1");
        _storageRoot = AppStoragePaths.GetUploadsRoot(environment.ContentRootPath);
        _baseStorageRoot = AppStoragePaths.GetStorageRoot(environment.ContentRootPath);
        _logger = logger;
        _recoveryTracker = recoveryTracker;
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
        await File.WriteAllBytesAsync(fullPath, encrypted, cancellationToken);

        _logger.LogInformation("Dosya kaydedildi: {RelativePath} ({Size} bytes)", relativePath, content.Length);
        return relativePath;
    }

    public async Task<byte[]?> ReadDecryptedAsync(
        string? relativePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var rawContent = await ReadRawAsync(relativePath, cancellationToken);
        if (rawContent == null)
            return null;

        var koa1Format = rawContent.Length >= 5 &&
                         rawContent[0] == (byte)'K' && rawContent[1] == (byte)'O' &&
                         rawContent[2] == (byte)'A' && rawContent[3] == (byte)'1';

        // 1) Yeni format: KOA1 magic ile sifreli (AES-256-GCM)
        if (koa1Format)
        {
            try
            {
                var result = _fileProtector.Unprotect(rawContent);
                _logger.LogDebug("Dosya okundu (AES-256-GCM): {RelativePath}", relativePath);
                return result;
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex, "❌ Yeni format decrypt başarısız, legacy format deneniyor. Path={Path}. İçerik: {ExMsg}", relativePath, ex.Message);
            }
        }

        // 2) Eski format: IDataProtector ile sifreli (geriye uyumluluk)
        try
        {
            var result = _legacyProtector.Unprotect(rawContent);
            _logger.LogInformation("✓ Dosya legacy formatla çözüldü (eski master key ile). Path={Path}", relativePath);
            return result;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex,
                "❌ DECODE HATA: Dosya dekrypt edilemedi (AES + Legacy). " +
                "Muhtemel Sebepler:\n" +
                "  - Master key değişti (eski key ile şifrelenmiş)\n" +
                "  - Dosya başka makinede şifrelenmiş\n" +
                "  - Dosya bozulmuş veya kesilebilir\n" +
                "Path={Path}\n" +
                "Detay: {DetailMessage}", relativePath, ex.Message);

            // Diagnostic bilgisi ekle (üretim ortamında sensitive değil)
            _logger.LogInformation("📋 Diagnostic: KOA1Format={IsKoa1}, RawLength={Length}", koa1Format, rawContent.Length);

            // Recovery tracker'a kaydet
            _recoveryTracker.TrackDecryptionFailure(
                relativePath,
                ex.InnerException?.Message ?? ex.Message);

            return null;
        }
    }

    public Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.CompletedTask;

        var fullPath = ResolveFullPath(NormalizeRelativePath(relativePath));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Dosya silindi: {RelativePath}", relativePath);
        }

        return Task.CompletedTask;
    }

    public async Task<string> CopyEncryptedAsync(
        string sourceRelativePath,
        string targetDirectory,
        string targetFileName,
        CancellationToken cancellationToken = default)
    {
        var sourceFull = ResolveFullPath(NormalizeRelativePath(sourceRelativePath));
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

        File.Copy(sourceFull, finalTargetFull, overwrite: false);

        _logger.LogInformation("Dosya kopyalandı: {Source} -> {Target}", sourceRelativePath, finalRelative);
        return finalRelative;
    }

    public Task<bool> ExistsAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.FromResult(false);

        var fullPath = ResolveFullPath(NormalizeRelativePath(relativePath));
        return Task.FromResult(File.Exists(fullPath));
    }

    private async Task<byte[]?> ReadRawAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = ResolveFullPath(NormalizeRelativePath(relativePath));
        if (!File.Exists(fullPath))
            return null;

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    private string ResolveFullPath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');

        string rootToUse;

        // Yeni arşiv path: Arsiv ile başlıyorsa base storage root altında (uploads değil)
        if (normalized.StartsWith("Arsiv/", StringComparison.OrdinalIgnoreCase))
        {
            rootToUse = _baseStorageRoot;
        }
        else
        {
            // Eski path: uploads/ ön ekini temizle, uploads root altına yerleştir
            if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("uploads/".Length);

            rootToUse = _storageRoot;
        }

        var fullPath = Path.GetFullPath(Path.Combine(rootToUse, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var uploadsRootPath = Path.GetFullPath(_storageRoot);
        var baseRootPath = Path.GetFullPath(_baseStorageRoot);

        // Her iki root altında olmasına izin ver
        if (!fullPath.StartsWith(uploadsRootPath, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(baseRootPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Geçersiz dosya yolu: {relativePath}");

        return fullPath;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("uploads/".Length);

        return normalized;
    }
}

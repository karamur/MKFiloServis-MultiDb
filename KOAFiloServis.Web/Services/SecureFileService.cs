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
    private readonly string _storageRoot;
    private readonly ILogger<SecureFileService> _logger;

    public SecureFileService(
        IFileProtector fileProtector,
        IDataProtectionProvider dataProtectionProvider,
        IWebHostEnvironment environment,
        ILogger<SecureFileService> logger)
    {
        _fileProtector = fileProtector;
        _legacyProtector = dataProtectionProvider.CreateProtector("KOAFiloServis.SecureFileStorage.v1");
        _storageRoot = AppStoragePaths.GetUploadsRoot(environment.ContentRootPath);
        _logger = logger;
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

        // 1) Yeni format: KOA1 magic ile sifreli (AES-256-GCM)
        if (rawContent.Length >= 5 &&
            rawContent[0] == (byte)'K' && rawContent[1] == (byte)'O' &&
            rawContent[2] == (byte)'A' && rawContent[3] == (byte)'1')
        {
            try
            {
                var result = _fileProtector.Unprotect(rawContent);
                _logger.LogDebug("Dosya okundu (AES-256-GCM): {RelativePath}", relativePath);
                return result;
            }
            catch (CryptographicException)
            {
                _logger.LogWarning("Dosya cozulemedi (AES-256-GCM): {RelativePath}", relativePath);
                return null;
            }
        }

        // 2) Eski format: IDataProtector ile sifreli (geriye uyumluluk)
        try
        {
            var result = _legacyProtector.Unprotect(rawContent);
            _logger.LogDebug("Dosya okundu (Legacy DPAPI): {RelativePath}", relativePath);
            return result;
        }
        catch (CryptographicException)
        {
            // 3) Ham dosya (test/migration)
            _logger.LogDebug("Dosya okundu (Ham/Plain): {RelativePath}", relativePath);
            return rawContent;
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

    private async Task<byte[]?> ReadRawAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = ResolveFullPath(NormalizeRelativePath(relativePath));
        if (!File.Exists(fullPath))
            return null;

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    private string ResolveFullPath(string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath)
            .Replace('/', Path.DirectorySeparatorChar);

        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, normalized));
        var rootPath = Path.GetFullPath(_storageRoot);

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Gecersiz dosya yolu.");

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

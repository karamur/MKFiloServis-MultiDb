using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Lokal dosya sistemi tabanlı object storage (S3 yokken fallback)
/// </summary>
public class LocalObjectStorageService : IObjectStorageService
{
    private readonly string _rootPath;
    private readonly ILogger<LocalObjectStorageService> _logger;

    public LocalObjectStorageService(IWebHostEnvironment env, ILogger<LocalObjectStorageService> logger)
    {
        _rootPath = AppStoragePaths.GetUploadsRoot(env.ContentRootPath);
        _logger = logger;
    }

    public async Task<string> UploadAsync(string key, byte[] content, string contentType = "application/octet-stream", CancellationToken ct = default)
    {
        var fullPath = GetFullPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, content, ct);
        _logger.LogDebug("LocalStorage: yüklendi {Key}", key);
        return key;
    }

    public async Task<byte[]?> DownloadAsync(string key, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(key);
        if (!File.Exists(fullPath)) return null;
        return await File.ReadAllBytesAsync(fullPath, ct);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(key);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => Task.FromResult(File.Exists(GetFullPath(key)));

    /// <summary>
    /// Lokal depolamada presigned URL desteklenmez — dosyalar sadece SecureFileService üzerinden erişilebilir.
    /// Dış tüketiciler dosyayı DownloadAsync ile indirmeli veya kendi güvenli endpoint'lerini kullanmalıdır.
    /// </summary>
    public Task<string> GetPresignedUrlAsync(string key, int expiresInMinutes = 60)
        => Task.FromResult(string.Empty);

    public string GetStorageProvider() => "Local";

    private string GetFullPath(string key)
    {
        var normalized = key.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(_rootPath, normalized);
    }
}



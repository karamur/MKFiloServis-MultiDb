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
        _rootPath = Path.GetFullPath(AppStoragePaths.GetUploadsRoot(env.ContentRootPath))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _logger = logger;
    }

    public async Task<string> UploadAsync(string key, byte[] content, string contentType = "application/octet-stream", CancellationToken ct = default)
    {
        var fullPath = GetFullPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + ".uploading-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, content, ct);
            ct.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch (Exception writeException)
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Yerel nesne deposu yüklemesi tamamlanamadı ve geçici dosya temizlenemedi.",
                    writeException, cleanupException);
            }

            throw;
        }
        _logger.LogDebug("LocalStorage: yüklendi {Key}", key);
        return key;
    }

    public async Task<byte[]?> DownloadAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var fullPath = GetFullPath(key);
        try
        {
            return await File.ReadAllBytesAsync(fullPath, ct);
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var fullPath = GetFullPath(key);
        StorageFilePath.DeleteIdempotently(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var fullPath = GetFullPath(key);
        try
        {
            return Task.FromResult(!File.GetAttributes(fullPath).HasFlag(FileAttributes.Directory));
        }
        catch (FileNotFoundException) { return Task.FromResult(false); }
        catch (DirectoryNotFoundException) { return Task.FromResult(false); }
    }

    /// <summary>
    /// Lokal depolamada presigned URL desteklenmez — dosyalar sadece SecureFileService üzerinden erişilebilir.
    /// Dış tüketiciler dosyayı DownloadAsync ile indirmeli veya kendi güvenli endpoint'lerini kullanmalıdır.
    /// </summary>
    public Task<string> GetPresignedUrlAsync(string key, int expiresInMinutes = 60)
        => Task.FromException<string>(new NotSupportedException(
            "Yerel depolamada imzalı indirme URL'si desteklenmiyor; dosyayı yetkili indirme uç noktasından alın."));

    public string GetStorageProvider() => "Local";

    private string GetFullPath(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || Path.IsPathRooted(key))
            throw new InvalidOperationException("Yerel depo anahtarı göreli ve boş olmayan bir yol olmalıdır.");

        var normalized = key.Replace('\\', '/');
        return StorageFilePath.Resolve(_rootPath, normalized);
    }
}



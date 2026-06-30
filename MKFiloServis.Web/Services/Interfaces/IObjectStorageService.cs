namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Nesne depolama soyutlama katmanı — Local veya S3-uyumlu (AWS, MinIO, DigitalOcean Spaces vb.)
/// </summary>
public interface IObjectStorageService
{
    Task<string> UploadAsync(string key, byte[] content, string contentType = "application/octet-stream", CancellationToken ct = default);
    Task<byte[]?> DownloadAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string key, int expiresInMinutes = 60);
    string GetStorageProvider();
}





namespace KOAFiloServis.Web.Services;

public interface ISecureFileService
{
    Task<string> SaveEncryptedAsync(string relativeDirectory, string originalFileName, byte[] content, CancellationToken cancellationToken = default);
    Task<byte[]?> ReadDecryptedAsync(string? relativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Şifreli dosyayı olduğu gibi (decrypt etmeden) yeni bir relative path'e kopyalar.
    /// Arşiv taşıma (migration) için kullanılır.
    /// </summary>
    Task<string> CopyEncryptedAsync(string sourceRelativePath, string targetDirectory, string targetFileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen relative path'te dosya var mı?
    /// </summary>
    Task<bool> ExistsAsync(string? relativePath, CancellationToken cancellationToken = default);
}

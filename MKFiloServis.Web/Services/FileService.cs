using MKFiloServis.Web.Helpers;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Geriye dönük ortak evrak dosyalarını çözümlemek ve okumak için salt-okunur legacy erişim.
/// Yeni dosyalar ISecureFileService ile yazılır; bu servis düz metin yazma/silme yapmaz.
/// </summary>
public class FileService
{
    private const string UploadRoot = @"C:\MKFiloServis\uploads";

    public string UploadRootPath => UploadRoot;

    /// <summary>Eski dosyanın izin verilen kök altındaki tam yolunu döndürür.</summary>
    public string GetFullPath(string fileName) => ResolveUploadPath(fileName);

    /// <summary>Eski dosyayı okur; yeni belge yazımında kullanılmaz.</summary>
    public async Task<byte[]> ReadAsync(string fileName)
    {
        var path = ResolveUploadPath(fileName);
        try
        {
            return await File.ReadAllBytesAsync(path);
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException($"Dosya bulunamadı: {fileName}");
        }
        catch (DirectoryNotFoundException)
        {
            throw new FileNotFoundException($"Dosya bulunamadı: {fileName}");
        }
    }

    private static string ResolveUploadPath(string fileName) =>
        StorageFilePath.Resolve(UploadRoot, fileName);
}

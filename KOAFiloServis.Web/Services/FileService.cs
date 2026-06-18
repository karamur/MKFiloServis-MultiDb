using Microsoft.AspNetCore.Components.Forms;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Basit dosya servisi — evrak dosyalarını diske yazar/okur/siler.
/// Şifreleme yok, düz dosya depolama.
/// </summary>
public class FileService
{
    private const string UploadRoot = @"C:\KOAFiloServis\uploads";

    /// <summary>IBrowserFile'ı diske kaydeder, GUID'li dosya adını döner.</summary>
    public async Task<string> SaveAsync(IBrowserFile file)
    {
        Directory.CreateDirectory(UploadRoot);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
        var fullPath = Path.Combine(UploadRoot, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(stream);

        return fileName;
    }

    /// <summary>Diskteki dosyanın tam yolunu döner.</summary>
    public string GetFullPath(string fileName)
    {
        return Path.Combine(UploadRoot, fileName);
    }

    /// <summary>Diskteki dosyayı okur, byte[] döner.</summary>
    public async Task<byte[]> ReadAsync(string fileName)
    {
        var path = Path.Combine(UploadRoot, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Dosya bulunamadi: {fileName}");

        return await File.ReadAllBytesAsync(path);
    }

    /// <summary>Diskteki dosyayı siler.</summary>
    public void Delete(string fileName)
    {
        var path = Path.Combine(UploadRoot, fileName);
        if (File.Exists(path))
            File.Delete(path);
    }
}

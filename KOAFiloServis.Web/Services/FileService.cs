using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Basit dosya servisi — evrak dosyalarını diske yazar/okur/siler.
/// Şifreleme yok, düz dosya depolama.
/// </summary>
public class FileService
{
    private const string UploadRoot = @"C:\KOAFiloServis\uploads";
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    /// <summary>IBrowserFile'ı diske kaydeder, GUID'li dosya adını döner.</summary>
    public async Task<string> SaveAsync(IBrowserFile file)
    {
        var ext = Path.GetExtension(file.Name);

        // PART 3: Unique dosya adı
        Directory.CreateDirectory(UploadRoot);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(UploadRoot, fileName);

        // PART 4: Güvenli dosya yazımı — limitsiz
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.OpenReadStream(maxAllowedSize: long.MaxValue).CopyToAsync(stream);

        _logger.LogInformation("Dosya diske yazildi: {FileName} ({Size} bytes) → {DiskName}", file.Name, file.Size, fileName);
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

    /// <summary>Diskteki dosyayı siler (varsa).</summary>
    public void Delete(string fileName)
    {
        var path = Path.Combine(UploadRoot, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogInformation("Dosya diskten silindi: {FileName}", fileName);
        }
    }
}

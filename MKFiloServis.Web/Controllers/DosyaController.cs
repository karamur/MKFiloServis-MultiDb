using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// Güvenli dosya indirme endpoint'i — Kural 16 (Dosya Güvenliği).
/// Tüm erişimler auth + audit log gerektirir.
/// Doğrudan /uploads static files KALDIRILDI — dosyalar sadece bu endpoint üzerinden erişilebilir.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DosyaController : ControllerBase
{
    private readonly ISecureFileService _secureFileService;
    private readonly ILogger<DosyaController> _logger;

    public DosyaController(
        ISecureFileService secureFileService,
        ILogger<DosyaController> logger)
    {
        _secureFileService = secureFileService;
        _logger = logger;
    }

    /// <summary>
    /// Şifreli dosyayı çözüp indirir. Auth zorunlu.
    /// </summary>
    /// <param name="path">Dosya yolu (SecureFileService relative path)</param>
    [HttpGet("download")]
    public async Task<IActionResult> DownloadAsync([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest("Dosya yolu belirtilmedi.");

        var content = await _secureFileService.ReadDecryptedAsync(path);
        if (content == null)
        {
            _logger.LogWarning("Dosya bulunamadi veya cozulemedi: {Path}, Kullanici: {User}",
                path, User.Identity?.Name);
            return NotFound("Dosya bulunamadı.");
        }

        var fileName = Path.GetFileName(path);
        // .enc uzantısını kaldır
        if (fileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            fileName = fileName[..^4];

        var mimeType = GetMimeType(Path.GetExtension(fileName));

        _logger.LogInformation("Dosya API uzerinden indirildi: {Path}, Kullanici: {User}, Boyut: {Size}",
            path, User.Identity?.Name, content.Length);

        return File(content, mimeType, fileName);
    }

    /// <summary>
    /// Dosya yolunu doğrular ve dosyanın var olduğunu teyit eder.
    /// İstemci tarafı indirmeden önce ön kontrol için.
    /// </summary>
    [HttpGet("preview")]
    public async Task<IActionResult> PreviewAsync([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest("Dosya yolu belirtilmedi.");

        var content = await _secureFileService.ReadDecryptedAsync(path);
        if (content == null)
            return NotFound("Dosya bulunamadı.");

        var fileName = Path.GetFileName(path);
        if (fileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            fileName = fileName[..^4];

        return Ok(new
        {
            fileName,
            size = content.Length,
            mimeType = GetMimeType(Path.GetExtension(fileName))
        });
    }

    private static string GetMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",
            _ => "application/octet-stream"
        };
    }
}




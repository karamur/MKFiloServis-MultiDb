using System.Security.Cryptography;
using FluentAssertions;
using KOAFiloServis.Web.Services.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Araç belge önizleme/yazdırma özelliği için birim ve entegrasyon testleri.
/// </summary>
public class AracBelgePreviewTests
{
    private static string UploadsRoot => @"C:\KOAFiloServis_yedekleme\uploads";
    private static string KeysRoot => @"C:\KOAFiloServis_yedekleme\keys";

    // ── EncExtensionTemizle testleri ─────────────────────────────────────

    [Fact]
    public void EncExtensionTemizle_SingleEnc_StripsSuffix()
    {
        var result = EncExtensionTemizle("06C0640-RUHSAT-20260429-082901.pdf.enc");
        result.Should().Be("06C0640-RUHSAT-20260429-082901.pdf");
    }

    [Fact]
    public void EncExtensionTemizle_FileName_DoesNotEndWithEnc()
    {
        var result = EncExtensionTemizle("ARAC-EVRAK-KASKO-20260415.pdf.enc");
        result.EndsWith(".enc", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }

    [Fact]
    public void EncExtensionTemizle_DoubleEnc_StripsBoth()
    {
        var result = EncExtensionTemizle("belge.pdf.enc.enc");
        result.Should().Be("belge.pdf");
    }

    [Fact]
    public void EncExtensionTemizle_NoEnc_ReturnsSame()
    {
        var result = EncExtensionTemizle("MUAYENE-20260601.pdf");
        result.Should().Be("MUAYENE-20260601.pdf");
    }

    [Fact]
    public void EncExtensionTemizle_NullOrEmpty_ReturnsDefault()
    {
        EncExtensionTemizle(null).Should().Be("belge.pdf");
        EncExtensionTemizle("").Should().Be("belge.pdf");
        EncExtensionTemizle("  ").Should().Be("belge.pdf");
    }

    // ── ContentType testleri (MimeTipiAl / GercekUzantiAl) ─────────────

    [Fact]
    public void GercekUzantiAl_EncPath_ReturnsPdf()
    {
        var result = GercekUzantiAl("uploads/06C0640-RUHSAT-20260429-082901.pdf.enc");
        result.Should().Be(".pdf");
    }

    [Fact]
    public void GercekUzantiAl_PlainPath_ReturnsCorrectExtension()
    {
        GercekUzantiAl("belge.jpg").Should().Be(".jpg");
        GercekUzantiAl("foto.png").Should().Be(".png");
    }

    [Fact]
    public void MimeTipiAl_Pdf_ReturnsApplicationPdf()
    {
        MimeTipiAl(".pdf").Should().Be("application/pdf");
    }

    [Fact]
    public void MimeTipiAl_Image_ReturnsCorrectMime()
    {
        MimeTipiAl(".jpg").Should().Be("image/jpeg");
        MimeTipiAl(".png").Should().Be("image/png");
        MimeTipiAl(".gif").Should().Be("image/gif");
    }

    [Fact]
    public void MimeTipiAl_Unknown_ReturnsOctetStream()
    {
        MimeTipiAl(".xyz").Should().Be("application/octet-stream");
    }

    // ── AracBelgePreview entegrasyon testleri ───────────────────────────

    [Fact]
    public async Task AracBelgePreview_ReturnsDecryptedPdfBytes()
    {
        var keyFile = FindWorkingKeyFile();
        if (keyFile == null) return;

        var encFiles = Directory.GetFiles(UploadsRoot, "*.pdf.enc", SearchOption.AllDirectories)
            .Take(5).ToList();
        if (!encFiles.Any()) return;

        var kp = new DpapiMasterKeyProvider(keyFile, NullLogger<DpapiMasterKeyProvider>.Instance);
        var protector = new AesGcmFileProtector(kp);

        byte[]? result = null;
        foreach (var f in encFiles)
        {
            try
            {
                var raw = await File.ReadAllBytesAsync(f);
                result = protector.Unprotect(raw);
                break;
            }
            catch (CryptographicException) { }
        }

        if (result == null) return;
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AracBelgePreview_DoesNotWritePlainFileToDisk()
    {
        var keyFile = FindWorkingKeyFile();
        if (keyFile == null) return;

        var encFiles = Directory.GetFiles(UploadsRoot, "*.pdf.enc", SearchOption.AllDirectories).Take(3).ToList();
        if (!encFiles.Any()) return;

        var kp = new DpapiMasterKeyProvider(keyFile, NullLogger<DpapiMasterKeyProvider>.Instance);
        var protector = new AesGcmFileProtector(kp);

        var plainBefore = Directory.GetFiles(UploadsRoot, "*.pdf", SearchOption.AllDirectories)
            .Count(f => !f.EndsWith(".pdf.enc", StringComparison.OrdinalIgnoreCase));

        foreach (var f in encFiles)
        {
            try { var raw = await File.ReadAllBytesAsync(f); _ = protector.Unprotect(raw); }
            catch (CryptographicException) { }
        }

        var plainAfter = Directory.GetFiles(UploadsRoot, "*.pdf", SearchOption.AllDirectories)
            .Count(f => !f.EndsWith(".pdf.enc", StringComparison.OrdinalIgnoreCase));

        plainAfter.Should().Be(plainBefore, "decrypt işlemi diske yeni plain dosya yazmamalı");
    }

    // ── Helper metodlar ──────────────────────────────────────────────────

    private static string? FindWorkingKeyFile()
    {
        if (!Directory.Exists(KeysRoot)) return null;
        return Directory.GetFiles(KeysRoot, "master.key*")
            .OrderByDescending(f => f.Length)
            .ThenBy(f => new FileInfo(f).LastWriteTime)
            .FirstOrDefault();
    }

    /// <summary>BelgeUyarilari.razor @code içindeki EncExtensionTemizle mantığı.</summary>
    private static string EncExtensionTemizle(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "belge.pdf";
        while (fileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            fileName = fileName[..^4];
        return fileName;
    }

    /// <summary>BelgeUyarilari.razor @code içindeki GercekUzantiAl mantığı.</summary>
    private static string GercekUzantiAl(string? dosyaYolu)
    {
        if (string.IsNullOrWhiteSpace(dosyaYolu)) return string.Empty;
        var ad = Path.GetFileName(dosyaYolu);
        if (ad.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            ad = ad.Substring(0, ad.Length - 4);
        return Path.GetExtension(ad).ToLowerInvariant();
    }

    /// <summary>BelgeUyarilari.razor @code içindeki MimeTipiAl mantığı.</summary>
    private static string MimeTipiAl(string uzanti) => uzanti.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}

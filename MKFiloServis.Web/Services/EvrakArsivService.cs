using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Security;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Evrak arşivleme servisi implementasyonu.
/// Yüklenen her evrak için iki kopya oluşturur:
/// 1) <c>Arsiv\Sifreli\...</c> — AES-256-GCM (KOA1) ile şifreli, uygulama tarafından açılabilir
/// 2) <c>Arsiv\Sifresiz\...</c> — Plain (orijinal içerik), wwwroot dışında
///
/// Klasör/dosya adlandırması:
///   Personel: {AD-SOYAD}-{EVRAK_NITELIGI}-{yyyyMMdd-HHmmss}
///   Araç:     {PLAKA}-{SASI_NO}-{EVRAK_NITELIGI}-{yyyyMMdd-HHmmss}
/// </summary>
public sealed class EvrakArsivService : IEvrakArsivService
{
    private readonly IFileProtector _fileProtector;
    private readonly string _arsivRoot;
    private readonly ILogger<EvrakArsivService> _logger;

    public EvrakArsivService(
        IFileProtector fileProtector,
        IWebHostEnvironment environment,
        ILogger<EvrakArsivService> logger)
    {
        _fileProtector = fileProtector ?? throw new ArgumentNullException(nameof(fileProtector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var storageRoot = AppStoragePaths.GetStorageRoot(
            environment?.ContentRootPath ?? throw new ArgumentNullException(nameof(environment)));
        _arsivRoot = Path.Combine(storageRoot, "Arsiv");
    }

    /// <inheritdoc />
    public async Task ArsivlePersonelEvrakAsync(
        string adSoyad,
        string evrakNiteligi,
        byte[] icerik,
        string uzanti,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(icerik);

        var evrakNiteligiNorm = FileNameHelper.NormalizeFileName(evrakNiteligi, fallback: "EVRAK");
        var adSoyadNorm = FileNameHelper.NormalizeFileName(adSoyad, fallback: "PERSONEL");
        var tarihSaat = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        var dizinAdi = $"{adSoyadNorm}-{evrakNiteligiNorm}-{tarihSaat}";
        var dosyaAdiBase = dizinAdi; // aynı format

        await ArsivleAsync("Personeller", dizinAdi, dosyaAdiBase, icerik, uzanti, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ArsivleAracEvrakAsync(
        string plaka,
        string sasiNo,
        string evrakNiteligi,
        byte[] icerik,
        string uzanti,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(icerik);

        var evrakNiteligiNorm = FileNameHelper.NormalizeFileName(evrakNiteligi, fallback: "EVRAK");
        var plakaNorm = FileNameHelper.NormalizeFileName(plaka, fallback: "PLAKA");
        var sasiNoNorm = FileNameHelper.NormalizeFileName(sasiNo, fallback: "SASI");

        var tarihSaat = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var dizinAdi = $"{plakaNorm}-{sasiNoNorm}-{evrakNiteligiNorm}-{tarihSaat}";
        var dosyaAdiBase = dizinAdi; // aynı format

        await ArsivleAsync("Araclar", dizinAdi, dosyaAdiBase, icerik, uzanti, cancellationToken);
    }

    private async Task ArsivleAsync(
        string kategori,
        string dizinAdi,
        string dosyaAdiBase,
        byte[] icerik,
        string uzanti,
        CancellationToken cancellationToken)
    {
        var ext = FileNameHelper.NormalizeExtension(uzanti);

        // 1) Şifreli kopya — KOA1 formatında, uygulamanın ReadDecryptedAsync ile açabileceği
        var sifreliDir = Path.Combine(_arsivRoot, "Sifreli", kategori, dizinAdi);
        Directory.CreateDirectory(sifreliDir);
        var encrypted = _fileProtector.Protect(icerik); // KOA1 + VER + NONCE + TAG + CIPHER
        var sifreliPath = Path.Combine(sifreliDir, $"{dosyaAdiBase}{ext}.enc");
        await File.WriteAllBytesAsync(sifreliPath, encrypted, cancellationToken);
        _logger.LogDebug("Arsiv sifreli (KOA1): {Path}", sifreliPath);

        // 2) Şifresiz kopya — wwwroot dışında
        var sifresizDir = Path.Combine(_arsivRoot, "Sifresiz", kategori, dizinAdi);
        Directory.CreateDirectory(sifresizDir);
        var sifresizPath = Path.Combine(sifresizDir, $"{dosyaAdiBase}{ext}");
        await File.WriteAllBytesAsync(sifresizPath, icerik, cancellationToken);
        _logger.LogDebug("Arsiv sifresiz: {Path}", sifresizPath);
    }
}




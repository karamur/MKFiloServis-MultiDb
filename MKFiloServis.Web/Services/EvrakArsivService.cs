using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Security;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Evrak arşivleme servisi implementasyonu.
/// Yüklenen her evrak için yalnızca tek kopya oluşturur:
/// <c>Arsiv\Sifreli\...</c> — AES-256-GCM (KOA1) ile şifreli, uygulama tarafından açılabilir.
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
    public async Task<string> ArsivlePersonelEvrakAsync(
        string adSoyad,
        string firmaAdi,
        string evrakNiteligi,
        byte[] icerik,
        string uzanti,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(icerik);

        var normalizedAdSoyad = AppStoragePaths.NormalizeFolderName(adSoyad);
        var splitIndex = normalizedAdSoyad.LastIndexOf(' ');
        var ad = splitIndex > 0 ? normalizedAdSoyad[..splitIndex] : normalizedAdSoyad;
        var soyad = splitIndex > 0 ? normalizedAdSoyad[(splitIndex + 1)..] : string.Empty;
        var klasor = AppStoragePaths.BuildPersonelArsivKlasoru(ad, soyad, firmaAdi);
        var dosyaAdiBase = BuildTekilDosyaAdi(evrakNiteligi);

        return await ArsivleAsync("Personeller", klasor, dosyaAdiBase, icerik, uzanti, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ArsivleAracEvrakAsync(
        string plaka,
        string firmaAdi,
        string evrakNiteligi,
        byte[] icerik,
        string uzanti,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(icerik);

        var klasor = AppStoragePaths.BuildAracArsivKlasoru(plaka, firmaAdi);
        var dosyaAdiBase = BuildTekilDosyaAdi(evrakNiteligi);

        return await ArsivleAsync("Araclar", klasor, dosyaAdiBase, icerik, uzanti, cancellationToken);
    }

    private async Task<string> ArsivleAsync(
        string kategori,
        string klasor,
        string dosyaAdiBase,
        byte[] icerik,
        string uzanti,
        CancellationToken cancellationToken)
    {
        var ext = FileNameHelper.NormalizeExtension(uzanti);

        var sifreliDir = Path.Combine(_arsivRoot, "Sifreli", kategori, klasor);
        Directory.CreateDirectory(sifreliDir);
        var encrypted = _fileProtector.Protect(icerik);
        var sifreliFileName = $"{dosyaAdiBase}{ext}.enc";
        var sifreliPath = Path.Combine(sifreliDir, sifreliFileName);
        await File.WriteAllBytesAsync(sifreliPath, encrypted, cancellationToken);
        _logger.LogDebug("Arsiv sifreli (KOA1): {Path}", sifreliPath);

        return Path.Combine("Arsiv", "Sifreli", kategori, klasor, sifreliFileName).Replace('\\', '/');
    }

    private static string BuildTekilDosyaAdi(string evrakNiteligi)
    {
        var normalized = AppStoragePaths.NormalizeFolderName(evrakNiteligi ?? "EVRAK");
        return normalized.Replace(" ", "").Replace("-", "");
    }
}




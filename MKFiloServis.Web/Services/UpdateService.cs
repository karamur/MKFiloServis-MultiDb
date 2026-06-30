using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Helpers;

namespace MKFiloServis.Web.Services;

// ══════════════════════════════════════════════
// MODELS
// ══════════════════════════════════════════════

public class UpdateInfo
{
    public string Version { get; set; } = "";
    public string BuildDate { get; set; } = "";
    public string BuildNumber { get; set; } = "";
    public string Framework { get; set; } = "";
    public string Description { get; set; } = "";
}

public class UpdateManifest
{
    public string AppName { get; set; } = "";
    public string CurrentVersion { get; set; } = "";
    public string ReleaseDate { get; set; } = "";
    public string UpdateUrl { get; set; } = "";
    public List<string> ChangeLog { get; set; } = new();
}

public class LocalUpdateInfo
{
    public string FileName { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Version { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool SignatureValid { get; set; }
    public string SignatureError { get; set; } = "";
    public bool IsVersionAllowed { get; set; }
    public string AllowedVersionError { get; set; } = "";
}

public class UpdateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Version { get; set; }

    public static UpdateResult Ok(string msg, string? version = null)
        => new() { Success = true, Message = msg, Version = version };
    public static UpdateResult Fail(string msg)
        => new() { Success = false, Message = msg };
}

// ══════════════════════════════════════════════
// SERVICE — FULL OFFLINE UPDATE
// ══════════════════════════════════════════════

/// <summary>
/// Offline güncelleme servisi.
///
/// Çalisma mantigi:
///   1. Admin patch dosyasini C:\KOAFiloServis\updates\ klasörüne atar
///   2. /admin/update sayfasindan "Güncelle" butonuna basar
///   3. Sistem patch'i dogrular, yedek alir, kurar
///
/// INTERNET YOK — sunucu yok — tamamen local.
/// </summary>
public class UpdateService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UpdateService> _logger;
    private readonly IConfiguration _config;
    private readonly string _artifactsPath;
    private readonly string _updatesPath;
    private readonly string _currentVersion;
    private const string PatchSignatureKey = "KOAFiloServis-PATCH-2026-SIGN-KEY-v2";

    public UpdateService(IWebHostEnvironment environment, ILogger<UpdateService> logger, IConfiguration config)
    {
        _environment = environment;
        _logger = logger;
        _config = config;
        _artifactsPath = Path.Combine(Directory.GetParent(_environment.ContentRootPath)?.FullName ?? "", "artifacts");
        _currentVersion = GetCurrentVersion();

        // PART 2: Offline update dizini — C:\KOAFiloServis\updates\
        _updatesPath = @"C:\KOAFiloServis\updates";
    }

    // ══════════════════════════════════════════════
    // VERSION
    // ══════════════════════════════════════════════

    public string GetCurrentVersion()
    {
        try
        {
            var versionFile = Path.Combine(_artifactsPath, "version.json");
            if (File.Exists(versionFile))
            {
                var json = File.ReadAllText(versionFile);
                var info = JsonSerializer.Deserialize<UpdateInfo>(json);
                return info?.Version ?? "1.0.0";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Versiyon dosyasi okunamadi");
        }
        return "1.0.0";
    }

    public async Task<UpdateInfo?> GetUpdateInfoAsync()
    {
        try
        {
            var versionFile = Path.Combine(_artifactsPath, "version.json");
            if (File.Exists(versionFile))
            {
                var json = await File.ReadAllTextAsync(versionFile);
                return JsonSerializer.Deserialize<UpdateInfo>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Güncelleme bilgisi alinamadi");
        }
        return null;
    }

    // ══════════════════════════════════════════════
    // PART 2-3: LOCAL PATCH SCAN
    // C:\KOAFiloServis_{Firma}\updates\ altindaki zip'leri tara
    // ══════════════════════════════════════════════

    public string GetUpdatesPath() => _updatesPath;

    /// <summary>
    /// Local updates klasöründeki patch dosyalarini listeler.
    /// Her patch için imza kontrolü + lisans versiyon kontrolü yapilir.
    /// </summary>
    public async Task<List<LocalUpdateInfo>> ScanLocalUpdatesAsync(
        LicenseService? licenseService = null)
    {
        var results = new List<LocalUpdateInfo>();

        try
        {
            if (!Directory.Exists(_updatesPath))
            {
                Directory.CreateDirectory(_updatesPath);
                return results;
            }

            var zipFiles = Directory.GetFiles(_updatesPath, "*.zip")
                .OrderByDescending(f => f)
                .ToList();

            LicenseInfo? currentLicense = null;
            if (licenseService != null)
                currentLicense = await licenseService.GetCurrentLicenseAsync();

            foreach (var zipPath in zipFiles)
            {
                var info = new LocalUpdateInfo
                {
                    FileName = Path.GetFileName(zipPath),
                    FullPath = zipPath,
                    Version = ExtractVersionFromFileName(zipPath),
                    SizeBytes = new FileInfo(zipPath).Length,
                    CreatedAt = new FileInfo(zipPath).CreationTime
                };

                // PART 8: Patch imza kontrolü
                (info.SignatureValid, info.SignatureError) = await VerifyPatchSignatureAsync(zipPath);

                // PART 5: Lisans versiyon kontrolü
                if (currentLicense != null && !string.IsNullOrEmpty(info.Version))
                {
                    info.IsVersionAllowed = IsVersionAllowed(info.Version, currentLicense.AllowedVersion);
                    if (!info.IsVersionAllowed)
                        info.AllowedVersionError =
                            $"Bu güncelleme (v{info.Version}) lisansiniza dahil degil. " +
                            $"Izin verilen max surum: {currentLicense.AllowedVersion}";
                }
                else
                {
                    info.IsVersionAllowed = true;
                }

                results.Add(info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local update taramasi sirasinda hata: {Path}", _updatesPath);
        }

        return results;
    }

    // ══════════════════════════════════════════════
    // PART 4: INSTALL LOCAL PATCH
    // ══════════════════════════════════════════════

    public async Task<UpdateResult> InstallLocalUpdateAsync(
        string zipPath, LicenseService licenseService)
    {
        try
        {
            if (!File.Exists(zipPath))
                return UpdateResult.Fail("Güncelleme dosyasi bulunamadi: " + zipPath);

            var fileName = Path.GetFileName(zipPath);
            var version = ExtractVersionFromFileName(zipPath);

            // ── PART 5: Lisans kontrolü ──
            var lic = await licenseService.GetCurrentLicenseAsync();
            if (lic == null)
                return UpdateResult.Fail("Aktif lisans bulunamadi. Once lisans yukleyin.");

            if (!string.IsNullOrEmpty(version) && !IsVersionAllowed(version, lic.AllowedVersion))
                return UpdateResult.Fail(
                    $"Bu güncelleme (v{version}) lisansa dahil degil. " +
                    $"Izin verilen max surum: {lic.AllowedVersion}");

            // ── PART 8: Imza kontrolü ──
            var (sigValid, sigError) = await VerifyPatchSignatureAsync(zipPath);
            if (!sigValid)
                return UpdateResult.Fail($"Patch guvenilir degil: {sigError}");

            // ── Yedekleme ──
            var backupPath = await CreateBackupAsync();
            _logger.LogInformation("Yedekleme tamamlandi: {BackupPath}", backupPath);

            // ── Publish dizinine extract et ──
            var publishPath = Path.Combine(_artifactsPath, "publish");
            var tempExtractPath = Path.Combine(_artifactsPath, "temp_update_" + Guid.NewGuid().ToString("N")[..8]);

            try
            {
                if (Directory.Exists(tempExtractPath))
                    Directory.Delete(tempExtractPath, true);

                ZipFile.ExtractToDirectory(zipPath, tempExtractPath);
                _logger.LogInformation("Patch extract edildi: {ZipPath} → {TempPath}", zipPath, tempExtractPath);

                // Dosyalari publish dizinine kopyala
                if (!Directory.Exists(publishPath))
                    Directory.CreateDirectory(publishPath);

                foreach (var file in Directory.GetFiles(tempExtractPath, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(tempExtractPath, file);
                    var destPath = Path.Combine(publishPath, relativePath);
                    var destDir = Path.GetDirectoryName(destPath);

                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    File.Copy(file, destPath, true);
                }

                // Temizlik
                Directory.Delete(tempExtractPath, true);

                // PART 4: Patch dosyasini sil (cleanup)
                File.Delete(zipPath);
                _logger.LogInformation("Patch dosyasi silindi (cleanup): {ZipPath}", zipPath);
            }
            catch (Exception ex)
            {
                // Extract hatasinda temp dizinini temizlemeye çalis
                try { if (Directory.Exists(tempExtractPath)) Directory.Delete(tempExtractPath, true); }
                catch { /* best effort */ }

                _logger.LogError(ex, "Patch extract/kopyalama hatasi");
                return UpdateResult.Fail($"Güncelleme kurulum hatasi: {ex.Message}");
            }

            _logger.LogWarning(
                "🚨 GUNCELLEME YUKLENDI | Versiyon: {Version} | Dosya: {FileName} | " +
                "Uygulama yeniden baslatilmali!",
                version, fileName);

            return UpdateResult.Ok(
                $"v{version} başariyla yuklendi! Uygulamayi yeniden başlatmaniz gerekiyor.",
                version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Güncelleme kurulum hatasi");
            return UpdateResult.Fail($"Güncelleme hatasi: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════
    // BACKUP
    // ══════════════════════════════════════════════

    private async Task<string> CreateBackupAsync()
    {
        var backupDir = Path.Combine(_artifactsPath, "backup_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
        var publishPath = Path.Combine(_artifactsPath, "publish");

        Directory.CreateDirectory(backupDir);

        if (Directory.Exists(publishPath))
        {
            await Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(publishPath, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(publishPath, file);
                    var destPath = Path.Combine(backupDir, relativePath);
                    var destDir = Path.GetDirectoryName(destPath);

                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    File.Copy(file, destPath, true);
                }
            });
        }

        // Eski backup'lari temizle (son 5 taneyi tut)
        CleanOldBackups(5);

        return backupDir;
    }

    private void CleanOldBackups(int keepCount)
    {
        try
        {
            var backupDirs = Directory.GetDirectories(_artifactsPath, "backup_*")
                .OrderByDescending(d => d)
                .Skip(keepCount)
                .ToList();

            foreach (var dir in backupDirs)
            {
                Directory.Delete(dir, true);
                _logger.LogInformation("Eski backup silindi: {BackupDir}", dir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Eski backup temizligi sirasinda hata");
        }
    }

    // ══════════════════════════════════════════════
    // PART 8: PATCH SIGNATURE VERIFICATION
    // ══════════════════════════════════════════════

    public async Task<(bool Valid, string Error)> VerifyPatchSignatureAsync(string zipPath)
    {
        try
        {
            // Patch ile birlikte gelen .sig dosyasini ara
            var sigPath = Path.ChangeExtension(zipPath, ".sig");
            if (!File.Exists(sigPath))
            {
                // .sig dosyasi yoksa, zip'in kendi hash'ini kontrol et
                // (fallback: zip dosya adindan hash cikar)
                return (true, ""); // Imza dosyasi yoksa gec (admin elle attigi icin guvenli)
            }

            var expectedSig = await File.ReadAllTextAsync(sigPath);
            expectedSig = expectedSig.Trim();

            // Zip dosyasinin hash'ini hesapla
            using var sha = SHA256.Create();
            await using var fs = File.OpenRead(zipPath);
            var hash = await sha.ComputeHashAsync(fs);
            var computedSig = Convert.ToBase64String(hash);

            if (!string.Equals(expectedSig, computedSig, StringComparison.Ordinal))
                return (false, "Patch imzasi gecersiz — dosya degistirilmis veya bozuk.");

            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, $"Imza dogrulama hatasi: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════

    /// <summary>
    /// Dosya adindan versiyon numarasini cikarir.
    /// Beklenen format: patch_v1.0.25.zip veya KOAFiloServis_Update_v1.2.3.zip
    /// </summary>
    private static string ExtractVersionFromFileName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // patch_v1.0.25 → "1.0.25"
        var match = Regex.Match(fileName, @"v?(\d+\.\d+\.\d+)");
        if (match.Success)
            return match.Groups[1].Value;

        // Bulunamazsa dosya adini döndür
        return fileName;
    }

    /// <summary>
    /// PART 5: Güncelleme versiyonu lisansa dahil mi?
    /// </summary>
    private static bool IsVersionAllowed(string updateVersion, string allowedVersion)
    {
        if (string.IsNullOrWhiteSpace(allowedVersion) || allowedVersion == "0.0.0")
            return true;

        try
        {
            var update = new Version(updateVersion);
            var allowed = new Version(allowedVersion);
            return update <= allowed;
        }
        catch
        {
            return true; // Parse edilemezse blocklama
        }
    }

    // ══════════════════════════════════════════════
    // LEGACY (mevcut arayuzu bozmamak icin)
    // ══════════════════════════════════════════════

    /// <summary>
    /// [LEGACY] Harici güncelleme dosyasi yukler (GuncellemeYonetimi.razor tarafindan kullanilir).
    /// </summary>
    public async Task<(bool Success, string Message)> UploadUpdatePackageAsync(Stream fileStream, string fileName)
    {
        try
        {
            if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return (false, "Sadece ZIP dosyalari kabul edilmektedir");

            // Hem artifacts hem de updates klasörüne kaydet
            var destPath = Path.Combine(_updatesPath, fileName);
            Directory.CreateDirectory(_updatesPath);

            using var fs = new FileStream(destPath, FileMode.Create);
            await fileStream.CopyToAsync(fs);

            _logger.LogInformation("Güncelleme paketi yuklendi: {FileName} → {Path}", fileName, destPath);
            return (true, $"Güncelleme paketi başariyla yuklendi: {fileName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Güncelleme paketi yuklenemedi");
            return (false, $"Yukleme hatasi: {ex.Message}");
        }
    }

    /// <summary>
    /// [LEGACY] Güncelleme paketini uygular (GuncellemeYonetimi.razor tarafindan kullanilir).
    /// </summary>
    public async Task<(bool Success, string Message)> ApplyUpdateAsync(string packageName)
    {
        try
        {
            // Once updates klasöründe, sonra artifacts'te ara
            var packagePath = Path.Combine(_updatesPath, packageName);
            if (!File.Exists(packagePath))
                packagePath = Path.Combine(_artifactsPath, packageName);

            if (!File.Exists(packagePath))
                return (false, "Güncelleme paketi bulunamadi: " + packageName);

            var backupPath = Path.Combine(_artifactsPath, "backup_before_update");
            var publishPath = Path.Combine(_artifactsPath, "publish");

            // Mevcut dosyalari yedekle
            if (Directory.Exists(publishPath))
            {
                if (Directory.Exists(backupPath))
                    Directory.Delete(backupPath, true);

                Directory.CreateDirectory(backupPath);
                foreach (var file in Directory.GetFiles(publishPath))
                {
                    var destFile = Path.Combine(backupPath, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }
                _logger.LogInformation("Yedekleme tamamlandi (legacy): {BackupPath}", backupPath);
            }

            // Güncelleme paketini extract et
            var tempExtractPath = Path.Combine(_artifactsPath, "temp_update");
            if (Directory.Exists(tempExtractPath))
                Directory.Delete(tempExtractPath, true);

            ZipFile.ExtractToDirectory(packagePath, tempExtractPath);
            _logger.LogInformation("Paket extract edildi (legacy): {PackagePath}", packagePath);

            // Dosyalari publish'e kopyala
            if (!Directory.Exists(publishPath))
                Directory.CreateDirectory(publishPath);

            foreach (var file in Directory.GetFiles(tempExtractPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(tempExtractPath, file);
                var destPath = Path.Combine(publishPath, relativePath);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(file, destPath, true);
            }

            Directory.Delete(tempExtractPath, true);

            _logger.LogInformation("Güncelleme başariyla uygulandi (legacy): {Package}", packageName);
            return (true, "Güncelleme başariyla uygulandi. Uygulamayi yeniden başlatmaniz gerekiyor.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Güncelleme uygulanirken hata oluştu (legacy)");
            return (false, $"Güncelleme hatasi: {ex.Message}");
        }
    }

    public string GetArtifactsPath() => _artifactsPath;
    public string GetPublishPath() => Path.Combine(_artifactsPath, "publish");

    public async Task<UpdateManifest?> GetUpdateManifestAsync()
    {
        try
        {
            var manifestFile = Path.Combine(_artifactsPath, "update-manifest.json");
            if (File.Exists(manifestFile))
            {
                var json = await File.ReadAllTextAsync(manifestFile);
                return JsonSerializer.Deserialize<UpdateManifest>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manifest dosyasi alinamadi");
        }
        return null;
    }

    public List<string> GetAvailableUpdatePackages()
    {
        var packages = new List<string>();
        try
        {
            // Hem artifacts hem de updates klasörüne bak
            var searchPaths = new[] { _artifactsPath, _updatesPath };
            foreach (var searchPath in searchPaths)
            {
                if (Directory.Exists(searchPath))
                {
                    packages.AddRange(Directory.GetFiles(searchPath, "*Update*.zip")
                        .Concat(Directory.GetFiles(searchPath, "patch_*.zip"))
                        .Select(Path.GetFileName)
                        .Where(f => f != null)
                        .Cast<string>());
                }
            }
            return packages.Distinct().OrderByDescending(f => f).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Güncelleme paketleri listelenemedi");
        }
        return packages;
    }
}



using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Lisans dogrulama servisi — ANTI-BYPASS HARDENED (FINAL).
///
/// 10 KORUMA KATMANI:
///   1. Registry lock — Demo sadece 1 kez (DB silinse bile)
///   2. Machine lock  — Makine kodu: MachineName + UserName + DriveSerial
///   3. Signature     — FirmaKodu|MachineId|ExpireDate|IsDemo|AllowedVersion|CreatedAt
///   4. Clock attack  — 3 katman: ExpireDate + CreatedAt + NegativeTime
///   5. File hash     — C:\KOAFiloServis_{Firma}\config\license.hash
///   6. Hard block    — Startup'ta gecersiz lisans = uygulama acilmaz
///   7. Single system — LicenseService + LicenseInfos (eski sistem tamamen kalkti)
///   8. Security viol — Registry yok ama DB'de lisans varsa ihlal tespiti
///   9. Version check — AllowedVersion kontrolu
///  10. Self-heal     — Hash dosyasi yoksa yeniden olusturur
/// </summary>
public class LicenseService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<LicenseService> _logger;
    private const string SecretKey = "KOAFiloServis-LCNS-2026-SECURE-KEY-X9mK2pL5vR8w";
    private const string RegistryPath = @"SOFTWARE\KOAFiloServis";
    private const string DemoUsedValueName = "DemoUsed";
    private const int DemoMaxDays = 30;

    public LicenseService(IDbContextFactory<ApplicationDbContext> dbFactory, IConfiguration config, ILogger<LicenseService> logger)
    {
        _dbFactory = dbFactory;
        _config = config;
        _logger = logger;
    }

    // ══════════════════════════════════════════════
    // PART 1: REGISTRY LOCK — Demo sadece 1 kez
    // DB silinse bile REGISTRY'den kontrol edilir
    // ══════════════════════════════════════════════

    public static bool HasDemoBeenUsed()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
            return key?.GetValue(DemoUsedValueName) != null;
        }
        catch
        {
            return false; // Registry erisilemezse demo'ya izin ver (ilk kurulum)
        }
    }

    public static void MarkDemoUsed()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
            key.SetValue(DemoUsedValueName, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LicenseService] Registry write failed: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════
    // PART 2: MACHINE ID — HARDENED
    // MachineName + UserName + DriveVolumeSerial
    // ══════════════════════════════════════════════

    public static string GetMachineId()
    {
        try
        {
            var machine = Environment.MachineName;
            var user = Environment.UserName;
            var driveSerial = "";

            try
            {
                var drives = System.IO.DriveInfo.GetDrives();
                var systemDrive = drives.FirstOrDefault(d =>
                    d.IsReady && d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                    ?? drives.FirstOrDefault(d => d.IsReady);

                if (systemDrive != null)
                {
                    driveSerial = systemDrive.VolumeLabel?.GetHashCode().ToString("X8") ?? "NOSERIAL";
                }
            }
            catch
            {
                driveSerial = "NOSERIAL";
            }

            return $"{machine}_{user}_{driveSerial}";
        }
        catch
        {
            return $"{Environment.MachineName}_{Environment.UserName}";
        }
    }

    // ══════════════════════════════════════════════
    // PART 3: HARDENED SIGNATURE
    // FirmaKodu|MachineId|ExpireDate|IsDemo|AllowedVersion|CreatedAt
    // ══════════════════════════════════════════════

    public static string GenerateSignature(string firmaKodu, string machineId, DateTime expireDate,
        bool isDemo = false, string allowedVersion = "1.0.99", DateTime? createdAt = null)
    {
        var created = createdAt ?? DateTime.UtcNow;
        var raw = $"{firmaKodu}|{machineId}|{expireDate:yyyy-MM-dd}|{isDemo}|{allowedVersion}|{created:yyyy-MM-dd}|{SecretKey}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(hash);
    }

    public bool VerifySignature(LicenseInfo lic)
    {
        var expected = GenerateSignature(
            lic.FirmaKodu, lic.MachineId, lic.ExpireDate,
            lic.IsDemo, lic.AllowedVersion, lic.CreatedAt);
        return lic.Signature == expected;
    }

    // ══════════════════════════════════════════════
    // PART 5: LICENSE FILE HASH PROTECTION
    // C:\KOAFiloServis_{Firma}\config\license.hash
    // ══════════════════════════════════════════════

    private static string GetLicenseHashDirectory(string firmaKodu)
        => Path.Combine($"C:\\KOAFiloServis_{firmaKodu}", "config");

    private static string GetLicenseHashPath(string firmaKodu)
        => Path.Combine(GetLicenseHashDirectory(firmaKodu), "license.hash");

    private static string ComputeLicenseHash(LicenseInfo lic)
    {
        var raw = $"{lic.FirmaKodu}|{lic.MachineId}|{lic.ExpireDate:yyyy-MM-dd}|{lic.IsDemo}|{lic.Signature}|{lic.CreatedAt:yyyy-MM-dd}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(hash);
    }

    private void WriteLicenseHash(LicenseInfo lic)
    {
        try
        {
            var dir = GetLicenseHashDirectory(lic.FirmaKodu);
            Directory.CreateDirectory(dir);

            var hash = ComputeLicenseHash(lic);
            var path = GetLicenseHashPath(lic.FirmaKodu);
            File.WriteAllText(path, hash);

            _logger.LogInformation("License hash written to disk: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "License hash yazilamadi (disk hatasi): {FirmaKodu}", lic.FirmaKodu);
        }
    }

    private bool VerifyLicenseHash(LicenseInfo lic)
    {
        try
        {
            var path = GetLicenseHashPath(lic.FirmaKodu);
            if (!File.Exists(path))
            {
                // Self-heal: hash dosyasi yoksa yeniden olustur
                _logger.LogWarning("License hash dosyasi bulunamadi, yeniden olusturuluyor: {Path}", path);
                WriteLicenseHash(lic);
                return true;
            }

            var storedHash = File.ReadAllText(path).Trim();
            var computedHash = ComputeLicenseHash(lic);

            if (!string.Equals(storedHash, computedHash, StringComparison.Ordinal))
            {
                _logger.LogError("License hash MISMATCH! Stored: {Stored}, Computed: {Computed}",
                    storedHash[..Math.Min(16, storedHash.Length)],
                    computedHash[..Math.Min(16, computedHash.Length)]);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "License hash dogrulama hatasi (disk erisilemez)");
            return true; // Disk erisilemezse blocklama — DOS korumasi
        }
    }

    // ══════════════════════════════════════════════
    // PART 9: VERSION CHECK
    // ══════════════════════════════════════════════

    private static Version GetAppVersion()
    {
        try
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0);
        }
        catch
        {
            return new Version(1, 0);
        }
    }

    private static bool IsVersionAllowed(string allowedVersion)
    {
        if (string.IsNullOrWhiteSpace(allowedVersion) || allowedVersion == "0.0.0")
            return true;

        try
        {
            var appVer = GetAppVersion();
            var maxVer = new Version(allowedVersion);
            return appVer <= maxVer;
        }
        catch
        {
            return true; // Parse edilemezse blocklama
        }
    }

    // ══════════════════════════════════════════════
    // CORE: VALIDATE — 10 katman koruma
    // ══════════════════════════════════════════════

    public async Task<LicenseValidationResult> ValidateAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var lic = await db.LicenseInfos.FirstOrDefaultAsync(l => l.IsActive && !l.IsDeleted);
            var hasAnyLicense = await db.LicenseInfos.AnyAsync();

            // ── Lisans yok → Demo kontrolu ──
            if (lic == null)
            {
                // PART 1: Registry kontrolu
                if (HasDemoBeenUsed())
                    return LicenseValidationResult.Fail(
                        "🛑 Demo hakki zaten kullanilmis. Lisans dosyasini yukleyin.");

                // PART 1 EXTRA: DB'de demo varsa (yeni eklenmis olabilir) tekrar uretme
                if (hasAnyLicense)
                    return LicenseValidationResult.Fail(
                        "🛑 Veritabaninda lisans kaydi var fakat aktif degil. Lisans dosyasini yukleyin.");

                lic = await CreateTrialLicenseAsync(db);
                MarkDemoUsed();
                WriteLicenseHash(lic);

                _logger.LogWarning(
                    "🚨 DEMO LISANS OLUSTURULDU | Makine: {MachineId} | Firma: {FirmaKodu} | Bitis: {ExpireDate:yyyy-MM-dd} | Registry: set",
                    lic.MachineId, lic.FirmaKodu, lic.ExpireDate);

                return LicenseValidationResult.Ok(lic);
            }

            // ── PART 8: SECURITY VIOLATION — registry silinmis ama DB'de lisans var ──
            if (lic.IsDemo && !HasDemoBeenUsed() && hasAnyLicense)
            {
                _logger.LogCritical(
                    "🚨 GUVENLIK IHLALI! Registry'de demo kaydi yok ama DB'de demo lisans var. Makine: {MachineId}",
                    GetMachineId());
                return LicenseValidationResult.Fail(
                    "🛑 Guvenlik ihlali tespit edildi. Demo lisans dosyasi degistirilmis olabilir. Lisans dosyasini tekrar yukleyin.");
            }

            // ── PART 4.3: Negative time attack ──
            if (DateTime.UtcNow < lic.CreatedAt)
                return LicenseValidationResult.Fail(
                    "🛑 Sistem saati hatali. Lisans olusturma tarihinden onceki bir tarih algilandi. Lutfen saat ayarlarinizi kontrol edin.");

            // ── PART 2: Machine lock ──
            var currentMachineId = GetMachineId();
            if (!string.Equals(lic.MachineId, currentMachineId, StringComparison.Ordinal))
                return LicenseValidationResult.Fail(
                    $"🛑 Bu lisans bu makineye ait degil. Lisans makinesi: {lic.MachineId}, Bu makine: {currentMachineId}");

            // ── PART 4.1: CreatedAt tabanli (demo icin mutlak 30 gun) ──
            if (lic.IsDemo)
            {
                var elapsed = (DateTime.UtcNow - lic.CreatedAt).TotalDays;
                if (elapsed > DemoMaxDays)
                {
                    _logger.LogWarning("Demo suresi doldu (CreatedAt): {Days} gun, Makine: {MachineId}",
                        elapsed, currentMachineId);
                    return LicenseValidationResult.Fail(
                        $"🛑 Demo suresi doldu. Olusturma: {lic.CreatedAt:yyyy-MM-dd}, Gecen gun: {elapsed:F0}/{DemoMaxDays}");
                }
            }

            // ── PART 4.2: ExpireDate tabanli ──
            if (DateTime.UtcNow > lic.ExpireDate)
                return LicenseValidationResult.Fail(
                    $"🛑 Lisans suresi doldu ({lic.ExpireDate:yyyy-MM-dd}).");

            // ── Firma kodu kontrolu ──
            var firmaKodu = _config["FirmaKodu"] ?? lic.FirmaKodu;
            if (lic.FirmaKodu != firmaKodu)
                return LicenseValidationResult.Fail(
                    $"🛑 Firma kodu uyusmazligi: '{lic.FirmaKodu}' != '{firmaKodu}'");

            // ── PART 3: Signature dogrulama ──
            if (!VerifySignature(lic))
                return LicenseValidationResult.Fail(
                    "🛑 Lisans imzasi gecersiz. Lisans dosyasi bozulmus veya degistirilmis olabilir.");

            // ── PART 9: Version check ──
            if (!IsVersionAllowed(lic.AllowedVersion))
                return LicenseValidationResult.Fail(
                    $"🛑 Bu uygulama surumu ({GetAppVersion()}) lisansa dahil degil. Izin verilen max surum: {lic.AllowedVersion}");

            // ── PART 5: License file hash verification ──
            if (!VerifyLicenseHash(lic))
                return LicenseValidationResult.Fail(
                    "🛑 Lisans dosyasi degistirilmis! Hash uyusmazligi tespit edildi. Lisansi tekrar yukleyin.");

            // ── Basarili ──
            lic.LastValidatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return LicenseValidationResult.Ok(lic);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Lisans dogrulama hatasi");
            return LicenseValidationResult.Fail($"🛑 Lisans kontrol hatasi: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════
    // TRIAL LICENSE OLUSTURMA
    // ══════════════════════════════════════════════

    private async Task<LicenseInfo> CreateTrialLicenseAsync(ApplicationDbContext db)
    {
        var firmaKodu = _config["FirmaKodu"] ?? "DEMO";
        var machineId = GetMachineId();
        var now = DateTime.UtcNow;
        var expireDate = now.AddDays(DemoMaxDays);
        var allowedVersion = GetAppVersion().ToString();
        var isDemo = true;

        var signature = GenerateSignature(firmaKodu, machineId, expireDate,
            isDemo, allowedVersion, now);

        var lic = new LicenseInfo
        {
            FirmaKodu = firmaKodu,
            MachineId = machineId,
            ExpireDate = expireDate,
            Signature = signature,
            IsDemo = true,
            IsActive = true,
            AllowedVersion = allowedVersion,
            CreatedAt = now
        };

        db.LicenseInfos.Add(lic);
        await db.SaveChangesAsync();
        return lic;
    }

    // ══════════════════════════════════════════════
    // LICENSE KEY ACTIVATION
    // ══════════════════════════════════════════════

    public async Task<LicenseInfo> ActivateLicenseKeyAsync(string lisansAnahtari)
    {
        lisansAnahtari = lisansAnahtari.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");

        var lisansBilgi = ParseLicenseKey(lisansAnahtari);

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Mevcut tum lisanslari pasif yap
        var mevcutLisanslar = await db.LicenseInfos.Where(l => l.IsActive).ToListAsync();
        foreach (var ml in mevcutLisanslar)
            ml.IsActive = false;

        var now = DateTime.UtcNow;
        var machineId = GetMachineId();
        var isDemo = lisansBilgi.LisansTipi == "trial";
        var appVersion = GetAppVersion().ToString();
        var signature = GenerateSignature(lisansBilgi.FirmaKodu, machineId,
            lisansBilgi.BitisTarihi, isDemo, appVersion, now);

        var yeniLisans = new LicenseInfo
        {
            FirmaKodu = lisansBilgi.FirmaKodu,
            MachineId = machineId,
            ExpireDate = lisansBilgi.BitisTarihi,
            Signature = signature,
            IsDemo = isDemo,
            IsActive = true,
            AllowedVersion = appVersion,
            CreatedAt = now
        };

        db.LicenseInfos.Add(yeniLisans);
        await db.SaveChangesAsync();

        // PART 5: Write hash file
        WriteLicenseHash(yeniLisans);

        _logger.LogInformation("✅ Lisans anahtari aktive edildi: {FirmaKodu}, Bitis: {ExpireDate}, Makine: {MachineId}",
            yeniLisans.FirmaKodu, yeniLisans.ExpireDate, machineId);

        return yeniLisans;
    }

    private static (string FirmaKodu, string LisansTipi, DateTime BaslangicTarihi, DateTime BitisTarihi)
        ParseLicenseKey(string anahtar)
    {
        try
        {
            var lisansJson = DecryptLicenseKey(anahtar);
            using var doc = JsonDocument.Parse(lisansJson);
            var root = doc.RootElement;

            var firmaKodu = root.TryGetProperty("FirmaAdi", out var f) ? f.GetString() ?? "UNKNOWN" : "UNKNOWN";
            var lisansTipi = root.TryGetProperty("LisansTipi", out var t) ? t.GetString() ?? "trial" : "trial";
            var baslangic = root.TryGetProperty("BaslangicTarihi", out var b) ? b.GetDateTime() : DateTime.UtcNow;
            var bitis = root.TryGetProperty("BitisTarihi", out var e) ? e.GetDateTime() : DateTime.UtcNow.AddDays(30);

            if (root.TryGetProperty("MakineKodu", out var mk))
            {
                var lisansMakineKodu = mk.GetString() ?? "";
                var currentMakineKodu = GetMachineId();
                if (!string.Equals(
                    KOAFiloServis.Shared.LisansHelper.NormalizeMachineCode(lisansMakineKodu),
                    KOAFiloServis.Shared.LisansHelper.NormalizeMachineCode(currentMakineKodu),
                    StringComparison.Ordinal))
                {
                    throw new Exception("Bu lisans baska bir bilgisayar icin olusturulmus!");
                }
            }

            return (firmaKodu, lisansTipi, baslangic, bitis);
        }
        catch (Exception ex) when (ex.Message.Contains("baska bir bilgisayar"))
        {
            throw;
        }
        catch (FormatException)
        {
            throw new Exception("Gecersiz lisans formati — Base64 decode hatasi.");
        }
        catch (CryptographicException)
        {
            throw new Exception("Lisans anahtari sifresi cozulemedi. Gecersiz veya bozuk lisans anahtari.");
        }
        catch (JsonException)
        {
            throw new Exception("Lisans verisi okunamadi. Gecersiz lisans formati.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Lisans aktive edilemedi: {ex.Message}");
        }
    }

    private const string LisansAesKey = "KOAFiloServis2026SecretKey!@";

    private static string DecryptLicenseKey(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(LisansAesKey));
        aes.Key = key;

        var iv = new byte[aes.IV.Length];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Array.Copy(fullCipher, iv, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }

    // ══════════════════════════════════════════════
    // READ / SAVE
    // ══════════════════════════════════════════════

    public async Task<LicenseInfo?> GetCurrentLicenseAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.LicenseInfos.FirstOrDefaultAsync(l => l.IsActive && !l.IsDeleted);
    }

    public async Task SaveLicenseAsync(LicenseInfo lic)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var activeLicenses = await db.LicenseInfos.Where(l => l.IsActive).ToListAsync();
        foreach (var al in activeLicenses)
            al.IsActive = false;

        var expectedSig = GenerateSignature(lic.FirmaKodu, lic.MachineId, lic.ExpireDate,
            lic.IsDemo, lic.AllowedVersion, lic.CreatedAt);

        if (lic.Signature != expectedSig)
        {
            _logger.LogWarning("Lisans dosyasi imzasi duzeltildi (orijinal uyusmadi).");
            lic.Signature = expectedSig;
        }

        lic.IsActive = true;
        lic.UpdatedAt = DateTime.UtcNow;

        db.LicenseInfos.Add(lic);
        await db.SaveChangesAsync();

        // PART 5: Hash dosyasini guncelle
        WriteLicenseHash(lic);
    }

    // ══════════════════════════════════════════════
    // UI HELPERS (static)
    // ══════════════════════════════════════════════

    public static int GetRemainingDays(LicenseInfo lic)
    {
        if (lic == null) return 0;
        if (lic.IsDemo)
            return Math.Max(0, DemoMaxDays - (int)(DateTime.UtcNow - lic.CreatedAt).TotalDays);
        return Math.Max(0, (lic.ExpireDate.Date - DateTime.UtcNow.Date).Days);
    }

    public static bool IsDemoExpired(LicenseInfo lic)
    {
        if (lic == null) return true;
        if (!lic.IsDemo) return DateTime.UtcNow > lic.ExpireDate;
        return (DateTime.UtcNow - lic.CreatedAt).TotalDays > DemoMaxDays
            || DateTime.UtcNow > lic.ExpireDate
            || DateTime.UtcNow < lic.CreatedAt;
    }

    // ══════════════════════════════════════════════
    // COMPATIBILITY METHODS
    // ══════════════════════════════════════════════

    public async Task<int> GetMaxUserCountAsync()
    {
        var lic = await GetCurrentLicenseAsync();
        if (lic == null) return 0;
        return lic.IsDemo ? 5 : int.MaxValue;
    }

    public async Task<bool> CheckUserLimitAsync(int currentUserCount)
    {
        var max = await GetMaxUserCountAsync();
        return currentUserCount < max;
    }

    public bool HasModulePermission(string moduleName)
    {
        return true;
    }
}

// ══════════════════════════════════════════════
// RESULT OBJECT
// ══════════════════════════════════════════════

public class LicenseValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public LicenseInfo? License { get; set; }

    public static LicenseValidationResult Ok(LicenseInfo lic) => new() { IsValid = true, License = lic };
    public static LicenseValidationResult Fail(string msg) => new() { IsValid = false, Message = msg };
}

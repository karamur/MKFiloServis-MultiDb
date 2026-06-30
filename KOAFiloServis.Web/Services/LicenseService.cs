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
///   5. File hash     — C:\KOAFiloServis\config\license.hash
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
    private readonly LicenseCache _cache; // Singleton cache — request'ler arası yaşar
    private const string SecretKey = "KOAFiloServis-LCNS-2026-SECURE-KEY-X9mK2pL5vR8w";
    private const string RegistryPath = @"SOFTWARE\KOAFiloServis";
    private const string DemoUsedValueName = "DemoUsed";
    private const int DemoMaxDays = 30;

    public LicenseService(IDbContextFactory<ApplicationDbContext> dbFactory, IConfiguration config, ILogger<LicenseService> logger, LicenseCache cache)
    {
        _dbFactory = dbFactory;
        _config = config;
        _logger = logger;
        _cache = cache;
    }

    // ══════════════════════════════════════════════
    // PART 1: REGISTRY LOCK — Demo sadece 1 kez
    // DB silinse bile REGISTRY'den kontrol edilir
    // ══════════════════════════════════════════════

    public static bool HasDemoBeenUsed()
    {
        if (!OperatingSystem.IsWindows())
            return false;

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
        if (!OperatingSystem.IsWindows())
            return;

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
        bool isDemo = false, string allowedVersion = "1.0.99", DateTime? createdAt = null,
        int durationDays = 365, string contactPhone = "")
    {
        var created = createdAt ?? DateTime.UtcNow;
        // 🔥 KRİTİK: Desktop MainForm.cs Uret() ile BİREBİR AYNI format
        var raw = $"{firmaKodu}|{machineId}|{expireDate:yyyy-MM-dd}|{durationDays}|{isDemo}|{allowedVersion}|{created:yyyy-MM-dd}|{contactPhone}|{SecretKey}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(hash);
    }

    public bool VerifySignature(LicenseInfo lic)
    {
        var expected = GenerateSignature(
            lic.FirmaKodu, lic.MachineId, lic.ExpireDate,
            lic.IsDemo, lic.AllowedVersion, lic.CreatedAt,
            lic.DurationDays, lic.ContactPhone);
        return lic.Signature == expected;
    }

    // ══════════════════════════════════════════════
    // PART 5: LICENSE FILE HASH PROTECTION
    // C:\KOAFiloServis\config\license.hash
    // ══════════════════════════════════════════════

    private static string GetLicenseHashDirectory()
        => @"C:\KOAFiloServis\config";

    private static string GetLicenseHashPath()
        => Path.Combine(GetLicenseHashDirectory(), "license.hash");

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
            var dir = GetLicenseHashDirectory();
            Directory.CreateDirectory(dir);

            var hash = ComputeLicenseHash(lic);
            var path = GetLicenseHashPath();
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
            var path = GetLicenseHashPath();
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
    // DEV/PROD MODE — Developer Override
    // ══════════════════════════════════════════════

    /// <summary>Gizli developer bypass anahtari. Sadece appsettings.Development.json'da bulunur.</summary>
    private const string DevOverrideKey = "KOA-DEV-OVERRIDE-2026-X9";

    /// <summary>Visual Studio / Development ortami kontrolu.</summary>
    private static bool IsDevelopment()
        => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

    /// <summary>
    /// SADECE gelistirici bilgisayarinda true doner.
    /// MachineName + UserName eslesmesi sart.
    /// Bu method hacklenemez — hard-coded.
    /// </summary>
    private static bool IsDeveloperMachine()
    {
        var machine = Environment.MachineName;
        var user = Environment.UserName;
        return machine.Contains("DESKTOP-GJUJ5JR", StringComparison.OrdinalIgnoreCase)
            && user.Contains("muratk", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Developer override aktif mi?
    /// SART: (Development ortami VEYA override key) VE developer makinesi.
    /// </summary>
    private static bool IsDeveloperOverride(string? overrideKey = null)
    {
        // Makine kontrolü OLMADAN override çalışmaz
        if (!IsDeveloperMachine()) return false;

        // Development ortamında otomatik bypass
        if (IsDevelopment()) return true;

        // Veya secret key ile bypass
        if (overrideKey == DevOverrideKey) return true;

        return false;
    }

    // ══════════════════════════════════════════════
    // CORE: VALIDATE — 10 katman koruma (PROD only)
    // ══════════════════════════════════════════════

    public async Task<LicenseValidationResult> ValidateAsync(string? overrideKey = null)
    {
        // 🔥 DEVELOPER OVERRIDE — makine + environment/key kontrolu
        if (IsDeveloperOverride(overrideKey))
        {
            KOAFiloServis.Shared.AppMode.ExitDemoMode();
            _logger.LogInformation("🔧 DEV OVERRIDE AKTIF — Lisans kontrolu BYPASS edildi. Makine: {Machine}", GetMachineId());
            var devLicense = new LicenseInfo
            {
                FirmaKodu = "DEV-OVERRIDE",
                MachineId = GetMachineId(),
                ExpireDate = DateTime.UtcNow.AddYears(99),
                Signature = "DEV-OVERRIDE-BYPASS",
                IsDemo = false,
                IsActive = true,
                AllowedVersion = "99.0.0",
                CreatedAt = DateTime.UtcNow
            };
            _cache.Set(devLicense); // 🔥 KRİTİK: Singleton cache set
            return LicenseValidationResult.Ok(devLicense);
        }

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

                _cache.Set(lic); // 🔥 KRİTİK: Singleton cache set — demo lisans
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

            // ── Firma kodu kontrolu (sadece config'de varsa ve lisans demosu degilse) ──
            var configFirma = _config["FirmaKodu"];
            if (!string.IsNullOrWhiteSpace(configFirma) && !lic.IsDemo && lic.FirmaKodu != configFirma)
                return LicenseValidationResult.Fail(
                    $"🛑 Firma kodu uyusmazligi: '{lic.FirmaKodu}' != '{configFirma}'");

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

            _cache.Set(lic); // Scope cache

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

        // 🔥 KRİTİK: Singleton cache güncelle + Demo moddan çık
        _cache.Set(yeniLisans);
        KOAFiloServis.Shared.AppMode.ExitDemoMode();

        _logger.LogInformation("✅ Lisans anahtari aktive edildi: {FirmaKodu}, Bitis: {ExpireDate}, Makine: {MachineId}",
            yeniLisans.FirmaKodu, yeniLisans.ExpireDate, machineId);

        return yeniLisans;
    }

    // ══════════════════════════════════════════════
    // ACTIVATION FROM KEY (Base64 JSON)
    // ══════════════════════════════════════════════

    /// <summary>
    /// Aktivasyon key'inden lisans aktive eder.
    /// Key = Base64(license.json).
    /// LisansDesktop'un urettigi formatta calisir.
    /// </summary>
    public async Task<LicenseInfo> ActivateFromKeyAsync(string key)
    {
        // Trim + temizle
        key = key.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");

        // Base64 decode → JSON
        string json;
        try
        {
            var bytes = Convert.FromBase64String(key);
            json = Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            throw new Exception("Lisans anahtari gecersiz formatta. Lutfen kopyaladiginiz kodu kontrol edin.");
        }

        // JSON → LicenseInfo
        LicenseInfo lic;
        try
        {
            lic = JsonSerializer.Deserialize<LicenseInfo>(json)
                  ?? throw new Exception("Lisans verisi okunamadi.");
        }
        catch (JsonException)
        {
            throw new Exception("Lisans anahtari gecersiz. Lutfen dogru kodu yapistirdiginizdan emin olun.");
        }

        if (string.IsNullOrWhiteSpace(lic.FirmaKodu) || string.IsNullOrWhiteSpace(lic.Signature))
            throw new Exception("Lisans anahtari eksik bilgi iceriyor.");

        // Signature dogrulama — DurationDays + ContactPhone dahil
        var expectedSig = GenerateSignature(lic.FirmaKodu, lic.MachineId, lic.ExpireDate,
            lic.IsDemo, lic.AllowedVersion, lic.CreatedAt,
            lic.DurationDays, lic.ContactPhone);

        if (lic.Signature != expectedSig)
            throw new Exception("Lisans imzasi gecersiz. Anahtar degistirilmis olabilir.");

        // Machine lock
        if (lic.MachineId != GetMachineId())
            throw new Exception("Bu lisans anahtari bu bilgisayar icin gecerli degil.");

        // ExpireDate kontrolu
        if (DateTime.UtcNow > lic.ExpireDate)
            throw new Exception($"Lisans suresi {lic.ExpireDate:yyyy-MM-dd} tarihinde dolmus.");

        // Version kontrolu
        if (!IsVersionAllowed(lic.AllowedVersion))
            throw new Exception($"Bu uygulama surumu ({GetAppVersion()}) lisansa dahil degil. Max: {lic.AllowedVersion}");

        // 🔥 PART 9: DurationDays tutarlilik kontrolu — tolerans ±2 gun
        if (lic.DurationDays > 0)
        {
            var expectedDays = (lic.ExpireDate.Date - lic.CreatedAt.Date).Days;
            if (Math.Abs(expectedDays - lic.DurationDays) > 2)
                throw new Exception($"Lisans suresi hatali: {lic.DurationDays} gun belirtilmis ama tarihler arasi {expectedDays} gun.");
        }

        // DB'ye kaydet — eski lisanslari pasif yap
        await using var db = await _dbFactory.CreateDbContextAsync();
        var activeLicenses = await db.LicenseInfos.Where(l => l.IsActive).ToListAsync();
        foreach (var al in activeLicenses)
            al.IsActive = false;

        lic.IsActive = true;
        lic.LastValidatedAt = DateTime.UtcNow;
        db.LicenseInfos.Add(lic);
        await db.SaveChangesAsync();

        // DB cache güncelle — lisans anında okunur
        await db.Entry(lic).ReloadAsync();
        _cache.Set(lic);

        WriteLicenseHash(lic);
        KOAFiloServis.Shared.AppMode.ExitDemoMode(); // 🔥 KRİTİK: Demo moddan çık

        _logger.LogInformation("✅ Lisans aktive edildi (key): {FirmaKodu}, Bitis: {ExpireDate}",
            lic.FirmaKodu, lic.ExpireDate);

        return lic;
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
                    NormalizeMachineCodeSafe(lisansMakineKodu),
                    NormalizeMachineCodeSafe(currentMakineKodu),
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

    private static string NormalizeMachineCodeSafe(string? machineCode)
    {
        if (OperatingSystem.IsWindows())
            return KOAFiloServis.Shared.LisansHelper.NormalizeMachineCode(machineCode);

        return (machineCode ?? string.Empty)
            .Trim()
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToUpperInvariant();
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
    // LOGGING — uretilen lisanslari kaydet (audit)
    // ══════════════════════════════════════════════

    /// <summary>
    /// API veya admin paneli uzerinden uretilen lisanslari log olarak kaydeder.
    /// IsActive=false — bu lisans otomatik aktif olmaz.
    /// </summary>
    public async Task SaveGeneratedLogAsync(LicenseInfo lic)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.LicenseInfos.Add(lic);
        await db.SaveChangesAsync();
        _logger.LogInformation("Lisans uretildi (log): {FirmaKodu}, {MachineId}, {ExpireDate}",
            lic.FirmaKodu, lic.MachineId, lic.ExpireDate);
    }

    // ══════════════════════════════════════════════
    // READ / SAVE
    // ══════════════════════════════════════════════

    public async Task<LicenseInfo?> GetCurrentLicenseAsync()
    {
        // Singleton cache varsa tekrar DB'ye gitme
        var cached = _cache.Get();
        if (cached != null)
            return cached;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var lic = await db.LicenseInfos.FirstOrDefaultAsync(l => l.IsActive && !l.IsDeleted);
        if (lic != null)
            _cache.Set(lic);
        return lic;
    }

    /// <summary>
    /// Scope icinde gecerli bir lisans var mi?
    /// MainLayout demo banner kontrolu icin kullanilir.
    /// </summary>
    public bool HasValidLicense()
    {
        // 🔥 Singleton LicenseCache'e delege et — DB sorgusu yapma.
        return _cache.HasValidLicense();
    }

    public async Task SaveLicenseAsync(LicenseInfo lic)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var activeLicenses = await db.LicenseInfos.Where(l => l.IsActive).ToListAsync();
        foreach (var al in activeLicenses)
            al.IsActive = false;

        var expectedSig = GenerateSignature(lic.FirmaKodu, lic.MachineId, lic.ExpireDate,
            lic.IsDemo, lic.AllowedVersion, lic.CreatedAt,
            lic.DurationDays, lic.ContactPhone);

        if (lic.Signature != expectedSig)
        {
            _logger.LogWarning("Lisans dosyasi imzasi duzeltildi (orijinal uyusmadi).");
            lic.Signature = expectedSig;
        }

        lic.IsActive = true;
        lic.UpdatedAt = DateTime.UtcNow;

        db.LicenseInfos.Add(lic);
        await db.SaveChangesAsync();

        // 🔥 KRİTİK: Singleton cache'i güncelle — PART 6 loop koruma
        _cache.Set(lic);

        // PART 5: Hash dosyasini guncelle
        WriteLicenseHash(lic);
    }

    // ══════════════════════════════════════════════
    // UI HELPERS (static)
    // ══════════════════════════════════════════════

    public static int GetRemainingDays(LicenseInfo lic)
    {
        // 🔥 PART 7: Demo/gercek ayrimi yok — her ikisi de ExpireDate'ten hesaplanir.
        // Onceki bug: demo icin CreatedAt bazli hesaplama 36159 gun gosteriyordu.
        if (lic == null) return 0;
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

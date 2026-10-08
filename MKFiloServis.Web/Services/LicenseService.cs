using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Globalization;

namespace MKFiloServis.Web.Services;

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
///  10. File integrity — Hash yoksa veya okunamiyorsa dogrulama basarisiz olur
/// </summary>
public class LicenseService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<LicenseService> _logger;
    private readonly LicenseCache _cache; // Singleton cache — request'ler arası yaşar
    private readonly DatabaseRuntimeInfo _databaseRuntime;
    private readonly IDataProtector _demoProtector;
    // Public verification key only. The private signing key is kept outside the repository and installers.
    private const string PublicKeyPem = MKFiloServis.Shared.Licensing.LicenseSigningPublicKey.Pem;
    private const string RegistryPath = @"SOFTWARE\KOAFiloServis";
    private const string DemoUsedValueName = "DemoUsed";
    private const int DemoMaxDays = 15;
    private const int DemoButtonDays = 15;

    public LicenseService(IDbContextFactory<ApplicationDbContext> dbFactory, IConfiguration config, ILogger<LicenseService> logger, LicenseCache cache, DatabaseRuntimeInfo databaseRuntime, IDataProtectionProvider dataProtection)
    {
        _dbFactory = dbFactory;
        _config = config;
        _logger = logger;
        _cache = cache;
        _databaseRuntime = databaseRuntime;
        _demoProtector = dataProtection.CreateProtector("MKFiloServis.License.Demo.v1");
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
    // PART 2: MACHINE ID — STABLE
    // MachineName + UserName + StableHardwareFingerprint
    // ══════════════════════════════════════════════

    public static string GetMachineId()
    {
        try
        {
            var machine = Environment.MachineName;
            var user = Environment.UserName;
            var stableHardwareCode = "UNKNOWN";

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    // Tek kaynak: paylaşılan donanım tabanlı kod
                    stableHardwareCode = MKFiloServis.Shared.LisansHelper.GetMachineCode();
                }
            }
            catch
            {
                stableHardwareCode = "UNKNOWN";
            }

            return $"{machine}_{user}_{stableHardwareCode}";
        }
        catch
        {
            return $"{Environment.MachineName}_{Environment.UserName}_UNKNOWN";
        }
    }

    // ══════════════════════════════════════════════
    // PART 3: HARDENED SIGNATURE
    // FirmaKodu|MachineId|ExpireDate|IsDemo|AllowedVersion|CreatedAt
    // ══════════════════════════════════════════════

    private static string LegacyPayload(LicenseInfo lic) =>
        $"{lic.FirmaKodu}|{lic.MachineId}|{lic.ExpireDate:yyyy-MM-dd}|{lic.DurationDays}|{lic.IsDemo}|{lic.AllowedVersion}|{lic.CreatedAt:yyyy-MM-dd}|{lic.ContactPhone}";

    private static string SignaturePayload(LicenseInfo lic)
        => MKFiloServis.Shared.Licensing.LicenseSignaturePayload.Create(
            lic.FirmaKodu, lic.MachineId, lic.ExpireDate, lic.DurationDays,
            lic.IsDemo, lic.AllowedVersion, lic.CreatedAt, lic.ContactPhone);

    private string CreateDemoSignature(LicenseInfo lic) =>
        "demo:" + _demoProtector.Protect(SignaturePayload(lic));

    public bool VerifySignature(LicenseInfo lic)
    {
        var signature = lic.Signature ?? string.Empty;
        var payload = Encoding.UTF8.GetBytes(SignaturePayload(lic));
        try
        {
            if (MKFiloServis.Shared.Licensing.LicenseModules.TryRead(signature, out var modules, out var signed) && !lic.IsDemo)
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(PublicKeyPem);
                var modulePayload = MKFiloServis.Shared.Licensing.LicenseModules.Payload(SignaturePayload(lic), modules);
                return rsa.VerifyData(Encoding.UTF8.GetBytes(modulePayload), Convert.FromBase64String(signed),
                    HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }

            if (signature.StartsWith("v2:", StringComparison.Ordinal) && !lic.IsDemo)
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(PublicKeyPem);
                return rsa.VerifyData(payload, Convert.FromBase64String(signature[3..]),
                    HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }

            if (signature.StartsWith("demo:", StringComparison.Ordinal) && lic.IsDemo)
            {
                var original = _demoProtector.Unprotect(signature[5..]);
                return CryptographicOperations.FixedTimeEquals(payload, Encoding.UTF8.GetBytes(original));
            }

            // Eski lisanslar yalnızca kurulum sahibinin tanımladığı süreli geçişte kabul edilir.
            var legacySecret = _config["Licensing:LegacyVerificationSecret"];
            var cutoff = _config["Licensing:LegacyAcceptUntilUtc"];
            if (string.IsNullOrWhiteSpace(legacySecret) ||
                !DateTimeOffset.TryParse(cutoff, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var deadline) ||
                DateTimeOffset.UtcNow > deadline)
                return false;

            var legacyDigest = SHA256.HashData(Encoding.UTF8.GetBytes(LegacyPayload(lic) + "|" + legacySecret));
            return CryptographicOperations.FixedTimeEquals(legacyDigest, Convert.FromBase64String(signature));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or FormatException or CryptographicException)
        {
            _logger.LogWarning("Lisans imzasi veya dogrulama anahtari okunamadi: {Reason}", ex.GetType().Name);
            return false;
        }
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
                _logger.LogError("Lisans bütünlük dosyası bulunamadı: {Path}. Lisans yeniden yüklenmelidir.", path);
                return false;
            }

            var storedHash = File.ReadAllText(path).Trim();
            var computedHash = ComputeLicenseHash(lic);

            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(storedHash),
                    Encoding.UTF8.GetBytes(computedHash)))
            {
                _logger.LogError("Lisans bütünlük doğrulaması başarısız oldu.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "License hash dogrulama hatasi (disk erisilemez)");
            return false;
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
        => MKFiloServis.Shared.Licensing.LicenseVersionPolicy.Allows(allowedVersion, GetAppVersion());

    // ══════════════════════════════════════════════
    // CORE: VALIDATE — 10 katman koruma (PROD only)
    // ══════════════════════════════════════════════

    private async Task PublishValidatedLicenseAsync(LicenseInfo lic)
    {
        WriteLicenseHash(lic);
        var validation = await ValidateAsync(persistValidation: false);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message);
    }

    private LicenseValidationResult FailValidation(string message)
    {
        _cache.Clear();
        return LicenseValidationResult.Fail(message);
    }

    public async Task<LicenseValidationResult> ValidateAsync(bool persistValidation = true)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var lic = await db.LicenseInfos
                .IgnoreQueryFilters()
                .Where(l => l.IsActive && !l.IsDeleted)
                .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
                .ThenByDescending(l => l.Id)
                .FirstOrDefaultAsync();
            var hasAnyLicense = await db.LicenseInfos
                .IgnoreQueryFilters()
                .AnyAsync(l => !l.IsDeleted);

            // ── Lisans yok: demo ancak kullanıcı giriş ekranından açıkça başlatır ──
            if (lic == null)
            {
                // PART 1: Registry kontrolu
                if (HasDemoBeenUsed())
                    return FailValidation(
                        "🛑 Demo hakki zaten kullanilmis. Lisans dosyasini yukleyin.");

                if (hasAnyLicense)
                    return FailValidation(
                        "🛑 Veritabaninda lisans kaydi var fakat aktif degil. Lisans dosyasini yukleyin.");

                return FailValidation(
                    "Lisans bulunamadı. Giriş ekranından 15 günlük demo modunu başlatabilir veya lisans anahtarı yükleyebilirsiniz.");
            }

            // ── PART 8: SECURITY VIOLATION — registry silinmis ama DB'de lisans var ──
            if (lic.IsDemo && !HasDemoBeenUsed() && hasAnyLicense)
            {
                _logger.LogCritical(
                    "🚨 GUVENLIK IHLALI! Registry'de demo kaydi yok ama DB'de demo lisans var. Makine: {MachineId}",
                    GetMachineId());
                return FailValidation(
                    "🛑 Guvenlik ihlali tespit edildi. Demo lisans dosyasi degistirilmis olabilir. Lisans dosyasini tekrar yukleyin.");
            }

            // ── PART 4.3: Negative time attack ──
            if (DateTime.UtcNow < lic.CreatedAt)
                return FailValidation(
                    "🛑 Sistem saati hatali. Lisans olusturma tarihinden onceki bir tarih algilandi. Lutfen saat ayarlarinizi kontrol edin.");

            // ── PART 2: Machine lock ──
            var currentMachineId = GetMachineId();
            if (!IsSameMachineBinding(lic.MachineId, currentMachineId))
            {
                // Veritabanı farklı bilgisayara taşındıysa mevcut aktif lisansı pasife al.
                // Böylece sistem yeni PC'de yeniden lisans aktivasyonu ister.
                await db.LicenseInfos
                    .IgnoreQueryFilters()
                    .Where(l => l.IsActive)
                    .UpdateTrackedAsync(db, record =>
                    {
                        record.IsActive = false;
                        record.UpdatedAt = DateTime.UtcNow;
                    });

                _cache.Clear();

                return FailValidation(
                    $"🛑 Lisans bu bilgisayara ait degil. Veritabani baska bir bilgisayara tasinmis olabilir. Lutfen bu makine icin yeni lisans anahtari girin. Lisans makinesi: {lic.MachineId}, Bu makine: {currentMachineId}");
            }

            // ── PART 4.1: CreatedAt tabanli (demo icin mutlak 30 gun) ──
            if (lic.IsDemo)
            {
                var elapsed = (DateTime.UtcNow - lic.CreatedAt).TotalDays;
                if (elapsed > DemoMaxDays)
                {
                    _logger.LogWarning("Demo suresi doldu (CreatedAt): {Days} gun, Makine: {MachineId}",
                        elapsed, currentMachineId);
                    return FailValidation(
                        $"🛑 Demo suresi doldu. Olusturma: {lic.CreatedAt:yyyy-MM-dd}, Gecen gun: {elapsed:F0}/{DemoMaxDays}");
                }
            }

            // Son başarılı kontrolden geriye alınan saat, tolerans dışında reddedilir.
            var nowUtc = DateTime.UtcNow;
            if (lic.LastValidatedAt.HasValue && lic.LastValidatedAt.Value - nowUtc > TimeSpan.FromMinutes(5))
                return FailValidation("🛑 Sistem saati son lisans kontrolünden önceye alınmış.");

            // Ticari lisanslarda en fazla 14 gün sabit yenileme toleransı.
            // Demo için tolerans verilmez; önceden doğrulanmamış lisans tolerans kullanamaz.
            var inRenewalGrace = !lic.IsDemo && lic.LastValidatedAt.HasValue &&
                nowUtc > lic.ExpireDate && nowUtc - lic.ExpireDate <= TimeSpan.FromDays(14);
            if (nowUtc > lic.ExpireDate && !inRenewalGrace)
                return FailValidation(
                    $"🛑 Lisans suresi doldu ({lic.ExpireDate:yyyy-MM-dd}).");

            // ── Firma kodu kontrolu (sadece config'de varsa ve lisans demosu degilse) ──
            var configFirma = _config["FirmaKodu"];
            if (!string.IsNullOrWhiteSpace(configFirma) && !lic.IsDemo && lic.FirmaKodu != configFirma)
                return FailValidation(
                    $"🛑 Firma kodu uyusmazligi: '{lic.FirmaKodu}' != '{configFirma}'");

            // ── PART 3: Signature dogrulama ──
            if (!VerifySignature(lic))
                return FailValidation(
                    !string.IsNullOrEmpty(lic.Signature) && !lic.Signature.Contains(':')
                        ? "🛑 Eski lisans imzasi artik kabul edilmiyor. Mevcut sureyi koruyan v2 imzali lisans yukleyin."
                        : "🛑 Lisans imzasi gecersiz. Lisans dosyasi bozulmus veya degistirilmis olabilir.");

            // ── PART 9: Version check ──
            if (!IsVersionAllowed(lic.AllowedVersion))
                return FailValidation(
                    $"🛑 Bu uygulama surumu ({GetAppVersion()}) lisansa dahil degil. Izin verilen max surum: {lic.AllowedVersion}");

            // ── PART 5: License file hash verification ──
            if (!VerifyLicenseHash(lic))
                return FailValidation(
                    "🛑 Lisans dosyasi degistirilmis! Hash uyusmazligi tespit edildi. Lisansi tekrar yukleyin.");

            if (inRenewalGrace)
                _logger.LogWarning("Lisans yenileme toleransinda: Firma={Firma}, Bitis={ExpireDate}",
                    lic.FirmaKodu, lic.ExpireDate);

            // ── Basarili ──
            if (persistValidation)
            {
                lic.LastValidatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            _cache.Set(lic, validated: true); // Tam doğrulama sonrası modül hakları açılır

            return LicenseValidationResult.Ok(lic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lisans dogrulama hatasi");
            return FailValidation($"🛑 Lisans kontrol hatasi: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════
    // TRIAL LICENSE OLUSTURMA
    // ══════════════════════════════════════════════

    private async Task<LicenseInfo> CreateTrialLicenseAsync(ApplicationDbContext db, int durationDays = DemoMaxDays)
    {
        var firmaKodu = _config["FirmaKodu"] ?? "DEMO";
        var machineId = GetMachineId();
        var now = DateTime.UtcNow;
        var expireDate = now.AddDays(durationDays);
        var allowedVersion = GetAppVersion().ToString();
        var lic = new LicenseInfo
        {
            FirmaKodu = firmaKodu,
            MachineId = machineId,
            ExpireDate = expireDate,
            DurationDays = durationDays,
            IsDemo = true,
            IsActive = true,
            AllowedVersion = allowedVersion,
            CreatedAt = now
        };
        lic.Signature = CreateDemoSignature(lic);

        db.LicenseInfos.Add(lic);
        await db.SaveChangesAsync();
        return lic;
    }

    public async Task<LicenseInfo> InstallDemoLicenseAsync()
    {
        if (!_databaseRuntime.IsSqlite)
            throw new InvalidOperationException("Demo modu yalnızca kurulumda seçilen bağımsız SQLite demo veritabanında kullanılabilir. Mevcut sunucu veritabanınız değiştirilmedi.");

        if (HasDemoBeenUsed())
            throw new InvalidOperationException("Demo hakki zaten kullanildi. Lisans anahtarini girin.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.LicenseInfos.IgnoreQueryFilters().AnyAsync(l => !l.IsDeleted))
            throw new InvalidOperationException("Bu veritabanında daha önce lisans kaydı oluşturulmuş. Demo lisansı mevcut verinin üzerine kurulamaz.");

        await db.LicenseInfos
            .IgnoreQueryFilters()
            .Where(l => l.IsActive)
            .UpdateTrackedAsync(db, record =>
            {
                record.IsActive = false;
                record.UpdatedAt = DateTime.UtcNow;
            });

        var lic = await CreateTrialLicenseAsync(db, DemoButtonDays);
        MarkDemoUsed();
        await PublishValidatedLicenseAsync(lic);
        MKFiloServis.Shared.AppMode.ExitDemoMode();

        _logger.LogInformation("✅ Demo lisans kurulumu tamamlandi: {FirmaKodu}, Bitis: {ExpireDate}, Makine: {MachineId}, Gun: {Days}",
            lic.FirmaKodu, lic.ExpireDate, lic.MachineId, lic.DurationDays);

        return lic;
    }

    // ══════════════════════════════════════════════
    // LICENSE KEY ACTIVATION
    // ══════════════════════════════════════════════

    public Task<LicenseInfo> ActivateLicenseKeyAsync(string lisansAnahtari)
        => ActivateFromKeyAsync(lisansAnahtari);

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
        key = key.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");

        var lic = TryParseJsonLicenseKey(key);
        if (lic == null)
        {
            lic = TryParseLegacyEncryptedLicenseKey(key)
                  ?? throw new Exception("Lisans anahtari gecersiz. Yeni Base64 lisans veya eski sifreli lisans formati bekleniyor.");
        }

        if (string.IsNullOrWhiteSpace(lic.FirmaKodu) || string.IsNullOrWhiteSpace(lic.Signature))
            throw new Exception("Lisans anahtari eksik bilgi iceriyor.");

        if (!VerifySignature(lic))
            throw new Exception("Lisans imzasi gecersiz. Anahtar degistirilmis olabilir.");

        // Makine kodunun tamamı eşleşmeli; donanım değişiminde yeni imzalı lisans gerekir.
        if (!IsSameMachineBinding(lic.MachineId, GetMachineId()))
            throw new Exception("Bu lisans anahtari bu bilgisayar icin gecerli degil.");

        if (DateTime.UtcNow < lic.CreatedAt)
            throw new InvalidOperationException("Lisans oluşturma tarihi gelecekte. Sistem saatini ve lisans kaydını kontrol edin.");

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
        await using var activationTransaction = await db.Database.BeginTransactionAsync();

        // Mevcut kurulumda firma kodu eşleşmiyorsa lisansı başka firmaya bağlama.
        // Yalnızca henüz hiç firma kaydı olmayan temiz kurulum lisans bilgisiyle ilk firmayı kurabilir.
        var firma = await ResolveFirmaForLicenseAsync(db, lic.FirmaKodu);
        if (firma == null)
        {
            var mevcutFirmaVar = await db.Firmalar.IgnoreQueryFilters()
                .AnyAsync(f => !f.IsDeleted);
            if (mevcutFirmaVar)
                throw new InvalidOperationException(
                    $"Lisans firma kodu ('{lic.FirmaKodu}') mevcut aktif firmalarla eşleşmiyor. Firma kodunu doğrulayın veya doğru lisansı kullanın.");

            firma = await CreateFirmaFromLicenseAsync(db, lic);
        }

        // 🔥 KRİTİK: FirmaKodu'nu DEGISTIRME! Signature, anahtar icindeki orijinal
        // FirmaKodu ile uretildi. Degistirirsek sonraki ValidateAsync/VerifySignature
        // dongusunde imza uyusmaz ve lisans "gecersiz" olarak reddedilir.
        // Sadece FirmaId iliskisini kur.
        lic.FirmaId = firma.Id;

        await db.LicenseInfos
            .IgnoreQueryFilters()
            .Where(l => l.IsActive)
            .UpdateTrackedAsync(db, record =>
            {
                record.IsActive = false;
                record.UpdatedAt = DateTime.UtcNow;
            });

        lic.IsActive = true;
        lic.LastValidatedAt = DateTime.UtcNow;
        db.LicenseInfos.Add(lic);
        await db.SaveChangesAsync();
        await activationTransaction.CommitAsync();

        // DB cache güncelle — lisans anında okunur
        await db.Entry(lic).ReloadAsync();
        await PublishValidatedLicenseAsync(lic);
        MKFiloServis.Shared.AppMode.ExitDemoMode(); // 🔥 KRİTİK: Demo moddan çık

        _logger.LogInformation("✅ Lisans aktive edildi (key): {FirmaKodu}, Bitis: {ExpireDate}",
            lic.FirmaKodu, lic.ExpireDate);

        return lic;
    }

    private async Task<Firma?> ResolveFirmaForLicenseAsync(ApplicationDbContext db, string? licenseFirmaValue)
    {
        var firmalar = await db.Firmalar
            .IgnoreQueryFilters()
            .Where(f => !f.IsDeleted && f.Aktif)
            .OrderBy(f => f.Id)
            .ToListAsync();

        var normalizedLicenseValue = NormalizeFirmaMatchValue(licenseFirmaValue);
        if (!string.IsNullOrWhiteSpace(normalizedLicenseValue))
        {
            var matches = firmalar.Where(f => f.Aktif &&
                (NormalizeFirmaMatchValue(f.FirmaKodu) == normalizedLicenseValue ||
                 NormalizeFirmaMatchValue(f.FirmaAdi) == normalizedLicenseValue ||
                 NormalizeFirmaMatchValue(f.UnvanTam) == normalizedLicenseValue)).ToList();
            if (matches.Count > 1)
                throw new InvalidOperationException("Lisans firma bilgisi birden fazla aktif firmayla eşleşiyor. Benzersiz firma kodunu kullanın.");
            if (matches.Count == 1) return matches[0];
        }

        _logger.LogWarning("Lisans firma koduyla eşleşen aktif firma yok. LisansDegeri={LicenseFirmaValue}", licenseFirmaValue);
        return null;
    }

    private async Task<Firma> CreateFirmaFromLicenseAsync(ApplicationDbContext db, LicenseInfo lic)
    {
        var firmaKodu = NormalizeFirmaCode(lic.FirmaKodu);
        var firmaAdi = string.IsNullOrWhiteSpace(lic.FirmaKodu) ? "Lisans Firmasi" : lic.FirmaKodu.Trim();
        var normalizedName = NormalizeOptionalText(firmaAdi) ?? "Lisans Firmasi";

        var existingWithCode = await db.Firmalar
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => !f.IsDeleted && f.FirmaKodu == firmaKodu);
        if (existingWithCode != null)
            return existingWithCode;

        var organizationId = await EnsureOrganizasyonIdAsync(db);
        var isFirstFirma = !await db.Firmalar.IgnoreQueryFilters().AnyAsync(f => !f.IsDeleted);

        var firma = new Firma
        {
            FirmaKodu = await ResolveUniqueFirmaCodeAsync(db, firmaKodu),
            FirmaAdi = normalizedName,
            UnvanTam = normalizedName,
            Aktif = true,
            VarsayilanFirma = isFirstFirma,
            OrganizasyonId = organizationId,
            AktifDonemYil = DateTime.Today.Year,
            AktifDonemAy = DateTime.Today.Month,
            CreatedAt = DateTime.UtcNow
        };

        db.Firmalar.Add(firma);
        await db.SaveChangesAsync();

        _logger.LogWarning("Lisans aktivasyonu sirasinda yeni firma olusturuldu. LisansDegeri={LicenseFirmaValue}, FirmaId={FirmaId}, FirmaKodu={FirmaKodu}",
            lic.FirmaKodu, firma.Id, firma.FirmaKodu);

        return firma;
    }

    private async Task<int> EnsureOrganizasyonIdAsync(ApplicationDbContext db)
    {
        var mevcut = await db.Organizasyonlar
            .IgnoreQueryFilters()
            .OrderBy(o => o.Id)
            .FirstOrDefaultAsync();

        if (mevcut != null)
            return mevcut.Id;

        var organizasyon = new Organizasyon
        {
            Adi = "Ustun Holding",
            Kod = "USTUNHOLDING",
            Aciklama = "Lisans aktivasyonu sirasinda otomatik olusturulan organizasyon",
            CreatedAt = DateTime.UtcNow
        };

        db.Organizasyonlar.Add(organizasyon);
        await db.SaveChangesAsync();
        return organizasyon.Id;
    }

    private async Task<string> ResolveUniqueFirmaCodeAsync(ApplicationDbContext db, string? requestedCode)
    {
        var normalizedCode = NormalizeFirmaCode(requestedCode);
        var codeExists = await db.Firmalar
            .IgnoreQueryFilters()
            .AnyAsync(f => f.FirmaKodu == normalizedCode);

        if (!codeExists)
            return normalizedCode;

        var nextNumber = 1;
        var existingCodes = await db.Firmalar
            .IgnoreQueryFilters()
            .Select(f => f.FirmaKodu)
            .ToListAsync();

        foreach (var code in existingCodes)
        {
            if (string.IsNullOrWhiteSpace(code) || !code.StartsWith("F", StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(code[1..], out var parsed) && parsed >= nextNumber)
                nextNumber = parsed + 1;
        }

        var candidate = $"F{nextNumber:000}";
        while (existingCodes.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            nextNumber++;
            candidate = $"F{nextNumber:000}";
        }

        return candidate;
    }

    private static string NormalizeFirmaCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "F001";

        return MKFiloServis.Shared.Licensing.LicenseIdentity.CompanyCode(code);
    }

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeFirmaMatchValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private (string FirmaKodu, string LisansTipi, DateTime BaslangicTarihi, DateTime BitisTarihi)
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
                if (!IsSameMachineBinding(lisansMakineKodu, currentMakineKodu))
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

    private static bool IsSameMachineBinding(string? licenseMachineId, string? currentMachineId)
        => MKFiloServis.Shared.Licensing.LicenseIdentity.MatchesMachine(licenseMachineId, currentMachineId);

    private string DecryptLicenseKey(string cipherText)
    {
        var legacyEncryptionKey = _config["Licensing:LegacyEncryptionKey"];
        if (string.IsNullOrWhiteSpace(legacyEncryptionKey))
            throw new CryptographicException("Eski sifreli lisans gecisi etkin degil.");
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(legacyEncryptionKey));
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

    private static LicenseInfo? TryParseJsonLicenseKey(string key)
    {
        try
        {
            var bytes = Convert.FromBase64String(key);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<LicenseInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    private LicenseInfo? TryParseLegacyEncryptedLicenseKey(string key)
    {
        try
        {
            var legacySecret = _config["Licensing:LegacyVerificationSecret"];
            var cutoff = _config["Licensing:LegacyAcceptUntilUtc"];
            if (string.IsNullOrWhiteSpace(legacySecret) ||
                !DateTimeOffset.TryParse(cutoff, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var deadline) ||
                DateTimeOffset.UtcNow > deadline)
                return null;

            var (firmaKodu, lisansTipi, baslangicTarihi, bitisTarihi) = ParseLicenseKey(key);
            var machineId = GetMachineId();
            var durationDays = Math.Max(1, (bitisTarihi.Date - baslangicTarihi.Date).Days);
            var allowedVersion = "1.0.99";
            var contactPhone = string.Empty;
            var isDemo = string.Equals(lisansTipi, "trial", StringComparison.OrdinalIgnoreCase);
            var createdAt = baslangicTarihi == default ? DateTime.UtcNow : baslangicTarihi;
            var lic = new LicenseInfo
            {
                FirmaKodu = firmaKodu,
                MachineId = machineId,
                ExpireDate = bitisTarihi,
                DurationDays = durationDays,
                AllowedVersion = allowedVersion,
                IsDemo = isDemo,
                CreatedAt = createdAt,
                ContactPhone = contactPhone
            };
            lic.Signature = Convert.ToBase64String(SHA256.HashData(
                Encoding.UTF8.GetBytes(LegacyPayload(lic) + "|" + legacySecret)));
            return lic;
        }
        catch
        {
            return null;
        }
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
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.LicenseInfos.Add(lic);
            await db.SaveChangesAsync();
            _logger.LogInformation("Lisans uretildi (log): {FirmaKodu}, {MachineId}, {ExpireDate}",
                lic.FirmaKodu, lic.MachineId, lic.ExpireDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lisans kaydetme hatası. FirmaKodu={FirmaKodu}, MachineId={MachineId}, InnerException={InnerException}",
                lic.FirmaKodu, lic.MachineId, ex.InnerException?.Message);
            throw;
        }
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
        var lic = await db.LicenseInfos
            .IgnoreQueryFilters()
            .Where(l => l.IsActive && !l.IsDeleted)
            .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .FirstOrDefaultAsync();
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
        var key = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(lic)));
        await ActivateFromKeyAsync(key);
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
        var lic = _cache.GetValidated();
        if (lic == null || !lic.IsActive || lic.IsDeleted || !VerifySignature(lic)) return false;
        var now = DateTime.UtcNow;
        if (now < lic.CreatedAt || !IsVersionAllowed(lic.AllowedVersion)) return false;
        var grace = !lic.IsDemo && lic.LastValidatedAt.HasValue && now - lic.ExpireDate <= TimeSpan.FromDays(14);
        if (now > lic.ExpireDate && !grace) return false;
        if (!MKFiloServis.Shared.Licensing.LicenseModules.All.ContainsKey(moduleName)) return false;
        if (lic.IsDemo) return !IsDemoExpired(lic);
        return MKFiloServis.Shared.Licensing.LicenseModules.TryRead(lic.Signature, out var modules, out _)
            && modules.Split(',').Contains(moduleName, StringComparer.Ordinal);
    }

    public bool HasLicensedPermission(string permission)
    {
        var prefix = permission.Split('.')[0];
        if (permission.StartsWith("menu.", StringComparison.Ordinal))
        {
            var module = permission[5..];
            if (module == "checklist") module = "planlama";
            return !MKFiloServis.Shared.Licensing.LicenseModules.All.ContainsKey(module) || HasModulePermission(module);
        }
        var required = prefix switch
        {
            "rentacar" => "rentacar",
            "cari" or "cariler" or "tahsilat" => "cari",
            "arac" or "araclar" or "guzergah" or "guzergahlar" or "servis" or "serviscalisma" or "toplucalisma"
                or "aracmasraf" or "filo" or "hakedis" or "tedarikciservis" or "tedarikciarac" or "tedarikcipersonel"
                or "tedarikciaracevrak" or "masrafkalem" or "aracsase" => "filoservis",
            "muhasebe" or "muhasebedash" or "muhasebefis" or "muhaseberapor" or "hesapplani" or "malianaliz" => "muhasebe",
            "personel" or "maas" or "izin" => "personel",
            "fatura" or "faturalar" or "kesilenfatura" or "gelenfatura" or "faturahazirlik" => "fatura",
            "banka" or "bankahesap" or "bankahareket" or "kasa" or "odemeeslestir" => "bankakasa",
            "butce" or "butceanaliz" or "odemeyonetim" or "tekrarlayanodem" or "odeme" => "butce",
            "bildirim" or "mesaj" or "email" or "whatsapp" or "hatirlatici" or "kullanicicari" => "crm",
            "rapor" or "raporlar" or "raporozmal" or "raporkirala" or "raporkomisyon" or "raporservis"
                or "raporfatura" or "rapormasraf" or "raporekstre" => "raporlar",
            "ebys" or "evrak" or "belgeuyari" or "arsivgoruntuleyici" => "ebys",
            "stok" => "stok",
            "holding" => "holding",
            "satis" or "satisdash" or "piyasa" or "piyasaarastir" or "ilan" => "satis",
            "planlama" or "checklist" or "checklistmali" => "planlama",
            _ => null
        };
        if (required != null) return HasModulePermission(required);
        var modules = Yetkiler.GetMenuYetkiGruplari()
            .Where(g => g.AltMenuler.Any(m => m.Yetkiler.Any(y => y.Kod == permission)))
            .Select(g => g.AnaMenuYetkiKodu.Replace("menu.", "", StringComparison.Ordinal))
            .Where(MKFiloServis.Shared.Licensing.LicenseModules.All.ContainsKey).Distinct().ToArray();
        return modules.Length == 0 || modules.Any(HasModulePermission);
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




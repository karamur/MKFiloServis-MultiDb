using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Shared.Licensing;
using MKFiloServis.Web.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Globalization;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Lisans servisi (v4). Karar mantığı Shared.Licensing.LicenseValidator'dadır; bu sınıf yalnız
/// veri erişimi, aktivasyon ve önbellek yönetimini yapar.
///
///   1. İmza        — RSA-PSS (v3 modüllü / v2), çoklu genel anahtar (rotasyon)
///   2. Makine      — Parmak izi v2 (4 bileşen, 1 değişikliğe tolerans); eski biçim de kabul edilir
///   3. Saat        — CreatedAt + son görülen zaman (DB + yerel durum dosyası), 1 saat tolerans
///   4. Süre        — 14 gün yenileme toleransı (yalnız daha önce doğrulanmış ticari lisans)
///   5. Demo        — Registry kilidi, 15 gün, yalnız SQLite demo veritabanı
///   6. Sürüm/Firma — AllowedVersion ve yapılandırılmış FirmaKodu kontrolü
///   7. Önbellek    — Doğrulanan modül kümesi bellekte tutulur; yetki sorguları RSA çalıştırmaz
/// Hiçbir kontrol yerel dosya yazılabilirliğine bağlı değildir (dosya hatası lisansı bozmaz).
/// </summary>
public class LicenseService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<LicenseService> _logger;
    private readonly LicenseCache _cache; // Singleton cache — request'ler arası yaşar
    private readonly DatabaseRuntimeInfo _databaseRuntime;
    private readonly IDataProtector _demoProtector;
    private readonly IWebHostEnvironment _env;
    private readonly IDataProtector _stateProtector;
    private const string RegistryPath = @"SOFTWARE\KOAFiloServis";
    private const string DemoUsedValueName = "DemoUsed";
    private const int DemoMaxDays = 15;
    private const int DemoButtonDays = 15;

    public LicenseService(IDbContextFactory<ApplicationDbContext> dbFactory, IConfiguration config, ILogger<LicenseService> logger, LicenseCache cache, DatabaseRuntimeInfo databaseRuntime, IDataProtectionProvider dataProtection, IWebHostEnvironment env)
    {
        _dbFactory = dbFactory;
        _config = config;
        _logger = logger;
        _cache = cache;
        _databaseRuntime = databaseRuntime;
        _demoProtector = dataProtection.CreateProtector("MKFiloServis.License.Demo.v1");
        _stateProtector = dataProtection.CreateProtector("MKFiloServis.License.State.v1");
        _env = env;
    }

    /// <summary>Development'ta varsayılan olarak tüm modüller açık sahte lisans kullanılır; Licensing:EnforceInDevelopment=true ile gerçek doğrulama denenir.</summary>
    private bool UseDevelopmentBypass
        => _env.IsDevelopment() && !_config.GetValue<bool>("Licensing:EnforceInDevelopment");

    // ══════════════════════════════════════════════
    // PART 1: REGISTRY LOCK — Demo sadece 1 kez
    // DB silinse bile REGISTRY'den kontrol edilir
    // ══════════════════════════════════════════════

    public static bool HasDemoBeenUsed()
    {
        if (!OperatingSystem.IsWindows())
            return true; // Demo lock has no durable registry backing on this platform.

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
            return key?.GetValue(DemoUsedValueName) != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LicenseService] Demo registry check failed: {ex.GetType().Name}");
            return true; // Fail closed: an unavailable lock must not grant a fresh demo.
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

    /// <summary>Lisans bağlama kimliği: parmak izi v2 (PC adı/kullanıcı adından bağımsız).</summary>
    public static string GetMachineId() => MKFiloServis.Shared.Licensing.MachineFingerprint.Current;

    /// <summary>Bu makinenin kabul edilen kimlikleri; eski biçimde bağlanmış lisanslar için ikinci sırada eski kimlik üretilir.</summary>
    private static IEnumerable<string> AcceptedMachineIds()
    {
        yield return GetMachineId();
        yield return GetLegacyMachineId();
    }

    private static string GetLegacyMachineId()
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
        try
        {
            if (!lic.IsDemo && MKFiloServis.Shared.Licensing.LicenseSignatureVerifier.VerifyRsa(
                    lic, MKFiloServis.Shared.Licensing.LicenseSigningPublicKey.All))
                return true;

            if (signature.StartsWith("demo:", StringComparison.Ordinal) && lic.IsDemo)
            {
                var original = _demoProtector.Unprotect(signature[5..]);
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(SignaturePayload(lic)), Encoding.UTF8.GetBytes(original));
            }

            if (signature.StartsWith("v2:", StringComparison.Ordinal) || signature.StartsWith("v3:", StringComparison.Ordinal))
                return false;

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
            _logger.LogWarning("Lisans imzasi dogrulanamadi: {Reason}", ex.GetType().Name);
            return false;
        }
    }

    // ══════════════════════════════════════════════
    // SON GÖRÜLEN ZAMAN (saat geri alma koruması)
    // Yerel, korumalı bir dosyada tutulur; DB geri yüklense bile saat geri alınması fark edilir.
    // Dosya okunamaz/yazılamazsa lisans etkilenmez (yalnızca ek koruma devre dışı kalır).
    // ══════════════════════════════════════════════

    private static IEnumerable<string> StatePaths()
    {
        foreach (var folder in new[] { Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolder.LocalApplicationData })
        {
            var root = Environment.GetFolderPath(folder);
            if (!string.IsNullOrWhiteSpace(root))
                yield return Path.Combine(root, "MKFiloServis", "license-state.bin");
        }
    }

    private DateTime? ReadLastSeenUtc()
    {
        DateTime? latest = null;
        foreach (var path in StatePaths())
        {
            try
            {
                if (!File.Exists(path)) continue;
                var text = _stateProtector.Unprotect(File.ReadAllText(path).Trim());
                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks)
                    && ticks > 0 && ticks <= DateTime.MaxValue.Ticks)
                {
                    var seen = new DateTime(ticks, DateTimeKind.Utc);
                    if (latest is null || seen > latest) latest = seen;
                }
            }
            catch (Exception ex)
            {
                // Anahtar halkası yenilenmiş veya dosya bozuk: bu ek koruma sessizce atlanır.
                _logger.LogDebug(ex, "Lisans durum dosyasi okunamadi: {Path}", path);
            }
        }
        return latest;
    }

    private void WriteLastSeenUtc(DateTime utc)
    {
        string payload;
        try
        {
            payload = _stateProtector.Protect(utc.Ticks.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Yerel lisans saat koruması yazılamadı; veritabanındaki doğrulama zamanı korunuyor.");
            return;
        }
        foreach (var path in StatePaths())
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, payload);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Lisans durum dosyasi yazilamadi: {Path}", path);
            }
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
    // CORE: VALIDATE — politika Shared.LicenseValidator'da
    // ══════════════════════════════════════════════

    private async Task PublishValidatedLicenseAsync(LicenseInfo lic)
    {
        var validation = await ValidateAsync(persistValidation: false);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message);
        WriteLastSeenUtc(DateTime.UtcNow);
    }

    private LicenseValidationResult FailValidation(MKFiloServis.Shared.Licensing.LicenseStatusCode code, string message)
    {
        _cache.Clear(new MKFiloServis.Shared.Licensing.LicenseEvaluation { Code = code, Message = message });
        return LicenseValidationResult.Fail(code, message);
    }

    private MKFiloServis.Shared.Licensing.LicenseValidationContext BuildContext(LicenseInfo? lic, DateTime? databaseLastSeen = null)
    {
        var lastSeen = ReadLastSeenUtc();
        if (lic?.LastValidatedAt is { } validated && (lastSeen is null || validated > lastSeen))
            lastSeen = validated;
        if (databaseLastSeen is { } databaseSeen && (lastSeen is null || databaseSeen > lastSeen))
            lastSeen = databaseSeen;

        return new MKFiloServis.Shared.Licensing.LicenseValidationContext
        {
            NowUtc = DateTime.UtcNow,
            MachineIds = AcceptedMachineIds(),
            AppVersion = GetAppVersion(),
            ExpectedCompanyCode = _config["FirmaKodu"],
            LastSeenUtc = lastSeen,
            DemoMaxDays = DemoMaxDays,
            VerifySignature = VerifySignature
        };
    }

    public async Task<LicenseValidationResult> ValidateAsync(bool persistValidation = true)
    {
        await _cache.ValidationGate.WaitAsync();
        try
        {
            return await ValidateCoreAsync(persistValidation);
        }
        finally
        {
            _cache.ValidationGate.Release();
        }
    }

    private async Task<LicenseValidationResult> ValidateCoreAsync(bool persistValidation)
    {
        // Development ortamında varsayılan olarak sahte lisans döner (EnforceInDevelopment ile kapatılabilir).
        if (UseDevelopmentBypass)
        {
            var mockLicense = await GetCurrentLicenseAsync();
            _cache.Set(mockLicense!, DevelopmentEvaluation());
            return LicenseValidationResult.Ok(mockLicense!);
        }

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
                if (hasAnyLicense)
                    return FailValidation(MKFiloServis.Shared.Licensing.LicenseStatusCode.Inactive,
                        "Veritabanında lisans kaydı var fakat aktif değil. Lisans anahtarını yeniden yükleyin.");

                return FailValidation(MKFiloServis.Shared.Licensing.LicenseStatusCode.NotFound,
                    HasDemoBeenUsed()
                        ? "Lisans bulunamadı. Demo hakkı kullanılmış; lisans anahtarı yükleyin."
                        : "Lisans bulunamadı. Giriş ekranından 15 günlük demo modunu başlatabilir veya lisans anahtarı yükleyebilirsiniz.");
            }

            // Registry'de demo kaydı yok ama DB'de demo lisans var: demo kilidi atlatılmaya çalışılmış olabilir.
            if (lic.IsDemo && !HasDemoBeenUsed())
            {
                _logger.LogCritical("Registry'de demo kaydi yok ama DB'de demo lisans var. Makine: {MachineId}", GetMachineId());
                return FailValidation(MKFiloServis.Shared.Licensing.LicenseStatusCode.SignatureInvalid,
                    "Demo lisans kaydı doğrulanamadı. Lisans anahtarını yükleyin.");
            }

            var evaluation = MKFiloServis.Shared.Licensing.LicenseValidator.Evaluate(lic, BuildContext(lic));
            if (!evaluation.IsValid)
            {
                _logger.LogWarning("Lisans reddedildi: {Code}, Makine: {MachineId}", evaluation.Code, GetMachineId());
                return FailValidation(evaluation.Code, evaluation.Message);
            }

            if (evaluation.InGrace)
                _logger.LogWarning("Lisans yenileme toleransinda: Firma={Firma}, Bitis={ExpireDate}",
                    lic.FirmaKodu, lic.ExpireDate);

            if (persistValidation)
            {
                lic.LastValidatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                WriteLastSeenUtc(DateTime.UtcNow);
            }

            // Doğrulanan modül kümesi önbelleğe alınır; yetki sorguları artık RSA çalıştırmaz.
            _cache.Set(lic, evaluation);

            return LicenseValidationResult.Ok(lic, evaluation.Code, evaluation.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lisans dogrulama hatasi");
            // Veritabanı erişimi gibi geçici bir hata, önceden doğrulanmış lisansı
            // silip açık formların tüm kullanıcılarda kapanmasına neden olmamalı.
            // Yeni giriş başarısız döner; mevcut oturum kendi durumunu korur.
            return LicenseValidationResult.Unavailable("Lisans doğrulanamadı. Sistem yöneticisine başvurun.");
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
            throw new InvalidOperationException("Demo hakkı kullanılmış veya doğrulanamıyor. Ticari lisans anahtarı girin.");

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
        key = NormalizeKeyInput(key);

        var lic = TryParseJsonLicenseKey(key);
        if (lic == null)
        {
            lic = TryParseLegacyEncryptedLicenseKey(key)
                  ?? throw new Exception("Lisans anahtari gecersiz. Yeni Base64 lisans veya eski sifreli lisans formati bekleniyor.");
        }

        if (string.IsNullOrWhiteSpace(lic.FirmaKodu) || string.IsNullOrWhiteSpace(lic.Signature))
            throw new Exception("Lisans anahtari eksik bilgi iceriyor.");

        if (!VerifySignature(lic))
            throw new Exception("Lisans imzası geçersiz. Anahtar eksik kopyalanmış veya değiştirilmiş olabilir; lisansı yeniden kopyalayın.");

        // Makine kodu parmak izi toleranslıdır; kökten farklı bir bilgisayar için üretilmiş lisans reddedilir.
        if (!IsSameMachineBinding(lic.MachineId))
            throw new Exception($"Bu lisans bu bilgisayar için geçerli değil. Bu bilgisayarın makine kodu: {GetMachineId()}");

        if (DateTime.UtcNow < lic.CreatedAt - TimeSpan.FromDays(1))
            throw new InvalidOperationException("Lisans oluşturma tarihi gelecekte görünüyor. Bu bilgisayarın tarih ve saat ayarını kontrol edin.");

        if (DateTime.UtcNow > MKFiloServis.Shared.Licensing.LicenseValidator.EffectiveExpiry(lic))
            throw new Exception($"Lisans süresi {lic.ExpireDate:yyyy-MM-dd} tarihinde dolmuş. Yeni lisans anahtarı alın.");

        if (!IsVersionAllowed(lic.AllowedVersion))
            throw new Exception($"Bu uygulama sürümü ({GetAppVersion()}) lisansa dahil değil. İzin verilen en yüksek sürüm: {lic.AllowedVersion}");

        // Gün sayısı ile tarih farkı tutarsızsa imza zaten bunları birlikte doğruladığı için müşteri engellenmez.
        if (lic.DurationDays > 0)
        {
            var expectedDays = (lic.ExpireDate.Date - lic.CreatedAt.Date).Days;
            if (Math.Abs(expectedDays - lic.DurationDays) > 2)
                _logger.LogWarning("Lisans sure tutarsizligi: DurationDays={Days}, tarih farki={Expected}", lic.DurationDays, expectedDays);
        }

        await _cache.ValidationGate.WaitAsync();
        try
        {
            await using var strategyDb = await _dbFactory.CreateDbContextAsync();
            var source = lic;
            var commitStarted = false;
            var activated = await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                if (commitStarted)
                    throw new InvalidOperationException("Lisans aktivasyonunun commit sonucu belirsiz. Lisans durumunu kontrol edip anahtarı yeniden yükleyin.");

                await using var db = await _dbFactory.CreateDbContextAsync();
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                var candidate = new LicenseInfo
                {
                    FirmaKodu = source.FirmaKodu,
                    MachineId = source.MachineId,
                    ExpireDate = source.ExpireDate,
                    DurationDays = source.DurationDays,
                    IsDemo = source.IsDemo,
                    AllowedVersion = source.AllowedVersion,
                    CreatedAt = source.CreatedAt,
                    ContactPhone = source.ContactPhone,
                    Signature = source.Signature,
                    IsActive = true
                };
                var databaseLastSeen = await db.LicenseInfos.IgnoreQueryFilters()
                    .MaxAsync(l => l.LastValidatedAt);
                var evaluation = MKFiloServis.Shared.Licensing.LicenseValidator.Evaluate(candidate, BuildContext(candidate, databaseLastSeen));
                if (!evaluation.IsValid)
                    throw new InvalidOperationException(evaluation.Message);

                candidate = await StoreActivatedLicenseAsync(db, candidate);
                await db.Entry(candidate).ReloadAsync();
                evaluation = MKFiloServis.Shared.Licensing.LicenseValidator.Evaluate(candidate, BuildContext(candidate, databaseLastSeen));
                if (!evaluation.IsValid)
                    throw new InvalidOperationException(evaluation.Message);

                commitStarted = true;
                await transaction.CommitAsync();
                return (License: candidate, Evaluation: evaluation);
            });

            _cache.Set(activated.License, activated.Evaluation);
            WriteLastSeenUtc(DateTime.UtcNow);
            MKFiloServis.Shared.AppMode.ExitDemoMode();
            _logger.LogInformation("✅ Lisans aktive edildi (key): {FirmaKodu}, Bitis: {ExpireDate}",
                activated.License.FirmaKodu, activated.License.ExpireDate);
            return activated.License;
        }
        finally
        {
            _cache.ValidationGate.Release();
        }
    }

    private async Task<LicenseInfo> StoreActivatedLicenseAsync(ApplicationDbContext db, LicenseInfo lic)
    {

        // Mevcut kurulumda firma kodu eşleşmiyorsa lisansı başka firmaya bağlama.
        // Yalnızca henüz hiç firma kaydı olmayan temiz kurulum lisans bilgisiyle ilk firmayı kurabilir.
        var firma = await ResolveFirmaForLicenseAsync(db, lic.FirmaKodu);
        if (firma == null)
        {
            var mevcutFirmaVar = await db.Firmalar.IgnoreQueryFilters()
                .AnyAsync(f => !f.IsDeleted);
            if (mevcutFirmaVar)
            {
                var activeCompanyCodes = await db.Firmalar.IgnoreQueryFilters()
                    .Where(f => !f.IsDeleted && f.Aktif && f.FirmaKodu != null && f.FirmaKodu != "")
                    .OrderBy(f => f.FirmaAdi)
                    .Select(f => f.FirmaKodu)
                    .Distinct()
                    .ToListAsync();
                var expectedCodes = activeCompanyCodes.Count == 0
                    ? "kayıtlı aktif firma kodu yok"
                    : string.Join(", ", activeCompanyCodes);
                throw new InvalidOperationException(
                    $"Lisans firma kodu ('{lic.FirmaKodu}') aktif bir firma koduyla eşleşmiyor. " +
                    $"Bu kurulumdaki aktif firma kodları: {expectedCodes}. Lisansın bu kodlardan biri ve bu makine kodu için yeniden üretilmesi gerekir.");
            }

            firma = await CreateFirmaFromLicenseAsync(db, lic);
        }

        // 🔥 KRİTİK: FirmaKodu'nu DEGISTIRME! Signature, anahtar icindeki orijinal
        // FirmaKodu ile uretildi. Degistirirsek sonraki ValidateAsync/VerifySignature
        // dongusunde imza uyusmaz ve lisans "gecersiz" olarak reddedilir.
        // Sadece FirmaId iliskisini kur.
        lic.FirmaId = firma.Id;

        var activeLicenses = await db.LicenseInfos
            .IgnoreQueryFilters()
            .Where(l => l.IsActive)
            .ToListAsync();
        foreach (var activeLicense in activeLicenses)
        {
            activeLicense.IsActive = false;
            activeLicense.UpdatedAt = DateTime.UtcNow;
        }

        // Re-uploading the same signed key reuses its row. That keeps activation
        // idempotent if a transient database retry happens around SaveChanges.
        var existingLicense = await db.LicenseInfos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => !l.IsDeleted && l.Signature == lic.Signature && l.MachineId == lic.MachineId);
        if (existingLicense is not null)
        {
            // Anahtar (Id) değerleri aynı olmalı; yoksa EF birincil anahtarı değiştirmeye çalışıp hata verir.
            lic.Id = existingLicense.Id;
            db.Entry(existingLicense).CurrentValues.SetValues(lic);
            lic = existingLicense;
        }

        lic.IsActive = true;
        lic.LastValidatedAt = DateTime.UtcNow;
        lic.UpdatedAt = DateTime.UtcNow;
        if (existingLicense is null)
        {
            lic.Id = 0;
            db.LicenseInfos.Add(lic);
        }
        await db.SaveChangesAsync();

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
                NormalizeFirmaMatchValue(f.FirmaKodu) == normalizedLicenseValue).ToList();
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
            AktifDonemYil = MKFiloServis.Shared.Time.BusinessTime.Today.Year,
            AktifDonemAy = MKFiloServis.Shared.Time.BusinessTime.Today.Month,
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
                if (!IsSameMachineBinding(lisansMakineKodu))
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

    private static bool IsSameMachineBinding(string? licenseMachineId)
        => MKFiloServis.Shared.Licensing.LicenseValidator.MachineAllowed(licenseMachineId, AcceptedMachineIds());

    /// <summary>Anahtar kutusuna yapıştırılan metinden lisans anahtarını ayıklar ("LicenseKey=" satırı, tırnak, boşluk, satır sonu).</summary>
    public static string NormalizeKeyInput(string? input)
    {
        var text = (input ?? string.Empty).Trim().Trim('\uFEFF');
        var marker = text.LastIndexOf("LicenseKey=", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0) text = text[(marker + "LicenseKey=".Length)..];
        return new string(text.Where(c => !char.IsWhiteSpace(c) && c != '"' && c != '\'' && c != '\uFEFF').ToArray());
    }

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
            var lic = JsonSerializer.Deserialize<LicenseInfo>(json);
            if (lic == null) return null;

            // İmza tarihleri takvim günü olarak alır; "+03:00" gibi ofsetli yazılmış değerler
            // saat dilimi dönüşümüyle bir gün kayıp imzayı bozmasın diye yazıldığı gibi okunur.
            using var doc = JsonDocument.Parse(json);
            lic.ExpireDate = ReadLiteralDate(doc.RootElement, nameof(LicenseInfo.ExpireDate), lic.ExpireDate);
            lic.CreatedAt = ReadLiteralDate(doc.RootElement, nameof(LicenseInfo.CreatedAt), lic.CreatedAt);
            return lic;
        }
        catch
        {
            return null;
        }
    }

    private static DateTime ReadLiteralDate(JsonElement root, string name, DateTime fallback)
    {
        foreach (var p in root.EnumerateObject())
        {
            if (!string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) || p.Value.ValueKind != JsonValueKind.String)
                continue;
            var text = p.Value.GetString();
            if (text is { Length: >= 10 } &&
                DateTime.TryParse(text.Length >= 19 ? text[..19] : text[..10], CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var literal))
                return DateTime.SpecifyKind(literal, DateTimeKind.Utc);
        }
        return fallback;
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
        // Development ortamında varsayılan olarak mock lisans döndür
        if (UseDevelopmentBypass)
        {
            var mockLicense = new LicenseInfo
            {
                Id = 1,
                FirmaKodu = "DEV",
                MachineId = GetMachineId(),
                ExpireDate = DateTime.UtcNow.AddYears(10),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                IsDeleted = false,
                IsDemo = false,
                AllowedVersion = "10.0.0",
                Signature = "mock_dev_signature",
                LastValidatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            return mockLicense;
        }

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

    public LicenseEvaluation EvaluateCachedLicense() => UseDevelopmentBypass
        ? DevelopmentEvaluation()
        : _cache.GetEvaluation() ?? new LicenseEvaluation
        {
            Code = LicenseStatusCode.NotFound,
            Message = "Lisans henüz doğrulanmadı. Doğrulamak için giriş yapın."
        };

    private static LicenseEvaluation DevelopmentEvaluation() => new()
    {
        Code = LicenseStatusCode.Ok,
        Modules = new HashSet<string>(LicenseModules.All.Keys, StringComparer.Ordinal)
    };

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
        // Development bypass: tüm modüller açık
        if (UseDevelopmentBypass)
            return true;

        if (!MKFiloServis.Shared.Licensing.LicenseModules.All.ContainsKey(moduleName)) return false;

        // Modül kontrolleri lisansı yeniden doğrulamaz; girişte belirlenen hakları kullanır.
        return _cache.GetSnapshot()?.Modules.Contains(moduleName) == true;
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
    public bool IsTransientFailure { get; set; }
    public MKFiloServis.Shared.Licensing.LicenseStatusCode Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public LicenseInfo? License { get; set; }

    public static LicenseValidationResult Ok(LicenseInfo lic, MKFiloServis.Shared.Licensing.LicenseStatusCode code = MKFiloServis.Shared.Licensing.LicenseStatusCode.Ok, string message = "")
        => new() { IsValid = true, Code = code, Message = message, License = lic };
    public static LicenseValidationResult Fail(MKFiloServis.Shared.Licensing.LicenseStatusCode code, string msg) => new() { IsValid = false, Code = code, Message = msg };
    public static LicenseValidationResult Unavailable(string msg) => new() { IsValid = false, IsTransientFailure = true, Code = MKFiloServis.Shared.Licensing.LicenseStatusCode.Unavailable, Message = msg };
}




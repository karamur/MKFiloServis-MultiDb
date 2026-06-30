using System.Security.Cryptography;

namespace MKFiloServis.Web.Services.Security;

/// <summary>
/// Master key'i <c>{storageRoot}\keys\master.key</c> icinde DPAPI (LocalMachine) ile sifreli tutar.
/// Anahtar yoksa rastgele 32 byte uretip saklar. Thread-safe, singleton kullanima uygundur.
///
/// Platform: Windows. Linux/Mac uretimde calismaz (DPAPI yok) - o senaryoda
/// alternatif bir IMasterKeyProvider yazilip DI'da swap edilmelidir.
/// </summary>
public sealed class DpapiMasterKeyProvider : IMasterKeyProvider
{
    private const int KeyLength = 32; // AES-256
    private static readonly byte[] Entropy = "MKFiloServis.MasterKey.v1"u8.ToArray();
    private static readonly string[] LegacyRawKeyFileNames = [
        "raw-key.txt",
        "raw-key.txt.bak",
        "master.key.raw",
        "master.key.txt"
    ];

    private readonly string _keyFilePath;
    private readonly ILogger<DpapiMasterKeyProvider> _logger;
    private readonly bool _throwOnMissing;
    private readonly Lock _lock = new();
    private byte[]? _cachedKey;

    /// <summary>
    /// DpapiMasterKeyProvider oluşturur.
    /// </summary>
    /// <param name="keyFilePath">Master key dosyasının tam yolu.</param>
    /// <param name="logger">Log kaydı için logger.</param>
    /// <param name="throwOnMissing">
    /// true (Production): Key yoksa veya çözülemiyorsa yeni key üretmez, kritik exception fırlatır.
    /// false (Development): Eksik/bozuk key durumunda otomatik yeni key üretir.
    /// </param>
    public DpapiMasterKeyProvider(string keyFilePath, ILogger<DpapiMasterKeyProvider> logger, bool throwOnMissing = false)
    {
        _keyFilePath = keyFilePath ?? throw new ArgumentNullException(nameof(keyFilePath));
        _logger = logger;
        _throwOnMissing = throwOnMissing;
    }

    public ReadOnlyMemory<byte> GetMasterKey()
    {
        if (_cachedKey is not null)
        {
            return _cachedKey;
        }

        lock (_lock)
        {
            if (_cachedKey is not null)
            {
                return _cachedKey;
            }

            _cachedKey = LoadOrCreate();
            return _cachedKey;
        }
    }

    private byte[] LoadOrCreate()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "DpapiMasterKeyProvider sadece Windows'ta calisir. Linux/Mac icin alternatif bir IMasterKeyProvider kullanin.");
        }

        var dir = Path.GetDirectoryName(_keyFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (File.Exists(_keyFilePath))
        {
            var protectedBytes = File.ReadAllBytes(_keyFilePath);
            byte[]? plain = null;
            CryptographicException? localMachineEx = null;
            CryptographicException? currentUserEx = null;

            try
            {
                plain = System.Security.Cryptography.ProtectedData.Unprotect(
                    protectedBytes, Entropy, DataProtectionScope.LocalMachine);
                _logger.LogDebug("Master key LocalMachine scope ile başarıyla yüklendi.");
            }
            catch (CryptographicException ex)
            {
                localMachineEx = ex;
                _logger.LogDebug("LocalMachine scope başarısız, CurrentUser denenecek: {Exception}", ex.Message);
            }

            if (plain == null)
            {
                try
                {
                    plain = System.Security.Cryptography.ProtectedData.Unprotect(
                        protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                    _logger.LogWarning("⚠️ Master key CurrentUser scope ile yüklendi (eski ortam fallback). Dosyalar kurtarılabilir ama yeni key'e migration önerilir.");
                }
                catch (CryptographicException ex)
                {
                    currentUserEx = ex;
                    _logger.LogDebug("CurrentUser scope da başarısız: {Exception}", ex.Message);
                }
            }

            if (plain != null)
            {
                if (plain.Length == KeyLength)
                {
                    _logger.LogInformation("Master key yuklendi: {Path}", _keyFilePath);
                    return plain;
                }

                _logger.LogWarning("Master key beklenmeyen uzunlukta ({Len} byte)", plain.Length);
                if (_throwOnMissing)
                    throw new InvalidOperationException(
                        $"KRİTİK: master.key beklenmeyen uzunlukta ({plain.Length} byte). " +
                        $"Evraklar açılamaz. Path={_keyFilePath}. " +
                        $"Orijinal key yedeğini geri yükleyin.");
            }
            else
            {
                if (TryLoadLegacyRawKey(out var legacyRawKey, out var source))
                {
                    _logger.LogWarning(
                        "Master key DPAPI ile cozulemedi fakat legacy raw key bulundu ({Source}). Anahtar yeniden DPAPI(LocalMachine) ile saklanacak.",
                        source);

                    PersistWithLocalMachineDpapi(legacyRawKey);
                    return legacyRawKey;
                }

                var errors = new List<Exception>();
                if (localMachineEx != null) errors.Add(localMachineEx);
                if (currentUserEx != null) errors.Add(currentUserEx);
                var combined = new AggregateException(
                    "DPAPI LocalMachine ve CurrentUser scope denemeleri basarisiz.",
                    errors);

                if (_throwOnMissing)
                    throw new InvalidOperationException(
                        $"KRİTİK: master.key DPAPI ile çözülemedi (LocalMachine/CurrentUser). " +
                        $"Evraklar açılamaz, yeni key otomatik üretilemez. Path={_keyFilePath}. " +
                        $"Orijinal key yedeğini geri yükleyin.", combined);

                _logger.LogError(combined,
                    "Master key cozulemedi (LocalMachine/CurrentUser): {Path}. Yenisi uretiliyor.",
                    _keyFilePath);
                // Bozuk dosyayi yedekle
                var backup = _keyFilePath + ".corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                try { File.Move(_keyFilePath, backup); } catch { /* yoksay */ }
            }
        }
        else if (_throwOnMissing)
        {
            throw new InvalidOperationException(
                $"KRİTİK: master.key bulunamadı. " +
                $"Evraklar açılamaz, yeni key otomatik üretilemez. Path={_keyFilePath}. " +
                $"Orijinal key yedeğini geri yükleyin.");
        }

        // Yeni anahtar uret (sadece Development / key yoksa)
        var fresh = RandomNumberGenerator.GetBytes(KeyLength);
        PersistWithLocalMachineDpapi(fresh);
        _logger.LogInformation("Yeni master key olusturuldu: {Path}", _keyFilePath);
        return fresh;
    }

    private void PersistWithLocalMachineDpapi(byte[] key)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "DPAPI LocalMachine sadece Windows'ta desteklenir.");
        }

        var protectedBytes = System.Security.Cryptography.ProtectedData.Protect(
            key, Entropy, DataProtectionScope.LocalMachine);
        File.WriteAllBytes(_keyFilePath, protectedBytes);
    }

    private bool TryLoadLegacyRawKey(out byte[] key, out string source)
    {
        key = Array.Empty<byte>();
        source = string.Empty;

        var keyDir = Path.GetDirectoryName(_keyFilePath);
        if (string.IsNullOrWhiteSpace(keyDir))
            return false;

        foreach (var fileName in LegacyRawKeyFileNames)
        {
            var candidate = Path.Combine(keyDir, fileName);
            if (!File.Exists(candidate))
                continue;

            var content = File.ReadAllText(candidate).Trim();
            if (TryParseRawKey(content, out key))
            {
                source = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryParseRawKey(string content, out byte[] key)
    {
        key = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var compact = string.Concat(content.Where(c => !char.IsWhiteSpace(c)));

        if (compact.Length == KeyLength * 2)
        {
            try
            {
                key = Convert.FromHexString(compact);
                return key.Length == KeyLength;
            }
            catch (FormatException)
            {
                // Hex degilse Base64 denenecek
            }
        }

        try
        {
            key = Convert.FromBase64String(compact);
            return key.Length == KeyLength;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}





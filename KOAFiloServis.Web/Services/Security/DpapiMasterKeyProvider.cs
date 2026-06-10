using System.Security.Cryptography;

namespace KOAFiloServis.Web.Services.Security;

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
    private static readonly byte[] Entropy = "KOAFiloServis.MasterKey.v1"u8.ToArray();

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
            }
            catch (CryptographicException ex)
            {
                localMachineEx = ex;
            }

            if (plain == null)
            {
                try
                {
                    plain = System.Security.Cryptography.ProtectedData.Unprotect(
                        protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                    _logger.LogWarning("Master key CurrentUser scope ile yuklendi: {Path}", _keyFilePath);
                }
                catch (CryptographicException ex)
                {
                    currentUserEx = ex;
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
        var protectedFresh = System.Security.Cryptography.ProtectedData.Protect(
            fresh, Entropy, DataProtectionScope.LocalMachine);
        File.WriteAllBytes(_keyFilePath, protectedFresh);
        _logger.LogInformation("Yeni master key olusturuldu: {Path}", _keyFilePath);
        return fresh;
    }
}

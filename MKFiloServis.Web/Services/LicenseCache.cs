using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

/// <summary>Tam doğrulanmış lisansın bellek anlık görüntüsü: kayıt ve doğrulanan modül kümesi.</summary>
public sealed record LicenseSnapshot(LicenseInfo License, IReadOnlySet<string> Modules);

/// <summary>
/// Singleton lisans cache'i.
/// LicenseService (Scoped) tarafından yazılır, tüm component'ler tarafından okunur.
/// App.razor (static SSR) dahil her yerden inject edilebilir.
/// </summary>
public class LicenseCache
{
    private readonly object _lock = new();
    private static readonly IReadOnlySet<string> NoModules = new HashSet<string>();
    private IReadOnlySet<string> _modules = NoModules;
    private LicenseInfo? _cachedLicense;
    private bool _validated;
    public event Action? Changed;

    /// <summary>Cache'teki lisansı getir/set et (thread-safe).</summary>
    public LicenseInfo? Get()
    {
        lock (_lock) return _cachedLicense;
    }

    /// <summary>Okunmuş kayıt yeterli değildir; yalnız tam doğrulanmış kayıt erişim açabilir.</summary>
    public LicenseInfo? GetValidated()
    {
        lock (_lock) return _validated ? _cachedLicense : null;
    }

    /// <summary>Tam doğrulanmış lisans ve modülleri; doğrulanmamışsa null.</summary>
    public LicenseSnapshot? GetSnapshot()
    {
        lock (_lock) return _validated && _cachedLicense != null ? new LicenseSnapshot(_cachedLicense, _modules) : null;
    }

    /// <summary>Cache'i güncelle (thread-safe).</summary>
    public void Set(LicenseInfo lic, bool validated = false, IReadOnlySet<string>? modules = null)
    {
        bool changed;
        lock (_lock)
        {
            changed = _cachedLicense?.Signature != lic.Signature || _cachedLicense?.IsActive != lic.IsActive || _validated != validated;
            _cachedLicense = lic;
            _validated = validated;
            _modules = validated ? modules ?? NoModules : NoModules;
        }
        if (changed) Changed?.Invoke();
    }

    /// <summary>Cache'i temizle (uygulama başlangıcında DB'den yeniden doğrulamak için).</summary>
    public void Clear()
    {
        bool changed;
        lock (_lock)
        {
            changed = _cachedLicense != null || _validated;
            _cachedLicense = null;
            _modules = NoModules;
            _validated = false;
        }
        if (changed) Changed?.Invoke();
    }

    /// <summary>Geçerli lisans var mı? DB sorgusu yapmaz, sadece cache'e bakar.</summary>
    public bool HasValidLicense()
    {
        lock (_lock)
            return _validated && _cachedLicense != null && _cachedLicense.IsActive && !_cachedLicense.IsDeleted;
    }

    /// <summary>Kalan gün sayısı.</summary>
    public int GetRemainingDays()
    {
        LicenseInfo? lic;
        lock (_lock) lic = _cachedLicense;
        if (lic == null) return 0;
        return LicenseService.GetRemainingDays(lic);
    }
}



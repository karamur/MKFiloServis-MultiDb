using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Singleton lisans cache'i.
/// LicenseService (Scoped) tarafından yazılır, tüm component'ler tarafından okunur.
/// App.razor (static SSR) dahil her yerden inject edilebilir.
/// </summary>
public class LicenseCache
{
    private readonly object _lock = new();
    private LicenseInfo? _cachedLicense;

    /// <summary>Cache'teki lisansı getir/set et (thread-safe).</summary>
    public LicenseInfo? Get()
    {
        lock (_lock) return _cachedLicense;
    }

    /// <summary>Cache'i güncelle (thread-safe).</summary>
    public void Set(LicenseInfo lic)
    {
        lock (_lock) _cachedLicense = lic;
    }

    /// <summary>Cache'i temizle (uygulama başlangıcında DB'den yeniden doğrulamak için).</summary>
    public void Clear()
    {
        lock (_lock) _cachedLicense = null;
    }

    /// <summary>Geçerli lisans var mı? DB sorgusu yapmaz, sadece cache'e bakar.</summary>
    public bool HasValidLicense()
    {
        LicenseInfo? lic;
        lock (_lock) lic = _cachedLicense;
        return lic != null && lic.IsActive && !lic.IsDeleted;
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



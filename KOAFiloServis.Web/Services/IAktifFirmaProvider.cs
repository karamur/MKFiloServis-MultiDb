using KOAFiloServis.Shared.Entities;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Aktif (seçili) firmayı per-user / per-circuit tutar.
/// <para>
/// Blazor Server'da <b>Scoped</b> olarak kaydedilir; her circuit (kullanıcı oturumu)
/// kendi aktif firma bilgisine sahip olur. <see cref="FirmaService"/> içindeki eski
/// <c>static</c> yaklaşımın aksine farklı kullanıcılar birbirinin firmasını <b>görmez</b>.
/// </para>
/// <para>
/// Aktif firma bilgisi ApplicationDbContext'in global query filter'ı ve SaveChanges
/// interceptor'u tarafından da okunur, bu sayede tenant izolasyonu otomatik sağlanır.
/// </para>
/// </summary>
public interface IAktifFirmaProvider
{
    /// <summary>
    /// Aktif firmanın Id'si. 0 veya null ise henüz firma seçilmemiştir.
    /// </summary>
    int? AktifFirmaId { get; }

    /// <summary>
    /// "Tüm firmalar" modu (SuperAdmin / yönetici için cross-tenant rapor).
    /// True iken global query filter devre dışı bırakılır.
    /// </summary>
    bool TumFirmalar { get; }

    /// <summary>
    /// Aktif firmanın tüm bilgisi (Id, kod, ad, dönem).
    /// </summary>
    AktifFirmaBilgisi Mevcut { get; }

    /// <summary>
    /// Aktif firmayı değiştirir. Login sonrası firma seçim ekranı veya üst bardaki
    /// firma değiştiriciden çağrılır.
    /// </summary>
    void Set(AktifFirmaBilgisi firma);

    /// <summary>
    /// "Tüm firmalar" modunu açar/kapatır.
    /// </summary>
    void SetTumFirmalar(bool tumFirmalar);

    /// <summary>
    /// Aktif dönem (yıl/ay) günceller. Firma kaydındaki dönem alanını da senkronlamak
    /// FirmaService.SetAktifDonem'in sorumluluğundadır.
    /// </summary>
    void SetDonem(int yil, int ay);

    /// <summary>
    /// Aktif firma değiştiğinde tetiklenir (UI yenileme, cache invalidation vb. için).
    /// </summary>
    event Action? AktifFirmaDegisti;

    /// <summary>
    /// Tarayıcı/circuit yeniden bağlandığında daha önce seçilmiş firmayı
    /// <see cref="ProtectedLocalStorage"/> üzerinden geri yükler.
    /// Sadece interaktif render bağlamında (OnAfterRender first render) çağrılmalıdır.
    /// </summary>
    /// <returns>Restore başarılı ise true.</returns>
    Task<bool> TryRestoreAsync();
}

/// <summary>
/// <see cref="IAktifFirmaProvider"/> default implementasyonu.
/// <para>
/// Per-circuit in-memory state tutar; ayrıca <see cref="ProtectedLocalStorage"/>
/// üzerinden tarayıcıda da kalıcı saklar. Böylece circuit reset / sayfa kapatma
/// sonrası kullanıcı yine aynı firmaya devam eder, varsayılan firmaya düşmez.
/// </para>
/// </summary>
public sealed class AktifFirmaProvider : IAktifFirmaProvider
{
    private const string StorageKey = "koa.aktifFirma.v1";

    private readonly ProtectedLocalStorage _storage;
    private readonly ILogger<AktifFirmaProvider> _logger;
    private AktifFirmaBilgisi _mevcut = new();

    public AktifFirmaProvider(ProtectedLocalStorage storage, ILogger<AktifFirmaProvider> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public int? AktifFirmaId => _mevcut.FirmaId > 0 ? _mevcut.FirmaId : null;

    public bool TumFirmalar => _mevcut.TumFirmalar;

    public AktifFirmaBilgisi Mevcut => _mevcut;

    public event Action? AktifFirmaDegisti;

    public void Set(AktifFirmaBilgisi firma)
    {
        _mevcut = firma ?? new AktifFirmaBilgisi();
        AktifFirmaDegisti?.Invoke();
        _ = PersistAsync();
    }

    public void SetTumFirmalar(bool tumFirmalar)
    {
        _mevcut.TumFirmalar = tumFirmalar;
        AktifFirmaDegisti?.Invoke();
        _ = PersistAsync();
    }

    public void SetDonem(int yil, int ay)
    {
        _mevcut.AktifDonemYil = yil;
        _mevcut.AktifDonemAy = ay;
        AktifFirmaDegisti?.Invoke();
        _ = PersistAsync();
    }

    public async Task<bool> TryRestoreAsync()
    {
        try
        {
            var sonuc = await _storage.GetAsync<AktifFirmaBilgisi>(StorageKey);
            if (!sonuc.Success || sonuc.Value == null)
                return false;

            var bilgi = sonuc.Value;
            // Geçerli bir firma seçimi mi? FirmaId > 0 veya TumFirmalar modu olmalı.
            if (bilgi.FirmaId <= 0 && !bilgi.TumFirmalar)
                return false;

            _mevcut = bilgi;
            AktifFirmaDegisti?.Invoke();
            return true;
        }
        catch (InvalidOperationException)
        {
            // Prerender veya non-interactive bağlam: storage'a erişilemez.
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AktifFirmaProvider TryRestoreAsync hata");
            return false;
        }
    }

    private async Task PersistAsync()
    {
        try
        {
            await _storage.SetAsync(StorageKey, _mevcut);
        }
        catch (InvalidOperationException)
        {
            // Prerender / non-interactive bağlamda yazma yok sayılır; bir sonraki
            // Set çağrısı interaktif circuit'te tetiklenecek.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AktifFirmaProvider PersistAsync hata");
        }
    }
}

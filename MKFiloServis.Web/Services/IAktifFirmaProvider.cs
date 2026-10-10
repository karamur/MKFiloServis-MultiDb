using MKFiloServis.Shared.Entities;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Services;

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
    /// Aktif firmanın Id'si. null ise henüz firma seçilmemiştir.
    /// Kural 11: Firma seçilmeden ERP modüllerine erişilemez.
    /// </summary>
    int? AktifFirmaId { get; }

    /// <summary>
    /// Kullanıcı geçerli bir firma seçmiş mi? (Kural 11 guard için)
    /// FirmaId &gt; 0 veya TumFirmalar modu aktifse true döner.
    /// </summary>
    bool HasAktifFirma { get; }

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
    private readonly AppAuthenticationStateProvider _authenticationStateProvider;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private AktifFirmaBilgisi _mevcut = new();

    public AktifFirmaProvider(ProtectedLocalStorage storage, ILogger<AktifFirmaProvider> logger,
        AppAuthenticationStateProvider authenticationStateProvider,
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _storage = storage;
        _logger = logger;
        _authenticationStateProvider = authenticationStateProvider;
        _contextFactory = contextFactory;
    }

    public int? AktifFirmaId => _mevcut.FirmaId > 0 ? _mevcut.FirmaId : null;

    /// <summary>
    /// Kural 11: Kullanıcı firma seçmeden ERP modüllerine erişemez.
    /// Guard olarak kullanılır — UI katmanı /firma-sec'e yönlendirir.
    /// </summary>
    public bool HasAktifFirma => _mevcut.FirmaId > 0 || _mevcut.TumFirmalar;

    public bool TumFirmalar => _mevcut.TumFirmalar;

    public AktifFirmaBilgisi Mevcut => _mevcut;

    public event Action? AktifFirmaDegisti;

    public void Set(AktifFirmaBilgisi firma)
    {
        firma ??= new AktifFirmaBilgisi();
        var activeUser = _authenticationStateProvider.GetAktifKullanici();
        if (firma.FirmaId > 0)
        {
            if (activeUser == null)
            {
                _logger.LogWarning("Oturumsuz kullanıcı için firma kapsamı seçimi reddedildi.");
                return;
            }

            using var db = _contextFactory.CreateDbContext();
            var user = db.Kullanicilar.AsNoTracking().Include(x => x.Rol)
                .FirstOrDefault(x => x.Id == activeUser.Id && x.Aktif && !x.IsDeleted);
            var locked = user?.Kilitli == true &&
                (user.KilitlenmeBitisUtc is null || user.KilitlenmeBitisUtc > DateTime.UtcNow);
            var isAdmin = string.Equals(user?.Rol?.RolAdi, "Admin", StringComparison.Ordinal);
            int? defaultFirmId = null;
            if (!isAdmin)
            {
                defaultFirmId = db.Firmalar.AsNoTracking()
                    .Where(item => item.Aktif && !item.IsDeleted)
                    .OrderByDescending(item => item.VarsayilanFirma)
                    .ThenBy(item => item.SiraNo).ThenBy(item => item.FirmaAdi)
                    .Select(item => (int?)item.Id).FirstOrDefault();
            }

            if (user == null || locked || user.Rol?.IsDeleted == true ||
                !AuthenticationSessionPolicy.AllowsFirmScopeSelection(
                    true, isAdmin, firma.FirmaId, defaultFirmId))
            {
                _logger.LogWarning("Kullanıcının yetkili firma kapsamı dışındaki seçim reddedildi. KullaniciId: {UserId}, FirmaId: {FirmId}",
                    activeUser.Id, firma.FirmaId);
                return;
            }

            var selectedFirm = db.Firmalar.AsNoTracking()
                .FirstOrDefault(item => item.Id == firma.FirmaId && item.Aktif && !item.IsDeleted);
            if (selectedFirm == null)
            {
                _logger.LogWarning("Etkin olmayan veya bulunmayan firma seçimi reddedildi. FirmaId: {FirmId}", firma.FirmaId);
                return;
            }

            _mevcut = new AktifFirmaBilgisi
            {
                FirmaId = selectedFirm.Id,
                FirmaKodu = selectedFirm.FirmaKodu,
                FirmaAdi = selectedFirm.FirmaAdi,
                AktifDonemYil = selectedFirm.AktifDonemYil,
                AktifDonemAy = selectedFirm.AktifDonemAy,
                DatabaseName = selectedFirm.DatabaseName,
                TumFirmalar = false,
                KullaniciId = user.Id
            };
        }
        else
        {
            _mevcut = new AktifFirmaBilgisi { KullaniciId = activeUser?.Id };
        }
        AktifFirmaDegisti?.Invoke();
        _ = PersistAsync();
    }

    public void SetTumFirmalar(bool tumFirmalar)
    {
        var activeUser = _authenticationStateProvider.GetAktifKullanici();
        if (tumFirmalar)
        {
            if (activeUser == null) return;
            using var db = _contextFactory.CreateDbContext();
            var currentUser = db.Kullanicilar.AsNoTracking().Include(x => x.Rol)
                .FirstOrDefault(x => x.Id == activeUser.Id && x.Aktif && !x.IsDeleted);
            var isLocked = currentUser?.Kilitli == true &&
                (currentUser.KilitlenmeBitisUtc is null || currentUser.KilitlenmeBitisUtc > DateTime.UtcNow);
            if (isLocked || currentUser?.Rol?.IsDeleted != false ||
                !string.Equals(currentUser.Rol.RolAdi, "Admin", StringComparison.Ordinal))
            {
                _logger.LogWarning("Tüm-firmalar kapsamı güncel DB rolü Admin olmayan kullanıcı için reddedildi.");
                return;
            }
        }
        _mevcut.TumFirmalar = tumFirmalar;
        _mevcut.KullaniciId = activeUser?.Id;
        AktifFirmaDegisti?.Invoke();
        _ = PersistAsync();
    }

    public void SetDonem(int yil, int ay)
    {
        _mevcut.AktifDonemYil = yil;
        _mevcut.AktifDonemAy = ay;
        _mevcut.KullaniciId = _authenticationStateProvider.GetAktifKullanici()?.Id;
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
            var activeUser = _authenticationStateProvider.GetAktifKullanici();
            if (activeUser == null || bilgi.KullaniciId != activeUser.Id)
            {
                await _storage.DeleteAsync(StorageKey);
                _mevcut = new AktifFirmaBilgisi();
                AktifFirmaDegisti?.Invoke();
                return false;
            }

            await using var db = await _contextFactory.CreateDbContextAsync();
            var user = await db.Kullanicilar.AsNoTracking().Include(x => x.Rol)
                .FirstOrDefaultAsync(x => x.Id == activeUser.Id && x.Aktif && !x.IsDeleted);
            var locked = user?.Kilitli == true &&
                (user.KilitlenmeBitisUtc is null || user.KilitlenmeBitisUtc > DateTime.UtcNow);
            var isAdmin = string.Equals(user?.Rol?.RolAdi, "Admin", StringComparison.Ordinal);
            int? defaultFirmaId = null;
            if (!isAdmin)
            {
                defaultFirmaId = await db.Firmalar.AsNoTracking()
                    .Where(firma => firma.Aktif && !firma.IsDeleted)
                    .OrderByDescending(firma => firma.VarsayilanFirma)
                    .ThenBy(firma => firma.SiraNo).ThenBy(firma => firma.FirmaAdi)
                    .Select(firma => (int?)firma.Id)
                    .FirstOrDefaultAsync();
            }
            var validUserScope = AuthenticationSessionPolicy.AllowsFirmScopeRestore(
                isAdmin, bilgi.TumFirmalar, bilgi.FirmaId, defaultFirmaId);
            var validFirm = bilgi.FirmaId <= 0 || await db.Firmalar.AsNoTracking()
                .AnyAsync(x => x.Id == bilgi.FirmaId && x.Aktif && !x.IsDeleted);
            if (user == null || locked || !validFirm || !validUserScope)
            {
                await _storage.DeleteAsync(StorageKey);
                _mevcut = new AktifFirmaBilgisi();
                AktifFirmaDegisti?.Invoke();
                if (locked) await _authenticationStateProvider.CikisYapAsync();
                return false;
            }

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
            _mevcut = new AktifFirmaBilgisi();
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



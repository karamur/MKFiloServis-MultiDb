using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Her kullanici/tarayici (circuit) icin bagimsiz oturum yonetimi saglayan Authentication Provider.
/// Scoped olarak kayitli - her Blazor circuit kendi instance'ini alir.
/// NOT: Bu provider static degisken KULLANMAZ - her circuit bagimsizdir.
/// ProtectedSessionStorage ile circuit yeniden baglantisinda kullanici geri yuklenir.
/// </summary>
public class AppAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string StorageKey = "koa_session.v2";

    private readonly ILogger<AppAuthenticationStateProvider> _logger;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly LicenseService _licenseService;

    private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
    private Kullanici? _aktifKullanici;
    private string? _sessionId;
    private bool _restoreAttempted = false;

    public AppAuthenticationStateProvider(
        ILogger<AppAuthenticationStateProvider> logger,
        ICurrentUserAccessor currentUserAccessor,
        ProtectedSessionStorage sessionStorage,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        LicenseService licenseService)
    {
        _logger = logger;
        _currentUserAccessor = currentUserAccessor;
        _sessionStorage = sessionStorage;
        _dbContextFactory = dbContextFactory;
        _licenseService = licenseService;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    /// <summary>
    /// Circuit yeniden baglandiginda tarayici storage'dan kullaniciyi geri yukler.
    /// MainLayout'un OnAfterRenderAsync(firstRender) metodundan cagirilmali.
    /// </summary>
    public async Task<bool> TryRestoreFromStorageAsync()
    {
        if (_restoreAttempted) return _aktifKullanici != null;
        _restoreAttempted = true;

        // Zaten giris yapmis
        if (_aktifKullanici != null) return true;

        try
        {
            var result = await _sessionStorage.GetAsync<string>(StorageKey);
            if (!result.Success || string.IsNullOrEmpty(result.Value))
                return false;

            var sessionParts = result.Value.Split(':', 2);
            if (sessionParts.Length != 2 ||
                !int.TryParse(sessionParts[0], out var userId) ||
                !long.TryParse(sessionParts[1], out var startedUnixSeconds))
            {
                await _sessionStorage.DeleteAsync(StorageKey);
                return false;
            }

            var sessionStartedAtUtc = DateTimeOffset.FromUnixTimeSeconds(startedUnixSeconds);
            if (!AuthenticationSessionPolicy.IsWithinLifetime(sessionStartedAtUtc, DateTimeOffset.UtcNow))
            {
                await _sessionStorage.DeleteAsync(StorageKey);
                return false;
            }

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var kullanici = await db.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == userId && k.Aktif && !k.IsDeleted && !k.Kilitli);

            if (kullanici == null)
            {
                await _sessionStorage.DeleteAsync(StorageKey);
                return false;
            }

            // Storage'dan geri dönmek yeni bir oturum açılışıdır; lisansı burada bir kez doğrula.
            var licenseValidation = await _licenseService.ValidateAsync();
            if (!licenseValidation.IsValid)
                return false;

            // Geri yukle (SessionId'yi yenile)
            await GirisYapAsync(kullanici, sessionStartedAtUtc);
            _logger.LogInformation("Kullanici storage'dan geri yuklendi: {KullaniciAdi}", kullanici.KullaniciAdi);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Storage'dan kullanici geri yuklenemedi");
            return false;
        }
    }

    /// <summary>
    /// Kullaniciyi oturum acar
    /// </summary>
    public void GirisYap(Kullanici kullanici)
    {
        _aktifKullanici = kullanici;
        _sessionId = Guid.NewGuid().ToString("N");
        _restoreAttempted = true;

        // CurrentUserAccessor'a kullanıcı bilgisini set et (interceptor için)
        _currentUserAccessor.SetCurrentUser(kullanici.KullaniciAdi, kullanici.AdSoyad);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
            new Claim(ClaimTypes.Name, kullanici.KullaniciAdi),
            new Claim("AdSoyad", kullanici.AdSoyad),
            new Claim(ClaimTypes.Role, kullanici.Rol?.RolAdi ?? "Kullanici"),
            new Claim("SessionId", _sessionId)
        };

        if (!string.IsNullOrEmpty(kullanici.Email))
            claims.Add(new Claim(ClaimTypes.Email, kullanici.Email));

        var identity = new ClaimsIdentity(claims, "MKFiloServisAuth");
        _currentUser = new ClaimsPrincipal(identity);

        _logger.LogInformation("Kullanici giris yapti: {KullaniciAdi}, Rol: {Rol}, SessionId: {SessionId}", 
            kullanici.KullaniciAdi, kullanici.Rol?.RolAdi, _sessionId);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    /// <summary>
    /// Async giris - storage'a da kaydeder
    /// </summary>
    public async Task GirisYapAsync(Kullanici kullanici, DateTimeOffset? sessionStartedAtUtc = null)
    {
        var sessionStarted = sessionStartedAtUtc ?? DateTimeOffset.UtcNow;
        if (!AuthenticationSessionPolicy.IsWithinLifetime(sessionStarted, DateTimeOffset.UtcNow))
        {
            await CikisYapAsync();
            return;
        }
        // Login sonucu ile claims üretimi arasındaki yarışta devre dışı bırakılmış hesabı engelle.
        await using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            var current = await db.Kullanicilar.AsNoTracking()
                .Include(k => k.Rol).ThenInclude(r => r.Yetkiler)
                .FirstOrDefaultAsync(k => k.Id == kullanici.Id && k.Aktif && !k.IsDeleted && !k.Kilitli);
            if (current?.Rol == null || current.Rol.IsDeleted)
            {
                _logger.LogWarning("Oturum acma reddedildi; kullanici veya rol artik etkin degil. KullaniciId: {KullaniciId}", kullanici.Id);
                await CikisYapAsync();
                return;
            }
            kullanici = current;
        }

        GirisYap(kullanici);
        try
        {
            await _sessionStorage.SetAsync(StorageKey, $"{kullanici.Id}:{sessionStarted.ToUnixTimeSeconds()}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kullanici session storage'a kaydedilemedi");
        }

    }

    /// <summary>
    /// Kullaniciyi oturumdan cikarir
    /// </summary>
    public void CikisYap()
    {
        var kullaniciAdi = _aktifKullanici?.KullaniciAdi;

        _aktifKullanici = null;
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        _restoreAttempted = false;

        // CurrentUserAccessor'dan kullanıcıyı temizle
        _currentUserAccessor.ClearCurrentUser();

        _logger.LogInformation("Kullanici cikis yapti: {KullaniciAdi}", kullaniciAdi);

        _sessionId = null;

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    /// <summary>
    /// Async cikis - storage'i da temizler
    /// </summary>
    public async Task CikisYapAsync()
    {
        CikisYap();
        try
        {
            await _sessionStorage.DeleteAsync(StorageKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Session storage temizlenemedi");
        }
    }

    /// <summary>
    /// Aktif kullaniciyi dondurur
    /// </summary>
    public Kullanici? GetAktifKullanici() => _aktifKullanici;

    /// <summary>
    /// Kullanici giris yapmis mi kontrol eder
    /// </summary>
    public bool IsAuthenticated => _aktifKullanici != null;

    /// <summary>
    /// Mevcut session ID'yi dondurur
    /// </summary>
    public string? GetSessionId() => _sessionId;

}



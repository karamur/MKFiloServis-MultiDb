namespace KOAFiloServis.Maui.Services;

/// <summary>
/// Kimlik doğrulama ve token yönetimi.
/// Token'ı SecureStorage'da saklar.
/// </summary>
public class AuthService
{
    private const string TokenKey = "jwt_token";
    private const string KullaniciAdiKey = "kullanici_adi";
    private const string AdSoyadKey = "ad_soyad";
    private const string RolKey = "rol";

    public bool IsLoggedIn => SecureStorage.Default.GetAsync(TokenKey).Result != null;

    public async Task<string?> GetTokenAsync()
        => await SecureStorage.Default.GetAsync(TokenKey);

    public async Task<string?> GetKullaniciAdiAsync()
        => await SecureStorage.Default.GetAsync(KullaniciAdiKey);

    public async Task SaveSessionAsync(LoginResponse response)
    {
        await SecureStorage.Default.SetAsync(TokenKey, response.Token);
        await SecureStorage.Default.SetAsync(KullaniciAdiKey, response.KullaniciAdi);
        await SecureStorage.Default.SetAsync(AdSoyadKey, response.AdSoyad);
        await SecureStorage.Default.SetAsync(RolKey, response.Rol);
    }

    public void Logout()
    {
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(KullaniciAdiKey);
        SecureStorage.Default.Remove(AdSoyadKey);
        SecureStorage.Default.Remove(RolKey);
    }

    public async Task<string> GetAdSoyadAsync()
        => await SecureStorage.Default.GetAsync(AdSoyadKey) ?? "Şoför";

    public async Task<string> GetRolAsync()
        => await SecureStorage.Default.GetAsync(RolKey) ?? "";
}

using KOAFiloServis.Maui.Services;

namespace KOAFiloServis.Maui.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiClientService _api;
    private readonly AuthService _auth;

    public LoginPage(ApiClientService api, AuthService auth)
    {
        InitializeComponent();
        _api = api;
        _auth = auth;

        // Hızlı giriş için test kullanıcı
        KullaniciAdiEntry.Text = "test";
        SifreEntry.Text = "test123";
    }

    private async void OnGirisClicked(object? sender, EventArgs e)
    {
        // UI durumu
        SetLoading(true);
        HataLabel.IsVisible = false;

        try
        {
            var kullaniciAdi = KullaniciAdiEntry.Text?.Trim();
            var sifre = SifreEntry.Text;

            if (string.IsNullOrEmpty(kullaniciAdi) || string.IsNullOrEmpty(sifre))
            {
                ShowError("Kullanıcı adı ve şifre gereklidir.");
                return;
            }

            var response = await _api.LoginAsync(new LoginRequest
            {
                KullaniciAdi = kullaniciAdi,
                Sifre = sifre
            });

            if (response == null || string.IsNullOrEmpty(response.Token))
            {
                ShowError("Giriş başarısız. Kullanıcı adı veya şifre hatalı.");
                return;
            }

            // Session'ı kaydet
            await _auth.SaveSessionAsync(response);

            // Ana sayfaya yönlendir
            Application.Current!.MainPage = new AppShell(_auth);
        }
        catch (HttpRequestException)
        {
            ShowError("Sunucuya bağlanılamadı. İnternet bağlantınızı kontrol edin.");
        }
        catch (Exception ex)
        {
            ShowError($"Hata: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsVisible = loading;
        LoadingIndicator.IsRunning = loading;
        GirisBtn.IsEnabled = !loading;
    }

    private void ShowError(string message)
    {
        HataLabel.Text = message;
        HataLabel.IsVisible = true;
    }
}

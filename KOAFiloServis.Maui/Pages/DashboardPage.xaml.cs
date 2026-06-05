using KOAFiloServis.Maui.Services;

namespace KOAFiloServis.Maui.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly ApiClientService _api;
    private readonly AuthService _auth;

    public DashboardPage(ApiClientService api, AuthService auth)
    {
        InitializeComponent();
        _api = api;
        _auth = auth;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            var adSoyad = await _auth.GetAdSoyadAsync();
            HosgeldinLabel.Text = $"Hoşgeldin, {adSoyad}";
            TarihLabel.Text = DateTime.Now.ToString("dd MMMM yyyy, dddd");

            // Bugünün puantaj özetini API'den al
            try
            {
                var bugun = DateTime.Today;
                var sonuc = await _api.GetAsync<List<SoforGorevDto>>(
                    $"/api/puantaj/gunluk?tarih={bugun:yyyy-MM-dd}");

                if (sonuc is { Count: > 0 })
                {
                    var ilk = sonuc[0];
                    SeferSayisiLabel.Text = sonuc.Sum(s => s.SeferSayisi).ToString();
                    RotaLabel.Text = ilk.GuzergahAdi;
                    PlakaLabel.Text = ilk.Plaka;
                }
                else
                {
                    SeferSayisiLabel.Text = "0";
                    RotaLabel.Text = "Atama yok";
                    PlakaLabel.Text = "—";
                }
            }
            catch
            {
                // API offline — sessizce geç
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Dashboard yüklenirken hata: {ex.Message}", "Tamam");
        }
    }

    private async void OnPuantajTapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("puantaj");

    private async void OnGuzergahTapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("guzergah");

    private async void OnAracTapped(object? sender, EventArgs e)
        => await DisplayAlert("Araç Bilgisi", "Araç detay sayfası yakında eklenecek.", "Tamam");

    private async void OnBildirimTapped(object? sender, EventArgs e)
        => await DisplayAlert("Bildirimler", "Henüz yeni bildiriminiz yok.", "Tamam");
}

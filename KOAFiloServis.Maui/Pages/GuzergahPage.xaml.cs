using KOAFiloServis.Maui.Services;

namespace KOAFiloServis.Maui.Pages;

public partial class GuzergahPage : ContentPage
{
    private readonly ApiClientService _api;
    private DateTime _currentMonth = DateTime.Today;
    private List<SoforGorevDto> _gorevler = new();

    public GuzergahPage(ApiClientService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadGuzergahlarAsync();
    }

    private async Task LoadGuzergahlarAsync()
    {
        AyLabel.Text = _currentMonth.ToString("MMMM yyyy");
        try
        {
            var yil = _currentMonth.Year;
            var ay = _currentMonth.Month;
            _gorevler = await _api.GetAsync<List<SoforGorevDto>>(
                $"/api/puantaj/aylik?yil={yil}&ay={ay}") ?? new();
            GuzergahListesi.ItemsSource = _gorevler;
        }
        catch
        {
            // API offline — boş liste
            GuzergahListesi.ItemsSource = new List<SoforGorevDto>();
        }
    }

    private async void OnOncekiAy(object? sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        await LoadGuzergahlarAsync();
    }

    private async void OnSonrakiAy(object? sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        await LoadGuzergahlarAsync();
    }
}

namespace MKFiloServis.Client;

public partial class MainPage : ContentPage
{
    private const string ServerAddressKey = "server_address";

    public MainPage()
    {
        InitializeComponent();

        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            HeaderLabel.IsVisible = false;
            AddressHeader.Padding = new Thickness(12, 6);
        }

        var savedAddress = Preferences.Default.Get(ServerAddressKey, string.Empty);
        ServerAddressEntry.Text = string.IsNullOrWhiteSpace(savedAddress) && DeviceInfo.Platform == DevicePlatform.WinUI
            ? "http://localhost:5050"
            : savedAddress;

        if (!string.IsNullOrWhiteSpace(savedAddress))
            Connect(savedAddress);
        else
            StatusLabel.Text = "Sunucu adresini girin ve Bağlan'a basın.";
    }

    private void OnAddressCompleted(object? sender, EventArgs e) => ConnectFromEntry();

    private void OnConnectClicked(object? sender, EventArgs e) => ConnectFromEntry();

    private void ConnectFromEntry() => Connect(ServerAddressEntry.Text);

    private void Connect(string? address)
    {
        address = address?.Trim().TrimEnd('/');
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            StatusLabel.Text = "Geçerli bir http:// veya https:// sunucu adresi girin.";
            return;
        }

        if (DeviceInfo.Platform == DevicePlatform.Android && uri.IsLoopback)
        {
            StatusLabel.Text = "Telefonda localhost bu cihazı gösterir. Sunucunun ağ adresini girin.";
            return;
        }

        Preferences.Default.Set(ServerAddressKey, address!);
        ServerAddressEntry.Text = address;
        WelcomePanel.IsVisible = false;
        AppWebView.IsVisible = true;
        AppWebView.Source = new UrlWebViewSource { Url = address };
        StatusLabel.Text = "Bağlanıyor...";
    }

    private void OnRefreshClicked(object? sender, EventArgs e)
    {
        if (AppWebView.IsVisible)
            AppWebView.Reload();
        else
            ConnectFromEntry();
    }

    private void OnNavigating(object? sender, WebNavigatingEventArgs e) => StatusLabel.Text = "Yükleniyor...";

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        StatusLabel.Text = e.Result == WebNavigationResult.Success
            ? "Bağlandı"
            : "Bağlantı kurulamadı. Sunucu adresini ve ağ bağlantısını kontrol edin.";
    }

    protected override bool OnBackButtonPressed()
    {
        if (AppWebView.CanGoBack)
        {
            AppWebView.GoBack();
            return true;
        }

        return base.OnBackButtonPressed();
    }
}

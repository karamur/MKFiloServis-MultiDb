using KOAFiloServis.Maui.Pages;
using KOAFiloServis.Maui.Services;

namespace KOAFiloServis.Maui;

public partial class AppShell : Shell
{
    private readonly AuthService _auth;

    public AppShell(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;

        Routing.RegisterRoute("puantaj", typeof(DashboardPage));
        Routing.RegisterRoute("guzergah", typeof(GuzergahPage));
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        _auth.Logout();
        Application.Current!.MainPage = new NavigationPage(
            Application.Current.Handler!.MauiContext!.Services.GetRequiredService<LoginPage>());
    }
}

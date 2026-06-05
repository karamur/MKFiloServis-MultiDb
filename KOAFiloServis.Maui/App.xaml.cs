using KOAFiloServis.Maui.Pages;
using KOAFiloServis.Maui.Services;

namespace KOAFiloServis.Maui;

public partial class App : Application
{
    private readonly AuthService _auth;
    private readonly IServiceProvider _services;

    public App(AuthService auth, IServiceProvider services)
    {
        InitializeComponent();
        _auth = auth;
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Page startPage;

        if (_auth.IsLoggedIn)
        {
            startPage = new AppShell(_auth);
        }
        else
        {
            startPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
        }

        return new Window(startPage);
    }
}

using KOAFiloServis.Maui.Pages;
using KOAFiloServis.Maui.Services;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Maui;

public static class MauiProgram
{
    /// <summary>
    /// API base URL — Debug'da localhost, Release'de production.
    /// Android emulator'da 10.0.2.2, localhost'u host makineye yönlendirir.
    /// </summary>
#if DEBUG && ANDROID
    private const string ApiBaseUrl = "https://10.0.2.2:5001";
#elif DEBUG
    private const string ApiBaseUrl = "https://localhost:5001";
#else
    private const string ApiBaseUrl = "https://YOUR_SERVER:5001";
#endif

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Servisler
        builder.Services.AddSingleton<AuthService>();

        builder.Services.AddHttpClient<ApiClientService>(client =>
        {
            client.BaseAddress = new Uri(ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Sayfalar (DI ile)
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<GuzergahPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

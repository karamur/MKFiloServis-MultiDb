using System.Text.Json;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

public sealed class DatabaseRuntimeInfo
{
    public DatabaseProvider Provider { get; init; } = DatabaseProvider.PostgreSQL;
    public DatabaseProvider CanonicalProvider { get; init; } = DatabaseProvider.PostgreSQL;
    public string ConnectionString { get; init; } = string.Empty;
    public string Source { get; init; } = "appsettings.json";
    public string? SettingsPath { get; init; }

    public bool IsSqlite => Provider == DatabaseProvider.SQLite;
    public bool IsPostgreSql => Provider == DatabaseProvider.PostgreSQL;
    public bool IsSqlServer => Provider == DatabaseProvider.SQLServer;
    public bool IsMySql => Provider == DatabaseProvider.MySQL;
}

public static class DatabaseRuntimeResolver
{
    /// <summary>
    /// dbsettings.json yoksa/okunamazsa kullanılacak kanonik sağlayıcı. Tek sabit noktada tanımlı;
    /// migration hedefi aktif bağlantı sağlayıcısından bağımsızdır.
    /// </summary>
    private const DatabaseProvider VARSAYILAN_KANONIK_SAGLAYICI = DatabaseProvider.PostgreSQL;

    public static async Task<DatabaseRuntimeInfo> ResolveAsync(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var fallbackProvider = DatabaseSettings.ParseProvider(configuration.GetValue<string>("DatabaseProvider"));
        var fallbackConnectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var settingsPath = Path.Combine(environment.ContentRootPath, "dbsettings.json");

        if (!File.Exists(settingsPath))
        {
            return CreateFallbackInfo(fallbackProvider, fallbackConnectionString, settingsPath);
        }

        try
        {
            var dbSettingsJson = await File.ReadAllTextAsync(settingsPath);
            var dbSettings = JsonSerializer.Deserialize<DatabaseSettings>(dbSettingsJson);
            if (dbSettings is null)
            {
                return CreateFallbackInfo(fallbackProvider, fallbackConnectionString, settingsPath);
            }

            var runtimeProvider = DatabaseSettings.NormalizeRuntimeProvider(dbSettings.Provider);
            // Kanonik sağlayıcı aktif bağlantı sağlayıcısı değildir; şema migration hedefidir.
            // dbsettings.json bunu tanımlıyorsa ona saygı gösterilir, yoksa PostgreSQL varsayılanı kullanılır.
            var canonicalProvider = DatabaseSettings.NormalizeCanonicalProvider(dbSettings.CanonicalProvider);
            var connectionString = runtimeProvider == DatabaseProvider.SQLite && string.IsNullOrWhiteSpace(dbSettings.DatabaseName)
                ? new DatabaseSettings { Provider = DatabaseProvider.SQLite, DatabaseName = "MKFiloServis.db" }.GetConnectionString()
                : dbSettings.GetConnectionString();

            // dbsettings.json'dan üretilen bağlantı geçersizse (Host boş veya Port=0)
            // appsettings.json DefaultConnection'a geri dön
            if (!IsConnectionStringValid(connectionString, runtimeProvider))
            {
                var fallback = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
                var fallbackProvider2 = DatabaseSettings.ParseProvider(configuration.GetValue<string>("DatabaseProvider"));
                if (IsConnectionStringValid(fallback, fallbackProvider2))
                    return CreateInfo(fallbackProvider2, canonicalProvider, fallback, "appsettings.json (fallback)", settingsPath);

                throw new InvalidOperationException(
                    $"dbsettings.json ayarı geçersiz ve appsettings.json içindeki {fallbackProvider2} bağlantısı bu sağlayıcı için uygun değil.");
            }

            return CreateInfo(runtimeProvider, canonicalProvider, connectionString, "dbsettings.json", settingsPath);
        }
        catch (Exception ex)
        {
            if (IsConnectionStringValid(fallbackConnectionString, fallbackProvider))
                return CreateInfo(fallbackProvider, VARSAYILAN_KANONIK_SAGLAYICI, fallbackConnectionString, "appsettings.json (fallback)", settingsPath);

            throw new InvalidOperationException(
                $"Veritabanı ayarları okunamadı ve appsettings.json içindeki {fallbackProvider} bağlantısı geçersiz.", ex);
        }
    }

    private static DatabaseRuntimeInfo CreateFallbackInfo(
        DatabaseProvider provider,
        string connectionString,
        string settingsPath)
    {
        if (!IsConnectionStringValid(connectionString, provider))
            throw new InvalidOperationException($"appsettings.json içindeki {provider} veritabanı bağlantısı geçersiz.");

        return CreateInfo(provider, VARSAYILAN_KANONIK_SAGLAYICI, connectionString, "appsettings.json", settingsPath);
    }

    private static bool IsConnectionStringValid(string connectionString, DatabaseProvider provider)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;

        return provider switch
        {
            DatabaseProvider.SQLite => connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase),
            DatabaseProvider.PostgreSQL => connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
                && !connectionString.Contains("Host=;", StringComparison.OrdinalIgnoreCase)
                && !connectionString.Contains("Host= ;", StringComparison.OrdinalIgnoreCase)
                && !connectionString.Contains("Port=0;", StringComparison.OrdinalIgnoreCase)
                && !connectionString.Contains("Port=0,", StringComparison.OrdinalIgnoreCase),
            DatabaseProvider.SQLServer => connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                && connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase),
            DatabaseProvider.MySQL => connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                && connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static DatabaseRuntimeInfo CreateInfo(
        DatabaseProvider provider,
        DatabaseProvider canonicalProvider,
        string connectionString,
        string source,
        string? settingsPath)
    {
        var normalizedProvider = DatabaseSettings.NormalizeRuntimeProvider(provider);
        var normalizedCanonicalProvider = DatabaseSettings.NormalizeRuntimeProvider(canonicalProvider);

        return new DatabaseRuntimeInfo
        {
            Provider = normalizedProvider,
            CanonicalProvider = normalizedCanonicalProvider,
            ConnectionString = connectionString,
            Source = source,
            SettingsPath = settingsPath
        };
    }
}

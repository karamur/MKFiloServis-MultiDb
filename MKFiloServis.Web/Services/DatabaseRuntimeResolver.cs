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
    public static async Task<DatabaseRuntimeInfo> ResolveAsync(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var fallbackProvider = DatabaseSettings.ParseProvider(configuration.GetValue<string>("DatabaseProvider"));
        var fallbackConnectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var settingsPath = Path.Combine(environment.ContentRootPath, "dbsettings.json");

        if (!File.Exists(settingsPath))
        {
            return CreateInfo(fallbackProvider, fallbackProvider, fallbackConnectionString, "appsettings.json", settingsPath);
        }

        try
        {
            var dbSettingsJson = await File.ReadAllTextAsync(settingsPath);
            var dbSettings = JsonSerializer.Deserialize<DatabaseSettings>(dbSettingsJson);
            if (dbSettings is null)
            {
                return CreateInfo(fallbackProvider, fallbackProvider, fallbackConnectionString, "appsettings.json", settingsPath);
            }

            var runtimeProvider = DatabaseSettings.NormalizeRuntimeProvider(dbSettings.Provider);
            var canonicalProvider = DatabaseProvider.PostgreSQL;
            var connectionString = runtimeProvider == DatabaseProvider.SQLite && string.IsNullOrWhiteSpace(dbSettings.DatabaseName)
                ? new DatabaseSettings { Provider = DatabaseProvider.SQLite, DatabaseName = "MKFiloServis.db" }.GetConnectionString()
                : dbSettings.GetConnectionString();

            // dbsettings.json'dan üretilen bağlantı geçersizse (Host boş veya Port=0)
            // appsettings.json DefaultConnection'a geri dön
            if (!IsConnectionStringValid(connectionString, runtimeProvider))
            {
                var fallback = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
                // Fallback da SQLite ise provider'ı PostgreSQL yap (geçiş tamamlandı)
                var fallbackProvider2 = DatabaseSettings.ParseProvider(configuration.GetValue<string>("DatabaseProvider"));
                if (fallbackProvider2 == DatabaseProvider.SQLite)
                    fallbackProvider2 = DatabaseProvider.PostgreSQL;
                return CreateInfo(fallbackProvider2, canonicalProvider, fallback, "appsettings.json (fallback)", settingsPath);
            }

            return CreateInfo(runtimeProvider, canonicalProvider, connectionString, "dbsettings.json", settingsPath);
        }
        catch
        {
            return CreateInfo(fallbackProvider, fallbackProvider, fallbackConnectionString, "appsettings.json", settingsPath);
        }
    }

    private static bool IsConnectionStringValid(string connectionString, DatabaseProvider provider)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;

        // SQLite: Data Source yeterli
        if (provider == DatabaseProvider.SQLite)
            return connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase);

        // PostgreSQL / MySQL / SQLServer: Host boş veya Port=0 ise geçersiz
        if (connectionString.Contains("Host=;", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Host= ;", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Port=0;", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Port=0,", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
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

using System.Text.Json;
using MKFiloServis.Shared.Entities;
using Microsoft.Data.Sqlite;

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
            return CreateFallbackInfo(fallbackProvider, fallbackConnectionString, settingsPath, environment.ContentRootPath);
        }

        try
        {
            var dbSettingsJson = await File.ReadAllTextAsync(settingsPath);
            var dbSettings = JsonSerializer.Deserialize<DatabaseSettings>(dbSettingsJson);
            if (dbSettings is null)
            {
                throw new InvalidOperationException("dbsettings.json boş veya geçersiz.");
            }

            var runtimeProvider = DatabaseSettings.NormalizeRuntimeProvider(dbSettings.Provider);
            // Kanonik sağlayıcı aktif bağlantı sağlayıcısı değildir; şema migration hedefidir.
            // dbsettings.json bunu tanımlıyorsa ona saygı gösterilir, yoksa PostgreSQL varsayılanı kullanılır.
            var canonicalProvider = DatabaseSettings.NormalizeCanonicalProvider(dbSettings.CanonicalProvider);
            var connectionString = dbSettings.GetConnectionString();
            if (runtimeProvider == DatabaseProvider.SQLite)
                connectionString = ResolveSqlitePath(connectionString, environment.ContentRootPath);

            // A saved runtime target must never silently fall back to another database.
            if (!IsConnectionStringValid(connectionString, runtimeProvider))
            {
                throw new InvalidOperationException(
                    $"dbsettings.json içindeki {runtimeProvider} hedefi geçersiz. Başka veritabanına sessizce geçilmeyecek.");
            }

            return CreateInfo(runtimeProvider, canonicalProvider, connectionString, "dbsettings.json", settingsPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Kaydedilmiş veritabanı ayarları okunamadı. Yanlış veritabanına bağlanmamak için uygulama başlatılmadı.", ex);
        }
    }

    private static DatabaseRuntimeInfo CreateFallbackInfo(
        DatabaseProvider provider,
        string connectionString,
        string settingsPath,
        string contentRootPath)
    {
        if (provider == DatabaseProvider.SQLite)
            connectionString = ResolveSqlitePath(connectionString, contentRootPath);

        if (!IsConnectionStringValid(connectionString, provider))
            throw new InvalidOperationException($"appsettings.json içindeki {provider} veritabanı bağlantısı geçersiz.");

        return CreateInfo(provider, VARSAYILAN_KANONIK_SAGLAYICI, connectionString, "appsettings.json", settingsPath);
    }

    private static string ResolveSqlitePath(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource))
            builder.DataSource = "MKFiloServis.db";
        if (!Path.IsPathRooted(builder.DataSource))
            builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));
        return builder.ToString();
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

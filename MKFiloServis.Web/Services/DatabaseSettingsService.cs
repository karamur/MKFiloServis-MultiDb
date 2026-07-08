using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MKFiloServis.Shared.Entities;
using MySqlConnector;
using Npgsql;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public interface IDatabaseSettingsService
{
    Task<DatabaseSettings> GetSettingsAsync();
    Task SaveSettingsAsync(DatabaseSettings settings);
    Task<(bool Success, string Message)> TestConnectionAsync(DatabaseSettings settings);
    Task<(bool Success, string Message)> ApplyConnectionAsync(DatabaseSettings settings);
}

public class DatabaseSettingsService : IDatabaseSettingsService
{
    private readonly string _settingsPath;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public DatabaseSettingsService(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
        _settingsPath = Path.Combine(_env.ContentRootPath, "dbsettings.json");
    }

    public async Task<DatabaseSettings> GetSettingsAsync()
    {
        if (File.Exists(_settingsPath))
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonSerializer.Deserialize<DatabaseSettings>(json);
            if (settings != null)
            {
                settings.Provider = DatabaseSettings.NormalizeRuntimeProvider(settings.Provider);
                settings.CanonicalProvider = DatabaseProvider.PostgreSQL;
                if (settings.Provider == DatabaseProvider.SQLite)
                {
                    settings.DatabaseName = settings.GetNormalizedSqliteDatabaseName();
                }

                return settings;
            }
        }

        var configuredProvider = DatabaseSettings.NormalizeRuntimeProvider(
            DatabaseSettings.ParseProvider(_configuration.GetValue<string>("DatabaseProvider")));
        var connStr = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var parsedSettings = ParseConnectionString(connStr, configuredProvider);
        parsedSettings.CanonicalProvider = DatabaseProvider.PostgreSQL;
        return parsedSettings;
    }

    public async Task SaveSettingsAsync(DatabaseSettings settings)
    {
        NormalizeSettings(settings);
        settings.LastUpdated = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(DatabaseSettings settings)
    {
        try
        {
            NormalizeSettings(settings);
            var connectionString = settings.GetConnectionString();

            switch (settings.Provider)
            {
                case DatabaseProvider.PostgreSQL:
                    await using (var conn = new NpgsqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        await using var cmd = new NpgsqlCommand("SELECT 1", conn);
                        await cmd.ExecuteScalarAsync();
                        return (true, "PostgreSQL baglantisi basarili!");
                    }

                case DatabaseProvider.SQLite:
                    var sqliteDataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
                    var sqlitePath = Path.IsPathRooted(sqliteDataSource)
                        ? sqliteDataSource
                        : Path.Combine(_env.ContentRootPath, sqliteDataSource);
                    Directory.CreateDirectory(Path.GetDirectoryName(sqlitePath) ?? _env.ContentRootPath);

                    await using (var conn = new SqliteConnection($"Data Source={sqlitePath}"))
                    {
                        await conn.OpenAsync();
                        await using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT 1";
                        await cmd.ExecuteScalarAsync();
                        return (true, "SQLite baglantisi basarili!");
                    }

                case DatabaseProvider.SQLServer:
                    await using (var conn = new SqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        await using var cmd = new SqlCommand("SELECT 1", conn);
                        await cmd.ExecuteScalarAsync();
                        return (true, "SQL Server baglantisi basarili!");
                    }

                case DatabaseProvider.MySQL:
                    await using (var conn = new MySqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        await using var cmd = new MySqlCommand("SELECT 1", conn);
                        await cmd.ExecuteScalarAsync();
                        return (true, "MySQL baglantisi basarili!");
                    }

                default:
                    return (false, "Desteklenmeyen veritabani tipi.");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Baglanti hatasi: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ApplyConnectionAsync(DatabaseSettings settings)
    {
        try
        {
            NormalizeSettings(settings);

            var testResult = await TestConnectionAsync(settings);
            if (!testResult.Success)
            {
                return testResult;
            }

            var previousSettings = await GetSettingsAsync();
            var manifestPath = await WriteTransitionManifestAsync(previousSettings, settings);
            settings.TransitionManifestPath = manifestPath;
            settings.LastTransitionAtUtc = DateTime.UtcNow;

            await SaveSettingsAsync(settings);
            await UpdateAppSettingsAsync(settings);

            var providerLabel = settings.GetProviderDisplayName();
            return (true, $"{providerLabel} ayarlari kaydedildi. Kanonik migration kaynagi PostgreSQL olarak korunur. Uygulamayi yeniden baslatmaniz gerekiyor.");
        }
        catch (Exception ex)
        {
            return (false, $"Ayarlar kaydedilemedi: {ex.Message}");
        }
    }

    private async Task UpdateAppSettingsAsync(DatabaseSettings settings)
    {
        var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
        var json = await File.ReadAllTextAsync(appSettingsPath);
        using var doc = JsonDocument.Parse(json);

        using var stream = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            var hasConnectionStrings = false;
            var hasDatabaseProvider = false;

            foreach (var element in doc.RootElement.EnumerateObject())
            {
                if (element.NameEquals("ConnectionStrings"))
                {
                    hasConnectionStrings = true;
                    writer.WritePropertyName("ConnectionStrings");
                    writer.WriteStartObject();
                    writer.WriteString("DefaultConnection", settings.GetConnectionString());
                    writer.WriteEndObject();
                }
                else if (element.NameEquals("DatabaseProvider"))
                {
                    hasDatabaseProvider = true;
                    writer.WriteString("DatabaseProvider", settings.GetProviderDisplayName());
                }
                else
                {
                    element.WriteTo(writer);
                }
            }

            if (!hasDatabaseProvider)
            {
                writer.WriteString("DatabaseProvider", settings.GetProviderDisplayName());
            }

            if (!hasConnectionStrings)
            {
                writer.WritePropertyName("ConnectionStrings");
                writer.WriteStartObject();
                writer.WriteString("DefaultConnection", settings.GetConnectionString());
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        var newJson = Encoding.UTF8.GetString(stream.ToArray());
        await File.WriteAllTextAsync(appSettingsPath, newJson);
    }

    private async Task<string> WriteTransitionManifestAsync(DatabaseSettings previousSettings, DatabaseSettings nextSettings)
    {
        var manifestsDirectory = Path.Combine(_env.ContentRootPath, "App_Data", "db-transitions");
        Directory.CreateDirectory(manifestsDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var manifestPath = Path.Combine(manifestsDirectory, $"transition-{timestamp}.json");
        var manifest = new
        {
            createdAtUtc = DateTime.UtcNow,
            canonicalProvider = DatabaseProvider.PostgreSQL.ToString(),
            previousProvider = previousSettings.GetProviderDisplayName(),
            nextProvider = nextSettings.GetProviderDisplayName(),
            previousConnectionString = previousSettings.GetConnectionString(),
            nextConnectionString = nextSettings.GetConnectionString(),
            strategy = previousSettings.Provider == nextSettings.Provider
                ? "in-place-update"
                : "snapshot-and-convert",
            note = "Provider gecislerinde migration replay yerine bu manifest ve veri snapshot/export akisi kullanilmalidir."
        };

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(manifestPath, json);
        return manifestPath;
    }

    private static void NormalizeSettings(DatabaseSettings settings)
    {
        settings.Provider = DatabaseSettings.NormalizeRuntimeProvider(settings.Provider);
        settings.CanonicalProvider = DatabaseProvider.PostgreSQL;

        switch (settings.Provider)
        {
            case DatabaseProvider.PostgreSQL:
                settings.Port = settings.Port <= 0 ? 5432 : settings.Port;
                settings.Host = string.IsNullOrWhiteSpace(settings.Host) ? "localhost" : settings.Host.Trim();
                settings.DatabaseName = string.IsNullOrWhiteSpace(settings.DatabaseName) ? "MKFiloServis" : settings.DatabaseName.Trim();
                settings.Username = string.IsNullOrWhiteSpace(settings.Username) ? "postgres" : settings.Username.Trim();
                settings.UseIntegratedSecurity = false;
                break;

            case DatabaseProvider.SQLServer:
                settings.Port = settings.Port <= 0 ? 1433 : settings.Port;
                settings.Host = string.IsNullOrWhiteSpace(settings.Host) ? "localhost" : settings.Host.Trim();
                settings.DatabaseName = string.IsNullOrWhiteSpace(settings.DatabaseName) ? "MKFiloServis" : settings.DatabaseName.Trim();
                if (!settings.UseIntegratedSecurity)
                {
                    settings.Username = string.IsNullOrWhiteSpace(settings.Username) ? "sa" : settings.Username.Trim();
                }
                break;

            case DatabaseProvider.MySQL:
                settings.Port = settings.Port <= 0 ? 3306 : settings.Port;
                settings.Host = string.IsNullOrWhiteSpace(settings.Host) ? "localhost" : settings.Host.Trim();
                settings.DatabaseName = string.IsNullOrWhiteSpace(settings.DatabaseName) ? "MKFiloServis" : settings.DatabaseName.Trim();
                settings.Username = string.IsNullOrWhiteSpace(settings.Username) ? "root" : settings.Username.Trim();
                settings.UseIntegratedSecurity = false;
                break;

            case DatabaseProvider.SQLite:
                settings.Port = 0;
                settings.Host = string.Empty;
                settings.Username = string.Empty;
                settings.Password = string.Empty;
                settings.UseIntegratedSecurity = false;
                settings.DatabaseName = settings.GetNormalizedSqliteDatabaseName();
                break;
        }
    }

    private static DatabaseSettings ParseConnectionString(string connStr, DatabaseProvider configuredProvider)
    {
        var settings = new DatabaseSettings
        {
            Provider = configuredProvider,
            CanonicalProvider = DatabaseProvider.PostgreSQL,
            Port = configuredProvider switch
            {
                DatabaseProvider.SQLite => 0,
                DatabaseProvider.SQLServer => 1433,
                DatabaseProvider.MySQL => 3306,
                _ => 5432
            }
        };

        if (connStr.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            settings.Provider = DatabaseProvider.SQLite;
            settings.Port = 0;
        }
        else if (connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            settings.Provider = DatabaseProvider.PostgreSQL;
            settings.Port = 5432;
        }
        else if (connStr.Contains("Server=", StringComparison.OrdinalIgnoreCase) && connStr.Contains("User Id=", StringComparison.OrdinalIgnoreCase))
        {
            settings.Provider = DatabaseProvider.SQLServer;
            settings.Port = 1433;
        }
        else if (connStr.Contains("Server=", StringComparison.OrdinalIgnoreCase) && connStr.Contains("User=", StringComparison.OrdinalIgnoreCase))
        {
            settings.Provider = DatabaseProvider.MySQL;
            settings.Port = 3306;
        }

        var parts = connStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2)
            {
                continue;
            }

            var key = kv[0].Trim().ToLowerInvariant();
            var value = kv[1].Trim();

            switch (key)
            {
                case "host":
                    settings.Host = value;
                    break;
                case "server":
                    settings.Host = value;
                    break;
                case "port":
                    if (int.TryParse(value, out var port))
                    {
                        settings.Port = port;
                    }
                    break;
                case "database":
                case "data source":
                    settings.DatabaseName = value;
                    break;
                case "username":
                case "user":
                case "user id":
                    settings.Username = value;
                    break;
                case "integrated security":
                    settings.UseIntegratedSecurity = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("sspi", StringComparison.OrdinalIgnoreCase);
                    break;
                case "password":
                    settings.Password = value;
                    break;
            }
        }

        NormalizeSettings(settings);
        return settings;
    }
}





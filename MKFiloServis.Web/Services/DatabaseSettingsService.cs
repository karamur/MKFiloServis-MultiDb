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
    private readonly ILogger<DatabaseSettingsService> _logger;

    public DatabaseSettingsService(IConfiguration configuration, IWebHostEnvironment env, ILogger<DatabaseSettingsService> logger)
    {
        _configuration = configuration;
        _env = env;
        _logger = logger;
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
                settings.CanonicalProvider = DatabaseSettings.NormalizeCanonicalProvider(settings.CanonicalProvider);
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
        EnsureSupportedRuntimeProvider(settings.Provider);
        settings.LastUpdated = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(DatabaseSettings settings)
    {
        if (!settings.UsesSupportedRuntimeProvider())
            return (false, GetUnsupportedProviderMessage(settings.Provider));

        try
        {
            NormalizeSettings(settings);
            EnsureSupportedRuntimeProvider(settings.Provider);
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
            _logger.LogWarning(ex, "Veritabanı bağlantı denemesi başarısız oldu.");
            return (false, "Veritabanı bağlantısı kurulamadı. Sağlayıcı ve bağlantı ayarlarını kontrol edin.");
        }
    }

    public async Task<(bool Success, string Message)> ApplyConnectionAsync(DatabaseSettings settings)
    {
        try
        {
            NormalizeSettings(settings);
            EnsureSupportedRuntimeProvider(settings.Provider);

            var createdDatabase = false;
            if (settings.Provider == DatabaseProvider.PostgreSQL)
            {
                createdDatabase = await EnsurePostgreSqlDatabaseExistsAsync(settings);
            }

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

            var providerLabel = settings.GetProviderDisplayName();
            var databaseMessage = createdDatabase
                ? $" '{settings.DatabaseName}' veritabanı oluşturuldu; uygulama şema migration'larını yeniden başlatmada uygulayacak."
                : string.Empty;
            return (true, $"{providerLabel} ayarları kaydedildi.{databaseMessage} Kanonik migration kaynağı PostgreSQL olarak korunur. Tüm uygulama servislerinin bu veritabanını kullanması için uygulamayı yeniden başlatın. Eski veritabanındaki veriler otomatik kopyalanmaz.");
        }
        catch (NotSupportedException ex)
        {
            return (false, ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState == "42501")
        {
            _logger.LogWarning("PostgreSQL hedef veritabanı oluşturulamadı: CREATE DATABASE yetkisi eksik.");
            return (false, "Hedef PostgreSQL veritabanı mevcut değil ve bu kullanıcıda CREATE DATABASE yetkisi yok. DBA yetkisiyle veritabanını oluşturun veya yetki verin; ayarlar kaydedilmedi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veritabanı bağlantı ayarları uygulanamadı.");
            return (false, "Veritabanı ayarları uygulanamadı. Ayrıntı için sistem yöneticisine başvurun.");
        }
    }

    private static async Task<bool> EnsurePostgreSqlDatabaseExistsAsync(DatabaseSettings settings)
    {
        var databaseName = settings.DatabaseName?.Trim();
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("PostgreSQL veritabanı adı boş olamaz.");
        if (Encoding.UTF8.GetByteCount(databaseName) > 63 || databaseName.Contains('\0'))
            throw new InvalidOperationException("PostgreSQL veritabanı adı 63 UTF-8 baytı aşamaz veya NULL karakter içeremez.");

        var targetConnection = new NpgsqlConnectionStringBuilder
        {
            Host = settings.Host,
            Port = settings.Port,
            Database = databaseName,
            Username = settings.Username,
            Password = settings.Password,
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 20
        };
        var maintenanceConnection = new NpgsqlConnectionStringBuilder(targetConnection.ConnectionString)
        {
            Database = "postgres",
            Pooling = false,
            MinPoolSize = 0,
            MaxPoolSize = 1
        };

        await using var connection = new NpgsqlConnection(maintenanceConnection.ConnectionString);
        await connection.OpenAsync();
        await using var existsCommand = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName)", connection);
        existsCommand.Parameters.AddWithValue("databaseName", databaseName);
        if (Equals(await existsCommand.ExecuteScalarAsync(), true))
            return false;

        var quotedName = new NpgsqlCommandBuilder().QuoteIdentifier(databaseName);
        await using var createCommand = new NpgsqlCommand($"CREATE DATABASE {quotedName}", connection);
        try
        {
            await createCommand.ExecuteNonQueryAsync();
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == "42P04")
        {
            // Another app instance may have created it after the existence check.
            return false;
        }
    }

    private static string GetUnsupportedProviderMessage(DatabaseProvider provider) =>
        $"{provider} için otomatik şema migration desteği bulunmuyor. Bu sürümde yalnız PostgreSQL ve SQLite seçilebilir. " +
        "Ayar kaydedilmedi ve veritabanı geçişi başlatılmadı.";

    private static void EnsureSupportedRuntimeProvider(DatabaseProvider provider)
    {
        if (provider is not (DatabaseProvider.PostgreSQL or DatabaseProvider.SQLite))
        {
            throw new NotSupportedException(GetUnsupportedProviderMessage(provider));
        }
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
            canonicalProvider = nextSettings.CanonicalProvider.ToString(),
            previousProvider = previousSettings.GetProviderDisplayName(),
            nextProvider = nextSettings.GetProviderDisplayName(),
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
        settings.CanonicalProvider = DatabaseSettings.NormalizeCanonicalProvider(settings.CanonicalProvider);

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





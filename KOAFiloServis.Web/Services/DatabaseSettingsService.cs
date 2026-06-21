using KOAFiloServis.Shared.Entities;
using System.Text.Json;
using Npgsql;
using Microsoft.Data.SqlClient;
using MySqlConnector;
namespace KOAFiloServis.Web.Services;

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
            if (settings != null) return settings;
        }

        // Varsayilan ayarlar - appsettings'den oku
        var connStr = _configuration.GetConnectionString("DefaultConnection") ?? "";
        return ParseConnectionString(connStr);
    }

    public async Task SaveSettingsAsync(DatabaseSettings settings)
    {
        settings.LastUpdated = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(DatabaseSettings settings)
    {
        try
        {
            var connectionString = settings.GetConnectionString();

            switch (settings.Provider)
            {
                case DatabaseProvider.PostgreSQL:
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        using var cmd = new NpgsqlCommand("SELECT 1", conn);
                        await cmd.ExecuteScalarAsync();
                        return (true, "PostgreSQL baglantisi basarili!");
                    }

                case DatabaseProvider.SQLServer:
                    using (var conn = new SqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        using var cmd = new SqlCommand("SELECT 1", conn);
                        await cmd.ExecuteScalarAsync();
                        return (true, "SQL Server baglantisi basarili!");
                    }

                case DatabaseProvider.MySQL:
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        using var cmd = new MySqlCommand("SELECT 1", conn);
                        await cmd.ExecuteScalarAsync();
                        return (true, "MySQL baglantisi basarili!");
                    }

                case DatabaseProvider.SQLite:
                    // SQLite destegi kaldirildi — proje PostgreSQL-only.
                    // SQLite baglanti testi icin KOAFiloServis.SqliteTool kullanin.
                    return (false, "SQLite desteklenmiyor. Proje PostgreSQL-only mimariye gecildi.");

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
            // Once baglanti test et
            var testResult = await TestConnectionAsync(settings);
            if (!testResult.Success)
                return testResult;

            // Ayarlari kaydet
            await SaveSettingsAsync(settings);

            // appsettings.json guncelle
            var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            var json = await File.ReadAllTextAsync(appSettingsPath);
            var doc = JsonDocument.Parse(json);

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                foreach (var element in doc.RootElement.EnumerateObject())
                {
                    if (element.Name == "ConnectionStrings")
                    {
                        writer.WritePropertyName("ConnectionStrings");
                        writer.WriteStartObject();
                        writer.WriteString("DefaultConnection", settings.GetConnectionString());
                        writer.WriteEndObject();
                    }
                    else
                    {
                        element.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            var newJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            await File.WriteAllTextAsync(appSettingsPath, newJson);

            return (true, "Baglanti ayarlari kaydedildi. Uygulamayi yeniden baslatmaniz gerekiyor.");
        }
        catch (Exception ex)
        {
            return (false, $"Ayarlar kaydedilemedi: {ex.Message}");
        }
    }

    private DatabaseSettings ParseConnectionString(string connStr)
    {
        var settings = new DatabaseSettings();

        if (connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            settings.Provider = DatabaseProvider.PostgreSQL;
            settings.Port = 5432;
        }
        else if (connStr.Contains("Server=", StringComparison.OrdinalIgnoreCase) && connStr.Contains("User Id=", StringComparison.OrdinalIgnoreCase))
        {
            settings.Provider = DatabaseProvider.SQLServer;
            settings.Port = 1433;
        }
        else if (connStr.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            settings.Provider = DatabaseProvider.SQLite;
        }

        var parts = connStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;

            var key = kv[0].Trim().ToLower();
            var value = kv[1].Trim();

            switch (key)
            {
                case "host":
                case "server":
                    if (value.Contains(','))
                    {
                        var serverParts = value.Split(',');
                        settings.Host = serverParts[0];
                        if (int.TryParse(serverParts[1], out var port))
                            settings.Port = port;
                    }
                    else
                    {
                        settings.Host = value;
                    }
                    break;
                case "port":
                    if (int.TryParse(value, out var p)) settings.Port = p;
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
                case "password":
                    settings.Password = value;
                    break;
            }
        }

        return settings;
    }
}

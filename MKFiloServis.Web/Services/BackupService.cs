using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.Common;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class BackupService : IBackupService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackupService> _logger;
    private readonly string _settingsFile;

    public BackupService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IServiceProvider serviceProvider,
        ILogger<BackupService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settingsFile = Path.Combine(_environment.ContentRootPath, "backup_settings.json");
    }

    public string GetCurrentDatabaseProvider()
    {
        var dbSettings = ReadDatabaseSettings();
        if (dbSettings != null)
        {
            // Provider, CanonicalProvider ile eşleşmiyorsa (geçiş sonrası kalıntı) düzelt
            if (dbSettings.Provider == DatabaseProvider.SQLite &&
                dbSettings.CanonicalProvider == DatabaseProvider.PostgreSQL)
            {
                _logger.LogDebug(
                    "dbsettings.json Provider=SQLite ama CanonicalProvider=PostgreSQL; PostgreSQL-only modda devam ediliyor.");
                return "PostgreSQL";
            }
            return dbSettings.GetProviderDisplayName();
        }

        var configuredProvider = _configuration.GetValue<string>("DatabaseProvider");
        if (!string.IsNullOrWhiteSpace(configuredProvider))
        {
            return DatabaseSettings.ParseProvider(configuredProvider) switch
            {
                DatabaseProvider.SQLServer => "SQLServer",
                DatabaseProvider.MySQL => "MySQL",
                _ => "PostgreSQL"
            };
        }

        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(defaultConnection))
        {
            return GetProviderFromConnectionString(defaultConnection) switch
            {
                "SQLSERVER" => "SQLServer",
                "MYSQL" => "MySQL",
                _ => "PostgreSQL"
            };
        }

        return "PostgreSQL";
    }

    public async Task<BackupResult> CreateBackupAsync(string? customBackupFolder = null)
    {
        var result = new BackupResult();
        var dbProvider = GetCurrentDatabaseProvider();

        try
        {
            var settings = GetSettings();
            var backupRoot = customBackupFolder ?? GetBackupFolderPath(settings);
            var backupFolder = GetArchiveFolderPath(backupRoot, DateTime.Now);

            if (!Directory.Exists(backupFolder))
                Directory.CreateDirectory(backupFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFileName;
            string backupFilePath;

            switch (dbProvider.ToUpperInvariant())
            {
                case "POSTGRESQL":
                    backupFileName = $"MKFiloServis_PostgreSQL_{timestamp}.backup";
                    backupFilePath = Path.Combine(backupFolder, backupFileName);
                    result = await CreatePostgreSqlBackupAsync(backupFilePath);
                    break;

                case "MSSQL":
                case "SQLSERVER":
                    backupFileName = $"MKFiloServis_MSSQL_{timestamp}.bak";
                    backupFilePath = Path.Combine(backupFolder, backupFileName);
                    result = await CreateMsSqlBackupAsync(backupFilePath);
                    break;

                case "MYSQL":
                    backupFileName = $"MKFiloServis_MySQL_{timestamp}.sql";
                    backupFilePath = Path.Combine(backupFolder, backupFileName);
                    result = await CreateMySqlBackupAsync(backupFilePath);
                    break;

                case "MONGO":
                case "MONGODB":
                    backupFileName = $"MKFiloServis_Mongo_{timestamp}.json";
                    backupFilePath = Path.Combine(backupFolder, backupFileName);
                    await CreateJsonBackupAsync(backupFilePath);
                    result = CreateSuccessResult(backupFilePath);
                    break;

                case "EXCEL":
                    backupFileName = $"MKFiloServis_Excel_{timestamp}.json";
                    backupFilePath = Path.Combine(backupFolder, backupFileName);
                    await CreateJsonBackupAsync(backupFilePath);
                    result = CreateSuccessResult(backupFilePath);
                    break;

                default:
                    backupFileName = $"MKFiloServis_{dbProvider}_{timestamp}.json";
                    backupFilePath = Path.Combine(backupFolder, backupFileName);
                    await CreateJsonBackupAsync(backupFilePath);
                    result = CreateSuccessResult(backupFilePath);
                    break;
            }

            if (result.Success)
            {
                // PostgreSQL: firma bazlı veri yedekleme (tek DB içinde)
                if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                {
                    await CreateFirmaBazliYedeklemeAsync(backupFolder, timestamp);
                }

                settings.LastBackupTime = DateTime.Now;
                await SaveSettingsAsync(settings);
                await CleanupOldBackupsAsync(settings.KeepBackupCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yedekleme hatasi");
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
    /// <summary>
    /// Tek PostgreSQL mimarisinde firma bazlı veri yedekleme.
    /// Tüm firmalar aynı DB'de olduğu için tek pg_dump yeterlidir.
    /// </summary>
    private async Task CreateFirmaBazliYedeklemeAsync(string backupFolder, string timestamp)
    {
        try
        {
            var connStr = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr)) return;

            var parts = ParseConnectionString(connStr);
            var dbName = parts.GetValueOrDefault("Database", "MKFiloServis");
            var safeDbName = SanitizeFileName(dbName);
            var backupPath = Path.Combine(backupFolder, $"MKFiloServis_{safeDbName}_{timestamp}.backup");

            await RunPgDumpForDatabaseAsync(connStr, backupPath, dbName);

            // Firma listesini logla (tek DB içinde)
            using var scope = _serviceProvider.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            await using var ctx = await factory.CreateDbContextAsync();
            var firmaCount = await ctx.Firmalar.CountAsync(f => f.Aktif && !f.IsDeleted);
            _logger.LogInformation("Tek DB yedeklendi: {DbName}, {FirmaCount} aktif firma", dbName, firmaCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Firma bazli yedekleme hatasi (ana yedek alindi, ek yedekleme atlandi)");
        }
    }

    private async Task RunPgDumpForDatabaseAsync(string connectionString, string backupPath, string label)
    {
        try
        {
            var pgDumpPath = FindPgDump();
            if (string.IsNullOrWhiteSpace(pgDumpPath))
            {
                _logger.LogWarning("pg_dump bulunamadi, {Label} DB yedeklenemedi", label);
                return;
            }

            var connParts = ParseConnectionString(connectionString);
            var host = connParts.GetValueOrDefault("Host", "localhost");
            var port = connParts.GetValueOrDefault("Port", "5432");
            var username = connParts.GetValueOrDefault("Username", "postgres");
            var database = connParts.GetValueOrDefault("Database", "");

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pgDumpPath,
                Arguments = $"-h {host} -p {port} -U {username} -d {database} --format=custom --compress=9 --blobs --no-owner --no-privileges --encoding=UTF8 -f \"{backupPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            processInfo.Environment["PGPASSWORD"] = connParts.GetValueOrDefault("Password", "");

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                if (process.ExitCode == 0)
                    _logger.LogInformation("{Label} DB yedeklendi: {Path}", label, Path.GetFileName(backupPath));
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning("{Label} DB yedekleme hatasi: {Error}", label, error.Trim());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Label} DB yedekleme sirasinda hata", label);
        }
    }

    private static string BuildPgConnectionString(string host, string port, string database, string username, string password)
    {
        return $"Host={host};Port={port};Database={database};Username={username};Password={password};";
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(name.Length);
        foreach (var c in name)
            sanitized.Append(invalid.Contains(c) ? '_' : c);
        return sanitized.ToString();
    }

    /// <summary>SQLite backup devre disi — proje PostgreSQL-only. MKFiloServis.SqliteTool kullanin.</summary>
    private Task<BackupResult> CreateSqliteBackupAsync(string backupFilePath)
    {
        _logger.LogWarning("SQLite yedekleme desteklenmiyor: {Path}. Proje PostgreSQL-only.", backupFilePath);
        return Task.FromResult(new BackupResult { ErrorMessage = "SQLite yedekleme desteklenmiyor. Proje PostgreSQL-only mimariye gecildi." });
    }

    private async Task<BackupResult> CreatePostgreSqlBackupAsync(string backupFilePath)
    {
        var result = new BackupResult();

        try
        {
            var connectionString = ResolveConnectionString("PostgreSQL");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                result.ErrorMessage = "PostgreSQL connection string bulunamadi.";
                return result;
            }

            var connParts = ParseConnectionString(connectionString);
            var pgDumpPath = FindPgDump();

            if (string.IsNullOrWhiteSpace(pgDumpPath))
            {
                result.ErrorMessage = "pg_dump bulunamadi. PostgreSQL full dump icin PostgreSQL client araclari kurulmalidir.";
                return result;
            }

            var host = connParts.GetValueOrDefault("Host", "localhost");
            var port = connParts.GetValueOrDefault("Port", "5432");
            var username = connParts.GetValueOrDefault("Username", string.Empty);
            var database = connParts.GetValueOrDefault("Database", string.Empty);

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pgDumpPath,
                Arguments = $"-h {host} -p {port} -U {username} -d {database} --format=custom --compress=9 --blobs --verbose --no-owner --no-privileges --encoding=UTF8 -f \"{backupFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            processInfo.Environment["PGPASSWORD"] = connParts.GetValueOrDefault("Password", string.Empty);

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogError("pg_dump hatasi: {Error}", error);
                    result.ErrorMessage = $"pg_dump hatasi: {error}";
                    return result;
                }
            }

            if (File.Exists(backupFilePath))
                return CreateSuccessResult(backupFilePath);

            result.ErrorMessage = "pg_dump tamamlandi ancak dump dosyasi olusturulamadi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostgreSQL yedekleme hatasi");
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task<BackupResult> CreateMsSqlBackupAsync(string backupFilePath)
    {
        var result = new BackupResult();

        try
        {
            var connectionString = ResolveConnectionString("MSSQL");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var jsonPath = backupFilePath.Replace(".bak", ".json", StringComparison.OrdinalIgnoreCase);
                await CreateJsonBackupAsync(jsonPath);
                return CreateSuccessResult(jsonPath);
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dbName = context.Database.GetDbConnection().Database;
            var backupSql = $"BACKUP DATABASE [{dbName}] TO DISK = N'{backupFilePath}' WITH FORMAT, INIT, NAME = N'MKFiloServis Backup'";

            await context.Database.ExecuteSqlRawAsync(backupSql);

            if (File.Exists(backupFilePath))
                return CreateSuccessResult(backupFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MSSQL yedekleme hatasi");

            try
            {
                var jsonPath = backupFilePath.Replace(".bak", ".json", StringComparison.OrdinalIgnoreCase);
                await CreateJsonBackupAsync(jsonPath);
                return CreateSuccessResult(jsonPath);
            }
            catch
            {
                result.ErrorMessage = ex.Message;
            }
        }

        return result;
    }

    private async Task<BackupResult> CreateMySqlBackupAsync(string backupFilePath)
    {
        var result = new BackupResult();

        try
        {
            var connectionString = ResolveConnectionString("MySQL");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                result.ErrorMessage = "MySQL connection string bulunamadi.";
                return result;
            }

            var connParts = ParseConnectionString(connectionString);
            var mySqlDumpPath = FindMySqlDump();

            if (string.IsNullOrWhiteSpace(mySqlDumpPath))
            {
                var jsonPath = backupFilePath.Replace(".sql", ".json", StringComparison.OrdinalIgnoreCase);
                await CreateJsonBackupAsync(jsonPath);
                return CreateSuccessResult(jsonPath);
            }

            var host = connParts.GetValueOrDefault("Server") ?? connParts.GetValueOrDefault("Host") ?? "localhost";
            var port = connParts.GetValueOrDefault("Port") ?? "3306";
            var username = connParts.GetValueOrDefault("User") ?? connParts.GetValueOrDefault("User Id") ?? connParts.GetValueOrDefault("Username") ?? string.Empty;
            var password = connParts.GetValueOrDefault("Password") ?? string.Empty;
            var database = connParts.GetValueOrDefault("Database") ?? string.Empty;

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = mySqlDumpPath,
                Arguments = $"--host={host} --port={port} --user={username} --result-file=\"{backupFilePath}\" {database}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            processInfo.Environment["MYSQL_PWD"] = password;

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning("mysqldump hatasi: {Error}", error);

                    var jsonPath = backupFilePath.Replace(".sql", ".json", StringComparison.OrdinalIgnoreCase);
                    await CreateJsonBackupAsync(jsonPath);
                    backupFilePath = jsonPath;
                }
            }

            if (File.Exists(backupFilePath))
                return CreateSuccessResult(backupFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MySQL yedekleme hatasi");
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task CreateJsonBackupAsync(string filePath)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var provider = GetCurrentDatabaseProvider();
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            var tables = await GetUserTablesAsync(connection, provider);
            var exportedTables = new List<object>(tables.Count);

            foreach (var table in tables)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {BuildQualifiedTableName(table, provider)}";

                await using var reader = await command.ExecuteReaderAsync();
                var rows = new List<Dictionary<string, object?>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = ReadBackupValue(reader, i);
                    }

                    rows.Add(row);
                }

                exportedTables.Add(new
                {
                    Schema = table.Schema,
                    TableName = table.Name,
                    RowCount = rows.Count,
                    Rows = rows
                });
            }

            var backup = new
            {
                ExportDateUtc = DateTime.UtcNow,
                DatabaseProvider = provider,
                BackupType = "FullLogicalJson",
                TableCount = exportedTables.Count,
                Tables = exportedTables
            };

            var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            await File.WriteAllTextAsync(filePath, json);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private async Task<List<BackupTableDefinition>> GetUserTablesAsync(DbConnection connection, string provider)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = provider.ToUpperInvariant() switch
        {
            "POSTGRESQL" => @"
                SELECT table_schema, table_name
                FROM information_schema.tables
                WHERE table_type = 'BASE TABLE'
                  AND table_schema = current_schema()
                ORDER BY table_schema, table_name",
            "MYSQL" => @"
                SELECT table_schema, table_name
                FROM information_schema.tables
                WHERE table_type = 'BASE TABLE'
                  AND table_schema = DATABASE()
                ORDER BY table_schema, table_name",
            "MSSQL" or "SQLSERVER" => @"
                SELECT s.name AS table_schema, t.name AS table_name
                FROM sys.tables t
                INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                WHERE t.is_ms_shipped = 0
                ORDER BY s.name, t.name",
            _ => @"
                SELECT NULL AS table_schema, name AS table_name
                FROM sqlite_master
                WHERE type = 'table'
                  AND name NOT LIKE 'sqlite_%'
                ORDER BY name"
        };

        var tables = new List<BackupTableDefinition>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var schema = reader.IsDBNull(0) ? null : reader.GetString(0);
            var name = reader.GetString(1);
            tables.Add(new BackupTableDefinition(schema, name));
        }

        return tables;
    }

    private static string BuildQualifiedTableName(BackupTableDefinition table, string provider)
    {
        var tableName = QuoteIdentifier(table.Name, provider);
        return string.IsNullOrWhiteSpace(table.Schema)
            ? tableName
            : $"{QuoteIdentifier(table.Schema, provider)}.{tableName}";
    }

    private static string QuoteIdentifier(string identifier, string provider)
    {
        return provider.ToUpperInvariant() switch
        {
            "MSSQL" or "SQLSERVER" => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]",
            "MYSQL" => $"`{identifier.Replace("`", "``", StringComparison.Ordinal)}`",
            _ => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
        };
    }

    private static object? ReadBackupValue(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetValue(ordinal);
        return value switch
        {
            byte[] bytes => Convert.ToBase64String(bytes),
            char ch => ch.ToString(),
            Guid guid => guid.ToString(),
            TimeSpan timeSpan => timeSpan.ToString(),
            DateOnly dateOnly => dateOnly.ToString("O"),
            TimeOnly timeOnly => timeOnly.ToString("O"),
            _ => value
        };
    }

    private sealed record BackupTableDefinition(string? Schema, string Name);

    private Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2)
                parts[keyValue[0].Trim()] = keyValue[1].Trim();
        }

        if (!parts.ContainsKey("Port"))
            parts["Port"] = "5432";
        if (!parts.ContainsKey("Host") && parts.ContainsKey("Server"))
            parts["Host"] = parts["Server"];
        if (!parts.ContainsKey("Host"))
            parts["Host"] = "localhost";

        return parts;
    }

    private string? FindPgDump()
    {
        var possiblePaths = new[]
        {
            @"C:\Program Files\PostgreSQL\17\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\13\bin\pg_dump.exe",
            "/usr/bin/pg_dump",
            "/usr/local/bin/pg_dump"
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    private string? FindMySqlDump()
    {
        var possiblePaths = new[]
        {
            @"C:\Program Files\MySQL\MySQL Server 8.4\bin\mysqldump.exe",
            @"C:\Program Files\MySQL\MySQL Server 8.3\bin\mysqldump.exe",
            @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe",
            @"C:\xampp\mysql\bin\mysqldump.exe",
            "/usr/bin/mysqldump",
            "/usr/local/bin/mysqldump"
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    private string? ResolveConnectionString(string provider)
    {
        var dbSettings = ReadDatabaseSettings();
        if (dbSettings != null)
        {
            var settingsProvider = dbSettings.Provider switch
            {
                DatabaseProvider.PostgreSQL => "POSTGRESQL",
                DatabaseProvider.SQLServer => "SQLSERVER",
                DatabaseProvider.MySQL => "MYSQL",
                DatabaseProvider.SQLite => "SQLITE",
                _ => string.Empty
            };

            if (string.Equals(settingsProvider, provider, StringComparison.OrdinalIgnoreCase) ||
                (provider.Equals("MSSQL", StringComparison.OrdinalIgnoreCase) && settingsProvider == "SQLSERVER"))
            {
                return dbSettings.GetConnectionString();
            }
        }

        var directConnection = provider.ToUpperInvariant() switch
        {
            "POSTGRESQL" => _configuration.GetConnectionString("PostgreSQL"),
            "MSSQL" or "SQLSERVER" => _configuration.GetConnectionString("MSSQL") ?? _configuration.GetConnectionString("SqlServer"),
            "SQLITE" => _configuration.GetConnectionString("SQLite"),
            "MYSQL" => _configuration.GetConnectionString("MySQL"),
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(directConnection))
            return directConnection;

        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(defaultConnection))
        {
            var inferredProvider = GetProviderFromConnectionString(defaultConnection);
            if (string.Equals(inferredProvider, provider, StringComparison.OrdinalIgnoreCase) ||
                (provider.Equals("MSSQL", StringComparison.OrdinalIgnoreCase) && inferredProvider == "SQLSERVER"))
            {
                return defaultConnection;
            }
        }

        return null;
    }

    private string GetProviderFromConnectionString(string connectionString)
    {
        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase))
            return "POSTGRESQL";

        if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
            return "SQLITE";

        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) &&
            connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString.Contains("User Id=", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.Contains("Integrated Security=", StringComparison.OrdinalIgnoreCase)
                ? "SQLSERVER"
                : "MYSQL";
        }

        return string.Empty;
    }

    private DatabaseSettings? ReadDatabaseSettings()
    {
        try
        {
            var dbSettingsPath = Path.Combine(_environment.ContentRootPath, "dbsettings.json");
            if (!File.Exists(dbSettingsPath))
                return null;

            var json = File.ReadAllText(dbSettingsPath);
            return JsonSerializer.Deserialize<DatabaseSettings>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "dbsettings.json okunamadi");
            return null;
        }
    }

    private static BackupResult CreateSuccessResult(string filePath)
    {
        long fileSize = 0;
        try
        {
            fileSize = new FileInfo(filePath).Length;
        }
        catch (IOException) { /* dosya hala yazılıyor olabilir, boyutu 0 bırak */ }

        return new BackupResult
        {
            Success = true,
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            FileSizeBytes = fileSize,
            CreatedAt = DateTime.Now
        };
    }

    public Task<List<BackupInfo>> GetBackupListAsync()
    {
        var settings = GetSettings();
        var backupFolder = GetBackupFolderPath(settings);
        var backups = new List<BackupInfo>();

        if (Directory.Exists(backupFolder))
        {
            // MKFiloServis_ ile baslayan dosyalar
            var crmFiles = Directory.GetFiles(backupFolder, "MKFiloServis_*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".sql") || f.EndsWith(".json") || f.EndsWith(".db") || f.EndsWith(".bak") || f.EndsWith(".backup"));

            // uploaded_ ile baslayan dosyalar (disaridan yuklenen)
            var uploadedFiles = Directory.GetFiles(backupFolder, "uploaded_*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".sql") || f.EndsWith(".db") || f.EndsWith(".bak") || f.EndsWith(".backup"));

            // Diger yedek dosyalari
            var otherFiles = Directory.GetFiles(backupFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).StartsWith("MKFiloServis_") && 
                            !Path.GetFileName(f).StartsWith("uploaded_") &&
                            (f.EndsWith(".sql") || f.EndsWith(".db") || f.EndsWith(".bak") || f.EndsWith(".backup")));

            var allFiles = crmFiles.Concat(uploadedFiles).Concat(otherFiles)
                .Distinct()
                .OrderByDescending(f => new FileInfo(f).CreationTime);

            foreach (var file in allFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    backups.Add(new BackupInfo
                    {
                        FileName = fileInfo.Name,
                        FilePath = file,
                        FileSizeBytes = fileInfo.Length,
                        CreatedAt = fileInfo.CreationTime
                    });
                }
                catch (IOException)
                {
                    // Dosya baska bir islem tarafindan kullaniliyorsa atla
                    _logger.LogWarning("Yedek listesine eklenemedi (kullanımda): {File}", file);
                }
            }
        }

        return Task.FromResult(backups);
    }

    public async Task<bool> RestoreBackupAsync(string backupFileName)
    {
        try
        {
            var settings = GetSettings();
            var backupFolder = GetBackupFolderPath(settings);
            var backupFilePath = FindBackupFilePath(backupFolder, backupFileName);

            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
            {
                _logger.LogError("Yedek dosyasi bulunamadi: {FileName}", backupFileName);
                return false;
            }

            var dbProvider = GetCurrentDatabaseProvider();
            _logger.LogInformation("Restore baslatiliyor: {FileName}, Provider: {Provider}", backupFileName, dbProvider);

            // SQLite restore
            if (backupFilePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            {
                return await RestoreSqliteAsync(backupFilePath);
            }

            // JSON restore desteklenmiyor
            if (backupFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("JSON yedekten geri yukleme henuz desteklenmiyor.");
                return false;
            }

            // PostgreSQL restore
            if (backupFilePath.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && 
                (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || backupFileName.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase)))
            {
                return await RestorePostgreSqlAsync(backupFilePath);
            }

            // MSSQL restore
            if (backupFilePath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) && 
                (dbProvider.Equals("MSSQL", StringComparison.OrdinalIgnoreCase) || dbProvider.Equals("SQLServer", StringComparison.OrdinalIgnoreCase)))
            {
                return await RestoreMsSqlAsync(backupFilePath);
            }

            // MySQL restore
            if (backupFilePath.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && 
                (dbProvider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || backupFileName.Contains("MySQL", StringComparison.OrdinalIgnoreCase)))
            {
                return await RestoreMySqlAsync(backupFilePath);
            }

            // .backup uzantılı dosyalar için provider'a göre restore dene
            if (backupFilePath.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
            {
                return dbProvider.ToUpperInvariant() switch
                {
                    "POSTGRESQL" => await RestorePostgreSqlAsync(backupFilePath),
                    "MYSQL" => await RestoreMySqlAsync(backupFilePath),
                    _ => false
                };
            }

            _logger.LogWarning("Desteklenmeyen yedek formati: {FileName}", backupFileName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore hatasi");
            return false;
        }
    }

    private async Task<bool> RestoreSqliteAsync(string backupFilePath)
    {
        try
        {
            var dbSettings = ReadDatabaseSettings();
            if (dbSettings == null)
            {
                _logger.LogError("SQLite restore icin dbsettings.json bulunamadi.");
                return false;
            }

            var sqliteSettings = new DatabaseSettings
            {
                Provider = DatabaseProvider.SQLite,
                CanonicalProvider = DatabaseProvider.PostgreSQL,
                DatabaseName = dbSettings.GetNormalizedSqliteDatabaseName()
            };

            var targetPath = sqliteSettings.GetNormalizedSqliteDatabaseName();
            if (!Path.IsPathRooted(targetPath))
            {
                targetPath = Path.Combine(_environment.ContentRootPath, targetPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? _environment.ContentRootPath);
            File.Copy(backupFilePath, targetPath, overwrite: true);
            _logger.LogInformation("SQLite yedegi hedef veritabanina geri yuklendi: {Path}", targetPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQLite restore hatasi");
            return false;
        }
    }

    private async Task<bool> RestorePostgreSqlAsync(string backupFilePath)
    {
        try
        {
            var connectionString = ResolveConnectionString("PostgreSQL");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogError("PostgreSQL connection string bulunamadi");
                return false;
            }

            var connParts = ParseConnectionString(connectionString);
            var psqlPath = FindPsql();

            if (string.IsNullOrWhiteSpace(psqlPath))
            {
                _logger.LogError("psql bulunamadi. PostgreSQL client kurulu olmali.");
                return false;
            }

            var host = connParts.GetValueOrDefault("Host", "localhost");
            var port = connParts.GetValueOrDefault("Port", "5432");
            var username = connParts.GetValueOrDefault("Username", string.Empty);
            var database = connParts.GetValueOrDefault("Database", string.Empty);
            var password = connParts.GetValueOrDefault("Password", string.Empty);

            if (backupFilePath.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
            {
                var pgRestorePath = FindPgRestore();
                if (string.IsNullOrWhiteSpace(pgRestorePath))
                {
                    _logger.LogError("pg_restore bulunamadi. PostgreSQL custom dump geri yukleme icin PostgreSQL client araclari kurulmalidir.");
                    return false;
                }

                var restoreInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pgRestorePath,
                    Arguments = $"-h {host} -p {port} -U {username} -d {database} --clean --if-exists --no-owner --no-privileges --verbose \"{backupFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                restoreInfo.Environment["PGPASSWORD"] = password;

                _logger.LogInformation("PostgreSQL restore calistiriliyor: pg_restore -h {Host} -d {Database}", host, database);

                using var restoreProcess = System.Diagnostics.Process.Start(restoreInfo);
                if (restoreProcess != null)
                {
                    var restoreError = await restoreProcess.StandardError.ReadToEndAsync();
                    await restoreProcess.WaitForExitAsync();

                    if (restoreProcess.ExitCode == 0)
                    {
                        _logger.LogInformation("PostgreSQL restore basarili");
                        return true;
                    }

                    _logger.LogError("PostgreSQL restore hatasi: {Error}", restoreError);
                }

                return false;
            }

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = psqlPath,
                Arguments = $"-h {host} -p {port} -U {username} -d {database} -f \"{backupFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            processInfo.Environment["PGPASSWORD"] = password;

            _logger.LogInformation("PostgreSQL restore calistiriliyor: psql -h {Host} -d {Database}", host, database);

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("PostgreSQL restore basarili");
                    return true;
                }
                else
                {
                    _logger.LogError("PostgreSQL restore hatasi: {Error}", error);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostgreSQL restore hatasi");
            return false;
        }
    }

    private async Task<bool> RestoreMsSqlAsync(string backupFilePath)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dbName = context.Database.GetDbConnection().Database;

            var restoreSql = $@"
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                RESTORE DATABASE [{dbName}] FROM DISK = N'{backupFilePath}' WITH REPLACE;
                ALTER DATABASE [{dbName}] SET MULTI_USER;";

            await context.Database.ExecuteSqlRawAsync(restoreSql);
            _logger.LogInformation("MSSQL restore basarili");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MSSQL restore hatasi");
            return false;
        }
    }

    private async Task<bool> RestoreMySqlAsync(string backupFilePath)
    {
        try
        {
            var connectionString = ResolveConnectionString("MySQL");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogError("MySQL connection string bulunamadi");
                return false;
            }

            var connParts = ParseConnectionString(connectionString);
            var mysqlPath = FindMySql();

            if (string.IsNullOrWhiteSpace(mysqlPath))
            {
                _logger.LogError("mysql client bulunamadi.");
                return false;
            }

            var host = connParts.GetValueOrDefault("Server") ?? connParts.GetValueOrDefault("Host") ?? "localhost";
            var port = connParts.GetValueOrDefault("Port") ?? "3306";
            var username = connParts.GetValueOrDefault("User") ?? connParts.GetValueOrDefault("User Id") ?? connParts.GetValueOrDefault("Username") ?? string.Empty;
            var password = connParts.GetValueOrDefault("Password") ?? string.Empty;
            var database = connParts.GetValueOrDefault("Database") ?? string.Empty;

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = mysqlPath,
                Arguments = $"--host={host} --port={port} --user={username} {database} < \"{backupFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            processInfo.Environment["MYSQL_PWD"] = password;

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                // SQL dosyasını stdin'e yaz
                var sql = await File.ReadAllTextAsync(backupFilePath);
                await process.StandardInput.WriteAsync(sql);
                process.StandardInput.Close();

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("MySQL restore basarili");
                    return true;
                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogError("MySQL restore hatasi: {Error}", error);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MySQL restore hatasi");
            return false;
        }
    }

    private string? FindPsql()
    {
        var pgDumpPath = FindPgDump();
        if (!string.IsNullOrWhiteSpace(pgDumpPath))
        {
            var psqlPath = pgDumpPath.Replace("pg_dump", "psql", StringComparison.OrdinalIgnoreCase);
            if (File.Exists(psqlPath))
                return psqlPath;
        }

        var commonPaths = new[]
        {
            @"C:\Program Files\PostgreSQL\17\bin\psql.exe",
            @"C:\Program Files\PostgreSQL\16\bin\psql.exe",
            @"C:\Program Files\PostgreSQL\15\bin\psql.exe",
            @"C:\Program Files\PostgreSQL\14\bin\psql.exe",
            "/usr/bin/psql",
            "/usr/local/bin/psql"
        };

        return commonPaths.FirstOrDefault(File.Exists);
    }

    private string? FindPgRestore()
    {
        var pgDumpPath = FindPgDump();
        if (!string.IsNullOrWhiteSpace(pgDumpPath))
        {
            var pgRestorePath = pgDumpPath.Replace("pg_dump", "pg_restore", StringComparison.OrdinalIgnoreCase);
            if (File.Exists(pgRestorePath))
                return pgRestorePath;
        }

        var commonPaths = new[]
        {
            @"C:\Program Files\PostgreSQL\17\bin\pg_restore.exe",
            @"C:\Program Files\PostgreSQL\16\bin\pg_restore.exe",
            @"C:\Program Files\PostgreSQL\15\bin\pg_restore.exe",
            @"C:\Program Files\PostgreSQL\14\bin\pg_restore.exe",
            "/usr/bin/pg_restore",
            "/usr/local/bin/pg_restore"
        };

        return commonPaths.FirstOrDefault(File.Exists);
    }

    private string? FindMySql()
    {
        var commonPaths = new[]
        {
            @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe",
            @"C:\Program Files\MySQL\MySQL Server 5.7\bin\mysql.exe",
            "/usr/bin/mysql",
            "/usr/local/bin/mysql"
        };

        return commonPaths.FirstOrDefault(File.Exists);
    }

    public async Task<bool> DeleteBackupAsync(string backupFileName)
    {
        try
        {
            var settings = GetSettings();
            var backupFolder = GetBackupFolderPath(settings);
            var backupFilePath = FindBackupFilePath(backupFolder, backupFileName);

            if (!string.IsNullOrWhiteSpace(backupFilePath) && File.Exists(backupFilePath))
            {
                File.Delete(backupFilePath);
                _logger.LogInformation("Yedek silindi: {FileName}", backupFileName);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yedek silme hatasi");
            return false;
        }
    }

    public async Task CleanupOldBackupsAsync(int keepCount = 10)
    {
        try
        {
            var settings = GetSettings();
            var backupFolder = GetBackupFolderPath(settings);

            if (!Directory.Exists(backupFolder))
                return;

            var files = Directory.GetFiles(backupFolder, "MKFiloServis_*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".sql") || f.EndsWith(".json") || f.EndsWith(".db") || f.EndsWith(".bak") || f.EndsWith(".backup"))
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .Skip(keepCount)
                .ToList();

            foreach (var file in files)
            {
                File.Delete(file);
                _logger.LogInformation("Eski yedek silindi: {FileName}", Path.GetFileName(file));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eski yedekleri temizleme hatasi");
        }
    }

    public BackupSettings GetSettings()
    {
        try
        {
            if (File.Exists(_settingsFile))
            {
                var json = File.ReadAllText(_settingsFile);
                return JsonSerializer.Deserialize<BackupSettings>(json) ?? new BackupSettings();
            }
        }
        catch
        {
        }

        return new BackupSettings();
    }

    public async Task SaveSettingsAsync(BackupSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ayar kaydetme hatasi");
        }
    }

    private string GetBackupFolderPath(BackupSettings settings)
    {
        var folder = settings.BackupFolder;

        if (string.IsNullOrWhiteSpace(folder))
            folder = "database";

        if (!Path.IsPathRooted(folder))
            folder = Path.Combine(AppStoragePaths.GetStorageRoot(_environment.ContentRootPath), folder);

        return folder;
    }

    private static string GetArchiveFolderPath(string backupRoot, DateTime tarih)
    {
        return Path.Combine(backupRoot, tarih.ToString("yyyy"), tarih.ToString("MM"));
    }

    private static string? FindBackupFilePath(string backupRoot, string backupFileName)
    {
        if (string.IsNullOrWhiteSpace(backupRoot) || !Directory.Exists(backupRoot))
            return null;

        return Directory.GetFiles(backupRoot, backupFileName, SearchOption.AllDirectories)
            .OrderByDescending(f => new FileInfo(f).CreationTime)
            .FirstOrDefault();
    }

    public async Task<bool> ApplyMigrationsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            var pendingCount = pendingMigrations.Count();

            if (pendingCount > 0)
            {
                _logger.LogInformation("{Count} adet migration uygulanacak", pendingCount);
                await context.Database.MigrateAsync();
                _logger.LogInformation("Migration basariyla uygulandi");
                return true;
            }
            else
            {
                _logger.LogInformation("Uygulanacak migration yok, EnsureCreated deneniyor");
                await context.Database.EnsureCreatedAsync();
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration hatasi");
            throw;
        }
    }

    public async Task<ConversionResult> ConvertAndRestoreAsync(string backupFileName, string sourceProvider, string targetProvider)
    {
        try
        {
            var settings = GetSettings();
            var backupFolder = GetBackupFolderPath(settings);
            var backupFilePath = FindBackupFilePath(backupFolder, backupFileName);

            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
            {
                var message = $"Yedek dosyasi bulunamadi: {backupFileName}";
                _logger.LogError("{Message}", message);
                return new ConversionResult { Success = false, Message = message };
            }

            _logger.LogInformation("Veritabani donusumu baslatiliyor: {Source} -> {Target}", sourceProvider, targetProvider);

            if (sourceProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) &&
                targetProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
            {
                if (backupFilePath.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                {
                    return await ImportPostgreSqlDumpToSqliteAsync(backupFilePath);
                }

                if (backupFilePath.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
                {
                    return await ImportPostgreSqlCustomBackupToSqliteAsync(backupFilePath);
                }
            }

            var jsonData = await ReadBackupToJsonAsync(backupFilePath, sourceProvider);
            if (jsonData == null)
            {
                const string message = "Yedek dosyasi okunamadi veya desteklenmeyen formatta.";
                _logger.LogError(message);
                return new ConversionResult { Success = false, Message = message };
            }

            var result = await WriteJsonToTargetDatabaseAsync(jsonData, targetProvider);
            if (result)
            {
                _logger.LogInformation("Veritabani donusumu basarili: {Source} -> {Target}", sourceProvider, targetProvider);
                return new ConversionResult { Success = true, Message = $"Veritabani donusumu basarili: {sourceProvider} -> {targetProvider}" };
            }

            return new ConversionResult { Success = false, Message = "Veritabani donusumu tamamlanamadi." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veritabani donusum hatasi");
            return new ConversionResult { Success = false, Message = ex.GetBaseException().Message };
        }
    }

    private async Task<ConversionResult> ImportPostgreSqlCustomBackupToSqliteAsync(string backupFilePath)
    {
        var tempSqlPath = Path.Combine(Path.GetTempPath(), $"mkfiloservis_pgsql_to_sqlite_{Guid.NewGuid():N}.sql");

        try
        {
            var pgRestorePath = FindPgRestore();
            if (string.IsNullOrWhiteSpace(pgRestorePath))
            {
                const string message = "pg_restore bulunamadi. PostgreSQL custom backup dosyasi SQLite'a aktarılamaz.";
                _logger.LogError(message);
                return new ConversionResult { Success = false, Message = message };
            }

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pgRestorePath,
                Arguments = $"--no-owner --no-privileges --data-only --format=custom --file \"{tempSqlPath}\" \"{backupFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process == null)
            {
                const string message = "pg_restore islemi baslatilamadi.";
                _logger.LogError(message);
                return new ConversionResult { Success = false, Message = message };
            }

            var errorOutput = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var message = $"pg_restore SQL donusum hatasi: {errorOutput}";
                _logger.LogError("{Message}", message);
                return new ConversionResult { Success = false, Message = message };
            }

            return await ImportPostgreSqlDumpToSqliteAsync(tempSqlPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostgreSQL custom backup SQLite aktarim hatasi");
            return new ConversionResult { Success = false, Message = ex.GetBaseException().Message };
        }
        finally
        {
            try
            {
                if (File.Exists(tempSqlPath))
                {
                    File.Delete(tempSqlPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gecici SQL donusum dosyasi silinemedi: {Path}", tempSqlPath);
            }
        }
    }

    private async Task<ConversionResult> ImportPostgreSqlDumpToSqliteAsync(string backupFilePath)
    {
        try
        {
            var copyBlocks = await ParsePostgreSqlCopyBlocksAsync(backupFilePath);
            if (copyBlocks.Count == 0)
            {
                var message = $"PostgreSQL dump icinde aktarilacak COPY blogu bulunamadi: {backupFilePath}";
                _logger.LogError("{Message}", message);
                return new ConversionResult { Success = false, Message = message };
            }

            using var scope = _serviceProvider.CreateScope();
            var sqliteSettings = ReadDatabaseSettings();
            if (sqliteSettings == null)
            {
                const string message = "SQLite hedef ayarlari bulunamadi. dbsettings.json okunamadi.";
                _logger.LogError(message);
                return new ConversionResult { Success = false, Message = message };
            }

            sqliteSettings.Provider = DatabaseProvider.SQLite;
            sqliteSettings.DatabaseName = sqliteSettings.GetNormalizedSqliteDatabaseName();

            var sqlitePath = sqliteSettings.GetNormalizedSqliteDatabaseName();
            if (!Path.IsPathRooted(sqlitePath))
            {
                sqlitePath = Path.Combine(_environment.ContentRootPath, sqlitePath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(sqlitePath) ?? _environment.ContentRootPath);
            var sqliteConnectionString = $"Data Source={sqlitePath}";

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite(sqliteConnectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
                sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            await using var context = new ApplicationDbContext(optionsBuilder.Options);
            context.SetServiceProvider(scope.ServiceProvider);
            await context.Database.EnsureCreatedAsync();

            await context.Database.OpenConnectionAsync();
            await SetSqliteForeignKeysAsync(context, enabled: false);

            var strategy = context.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var targetTables = await GetSqliteUserTablesAsync(context);
                    await ClearSqliteTablesAsync(context, targetTables);

                    foreach (var block in copyBlocks)
                    {
                        if (!targetTables.Contains(block.TableName, StringComparer.OrdinalIgnoreCase))
                        {
                            _logger.LogDebug("Hedef SQLite veritabaninda olmayan tablo atlandi: {TableName}", block.TableName);
                            continue;
                        }

                        await InsertCopyBlockIntoSqliteAsync(context, block);
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            await SetSqliteForeignKeysAsync(context, enabled: true);
            await context.Database.CloseConnectionAsync();
            if (result)
            {
                _logger.LogInformation("PostgreSQL dump SQLite veritabanina basariyla aktarildi: {Path} -> {SqlitePath}", backupFilePath, sqlitePath);
                return new ConversionResult { Success = true, Message = $"PostgreSQL yedegi SQLite veritabanina aktarildi: {sqlitePath}" };
            }

            return new ConversionResult { Success = false, Message = "PostgreSQL dump SQLite veritabanina yazilamadi." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostgreSQL dump SQLite aktarim hatasi");
            return new ConversionResult { Success = false, Message = ex.GetBaseException().Message };
        }
    }

    private static async Task SetSqliteForeignKeysAsync(ApplicationDbContext context, bool enabled)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_keys = {(enabled ? 1 : 0)};";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ClearSqliteTablesAsync(ApplicationDbContext context, List<string> targetTables)
    {
        var orderedTables = new[]
        {
            "MuhasebeFisKalemleri",
            "MuhasebeFisleri",
            "BankaKasaHareketleri",
            "FaturaKalemleri",
            "Faturalar",
            "ServisCalismalari",
            "AracMasraflari",
            "StokKartlari",
            "StokKategoriler",
            "BankaHesaplari",
            "Kullanicilar",
            "Personeller",
            "Soforler",
            "Guzergahlar",
            "Araclar",
            "Cariler",
            "MuhasebeHesaplari",
            "Roller",
            "Firmalar"
        };

        foreach (var tableName in orderedTables.Where(targetTables.Contains))
        {
#pragma warning disable EF1002 // Tablo isimleri güvenli kaynaktan geliyor ve escape ediliyor
            await context.Database.ExecuteSqlRawAsync($"DELETE FROM {EscapeSqliteIdentifier(tableName)};");
#pragma warning restore EF1002
        }

        foreach (var tableName in targetTables.Except(orderedTables, StringComparer.OrdinalIgnoreCase))
        {
#pragma warning disable EF1002 // Tablo isimleri güvenli kaynaktan geliyor ve escape ediliyor
            await context.Database.ExecuteSqlRawAsync($"DELETE FROM {EscapeSqliteIdentifier(tableName)};");
#pragma warning restore EF1002
        }

        await context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence;");
    }

    private async Task<List<PostgreSqlCopyBlock>> ParsePostgreSqlCopyBlocksAsync(string backupFilePath)
    {
        var blocks = new List<PostgreSqlCopyBlock>();
        PostgreSqlCopyBlock? currentBlock = null;

        await using var stream = File.OpenRead(backupFilePath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        while (await reader.ReadLineAsync() is { } line)
        {
            var trimmedLine = line.Trim();

            if (currentBlock == null)
            {
                if (!trimmedLine.StartsWith("COPY ", StringComparison.OrdinalIgnoreCase) ||
                    !trimmedLine.EndsWith("FROM stdin;", StringComparison.OrdinalIgnoreCase))
                    continue;

                var openParenIndex = trimmedLine.IndexOf('(');
                var closeParenIndex = trimmedLine.LastIndexOf(") FROM stdin;", StringComparison.OrdinalIgnoreCase);
                if (openParenIndex < 0 || closeParenIndex <= openParenIndex)
                    continue;

                var targetPart = trimmedLine[5..openParenIndex].Trim();
                if (targetPart.StartsWith("public.", StringComparison.OrdinalIgnoreCase))
                {
                    targetPart = targetPart[7..];
                }

                var tableName = targetPart.Trim().Trim('"');
                var columnPart = trimmedLine[(openParenIndex + 1)..closeParenIndex];

                currentBlock = new PostgreSqlCopyBlock(
                    tableName,
                    columnPart
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(static c => c.Trim().Trim('"'))
                        .ToList());

                continue;
            }

            if (trimmedLine == @"\.")
            {
                blocks.Add(currentBlock);
                currentBlock = null;
                continue;
            }

            currentBlock.Rows.Add(line.Split('\t').Select(ParsePostgreSqlCopyValue).ToList());
        }

        return blocks;
    }

    private async Task<List<string>> GetSqliteUserTablesAsync(ApplicationDbContext context)
    {
        var result = new List<string>();
        var connection = context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = @"
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%'
              AND name <> '__EFMigrationsHistory';";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    private async Task<HashSet<string>> GetSqliteTableColumnsAsync(ApplicationDbContext context, string tableName)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = $"PRAGMA table_info({EscapeSqliteIdentifier(tableName)});";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(1));
        }

        return result;
    }

    private async Task InsertCopyBlockIntoSqliteAsync(ApplicationDbContext context, PostgreSqlCopyBlock block)
    {
        var targetColumns = await GetSqliteTableColumnsAsync(context, block.TableName);
        var mappedColumns = block.Columns
            .Select((columnName, index) => new { columnName, index })
            .Where(x => targetColumns.Contains(x.columnName))
            .ToList();

        if (mappedColumns.Count == 0)
        {
            _logger.LogWarning("SQLite hedefinde eslesen kolon bulunamadi, tablo atlandi: {TableName}", block.TableName);
            return;
        }

        var firmaIdFallback = await ResolveSqliteFirmaIdFallbackAsync(context, block.TableName, targetColumns);
        var columnList = string.Join(", ", mappedColumns.Select(x => EscapeSqliteIdentifier(x.columnName)));
        var parameterList = string.Join(", ", Enumerable.Range(0, mappedColumns.Count).Select(i => $"@p{i}"));
        var insertSql = $"INSERT INTO {EscapeSqliteIdentifier(block.TableName)} ({columnList}) VALUES ({parameterList});";
        var connection = context.Database.GetDbConnection();

        foreach (var row in block.Rows)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = insertSql;

            for (var i = 0; i < mappedColumns.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{i}";

                var valueIndex = mappedColumns[i].index;
                var value = valueIndex < row.Count ? row[valueIndex] : null;
                value = NormalizeSqliteInsertValue(block.TableName, mappedColumns[i].columnName, value, firmaIdFallback);
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task<int?> ResolveSqliteFirmaIdFallbackAsync(ApplicationDbContext context, string tableName, HashSet<string> targetColumns)
    {
        if (!targetColumns.Contains("FirmaId"))
            return null;

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT \"Id\" FROM \"Firmalar\" WHERE COALESCE(\"IsDeleted\", 0) = 0 ORDER BY \"Id\" LIMIT 1;";

        var scalar = await command.ExecuteScalarAsync();
        if (scalar == null || scalar == DBNull.Value)
            return 1;

        return Convert.ToInt32(scalar);
    }

    private static object? NormalizeSqliteInsertValue(string tableName, string columnName, object? value, int? firmaIdFallback)
    {
        if (!columnName.Equals("FirmaId", StringComparison.OrdinalIgnoreCase))
            return value;

        if (value is int intValue)
            return intValue > 0 ? intValue : firmaIdFallback ?? 1;

        if (value is long longValue)
            return longValue > 0 ? longValue : firmaIdFallback ?? 1;

        if (value is string stringValue)
        {
            if (int.TryParse(stringValue, out var parsedValue) && parsedValue > 0)
                return parsedValue;

            if (string.IsNullOrWhiteSpace(stringValue) || stringValue == "0")
                return firmaIdFallback ?? 1;
        }

        if (value == null || value == DBNull.Value)
            return firmaIdFallback ?? 1;

        return value;
    }

    private static object? ParsePostgreSqlCopyValue(string rawValue)
    {
        if (rawValue == @"\N")
            return null;

        if (rawValue.StartsWith(@"\x", StringComparison.OrdinalIgnoreCase) && rawValue.Length > 2)
        {
            try
            {
                return Convert.FromHexString(rawValue[2..]);
            }
            catch
            {
            }
        }

        var builder = new StringBuilder(rawValue.Length);
        for (var i = 0; i < rawValue.Length; i++)
        {
            if (rawValue[i] != '\\' || i == rawValue.Length - 1)
            {
                builder.Append(rawValue[i]);
                continue;
            }

            i++;
            builder.Append(rawValue[i] switch
            {
                't' => '\t',
                'n' => '\n',
                'r' => '\r',
                'b' => '\b',
                'f' => '\f',
                'v' => '\v',
                '\\' => '\\',
                _ => rawValue[i]
            });
        }

        var value = builder.ToString();
        return value switch
        {
            "t" => 1,
            "f" => 0,
            _ => value
        };
    }

    private static string EscapeSqliteIdentifier(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"")}\"";

    // ─────────────────────────────────────────────────────────────────
    // Dosya / Veri Yedeği (ZIP)
    // ─────────────────────────────────────────────────────────────────

    public async Task<BackupResult> CreateFileBackupAsync(CancellationToken cancellationToken = default)
    {
        var result = new BackupResult();
        try
        {
            var storageRoot = AppStoragePaths.GetStorageRoot(_environment.ContentRootPath);
            var filesBackupDir = Path.Combine(storageRoot, "Backups", "Files");
            Directory.CreateDirectory(filesBackupDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var zipFileName = $"files_{timestamp}.zip";
            var zipFilePath = Path.Combine(filesBackupDir, zipFileName);

            // Master key kontrolü
            var masterKeyPath = Path.Combine(storageRoot, "keys", "master.key");
            if (!File.Exists(masterKeyPath))
            {
                result.ErrorMessage = "KRİTİK: master.key bulunamadı. Evrak şifreleri bu dosya olmadan açılamaz. Dosya yedeği alınmadı.";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            // Yedeklenecek kaynak klasörler
            var kaynakKlasorler = new Dictionary<string, string>
            {
                ["uploads"] = Path.Combine(storageRoot, "uploads"),
                ["Arsiv"] = Path.Combine(storageRoot, "Arsiv"),
                ["keys"] = Path.Combine(storageRoot, "keys"),
                ["logs"] = Path.Combine(storageRoot, "logs"),
                ["data"] = Path.Combine(storageRoot, "data")
            };

            using var zipStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write);
            using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create);

            foreach (var (klasorAdi, kaynakYol) in kaynakKlasorler)
            {
                if (!Directory.Exists(kaynakYol))
                {
                    _logger.LogWarning("Yedeklenecek klasör bulunamadı, atlandı: {Path}", kaynakYol);
                    continue;
                }

                await ZipKlasorEkleAsync(archive, kaynakYol, klasorAdi, storageRoot, cancellationToken);
            }

            // Create modda archive.GetEntry/Entries erişimi yapılmamalı.
            // Güvence için kaynak dosya varlığını kullanıyoruz (yukarıda da kontrol edildi).
            if (!File.Exists(masterKeyPath))
            {
                result.ErrorMessage = "Kaynak depolamada keys/master.key bulunamadı.";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            result = CreateSuccessResult(zipFilePath);
            _logger.LogInformation("Dosya yedeği alındı: {Path} ({Size} bytes)", zipFilePath, result.FileSizeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya yedeği hatası");
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private static async Task ZipKlasorEkleAsync(
        System.IO.Compression.ZipArchive archive,
        string kaynakYol,
        string zipIciKlasorAdi,
        string storageRoot,
        CancellationToken ct)
    {
        foreach (var dosya in Directory.GetFiles(kaynakYol, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            // Backups klasörünü dahil etme
            var normalizedPath = dosya.Replace('\\', '/');
            if (normalizedPath.Contains("/Backups/"))
                continue;

            var relativePath = normalizedPath.Replace(kaynakYol.Replace('\\', '/'), "").TrimStart('/');
            var entryName = $"{zipIciKlasorAdi}/{relativePath}";

            var entry = archive.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await using var fileStream = File.OpenRead(dosya);
            await fileStream.CopyToAsync(entryStream, ct);
        }
    }

    public async Task<BackupResult> CreateFullBackupAsync(CancellationToken cancellationToken = default)
    {
        var dbResult = await CreateBackupAsync();
        var fileResult = await CreateFileBackupAsync(cancellationToken);

        return new BackupResult
        {
            Success = dbResult.Success && fileResult.Success,
            FileName = dbResult.Success
                ? $"database={Path.GetFileName(dbResult.FilePath ?? "")} + files={Path.GetFileName(fileResult.FilePath ?? "")}"
                : "Tam yedek başarısız",
            FilePath = dbResult.FilePath,
            FileSizeBytes = dbResult.FileSizeBytes + fileResult.FileSizeBytes,
            ErrorMessage = !dbResult.Success ? $"DB: {dbResult.ErrorMessage}" :
                           !fileResult.Success ? $"Dosya: {fileResult.ErrorMessage}" : null,
            CreatedAt = DateTime.Now
        };
    }

    public Task<List<BackupInfo>> GetFileBackupListAsync()
    {
        var backups = new List<BackupInfo>();
        try
        {
            var storageRoot = AppStoragePaths.GetStorageRoot(_environment.ContentRootPath);
            var filesBackupDir = Path.Combine(storageRoot, "Backups", "Files");

            if (Directory.Exists(filesBackupDir))
            {
                foreach (var file in Directory.GetFiles(filesBackupDir, "files_*.zip", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => new FileInfo(f).CreationTime))
                {
                    var fi = new FileInfo(file);
                    backups.Add(new BackupInfo
                    {
                        FileName = fi.Name,
                        FilePath = file,
                        FileSizeBytes = fi.Length,
                        CreatedAt = fi.CreationTime
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dosya yedek listesi alınamadı");
        }

        return Task.FromResult(backups);
    }

    public Task<bool> DeleteFileBackupAsync(string backupFileName)
    {
        try
        {
            var storageRoot = AppStoragePaths.GetStorageRoot(_environment.ContentRootPath);
            var filesBackupDir = Path.Combine(storageRoot, "Backups", "Files");
            var filePath = Path.Combine(filesBackupDir, backupFileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Dosya yedeği silindi: {FileName}", backupFileName);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya yedeği silme hatası");
            return Task.FromResult(false);
        }
    }

    public async Task CleanupOldFileBackupsAsync(int keepCount = 10)
    {
        try
        {
            var storageRoot = AppStoragePaths.GetStorageRoot(_environment.ContentRootPath);
            var filesBackupDir = Path.Combine(storageRoot, "Backups", "Files");

            if (!Directory.Exists(filesBackupDir))
                return;

            var files = Directory.GetFiles(filesBackupDir, "files_*.zip")
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .Skip(keepCount);

            foreach (var file in files)
            {
                File.Delete(file);
                _logger.LogInformation("Eski dosya yedeği silindi: {FileName}", Path.GetFileName(file));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eski dosya yedeklerini temizleme hatası");
        }

        await Task.CompletedTask;
    }

    private sealed class PostgreSqlCopyBlock(string tableName, List<string> columns)
    {
        public string TableName { get; } = tableName;
        public List<string> Columns { get; } = columns;
        public List<List<object?>> Rows { get; } = new();
    }

    private async Task<Dictionary<string, object>?> ReadBackupToJsonAsync(string backupFilePath, string sourceProvider)
    {
        try
        {
            // SQLite db dosyasından doğrudan oku
            if (backupFilePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            {
                return await ReadSqliteToJsonAsync(backupFilePath);
            }

            // JSON dosyası ise doğrudan oku
            if (backupFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var json = await File.ReadAllTextAsync(backupFilePath);
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            }

            // SQL dosyası için - mevcut DB context'ten oku
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var data = new Dictionary<string, object>
            {
                ["Cariler"] = await context.Cariler.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["Araclar"] = await context.Araclar.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["Soforler"] = await context.Soforler.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["Guzergahlar"] = await context.Guzergahlar.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["Faturalar"] = await context.Faturalar.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["FaturaKalemleri"] = await context.FaturaKalemleri.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["BankaHesaplari"] = await context.BankaHesaplari.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["BankaKasaHareketleri"] = await context.BankaKasaHareketleri.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["MuhasebeHesaplari"] = await context.MuhasebeHesaplari.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["MuhasebeFisleri"] = await context.MuhasebeFisleri.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["MuhasebeFisKalemleri"] = await context.MuhasebeFisKalemleri.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["Kullanicilar"] = await context.Kullanicilar.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["Roller"] = await context.Roller.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["StokKartlari"] = await context.StokKartlari.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
                ["StokKategoriler"] = await context.StokKategoriler.IgnoreQueryFilters().AsNoTracking().ToListAsync()
            };

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yedek okuma hatasi");
            return null;
        }
    }

    /// <summary>
    /// ESKI: SQLite .db backup dosyasindan EF Core ile entity okurdu.
    /// Proje PostgreSQL-only mimariye gectigi icin Microsoft.EntityFrameworkCore.Sqlite kaldirildi.
    /// SQLite backup'tan veri okuma icin ayri MKFiloServis.SqliteTool konsol araci kullanilmalidir.
    /// </summary>
    private Task<Dictionary<string, object>?> ReadSqliteToJsonAsync(string sqliteFilePath)
    {
        _logger.LogWarning("SQLite backup okuma desteklenmiyor: {Path}. EF Core SQLite provider kaldirildi.", sqliteFilePath);
        return Task.FromResult<Dictionary<string, object>?>(null);
    }

    private async Task<bool> WriteJsonToTargetDatabaseAsync(Dictionary<string, object> data, string targetProvider)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Önce mevcut tabloları temizle (dikkatli kullan!)
            _logger.LogWarning("Hedef veritabani tablolari temizleniyor...");

            // Transaction ile tüm işlemleri yap
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Tablolari sırayla temizle (foreign key sırasına dikkat)
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"MuhasebeFisKalemleri\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"MuhasebeFisleri\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"BankaKasaHareketleri\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"FaturaKalemleri\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Faturalar\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"StokKartlari\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"StokKategoriler\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"BankaHesaplari\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Soforler\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Guzergahlar\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Araclar\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Cariler\"");
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"MuhasebeHesaplari\"");

                    _logger.LogInformation("Tablolar temizlendi, veriler aktariliyor...");

                    // Verileri ekle
                    if (data.TryGetValue("MuhasebeHesaplari", out var hesaplar) && hesaplar is System.Text.Json.JsonElement hesaplarJson)
                    {
                        var list = hesaplarJson.Deserialize<List<MuhasebeHesap>>();
                        if (list?.Any() == true) context.MuhasebeHesaplari.AddRange(list);
                    }

                    if (data.TryGetValue("Cariler", out var cariler) && cariler is System.Text.Json.JsonElement carilerJson)
                    {
                        var list = carilerJson.Deserialize<List<Cari>>();
                        if (list?.Any() == true) context.Cariler.AddRange(list);
                    }

                    if (data.TryGetValue("Guzergahlar", out var guzergahlar) && guzergahlar is System.Text.Json.JsonElement guzergahlarJson)
                    {
                        var list = guzergahlarJson.Deserialize<List<Guzergah>>();
                        if (list?.Any() == true) context.Guzergahlar.AddRange(list);
                    }

                    if (data.TryGetValue("Araclar", out var araclar) && araclar is System.Text.Json.JsonElement araclarJson)
                    {
                        var list = araclarJson.Deserialize<List<Arac>>();
                        if (list?.Any() == true) context.Araclar.AddRange(list);
                    }

                    if (data.TryGetValue("Soforler", out var soforler) && soforler is System.Text.Json.JsonElement soforlerJson)
                    {
                        var list = soforlerJson.Deserialize<List<Sofor>>();
                        if (list?.Any() == true) context.Soforler.AddRange(list);
                    }

                    if (data.TryGetValue("BankaHesaplari", out var bankaHesaplari) && bankaHesaplari is System.Text.Json.JsonElement bankaHesaplariJson)
                    {
                        var list = bankaHesaplariJson.Deserialize<List<BankaHesap>>();
                        if (list?.Any() == true) context.BankaHesaplari.AddRange(list);
                    }

                    if (data.TryGetValue("StokKategoriler", out var stokKategorileri) && stokKategorileri is System.Text.Json.JsonElement stokKategorileriJson)
                    {
                        var list = stokKategorileriJson.Deserialize<List<StokKategori>>();
                        if (list?.Any() == true) context.StokKategoriler.AddRange(list);
                    }

                    if (data.TryGetValue("StokKartlari", out var stokKartlari) && stokKartlari is System.Text.Json.JsonElement stokKartlariJson)
                    {
                        var list = stokKartlariJson.Deserialize<List<StokKarti>>();
                        if (list?.Any() == true) context.StokKartlari.AddRange(list);
                    }

                    if (data.TryGetValue("Faturalar", out var faturalar) && faturalar is System.Text.Json.JsonElement faturalarJson)
                    {
                        var list = faturalarJson.Deserialize<List<Fatura>>();
                        if (list?.Any() == true) context.Faturalar.AddRange(list);
                    }

                    if (data.TryGetValue("FaturaKalemleri", out var faturaKalemleri) && faturaKalemleri is System.Text.Json.JsonElement faturaKalemleriJson)
                    {
                        var list = faturaKalemleriJson.Deserialize<List<FaturaKalem>>();
                        if (list?.Any() == true) context.FaturaKalemleri.AddRange(list);
                    }

                    if (data.TryGetValue("BankaKasaHareketleri", out var bankaHareketleri) && bankaHareketleri is System.Text.Json.JsonElement bankaHareketleriJson)
                    {
                        var list = bankaHareketleriJson.Deserialize<List<BankaKasaHareket>>();
                        if (list?.Any() == true) context.BankaKasaHareketleri.AddRange(list);
                    }

                    if (data.TryGetValue("MuhasebeFisleri", out var muhasebeFisler) && muhasebeFisler is System.Text.Json.JsonElement muhasebeFislerJson)
                    {
                        var list = muhasebeFislerJson.Deserialize<List<MuhasebeFis>>();
                        if (list?.Any() == true) context.MuhasebeFisleri.AddRange(list);
                    }

                    if (data.TryGetValue("MuhasebeFisKalemleri", out var muhasebeFisKalemleri) && muhasebeFisKalemleri is System.Text.Json.JsonElement muhasebeFisKalemleriJson)
                    {
                        var list = muhasebeFisKalemleriJson.Deserialize<List<MuhasebeFisKalem>>();
                        if (list?.Any() == true) context.MuhasebeFisKalemleri.AddRange(list);
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Veri aktarimi tamamlandi");
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hedef veritabanina yazma hatasi");
            return false;
        }
    }
}






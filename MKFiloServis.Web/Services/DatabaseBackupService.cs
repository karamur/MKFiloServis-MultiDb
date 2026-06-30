using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;
using Npgsql;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Otomatik veritabanı yedekleme servisi
/// PostgreSQL için pg_dump kullanır veya EF Core backup
/// </summary>
public class DatabaseBackupService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly IConfiguration _configuration;
    private Timer? _timer;
    private readonly string _backupPath;
    private readonly int _retentionDays;
    private readonly bool _enabled;

    public DatabaseBackupService(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseBackupService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
        
        _backupPath = configuration["Backup:Path"] ?? Path.Combine(AppContext.BaseDirectory, "backups");
        _retentionDays = configuration.GetValue("Backup:RetentionDays", 30);
        _enabled = configuration.GetValue("Backup:Enabled", true);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Veritabanı yedekleme servisi devre dışı");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Veritabanı yedekleme servisi başlatıldı");

        // Klasörü oluştur
        if (!Directory.Exists(_backupPath))
            Directory.CreateDirectory(_backupPath);

        // Her gün gece 03:00'te yedek al
        var now = DateTime.Now;
        var nextRun = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0);
        if (now > nextRun)
            nextRun = nextRun.AddDays(1);

        var initialDelay = nextRun - now;

        _timer = new Timer(ExecuteBackup, null, initialDelay, TimeSpan.FromDays(1));

        return Task.CompletedTask;
    }

    private async void ExecuteBackup(object? state)
    {
        try
        {
            _logger.LogInformation("Otomatik veritabanı yedekleme başlatıldı");
            
            var result = await CreateBackupAsync();
            
            if (result.Success)
            {
                _logger.LogInformation("Veritabanı yedeği oluşturuldu: {Path}", result.FilePath);
                
                // Eski yedekleri temizle
                CleanupOldBackups();
            }
            else
            {
                _logger.LogError("Veritabanı yedeği oluşturulamadı: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yedekleme sırasında hata oluştu");
        }
    }

    /// <summary>
    /// Manuel yedekleme oluşturur
    /// </summary>
    public async Task<BackupResult> CreateBackupAsync(string? customName = null)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = string.IsNullOrEmpty(customName) 
                ? $"KOAFiloServis_Backup_{timestamp}" 
                : $"{customName}_{timestamp}";

            var backupDir = Path.Combine(_backupPath, fileName);
            Directory.CreateDirectory(backupDir);

            // 1. Veritabanı yedeği (PostgreSQL full dump)
            var dumpFile = Path.Combine(backupDir, "database.backup");
            await ExportDatabaseToSqlAsync(dumpFile);

            // 2. Uploads klasörü yedeği
            var uploadsSource = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
            if (Directory.Exists(uploadsSource))
            {
                var uploadsDest = Path.Combine(backupDir, "uploads");
                CopyDirectory(uploadsSource, uploadsDest);
            }

            // 3. appsettings.json yedeği
            var settingsSource = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(settingsSource))
            {
                File.Copy(settingsSource, Path.Combine(backupDir, "appsettings.json"), true);
            }

            // 4. ZIP oluştur
            var zipPath = $"{backupDir}.zip";
            ZipFile.CreateFromDirectory(backupDir, zipPath, CompressionLevel.Optimal, false);

            // 5. Geçici klasörü sil
            Directory.Delete(backupDir, true);

            var fileInfo = new FileInfo(zipPath);

            return new BackupResult
            {
                Success = true,
                FileName = Path.GetFileName(zipPath),
                FilePath = zipPath,
                FileSizeBytes = fileInfo.Length,
                CreatedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yedekleme hatası");
            return new BackupResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task ExecuteScheduledBackupAsync()
    {
        _logger.LogInformation("Otomatik veritabanı yedekleme başlatıldı");

        var result = await CreateBackupAsync();

        if (result.Success)
        {
            _logger.LogInformation("Veritabanı yedeği oluşturuldu: {Path}", result.FilePath);
            CleanupOldBackups();
        }
        else
        {
            _logger.LogError("Veritabanı yedeği oluşturulamadı: {Error}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// Veritabanını SQL dosyasına export eder
    /// </summary>
    private async Task ExportDatabaseToSqlAsync(string filePath)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var connectionString = context.Database.GetConnectionString();
        
        // pg_dump varsa kullan, yoksa basit export yap
        var pgDumpPath = FindPgDump();
        
        if (!string.IsNullOrEmpty(pgDumpPath))
        {
            await ExportWithPgDumpAsync(connectionString!, filePath, pgDumpPath);
        }
        else
        {
            throw new InvalidOperationException("pg_dump bulunamadi. Full dump icin PostgreSQL client araclari kurulmalidir.");
        }
    }

    private string? FindPgDump()
    {
        var possiblePaths = new[]
        {
            @"C:\Program Files\PostgreSQL\17\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
            "/usr/bin/pg_dump",
            "/usr/local/bin/pg_dump"
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    private async Task ExportWithPgDumpAsync(string connectionString, string outputPath, string pgDumpPath)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        
        var args = $"-h {builder.Host} -p {builder.Port} -U {builder.Username} -d {builder.Database} --format=custom --compress=9 --blobs --verbose --no-owner --no-privileges --encoding=UTF8 -f \"{outputPath}\" --no-password";
        
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = pgDumpPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Environment = { { "PGPASSWORD", builder.Password } }
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process != null)
        {
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"pg_dump hatası: {error}");
            }
        }
    }

    private async Task ExportWithEFCoreAsync(ApplicationDbContext context, string filePath)
    {
        // Basit SQL export - tablo yapıları ve veriler
        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
        
        await writer.WriteLineAsync("-- CRM Filo Servis Database Backup");
        await writer.WriteLineAsync($"-- Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync("");

        // Tablo isimlerini al
        var tables = context.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .ToList();

        foreach (var tableName in tables)
        {
            try
            {
                await writer.WriteLineAsync($"-- Table: {tableName}");
                
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = $"SELECT * FROM \"{tableName}\"";
                
                await context.Database.OpenConnectionAsync();
                
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var columns = new List<string>();
                    var values = new List<string>();
                    
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        columns.Add($"\"{reader.GetName(i)}\"");
                        
                        if (reader.IsDBNull(i))
                            values.Add("NULL");
                        else if (reader.GetFieldType(i) == typeof(string) || reader.GetFieldType(i) == typeof(DateTime))
                            values.Add($"'{reader.GetValue(i).ToString()?.Replace("'", "''")}'");
                        else if (reader.GetFieldType(i) == typeof(bool))
                            values.Add(reader.GetBoolean(i) ? "TRUE" : "FALSE");
                        else
                            values.Add(reader.GetValue(i).ToString() ?? "NULL");
                    }
                    
                    await writer.WriteLineAsync($"INSERT INTO \"{tableName}\" ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)});");
                }
                
                await context.Database.CloseConnectionAsync();
                await writer.WriteLineAsync("");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"-- Error exporting {tableName}: {ex.Message}");
            }
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    /// <summary>
    /// Eski yedekleri temizler
    /// </summary>
    private void CleanupOldBackups()
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-_retentionDays);
            var backupFiles = Directory.GetFiles(_backupPath, "*.zip");

            foreach (var file in backupFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(file);
                    _logger.LogInformation("Eski yedek silindi: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eski yedekleri temizlerken hata");
        }
    }

    /// <summary>
    /// Mevcut yedekleri listeler
    /// </summary>
    public List<BackupInfo> GetBackupList()
    {
        var backups = new List<BackupInfo>();

        if (!Directory.Exists(_backupPath))
            return backups;

        foreach (var file in Directory.GetFiles(_backupPath, "*.zip").OrderByDescending(f => f))
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

        return backups;
    }

    /// <summary>
    /// Yedeği geri yükler
    /// </summary>
    public async Task<bool> RestoreBackupAsync(string backupPath)
    {
        // TODO: Restore işlemi - dikkatli kullanılmalı
        _logger.LogWarning("Restore işlemi henüz uygulanmadı: {Path}", backupPath);
        return await Task.FromResult(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Veritabanı yedekleme servisi durduruluyor");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}




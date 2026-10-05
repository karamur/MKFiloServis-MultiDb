using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
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
    private readonly IWebHostEnvironment _environment;
    private Timer? _timer;
    private string _backupPath = string.Empty;
    private readonly int _retentionDays;
    private readonly bool _enabled;

    public DatabaseBackupService(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseBackupService> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _environment = environment;
        _retentionDays = configuration.GetValue("Backup:RetentionDays", 30);
        _enabled = configuration.GetValue("Backup:Enabled", true);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Veritabanı yedekleme servisi devre dışı");
            return;
        }

        var configuredDirectory = AppStoragePaths.DefaultStorageRoot;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            configuredDirectory = await db.AppAyarlari
                .AsNoTracking()
                .Where(a => a.Anahtar == "BackupDizin" && a.Kategori == "Dizin")
                .Select(a => a.Deger)
                .FirstOrDefaultAsync(cancellationToken)
                ?? configuredDirectory;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dizin Yönetimi yedekleme yolu okunamadı; varsayılan yedekleme dizini kullanılacak.");
        }

        _backupPath = AppStoragePaths.GetWritableBackupFolder(AppContext.BaseDirectory, configuredDirectory);
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
            if (string.IsNullOrWhiteSpace(_backupPath))
                _backupPath = AppStoragePaths.GetWritableBackupFolder(AppContext.BaseDirectory, AppStoragePaths.DefaultStorageRoot);
            if (!string.IsNullOrEmpty(customName) && (Path.GetFileName(customName) != customName || customName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
                throw new ArgumentException("Yedek adı yalnız dosya adı olmalıdır.", nameof(customName));
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N");
            var fileName = string.IsNullOrEmpty(customName) 
                ? $"MKFiloServis_Backup_{timestamp}" 
                : $"{customName}_{timestamp}";

            var backupDir = Path.Combine(_backupPath, fileName);
            Directory.CreateDirectory(backupDir);

            // 1. Veritabanı yedeği (PostgreSQL full dump)
            var dumpFile = Path.Combine(backupDir, "database.backup");
            await ExportDatabaseToSqlAsync(dumpFile);

            // Aynı arşivde DB, kullanılan depolama, ayarlar ve DataProtection key ring.
            // Başarı bildirmeden kapatılmış ZIP, manifest ve SHA-256 değerleriyle kontrol edilir.
            var zipPath = $"{backupDir}.zip";
            using (var scope = _scopeFactory.CreateScope())
            {
                var protection = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
                await RecoveryArchive.CreateAsync(zipPath, _environment.ContentRootPath, protection, dumpFile, CancellationToken.None);
            }
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

        if (!context.Database.IsNpgsql())
            throw new NotSupportedException("ZIP veritabanı arşivi PostgreSQL içindir. Diğer sağlayıcılar için IBackupService kullanın.");
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
        
        var psi = PostgreSqlProcess(pgDumpPath, builder);
        foreach (var argument in new[] { "--format=custom", "--compress=9", "--blobs", "--no-owner", "--no-privileges", "--encoding=UTF8", "-f", outputPath })
            psi.ArgumentList.Add(argument);
        await RunPostgreSqlToolAsync(psi);
        if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            throw new IOException("pg_dump geçerli yedek oluşturmadı.");
    }

    private static System.Diagnostics.ProcessStartInfo PostgreSqlProcess(string executable, NpgsqlConnectionStringBuilder connection)
    {
        var info = new System.Diagnostics.ProcessStartInfo(executable)
        {
            UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true
        };
        foreach (var argument in new[] { "-h", connection.Host ?? "localhost", "-p", connection.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "-U", connection.Username ?? "", "-d", connection.Database ?? "", "--no-password" })
            info.ArgumentList.Add(argument);
        info.Environment["PGPASSWORD"] = connection.Password ?? "";
        return info;
    }

    private static async Task RunPostgreSqlToolAsync(System.Diagnostics.ProcessStartInfo info)
    {
        using var process = System.Diagnostics.Process.Start(info)
            ?? throw new IOException("PostgreSQL aracı başlatılamadı.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await output;
        var errorText = await error;
        if (process.ExitCode != 0) throw new IOException($"PostgreSQL aracı başarısız ({process.ExitCode}): {errorText}");
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
    /// ZIP içindeki PostgreSQL veritabanını atomik olarak geri yükler.
    /// Dosya ekleri ve appsettings.json, çalışır uygulamanın ayarlarını değiştirmeden arşivde tutulur.
    /// </summary>
    public async Task<bool> RestoreBackupAsync(string backupPath)
    {
        if (!backupPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var nativeScope = _scopeFactory.CreateScope();
            return await nativeScope.ServiceProvider.GetRequiredService<IBackupService>().RestoreBackupAsync(backupPath);
        }
        var staging = Path.Combine(Path.GetTempPath(), "MKFiloServis-Restore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            using var archive = ZipFile.OpenRead(backupPath);
            // Yeni biçimde bozuk/eksik arşiv DB'ye dokunmadan reddedilir.
            if (archive.GetEntry("recovery-manifest.json") != null)
                await RecoveryArchive.VerifyAsync(backupPath, CancellationToken.None);
            var candidates = archive.Entries.Where(e => e.FullName == "database.backup").ToList();
            if (candidates.Count != 1) throw new InvalidDataException("Arşiv tek database.backup kaydı içermelidir.");
            var entry = candidates[0];
            if (entry.Length <= 5 || entry.Length > 20L * 1024 * 1024 * 1024)
                throw new InvalidDataException("Veritabanı yedeğinin boyutu geçersiz.");
            var dump = Path.Combine(staging, "database.backup");
            entry.ExtractToFile(dump);
            await using (var stream = File.OpenRead(dump))
            {
                var magic = new byte[5];
                await stream.ReadExactlyAsync(magic);
                if (System.Text.Encoding.ASCII.GetString(magic) != "PGDMP")
                    throw new InvalidDataException("PostgreSQL custom dump bekleniyor.");
            }
            using var scope = _scopeFactory.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            await using var context = await factory.CreateDbContextAsync();
            if (!context.Database.IsNpgsql()) throw new NotSupportedException("PostgreSQL ZIP yedeği farklı sağlayıcıya yüklenemez.");
            var restoreTool = Path.Combine(Path.GetDirectoryName(FindPgDump()) ?? "", OperatingSystem.IsWindows() ? "pg_restore.exe" : "pg_restore");
            if (!File.Exists(restoreTool)) throw new FileNotFoundException("pg_restore bulunamadı.");
            // Geri dönüş için mevcut durumun yedeği tamamlanmadan restore başlatma.
            var recoveryBackup = await CreateBackupAsync("BeforeRestore");
            if (!recoveryBackup.Success) throw new IOException("Geri dönüş yedeği alınamadı: " + recoveryBackup.ErrorMessage);
            var info = PostgreSqlProcess(restoreTool, new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString()));
            foreach (var argument in new[] { "--single-transaction", "--exit-on-error", "--clean", "--if-exists", "--no-owner", "--no-privileges", dump })
                info.ArgumentList.Add(argument);
            await RunPostgreSqlToolAsync(info);
            _logger.LogInformation("ZIP veritabanı geri yüklemesi tamamlandı. Geri dönüş yedeği: {RecoveryBackup}", recoveryBackup.FilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZIP veritabanı geri yüklemesi başarısız");
            return false;
        }
        finally
        {
            // staging sabit geçici kök altında bu çağrıya özel oluşturulmuştur.
            Directory.Delete(staging, recursive: true);
        }
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




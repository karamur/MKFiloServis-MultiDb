using System.Diagnostics;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Sistem sağlık kontrolü servisi
/// </summary>
public interface ISystemHealthService
{
    Task<SystemHealthReport> GetHealthReportAsync();
    Task<DatabaseHealthInfo> CheckDatabaseHealthAsync();
    Task<DiskHealthInfo> CheckDiskHealthAsync();
    Task<MemoryHealthInfo> CheckMemoryHealthAsync();
}

public class SystemHealthService : ISystemHealthService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemHealthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public SystemHealthService(
        IServiceScopeFactory scopeFactory,
        ILogger<SystemHealthService> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<SystemHealthReport> GetHealthReportAsync()
    {
        var report = new SystemHealthReport
        {
            CheckedAt = DateTime.Now,
            MachineName = Environment.MachineName,
            OsVersion = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            DotNetVersion = Environment.Version.ToString()
        };

        // Paralel kontroller
        var tasks = new List<Task>
        {
            Task.Run(async () => report.Database = await CheckDatabaseHealthAsync()),
            Task.Run(async () => report.Disk = await CheckDiskHealthAsync()),
            Task.Run(async () => report.Memory = await CheckMemoryHealthAsync())
        };

        await Task.WhenAll(tasks);

        // Uygulama bilgileri
        report.Uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
        report.ThreadCount = Process.GetCurrentProcess().Threads.Count;

        // Genel durum belirleme
        report.OverallStatus = DetermineOverallStatus(report);

        return report;
    }

    public async Task<DatabaseHealthInfo> CheckDatabaseHealthAsync()
    {
        var info = new DatabaseHealthInfo();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var sw = Stopwatch.StartNew();
            
            // Basit sorgu ile bağlantı testi
            var canConnect = await context.Database.CanConnectAsync();
            sw.Stop();

            info.IsHealthy = canConnect;
            info.ResponseTimeMs = sw.ElapsedMilliseconds;

            if (canConnect)
            {
                var providerName = context.Database.ProviderName ?? _configuration["DatabaseProvider"] ?? "Bilinmiyor";
                info.ProviderName = providerName;

                // Tablo sayısı
                info.TableCount = context.Model.GetEntityTypes().Count();

                if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ||
                    providerName.Contains("SQLite", StringComparison.OrdinalIgnoreCase))
                {
                    var connectionString = context.Database.GetConnectionString() ?? string.Empty;
                    var sqlitePath = ResolveSqlitePath(connectionString);
                    info.DatabasePath = sqlitePath;

                    if (File.Exists(sqlitePath))
                    {
                        var fileInfo = new FileInfo(sqlitePath);
                        info.DatabaseSizeBytes = fileInfo.Length;
                    }

                    info.ActiveConnections = canConnect ? 1 : 0;
                }
                else if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ||
                         providerName.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var dbName = context.Database.GetDbConnection().Database;
#pragma warning disable EF1002
                        var sizeQuery = await context.Database
                            .SqlQueryRaw<long>($"SELECT pg_database_size('{dbName}')")
                            .FirstOrDefaultAsync();
#pragma warning restore EF1002
                        info.DatabaseSizeBytes = sizeQuery;
                    }
                    catch
                    {
                    }

                    try
                    {
                        var activeConnections = await context.Database
                            .SqlQueryRaw<int>("SELECT count(*) FROM pg_stat_activity WHERE state = 'active'")
                            .FirstOrDefaultAsync();
                        info.ActiveConnections = activeConnections;
                    }
                    catch
                    {
                    }
                }
                else
                {
                    info.ActiveConnections = canConnect ? 1 : 0;
                }
            }
        }
        catch (Exception ex)
        {
            info.IsHealthy = false;
            info.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Veritabanı sağlık kontrolü başarısız");
        }

        return info;
    }

    public Task<DiskHealthInfo> CheckDiskHealthAsync()
    {
        var info = new DiskHealthInfo();

        try
        {
            var appPath = AppContext.BaseDirectory;
            var driveInfo = new DriveInfo(Path.GetPathRoot(appPath) ?? "C:\\");

            info.IsHealthy = driveInfo.IsReady;
            info.DriveName = driveInfo.Name;
            info.DriveFormat = driveInfo.DriveFormat;
            info.TotalSizeBytes = driveInfo.TotalSize;
            info.FreeSpaceBytes = driveInfo.AvailableFreeSpace;
            info.UsedSpaceBytes = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
            info.UsedPercentage = Math.Round((double)info.UsedSpaceBytes / info.TotalSizeBytes * 100, 1);

            // %90 üzeri kritik
            if (info.UsedPercentage > 90)
            {
                info.IsHealthy = false;
                info.WarningMessage = "Disk alanı kritik seviyede!";
            }
            else if (info.UsedPercentage > 80)
            {
                info.WarningMessage = "Disk alanı azalıyor";
            }
        }
        catch (Exception ex)
        {
            info.IsHealthy = false;
            info.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Disk sağlık kontrolü başarısız");
        }

        return Task.FromResult(info);
    }

    public Task<MemoryHealthInfo> CheckMemoryHealthAsync()
    {
        var info = new MemoryHealthInfo();

        try
        {
            var process = Process.GetCurrentProcess();

            info.WorkingSetBytes = process.WorkingSet64;
            info.PrivateMemoryBytes = process.PrivateMemorySize64;
            info.VirtualMemoryBytes = process.VirtualMemorySize64;
            info.GcTotalMemory = GC.GetTotalMemory(false);
            info.Gen0Collections = GC.CollectionCount(0);
            info.Gen1Collections = GC.CollectionCount(1);
            info.Gen2Collections = GC.CollectionCount(2);

            // 2GB üzeri working set kritik
            info.IsHealthy = info.WorkingSetBytes < 2L * 1024 * 1024 * 1024;

            if (!info.IsHealthy)
            {
                info.WarningMessage = "Bellek kullanımı yüksek!";
            }
        }
        catch (Exception ex)
        {
            info.IsHealthy = false;
            info.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Bellek sağlık kontrolü başarısız");
        }

        return Task.FromResult(info);
    }

    private HealthStatus DetermineOverallStatus(SystemHealthReport report)
    {
        if (!report.Database.IsHealthy)
            return HealthStatus.Critical;

        if (!report.Disk.IsHealthy || !report.Memory.IsHealthy)
            return HealthStatus.Warning;

        if (report.Database.ResponseTimeMs > 1000)
            return HealthStatus.Warning;

        return HealthStatus.Healthy;
    }

    private string ResolveSqlitePath(string connectionString)
    {
        const string prefix = "Data Source=";
        var source = connectionString;
        var index = source.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);

        if (index >= 0)
        {
            source = source[(index + prefix.Length)..];
        }

        var endIndex = source.IndexOf(';');
        if (endIndex >= 0)
        {
            source = source[..endIndex];
        }

        source = source.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(source))
        {
            source = "MKFiloServis.db";
        }

        return Path.IsPathRooted(source)
            ? source
            : Path.Combine(_environment.ContentRootPath, source);
    }
}

public class SystemHealthReport
{
    public DateTime CheckedAt { get; set; }
    public string MachineName { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public int ProcessorCount { get; set; }
    public string DotNetVersion { get; set; } = "";
    public TimeSpan Uptime { get; set; }
    public int ThreadCount { get; set; }
    
    public DatabaseHealthInfo Database { get; set; } = new();
    public DiskHealthInfo Disk { get; set; } = new();
    public MemoryHealthInfo Memory { get; set; } = new();
    
    public HealthStatus OverallStatus { get; set; }
    
    public string UptimeFormatted => Uptime.TotalDays >= 1 
        ? $"{(int)Uptime.TotalDays} gün {Uptime.Hours} saat" 
        : $"{Uptime.Hours} saat {Uptime.Minutes} dakika";
}

public class DatabaseHealthInfo
{
    public bool IsHealthy { get; set; }
    public long ResponseTimeMs { get; set; }
    public int TableCount { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public int ActiveConnections { get; set; }
    public string ProviderName { get; set; } = "";
    public string DatabasePath { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    
    public string DatabaseSizeFormatted => DatabaseSizeBytes switch
    {
        < 1024 => $"{DatabaseSizeBytes} B",
        < 1024 * 1024 => $"{DatabaseSizeBytes / 1024.0:N1} KB",
        < 1024 * 1024 * 1024 => $"{DatabaseSizeBytes / 1024.0 / 1024.0:N1} MB",
        _ => $"{DatabaseSizeBytes / 1024.0 / 1024.0 / 1024.0:N2} GB"
    };
}

public class DiskHealthInfo
{
    public bool IsHealthy { get; set; }
    public string DriveName { get; set; } = "";
    public string DriveFormat { get; set; } = "";
    public long TotalSizeBytes { get; set; }
    public long FreeSpaceBytes { get; set; }
    public long UsedSpaceBytes { get; set; }
    public double UsedPercentage { get; set; }
    public string WarningMessage { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    
    public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);
    public string FreeSpaceFormatted => FormatBytes(FreeSpaceBytes);
    public string UsedSpaceFormatted => FormatBytes(UsedSpaceBytes);
    
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:N1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / 1024.0 / 1024.0:N1} MB",
        _ => $"{bytes / 1024.0 / 1024.0 / 1024.0:N2} GB"
    };
}

public class MemoryHealthInfo
{
    public bool IsHealthy { get; set; }
    public long WorkingSetBytes { get; set; }
    public long PrivateMemoryBytes { get; set; }
    public long VirtualMemoryBytes { get; set; }
    public long GcTotalMemory { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public string WarningMessage { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    
    public string WorkingSetFormatted => FormatBytes(WorkingSetBytes);
    public string GcMemoryFormatted => FormatBytes(GcTotalMemory);
    
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:N1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / 1024.0 / 1024.0:N1} MB",
        _ => $"{bytes / 1024.0 / 1024.0 / 1024.0:N2} GB"
    };
}

public enum HealthStatus
{
    Healthy,
    Warning,
    Critical
}





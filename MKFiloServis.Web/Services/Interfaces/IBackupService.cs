namespace MKFiloServis.Web.Services.Interfaces;

public interface IBackupService
{
    string GetCurrentDatabaseProvider();
    Task<BackupResult> CreateBackupAsync(string? customBackupFolder = null);
    Task<List<BackupInfo>> GetBackupListAsync();
    Task<bool> RestoreBackupAsync(string backupFileName);
    Task<bool> DeleteBackupAsync(string backupFileName);
    Task CleanupOldBackupsAsync(int keepCount = 10);
    BackupSettings GetSettings();
    string GetResolvedBackupFolderPath();
    Task SaveSettingsAsync(BackupSettings settings);
    Task<bool> ApplyMigrationsAsync();
    Task<ConversionResult> ConvertAndRestoreAsync(string backupFileName, string sourceProvider, string targetProvider);

    // ── Dosya / Veri Yedeği ─────────────────────────────────────────
    Task<BackupResult> CreateFileBackupAsync(CancellationToken cancellationToken = default);
    Task<BackupResult> CreateFullBackupAsync(CancellationToken cancellationToken = default);
    Task<List<BackupInfo>> GetFileBackupListAsync();
    Task<bool> DeleteFileBackupAsync(string backupFileName);
    Task CleanupOldFileBackupsAsync(int keepCount = 10);
    Task<RecoveryPreparationResult> PrepareRecoveryAsync(string backupFileName, CancellationToken cancellationToken = default);
}

public sealed class RecoveryPreparationResult
{
    public bool Prepared { get; init; }
    public string? FolderPath { get; init; }
    public int FileCount { get; init; }
    public bool KeyProbeVerified { get; init; }
    public bool HasDatabaseDump { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class BackupResult
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BackupInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }

    public string FileSizeFormatted => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB"
    };
}

public class ConversionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BackupSettings
{
    public bool AutoBackupEnabled { get; set; } = true;
    public int AutoBackupIntervalHours { get; set; } = 24;
    public BackupScheduleType ScheduleType { get; set; } = BackupScheduleType.Daily;
    public int ScheduledHour { get; set; } = 3;
    public int ScheduledMinute { get; set; } = 0;
    public DayOfWeek WeeklyBackupDay { get; set; } = DayOfWeek.Sunday;
    public int KeepBackupCount { get; set; } = 10;
    public string BackupFolder { get; set; } = "database";
    public DateTime? LastBackupTime { get; set; }

    public DateTime? GetNextBackupTime(DateTime? referenceTime = null)
    {
        if (!AutoBackupEnabled)
            return null;

        var now = referenceTime ?? MKFiloServis.Shared.Time.BusinessTime.Now;

        return ScheduleType switch
        {
            BackupScheduleType.Interval => GetNextIntervalTime(now),
            BackupScheduleType.Weekly => GetNextWeeklyTime(now),
            _ => GetNextDailyTime(now)
        };
    }

    public bool ShouldRun(DateTime? referenceTime = null)
    {
        if (!AutoBackupEnabled)
            return false;

        var now = referenceTime ?? MKFiloServis.Shared.Time.BusinessTime.Now;

        return ScheduleType switch
        {
            BackupScheduleType.Interval => ShouldRunInterval(now),
            BackupScheduleType.Weekly => ShouldRunWeekly(now),
            _ => ShouldRunDaily(now)
        };
    }

    private DateTime GetNextIntervalTime(DateTime now)
    {
        var intervalHours = Math.Max(1, AutoBackupIntervalHours);
        return LastBackupTime.HasValue
            ? GetLastBackupBusinessTime()!.Value.AddHours(intervalHours)
            : now;
    }

    private bool ShouldRunInterval(DateTime now)
    {
        return !LastBackupTime.HasValue || now >= GetLastBackupBusinessTime()!.Value.AddHours(Math.Max(1, AutoBackupIntervalHours));
    }

    private DateTime GetNextDailyTime(DateTime now)
    {
        var todayRun = now.Date.AddHours(ClampHour(ScheduledHour)).AddMinutes(ClampMinute(ScheduledMinute));

        if (!LastBackupTime.HasValue)
            return now <= todayRun ? todayRun : todayRun.AddDays(1);

        if (GetLastBackupBusinessTime()!.Value.Date < now.Date && now >= todayRun)
            return now;

        return now < todayRun ? todayRun : todayRun.AddDays(1);
    }

    private bool ShouldRunDaily(DateTime now)
    {
        var scheduledTime = now.Date.AddHours(ClampHour(ScheduledHour)).AddMinutes(ClampMinute(ScheduledMinute));
        if (now < scheduledTime)
            return false;

        return !LastBackupTime.HasValue || GetLastBackupBusinessTime()!.Value < scheduledTime;
    }

    private DateTime GetNextWeeklyTime(DateTime now)
    {
        var scheduledTime = now.Date.AddHours(ClampHour(ScheduledHour)).AddMinutes(ClampMinute(ScheduledMinute));
        var daysUntilTarget = ((int)WeeklyBackupDay - (int)now.DayOfWeek + 7) % 7;
        var nextRun = scheduledTime.AddDays(daysUntilTarget);

        if (!LastBackupTime.HasValue)
            return now <= nextRun ? nextRun : nextRun.AddDays(7);

        if (daysUntilTarget == 0 && now >= scheduledTime && GetLastBackupBusinessTime()!.Value < scheduledTime)
            return now;

        return now < nextRun ? nextRun : nextRun.AddDays(7);
    }

    private bool ShouldRunWeekly(DateTime now)
    {
        if (now.DayOfWeek != WeeklyBackupDay)
            return false;

        var scheduledTime = now.Date.AddHours(ClampHour(ScheduledHour)).AddMinutes(ClampMinute(ScheduledMinute));
        if (now < scheduledTime)
            return false;

        return !LastBackupTime.HasValue || GetLastBackupBusinessTime()!.Value < scheduledTime;
    }

    private DateTime? GetLastBackupBusinessTime()
    {
        if (!LastBackupTime.HasValue)
            return null;

        var recordedTime = LastBackupTime.Value;
        var utcInstant = recordedTime.Kind switch
        {
            DateTimeKind.Utc => recordedTime,
            DateTimeKind.Local => recordedTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(recordedTime, DateTimeKind.Utc)
        };

        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        return TimeZoneInfo.ConvertTimeFromUtc(utcInstant, turkeyTimeZone);
    }
    private static int ClampHour(int hour) => Math.Clamp(hour, 0, 23);
    private static int ClampMinute(int minute) => Math.Clamp(minute, 0, 59);
}

public enum BackupScheduleType
{
    Interval = 0,
    Daily = 1,
    Weekly = 2
}




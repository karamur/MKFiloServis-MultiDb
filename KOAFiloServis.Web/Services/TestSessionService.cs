using KOAFiloServis.Shared;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Canlı veriyle güvenli test oturumu.
/// SQL backup → test → SQL restore.
/// </summary>
public class TestSessionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<TestSessionService> _logger;

    private static readonly string[] _backupTables = new[]
    {
        "MaasOdemeSnapshotlar",
        "HakedisPuantajlar",
        "Faturalar",
        "MuhasebeFisleri",
        "SnapshotTransactions"
    };

    private static readonly Regex SafeIdentifierRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public bool IsTestActive { get; private set; }

    public TestSessionService(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<TestSessionService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>Test oturumu başlat — SQL backup al</summary>
    public async Task<TestBaslatSonuc> BaslatAsync(string tag)
    {
        if (IsTestActive)
            return new TestBaslatSonuc { Basarili = false, Mesaj = "Test zaten aktif! Önce geri alın." };

        var sonuc = new TestBaslatSonuc();
        await using var db = await _dbFactory.CreateDbContextAsync();

        try
        {
            // Her tablo için backup oluştur
            foreach (var table in _backupTables)
            {
                var safeTable = EnsureSafeIdentifier(table, nameof(table));
                var backupName = BuildBackupTableName(safeTable, tag);
                await ExecuteNonQueryAsync(db, $"DROP TABLE IF EXISTS \"{backupName}\"");
                await ExecuteNonQueryAsync(db, $"CREATE TABLE \"{backupName}\" AS SELECT * FROM \"{safeTable}\"");
                sonuc.BackupTables.Add(backupName);
            }

            IsTestActive = true;
            AppMode.EnterTestMode(tag);

            var sessionId = await db.TestSessionLogs.AnyAsync()
                ? await db.TestSessionLogs.MaxAsync(t => t.SessionId) + 1
                : 1;
            AppMode.CurrentSessionId = sessionId;

            sonuc.Basarili = true;
            sonuc.SessionId = sessionId;
            sonuc.Mesaj = $"Test başladı. {_backupTables.Length} tablo yedeklendi. Session={sessionId}";

            _logger.LogWarning("TEST BAŞLADI: Tag={Tag} Backup={Count} tablo", tag, _backupTables.Length);
        }
        catch (Exception ex)
        {
            IsTestActive = false;
            AppMode.ExitTestMode();
            sonuc.Basarili = false;
            sonuc.Mesaj = $"Backup BAŞARISIZ: {ex.Message}";
            _logger.LogError(ex, "Test backup hatası");
        }

        return sonuc;
    }

    /// <summary>Test oturumu başlat (basit)</summary>
    public async Task<int> BeginSessionAsync(string tag)
    {
        AppMode.EnterTestMode(tag);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var sessionId = await db.TestSessionLogs.AnyAsync()
            ? await db.TestSessionLogs.MaxAsync(t => t.SessionId) + 1
            : 1;
        AppMode.CurrentSessionId = sessionId;

        _logger.LogWarning("TEST MODU AKTİF: Tag={Tag} Session={SessionId}", tag, sessionId);
        return sessionId;
    }

    /// <summary>Test kaydı logla</summary>
    public async Task LogAsync(string entityAdi, int entityId, string islemTipi = "Insert")
    {
        if (!AppMode.IsTestMode || AppMode.CurrentSessionId == null) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.TestSessionLogs.Add(new TestSessionLog
        {
            SessionId = AppMode.CurrentSessionId.Value,
            TestTag = AppMode.CurrentTestTag ?? "UNKNOWN",
            EntityAdi = entityAdi,
            EntityId = entityId,
            IslemTipi = islemTipi,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    /// <summary>SQL Restore — tüm tabloları eski haline döndür</summary>
    public async Task<TestRollbackSonuc> GeriAlAsync(string tag)
    {
        var sonuc = new TestRollbackSonuc();
        if (!IsTestActive) { sonuc.Mesaj = "Aktif test yok."; return sonuc; }

        await using var db = await _dbFactory.CreateDbContextAsync();

        try
        {
            foreach (var table in _backupTables)
            {
                var safeTable = EnsureSafeIdentifier(table, nameof(table));
                var backupName = BuildBackupTableName(safeTable, tag);

                // Backup tablosu var mı kontrol et
                var exists = await TableExistsAsync(db, backupName);
                if (!exists) continue;

                // Tabloyu temizle ve backup'tan geri yükle
                await ExecuteNonQueryAsync(db, $"TRUNCATE TABLE \"{safeTable}\" CASCADE");
                await ExecuteNonQueryAsync(db, $"INSERT INTO \"{safeTable}\" SELECT * FROM \"{backupName}\"");
            }

            sonuc.Basarili = true;
            sonuc.Mesaj = $"Restore başarılı: {_backupTables.Length} tablo eski haline döndü.";
            _logger.LogWarning("TEST RESTORE BAŞARILI: Tag={Tag}", tag);
        }
        catch (Exception ex)
        {
            sonuc.Basarili = false;
            sonuc.Mesaj = $"Restore BAŞARISIZ: {ex.Message}";
            _logger.LogError(ex, "Test restore hatası");
        }
        finally
        {
            IsTestActive = false;
            AppMode.ExitTestMode();
        }

        return sonuc;
    }

    /// <summary>Backup tablolarını temizle</summary>
    public async Task TemizleAsync(string tag)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        foreach (var table in _backupTables)
        {
            var safeTable = EnsureSafeIdentifier(table, nameof(table));
            var backupName = BuildBackupTableName(safeTable, tag);
            await ExecuteNonQueryAsync(db, $"DROP TABLE IF EXISTS \"{backupName}\"");
        }
    }

    private static string BuildBackupTableName(string table, string tag)
    {
        var normalizedTag = NormalizeTag(tag);
        return EnsureSafeIdentifier($"backup_{table}_{normalizedTag}", nameof(tag));
    }

    private static string NormalizeTag(string tag)
    {
        var cleaned = new string((tag ?? string.Empty)
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_')
            .ToArray());

        return string.IsNullOrWhiteSpace(cleaned) ? "default" : cleaned;
    }

    private static string EnsureSafeIdentifier(string value, string paramName)
    {
        if (!SafeIdentifierRegex.IsMatch(value))
            throw new ArgumentException($"Geçersiz SQL identifier: {value}", paramName);

        return value;
    }

    private static async Task ExecuteNonQueryAsync(ApplicationDbContext db, string sql)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        if (command.Connection!.State != ConnectionState.Open)
            await command.Connection.OpenAsync();

        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> TableExistsAsync(ApplicationDbContext db, string tableName)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        if (command.Connection!.State != ConnectionState.Open)
            await command.Connection.OpenAsync();

        command.CommandText = "SELECT 1 FROM information_schema.tables WHERE table_name = @tableName LIMIT 1";
        var param = command.CreateParameter();
        param.ParameterName = "@tableName";
        param.Value = tableName;
        command.Parameters.Add(param);

        var result = await command.ExecuteScalarAsync();
        return result != null && result != DBNull.Value;
    }

    /// <summary>Test oturumunu SONLANDIR ve TÜM kayıtları GERİ AL (eski yöntem)</summary>
    public async Task<TestRollbackSonuc> RollbackSessionAsync()
    {
        var sonuc = new TestRollbackSonuc();
        if (AppMode.CurrentSessionId == null)
        {
            sonuc.Mesaj = "Aktif test oturumu yok.";
            return sonuc;
        }

        _logger.LogWarning("TEST ROLLBACK BAŞLADI: Session={SessionId}", AppMode.CurrentSessionId);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var sessionId = AppMode.CurrentSessionId.Value;
            var logs = await db.TestSessionLogs
                .Where(t => t.SessionId == sessionId && !t.IsDeleted)
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            sonuc.ToplamKayit = logs.Count;

            foreach (var log in logs)
            {
                try
                {
                    switch (log.EntityAdi)
                    {
                        case "HakedisPuantaj":
                            await db.HakedisPuantajlar.Where(h => h.Id == log.EntityId)
                                .ExecuteUpdateAsync(s => s
                                    .SetProperty(x => x.IsDeleted, true)
                                    .SetProperty(x => x.DeletedAt, DateTime.UtcNow));
                            sonuc.Silinen++;
                            break;
                        case "SnapshotTransaction":
                            await db.SnapshotTransactions.Where(t => t.Id == log.EntityId)
                                .ExecuteUpdateAsync(s => s
                                    .SetProperty(x => x.IsDeleted, true)
                                    .SetProperty(x => x.DeletedAt, DateTime.UtcNow));
                            sonuc.Silinen++;
                            break;
                        case "IncidentLog":
                            await db.IncidentLogs.Where(i => i.Id == log.EntityId)
                                .ExecuteUpdateAsync(s => s
                                    .SetProperty(x => x.IsDeleted, true)
                                    .SetProperty(x => x.DeletedAt, DateTime.UtcNow));
                            sonuc.Silinen++;
                            break;
                        default:
                            sonuc.Atlanan++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    sonuc.Hatalar.Add($"#{log.EntityId} {log.EntityAdi}: {ex.Message}");
                    sonuc.Hata++;
                }

                log.IsDeleted = true;
            }

            // Test session log'larını da temizle
            await db.TestSessionLogs.Where(t => t.SessionId == sessionId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletedAt, DateTime.UtcNow));

            await db.SaveChangesAsync();

            sonuc.Basarili = sonuc.Hata == 0;
            sonuc.Mesaj = sonuc.Basarili
                ? $"Rollback başarılı: {sonuc.Silinen} kayıt silindi, {sonuc.Atlanan} atlandı."
                : $"Rollback KISMİ: {sonuc.Silinen} silindi, {sonuc.Hata} hata.";
        }
        catch (Exception ex)
        {
            sonuc.Basarili = false;
            sonuc.Mesaj = $"Rollback BAŞARISIZ: {ex.Message}";
            _logger.LogError(ex, "Test rollback hatası");
        }
        finally
        {
            AppMode.ExitTestMode();
        }

        _logger.LogWarning("TEST MODU KAPATILDI: {Mesaj}", sonuc.Mesaj);
        return sonuc;
    }
}

public class TestBaslatSonuc
{
    public bool Basarili { get; set; }
    public int SessionId { get; set; }
    public string? Mesaj { get; set; }
    public List<string> BackupTables { get; set; } = [];
}

public class TestRollbackSonuc
{
    public int ToplamKayit { get; set; }
    public int Silinen { get; set; }
    public int Atlanan { get; set; }
    public int Hata { get; set; }
    public bool Basarili { get; set; }
    public string? Mesaj { get; set; }
    public List<string> Hatalar { get; set; } = [];
}

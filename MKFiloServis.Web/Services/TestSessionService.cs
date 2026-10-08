using MKFiloServis.Shared;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Canlı veriyle güvenli test oturumu.
/// SQL backup → test → SQL restore.
/// </summary>
public class TestSessionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<TestSessionService> _logger;
    private readonly IAktifFirmaProvider _firmaProvider;
    private readonly IHttpContextAccessor _http;
    private readonly AuthenticationStateProvider _auth;

    private static readonly string[] _backupTables = new[]
    {
        "MaasOdemeSnapshotlar",
        "Hakedisler",
        "HakedisDetaylari",
        "Faturalar",
        "MuhasebeFisleri",
        "SnapshotTransactions"
    };

    private static readonly Regex SafeIdentifierRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private static readonly SemaphoreSlim MaintenanceGate = new(1, 1);
    public bool IsTestActive { get; private set; }

    public TestSessionService(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<TestSessionService> logger, IAktifFirmaProvider firmaProvider, IHttpContextAccessor http, AuthenticationStateProvider auth)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _firmaProvider = firmaProvider;
        _http = http;
        _auth = auth;
    }

    /// <summary>Test oturumu başlat — SQL backup al</summary>
    public async Task<TestBaslatSonuc> BaslatAsync(string tag)
    {
        var sonuc = new TestBaslatSonuc();
        await MaintenanceGate.WaitAsync();
        try
        {
            if (IsTestActive || AppMode.IsTestMode)
                throw new InvalidOperationException("Test zaten aktif; önce geri alın.");
            var sessionId = await MaintenanceOperationAsync(tag, "TestBackup", async db =>
            {
                var sourceTables = string.Join(", ", _backupTables.Select(t => $"\"{t}\""));
                await ExecuteNonQueryAsync(db, $"LOCK TABLE {sourceTables} IN SHARE MODE");
                foreach (var table in _backupTables)
                {
                    var backup = BuildBackupTableName(table, tag);
                    if (await TableExistsAsync(db, backup))
                        throw new InvalidOperationException("Bu etiket için mevcut yedek var; üzerine yazılmaz.");
                }
                foreach (var table in _backupTables)
                {
                    var backup = BuildBackupTableName(table, tag);
                    await ExecuteNonQueryAsync(db, $"CREATE TABLE \"{backup}\" AS SELECT * FROM \"{table}\"");
                    sonuc.BackupTables.Add(backup);
                }
                return await ReserveSessionAsync(db, tag);
            });
            IsTestActive = true;
            AppMode.EnterTestMode(tag);
            AppMode.CurrentSessionId = sessionId;
            sonuc.Basarili = true;
            sonuc.SessionId = sessionId;
            sonuc.Mesaj = $"Test başladı. {_backupTables.Length} tablo yedeklendi. Session={sessionId}";
        }
        catch (Exception ex)
        {
            sonuc.BackupTables.Clear();
            sonuc.Mesaj = "Test başlatılamadı. Admin/firma seçimini ve mevcut yedek etiketini kontrol edin.";
            _logger.LogError(ex, "Test yedek oluşturma başarısız");
        }
        finally { MaintenanceGate.Release(); }
        return sonuc;
    }

    /// <summary>Test oturumu başlat (SQL backup oluşturmaz).</summary>
    public async Task<int> BeginSessionAsync(string tag)
    {
        await MaintenanceGate.WaitAsync();
        try
        {
            if (IsTestActive || AppMode.IsTestMode)
                throw new InvalidOperationException("Aktif test oturumu varken yeni oturum başlatılamaz.");
            var sessionId = await MaintenanceOperationAsync(tag, "TestSessionBegin", db => ReserveSessionAsync(db, tag));
            AppMode.EnterTestMode(tag);
            AppMode.CurrentSessionId = sessionId;
            return sessionId;
        }
        finally { MaintenanceGate.Release(); }
    }

    private static async Task<int> ReserveSessionAsync(ApplicationDbContext db, string tag)
    {
        var last = await db.TestSessionLogs.IgnoreQueryFilters().Select(t => (int?)t.SessionId).MaxAsync() ?? 0;
        var sessionId = checked(last + 1);
        db.TestSessionLogs.Add(new TestSessionLog
        {
            SessionId = sessionId, TestTag = tag, EntityAdi = "TestSession", EntityId = 0, IslemTipi = "Begin"
        });
        return sessionId;
    }

    private async Task<int> MaintenanceOperationAsync(string tag, string operation, Func<ApplicationDbContext, Task<int>> action)
    {
        if (string.IsNullOrWhiteSpace(tag) || tag.Length > 100)
            throw new ArgumentException("Test etiketi boş olamaz ve 100 karakteri aşamaz.", nameof(tag));
        var firmaId = _firmaProvider.AktifFirmaId;
        long version = 0;
        void FirmaDegisti() => System.Threading.Interlocked.Increment(ref version);
        void SecimiDogrula()
        {
            if (System.Threading.Volatile.Read(ref version) != 0 || firmaId is not > 0 || _firmaProvider.TumFirmalar || _firmaProvider.AktifFirmaId != firmaId)
                throw new InvalidOperationException("Bakım işlemi için tek firma seçilmeli ve işlem sırasında değiştirilmemelidir.");
        }
        _firmaProvider.AktifFirmaDegisti += FirmaDegisti;
        try
        {
            SecimiDogrula();
            await using var strategyDb = await _dbFactory.CreateDbContextAsync();
            if (!strategyDb.Database.IsNpgsql()) throw new NotSupportedException("Test tablo bakımı PostgreSQL gerektirir.");
            return await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var principal = _http.HttpContext is { } http ? http.User : (await _auth.GetAuthenticationStateAsync()).User;
                if (principal.Identity?.IsAuthenticated != true ||
                    !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("KullaniciId")?.Value, out var userId) || userId <= 0 ||
                    !await db.Kullanicilar.AnyAsync(k => k.Id == userId && k.Aktif && !k.IsDeleted && !k.Rol.IsDeleted && k.Rol.RolAdi == SistemRolleri.Admin))
                    throw new UnauthorizedAccessException("Test bakımı için aktif Admin yetkisi gerekir.");
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
                await ExecuteNonQueryAsync(db, "SELECT pg_advisory_xact_lock(74008105)");
                SecimiDogrula();
                var result = await action(db);
                db.AktiviteLoglar.Add(new AktiviteLog
                {
                    FirmaId = firmaId, KullaniciId = userId, KullaniciAdi = principal.Identity!.Name ?? "Admin",
                    IslemTipi = operation, Modul = "Bakim", EntityTipi = "TestSession",
                    Aciklama = "Test bakım işlemi ve operasyon audit'i ortak transaction kapsamındadır.",
                    YeniDeger = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        OperationId = Guid.NewGuid(), Tag = tag, Result = result, Tables = _backupTables, Scope = "WholeDatabaseTables"
                    })
                });
                await db.SaveChangesAsync();
                SecimiDogrula();
                await transaction.CommitAsync();
                return result;
            });
        }
        finally { _firmaProvider.AktifFirmaDegisti -= FirmaDegisti; }
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
        if (!string.Equals(tag, AppMode.CurrentTestTag, StringComparison.Ordinal))
        {
            sonuc.Mesaj = "Geri yükleme etiketi aktif test oturumuyla eşleşmiyor.";
            return sonuc;
        }
        var firmaId = _firmaProvider.AktifFirmaId;
        long version = 0;
        void FirmaDegisti() => System.Threading.Interlocked.Increment(ref version);
        void SecimiDogrula()
        {
            if (System.Threading.Volatile.Read(ref version) != 0 || _firmaProvider.TumFirmalar || firmaId is not > 0 || _firmaProvider.AktifFirmaId != firmaId)
                throw new InvalidOperationException("Test geri yükleme için tek firma seçilmeli ve işlem boyunca değiştirilmemelidir.");
        }
        _firmaProvider.AktifFirmaDegisti += FirmaDegisti;
        try
        {
            SecimiDogrula();
            await using var strategyDb = await _dbFactory.CreateDbContextAsync();
            if (!strategyDb.Database.IsNpgsql()) throw new NotSupportedException("Tablo snapshot geri yükleme PostgreSQL gerektirir.");
            await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                SecimiDogrula();
                var principal = _http.HttpContext is { } http ? http.User : (await _auth.GetAuthenticationStateAsync()).User;
                if (principal.Identity?.IsAuthenticated != true ||
                    !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("KullaniciId")?.Value, out var userId) || userId <= 0 ||
                    !await db.Kullanicilar.AnyAsync(k => k.Id == userId && k.Aktif && !k.IsDeleted && !k.Rol.IsDeleted && k.Rol.RolAdi == SistemRolleri.Admin))
                    throw new UnauthorizedAccessException("Tablo geri yükleme için aktif Admin yetkisi gerekir.");
                await using var transaction = await db.Database.BeginTransactionAsync();
                await ExecuteNonQueryAsync(db, "SELECT pg_advisory_xact_lock(74008105)");
                foreach (var table in _backupTables)
                {
                    if (!await TableExistsAsync(db, BuildBackupTableName(table, tag)))
                        throw new InvalidOperationException("Gerekli test yedek tablosu eksik; geri yükleme başlatılmadı.");
                }
                SecimiDogrula();
                var tables = string.Join(", ", _backupTables.Select(t => $"\"{EnsureSafeIdentifier(t, nameof(t))}\""));
                // CASCADE dışarıdaki yedeklenmemiş tabloları silebilir; bağımlılıkta RESTRICT ile güvenli biçimde dur.
                await ExecuteNonQueryAsync(db, $"TRUNCATE TABLE {tables} RESTRICT");
                foreach (var table in _backupTables)
                {
                    var safeTable = EnsureSafeIdentifier(table, nameof(table));
                    var backupName = BuildBackupTableName(safeTable, tag);
                    await ExecuteNonQueryAsync(db, $"INSERT INTO \"{safeTable}\" SELECT * FROM \"{backupName}\"");
                }
                db.AktiviteLoglar.Add(new AktiviteLog
                {
                    FirmaId = firmaId, KullaniciId = userId, KullaniciAdi = principal.Identity!.Name ?? "Admin",
                    IslemTipi = "TestSnapshotRestore", Modul = "Bakim", EntityTipi = "TestSession",
                    Aciklama = "Test tablo snapshot geri yüklemesi; veri ve operasyon audit'i ortak transaction içinde.",
                    YeniDeger = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        OperationId = Guid.NewGuid(), Tag = tag, Tables = _backupTables,
                        Scope = "WholeDatabaseTables", SessionId = AppMode.CurrentSessionId
                    })
                });
                await db.SaveChangesAsync();
                SecimiDogrula();
                await transaction.CommitAsync();
            });
            sonuc.Basarili = true;
            sonuc.Mesaj = $"Restore başarılı: {_backupTables.Length} tablo geri yüklendi.";
            _logger.LogWarning("TEST RESTORE BAŞARILI: Tag={Tag}", tag);
            IsTestActive = false;
            AppMode.ExitTestMode();
        }
        catch (Exception ex)
        {
            sonuc.Basarili = false;
            sonuc.Mesaj = "Geri yükleme tamamlanamadı. Yedek tablolarını, bağımlılıkları ve Admin/firma seçimini kontrol edin; test oturumu korunmuştur.";
            _logger.LogError(ex, "Test restore hatası; oturum korunuyor");
        }
        finally { _firmaProvider.AktifFirmaDegisti -= FirmaDegisti; }
        return sonuc;
    }

    /// <summary>Backup tablolarını temizle</summary>
    public async Task TemizleAsync(string tag)
    {
        await MaintenanceGate.WaitAsync();
        try
        {
            if (IsTestActive || AppMode.IsTestMode)
                throw new InvalidOperationException("Aktif test oturumunun yedekleri temizlenemez.");
            await MaintenanceOperationAsync(tag, "TestBackupCleanup", async db =>
            {
                foreach (var table in _backupTables)
                    await ExecuteNonQueryAsync(db, $"DROP TABLE IF EXISTS \"{BuildBackupTableName(table, tag)}\"");
                return _backupTables.Length;
            });
        }
        finally { MaintenanceGate.Release(); }
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

        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> TableExistsAsync(ApplicationDbContext db, string tableName)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        if (command.Connection!.State != ConnectionState.Open)
            await command.Connection.OpenAsync();

        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @tableName LIMIT 1";
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
                        case "Hakedis":
                            await db.Hakedisler.Where(h => h.Id == log.EntityId)
                                .UpdateTrackedAsync(db, record =>
                                {
                                    record.IsDeleted = true;
                                    record.DeletedAt = DateTime.UtcNow;
                                });
                            sonuc.Silinen++;
                            break;
                        case "SnapshotTransaction":
                            await db.SnapshotTransactions.Where(t => t.Id == log.EntityId)
                                .UpdateTrackedAsync(db, record =>
                                {
                                    record.IsDeleted = true;
                                    record.DeletedAt = DateTime.UtcNow;
                                });
                            sonuc.Silinen++;
                            break;
                        case "IncidentLog":
                            await db.IncidentLogs.Where(i => i.Id == log.EntityId)
                                .UpdateTrackedAsync(db, record =>
                                {
                                    record.IsDeleted = true;
                                    record.DeletedAt = DateTime.UtcNow;
                                });
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
                .UpdateTrackedAsync(db, record =>
                {
                    record.IsDeleted = true;
                    record.DeletedAt = DateTime.UtcNow;
                });

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



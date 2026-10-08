using MKFiloServis.Shared;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Claims;

namespace MKFiloServis.Web.Data;

/// <summary>Admin yetkisiyle, tek seçili firma için transaction ve operasyon audit'i içeren demo bakımı.</summary>
public class DemoDataService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<DemoDataService> _logger;
    private readonly ILogger<TestDataSeeder> _seederLogger;
    private readonly IAktifFirmaProvider _firmaProvider;
    private readonly IHttpContextAccessor _http;
    private readonly AuthenticationStateProvider _auth;

    public DemoDataService(IDbContextFactory<ApplicationDbContext> dbFactory,
        ILogger<DemoDataService> logger, ILogger<TestDataSeeder> seederLogger,
        IAktifFirmaProvider firmaProvider, IHttpContextAccessor http, AuthenticationStateProvider auth)
    {
        _dbFactory = dbFactory; _logger = logger; _seederLogger = seederLogger;
        _firmaProvider = firmaProvider; _http = http; _auth = auth;
    }

    // Eski tüm-veritabanı silme API'si güvenlik, lisans ve audit geçmişini silemez.
    public Task<DemoDataResult> TruncateAllAsync() => Task.FromResult(new DemoDataResult
    {
        Mesaj = "Tüm veritabanını silme desteklenmiyor. Seçili firmanın işaretli demo kayıtlarını yenileyin."
    });

    public Task<DemoDataResult> SeedDemoDataAsync() => SeedAsync(false);
    public Task<DemoDataResult> ResetAndSeedAsync() => SeedAsync(true);

    private Task<DemoDataResult> SeedAsync(bool replace) => RunMaintenanceAsync(
        replace ? "DemoReplace" : "DemoSeed", _firmaProvider.AktifFirmaId ?? 0, async db =>
        {
            var seeder = new TestDataSeeder(db, _seederLogger, _firmaProvider.AktifFirmaId);
            var seeded = await seeder.SeedAllAsync(replace);
            if (!seeded.Basarili)
                throw new InvalidOperationException("Demo üretimi tamamlanamadı; transaction geri alınacak.");
            return new DemoDataResult
            {
                SeedCount = seeded.ToplamKayit,
                SeedDetails = seeded,
                Mesaj = $"Seçili firmada {seeded.ToplamKayit} demo kayıt oluşturuldu.",
                Mesajlar = seeded.Mesajlar
            };
        });

    public Task<DemoDataResult> RemoveDemoDataAsync() => RunMaintenanceAsync(
        "DemoCleanup", _firmaProvider.AktifFirmaId ?? 0, async db =>
        {
            await new TestDataSeeder(db, _seederLogger, _firmaProvider.AktifFirmaId).TemizleAsync();
            return new DemoDataResult { Mesaj = "Seçili firmanın [TEST] işaretli demo kayıtları temizlendi." };
        });

    public Task<DemoDataResult> ClearFirmaDataAsync(int firmaId) => RunMaintenanceAsync(
        "FirmaDataCleanup", firmaId, async db =>
        {
            var deleted = 0;
            foreach (var table in FirmaCleanupTables)
                deleted += await DeleteByFirmaAsync(db, table, firmaId);
            return new DemoDataResult
            {
                Mesaj = $"Tanımlı firma tablolarından {deleted} kayıt temizlendi. Ortak muhasebe tabloları bu kapsama dahil değildir.",
                TruncatedTables = FirmaCleanupTables.ToList(),
                Mesajlar = [$"Etkilenen satır: {deleted}"]
            };
        });

    private async Task<DemoDataResult> RunMaintenanceAsync(string operation, int firmaId,
        Func<ApplicationDbContext, Task<DemoDataResult>> action)
    {
        long version = 0;
        void FirmaDegisti() => Interlocked.Increment(ref version);
        void SecimiDogrula()
        {
            if (firmaId <= 0 || _firmaProvider.TumFirmalar || _firmaProvider.AktifFirmaId != firmaId || Volatile.Read(ref version) != 0)
                throw new InvalidOperationException("Bakım için tek firma seçilmeli ve işlem boyunca değiştirilmemelidir.");
        }
        _firmaProvider.AktifFirmaDegisti += FirmaDegisti;
        try
        {
            SecimiDogrula();
            await using var strategyDb = await _dbFactory.CreateDbContextAsync();
            if (!strategyDb.Database.IsNpgsql()) throw new NotSupportedException("Demo bakımı PostgreSQL gerektirir.");
            return await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var principal = _http.HttpContext is { } http ? http.User : (await _auth.GetAuthenticationStateAsync()).User;
                if (principal.Identity?.IsAuthenticated != true ||
                    !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("KullaniciId")?.Value, out var userId) || userId <= 0 ||
                    !await db.Kullanicilar.AnyAsync(k => k.Id == userId && k.Aktif && !k.IsDeleted && !k.Rol.IsDeleted && k.Rol.RolAdi == SistemRolleri.Admin))
                    throw new UnauthorizedAccessException("Demo bakımı için aktif Admin yetkisi gerekir.");
                if (!await db.Firmalar.AnyAsync(f => f.Id == firmaId && f.Aktif && !f.IsDeleted))
                    throw new InvalidOperationException("Aktif firma bulunamadı.");
                await using var transaction = await db.Database.BeginTransactionAsync();
                await using (var command = db.Database.GetDbConnection().CreateCommand())
                {
                    command.Transaction = transaction.GetDbTransaction();
                    command.CommandText = "SELECT pg_advisory_xact_lock(74008105)";
                    await command.ExecuteNonQueryAsync();
                }
                SecimiDogrula();
                var result = await action(db);
                SecimiDogrula();
                db.AktiviteLoglar.Add(new AktiviteLog
                {
                    FirmaId = firmaId, KullaniciId = userId, KullaniciAdi = principal.Identity!.Name ?? "Admin",
                    IslemTipi = operation, Modul = "Bakim", EntityTipi = "Firma", EntityId = firmaId,
                    Aciklama = "Demo bakım verisi ve operasyon audit'i ortak transaction içinde; FK kontrolleri açıktır.",
                    YeniDeger = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        OperationId = Guid.NewGuid(), FirmaId = firmaId, Operation = operation,
                        result.SeedCount, result.TruncatedTables, result.Mesajlar,
                        Scope = operation == "FirmaDataCleanup" ? "ListedFirmaTables" : "SelectedFirmaTestMarkers"
                    })
                });
                await db.SaveChangesAsync();
                SecimiDogrula();
                await transaction.CommitAsync();
                result.Basarili = true;
                return result;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo bakım işlemi başarısız. Operation={Operation} FirmaId={FirmaId}", operation, firmaId);
            return new DemoDataResult { Mesaj = "İşlem tamamlanamadı. Admin yetkisini, aktif firmayı ve kayıt bağımlılıklarını kontrol edin; ayrıntılar sunucu günlüğündedir." };
        }
        finally { _firmaProvider.AktifFirmaDegisti -= FirmaDegisti; }
    }

    // FirmaId taşımayan ortak muhasebe tabloları burada silinmez.
    private static readonly string[] FirmaCleanupTables =
    [
        "ServisCalismalari", "Faturalar", "PersonelMaaslari", "PersonelPuantajlar",
        "AracMasraflari", "Araclar", "Soforler", "Cariler", "Guzergahlar", "BudgetOdemeler", "BankaHesaplari"
    ];

    private static async Task<int> DeleteByFirmaAsync(ApplicationDbContext db, string tableName, int firmaId)
    {
        if (!FirmaCleanupTables.Contains(tableName) || db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Firma temizliği yalnız tanımlı tablolar ve açık transaction ile yapılabilir.");
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = $"DELETE FROM \"{tableName}\" WHERE \"FirmaId\" = @firmaId";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "firmaId"; parameter.Value = firmaId;
        command.Parameters.Add(parameter);
        return await command.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Demo veri operasyonu sonucu
/// </summary>
public class DemoDataResult
{
    public bool Basarili { get; set; }
    public string Mesaj { get; set; } = "";
    public List<string> Mesajlar { get; set; } = new();
    public List<string> TruncatedTables { get; set; } = new();
    public TestDataResult? SeedDetails { get; set; }
    public int SeedCount { get; set; }
}

using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// RebuildAll motoru — belirtilen dönem için tüm finans zincirini baştan hesaplar.
/// </summary>
public class RebuildService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IPuantajFinansService _finansService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RebuildService> _logger;

    public RebuildService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IPuantajFinansService finansService,
        IMemoryCache cache,
        ILogger<RebuildService> logger)
    {
        _dbFactory = dbFactory;
        _finansService = finansService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RebuildSonuc> RebuildAsync(int firmaId, int yil, int ay)
    {
        var sonuc = new RebuildSonuc();
        SystemLock? lockEntity = null;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // 🔴 Auto-unlock: 10 dk'dan eski kilitli lock'ları otomatik aç
            var staleLock = await db.SystemLocks
                .FirstOrDefaultAsync(x => x.Key == "REBUILD" && x.IsLocked && !x.IsDeleted);
            if (staleLock != null && staleLock.LockedAt.HasValue
                && staleLock.LockedAt.Value < DateTime.UtcNow.AddMinutes(-10))
            {
                _logger.LogWarning("Stale lock tespit edildi, otomatik açılıyor: {Reason}", staleLock.Reason);
                staleLock.IsLocked = false;
                staleLock.UnlockedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            // 🔴 Rate limit: Son 5 dk içinde rebuild yapıldı mı?
            var cacheKey = $"rebuild_last_{firmaId}_{yil}_{ay}";
            if (_cache.TryGetValue(cacheKey, out _))
                throw new InvalidOperationException("Bu dönem için son 5 dakika içinde rebuild yapıldı. Lütfen bekleyin.");

            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(5));

            // 🔴 SystemLock: Rebuild çakışmasını engelle
            var locked = await db.SystemLocks.AnyAsync(x => x.Key == "REBUILD" && x.IsLocked && !x.IsDeleted);
            if (locked)
                throw new InvalidOperationException("Sistem şu anda rebuild altında. Lütfen bekleyin.");

            lockEntity = new SystemLock { Key = "REBUILD", IsLocked = true, Reason = $"Rebuild {yil}/{ay} Firma:{firmaId}", LockedAt = DateTime.UtcNow };
            db.SystemLocks.Add(lockEntity);
            await db.SaveChangesAsync();

            var strategy = db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();

                _logger.LogInformation("Rebuild başladı: Firma={FirmaId} Yil={Yil} Ay={Ay}", firmaId, yil, ay);

                // Adım 0: Snapshot BACKUP (SnapshotHistory)
                await BackupSnapshotAsync(db, firmaId, yil, ay);

                // Adım 1: SnapshotTransaction'ları soft-delete
                var txCount = await db.SnapshotTransactions
                    .Where(t => t.FirmaId == firmaId && t.Yil == yil && t.Ay == ay && !t.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.IsDeleted, true)
                        .SetProperty(t => t.DeletedAt, DateTime.UtcNow));
                sonuc.SnapshotTransactionSilinen = txCount;

                // Adım 2: HakedisPuantaj fatura referanslarını temizle
                var hakedisler = await db.HakedisPuantajlar
                    .Where(h => h.FirmaId == firmaId && h.Yil == yil && h.Ay == ay && !h.IsDeleted)
                    .ToListAsync();
                sonuc.ToplamHakedis = hakedisler.Count;

                foreach (var h in hakedisler)
                {
                    h.GelirFaturaId = null;
                    h.GiderFaturaId = null;
                }
                await db.SaveChangesAsync();

                // Adım 3: Snapshot HakedisGelir/HakedisGider sıfırla
                await db.MaasOdemeSnapshotlar
                    .Where(s => s.FirmaId == firmaId && s.Yil == yil && s.Ay == ay && !s.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.HakedisGelir, 0m)
                        .SetProperty(x => x.HakedisGider, 0m)
                        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

                // Adım 4: Her HakedisPuantaj için IsleAsync çağır
                foreach (var h in hakedisler)
                {
                    try
                    {
                        var finansSonuc = await _finansService.IsleAsync(h);
                        if (finansSonuc.Basarili)
                            sonuc.BasariliIslem++;
                        else
                        {
                            sonuc.AtlananIslem++;
                            sonuc.Hatalar.Add($"#{h.Id}: {finansSonuc.Mesaj}");
                        }
                    }
                    catch (Exception ex)
                    {
                        sonuc.Hata++;
                        sonuc.Hatalar.Add($"#{h.Id}: {ex.Message}");
                        _logger.LogError(ex, "Rebuild hatası: HakedisPuantaj #{Id}", h.Id);
                    }
                }

                await tx.CommitAsync();
            });
        }
        catch
        {
            throw;
        }
        finally
        {
            // 🔴 Safe exit: Crash olsa bile lock HER ZAMAN açılır
            if (lockEntity != null)
            {
                try
                {
                    await using var db3 = await _dbFactory.CreateDbContextAsync();
                    var entity = await db3.SystemLocks.FindAsync(lockEntity.Id);
                    if (entity != null)
                    {
                        entity.IsLocked = false;
                        entity.UnlockedAt = DateTime.UtcNow;
                        await db3.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rebuild lock serbest bırakılamadı (auto-unlock 10 dk sonra devreye girer)");
                }
            }
        }

        _logger.LogInformation("Rebuild tamamlandı: Başarılı={Basarili} Atlanan={Atlanan} Hata={Hata}",
            sonuc.BasariliIslem, sonuc.AtlananIslem, sonuc.Hata);

        return sonuc;
    }

    private static async Task BackupSnapshotAsync(ApplicationDbContext db, int firmaId, int yil, int ay)
    {
        var snapshots = await db.MaasOdemeSnapshotlar
            .Where(s => s.FirmaId == firmaId && s.Yil == yil && s.Ay == ay && !s.IsDeleted)
            .ToListAsync();

        foreach (var s in snapshots)
        {
            db.SnapshotHistories.Add(new SnapshotHistory
            {
                FirmaId = firmaId,
                SnapshotId = s.Id,
                Yil = yil,
                Ay = ay,
                HakedisGelirEski = s.HakedisGelir,
                HakedisGiderEski = s.HakedisGider,
                IslemTipi = "Rebuild",
                KayitTarihi = DateTime.UtcNow
            });
        }
    }

    /// <summary>Hard clean: tüm hakedis + snapshot transaction'ları sil, sonra rebuild</summary>
    public async Task<RebuildSonuc> HardRebuildAsync(int firmaId, int yil, int ay)
    {
        var sonuc = new RebuildSonuc();
        await using var db = await _dbFactory.CreateDbContextAsync();

        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                // Auto-fix: Negatif ve aşırı değerleri clamp'la
                await db.Database.ExecuteSqlRawAsync(@"
                    UPDATE ""HakedisPuantajDetaylar""
                    SET ""SeferSayisi"" = LEAST(GREATEST(""SeferSayisi"", 0), 10)");
                await db.Database.ExecuteSqlRawAsync(@"
                    UPDATE ""HakedisPuantajlar""
                    SET ""ToplamSefer"" = LEAST(GREATEST(""ToplamSefer"", 0), 310)");

                // SnapshotTransactions temizle
                await db.SnapshotTransactions
                    .Where(t => t.FirmaId == firmaId && t.Yil == yil && t.Ay == ay)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));

                // HakedisPuantajDetaylar temizle
                await db.HakedisPuantajDetaylar
                    .Where(d => d.HakedisPuantaj != null
                        && d.HakedisPuantaj.FirmaId == firmaId
                        && d.HakedisPuantaj.Yil == yil && d.HakedisPuantaj.Ay == ay)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));

                // HakedisPuantajlar temizle
                await db.HakedisPuantajlar
                    .Where(h => h.FirmaId == firmaId && h.Yil == yil && h.Ay == ay && !h.IsDeleted)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));

                // Snapshot sıfırla
                await db.MaasOdemeSnapshotlar
                    .Where(s => s.FirmaId == firmaId && s.Yil == yil && s.Ay == ay && !s.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.HakedisGelir, 0m)
                        .SetProperty(x => x.HakedisGider, 0m));

                // Grid verisinden yeniden üret (Grid kaydettikten sonra SyncFromGridAsync zaten çağrılır)
                sonuc.ToplamHakedis = 0;
                sonuc.BasariliIslem = 0;
                sonuc.Hatalar.Add("Hard clean tamamlandı. Grid'den tekrar Kaydet yaparak HakedisPuantaj'ları yeniden oluşturun.");

                await tx.CommitAsync();
            }
            catch { await tx.RollbackAsync(); throw; }
        });

        return sonuc;
    }

    public async Task<RebuildOnizleme> PreviewAsync(int firmaId, int yil, int ay)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var hakedisler = await db.HakedisPuantajlar
            .Where(h => h.FirmaId == firmaId && h.Yil == yil && h.Ay == ay && !h.IsDeleted)
            .ToListAsync();

        var snapshotTxCount = await db.SnapshotTransactions
            .CountAsync(t => t.FirmaId == firmaId && t.Yil == yil && t.Ay == ay && !t.IsDeleted);

        var snapshot = await db.MaasOdemeSnapshotlar
            .Where(s => s.FirmaId == firmaId && s.Yil == yil && s.Ay == ay && !s.IsDeleted)
            .ToListAsync();

        return new RebuildOnizleme
        {
            HakedisSayisi = hakedisler.Count,
            IslenmisHakedisSayisi = hakedisler.Count(h => h.GelirFaturaId != null || h.GiderFaturaId != null),
            IslenmemisHakedisSayisi = hakedisler.Count(h => h.GelirFaturaId == null && h.GiderFaturaId == null),
            SnapshotTransactionSayisi = snapshotTxCount,
            SnapshotHakedisGelir = snapshot.Sum(s => s.HakedisGelir),
            SnapshotHakedisGider = snapshot.Sum(s => s.HakedisGider),
            HakedisGelirToplam = hakedisler.Sum(h => h.TahsilEdilecekTutar),
            HakedisGiderToplam = hakedisler.Sum(h => h.OdenecekTutar),
        };
    }
}

public class RebuildSonuc
{
    public int ToplamHakedis { get; set; }
    public int BasariliIslem { get; set; }
    public int AtlananIslem { get; set; }
    public int Hata { get; set; }
    public int SnapshotTransactionSilinen { get; set; }
    public List<string> Hatalar { get; set; } = [];
    public bool TamBasarili => Hata == 0;
}

public class RebuildOnizleme
{
    public int HakedisSayisi { get; set; }
    public int IslenmisHakedisSayisi { get; set; }
    public int IslenmemisHakedisSayisi { get; set; }
    public int SnapshotTransactionSayisi { get; set; }
    public decimal SnapshotHakedisGelir { get; set; }
    public decimal SnapshotHakedisGider { get; set; }
    public decimal HakedisGelirToplam { get; set; }
    public decimal HakedisGiderToplam { get; set; }
    public bool Tutarli => SnapshotHakedisGelir == HakedisGelirToplam && SnapshotHakedisGider == HakedisGiderToplam;
}

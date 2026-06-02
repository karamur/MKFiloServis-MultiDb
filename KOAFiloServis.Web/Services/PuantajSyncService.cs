using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Models;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Services;

public sealed class PuantajSyncService : IPuantajSyncService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IAktifFirmaProvider _firmaProvider;
    private readonly ILogger<PuantajSyncService> _logger;

    public PuantajSyncService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ICurrentUserAccessor currentUser,
        IAktifFirmaProvider firmaProvider,
        ILogger<PuantajSyncService> logger)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
        _firmaProvider = firmaProvider;
        _logger = logger;
    }

    private string TenantAdi => _firmaProvider.Mevcut.DatabaseName ?? "Shared";
    private string CurrentUser => _currentUser.GetCurrentUserName() ?? "Sistem";

    // ── Tek kayıt sync ───────────────────────────────────────────────────

    public async Task SyncFromPuantajAsync(PuantajKayit puantaj, PuantajSyncMode mode, Guid? batchId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await SyncFromPuantajWithContextAsync(db, puantaj, mode, batchId);
    }

    /// <summary>
    /// Var olan DbContext ile sync. Transaction bütünlüğü için kullanılır.
    /// </summary>
    internal async Task SyncFromPuantajWithContextAsync(ApplicationDbContext db, PuantajKayit puantaj, PuantajSyncMode mode, Guid? batchId = null)
    {
        if (puantaj.GuzergahId == null || puantaj.AracId.GetValueOrDefault() <= 0)
            return;

        var batch = batchId ?? Guid.NewGuid();

        var guzergahId = puantaj.GuzergahId.Value;
        var aracId = puantaj.AracId!.Value;

        var carpani = await db.Guzergahlar
            .Where(g => g.Id == guzergahId)
            .Select(g => g.PuantajCarpani)
            .FirstOrDefaultAsync();
        if (carpani == 0) carpani = 1.0m;

        for (int gun = 1; gun <= 31; gun++)
        {
            var gunDeger = puantaj.GetGunDeger(gun);
            var tarih = new DateTime(puantaj.Yil, puantaj.Ay, gun);

            if (gunDeger <= 0)
            {
                // Soft-delete varsa
                var mevcut = await db.OperasyonKayitlari
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o =>
                        o.KaynakPuantajId == puantaj.Id && o.Tarih == tarih && !o.IsDeleted);
                if (mevcut != null)
                {
                    mevcut.IsDeleted = true;
                    mevcut.DeletedAt = DateTime.UtcNow;
                    mevcut.DeletedBy = CurrentUser;
                    _logger.LogInformation("PuantajSync: SoftDelete | Batch={Batch} | Tenant={Tenant} | PkId={PkId} | Gun={Gun}",
                        batch, TenantAdi, puantaj.Id, gun);
                }
                continue;
            }

            // gunDeger > 0: kayıt ara (IsDeleted dahil)
            var existing = await db.OperasyonKayitlari
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o =>
                    o.KaynakPuantajId == puantaj.Id && o.Tarih == tarih);

            if (existing == null)
            {
                // Yeni oluştur
                db.OperasyonKayitlari.Add(new OperasyonKaydi
                {
                    KaynakPuantajId = puantaj.Id,
                    Tarih = tarih,
                    GuzergahId = guzergahId,
                    AracId = aracId,
                    SoforId = puantaj.SoforId,
                    Slot = puantaj.Slot,
                    SlotAdi = puantaj.SlotAdi,
                    Yon = puantaj.Yon,
                    KurumId = puantaj.KurumId,
                    IsverenFirmaId = puantaj.IsverenFirmaId,
                    SeferSayisi = gunDeger,
                    PuantajCarpani = carpani,
                    OperasyonDurumu = OperasyonDurumu.Gitti,
                    KaynakTipi = puantaj.KaynakTipi,
                    FinansYonu = puantaj.FinansYonu,
                    SoforOdemeTipi = puantaj.SoforOdemeTipi,
                    OdemeYapilacakCariId = puantaj.OdemeYapilacakCariId,
                    FaturaKesiciCariId = puantaj.FaturaKesiciCariId,
                    BelgeNo = puantaj.BelgeNo,
                    TransferDurum = puantaj.TransferDurum,
                    Kaynak = PuantajKaynak.Puantaj,
                    Notlar = puantaj.Notlar,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = CurrentUser
                });
                _logger.LogInformation("PuantajSync: Olusturuldu | Batch={Batch} | Tenant={Tenant} | PkId={PkId} | Gun={Gun} | Tarih={Tarih}",
                    batch, TenantAdi, puantaj.Id, gun, tarih.ToString("yyyy-MM-dd"));
            }
            else if (existing.IsDeleted)
            {
                // Restore
                existing.IsDeleted = false;
                existing.DeletedAt = null;
                existing.DeletedBy = null;
                AlanlariGuncelle(existing, puantaj, gunDeger, carpani);
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = CurrentUser;
                _logger.LogInformation("PuantajSync: RestoreEdildi | Batch={Batch} | Tenant={Tenant} | PkId={PkId} | Gun={Gun}",
                    batch, TenantAdi, puantaj.Id, gun);
            }
            else
            {
                // Aktif kayıt: kilit kontrolü
                if (existing.KullaniciKilitliMi)
                {
                    _logger.LogWarning("PuantajSync: Atladi (KullaniciKilitli) | Batch={Batch} | Tenant={Tenant} | PkId={PkId} | Gun={Gun}",
                        batch, TenantAdi, puantaj.Id, gun);
                    continue;
                }

                if (mode == PuantajSyncMode.ForceOverwrite || mode == PuantajSyncMode.CreateUpdate)
                {
                    AlanlariGuncelle(existing, puantaj, gunDeger, carpani);
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = CurrentUser;
                    _logger.LogInformation("PuantajSync: Guncellendi | Batch={Batch} | Tenant={Tenant} | PkId={PkId} | Gun={Gun}",
                        batch, TenantAdi, puantaj.Id, gun);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    // ── Toplu sync ───────────────────────────────────────────────────────

    public async Task<SyncOzetRaporu> SyncFromPuantajTopluAsync(
        int yil, int ay, List<PuantajKayit>? onlyThese = null,
        PuantajSyncMode mode = PuantajSyncMode.CreateUpdate)
    {
        var rapor = new SyncOzetRaporu
        {
            BatchId = Guid.NewGuid(),
            IslemTipi = mode == PuantajSyncMode.ForceOverwrite ? "ForceSync" : "NormalSync",
            Baslangic = DateTime.UtcNow
        };

        await using var db = await _dbFactory.CreateDbContextAsync();

        var kayitlar = onlyThese ?? await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && p.AracId != null)
            .ToListAsync();

        foreach (var pk in kayitlar)
        {
            try
            {
                await SyncFromPuantajAsync(pk, mode, rapor.BatchId);
            }
            catch (Exception ex)
            {
                rapor.Hata++;
                rapor.HataMesajlari.Add($"PkId={pk.Id}: {ex.Message}");
                _logger.LogError(ex, "PuantajSync: Hata | Batch={Batch} | PkId={PkId}", rapor.BatchId, pk.Id);
            }
        }

        rapor.Bitis = DateTime.UtcNow;
        // Count summary from sync logs would require a different approach;
        // for now, report completion with batch ID for log correlation
        _logger.LogInformation("PuantajSync: TopluSyncTamamlandi | Batch={Batch} | Tenant={Tenant} | {KayitSayisi} kayit islendi",
            rapor.BatchId, TenantAdi, kayitlar.Count);

        return rapor;
    }

    // ── Force Sync (dönem) ───────────────────────────────────────────────

    public async Task<SyncOzetRaporu> ForceSyncForDonemAsync(int kurumId, int yil, int ay)
    {
        var rapor = new SyncOzetRaporu
        {
            BatchId = Guid.NewGuid(),
            IslemTipi = "ForceSync",
            Baslangic = DateTime.UtcNow
        };

        await using var db = await _dbFactory.CreateDbContextAsync();

        _logger.LogInformation("PuantajSync: ForceSyncBasladi | Batch={Batch} | Tenant={Tenant} | Donem={Yil}-{Ay} | KurumId={KurumId}",
            rapor.BatchId, TenantAdi, yil, ay, kurumId);

        // SADECE Kaynak=Puantaj olan OperasyonKayitlari soft-delete
        // KullaniciKilitliMi=true olanları KORU
        var silinecekler = await db.OperasyonKayitlari
            .Where(o => !o.IsDeleted
                        && o.Kaynak == PuantajKaynak.Puantaj
                        && o.KaynakPuantajId != null
                        && !o.KullaniciKilitliMi
                        && o.Tarih.Year == yil && o.Tarih.Month == ay)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var o in silinecekler)
        {
            o.IsDeleted = true;
            o.DeletedAt = now;
            o.DeletedBy = CurrentUser;
        }
        rapor.SoftDeleteYapilan = silinecekler.Count;

        // Kilitli olanları say
        rapor.AtlananKilitli = await db.OperasyonKayitlari
            .CountAsync(o => !o.IsDeleted
                             && o.Kaynak == PuantajKaynak.Puantaj
                             && o.KullaniciKilitliMi
                             && o.Tarih.Year == yil && o.Tarih.Month == ay);

        await db.SaveChangesAsync();

        // Yeniden üret: ForceOverwrite ile
        var puantajKayitlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && p.AracId != null)
            .ToListAsync();

        var kurumGuzergahIds = await db.Guzergahlar
            .Where(g => !g.IsDeleted && g.KurumId == kurumId)
            .Select(g => g.Id)
            .ToListAsync();

        var filtered = puantajKayitlar
            .Where(p => p.GuzergahId != null && kurumGuzergahIds.Contains(p.GuzergahId.Value))
            .ToList();

        foreach (var pk in filtered)
        {
            try
            {
                await SyncFromPuantajAsync(pk, PuantajSyncMode.ForceOverwrite, rapor.BatchId);
            }
            catch (Exception ex)
            {
                rapor.Hata++;
                rapor.HataMesajlari.Add($"PkId={pk.Id}: {ex.Message}");
            }
        }

        rapor.Bitis = DateTime.UtcNow;
        _logger.LogInformation("PuantajSync: ForceSyncTamamlandi | Batch={Batch} | Tenant={Tenant} | {Ozet}",
            rapor.BatchId, TenantAdi, rapor.Ozet);

        return rapor;
    }

    // ── Delete linked ────────────────────────────────────────────────────

    public async Task DeleteLinkedOpsAsync(int puantajKayitId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var ops = await db.OperasyonKayitlari
            .Where(o => !o.IsDeleted && o.KaynakPuantajId == puantajKayitId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var o in ops)
        {
            o.IsDeleted = true;
            o.DeletedAt = now;
            o.DeletedBy = CurrentUser;
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("PuantajSync: DeleteLinked | Tenant={Tenant} | PkId={PkId} | {Count} kayit soft-delete",
            TenantAdi, puantajKayitId, ops.Count);
    }

    // ── Backfill ─────────────────────────────────────────────────────────

    public async Task<SyncOzetRaporu> BackfillAsync(int yil, int ay, int? kurumId = null)
    {
        var rapor = new SyncOzetRaporu
        {
            BatchId = Guid.NewGuid(),
            IslemTipi = "Backfill",
            Baslangic = DateTime.UtcNow
        };

        await using var db = await _dbFactory.CreateDbContextAsync();

        var guzergahIds = kurumId.HasValue && kurumId > 0
            ? await db.Guzergahlar.Where(g => !g.IsDeleted && g.KurumId == kurumId.Value)
                .Select(g => g.Id).ToListAsync()
            : await db.Guzergahlar.Where(g => !g.IsDeleted)
                .Select(g => g.Id).ToListAsync();

        var puantajKayitlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && p.AracId != null
                        && guzergahIds.Contains(p.GuzergahId!.Value))
            .ToListAsync();

        _logger.LogInformation("PuantajSync: BackfillBasladi | Batch={Batch} | Tenant={Tenant} | Donem={Yil}-{Ay} | {Count} PuantajKayit",
            rapor.BatchId, TenantAdi, yil, ay, puantajKayitlar.Count);

        foreach (var pk in puantajKayitlar)
        {
            try
            {
                await SyncFromPuantajAsync(pk, PuantajSyncMode.CreateUpdate, rapor.BatchId);
            }
            catch (Exception ex)
            {
                rapor.Hata++;
                rapor.HataMesajlari.Add($"PkId={pk.Id}: {ex.Message}");
            }
        }

        rapor.Bitis = DateTime.UtcNow;
        _logger.LogInformation("PuantajSync: BackfillTamamlandi | Batch={Batch} | Tenant={Tenant} | {Count} PuantajKayit islendi",
            rapor.BatchId, TenantAdi, puantajKayitlar.Count);

        return rapor;
    }

    // ── Helper ───────────────────────────────────────────────────────────

    private static void AlanlariGuncelle(OperasyonKaydi o, PuantajKayit pk, int seferSayisi, decimal carpani)
    {
        o.GuzergahId = pk.GuzergahId!.Value;
        o.AracId = pk.AracId!.Value;
        o.SoforId = pk.SoforId;
        o.Slot = pk.Slot;
        o.SlotAdi = pk.SlotAdi;
        o.Yon = pk.Yon;
        o.KurumId = pk.KurumId;
        o.IsverenFirmaId = pk.IsverenFirmaId;
        o.SeferSayisi = seferSayisi;
        o.PuantajCarpani = carpani;
        o.OperasyonDurumu = OperasyonDurumu.Gitti;
        o.KaynakTipi = pk.KaynakTipi;
        o.FinansYonu = pk.FinansYonu;
        o.SoforOdemeTipi = pk.SoforOdemeTipi;
        o.OdemeYapilacakCariId = pk.OdemeYapilacakCariId;
        o.FaturaKesiciCariId = pk.FaturaKesiciCariId;
        o.BelgeNo = pk.BelgeNo;
        o.TransferDurum = pk.TransferDurum;
        o.Kaynak = PuantajKaynak.Puantaj;
        o.Notlar = pk.Notlar;
    }
}

using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class GuzergahSeferService : IGuzergahSeferService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;
    private readonly ILogger<GuzergahSeferService> _logger;

    public GuzergahSeferService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IAktifFirmaProvider aktifFirmaProvider,
        ILogger<GuzergahSeferService> logger)
    {
        _contextFactory = contextFactory;
        _aktifFirmaProvider = aktifFirmaProvider;
        _logger = logger;
    }

    public async Task<List<GuzergahSefer>> GetByGuzergahIdAsync(int guzergahId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Parent Guzergah doğrulaması
        await VerifyGuzergahAccessAsync(context, guzergahId);

        return await context.GuzergahSeferleri
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.GuzergahId == guzergahId && !s.IsDeleted)
            .OrderBy(s => s.Sira)
            .ToListAsync();
    }

    public async Task<Dictionary<int, List<GuzergahSefer>>> GetAllGroupedAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var liste = await context.GuzergahSeferleri
            .AsNoTracking()
            .OrderBy(s => s.GuzergahId).ThenBy(s => s.Sira)
            .ToListAsync();
        return liste.GroupBy(s => s.GuzergahId).ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task ReplaceAllAsync(int guzergahId, List<GuzergahSefer> seferler)
    {
        seferler ??= [];

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Parent Guzergah doğrulaması
        var guzergah = await VerifyGuzergahAccessAsync(context, guzergahId);
        var parentFirmaId = guzergah.FirmaId;

        await using var tx = await context.Database.BeginTransactionAsync();

        try
        {
            var mevcut = await context.GuzergahSeferleri
                .IgnoreQueryFilters()
                .Where(s => s.GuzergahId == guzergahId)
                .ToListAsync();

            // Gelen Id'leri topla
            var gelenIdler = seferler
                .Where(s => s.Id > 0)
                .Select(s => s.Id)
                .ToHashSet();

            // Silinen seferleri soft delete yap
            foreach (var m in mevcut.Where(x => !gelenIdler.Contains(x.Id)))
            {
                m.IsDeleted = true;
                m.UpdatedAt = DateTime.UtcNow;
            }

            int sira = 1;
            foreach (var s in seferler)
            {
                if (s.Id > 0)
                {
                    var mevcutKayit = mevcut.FirstOrDefault(x => x.Id == s.Id);
                    if (mevcutKayit == null) continue;

                    mevcutKayit.FirmaId = parentFirmaId;
                    mevcutKayit.Sira = sira++;
                    mevcutKayit.Slot = s.Slot;
                    mevcutKayit.SeferTipi = s.SeferTipi;
                    mevcutKayit.KapasiteAdi = s.KapasiteAdi;
                    mevcutKayit.AracId = s.AracId;
                    mevcutKayit.SoforAd = s.SoforAd;
                    mevcutKayit.SoforTelefon = s.SoforTelefon;
                    mevcutKayit.FirmaAdiSerbest = s.FirmaAdiSerbest;
                    mevcutKayit.IsDeleted = false;
                    mevcutKayit.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    context.GuzergahSeferleri.Add(new GuzergahSefer
                    {
                        GuzergahId = guzergahId,
                        FirmaId = parentFirmaId,
                        Sira = sira++,
                        Slot = s.Slot,
                        SeferTipi = s.SeferTipi,
                        KapasiteAdi = s.KapasiteAdi,
                        AracId = s.AracId,
                        SoforAd = s.SoforAd,
                        SoforTelefon = s.SoforTelefon,
                        FirmaAdiSerbest = s.FirmaAdiSerbest,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation(
                "Guzergah seferleri kaydedildi. GuzergahId={GuzergahId}, SeferSayisi={SeferSayisi}, FirmaId={FirmaId}",
                guzergahId, seferler.Count, parentFirmaId);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<Guzergah> VerifyGuzergahAccessAsync(ApplicationDbContext context, int guzergahId)
    {
        var guzergah = await context.Guzergahlar
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guzergahId && !g.IsDeleted);

        if (guzergah == null)
            throw new InvalidOperationException($"Güzergah bulunamadı. Id={guzergahId}");

        var aktifFirmaId = _aktifFirmaProvider.AktifFirmaId ?? _aktifFirmaProvider.Mevcut.FirmaId;

        if (aktifFirmaId > 0 && guzergah.FirmaId.HasValue && guzergah.FirmaId != aktifFirmaId)
        {
            _logger.LogWarning(
                "Firma dışı güzergah sefer erişimi engellendi. GuzergahId={GuzergahId}, GuzergahFirmaId={GuzergahFirmaId}, AktifFirmaId={AktifFirmaId}",
                guzergahId, guzergah.FirmaId, aktifFirmaId);
            throw new UnauthorizedAccessException(
                $"Güzergah aktif firmaya ait değil. GuzergahId={guzergahId}");
        }

        return guzergah;
    }
}

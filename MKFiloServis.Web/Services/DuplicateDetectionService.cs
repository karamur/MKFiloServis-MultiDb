using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public sealed class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public DuplicateDetectionService(IDbContextFactory<ApplicationDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task<List<DuplicateGrup>> TespitRaporuAsync(int yil, int ay, int? kurumId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var guzergahIds = kurumId.HasValue && kurumId > 0
            ? await db.Guzergahlar.Where(g => !g.IsDeleted && g.KurumId == kurumId.Value)
                .Select(g => g.Id).ToListAsync()
            : await db.Guzergahlar.Where(g => !g.IsDeleted)
                .Select(g => g.Id).ToListAsync();

        var dupGroups = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && p.AracId.HasValue && p.AracId > 0
                        && guzergahIds.Contains(p.GuzergahId!.Value))
            .GroupBy(p => new { GuzergahId = p.GuzergahId!.Value, AracId = p.AracId!.Value })
            .Where(g => g.Count() > 1)
            .Select(g => new
            {
                g.Key.GuzergahId,
                g.Key.AracId,
                Satirlar = g.Select(p => new
                {
                    p.Id,
                    p.Yon,
                    p.Slot,
                    p.SeferSayisi,
                    p.SoforAdi,
                    p.Plaka
                }).ToList()
            })
            .ToListAsync();

        if (!dupGroups.Any()) return [];

        var guzergahAdlari = await db.Guzergahlar
            .Where(g => dupGroups.Select(d => d.GuzergahId).Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.GuzergahAdi ?? "");

        return dupGroups.Select(d => new DuplicateGrup
        {
            GuzergahId = d.GuzergahId,
            GuzergahAdi = guzergahAdlari.GetValueOrDefault(d.GuzergahId, ""),
            AracId = d.AracId,
            Plaka = d.Satirlar.FirstOrDefault()?.Plaka ?? "",
            Satirlar = d.Satirlar.Select(s => new DuplicateSatir
            {
                PuantajKayitId = s.Id,
                Yon = s.Yon.ToString(),
                Slot = s.Slot.ToString(),
                SeferSayisi = s.SeferSayisi,
                SoforAdi = s.SoforAdi ?? ""
            }).ToList(),
            MergeOnerisi = d.Satirlar.Any(s => s.Yon == Shared.Entities.PuantajYon.SabahAksam)
                ? "Zaten SabahAksam — slot çakışması var"
                : "SabahAksam yap, birleştir"
        }).ToList();
    }
}



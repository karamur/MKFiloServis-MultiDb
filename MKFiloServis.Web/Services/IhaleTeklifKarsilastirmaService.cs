using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class IhaleTeklifKarsilastirmaService : IIhaleTeklifKarsilastirmaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public IhaleTeklifKarsilastirmaService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IhaleTeklifKarsilastirmaDto?> CompareAsync(int solVersiyonId, int sagVersiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (solVersiyonId == sagVersiyonId)
            throw new InvalidOperationException("Karşılaştırma için farklı iki versiyon seçilmelidir.");

        var versiyonlar = await context.IhaleTeklifVersiyonlari
            .Where(x => x.Id == solVersiyonId || x.Id == sagVersiyonId)
            .ToListAsync();

        var sol = versiyonlar.FirstOrDefault(x => x.Id == solVersiyonId);
        var sag = versiyonlar.FirstOrDefault(x => x.Id == sagVersiyonId);

        if (sol == null || sag == null)
            return null;

        if (sol.IhaleProjeId != sag.IhaleProjeId)
            throw new InvalidOperationException("Sadece aynı ihaleye ait versiyonlar karşılaştırılabilir.");

        return new IhaleTeklifKarsilastirmaDto
        {
            SolVersiyonId = sol.Id,
            SolRevizyonKodu = sol.RevizyonKodu,
            SagVersiyonId = sag.Id,
            SagRevizyonKodu = sag.RevizyonKodu,
            SolToplamMaliyet = sol.ToplamMaliyet,
            SagToplamMaliyet = sag.ToplamMaliyet,
            ToplamMaliyetFarki = sag.ToplamMaliyet - sol.ToplamMaliyet,
            SolTeklifTutari = sol.TeklifTutari,
            SagTeklifTutari = sag.TeklifTutari,
            TeklifTutariFarki = sag.TeklifTutari - sol.TeklifTutari,
            SolKarMarjiTutari = sol.KarMarjiTutari,
            SagKarMarjiTutari = sag.KarMarjiTutari,
            KarMarjiTutariFarki = sag.KarMarjiTutari - sol.KarMarjiTutari,
            SolKarMarjiOrani = sol.KarMarjiOrani,
            SagKarMarjiOrani = sag.KarMarjiOrani,
            KarMarjiOraniFarki = sag.KarMarjiOrani - sol.KarMarjiOrani
        };
    }
}




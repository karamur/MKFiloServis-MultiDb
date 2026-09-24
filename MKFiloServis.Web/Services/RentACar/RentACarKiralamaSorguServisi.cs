using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services.RentACar;

public sealed class RentACarKiralamaSorguServisi : IRentACarKiralamaSorguServisi
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public RentACarKiralamaSorguServisi(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IAktifFirmaProvider aktifFirmaProvider)
    {
        _contextFactory = contextFactory;
        _aktifFirmaProvider = aktifFirmaProvider;
    }

    public async Task<IReadOnlyList<RentACarKiralamaSatiri>> GetKiralamalarAsync(
        CancellationToken cancellationToken = default)
    {
        var firmaId = _aktifFirmaProvider.AktifFirmaId;
        if (!firmaId.HasValue || firmaId.Value <= 0)
        {
            return Array.Empty<RentACarKiralamaSatiri>();
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await (
            from kiralama in context.MusteriKiralamalar.AsNoTracking()
            join cari in context.Cariler.AsNoTracking() on kiralama.MusteriId equals cari.Id
            join arac in context.Araclar.AsNoTracking() on kiralama.AracId equals arac.Id
            where kiralama.FirmaId == firmaId.Value && !kiralama.IsDeleted
            orderby kiralama.BaslangicTarihi descending
            select new RentACarKiralamaSatiri
            {
                Id = kiralama.Id,
                MusteriId = kiralama.MusteriId,
                AracId = kiralama.AracId,
                SozlesmeNo = kiralama.SozlesmeNo,
                MusteriUnvan = cari.Unvan,
                Plaka = arac.AktifPlaka ?? string.Empty,
                AracTanimi = ((arac.Marka ?? string.Empty) + " " + (arac.Model ?? string.Empty)).Trim(),
                BaslangicTarihi = kiralama.BaslangicTarihi,
                PlanlananBitisTarihi = kiralama.PlanlananBitisTarihi,
                GercekBitisTarihi = kiralama.GercekBitisTarihi,
                Durum = kiralama.Durum,
                ToplamTutar = kiralama.ToplamTutar,
                GunlukFiyat = kiralama.GunlukFiyat,
                Depozito = kiralama.Depozito,
                Notlar = kiralama.Notlar,
                OdemeDurumu = kiralama.OdemeDurumu
            }).ToListAsync(cancellationToken);
    }
}

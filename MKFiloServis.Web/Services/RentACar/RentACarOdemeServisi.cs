using System.ComponentModel.DataAnnotations;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services.RentACar;

public sealed class RentACarOdemeKayitTalebi
{
    [Range(1, int.MaxValue, ErrorMessage = "Kiralama seçilmelidir.")]
    public int MusteriKiralamaId { get; set; }
    public DateTime IslemTarihi { get; set; } = DateTime.Now;
    public RentACarOdemeHareketTuru HareketTuru { get; set; }
    public RentACarOdemeYontemi OdemeYontemi { get; set; }
    [Range(typeof(decimal), "0.01", "999999999.99", ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
    public decimal Tutar { get; set; }
    [StringLength(100)] public string? BelgeNo { get; set; }
    [StringLength(500)] public string? Aciklama { get; set; }
}

public sealed class RentACarOdemeOzet
{
    public int KiralamaId { get; init; }
    public string SozlesmeNo { get; init; } = string.Empty;
    public string Musteri { get; init; } = string.Empty;
    public string Plaka { get; init; } = string.Empty;
    public KiralamaDurumu KiralamaDurumu { get; init; }
    public decimal KiralamaTutari { get; init; }
    public decimal KiraTahsilati { get; init; }
    public decimal KiraIadesi { get; init; }
    public decimal KalanKiraTutari => Math.Max(0, KiralamaTutari - NetKiraTahsilati);
    public decimal NetKiraTahsilati => KiraTahsilati - KiraIadesi;
    public decimal DepozitoTutari { get; init; }
    public decimal DepozitoTahsilati { get; init; }
    public decimal DepozitoIadesi { get; init; }
    public decimal EmanettekiDepozito => DepozitoTahsilati - DepozitoIadesi;
    public KiralamaOdemeDurumu OdemeDurumu { get; init; }
}

public sealed class RentACarOdemeHareketSatiri
{
    public int Id { get; init; }
    public int MusteriKiralamaId { get; init; }
    public string SozlesmeNo { get; init; } = string.Empty;
    public string Musteri { get; init; } = string.Empty;
    public string Plaka { get; init; } = string.Empty;
    public DateTime IslemTarihi { get; init; }
    public RentACarOdemeHareketTuru HareketTuru { get; init; }
    public RentACarOdemeYontemi OdemeYontemi { get; init; }
    public decimal Tutar { get; init; }
    public string? BelgeNo { get; init; }
    public string? Aciklama { get; init; }
}

public sealed class RentACarOdemeServisi
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public RentACarOdemeServisi(IDbContextFactory<ApplicationDbContext> contextFactory, IAktifFirmaProvider aktifFirmaProvider)
    {
        _contextFactory = contextFactory;
        _aktifFirmaProvider = aktifFirmaProvider;
    }

    public async Task<IReadOnlyList<RentACarOdemeOzet>> GetOzetlerAsync(CancellationToken cancellationToken = default)
    {
        var firmaId = AktifFirmaIdAl();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var kiralamalar = await (from k in context.MusteriKiralamalar.AsNoTracking()
                                join c in context.Cariler.AsNoTracking() on k.MusteriId equals c.Id
                                join a in context.Araclar.AsNoTracking() on k.AracId equals a.Id
                                where k.FirmaId == firmaId && !k.IsDeleted && k.Durum != KiralamaDurumu.IptalEdildi
                                orderby k.BaslangicTarihi descending
                                select new { k, c.Unvan, Plaka = a.AktifPlaka ?? string.Empty }).ToListAsync(cancellationToken);

        var hareketler = await context.RentACarOdemeHareketleri.AsNoTracking()
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        return kiralamalar.Select(x =>
        {
            var h = hareketler.Where(y => y.MusteriKiralamaId == x.k.Id).ToList();
            return new RentACarOdemeOzet
            {
                KiralamaId = x.k.Id,
                SozlesmeNo = x.k.SozlesmeNo ?? "-",
                Musteri = x.Unvan,
                Plaka = x.Plaka,
                KiralamaDurumu = x.k.Durum,
                KiralamaTutari = x.k.ToplamTutar,
                KiraTahsilati = Toplam(h, RentACarOdemeHareketTuru.KiraTahsilati),
                KiraIadesi = Toplam(h, RentACarOdemeHareketTuru.KiraIadesi),
                DepozitoTutari = x.k.Depozito ?? 0,
                DepozitoTahsilati = Toplam(h, RentACarOdemeHareketTuru.DepozitoTahsilati),
                DepozitoIadesi = Toplam(h, RentACarOdemeHareketTuru.DepozitoIadesi),
                OdemeDurumu = x.k.OdemeDurumu
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<RentACarOdemeHareketSatiri>> GetHareketlerAsync(CancellationToken cancellationToken = default)
    {
        var firmaId = AktifFirmaIdAl();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await (from h in context.RentACarOdemeHareketleri.AsNoTracking()
                      join k in context.MusteriKiralamalar.AsNoTracking() on h.MusteriKiralamaId equals k.Id
                      join c in context.Cariler.AsNoTracking() on k.MusteriId equals c.Id
                      join a in context.Araclar.AsNoTracking() on k.AracId equals a.Id
                      where h.FirmaId == firmaId && !h.IsDeleted
                      orderby h.IslemTarihi descending, h.Id descending
                      select new RentACarOdemeHareketSatiri
                      {
                          Id = h.Id, MusteriKiralamaId = k.Id, SozlesmeNo = k.SozlesmeNo ?? "-",
                          Musteri = c.Unvan, Plaka = a.AktifPlaka ?? string.Empty,
                          IslemTarihi = h.IslemTarihi, HareketTuru = h.HareketTuru,
                          OdemeYontemi = h.OdemeYontemi, Tutar = h.Tutar,
                          BelgeNo = h.BelgeNo, Aciklama = h.Aciklama
                      }).ToListAsync(cancellationToken);
    }

    public async Task KaydetAsync(RentACarOdemeKayitTalebi talep, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(talep);
        if (talep.Tutar <= 0) throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");
        var firmaId = AktifFirmaIdAl();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var kiralama = await context.MusteriKiralamalar.FirstOrDefaultAsync(x => x.Id == talep.MusteriKiralamaId && x.FirmaId == firmaId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Kiralama kaydı bulunamadı.");
        if (kiralama.Durum == KiralamaDurumu.IptalEdildi) throw new InvalidOperationException("İptal edilmiş rezervasyona ödeme hareketi eklenemez.");

        var mevcut = await context.RentACarOdemeHareketleri.Where(x => x.MusteriKiralamaId == kiralama.Id && x.FirmaId == firmaId && !x.IsDeleted).ToListAsync(cancellationToken);
        LimitDogrula(kiralama, mevcut, talep);
        context.RentACarOdemeHareketleri.Add(new RentACarOdemeHareketi
        {
            FirmaId = firmaId, MusteriKiralamaId = kiralama.Id, IslemTarihi = talep.IslemTarihi,
            HareketTuru = talep.HareketTuru, OdemeYontemi = talep.OdemeYontemi,
            Tutar = decimal.Round(talep.Tutar, 2), BelgeNo = Temizle(talep.BelgeNo), Aciklama = Temizle(talep.Aciklama)
        });

        mevcut.Add(new RentACarOdemeHareketi { HareketTuru = talep.HareketTuru, Tutar = talep.Tutar });
        var net = Toplam(mevcut, RentACarOdemeHareketTuru.KiraTahsilati) - Toplam(mevcut, RentACarOdemeHareketTuru.KiraIadesi);
        kiralama.OdemeDurumu = net <= 0 ? KiralamaOdemeDurumu.Beklemede
            : net >= kiralama.ToplamTutar ? KiralamaOdemeDurumu.Odendi : KiralamaOdemeDurumu.KismiOdendi;
        kiralama.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void LimitDogrula(MusteriKiralama kiralama, List<RentACarOdemeHareketi> mevcut, RentACarOdemeKayitTalebi talep)
    {
        var kiraNet = Toplam(mevcut, RentACarOdemeHareketTuru.KiraTahsilati) - Toplam(mevcut, RentACarOdemeHareketTuru.KiraIadesi);
        var depozitoNet = Toplam(mevcut, RentACarOdemeHareketTuru.DepozitoTahsilati) - Toplam(mevcut, RentACarOdemeHareketTuru.DepozitoIadesi);
        if (talep.HareketTuru == RentACarOdemeHareketTuru.KiraTahsilati && kiraNet + talep.Tutar > kiralama.ToplamTutar)
            throw new InvalidOperationException("Tahsilat, kalan kiralama tutarını aşamaz.");
        if (talep.HareketTuru == RentACarOdemeHareketTuru.KiraIadesi && talep.Tutar > kiraNet)
            throw new InvalidOperationException("Kira iadesi, net tahsilatı aşamaz.");
        if (talep.HareketTuru == RentACarOdemeHareketTuru.DepozitoTahsilati && depozitoNet + talep.Tutar > (kiralama.Depozito ?? 0))
            throw new InvalidOperationException("Depozito tahsilatı, planlanan depozitoyu aşamaz.");
        if (talep.HareketTuru == RentACarOdemeHareketTuru.DepozitoIadesi && talep.Tutar > depozitoNet)
            throw new InvalidOperationException("Depozito iadesi, emanetteki depozitoyu aşamaz.");
    }

    private static decimal Toplam(IEnumerable<RentACarOdemeHareketi> hareketler, RentACarOdemeHareketTuru tur)
        => hareketler.Where(x => x.HareketTuru == tur).Sum(x => x.Tutar);
    private static string? Temizle(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private int AktifFirmaIdAl()
        => _aktifFirmaProvider.AktifFirmaId is int id && id > 0
            ? id
            : throw new InvalidOperationException("Aktif firma seçilmelidir.");
}

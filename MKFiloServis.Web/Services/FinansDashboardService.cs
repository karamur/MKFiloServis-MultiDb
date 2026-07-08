using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Finans Dashboard — maaş + hakediş + operasyon verilerini tek ekranda birleştirir.
/// </summary>
public class FinansDashboardService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public FinansDashboardService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<FinansDashboardDto> GetAsync(int yil, int ay, int firmaId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Maaş snapshot (varsa kullan, yoksa boş)
        var snapshot = await context.MaasOdemeSnapshotlar
            .AsNoTracking()
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
            .ToListAsync();

        // Snapshot güvenilir mi? (kilitli + fiş var)
        var snapshotKullaniliyor = snapshot.Any() && snapshot.All(x => x.Kilitli) && snapshot.Any(x => x.MuhasebeFisId != null);

        // Hakediş verisi: Snapshot varsa oradan, yoksa canlı Hakedis'ten (yeni operasyonel model)
        decimal toplamHakedisGelir, toplamHakedisGider;
        int toplamSefer;

        if (snapshotKullaniliyor)
        {
            // Faz 5: Dashboard SADECE snapshot'tan okur
            toplamHakedisGelir = snapshot.Sum(x => x.HakedisGelir);
            toplamHakedisGider = snapshot.Sum(x => x.HakedisGider);
            toplamSefer = 0; // Snapshot'ta sefer verisi yok, 0 göster (opsiyonel: ayrı alan eklenebilir)
        }
        else
        {
            // Fallback: Snapshot yoksa canlı Hakedis tablosundan oku (yeni operasyonel model)
            var hakedisler = await context.Hakedisler
                .AsNoTracking()
                .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
                .ToListAsync();

            toplamHakedisGelir = hakedisler
                .Where(x => x.Tip == HakedisTipi.Kurum)
                .Sum(x => x.GenelToplam > 0 ? x.GenelToplam : x.Tutar);

            toplamHakedisGider = hakedisler
                .Where(x => x.Tip == HakedisTipi.Tedarikci)
                .Sum(x => x.GenelToplam > 0 ? x.GenelToplam : x.Tutar);

            // Sefer sayısında çift sayımı önlemek için kurum hakedişleri baz alınır.
            toplamSefer = (int)Math.Round(
                hakedisler
                    .Where(x => x.Tip == HakedisTipi.Kurum)
                    .Sum(x => x.ToplamSeferSayisi),
                MidpointRounding.AwayFromZero);
        }

        // Muhasebe fişleri — YIL + AY + FİRMA + ONAYLI (770 gider)
        var muhasebeFisler = await context.MuhasebeFisleri
            .AsNoTracking()
            .Include(x => x.Kalemler).ThenInclude(k => k.Hesap)
            .Where(x => x.FisTarihi.Year == yil && x.FisTarihi.Month == ay
                     && !x.IsDeleted && x.Durum == FisDurum.Onaylandi)
            .ToListAsync();

        // Muhasebe 770 toplamı (yıl + ay + onaylı)
        var muhasebe770Toplam = muhasebeFisler
            .SelectMany(f => f.Kalemler)
            .Where(k => k.Hesap != null && k.Hesap.HesapKodu.StartsWith("770"))
            .Sum(k => k.Borc);

        var toplamMaas = snapshot.Sum(x => x.Odenecek);
        var personelSayisi = snapshot.Select(x => x.PersonelId).Distinct().Count();

        // Net Kar = Gelir - Maaş - Gider
        var netKar = toplamHakedisGelir - toplamMaas - toplamHakedisGider;

        // Fatura metrikleri
        var faturalar = await context.Faturalar
            .AsNoTracking()
            .Where(x => x.FaturaTarihi.Year == yil && x.FaturaTarihi.Month == ay
                     && x.FirmaId == firmaId && !x.IsDeleted)
            .ToListAsync();
        var gelenFaturaSayisi = faturalar.Count(x => x.FaturaYonu == FaturaYonu.Gelen);
        var gidenFaturaSayisi = faturalar.Count(x => x.FaturaYonu == FaturaYonu.Giden);
        var toplamKDV = faturalar.Sum(x => x.KdvTutar);
        var toplamFaturaGider = faturalar.Where(x => x.FaturaYonu == FaturaYonu.Gelen).Sum(x => x.GenelToplam);
        var toplamFaturaGelir = faturalar.Where(x => x.FaturaYonu == FaturaYonu.Giden).Sum(x => x.GenelToplam);

        return new FinansDashboardDto
        {
            Yil = yil,
            Ay = ay,

            ToplamMaas = toplamMaas,
            ToplamHakedisGelir = toplamHakedisGelir,
            ToplamHakedisGider = toplamHakedisGider,
            NetKar = netKar,
            ToplamSefer = toplamSefer,
            PersonelSayisi = personelSayisi,
            Muhasebe770Toplam = muhasebe770Toplam,

            GelenFaturaSayisi = gelenFaturaSayisi,
            GidenFaturaSayisi = gidenFaturaSayisi,
            ToplamKDV = toplamKDV,
            ToplamFaturaGider = toplamFaturaGider,
            ToplamFaturaGelir = toplamFaturaGelir,

            SnapshotVar = snapshot.Any(),
            SnapshotKilitli = snapshot.Any() && snapshot.All(x => x.Kilitli),
            FisVar = snapshot.Any(x => x.MuhasebeFisId != null),
            SnapshotKullaniliyorMu = snapshotKullaniliyor
        };
    }
}

public class FinansDashboardDto
{
    public int Yil { get; set; }
    public int Ay { get; set; }

    public decimal ToplamMaas { get; set; }
    public decimal ToplamHakedisGelir { get; set; }
    public decimal ToplamHakedisGider { get; set; }
    public decimal NetKar { get; set; }
    public int ToplamSefer { get; set; }
    public int PersonelSayisi { get; set; }
    public decimal Muhasebe770Toplam { get; set; }

    public int GelenFaturaSayisi { get; set; }
    public int GidenFaturaSayisi { get; set; }
    public decimal ToplamKDV { get; set; }
    public decimal ToplamFaturaGider { get; set; }
    public decimal ToplamFaturaGelir { get; set; }

    public bool SnapshotVar { get; set; }
    public bool SnapshotKilitli { get; set; }
    public bool FisVar { get; set; }
    public bool SnapshotKullaniliyorMu { get; set; }
}



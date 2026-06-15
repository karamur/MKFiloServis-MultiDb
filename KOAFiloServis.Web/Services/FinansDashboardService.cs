using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

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

        // Hakediş gelirleri
        var hakedisler = await context.HakedisPuantajlar
            .AsNoTracking()
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
            .ToListAsync();

        // Operasyon kayıtları (sefer sayısı)
        var donemBaslangic = new DateTime(yil, ay, 1);
        var donemBitis = donemBaslangic.AddMonths(1);
        var operasyonlar = await context.OperasyonKayitlari
            .AsNoTracking()
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted
                     && x.Tarih >= donemBaslangic && x.Tarih < donemBitis)
            .ToListAsync();

        // Muhasebe fişleri (770 gider)
        var muhasebeFisler = await context.MuhasebeFisleri
            .AsNoTracking()
            .Include(x => x.Kalemler)
            .Where(x => x.FisTarihi.Year == yil && x.FisTarihi.Month == ay
                     && !x.IsDeleted && x.Durum == FisDurum.Onaylandi)
            .ToListAsync();

        var toplamMaas = snapshot.Sum(x => x.Odenecek);
        var toplamHakedisGelir = hakedisler.Sum(x => x.TahsilEdilecekTutar);
        var toplamHakedisGider = hakedisler.Sum(x => x.OdenecekTutar);
        var toplamSefer = operasyonlar.Sum(x => x.SeferSayisi);
        var personelSayisi = snapshot.Select(x => x.PersonelId).Distinct().Count();

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

        // Muhasebe 770 toplamı
        var muhasebe770Toplam = muhasebeFisler
            .SelectMany(f => f.Kalemler)
            .Where(k => k.Hesap != null && k.Hesap.HesapKodu.StartsWith("770"))
            .Sum(k => k.Borc);

        return new FinansDashboardDto
        {
            Yil = yil,
            Ay = ay,

            ToplamMaas = toplamMaas,
            ToplamHakedisGelir = toplamHakedisGelir,
            ToplamHakedisGider = toplamHakedisGider,
            NetKar = toplamHakedisGelir - toplamMaas - toplamHakedisGider,
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
            FisVar = snapshot.Any(x => x.MuhasebeFisId != null)
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
}

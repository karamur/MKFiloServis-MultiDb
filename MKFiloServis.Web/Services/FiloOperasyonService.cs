using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public interface IFiloOperasyonService
{
    // Araç Alım/Satım
    Task<List<AracAlimSatim>> GetAracAlimSatimlarAsync();
    Task<List<AracAlimSatim>> GetAracAlimSatimlarAsync(int aracId);
    Task<AracAlimSatim?> GetAracAlimSatimAsync(int id);
    Task<AracAlimSatim> CreateAracAlimSatimAsync(AracAlimSatim alimSatim);
    Task<AracAlimSatim> UpdateAracAlimSatimAsync(AracAlimSatim alimSatim);
    Task DeleteAracAlimSatimAsync(int id);
    Task<List<AracAlimSatim>> GetFaturaKontrolBekleyenlerAsync();

    // Kiralık C Plaka Takip
    Task<List<PlakaDonusum>> GetPlakaDonusumlerAsync();
    Task<PlakaDonusum?> GetPlakaDonusumAsync(int id);
    Task<PlakaDonusum> CreatePlakaDonusumAsync(PlakaDonusum donusum);
    Task<PlakaDonusum> UpdatePlakaDonusumAsync(PlakaDonusum donusum);
    Task DeletePlakaDonusumAsync(int id);


    // Operasyon Durum
    Task<List<AracOperasyonDurum>> GetAracOperasyonDurumlariAsync(int yil, int ay);
    Task<AracOperasyonDurum?> GetAracOperasyonDurumAsync(int aracId, int yil, int ay);
    Task<AracOperasyonDurum> CreateOrUpdateOperasyonDurumAsync(AracOperasyonDurum durum);

    // Raporlar
    Task<List<AracKarZararRaporu>> GetAracKarZararRaporuAsync(int yil, int ay);
    Task<FiloOzetRaporu> GetFiloOzetRaporuAsync(DateTime? tarih = null);
}

public class FiloOperasyonService : IFiloOperasyonService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public FiloOperasyonService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Araç Alım/Satım

    public async Task<List<AracAlimSatim>> GetAracAlimSatimlarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracAlimSatimlar
            .Include(a => a.Arac)
            .Include(a => a.KarsiTarafCari)
            .Include(a => a.Fatura)
            .OrderByDescending(a => a.IslemTarihi)
            .ToListAsync();
    }

    public async Task<List<AracAlimSatim>> GetAracAlimSatimlarAsync(int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracAlimSatimlar
            .Include(a => a.KarsiTarafCari)
            .Include(a => a.Fatura)
            .Where(a => a.AracId == aracId)
            .OrderByDescending(a => a.IslemTarihi)
            .ToListAsync();
    }

    public async Task<AracAlimSatim?> GetAracAlimSatimAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracAlimSatimlar
            .Include(a => a.Arac)
            .Include(a => a.KarsiTarafCari)
            .Include(a => a.Fatura)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<AracAlimSatim> CreateAracAlimSatimAsync(AracAlimSatim alimSatim)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        alimSatim.CreatedAt = DateTime.UtcNow;
        context.AracAlimSatimlar.Add(alimSatim);
        await context.SaveChangesAsync();
        
        // Araç satıldıysa durumu güncelle
        if (alimSatim.IslemTipi == AracIslemTipiDetay.Satis && alimSatim.OdemeDurum == AracIslemOdemeDurum.TamOdendi)
        {
            var arac = await context.Araclar.FindAsync(alimSatim.AracId);
            if (arac != null)
            {
                arac.Aktif = false;
                arac.SatisaAcik = false;
                arac.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
        
        return alimSatim;
    }

    public async Task<AracAlimSatim> UpdateAracAlimSatimAsync(AracAlimSatim alimSatim)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.AracAlimSatimlar.FindAsync(alimSatim.Id);
        if (existing == null)
            throw new InvalidOperationException("Araç alım/satım kaydı bulunamadı.");

        existing.IslemTipi = alimSatim.IslemTipi;
        existing.KarsiTarafCariId = alimSatim.KarsiTarafCariId;
        existing.KarsiTarafAdSoyad = alimSatim.KarsiTarafAdSoyad;
        existing.KarsiTarafTcKimlik = alimSatim.KarsiTarafTcKimlik;
        existing.KarsiTarafTelefon = alimSatim.KarsiTarafTelefon;
        existing.IslemTarihi = alimSatim.IslemTarihi;
        existing.IslemTutari = alimSatim.IslemTutari;
        existing.KDVTutari = alimSatim.KDVTutari;
        existing.ToplamTutar = alimSatim.ToplamTutar;
        existing.NoterAdi = alimSatim.NoterAdi;
        existing.NoterTarihi = alimSatim.NoterTarihi;
        existing.NoterYevmiyeNo = alimSatim.NoterYevmiyeNo;
        existing.NoterIslemTamam = alimSatim.NoterIslemTamam;
        existing.FaturaId = alimSatim.FaturaId;
        existing.FaturaKesildi = alimSatim.FaturaKesildi;
        existing.FaturaKesimTarihi = alimSatim.FaturaKesimTarihi;
        existing.FaturaUyumu = alimSatim.FaturaUyumu;
        existing.FaturaUyumsuzlukAciklama = alimSatim.FaturaUyumsuzlukAciklama;
        existing.OdemeDurum = alimSatim.OdemeDurum;
        existing.OdemeTarihi = alimSatim.OdemeTarihi;
        existing.OdenenTutar = alimSatim.OdenenTutar;
        existing.Notlar = alimSatim.Notlar;
        existing.RuhsatTeslimAlindi = alimSatim.RuhsatTeslimAlindi;
        existing.SigortaTeslimAlindi = alimSatim.SigortaTeslimAlindi;
        existing.MuayeneBelgesiTeslimAlindi = alimSatim.MuayeneBelgesiTeslimAlindi;
        existing.AnahtarTeslimAlindi = alimSatim.AnahtarTeslimAlindi;
        existing.YedekAnahtarTeslimAlindi = alimSatim.YedekAnahtarTeslimAlindi;
        existing.ServisBakimDefteri = alimSatim.ServisBakimDefteri;
        existing.EksikBelgeler = alimSatim.EksikBelgeler;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAracAlimSatimAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var alimSatim = await context.AracAlimSatimlar.FindAsync(id);
        if (alimSatim != null)
        {
            alimSatim.IsDeleted = true;
            alimSatim.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<AracAlimSatim>> GetFaturaKontrolBekleyenlerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracAlimSatimlar
            .Include(a => a.Arac)
            .Include(a => a.KarsiTarafCari)
            .Include(a => a.Fatura)
            .Where(a => a.NoterIslemTamam && !a.FaturaKesildi)
            .OrderBy(a => a.NoterTarihi)
            .ToListAsync();
    }

    #endregion

    #region Kiralık C Plaka Takip

    public async Task<List<PlakaDonusum>> GetPlakaDonusumlerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PlakaDonusumler
            .Include(p => p.Arac)
            .Include(p => p.PlakaSatisCarisi)
            .OrderByDescending(p => p.BasvuruTarihi)
            .ToListAsync();
    }

    public async Task<PlakaDonusum?> GetPlakaDonusumAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PlakaDonusumler
            .Include(p => p.Arac)
            .Include(p => p.PlakaSatisCarisi)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PlakaDonusum> CreatePlakaDonusumAsync(PlakaDonusum donusum)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        donusum.CreatedAt = DateTime.UtcNow;
        context.PlakaDonusumler.Add(donusum);
        await context.SaveChangesAsync();
        return donusum;
    }

    public async Task<PlakaDonusum> UpdatePlakaDonusumAsync(PlakaDonusum donusum)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PlakaDonusumler.FindAsync(donusum.Id);
        if (existing == null)
            throw new InvalidOperationException("Plaka dönüşüm kaydı bulunamadı.");

        existing.YeniPlaka = donusum.YeniPlaka;
        existing.YeniPlakaTipi = donusum.YeniPlakaTipi;
        existing.Durum = donusum.Durum;
        existing.OnayTarihi = donusum.OnayTarihi;
        existing.TamamlanmaTarihi = donusum.TamamlanmaTarihi;
        existing.PlakaBedeliMasrafi = donusum.PlakaBedeliMasrafi;
        existing.EmnivetHarci = donusum.EmnivetHarci;
        existing.NoterMasrafi = donusum.NoterMasrafi;
        existing.DigerMasraflar = donusum.DigerMasraflar;
        existing.PlakaSatilacakMi = donusum.PlakaSatilacakMi;
        existing.PlakaSatisBedeli = donusum.PlakaSatisBedeli;
        existing.PlakaSatisCarisiId = donusum.PlakaSatisCarisiId;
        existing.PlakaSatildi = donusum.PlakaSatildi;
        existing.PlakaSatisTarihi = donusum.PlakaSatisTarihi;
        existing.Notlar = donusum.Notlar;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeletePlakaDonusumAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var donusum = await context.PlakaDonusumler.FindAsync(id);
        if (donusum != null)
        {
            donusum.IsDeleted = true;
            donusum.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    #endregion


    #region Operasyon Durum

    public async Task<List<AracOperasyonDurum>> GetAracOperasyonDurumlariAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracOperasyonDurumlari
            .Include(a => a.Arac)
            .Where(a => a.Yil == yil && a.Ay == ay)
            .OrderBy(a => a.Arac.AktifPlaka)
            .ToListAsync();
    }

    public async Task<AracOperasyonDurum?> GetAracOperasyonDurumAsync(int aracId, int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracOperasyonDurumlari
            .Include(a => a.Arac)
            .FirstOrDefaultAsync(a => a.AracId == aracId && a.Yil == yil && a.Ay == ay);
    }

    public async Task<AracOperasyonDurum> CreateOrUpdateOperasyonDurumAsync(AracOperasyonDurum durum)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.AracOperasyonDurumlari
            .FirstOrDefaultAsync(a => a.AracId == durum.AracId && a.Yil == durum.Yil && a.Ay == durum.Ay);

        if (existing == null)
        {
            durum.CreatedAt = DateTime.UtcNow;
            context.AracOperasyonDurumlari.Add(durum);
        }
        else
        {
            existing.OperasyonTipi = durum.OperasyonTipi;
            existing.ToplamCalismaGunu = durum.ToplamCalismaGunu;
            existing.ToplamSeferSayisi = durum.ToplamSeferSayisi;
            existing.ToplamKm = durum.ToplamKm;
            existing.BrutGelir = durum.BrutGelir;
            existing.KomisyonKesintisi = durum.KomisyonKesintisi;
            existing.YakitGideri = durum.YakitGideri;
            existing.SoforMaliyeti = durum.SoforMaliyeti;
            existing.KiraBedeli = durum.KiraBedeli;
            existing.BakimOnarimGideri = durum.BakimOnarimGideri;
            existing.SigortaGideri = durum.SigortaGideri;
            existing.VergiGideri = durum.VergiGideri;
            existing.OtoyolGideri = durum.OtoyolGideri;
            existing.DigerGiderler = durum.DigerGiderler;
            existing.Notlar = durum.Notlar;
            existing.UpdatedAt = DateTime.UtcNow;
            durum = existing;
        }

        await context.SaveChangesAsync();
        return durum;
    }

    #endregion

    #region Raporlar

    public async Task<List<AracKarZararRaporu>> GetAracKarZararRaporuAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var durumlar = await context.AracOperasyonDurumlari
            .Include(a => a.Arac)
            .Where(a => a.Yil == yil && a.Ay == ay)
            .ToListAsync();

        return durumlar.Select(d => new AracKarZararRaporu
        {
            AracId = d.AracId,
            Plaka = d.Arac?.AktifPlaka ?? "-",
            Marka = d.Arac?.Marka ?? "-",
            Model = d.Arac?.Model ?? "-",
            OperasyonTipi = d.OperasyonTipi,
            ToplamCalismaGunu = d.ToplamCalismaGunu,
            ToplamSeferSayisi = d.ToplamSeferSayisi,
            BrutGelir = d.BrutGelir,
            ToplamGider = d.ToplamGider,
            NetKarZarar = d.NetKarZarar
        }).ToList();
    }

    public async Task<FiloOzetRaporu> GetFiloOzetRaporuAsync(DateTime? tarih = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = tarih ?? DateTime.Today;
        var yil = bugun.Year;
        var ay = bugun.Month;

        var araclar = await context.Araclar
            .Where(a => a.Aktif)
            .ToListAsync();

        var satisaBekleyenler = await context.AracAlimSatimlar
            .Where(a => a.IslemTipi == AracIslemTipiDetay.Satis && !a.NoterIslemTamam)
            .CountAsync();

        var plakaDonusumleri = await context.PlakaDonusumler
            .Where(p => p.Durum != PlakaDonusumDurum.Tamamlandi && p.Durum != PlakaDonusumDurum.IptalEdildi)
            .CountAsync();

        var aktifTedarikciIs = await context.TasimaTedarikciIsler
            .Where(i => i.Durum == TasimaTedarikciIsDurum.Aktif)
            .CountAsync();

        return new FiloOzetRaporu
        {
            Tarih = bugun,
            ToplamAracSayisi = araclar.Count,
            OzmalAracSayisi = araclar.Count(a => a.SahiplikTipi == AracSahiplikTipi.Ozmal),
            KiralikAracSayisi = araclar.Count(a => a.SahiplikTipi == AracSahiplikTipi.Kiralik),
            TedarikciAraciSayisi = araclar.Count(a => a.TasimaTedarikciId.HasValue),
            SatisaBekleyenAracSayisi = araclar.Count(a => a.SatisaAcik),
            NoterBekleyenSatisSayisi = satisaBekleyenler,
            DevamEdenPlakaDonusumSayisi = plakaDonusumleri,
            AktifTedarikciIsSayisi = aktifTedarikciIs
        };
    }

    #endregion
}

#region Rapor Modelleri

public class AracKarZararRaporu
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string Marka { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public AracOperasyonTipi OperasyonTipi { get; set; }
    public int ToplamCalismaGunu { get; set; }
    public int ToplamSeferSayisi { get; set; }
    public decimal BrutGelir { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal NetKarZarar { get; set; }
}

public class FiloOzetRaporu
{
    public DateTime Tarih { get; set; }
    public int ToplamAracSayisi { get; set; }
    public int OzmalAracSayisi { get; set; }
    public int KiralikAracSayisi { get; set; }
    public int TedarikciAraciSayisi { get; set; }
    public int SatisaBekleyenAracSayisi { get; set; }
    public int NoterBekleyenSatisSayisi { get; set; }
    public int DevamEdenPlakaDonusumSayisi { get; set; }
    public int AktifTedarikciIsSayisi { get; set; }
}

#endregion






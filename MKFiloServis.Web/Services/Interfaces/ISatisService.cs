using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface ISatisService
{
    // Satis Personeli
    Task<List<SatisPersoneli>> GetSatisPersonelListesiAsync();
    Task<SatisPersoneli?> GetSatisPersonelByIdAsync(int id);
    Task<SatisPersoneli> CreateSatisPersonelAsync(SatisPersoneli personel);
    Task<SatisPersoneli> UpdateSatisPersonelAsync(SatisPersoneli personel);
    Task DeleteSatisPersonelAsync(int id);
    Task<SatisPersonelPerformans> GetPersonelPerformansAsync(int personelId, int yil, int? ay = null);

    // Arac Ilanlari
    Task<List<AracIlan>> GetAracIlanListesiAsync(IlanDurum? durum = null);
    Task<AracIlan?> GetAracIlanByIdAsync(int id);
    Task<AracIlan> CreateAracIlanAsync(AracIlan ilan);
    Task<AracIlan> UpdateAracIlanAsync(AracIlan ilan);
    Task DeleteAracIlanAsync(int id);
    Task<AracIlan> IlanSatAsync(int ilanId, AracSatis satisInfo);

    // Piyasa Karsilastirma
    Task<List<PiyasaIlan>> GetPiyasaIlanlariAsync(int aracIlanId);
    Task<PiyasaIlan> AddPiyasaIlanAsync(PiyasaIlan piyasaIlan);
    Task DeletePiyasaIlanAsync(int id);
    Task<PiyasaDegerlendirme> GetPiyasaDegerlendirmeAsync(int aracIlanId);
    Task<List<PiyasaIlan>> TaraPiyasaAsync(AracIlan aracIlan);

    // Arac Satislari
    Task<List<AracSatis>> GetAracSatisListesiAsync(int yil, int? ay = null);
    Task<AracSatis?> GetAracSatisByIdAsync(int id);

    // Marka/Model
    Task<List<AracMarka>> GetAracMarkalarAsync();
    Task<List<AracModelTanim>> GetAracModelleriAsync(int markaId);
    Task SeedMarkaModelAsync();

    // Dashboard
    Task<SatisDashboardData> GetDashboardDataAsync(int yil, int? ay = null);
}

#region Models

public class SatisPersonelPerformans
{
    public int PersonelId { get; set; }
    public string PersonelAdi { get; set; } = "";
    public int ToplamIlan { get; set; }
    public int SatilanArac { get; set; }
    public int AktifIlan { get; set; }
    public decimal ToplamSatisTutari { get; set; }
    public decimal ToplamKomisyon { get; set; }
    public decimal HedefGerceklesme { get; set; } // Yuzde
    public List<AylikSatisData> AylikVeriler { get; set; } = new();
}

public class AylikSatisData
{
    public int Ay { get; set; }
    public string AyAdi { get; set; } = "";
    public int SatisSayisi { get; set; }
    public decimal SatisTutari { get; set; }
    public decimal Komisyon { get; set; }
}

public class PiyasaDegerlendirme
{
    public int AracIlanId { get; set; }
    public int KarsilastirmaAdet { get; set; }
    public decimal MinFiyat { get; set; }
    public decimal MaxFiyat { get; set; }
    public decimal OrtalamaFiyat { get; set; }
    public decimal MedianFiyat { get; set; }
    public decimal BizimFiyat { get; set; }
    public string Degerlendirme { get; set; } = ""; // "Uygun", "Piyasada", "Yuksek"
    public decimal FiyatFarki { get; set; } // Ortalamadan fark
    public decimal FiyatFarkiYuzde { get; set; }
    public List<SehirDagilimi> SehirDagilimi { get; set; } = new();
}

public class SehirDagilimi
{
    public string Sehir { get; set; } = "";
    public int IlanSayisi { get; set; }
    public decimal OrtalamaFiyat { get; set; }
}

public class SatisDashboardData
{
    public int ToplamAktifIlan { get; set; }
    public int BuAySatilanArac { get; set; }
    public decimal BuAySatisTutari { get; set; }
    public decimal BuAyKomisyon { get; set; }
    public decimal OrtalamaKarlilik { get; set; }
    public int BekleyenIlan { get; set; }
    public int RezerveIlan { get; set; }
    public List<SatisPersonelOzet> PersonelOzetleri { get; set; } = new();
    public List<MarkaIlanDagilimi> MarkaIlanDagilimi { get; set; } = new();
    public List<AylikSatisData> AylikTrend { get; set; } = new();
}

public class SatisPersonelOzet
{
    public int PersonelId { get; set; }
    public string PersonelAdi { get; set; } = "";
    public int AktifIlan { get; set; }
    public int BuAySatis { get; set; }
    public decimal BuAyKomisyon { get; set; }
}

public class MarkaIlanDagilimi
{
    public string Marka { get; set; } = "";
    public int IlanSayisi { get; set; }
    public int SatisSayisi { get; set; }
}

#endregion




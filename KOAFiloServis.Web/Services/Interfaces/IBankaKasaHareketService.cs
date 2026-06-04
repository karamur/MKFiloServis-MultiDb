using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Models;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Banka/Kasa hareket filtre parametreleri
/// </summary>
public class BankaHareketFilterParams : PagingParameters
{
    public string? SearchTerm { get; set; }
    public int? HesapId { get; set; }
    public int? CariId { get; set; }
    public HareketTipi? HareketTipi { get; set; }
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
}

public interface IBankaKasaHareketService
{
    Task<List<BankaKasaHareket>> GetAllAsync();
    Task<PagedResult<BankaKasaHareket>> GetPagedAsync(BankaHareketFilterParams filter);
    Task<List<BankaKasaHareket>> GetRecentAsync(int count = 5);
    Task<List<BankaKasaHareket>> GetByHesapIdAsync(int hesapId);
    Task<List<BankaKasaHareket>> GetByCariIdAsync(int cariId);
    Task<List<BankaKasaHareket>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<BankaKasaHareket>> GetByTipAsync(HareketTipi tip);
    Task<List<BankaKasaHareket>> GetEslestirmeyeUygunHareketlerAsync(int cariId, HareketTipi tip);
    Task<BankaKasaHareket?> GetByIdAsync(int id);
    Task<BankaKasaHareket> CreateAsync(BankaKasaHareket hareket);
    Task<BankaKasaHareket> UpdateAsync(BankaKasaHareket hareket);
    Task DeleteAsync(int id);
    Task<string> GenerateNextIslemNoAsync(int firmaId = 0);

    // BankaHesap (Kasa/Banka) işlemleri
    Task<List<BankaHesap>> GetHesaplarAsync();
    Task<List<BankaHesap>> GetAktifHesaplarAsync();
    Task<BankaHesap?> GetHesapByIdAsync(int id);
    Task<BankaHesap> CreateHesapAsync(BankaHesap hesap);
    Task<BankaHesap> UpdateHesapAsync(BankaHesap hesap);
    Task DeleteHesapAsync(int id);

    // Mahsup işlemleri
    Task<MahsupSonuc> HesaplarArasiTransferAsync(int kaynakHesapId, int hedefHesapId, decimal tutar, DateTime tarih, string aciklama, string? belgeNo = null, string? muhasebeHesapKodu = null, string? kostMerkeziKodu = null, string? projeKodu = null);
    Task<MahsupSonuc> CariMahsupAsync(int cariId, int hesapId, decimal tutar, DateTime tarih, string aciklama, bool caridenHesaba, string? belgeNo = null, string? muhasebeHesapKodu = null, string? kostMerkeziKodu = null, string? projeKodu = null);
    Task<List<BankaKasaHareket>> GetMahsupHareketleriAsync(DateTime? baslangic = null, DateTime? bitis = null);
    Task MahsupIptalAsync(Guid mahsupGrupId);
    Task<decimal> GetHesapBakiyeAsync(int hesapId);
    Task<Dictionary<int, decimal>> GetTumHesapBakiyeleriAsync();

    // Personel cebinden geri ödeme (kesin çözüm: tek noktadan kapanış)
    Task<PersonelGeriOdemeSonuc> PersonelGeriOdemeYapAsync(int personelId, IEnumerable<int> cebindenHareketIds, int? hesapId, DateTime odemeTarihi, string? aciklama = null);
    Task PersonelGeriOdemeIptalAsync(int cebindenHareketId);

    // Dashboard optimized methods
    Task<DashboardBankaStats> GetDashboardStatsAsync();
}

public class DashboardBankaStats
{
    public decimal ToplamKasa { get; set; }
    public decimal ToplamBanka { get; set; }
}

public class MahsupSonuc
{
    public bool Basarili { get; set; }
    public string? Hata { get; set; }
    public Guid? MahsupGrupId { get; set; }
    public BankaKasaHareket? KaynakHareket { get; set; }
    public BankaKasaHareket? HedefHareket { get; set; }
}

public class PersonelGeriOdemeSonuc
{
    public bool Basarili { get; set; }
    public string? Hata { get; set; }
    public BankaKasaHareket? OdemeHareketi { get; set; }
    public int KapatilanKayitSayisi { get; set; }
    public decimal ToplamTutar { get; set; }
}

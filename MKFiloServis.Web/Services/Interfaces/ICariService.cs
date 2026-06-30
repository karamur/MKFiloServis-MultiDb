using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

public interface ICariService
{
    Task<List<Cari>> GetAllAsync();
    Task<List<Cari>> GetAllWithBakiyeAsync(); // Borc/Alacak hesaplanmis
    Task<PagedResult<Cari>> GetPagedAsync(CariFilterParams filter); // Sayfalı ve filtrelenmiş
    Task<int> GetCountAsync();
    Task<Cari?> GetByIdAsync(int id);
    Task<Cari?> GetByKodAsync(string cariKodu);
    Task<List<Cari>> GetByTipAsync(CariTipi tip);
    Task<Cari> CreateAsync(Cari cari);
    Task<Cari> UpdateAsync(Cari cari);
    Task<Cari> MatchMuhasebeHesapByKodAsync(int cariId, string hesapKodu);
    Task<Cari> EnsureMuhasebeHesapAsync(int cariId);
    Task<bool> DeleteAsync(int id);
    Task<string> GenerateNextKodAsync();

    // İletişim Geçmişi
    Task<List<CariIletisimNot>> GetIletisimNotlariAsync(int cariId, int? adet = null);
    Task<CariIletisimNot> AddIletisimNotuAsync(CariIletisimNot not);
    Task<CariIletisimNot> UpdateIletisimNotuAsync(CariIletisimNot not);
    Task<bool> DeleteIletisimNotuAsync(int notId);

    // Hatırlatıcılar
    Task<List<Hatirlatici>> GetCariHatirlaticilariAsync(int cariId);
    Task<Hatirlatici> AddCariHatirlaticiAsync(Hatirlatici hatirlatici);

    // Vade Uyarıları
    Task<List<CariVadeUyari>> GetVadeUyarilariAsync(int? cariId = null, int yaklasmaSuresiGun = 7);

    // Sefer Ücretleri (taşıma tedarikçisi cari için birden fazla sefer ücreti)
    Task<List<CariSeferUcreti>> GetSeferUcretleriAsync(int cariId);
    Task<CariSeferUcreti> AddSeferUcretiAsync(CariSeferUcreti ucret);
    Task<CariSeferUcreti> UpdateSeferUcretiAsync(CariSeferUcreti ucret);
    Task<bool> DeleteSeferUcretiAsync(int ucretId);
}

/// <summary>
/// Cari listesi için filtre parametreleri
/// </summary>
public class CariFilterParams : PagingParameters
{
    public string? SearchTerm { get; set; }
    public CariTipi? CariTipi { get; set; }
    public string? DurumFiltre { get; set; } // borclu, alacakli, sifir, islemsiz
    public bool? Aktif { get; set; } = true;
}




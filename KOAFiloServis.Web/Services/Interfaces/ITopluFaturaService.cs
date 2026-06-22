using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Models;

namespace KOAFiloServis.Web.Services;

public interface ITopluFaturaService
{
    /// <summary>
    /// Dönem için toplu fatura özeti getirir
    /// </summary>
    Task<TopluFaturaOzet> GetDonemOzetiAsync(int yil, int ay, int? firmaId = null);
    
    /// <summary>
    /// Filtreye göre toplu fatura önizlemesi oluşturur
    /// </summary>
    Task<List<TopluFaturaOnizleme>> GetOnizlemeAsync(TopluFaturaFiltre filtre);
    
    /// <summary>
    /// Seçilen önizlemelere göre toplu fatura oluşturur
    /// </summary>
    Task<TopluFaturaSonuc> FaturaOlusturAsync(List<TopluFaturaOnizleme> onizlemeler, int? firmaId = null);
    
    /// <summary>
    /// Tek cari için fatura oluşturur
    /// </summary>
    Task<TopluFaturaSonuc> TekFaturaOlusturAsync(TopluFaturaOnizleme onizleme, int? firmaId = null);
    
    /// <summary>
    /// Cari için varsayılan fatura ayarlarını getirir
    /// </summary>
    Task<CariFaturaAyar?> GetCariFaturaAyarAsync(int cariId);
    
    /// <summary>
    /// Fatura kesilmemiş puantaj kayıtlarını getirir
    /// </summary>
    Task<List<PuantajKayit>> GetFaturaKesilmemisPuantajlarAsync(int yil, int ay, FaturaYonu yon, int? cariId = null);
    
    /// <summary>
    /// Dönem listesi (puantaj verisi olan dönemler)
    /// </summary>
    Task<List<(int Yil, int Ay)>> GetMevcutDonemlerAsync();
}

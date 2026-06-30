using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Cari borç/alacak detaylı takip servisi interface
/// </summary>
public interface ICariHareketTakipService
{
    /// <summary>
    /// Tüm carilerin borç/alacak özetini getirir
    /// </summary>
    Task<CariBorcAlacakOzet> GetBorcAlacakOzetAsync(
        DateTime? baslangicTarihi = null, 
        DateTime? bitisTarihi = null,
        CariTipi? cariTipi = null,
        bool sadeceBorclu = false,
        bool sadeceRiskli = false);
    
    /// <summary>
    /// Tek bir carinin detaylı borç/alacak takip raporunu getirir
    /// </summary>
    Task<CariHareketTakipRapor> GetCariDetayAsync(
        int cariId, 
        DateTime? baslangicTarihi = null, 
        DateTime? bitisTarihi = null);
    
    /// <summary>
    /// Carinin tüm hareketlerini (fatura + ödeme) tarih sıralı getirir
    /// </summary>
    Task<List<CariHareketDetay>> GetCariHareketlerAsync(
        int cariId, 
        DateTime? baslangicTarihi = null, 
        DateTime? bitisTarihi = null);
    
    /// <summary>
    /// Carinin açık (ödenmemiş/kısmen ödenmiş) faturalarını getirir
    /// </summary>
    Task<List<CariAcikFatura>> GetAcikFaturalarAsync(int cariId);
    
    /// <summary>
    /// Tüm açık faturaları getirir (tahsilat planı için)
    /// </summary>
    Task<List<CariAcikFatura>> GetTumAcikFaturalarAsync(
        CariTipi? cariTipi = null,
        bool sadeceVadesiGecmis = false);
    
    /// <summary>
    /// Carinin aylık trend verilerini getirir
    /// </summary>
    Task<List<CariAylikTrend>> GetAylikTrendAsync(int cariId, int aySayisi = 12);
    
    /// <summary>
    /// Cari risk skorunu hesaplar
    /// </summary>
    Task<int> HesaplaRiskSkoruAsync(int cariId);
    
    /// <summary>
    /// Tahsilat planı önerisi oluşturur
    /// </summary>
    Task<List<TahsilatPlanItem>> OlusturTahsilatPlaniAsync(int cariId);
    
    /// <summary>
    /// Raporu Excel'e aktarır
    /// </summary>
    Task<byte[]> ExportToExcelAsync(CariBorcAlacakOzet rapor);
    
    /// <summary>
    /// Cari detay raporunu Excel'e aktarır
    /// </summary>
    Task<byte[]> ExportCariDetayToExcelAsync(CariHareketTakipRapor rapor);
}




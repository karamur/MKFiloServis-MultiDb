using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IProformaFaturaService
{
    // CRUD
    Task<List<ProformaFatura>> GetAllAsync();
    Task<List<ProformaFatura>> GetByCariIdAsync(int cariId);
    Task<List<ProformaFatura>> GetByDurumAsync(ProformaDurum durum);
    Task<List<ProformaFatura>> GetByDateRangeAsync(DateTime baslangic, DateTime bitis);
    Task<ProformaFatura?> GetByIdAsync(int id);
    Task<ProformaFatura?> GetByIdWithKalemlerAsync(int id);
    Task<ProformaFatura> CreateAsync(ProformaFatura proforma);
    Task<ProformaFatura> UpdateAsync(ProformaFatura proforma);
    Task DeleteAsync(int id);
    
    // Numara Üretimi
    Task<string> GenerateNextProformaNoAsync(int firmaId = 0);
    
    // Kalem İşlemleri
    Task<ProformaFaturaKalem> AddKalemAsync(ProformaFaturaKalem kalem);
    Task<ProformaFaturaKalem> UpdateKalemAsync(ProformaFaturaKalem kalem);
    Task DeleteKalemAsync(int kalemId);
    Task<List<ProformaFaturaKalem>> GetKalemlerAsync(int proformaId);
    
    // Hesaplama
    Task<ProformaFatura> HesaplaAsync(ProformaFatura proforma);
    
    // Durum Değişiklikleri
    Task<ProformaFatura> DurumDegistirAsync(int id, ProformaDurum yeniDurum);
    Task<ProformaFatura> GonderildiOlarakIsaretle(int id);
    Task<ProformaFatura> OnaylandiOlarakIsaretle(int id);
    Task<ProformaFatura> ReddedildiOlarakIsaretle(int id);
    
    // Faturaya Dönüştürme
    Task<Fatura> FaturayaDonusturAsync(int proformaId, DateTime? faturaTarihi = null, DateTime? vadeTarihi = null);
    Task<bool> FaturayaDonusturulmusMu(int proformaId);
    
    // PDF Export
    Task<byte[]> ExportToPdfAsync(int proformaId);
    Task<byte[]> ExportToExcelAsync(List<ProformaFatura> proformalars);
    
    // Dashboard
    Task<ProformaDashboardStats> GetDashboardStatsAsync();
}

public class ProformaDashboardStats
{
    public int TaslakSayisi { get; set; }
    public int GonderilmisSayisi { get; set; }
    public int OnayliSayisi { get; set; }
    public int ReddedilenSayisi { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OnayliTutar { get; set; }
    public List<ProformaFatura> SuresiDolacaklar { get; set; } = [];
}




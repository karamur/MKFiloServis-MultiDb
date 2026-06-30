using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IHakedisPuantajService
{
    // CRUD
    Task<List<HakedisPuantaj>> GetAllAsync(int? firmaId = null, int? yil = null, int? ay = null);
    Task<HakedisPuantaj?> GetByIdAsync(int id);
    Task<HakedisPuantaj> CreateAsync(HakedisPuantaj hakedis);
    Task<HakedisPuantaj> UpdateAsync(HakedisPuantaj hakedis);
    Task DeleteAsync(int id);

    // Detay
    Task<List<HakedisPuantajDetay>> GetDetaylarAsync(int hakedisId);
    Task GunlukSeferGuncelleAsync(int hakedisId, int gun, int seferSayisi, int? seferTuruId, decimal fiyatCarpani, bool mesaiMi, bool ekSeferMi, string? aciklama = null);

    // Kesinti
    Task<List<HakedisKesinti>> GetKesintilerAsync(int hakedisId);
    Task KesintiEkleAsync(int hakedisId, string kesintiAdi, decimal tutar, string? aciklama = null);
    Task KesintiSilAsync(int kesintiId);

    // Hesaplama ve Onay
    Task<HakedisPuantaj> HakedisHesaplaAsync(int hakedisId);
    Task<HakedisPuantaj> OnayaGonderAsync(int id);
    Task<HakedisPuantaj> OnaylaAsync(int id);

    // Dashboard
    Task<HakedisDashboard> GetDashboardAsync(int yil, int ay, int? firmaId = null);
}




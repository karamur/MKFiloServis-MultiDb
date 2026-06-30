namespace MKFiloServis.Web.Services.Interfaces;

public interface IHakedisRaporService
{
    Task<List<AracRapor>> GetAracRaporuAsync(int? firmaId = null, int? yil = null, int? ay = null);
    Task<List<SoforRapor>> GetSoforRaporuAsync(int? firmaId = null, int? yil = null, int? ay = null);
    Task<List<GuzergahRapor>> GetGuzergahRaporuAsync(int? firmaId = null, int? yil = null, int? ay = null);
    Task<List<GunlukRapor>> GetGunlukRaporAsync(int yil, int ay, int? firmaId = null);
    Task<HakedisDashboardKpi> GetDashboardKpiAsync(int yil, int ay, int? firmaId = null);
}




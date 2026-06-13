using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services;

public interface IGuzergahService
{
    Task<List<Guzergah>> GetAllAsync();
    Task<List<Guzergah>> GetActiveAsync();
    Task<List<Guzergah>> GetByCariIdAsync(int cariId);
    Task<List<Guzergah>> GetByFirmaIdAsync(int firmaId);
    Task<Guzergah?> GetByIdAsync(int id);
    Task<Guzergah> CreateAsync(Guzergah guzergah);
    Task<Guzergah> AddAsync(Guzergah guzergah);
    Task<Guzergah> UpdateAsync(Guzergah guzergah);
    Task UpdateWithSeferlerAsync(Guzergah guzergah, List<GuzergahSefer> seferler);
    Task DeleteAsync(int id);
    Task<string> GenerateNextKodAsync();
    Task<string> GenerateGuzergahKoduAsync(int firmaId);

    // Doğrulama metodları
    Task<bool> FaturaKalemdenGuzergahVarMiAsync(int faturaKalemId);
    Task<Guzergah?> GetByFaturaKalemIdAsync(int faturaKalemId);
    Task<bool> BenzersizGuzergahMiAsync(int firmaId, string guzergahAdi, int? haricId = null);
}

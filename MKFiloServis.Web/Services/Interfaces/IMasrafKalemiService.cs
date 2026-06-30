using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IMasrafKalemiService
{
    Task<List<MasrafKalemi>> GetAllAsync();
    Task<List<MasrafKalemi>> GetActiveAsync();
    Task<List<MasrafKalemi>> GetByKategoriAsync(MasrafKategori kategori);
    Task<MasrafKalemi?> GetByIdAsync(int id);
    Task<MasrafKalemi> CreateAsync(MasrafKalemi masrafKalemi);
    Task<MasrafKalemi> UpdateAsync(MasrafKalemi masrafKalemi);
    Task DeleteAsync(int id);
    Task<string> GenerateNextKodAsync();
    Task<int> DeleteDuplicatesAsync();
}




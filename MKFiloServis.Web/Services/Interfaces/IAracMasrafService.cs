using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IAracMasrafService
{
    Task<List<AracMasraf>> GetAllAsync();
    Task<List<AracMasraf>> GetByAracIdAsync(int aracId);
    Task<List<AracMasraf>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<AracMasraf>> GetByAracAndDateRangeAsync(int aracId, DateTime startDate, DateTime endDate);
    Task<List<AracMasraf>> GetByKategoriAsync(MasrafKategori kategori, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<AracMasraf>> GetArizaMasraflariAsync();
    Task<AracMasraf?> GetByIdAsync(int id);
    Task<AracMasraf> CreateAsync(AracMasraf aracMasraf, bool muhasebeFisiOlustur = true);
    Task<AracMasraf> UpdateAsync(AracMasraf aracMasraf, bool muhasebeFisiOlustur = true);
    Task DeleteAsync(int id);
    Task<decimal> GetToplamMasrafByAracAsync(int aracId, DateTime? startDate = null, DateTime? endDate = null);
}




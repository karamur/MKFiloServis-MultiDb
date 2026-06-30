using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IServisCalismaService
{
    Task<List<ServisCalisma>> GetAllAsync();
    Task<List<ServisCalisma>> GetRecentAsync(int count = 5);
    Task<List<ServisCalisma>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<ServisCalisma>> GetByAracIdAsync(int aracId, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<ServisCalisma>> GetBySoforIdAsync(int soforId, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<ServisCalisma>> GetByGuzergahIdAsync(int guzergahId, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<ServisCalisma>> GetByCariIdAsync(int cariId, DateTime? startDate = null, DateTime? endDate = null);
    Task<ServisCalisma?> GetByIdAsync(int id);
    Task<ServisCalisma> CreateAsync(ServisCalisma servisCalisma);
    Task<ServisCalisma> UpdateAsync(ServisCalisma servisCalisma);
    Task DeleteAsync(int id);
    Task<List<ServisCalisma>> FilterAsync(
        DateTime startDate, 
        DateTime endDate, 
        int? aracId = null, 
        int? soforId = null, 
        int? guzergahId = null, 
        int? cariId = null);
}




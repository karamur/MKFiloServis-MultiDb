using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IGuzergahSeferService
{
    Task<List<GuzergahSefer>> GetByGuzergahIdAsync(int guzergahId);
    Task<Dictionary<int, List<GuzergahSefer>>> GetAllGroupedAsync();
    Task ReplaceAllAsync(int guzergahId, List<GuzergahSefer> seferler);
}




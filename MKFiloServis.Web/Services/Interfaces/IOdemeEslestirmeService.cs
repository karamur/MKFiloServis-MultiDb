using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IOdemeEslestirmeService
{
    Task<List<OdemeEslestirme>> GetAllAsync();
    Task<List<OdemeEslestirme>> GetByFaturaIdAsync(int faturaId);
    Task<List<OdemeEslestirme>> GetByHareketIdAsync(int hareketId);
    Task<OdemeEslestirme?> GetByIdAsync(int id);
    Task<OdemeEslestirme> CreateAsync(OdemeEslestirme eslestirme);
    Task DeleteAsync(int id);
    Task<decimal> GetFaturaEslestirilenTutarAsync(int faturaId);
    Task<decimal> GetHareketEslestirilenTutarAsync(int hareketId);
}




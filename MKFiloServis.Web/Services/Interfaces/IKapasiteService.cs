using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IKapasiteService
{
    Task<List<Kapasite>> GetAllAsync();
    Task<List<Kapasite>> GetActiveAsync();
    Task<Kapasite?> GetByIdAsync(int id);
    Task<Kapasite> CreateAsync(Kapasite kapasite);
    Task<Kapasite> UpdateAsync(Kapasite kapasite);
    Task DeleteAsync(int id);
}



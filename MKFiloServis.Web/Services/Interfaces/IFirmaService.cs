using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IFirmaService
{
    // Firma CRUD
    Task<List<Firma>> GetAllAsync();
    Task<List<Firma>> GetAktifFirmalarAsync();
    Task<Firma?> GetByIdAsync(int id);
    Task<Firma?> GetVarsayilanFirmaAsync();
    Task<Firma> CreateAsync(Firma firma);
    Task<Firma> UpdateAsync(Firma firma);
    Task DeleteAsync(int id);
    Task SetVarsayilanAsync(int firmaId);

    // Aktif Firma Yonetimi
    AktifFirmaBilgisi GetAktifFirma();
    void SetAktifFirma(int firmaId);
    void SetAktifFirma(AktifFirmaBilgisi firma);
    void SetTumFirmalar(bool tumFirmalar);
    void SetAktifDonem(int yil, int ay);

    // Seed
    Task SeedVarsayilanFirmaAsync();
}




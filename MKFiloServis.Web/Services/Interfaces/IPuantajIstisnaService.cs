using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Puantaj istisna yönetimi CRUD servisi.
/// Onaylı puantajda değişiklik yapılamaz.
/// </summary>
public interface IPuantajIstisnaService
{
    Task<List<PuantajIstisna>> GetByPuantajKayitAsync(int puantajKayitId, CancellationToken ct = default);
    Task<PuantajIstisna?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PuantajIstisna> CreateAsync(PuantajIstisna istisna, CancellationToken ct = default);
    Task<PuantajIstisna> UpdateAsync(PuantajIstisna istisna, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<List<PuantajIstisna>> GetByDonemAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default);
}





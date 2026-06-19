using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services.Interfaces;

/// <summary>
/// PuantajKayit (B1) verisinden fatura hazırlık kayıtları üretme ve yönetme servisi.
/// </summary>
public interface IPuantajFaturaHazirlikService
{
    // ── CRUD ──

    Task<PuantajFaturaHazirlik?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<PuantajFaturaHazirlik>> GetByDonemAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default);
    Task<PuantajFaturaHazirlik> CreateAsync(PuantajFaturaHazirlik hazirlik, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    // ── Satır yönetimi ──

    Task<List<PuantajFaturaHazirlikSatir>> GetSatirlarAsync(int hazirlikId, CancellationToken ct = default);
    Task<PuantajFaturaHazirlikSatir> ManuelSatirEkleAsync(int hazirlikId, PuantajFaturaHazirlikSatir satir, CancellationToken ct = default);
    Task<bool> SatirSilAsync(int satirId, CancellationToken ct = default);
    Task<bool> SatirGuncelleAsync(PuantajFaturaHazirlikSatir satir, CancellationToken ct = default);

    // ── Workflow ──

    Task<PuantajFaturaHazirlik> OnaylaAsync(int id, string kullanici, CancellationToken ct = default);
    Task<PuantajFaturaHazirlik> FaturalastiAsync(int id, int? faturaId = null, CancellationToken ct = default);

    // ── Toplu hesaplama ──

    Task HesaplaToplamlarAsync(int hazirlikId, CancellationToken ct = default);
}

using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services.Interfaces;

/// <summary>
/// Fatura hazırlık raporu için ağaç gruplama şablonu CRUD servisi.
/// Kullanıcı bazlı veya firma geneli şablonları yönetir.
/// </summary>
public interface IFaturaGrupSablonuService
{
    /// <summary>Firmanın tüm şablonlarını (ve opsiyonel kullanıcı filtresiyle) getirir.</summary>
    Task<List<FaturaGrupSablonu>> GetByFirmaAsync(int firmaId, int? kullaniciId = null, CancellationToken ct = default);

    /// <summary>Tek şablon getirir.</summary>
    Task<FaturaGrupSablonu?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Kullanıcının varsayılan şablonunu getirir (yoksa null).</summary>
    Task<FaturaGrupSablonu?> GetVarsayilanAsync(int firmaId, int? kullaniciId = null, CancellationToken ct = default);

    /// <summary>Yeni şablon oluşturur.</summary>
    Task<FaturaGrupSablonu> CreateAsync(FaturaGrupSablonu sablon, CancellationToken ct = default);

    /// <summary>Şablon günceller.</summary>
    Task<FaturaGrupSablonu> UpdateAsync(FaturaGrupSablonu sablon, CancellationToken ct = default);

    /// <summary>Şablonu soft-delete yapar.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Belirtilen şablonu varsayılan yapar (diğerlerinin varsayılanını kaldırır).</summary>
    Task<bool> SetVarsayilanAsync(int id, CancellationToken ct = default);
}

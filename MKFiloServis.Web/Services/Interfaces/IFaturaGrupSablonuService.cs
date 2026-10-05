using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Fatura hazırlık raporu için ağaç gruplama şablonu CRUD servisi.
/// Kullanıcı bazlı veya firma geneli şablonları yönetir.
/// </summary>
public interface IFaturaGrupSablonuService
{
    /// <summary>Seçili firmada oturum kullanıcısının özel ve firma geneli şablonlarını getirir; başka kullanıcı kimliği reddedilir.</summary>
    Task<List<FaturaGrupSablonu>> GetByFirmaAsync(int firmaId, int? kullaniciId = null, CancellationToken ct = default);

    /// <summary>Seçili firmada kullanıcıya erişilebilir tek şablonu getirir.</summary>
    Task<FaturaGrupSablonu?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Kullanıcı belirtilmezse firma geneli, belirtilirse oturum kullanıcısının varsayılanını getirir (yoksa null).</summary>
    Task<FaturaGrupSablonu?> GetVarsayilanAsync(int firmaId, int? kullaniciId = null, CancellationToken ct = default);

    /// <summary>Yeni şablon oluşturur.</summary>
    Task<FaturaGrupSablonu> CreateAsync(FaturaGrupSablonu sablon, CancellationToken ct = default);

    /// <summary>Şablonu günceller; kayıtlı firma ve kullanıcı sahipliğini korur.</summary>
    Task<FaturaGrupSablonu> UpdateAsync(FaturaGrupSablonu sablon, CancellationToken ct = default);

    /// <summary>Şablonu soft-delete yapar.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Belirtilen şablonu varsayılan yapar (diğerlerinin varsayılanını kaldırır).</summary>
    Task<bool> SetVarsayilanAsync(int id, CancellationToken ct = default);
}





using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Özmal / Kiralık araçların aylık maliyet snapshot'unu üreten servis.
/// AracMasraf (yakıt, bakım, diğer), LastikDegisim, ServisKontrat (kiralık plaka) ve
/// FiloGunlukPuantaj kayıtlarından konsolide rapor üretir.
/// Tedarikçi araçları için snapshot üretilmez (maliyet sahibine aittir).
/// </summary>
public interface IAracMaliyetService
{
    /// <summary>Belirli dönem için araç maliyet snapshot'unu üretir/günceller.</summary>
    Task<AracMaliyetSnapshot> SnapshotUretAsync(int aracId, int yil, int ay);

    /// <summary>Bir dönem için tüm özmal+kiralık araçların snapshot'unu üretir.</summary>
    Task<List<AracMaliyetSnapshot>> TumAraclarIcinUretAsync(int yil, int ay);

    Task<List<AracMaliyetSnapshot>> GetSnapshotlarAsync(int? aracId = null, int? yil = null, int? ay = null);

    Task<AracMaliyetSnapshot?> GetByIdAsync(int id);

    Task<bool> SilAsync(int snapshotId);
}





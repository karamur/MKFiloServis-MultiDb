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

    /// <summary>
    /// Fullpet gibi tek fatura / çok araç yakıt dağılımını plakalara kaydeder.
    /// Her araca ayrı AracMasraf (Yakıt) kaydı oluşturur.
    /// </summary>
    /// <param name="faturaTarihi">Fatura tarihi</param>
    /// <param name="faturaNo">Fatura numarası (opsiyonel)</param>
    /// <param name="aciklama">Açıklama (opsiyonel)</param>
    /// <param name="aracIdler">Dağıtım yapılacak araç ID listesi</param>
    /// <param name="toplamTutar">Toplam fatura tutarı</param>
    /// <param name="esitDagit">true → tutarı araç sayısına böl; false → aracTutarlari dict kullan</param>
    /// <param name="aracTutarlari">esitDagit=false ise her araç için bireysel tutar (aracId → tutar)</param>
    /// <param name="firmaId">Firma ID (opsiyonel, null ise aktif firma kullanılır)</param>
    /// <returns>Oluşturulan kayıt sayısı</returns>
    Task<int> FullpetFaturaDagitAsync(
        DateTime faturaTarihi,
        string? faturaNo,
        string? aciklama,
        List<int> aracIdler,
        decimal toplamTutar,
        bool esitDagit,
        Dictionary<int, decimal>? aracTutarlari,
        int? firmaId = null);
}





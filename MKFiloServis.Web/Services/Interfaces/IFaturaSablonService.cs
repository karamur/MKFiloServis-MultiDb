using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Fatura şablon yönetim servisi interface'i
/// </summary>
public interface IFaturaSablonService
{
    // Şablon CRUD işlemleri
    /// <summary>Seçili firma şablonlarını, Tüm firmalar görünümünde mevcut filtre kapsamını salt okunur getirir.</summary>
    Task<List<FaturaSablon>> TumSablonlariGetirAsync();
    /// <summary>Tek seçili firmada erişilebilir/silinmemiş şablonu getirir; tek firma seçimi gerektirir.</summary>
    Task<FaturaSablon?> SablonGetirAsync(int id);
    /// <summary>Tek seçili firmada aktif varsayılanı, yoksa kimlik sırasıyla ilk aktif şablonu getirir.</summary>
    Task<FaturaSablon?> VarsayilanSablonGetirAsync();
    Task<FaturaSablon> SablonEkleAsync(FaturaSablon sablon);
    Task<bool> SablonGuncelleAsync(FaturaSablon sablon);
    Task<bool> SablonSilAsync(int id);
    Task<bool> VarsayilanYapAsync(int id);
    Task<FaturaSablon> SablonKopyalaAsync(int id, string yeniAd);

    // PDF oluşturma işlemleri
    /// <summary>Tek seçili firmadaki kayıtlı fatura ve aktif şablonla PDF üretir; açık şablon seçimi bulunamazsa reddeder.</summary>
    Task<FaturaPdfResult> FaturaPdfOlusturAsync(int faturaId, int? sablonId = null);
    /// <summary>Yalnız kayıt kimliklerini kullanır; fatura ve şablon içeriğini DB'den yeniden yükler. Kaydedilmemiş tasarım için önizleme kullanılır.</summary>
    Task<FaturaPdfResult> FaturaPdfOlusturAsync(Fatura fatura, FaturaSablon? sablon = null);
    /// <summary>Tek seçili firmada düzenlenen şablon ayarlarıyla örnek PDF üretir; kayıtlı şablonun firma kapsamını doğrular.</summary>
    Task<byte[]> OnizlemePdfOlusturAsync(FaturaSablon sablon);

    // Email gönderimi
    Task<bool> FaturaEmailGonderAsync(FaturaYazdirRequest request);
    Task<bool> TopluFaturaEmailGonderAsync(List<int> faturaIds, int? sablonId = null, string? emailKonu = null, string? emailMesaj = null);

    // Logo/kaşe yazımları tek seçili firmaya ait silinmemiş şablonlarda çalışır.
    Task<bool> LogoYukleAsync(int sablonId, byte[] logoData, string dosyaAdi);
    Task<bool> LogoSilAsync(int sablonId);
    Task<bool> KaseYukleAsync(int sablonId, byte[] kaseData, string dosyaAdi);
    Task<bool> KaseSilAsync(int sablonId);
}




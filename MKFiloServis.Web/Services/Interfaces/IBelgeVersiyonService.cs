using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// EBYS Belge Versiyon Yönetim Servisi Interface
/// </summary>
public interface IBelgeVersiyonService
{
    // EBYS Evrak Dosya Versiyonları
    Task<List<EbysEvrakDosyaVersiyon>> GetEbysEvrakDosyaVersiyonlariAsync(int evrakDosyaId);
    Task<EbysEvrakDosyaVersiyon?> GetEbysEvrakDosyaVersiyonAsync(int versiyonId);
    Task ArsivleEbysEvrakDosyaAsync(int evrakDosyaId, string? degisiklikNotu = null, int? kullaniciId = null);
    Task<byte[]?> GetEbysEvrakVersiyonIcerikAsync(int versiyonId);
    Task SilEbysEvrakVersiyonAsync(int versiyonId);
    
    // Araç Evrak Dosya Versiyonları
    Task<List<AracEvrakDosyaVersiyon>> GetAracEvrakDosyaVersiyonlariAsync(int aracEvrakDosyaId);
    Task<AracEvrakDosyaVersiyon?> GetAracEvrakDosyaVersiyonAsync(int versiyonId);
    Task ArsivleAracEvrakDosyaAsync(int aracEvrakDosyaId, string? degisiklikNotu = null, int? kullaniciId = null);
    Task<byte[]?> GetAracEvrakVersiyonIcerikAsync(int versiyonId);
    Task SilAracEvrakVersiyonAsync(int versiyonId);
    
    // Personel Özlük Evrak Versiyonları
    Task<List<PersonelOzlukEvrakVersiyon>> GetPersonelOzlukEvrakVersiyonlariAsync(int personelOzlukEvrakId);
    Task<PersonelOzlukEvrakVersiyon?> GetPersonelOzlukEvrakVersiyonAsync(int versiyonId);
    Task ArsivlePersonelOzlukEvrakAsync(int personelOzlukEvrakId, string? degisiklikNotu = null, int? kullaniciId = null);
    Task<byte[]?> GetPersonelOzlukEvrakVersiyonIcerikAsync(int versiyonId);
    Task SilPersonelOzlukEvrakVersiyonAsync(int versiyonId);
    
    // Versiyon Karşılaştırma
    Task<BelgeVersiyonKarsilastirma?> KarsilastirEbysVersiyonlarAsync(int versiyon1Id, int versiyon2Id);
    Task<BelgeVersiyonKarsilastirma?> KarsilastirAracVersiyonlarAsync(int versiyon1Id, int versiyon2Id);
    
    // Geri Yükleme
    Task GeriYukleEbysVersiyonAsync(int versiyonId, string? geriYuklemeNotu = null, int? kullaniciId = null);
    Task GeriYukleAracVersiyonAsync(int versiyonId, string? geriYuklemeNotu = null, int? kullaniciId = null);
    Task GeriYuklePersonelOzlukVersiyonAsync(int versiyonId, string? geriYuklemeNotu = null, int? kullaniciId = null);
}

/// <summary>
/// Versiyon özet bilgisi DTO
/// </summary>
public class VersiyonOzet
{
    public int VersiyonId { get; set; }
    public int VersiyonNo { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public long DosyaBoyutu { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public string? DegisiklikNotu { get; set; }
    public string? OlusturanKullanici { get; set; }
    public bool MevcutVersiyon { get; set; }
}




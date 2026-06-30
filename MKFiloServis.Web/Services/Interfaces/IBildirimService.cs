using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Bildirim servisi interface - vade/belge süresi uyarıları ve kullanıcı bildirimleri
/// </summary>
public interface IBildirimService
{
    // Bildirim CRUD
    Task<List<Bildirim>> GetKullaniciBildirimlerAsync(int kullaniciId, bool sadeceoOkunmamis = false);
    Task<int> GetOkunmamisBildirimSayisiAsync(int kullaniciId);
    Task<Bildirim?> GetByIdAsync(int id);
    Task<Bildirim> CreateAsync(Bildirim bildirim);
    Task OkunduOlarakIsaretle(int bildirimId);
    Task TumunuOkunduYapAsync(int kullaniciId);
    Task DeleteAsync(int id);
    
    // Bildirim Ayarları
    Task<BildirimAyar?> GetKullaniciAyarAsync(int kullaniciId);
    Task<BildirimAyar> SaveAyarAsync(BildirimAyar ayar);
    
    // Otomatik Bildirim Oluşturma (Background Service tarafından çağrılır)
    Task<List<BildirimOzet>> TaraVeBildirimOlusturAsync();
    Task<List<BildirimOzet>> VadeYaklasanFaturalariTaraAsync(int gunSayisi = 7);
    Task<List<BildirimOzet>> SuresiDolanBelgeleriTaraAsync(int gunSayisi = 30);
    
    // Dashboard için özet
    Task<BildirimDashboardDto> GetDashboardOzetAsync(int? kullaniciId = null);

    // E-posta Bildirimleri
    Task<int> EpostaBildirimGonderAsync();
    Task<bool> TestEpostaGonderAsync(int kullaniciId);
}

/// <summary>
/// Bildirim özet DTO - tarama sonuçları için
/// </summary>
public class BildirimOzet
{
    public BildirimTipi Tip { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string? IliskiliTablo { get; set; }
    public int? IliskiliKayitId { get; set; }
    public string? Link { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public int KalanGun { get; set; }
    public BildirimOncelik Oncelik { get; set; }
}

/// <summary>
/// Bildirim Dashboard DTO
/// </summary>
public class BildirimDashboardDto
{
    public int ToplamBildirim { get; set; }
    public int OkunmamisBildirim { get; set; }
    public int KritikBildirim { get; set; }
    public int VadeYaklasanFatura { get; set; }
    public int SuresiDolanBelge { get; set; }
    
    // Son 10 bildirim
    public List<Bildirim> SonBildirimler { get; set; } = new();
    
    // Kategorilere göre özet
    public Dictionary<BildirimTipi, int> KategoriBazliSayilar { get; set; } = new();
    
    // Yaklaşan olaylar
    public List<BildirimOzet> YaklasanOlaylar { get; set; } = new();
}





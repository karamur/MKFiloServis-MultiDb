using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Cari hatırlatma servisi arayüzü
/// </summary>
public interface ICariHatirlatmaService
{
    // Ayarlar
    Task<CariHatirlatmaSettings> GetAyarlarAsync(int? firmaId = null);
    Task SaveAyarlarAsync(CariHatirlatmaSettings ayarlar, int? firmaId = null);
    
    // Manuel Kontrol
    Task<CariHatirlatmaRapor> HatirlatmaKontroluYapAsync(int? firmaId = null, bool emailGonder = true, bool bildirimOlustur = true);
    
    // Vade Kontrolleri
    Task<List<CariHatirlatmaDetay>> VadeYaklasanFaturalariGetirAsync(int? firmaId = null);
    Task<List<CariHatirlatmaDetay>> VadeGecmisFaturalariGetirAsync(int? firmaId = null);
    
    // Borç/Alacak Kontrolleri
    Task<List<CariHatirlatmaDetay>> BorcEsikAsilanCarileriGetirAsync(int? firmaId = null);
    Task<List<CariHatirlatmaDetay>> AlacakEsikAsilanCarileriGetirAsync(int? firmaId = null);
    
    // Hareketsiz Cari Kontrolü
    Task<List<CariHatirlatmaDetay>> HareketsizCarileriGetirAsync(int? firmaId = null);
    
    // Hatırlatma Geçmişi
    Task<List<CariHatirlatma>> GetHatirlatmaGecmisiAsync(int? cariId = null, int? firmaId = null, int sonKacGun = 30);
    
    // Tek Cari Hatırlatma
    Task<bool> TekCariHatirlatmaGonderAsync(int cariId, string mesaj, bool email = true, bool bildirim = true);
    
    // E-posta İşlemleri
    Task<bool> VadeHatirlatmaEmailiGonderAsync(CariHatirlatmaDetay detay);
    Task<bool> TopluHatirlatmaEmailiGonderAsync(CariHatirlatmaRapor rapor, List<string> alicilar);
    
    // Özet İstatistikler
    Task<CariHatirlatmaOzet> GetHatirlatmaOzetiAsync(int? firmaId = null);
}

/// <summary>
/// Cari hatırlatma özet istatistikleri
/// </summary>
public class CariHatirlatmaOzet
{
    public int VadeYaklasanFaturaSayisi { get; set; }
    public decimal VadeYaklasanTutar { get; set; }
    public int VadeGecmisFaturaSayisi { get; set; }
    public decimal VadeGecmisTutar { get; set; }
    public int BorcEsikAsilanCariSayisi { get; set; }
    public int AlacakEsikAsilanCariSayisi { get; set; }
    public int HareketsizCariSayisi { get; set; }
    public int BugunGonderilenHatirlatmaSayisi { get; set; }
    public int BuHaftaGonderilenHatirlatmaSayisi { get; set; }
    public DateTime? SonKontrolTarihi { get; set; }
}





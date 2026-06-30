using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Luca Portal entegrasyon ayarlari
/// E-Fatura ve E-Arsiv belgelerine erisim icin kullanilir
/// </summary>
public class LucaPortalSettings
{
    public int Id { get; set; }
    
    // Portal Giris Bilgileri
    public string KullaniciAdi { get; set; } = string.Empty;
    public string Sifre { get; set; } = string.Empty;
    public string PortalUrl { get; set; } = "https://edonusum.lfrms.com.tr";
    
    // Token Bilgileri
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenGecerlilikTarihi { get; set; }
    
    // Ayarlar
    public bool OtomatikSenkron { get; set; } = false;
    public int SenkronAralikSaat { get; set; } = 24; // Kac saatte bir senkron yapilsin
    public DateTime? SonSenkronTarihi { get; set; }
    
    // Firma Bilgisi
    public int? FirmaId { get; set; }
    public string? LucaFirmaKodu { get; set; }
    
    // Kayit Bilgileri
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public DateTime? GuncellemeTarihi { get; set; }

    [NotMapped]
    public bool TokenGecerliMi => !string.IsNullOrEmpty(AccessToken) && 
                                   TokenGecerlilikTarihi.HasValue && 
                                   TokenGecerlilikTarihi > DateTime.UtcNow;
}

/// <summary>
/// Luca Portal'dan cekilen belge bilgisi
/// </summary>
public class LucaBelge
{
    public string BelgeId { get; set; } = string.Empty;
    public string EttnNo { get; set; } = string.Empty;
    public string FaturaNo { get; set; } = string.Empty;
    public DateTime BelgeTarihi { get; set; }
    public LucaBelgeTipi BelgeTipi { get; set; }
    public LucaBelgeYonu BelgeYonu { get; set; }
    
    // Taraf Bilgileri
    public string GondericiVkn { get; set; } = string.Empty;
    public string GondericiUnvan { get; set; } = string.Empty;
    public string AliciVkn { get; set; } = string.Empty;
    public string AliciUnvan { get; set; } = string.Empty;
    
    // Tutarlar
    public decimal AraToplam { get; set; }
    public decimal KdvToplam { get; set; }
    public decimal GenelToplam { get; set; }
    
    // Durum
    public string Durum { get; set; } = string.Empty;
    public string? DurumAciklama { get; set; }
    
    // Dosya Bilgileri
    public bool XmlMevcut { get; set; }
    public bool PdfMevcut { get; set; }
    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }
}

public enum LucaBelgeTipi
{
    EFatura = 1,
    EArsiv = 2,
    EMustahsil = 3,
    EIrsaliye = 4
}

public enum LucaBelgeYonu
{
    Gelen = 1,  // Alinan faturalar
    Giden = 2   // Kesilen faturalar
}

/// <summary>
/// Luca Portal sorgu filtresi
/// </summary>
public class LucaSorguFiltre
{
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime BitisTarihi { get; set; } = DateTime.Today;
    public LucaBelgeTipi? BelgeTipi { get; set; }
    public LucaBelgeYonu? BelgeYonu { get; set; }
    public string? VknArama { get; set; }
    public string? FaturaNoArama { get; set; }
    public int Sayfa { get; set; } = 1;
    public int SayfaBoyutu { get; set; } = 50;
}

/// <summary>
/// Luca Portal sorgu sonucu
/// </summary>
public class LucaSorguSonuc
{
    public List<LucaBelge> Belgeler { get; set; } = new();
    public int ToplamKayit { get; set; }
    public int Sayfa { get; set; }
    public int SayfaBoyutu { get; set; }
    public int ToplamSayfa => (int)Math.Ceiling((double)ToplamKayit / SayfaBoyutu);
}

/// <summary>
/// Luca Portal login sonucu
/// </summary>
public class LucaLoginSonuc
{
    public bool Basarili { get; set; }
    public string? HataMesaji { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenGecerlilikTarihi { get; set; }
    public string? FirmaKodu { get; set; }
    public string? FirmaUnvan { get; set; }
}

/// <summary>
/// Belge indirme sonucu
/// </summary>
public class LucaBelgeIndirmeSonuc
{
    public bool Basarili { get; set; }
    public string? HataMesaji { get; set; }
    public byte[]? Icerik { get; set; }
    public string? DosyaAdi { get; set; }
    public string? ContentType { get; set; }
}



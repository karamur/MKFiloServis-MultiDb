using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Models;

/// <summary>
/// Cari risk analizi özet modeli
/// </summary>
public class CariRiskOzet
{
    public int ToplamCariSayisi { get; set; }
    public int RiskliCariSayisi { get; set; }
    public int VadesiGecmisCariSayisi { get; set; }
    public decimal ToplamVadesiGecmisBorc { get; set; }
    public decimal OrtalamaTahsilatSuresi { get; set; } // gün cinsinden
    public decimal ToplamAcikAlacak { get; set; }
    public decimal ToplamAcikBorc { get; set; }
}

/// <summary>
/// Cari risk kartı modeli
/// </summary>
public class CariRiskKarti
{
    public int CariId { get; set; }
    public string CariKodu { get; set; } = string.Empty;
    public string CariUnvan { get; set; } = string.Empty;
    public CariTipi CariTipi { get; set; }
    
    // Risk Skoru (0-100)
    public int RiskSkoru { get; set; }
    public RiskSeviyesi RiskSeviyesi { get; set; }
    
    // Bakiye Bilgileri
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal Bakiye { get; set; }
    
    // Vade Bilgileri
    public decimal VadesiGecmisBorc { get; set; }
    public int VadesiGecmisGunSayisi { get; set; } // En eski vadesi geçmiş fatura
    public int VadesiGecmisFaturaSayisi { get; set; }
    
    // Yaşlandırma (gün bazlı vadesi geçmiş)
    public decimal Vade0_30 { get; set; }
    public decimal Vade31_60 { get; set; }
    public decimal Vade61_90 { get; set; }
    public decimal Vade90Plus { get; set; }
    
    // Ödeme Geçmişi
    public int SonBirYilFaturaSayisi { get; set; }
    public decimal SonBirYilToplamCiro { get; set; }
    public decimal OrtalamaTahsilatSuresi { get; set; } // gün
    public decimal OdemeVadeUyumOrani { get; set; } // % cinsinden
    
    // Son İşlem
    public DateTime? SonFaturaTarihi { get; set; }
    public DateTime? SonOdemeTarihi { get; set; }
    public DateTime? SonIletisimTarihi { get; set; }
    
    // Risk Açıklamaları
    public List<string> RiskFaktorleri { get; set; } = new();
    public string AIYorumu { get; set; } = string.Empty;
}

/// <summary>
/// Risk seviyesi enum
/// </summary>
public enum RiskSeviyesi
{
    DusukRisk = 1,    // Yeşil - 0-25 puan
    OrtaRisk = 2,     // Sarı - 26-50 puan
    YuksekRisk = 3,   // Turuncu - 51-75 puan
    KritikRisk = 4    // Kırmızı - 76-100 puan
}

/// <summary>
/// Vadesi geçmiş fatura detayı
/// </summary>
public class VadesiGecmisFatura
{
    public int FaturaId { get; set; }
    public string FaturaNo { get; set; } = string.Empty;
    public DateTime FaturaTarihi { get; set; }
    public DateTime VadeTarihi { get; set; }
    public decimal Tutar { get; set; }
    public decimal KalanTutar { get; set; }
    public int GecikmeGunu { get; set; }
    public int CariId { get; set; }
    public string CariUnvan { get; set; } = string.Empty;
}

/// <summary>
/// Risk trend analizi
/// </summary>
public class RiskTrendItem
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string Donem { get; set; } = string.Empty;
    public int RiskliCariSayisi { get; set; }
    public decimal VadesiGecmisToplamBorc { get; set; }
    public decimal OrtalamaTahsilatSuresi { get; set; }
}

/// <summary>
/// Risk filtre parametreleri
/// </summary>
public class CariRiskFilterParams
{
    public RiskSeviyesi? RiskSeviyesi { get; set; }
    public CariTipi? CariTipi { get; set; }
    public int? MinGecikmeGunu { get; set; }
    public decimal? MinBorcTutari { get; set; }
    public string? SearchTerm { get; set; }
    public string SortBy { get; set; } = "RiskSkoru"; // RiskSkoru, VadesiGecmisBorc, GecikmeGunu
    public bool SortDescending { get; set; } = true;
}




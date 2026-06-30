namespace MKFiloServis.Web.Models;

/// <summary>
/// Cari bakiye yaşlandırma rapor özeti
/// </summary>
public class CariYaslandirmaRapor
{
    public List<CariYaslandirmaOzet> Cariler { get; set; } = [];
    
    // Toplam özet
    public decimal ToplamBakiye { get; set; }
    public decimal Guncel { get; set; }        // 0-30 gün
    public decimal Vadesi30_60 { get; set; }   // 31-60 gün
    public decimal Vadesi60_90 { get; set; }   // 61-90 gün
    public decimal Vadesi90Plus { get; set; }  // 90+ gün
    
    // Cari sayıları
    public int ToplamCariSayisi { get; set; }
    public int BorcluCariSayisi { get; set; }
    public int AlacakliCariSayisi { get; set; }
    
    // Yaşlandırma bantları özeti
    public List<YaslandirmaBandi> YaslandirmaBantlari { get; set; } = [];
    
    // Cari tipi bazlı dağılım
    public List<CariTipiDagilimi> CariTipiDagilimi { get; set; } = [];
}

/// <summary>
/// Cari bazlı yaşlandırma özeti
/// </summary>
public class CariYaslandirmaOzet
{
    public int CariId { get; set; }
    public string CariKodu { get; set; } = string.Empty;
    public string Unvan { get; set; } = string.Empty;
    public string CariTipi { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    
    // Bakiye bilgileri
    public decimal ToplamBakiye { get; set; }
    public decimal Guncel { get; set; }        // 0-30 gün (vadesi gelmemiş + 30 gün içinde vadesi geçmiş)
    public decimal Vadesi30_60 { get; set; }   // 31-60 gün
    public decimal Vadesi60_90 { get; set; }   // 61-90 gün
    public decimal Vadesi90Plus { get; set; }  // 90+ gün vadesi geçmiş
    
    // Fatura sayıları
    public int ToplamFaturaSayisi { get; set; }
    public int VadesiGecmisFaturaSayisi { get; set; }
    
    // Risk seviyesi
    public string RiskSeviyesi => VadesiGecmisOran switch
    {
        >= 50 => "Yüksek",
        >= 25 => "Orta",
        > 0 => "Düşük",
        _ => "Normal"
    };
    
    public string RiskRengi => RiskSeviyesi switch
    {
        "Yüksek" => "danger",
        "Orta" => "warning",
        "Düşük" => "info",
        _ => "success"
    };
    
    // Vadesi geçmiş bakiye oranı
    public decimal VadesiGecmisBakiye => Vadesi30_60 + Vadesi60_90 + Vadesi90Plus;
    public decimal VadesiGecmisOran => ToplamBakiye != 0 
        ? Math.Abs(VadesiGecmisBakiye / ToplamBakiye) * 100 
        : 0;
    
    // Son işlem bilgisi
    public DateTime? SonFaturaTarihi { get; set; }
    public DateTime? SonOdemeTarihi { get; set; }
    
    // Fatura detayları
    public List<YaslandirmaFaturaDetay> FaturaDetaylari { get; set; } = [];
}

/// <summary>
/// Yaşlandırma bandı özeti
/// </summary>
public class YaslandirmaBandi
{
    public string BantAdi { get; set; } = string.Empty; // "0-30 Gün", "31-60 Gün", vb.
    public int MinGun { get; set; }
    public int MaxGun { get; set; }
    public decimal Tutar { get; set; }
    public int FaturaSayisi { get; set; }
    public int CariSayisi { get; set; }
    public decimal Oran { get; set; } // Toplam içindeki yüzde
    public string Renk { get; set; } = "success"; // Bootstrap renk sınıfı
}

/// <summary>
/// Cari tipi bazlı dağılım
/// </summary>
public class CariTipiDagilimi
{
    public string CariTipi { get; set; } = string.Empty;
    public int CariSayisi { get; set; }
    public decimal ToplamBakiye { get; set; }
    public decimal VadesiGecmisBakiye { get; set; }
    public decimal Oran { get; set; }
}

/// <summary>
/// Yaşlandırma fatura detayı
/// </summary>
public class YaslandirmaFaturaDetay
{
    public int FaturaId { get; set; }
    public string FaturaNo { get; set; } = string.Empty;
    public DateTime FaturaTarihi { get; set; }
    public DateTime? VadeTarihi { get; set; }
    public decimal GenelToplam { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal KalanTutar { get; set; }
    
    // Yaşlandırma bilgileri
    public int VadeGunSayisi { get; set; } // Bugünden vade tarihine kaç gün (negatif = vadesi geçmiş)
    public int GecikmeGunSayisi => VadeGunSayisi < 0 ? Math.Abs(VadeGunSayisi) : 0;
    public bool VadesiGecmisMi => VadeGunSayisi < 0;
    
    public string YaslandirmaBandi => GecikmeGunSayisi switch
    {
        0 => "Güncel",
        <= 30 => "0-30 Gün",
        <= 60 => "31-60 Gün",
        <= 90 => "61-90 Gün",
        _ => "90+ Gün"
    };
    
    public string BantRengi => GecikmeGunSayisi switch
    {
        0 => "success",
        <= 30 => "info",
        <= 60 => "warning",
        <= 90 => "orange",
        _ => "danger"
    };
}




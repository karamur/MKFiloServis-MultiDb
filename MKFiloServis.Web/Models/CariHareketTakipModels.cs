namespace MKFiloServis.Web.Models;

/// <summary>
/// Cari borç/alacak detaylı takip ana modeli
/// </summary>
public class CariHareketTakipRapor
{
    public int CariId { get; set; }
    public string CariKodu { get; set; } = string.Empty;
    public string Unvan { get; set; } = string.Empty;
    public string CariTipi { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? YetkiliKisi { get; set; }
    
    // Bakiye özeti
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal NetBakiye => ToplamBorc - ToplamAlacak;
    public string BakiyeDurumu => NetBakiye > 0 ? "Borçlu" : NetBakiye < 0 ? "Alacaklı" : "Dengeli";
    public string BakiyeRengi => NetBakiye > 0 ? "danger" : NetBakiye < 0 ? "success" : "secondary";
    
    // Vade analizi
    public decimal VadesiGelmemisBakiye { get; set; }
    public decimal VadesiGecmisBakiye { get; set; }
    public int VadesiGecmisGunOrtalama { get; set; }
    
    // Risk skoru (0-100)
    public int RiskSkoru { get; set; }
    public string RiskSeviyesi => RiskSkoru switch
    {
        >= 80 => "Çok Yüksek",
        >= 60 => "Yüksek",
        >= 40 => "Orta",
        >= 20 => "Düşük",
        _ => "Çok Düşük"
    };
    public string RiskRengi => RiskSkoru switch
    {
        >= 80 => "danger",
        >= 60 => "warning",
        >= 40 => "info",
        _ => "success"
    };
    
    // İstatistikler
    public int ToplamFaturaSayisi { get; set; }
    public int AcikFaturaSayisi { get; set; }
    public int VadesiGecmisFaturaSayisi { get; set; }
    public int ToplamOdemeSayisi { get; set; }
    
    // Ortalama ödeme süresi (gün)
    public int OrtalamaOdemeSuresi { get; set; }
    
    // Son işlemler
    public DateTime? SonFaturaTarihi { get; set; }
    public DateTime? SonOdemeTarihi { get; set; }
    public decimal SonOdemeTutari { get; set; }
    
    // Aylık trend
    public List<CariAylikTrend> AylikTrendler { get; set; } = [];
    
    // Hareket listesi
    public List<CariHareketDetay> Hareketler { get; set; } = [];
    
    // Açık faturalar
    public List<CariAcikFatura> AcikFaturalar { get; set; } = [];
    
    // Tahsilat planı
    public List<TahsilatPlanItem> TahsilatPlani { get; set; } = [];
}

/// <summary>
/// Cari hareket detayı (fatura + ödeme birleşik)
/// </summary>
public class CariHareketDetay
{
    public int Id { get; set; }
    public string? DetayUrl { get; set; }
    public DateTime Tarih { get; set; }
    public string HareketTipi { get; set; } = string.Empty; // "Fatura", "Tahsilat", "Ödeme", "Açılış"
    public string Aciklama { get; set; } = string.Empty;
    public string? BelgeNo { get; set; }
    public DateTime? VadeTarihi { get; set; }
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye { get; set; } // Kümülatif bakiye
    
    public string HareketRengi => HareketTipi switch
    {
        "Fatura" => "primary",
        "Tahsilat" => "success",
        "Ödeme" => "info",
        "Araç Masrafı" => "warning",
        "Açılış" => "secondary",
        _ => "dark"
    };
    
    public string HareketIkon => HareketTipi switch
    {
        "Fatura" => "bi-file-text",
        "Tahsilat" => "bi-cash-coin",
        "Ödeme" => "bi-credit-card",
        "Araç Masrafı" => "bi-fuel-pump",
        "Açılış" => "bi-box-arrow-in-right",
        _ => "bi-circle"
    };
}

/// <summary>
/// Cari açık fatura detayı
/// </summary>
public class CariAcikFatura
{
    public int FaturaId { get; set; }
    public string FaturaNo { get; set; } = string.Empty;
    public DateTime FaturaTarihi { get; set; }
    public DateTime? VadeTarihi { get; set; }
    public decimal FaturaTutari { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal KalanTutar => FaturaTutari - OdenenTutar;
    
    // Vade durumu
    public int KalanGun { get; set; } // Negatif = vadesi geçmiş
    public bool VadesiGecmisMi => KalanGun < 0;
    public int GecikmeGunu => VadesiGecmisMi ? Math.Abs(KalanGun) : 0;
    
    public string VadeDurumu => KalanGun switch
    {
        < 0 => $"{Math.Abs(KalanGun)} gün gecikmiş",
        0 => "Bugün vadeli",
        <= 7 => $"{KalanGun} gün kaldı",
        _ => $"{KalanGun} gün"
    };
    
    public string VadeRengi => KalanGun switch
    {
        < -30 => "danger",
        < 0 => "warning",
        0 => "info",
        <= 7 => "primary",
        _ => "success"
    };
    
    // Öncelik (tahsilat için)
    public int Oncelik => GecikmeGunu switch
    {
        > 90 => 1,
        > 60 => 2,
        > 30 => 3,
        > 0 => 4,
        _ => 5
    };
}

/// <summary>
/// Cari aylık trend verisi
/// </summary>
public class CariAylikTrend
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string AyAdi => new DateTime(Yil, Ay, 1).ToString("MMM yy");
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal NetHareket => ToplamBorc - ToplamAlacak;
    public decimal AySonuBakiye { get; set; }
    public int FaturaSayisi { get; set; }
    public int OdemeSayisi { get; set; }
}

/// <summary>
/// Tahsilat planı öğesi
/// </summary>
public class TahsilatPlanItem
{
    public DateTime PlanTarihi { get; set; }
    public decimal PlanTutar { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public int FaturaId { get; set; }
    public string FaturaNo { get; set; } = string.Empty;
    public int OncelikSirasi { get; set; }
    public bool Onaylandı { get; set; }
    
    public string OncelikRengi => OncelikSirasi switch
    {
        1 => "danger",
        2 => "warning",
        3 => "info",
        _ => "secondary"
    };
}

/// <summary>
/// Tüm cariler için özet borç/alacak raporu
/// </summary>
public class CariBorcAlacakOzet
{
    public List<CariHareketTakipOzet> Cariler { get; set; } = [];
    
    // Genel toplamlar
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal NetBakiye => ToplamBorc - ToplamAlacak;
    
    // Vade analizi
    public decimal VadesiGelmemis { get; set; }
    public decimal Vadesi0_30 { get; set; }
    public decimal Vadesi31_60 { get; set; }
    public decimal Vadesi61_90 { get; set; }
    public decimal Vadesi90Plus { get; set; }
    
    // İstatistikler
    public int ToplamCariSayisi { get; set; }
    public int BorcluCariSayisi { get; set; }
    public int AlacakliCariSayisi { get; set; }
    public int RiskliCariSayisi { get; set; } // Risk skoru >= 60
    
    // Cari tipi dağılımı
    public List<CariTipiBakiyeDagilimi> TipiDagilimi { get; set; } = [];
    
    // Aylık trend
    public List<GenelAylikTrend> AylikTrendler { get; set; } = [];
}

/// <summary>
/// Cari borç/alacak özet satırı
/// </summary>
public class CariHareketTakipOzet
{
    public int CariId { get; set; }
    public string CariKodu { get; set; } = string.Empty;
    public string Unvan { get; set; } = string.Empty;
    public string CariTipi { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal NetBakiye => ToplamBorc - ToplamAlacak;
    
    public decimal VadesiGecmisBakiye { get; set; }
    public int VadesiGecmisFaturaSayisi { get; set; }
    
    public int RiskSkoru { get; set; }
    public string RiskSeviyesi => RiskSkoru switch
    {
        >= 80 => "Çok Yüksek",
        >= 60 => "Yüksek",
        >= 40 => "Orta",
        >= 20 => "Düşük",
        _ => "Çok Düşük"
    };
    public string RiskRengi => RiskSkoru switch
    {
        >= 80 => "danger",
        >= 60 => "warning",
        >= 40 => "info",
        _ => "success"
    };
    
    public DateTime? SonIslemTarihi { get; set; }
    public int OrtalamaOdemeSuresi { get; set; }
}

/// <summary>
/// Cari tipi bazlı bakiye dağılımı
/// </summary>
public class CariTipiBakiyeDagilimi
{
    public string CariTipi { get; set; } = string.Empty;
    public int CariSayisi { get; set; }
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal NetBakiye => ToplamBorc - ToplamAlacak;
    public decimal VadesiGecmis { get; set; }
    public decimal Oran { get; set; } // Toplam içindeki yüzde
}

/// <summary>
/// Genel aylık trend verisi
/// </summary>
public class GenelAylikTrend
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string AyAdi => new DateTime(Yil, Ay, 1).ToString("MMM yy");
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal NetHareket => ToplamBorc - ToplamAlacak;
    public decimal AySonuToplamBakiye { get; set; }
    public int FaturaSayisi { get; set; }
    public int OdemeSayisi { get; set; }
    public decimal TahsilatOrani => ToplamBorc != 0 ? (ToplamAlacak / ToplamBorc) * 100 : 0;
}

/// <summary>
/// Cari vade uyarısı
/// </summary>
public class CariVadeUyari
{
    public int CariId { get; set; }
    public string CariKodu { get; set; } = string.Empty;
    public string CariUnvan { get; set; } = string.Empty;
    public int FaturaId { get; set; }
    public string FaturaNo { get; set; } = string.Empty;
    public DateTime FaturaTarihi { get; set; }
    public DateTime? VadeTarihi { get; set; }
    public decimal FaturaTutari { get; set; }
    public decimal KalanTutar { get; set; }
    public int KalanGun { get; set; }
    public VadeUyariSeviye Seviye { get; set; }

    public string SeviyeRengi => Seviye switch
    {
        VadeUyariSeviye.VadesiGecmisKritik => "danger",
        VadeUyariSeviye.VadesiGecmis => "warning",
        VadeUyariSeviye.BugunVadeli => "info",
        VadeUyariSeviye.YaklasanVade => "primary",
        _ => "secondary"
    };

    public string SeviyeIkon => Seviye switch
    {
        VadeUyariSeviye.VadesiGecmisKritik => "bi-exclamation-triangle-fill",
        VadeUyariSeviye.VadesiGecmis => "bi-exclamation-circle-fill",
        VadeUyariSeviye.BugunVadeli => "bi-clock-fill",
        VadeUyariSeviye.YaklasanVade => "bi-bell-fill",
        _ => "bi-info-circle"
    };

    public string SeviyeMetin => Seviye switch
    {
        VadeUyariSeviye.VadesiGecmisKritik => $"{Math.Abs(KalanGun)} gün gecikmiş (KRİTİK)",
        VadeUyariSeviye.VadesiGecmis => $"{Math.Abs(KalanGun)} gün gecikmiş",
        VadeUyariSeviye.BugunVadeli => "Bugün vadeli",
        VadeUyariSeviye.YaklasanVade => $"{KalanGun} gün kaldı",
        _ => ""
    };
}

public enum VadeUyariSeviye
{
    YaklasanVade = 0,
    BugunVadeli = 1,
    VadesiGecmis = 2,
    VadesiGecmisKritik = 3
}




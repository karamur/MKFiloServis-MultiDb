namespace MKFiloServis.Web.Models;

/// <summary>
/// Muhasebeleştirilmemiş fatura özet bilgileri
/// </summary>
public class MuhasebeFaturaOzet
{
    public int FaturaId { get; set; }
    public string FaturaNo { get; set; } = string.Empty;
    public DateTime FaturaTarihi { get; set; }
    public string CariUnvan { get; set; } = string.Empty;
    public string FaturaYonu { get; set; } = string.Empty; // Giden/Gelen
    public string FaturaTipi { get; set; } = string.Empty;
    public decimal AraToplam { get; set; }
    public decimal KdvTutar { get; set; }
    public decimal GenelToplam { get; set; }
    public bool TevkifatliMi { get; set; }
    public decimal TevkifatTutar { get; set; }
    public bool Secildi { get; set; }
}

/// <summary>
/// Muhasebeleştirilmemiş masraf özet bilgileri
/// </summary>
public class MuhasebeMasrafOzet
{
    public int MasrafId { get; set; }
    public DateTime MasrafTarihi { get; set; }
    public string AracPlaka { get; set; } = string.Empty;
    public string MasrafKalemi { get; set; } = string.Empty;
    public string MasrafKategori { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string? BelgeNo { get; set; }
    public string? CariUnvan { get; set; }
    public string? SoforAd { get; set; }
    public string? Aciklama { get; set; }
    public bool Secildi { get; set; }
}

/// <summary>
/// Toplu muhasebeleştirme sonucu
/// </summary>
public class MuhasbelestirmeSonuc
{
    public int BasariliSayisi { get; set; }
    public int HataliSayisi { get; set; }
    public List<string> Hatalar { get; set; } = new();
    public List<int> OlusturulanFisIdleri { get; set; } = new();
}

/// <summary>
/// Muhasebeleştirme durumu özet
/// </summary>
public class MuhasbelestirmeDurum
{
    public int ToplamFatura { get; set; }
    public int MuhasbelestirilmisFatura { get; set; }
    public int BekleyenFatura { get; set; }
    public decimal BekleyenFaturaTutar { get; set; }

    public int ToplamMasraf { get; set; }
    public int MuhasbelestirilmisMasraf { get; set; }
    public int BekleyenMasraf { get; set; }
    public decimal BekleyenMasrafTutar { get; set; }
}

/// <summary>
/// Muhasebeleştirme öncesi kontrol sonucu
/// </summary>
public class MuhasbelestirmeKontrol
{
    public bool HazirMi { get; set; } = true;
    public List<KontrolMaddesi> Maddeler { get; set; } = new();
    public int UyariSayisi => Maddeler.Count(m => m.Seviye == KontrolSeviye.Uyari);
    public int HataSayisi => Maddeler.Count(m => m.Seviye == KontrolSeviye.Hata);
    public int BilgiSayisi => Maddeler.Count(m => m.Seviye == KontrolSeviye.Bilgi);
}

public class KontrolMaddesi
{
    public string Baslik { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public KontrolSeviye Seviye { get; set; }
    public string? IlgiliKayit { get; set; }
}

public enum KontrolSeviye
{
    Bilgi,
    Uyari,
    Hata
}

/// <summary>
/// Muhasebeleştirilmiş fatura/masraf özet bilgileri
/// </summary>
public class MuhasbelestirilmisKayit
{
    public int KaynakId { get; set; }
    public string KaynakTip { get; set; } = string.Empty; // Fatura / AracMasraf
    public string KaynakNo { get; set; } = string.Empty;
    public DateTime KaynakTarih { get; set; }
    public string? CariUnvan { get; set; }
    public decimal Tutar { get; set; }
    public int FisId { get; set; }
    public string FisNo { get; set; } = string.Empty;
    public DateTime FisTarihi { get; set; }
    public string? Aciklama { get; set; }
    public bool Secildi { get; set; }
}

/// <summary>
/// AI analiz aksiyonu
/// </summary>
public class AIAksiyon
{
    public string Aciklama { get; set; } = "";
    public string Oncelik { get; set; } = "ORTA";
    public bool Secildi { get; set; }
}

/// <summary>
/// Puantaj AI analiz aksiyonu
/// </summary>
public class PuantajAIAksiyon
{
    public string Aciklama { get; set; } = "";
    public string Oncelik { get; set; } = "ORTA";
    public bool Secildi { get; set; }
}




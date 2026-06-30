namespace MKFiloServis.Web.Models;

/// <summary>
/// Şoför performans raporu ana özet modeli
/// </summary>
public class SoforPerformansOzet
{
    public int SoforId { get; set; }
    public string SoforAdi { get; set; } = string.Empty;
    public string SoforKodu { get; set; } = string.Empty;
    
    // Genel İstatistikler
    public int ToplamSeferSayisi { get; set; }
    public int CalistigiGunSayisi { get; set; }
    public decimal ToplamKazanc { get; set; }
    public decimal OrtalamaGunlukKazanc => CalistigiGunSayisi > 0 ? ToplamKazanc / CalistigiGunSayisi : 0;
    public decimal OrtalamaSeferBasiKazanc => ToplamSeferSayisi > 0 ? ToplamKazanc / ToplamSeferSayisi : 0;
    
    // Arıza İstatistikleri
    public int ArizaliSeferSayisi { get; set; }
    public decimal ArizaOrani => ToplamSeferSayisi > 0 ? (decimal)ArizaliSeferSayisi / ToplamSeferSayisi * 100 : 0;
    
    // KM Bilgileri
    public int? ToplamKm { get; set; }
    public decimal? OrtalamaKmPerSefer => ToplamSeferSayisi > 0 && ToplamKm.HasValue ? (decimal)ToplamKm.Value / ToplamSeferSayisi : null;
    
    // Detaylar
    public List<SoforAracPerformansi> CalistigiAraclar { get; set; } = new();
    public List<SoforGuzergahPerformansi> CalistigiGuzergahlar { get; set; } = new();
    public List<SoforAylikPerformans> AylikPerformans { get; set; } = new();
}

/// <summary>
/// Şoförün araç bazında performansı
/// </summary>
public class SoforAracPerformansi
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public int SeferSayisi { get; set; }
    public decimal ToplamKazanc { get; set; }
    public int ArizaSayisi { get; set; }
}

/// <summary>
/// Şoförün güzergah bazında performansı
/// </summary>
public class SoforGuzergahPerformansi
{
    public int GuzergahId { get; set; }
    public string GuzergahAdi { get; set; } = string.Empty;
    public string CariAdi { get; set; } = string.Empty;
    public int SeferSayisi { get; set; }
    public decimal ToplamKazanc { get; set; }
    public decimal OrtalamaKazanc => SeferSayisi > 0 ? ToplamKazanc / SeferSayisi : 0;
}

/// <summary>
/// Aylık performans verisi (grafik için)
/// </summary>
public class SoforAylikPerformans
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public int SeferSayisi { get; set; }
    public decimal ToplamKazanc { get; set; }
    public int CalistigiGun { get; set; }
}

/// <summary>
/// Tüm şoförlerin karşılaştırmalı özeti
/// </summary>
public class SoforKarsilastirmaOzeti
{
    public int SoforId { get; set; }
    public string SoforAdi { get; set; } = string.Empty;
    public int SeferSayisi { get; set; }
    public decimal ToplamKazanc { get; set; }
    public decimal ArizaOrani { get; set; }
    public int CalistigiGun { get; set; }
}




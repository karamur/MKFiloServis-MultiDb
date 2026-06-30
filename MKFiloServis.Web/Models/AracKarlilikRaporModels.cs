namespace MKFiloServis.Web.Models;

/// <summary>
/// Araç karlılık özet bilgileri
/// </summary>
public class AracKarlilikOzet
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public string SahiplikTipi { get; set; } = string.Empty; // Özmal / Kiralık
    
    // Gelir bilgileri
    public int ToplamSeferSayisi { get; set; }
    public decimal ToplamGelir { get; set; }
    
    // Gider bilgileri
    public decimal ToplamMasraf { get; set; }
    public decimal KiraBedeli { get; set; } // Kiralık araçlar için
    public decimal KomisyonTutari { get; set; } // Komisyonlu araçlar için
    public decimal ToplamGider => ToplamMasraf + KiraBedeli + KomisyonTutari;
    
    // Karlılık
    public decimal NetKar => ToplamGelir - ToplamGider;
    public decimal KarMarji => ToplamGelir > 0 ? (NetKar / ToplamGelir) * 100 : 0;
    
    // Ortalamalar
    public decimal OrtalamaGelirPerSefer => ToplamSeferSayisi > 0 ? ToplamGelir / ToplamSeferSayisi : 0;
    public decimal OrtalamaGiderPerSefer => ToplamSeferSayisi > 0 ? ToplamGider / ToplamSeferSayisi : 0;
    
    // Arıza bilgileri
    public int ArizaSayisi { get; set; }
    public decimal ArizaOrani => ToplamSeferSayisi > 0 ? ((decimal)ArizaSayisi / ToplamSeferSayisi) * 100 : 0;
    
    // Çalışma detayları
    public int CalismaGunSayisi { get; set; }
    public DateTime? SonCalismaTarihi { get; set; }
    
    // Detaylar
    public List<AracMasrafDetay> MasrafDetaylari { get; set; } = [];
    public List<AracAylikKarlilik> AylikKarlilik { get; set; } = [];
    public List<AracGuzergahPerformansi> GuzergahPerformanslari { get; set; } = [];
}

/// <summary>
/// Araç masraf detayı (kategori bazlı)
/// </summary>
public class AracMasrafDetay
{
    public int MasrafKalemiId { get; set; }
    public string MasrafKalemiAdi { get; set; } = string.Empty;
    public decimal ToplamTutar { get; set; }
    public int Adet { get; set; }
    public decimal Oran { get; set; } // Toplam masraf içindeki yüzde
}

/// <summary>
/// Araç aylık karlılık verisi
/// </summary>
public class AracAylikKarlilik
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public int SeferSayisi { get; set; }
    public decimal Gelir { get; set; }
    public decimal Masraf { get; set; }
    public decimal KiraBedeli { get; set; }
    public decimal Komisyon { get; set; }
    public decimal ToplamGider => Masraf + KiraBedeli + Komisyon;
    public decimal NetKar => Gelir - ToplamGider;
}

/// <summary>
/// Araç güzergah bazlı performans
/// </summary>
public class AracGuzergahPerformansi
{
    public int GuzergahId { get; set; }
    public string GuzergahAdi { get; set; } = string.Empty;
    public string? FirmaAdi { get; set; }
    public int SeferSayisi { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal OrtalamaGelir => SeferSayisi > 0 ? ToplamGelir / SeferSayisi : 0;
}

/// <summary>
/// Araçlar arası karşılaştırma özeti
/// </summary>
public class AracKarsilastirmaOzeti
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string? MarkaModel { get; set; }
    public string SahiplikTipi { get; set; } = string.Empty;
    
    public int SeferSayisi { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal NetKar { get; set; }
    public decimal KarMarji { get; set; }
    public int ArizaSayisi { get; set; }
    public decimal ArizaOrani { get; set; }
    
    // Sıralama için
    public int KarlilikSirasi { get; set; }
    public int VerimlilikSirasi { get; set; } // Sefer başına kar
}




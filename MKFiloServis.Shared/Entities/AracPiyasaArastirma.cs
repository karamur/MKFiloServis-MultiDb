namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Ara� piyasa ara�t�rma kay�tlar�
/// </summary>
public class AracPiyasaArastirma : BaseEntity
{
    public string Marka { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Versiyon { get; set; }
    public int? YilBaslangic { get; set; }
    public int? YilBitis { get; set; }
    public string? YakitTipi { get; set; }
    public string? VitesTipi { get; set; }
    public int? MinKilometre { get; set; }
    public int? MaxKilometre { get; set; }
    public decimal? MinFiyat { get; set; }
    public decimal? MaxFiyat { get; set; }
    public string? Sehir { get; set; }

    // Ara�t�rma sonu�lar� �zeti
    public int ToplamIlanSayisi { get; set; }
    public decimal OrtalamaFiyat { get; set; }
    public decimal EnDusukFiyat { get; set; }
    public decimal EnYuksekFiyat { get; set; }
    public decimal MedianFiyat { get; set; }
    public int OrtalamaKilometre { get; set; }

    public DateTime ArastirmaTarihi { get; set; } = DateTime.Now;
    public ArastirmaDurum Durum { get; set; } = ArastirmaDurum.Bekliyor;
    public string? HataMesaji { get; set; }
    public string? AIAnalizi { get; set; }

    // Navigation
    public virtual ICollection<PiyasaArastirmaIlan> Ilanlar { get; set; } = new List<PiyasaArastirmaIlan>();
}

/// <summary>
/// Piyasadan toplanan ilan bilgileri (Ara�t�rma i�in)
/// </summary>
public class PiyasaArastirmaIlan : BaseEntity
{
    public int ArastirmaId { get; set; }

    public string Kaynak { get; set; } = string.Empty; // Sahibinden, Arabam, vs.
    public string? IlanNo { get; set; }
    public string IlanBasligi { get; set; } = string.Empty;
    public string? IlanUrl { get; set; }
    public string? ResimUrl { get; set; } // Ana ilan fotografi (thumbnail)

    public string Marka { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Versiyon { get; set; }
    public int ModelYili { get; set; }
    public int Kilometre { get; set; }
    public decimal Fiyat { get; set; }
    public string? ParaBirimi { get; set; } = "TRY";

    public string? YakitTipi { get; set; }
    public string? VitesTipi { get; set; }
    public string? KasaTipi { get; set; }
    public string? MotorHacmi { get; set; }
    public string? MotorGucu { get; set; }
    public string? Renk { get; set; }
    public string? Kapasite { get; set; } // Motor kapasitesi (cc veya kW)
    public string? TasimaKapasitesi { get; set; } // Koltuk sayisi / Yolcu kapasitesi

    public int? BoyaliParcaSayisi { get; set; }
    public int? DegisenParcaSayisi { get; set; }
    public decimal? TramerTutari { get; set; }
    public bool HasarKayitli { get; set; }

    public string? Sehir { get; set; }
    public string? Ilce { get; set; }
    public string? SaticiTipi { get; set; } // Galeri, Bireysel
    public string? SaticiAdi { get; set; }

    public DateTime? IlanTarihi { get; set; }
    public DateTime ToplanmaTarihi { get; set; } = DateTime.Now;
    public bool AktifMi { get; set; } = true;
    public string? Notlar { get; set; }

    // Birden fazla fotograf icin JSON array - AI tarafindan cekilir
    public string? Fotograflar { get; set; } // JSON array: ["url1", "url2", ...]

    // Navigation
    public virtual AracPiyasaArastirma? Arastirma { get; set; }
}

/// <summary>
/// Ara� marka ve model veritaban�
/// </summary>
public class AracMarkaModel : BaseEntity
{
    public string Marka { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Versiyon { get; set; }
    public int? BaslangicYili { get; set; }
    public int? BitisYili { get; set; }
    public string? KasaTipi { get; set; }
    public string? YakitTipleri { get; set; } // JSON array
    public string? VitesTipleri { get; set; } // JSON array
    public string? Segment { get; set; } // A, B, C, D, SUV, etc.
    public bool Aktif { get; set; } = true;
    public int Sira { get; set; }
}

/// <summary>
/// Piyasa ara�t�rma favorileri
/// </summary>
public class PiyasaArastirmaFavori : BaseEntity
{
    public int IlanId { get; set; }
    public string? Notlar { get; set; }
    public FavoriDurum Durum { get; set; } = FavoriDurum.Inceleniyor;
    public decimal? TeklifFiyati { get; set; }

    public virtual PiyasaArastirmaIlan? Ilan { get; set; }
}

public enum ArastirmaDurum
{
    Bekliyor = 0,
    Devam = 1,
    Tamamlandi = 2,
    Hata = 3,
    Iptal = 4
}

public enum FavoriDurum
{
    Inceleniyor = 0,
    TeklifVerildi = 1,
    Gorusuldu = 2,
    Reddedildi = 3,
    SatinAlindi = 4
}



namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Arac Evrak/Belge Yonetimi - Ruhsat, Sigorta, Muayene vb.
/// </summary>
public class AracEvrak : BaseEntity, IFirmaTenant
{
    // Kural 4: FirmaId dogrudan entity'de (Arac uzerinden join gerekmez)
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int AracId { get; set; }
    public string EvrakKategorisi { get; set; } = string.Empty; // Ruhsat, Sigorta, Muayene, Kasko, SRC Belgesi vb.
    public string? EvrakAdi { get; set; }
    public string? Aciklama { get; set; }

    // Tarih bilgileri
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public DateTime? HatirlatmaTarihi { get; set; }

    // Tutar bilgileri
    public decimal? Tutar { get; set; }
    public string? SigortaSirketi { get; set; }
    public string? PoliceNo { get; set; }

    // Durum
    public EvrakDurum Durum { get; set; } = EvrakDurum.Aktif;
    public bool HatirlatmaAktif { get; set; } = true;
    public int HatirlatmaGunOnce { get; set; } = 15; // Kac gun once hatirlatilsin

    // Navigation
    public virtual Arac? Arac { get; set; }
    public virtual ICollection<AracEvrakDosya> Dosyalar { get; set; } = new List<AracEvrakDosya>();
}

/// <summary>
/// Arac Evrak Dosyalari - Birden fazla dosya eklenebilir
/// </summary>
public class AracEvrakDosya : BaseEntity, IFirmaTenant
{
    // Kural 4: FirmaId dogrudan entity'de
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int AracEvrakId { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;
    public string? DosyaTipi { get; set; } // pdf, jpg, png vb.
    public long DosyaBoyutu { get; set; }
    public string? Aciklama { get; set; }

    /// <summary>
    /// Mevcut versiyon numarasi (1'den baslar)
    /// </summary>
    public int VersiyonNo { get; set; } = 1;

    /// <summary>
    /// Son degisiklik notu
    /// </summary>
    public string? SonDegisiklikNotu { get; set; }

    // Navigation
    public virtual AracEvrak? AracEvrak { get; set; }

    /// <summary>
    /// Versiyon geçmişi - önceki versiyonlar
    /// </summary>
    public virtual ICollection<AracEvrakDosyaVersiyon> Versiyonlar { get; set; } = new List<AracEvrakDosyaVersiyon>();
}

public enum EvrakDurum
{
    Aktif = 1,
    Pasif = 2,
    SuresiDolmus = 3
}

/// <summary>
/// Evrak Kategorileri - Varsayilan kategoriler
/// </summary>
public static class EvrakKategorileri
{
    public const string Ruhsat = "Ruhsat";
    public const string TrafikSigortasi = "Trafik Sigortasi";
    public const string KoltukSigortasi = "Koltuk Sigortasi";
    public const string Kasko = "Kasko";
    public const string Muayene = "Muayene";
    public const string UygunlukBelgesi = "Uygunluk Belgesi";
    public const string EmisyonBelgesi = "Emisyon Belgesi";
    public const string YetkiBelgesi = "Yetki Belgesi";
    public const string Diger = "Diger";

    /// <summary>
    /// Görüntü/sıralama düzeni: Ruhsat, Trafik Sigortası, Koltuk Sigortası, Kasko,
    /// Muayene, Uygunluk Belgesi, Emisyon Belgesi, Yetki Belgesi, Diğer.
    /// </summary>
    public static readonly string[] TumKategoriler = new[]
    {
        Ruhsat, TrafikSigortasi, KoltukSigortasi, Kasko,
        Muayene, UygunlukBelgesi, EmisyonBelgesi, YetkiBelgesi, Diger
    };

    public static int SiraIndex(string? kategori)
    {
        if (string.IsNullOrWhiteSpace(kategori)) return int.MaxValue;
        var idx = Array.IndexOf(TumKategoriler, kategori);
        return idx < 0 ? int.MaxValue : idx;
    }
}



namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Personel taşımacılığında dış tedarikçi (alt yüklenici) firma.
/// Tedarikçinin kendi personeli mevcut <see cref="Sofor"/>, kendi araçları
/// mevcut <see cref="Arac"/> kayıtları üzerinden takip edilir; bu modül
/// sadece tedarikçi şirketinin kimlik/sözleşme bilgisini ve iş eşleşmesini tutar.
/// </summary>
public class TasimaTedarikci : BaseEntity
{
    public string TedarikciKodu { get; set; } = string.Empty;
    public string Unvan { get; set; } = string.Empty;

    // İletişim
    public string? YetkiliKisi { get; set; }
    public string? Telefon { get; set; }
    public string? Telefon2 { get; set; }
    public string? Email { get; set; }
    public string? Adres { get; set; }
    public string? Il { get; set; }
    public string? Ilce { get; set; }

    // Vergi
    public string? VergiDairesi { get; set; }
    public string? VergiNo { get; set; }

    // Finans / Cari bağlantısı (mali analiz ve fatura için tek kaynak)
    public int? CariId { get; set; }
    public virtual Cari? Cari { get; set; }

    // Sözleşme bilgileri
    public DateTime? SozlesmeBaslangicTarihi { get; set; }
    public DateTime? SozlesmeBitisTarihi { get; set; }
    public string? SozlesmeNo { get; set; }
    public decimal? VarsayilanSeferUcreti { get; set; }

    public bool Aktif { get; set; } = true;
    public string? Notlar { get; set; }

    // Navigation - tedarikçinin personeli (Sofor.TasimaTedarikciId)
    public virtual ICollection<Sofor> Personeller { get; set; } = new List<Sofor>();

    // Navigation - tedarikçinin araçları (Arac.TasimaTedarikciId)
    public virtual ICollection<Arac> Araclar { get; set; } = new List<Arac>();

    // Navigation - tedarikçi iş atamaları (güzergah eşleşmeleri)
    public virtual ICollection<TasimaTedarikciIs> Isler { get; set; } = new List<TasimaTedarikciIs>();

    // Navigation - tedarikçi firma evrakları (Ticaret Sicil, Vergi Levhası vb.)
    public virtual ICollection<TedarikciEvrak> Evraklar { get; set; } = new List<TedarikciEvrak>();
}

/// <summary>
/// Tedarikçi firma evrakı (Ticaret Sicil, Vergi Levhası, İmza Sirküleri vb.)
/// AracEvrak ile aynı yapı, TasimaTedarikci'ye bağlı.
/// </summary>
public class TedarikciEvrak : BaseEntity
{
    public int TasimaTedarikciId { get; set; }
    public virtual TasimaTedarikci? TasimaTedarikci { get; set; }

    public string EvrakKategorisi { get; set; } = string.Empty;
    public string? EvrakAdi { get; set; }
    public string? Aciklama { get; set; }

    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public DateTime? HatirlatmaTarihi { get; set; }

    public decimal? Tutar { get; set; }
    public string? SigortaSirketi { get; set; }
    public string? PoliceNo { get; set; }

    public EvrakDurum Durum { get; set; } = EvrakDurum.Aktif;
    public bool HatirlatmaAktif { get; set; } = true;
    public int HatirlatmaGunOnce { get; set; } = 15;

    public virtual ICollection<TedarikciEvrakDosya> Dosyalar { get; set; } = new List<TedarikciEvrakDosya>();
}

/// <summary>
/// Tedarikçi evrak dosyası (birden fazla dosya eklenebilir).
/// </summary>
public class TedarikciEvrakDosya : BaseEntity
{
    public int TedarikciEvrakId { get; set; }
    public virtual TedarikciEvrak? TedarikciEvrak { get; set; }

    public string DosyaAdi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;
    public string? DosyaTipi { get; set; }
    public long DosyaBoyutu { get; set; }
    public string? Aciklama { get; set; }
    public int VersiyonNo { get; set; } = 1;
    public string? SonDegisiklikNotu { get; set; }
}

/// <summary>
/// Tedarikçi firma evrak kategorileri.
/// </summary>
public static class TedarikciEvrakKategorileri
{
    public const string TicaretSicil     = "Ticaret Sicil Gazetesi";
    public const string VergiLevhasi     = "Vergi Levhası";
    public const string ImzaSirkuleri    = "İmza Sirküleri";
    public const string YetkiBelgesi     = "Yetki Belgesi";
    public const string FaaliyetBelgesi  = "Faaliyet Belgesi";
    public const string SgkBelgesi       = "SGK Belgesi";
    public const string IsGuvenligi      = "İş Güvenliği Belgesi";
    public const string SorumlulukSig    = "Sorumluluk Sigortası";
    public const string KapasteRaporu    = "Kapasite Raporu";
    public const string Diger            = "Diğer";

    /// <summary>
    /// Sıralı tam kategori listesi (aynı yapı AracEvrak.EvrakKategorileri gibi).
    /// </summary>
    public static readonly string[] TumKategoriler =
    {
        TicaretSicil, VergiLevhasi, ImzaSirkuleri, YetkiBelgesi,
        FaaliyetBelgesi, SgkBelgesi, IsGuvenligi, SorumlulukSig,
        KapasteRaporu, Diger
    };

    public static int SiraIndex(string? kategori)
    {
        if (string.IsNullOrWhiteSpace(kategori)) return int.MaxValue;
        var idx = Array.IndexOf(TumKategoriler, kategori);
        return idx < 0 ? int.MaxValue : idx;
    }
}

/// <summary>
/// Tedarikçi - Güzergah - İş eşleşmesi.
/// Bir tedarikçinin hangi güzergahta hangi tarih aralığında, hangi araç/şoför ile
/// çalıştığını ve sözleşme ücretini tutar.
/// </summary>
public class TasimaTedarikciIs : BaseEntity
{
    public int TasimaTedarikciId { get; set; }
    public virtual TasimaTedarikci TasimaTedarikci { get; set; } = null!;

    public int GuzergahId { get; set; }
    public virtual Guzergah Guzergah { get; set; } = null!;

    // Tedarikçinin atadığı araç / şoför (opsiyonel - tek kaynak: Arac / Sofor tabloları)
    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    public int? SoforId { get; set; }
    public virtual Sofor? Sofor { get; set; }

    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;
    public DateTime? BitisTarihi { get; set; }

    public decimal? SeferUcreti { get; set; }
    public decimal? AylikUcret { get; set; }

    public TasimaTedarikciIsDurum Durum { get; set; } = TasimaTedarikciIsDurum.Aktif;
    public string? Aciklama { get; set; }
}

public enum TasimaTedarikciIsDurum
{
    Beklemede = 0,
    Aktif = 1,
    Tamamlandi = 2,
    IptalEdildi = 3
}



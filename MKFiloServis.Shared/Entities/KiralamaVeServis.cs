using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Kiralanm�� ara� kay�tlar� (d��ar�dan kiralanan ara�lar)
/// </summary>
public class KiralamaArac : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }
    int? IFirmaTenant.FirmaId { get => FirmaId; set => FirmaId = value ?? 0; }

    /// <summary>
    /// Kiralayan cari (ara� sahibi)
    /// </summary>
    [Required]
    public int KiralayiciCariId { get; set; }

    [Required]
    [StringLength(15)]
    public string Plaka { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Marka { get; set; }

    [StringLength(50)]
    public string? Model { get; set; }

    public int? ModelYili { get; set; }

    public AracTipi AracTipi { get; set; }

    public int? KoltukSayisi { get; set; }

    /// <summary>
    /// Kiralama ba�lang�� tarihi
    /// </summary>
    [Required]
    public DateTime KiralamaBaslangic { get; set; }

    /// <summary>
    /// Kiralama biti� tarihi (null ise s�resiz)
    /// </summary>
    public DateTime? KiralamaBitis { get; set; }

    /// <summary>
    /// G�nl�k kira bedeli
    /// </summary>
    public decimal? GunlukKiraBedeli { get; set; }

    /// <summary>
    /// Sefer ba��na kira bedeli
    /// </summary>
    public decimal? SeferBasinaKiraBedeli { get; set; }

    /// <summary>
    /// Ayl�k sabit kira bedeli
    /// </summary>
    public decimal? AylikKiraBedeli { get; set; }

    /// <summary>
    /// Komisyon oran� (%)
    /// </summary>
    public decimal? KomisyonOrani { get; set; }

    /// <summary>
    /// Sabit komisyon tutar�
    /// </summary>
    public decimal? SabitKomisyonTutari { get; set; }

    public string? SozlesmeNo { get; set; }

    public string? Notlar { get; set; }

    public bool Aktif { get; set; } = true;

    // Navigation
    public virtual Firma? Firma { get; set; }
    public virtual Cari? KiralayiciCari { get; set; }
    public virtual ICollection<ServisCalismaKiralama> ServisCalismalari { get; set; } = new List<ServisCalismaKiralama>();
}

/// <summary>
/// Kiralanm�� ara�lar�n servis �al��malar�
/// (Hem kendi ara�lar� hem kiral�k ara�lar i�in ortak kay�t)
/// </summary>
public class ServisCalismaKiralama : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }
    int? IFirmaTenant.FirmaId { get => FirmaId; set => FirmaId = value ?? 0; }

    [Required]
    public DateTime CalismaTarihi { get; set; }

    [Required]
    public ServisTuru ServisTuru { get; set; }

    /// <summary>
    /// Ara� t�r� (Kendi/Kiral�k)
    /// </summary>
    [Required]
    public AracSahiplikTuru AracSahiplikTuru { get; set; }

    /// <summary>
    /// Kendi arac�m�z ise
    /// </summary>
    public int? AracId { get; set; }

    /// <summary>
    /// Kiral�k ara� ise
    /// </summary>
    public int? KiralamaAracId { get; set; }

    [Required]
    public int SoforId { get; set; }

    [Required]
    public int GuzergahId { get; set; }

    /// <summary>
    /// M��teri firma (Ba�kas�n�n g�zergah�nda �al���yorsak)
    /// </summary>
    public int? MusteriFirmaId { get; set; }

    /// <summary>
    /// �al��ma fiyat�
    /// </summary>
    public decimal? CalismaBedeli { get; set; }

    /// <summary>
    /// Ara� kira bedeli (kiral�k ara� ise)
    /// </summary>
    public decimal? AracKiraBedeli { get; set; }

    /// <summary>
    /// Komisyon tutar� (varsa)
    /// </summary>
    public decimal? KomisyonTutari { get; set; }

    /// <summary>
    /// Net kazan� (�al��ma bedeli - Kira - Komisyon)
    /// </summary>
    public decimal? NetKazanc { get; set; }

    public int? KmBaslangic { get; set; }
    public int? KmBitis { get; set; }
    public int? ToplamKm { get; set; }

    public TimeSpan? BaslangicSaati { get; set; }
    public TimeSpan? BitisSaati { get; set; }

    public bool ArizaOlduMu { get; set; }
    public string? ArizaAciklamasi { get; set; }

    public CalismaDurum Durum { get; set; } = CalismaDurum.Tamamlandi;

    public string? Notlar { get; set; }

    // Navigation
    public virtual Firma? Firma { get; set; }
    public virtual Arac? Arac { get; set; }
    public virtual KiralamaArac? KiralamaArac { get; set; }
    public virtual Sofor? Sofor { get; set; }
    public virtual Guzergah? Guzergah { get; set; }
    public virtual Firma? MusteriFirma { get; set; }
}

/// <summary>
/// Ara� sahiplik t�r�
/// </summary>
public enum AracSahiplikTuru
{
    /// <summary>
    /// Kendi arac�m�z
    /// </summary>
    KendiArac = 1,

    /// <summary>
    /// Kiral�k ara�
    /// </summary>
    KiralikArac = 2
}

/// <summary>
/// Servis �al��ma puantaj raporu (Excel i�in)
/// </summary>
public class ServisCalismaRapor
{
    public DateTime Tarih { get; set; }
    public string? Plaka { get; set; }
    public string? AracSahiplik { get; set; } // "Kendi" veya "Kiral�k"
    public string? SoforAdi { get; set; }
    public string? GuzergahAdi { get; set; }
    public string? MusteriFirma { get; set; } // Ba�ka firma i�in �al���yorsak
    public string? ServisTuru { get; set; }
    public decimal? CalismaBedeli { get; set; }
    public decimal? AracKiraBedeli { get; set; }
    public decimal? KomisyonTutari { get; set; }
    public decimal? NetKazanc { get; set; }
    public int? ToplamKm { get; set; }
    public string? BaslangicSaati { get; set; }
    public string? BitisSaati { get; set; }
    public string? Durum { get; set; }
    public string? Notlar { get; set; }
}



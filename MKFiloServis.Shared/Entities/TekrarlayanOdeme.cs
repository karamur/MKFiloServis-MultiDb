using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Tekrarlayan odeme tanimlari (Kira, Elektrik, Su, Dogalgaz, Internet vb.)
/// Bu kayitlar belirtilen periyotlarda otomatik olarak BudgetOdeme kayitlari olusturur.
/// </summary>
public class TekrarlayanOdeme : BaseEntity, IFirmaTenant
{
    [Required]
    [StringLength(200)]
    public string OdemeAdi { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string MasrafKalemi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    [Required]
    public decimal Tutar { get; set; }

    /// <summary>
    /// Tekrar periyodu
    /// </summary>
    [Required]
    public TekrarPeriyodu Periyod { get; set; } = TekrarPeriyodu.Aylik;

    /// <summary>
    /// Ayin hangi gunu odeme yapilacak (1-31)
    /// </summary>
    [Range(1, 31)]
    public int OdemeGunu { get; set; } = 1;

    /// <summary>
    /// Baslangic tarihi
    /// </summary>
    [Required]
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;

    /// <summary>
    /// Bitis tarihi (null ise suresiz devam eder)
    /// </summary>
    public DateTime? BitisTarihi { get; set; }

    /// <summary>
    /// Hatirlatici - Odeme gununen kac gun once uyari verilsin
    /// </summary>
    public int HatirlatmaGunSayisi { get; set; } = 3;

    /// <summary>
    /// Firma iliskisi
    /// </summary>
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;

    /// <summary>
    /// Renk kodu (gorunum icin)
    /// </summary>
    [StringLength(20)]
    public string? Renk { get; set; }

    /// <summary>
    /// Icon (gorunum icin)
    /// </summary>
    [StringLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// Notlar
    /// </summary>
    public string? Notlar { get; set; }
}

/// <summary>
/// Tekrar periyodu
/// </summary>
public enum TekrarPeriyodu
{
    Gunluk = 1,
    Haftalik = 2,
    Aylik = 3,
    IkiAylik = 4,
    UcAylik = 5,
    AltiAylik = 6,
    Yillik = 7
}



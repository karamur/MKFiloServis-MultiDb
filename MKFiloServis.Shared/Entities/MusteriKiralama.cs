using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// M��teriye ara� kiralama kayd�
/// �irketin kendi ara�lar�n� m��terilere kiralamas�
/// </summary>
public class MusteriKiralama : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }
    int? IFirmaTenant.FirmaId { get => FirmaId; set => FirmaId = value ?? 0; }
    public virtual Firma? Firma { get; set; }

    /// <summary>
    /// Kiralayan m��teri
    /// </summary>
    [Required]
    public int MusteriId { get; set; }

    /// <summary>
    /// Kiralanan ara�
    /// </summary>
    [Required]
    public int AracId { get; set; }

    /// <summary>
    /// Kiralama ba�lang�� tarihi ve saati
    /// </summary>
    [Required]
    public DateTime BaslangicTarihi { get; set; }

    /// <summary>
    /// Planlanan biti� tarihi
    /// </summary>
    [Required]
    public DateTime PlanlananBitisTarihi { get; set; }

    /// <summary>
    /// Ger�ek teslim tarihi
    /// </summary>
    public DateTime? GercekBitisTarihi { get; set; }

    /// <summary>
    /// Ba�lang�� kilometresi
    /// </summary>
    public int? BaslangicKm { get; set; }

    /// <summary>
    /// Biti� kilometresi
    /// </summary>
    public int? BitisKm { get; set; }

    /// <summary>
    /// G�nl�k kira bedeli
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal GunlukFiyat { get; set; }

    /// <summary>
    /// Toplam tutar
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamTutar { get; set; }

    /// <summary>
    /// Al�nan depozito
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Depozito { get; set; }

    /// <summary>
    /// Kiralama durumu
    /// </summary>
    public KiralamaDurumu Durum { get; set; } = KiralamaDurumu.Rezervasyon;

    /// <summary>
    /// �deme durumu
    /// </summary>
    public KiralamaOdemeDurumu OdemeDurumu { get; set; } = KiralamaOdemeDurumu.Beklemede;

    /// <summary>
    /// Teslim alan personel
    /// </summary>
    public int? TeslimAlanPersonelId { get; set; }

    /// <summary>
    /// Teslim eden personel
    /// </summary>
    public int? TeslimEdenPersonelId { get; set; }

    /// <summary>
    /// Notlar
    /// </summary>
    [StringLength(500)]
    public string? Notlar { get; set; }

    /// <summary>
    /// S�zle�me numaras�
    /// </summary>
    [StringLength(50)]
    public string? SozlesmeNo { get; set; }
}

public enum KiralamaDurumu
{
    Rezervasyon = 0,
    Aktif = 1,
    Tamamlandi = 2,
    IptalEdildi = 3
}

public enum KiralamaOdemeDurumu
{
    Beklemede = 0,
    KismiOdendi = 1,
    Odendi = 2,
    IadeEdildi = 3
}



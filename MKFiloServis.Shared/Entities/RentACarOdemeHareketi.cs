using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

public class RentACarOdemeHareketi : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }
    int? IFirmaTenant.FirmaId { get => FirmaId; set => FirmaId = value ?? 0; }

    [Required]
    public int MusteriKiralamaId { get; set; }
    public virtual MusteriKiralama? MusteriKiralama { get; set; }

    public DateTime IslemTarihi { get; set; } = DateTime.Now;
    public RentACarOdemeHareketTuru HareketTuru { get; set; }
    public RentACarOdemeYontemi OdemeYontemi { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tutar { get; set; }

    [StringLength(100)]
    public string? BelgeNo { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }
}

public enum RentACarOdemeHareketTuru
{
    KiraTahsilati = 0,
    DepozitoTahsilati = 1,
    KiraIadesi = 2,
    DepozitoIadesi = 3
}

public enum RentACarOdemeYontemi
{
    Nakit = 0,
    KrediKarti = 1,
    BankaKarti = 2,
    Havale = 3,
    EFT = 4,
    CariMahsup = 5,
    Diger = 6
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Aylık araç maliyet snapshot'u (Özmal / Kiralık araçlar için).
/// Yakıt + Bakım + Lastik + Sigorta + Plaka kirası + Şoför maaş payı + Amortisman özeti.
/// </summary>
public class AracMaliyetSnapshot : BaseEntity
{
    [Required]
    public int AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    [Required]
    public int Yil { get; set; }

    [Required]
    public int Ay { get; set; }

    /// <summary>Sahiplik snapshot tarihindeki değer (Özmal / Kiralık)</summary>
    public AracSahiplikTipi SahiplikTipi { get; set; } = AracSahiplikTipi.Ozmal;

    public decimal ToplamKm { get; set; }
    public decimal ToplamSefer { get; set; }

    public decimal YakitMasraf { get; set; }
    public decimal BakimMasraf { get; set; }
    public decimal LastikMasraf { get; set; }
    public decimal SigortaMasraf { get; set; }
    public decimal KaskoMasraf { get; set; }
    public decimal PlakaKirasi { get; set; }
    public decimal SoforMaasPayi { get; set; }
    public decimal AmortismanPayi { get; set; }
    public decimal DigerMasraf { get; set; }

    [NotMapped]
    public decimal ToplamMaliyet =>
        YakitMasraf + BakimMasraf + LastikMasraf + SigortaMasraf + KaskoMasraf
        + PlakaKirasi + SoforMaasPayi + AmortismanPayi + DigerMasraf;

    [NotMapped]
    public decimal SeferBasiMaliyet => ToplamSefer > 0 ? Math.Round(ToplamMaliyet / ToplamSefer, 2) : 0m;

    /// <summary>Kuruma kesilen / tahakkuk eden toplam gelir (snapshot anı)</summary>
    public decimal ToplamGelir { get; set; }

    [NotMapped]
    public decimal KarZarar => ToplamGelir - ToplamMaliyet;

    public string? Notlar { get; set; }
}



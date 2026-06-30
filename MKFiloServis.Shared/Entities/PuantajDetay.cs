using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// OperasyonKaydi ↔ PuantajKayit bağlantı tablosu.
/// Her hesaplama döngüsünde OperasyonKaydi'nın PuantajKayit'a katkısını snapshot ile kaydeder.
/// Finansal audit trail için hesaplama anındaki birim fiyatları dondurur.
/// </summary>
public class PuantajDetay : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    // FK'lar
    public int OperasyonKaydiId { get; set; }
    public virtual OperasyonKaydi? OperasyonKaydi { get; set; }

    public int PuantajKayitId { get; set; }
    public virtual PuantajKayit? PuantajKayit { get; set; }

    public int HesapDonemiId { get; set; }
    public virtual PuantajHesapDonemi? HesapDonemi { get; set; }

    // Snapshot değerler (hesaplama anında dondurulur)
    [Column(TypeName = "decimal(18,2)")]
    public decimal BirimGelir { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BirimGider { get; set; }

    /// <summary>Bu operasyonun PuantajKayit'a kattığı sefer sayısı</summary>
    public int SeferSayisi { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal HesaplananTutar { get; set; }
}



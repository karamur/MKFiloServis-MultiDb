using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Models;

public class PuantajSatirDetayDto
{
    public required FiloGunlukPuantaj Puantaj { get; set; }
    public string AracSahibiAd { get; set; } = "-";
    public string AracSahibiTip { get; set; } = "-";
    public decimal GelenFaturaTutari { get; set; }
    public decimal GidenFaturaTutari { get; set; }
}

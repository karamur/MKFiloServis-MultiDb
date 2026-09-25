using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

public sealed class RentACarKaraListeKaydi : BaseEntity, IFirmaTenant
{
    public int FirmaId { get; set; }
    int? IFirmaTenant.FirmaId { get => FirmaId; set => FirmaId = value ?? 0; }
    public int MusteriId { get; set; }
    [Required, StringLength(500)]
    public string Neden { get; set; } = string.Empty;
    public int EkleyenKullaniciId { get; set; }
    public DateTime? KaldirmaTarihi { get; set; }
    public int? KaldiranKullaniciId { get; set; }
    [StringLength(500)]
    public string? KaldirmaNedeni { get; set; }
}

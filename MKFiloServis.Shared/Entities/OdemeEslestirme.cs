namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Fatura ve Banka/Kasa hareketi e�le�tirme
/// </summary>
public class OdemeEslestirme : BaseEntity
{
    public DateTime EslestirmeTarihi { get; set; }
    public decimal EslestirilenTutar { get; set; }
    public string? Aciklama { get; set; }

    // Foreign Keys
    public int FaturaId { get; set; }
    public int BankaKasaHareketId { get; set; }

    // Navigation Properties
    public virtual Fatura Fatura { get; set; } = null!;
    public virtual BankaKasaHareket BankaKasaHareket { get; set; } = null!;
}



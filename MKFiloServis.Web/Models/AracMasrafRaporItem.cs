namespace MKFiloServis.Web.Models;

public class AracMasrafRaporItem
{
    public DateTime MasrafTarihi { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string MasrafKalemi { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public string? GuzergahAdi { get; set; }
    public decimal Tutar { get; set; }
    public string? BelgeNo { get; set; }
    public string? Aciklama { get; set; }
    public bool ArizaKaynakli { get; set; }
}




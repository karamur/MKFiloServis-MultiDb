namespace MKFiloServis.Web.Models;

public class CariEkstre
{
    public int CariId { get; set; }
    public string CariKodu { get; set; } = string.Empty;
    public string CariUnvan { get; set; } = string.Empty;
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public decimal DevredenBakiye { get; set; }
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal Bakiye { get; set; }
    public List<CariEkstreItem> Hareketler { get; set; } = new();
}

public class CariEkstreItem
{
    public DateTime Tarih { get; set; }
    public string BelgeNo { get; set; } = string.Empty;
    public string IslemTipi { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye { get; set; }
}




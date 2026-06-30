namespace MKFiloServis.Web.Models;

public class FaturaOdemeRaporItem
{
    public int FaturaId { get; set; }
    public string FaturaNo { get; set; } = string.Empty;
    public DateTime FaturaTarihi { get; set; }
    public DateTime? VadeTarihi { get; set; }
    public string CariUnvan { get; set; } = string.Empty;
    public string FaturaTipi { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;
    public decimal GenelToplam { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal KalanTutar { get; set; }
    public int VadeGunu { get; set; } // Negatif ise gecikmi�
}




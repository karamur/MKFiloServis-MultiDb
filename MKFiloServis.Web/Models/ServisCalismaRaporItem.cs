namespace MKFiloServis.Web.Models;

public class ServisCalismaRaporItem
{
    public DateTime Tarih { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string SoforAdi { get; set; } = string.Empty;
    public string GuzergahAdi { get; set; } = string.Empty;
    public string FirmaAdi { get; set; } = string.Empty;
    public string ServisTuru { get; set; } = string.Empty;
    public decimal BirimFiyat { get; set; }
    public int CalisilanGun { get; set; }
    public decimal ToplamTutar { get; set; }
}

public class ServisCalismaRaporOzet
{
    public string Plaka { get; set; } = string.Empty;
    public string SoforAdi { get; set; } = string.Empty;
    public string GuzergahAdi { get; set; } = string.Empty;
    public string FirmaAdi { get; set; } = string.Empty;
    public decimal BirimFiyat { get; set; }
    public int CalisilanGun { get; set; }
    public decimal ToplamTutar { get; set; }
}




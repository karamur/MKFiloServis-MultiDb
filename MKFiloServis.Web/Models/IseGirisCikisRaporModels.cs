using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Models;

public enum IseGirisCikisFiltreTipi
{
    Tumu = 0,
    IseGiris = 1,
    IstenCikis = 2
}

public enum IseGirisCikisKayitTipi
{
    IseGiris = 1,
    IstenCikis = 2
}

public class IseGirisCikisBildirgeRaporu
{
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public IseGirisCikisFiltreTipi FiltreTipi { get; set; }
    public PersonelGorev? Gorev { get; set; }
    public int ToplamKayit { get; set; }
    public int IseGirisSayisi { get; set; }
    public int IstenCikisSayisi { get; set; }
    public int AktifPersonelSayisi { get; set; }
    public List<IseGirisCikisBildirgeSatiri> Kayitlar { get; set; } = new();
}

public class IseGirisCikisBildirgeSatiri
{
    public int SoforId { get; set; }
    public IseGirisCikisKayitTipi KayitTipi { get; set; }
    public DateTime BildirgeTarihi { get; set; }
    public string PersonelKodu { get; set; } = string.Empty;
    public string PersonelAdi { get; set; } = string.Empty;
    public string? TcKimlikNo { get; set; }
    public PersonelGorev Gorev { get; set; }
    public string? Departman { get; set; }
    public DateTime? IseBaslamaTarihi { get; set; }
    public DateTime? IstenAyrilmaTarihi { get; set; }
    public DateTime? SgkCikisTarihi { get; set; }
    public bool AktifMi { get; set; }
    public string? Notlar { get; set; }
}




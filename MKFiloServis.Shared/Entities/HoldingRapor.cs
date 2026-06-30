namespace MKFiloServis.Shared.Entities;

public class HoldingRapor
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
    public string Tip { get; set; } = "";
    public int Yil { get; set; }
    public int? Ay { get; set; }
    public string? JsonFiltreler { get; set; }
    public string? JsonSonuc { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public string? OlusturanKullanici { get; set; }
}



namespace MKFiloServis.Web.Services.RentACar;

public sealed class RentACarKiralamaRaporSatiri
{
    public string RaporTarihAraligi { get; init; } = string.Empty;
    public string DurumFiltresi { get; init; } = string.Empty;
    public string? SozlesmeNo { get; init; }
    public string Musteri { get; init; } = string.Empty;
    public string Plaka { get; init; } = string.Empty;
    public string Arac { get; init; } = string.Empty;
    public DateTime Baslangic { get; init; }
    public DateTime PlanlananIade { get; init; }
    public DateTime? GercekIade { get; init; }
    public string Durum { get; init; } = string.Empty;
    public decimal PlanlananTutar { get; init; }
    public string OdemeDurumu { get; init; } = string.Empty;
}

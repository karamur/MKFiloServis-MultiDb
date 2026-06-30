using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Models;

public sealed class OperasyonKarRaporuSatir
{
    public string FirmaTipi { get; init; } = string.Empty;
    public AracSahiplikTipi SahiplikTipi { get; init; }
    public string Plaka { get; init; } = string.Empty;
    public string SoforAdi { get; init; } = string.Empty;
    public string GuzergahAdi { get; init; } = string.Empty;
    public string Slot { get; init; } = string.Empty;
    public int SeferSayisi { get; init; }
    public decimal BirimFiyat { get; init; }
    public decimal ToplamGelir { get; init; }
    public int AracId { get; init; }
    public int GuzergahId { get; init; }
}

public sealed class OperasyonKarRaporuOzet
{
    public string Baslik { get; init; } = string.Empty;
    public int AracSayisi { get; init; }
    public int ToplamSefer { get; init; }
    public decimal ToplamGelir { get; init; }
}




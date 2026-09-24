using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.RentACar;

public sealed class RentACarKiralamaSatiri
{
    public int Id { get; init; }
    public int MusteriId { get; init; }
    public int AracId { get; init; }
    public string? SozlesmeNo { get; init; }
    public string MusteriUnvan { get; init; } = string.Empty;
    public string MusteriKimlikNo { get; init; } = string.Empty;
    public string MusteriVergiDairesi { get; init; } = string.Empty;
    public string MusteriVergiNo { get; init; } = string.Empty;
    public string MusteriAdres { get; init; } = string.Empty;
    public string MusteriTelefon { get; init; } = string.Empty;
    public string Plaka { get; init; } = string.Empty;
    public string AracTanimi { get; init; } = string.Empty;
    public int? BaslangicKm { get; init; }
    public int? BitisKm { get; init; }
    public DateTime BaslangicTarihi { get; init; }
    public DateTime PlanlananBitisTarihi { get; init; }
    public DateTime? GercekBitisTarihi { get; init; }
    public KiralamaDurumu Durum { get; init; }
    public decimal ToplamTutar { get; init; }
    public decimal GunlukFiyat { get; init; }
    public decimal? Depozito { get; init; }
    public string? Notlar { get; init; }
    public KiralamaOdemeDurumu OdemeDurumu { get; init; }
    public string FirmaUnvan { get; init; } = string.Empty;
    public string FirmaVergiDairesi { get; init; } = string.Empty;
    public string FirmaVergiNo { get; init; } = string.Empty;
    public string FirmaAdres { get; init; } = string.Empty;
    public string FirmaTelefon { get; init; } = string.Empty;
}

public interface IRentACarKiralamaSorguServisi
{
    Task<IReadOnlyList<RentACarKiralamaSatiri>> GetKiralamalarAsync(CancellationToken cancellationToken = default);
}

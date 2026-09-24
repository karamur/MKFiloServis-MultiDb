using MKFiloServis.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Web.Services.RentACar;

public sealed class RentACarRezervasyonTalebi : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Müşteri seçilmelidir.")]
    public int MusteriId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Araç seçilmelidir.")]
    public int AracId { get; set; }

    public DateTime BaslangicTarihi { get; set; }

    public DateTime PlanlananBitisTarihi { get; set; }

    public decimal GunlukFiyat { get; set; }

    public decimal? Depozito { get; set; }

    [StringLength(500, ErrorMessage = "Not alanı en fazla 500 karakter olabilir.")]
    public string? Notlar { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GunlukFiyat <= 0)
        {
            yield return new ValidationResult(
                "Günlük fiyat sıfırdan büyük olmalıdır.",
                [nameof(GunlukFiyat)]);
        }

        if (Depozito < 0)
        {
            yield return new ValidationResult(
                "Depozito negatif olamaz.",
                [nameof(Depozito)]);
        }

        if (PlanlananBitisTarihi <= BaslangicTarihi)
        {
            yield return new ValidationResult(
                "Planlanan iade zamanı başlangıç zamanından sonra olmalıdır.",
                [nameof(PlanlananBitisTarihi)]);
        }
    }
}

public interface IRentACarRezervasyonServisi
{
    Task<IReadOnlyList<Cari>> GetAktifMusterilerAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Arac>> GetAktifAraclarAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Arac>> GetMusaitAraclarAsync(
        DateTime baslangic,
        DateTime bitis,
        CancellationToken cancellationToken = default);
    Task<MusteriKiralama> RezervasyonOlusturAsync(
        RentACarRezervasyonTalebi talep,
        CancellationToken cancellationToken = default);
    Task RezervasyonGuncelleAsync(
        int kiralamaId,
        RentACarRezervasyonTalebi talep,
        CancellationToken cancellationToken = default);
    Task IptalEtAsync(int kiralamaId, string iptalNedeni, CancellationToken cancellationToken = default);
}

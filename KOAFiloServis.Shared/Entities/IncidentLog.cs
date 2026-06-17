using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Olay/incident kaydı — finansal tutarsızlık, hata, uyarı.
/// Denetim paneli tarafından otomatik oluşturulur.
/// </summary>
public class IncidentLog : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }

    [Required, StringLength(20)]
    public string Level { get; set; } = "Info"; // Info, Warning, Error, Critical

    [Required]
    public string Message { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Entity { get; set; } // "HakedisPuantaj", "Snapshot", "Fatura"

    public int? EntityId { get; set; }

    public decimal? BeklenenDeger { get; set; }
    public decimal? GerceklesenDeger { get; set; }

    public bool Cozuldu { get; set; }
    public DateTime? CozulmeTarihi { get; set; }

    [StringLength(500)]
    public string? CozumNotu { get; set; }
}

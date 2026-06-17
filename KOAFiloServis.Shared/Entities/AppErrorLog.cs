using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Uygulama hata log kaydı. Global exception middleware tarafından otomatik yazılır.
/// Admin panelinde (/admin/errors) görüntülenir.
/// </summary>
public class AppErrorLog : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    [StringLength(500)]
    public string? Path { get; set; }

    [StringLength(100)]
    public string? UserId { get; set; }

    [Required, StringLength(20)]
    public string Severity { get; set; } = "Error"; // Info, Warning, Error, Critical

    public string? RequestPayload { get; set; }

    public bool Cozuldu { get; set; }

    public DateTime? CozulmeTarihi { get; set; }

    [StringLength(200)]
    public string? CozumNotu { get; set; }
}

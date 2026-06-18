using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

/// <summary>Lisans bilgisi. Her kurulum için firma+makine bazlı kilit.</summary>
public class LicenseInfo : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }

    [Required, StringLength(50)]
    public string FirmaKodu { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string MachineId { get; set; } = string.Empty;

    public DateTime ExpireDate { get; set; }

    public int DurationDays { get; set; } // Lisans süresi (gün) — Desktop'ta NumericUpDown ile belirlenir

    public bool IsDemo { get; set; } // Demo lisansı mı?

    [StringLength(20)]
    public string AllowedVersion { get; set; } = "1.0.99"; // Max izin verilen versiyon

    [StringLength(20)]
    public string ContactPhone { get; set; } = string.Empty; // Süre uzatma için iletişim telefonu

    [Required]
    public string Signature { get; set; } = string.Empty; // SHA256 + secret

    public bool IsActive { get; set; } = true;

    public DateTime? LastValidatedAt { get; set; }
}

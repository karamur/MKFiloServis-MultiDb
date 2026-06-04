using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Operasyonel kapasite tanımı.
/// </summary>
/// Kural 4: FirmaId NOT NULL (TenantNullableFirmaId kaldırıldı, DB seviyesinde NOT NULL).
public class Kapasite : BaseEntity, IFirmaTenant
{
    /// <summary>
    /// Tenant: Bu kapasitenin ait olduğu firma. (K3+K4)
    /// </summary>
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Required]
    [StringLength(100)]
    public string KapasiteAdi { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Aciklama { get; set; }

    public decimal Carpan { get; set; } = 1m;
    public bool Aktif { get; set; } = true;
}

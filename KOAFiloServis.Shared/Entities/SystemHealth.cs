namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Sistem sağlık durumu — dashboard ve write protection için.
/// </summary>
public class SystemHealth : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public int Yil { get; set; }
    public int Ay { get; set; }

    public bool IsHealthy { get; set; } = true;
    public int DenetimSkoru { get; set; } = 100; // 0-100
    public int IncidentCount { get; set; }
    public int CriticalCount { get; set; }

    public string? LastError { get; set; }
    public DateTime LastCheck { get; set; } = DateTime.UtcNow;

    /// <summary>100=Sağlıklı, 70-99=Riskli, <70=Kritik</summary>
    public string Status => DenetimSkoru >= 100 ? "Healthy" : DenetimSkoru >= 70 ? "Warning" : "Critical";
}

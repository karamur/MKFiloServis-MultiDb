using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Sistem seviyesinde kilit mekanizması.
/// Rebuild sırasında veri girişini engeller, safe mode yönetir.
/// </summary>
public class SystemLock : BaseEntity
{
    [Required, StringLength(100)]
    public string Key { get; set; } = string.Empty; // "REBUILD", "SAFE_MODE"

    public bool IsLocked { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }

    public DateTime? LockedAt { get; set; }
    public DateTime? UnlockedAt { get; set; }
}



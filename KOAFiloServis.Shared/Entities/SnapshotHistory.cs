using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Snapshot geçmiş kaydı — her güncelleme öncesi eski değerler saklanır.
/// Geri alma (rollback) için kullanılır.
/// </summary>
public class SnapshotHistory : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }

    public int SnapshotId { get; set; }

    public int Yil { get; set; }
    public int Ay { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal HakedisGelirEski { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal HakedisGiderEski { get; set; }

    [StringLength(50)]
    public string IslemTipi { get; set; } = "SnapshotGuncelle"; // "IsleAsync", "Rebuild", "ManualRestore"

    public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Snapshot işlem kaydı — idempotency garantisi.
/// Her finans işlemi için TEK bir kayıt. Aynı IslemId tekrar gelirse skip.
/// </summary>
public class SnapshotTransaction : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }

    /// <summary>Benzersiz işlem ID'si. Mükerrer kontrolü için.</summary>
    [Required]
    public Guid IslemId { get; set; }

    public int Yil { get; set; }
    public int Ay { get; set; }

    /// <summary>"HakedisFinans", "SnapshotGuncelle", "MuhasebeFis"</summary>
    [Required, StringLength(50)]
    public string IslemTipi { get; set; } = string.Empty;

    /// <summary>Gelir delta (pozitif = artış)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal GelirDelta { get; set; }

    /// <summary>Gider delta (pozitif = artış)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal GiderDelta { get; set; }

    /// <summary>İlgili HakedisPuantaj (opsiyonel)</summary>
    public int? HakedisPuantajId { get; set; }

    /// <summary>İlgili Fatura (opsiyonel)</summary>
    public int? FaturaId { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }
}



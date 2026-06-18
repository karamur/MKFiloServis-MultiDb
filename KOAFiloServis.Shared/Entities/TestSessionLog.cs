using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Test oturumu sırasında oluşturulan/değiştirilen kayıtları izler.
/// Test sonunda rollback için kullanılır.
/// </summary>
public class TestSessionLog : BaseEntity
{
    public int SessionId { get; set; }

    [Required, StringLength(100)]
    public string TestTag { get; set; } = string.Empty;

    /// <summary>Hangi entity etkilendi: "HakedisPuantaj", "Fatura", "MuhasebeFis"</summary>
    [Required, StringLength(100)]
    public string EntityAdi { get; set; } = string.Empty;

    public int EntityId { get; set; }

    /// <summary>Insert, Update</summary>
    [StringLength(20)]
    public string IslemTipi { get; set; } = "Insert";

    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

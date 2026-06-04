using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Kurum (Firma) + Güzergah kalıcı eşleştirmesi.
/// Bir kurum birden fazla güzergaha, bir güzergah birden fazla kuruma bağlanabilir (N-N).
/// </summary>
public class FirmaGuzergahEslestirme : BaseEntity, IFirmaTenant
{
    /// <summary>
    /// İşi alan / faturayı kesecek olan kendi firmamız (multi-tenant)
    /// </summary>
    [Required]
    public int FirmaId { get; set; }
    int? IFirmaTenant.FirmaId { get => FirmaId; set => FirmaId = value ?? 0; }
    public virtual Firma? Firma { get; set; }

    /// <summary>
    /// İşin sahibi olan müşteri kurumu (Cari)
    /// </summary>
    [Required]
    public int KurumCariId { get; set; }

    [Required]
    public int GuzergahId { get; set; }

    /// <summary>
    /// Eşleştirmenin geçerli olduğu başlangıç tarihi (opsiyonel)
    /// </summary>
    public DateTime? BaslangicTarihi { get; set; }

    /// <summary>
    /// Eşleştirmenin geçerli olduğu bitiş tarihi (opsiyonel - boş = süresiz)
    /// </summary>
    public DateTime? BitisTarihi { get; set; }

    /// <summary>
    /// Bu kurum-güzergah ilişkisi için sefer başına satış ücreti
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal SeferUcreti { get; set; }

    public int KdvOrani { get; set; } = 20;

    public bool Aktif { get; set; } = true;

    public string? Notlar { get; set; }

    // Navigation
    [ForeignKey(nameof(KurumCariId))]
    public virtual Cari? KurumCari { get; set; }

    [ForeignKey(nameof(GuzergahId))]
    public virtual Guzergah? Guzergah { get; set; }
}

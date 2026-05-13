using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Kurum (Firma) + Araç + Şoför kalıcı eşleştirmesi.
/// Bir defa yapılır, puantajlarda otomatik kullanılır. Çoka-çok ilişki desteklenir
/// (aynı araç-şoför birden fazla kuruma, aynı kurum birden fazla araç-şoföre bağlanabilir).
/// </summary>
public class FirmaAracSoforEslestirme : BaseEntity
{
    /// <summary>
    /// İşi alan / faturayı kesecek olan kendi firmamız (multi-tenant)
    /// </summary>
    [Required]
    public int FirmaId { get; set; }

    /// <summary>
    /// İşin sahibi olan müşteri kurumu (Cari)
    /// </summary>
    [Required]
    public int KurumCariId { get; set; }

    [Required]
    public int AracId { get; set; }

    [Required]
    public int SoforId { get; set; }

    /// <summary>
    /// Eşleştirmenin geçerli olduğu başlangıç tarihi (opsiyonel)
    /// </summary>
    public DateTime? BaslangicTarihi { get; set; }

    /// <summary>
    /// Eşleştirmenin geçerli olduğu bitiş tarihi (opsiyonel - boş = süresiz)
    /// </summary>
    public DateTime? BitisTarihi { get; set; }

    /// <summary>
    /// Bu kurum-araç-şoför ilişkisi için varsayılan birim ücret (sefer başına)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal VarsayilanBirimUcret { get; set; }

    public bool Aktif { get; set; } = true;

    public string? Notlar { get; set; }

    // Navigation
    [ForeignKey(nameof(KurumCariId))]
    public virtual Cari? KurumCari { get; set; }

    [ForeignKey(nameof(AracId))]
    public virtual Arac? Arac { get; set; }

    [ForeignKey(nameof(SoforId))]
    public virtual Sofor? Sofor { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Müşteri / Kurum kartı bilgileri
/// </summary>
public class Kurum : BaseEntity, IKopyalanabilirTenant
{
    /// <summary>Firma kopyalama (K8) audit: kaynak firma Id'si.</summary>
    public int? KaynakFirmaId { get; set; }
    /// <summary>Firma kopyalama (K8) audit: kaynak kayıt Id'si.</summary>
    public int? KaynakKayitId { get; set; }

    /// <summary>
    /// Tenant: Bu kurum kartının ait olduğu firma. (K3+K4)
    /// Nullable: Aşama C "doldur" adımında varsayılan firma ile güncellenir; ardından NOT NULL.
    /// </summary>
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Required]
    [StringLength(50)]
    public string KurumKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string KurumAdi { get; set; } = string.Empty;

    [StringLength(250)]
    public string? UnvanTam { get; set; }

    [StringLength(11)]
    public string? VergiNo { get; set; }

    [StringLength(100)]
    public string? VergiDairesi { get; set; }

    [StringLength(500)]
    public string? Adres { get; set; }

    [StringLength(100)]
    public string? Il { get; set; }

    [StringLength(100)]
    public string? Ilce { get; set; }

    [StringLength(20)]
    public string? Telefon { get; set; }

    [StringLength(20)]
    public string? Telefon2 { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? WebSite { get; set; }

    [StringLength(100)]
    public string? YetkiliKisi { get; set; }

    [StringLength(100)]
    public string? YetkiliTelefon { get; set; }

    [StringLength(100)]
    public string? YetkiliEmail { get; set; }

    [StringLength(2000)]
    public string? Notlar { get; set; }

    public bool Aktif { get; set; } = true;

    /// <summary>
    /// Eğer muhasebe tarafında bir Cari ile eşleşiyorsa
    /// </summary>
    public int? CariId { get; set; }
    public virtual Cari? Cari { get; set; }
}

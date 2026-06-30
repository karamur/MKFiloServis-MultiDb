using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Onaylanmış PuantajKayit'ın finansal snapshot'ı.
/// Fatura üretimi için köprü görevi görür.
/// Kilitli hesap döneminde oluşturulur, fiyat değişse bile korunur.
/// </summary>
public class PuantajFinansalKayit : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int PuantajKayitId { get; set; }
    public virtual PuantajKayit? PuantajKayit { get; set; }

    public int HesapDonemiId { get; set; }
    public virtual PuantajHesapDonemi? HesapDonemi { get; set; }

    // ── Snapshot (onay anında dondurulur) ──────────────────────────────
    [Column(TypeName = "decimal(18,2)")]
    public decimal BirimGelir { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal BirimGider { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamGelir { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamGider { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal KdvTutar { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal GenelToplam { get; set; }
    public int SeferGunu { get; set; }

    // ── Cari bağlantısı ────────────────────────────────────────────────
    public int? GelirCariId { get; set; }
    public int? GiderCariId { get; set; }

    // ── Fatura bağlantısı (üretilince doldurulur) ──────────────────────
    public int? GelirFaturaId { get; set; }
    public int? GiderFaturaId { get; set; }

    public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;
    public PuantajFinansalDurum Durum { get; set; } = PuantajFinansalDurum.Bekliyor;

    // Audit
    [StringLength(100)] public string? CreatedBy { get; set; }
    [StringLength(100)] public string? UpdatedBy { get; set; }
}

public enum PuantajFinansalDurum
{
    Bekliyor = 0,
    GelirFaturasiUretildi = 1,
    GiderFaturasiUretildi = 2,
    TumFaturalarUretildi = 3,
    Iptal = 4
}



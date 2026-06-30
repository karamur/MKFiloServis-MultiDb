using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Puantaj onay/hesap aksiyonlarının audit log kaydı.
/// Her durum geçişinde otomatik oluşturulur.
/// </summary>
public class PuantajAuditLog : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }
    public int? HesapDonemiId { get; set; }

    public PuantajAuditAksiyon Aksiyon { get; set; }

    [StringLength(100)]
    public string? Kullanici { get; set; }
    public DateTime AksiyonTarihi { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? OncekiDurum { get; set; }
    [StringLength(100)]
    public string? YeniDurum { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }
}

public enum PuantajAuditAksiyon
{
    Hesaplandi = 1,
    FinansOnaylandi = 2,
    MuhasebeOnaylandi = 3,
    Kilitlendi = 4,
    KilitAcildi = 5,
    RevizyonYapildi = 6,
    IptalEdildi = 7
}



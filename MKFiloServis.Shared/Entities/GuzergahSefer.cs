namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Bir güzergaha bağlı sefer detay satırı.
/// Güzergah listesinde "Sefer Detayları" panelinden girilen verileri kalıcı olarak saklar.
/// </summary>
public class GuzergahSefer : BaseEntity, IFirmaTenant
{
    /// <summary>
    /// Tenant: ait olduğu firma. Parent <see cref="Guzergah"/> ile aynı firmaya ait olmalıdır.
    /// (Bkz: TENANT_MIGRATION_PLAN.md — EF Core warning 10622'nin kapatılması için eklendi.)
    /// </summary>
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int GuzergahId { get; set; }
    public virtual Guzergah? Guzergah { get; set; }

    /// <summary>Satır sırası (1. sefer, 2. sefer ...)</summary>
    public int Sira { get; set; }

    public SeferTipi SeferTipi { get; set; } = SeferTipi.SabahAksam;

    /// <summary>Operasyonel slot (Sabah, Aksam, Mesai, Diger1-5)</summary>
    public SeferSlot Slot { get; set; } = SeferSlot.Sabah;

    /// <summary>Kapasite tablosundan gelen ad ("16+1" gibi)</summary>
    public string? KapasiteAdi { get; set; }

    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    public string? SoforAd { get; set; }
    public string? SoforTelefon { get; set; }

    /// <summary>
    /// Tedarikçi/firma adı (serbest metin). Eskiden <c>Firma</c> olarak adlandırılıyordu;
    /// <see cref="IFirmaTenant"/> navigation çakışması nedeniyle <c>FirmaAdiSerbest</c>'e taşındı.
    /// DB kolonu eskiden olduğu gibi <c>Firma</c> kalır (uyumluluk için).
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.Column("Firma")]
    public string? FirmaAdiSerbest { get; set; }
}



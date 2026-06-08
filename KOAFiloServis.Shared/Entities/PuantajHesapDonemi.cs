using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Puantaj hesaplama döngüsü / batch.
/// Her hesaplama yeni bir HesapDonemi oluşturur.
/// Versiyon zinciri ile revizyon geçmişi tutulur.
/// </summary>
public class PuantajHesapDonemi : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int Yil { get; set; }
    public int Ay { get; set; }
    public int? KurumId { get; set; }

    /// <summary>Aynı dönem için kaçıncı hesaplama (1, 2, 3...)</summary>
    public int Versiyon { get; set; } = 1;

    public PuantajHesapDurum Durum { get; set; } = PuantajHesapDurum.Taslak;

    [StringLength(100)]
    public string? HesaplayanKullanici { get; set; }
    public DateTime HesaplamaTarihi { get; set; } = DateTime.UtcNow;

    /// <summary>Self-FK: önceki hesap dönemi (revizyon zinciri)</summary>
    public int? OncekiDonemId { get; set; }
    public virtual PuantajHesapDonemi? OncekiDonem { get; set; }

    [StringLength(500)]
    public string? Notlar { get; set; }

    // ── Onay Workflow (Sprint 5) ──────────────────────────────────────
    public PuantajDonemOnayDurum OnayDurum { get; set; } = PuantajDonemOnayDurum.Bekliyor;

    [StringLength(100)] public string? FinansOnaylayan { get; set; }
    public DateTime? FinansOnayTarihi { get; set; }

    [StringLength(100)] public string? MuhasebeOnaylayan { get; set; }
    public DateTime? MuhasebeOnayTarihi { get; set; }

    public DateTime? KilitTarihi { get; set; }
    [StringLength(100)] public string? KilitAciklama { get; set; }

    // Audit
    [StringLength(100)] public string? CreatedBy { get; set; }
    [StringLength(100)] public string? UpdatedBy { get; set; }
    // DeletedAt + DeletedBy artık BaseEntity'den miras alınır (Kural 16)
    [StringLength(100)] public new string? DeletedBy { get; set; }

    // Navigation
    public virtual ICollection<PuantajDetay> Detaylar { get; set; } = new List<PuantajDetay>();

    public virtual ICollection<PuantajKayit> PuantajKayitlari { get; set; } = new List<PuantajKayit>();
}

public enum PuantajHesapDurum
{
    Taslak = 0,
    Aktif = 1,
    Superseded = 2,
    Iptal = 3
}

public enum PuantajDonemOnayDurum
{
    Bekliyor = 0,
    FinansOnaylandi = 1,
    MuhasebeOnaylandi = 2,
    Kilitli = 3
}

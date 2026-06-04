using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Günlük ham operasyon kaydı.
/// PuantajKayit bu kayıtlardan PuantajEngine tarafından türetilir.
/// </summary>
public class OperasyonKaydi : BaseEntity, IFirmaTenant
{
    // Tenant
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    // Tarih
    public DateTime Tarih { get; set; }

    // Güzergah / Araç / Şoför
    public int GuzergahId { get; set; }
    public virtual Guzergah? Guzergah { get; set; }

    public int AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    public int? SoforId { get; set; }
    public virtual Sofor? Sofor { get; set; }

    // Slot / Yön
    public SeferSlot Slot { get; set; } = SeferSlot.Sabah;
    [StringLength(50)]
    public string? SlotAdi { get; set; }
    public PuantajYon Yon { get; set; } = PuantajYon.SabahAksam;

    // Kurum
    public int? KurumId { get; set; }
    public virtual Kurum? Kurum { get; set; }

    public int? IsverenFirmaId { get; set; }
    public virtual Firma? IsverenFirma { get; set; }

    // Sefer Bilgisi
    public int SeferSayisi { get; set; } = 1;
    [Column(TypeName = "decimal(10,2)")]
    public decimal PuantajCarpani { get; set; } = 1.0m;

    // Operasyon Durumu
    public OperasyonDurumu OperasyonDurumu { get; set; } = OperasyonDurumu.Gitti;

    // Kaynak / Finans
    public PlanlamaKaynakTipi KaynakTipi { get; set; } = PlanlamaKaynakTipi.Kendi;
    public PlanlamaFinansYonu FinansYonu { get; set; } = PlanlamaFinansYonu.Giden;
    public SoforOdemeTipi SoforOdemeTipi { get; set; } = SoforOdemeTipi.Ozmal;

    // Ödeme / Fatura
    public int? OdemeYapilacakCariId { get; set; }
    public virtual Cari? OdemeYapilacakCari { get; set; }

    public int? FaturaKesiciCariId { get; set; }
    public virtual Cari? FaturaKesiciCari { get; set; }

    // Referans
    [StringLength(50)]
    public string? BelgeNo { get; set; }
    [StringLength(50)]
    public string? TransferDurum { get; set; }

    // Kaynak Takip
    public PuantajKaynak Kaynak { get; set; } = PuantajKaynak.Manuel;
    public int? ExcelImportId { get; set; }
    public int? ExcelSatirNo { get; set; }

    // ── Puantaj Sync ───────────────────────────────────────────────────────
    public int? KaynakPuantajId { get; set; }
    public bool KullaniciKilitliMi { get; set; }
    public DateTime? KilitTarihi { get; set; }
    public int? KilitleyenKullaniciId { get; set; }

    // Yardımcı property'ler (Tarih'ten türetilir)
    public int Yil => Tarih.Year;
    public int Ay => Tarih.Month;
    public int Gun => Tarih.Day;

    // ── Audit ──────────────────────────────────────────────────────────────
    [StringLength(100)]
    public string? CreatedBy { get; set; }
    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    // ── Soft Delete ────────────────────────────────────────────────────────
    // DeletedAt artık BaseEntity'den miras alınır (Kural 16)
    [StringLength(100)]
    public string? DeletedBy { get; set; }

    // Diğer
    [StringLength(1000)]
    public string? Notlar { get; set; }
}

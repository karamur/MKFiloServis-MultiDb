using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Güzergah bilgileri (Firma bazlı)
/// </summary>
/// Kural 4: FirmaId NOT NULL (TenantNullableFirmaId kaldırıldı, DB seviyesinde NOT NULL).
public class Guzergah : BaseEntity, IKopyalanabilirTenant, IFirmaTenant
{
    /// <summary>Firma kopyalama (K8) audit: kaynak firma Id'si.</summary>
    public int? KaynakFirmaId { get; set; }
    /// <summary>Firma kopyalama (K8) audit: kaynak kayıt Id'si.</summary>
    public int? KaynakKayitId { get; set; }

    /// <summary>
    /// Tenant: Bu güzergahın ait olduğu firma. (K3+K4)
    /// </summary>
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public string GuzergahKodu { get; set; } = string.Empty;
    public string GuzergahAdi { get; set; } = string.Empty;
    public string? BaslangicNoktasi { get; set; }
    public string? BitisNoktasi { get; set; }

    // Harita Koordinatları - Başlangıç
    public double? BaslangicLatitude { get; set; }
    public double? BaslangicLongitude { get; set; }

    // Harita Koordinatları - Bitiş
    public double? BitisLatitude { get; set; }
    public double? BitisLongitude { get; set; }

    // Rota Rengi (Harita gösterimi için)
    public string? RotaRengi { get; set; } = "#3388ff";

    public decimal BirimFiyat { get; set; }
    public decimal GiderFiyat { get; set; }
    public decimal PuantajCarpani { get; set; } = 1.0m;

    // UI uyumluluğu: Gelir fiyatı, mevcut BirimFiyat alanını kullanır.
    [NotMapped]
    public decimal GelirFiyat
    {
        get => BirimFiyat;
        set => BirimFiyat = value;
    }
    public decimal? Mesafe { get; set; } // km
    public int? TahminiSure { get; set; } // dakika
    public bool Aktif { get; set; } = true;
    public string? Notlar { get; set; }

    // Sefer Tipi (Sabah, Akşam, Sabah-Akşam, Saatlik)
    public SeferTipi SeferTipi { get; set; } = SeferTipi.SabahAksam;

    // Personel Sayısı (sayısal değer, hesaplamalar için)
    public int PersonelSayisi { get; set; } = 0;

    // Kapasite Adı (Kapasite tablosundan gelen serbest metin: "16+1", "27+1" vb.)
    public string? KapasiteAdi { get; set; }

    // Varsayılan Araç ve Şoför
    public int? VarsayilanAracId { get; set; }
    public virtual Arac? VarsayilanArac { get; set; }

    public int? VarsayilanSoforId { get; set; }
    public virtual Sofor? VarsayilanSofor { get; set; }

    // Fatura Kalem İlişkisi (Hangi fatura kaleminden oluşturuldu)
    public int? FaturaKalemId { get; set; }

    // Foreign Key - Cari (eski uyumluluk için)
    public int CariId { get; set; }

    // Kurum / Müşteri Kartı İlişkisi (opsiyonel, güzergahın bağlı olduğu kurum)
    public int? KurumId { get; set; }
    public virtual Kurum? Kurum { get; set; }

    // Navigation Properties
    public virtual Cari Cari { get; set; } = null!;
    public virtual ICollection<ServisCalisma> ServisCalismalari { get; set; } = new List<ServisCalisma>();
    public virtual ICollection<AracMasraf> AracMasraflari { get; set; } = new List<AracMasraf>();
    public virtual ICollection<FiloGuzergahEslestirme> AracEslestirmeleri { get; set; } = new List<FiloGuzergahEslestirme>();
}

/// <summary>
/// Sefer tipi
/// </summary>
public enum SeferTipi
{
    Sabah = 1,
    Aksam = 2,
    SabahAksam = 3,
    Saatlik = 4,
    Mesai = 5,
    Vardiya = 6
}

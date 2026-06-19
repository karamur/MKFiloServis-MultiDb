using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

// ══════════════════════════════════════════════
// ENUMS
// ══════════════════════════════════════════════

public enum PuantajFaturaHazirlikDurum
{
    Taslak = 0,
    Onaylandi = 1,
    Faturalasti = 2,
    Iptal = 3
}

// ══════════════════════════════════════════════
// ANA KAYIT
// ══════════════════════════════════════════════

/// <summary>
/// Puantaj verisinden üretilen fatura hazırlık başlığı.
/// PuantajKayit (B1) tabanlıdır, manuel düzeltme imkanı sunar.
/// Onay → Faturalasti akışı ile faturaya bağlanır.
/// </summary>
public class PuantajFaturaHazirlik : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int Yil { get; set; }
    public int Ay { get; set; }

    /// <summary>Opsiyonel kurum filtresi (null = tüm kurumlar).</summary>
    public int? KurumId { get; set; }

    /// <summary>Kullanılan ağaç gruplama yapısı.</summary>
    public PuantajFaturaAgacYapisi AgacYapisi { get; set; } = PuantajFaturaAgacYapisi.CariAracGuzergah;

    /// <summary>Gelir veya Gider yönü.</summary>
    public PuantajFaturaYonu FaturaYonu { get; set; } = PuantajFaturaYonu.Gelir;

    /// <summary>Durum: Taslak → Onaylandi → Faturalasti.</summary>
    public PuantajFaturaHazirlikDurum Durum { get; set; } = PuantajFaturaHazirlikDurum.Taslak;

    [StringLength(200)]
    public string? Aciklama { get; set; }

    // ── Özet tutarlar (satırlardan hesaplanır) ──

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToplamGelir { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToplamGider { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToplamKdv { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToplamKesinti { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetTutar { get; set; }

    public int ToplamSefer { get; set; }
    public int SatirSayisi { get; set; }

    // ── Onay / Fatura bağlantısı ──

    [StringLength(100)]
    public string? OnaylayanKullanici { get; set; }

    public DateTime? OnayTarihi { get; set; }

    /// <summary>Bağlanan ilk fatura (toplu fatura ise ayrı ayrı satırlarda).</summary>
    public int? FaturaId { get; set; }

    /// <summary>Oluşturan kullanıcı.</summary>
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    // ── Navigation ──

    public virtual ICollection<PuantajFaturaHazirlikSatir> Satirlar { get; set; } = new List<PuantajFaturaHazirlikSatir>();
}

// ══════════════════════════════════════════════
// SATIR KAYIT
// ══════════════════════════════════════════════

/// <summary>
/// Fatura hazırlık satırı — her biri bir PuantajKayit'a karşılık gelir.
/// Manuel düzeltme yapılabilir (ek kalem, fiyat değişikliği, silme).
/// </summary>
public class PuantajFaturaHazirlikSatir : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    /// <summary>Bağlı olduğu hazırlık başlığı.</summary>
    public int HazirlikId { get; set; }
    public virtual PuantajFaturaHazirlik? Hazirlik { get; set; }

    /// <summary>Kaynak PuantajKayit (manuel satır ise null).</summary>
    public int? PuantajKayitId { get; set; }

    // ── Denormalize referans alanları ──

    public int? KurumId { get; set; }
    public int? CariId { get; set; }
    public int? AracId { get; set; }
    public int? GuzergahId { get; set; }
    public int? SoforId { get; set; }

    [StringLength(50)]
    public string? Plaka { get; set; }

    [StringLength(150)]
    public string? SoforAdi { get; set; }

    [StringLength(30)]
    public string? Telefon { get; set; }

    [StringLength(300)]
    public string? CariUnvan { get; set; }

    [StringLength(300)]
    public string? GuzergahAdi { get; set; }

    // ── Günlük sefer değerleri (PuantajKayit'tan kopya) ──

    public int Gun01 { get; set; } public int Gun02 { get; set; } public int Gun03 { get; set; }
    public int Gun04 { get; set; } public int Gun05 { get; set; } public int Gun06 { get; set; }
    public int Gun07 { get; set; } public int Gun08 { get; set; } public int Gun09 { get; set; }
    public int Gun10 { get; set; } public int Gun11 { get; set; } public int Gun12 { get; set; }
    public int Gun13 { get; set; } public int Gun14 { get; set; } public int Gun15 { get; set; }
    public int Gun16 { get; set; } public int Gun17 { get; set; } public int Gun18 { get; set; }
    public int Gun19 { get; set; } public int Gun20 { get; set; } public int Gun21 { get; set; }
    public int Gun22 { get; set; } public int Gun23 { get; set; } public int Gun24 { get; set; }
    public int Gun25 { get; set; } public int Gun26 { get; set; } public int Gun27 { get; set; }
    public int Gun28 { get; set; } public int Gun29 { get; set; } public int Gun30 { get; set; }
    public int Gun31 { get; set; }

    public int ToplamGun { get; set; }
    public int ToplamSefer { get; set; }

    // ── Finansal alanlar ──

    [Column(TypeName = "decimal(18,4)")]
    public decimal BirimGelir { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToplamGelir { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BirimGider { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToplamGider { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Kdv10Tutar { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Kdv20Tutar { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal KesintiTutar { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TahsilEdilecek { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Odenecek { get; set; }

    // ── Manuel düzeltme ──

    /// <summary>Bu satır manuel mi eklendi?</summary>
    public bool ManuelDuzeltmeMi { get; set; }

    /// <summary>Manuel değişiklik öncesi orijinal tutar (varsa).</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? OrijinalTutar { get; set; }

    [StringLength(500)]
    public string? DuzeltmeAciklamasi { get; set; }

    // ── Fatura bağlantısı ──

    /// <summary>Bu satırdan üretilen fatura.</summary>
    public int? FaturaId { get; set; }
}

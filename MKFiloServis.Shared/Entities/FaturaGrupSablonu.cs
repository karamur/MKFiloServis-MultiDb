using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

// ══════════════════════════════════════════════
// ENUMLAR (Shared — hem entity hem Web modelleri kullanır)
// ══════════════════════════════════════════════

/// <summary>Fatura hazırlık raporunda gelir/gider yön filtresi.</summary>
public enum PuantajFaturaYonu
{
    Tumu = 0,
    Gelir = 1,
    Gider = 2
}

/// <summary>Fatura hazırlık raporu için ağaç gruplama yapısı.</summary>
public enum PuantajFaturaAgacYapisi
{
    CariAracGuzergah = 1,
    KurumAracGuzergah = 2,
    TedarikciAracGuzergah = 3,
    KurumGuzergahArac = 4
}

// ══════════════════════════════════════════════
// ENTITY
// ══════════════════════════════════════════════

/// <summary>
/// Kullanıcının puantaj-fatura raporu için kaydettiği ağaç gruplama şablonu.
/// Her kullanıcı birden fazla şablon kaydedebilir, birini varsayılan yapabilir.
/// </summary>
public class FaturaGrupSablonu : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    /// <summary>Şablonun görünen adı (örn: "Ustun Grup Aylik", "Recep Ustun Gelir")</summary>
    [StringLength(150)]
    public string Ad { get; set; } = null!;

    /// <summary>Ağaç gruplama yapısı</summary>
    public PuantajFaturaAgacYapisi AgacYapisi { get; set; } = PuantajFaturaAgacYapisi.CariAracGuzergah;

    /// <summary>Kullanıcı için varsayılan şablon mu?</summary>
    public bool VarsayilanMi { get; set; }

    /// <summary>Kullanıcı bazlı şablon (null = firma geneli)</summary>
    public int? KullaniciId { get; set; }
}



using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

// ══════════════════════════════════════════════
// ENUMS
// ══════════════════════════════════════════════

/// <summary>İstisna tipi — ne tür bir istisna oluştu?</summary>
public enum IstisnaTipi
{
    EkSefer = 1,       // Normal servis dışında ek sefer/özel servis
    AracDegisimi = 2,  // Araç arıza/bakım nedeniyle farklı araç gönderildi
    TaksiFisi = 3,     // Araç gelmedi, personel taksiye bindi
    Diger = 4          // Diğer istisnalar
}

/// <summary>Kullanıcının istisnaya verdiği karar.</summary>
public enum KararTipi
{
    Ceza = 1,          // Ceza uygula — tedarikçiden kes
    Masraf = 2,        // Masraf olarak işle — gider yaz
    GozArdi = 3,       // Göz ardı et — finansal etkisi yok
    SadeceNot = 4      // Sadece not olarak sakla — bilgi amaçlı
}

// ══════════════════════════════════════════════
// ENTITY
// ══════════════════════════════════════════════

/// <summary>
/// Puantaj istisna kaydı.
/// Bir PuantajKayit'a bağlıdır, isteğe bağlı OperasyonKaydi'ye de bağlanabilir.
/// Kullanıcı manuel girer. Onaylı puantajda değiştirilemez.
/// </summary>
public class PuantajIstisna : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    /// <summary>Hangi puantaj kaydına ait.</summary>
    public int PuantajKayitId { get; set; }
    public virtual PuantajKayit? PuantajKayit { get; set; }

    /// <summary>Hangi günlük operasyon kaydından kaynaklandı (opsiyonel).</summary>
    public int? OperasyonKaydiId { get; set; }
    public virtual OperasyonKaydi? OperasyonKaydi { get; set; }

    /// <summary>Ayın hangi günü (1-31).</summary>
    public int Gun { get; set; }

    /// <summary>İstisna tipi.</summary>
    public IstisnaTipi IstisnaTipi { get; set; } = IstisnaTipi.Diger;

    /// <summary>Kullanıcının verdiği karar.</summary>
    public KararTipi KararTipi { get; set; } = KararTipi.SadeceNot;

    /// <summary>Ceza/masraf/taksi tutarı (finansal etkisi olan kararlar için).</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal Tutar { get; set; }

    /// <summary>Açıklama / not.</summary>
    [StringLength(500)]
    public string? Aciklama { get; set; }

    // ── Araç değişimi için ──

    /// <summary>Asıl çalışması planlanan araç (arıza/bakım nedeniyle çalışmadı).</summary>
    public int? EskiAracId { get; set; }
    public virtual Arac? EskiArac { get; set; }

    /// <summary>Fiilen çalışan yedek araç.</summary>
    public int? YeniAracId { get; set; }
    public virtual Arac? YeniArac { get; set; }

    // ── Taksi fişi için ──

    /// <summary>Taksi fiş numarası.</summary>
    [StringLength(50)]
    public string? FisNo { get; set; }

    // ── Faturaya yansıtma ──

    /// <summary>Bu istisna hangi faturaya yansıtıldı (varsa).</summary>
    public int? FaturaId { get; set; }
}

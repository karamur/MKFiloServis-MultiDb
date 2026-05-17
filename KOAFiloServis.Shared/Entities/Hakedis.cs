using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Hakediş başlığı. Belirli bir dönem (Yıl/Ay) ve referans (Kurum/Tedarikçi/Araç) için
/// FiloGunlukPuantaj + ServisPuantaj kayıtlarından toplulaştırılarak üretilir.
/// Onaylandıktan sonra Gelir veya Gider faturasına dönüştürülebilir.
/// </summary>
public class Hakedis : BaseEntity, IFirmaTenant
{
    /// <summary>K1: Aktif firma (tenant). C3 backfill sonrası NOT NULL'a alınır.</summary>
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Obsolete("K1: Legacy Sirket alanı. FirmaId'ye geçildi. Veri taşıma sonrası kaldırılacak.")]
    public int? SirketId { get; set; }
    [Obsolete("K1: Legacy Sirket navigation. FirmaId/Firma kullanılacak.")]
    public virtual Sirket? Sirket { get; set; }

    [Required]
    public int Yil { get; set; }

    [Required]
    public int Ay { get; set; }

    [Required]
    public HakedisTipi Tip { get; set; } = HakedisTipi.Kurum;

    /// <summary>
    /// Tip'e göre referans:
    /// - Kurum     -> Cari.Id (kurum cari)
    /// - Tedarikci -> TasimaTedarikci.Id
    /// - Arac      -> Arac.Id
    /// </summary>
    public int ReferansId { get; set; }

    /// <summary>Toplam sefer / çalışma sayısı</summary>
    public decimal ToplamSeferSayisi { get; set; }

    /// <summary>Snapshot birim fiyat (ortalama veya kontrat birim fiyatı)</summary>
    public decimal BirimFiyat { get; set; }

    /// <summary>KDV hariç tutar</summary>
    public decimal Tutar { get; set; }

    public decimal KdvOran { get; set; } = 20m;
    public decimal KdvTutar { get; set; }
    public decimal GenelToplam { get; set; }

    public HakedisDurum Durum { get; set; } = HakedisDurum.Taslak;

    /// <summary>Üretim parametreleri (filtre/dönem) – JSON</summary>
    public string? GenerationParams { get; set; }

    public string? OnaylayanKisi { get; set; }
    public DateTime? OnayTarihi { get; set; }

    /// <summary>Hakediş onaylanıp faturaya dönüştürüldüyse Fatura.Id</summary>
    public int? FaturaId { get; set; }
    public virtual Fatura? Fatura { get; set; }

    public string? Notlar { get; set; }

    public virtual ICollection<HakedisDetay> Detaylar { get; set; } = new List<HakedisDetay>();
}

/// <summary>
/// Hakediş detay satırı – her bir gün/sefer kaydını saklar.
/// Hakediş PDF'inin kalemlerini ve denetim izini sağlar.
/// </summary>
public class HakedisDetay : BaseEntity, IFirmaTenant
{
    /// <summary>K1: Aktif firma (tenant). Parent Hakedis ile aynı firmaya bağlanır.</summary>
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Required]
    public int HakedisId { get; set; }
    public virtual Hakedis? Hakedis { get; set; }

    public DateTime Tarih { get; set; }
    public ServisTuru ServisTuru { get; set; }

    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    public int? SoforId { get; set; }
    public virtual Sofor? Sofor { get; set; }

    public int? GuzergahId { get; set; }
    public virtual Guzergah? Guzergah { get; set; }

    /// <summary>Kaynak günlük puantaj kaydı (varsa)</summary>
    public int? FiloGunlukPuantajId { get; set; }
    public virtual FiloGunlukPuantaj? FiloGunlukPuantaj { get; set; }

    public decimal SeferSayisi { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal Tutar { get; set; }

    public string? Aciklama { get; set; }
}

public enum HakedisTipi
{
    /// <summary>Kuruma kesilecek gelir hakedişi</summary>
    Kurum = 1,
    /// <summary>Tedarikçiye ödenecek gider hakedişi</summary>
    Tedarikci = 2,
    /// <summary>Araç bazında iç maliyet/karlılık hakedişi (faturalanmaz)</summary>
    Arac = 3
}

public enum HakedisDurum
{
    Taslak = 0,
    Onaylandi = 1,
    Faturalandi = 2,
    TahsilEdildi = 3,
    Odendi = 4,
    Iptal = 5
}

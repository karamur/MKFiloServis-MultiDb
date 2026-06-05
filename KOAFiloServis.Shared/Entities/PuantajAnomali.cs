using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Puantaj verilerinde tespit edilen anomali kaydı.
/// Hem kural-tabanlı hem de AI destekli tespitler bu tabloda saklanır.
/// </summary>
public class PuantajAnomali : BaseEntity, IFirmaTenant
{
    /// <summary>Anomali tipi</summary>
    public PuantajAnomaliTipi AnomaliTipi { get; set; }

    /// <summary>Tespit yöntemi</summary>
    public PuantajAnomaliTespitYontemi TespitYontemi { get; set; } = PuantajAnomaliTespitYontemi.Kural;

    /// <summary>Önem seviyesi: 1=Düşük, 2=Orta, 3=Yüksek, 4=Kritik</summary>
    public int OnemSeviyesi { get; set; } = 2;

    /// <summary>İlgili firma</summary>
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    /// <summary>İlgili puantaj kaydı</summary>
    public int? PuantajKayitId { get; set; }
    public virtual PuantajKayit? PuantajKayit { get; set; }

    /// <summary>İlgili dönem</summary>
    public int Yil { get; set; }
    public int Ay { get; set; }

    /// <summary>Anomali başlığı (kısa açıklama)</summary>
    [Required, MaxLength(250)]
    public string Baslik { get; set; } = string.Empty;

    /// <summary>Detaylı açıklama (AI tarafından oluşturulabilir)</summary>
    [MaxLength(2000)]
    public string? Aciklama { get; set; }

    /// <summary>Anomaliye konu olan değer (JSON formatında)</summary>
    [Column(TypeName = "jsonb")]
    public string? AnomaliDetay { get; set; }

    /// <summary>AI tarafından üretilen öneri/aksiyon</summary>
    [MaxLength(2000)]
    public string? Oneri { get; set; }

    /// <summary>AI güven skoru (0-100, sadece AI tespitlerinde)</summary>
    public int? GuvenSkoru { get; set; }

    /// <summary>Tespit tarihi</summary>
    public DateTime TespitTarihi { get; set; } = DateTime.UtcNow;

    /// <summary>Çözüm durumu</summary>
    public PuantajAnomaliCozumDurumu CozumDurumu { get; set; } = PuantajAnomaliCozumDurumu.Bekliyor;

    /// <summary>Çözüm tarihi</summary>
    public DateTime? CozumTarihi { get; set; }

    /// <summary>Çözümü yapan kullanıcı</summary>
    [MaxLength(100)]
    public string? CozenKullanici { get; set; }

    /// <summary>Çözüm açıklaması</summary>
    [MaxLength(500)]
    public string? CozumAciklamasi { get; set; }
}

/// <summary>
/// Anomali tipleri
/// </summary>
public enum PuantajAnomaliTipi
{
    /// <summary>BirimGelir veya BirimGider sıfır</summary>
    SifirTutar = 1,

    /// <summary>BirimGelir &lt; BirimGider (zarar eden sefer)</summary>
    NegatifMarj = 2,

    /// <summary>Birim fiyat rota ortalamasının 3 katından fazla</summary>
    AsiriYuksekFiyat = 3,

    /// <summary>Birim fiyat rota ortalamasının 1/3'ünden az</summary>
    AsiriDusukFiyat = 4,

    /// <summary>Aynı rota+araç+slot için mükerrer kayıt</summary>
    MukerrerKayit = 5,

    /// <summary>Gun değeri Gun01..Gun31 toplamıyla uyuşmuyor</summary>
    GunTutarsizligi = 6,

    /// <summary>Gelir/Gider ödemesi gecikmiş</summary>
    OdemeGecikmesi = 7,

    /// <summary>Aynı veri hem Excel hem Operasyon kaynağından gelmiş</summary>
    KaynakCakismasi = 8,

    /// <summary>AI tarafından tespit edilen pattern anomalisi</summary>
    AIPatternAnomalisi = 9,

    /// <summary>Toplam gelir/gider tutarında olağandışı değişim (aylar arası)</summary>
    AsiriDegisim = 10,
}

/// <summary>
/// Anomali tespit yöntemi
/// </summary>
public enum PuantajAnomaliTespitYontemi
{
    /// <summary>Kural-tabanlı (SQL/istatistiksel eşik)</summary>
    Kural = 1,

    /// <summary>AI/LLM destekli pattern analizi</summary>
    AI = 2,

    /// <summary>Hem kural hem AI</summary>
    Hibrit = 3,
}

/// <summary>
/// Anomali çözüm durumu
/// </summary>
public enum PuantajAnomaliCozumDurumu
{
    Bekliyor = 1,
    Inceleniyor = 2,
    Cozuldu = 3,
    YanlisPozitif = 4,
    KabulEdildi = 5, // Anomali doğru ama şimdilik kabul edildi
}

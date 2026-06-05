namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Araç bakım periyot tanımı — her araç için km veya gün bazlı bakım kuralları
/// </summary>
public class BakimPeriyot : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;

    /// <summary>Yağ değişimi, filtre, fren balataları vb.</summary>
    public string BakimAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    // --- Km bazlı periyot ---
    /// <summary>Periyot km aralığı (örn: 10000 km'de bir)</summary>
    public int? PeriyotKm { get; set; }

    /// <summary>Son bakımın yapıldığı km</summary>
    public int? SonBakimKm { get; set; }

    /// <summary>Son bakım tarihi</summary>
    public DateTime? SonBakimTarihi { get; set; }

    // --- Gün bazlı periyot ---
    /// <summary>Periyot gün aralığı (örn: 180 günde bir)</summary>
    public int? PeriyotGun { get; set; }

    // --- Uyarı eşikleri ---
    /// <summary>Kaç km kala uyarı verilsin (varsayılan: 500)</summary>
    public int UyariKmEsigi { get; set; } = 500;

    /// <summary>Kaç gün kala uyarı verilsin (varsayılan: 14)</summary>
    public int UyariGunEsigi { get; set; } = 14;

    public bool Aktif { get; set; } = true;

    // Navigation
    public virtual ICollection<AracBakimUyari> BakimUyarilari { get; set; } = new List<AracBakimUyari>();

    // --- Hesaplanan özellikler ---

    /// <summary>Bir sonraki bakım km'si</summary>
    public int? SonrakiBakimKm => SonBakimKm.HasValue && PeriyotKm.HasValue
        ? SonBakimKm.Value + PeriyotKm.Value
        : null;

    /// <summary>Bir sonraki bakım tarihi (gün bazlı)</summary>
    public DateTime? SonrakiBakimTarihi => SonBakimTarihi.HasValue && PeriyotGun.HasValue
        ? SonBakimTarihi.Value.AddDays(PeriyotGun.Value)
        : null;

    /// <summary>Mevcut araç km'sine göre kalan km hesabı</summary>
    public int? KalanKm(int aracGuncelKm) =>
        SonrakiBakimKm.HasValue ? SonrakiBakimKm.Value - aracGuncelKm : null;
}

/// <summary>
/// Bakım uyarı bildirimi log'u — mükerrer gönderimleri önler
/// </summary>
public class AracBakimUyari : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int BakimPeriyotId { get; set; }
    public virtual BakimPeriyot BakimPeriyot { get; set; } = null!;

    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;

    public BakimUyariTipi UyariTipi { get; set; }

    /// <summary>Uyarı gönderildiğinde araç km değeri</summary>
    public int? AracKm { get; set; }

    /// <summary>Uyarı gönderildiğinde kalan km</summary>
    public int? KalanKm { get; set; }

    /// <summary>Uyarı gönderildiğinde kalan gün</summary>
    public int? KalanGun { get; set; }

    public DateTime GonderimTarihi { get; set; } = DateTime.UtcNow;

    public bool EmailGonderildi { get; set; }
    public bool WhatsAppGonderildi { get; set; }

    public string? HataMesaji { get; set; }
}

public enum BakimUyariTipi
{
    KmYaklasiyor = 1,   // Km bazlı uyarı eşiğine girdi
    KmAsildi = 2,       // Bakım km'si geçildi
    GunYaklasiyor = 3,  // Gün bazlı uyarı eşiğine girdi
    GunAsildi = 4       // Bakım tarihi geçildi
}

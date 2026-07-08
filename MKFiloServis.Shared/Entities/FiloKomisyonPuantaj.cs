using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Araç, Şoför ve Güzergah eşleştirme şablonu.
/// Özmal araçlar veya Şahsi/Taşeron araçlar bu havuzda tanımlanır ve Toplu Çalışma/Puantaj girişi oluşturmak için baz alınır.
/// </summary>
public class FiloGuzergahEslestirme : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }
    int? IFirmaTenant.FirmaId { get => FirmaId; set => FirmaId = value ?? 0; }

    /// <summary>
    /// İşi aldığımız asıl Kurum / Firma
    /// </summary>
    [Required]
    public int KurumFirmaId { get; set; }

    [Required]
    public int GuzergahId { get; set; }

    /// <summary>
    /// Bu güzergahta çalışacak Araç
    /// Özmal(Bize ait) ya da Şahıs(Taşeron/Komisyon) aracı olabilir.
    /// </summary>
    [Required]
    public int AracId { get; set; }

    [Required]
    public int SoforId { get; set; }

    public int? KullaniciId { get; set; }

    public ServisTuru ServisTuru { get; set; } = ServisTuru.SabahAksam;

    /// <summary>
    /// İşi aldığımız kuruma (asıl firmaya) 1 sefer için fatura edeceğimiz / tahsil edeceğimiz miktar
    /// </summary>
    public decimal KurumaKesilecekUcret { get; set; }

    /// <summary>
    /// Taşeron / Şahıs bir araca/şoföre veriyorsak ona ödeyeceğimiz 1 seferlik miktar
    /// Özmal araçlarda 0 olabilir.
    /// </summary>
    public decimal TaseronaOdenenUcret { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    [ForeignKey(nameof(KurumFirmaId))]
    public virtual Cari? MusteriCari { get; set; }
    [ForeignKey(nameof(FirmaId))]
    public virtual Firma? Firma { get; set; }
    public virtual Guzergah? Guzergah { get; set; }
    public virtual Arac? Arac { get; set; }
    public virtual Sofor? Sofor { get; set; }
    [ForeignKey(nameof(KullaniciId))]
    public virtual Kullanici? Kullanici { get; set; }
}

/// <summary>
/// Günlük operasyon ve puantaj verisi.
/// Eşleştirmeden yola çıkarak her güne bir kayıt açılır, fiili durum yazılır.
/// </summary>
public class FiloGunlukPuantaj : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }
    int? IFirmaTenant.FirmaId { get => FirmaId; set => FirmaId = value ?? 0; }

    [Required]
    public DateTime Tarih { get; set; }

    public int? FiloGuzergahEslestirmeId { get; set; } // Şablondan türediyse bağlantısı

    [Required]
    public int KurumFirmaId { get; set; }

    [Required]
    public int GuzergahId { get; set; }

    [Required]
    public int AracId { get; set; }

    [Required]
    public int SoforId { get; set; }

    public int? KullaniciId { get; set; }

    public OperasyonDurumu Durum { get; set; } = OperasyonDurumu.Gitti;

    /// <summary>
    /// O gün gerçekleşen vardiya (Sabah / Akşam / Yarda Mesai / SabahAkşam)
    /// </summary>
    public ServisTuru ServisTuru { get; set; } = ServisTuru.SabahAksam;

    /// <summary>
    /// O gün o vardiyada gerçekleşen sefer sayısı (varsayılan 1.0)
    /// </summary>
    public decimal SeferSayisi { get; set; } = 1m;

    /// <summary>
    /// Pazar günleri vs. yarım gün ya da tam gün (Örn: 1.0, 0.5)
    /// </summary>
    public decimal PuantajCarpani { get; set; } = 1.0m;

    /// <summary>
    /// O gün gerçekleşen duruma göre Kuruma kesilecek (Puantaj x Şablon Ücreti) tahakkuk tutarı
    /// </summary>
    public decimal TahakkukEdenKurumUcreti { get; set; }

    /// <summary>
    /// O gün gerçekleşen duruma göre taşerona ödenecek tutar
    /// </summary>
    public decimal TahakkukEdenTaseronUcreti { get; set; }

    public bool TaksiKullanildiMi { get; set; } = false;
    public decimal? TaksiFisTutari { get; set; }
    public string? TaksiFisAciklama { get; set; }

    public bool ArizaYaptiMi { get; set; } = false;
    public string? ArizaAciklamasi { get; set; }

    public string? Notlar { get; set; }

    /// <summary>
    /// Faturalaştırıldı / Elden Tahsil Edildi gibi işlemleri takip etmek için
    /// </summary>
    public bool KurumFaturaKesildiMi { get; set; } = false;
    public bool TaseronOdemeYapildiMi { get; set; } = false;

    /// <summary>
    /// Puantaj onaylandı mı? Onaylanmış kayıtlar toplu yeniden hesapla işleminden etkilenmez.
    /// </summary>
    public bool Onaylandi { get; set; } = false;
    public DateTime? OnayTarihi { get; set; }

    /// <summary>
    /// Özmal/Kiralık araçlar için o sefer için hesaplanan birim maliyet snapshot'u
    /// </summary>
    public decimal? MaliyetOzmalKiralik { get; set; }

    /// <summary>
    /// Kuruma kesilen gelir faturasına bağlantı
    /// </summary>
    public int? KurumFaturaId { get; set; }

    /// <summary>
    /// Tedarikçiden gelen / tedarikçiye kesilen gider faturasına bağlantı
    /// </summary>
    public int? TedarikciOdemeFaturaId { get; set; }

    // Navigation
    [ForeignKey(nameof(KurumFirmaId))]
    public virtual Cari? MusteriCari { get; set; }
    [ForeignKey(nameof(FirmaId))]
    public virtual Firma? Firma { get; set; }
    public virtual Guzergah? Guzergah { get; set; }
    public virtual Arac? Arac { get; set; }
    public virtual Sofor? Sofor { get; set; }
    [ForeignKey(nameof(KullaniciId))]
    public virtual Kullanici? Kullanici { get; set; }
    public virtual FiloGuzergahEslestirme? EslestirmeSablonu { get; set; }
}

public enum OperasyonDurumu
{
    Gitti = 1,
    Gitmedi_Mazeretli = 2,
    Gitmedi_Mazeretsiz = 3,
    Taksiyle_Gidildi = 4,
    Arizalandi_YoldaKaldi = 5,
    Iptal_KurumTarafindan = 6
}



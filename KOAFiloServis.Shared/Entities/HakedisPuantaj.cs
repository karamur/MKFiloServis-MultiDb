using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Operasyonel Hakediş Puantajı — Ana Kayıt.
/// Her ay bir (Güzergah + Araç + Şoför + Tedarikçi) kombinasyonu için bir kayıt.
/// Personel maaş puantajından BAĞIMSIZDIR.
/// </summary>
public class HakedisPuantaj : BaseEntity, IFirmaTenant
{
    // Tenant
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int? SubeId { get; set; }

    // Dönem
    public int Yil { get; set; }
    public int Ay { get; set; }

    // Güzergah
    public int GuzergahId { get; set; }
    public virtual Guzergah? Guzergah { get; set; }

    // Araç
    public int AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    // Şoför
    public int SoforId { get; set; }
    public virtual Sofor? Sofor { get; set; }

    // Tedarikçi (Cari)
    public int CariId { get; set; }
    public virtual Cari? Cari { get; set; }

    // Yön Tipi (Güzergah varsayılanından alınır, değiştirilebilir)
    public YonTipi YonTipi { get; set; } = YonTipi.SabahAksam;

    // Fiyatlandırma
    [Column(TypeName = "decimal(18,4)")]
    public decimal BirimFiyat { get; set; } // Günlük toplam fiyat (Sabah+Aksam ise 2 seferlik)

    public int KdvOrani { get; set; } = 20; // 20, 10, 1, 0

    // Sefer Bilgisi
    [Column(TypeName = "decimal(18,2)")]
    public decimal GunlukSeferSayisi { get; set; } = 2; // Sabah+Aksam=2, Sabah=1, Aksam=1

    /// <summary>Sefer başı birim fiyat. Sabah+Aksam ise BirimFiyat / 2.</summary>
    [NotMapped]
    public decimal SeferBirimFiyat => GunlukSeferSayisi > 0 ? BirimFiyat / GunlukSeferSayisi : 0;

    // Toplamlar
    public int ToplamSefer { get; set; }       // Tüm günlerin toplam sefer sayısı
    public int ToplamEkSefer { get; set; }     // Ek sefer toplamı

    [Column(TypeName = "decimal(18,2)")]
    public decimal HakedisTutari { get; set; } // ToplamSefer × SeferBirimFiyat

    [Column(TypeName = "decimal(18,2)")]
    public decimal KdvTutari { get; set; }     // HakedisTutari × KdvOrani / 100

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamKesinti { get; set; } // Tüm kesintilerin toplamı

    [Column(TypeName = "decimal(18,2)")]
    public decimal OdenecekTutar { get; set; } // HakedisTutari + KdvTutari - ToplamKesinti

    // Durum
    public HakedisDurumu Durum { get; set; } = HakedisDurumu.Taslak;

    // Fatura
    public int? FaturaId { get; set; }
    public virtual Fatura? Fatura { get; set; }

    // Notlar
    [StringLength(500)]
    public string? Aciklama { get; set; }

    // Navigation
    public virtual ICollection<HakedisPuantajDetay> Detaylar { get; set; } = new List<HakedisPuantajDetay>();
    public virtual ICollection<HakedisKesinti> Kesintiler { get; set; } = new List<HakedisKesinti>();

    /// <summary>
    /// Hakediş toplamlarını detay ve kesintilerden yeniden hesaplar.
    /// </summary>
    public void Hesapla()
    {
        if (Detaylar.Any())
        {
            ToplamSefer = Detaylar.Sum(d => (int)d.SeferSayisi);
            ToplamEkSefer = Detaylar.Count(d => d.EkSeferMi);
        }

        HakedisTutari = ToplamSefer * SeferBirimFiyat;
        KdvTutari = HakedisTutari * KdvOrani / 100;
        ToplamKesinti = Kesintiler.Where(k => !k.IsDeleted).Sum(k => k.Tutar);
        OdenecekTutar = HakedisTutari + KdvTutari - ToplamKesinti;
    }
}

/// <summary>
/// Hakediş Puantaj Detayı — Günlük sefer kaydı.
/// Ayın her günü için bir kayıt (0 sefer de olabilir).
/// </summary>
public class HakedisPuantajDetay : BaseEntity
{
    public int HakedisPuantajId { get; set; }
    public virtual HakedisPuantaj? HakedisPuantaj { get; set; }

    /// <summary>Ayın kaçıncı günü (1-31)</summary>
    public int Gun { get; set; }

    /// <summary>Günlük sefer sayısı. 0=çalışmadı, 1=tek sefer, 2=S+A, 3=S+A+ek...</summary>
    public int SeferSayisi { get; set; }

    /// <summary>Ek sefer (ana seferlere ek olarak)</summary>
    public bool EkSeferMi { get; set; }

    [StringLength(200)]
    public string? Aciklama { get; set; }
}

/// <summary>
/// Hakediş Kesintisi — Birden fazla kesinti kalemi.
/// Lastik, Yakıt, Ceza, Avans, Hasar, Bakım vb.
/// </summary>
public class HakedisKesinti : BaseEntity
{
    public int HakedisPuantajId { get; set; }
    public virtual HakedisPuantaj? HakedisPuantaj { get; set; }

    [Required]
    [StringLength(100)]
    public string KesintiAdi { get; set; } = string.Empty; // "Lastik", "Yakıt", "Ceza" vb.

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tutar { get; set; }

    [StringLength(200)]
    public string? Aciklama { get; set; }
}

/// <summary>
/// Yön Tipleri — Güzergah başına sefer sayısını belirler.
/// </summary>
public enum YonTipi
{
    /// <summary>Sadece sabah seferi — 1 sefer/gün</summary>
    Sabah = 1,

    /// <summary>Sadece akşam seferi — 1 sefer/gün</summary>
    Aksam = 2,

    /// <summary>Sabah + Akşam — 2 sefer/gün</summary>
    SabahAksam = 3
}

/// <summary>
/// Hakediş Durumu — İş akışı.
/// </summary>
public enum HakedisDurumu
{
    Taslak = 0,
    OnayBekliyor = 1,
    Onaylandi = 2,
    Faturalasti = 3,
    Odendi = 4
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

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
    public decimal GelirBirimFiyat { get; set; } // Müşteriye kesilecek günlük toplam fiyat

    [Column(TypeName = "decimal(18,4)")]
    public decimal GiderBirimFiyat { get; set; } // Tedarikçiye ödenecek günlük toplam fiyat

    // Backward compat
    [NotMapped]
    public decimal BirimFiyat { get => GelirBirimFiyat; set => GelirBirimFiyat = value; }

    public int KdvOrani { get; set; } = 20; // 20, 10, 1, 0

    // Sefer Bilgisi
    [Column(TypeName = "decimal(18,2)")]
    public decimal GunlukSeferSayisi { get; set; } = 2; // Sabah+Aksam=2, Sabah=1, Aksam=1

    /// <summary>Sefer başı gelir birim fiyat. Sabah+Aksam ise /2.</summary>
    [NotMapped]
    public decimal GelirSeferBirimFiyat => GunlukSeferSayisi > 0 ? GelirBirimFiyat / GunlukSeferSayisi : 0;

    /// <summary>Sefer başı gider birim fiyat. Sabah+Aksam ise /2.</summary>
    [NotMapped]
    public decimal GiderSeferBirimFiyat => GunlukSeferSayisi > 0 ? GiderBirimFiyat / GunlukSeferSayisi : 0;

    // Toplamlar
    public int ToplamSefer { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GelirToplam { get; set; }  // Tahsil edilecek

    [Column(TypeName = "decimal(18,2)")]
    public decimal GiderToplam { get; set; }  // Ödenecek (KDV hariç)

    [Column(TypeName = "decimal(18,2)")]
    public decimal KdvTutari { get; set; }    // GiderToplam × KdvOrani / 100 (Gider KDV)

    [Column(TypeName = "decimal(18,2)")]
    public decimal GelirKdvTutari { get; set; } // GelirToplam × KdvOrani / 100 (Gelir KDV)

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamKesinti { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OdenecekTutar { get; set; } // GiderToplam + KdvTutari - ToplamKesinti

    [Column(TypeName = "decimal(18,2)")]
    public decimal TahsilEdilecekTutar { get; set; } // GelirToplam + GelirKdvTutari

    [NotMapped]
    public decimal KarTutar => TahsilEdilecekTutar - OdenecekTutar;

    // Durum
    public HakedisDurumu Durum { get; set; } = HakedisDurumu.Taslak;

    // Concurrency — optimistic locking
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Muhasebe Entegrasyonu
    public int? GelirFisId { get; set; }           // Gelir muhasebe fişi
    public virtual MuhasebeFis? GelirFis { get; set; }
    public int? GiderFisId { get; set; }           // Gider muhasebe fişi
    public virtual MuhasebeFis? GiderFis { get; set; }
    public bool MuhasebeyeAktarildiMi { get; set; }

    // Fatura
    public int? FaturaId { get; set; }
    public virtual Fatura? Fatura { get; set; }
    public int? GelirFaturaId { get; set; }
    public virtual Fatura? GelirFatura { get; set; }
    public int? GiderFaturaId { get; set; }
    public virtual Fatura? GiderFatura { get; set; }

    // Notlar
    [StringLength(500)]
    public string? Aciklama { get; set; }

    // Navigation
    public virtual ICollection<HakedisPuantajDetay> Detaylar { get; set; } = new List<HakedisPuantajDetay>();
    public virtual ICollection<HakedisKesinti> Kesintiler { get; set; } = new List<HakedisKesinti>();

    /// <summary>
    /// Hakediş toplamlarını detay ve kesintilerden yeniden hesaplar.
    /// 🔴 Grid hücre değeri TEK GERÇEK. Yön hesaplamaya ETKİ ETMEZ.
    /// </summary>
    public void Hesapla()
    {
        if (Detaylar.Any())
        {
            // 🔴 Defansif clamp: Her detay SeferSayisi 0-10 aralığında
            foreach (var d in Detaylar)
            {
                if (d.SeferSayisi < 0) d.SeferSayisi = 0;
                if (d.SeferSayisi > 10) d.SeferSayisi = 10;
            }

            // 🔴 TEK FORMÜL: ToplamSefer = Sum(SeferSayisi). Yön etki ETMEZ.
            ToplamSefer = Detaylar.Sum(d => d.SeferSayisi);

            // 🔴 Double-count koruması: Aylık sefer 310'u geçemez (31 gün × 10)
            if (ToplamSefer < 0) ToplamSefer = 0;
            if (ToplamSefer > 310) ToplamSefer = 310;

            // Her detay için GelirTutar = SeferSayisi × GelirSeferBirimFiyat × FiyatCarpani
            GelirToplam = Detaylar.Sum(d => d.SeferSayisi * GelirSeferBirimFiyat * d.FiyatCarpani);

            // Her detay için GiderTutar = SeferSayisi × GiderSeferBirimFiyat × FiyatCarpani
            GiderToplam = Detaylar.Sum(d => d.SeferSayisi * GiderSeferBirimFiyat * d.FiyatCarpani);
        }

        KdvTutari = GiderToplam * KdvOrani / 100;
        GelirKdvTutari = GelirToplam * KdvOrani / 100;
        ToplamKesinti = Kesintiler.Where(k => !k.IsDeleted).Sum(k => k.Tutar);
        OdenecekTutar = GiderToplam + KdvTutari - ToplamKesinti;
        TahsilEdilecekTutar = GelirToplam + GelirKdvTutari;
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

    /// <summary>Sefer türü (null=standart güzergah seferi)</summary>
    public int? SeferTuruId { get; set; }
    public virtual HakedisSeferTuru? SeferTuru { get; set; }

    /// <summary>Fiyat çarpanı (1=normal, 1.5=mesai, 2=resmi tatil)</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal FiyatCarpani { get; set; } = 1;

    /// <summary>Mesai seferi mi?</summary>
    public bool MesaiMi { get; set; }

    /// <summary>Ek sefer (ana seferlere ek olarak)</summary>
    public bool EkSeferMi { get; set; }

    [StringLength(200)]
    public string? Aciklama { get; set; }
}

/// <summary>
/// Kullanıcı tanımlı sefer türü.
/// Her firma kendi sefer türlerini oluşturabilir.
/// </summary>
public class HakedisSeferTuru : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Required, StringLength(20)]
    public string Kod { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Ad { get; set; } = string.Empty;

    /// <summary>Varsayılan sefer sayısı (Sabah=1, Sabah+Aksam=2)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal VarsayilanSeferSayisi { get; set; } = 1;

    /// <summary>Fiyat çarpanı (1=normal, 1.5=mesai, 2=tatil)</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal FiyatCarpani { get; set; } = 1;

    public bool MesaiMi { get; set; }
    public bool EkSeferMi { get; set; }

    /// <summary>Sistem tanımlı mı? (seed ile gelenler)</summary>
    public bool SistemTanimliMi { get; set; }

    public bool Aktif { get; set; } = true;

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



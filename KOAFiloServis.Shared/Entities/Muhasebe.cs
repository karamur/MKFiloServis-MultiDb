using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Muhasebe Hesap Plani - Tek Duzen Hesap Plani
/// </summary>
public class MuhasebeHesap : BaseEntity
{
    [Required]
    [StringLength(10)]
    public string HesapKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string HesapAdi { get; set; } = string.Empty;

    public HesapTuru HesapTuru { get; set; }
    public HesapGrubu HesapGrubu { get; set; }

    public int? UstHesapId { get; set; }
    public virtual MuhasebeHesap? UstHesap { get; set; }

    public bool AltHesapVar { get; set; } = false;
    public bool Aktif { get; set; } = true;
    public bool SistemHesabi { get; set; } = false; // Silinemeyen sistem hesaplari

    public string? Aciklama { get; set; }

    // Navigation
    public virtual ICollection<MuhasebeFisKalem> FisKalemleri { get; set; } = new List<MuhasebeFisKalem>();
}

public enum HesapTuru
{
    Aktif = 1,      // Varlik hesaplari (1-2)
    Pasif = 2,      // Kaynak hesaplari (3-4-5)
    Gelir = 3,      // Gelir hesaplari (6)
    Gider = 4,      // Gider hesaplari (7)
    Maliyet = 5,    // Maliyet hesaplari (7)
    Nazim = 6       // Nazim hesaplar (9)
}

public enum HesapGrubu
{
    // 1 - Donen Varliklar
    DonenVarliklar = 1,
    // 2 - Duran Varliklar
    DuranVarliklar = 2,
    // 3 - Kisa Vadeli Yabanci Kaynaklar
    KisaVadeliYabanciKaynaklar = 3,
    // 4 - Uzun Vadeli Yabanci Kaynaklar
    UzunVadeliYabanciKaynaklar = 4,
    // 5 - Ozkaynaklar
    Ozkaynaklar = 5,
    // 6 - Gelir Tablosu Hesaplari
    GelirTablosu = 6,
    // 7 - Maliyet Hesaplari
    MaliyetHesaplari = 7,
    // 9 - Nazim Hesaplar
    NazimHesaplar = 9
}

/// <summary>
/// Muhasebe Fisi
/// </summary>
public class MuhasebeFis : BaseEntity
{
    [Required]
    public string FisNo { get; set; } = string.Empty;

    [Required]
    public DateTime FisTarihi { get; set; }

    public FisTipi FisTipi { get; set; }

    public string? Aciklama { get; set; }

    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }

    public FisDurum Durum { get; set; } = FisDurum.Taslak;

    // Kaynak bilgisi
    public FisKaynak Kaynak { get; set; } = FisKaynak.Manuel;
    public int? KaynakId { get; set; } // Fatura, Hareket vb. ID
    public string? KaynakTip { get; set; } // "Fatura", "BankaHareket" vb.

    // Bordro bağlantısı (opsiyonel)
    public int? BordroId { get; set; }
    public virtual Bordro? Bordro { get; set; }

    // Navigation
    public virtual ICollection<MuhasebeFisKalem> Kalemler { get; set; } = new List<MuhasebeFisKalem>();
}

public enum FisTipi
{
    Mahsup = 1,
    Tahsilat = 2,
    Tediye = 3,
    Acilis = 4,
    Kapanis = 5,
    Devir = 6
}

public enum FisDurum
{
    Taslak = 1,
    Onaylandi = 2,
    IptalEdildi = 3
}

public enum FisKaynak
{
    Manuel = 1,
    Fatura = 2,
    BankaHareket = 3,
    Butce = 4,
    Otomatik = 5
}

/// <summary>
/// Muhasebe Fis Kalemi
/// </summary>
public class MuhasebeFisKalem : BaseEntity
{
    public int FisId { get; set; }
    public virtual MuhasebeFis Fis { get; set; } = null!;

    public int HesapId { get; set; }
    public virtual MuhasebeHesap Hesap { get; set; } = null!;

    public int SiraNo { get; set; }

    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }

    public DateTime? Tarih { get; set; } = DateTime.Today;

    public string? Aciklama { get; set; }

    // Cari/Detay bilgisi
    public int? CariId { get; set; }
    public virtual Cari? Cari { get; set; }
}

/// <summary>
/// Muhasebe Donem
/// </summary>
public class MuhasebeDonem : BaseEntity
{
    public int Yil { get; set; }
    public int Ay { get; set; }

    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }

    public DonemDurum Durum { get; set; } = DonemDurum.Acik;

    public DateTime? KapanisTarihi { get; set; }
}

public enum DonemDurum
{
    Acik = 1,
    Kapali = 2,
    Gecici = 3
}

/// <summary>
/// KDV Oranına Göre Hesap Kodu Eşleştirmesi
/// Örn: %20 → 391.20 / 191.20, %10 → 391.10 / 191.10, %1 → 391.01 / 191.01
/// </summary>
public class KdvHesapEslestirme : BaseEntity
{
    public int MuhasebeAyarId { get; set; }
    public MuhasebeAyar? MuhasebeAyar { get; set; }

    /// <summary>KDV Oranı (0, 1, 10, 20 vb.)</summary>
    public int KdvOrani { get; set; }

    /// <summary>Hesaplanan KDV Hesabı (Satış Faturası) — Ör: 391.20</summary>
    [StringLength(50)]
    public string HesaplananKdvHesabi { get; set; } = "391.01";

    /// <summary>İndirilecek KDV Hesabı (Alış Faturası) — Ör: 191.20</summary>
    [StringLength(50)]
    public string IndirilecekKdvHesabi { get; set; } = "191.01";

    [StringLength(100)]
    public string? Aciklama { get; set; }
}

/// <summary>
/// Cari Modülü / Muhasebe Otomatik Hesap Açma Ayarları
/// </summary>
public class MuhasebeAyar : BaseEntity
{
    // Cari Hesap Prefixleri
    [StringLength(50)]
    public string MusteriPrefix { get; set; } = "120.01";

    [StringLength(50)]
    public string TedarikciPrefix { get; set; } = "320.01";

    [StringLength(50)]
    public string PersonelPrefix { get; set; } = "335.01";

    [StringLength(50)]
    public string PersonelAvansPrefix { get; set; } = "195.01";
    
    public bool OtomatikHesapDuzenlensin { get; set; } = true;

    // Fatura Muhasebe Hesapları
    [StringLength(50)]
    public string SatisGelirHesabi { get; set; } = "600.01"; // Yurtiçi Satışlar

    [StringLength(50)]
    public string AlisGiderHesabi { get; set; } = "153.01"; // Ticari Mallar veya 740.01 Hizmet Giderleri

    [StringLength(50)]
    public string HesaplananKdvHesabi { get; set; } = "391.01"; // Hesaplanan KDV

    [StringLength(50)]
    public string IndirilecekKdvHesabi { get; set; } = "191.01"; // İndirilecek KDV

    // Tevkifat Hesapları
    [StringLength(50)]
    public string TevkifatKdvHesabi { get; set; } = "360.01"; // Sorumlu Sıfatıyla Ödenen KDV

    [StringLength(50)]
    public string TevkifatAlacakHesabi { get; set; } = "136.01"; // Diğer Çeşitli Alacaklar (Tevkifat)

    // Stok Masraf Aktarım Hesapları (Mal/Sarf Malzeme için)
    [StringLength(50)]
    public string MalMasrafHesabi { get; set; } = "740.99.001"; // Ticari Mal Masraf Hesabı

    [StringLength(50)]
    public string SarfMalzemeMasrafHesabi { get; set; } = "740.99.002"; // Sarf Malzeme Masraf Hesabı

    [StringLength(50)]
    public string StokCikisHesabi { get; set; } = "153"; // Ticari Mallar (Stok çıkış karşılığı)

    // Faturadan otomatik muhasebe fişi oluşturulsun mu?
    public bool FaturaOtomatikMuhasebeFisi { get; set; } = false;

    // Stok masraf aktarımı otomatik yapılsın mı?
    public bool StokMasrafAktarimiOtomatik { get; set; } = true;

    // XML Import ayarları
    public bool XmlImportOtomatikCariOlustur { get; set; } = true;
    public bool XmlImportOtomatikHesapKoduOlustur { get; set; } = true;

    // Bordro Muhasebe Entegrasyonu Hesapları
    [StringLength(50)]
    public string PersonelGiderHesabi { get; set; } = "770.01"; // 770 Genel Yonetim Giderleri (BORC)
    [StringLength(50)]
    public string VergiHesabi { get; set; } = "360.01"; // 360 Odenecek Vergi ve Fonlar (ALACAK)
    [StringLength(50)]
    public string SGKHesabi { get; set; } = "361.01"; // 361 Odenecek SGK Kesintileri (ALACAK)
    [StringLength(50)]
    public string IsverenSGKHesabi { get; set; } = "368.01"; // 368 Odenecek Isveren SGK (ALACAK)

    // KDV Oran → Hesap Kodu Eşleştirmeleri
    public List<KdvHesapEslestirme> KdvHesapEslestirmeleri { get; set; } = new();
}

/// <summary>
/// Masraf/Kost Merkezi Tanımları
/// </summary>
public class KostMerkezi : BaseEntity
{
    [Required]
    [StringLength(20)]
    public string KostKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string KostAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }
    public bool Aktif { get; set; } = true;
    public int? UstKostMerkeziId { get; set; }

    // Navigation
    public virtual KostMerkezi? UstKostMerkezi { get; set; }
    public virtual ICollection<KostMerkezi> AltKostMerkezleri { get; set; } = new List<KostMerkezi>();
}

/// <summary>
/// Proje Tanımları (Muhasebe takibi için)
/// </summary>
public class MuhasebeProje : BaseEntity
{
    [Required]
    [StringLength(20)]
    public string ProjeKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string ProjeAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public bool Aktif { get; set; } = true;
    public decimal? ButceTutar { get; set; }

    // İlişkili Cari/Firma
    public int? CariId { get; set; }
    public int? FirmaId { get; set; }

    // Navigation
    public virtual Cari? Cari { get; set; }
    public virtual Firma? Firma { get; set; }
}

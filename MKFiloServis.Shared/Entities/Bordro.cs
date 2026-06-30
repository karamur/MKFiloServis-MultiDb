namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Aylık bordro kayıtları
/// </summary>
public class Bordro : BaseEntity, IFirmaTenant
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public int? FirmaId { get; set; }
    public BordroTipi BordroTipi { get; set; } = BordroTipi.Normal;
    public DateTime? HesaplamaTarihi { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public bool Onaylandi { get; set; } = false;
    public string? OnaylayanKullanici { get; set; }
    public string? Aciklama { get; set; }
    
    // Özet Bilgiler
    public int ToplamPersonelSayisi { get; set; }
    public decimal ToplamBrutMaas { get; set; }
    public decimal ToplamNetMaas { get; set; }
    public decimal ToplamSgkMatrahi { get; set; }
    public decimal ToplamEkOdeme { get; set; }
    public decimal GenelToplam => ToplamNetMaas + ToplamEkOdeme;
    
    // Navigation Properties
    public virtual Firma? Firma { get; set; }
    public virtual ICollection<BordroDetay> BordroDetaylar { get; set; } = new List<BordroDetay>();
    public virtual ICollection<MuhasebeFis> MuhasebeFisleri { get; set; } = new List<MuhasebeFis>();
    
    public string DonemeAdi => $"{Ay}/{Yil}";
}

/// <summary>
/// Bordro detay satırları (personel bazında)
/// </summary>
public class BordroDetay : BaseEntity, IFirmaTenant
{
    public int BordroId { get; set; }
    public int PersonelId { get; set; }
    public int? FirmaId { get; set; }
    
    // Maaş Bilgileri
    public decimal BrutMaas { get; set; }
    public decimal NetMaas { get; set; }
    public decimal TopluMaas { get; set; } // Personelin toplu maaşı
    public decimal SgkMaasi { get; set; } // SGK'ya bildirilen
    public decimal EkOdeme { get; set; } // Toplu - SGK
    
    // Kesintiler (İşçi Payı)
    public decimal SgkIsciPrim { get; set; }
    public decimal IssizlikIsciPrim { get; set; }
    public decimal SgkIssizlikKesinti { get; set; } // Geriye uyumluluk: SgkIsciPrim + IssizlikIsciPrim
    public decimal GelirVergisi { get; set; }
    public decimal DamgaVergisi { get; set; }
    public decimal ToplamKesinti => SgkIssizlikKesinti + GelirVergisi + DamgaVergisi + OzelKesintilerToplam;

    // İşveren Maliyeti
    public decimal SgkIsverenPrim { get; set; }
    public decimal IssizlikIsverenPrim { get; set; }
    public decimal ToplamIsverenMaliyet => SgkIsverenPrim + IssizlikIsverenPrim;
    public decimal ToplamIsverenMaliyetDahilMaas => BrutMaas + ToplamIsverenMaliyet;

    // Kümülatif Vergi Matrahı
    public decimal KumulatifVergiMatrahi { get; set; }
    public int UygulananVergiDilimi { get; set; }

    // Ek Ödemeler / Sosyal Yardımlar
    public decimal YemekYardimi { get; set; }
    public decimal YolYardimi { get; set; }
    public decimal PrimTutar { get; set; }
    public decimal AileYardimi { get; set; }
    public decimal Ikramiye { get; set; }
    public decimal DigerEkOdeme { get; set; }
    public decimal ToplamEkOdeme => YemekYardimi + YolYardimi + PrimTutar + AileYardimi + Ikramiye + DigerEkOdeme;

    // Özel Kesintiler
    public decimal IcraKesintisi { get; set; }
    public decimal BESKesintisi { get; set; }
    public decimal SendikaKesintisi { get; set; }
    public decimal HayatSigortasi { get; set; }
    public decimal BireyselEmeklilik { get; set; }
    public decimal DigerOzelKesinti { get; set; }
    public decimal OzelKesintilerToplam => IcraKesintisi + BESKesintisi + SendikaKesintisi + HayatSigortasi + BireyselEmeklilik + DigerOzelKesinti;
    
    // Ödeme Durumu
    public bool BankaOdemesiYapildi { get; set; } = false;
    public DateTime? BankaOdemeTarihi { get; set; }
    public bool EkOdemeYapildi { get; set; } = false;
    public DateTime? EkOdemeTarihi { get; set; }
    
    public decimal ToplamOdenecek => NetMaas + ToplamEkOdeme + EkOdeme;

    public string? Notlar { get; set; }

    // UI Helper (NotMapped)
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool Secili { get; set; } = false;

    // Navigation Properties
    public virtual Bordro Bordro { get; set; } = null!;
    public virtual Sofor Personel { get; set; } = null!;
    public virtual Firma? Firma { get; set; }
}

/// <summary>
/// Bordro ödeme kayıtları (2 taksit sistemi için)
/// </summary>
public class BordroOdeme : BaseEntity
{
    public int BordroDetayId { get; set; }
    public OdemeTipi OdemeTipi { get; set; } // BankaOdemesi veya EkOdeme
    public DateTime OdemeTarihi { get; set; }
    public decimal OdemeTutari { get; set; }
    public OdemeSekli OdemeSekli { get; set; }
    public int? BankaHesapId { get; set; }
    public string? EvrakNo { get; set; }
    public string? Aciklama { get; set; }
    
    // Muhasebe Entegrasyonu
    public int? MuhasebeFisId { get; set; }
    
    // Navigation Properties
    public virtual BordroDetay BordroDetay { get; set; } = null!;
    public virtual BankaHesap? BankaHesap { get; set; }
    public virtual MuhasebeFis? MuhasebeFis { get; set; }
}

/// <summary>
/// Bordro ayarları
/// </summary>
public class BordroAyar : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    
    // Muhasebe Hesap Kodları
    public string PersonelMaasHesapKodu { get; set; } = "335"; // Personele Borçlar
    public string SgkPrimHesapKodu { get; set; } = "361"; // SGK Prim Borçları
    public string GelirVergisiHesapKodu { get; set; } = "360"; // Ödenecek Vergiler
    public string KasaHesapKodu { get; set; } = "100"; // Kasa
    public string BankaHesapKodu { get; set; } = "102"; // Banka
    public string PersonelAvansHesapKodu { get; set; } = "195"; // Personel Avansları (mahsup için)
    
    // İşçi Payı Oranları (%)
    public decimal SgkIsciPayiOrani { get; set; } = 14; // SGK işçi payı %
    public decimal IssizlikIsciPayiOrani { get; set; } = 1; // İşsizlik işçi payı %
    public decimal DamgaVergisiOrani { get; set; } = 0.759M; // Damga vergisi %

    // İşveren Payı Oranları (%)
    public decimal SgkIsverenPayiOrani { get; set; } = 20.5M; // SGK işveren payı (15.5 SGK + 2 İşKaz + 2 Genel Sağ + 1 kısa vade)
    public decimal IssizlikIsverenPayiOrani { get; set; } = 2; // İşsizlik işveren payı %
    public bool Sgk5PuanIndirimVarMi { get; set; } = true; // 5 puan SGK indirimi aktif mi

    // Gelir Vergisi Dilimleri (2025 yılı değerleri)
    public decimal GelirVergisiDilim1Sinir { get; set; } = 158_000; // 1. dilim üst sınır
    public decimal GelirVergisiDilim1Oran { get; set; } = 15;
    public decimal GelirVergisiDilim2Sinir { get; set; } = 330_000; // 2. dilim üst sınır
    public decimal GelirVergisiDilim2Oran { get; set; } = 20;
    public decimal GelirVergisiDilim3Sinir { get; set; } = 800_000; // 3. dilim üst sınır
    public decimal GelirVergisiDilim3Oran { get; set; } = 27;
    public decimal GelirVergisiDilim4Sinir { get; set; } = 4_300_000; // 4. dilim üst sınır
    public decimal GelirVergisiDilim4Oran { get; set; } = 35;
    public decimal GelirVergisiDilim5Oran { get; set; } = 40; // 5. dilim (üstü)

    // AGİ (Asgari Geçim İndirimi) - 2025'den itibaren uygulanmıyor ama uyumluluk için
    public bool AgiUygulaniyor { get; set; } = false;
    public decimal AgiTutari { get; set; } = 0;

    // ARGE Özel Ayarlar
    public bool ArgeSgkIsverenDestekVarMi { get; set; } = true;
    public decimal ArgeSgkIsverenDestekOrani { get; set; } = 100; // % tam destek
    public bool ArgeGelirVergisiStopajDestekVarMi { get; set; } = true;
    public decimal ArgeGelirVergisiStopajDestekOrani { get; set; } = 100; // %
    
    // Navigation Properties
    public virtual Firma? Firma { get; set; }
}

public enum BordroTipi
{
    Normal = 1,
    Arge = 2,
    Diger = 99
}

public enum OdemeTipi
{
    BankaOdemesi = 1, // SGK maaşı (1. taksit)
    EkOdeme = 2 // Kalan tutar (2. taksit)
}

public enum OdemeSekli
{
    Nakit = 1,
    BankaTransfer = 2,
    Cek = 3,
    Senet = 4
}

/// <summary>
/// Netten brüte hesaplama sonucu
/// </summary>
public class NettenBruteHesapSonucu
{
    public decimal IstenenNetMaas { get; set; }
    public decimal HesaplananBrutMaas { get; set; }
    public decimal SgkIsciPrim { get; set; }
    public decimal IssizlikIsciPrim { get; set; }
    public decimal SgkIsciToplam => SgkIsciPrim + IssizlikIsciPrim;
    public decimal GelirVergisiMatrahi { get; set; }
    public decimal GelirVergisi { get; set; }
    public decimal DamgaVergisi { get; set; }
    public decimal ToplamKesinti => SgkIsciToplam + GelirVergisi + DamgaVergisi;
    public decimal HesaplananNetMaas => HesaplananBrutMaas - ToplamKesinti;
    public decimal SgkIsverenPrim { get; set; }
    public decimal IssizlikIsverenPrim { get; set; }
    public decimal ToplamIsverenMaliyet => SgkIsverenPrim + IssizlikIsverenPrim;
    public decimal ToplamMaliyet => HesaplananBrutMaas + ToplamIsverenMaliyet;
    public int UygulananVergiDilimi { get; set; }
    public decimal KumulatifVergiMatrahi { get; set; }
}



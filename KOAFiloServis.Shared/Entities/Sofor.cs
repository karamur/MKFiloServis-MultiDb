using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Personel bilgileri (Şoför, Ofis Çalışanı, Yönetici vb.)
/// </summary>
/// Kural 4: FirmaId NOT NULL. TenantNullableFirmaId kaldırıldı.
public class Sofor : BaseEntity, IKopyalanabilirTenant, IFirmaTenant
{
    /// <summary>Firma kopyalama (K8) audit: kaynak firma Id'si.</summary>
    public int? KaynakFirmaId { get; set; }
    /// <summary>Firma kopyalama (K8) audit: kaynak kayıt Id'si.</summary>
    public int? KaynakKayitId { get; set; }

    public string SoforKodu { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string? TcKimlikNo { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? Adres { get; set; }

    // Sıralama ve Görev Bilgisi
    public int SiralamaNo { get; set; } = 0;
    public PersonelGorev Gorev { get; set; } = PersonelGorev.Sofor;
    public string? Departman { get; set; }
    public string? Pozisyon { get; set; }
    
    // Şoför Belgeler (Sadece şoförler için)
    public string? EhliyetNo { get; set; }
    public DateTime? EhliyetGecerlilikTarihi { get; set; }
    public DateTime? MykBelgesiGecerlilikTarihi { get; set; }
    public bool YayginEgitimSertifikasiVarMi { get; set; } = false;
    public DateTime? SrcBelgesiGecerlilikTarihi { get; set; }
    public DateTime? PsikoteknikGecerlilikTarihi { get; set; }
    public DateTime? SaglikRaporuGecerlilikTarihi { get; set; }
    public DateTime? KimlikGecerlilikTarihi { get; set; }
    public DateTime? AdliSicilGecerlilikTarihi { get; set; }
    public DateTime? SuruculCezaBarkodluBelgeTarihi { get; set; }
    
    // Genel Bilgiler
    public DateTime? IseBaslamaTarihi { get; set; }
    public DateTime? IstenAyrilmaTarihi { get; set; }
    public DateTime? SgkCikisTarihi { get; set; }
    public bool Aktif { get; set; } = true;
    public string? Notlar { get; set; }
    
    // Maaş Bilgileri
    public BrutMaasHesaplamaTipi BrutMaasHesaplamaTipi { get; set; } = BrutMaasHesaplamaTipi.Manuel;
    public decimal CalismaMiktari { get; set; }
    public decimal BirimUcret { get; set; }
    public decimal BrutMaas { get; set; }
    public decimal ResmiNetMaas { get; set; }
    public decimal DigerMaas { get; set; }
    public decimal NetMaas { get; set; }

    // Firma Bilgisi (çalıştığı firma) — Kural 4: NOT NULL (DB seviyesinde)
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    // Personel Taşıma Tedarikçisi (alt yüklenici)
    // Null ise personel kendi şirketimize ait; doluysa bu personel ilgili tedarikçiye aittir.
    public int? TasimaTedarikciId { get; set; }
    public virtual TasimaTedarikci? TasimaTedarikci { get; set; }

    // SGK Bordro Ayarları
    public bool SGKBordroDahilMi { get; set; } = false;
    public PersonelBordroTipi BordroTipiPersonel { get; set; } = PersonelBordroTipi.Yok;
    public SgkCalismaTuru? SgkCalismaTuru { get; set; }

    // ARGE ve Toplu Maaş Bilgileri
    public bool ArgePersoneli { get; set; } = false; // Geriye dönük uyumluluk
    public decimal TopluMaas { get; set; } // SGK'ya bildirilen + ekstra ödeme toplamı
    public decimal SgkMaasi { get; set; } // SGK'ya bildirilen maaş
    [NotMapped]
    public decimal EkOdeme => TopluMaas - SgkMaasi; // Geriye kalan ödeme

    // Özel Kesintiler (Aylık Sabit)
    public decimal IcraKesintisi { get; set; } // İcra kesintisi
    public decimal BESKesintisi { get; set; } // Bireysel Emeklilik
    public decimal SendikaKesintisi { get; set; } // Sendika aidatı
    public decimal HayatSigortasi { get; set; } // Hayat sigortası
    public decimal BireyselEmeklilik { get; set; } // Bireysel emeklilik (eski/2. kayıt)
    public decimal DigerOzelKesinti { get; set; } // Diğer kesintiler

    // Sosyal Yardımlar (Aylık Sabit)
    public decimal YemekYardimi { get; set; }
    public decimal YolYardimi { get; set; }
    public decimal AileYardimi { get; set; }

    // Banka Bilgileri
    public string? BankaAdi { get; set; }
    public string? IBAN { get; set; }

    // Muhasebe Hesap Entegrasyonu
    public int? MuhasebeHesapId { get; set; }
    public virtual MuhasebeHesap? MuhasebeHesap { get; set; }

    [NotMapped]
    public string TamAd => $"{Ad} {Soyad}";

    // Şoför mü kontrolü
    [NotMapped]
    public bool IsSofor => Gorev == PersonelGorev.Sofor;

    // Navigation Properties
    public virtual ICollection<ServisCalisma> ServisCalismalari { get; set; } = new List<ServisCalisma>();
    public virtual ICollection<PersonelMaas> Maaslar { get; set; } = new List<PersonelMaas>();
    public virtual ICollection<PersonelIzin> Izinler { get; set; } = new List<PersonelIzin>();
    public virtual ICollection<PersonelIzinHakki> IzinHaklari { get; set; } = new List<PersonelIzinHakki>();
    public virtual ICollection<PersonelAracAtama> AracAtamalari { get; set; } = new List<PersonelAracAtama>();
}

/// <summary>
/// Personel - Araç eşleştirme kaydı
/// </summary>
public class PersonelAracAtama : BaseEntity
{
    public int SoforId { get; set; }
    public virtual Sofor Sofor { get; set; } = null!;

    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;

    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;
    public DateTime? BitisTarihi { get; set; }
    public bool Aktif { get; set; } = true;
    public string? Notlar { get; set; }
}

/// <summary>
/// Personel görev türleri
/// </summary>
public enum PersonelGorev
{
    Sofor = 1,
    OfisCalisani = 2,
    Muhasebe = 3,
    Yonetici = 4,
    Teknik = 5,
    Diger = 99
}

/// <summary>
/// Personel bordro tipi (SGK bordrosuna dahil mi ve hangi tip)
/// </summary>
public enum PersonelBordroTipi
{
    Yok = 0,
    Normal = 1,
    Arge = 2
}

public enum BrutMaasHesaplamaTipi
{
    Manuel = 0,
    Saatlik = 1,
    Aylik = 2,
    Gunluk = 3
}

/// <summary>
/// SGK çalışma türü (MUHSGK Beyannamesi'nde kullanılan çalışma şekilleri)
/// </summary>
public enum SgkCalismaTuru
{
    TamZamanli = 1,           // Tam Zamanlı (Full-time) - en yaygın
    KismiZamanli = 2,         // Kısmi Süreli (Part-time)
    Cirak = 3,                // Çırak (3308 sayılı Mesleki Eğitim Kanunu)
    StajYuksekOgretim = 4,    // Stajyer - Yükseköğretim (2547 sayılı Kanun)
    IsBasiEgitimiIskur = 5,   // İş Başı Eğitim Programı (İŞKUR)
    EvHizmetleri10Alti = 6,   // Ev Hizmetleri - Ayda 10 günden az
    EvHizmetleri10Ustu = 7,   // Ev Hizmetleri - Ayda 10 gün ve üzeri
    MevsimlikTarim = 8,       // Mevsimlik Tarım İşçisi
    YabanciUyruklu = 9,       // Yabancı Uyruklu Çalışan
    EmekliAktif = 10,         // Emekli - Çalışmaya Devam Eden (5510/30.md)
    AsgariIscilikMuaf = 11,   // Asgari İşçilik Muafiyeti Kapsamında
    Engelli = 12,             // Engelli Çalışan (%3 kotası)
    EskiHukumlu = 13,         // Eski Hükümlü (%2 kotası)
    TerörMagduru = 14,        // Terör Mağduru (%1 kotası)
}






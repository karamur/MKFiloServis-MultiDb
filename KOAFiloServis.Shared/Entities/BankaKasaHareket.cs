using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Banka/Kasa hareketleri
/// </summary>
public class BankaKasaHareket : BaseEntity
{
    /// <summary>
    /// Multi-tenant: Şirket ID (null = sistem geneli)
    /// </summary>
    public int? SirketId { get; set; }
    public virtual Sirket? Sirket { get; set; }

    public string IslemNo { get; set; } = string.Empty;
    public DateTime IslemTarihi { get; set; }
    public HareketTipi HareketTipi { get; set; }
    public decimal Tutar { get; set; }
    public string? Aciklama { get; set; }
    public string? BelgeNo { get; set; } // Dekont, makbuz no vb.
    public IslemKaynak IslemKaynak { get; set; } = IslemKaynak.Manuel;

    /// <summary>
    /// Personel Cebinden Harcama: Personel kendi cebinden ödeme yaptıysa bu alan dolu olur
    /// Bu durumda BankaHesapId kullanılmaz, personele geri ödeme yapılması gerekir
    /// </summary>
    public int? PersonelCebindenId { get; set; }
    public virtual Sofor? PersonelCebinden { get; set; }

    /// <summary>
    /// Personele geri ödeme yapıldı mı?
    /// </summary>
    public bool PersoneleOdendi { get; set; } = false;

    /// <summary>
    /// Geri ödeme tarihi
    /// </summary>
    public DateTime? PersonelOdemeTarihi { get; set; }

    /// <summary>
    /// Geri ödeme yapılan banka/kasa hesabı
    /// </summary>
    public int? PersonelOdemeHesapId { get; set; }

    // Mahsup islemleri icin
    public int? MahsupHareketId { get; set; } // Iliskili karsi hareket (transfer/mahsup)
    public Guid? MahsupGrupId { get; set; } // Ayni mahsup isleminin iki hareketini gruplar

    // Muhasebe Eslestirme Kodlari
    public string? MuhasebeHesapKodu { get; set; } // Ana hesap kodu (orn: 100, 102, 320)
    public string? MuhasebeAltHesapKodu { get; set; } // Alt hesap kodu
    public string? KostMerkeziKodu { get; set; } // Masraf merkezi
    public string? ProjeKodu { get; set; } // Proje kodu
    public string? MuhasebeAciklama { get; set; } // Muhasebe icin ek aciklama

    /// <summary>
    /// İlişkili muhasebe fişi
    /// </summary>
    public int? MuhasebeFisId { get; set; }
    public virtual MuhasebeFis? MuhasebeFis { get; set; }

    [NotMapped]
    public string? MuhasebeFisNo { get; set; }

    [NotMapped]
    public string? MuhasebeFisDurumu { get; set; }

    [NotMapped]
    public string? IptalFisNo { get; set; }

    // Foreign Keys
    public int BankaHesapId { get; set; }
    public int? CariId { get; set; } // Iliskili cari varsa
    public int? AracId { get; set; } // Iliskili arac varsa (ozellikle personel cebinden arac masraflari icin)

    // Navigation Properties
    public virtual BankaHesap BankaHesap { get; set; } = null!;
    public virtual Cari? Cari { get; set; }
    public virtual Arac? Arac { get; set; }
    public virtual ICollection<OdemeEslestirme> OdemeEslestirmeleri { get; set; } = new List<OdemeEslestirme>();

    /// <summary>
    /// Personel cebinden harcama mı?
    /// </summary>
    [NotMapped]
    public bool IsPersonelCebinden => PersonelCebindenId.HasValue;
}

public enum HareketTipi
{
    Giris = 1,      // Tahsilat
    Cikis = 2       // Odeme
}

public enum IslemKaynak
{
    Manuel = 1,
    FaturaOdeme = 2,
    FaturaTahsilat = 3,
    Havale = 4,
    Eft = 5,
    Nakit = 6,
    Butce = 7,
    EFatura = 8,
    Mahsup = 9,         // Hesaplar arasi transfer
    CariMahsup = 10,    // Cari hesap mahsubu
    PersonelCebinden = 11 // Personel cebinden harcama
}

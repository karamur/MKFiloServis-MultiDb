namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Cari hesap (Musteri/Tedarikci/Firma/Personel)
/// </summary>
public class Cari : BaseEntity
{
    /// <summary>
    /// Multi-tenant: Şirket ID (null = sistem geneli)
    /// </summary>
    public int? SirketId { get; set; }
    public virtual Sirket? Sirket { get; set; }

    public string CariKodu { get; set; } = string.Empty;
    public string Unvan { get; set; } = string.Empty;
    public CariTipi CariTipi { get; set; }
    public string? VergiDairesi { get; set; }
    public string? VergiNo { get; set; }
    public string? TcKimlikNo { get; set; } // TC Kimlik Numarasi
    public string? Adres { get; set; }
    public string? Il { get; set; }
    public string? Ilce { get; set; }
    public string? PostaKodu { get; set; }
    public string? Telefon { get; set; }
    public string? Telefon2 { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? WebSitesi { get; set; }
    public string? YetkiliKisi { get; set; }
    public string? Notlar { get; set; }
    public bool Aktif { get; set; } = true;

    // Borc/Alacak (hesaplanan degerler - veritabaninda saklanmaz)
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }

    // Muhasebe Hesap Eslestirme
    public int? MuhasebeHesapId { get; set; } // 120.xxx veya 320.xxx veya 335.xx.xxx (personel)
    public virtual MuhasebeHesap? MuhasebeHesap { get; set; }

    // Personel iş avans alacak hesabı (195.01.xxx) - sadece personel cariler icin
    public int? PersonelAvansHesapId { get; set; }
    public virtual MuhasebeHesap? PersonelAvansHesap { get; set; }

    // Firma iliskisi (coklu firma destegi)
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }
    
    // Personel iliskisi (Personel carisi ise sofor/personel ile eslestir)
    public int? SoforId { get; set; }
    public virtual Sofor? Sofor { get; set; }

    // Taşıma tedarikçi sözleşme bilgileri (Tedarikçi/Müşteri+Tedarikçi cariler için)
    public string? SozlesmeNo { get; set; }
    public DateTime? SozlesmeBaslangicTarihi { get; set; }
    public DateTime? SozlesmeBitisTarihi { get; set; }

    // Navigation Properties
    public virtual ICollection<Fatura> Faturalar { get; set; } = new List<Fatura>();
    public virtual ICollection<Guzergah> Guzergahlar { get; set; } = new List<Guzergah>();
    public virtual ICollection<BankaKasaHareket> BankaKasaHareketler { get; set; } = new List<BankaKasaHareket>();
    public virtual ICollection<KullaniciCari> KullaniciEslestirmeleri { get; set; } = new List<KullaniciCari>();
    public virtual ICollection<CariIletisimNot> IletisimNotlari { get; set; } = new List<CariIletisimNot>();
    public virtual ICollection<Hatirlatici> Hatirlaticilar { get; set; } = new List<Hatirlatici>();
    public virtual ICollection<CariHatirlatma> CariHatirlatmalar { get; set; } = new List<CariHatirlatma>();
    public virtual ICollection<CariSeferUcreti> SeferUcretleri { get; set; } = new List<CariSeferUcreti>();
}

public enum CariTipi
{
    Musteri = 1,
    Tedarikci = 2,
    MusteriTedarikci = 3,
    Personel = 4
}

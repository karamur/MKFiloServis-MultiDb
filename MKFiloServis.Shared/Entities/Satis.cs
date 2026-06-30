using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

#region Satis Modulu

/// <summary>
/// Satis Personeli
/// </summary>
public class SatisPersoneli : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string PersonelKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string AdSoyad { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Telefon { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    public bool Aktif { get; set; } = true;

    // Komisyon Bilgileri
    public decimal KomisyonOrani { get; set; } = 2; // Yuzde
    public decimal SabitKomisyon { get; set; } = 0;

    // Hedefler
    public decimal AylikSatisHedefi { get; set; } = 0;
    public int AylikAracHedefi { get; set; } = 0;

    // Navigation
    public virtual ICollection<AracIlan> Ilanlar { get; set; } = new List<AracIlan>();
    public virtual ICollection<AracSatis> Satislar { get; set; } = new List<AracSatis>();
}

/// <summary>
/// Satilacak Arac Ilani
/// </summary>
public class AracIlan : BaseEntity
{
    [Required]
    [StringLength(15)]
    public string Plaka { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Marka { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    public int ModelYili { get; set; }

    [StringLength(50)]
    public string? Versiyon { get; set; } // 1.6 TDI, 1.4 TSI vs.

    public int Kilometre { get; set; }

    public YakitTuru YakitTuru { get; set; } = YakitTuru.Benzin;
    public VitesTuru VitesTuru { get; set; } = VitesTuru.Manuel;
    public KasaTipi KasaTipi { get; set; } = KasaTipi.Sedan;
    public AracRenk Renk { get; set; } = AracRenk.Beyaz;

    // Durum Bilgileri
    public AracDurum Durum { get; set; } = AracDurum.Sifir;
    public bool Boyali { get; set; } = false;
    public int BoyaliParcaSayisi { get; set; } = 0;
    public string? BoyaliParcalar { get; set; } // JSON veya virgul ayracli

    public bool DegisenVar { get; set; } = false;
    public int DegisenParcaSayisi { get; set; } = 0;
    public string? DegisenParcalar { get; set; }

    public bool HasarKaydi { get; set; } = false;
    public string? HasarAciklama { get; set; }

    public bool TramerKaydi { get; set; } = false;
    public decimal TramerTutari { get; set; } = 0;

    // Fiyat Bilgileri
    public decimal AlisFiyati { get; set; }
    public decimal SatisFiyati { get; set; }
    public decimal KaskoDegeri { get; set; }
    public decimal PiyasaDegeriMin { get; set; }
    public decimal PiyasaDegeriMax { get; set; }
    public decimal PiyasaDegeriOrtalama { get; set; }

    // Ilan Bilgileri
    public IlanDurum IlanDurum { get; set; } = IlanDurum.Aktif;
    public DateTime IlanTarihi { get; set; } = DateTime.Today;
    public DateTime? SatisTarihi { get; set; }

    public string? Aciklama { get; set; }
    public string? Notlar { get; set; }
    public string? Fotograflar { get; set; } // JSON array

    // Sahiplik
    public int? SahipCariId { get; set; }
    public virtual Cari? SahipCari { get; set; }

    public int? SatisPersoneliId { get; set; }
    public virtual SatisPersoneli? SatisPersoneli { get; set; }

    // Piyasa Karsilastirma
    public virtual ICollection<PiyasaIlan> PiyasaIlanlari { get; set; } = new List<PiyasaIlan>();
}

/// <summary>
/// Piyasa Ilan Karsilastirmasi (sahibinden, arabam vs.)
/// </summary>
public class PiyasaIlan : BaseEntity
{
    public int AracIlanId { get; set; }
    public virtual AracIlan AracIlan { get; set; } = null!;

    public PiyasaKaynagi Kaynak { get; set; } = PiyasaKaynagi.Sahibinden;

    public string? IlanUrl { get; set; }
    public string? IlanNo { get; set; }

    public decimal Fiyat { get; set; }

    [StringLength(100)]
    public string? Sehir { get; set; }

    [StringLength(100)]
    public string? Ilce { get; set; }

    public int Kilometre { get; set; }
    public int Yil { get; set; }

    public string? Durum { get; set; } // Sifir, Ikinci El vs.
    public int BoyaliParca { get; set; }
    public int DegisenParca { get; set; }

    public bool TramerVar { get; set; }
    public decimal? TramerTutari { get; set; }

    public DateTime TaramaTarihi { get; set; } = DateTime.Now;

    public string? EkBilgiler { get; set; } // JSON
}

/// <summary>
/// Arac Satis Kaydi
/// </summary>
public class AracSatis : BaseEntity
{
    public int AracIlanId { get; set; }
    public virtual AracIlan AracIlan { get; set; } = null!;

    public int? AliciCariId { get; set; }
    public virtual Cari? AliciCari { get; set; }

    public int? SatisPersoneliId { get; set; }
    public virtual SatisPersoneli? SatisPersoneli { get; set; }

    public DateTime SatisTarihi { get; set; } = DateTime.Today;

    public decimal SatisFiyati { get; set; }
    public decimal KomisyonTutari { get; set; }

    public SatisOdemeSekli OdemeSekli { get; set; } = SatisOdemeSekli.Nakit;

    public string? Notlar { get; set; }
}

#endregion

#region Enums

public enum YakitTuru
{
    Benzin = 1,
    Dizel = 2,
    LPG = 3,
    Hibrit = 4,
    Elektrik = 5,
    BenzinLPG = 6
}

public enum VitesTuru
{
    Manuel = 1,
    Otomatik = 2,
    YariOtomatik = 3
}

public enum KasaTipi
{
    Sedan = 1,
    Hatchback = 2,
    StationWagon = 3,
    SUV = 4,
    Coupe = 5,
    Cabrio = 6,
    Pickup = 7,
    Minivan = 8,
    Panelvan = 9
}

public enum AracRenk
{
    Beyaz = 1,
    Siyah = 2,
    Gri = 3,
    Gumus = 4,
    Mavi = 5,
    Kirmizi = 6,
    Lacivert = 7,
    Yesil = 8,
    Kahverengi = 9,
    Bej = 10,
    Sari = 11,
    Turuncu = 12,
    Mor = 13,
    Pembe = 14
}

public enum AracDurum
{
    Sifir = 1,
    IkinciEl = 2
}

public enum IlanDurum
{
    Aktif = 1,
    Pasif = 2,
    Satildi = 3,
    Rezerve = 4
}

public enum PiyasaKaynagi
{
    Sahibinden = 1,
    Arabam = 2,
    Araba = 3,
    LetGo = 4,
    Facebook = 5,
    Instagram = 6,
    Diger = 99
}

public enum SatisOdemeSekli
{
    Nakit = 1,
    Kredi = 2,
    Takas = 3,
    NakitKredi = 4,
    TakasNakit = 5
}

#endregion

#region Marka/Model Referans

/// <summary>
/// Arac Marka Listesi
/// </summary>
public class AracMarka : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string MarkaAdi { get; set; } = string.Empty;

    public string? Logo { get; set; }
    public int SiraNo { get; set; }
    public bool Aktif { get; set; } = true;

    public virtual ICollection<AracModelTanim> Modeller { get; set; } = new List<AracModelTanim>();
}

/// <summary>
/// Arac Model Tanimlari
/// </summary>
public class AracModelTanim : BaseEntity
{
    public int MarkaId { get; set; }
    public virtual AracMarka Marka { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string ModelAdi { get; set; } = string.Empty;

    public int BaslangicYili { get; set; }
    public int? BitisYili { get; set; }

    public KasaTipi VarsayilanKasaTipi { get; set; }

    public bool Aktif { get; set; } = true;
}

#endregion



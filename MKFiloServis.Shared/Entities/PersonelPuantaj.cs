using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Personel puantaj kaydı
/// </summary>
public class PersonelPuantaj : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }
    int? IFirmaTenant.FirmaId { get => FirmaId; set => FirmaId = value ?? 0; }

    [Required]
    public int PersonelId { get; set; } // Sofor veya diger personel

    [Required]
    public int Yil { get; set; }

    [Required]
    [Range(1, 12)]
    public int Ay { get; set; }

    /// <summary>
    /// Çalışılan gün sayısı
    /// </summary>
    public int CalisilanGun { get; set; }

    /// <summary>
    /// Fazla mesai saati
    /// </summary>
    public decimal FazlaMesaiSaat { get; set; }

    /// <summary>
    /// İzin günü
    /// </summary>
    public int IzinGunu { get; set; }

    /// <summary>
    /// Mazeret/Rapor günü
    /// </summary>
    public int MazeretGunu { get; set; }

    /// <summary>
    /// Brüt maaş
    /// </summary>
    public decimal BrutMaas { get; set; }

    /// <summary>
    /// Yemek ücreti
    /// </summary>
    public decimal YemekUcreti { get; set; }

    /// <summary>
    /// Yol ücreti
    /// </summary>
    public decimal YolUcreti { get; set; }

    /// <summary>
    /// Prim
    /// </summary>
    public decimal Prim { get; set; }

    /// <summary>
    /// Diğer ödeme
    /// </summary>
    public decimal DigerOdeme { get; set; }

    /// <summary>
    /// SGK kesintisi
    /// </summary>
    public decimal SgkKesinti { get; set; }

    /// <summary>
    /// Gelir vergisi
    /// </summary>
    public decimal GelirVergisi { get; set; }

    /// <summary>
    /// Damga vergisi
    /// </summary>
    public decimal DamgaVergisi { get; set; }

    /// <summary>
    /// Diğer kesintiler
    /// </summary>
    public decimal DigerKesinti { get; set; }

    /// <summary>
    /// Net ödeme
    /// </summary>
    public decimal NetOdeme { get; set; }

    /// <summary>
    /// Ödeme tarihi
    /// </summary>
    public DateTime? OdemeTarihi { get; set; }

    /// <summary>
    /// Ödeme durumu
    /// </summary>
    public bool Odendi { get; set; }

    /// <summary>
    /// Puantaj onay durumu
    /// </summary>
    public PersonelPuantajOnayDurumu OnayDurumu { get; set; } = PersonelPuantajOnayDurumu.Taslak;

    /// <summary>
    /// Onaylayan kullanıcı
    /// </summary>
    public string? OnaylayanKullanici { get; set; }

    /// <summary>
    /// Onay tarihi
    /// </summary>
    public DateTime? OnayTarihi { get; set; }

    /// <summary>
    /// Onay / red notu
    /// </summary>
    public string? OnayNotu { get; set; }

    /// <summary>
    /// Banka hesap numarası (IBAN)
    /// </summary>
    public string? BankaHesapNo { get; set; }

    public string? Aciklama { get; set; }

    // Navigation
    public virtual Firma? Firma { get; set; }
    public virtual Sofor? Personel { get; set; }
}

/// <summary>
/// Günlük puantaj detayı
/// </summary>
public class GunlukPuantaj : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Required]
    public int PersonelPuantajId { get; set; }

    /// <summary>
    /// Ayın kaçıncı günü (1-31)
    /// </summary>
    [Required]
    public int Gun { get; set; }

    [Required]
    public DateTime Tarih { get; set; }

    /// <summary>
    /// Puantaj durumu (0: Boş, 1: Çalıştı, 2: İzinli, 3: Mazeret/Rapor)
    /// </summary>
    public int Durum { get; set; } = 0;

    /// <summary>
    /// Çalıştı mı?
    /// </summary>
    public bool Calisti { get; set; }

    /// <summary>
    /// Fazla mesai saati
    /// </summary>
    public decimal FazlaMesaiSaat { get; set; }

    /// <summary>
    /// Çalışma saati (saatlik personeller için)
    /// </summary>
    public decimal CalismaSaati { get; set; }

    /// <summary>
    /// İzinli mi?
    /// </summary>
    public bool Izinli { get; set; }

    /// <summary>
    /// Mazeret/Rapor
    /// </summary>
    public bool Mazeret { get; set; }

    /// <summary>
    /// Çalıştığı güzergah/sefer
    /// </summary>
    public int? ServisCalismaId { get; set; }

    public string? Notlar { get; set; }

    // Navigation
    public virtual PersonelPuantaj? PersonelPuantaj { get; set; }
    public virtual ServisCalisma? ServisCalisma { get; set; }
}

public enum PersonelPuantajOnayDurumu
{
    Taslak = 0,
    OnayBekliyor = 1,
    Onaylandi = 2,
    Reddedildi = 3
}



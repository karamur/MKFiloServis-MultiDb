using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Sistem aktivite logu — tum onemli islemlerin kaydi (Kural 14).
/// </summary>
public class AktiviteLog : BaseEntity
{
    [Required]
    public DateTime IslemZamani { get; set; } = DateTime.Now;

    [Required]
    public string IslemTipi { get; set; } = string.Empty; // Ekleme, Guncelleme, Silme, Giris, Cikis

    [Required]
    public string Modul { get; set; } = string.Empty; // Cari, Arac, Fatura, vb.

    public string? EntityTipi { get; set; } // Entity sinif adi

    public int? EntityId { get; set; }

    public string? EntityAdi { get; set; } // Cari adi, Plaka, vb.

    public string? Aciklama { get; set; }

    public string? EskiDeger { get; set; } // JSON formatinda

    public string? YeniDeger { get; set; } // JSON formatinda

    public string? KullaniciAdi { get; set; }

    /// <summary>
    /// Kural 14: Audit log'a FirmaId eklendi. Hangi firmada islem yapildigini kaydeder.
    /// </summary>
    public int? FirmaId { get; set; }

    /// <summary>
    /// Islemi yapan kullanici Id'si.
    /// </summary>
    public int? KullaniciId { get; set; }

    public string? IpAdresi { get; set; }

    public string? Tarayici { get; set; }

    public AktiviteSeviye Seviye { get; set; } = AktiviteSeviye.Bilgi;

    // Navigation
    public virtual Firma? Firma { get; set; }
}

public enum AktiviteSeviye
{
    Bilgi = 1,
    Uyari = 2,
    Hata = 3,
    Kritik = 4
}

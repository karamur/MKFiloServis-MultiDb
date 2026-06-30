using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Piyasa ara�t�rma kaynaklar� (web siteleri)
/// </summary>
public class PiyasaKaynak
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Ad { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Kod { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string BaseUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AramaUrl { get; set; }

    [MaxLength(1000)]
    public string? AramaParametreleri { get; set; }

    /// <summary>
    /// CSS Selector'lar JSON format�nda
    /// </summary>
    [MaxLength(2000)]
    public string? Selectors { get; set; }

    /// <summary>
    /// Hangi markalar i�in ge�erli (bo� ise t�m markalar)
    /// </summary>
    [MaxLength(500)]
    public string? DesteklenenMarkalar { get; set; }

    /// <summary>
    /// Kaynak tipi: Genel, YetkiliBayi, Galeri, Sifir
    /// </summary>
    [MaxLength(50)]
    public string KaynakTipi { get; set; } = "Genel";

    public int Sira { get; set; } = 99;

    public bool Aktif { get; set; } = true;

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public DateTime? GuncellemeTarihi { get; set; }

    public bool IsDeleted { get; set; } = false;
}

/// <summary>
/// Selector yap�land�rmas�
/// </summary>
public class PiyasaKaynakSelector
{
    public string IlanListesi { get; set; } = "";
    public string IlanLink { get; set; } = "";
    public string IlanBaslik { get; set; } = "";
    public string Fiyat { get; set; } = "";
    public string Yil { get; set; } = "";
    public string Kilometre { get; set; } = "";
    public string Resim { get; set; } = "";
    public string Konum { get; set; } = "";
    public string Tarih { get; set; } = "";
    public string CookieKabul { get; set; } = "";
}



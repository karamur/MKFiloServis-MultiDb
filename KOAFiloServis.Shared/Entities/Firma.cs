using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Firma Bilgileri - Coklu firma destegi
/// </summary>
public class Firma : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string FirmaKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string FirmaAdi { get; set; } = string.Empty;

    [StringLength(250)]
    public string? UnvanTam { get; set; }

    [StringLength(11)]
    public string? VergiNo { get; set; }

    [StringLength(100)]
    public string? VergiDairesi { get; set; }

    [StringLength(500)]
    public string? Adres { get; set; }

    [StringLength(100)]
    public string? Il { get; set; }

    [StringLength(100)]
    public string? Ilce { get; set; }

    [StringLength(20)]
    public string? Telefon { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? WebSite { get; set; }

    public string? Logo { get; set; } // Base64 veya dosya yolu

    public bool Aktif { get; set; } = true;
    public bool VarsayilanFirma { get; set; } = false;

    public int SiraNo { get; set; } = 0;

    // ----------------------------------------------------------------
    // Organizasyon ve Şube (Nihai Mimari Kural 2, Kural 3, Kural 5)
    // ----------------------------------------------------------------

    /// <summary>
    /// Firmanın bağlı olduğu organizasyon Id'si (Kural 3: Her firma bir organizasyona bağlıdır).
    /// </summary>
    public int OrganizasyonId { get; set; } = 1; // Varsayılan: Üstün Holding

    /// <summary>
    /// Firmanın bağlı olduğu organizasyon.
    /// </summary>
    public virtual Organizasyon? Organizasyon { get; set; }

    /// <summary>
    /// Bu firmaya bağlı şubeler (Kural 5).
    /// </summary>
    public virtual ICollection<Sube> Subeler { get; set; } = new List<Sube>();

    /// <summary>
    /// Bu firma "kurum" rolünde de görünüyorsa (yani başka bir firmamız ona fatura kesiyorsa),
    /// muhasebe tarafında temsil ettiği Cari kaydı. Kurum↔Firma↔Cari eşleştirme için halen aktif.
    /// (Tenant izolasyonu için kullanılmıyor; sadece muhasebe eşleştirmesi.)
    /// </summary>
    public int? CariId { get; set; }

    /// <summary>
    /// Bu firmaya bağlı cari kartlar (Cari.FirmaId).
    /// </summary>
    public virtual ICollection<Cari> Cariler { get; set; } = new List<Cari>();

    // Muhasebe Donem Bilgisi
    public int AktifDonemYil { get; set; } = DateTime.Today.Year;
    public int AktifDonemAy { get; set; } = DateTime.Today.Month;

    /// <summary>
    /// [LEGACY] Per-firma dedicated database adı.
    /// Nihai mimari kararı (2026) ile kullanımdan kaldırılmıştır.
    /// Tüm firmalar artık tek KOAFiloServis veritabanında çalışır.
    /// Geriye dönük uyumluluk için NULL olarak bırakılır.
    /// </summary>
    [StringLength(100)]
    public string? DatabaseName { get; set; }
}

/// <summary>
/// Aktif firma bilgisini tutan servis
/// </summary>
public class AktifFirmaBilgisi
{
    public int FirmaId { get; set; }
    public string FirmaKodu { get; set; } = "";
    public string FirmaAdi { get; set; } = "";
    public int AktifDonemYil { get; set; } = DateTime.Today.Year;
    public int AktifDonemAy { get; set; } = DateTime.Today.Month;
    public bool TumFirmalar { get; set; } = false;
    public string? DatabaseName { get; set; }
}

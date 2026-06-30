using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Organizasyon (Holding) — hiyerarşinin en üst seviyesi (Kural 2).
/// </summary>
/// <remarks>
/// <para><b>Organizasyon Yapısı:</b></para>
/// <code>
/// ÜSTÜN HOLDİNG (Id=1)
/// ├── ÜSTÜN GRUP
/// ├── ÜSTÜN FİLO
/// └── RECEP ÜSTÜN
/// </code>
/// <para>
/// Her firma bir organizasyona bağlıdır (Kural 3).
/// Organizasyon bilgisi iş tablolarında doğrudan tutulmaz;
/// Firma üzerinden çözümlenir (Kural 10).
/// </para>
/// <para>
/// Organizasyon yöneticisi tüm alt firmaları görebilir ve
/// konsolide rapor alabilir.
/// </para>
/// </remarks>
public class Organizasyon : BaseEntity
{
    /// <summary>
    /// Organizasyon adı (örn: "Üstün Holding", "Üstün Grup").
    /// </summary>
    [Required]
    [StringLength(250)]
    public string Adi { get; set; } = string.Empty;

    /// <summary>
    /// Organizasyon kısa kodu / prefix (opsiyonel).
    /// </summary>
    [StringLength(20)]
    public string? Kod { get; set; }

    /// <summary>
    /// Organizasyon açıklaması (opsiyonel).
    /// </summary>
    [StringLength(500)]
    public string? Aciklama { get; set; }

    /// <summary>
    /// Bu organizasyona bağlı firmalar.
    /// </summary>
    public virtual ICollection<Firma> Firmalar { get; set; } = new List<Firma>();
}



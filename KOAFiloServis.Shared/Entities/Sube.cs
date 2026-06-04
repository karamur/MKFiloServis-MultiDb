using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Şube — Firma altı lokasyon/bölüm (Kural 2, Kural 5).
/// </summary>
/// <remarks>
/// <para><b>Hiyerarşi:</b> Organizasyon → Firma → Şube</para>
/// <para>
/// Gelecekteki büyüme için şube bazlı yönetim desteği.
/// İş tablolarında <c>SubeId INT NULL</c> olarak yer alır;
/// şube kullanılmayan modüllerde NULL bırakılır (Kural 5).
/// </para>
/// </remarks>
public class Sube : BaseEntity
{
    /// <summary>
    /// Şubenin bağlı olduğu firma Id'si.
    /// </summary>
    [Required]
    public int FirmaId { get; set; }

    /// <summary>
    /// Şube adı (örn: "Merkez", "Ankara Şube", "Depo").
    /// </summary>
    [Required]
    [StringLength(250)]
    public string SubeAdi { get; set; } = string.Empty;

    /// <summary>
    /// Şube kodu (opsiyonel).
    /// </summary>
    [StringLength(20)]
    public string? SubeKodu { get; set; }

    /// <summary>
    /// Şube adresi (opsiyonel).
    /// </summary>
    [StringLength(500)]
    public string? Adres { get; set; }

    /// <summary>
    /// Şube telefonu (opsiyonel).
    /// </summary>
    [StringLength(20)]
    public string? Telefon { get; set; }

    /// <summary>
    /// Şube aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;

    // ----------------------------------------------------------------
    // Navigasyon
    // ----------------------------------------------------------------

    /// <summary>
    /// Şubenin bağlı olduğu firma.
    /// </summary>
    public virtual Firma? Firma { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Firma bazlı tüm iş tabloları için ortak temel sınıf (Kural 9).
/// </summary>
/// <remarks>
/// <para><b>Mimari Kurallar:</b></para>
/// <list type="bullet">
///   <item><b>Kural 4:</b> FirmaId INT NOT NULL — tüm iş kayıtları bir firmaya ait olmak zorundadır.</item>
///   <item><b>Kural 5:</b> SubeId INT NULL — gelecekteki şube desteği için; kullanılmayan modüllerde NULL.</item>
///   <item><b>Kural 8:</b> Firma silinemez — IsActive ile pasifleştirme, DeletedByUserId ile izleme.</item>
///   <item><b>Kural 9:</b> Ortak taban sınıf — yeni tablolarda FirmaId unutulmasını engeller, kod tekrarını azaltır.</item>
///   <item><b>Kural 16:</b> Soft delete — fiziksel silme yapılmaz; IsDeleted + DeletedAt ile işaretleme.</item>
/// </list>
/// <para>
/// Bu sınıf <see cref="IFirmaTenant"/> arayüzünü explicit olarak implemente eder;
/// böylece mevcut ApplicationDbContext tenant filter ve auto-assign altyapısıyla
/// geriye dönük uyumlu çalışır.
/// </para>
/// <para><b>Yeni geliştirilen her iş tablosu bu sınıftan türetilmelidir (Kural 18).</b></para>
/// </remarks>
public abstract class FirmaBaseEntity : BaseEntity, IFirmaTenant
{
    /// <summary>
    /// Kaydın ait olduğu firma Id'si (Kural 4: INT NOT NULL).
    /// </summary>
    [Required]
    public int FirmaId { get; set; }

    /// <summary>
    /// Kaydın ait olduğu şube Id'si (Kural 5: INT NULL).
    /// Şube kullanılmayan modüllerde NULL bırakılır.
    /// </summary>
    public int? SubeId { get; set; }

    /// <summary>
    /// Kaydın aktif olup olmadığı (Kural 8).
    /// Silme yerine false yapılarak pasifleştirme yapılır.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Silme işlemini yapan kullanıcı Id'si (Kural 8, Kural 16).
    /// </summary>
    public int? DeletedByUserId { get; set; }

    // ----------------------------------------------------------------
    // Navigasyon Özellikleri
    // ----------------------------------------------------------------

    /// <summary>
    /// Kaydın ait olduğu firma.
    /// </summary>
    public virtual Firma? Firma { get; set; }

    /// <summary>
    /// Kaydın ait olduğu şube (opsiyonel).
    /// </summary>
    public virtual Sube? Sube { get; set; }

    // ----------------------------------------------------------------
    // IFirmaTenant Explicit Implementasyonu (Geriye Dönük Uyum)
    // ----------------------------------------------------------------

    /// <summary>
    /// IFirmaTenant arayüzünün nullable FirmaId gereksinimini,
    /// bu sınıfın non-nullable FirmaId alanı üzerinden karşılar.
    /// </summary>
    /// <remarks>
    /// ApplicationDbContext'in global tenant filter ve SaveChanges auto-assign
    /// mantığı IFirmaTenant üzerinden çalıştığı için bu köprü korunur.
    /// Set tarafında null gelmesi durumunda 0 atanır; bu, auto-assign
    /// mantığının (AssignFirmaTenantId) devreye girmesini sağlar.
    /// </remarks>
    int? IFirmaTenant.FirmaId
    {
        get => FirmaId;
        set => FirmaId = value ?? 0;
    }
}



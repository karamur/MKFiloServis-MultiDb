namespace KOAFiloServis.Shared.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Soft delete zaman damgası (Kural 16).
    /// Fiziksel silme yapılmaz; bu alan silme anını kaydeder.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}

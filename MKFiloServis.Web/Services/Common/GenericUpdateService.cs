using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services.Common;

/// <summary>
/// TÜM edit ekranları için TEK ORTAK update pattern'i.
/// Fetch tracked entity → SetValues → SaveChanges.
/// Update() / Attach() / AsNoTracking KULLANMAZ.
/// </summary>
public class GenericUpdateService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<GenericUpdateService> _logger;

    public GenericUpdateService(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<GenericUpdateService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Herhangi bir BaseEntity alt sınıfını günceller.
    /// DB'den tracked olarak çeker → CurrentValues.SetValues → SaveChanges.
    /// Concurrency güvenli, audit otomatik, silent fail imkansız.
    /// </summary>
    public async Task<T> UpdateAsync<T>(T entity) where T : BaseEntity
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Set<T>().FindAsync(entity.Id);

        if (existing == null)
        {
            _logger.LogError("UPDATE FAILED: {Entity} not found Id={Id}", typeof(T).Name, entity.Id);
            throw new InvalidOperationException($"{typeof(T).Name} bulunamadı (Id={entity.Id})");
        }

        try
        {
            // 🔴 SAFE COPY: Tracked entity'ye UI değerlerini kopyala
            db.Entry(existing).CurrentValues.SetValues(entity);

            // 🔴 AUDIT: UpdatedAt otomatik
            existing.UpdatedAt = DateTime.UtcNow;

            // 🔴 SAVE: Garanti persist
            await db.SaveChangesAsync();

            _logger.LogInformation("UPDATE OK: {Entity} Id={Id}", typeof(T).Name, entity.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "CONCURRENCY ERROR: {Entity} Id={Id}", typeof(T).Name, entity.Id);
            throw new InvalidOperationException("Kayıt başka kullanıcı tarafından değiştirilmiş. Lütfen yenileyip tekrar deneyin.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UPDATE ERROR: {Entity} Id={Id}", typeof(T).Name, entity.Id);
            throw;
        }

        return existing;
    }

    /// <summary>
    /// Navigation property hariç güncelleme (ilişkili entity'leri etkilemez).
    /// </summary>
    public async Task<T> UpdateExcludingNavigationsAsync<T>(T entity, params string[] excludeProperties) where T : BaseEntity
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Set<T>().FindAsync(entity.Id);
        if (existing == null)
            throw new InvalidOperationException($"{typeof(T).Name} bulunamadı. Id={entity.Id}");

        var entry = db.Entry(existing);
        entry.CurrentValues.SetValues(entity);

        // Belirtilen navigation property'leri modified olarak işaretleme
        foreach (var prop in excludeProperties)
        {
            if (entry.References.Any(r => r.Metadata.Name == prop))
                entry.Reference(prop).IsModified = false;
            if (entry.Member(prop) != null)
                entry.Property(prop).IsModified = false;
        }

        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogDebug("SAVE (exclude nav): {Type} Id={Id}", typeof(T).Name, entity.Id);
        return existing;
    }
}




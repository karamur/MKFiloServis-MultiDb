using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Services;

/// <summary>Toplu iş yazımlarını ApplicationDbContext SaveChanges/audit akışından geçirir.</summary>
internal static class TrackedWriteExtensions
{
    internal static async Task<int> UpdateTrackedAsync<T>(this IQueryable<T> query,
        ApplicationDbContext context, Action<T> update, CancellationToken cancellationToken = default, bool requireSingleRecord = false) where T : class
    {
        var records = await query.AsTracking().ToListAsync(cancellationToken);
        if (requireSingleRecord && records.Count != 1)
            throw new InvalidOperationException("Güncellenecek kayıt bulunamadı veya kapsamı tekil değil.");
        var before = records.Select(record =>
        {
            var entry = context.Entry(record);
            return (Entry: entry, Values: entry.CurrentValues.Clone(), State: entry.State,
                Modified: entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToHashSet());
        }).ToList();
        try
        {
            foreach (var record in records) update(record);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Bir satır hatası yakalanıp sonraki satıra geçildiğinde başarısız değişikliği yeniden kaydetme.
            foreach (var saved in before)
            {
                saved.Entry.CurrentValues.SetValues(saved.Values);
                saved.Entry.State = saved.State;
                foreach (var property in saved.Entry.Properties)
                    property.IsModified = saved.Modified.Contains(property.Metadata.Name);
            }
            throw;
        }
        return records.Count;
    }
}

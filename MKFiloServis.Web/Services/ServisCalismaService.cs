using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class ServisCalismaService : IServisCalismaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ServisCalismaService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private static IQueryable<ServisCalisma> CreateReadQuery(ApplicationDbContext context, bool includeArizaMasraflari = false)
    {
        IQueryable<ServisCalisma> query = context.ServisCalismalari
            .AsNoTracking()
            .Where(s => !s.IsDeleted);

        query = query.Include(s => s.Arac);
        query = query.Include(s => s.Sofor);
        query = query.Include(s => s.Guzergah)
            .ThenInclude(g => g.Cari);

        if (includeArizaMasraflari)
        {
            query = query.Include(s => s.ArizaMasraflari);
        }

        return query;
    }

    public async Task<List<ServisCalisma>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await CreateReadQuery(context)
            .OrderByDescending(s => s.CalismaTarihi)
            .ToListAsync();
    }

    public async Task<List<ServisCalisma>> GetRecentAsync(int count = 5)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            return await CreateReadQuery(context)
                .OrderByDescending(s => s.CalismaTarihi)
                .Take(count)
                .ToListAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42703")
        {
            // Eski tenant şemasında include zincirindeki bazı kolonlar eksik olabilir.
            return await context.ServisCalismalari
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.CalismaTarihi)
                .Take(count)
                .ToListAsync();
        }
    }

    public async Task<List<ServisCalisma>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await CreateReadQuery(context)
            .Where(s => s.CalismaTarihi >= startDate && s.CalismaTarihi <= endDate)
            .OrderByDescending(s => s.CalismaTarihi)
            .ToListAsync();
    }

    public async Task<List<ServisCalisma>> GetByAracIdAsync(int aracId, DateTime? startDate = null, DateTime? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = CreateReadQuery(context)
            .Where(s => s.AracId == aracId);

        if (startDate.HasValue)
            query = query.Where(s => s.CalismaTarihi >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(s => s.CalismaTarihi <= endDate.Value);

        return await query.OrderByDescending(s => s.CalismaTarihi).ToListAsync();
    }

    public async Task<List<ServisCalisma>> GetBySoforIdAsync(int soforId, DateTime? startDate = null, DateTime? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = CreateReadQuery(context)
            .Where(s => s.SoforId == soforId);

        if (startDate.HasValue)
            query = query.Where(s => s.CalismaTarihi >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(s => s.CalismaTarihi <= endDate.Value);

        return await query.OrderByDescending(s => s.CalismaTarihi).ToListAsync();
    }

    public async Task<List<ServisCalisma>> GetByGuzergahIdAsync(int guzergahId, DateTime? startDate = null, DateTime? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = CreateReadQuery(context)
            .Where(s => s.GuzergahId == guzergahId);

        if (startDate.HasValue)
            query = query.Where(s => s.CalismaTarihi >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(s => s.CalismaTarihi <= endDate.Value);

        return await query.OrderByDescending(s => s.CalismaTarihi).ToListAsync();
    }

    public async Task<List<ServisCalisma>> GetByCariIdAsync(int cariId, DateTime? startDate = null, DateTime? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = CreateReadQuery(context)
            .Where(s => s.Guzergah.CariId == cariId);

        if (startDate.HasValue)
            query = query.Where(s => s.CalismaTarihi >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(s => s.CalismaTarihi <= endDate.Value);

        return await query.OrderByDescending(s => s.CalismaTarihi).ToListAsync();
    }

    public async Task<ServisCalisma?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await CreateReadQuery(context, includeArizaMasraflari: true)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<ServisCalisma> CreateAsync(ServisCalisma servisCalisma)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Güzergah fiyatını al
        if (!servisCalisma.Fiyat.HasValue)
        {
            var guzergah = await context.Guzergahlar.FindAsync(servisCalisma.GuzergahId);
            if (guzergah != null)
            {
                servisCalisma.Fiyat = guzergah.BirimFiyat;
            }
        }

        context.ServisCalismalari.Add(servisCalisma);
        await context.SaveChangesAsync();
        return servisCalisma;
    }

    public async Task<ServisCalisma> UpdateAsync(ServisCalisma servisCalisma)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.ServisCalismalari.FindAsync(servisCalisma.Id);
        if (existing == null)
            throw new InvalidOperationException($"ServisCalisma bulunamadı: {servisCalisma.Id}");

        // 🔴 Fetch + map + SaveChanges — Update()/Attach() KULLANMA
        context.Entry(existing).CurrentValues.SetValues(servisCalisma);
        existing.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var servisCalisma = await context.ServisCalismalari.FindAsync(id);
        if (servisCalisma != null)
        {
            servisCalisma.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<ServisCalisma>> FilterAsync(
        DateTime startDate,
        DateTime endDate,
        int? aracId = null,
        int? soforId = null,
        int? guzergahId = null,
        int? cariId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = CreateReadQuery(context)
            .Where(s => s.CalismaTarihi >= startDate && s.CalismaTarihi <= endDate);

        if (aracId.HasValue)
            query = query.Where(s => s.AracId == aracId.Value);

        if (soforId.HasValue)
            query = query.Where(s => s.SoforId == soforId.Value);

        if (guzergahId.HasValue)
            query = query.Where(s => s.GuzergahId == guzergahId.Value);

        if (cariId.HasValue)
            query = query.Where(s => s.Guzergah.CariId == cariId.Value);

        return await query.OrderByDescending(s => s.CalismaTarihi).ToListAsync();
    }
}





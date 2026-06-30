using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class KurumService : IKurumService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public KurumService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Kurum>> GetAllAsync(bool includeDeleted = false)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var query = context.Kurumlar.Include(k => k.Cari).AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(k => !k.IsDeleted);
        }

        return await query.OrderBy(k => k.KurumAdi).ToListAsync();
    }

    public async Task<List<Kurum>> GetAktifAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Kurumlar
            .Include(k => k.Cari)
            .Where(k => !k.IsDeleted && k.Aktif)
            .OrderBy(k => k.KurumAdi)
            .ToListAsync();
    }

    public async Task<Kurum?> GetByIdAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Kurumlar
            .Include(k => k.Cari)
            .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);
    }

    public async Task<Kurum> CreateAsync(Kurum kurum)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        kurum.CreatedAt = DateTime.UtcNow;
        context.Kurumlar.Add(kurum);
        await context.SaveChangesAsync();
        return kurum;
    }

    public async Task<Kurum> UpdateAsync(Kurum kurum)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var existing = await context.Kurumlar.FindAsync(kurum.Id);

        if (existing == null || existing.IsDeleted)
            throw new Exception("Kurum bulunamadı");

        existing.KurumKodu = kurum.KurumKodu;
        existing.KurumAdi = kurum.KurumAdi;
        existing.UnvanTam = kurum.UnvanTam;
        existing.VergiNo = kurum.VergiNo;
        existing.VergiDairesi = kurum.VergiDairesi;
        existing.Adres = kurum.Adres;
        existing.Il = kurum.Il;
        existing.Ilce = kurum.Ilce;
        existing.Telefon = kurum.Telefon;
        existing.Telefon2 = kurum.Telefon2;
        existing.Email = kurum.Email;
        existing.WebSite = kurum.WebSite;
        existing.YetkiliKisi = kurum.YetkiliKisi;
        existing.YetkiliTelefon = kurum.YetkiliTelefon;
        existing.YetkiliEmail = kurum.YetkiliEmail;
        existing.Notlar = kurum.Notlar;
        existing.Aktif = kurum.Aktif;
        existing.CariId = kurum.CariId;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var kurum = await context.Kurumlar.FindAsync(id);

        if (kurum != null && !kurum.IsDeleted)
        {
            kurum.IsDeleted = true;
            kurum.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
}



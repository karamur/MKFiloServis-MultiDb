using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

// ─── Interface ────────────────────────────────────────────────────────────────

public interface IServisKontratService
{
    // Kontrat CRUD
    Task<List<ServisKontrat>> GetAllAsync(ServisKontratTip? tip = null, ServisKontratDurum? durum = null);
    Task<ServisKontrat?> GetAsync(int id);
    Task<ServisKontrat> CreateAsync(ServisKontrat kontrat);
    Task<ServisKontrat> UpdateAsync(ServisKontrat kontrat);
    Task DeleteAsync(int id);
    Task<string> GenerateKontratKoduAsync();

    // Puantaj CRUD
    Task<List<ServisPuantaj>> GetPuantajlarAsync(int kontratId);
    Task<ServisPuantaj?> GetPuantajAsync(int id);
    Task<ServisPuantaj> CreatePuantajAsync(ServisPuantaj puantaj);
    Task<ServisPuantaj> UpdatePuantajAsync(ServisPuantaj puantaj);
    Task DeletePuantajAsync(int id);
    Task<ServisPuantaj> OnaylaAsync(int puantajId, string onayanKisi);
    Task<ServisPuantaj> KapatAsync(int puantajId);

    // Ödeme CRUD (tedarikçiye)
    Task<List<ServisOdeme>> GetOdemelerAsync(int puantajId);
    Task<ServisOdeme> CreateOdemeAsync(ServisOdeme odeme);
    Task<ServisOdeme> UpdateOdemeAsync(ServisOdeme odeme);
    Task DeleteOdemeAsync(int id);

    // Tahsilat CRUD (kurumdan)
    Task<List<ServisTahsilat>> GetTahsilatlarAsync(int puantajId);
    Task<ServisTahsilat> CreateTahsilatAsync(ServisTahsilat tahsilat);
    Task<ServisTahsilat> UpdateTahsilatAsync(ServisTahsilat tahsilat);
    Task DeleteTahsilatAsync(int id);

    // Özet
    Task<ServisKontratOzet> GetOzetAsync(int kontratId);
}

// ─── DTO ─────────────────────────────────────────────────────────────────────

public class ServisKontratOzet
{
    public int KontratId { get; set; }
    public int ToplamPuantaj { get; set; }
    public decimal ToplamCalismaSayisi { get; set; }
    public decimal ToplamTahsilatTutari { get; set; }
    public decimal GerceklesenTahsilat { get; set; }
    public decimal BekleyenTahsilat { get; set; }
    public decimal? ToplamOdemeTutari { get; set; }
    public decimal? GerceklesenOdeme { get; set; }
    public decimal? BekleyenOdeme { get; set; }
}

// ─── Service ─────────────────────────────────────────────────────────────────

public class ServisKontratService : IServisKontratService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ServisKontratService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // ── Kontrat ───────────────────────────────────────────────────────────────

    public async Task<List<ServisKontrat>> GetAllAsync(ServisKontratTip? tip = null, ServisKontratDurum? durum = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var q = ctx.ServisKontratlar
            .Include(k => k.KurumCari)
            .Include(k => k.Guzergah)
            .Include(k => k.Arac)
            .Include(k => k.Sofor)
            .Include(k => k.TasimaTedarikci)
            .AsQueryable();

        if (tip.HasValue)   q = q.Where(k => k.Tip == tip.Value);
        if (durum.HasValue) q = q.Where(k => k.Durum == durum.Value);

        return await q.OrderByDescending(k => k.BaslangicTarihi).ToListAsync();
    }

    public async Task<ServisKontrat?> GetAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.ServisKontratlar
            .Include(k => k.KurumCari)
            .Include(k => k.Guzergah)
            .Include(k => k.Arac)
            .Include(k => k.Sofor)
            .Include(k => k.TasimaTedarikci)
            .Include(k => k.TasimaTedarikciIs)
            .Include(k => k.Puantajlar)
                .ThenInclude(p => p.Odemeler)
            .Include(k => k.Puantajlar)
                .ThenInclude(p => p.Tahsilatlar)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<ServisKontrat> CreateAsync(ServisKontrat kontrat)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(kontrat.KontratKodu))
            kontrat.KontratKodu = await GenerateKontratKoduAsync();
        kontrat.CreatedAt = DateTime.UtcNow;
        ctx.ServisKontratlar.Add(kontrat);
        await ctx.SaveChangesAsync();
        return kontrat;
    }

    public async Task<ServisKontrat> UpdateAsync(ServisKontrat kontrat)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        kontrat.UpdatedAt = DateTime.UtcNow;
        ctx.ServisKontratlar.Update(kontrat);
        await ctx.SaveChangesAsync();
        return kontrat;
    }

    public async Task DeleteAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entity = await ctx.ServisKontratlar.FindAsync(id);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<string> GenerateKontratKoduAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var son = await ctx.ServisKontratlar
            .OrderByDescending(k => k.Id)
            .Select(k => k.KontratKodu)
            .FirstOrDefaultAsync();

        int sonNo = 0;
        if (son != null && son.StartsWith("KNT-") && int.TryParse(son[4..], out var n))
            sonNo = n;

        return $"KNT-{(sonNo + 1):D5}";
    }

    // ── Puantaj ───────────────────────────────────────────────────────────────

    public async Task<List<ServisPuantaj>> GetPuantajlarAsync(int kontratId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.ServisPuantajlar
            .Include(p => p.Odemeler)
            .Include(p => p.Tahsilatlar)
            .Where(p => p.ServisKontratId == kontratId)
            .OrderByDescending(p => p.Yil).ThenByDescending(p => p.Ay)
            .ToListAsync();
    }

    public async Task<ServisPuantaj?> GetPuantajAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.ServisPuantajlar
            .Include(p => p.ServisKontrat)
                .ThenInclude(k => k!.KurumCari)
            .Include(p => p.ServisKontrat)
                .ThenInclude(k => k!.Guzergah)
            .Include(p => p.ServisKontrat)
                .ThenInclude(k => k!.TasimaTedarikci)
            .Include(p => p.Odemeler)
            .Include(p => p.Tahsilatlar)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<ServisPuantaj> CreatePuantajAsync(ServisPuantaj puantaj)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        puantaj.CreatedAt = DateTime.UtcNow;
        ctx.ServisPuantajlar.Add(puantaj);
        await ctx.SaveChangesAsync();
        return puantaj;
    }

    public async Task<ServisPuantaj> UpdatePuantajAsync(ServisPuantaj puantaj)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        puantaj.UpdatedAt = DateTime.UtcNow;
        ctx.ServisPuantajlar.Update(puantaj);
        await ctx.SaveChangesAsync();
        return puantaj;
    }

    public async Task DeletePuantajAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entity = await ctx.ServisPuantajlar.FindAsync(id);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<ServisPuantaj> OnaylaAsync(int puantajId, string onayanKisi)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var p = await ctx.ServisPuantajlar.FindAsync(puantajId);
        if (p is null) throw new InvalidOperationException("Puantaj bulunamadı.");
        p.Durum = ServisPuantajDurum.Onaylandi;
        p.OnayanKisi = onayanKisi;
        p.OnayTarihi = DateTime.UtcNow;
        p.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
        return p;
    }

    public async Task<ServisPuantaj> KapatAsync(int puantajId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var p = await ctx.ServisPuantajlar.FindAsync(puantajId);
        if (p is null) throw new InvalidOperationException("Puantaj bulunamadı.");
        p.Durum = ServisPuantajDurum.Kapandi;
        p.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
        return p;
    }

    // ── Ödeme ─────────────────────────────────────────────────────────────────

    public async Task<List<ServisOdeme>> GetOdemelerAsync(int puantajId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.ServisOdemeler
            .Where(o => o.ServisPuantajId == puantajId)
            .OrderByDescending(o => o.OdemeTarihi)
            .ToListAsync();
    }

    public async Task<ServisOdeme> CreateOdemeAsync(ServisOdeme odeme)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        odeme.CreatedAt = DateTime.UtcNow;
        ctx.ServisOdemeler.Add(odeme);
        await ctx.SaveChangesAsync();
        return odeme;
    }

    public async Task<ServisOdeme> UpdateOdemeAsync(ServisOdeme odeme)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        odeme.UpdatedAt = DateTime.UtcNow;
        ctx.ServisOdemeler.Update(odeme);
        await ctx.SaveChangesAsync();
        return odeme;
    }

    public async Task DeleteOdemeAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entity = await ctx.ServisOdemeler.FindAsync(id);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    // ── Tahsilat ──────────────────────────────────────────────────────────────

    public async Task<List<ServisTahsilat>> GetTahsilatlarAsync(int puantajId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.ServisTahsilatlar
            .Where(t => t.ServisPuantajId == puantajId)
            .OrderByDescending(t => t.TahsilatTarihi)
            .ToListAsync();
    }

    public async Task<ServisTahsilat> CreateTahsilatAsync(ServisTahsilat tahsilat)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        tahsilat.CreatedAt = DateTime.UtcNow;
        ctx.ServisTahsilatlar.Add(tahsilat);
        await ctx.SaveChangesAsync();
        return tahsilat;
    }

    public async Task<ServisTahsilat> UpdateTahsilatAsync(ServisTahsilat tahsilat)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        tahsilat.UpdatedAt = DateTime.UtcNow;
        ctx.ServisTahsilatlar.Update(tahsilat);
        await ctx.SaveChangesAsync();
        return tahsilat;
    }

    public async Task DeleteTahsilatAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entity = await ctx.ServisTahsilatlar.FindAsync(id);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    // ── Özet ─────────────────────────────────────────────────────────────────

    public async Task<ServisKontratOzet> GetOzetAsync(int kontratId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var puantajlar = await ctx.ServisPuantajlar
            .Include(p => p.Odemeler.Where(o => !o.IsDeleted))
            .Include(p => p.Tahsilatlar.Where(t => !t.IsDeleted))
            .Where(p => p.ServisKontratId == kontratId)
            .ToListAsync();

        var ozet = new ServisKontratOzet { KontratId = kontratId };
        ozet.ToplamPuantaj = puantajlar.Count;
        ozet.ToplamCalismaSayisi = puantajlar.Sum(p => p.CalismaSayisi);
        ozet.ToplamTahsilatTutari = puantajlar.Sum(p => p.TahsilatToplam);
        ozet.GerceklesenTahsilat = puantajlar.SelectMany(p => p.Tahsilatlar).Where(t => t.Tahsil).Sum(t => t.Tutar);
        ozet.BekleyenTahsilat = ozet.ToplamTahsilatTutari - ozet.GerceklesenTahsilat;
        ozet.ToplamOdemeTutari = puantajlar.Sum(p => p.OdemeToplam);
        ozet.GerceklesenOdeme = puantajlar.SelectMany(p => p.Odemeler).Where(o => o.Odendi).Sum(o => o.Tutar);
        ozet.BekleyenOdeme = ozet.ToplamOdemeTutari - ozet.GerceklesenOdeme;

        return ozet;
    }
}




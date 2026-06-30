using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class TekrarlayanOdemeService : ITekrarlayanOdemeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public TekrarlayanOdemeService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<TekrarlayanOdeme>> GetTekrarlayanOdemelerAsync(int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.TekrarlayanOdemeler
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (firmaId.HasValue)
            query = query.Where(t => t.FirmaId == firmaId.Value);

        return await query
            .Include(t => t.Firma)
            .OrderBy(t => t.Aktif ? 0 : 1)
            .ThenBy(t => t.OdemeGunu)
            .ThenBy(t => t.OdemeAdi)
            .ToListAsync();
    }

    public async Task<List<TekrarlayanOdeme>> GetAktifTekrarlayanOdemelerAsync(int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.TekrarlayanOdemeler
            .Where(t => !t.IsDeleted && t.Aktif);

        if (firmaId.HasValue)
            query = query.Where(t => t.FirmaId == firmaId.Value);

        return await query
            .Include(t => t.Firma)
            .OrderBy(t => t.OdemeGunu)
            .ThenBy(t => t.OdemeAdi)
            .ToListAsync();
    }

    public async Task<TekrarlayanOdeme?> GetTekrarlayanOdemeByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TekrarlayanOdemeler
            .Include(t => t.Firma)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }

    public async Task<TekrarlayanOdeme> CreateTekrarlayanOdemeAsync(TekrarlayanOdeme odeme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        odeme.BaslangicTarihi = DateTime.SpecifyKind(odeme.BaslangicTarihi, DateTimeKind.Utc);
        if (odeme.BitisTarihi.HasValue)
            odeme.BitisTarihi = DateTime.SpecifyKind(odeme.BitisTarihi.Value, DateTimeKind.Utc);
        odeme.CreatedAt = DateTime.UtcNow;

        context.TekrarlayanOdemeler.Add(odeme);
        await context.SaveChangesAsync();

        context.Entry(odeme).State = EntityState.Detached;

        return odeme;
    }

    public async Task<TekrarlayanOdeme> UpdateTekrarlayanOdemeAsync(TekrarlayanOdeme odeme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.TekrarlayanOdemeler.FindAsync(odeme.Id);
        if (existing == null)
            throw new Exception("Tekrarlayan odeme bulunamadi");

        existing.OdemeAdi = odeme.OdemeAdi;
        existing.MasrafKalemi = odeme.MasrafKalemi;
        existing.Aciklama = odeme.Aciklama;
        existing.Tutar = odeme.Tutar;
        existing.Periyod = odeme.Periyod;
        existing.OdemeGunu = odeme.OdemeGunu;
        existing.BaslangicTarihi = DateTime.SpecifyKind(odeme.BaslangicTarihi, DateTimeKind.Utc);
        existing.BitisTarihi = odeme.BitisTarihi.HasValue
            ? DateTime.SpecifyKind(odeme.BitisTarihi.Value, DateTimeKind.Utc)
            : null;
        existing.HatirlatmaGunSayisi = odeme.HatirlatmaGunSayisi;
        existing.FirmaId = odeme.FirmaId;
        existing.Aktif = odeme.Aktif;
        existing.Renk = odeme.Renk;
        existing.Icon = odeme.Icon;
        existing.Notlar = odeme.Notlar;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        context.Entry(existing).State = EntityState.Detached;

        return existing;
    }

    public async Task DeleteTekrarlayanOdemeAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.TekrarlayanOdemeler.FindAsync(id);
        if (odeme != null)
        {
            odeme.IsDeleted = true;
            odeme.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            context.Entry(odeme).State = EntityState.Detached;
        }
    }

    public async Task<int> TekrarlayanOdemelerdenKayitOlusturAsync(int yil, int ay, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var aktifPlanlar = await GetAktifTekrarlayanOdemelerAsync(firmaId);
        var olusturulanSayisi = 0;

        foreach (var plan in aktifPlanlar)
        {
            if (!PeriyodaUygunMu(plan, yil, ay))
                continue;

            var gunSayisi = DateTime.DaysInMonth(yil, ay);
            var odemeGunu = Math.Min(plan.OdemeGunu, gunSayisi);

            var mevcutKayit = await context.BudgetOdemeler
                .AnyAsync(o => o.OdemeYil == yil &&
                               o.OdemeAy == ay &&
                               o.MasrafKalemi == plan.MasrafKalemi &&
                               o.Aciklama != null && o.Aciklama.StartsWith("[Tekrarlayan") &&
                               o.Aciklama.Contains($"#{plan.Id}]"));

            if (!mevcutKayit)
            {
                var odemeTarihi = DateTime.SpecifyKind(new DateTime(yil, ay, odemeGunu), DateTimeKind.Utc);

                var yeniOdeme = new BudgetOdeme
                {
                    OdemeTarihi = odemeTarihi,
                    OdemeAy = ay,
                    OdemeYil = yil,
                    MasrafKalemi = plan.MasrafKalemi,
                    Aciklama = $"[Tekrarlayan#{plan.Id}] {plan.OdemeAdi}",
                    Miktar = plan.Tutar,
                    FirmaId = plan.FirmaId,
                    Durum = OdemeDurum.Bekliyor,
                    TaksitliMi = false,
                    ToplamTaksitSayisi = 1,
                    KacinciTaksit = 1,
                    Notlar = plan.Notlar,
                    CreatedAt = DateTime.UtcNow
                };

                context.BudgetOdemeler.Add(yeniOdeme);
                olusturulanSayisi++;
            }
        }

        if (olusturulanSayisi > 0)
            await context.SaveChangesAsync();

        return olusturulanSayisi;
    }

    private static bool PeriyodaUygunMu(TekrarlayanOdeme plan, int yil, int ay)
    {
        var tarih = new DateTime(yil, ay, 1);

        if (tarih < plan.BaslangicTarihi.Date)
            return false;

        if (plan.BitisTarihi.HasValue && tarih > plan.BitisTarihi.Value.Date)
            return false;

        var ayFarki = ((yil - plan.BaslangicTarihi.Year) * 12) + (ay - plan.BaslangicTarihi.Month);

        return plan.Periyod switch
        {
            TekrarPeriyodu.Gunluk => true,
            TekrarPeriyodu.Haftalik => true,
            TekrarPeriyodu.Aylik => true,
            TekrarPeriyodu.IkiAylik => ayFarki % 2 == 0,
            TekrarPeriyodu.UcAylik => ayFarki % 3 == 0,
            TekrarPeriyodu.AltiAylik => ayFarki % 6 == 0,
            TekrarPeriyodu.Yillik => ayFarki % 12 == 0,
            _ => true
        };
    }
}




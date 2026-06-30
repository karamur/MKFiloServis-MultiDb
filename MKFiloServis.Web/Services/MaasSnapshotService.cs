using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Calculation;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class MaasSnapshotService : IMaasSnapshotService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public MaasSnapshotService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> VarMiAsync(int yil, int ay, int firmaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MaasOdemeSnapshotlar
            .AnyAsync(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted);
    }

    public async Task<List<MaasOdemeSnapshot>> GetAsync(int yil, int ay, int firmaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MaasOdemeSnapshotlar
            .AsNoTracking()
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
            .OrderBy(x => x.PersonelAdSoyad)
            .ToListAsync();
    }

    public async Task<List<MaasOdemeSnapshot>> OlusturAsync(
        int yil, int ay, int firmaId,
        List<(int PersonelId, string AdSoyad, string? PersonelKodu, string? GorevAdi, string? AracPlakasi,
              decimal GercekMaas, decimal BankayaYatan, decimal Avans, decimal Kesinti, decimal Harcama, decimal Odenecek, decimal HakedisGelir, decimal HakedisGider)> data)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Zaten varsa tekrar oluşturma
        var varMi = await VarMiAsync(yil, ay, firmaId);
        if (varMi)
            return await GetAsync(yil, ay, firmaId);

        var now = DateTime.UtcNow;
        var snapshots = data.Select(x => new MaasOdemeSnapshot
        {
            FirmaId = firmaId,
            Yil = yil,
            Ay = ay,
            PersonelId = x.PersonelId,
            PersonelAdSoyad = x.AdSoyad,
            PersonelKodu = x.PersonelKodu,
            GorevAdi = x.GorevAdi,
            AracPlakasi = x.AracPlakasi,
            GercekMaas = x.GercekMaas,
            BankayaYatan = x.BankayaYatan,
            Avans = x.Avans,
            Kesinti = x.Kesinti,
            Harcama = x.Harcama,
            Odenecek = x.Odenecek,
            HakedisGelir = x.HakedisGelir,
            HakedisGider = x.HakedisGider,
            HesaplamaTarihi = now,
            Kilitli = true,
            CreatedAt = now
        }).ToList();

        context.MaasOdemeSnapshotlar.AddRange(snapshots);
        await context.SaveChangesAsync();

        return snapshots;
    }

    public async Task<List<MaasOdemeSnapshot>> GuncelleAsync(
        int yil, int ay, int firmaId,
        List<(int PersonelId, string AdSoyad, string? PersonelKodu, string? GorevAdi, string? AracPlakasi,
              decimal GercekMaas, decimal BankayaYatan, decimal Avans, decimal Kesinti, decimal Harcama, decimal Odenecek, decimal HakedisGelir, decimal HakedisGider)> data)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var snapshot = await context.MaasOdemeSnapshotlar
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
            .ToListAsync();

        if (!snapshot.Any())
            return snapshot;

        // Kilitli dönem güncellenemez
        if (snapshot.Any(x => x.Kilitli))
            throw new InvalidOperationException($"Kilitli dönem güncellenemez. Yil={yil} Ay={ay}");

        var dataMap = data.ToDictionary(x => x.PersonelId);

        foreach (var item in snapshot)
        {
            if (!dataMap.TryGetValue(item.PersonelId, out var guncel))
                continue;

            // ── ENGINE = TEK HESAP KAYNAĞI ──
            var hesap = MaasHesaplamaEngine.Hesapla(new MaasInput
            {
                GercekMaas = guncel.GercekMaas,
                BankayaYatan = guncel.BankayaYatan,
                Avans = guncel.Avans,
                Kesinti = guncel.Kesinti,
                Harcama = guncel.Harcama
            });

            item.GercekMaas = hesap.GercekMaas;
            item.BankayaYatan = hesap.BankayaYatan;
            item.Avans = hesap.Avans;
            item.Kesinti = hesap.Kesinti;
            item.Harcama = hesap.Harcama;
            item.Odenecek = hesap.Odenecek;
            item.HakedisGelir = guncel.HakedisGelir;
            item.HakedisGider = guncel.HakedisGider;
            item.HesaplamaTarihi = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            // Güncellendi
        }

        await context.SaveChangesAsync();        // Toplu güncelleme tamamlandı

        return snapshot;
    }

    public async Task KilitleAsync(int yil, int ay, int firmaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var affected = await context.MaasOdemeSnapshotlar
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Kilitli, true)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
        // Kilitlendi
    }

    public async Task SilAsync(int yil, int ay, int firmaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var affected = await context.MaasOdemeSnapshotlar
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.DeletedAt, DateTime.UtcNow));
        // Silindi
    }
}



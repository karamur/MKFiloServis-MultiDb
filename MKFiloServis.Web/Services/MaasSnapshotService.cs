using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Calculation;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class MaasSnapshotService : IMaasSnapshotService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public MaasSnapshotService(IDbContextFactory<ApplicationDbContext> contextFactory, IAktifFirmaProvider aktifFirmaProvider)
    {
        _contextFactory = contextFactory;
        _aktifFirmaProvider = aktifFirmaProvider;
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
        DonemiDogrula(yil, ay, firmaId);
        await using var context = await _contextFactory.CreateDbContextAsync();
        await FirmayiDogrulaAsync(context, firmaId);
        await PersonelleriDogrulaAsync(context, firmaId, data.Select(x => x.PersonelId).ToArray());

        // Zaten varsa tekrar oluşturma
        var mevcut = await context.MaasOdemeSnapshotlar.AsNoTracking()
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
            .OrderBy(x => x.PersonelAdSoyad).ToListAsync();
        if (mevcut.Count > 0)
            return mevcut;

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
        DonemiDogrula(yil, ay, firmaId);
        await using var context = await _contextFactory.CreateDbContextAsync();
        await FirmayiDogrulaAsync(context, firmaId);
        await PersonelleriDogrulaAsync(context, firmaId, data.Select(x => x.PersonelId).ToArray());

        var snapshot = await context.MaasOdemeSnapshotlar
            .AsTracking()
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
            .ToListAsync();

        if (!snapshot.Any())
            return snapshot;

        // Kilitli dönem güncellenemez
        if (snapshot.Any(x => x.Kilitli))
            throw new InvalidOperationException($"Kilitli dönem güncellenemez. Yil={yil} Ay={ay}");

        var dataMap = data.ToDictionary(x => x.PersonelId);
        var snapshotPersonelIds = snapshot.Select(x => x.PersonelId).ToHashSet();
        if (dataMap.Keys.Any(id => !snapshotPersonelIds.Contains(id)))
            throw new InvalidOperationException("Güncellenecek personelin bu dönemde maaş snapshot kaydı bulunmuyor.");

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
        DonemiDogrula(yil, ay, firmaId);
        await using var context = await _contextFactory.CreateDbContextAsync();
        await FirmayiDogrulaAsync(context, firmaId);
        var snapshots = await context.MaasOdemeSnapshotlar.AsTracking()
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted && !x.Kilitli)
            .ToListAsync();
        if (snapshots.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var snapshot in snapshots)
        {
            snapshot.Kilitli = true;
            snapshot.UpdatedAt = now;
        }
        await context.SaveChangesAsync();
    }

    public async Task SilAsync(int yil, int ay, int firmaId)
    {
        DonemiDogrula(yil, ay, firmaId);
        await using var context = await _contextFactory.CreateDbContextAsync();
        await FirmayiDogrulaAsync(context, firmaId);
        var snapshots = await context.MaasOdemeSnapshotlar.AsTracking()
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
            .ToListAsync();
        if (snapshots.Count == 0) return;

        if (snapshots.Any(x => x.Kilitli || x.MuhasebeFisId.HasValue || x.IptalFisId.HasValue))
            throw new InvalidOperationException(
                "Kilitli veya muhasebe fişine bağlanmış maaş dönemi silinemez. Düzeltmeyi yetkili muhasebe ters fiş/mahsup süreciyle yapın.");

        var now = DateTime.UtcNow;
        foreach (var snapshot in snapshots)
        {
            snapshot.IsDeleted = true;
            snapshot.DeletedAt = now;
            snapshot.UpdatedAt = now;
        }
        await context.SaveChangesAsync();
    }

    private void DonemiDogrula(int yil, int ay, int firmaId)
    {
        if (firmaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(firmaId), "Maaş snapshot işlemi için geçerli bir firma gereklidir.");
        if (yil is < 1 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(yil), "Yıl 1 ile 9999 arasında olmalıdır.");
        if (ay is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(ay), "Ay 1 ile 12 arasında olmalıdır.");
        if (_aktifFirmaProvider.TumFirmalar || _aktifFirmaProvider.AktifFirmaId is not > 0 ||
            _aktifFirmaProvider.AktifFirmaId != firmaId)
            throw new InvalidOperationException("Maaş snapshot yazımı için hedef firmayı tek aktif firma olarak seçin.");
    }

    private static async Task FirmayiDogrulaAsync(ApplicationDbContext context, int firmaId)
    {
        if (!await context.Firmalar.AnyAsync(x => x.Id == firmaId && !x.IsDeleted))
            throw new InvalidOperationException("Maaş snapshot firmasına erişilemiyor veya firma silinmiş.");
    }

    private static async Task PersonelleriDogrulaAsync(ApplicationDbContext context, int firmaId, int[] personelIds)
    {
        if (personelIds.Any(id => id <= 0) || personelIds.Distinct().Count() != personelIds.Length)
            throw new InvalidOperationException("Snapshot personel kimlikleri pozitif ve benzersiz olmalıdır.");
        // Uzun personel listelerinde tek sorgunun parametre sınırına yaklaşma.
        foreach (var ids in personelIds.Chunk(500))
        {
            var count = await context.Soforler.CountAsync(x => ids.Contains(x.Id) && x.FirmaId == firmaId && !x.IsDeleted);
            if (count != ids.Length)
                throw new InvalidOperationException("Snapshot personelleri seçili firmaya ait, erişilebilir ve silinmemiş olmalıdır.");
        }
    }
}



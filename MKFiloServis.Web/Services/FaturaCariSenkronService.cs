using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public sealed class FaturaCariSenkronService : IFaturaCariSenkronService
{
    private const int EnFazlaUyari = 20;

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMuhasebeService _muhasebeService;
    private readonly CurrentPermissionGuard _permissionGuard;

    public FaturaCariSenkronService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMuhasebeService muhasebeService,
        CurrentPermissionGuard permissionGuard)
    {
        _contextFactory = contextFactory;
        _muhasebeService = muhasebeService;
        _permissionGuard = permissionGuard;
    }

    public async Task<FaturaCariSenkronSonucu> GuncelleAsync(FaturaCariSenkronIstegi istek, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var baslangic = istek.Baslangic.Date;
        var bitis = istek.Bitis.Date;
        if (bitis < baslangic)
            throw new InvalidOperationException("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        if (istek.Uygula)
        {
            var yonler = istek.Yon.HasValue ? new[] { istek.Yon.Value } : new[] { FaturaYonu.Giden, FaturaYonu.Gelen };
            foreach (var yon in yonler)
                await RequireDuzenleAsync(yon);
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var bitisSon = bitis.AddDays(1);

        var sorgu = context.Faturalar
            .AsNoTracking()
            .Where(f => f.FaturaTarihi >= baslangic && f.FaturaTarihi < bitisSon && f.Durum != FaturaDurum.IptalEdildi);
        if (istek.Yon.HasValue)
        {
            var secilenYon = istek.Yon.Value;
            sorgu = sorgu.Where(f => f.FaturaYonu == secilenYon);
        }

        var faturalar = await sorgu.OrderBy(f => f.FaturaTarihi).ThenBy(f => f.Id).ToListAsync(cancellationToken);
        var cariIds = faturalar.Select(f => f.CariId).Distinct().ToList();

        var sonuc = new FaturaCariSenkronSonucu
        {
            Uygulandi = istek.Uygula,
            TaranFatura = faturalar.Count,
            TaranCari = cariIds.Count,
            GidenToplam = faturalar.Where(f => f.FaturaYonu == FaturaYonu.Giden).Sum(f => f.GenelToplam),
            GelenToplam = faturalar.Where(f => f.FaturaYonu == FaturaYonu.Gelen).Sum(f => f.GenelToplam)
        };

        if (istek.CariTipiGuncelle && cariIds.Count > 0)
        {
            var degisimler = await CariTipiDegisimleriniUygulaAsync(context, cariIds, istek.Uygula, cancellationToken);
            foreach (var degisim in degisimler)
            {
                sonuc.Satirlar.Add(new FaturaCariSenkronSatiri(
                    FaturaCariSenkronIslemi.CariTipiGuncellendi,
                    degisim.CariId,
                    degisim.Unvan,
                    $"{FaturaCariKurallari.TipAdi(degisim.Eski)} → {FaturaCariKurallari.TipAdi(degisim.Yeni)}"));
            }
            sonuc.CariTipiGuncellenen = degisimler.Count;
        }

        var eksikFisliFaturalar = faturalar.Where(f => !f.MuhasebeFisiOlusturuldu).ToList();
        sonuc.EksikFisSayisi = eksikFisliFaturalar.Count;

        if (istek.EksikMuhasebeFisiOlustur && istek.Uygula && eksikFisliFaturalar.Count > 0)
            await EksikFisleriOlusturAsync(context, eksikFisliFaturalar, sonuc, cancellationToken);

        return sonuc;
    }

    public async Task<int> CariTipleriniGuncelleAsync(IReadOnlyCollection<int> cariIds, List<string>? guncellenenler = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cariIds);
        if (cariIds.Count == 0)
            return 0;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var degisimler = await CariTipiDegisimleriniUygulaAsync(context, cariIds, uygula: true, cancellationToken);
        guncellenenler?.AddRange(degisimler.Select(d =>
            $"{d.Unvan}: {FaturaCariKurallari.TipAdi(d.Eski)} → {FaturaCariKurallari.TipAdi(d.Yeni)}"));
        return degisimler.Count;
    }

    private sealed record TipDegisimi(int CariId, string Unvan, CariTipi Eski, CariTipi Yeni);

    private static async Task<List<TipDegisimi>> CariTipiDegisimleriniUygulaAsync(
        ApplicationDbContext context, IReadOnlyCollection<int> cariIds, bool uygula, CancellationToken cancellationToken)
    {
        // Yalnızca görülebilen (silinmemiş, aktif firmaya ait) cariler; faturalarının yönü tüm zamanlar üzerinden okunur.
        var cariler = await context.Cariler.AsTracking().Where(c => cariIds.Contains(c.Id)).ToListAsync(cancellationToken);
        if (cariler.Count == 0)
            return [];

        var idler = cariler.Select(c => c.Id).ToList();
        var yonKayitlari = await context.Faturalar
            .AsNoTracking()
            .Where(f => idler.Contains(f.CariId) && f.Durum != FaturaDurum.IptalEdildi)
            .GroupBy(f => new { f.CariId, f.FaturaYonu })
            .Select(g => new { g.Key.CariId, g.Key.FaturaYonu })
            .ToListAsync(cancellationToken);

        var gidenOlanlar = yonKayitlari.Where(y => y.FaturaYonu == FaturaYonu.Giden).Select(y => y.CariId).ToHashSet();
        var gelenOlanlar = yonKayitlari.Where(y => y.FaturaYonu == FaturaYonu.Gelen).Select(y => y.CariId).ToHashSet();

        var degisimler = new List<TipDegisimi>();
        foreach (var cari in cariler.OrderBy(c => c.Unvan))
        {
            var yeni = FaturaCariKurallari.GerekenTip(cari.CariTipi, gidenOlanlar.Contains(cari.Id), gelenOlanlar.Contains(cari.Id));
            if (yeni == cari.CariTipi)
                continue;

            degisimler.Add(new TipDegisimi(cari.Id, cari.Unvan, cari.CariTipi, yeni));
            if (uygula)
            {
                cari.CariTipi = yeni;
                cari.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (uygula && degisimler.Count > 0)
            await context.SaveChangesAsync(cancellationToken);

        return degisimler;
    }

    private async Task EksikFisleriOlusturAsync(
        ApplicationDbContext context, List<Fatura> faturalar, FaturaCariSenkronSonucu sonuc, CancellationToken cancellationToken)
    {
        var ayar = await context.MuhasebeAyarlari.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (ayar == null || !ayar.FaturaOtomatikMuhasebeFisi)
        {
            sonuc.Uyarilar.Add("Muhasebe ayarlarında faturadan otomatik fiş kapalı olduğu için eksik fişler oluşturulmadı.");
            return;
        }

        // Fişi var ama işareti eksik kalmış faturalarda ikinci fiş açılmaz; yalnızca işaret düzeltilir.
        var faturaIdleri = faturalar.Select(f => f.Id).ToList();
        var mevcutFisler = await context.MuhasebeFisleri
            .AsNoTracking()
            .Where(f => f.Kaynak == FisKaynak.Fatura && f.KaynakId != null && faturaIdleri.Contains(f.KaynakId.Value))
            .Select(f => new { FaturaId = f.KaynakId!.Value, FisId = f.Id })
            .ToListAsync(cancellationToken);
        var fisHaritasi = mevcutFisler.GroupBy(f => f.FaturaId).ToDictionary(g => g.Key, g => g.Min(x => x.FisId));

        foreach (var fatura in faturalar)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (fatura.FirmaId == null)
            {
                sonuc.Satirlar.Add(new FaturaCariSenkronSatiri(
                    FaturaCariSenkronIslemi.MuhasebeFisiOlusturulamadi, fatura.CariId, fatura.FaturaNo, "Faturanın firması yok."));
                continue;
            }

            try
            {
                if (fisHaritasi.TryGetValue(fatura.Id, out var mevcutFisId))
                {
                    var izlenen = await context.Faturalar.AsTracking().FirstAsync(f => f.Id == fatura.Id, cancellationToken);
                    izlenen.MuhasebeFisiOlusturuldu = true;
                    izlenen.MuhasebeFisId ??= mevcutFisId;
                    await context.SaveChangesAsync(cancellationToken);
                    sonuc.Satirlar.Add(new FaturaCariSenkronSatiri(
                        FaturaCariSenkronIslemi.MuhasebeFisiOlusturuldu, fatura.CariId, fatura.FaturaNo, "Mevcut fiş faturaya bağlandı."));
                }
                else
                {
                    await _muhasebeService.CreateFaturaFisiAsync(fatura);
                    sonuc.Satirlar.Add(new FaturaCariSenkronSatiri(
                        FaturaCariSenkronIslemi.MuhasebeFisiOlusturuldu, fatura.CariId, fatura.FaturaNo, "Muhasebe fişi oluşturuldu."));
                }

                sonuc.OlusturulanFis++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var mesaj = ex.InnerException?.Message ?? ex.Message;
                sonuc.Satirlar.Add(new FaturaCariSenkronSatiri(
                    FaturaCariSenkronIslemi.MuhasebeFisiOlusturulamadi, fatura.CariId, fatura.FaturaNo, mesaj));
                if (sonuc.Uyarilar.Count < EnFazlaUyari)
                    sonuc.Uyarilar.Add($"{fatura.FaturaNo}: fiş oluşturulamadı ({mesaj})");

                foreach (var entry in context.ChangeTracker.Entries().ToList())
                    entry.State = EntityState.Detached;
            }
        }
    }

    private Task RequireDuzenleAsync(FaturaYonu yon)
    {
        var yonYetkisi = yon == FaturaYonu.Giden ? Yetkiler.KesilenFaturalarDuzenle : Yetkiler.GelenFaturalarDuzenle;
        return _permissionGuard.RequireAnyAsync(Yetkiler.FaturalarDuzenle, yonYetkisi);
    }
}

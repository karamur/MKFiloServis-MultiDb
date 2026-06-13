using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Tam döngü: save → reload → verify. Sefer sayısı artışı ve alan persistansı kontrolü.
/// </summary>
public class GuzergahFullCycleTests
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public GuzergahFullCycleTests(ITestOutputHelper o) => _output = o;

    private sealed class TestFirma : IAktifFirmaProvider
    {
        public TestFirma(int id) => Mevcut = new() { FirmaId = id };
        public int? AktifFirmaId => Mevcut.FirmaId > 0 ? Mevcut.FirmaId : null;
        public bool HasAktifFirma => AktifFirmaId.HasValue;
        public bool TumFirmalar => false;
        public AktifFirmaBilgisi Mevcut { get; private set; }
        public event Action? AktifFirmaDegisti;
        public void Set(AktifFirmaBilgisi f) { Mevcut = f; }
        public void SetTumFirmalar(bool t) { }
        public void SetDonem(int y, int m) { }
        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }

    private (IDbContextFactory<ApplicationDbContext>, GuzergahService, GuzergahSeferService) Setup()
    {
        var s = new ServiceCollection();
        s.AddSingleton<IAktifFirmaProvider>(new TestFirma(1));
        var sp = s.BuildServiceProvider();
        var f = new ScopedDbContextFactory(DbOpts, sp);
        var n = new NumaraSerisiService(f);
        var gs = new GuzergahService(f, Mock.Of<ICacheService>(), n, new TestFirma(1));
        var ss = new GuzergahSeferService(f, new TestFirma(1), Mock.Of<ILogger<GuzergahSeferService>>());
        return (f, gs, ss);
    }

    [Fact]
    public async Task FullCycle_SaveTwice_SeferCount_StaysSame()
    {
        var (factory, gSvc, sSvc) = Setup();
        await using var ctx = factory.CreateDbContext();
        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        cari.Should().NotBeNull();
        var arac = await ctx.Araclar.IgnoreQueryFilters().FirstOrDefaultAsync(a => !a.IsDeleted && a.FirmaId == 1);
        var sofor = await ctx.Soforler.IgnoreQueryFilters().FirstOrDefaultAsync(s => !s.IsDeleted && s.FirmaId == 1);

        var ts = DateTime.UtcNow.ToString("HHmmssfff");

        // 1. YENİ GÜZERGAH OLUŞTUR (2 varsayılan sefer ile)
        var g = new Guzergah
        {
            GuzergahKodu = "CYC-" + ts,
            GuzergahAdi = $"CYCLE TEST {ts}",
            BirimFiyat = 500,
            CariId = cari!.Id,
            Aktif = true,
            FirmaId = 1,
            SeferTipi = SeferTipi.SabahAksam
        };
        var cr = await gSvc.CreateAsync(g);
        cr.Id.Should().BeGreaterThan(0);

        // 2 sefer oluştur
        var seferler1 = new List<GuzergahSefer>
        {
            new() { GuzergahId = cr.Id, FirmaId = 1, Sira = 1, SeferTipi = SeferTipi.Sabah, Slot = SeferSlot.Sabah, KapasiteAdi = "16+1", AracId = arac?.Id, SoforAd = sofor?.TamAd },
            new() { GuzergahId = cr.Id, FirmaId = 1, Sira = 2, SeferTipi = SeferTipi.Aksam, Slot = SeferSlot.Aksam, KapasiteAdi = "16+1", AracId = arac?.Id, SoforAd = sofor?.TamAd }
        };
        await sSvc.ReplaceAllAsync(cr.Id, seferler1);

        // VERIFY: 2 aktif sefer
        var count1 = await CountActiveSeferler(factory, cr.Id);
        _output.WriteLine($"1. kayıt sonrası aktif sefer: {count1}");
        count1.Should().Be(2);

        // 2. GÜNCELLE (sefer sayısı değişmesin)
        var model = await ReadGuzergah(factory, cr.Id);
        model!.GuzergahAdi = "CYCLE UPDATED " + ts;
        model.SeferTipi = SeferTipi.Aksam;
        model.KapasiteAdi = "27+1";
        model.PersonelSayisi = 27;
        if (arac != null) model.VarsayilanAracId = arac.Id;
        if (sofor != null) model.VarsayilanSoforId = sofor.Id;
        await gSvc.UpdateAsync(model);

        // Yine 2 sefer kaydet (güncelleme sefer sayısını değiştirmemeli)
        var seferler2 = new List<GuzergahSefer>
        {
            new() { GuzergahId = cr.Id, FirmaId = 1, Sira = 1, SeferTipi = SeferTipi.Aksam, Slot = SeferSlot.Aksam, KapasiteAdi = "27+1", AracId = arac?.Id, SoforAd = sofor?.TamAd },
            new() { GuzergahId = cr.Id, FirmaId = 1, Sira = 2, SeferTipi = SeferTipi.Aksam, Slot = SeferSlot.Aksam, KapasiteAdi = "27+1", AracId = arac?.Id, SoforAd = sofor?.TamAd }
        };
        await sSvc.ReplaceAllAsync(cr.Id, seferler2);

        // VERIFY: HALA 2 aktif sefer
        var count2 = await CountActiveSeferler(factory, cr.Id);
        _output.WriteLine($"2. kayıt sonrası aktif sefer: {count2}");
        count2.Should().Be(2, "Aynı sayıda sefer tekrar kaydedilince artmamalı");

        // VERIFY: Ana form alanları persist edildi
        var reloaded = await ReadGuzergah(factory, cr.Id);
        reloaded!.SeferTipi.Should().Be(SeferTipi.Aksam);
        reloaded.KapasiteAdi.Should().Be("27+1");
        reloaded.PersonelSayisi.Should().Be(27);
        if (arac != null) reloaded.VarsayilanAracId.Should().Be(arac.Id);
        if (sofor != null) reloaded.VarsayilanSoforId.Should().Be(sofor.Id);

        _output.WriteLine($"Reload sonrası: SeferTipi={reloaded.SeferTipi} Kapasite={reloaded.KapasiteAdi} PersonelSayisi={reloaded.PersonelSayisi} AracId={reloaded.VarsayilanAracId} SoforId={reloaded.VarsayilanSoforId}");

        // 3. ÜÇÜNCÜ KEZ kaydet (yine aynı sayı)
        await sSvc.ReplaceAllAsync(cr.Id, seferler2);
        var count3 = await CountActiveSeferler(factory, cr.Id);
        _output.WriteLine($"3. kayıt sonrası aktif sefer: {count3}");
        count3.Should().Be(2, "3. kez kaydedince de artmamalı");

        // Cleanup
        await Cleanup(factory, cr.Id);
    }

    [Fact]
    public async Task FullCycle_SeferCount_8to16_AndBack()
    {
        var (factory, gSvc, sSvc) = Setup();
        await using var ctx = factory.CreateDbContext();
        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        cari.Should().NotBeNull();

        var ts = DateTime.UtcNow.ToString("HHmmssfff");
        var g = new Guzergah
        {
            GuzergahKodu = "CNT-" + ts, GuzergahAdi = $"COUNT TEST {ts}",
            BirimFiyat = 500, CariId = cari!.Id, Aktif = true, FirmaId = 1
        };
        var cr = await gSvc.CreateAsync(g);

        try
        {
            // 8 sefer
            var s8 = Enumerable.Range(1, 8).Select(i => new GuzergahSefer
            {
                GuzergahId = cr.Id, FirmaId = 1, Sira = i,
                SeferTipi = SeferTipi.SabahAksam, Slot = i % 2 == 1 ? SeferSlot.Sabah : SeferSlot.Aksam
            }).ToList();
            await sSvc.ReplaceAllAsync(cr.Id, s8);
            var c8 = await CountActiveSeferler(factory, cr.Id);
            c8.Should().Be(8);
            _output.WriteLine($"8 sefer: DB={c8}");

            // 16 sefer
            var s16 = Enumerable.Range(1, 16).Select(i => new GuzergahSefer
            {
                GuzergahId = cr.Id, FirmaId = 1, Sira = i,
                SeferTipi = SeferTipi.SabahAksam, Slot = i % 2 == 1 ? SeferSlot.Sabah : SeferSlot.Aksam
            }).ToList();
            await sSvc.ReplaceAllAsync(cr.Id, s16);
            var c16 = await CountActiveSeferler(factory, cr.Id);
            c16.Should().Be(16);
            _output.WriteLine($"16 sefer: DB={c16}");

            // Geri 8 sefer
            await sSvc.ReplaceAllAsync(cr.Id, s8);
            var c8again = await CountActiveSeferler(factory, cr.Id);
            c8again.Should().Be(8, "8'e düşürünce DB'de 8 kalmalı, 16+8=24 olmamalı");
            _output.WriteLine($"Tekrar 8 sefer: DB={c8again}");

            // Tekrar 16
            await sSvc.ReplaceAllAsync(cr.Id, s16);
            var c16again = await CountActiveSeferler(factory, cr.Id);
            c16again.Should().Be(16, "Tekrar 16 yapınca DB'de 16 kalmalı");
            _output.WriteLine($"Tekrar 16 sefer: DB={c16again}");
        }
        finally { await Cleanup(factory, cr.Id); }
    }

    private async Task<int> CountActiveSeferler(IDbContextFactory<ApplicationDbContext> f, int guzId)
    {
        await using var c = f.CreateDbContext();
        return await c.GuzergahSeferleri
            .IgnoreQueryFilters()
            .CountAsync(s => s.GuzergahId == guzId && !s.IsDeleted);
    }

    private async Task<Guzergah?> ReadGuzergah(IDbContextFactory<ApplicationDbContext> f, int id)
    {
        await using var c = f.CreateDbContext();
        return await c.Set<Guzergah>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    private async Task Cleanup(IDbContextFactory<ApplicationDbContext> f, int id)
    {
        try
        {
            await using var c = f.CreateDbContext();
            await c.Database.ExecuteSqlAsync($"UPDATE \"Guzergahlar\" SET \"IsDeleted\"=true,\"DeletedAt\"=NOW() WHERE \"Id\"={id}");
            await c.Database.ExecuteSqlAsync($"UPDATE \"GuzergahSeferleri\" SET \"IsDeleted\"=true,\"DeletedAt\"=NOW() WHERE \"GuzergahId\"={id}");
        }
        catch { }
    }
}

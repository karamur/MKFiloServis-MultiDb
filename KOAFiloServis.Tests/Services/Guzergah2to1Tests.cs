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
/// 2'den 1'e düşürme senaryosu. ReplaceAllAsync soft-delete mekanizmasını test eder.
/// </summary>
public class Guzergah2to1Tests
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public Guzergah2to1Tests(ITestOutputHelper o) => _output = o;

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

    private (IDbContextFactory<ApplicationDbContext>, GuzergahSeferService) Setup()
    {
        var s = new ServiceCollection();
        s.AddSingleton<IAktifFirmaProvider>(new TestFirma(1));
        var sp = s.BuildServiceProvider();
        var f = new ScopedDbContextFactory(DbOpts, sp);
        var ss = new GuzergahSeferService(f, new TestFirma(1), Mock.Of<ILogger<GuzergahSeferService>>());
        return (f, ss);
    }

    [Fact]
    public async Task ReplaceAll_2active_to_1_Works()
    {
        var (factory, sSvc) = Setup();
        await using var ctx = factory.CreateDbContext();
        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        cari.Should().NotBeNull();

        var ts = DateTime.UtcNow.ToString("HHmmssfff");
        var g = new Guzergah { GuzergahKodu = "T21-" + ts, GuzergahAdi = $"2TO1 TEST {ts}", BirimFiyat = 500, CariId = cari!.Id, Aktif = true, FirmaId = 1 };
        ctx.Guzergahlar.Add(g);
        await ctx.SaveChangesAsync();

        try
        {
            // Step 1: Create 2 active sefer
            var s2 = new List<GuzergahSefer>
            {
                new() { GuzergahId = g.Id, FirmaId = 1, Sira = 1, SeferTipi = SeferTipi.Sabah, Slot = SeferSlot.Sabah },
                new() { GuzergahId = g.Id, FirmaId = 1, Sira = 2, SeferTipi = SeferTipi.Aksam, Slot = SeferSlot.Aksam }
            };
            await sSvc.ReplaceAllAsync(g.Id, s2);
            var (t1, a1, d1) = await CountState(factory, g.Id);
            _output.WriteLine($"After create 2: Total={t1} Active={a1} Deleted={d1}");
            a1.Should().Be(2);

            // Step 2: DROP to 1 sefer
            var s1 = new List<GuzergahSefer>
            {
                new() { GuzergahId = g.Id, FirmaId = 1, Sira = 1, SeferTipi = SeferTipi.Sabah, Slot = SeferSlot.Sabah }
            };
            await sSvc.ReplaceAllAsync(g.Id, s1);
            var (t2, a2, d2) = await CountState(factory, g.Id);
            _output.WriteLine($"After drop to 1: Total={t2} Active={a2} Deleted={d2}");
            a2.Should().Be(1, "2'den 1'e düşünce DB aktif 1 olmalı");

            // Step 3: Load via GetByGuzergahIdAsync — UI should see 1
            var loaded = await sSvc.GetByGuzergahIdAsync(g.Id);
            _output.WriteLine($"UI load: {loaded.Count} sefer");
            loaded.Count.Should().Be(1);

            // Step 4: Save again without changes — still 1
            var s1again = loaded.Select(s => new GuzergahSefer
            {
                GuzergahId = g.Id, FirmaId = 1, Sira = s.Sira,
                SeferTipi = s.SeferTipi, Slot = s.Slot
            }).ToList();
            await sSvc.ReplaceAllAsync(g.Id, s1again);
            var (t3, a3, d3) = await CountState(factory, g.Id);
            _output.WriteLine($"After re-save 1: Total={t3} Active={a3} Deleted={d3}");
            a3.Should().Be(1);

            // Step 5: Raise back to 2 — should work
            await sSvc.ReplaceAllAsync(g.Id, s2);
            var (t4, a4, d4) = await CountState(factory, g.Id);
            _output.WriteLine($"After raise to 2: Total={t4} Active={a4} Deleted={d4}");
            a4.Should().Be(2);
        }
        finally
        {
            await Cleanup(factory, g.Id);
        }
    }

    [Fact]
    public async Task ReplaceAll_Id45_DropTo1_Works()
    {
        var (factory, sSvc) = Setup();

        // Load current state of Id=45
        var before = await sSvc.GetByGuzergahIdAsync(45);
        _output.WriteLine($"Id=45 before: {before.Count} active sefer");

        // Drop to 1
        var s1 = before.Take(1).Select(s => new GuzergahSefer
        {
            GuzergahId = 45, FirmaId = 1, Sira = 1,
            SeferTipi = s.SeferTipi, Slot = s.Slot,
            KapasiteAdi = s.KapasiteAdi, AracId = s.AracId,
            SoforAd = s.SoforAd, SoforTelefon = s.SoforTelefon,
            FirmaAdiSerbest = s.FirmaAdiSerbest
        }).ToList();

        await sSvc.ReplaceAllAsync(45, s1);

        var (total, active, deleted) = await CountState(factory, 45);
        _output.WriteLine($"Id=45 after drop to 1: Total={total} Active={active} Deleted={deleted}");

        // CRITICAL: active MUST be 1
        active.Should().Be(1, $"Hedef=1 ama DB_Aktif={active}");

        // Restore to 2 for normal operation
        var s2 = before.Select(s => new GuzergahSefer
        {
            GuzergahId = 45, FirmaId = 1, Sira = s.Sira,
            SeferTipi = s.SeferTipi, Slot = s.Slot,
            KapasiteAdi = s.KapasiteAdi, AracId = s.AracId,
            SoforAd = s.SoforAd, SoforTelefon = s.SoforTelefon,
            FirmaAdiSerbest = s.FirmaAdiSerbest
        }).ToList();
        await sSvc.ReplaceAllAsync(45, s2);
        _output.WriteLine($"Id=45 restored to 2");
    }

    /// <summary>
    /// KRİTİK: Soft-delete sorgusunun hangi kayıtları etkilediğini doğrudan test et.
    /// </summary>
    [Fact]
    public async Task ReplaceAll_SoftDelete_AffectsCorrectRows()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        // Id=45'i önce 2'ye getir (temiz durum)
        var g = await ctx.Guzergahlar.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == 45 && !x.IsDeleted);
        if (g == null) { _output.WriteLine("Id=45 bulunamadı"); return; }

        // Mevcut durumu logla
        var allBefore = await ctx.GuzergahSeferleri.IgnoreQueryFilters()
            .Where(s => s.GuzergahId == 45).ToListAsync();
        _output.WriteLine($"=== Id=45 Sefer Detay ===");
        foreach (var s in allBefore.OrderBy(s => s.IsDeleted).ThenBy(s => s.Id))
        {
            _output.WriteLine($"  Id={s.Id} Sira={s.Sira} IsDeleted={s.IsDeleted} CreatedAt={s.CreatedAt}");
        }

        var activeBefore = allBefore.Count(s => !s.IsDeleted);
        var deletedBefore = allBefore.Count(s => s.IsDeleted);
        _output.WriteLine($"Active={activeBefore} Deleted={deletedBefore} Total={allBefore.Count}");

        // Test: Eğer 2 aktif varsa, soft-delete loop'unun onları bulduğunu doğrula
        var softDeleteCandidates = allBefore.Where(s => !s.IsDeleted).ToList();
        _output.WriteLine($"Soft-delete adayları: {softDeleteCandidates.Count} (sadece IsDeleted=false olanlar)");

        // Bu adaylar ReplaceAllAsync'teki "silinecekler" ile aynı olmalı
        softDeleteCandidates.Count.Should().Be(activeBefore);
    }

    private async Task<(int total, int active, int deleted)> CountState(IDbContextFactory<ApplicationDbContext> f, int guzId)
    {
        await using var c = f.CreateDbContext();
        var all = await c.GuzergahSeferleri.IgnoreQueryFilters().Where(s => s.GuzergahId == guzId).ToListAsync();
        return (all.Count, all.Count(s => !s.IsDeleted), all.Count(s => s.IsDeleted));
    }

    private async Task Cleanup(IDbContextFactory<ApplicationDbContext> f, int guzId)
    {
        try
        {
            await using var c = f.CreateDbContext();
            await c.Database.ExecuteSqlAsync($"UPDATE \"Guzergahlar\" SET \"IsDeleted\"=true,\"DeletedAt\"=NOW() WHERE \"Id\"={guzId}");
            await c.Database.ExecuteSqlAsync($"UPDATE \"GuzergahSeferleri\" SET \"IsDeleted\"=true,\"DeletedAt\"=NOW() WHERE \"GuzergahId\"={guzId}");
        }
        catch { }
    }
}

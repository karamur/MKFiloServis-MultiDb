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
/// Gerçek UI akışını birebir simüle eder:
/// DB'de soft-deleted kayıtlar varken load/save/reload döngüsü.
/// </summary>
public class GuzergahSeferIsolationTests
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public GuzergahSeferIsolationTests(ITestOutputHelper o) => _output = o;

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

    private (IDbContextFactory<ApplicationDbContext>, GuzergahSeferService) SetupSeferService()
    {
        var s = new ServiceCollection();
        s.AddSingleton<IAktifFirmaProvider>(new TestFirma(1));
        var sp = s.BuildServiceProvider();
        var f = new ScopedDbContextFactory(DbOpts, sp);
        var ss = new GuzergahSeferService(f, new TestFirma(1), Mock.Of<ILogger<GuzergahSeferService>>());
        return (f, ss);
    }

    /// <summary>
    /// KRİTİK TEST: DB'de soft-deleted kayıtlar varken load sadece aktifleri getirmeli.
    /// </summary>
    [Fact]
    public async Task Load_WithSoftDeletedRows_ReturnsOnlyActive()
    {
        var (factory, sSvc) = SetupSeferService();
        await using var ctx = factory.CreateDbContext();
        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        cari.Should().NotBeNull();

        var ts = DateTime.UtcNow.ToString("HHmmssfff");

        // Create a guzergah
        var g = new Guzergah { GuzergahKodu = "ISO-" + ts, GuzergahAdi = $"ISO TEST {ts}", BirimFiyat = 500, CariId = cari!.Id, Aktif = true, FirmaId = 1 };
        ctx.Guzergahlar.Add(g);
        await ctx.SaveChangesAsync();

        try
        {
            // Round 1: 2 sefer
            var s2 = Enumerable.Range(1, 2).Select(i => new GuzergahSefer { GuzergahId = g.Id, FirmaId = 1, Sira = i, SeferTipi = SeferTipi.SabahAksam, Slot = i % 2 == 1 ? SeferSlot.Sabah : SeferSlot.Aksam }).ToList();
            await sSvc.ReplaceAllAsync(g.Id, s2);

            // Round 2: 3 sefer (simulate user increasing count)
            var s3 = Enumerable.Range(1, 3).Select(i => new GuzergahSefer { GuzergahId = g.Id, FirmaId = 1, Sira = i, SeferTipi = SeferTipi.SabahAksam, Slot = i % 2 == 1 ? SeferSlot.Sabah : SeferSlot.Aksam }).ToList();
            await sSvc.ReplaceAllAsync(g.Id, s3);

            // DB state
            var (total, active, deleted) = await CountSeferState(factory, g.Id);
            _output.WriteLine($"ROUND 1-2: Total={total} Active={active} Deleted={deleted}");
            active.Should().Be(3, "Son replace 3 sefer bırakmalı");
            total.Should().Be(5, "2 (soft-deleted) + 3 (active) = 5");

            // SIMULATE UI LOAD: GetByGuzergahIdAsync
            var loaded = await sSvc.GetByGuzergahIdAsync(g.Id);
            _output.WriteLine($"UI LOAD: {loaded.Count} sefer (beklenen: 3)");
            loaded.Count.Should().Be(3, "Load sadece aktif 3 sefer getirmeli, soft-deleted 2'yi DEĞİL");

            // Round 3: Kullanıcı sefer sayısını değiştirmeden kaydediyor
            var s3again = loaded.Select(s => new GuzergahSefer { GuzergahId = g.Id, FirmaId = 1, Sira = s.Sira, SeferTipi = s.SeferTipi, Slot = s.Slot, KapasiteAdi = s.KapasiteAdi, AracId = s.AracId, SoforAd = s.SoforAd, SoforTelefon = s.SoforTelefon, FirmaAdiSerbest = s.FirmaAdiSerbest }).ToList();
            await sSvc.ReplaceAllAsync(g.Id, s3again);

            var (t3, a3, d3) = await CountSeferState(factory, g.Id);
            _output.WriteLine($"ROUND 3 (değişiklik yok): Total={t3} Active={a3} Deleted={d3}");
            a3.Should().Be(3, "Değişiklik yokken 3 kalmalı");
            t3.Should().Be(8, "2+3+3 = 8 toplam (her replace'te soft-delete olanlar)");

            // Round 4: Aynı şekilde tekrar kaydet
            var loaded2 = await sSvc.GetByGuzergahIdAsync(g.Id);
            loaded2.Count.Should().Be(3);
            var s3again2 = loaded2.Select(s => new GuzergahSefer { GuzergahId = g.Id, FirmaId = 1, Sira = s.Sira, SeferTipi = s.SeferTipi, Slot = s.Slot, KapasiteAdi = s.KapasiteAdi, AracId = s.AracId, SoforAd = s.SoforAd, SoforTelefon = s.SoforTelefon, FirmaAdiSerbest = s.FirmaAdiSerbest }).ToList();
            await sSvc.ReplaceAllAsync(g.Id, s3again2);

            var (t4, a4, d4) = await CountSeferState(factory, g.Id);
            _output.WriteLine($"ROUND 4 (tekrar değişiklik yok): Total={t4} Active={a4} Deleted={d4}");
            a4.Should().Be(3, "4. kez de 3 aktif kalmalı");

            // FINAL LOAD
            var finalLoad = await sSvc.GetByGuzergahIdAsync(g.Id);
            _output.WriteLine($"FINAL LOAD: {finalLoad.Count} sefer (beklenen: 3)");
            finalLoad.Count.Should().Be(3);

            _output.WriteLine("TÜM KONTROLLER GEÇTİ: Soft-deleted kayıtlar UI'ya karışmıyor.");
        }
        finally
        {
            await Cleanup(factory, g.Id);
        }
    }

    /// <summary>
    /// KRİTİK TEST: UI'da gösterilen seferleri DB'ye yaz, tekrar oku — sayı aynı mı?
    /// </summary>
    [Fact]
    public async Task UiSaveReload_CountMatchesExactly()
    {
        var (factory, sSvc) = SetupSeferService();
        await using var ctx = factory.CreateDbContext();
        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        cari.Should().NotBeNull();

        var ts = DateTime.UtcNow.ToString("HHmmssfff");
        var g = new Guzergah { GuzergahKodu = "UI-" + ts, GuzergahAdi = $"UI TEST {ts}", BirimFiyat = 500, CariId = cari!.Id, Aktif = true, FirmaId = 1 };
        ctx.Guzergahlar.Add(g);
        await ctx.SaveChangesAsync();

        try
        {
            int expected = 5;
            for (int round = 1; round <= 5; round++)
            {
                // UI load
                var uiSeferler = await sSvc.GetByGuzergahIdAsync(g.Id);
                _output.WriteLine($"Round {round} LOAD: UI count={uiSeferler.Count}");

                // UI'da gösterilen kadar seferi kaydet
                var toSave = uiSeferler.Any()
                    ? uiSeferler.Select(s => new GuzergahSefer { GuzergahId = g.Id, FirmaId = 1, Sira = s.Sira, SeferTipi = s.SeferTipi, Slot = s.Slot }).ToList()
                    : Enumerable.Range(1, expected).Select(i => new GuzergahSefer { GuzergahId = g.Id, FirmaId = 1, Sira = i, SeferTipi = SeferTipi.SabahAksam, Slot = SeferSlot.Sabah }).ToList();

                await sSvc.ReplaceAllAsync(g.Id, toSave);

                var (total, active, deleted) = await CountSeferState(factory, g.Id);
                _output.WriteLine($"Round {round} SAVE: Total={total} Active={active} Deleted={deleted}");
                active.Should().Be(toSave.Count, $"Round {round}: Aktif={active} hedef={toSave.Count}");
            }

            _output.WriteLine("5 ROUND TAMAMLANDI: Her seferinde aktif sayı hedefle eşit.");
        }
        finally
        {
            await Cleanup(factory, g.Id);
        }
    }

    /// <summary>
    /// KRİTİK TEST: Sincan/Fatih guzergah'ının gerçek DB durumu.
    /// </summary>
    [Fact]
    public async Task Diagnose_SincanFatih_FullState()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        var guzergahlar = await ctx.Guzergahlar
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted)
            .Where(x => x.GuzergahAdi.Contains("SINCAN") || x.GuzergahAdi.Contains("SİNCAN")
                     || x.GuzergahAdi.Contains("FATIH") || x.GuzergahAdi.Contains("FATİH")
                     || x.GuzergahAdi.Contains("NOM"))
            .ToListAsync();

        _output.WriteLine($"Bulunan güzergah: {guzergahlar.Count}");
        foreach (var g in guzergahlar)
        {
            _output.WriteLine($"═══ Id={g.Id} Adı={g.GuzergahAdi} ═══");

            var allSeferler = await ctx.GuzergahSeferleri
                .IgnoreQueryFilters()
                .Where(s => s.GuzergahId == g.Id)
                .OrderBy(s => s.IsDeleted)
                .ThenBy(s => s.Id)
                .ToListAsync();

            var active = allSeferler.Where(s => !s.IsDeleted).ToList();
            var deleted = allSeferler.Where(s => s.IsDeleted).ToList();

            _output.WriteLine($"  Toplam kayıt: {allSeferler.Count}");
            _output.WriteLine($"  Aktif (IsDeleted=false): {active.Count}");
            _output.WriteLine($"  Silinmiş (IsDeleted=true): {deleted.Count}");

            if (active.Count > 0)
            {
                _output.WriteLine($"  Aktif SiraNo'lar: {string.Join(",", active.Select(s => s.Sira))}");
                var dupes = active.GroupBy(s => s.Sira).Where(g => g.Count() > 1).ToList();
                if (dupes.Any())
                {
                    _output.WriteLine($"  ⚠️ DUPLICATE SIRA NO: {string.Join(",", dupes.Select(d => $"{d.Key}(x{d.Count()})"))}");
                }
            }

            if (deleted.Count > 0)
            {
                _output.WriteLine($"  Silinmiş SiraNo'lar: {string.Join(",", deleted.Select(s => s.Sira))}");
            }

            // GUZERGAH ANA DEĞERLERİ
            _output.WriteLine($"  KapasiteAdi: '{g.KapasiteAdi ?? "NULL"}'");
            _output.WriteLine($"  VarsayilanAracId: {g.VarsayilanAracId?.ToString() ?? "NULL"}");
            _output.WriteLine($"  VarsayilanSoforId: {g.VarsayilanSoforId?.ToString() ?? "NULL"}");
            _output.WriteLine($"  SeferTipi: {g.SeferTipi}");
            _output.WriteLine($"  PersonelSayisi: {g.PersonelSayisi}");
            _output.WriteLine("");
        }
    }

    /// <summary>
    /// KRİTİK TEST: Check if GetByGuzergahIdAsync filters out IsDeleted properly.
    /// </summary>
    [Fact]
    public async Task GetByGuzergahId_FiltersIsDeleted()
    {
        var (factory, sSvc) = SetupSeferService();
        await using var ctx = factory.CreateDbContext();
        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        cari.Should().NotBeNull();

        var ts = DateTime.UtcNow.ToString("HHmmssfff");
        var g = new Guzergah { GuzergahKodu = "FLT-" + ts, GuzergahAdi = $"FILTER TEST {ts}", BirimFiyat = 500, CariId = cari!.Id, Aktif = true, FirmaId = 1 };
        ctx.Guzergahlar.Add(g);
        await ctx.SaveChangesAsync();

        try
        {
            // Create 5 sefer
            var s5 = Enumerable.Range(1, 5).Select(i => new GuzergahSefer { GuzergahId = g.Id, FirmaId = 1, Sira = i }).ToList();
            await sSvc.ReplaceAllAsync(g.Id, s5);

            // Replace with 3 sefer → 5 soft-deleted + 3 active
            var s3 = Enumerable.Range(1, 3).Select(i => new GuzergahSefer { GuzergahId = g.Id, FirmaId = 1, Sira = i }).ToList();
            await sSvc.ReplaceAllAsync(g.Id, s3);

            // Load: should get 3 active, NOT 5 deleted
            var loaded = await sSvc.GetByGuzergahIdAsync(g.Id);
            _output.WriteLine($"Loaded: {loaded.Count} (expected 3)");
            loaded.Count.Should().Be(3);
            loaded.All(s => !s.IsDeleted).Should().BeTrue("Tüm dönen kayıtlar IsDeleted=false olmalı");

            // Replace with same 3: counts should stay 3
            var s3again = loaded.Select(s => new GuzergahSefer { GuzergahId = g.Id, FirmaId = 1, Sira = s.Sira }).ToList();
            await sSvc.ReplaceAllAsync(g.Id, s3again);

            var loaded2 = await sSvc.GetByGuzergahIdAsync(g.Id);
            _output.WriteLine($"Loaded after same replace: {loaded2.Count} (expected 3)");
            loaded2.Count.Should().Be(3);
        }
        finally
        {
            await Cleanup(factory, g.Id);
        }
    }

    private async Task<(int total, int active, int deleted)> CountSeferState(IDbContextFactory<ApplicationDbContext> f, int guzId)
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

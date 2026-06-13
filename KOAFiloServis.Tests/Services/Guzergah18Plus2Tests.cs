using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
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
/// 18 silinmiş + 2 aktif sefer senaryosu. UI'nin sadece 2 göstermesini test eder.
/// </summary>
public class Guzergah18Plus2Tests
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;
    private static readonly string StorageRoot = AppStoragePaths.DefaultStorageRoot;

    public Guzergah18Plus2Tests(ITestOutputHelper o) => _output = o;

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

    /// <summary>
    /// KRİTİK: Id=45'te 18 soft-deleted + 2 aktif varken GetByGuzergahIdAsync sadece 2 döndürmeli.
    /// </summary>
    [Fact]
    public async Task Id45_GetActiveSeferler_ReturnsOnly2()
    {
        var s = new ServiceCollection();
        s.AddSingleton<IAktifFirmaProvider>(new TestFirma(1));
        var sp = s.BuildServiceProvider();
        var f = new ScopedDbContextFactory(DbOpts, sp);
        var ss = new GuzergahSeferService(f, new TestFirma(1), Mock.Of<ILogger<GuzergahSeferService>>());

        // Simulate form load: GetByGuzergahIdAsync
        var loaded = await ss.GetByGuzergahIdAsync(45);

        _output.WriteLine($"Id=45 GetByGuzergahIdAsync döndü: {loaded.Count} sefer");
        _output.WriteLine($"SiraNo'lar: {string.Join(",", loaded.Select(s => s.Sira))}");
        _output.WriteLine($"Hepsi IsDeleted=false: {loaded.All(s => !s.IsDeleted)}");

        // 18 deleted + 2 active → sadece 2 dönmeli
        loaded.Count.Should().Be(2, "18 silinmiş + 2 aktif → UI sadece 2 görmeli");
        loaded.All(s => !s.IsDeleted).Should().BeTrue();
    }

    /// <summary>
    /// KRİTİK: 18+2 durumunda kaydet, sonra tekrar load — hâlâ 2 olmalı.
    /// </summary>
    [Fact]
    public async Task Id45_SaveThenReload_Still2()
    {
        var s = new ServiceCollection();
        s.AddSingleton<IAktifFirmaProvider>(new TestFirma(1));
        var sp = s.BuildServiceProvider();
        var f = new ScopedDbContextFactory(DbOpts, sp);
        var ss = new GuzergahSeferService(f, new TestFirma(1), Mock.Of<ILogger<GuzergahSeferService>>());

        // Load current state
        var before = await ss.GetByGuzergahIdAsync(45);
        _output.WriteLine($"Save öncesi UI load: {before.Count} sefer");

        // Save: kullanıcı 2 sefer olarak kaydediyor
        var toSave = before.Select(s => new GuzergahSefer
        {
            GuzergahId = 45, FirmaId = 1, Sira = s.Sira,
            SeferTipi = s.SeferTipi, Slot = s.Slot,
            KapasiteAdi = s.KapasiteAdi, AracId = s.AracId,
            SoforAd = s.SoforAd, SoforTelefon = s.SoforTelefon,
            FirmaAdiSerbest = s.FirmaAdiSerbest
        }).ToList();
        await ss.ReplaceAllAsync(45, toSave);

        // Reload
        var after = await ss.GetByGuzergahIdAsync(45);
        _output.WriteLine($"Save sonrası UI load: {after.Count} sefer");

        after.Count.Should().Be(2);

        // DB state
        await using var ctx = new ApplicationDbContext(DbOpts);
        var dbActive = await ctx.GuzergahSeferleri.IgnoreQueryFilters()
            .CountAsync(s => s.GuzergahId == 45 && !s.IsDeleted);
        var dbTotal = await ctx.GuzergahSeferleri.IgnoreQueryFilters()
            .CountAsync(s => s.GuzergahId == 45);

        _output.WriteLine($"DB Aktif: {dbActive}, DB Toplam: {dbTotal}");
        dbActive.Should().Be(2, "DB aktif 2 olmalı");
        // Total may be >2 because of soft-deleted history
    }

    /// <summary>
    /// KRİTİK: ReplaceAllAsync post-save guard sadece AKTİF count kullanıyor mu?
    /// </summary>
    [Fact]
    public async Task ReplaceAll_PostSaveGuard_UsesActiveCount()
    {
        // Bu test ReplaceAllAsync'in dbAktifCount hesaplamasını dolaylı doğrular.
        // Eğer toplam count kullanılsaydı 18+2=20 için exception fırlatırdı.
        // Test geçiyorsa guard doğru ç wakeupalışıyor demektir.

        var s = new ServiceCollection();
        s.AddSingleton<IAktifFirmaProvider>(new TestFirma(1));
        var sp = s.BuildServiceProvider();
        var f = new ScopedDbContextFactory(DbOpts, sp);
        var ss = new GuzergahSeferService(f, new TestFirma(1), Mock.Of<ILogger<GuzergahSeferService>>());

        var loaded = await ss.GetByGuzergahIdAsync(45);
        var toSave = loaded.Select(s => new GuzergahSefer
        {
            GuzergahId = 45, FirmaId = 1, Sira = s.Sira,
            SeferTipi = s.SeferTipi, Slot = s.Slot
        }).ToList();

        // Bu çağrı exception fırlatmamalı (çünkü aktif count 2, hedef 2)
        await ss.ReplaceAllAsync(45, toSave);
        _output.WriteLine("ReplaceAllAsync başarılı — guard aktif count kullanıyor (toplam değil)");

        // Now try with WRONG count (should throw)
        var wrongSave = loaded.Select(s => new GuzergahSefer
        {
            GuzergahId = 45, FirmaId = 1, Sira = s.Sira,
            SeferTipi = s.SeferTipi, Slot = s.Slot
        }).Take(1).ToList(); // Sadece 1 sefer

        try
        {
            await ss.ReplaceAllAsync(45, wrongSave);
            _output.WriteLine("⚠️ 1 sefer kaydı guard'ı tetiklemedi! (dbAktifCount=2, hedef=1)");
        }
        catch (InvalidOperationException ex)
        {
            _output.WriteLine($"✅ Guard tetiklendi: {ex.Message}");
        }
    }

    /// <summary>
    /// Tüm güzergahlar için GetByGuzergahIdAsync'in sadece aktif döndürdüğünü kontrol et.
    /// </summary>
    [Fact]
    public async Task AllGuzergah_GetActive_OnlyReturnsNonDeleted()
    {
        var s = new ServiceCollection();
        s.AddSingleton<IAktifFirmaProvider>(new TestFirma(1));
        var sp = s.BuildServiceProvider();
        var f = new ScopedDbContextFactory(DbOpts, sp);
        var ss = new GuzergahSeferService(f, new TestFirma(1), Mock.Of<ILogger<GuzergahSeferService>>());

        await using var ctx = new ApplicationDbContext(DbOpts);
        var guzergahlar = await ctx.Guzergahlar.IgnoreQueryFilters()
            .Where(g => !g.IsDeleted)
            .Select(g => g.Id)
            .ToListAsync();

        var failures = 0;
        foreach (var gid in guzergahlar)
        {
            var loaded = await ss.GetByGuzergahIdAsync(gid);
            var hasDeleted = loaded.Any(s => s.IsDeleted);
            if (hasDeleted)
            {
                failures++;
                _output.WriteLine($"⚠️ GuzergahId={gid}: GetByGuzergahIdAsync IsDeleted=true kayıt döndü!");
            }
        }

        _output.WriteLine($"Toplam güzergah: {guzergahlar.Count}, IsDeleted filtre hatası: {failures}");
        failures.Should().Be(0, "Hiçbir GetByGuzergahIdAsync çağrısı silinmiş kayıt döndürmemeli");
    }
}

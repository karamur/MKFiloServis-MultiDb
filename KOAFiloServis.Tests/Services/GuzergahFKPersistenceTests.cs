using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KOAFiloServis.Tests.Services;

public class GuzergahFKPersistenceTests
{
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    private sealed class TestFirma : IAktifFirmaProvider
    {
        public TestFirma(int id) => Mevcut = new() { FirmaId = id };
        public int? AktifFirmaId => Mevcut.FirmaId > 0 ? Mevcut.FirmaId : null;
        public bool HasAktifFirma => AktifFirmaId.HasValue;
        public bool TumFirmalar => false;
        public AktifFirmaBilgisi Mevcut { get; private set; }
#pragma warning disable CS0067 // Event is never used
        public event Action? AktifFirmaDegisti;
#pragma warning restore CS0067
        public void Set(AktifFirmaBilgisi f) { Mevcut = f; }
        public void SetTumFirmalar(bool t) { }
        public void SetDonem(int y, int m) { }
        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }

    private (IDbContextFactory<ApplicationDbContext>, GuzergahService) Setup()
    {
        var s = new ServiceCollection();
        var fp = new TestFirma(1);
        s.AddSingleton<IAktifFirmaProvider>(fp);
        var sp = s.BuildServiceProvider();
        var f = new ScopedDbContextFactory(DbOpts, sp);
        var n = new NumaraSerisiService(f);
        return (f, new GuzergahService(f, Mock.Of<ICacheService>(), n, fp, null!));
    }

    [Fact]
    public async Task UpdateGuzergah_WritesCariIdAndKurumId_ToDatabase()
    {
        var (factory, svc) = Setup();
        await using var ctx = factory.CreateDbContext();
        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        var kurum = await ctx.Kurumlar.FirstOrDefaultAsync(k => !k.IsDeleted);
        cari.Should().NotBeNull("test için bir cari olmalı");

        // Create test guzergah
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var g = new Guzergah { GuzergahKodu = $"GKFX{ts}", GuzergahAdi = $"TEST FK {ts}", BirimFiyat = 500, CariId = cari!.Id, KurumId = kurum?.Id, Aktif = true, FirmaId = 1 };
        var cr = await svc.CreateAsync(g);
        cr.Id.Should().BeGreaterThan(0);

        // Now update: change KurumId and CariId from a model that has the correct values
        var yeniCariId = cari.Id; // same cari
        cr.CariId = yeniCariId;
        cr.KurumId = kurum?.Id;
        var updated = await svc.UpdateAsync(cr);

        // Verify
        var db = await ReadAsync<Guzergah>(factory, cr.Id);
        db.Should().NotBeNull();
        db!.CariId.Should().Be(yeniCariId, "CariId DB'ye yazılmalı");
        if (kurum != null)
            db.KurumId.Should().Be(kurum.Id, "KurumId DB'ye yazılmalı");

        // Cleanup
        await Cleanup(factory, "Guzergahlar", cr.Id);
    }

    [Fact]
    public async Task UpdateGuzergah_DoesNotLoseCariId_OnZeroModelValue()
    {
        var (factory, svc) = Setup();
        await using var ctx = factory.CreateDbContext();
        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        cari.Should().NotBeNull();

        var ts = DateTime.UtcNow.ToString("HHmmss");
        var g = new Guzergah { GuzergahKodu = $"GKFX0{ts}", GuzergahAdi = $"TEST NULL {ts}", BirimFiyat = 500, CariId = cari!.Id, Aktif = true, FirmaId = 1 };
        var cr = await svc.CreateAsync(g);

        // Update with CariId=0 (UI bug simülasyonu)
        cr.CariId = 0;
        cr.KurumId = null;
        var updated = await svc.UpdateAsync(cr);

        var db = await ReadAsync<Guzergah>(factory, cr.Id);
        db.Should().NotBeNull();
        db!.CariId.Should().Be(cari.Id, "CariId 0 gelse bile mevcut değer korunmalı");

        await Cleanup(factory, "Guzergahlar", cr.Id);
    }

    private async Task<T?> ReadAsync<T>(IDbContextFactory<ApplicationDbContext> f, int id) where T : BaseEntity
    {
        await using var c = f.CreateDbContext();
        return await c.Set<T>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    private async Task Cleanup(IDbContextFactory<ApplicationDbContext> f, string table, int id)
    {
        try { await using var c = f.CreateDbContext(); await c.Database.ExecuteSqlAsync($"UPDATE \"{table}\" SET \"IsDeleted\"=true,\"DeletedAt\"=NOW() WHERE \"Id\"={id}"); } catch { }
    }
}

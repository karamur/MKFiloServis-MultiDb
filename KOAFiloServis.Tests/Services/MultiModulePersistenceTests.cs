using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Moq;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Multi-module DB persistence tests — proves Create/Update/SoftDelete actually write to PostgreSQL.
/// </summary>
public class MultiModulePersistenceTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";

    private static readonly DbContextOptions<ApplicationDbContext> DbOptions =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options;

    private sealed class TestAktifFirmaProvider : IAktifFirmaProvider
    {
        public TestAktifFirmaProvider(int firmaId)
            => Mevcut = new AktifFirmaBilgisi { FirmaId = firmaId, TumFirmalar = false };
        public int? AktifFirmaId => Mevcut.FirmaId > 0 ? Mevcut.FirmaId : null;
        public bool HasAktifFirma => AktifFirmaId.HasValue || TumFirmalar;
        public bool TumFirmalar => Mevcut.TumFirmalar;
        public AktifFirmaBilgisi Mevcut { get; private set; }
        public event Action? AktifFirmaDegisti;
        public void Set(AktifFirmaBilgisi f) { Mevcut = f; AktifFirmaDegisti?.Invoke(); }
        public void SetTumFirmalar(bool tf) { Mevcut.TumFirmalar = tf; AktifFirmaDegisti?.Invoke(); }
        public void SetDonem(int y, int m) { Mevcut.AktifDonemYil = y; Mevcut.AktifDonemAy = m; AktifFirmaDegisti?.Invoke(); }
        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly IServiceProvider _sp;
        public TestDbContextFactory(IServiceProvider sp) => _sp = sp;
        public ApplicationDbContext CreateDbContext()
        {
            var ctx = new ApplicationDbContext(DbOptions);
            ctx.SetServiceProvider(_sp);
            return ctx;
        }
    }

    private IServiceProvider CreateSp(int firmaId = 1)
    {
        var s = new ServiceCollection();
        s.AddSingleton<IAktifFirmaProvider>(new TestAktifFirmaProvider(firmaId));
        return s.BuildServiceProvider();
    }

    private IDbContextFactory<ApplicationDbContext> CreateFactory(int firmaId = 1)
        => new TestDbContextFactory(CreateSp(firmaId));

    private async Task<T?> ReadFromDbAsync<T>(IDbContextFactory<ApplicationDbContext> f, int id, bool ignoreFilters = false) where T : BaseEntity
    {
        await using var ctx = f.CreateDbContext();
        var q = ctx.Set<T>().AsNoTracking();
        if (ignoreFilters)
        {
            q = q.IgnoreQueryFilters();
            return await q.FirstOrDefaultAsync(x => x.Id == id);
        }

        return await q.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    /// <summary>Soft-deletes a test row via raw SQL for quick cleanup.</summary>
    private async Task CleanupRow(IDbContextFactory<ApplicationDbContext> f, string table, int id)
    {
        try
        {
            await using var c = f.CreateDbContext();
            await c.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"{table}\" SET \"IsDeleted\"=true,\"DeletedAt\"=NOW() WHERE \"Id\"={id}");
        }
        catch { }
    }

    /// <summary>Soft-deletes test Arac records by SaseNo.</summary>
    private async Task CleanupArac(IDbContextFactory<ApplicationDbContext> f, string saseNo)
    {
        try
        {
            await using var c = f.CreateDbContext();
            var list = await c.Araclar.Where(a => a.SaseNo == saseNo).ToListAsync();
            foreach (var a in list) { a.IsDeleted = true; a.DeletedAt = DateTime.UtcNow; }
            if (list.Any()) await c.SaveChangesAsync();
            await EnsureAracPlakaSequenceSyncAsync();
        }
        catch { }
    }

    private static async Task EnsureAracPlakaSequenceSyncAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        const string sql = "SELECT setval(pg_get_serial_sequence('\"AracPlakalar\"','Id'), COALESCE((SELECT MAX(\"Id\") FROM \"AracPlakalar\"), 1), true);";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Finds one existing CariId for the test firm.</summary>
    private async Task<int> GetAnyCariId(IDbContextFactory<ApplicationDbContext> f)
    {
        await using var c = f.CreateDbContext();
        var cari = await c.Cariler.FirstOrDefaultAsync(x => !x.IsDeleted && x.FirmaId == 1);
        return cari?.Id ?? 1;
    }

    // ═══════════════════════════════════════════════════════════════
    // SoforService — Create / Update / SoftDelete
    // ═══════════════════════════════════════════════════════════════
    private SoforService CreateSoforService(int firmaId = 1)
    {
        var sp = CreateSp(firmaId);
        var factory = new TestDbContextFactory(sp);
        var numara = new NumaraSerisiService(factory);
        return new SoforService(factory, Mock.Of<IMuhasebeService>(), Mock.Of<ICacheService>(), numara, Mock.Of<IMaasSnapshotService>());
    }

    private static DateTime UtcNow => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

    [Fact]
    public async Task SoforService_Create_WritesToDatabase()
    {
        var f = CreateFactory(1);
        var svc = CreateSoforService(1);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var sofor = new Sofor { Ad = "TPERSIST", Soyad = ts, Gorev = PersonelGorev.Sofor, Aktif = true, SoforKodu = $"TP{ts}", Telefon = "5550000000", FirmaId = 1, IseBaslamaTarihi = UtcNow };

        var r = await svc.CreateAsync(sofor);
        r.Id.Should().BeGreaterThan(0);

        var db = await ReadFromDbAsync<Sofor>(f, r.Id);
        db.Should().NotBeNull();
        db!.Ad.Should().Be("TPERSIST");
        await CleanupRow(f, "Personeller", r.Id);
    }

    [Fact]
    public async Task SoforService_Update_WritesToDatabase()
    {
        var f = CreateFactory(1);
        var svc = CreateSoforService(1);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var sofor = new Sofor { Ad = "TUPD", Soyad = ts, Gorev = PersonelGorev.Sofor, Aktif = true, SoforKodu = $"TU{ts}", Telefon = "5550000001", FirmaId = 1, IseBaslamaTarihi = UtcNow };
        var cr = await svc.CreateAsync(sofor);

        cr.Ad = "UPDATED";
        await svc.UpdateAsync(cr);

        var db = await ReadFromDbAsync<Sofor>(f, cr.Id);
        db.Should().NotBeNull();
        db!.Ad.Should().Be("UPDATED");
        await CleanupRow(f, "Personeller", cr.Id);
    }

    [Fact]
    public async Task SoforService_SoftDelete_SetsIsDeleted()
    {
        var f = CreateFactory(1);
        var svc = CreateSoforService(1);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var sofor = new Sofor { Ad = "TDEL", Soyad = ts, Gorev = PersonelGorev.Sofor, Aktif = true, SoforKodu = $"TD{ts}", Telefon = "5550000002", FirmaId = 1, IseBaslamaTarihi = UtcNow };
        var cr = await svc.CreateAsync(sofor);

        await svc.DeleteAsync(cr.Id);

        var silinen = await ReadFromDbAsync<Sofor>(f, cr.Id, ignoreFilters: true);
        silinen.Should().NotBeNull();
        silinen!.IsDeleted.Should().BeTrue();
        silinen.DeletedAt.Should().NotBeNull();

        var normal = await ReadFromDbAsync<Sofor>(f, cr.Id);
        normal.Should().BeNull("normal query hide soft-deleted");
        await CleanupRow(f, "Personeller", cr.Id);
    }

    // ═══════════════════════════════════════════════════════════════
    // AracService — Create / Update / SoftDelete
    // ═══════════════════════════════════════════════════════════════
    private AracService CreateAracService(int firmaId = 1)
    {
        var sp = CreateSp(firmaId);
        var factory = new TestDbContextFactory(sp);
        return new AracService(factory, Mock.Of<ISecureFileService>(), Mock.Of<ICacheService>(),
            sp.GetRequiredService<IAktifFirmaProvider>());
    }

    [Fact]
    public async Task AracService_Create_WritesToDatabase()
    {
        var svc = CreateAracService(1);
        var f = CreateFactory(1);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var saseNo = $"TCH{ts}";
        var plaka = $"06TP{ts}";

        await CleanupArac(f, saseNo);

        var arac = new Arac { SaseNo = saseNo, Marka = "TPERSIST", Model = "T", ModelYili = 2026, Aktif = true, FirmaId = 1 };
        var r = await svc.CreateAsync(arac, plaka);
        r.Id.Should().BeGreaterThan(0);

        var db = await ReadFromDbAsync<Arac>(f, r.Id);
        db.Should().NotBeNull();
        db!.Marka.Should().Be("TPERSIST");
        await CleanupRow(f, "Araclar", r.Id);
    }

    [Fact]
    public async Task AracService_Update_WritesToDatabase()
    {
        var svc = CreateAracService(1);
        var f = CreateFactory(1);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var saseNo = $"TCHUP{ts}";
        var plaka = $"06TU{ts}";
        await CleanupArac(f, saseNo);

        var cr = await svc.CreateAsync(new Arac { SaseNo = saseNo, Marka = "TUPD", Model = "T", ModelYili = 2026, Aktif = true, FirmaId = 1 }, plaka);
        cr.Marka = "UPDATED";
        await svc.UpdateAsync(cr);

        var db = await ReadFromDbAsync<Arac>(f, cr.Id);
        db.Should().NotBeNull();
        db!.Marka.Should().Be("UPDATED");
        await CleanupRow(f, "Araclar", cr.Id);
    }

    [Fact]
    public async Task AracService_SoftDelete_SetsIsDeleted()
    {
        var svc = CreateAracService(1);
        var f = CreateFactory(1);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var saseNo = $"TCHDEL{ts}";
        var plaka = $"06TD{ts}";
        await CleanupArac(f, saseNo);

        var cr = await svc.CreateAsync(new Arac { SaseNo = saseNo, Marka = "TDEL", Model = "T", ModelYili = 2026, Aktif = true, FirmaId = 1 }, plaka);
        await svc.DeleteAsync(cr.Id);

        var silinen = await ReadFromDbAsync<Arac>(f, cr.Id, ignoreFilters: true);
        silinen.Should().NotBeNull();
        silinen!.IsDeleted.Should().BeTrue();

        var normal = await ReadFromDbAsync<Arac>(f, cr.Id);
        normal.Should().BeNull("normal query hide soft-deleted");
        await CleanupRow(f, "Araclar", cr.Id);
    }

    // ═══════════════════════════════════════════════════════════════
    // CariService — Create / Update / SoftDelete
    // ═══════════════════════════════════════════════════════════════
    private CariService CreateCariService(int firmaId = 1)
    {
        var sp = CreateSp(firmaId);
        var factory = new TestDbContextFactory(sp);
        var numara = new NumaraSerisiService(factory);
        return new CariService(factory, sp.GetRequiredService<IAktifFirmaProvider>(), numara);
    }

    [Fact]
    public async Task CariService_Create_WritesToDatabase()
    {
        var svc = CreateCariService(1);
        var f = CreateFactory(1);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var cari = new Cari { CariKodu = $"CT{ts}", Unvan = $"TPERSIST CARI {ts}", CariTipi = CariTipi.Tedarikci, Aktif = true, FirmaId = 1 };

        var r = await svc.CreateAsync(cari);
        r.Id.Should().BeGreaterThan(0);

        var db = await ReadFromDbAsync<Cari>(f, r.Id);
        db.Should().NotBeNull();
        db!.Unvan.Should().Contain("TPERSIST CARI");
        await CleanupRow(f, "Cariler", r.Id);
    }

    [Fact]
    public async Task CariService_Update_WritesToDatabase()
    {
        var svc = CreateCariService(1);
        var f = CreateFactory(1);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var cari = new Cari { CariKodu = $"CU{ts}", Unvan = $"TUPD CARI {ts}", CariTipi = CariTipi.Tedarikci, Aktif = true, FirmaId = 1 };
        var cr = await svc.CreateAsync(cari);

        cr.Unvan = $"UPDATED CARI {ts}";
        await svc.UpdateAsync(cr);

        var db = await ReadFromDbAsync<Cari>(f, cr.Id);
        db.Should().NotBeNull();
        db!.Unvan.Should().Contain("UPDATED CARI");
        await CleanupRow(f, "Cariler", cr.Id);
    }

    [Fact]
    public async Task CariService_SoftDelete_SetsIsDeleted()
    {
        var svc = CreateCariService(1);
        var f = CreateFactory(1);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var cr = await svc.CreateAsync(new Cari { CariKodu = $"CD{ts}", Unvan = $"TDEL CARI {ts}", CariTipi = CariTipi.Tedarikci, Aktif = true, FirmaId = 1 });

        await svc.DeleteAsync(cr.Id);

        var silinen = await ReadFromDbAsync<Cari>(f, cr.Id, ignoreFilters: true);
        silinen.Should().NotBeNull("ignoreFilters sonrası silinmiş kayıt bulunmalı");
        silinen!.IsDeleted.Should().BeTrue();

        var normal = await ReadFromDbAsync<Cari>(f, cr.Id);
        normal.Should().BeNull("normal query soft-deleted görünmemeli");
        await CleanupRow(f, "Cariler", cr.Id);
    }

    // ═══════════════════════════════════════════════════════════════
    // GuzergahService — Create / Update / SoftDelete
    // ═══════════════════════════════════════════════════════════════
    private GuzergahService CreateGuzergahService(int firmaId = 1)
    {
        var sp = CreateSp(firmaId);
        var factory = new TestDbContextFactory(sp);
        var numara = new NumaraSerisiService(factory);
        return new GuzergahService(factory, Mock.Of<ICacheService>(), numara, sp.GetRequiredService<IAktifFirmaProvider>(), null!);
    }

    [Fact]
    public async Task GuzergahService_Create_WritesToDatabase()
    {
        var svc = CreateGuzergahService(1);
        var f = CreateFactory(1);
        var cariId = await GetAnyCariId(f);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var g = new Guzergah { GuzergahKodu = $"GT{ts}", GuzergahAdi = $"TPERSIST GUZERGAH {ts}", BirimFiyat = 500, CariId = cariId, Aktif = true, FirmaId = 1 };

        var r = await svc.CreateAsync(g);
        r.Id.Should().BeGreaterThan(0);

        var db = await ReadFromDbAsync<Guzergah>(f, r.Id);
        db.Should().NotBeNull();
        db!.GuzergahAdi.Should().Contain("TPERSIST GUZERGAH");

        await svc.DeleteAsync(r.Id);
        var silinen = await ReadFromDbAsync<Guzergah>(f, r.Id, ignoreFilters: true);
        silinen!.IsDeleted.Should().BeTrue();
        await CleanupRow(f, "Guzergahlar", r.Id);
    }

    [Fact]
    public async Task GuzergahService_Update_WritesToDatabase()
    {
        var svc = CreateGuzergahService(1);
        var f = CreateFactory(1);
        var cariId = await GetAnyCariId(f);
        var ts = DateTime.UtcNow.ToString("HHmmss");
        var cr = await svc.CreateAsync(new Guzergah { GuzergahKodu = $"GU{ts}", GuzergahAdi = $"GUPD GUZ {ts}", BirimFiyat = 600, CariId = cariId, Aktif = true, FirmaId = 1 });

        cr.BirimFiyat = 999;
        await svc.UpdateAsync(cr);

        var db = await ReadFromDbAsync<Guzergah>(f, cr.Id);
        db.Should().NotBeNull();
        db!.BirimFiyat.Should().Be(999, "Update DB'ye yazılmalı");
        await CleanupRow(f, "Guzergahlar", cr.Id);
    }

    // ═══════════════════════════════════════════════════════════════
    // HakedisPuantaj — FK + Detay + Status (already proven, kept as regression)
    // ═══════════════════════════════════════════════════════════════
    [Fact]
    public async Task HakedisPuantaj_FK_And_Detay_WrittenToDatabase()
    {
        var sp = CreateSp(1);
        var f = new TestDbContextFactory(sp);
        var fp = sp.GetRequiredService<IAktifFirmaProvider>();
        var svc = new HakedisPuantajService(f, fp, NullLogger<HakedisPuantajService>.Instance);

        await using var ctx = f.CreateDbContext();
        var arac = await ctx.Araclar.FirstOrDefaultAsync(a => !a.IsDeleted && a.FirmaId == 1);
        var sofor = await ctx.Soforler.FirstOrDefaultAsync(p => !p.IsDeleted && p.FirmaId == 1);
        var guzergah = await ctx.Guzergahlar.FirstOrDefaultAsync(g => !g.IsDeleted && g.FirmaId == 1 && g.Aktif);
        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        arac.Should().NotBeNull(); sofor.Should().NotBeNull(); guzergah.Should().NotBeNull(); cari.Should().NotBeNull();

        var eski = await ctx.HakedisPuantajlar.Where(h => h.Yil == 2026 && h.Ay == 6 && h.GuzergahId == guzergah!.Id && h.AracId == arac!.Id && h.SoforId == sofor!.Id).ToListAsync();
        foreach (var e in eski) { e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; }
        if (eski.Any()) await ctx.SaveChangesAsync();

        var r = await svc.CreateAsync(new HakedisPuantaj { FirmaId = 1, Yil = 2026, Ay = 6, AracId = arac!.Id, SoforId = sofor!.Id, GuzergahId = guzergah!.Id, CariId = cari!.Id, GelirBirimFiyat = 1000, GiderBirimFiyat = 800, KdvOrani = 20 });

        var db = await ReadFromDbAsync<HakedisPuantaj>(f, r.Id);
        db.Should().NotBeNull();
        db!.AracId.Should().Be(arac.Id);
        db.SoforId.Should().Be(sofor.Id);
        db.GuzergahId.Should().Be(guzergah.Id);
        db.CariId.Should().Be(cari.Id);
        db.FirmaId.Should().Be(1);
        db.Durum.Should().Be(HakedisDurumu.Taslak);

        // Detay FK kontrol
        await using var ctx2 = f.CreateDbContext();
        var detaylar = await ctx2.HakedisPuantajDetaylar.Where(d => d.HakedisPuantajId == r.Id && !d.IsDeleted).ToListAsync();
        detaylar.Should().NotBeEmpty("GunlukDetayOlustur detay oluşturmalı");
        detaylar.All(d => d.HakedisPuantajId == r.Id).Should().BeTrue();

        foreach (var d in detaylar) { d.IsDeleted = true; d.DeletedAt = DateTime.UtcNow; }
        await ctx2.SaveChangesAsync();
        await CleanupRow(f, "HakedisPuantajlar", r.Id);
    }

    [Fact]
    public async Task HakedisPuantaj_StatusChange_WritesToDatabase()
    {
        var sp = CreateSp(1);
        var f = new TestDbContextFactory(sp);
        var fp = sp.GetRequiredService<IAktifFirmaProvider>();
        var svc = new HakedisPuantajService(f, fp, NullLogger<HakedisPuantajService>.Instance);

        await using var ctx = f.CreateDbContext();
        var arac = await ctx.Araclar.FirstOrDefaultAsync(a => !a.IsDeleted && a.FirmaId == 1);
        var sofor = await ctx.Soforler.FirstOrDefaultAsync(p => !p.IsDeleted && p.FirmaId == 1);
        var guzergah = await ctx.Guzergahlar.FirstOrDefaultAsync(g => !g.IsDeleted && g.FirmaId == 1 && g.Aktif);
        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);

        var eski = await ctx.HakedisPuantajlar.Where(h => h.Yil == 2026 && h.Ay == 6 && h.GuzergahId == guzergah!.Id && h.AracId == arac!.Id && h.SoforId == sofor!.Id).ToListAsync();
        foreach (var e in eski) { e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; }
        if (eski.Any()) await ctx.SaveChangesAsync();

        var cr = await svc.CreateAsync(new HakedisPuantaj { FirmaId = 1, Yil = 2026, Ay = 6, AracId = arac!.Id, SoforId = sofor!.Id, GuzergahId = guzergah!.Id, CariId = cari!.Id, GelirBirimFiyat = 1000, GiderBirimFiyat = 800, KdvOrani = 20 });
        cr.Durum.Should().Be(HakedisDurumu.Taslak);

        await svc.OnayaGonderAsync(cr.Id);
        (await ReadFromDbAsync<HakedisPuantaj>(f, cr.Id))!.Durum.Should().Be(HakedisDurumu.OnayBekliyor);

        await svc.OnaylaAsync(cr.Id);
        (await ReadFromDbAsync<HakedisPuantaj>(f, cr.Id))!.Durum.Should().Be(HakedisDurumu.Onaylandi);

        await CleanupRow(f, "HakedisPuantajlar", cr.Id);
        await using var ctx3 = f.CreateDbContext();
        var detaylar = await ctx3.HakedisPuantajDetaylar.Where(d => d.HakedisPuantajId == cr.Id).ToListAsync();
        foreach (var d in detaylar) { d.IsDeleted = true; d.DeletedAt = DateTime.UtcNow; }
        await ctx3.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // AracEvrak — Create + SoftDelete
    // ═══════════════════════════════════════════════════════════════
    [Fact]
    public async Task AracEvrak_CreateAndDelete_WritesToDatabase()
    {
        var svc = CreateAracService(1);
        var f = CreateFactory(1);

        await using var ctx = f.CreateDbContext();
        var arac = await ctx.Araclar.FirstOrDefaultAsync(a => !a.IsDeleted && a.FirmaId == 1);
        arac.Should().NotBeNull();

        var evrak = new AracEvrak { AracId = arac!.Id, EvrakAdi = $"TPERSIST EVRAK {DateTime.UtcNow:HHmmss}", EvrakKategorisi = "Diger", CreatedAt = DateTime.UtcNow };

        var r = await svc.CreateAracEvrakAsync(evrak);
        r.Id.Should().BeGreaterThan(0);

        var db = await ReadFromDbAsync<AracEvrak>(f, r.Id);
        db.Should().NotBeNull();
        db!.AracId.Should().Be(arac.Id);

        await svc.DeleteAracEvrakAsync(r.Id);
        var silinen = await ReadFromDbAsync<AracEvrak>(f, r.Id, ignoreFilters: true);
        silinen.Should().NotBeNull();
        silinen!.IsDeleted.Should().BeTrue();

        await CleanupRow(f, "AracEvraklari", r.Id);
    }
}

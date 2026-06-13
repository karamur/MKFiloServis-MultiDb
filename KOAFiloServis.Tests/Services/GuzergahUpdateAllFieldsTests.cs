using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Güzergah UpdateAsync'in TÜM alanları DB'ye yazdığını doğrular.
/// </summary>
public class GuzergahUpdateAllFieldsTests
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public GuzergahUpdateAllFieldsTests(ITestOutputHelper o) => _output = o;

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

    private (IDbContextFactory<ApplicationDbContext>, GuzergahService) Setup()
    {
        var s = new ServiceCollection();
        s.AddSingleton<IAktifFirmaProvider>(new TestFirma(1));
        var sp = s.BuildServiceProvider();
        var f = new ScopedDbContextFactory(DbOpts, sp);
        var n = new NumaraSerisiService(f);
        return (f, new GuzergahService(f, Mock.Of<ICacheService>(), n, new TestFirma(1)));
    }

    [Fact]
    public async Task UpdateGuzergah_AllFields_Persisted()
    {
        var (factory, svc) = Setup();
        await using var ctx = factory.CreateDbContext();

        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        cari.Should().NotBeNull("test için bir cari olmalı");

        // Create test guzergah
        var ts = DateTime.UtcNow.ToString("HHmmssfff");
        var g = new Guzergah
        {
            GuzergahKodu = "TEST-" + ts,
            GuzergahAdi = $"TEST ALL FIELDS {ts}",
            BaslangicNoktasi = "A Noktasi",
            BitisNoktasi = "B Noktasi",
            BirimFiyat = 100m,
            GiderFiyat = 80m,
            Mesafe = 15.5m,
            TahminiSure = 30,
            SeferTipi = SeferTipi.Sabah,
            PersonelSayisi = 10,
            KapasiteAdi = "16+1",
            CariId = cari!.Id,
            FirmaId = 1,
            Notlar = "Test not",
            PuantajCarpani = 1.5m,
            Aktif = true
        };
        var cr = await svc.CreateAsync(g);
        cr.Id.Should().BeGreaterThan(0);

        try
        {
            // Now reload and update all fields with DIFFERENT values
            var model = await ReadGuzergah(factory, cr.Id);
            model.Should().NotBeNull();
            model!.GuzergahAdi = "UPDATED " + ts;
            model.BaslangicNoktasi = "X Noktasi";
            model.BitisNoktasi = "Y Noktasi";
            model.SeferTipi = SeferTipi.SabahAksam;
            model.PersonelSayisi = 25;
            model.KapasiteAdi = "27+1";
            model.BirimFiyat = 250m;
            model.GiderFiyat = 180m;
            model.Mesafe = 25.0m;
            model.TahminiSure = 45;
            model.VarsayilanAracId = null;
            model.VarsayilanSoforId = null;
            model.Notlar = "Updated notlar";
            model.PuantajCarpani = 2.0m;
            model.Aktif = false;

            var updated = await svc.UpdateAsync(model);

            // Verify ALL fields
            var db = await ReadGuzergah(factory, cr.Id);
            db.Should().NotBeNull();

            _output.WriteLine($"GuzergahId={db!.Id}");
            _output.WriteLine($"Adi: {db.GuzergahAdi}");
            _output.WriteLine($"Baslangic: {db.BaslangicNoktasi}");
            _output.WriteLine($"Bitis: {db.BitisNoktasi}");
            _output.WriteLine($"SeferTipi: {db.SeferTipi}");
            _output.WriteLine($"PersonelSayisi: {db.PersonelSayisi}");
            _output.WriteLine($"KapasiteAdi: {db.KapasiteAdi}");
            _output.WriteLine($"BirimFiyat: {db.BirimFiyat}");
            _output.WriteLine($"GiderFiyat: {db.GiderFiyat}");
            _output.WriteLine($"Mesafe: {db.Mesafe}");
            _output.WriteLine($"TahminiSure: {db.TahminiSure}");
            _output.WriteLine($"AracId: {db.VarsayilanAracId}");
            _output.WriteLine($"SoforId: {db.VarsayilanSoforId}");
            _output.WriteLine($"Notlar: {db.Notlar}");
            _output.WriteLine($"PuantajCarpani: {db.PuantajCarpani}");
            _output.WriteLine($"Aktif: {db.Aktif}");

            // Assertions
            db.GuzergahAdi.Should().Be("UPDATED " + ts, "GuzergahAdi güncellenmeli");
            db.BaslangicNoktasi.Should().Be("X Noktasi", "BaslangicNoktasi güncellenmeli");
            db.BitisNoktasi.Should().Be("Y Noktasi", "BitisNoktasi güncellenmeli");
            db.SeferTipi.Should().Be(SeferTipi.SabahAksam, "SeferTipi güncellenmeli");
            db.PersonelSayisi.Should().Be(25, "PersonelSayisi güncellenmeli");
            db.KapasiteAdi.Should().Be("27+1", "KapasiteAdi güncellenmeli");
            db.BirimFiyat.Should().Be(250m, "BirimFiyat güncellenmeli");
            db.GiderFiyat.Should().Be(180m, "GiderFiyat güncellenmeli");
            db.Mesafe.Should().Be(25.0m, "Mesafe güncellenmeli");
            db.TahminiSure.Should().Be(45, "TahminiSure güncellenmeli");
            db.VarsayilanAracId.Should().BeNull("VarsayilanAracId güncellenmeli");
            db.VarsayilanSoforId.Should().BeNull("VarsayilanSoforId güncellenmeli");
            db.Notlar.Should().Be("Updated notlar", "Notlar güncellenmeli");
            db.PuantajCarpani.Should().Be(2.0m, "PuantajCarpani güncellenmeli");
            db.Aktif.Should().BeFalse("Aktif güncellenmeli");
        }
        finally
        {
            await Cleanup(factory, cr.Id);
        }
    }

    [Fact]
    public async Task UpdateGuzergah_FK_SoforId_NotOverwritten()
    {
        var (factory, svc) = Setup();
        await using var ctx = factory.CreateDbContext();

        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        cari.Should().NotBeNull("test için bir cari olmalı");

        // Gerçek araç ve şoför bul
        var arac = await ctx.Araclar
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => !a.IsDeleted && a.FirmaId == 1);
        var sofor = await ctx.Soforler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.FirmaId == 1);

        var ts = DateTime.UtcNow.ToString("HHmmssfff");
        var g = new Guzergah
        {
            GuzergahKodu = "TST-" + ts,
            GuzergahAdi = $"TEST SOFOR {ts}",
            BirimFiyat = 500,
            CariId = cari!.Id,
            Aktif = true,
            FirmaId = 1,
            VarsayilanAracId = arac?.Id,
            VarsayilanSoforId = sofor?.Id
        };
        var cr = await svc.CreateAsync(g);
        cr.Id.Should().BeGreaterThan(0);

        try
        {
            // Simulate edit: load, DON'T call AracSecimiDegistiAsync
            var model = await ReadGuzergah(factory, cr.Id);
            model.Should().NotBeNull();

            // Verify soforId is preserved from DB
            if (sofor != null)
            {
                model!.VarsayilanSoforId.Should().Be(sofor.Id,
                    "SoforId edit sayfası açıldığında DB'deki değeri korumalı");
            }

            // Change sofor manually
            var baskaSofor = await ctx.Soforler
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Id != sofor!.Id && s.FirmaId == 1);

            if (baskaSofor != null)
            {
                model!.VarsayilanSoforId = baskaSofor.Id;
                var updated = await svc.UpdateAsync(model);
                var db = await ReadGuzergah(factory, cr.Id);

                db!.VarsayilanSoforId.Should().Be(baskaSofor.Id,
                    "Değiştirilen VarsayilanSoforId DB'ye kaydedilmeli");
                _output.WriteLine($"SoforId başarıyla güncellendi: {sofor.Id} -> {baskaSofor.Id} (DB: {db.VarsayilanSoforId})");
            }
            else
            {
                _output.WriteLine("UYARI: Test için ikinci bir şoför bulunamadı, FK overwrite testi atlandı.");
            }
        }
        finally
        {
            await Cleanup(factory, cr.Id);
        }
    }

    [Fact]
    public async Task UpdateGuzergah_SeferSayisi_NotIncremented()
    {
        var (factory, svc) = Setup();
        await using var ctx = factory.CreateDbContext();

        var cari = await ctx.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);
        cari.Should().NotBeNull("test için bir cari olmalı");

        var ts = DateTime.UtcNow.ToString("HHmmssfff");
        var g = new Guzergah
        {
            GuzergahKodu = "TST-" + ts,
            GuzergahAdi = $"TEST SEFER {ts}",
            BirimFiyat = 500,
            CariId = cari!.Id,
            Aktif = true,
            FirmaId = 1
        };
        var cr = await svc.CreateAsync(g);
        cr.Id.Should().BeGreaterThan(0);

        try
        {
            // 1. Güncelleme: sefer sayısı değişmemiş
            var model1 = await ReadGuzergah(factory, cr.Id);
            model1!.GuzergahAdi = "ROUND 1 " + ts;
            model1.PersonelSayisi = 5;
            await svc.UpdateAsync(model1);

            var db1 = await ReadGuzergah(factory, cr.Id);
            db1!.PersonelSayisi.Should().Be(5);

            // 2. Güncelleme: sefer tipi ve diğer alanlar
            var model2 = await ReadGuzergah(factory, cr.Id);
            model2!.SeferTipi = SeferTipi.Aksam;
            model2.KapasiteAdi = "16+1";
            model2.PersonelSayisi = 8;
            await svc.UpdateAsync(model2);

            var db2 = await ReadGuzergah(factory, cr.Id);
            db2!.SeferTipi.Should().Be(SeferTipi.Aksam);
            db2.KapasiteAdi.Should().Be("16+1");
            db2.PersonelSayisi.Should().Be(8, "Sefer sayısı sürekli artmamalı, tam değer yazılmalı");

            _output.WriteLine($"Sefer sayısı: {db2.PersonelSayisi} (beklenen: 8, artış yok)");
            _output.WriteLine($"Sefer tipi: {db2.SeferTipi} (beklenen: Aksam)");
            _output.WriteLine($"Kapasite: {db2.KapasiteAdi} (beklenen: 16+1)");
        }
        finally
        {
            await Cleanup(factory, cr.Id);
        }
    }

    private async Task<Guzergah?> ReadGuzergah(IDbContextFactory<ApplicationDbContext> f, int id)
    {
        await using var c = f.CreateDbContext();
        return await c.Set<Guzergah>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    private async Task Cleanup(IDbContextFactory<ApplicationDbContext> f, int id)
    {
        try
        {
            await using var c = f.CreateDbContext();
            await c.Database.ExecuteSqlAsync(
                $"UPDATE \"Guzergahlar\" SET \"IsDeleted\"=true,\"DeletedAt\"=NOW() WHERE \"Id\"={id}");
        }
        catch { }
    }
}

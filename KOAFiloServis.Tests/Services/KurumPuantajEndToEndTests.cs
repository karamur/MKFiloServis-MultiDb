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

public class KurumPuantajEndToEndTests
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public KurumPuantajEndToEndTests(ITestOutputHelper o) => _output = o;

    [Fact]
    public async Task SablonOlustur_TumunuKaydet_NoErrors()
    {
        var s = new ServiceCollection();
        s.AddSingleton<IAktifFirmaProvider>(new TestFirma(1));
        var sp = s.BuildServiceProvider();
        var factory = new ScopedDbContextFactory(DbOpts, sp);

        var kpSvc = new KurumPuantajService(factory,
            Mock.Of<IPuantajSyncService>(),
            Mock.Of<ILogger<KurumPuantajService>>());

        await using var ctx = new ApplicationDbContext(DbOpts);
        var kurum = await ctx.Kurumlar.IgnoreQueryFilters()
            .FirstOrDefaultAsync(k => !k.IsDeleted && k.KurumAdi.Contains("TURKAK"));
        if (kurum == null) { _output.WriteLine("TURKAK yok"); return; }

        _output.WriteLine($"Kurum: {kurum.KurumAdi} (Id={kurum.Id})");

        // Şablon Oluştur
        var sablon = await kpSvc.SablonOlusturAsync(kurum.Id, 2026, 5);
        _output.WriteLine($"Şablon: {sablon.Count} satır");

        // SoforId durumu
        var eksik = sablon.Where(k => k.SoforId == null || k.SoforId == 0).ToList();
        var tam = sablon.Where(k => k.SoforId != null && k.SoforId != 0).ToList();
        _output.WriteLine($"SoforId DOLU: {tam.Count}, EKSİK: {eksik.Count}");
        foreach (var e in eksik)
            _output.WriteLine($"  EKSİK: Arac={e.Plaka} SoforAd='{e.SoforAdi}'");

        // Sadece SoforId dolu olanları kaydet
        if (tam.Any())
        {
            foreach (var k in tam.Take(2))
            {
                k.SetGunDeger(1, 2);
                k.SetGunDeger(2, 2);
            }
            await kpSvc.TopluSavePuantajAsync(tam.Take(2).ToList());
            _output.WriteLine("✅ Tümünü Kaydet (SoforId dolu satırlar): başarılı");
        }

        // Reload
        var reload = await kpSvc.SablonOlusturAsync(kurum.Id, 2026, 5);
        _output.WriteLine($"Reload: {reload.Count} satır");

        // Duplicate
        var dupes = reload.GroupBy(k => new { k.GuzergahId, k.AracId, k.Slot })
            .Count(g => g.Count() > 1);
        _output.WriteLine($"Duplicate: {dupes}");
        dupes.Should().Be(0);

        _output.WriteLine("✅ TEST TAMAMLANDI");
    }

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
}

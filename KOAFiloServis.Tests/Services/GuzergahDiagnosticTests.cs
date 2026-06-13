using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

public class GuzergahDiagnosticTests
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public GuzergahDiagnosticTests(ITestOutputHelper o) => _output = o;

    [Fact]
    public async Task Diagnose_SincanFatih_Guzergah()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        // Find the guzergah
        var g = await ctx.Guzergahlar
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                (x.GuzergahAdi != null && (x.GuzergahAdi.Contains("SINCAN") || x.GuzergahAdi.Contains("SİNCAN")
                                        || x.GuzergahAdi.Contains("FATIH") || x.GuzergahAdi.Contains("FATİH")))
                && !x.IsDeleted);

        if (g == null)
        {
            _output.WriteLine("Güzergah bulunamadı!");
            return;
        }

        _output.WriteLine($"═══ GÜZERGAH ANA KAYDI ═══");
        _output.WriteLine($"Id: {g.Id}");
        _output.WriteLine($"GuzergahAdi: {g.GuzergahAdi}");
        _output.WriteLine($"SeferTipi: {g.SeferTipi}");
        _output.WriteLine($"KapasiteAdi: '{g.KapasiteAdi ?? "NULL"}'");
        _output.WriteLine($"VarsayilanAracId: {g.VarsayilanAracId?.ToString() ?? "NULL"}");
        _output.WriteLine($"VarsayilanSoforId: {g.VarsayilanSoforId?.ToString() ?? "NULL"}");
        _output.WriteLine($"PersonelSayisi: {g.PersonelSayisi}");
        _output.WriteLine($"FirmaId: {g.FirmaId}");
        _output.WriteLine($"BirimFiyat: {g.BirimFiyat}");
        _output.WriteLine($"GiderFiyat: {g.GiderFiyat}");
        _output.WriteLine($"Mesafe: {g.Mesafe}");
        _output.WriteLine($"TahminiSure: {g.TahminiSure}");
        _output.WriteLine($"PuantajCarpani: {g.PuantajCarpani}");
        _output.WriteLine($"Aktif: {g.Aktif}");
        _output.WriteLine($"Notlar: '{g.Notlar ?? "NULL"}'");
        _output.WriteLine($"UpdatedAt: {g.UpdatedAt}");
        _output.WriteLine("");

        // Seferler
        _output.WriteLine($"═══ AKTİF SEFERLER ═══");
        var seferler = await ctx.GuzergahSeferleri
            .IgnoreQueryFilters()
            .Where(s => s.GuzergahId == g.Id)
            .ToListAsync();

        var aktif = seferler.Where(s => !s.IsDeleted).ToList();
        var deleted = seferler.Where(s => s.IsDeleted).ToList();

        _output.WriteLine($"Toplam kayıt (silinenler dahil): {seferler.Count}");
        _output.WriteLine($"Aktif: {aktif.Count}");
        _output.WriteLine($"Silinmiş (soft-delete): {deleted.Count}");

        // Heavily deleted?
        if (deleted.Count > 0)
        {
            _output.WriteLine($"UYARI: {deleted.Count} silinmiş sefer var! ReplaceAll soft-delete yapmış olabilir.");
            _output.WriteLine($"  En yüksek SıraNo (aktif): {aktif.Max(s => (int?)s.Id) ?? 0}");
        }

        // Show first 3 active
        foreach (var s in aktif.Take(3))
        {
            _output.WriteLine($"  Id={s.Id} Tip={s.SeferTipi} Slot={s.Slot} Kapasite={s.KapasiteAdi} AracId={s.AracId} SoforAd={s.SoforAd}");
        }

        // Check if deleted records are from previous saves
        if (deleted.Count > 0 && aktif.Count > 0)
        {
            _output.WriteLine("");
            _output.WriteLine("KRİTİK: Soft-delete edilmiş seferler var. ReplaceAll çalışıyor ama eski kayıtları siliyor.");
            _output.WriteLine("Bu normal bir davranış - replace = soft-delete old + insert new.");
            _output.WriteLine($"Aktif sefer sayısı UI'da {aktif.Count} olmalı.");
        }
    }

    [Fact]
    public async Task Diagnose_SeferReplace_Behavior()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        // Take a sample guzergah that has seferler
        var sample = await ctx.Guzergahlar
            .IgnoreQueryFilters()
            .Where(g => !g.IsDeleted)
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync(g => ctx.GuzergahSeferleri.Any(s => s.GuzergahId == g.Id && !s.IsDeleted));

        if (sample == null)
        {
            _output.WriteLine("Seferli güzergah bulunamadı.");
            return;
        }

        _output.WriteLine($"Test güzergah: Id={sample.Id} Adı={sample.GuzergahAdi}");

        // Count all records (including deleted)
        var allCount = await ctx.GuzergahSeferleri
            .IgnoreQueryFilters()
            .CountAsync(s => s.GuzergahId == sample.Id);

        var activeCount = await ctx.GuzergahSeferleri
            .IgnoreQueryFilters()
            .CountAsync(s => s.GuzergahId == sample.Id && !s.IsDeleted);

        var deletedCount = await ctx.GuzergahSeferleri
            .IgnoreQueryFilters()
            .CountAsync(s => s.GuzergahId == sample.Id && s.IsDeleted);

        _output.WriteLine($"Toplam kayıt: {allCount}");
        _output.WriteLine($"Aktif: {activeCount}");
        _output.WriteLine($"Silinmiş: {deletedCount}");

        if (deletedCount > 0)
        {
            var ratio = (double)deletedCount / allCount * 100;
            _output.WriteLine($"Silinme oranı: {ratio:F1}%");
            if (ratio > 50)
                _output.WriteLine("UYARI: Silinmiş sefer oranı yüksek. Her kaydetmede append+delete yapılıyor olabilir!");
        }
        else if (allCount == activeCount && activeCount > 0)
        {
            _output.WriteLine("OK: Tüm kayıtlar aktif, silinmiş kayıt yok.");
        }
    }
}

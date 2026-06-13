using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Execute sonrası doğrulama: dosya varlığı, DB path güncellemesi, eski dosya koruması.
/// </summary>
public class ArchiveMigrationVerifyTests
{
    private readonly ITestOutputHelper _output;
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOptions =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options;

    public ArchiveMigrationVerifyTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Verify_Migration_Results()
    {
        var storageRoot = AppStoragePaths.DefaultStorageRoot;
        await using var ctx = new ApplicationDbContext(DbOptions);

        _output.WriteLine("═══ PERSONEL EVRAK DOĞRULAMA ═══");
        var personelEvraklar = await ctx.PersonelOzlukEvraklar
            .IgnoreQueryFilters()
            .Include(e => e.Sofor).ThenInclude(s => s.Firma)
            .Include(e => e.EvrakTanim)
            .Where(e => !string.IsNullOrWhiteSpace(e.DosyaYolu) && !e.IsDeleted)
            .Take(10)
            .ToListAsync();

        var newFormatCount = 0;
        var oldFormatCount = 0;
        var diskExists = 0;
        var diskMissing = 0;

        foreach (var evrak in personelEvraklar)
        {
            var n = (evrak.DosyaYolu ?? "").Replace('\\', '/').TrimStart('/');
            if (n.StartsWith(AppStoragePaths.PersonelEvrakRelativeRoot + "/"))
                newFormatCount++;
            else
                oldFormatCount++;

            // Dosya var mı?
            if (n.StartsWith("Arsiv/"))
            {
                var fullPath = Path.Combine(storageRoot, n.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullPath))
                    diskExists++;
                else
                {
                    diskMissing++;
                    _output.WriteLine($"  EKSİK: {fullPath}");
                }
            }
        }
        _output.WriteLine($"Personel: Yeni format={newFormatCount} Eski format={oldFormatCount} Diskte var={diskExists} Eksik={diskMissing}");

        _output.WriteLine("");
        _output.WriteLine("═══ ARAÇ EVRAK DOĞRULAMA ═══");
        var aracEvraklar = await ctx.AracEvrakDosyalari
            .IgnoreQueryFilters()
            .Include(d => d.AracEvrak).ThenInclude(e => e.Arac).ThenInclude(a => a.Firma)
            .Where(d => !string.IsNullOrWhiteSpace(d.DosyaYolu) && !d.IsDeleted)
            .Take(10)
            .ToListAsync();

        newFormatCount = 0; oldFormatCount = 0; diskExists = 0; diskMissing = 0;
        foreach (var dosya in aracEvraklar)
        {
            var n = (dosya.DosyaYolu ?? "").Replace('\\', '/').TrimStart('/');
            if (n.StartsWith(AppStoragePaths.AracEvrakRelativeRoot + "/"))
                newFormatCount++;
            else
                oldFormatCount++;

            if (n.StartsWith("Arsiv/"))
            {
                var fullPath = Path.Combine(storageRoot, n.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullPath))
                    diskExists++;
                else
                {
                    diskMissing++;
                    _output.WriteLine($"  EKSİK: {fullPath}");
                }
            }
        }
        _output.WriteLine($"Araç: Yeni format={newFormatCount} Eski format={oldFormatCount} Diskte var={diskExists} Eksik={diskMissing}");

        _output.WriteLine("");
        _output.WriteLine("═══ YENİ DİZİN YAPISI ═══");
        var personelRoot = Path.Combine(storageRoot, "Arsiv", "Sifreli", "Personeller");
        if (Directory.Exists(personelRoot))
        {
            foreach (var dir in Directory.GetDirectories(personelRoot).Take(10))
                _output.WriteLine($"  Personel: {Path.GetFileName(dir)}");
        }

        var aracRoot = Path.Combine(storageRoot, "Arsiv", "Sifreli", "Araclar");
        if (Directory.Exists(aracRoot))
        {
            foreach (var dir in Directory.GetDirectories(aracRoot).Take(10))
                _output.WriteLine($"  Araç: {Path.GetFileName(dir)}");
        }

        _output.WriteLine("");
        _output.WriteLine("═══ ESKİ DOSYALAR KORUNUYOR MU? ═══");
        var eskiPersonelRoot = Path.Combine(storageRoot, "Arsiv", "Sifreli", "Personeller");
        if (Directory.Exists(eskiPersonelRoot))
        {
            var eskiDirs = Directory.GetDirectories(eskiPersonelRoot);
            var eskiCount = eskiDirs.Count(d => Path.GetFileName(d).Contains("-202") && !Path.GetFileName(d).Contains(" - "));
            _output.WriteLine($"Eski formatlı personel klasörleri (tireli, firmasız): {eskiCount}");
        }

        _output.WriteLine("");
        _output.WriteLine("DOĞRULAMA TAMAMLANDI.");
    }
}

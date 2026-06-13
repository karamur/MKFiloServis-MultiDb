using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// DB'deki evrak path'lerinin detaylı analizi.
/// </summary>
public class ArchivePathDetailTests
{
    private readonly ITestOutputHelper _output;

    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";

    private static readonly DbContextOptions<ApplicationDbContext> DbOptions =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options;

    public ArchivePathDetailTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task ShowAllPathFormats()
    {
        await using var ctx = new ApplicationDbContext(DbOptions);

        _output.WriteLine("═══ PERSONEL EVRAK PATH'LERİ ═══");
        var personelPaths = await ctx.PersonelOzlukEvraklar
            .IgnoreQueryFilters()
            .Where(e => !string.IsNullOrWhiteSpace(e.DosyaYolu) && !e.IsDeleted)
            .Select(e => e.DosyaYolu)
            .ToListAsync();

        var pathGroups = personelPaths
            .Select(p => p ?? "")
            .GroupBy(p =>
            {
                var n = p.Replace('\\', '/').TrimStart('/');
                if (n.StartsWith("Arsiv/Sifreli/Personeller/")) return "YENİ-Personel";
                if (n.StartsWith("Arsiv/Sifreli/")) return "YENİ-Arsiv";
                if (n.StartsWith("personel-ozluk/")) return "ESKİ-personel-ozluk";
                if (n.StartsWith("ozluk/")) return "ESKİ-ozluk";
                if (n.StartsWith("uploads/")) return "ESKİ-uploads";
                return "DİĞER: " + n.Split('/').FirstOrDefault();
            })
            .OrderByDescending(g => g.Count());

        foreach (var g in pathGroups)
        {
            _output.WriteLine($"  {g.Key}: {g.Count()} kayıt");
        }
        _output.WriteLine($"  TOPLAM: {personelPaths.Count}");
        _output.WriteLine("");

        // Örnek path'ler (her gruptan 2)
        foreach (var g in pathGroups)
        {
            _output.WriteLine($"--- {g.Key} ---");
            foreach (var p in g.Take(2))
                _output.WriteLine($"    {p}");
        }
        _output.WriteLine("");

        _output.WriteLine("═══ ARAÇ EVRAK PATH'LERİ ═══");
        var aracPaths = await ctx.AracEvrakDosyalari
            .IgnoreQueryFilters()
            .Where(d => !string.IsNullOrWhiteSpace(d.DosyaYolu) && !d.IsDeleted)
            .Select(d => d.DosyaYolu)
            .ToListAsync();

        var aracGroups = aracPaths
            .Select(p => p ?? "")
            .GroupBy(p =>
            {
                var n = p.Replace('\\', '/').TrimStart('/');
                if (n.StartsWith("Arsiv/Sifreli/Araclar/")) return "YENİ-Araclar";
                if (n.StartsWith("Arsiv/Sifreli/")) return "YENİ-Arsiv";
                if (n.StartsWith("arac-evrak/")) return "ESKİ-arac-evrak";
                if (n.StartsWith("evraklar/")) return "ESKİ-evraklar";
                if (n.StartsWith("uploads/")) return "ESKİ-uploads";
                return "DİĞER: " + n.Split('/').FirstOrDefault();
            })
            .OrderByDescending(g => g.Count());

        foreach (var g in aracGroups)
        {
            _output.WriteLine($"  {g.Key}: {g.Count()} kayıt");
        }
        _output.WriteLine($"  TOPLAM: {aracPaths.Count}");
        _output.WriteLine("");

        foreach (var g in aracGroups)
        {
            _output.WriteLine($"--- {g.Key} ---");
            foreach (var p in g.Take(2))
                _output.WriteLine($"    {p}");
        }

        Assert.True(true);
    }
}

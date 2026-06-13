using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// SON DOĞRULAMA: Kod değişikliği yapmaz. Sadece durumu raporlar.
/// </summary>
public class FinalVerificationTests
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public FinalVerificationTests(ITestOutputHelper o) => _output = o;

    [Fact]
    public async Task Verify_Id45_State_AfterCleanup()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        _output.WriteLine("═══ Id=45 /H.SEMT SİNCAN FATİH ═══");

        var g = await ctx.Guzergahlar.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == 45 && !x.IsDeleted);
        if (g == null) { _output.WriteLine("Güzergah bulunamadı!"); return; }

        _output.WriteLine($"Adı: {g.GuzergahAdi}");
        _output.WriteLine($"Aktif: {g.Aktif}");

        var all = await ctx.GuzergahSeferleri.IgnoreQueryFilters()
            .Where(s => s.GuzergahId == 45).OrderBy(s => s.IsDeleted).ThenBy(s => s.Sira).ThenBy(s => s.Id).ToListAsync();

        var active = all.Where(s => !s.IsDeleted).ToList();
        var deleted = all.Where(s => s.IsDeleted).ToList();

        _output.WriteLine($"Toplam: {all.Count}");
        _output.WriteLine($"Aktif (IsDeleted=false): {active.Count}");
        _output.WriteLine($"Silinmiş (IsDeleted=true): {deleted.Count}");

        if (active.Any())
        {
            _output.WriteLine($"Aktif SiraNo'lar: {string.Join(",", active.Select(s => s.Sira))}");
            var distinctSira = active.Select(s => s.Sira).Distinct().Count();
            _output.WriteLine($"Aktif distinct SiraNo: {distinctSira}");

            var dupes = active.GroupBy(s => s.Sira).Where(g => g.Count() > 1).ToList();
            if (dupes.Any())
                _output.WriteLine($"⚠️ DUPLICATE VAR: {string.Join(",", dupes.Select(d => $"{d.Key}(x{d.Count()})"))}");
            else
                _output.WriteLine("✅ Duplicate YOK");
        }

        // Beklenen: 2 aktif, 0 duplicate
        Assert.True(active.Count == 2, $"Aktif sefer 2 olmalı, gerçek: {active.Count}");
        var activeDupes = active.GroupBy(s => s.Sira).Any(g => g.Count() > 1);
        Assert.False(activeDupes, "Aktif duplicate olmamalı");
    }

    [Fact]
    public async Task Verify_AllGuzergah_DuplicateCheck()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        _output.WriteLine("═══ TÜM GÜZERGAHLAR AKTİF DUPLICATE KONTROLÜ ═══");

        var dupes = await ctx.GuzergahSeferleri
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted)
            .GroupBy(s => new { s.GuzergahId, s.Sira })
            .Where(g => g.Count() > 1)
            .Select(g => new { g.Key.GuzergahId, g.Key.Sira, Count = g.Count() })
            .ToListAsync();

        if (!dupes.Any())
        {
            _output.WriteLine("✅ Tüm güzergahlarda aktif duplicate YOK.");
        }
        else
        {
            _output.WriteLine($"⚠️ {dupes.Count} duplicate grubu bulundu:");
            foreach (var d in dupes.Take(20))
                _output.WriteLine($"  GuzergahId={d.GuzergahId} Sira={d.Sira} Adet={d.Count}");

            // Id=45 hariç başkaları var mı?
            var others = dupes.Where(d => d.GuzergahId != 45).ToList();
            if (others.Any())
            {
                _output.WriteLine($"⚠️ Id=45 dışında {others.Count} grup duplicate var! GuzergahId'ler: {string.Join(",", others.Select(d => d.GuzergahId).Distinct())}");
            }
            else
            {
                _output.WriteLine("Sadece Id=45'te duplicate var (zaten temizlendi/temizleniyor).");
            }
        }

        Assert.Empty(dupes);
    }

    [Fact]
    public async Task Verify_Guards_Present()
    {
        _output.WriteLine("═══ GUARD KONTROLÜ ═══");

        // Check _kaydetGuard in GuzergahForm.razor
        var formPath = System.IO.Path.Combine(
            System.AppContext.BaseDirectory,
            "../../../../KOAFiloServis.Web/Components/Pages/Guzergahlar/GuzergahForm.razor");
        var formContent = System.IO.File.ReadAllText(formPath);

        _output.WriteLine($"Double-submit guard (_kaydetGuard): {(formContent.Contains("_kaydetGuard") ? "✅ VAR" : "⚠️ YOK")}");
        _output.WriteLine($"Pre-save duplicate check: {(formContent.Contains("Distinct().Count()") ? "✅ VAR" : "⚠️ YOK")}");

        // Check ReplaceAllAsync guard
        var svcPath = System.IO.Path.Combine(
            System.AppContext.BaseDirectory,
            "../../../../KOAFiloServis.Web/Services/GuzergahSeferService.cs");
        var svcContent = System.IO.File.ReadAllText(svcPath);

        _output.WriteLine($"Post-save DB count guard: {(svcContent.Contains("dbAktifCount") ? "✅ VAR" : "⚠️ YOK")}");
        _output.WriteLine($"Debug log (GUZERGAH_REPLACE): {(svcContent.Contains("GUZERGAH_REPLACE") ? "✅ VAR" : "⚠️ YOK")}");
        _output.WriteLine($"Rollback: {(svcContent.Contains("RollbackAsync()") ? "✅ VAR" : "⚠️ YOK")}");
        _output.WriteLine($"!IsDeleted load filtresi: {(svcContent.Contains("!s.IsDeleted") ? "✅ VAR" : "⚠️ YOK")}");
    }
}

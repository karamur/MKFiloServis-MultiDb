using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

public class FixMissingSoforler
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public FixMissingSoforler(ITestOutputHelper o) => _output = o;

    [Fact]
    public async Task Diagnose_MissingSoforler()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        var names = new[] { "KENAN SÖYLEMEZ", "ATANER İLERİ" };
        foreach (var name in names)
        {
            var matches = await ctx.Soforler
                .IgnoreQueryFilters()
                .Where(s => !s.IsDeleted)
                .Where(s => s.Ad.Contains(name.Split(' ')[0]) || s.Soyad.Contains(name.Split(' ')[1]))
                .Select(s => new { s.Id, s.Ad, s.Soyad, s.TamAd, s.FirmaId })
                .ToListAsync();

            _output.WriteLine($"'{name}' için arama:");
            foreach (var m in matches)
                _output.WriteLine($"  Id={m.Id} Ad={m.Ad} Soyad={m.Soyad} TamAd={m.TamAd} FirmaId={m.FirmaId}");
            if (!matches.Any())
                _output.WriteLine($"  BULUNAMADI — eklenmesi gerekiyor");
        }
    }

    [Fact]
    public async Task Fix_Add_MissingSoforler()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        // FirmaId 1 altında şoför ekle
        var now = DateTime.UtcNow;

        // KENAN SÖYLEMEZ
        var existingK = await ctx.Soforler.IgnoreQueryFilters()
            .AnyAsync(s => s.Ad == "KENAN" && s.Soyad == "SÖYLEMEZ" && !s.IsDeleted);
        if (!existingK)
        {
            ctx.Soforler.Add(new KOAFiloServis.Shared.Entities.Sofor
            {
                Ad = "KENAN", Soyad = "SÖYLEMEZ", SoforKodu = "SOFOR-KS",
                FirmaId = 1, Gorev = KOAFiloServis.Shared.Entities.PersonelGorev.Sofor,
                Aktif = true, CreatedAt = now, UpdatedAt = now
            });
            _output.WriteLine("KENAN SÖYLEMEZ eklendi");
        }
        else
            _output.WriteLine("KENAN SÖYLEMEZ zaten var");

        // ATANER İLERİ
        var existingA = await ctx.Soforler.IgnoreQueryFilters()
            .AnyAsync(s => s.Ad == "ATANER" && s.Soyad == "İLERİ" && !s.IsDeleted);
        if (!existingA)
        {
            ctx.Soforler.Add(new KOAFiloServis.Shared.Entities.Sofor
            {
                Ad = "ATANER", Soyad = "İLERİ", SoforKodu = "SOFOR-AI",
                FirmaId = 1, Gorev = KOAFiloServis.Shared.Entities.PersonelGorev.Sofor,
                Aktif = true, CreatedAt = now, UpdatedAt = now
            });
            _output.WriteLine("ATANER İLERİ eklendi");
        }
        else
            _output.WriteLine("ATANER İLERİ zaten var");

        await ctx.SaveChangesAsync();
        _output.WriteLine("✅ Eksik şoförler eklendi");
    }

    [Fact]
    public async Task Fix_Duplicate_GuzergahSefer_06C1333()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        // 06C1333 için ATANER İLERİ duplicate aktif sefer var mı?
        var dupes = await ctx.GuzergahSeferleri
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.SoforAd == "ATANER İLERİ")
            .ToListAsync();

        _output.WriteLine($"ATANER İLERİ sefer sayısı: {dupes.Count}");
        foreach (var d in dupes)
            _output.WriteLine($"  Id={d.Id} GuzergahId={d.GuzergahId} AracId={d.AracId} Slot={d.Slot} SoforAd={d.SoforAd}");

        // Duplicate varsa, sadece 1 tanesini bırak
        if (dupes.Count > 1)
        {
            var keep = dupes.OrderBy(d => d.Id).First();
            var delete = dupes.Where(d => d.Id != keep.Id).ToList();
            foreach (var d in delete)
            {
                d.IsDeleted = true;
                d.UpdatedAt = DateTime.UtcNow;
            }
            await ctx.SaveChangesAsync();
            _output.WriteLine($"✅ {delete.Count} duplicate ATANER İLERİ seferi soft-delete edildi, Id={keep.Id} korundu");
        }
        else
            _output.WriteLine("Duplicate yok");
    }
}

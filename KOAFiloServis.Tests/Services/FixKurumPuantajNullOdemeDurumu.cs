using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

public class FixKurumPuantajNullOdemeDurumu
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public FixKurumPuantajNullOdemeDurumu(ITestOutputHelper o) => _output = o;

    [Fact]
    public async Task Fix_Null_GelirOdemeDurumu()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        // Raw SQL ile NULL kayıtları bul
        var nullGelir = await ctx.Database
            .ExecuteSqlAsync($@"SELECT COUNT(*) FROM ""PuantajKayitlar"" WHERE ""GelirOdemeDurumu"" IS NULL AND COALESCE(""IsDeleted"", false) = false");

        _output.WriteLine($"GelirOdemeDurumu NULL aktif kayıt: {nullGelir}");

        // Fix: 0 = Odenmedi
        var affected = await ctx.Database.ExecuteSqlAsync(
            $@"UPDATE ""PuantajKayitlar"" SET ""GelirOdemeDurumu"" = 0, ""UpdatedAt"" = NOW() WHERE ""GelirOdemeDurumu"" IS NULL AND COALESCE(""IsDeleted"", false) = false");

        _output.WriteLine($"GelirOdemeDurumu düzeltildi: {affected} kayıt → 0 (Odenmedi)");
    }

    [Fact]
    public async Task Fix_Null_GiderOdemeDurumu()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        var affected = await ctx.Database.ExecuteSqlAsync(
            $@"UPDATE ""PuantajKayitlar"" SET ""GiderOdemeDurumu"" = 0, ""UpdatedAt"" = NOW() WHERE ""GiderOdemeDurumu"" IS NULL AND COALESCE(""IsDeleted"", false) = false");

        _output.WriteLine($"GiderOdemeDurumu düzeltildi: {affected} kayıt → 0 (Odenmedi)");
    }

    [Fact]
    public async Task Verify_NoNullOdemeDurumu()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        // Raw count query
        var result = await ctx.Database.SqlQuery<int>(
            $@"SELECT COUNT(*)::int FROM ""PuantajKayitlar"" WHERE ""GelirOdemeDurumu"" IS NULL AND COALESCE(""IsDeleted"", false) = false").ToListAsync();

        _output.WriteLine($"Kalan GelirOdemeDurumu NULL: {result.FirstOrDefault()}");
        Assert.Equal(0, result.FirstOrDefault());
    }
}

using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

public class FixKurumPuantajNullKaynak
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public FixKurumPuantajNullKaynak(ITestOutputHelper o) => _output = o;

    [Fact]
    public async Task Fix_Null_Kaynak()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        var affected = await ctx.Database.ExecuteSqlAsync(
            $@"UPDATE ""PuantajKayitlar"" SET ""Kaynak"" = 0, ""UpdatedAt"" = NOW() WHERE ""Kaynak"" IS NULL AND COALESCE(""IsDeleted"", false) = false");

        _output.WriteLine($"Kaynak NULL → 0 (Manuel): {affected} kayıt");
    }

    [Fact]
    public async Task Verify_NoNullKaynak()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);
        var result = await ctx.Database.SqlQuery<int>(
            $@"SELECT COUNT(*)::int FROM ""PuantajKayitlar"" WHERE ""Kaynak"" IS NULL AND COALESCE(""IsDeleted"", false) = false").ToListAsync();
        _output.WriteLine($"Kalan Kaynak NULL: {result.FirstOrDefault()}");
        Assert.Equal(0, result.FirstOrDefault());
    }
}

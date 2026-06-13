using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

public class FixSoforOdemeTipi
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public FixSoforOdemeTipi(ITestOutputHelper o) => _output = o;

    [Fact]
    public async Task Fix_SoforOdemeTipi_Null()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        var affected = await ctx.Database.ExecuteSqlRawAsync(
            @"UPDATE ""PuantajKayitlar"" SET ""SoforOdemeTipi"" = COALESCE(""SoforOdemeTipi"", 'Ozmal'), ""UpdatedAt"" = NOW() WHERE ""SoforOdemeTipi"" IS NULL AND COALESCE(""IsDeleted"", false) = false");

        _output.WriteLine($"SoforOdemeTipi NULL → Ozmal: {affected} kayıt");
    }

    [Fact]
    public async Task Verify_AllNulls_Cleared()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);
        await using var cmd = ctx.Database.GetDbConnection().CreateCommand();
        await ctx.Database.OpenConnectionAsync();

        var cols = new[] { "SoforOdemeTipi", "OnayDurum", "Kaynak", "GelirOdemeDurumu", "GiderOdemeDurumu" };
        foreach (var col in cols)
        {
            cmd.CommandText = $"SELECT COUNT(*) FROM \"PuantajKayitlar\" WHERE \"{col}\" IS NULL AND COALESCE(\"IsDeleted\", false) = false";
            var count = (long)(await cmd.ExecuteScalarAsync())!;
            _output.WriteLine($"{col} NULL: {count}");
            Assert.Equal(0L, count);
        }
    }
}

using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

public class FixPuantajKayitAllNulls
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public FixPuantajKayitAllNulls(ITestOutputHelper o) => _output = o;

    [Fact]
    public async Task Diagnose_AllNullColumns()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);
        await using var cmd = ctx.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = @"SELECT COUNT(*) FROM ""PuantajKayitlar"" WHERE ""OnayDurum"" IS NULL AND COALESCE(""IsDeleted"", false) = false";
        await ctx.Database.OpenConnectionAsync();
        var onay = (long)(await cmd.ExecuteScalarAsync())!;
        cmd.CommandText = @"SELECT COUNT(*) FROM ""PuantajKayitlar"" WHERE ""Kaynak"" IS NULL AND COALESCE(""IsDeleted"", false) = false";
        var kaynak = (long)(await cmd.ExecuteScalarAsync())!;
        cmd.CommandText = @"SELECT COUNT(*) FROM ""PuantajKayitlar"" WHERE ""GelirOdemeDurumu"" IS NULL AND COALESCE(""IsDeleted"", false) = false";
        var gelir = (long)(await cmd.ExecuteScalarAsync())!;
        cmd.CommandText = @"SELECT COUNT(*) FROM ""PuantajKayitlar"" WHERE ""GiderOdemeDurumu"" IS NULL AND COALESCE(""IsDeleted"", false) = false";
        var gider = (long)(await cmd.ExecuteScalarAsync())!;

        _output.WriteLine($"OnayDurum NULL: {onay}");
        _output.WriteLine($"Kaynak NULL: {kaynak}");
        _output.WriteLine($"GelirOdemeDurumu NULL: {gelir}");
        _output.WriteLine($"GiderOdemeDurumu NULL: {gider}");
    }

    [Fact]
    public async Task Fix_AllNullColumns()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        var affected = await ctx.Database.ExecuteSqlRawAsync(
            @"UPDATE ""PuantajKayitlar"" SET ""OnayDurum"" = COALESCE(""OnayDurum"", 'Taslak'), ""Kaynak"" = COALESCE(""Kaynak"", 'Manuel'), ""GelirOdemeDurumu"" = COALESCE(""GelirOdemeDurumu"", 'Odenmedi'), ""GiderOdemeDurumu"" = COALESCE(""GiderOdemeDurumu"", 'Odenmedi'), ""UpdatedAt"" = NOW() WHERE COALESCE(""IsDeleted"", false) = false AND (""OnayDurum"" IS NULL OR ""Kaynak"" IS NULL OR ""GelirOdemeDurumu"" IS NULL OR ""GiderOdemeDurumu"" IS NULL)");

        _output.WriteLine($"Toplu düzeltme: {affected} kayıt");
    }

    [Fact]
    public async Task Verify_AllNullColumns_Cleared()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);
        await using var cmd = ctx.Database.GetDbConnection().CreateCommand();
        await ctx.Database.OpenConnectionAsync();

        var cols = new[] { "OnayDurum", "Kaynak", "GelirOdemeDurumu", "GiderOdemeDurumu" };
        foreach (var col in cols)
        {
            cmd.CommandText = $"SELECT COUNT(*) FROM \"PuantajKayitlar\" WHERE \"{col}\" IS NULL AND COALESCE(\"IsDeleted\", false) = false";
            var count = (long)(await cmd.ExecuteScalarAsync())!;
            _output.WriteLine($"{col} NULL: {count}");
            Assert.Equal(0L, count);
        }
    }
}

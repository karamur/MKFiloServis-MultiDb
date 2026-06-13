using FluentAssertions;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

public class FixId45Isolation
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public FixId45Isolation(ITestOutputHelper o) => _output = o;

    [Fact]
    public async Task Fix_Id45_Ensure2Active()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        _output.WriteLine("═══ FIX ÖNCESİ ═══");
        var before = await ctx.GuzergahSeferleri.IgnoreQueryFilters()
            .Where(s => s.GuzergahId == 45).OrderBy(s => s.Id).ToListAsync();
        foreach (var s in before)
            _output.WriteLine($"Id={s.Id} Sira={s.Sira} IsDeleted={s.IsDeleted} FirmaId={s.FirmaId}");

        var activeBefore = before.Count(s => !s.IsDeleted);
        _output.WriteLine($"Aktif önce: {activeBefore}");

        // Hard clean: raw SQL soft-delete ALL, then insert 2 fresh
        await ctx.Database.ExecuteSqlAsync(
            $"UPDATE \"GuzergahSeferleri\" SET \"IsDeleted\"=true, \"UpdatedAt\"=NOW() WHERE \"GuzergahId\"=45");

        var now = DateTime.UtcNow;
        ctx.GuzergahSeferleri.Add(new KOAFiloServis.Shared.Entities.GuzergahSefer { GuzergahId = 45, FirmaId = 1, Sira = 1, SeferTipi = KOAFiloServis.Shared.Entities.SeferTipi.Sabah, Slot = KOAFiloServis.Shared.Entities.SeferSlot.Sabah, CreatedAt = now, UpdatedAt = now });
        ctx.GuzergahSeferleri.Add(new KOAFiloServis.Shared.Entities.GuzergahSefer { GuzergahId = 45, FirmaId = 1, Sira = 2, SeferTipi = KOAFiloServis.Shared.Entities.SeferTipi.Aksam, Slot = KOAFiloServis.Shared.Entities.SeferSlot.Aksam, CreatedAt = now, UpdatedAt = now });
        await ctx.SaveChangesAsync();

        var after = await ctx.GuzergahSeferleri.IgnoreQueryFilters()
            .Where(s => s.GuzergahId == 45 && !s.IsDeleted).ToListAsync();
        _output.WriteLine($"Aktif sonra: {after.Count}");
        foreach (var s in after)
            _output.WriteLine($"  Id={s.Id} Sira={s.Sira} IsDeleted={s.IsDeleted}");

        after.Count.Should().Be(2);
    }

    [Fact]
    public async Task Verify_ReplaceAll_With_ExecuteUpdate_Works()
    {
        // Ensure clean state first
        await using (var ctx = new ApplicationDbContext(DbOpts))
        {
            await ctx.Database.ExecuteSqlAsync(
                $"UPDATE \"GuzergahSeferleri\" SET \"IsDeleted\"=true WHERE \"GuzergahId\"=45");
            var now = DateTime.UtcNow;
            ctx.GuzergahSeferleri.Add(new KOAFiloServis.Shared.Entities.GuzergahSefer { GuzergahId = 45, FirmaId = 1, Sira = 1, CreatedAt = now, UpdatedAt = now });
            ctx.GuzergahSeferleri.Add(new KOAFiloServis.Shared.Entities.GuzergahSefer { GuzergahId = 45, FirmaId = 1, Sira = 2, CreatedAt = now, UpdatedAt = now });
            await ctx.SaveChangesAsync();
        }

        // Now test ExecuteUpdate directly
        await using (var ctx = new ApplicationDbContext(DbOpts))
        {
            var before = await ctx.GuzergahSeferleri.IgnoreQueryFilters()
                .CountAsync(s => s.GuzergahId == 45 && !s.IsDeleted);
            _output.WriteLine($"Before ExecuteUpdate: Active={before}");

            var affected = await ctx.GuzergahSeferleri.IgnoreQueryFilters()
                .Where(s => s.GuzergahId == 45)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
            _output.WriteLine($"ExecuteUpdate affected: {affected}");

            var after = await ctx.GuzergahSeferleri.IgnoreQueryFilters()
                .CountAsync(s => s.GuzergahId == 45 && !s.IsDeleted);
            _output.WriteLine($"After ExecuteUpdate: Active={after}");

            after.Should().Be(0, "ExecuteUpdate tüm kayıtları soft-delete etmeli");
        }

        // Restore 2
        await using (var ctx = new ApplicationDbContext(DbOpts))
        {
            var now = DateTime.UtcNow;
            ctx.GuzergahSeferleri.Add(new KOAFiloServis.Shared.Entities.GuzergahSefer { GuzergahId = 45, FirmaId = 1, Sira = 1, CreatedAt = now, UpdatedAt = now });
            ctx.GuzergahSeferleri.Add(new KOAFiloServis.Shared.Entities.GuzergahSefer { GuzergahId = 45, FirmaId = 1, Sira = 2, CreatedAt = now, UpdatedAt = now });
            await ctx.SaveChangesAsync();
        }
    }
}

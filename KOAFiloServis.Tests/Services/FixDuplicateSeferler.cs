using FluentAssertions;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Id=45 /H.SEMT SİNCAN FATİH güzergahındaki duplicate aktif seferleri temizler.
/// Her SiraNo'dan sadece 1 tane bırakır, fazlalıkları soft-delete yapar.
/// </summary>
public class FixDuplicateSeferler
{
    private readonly ITestOutputHelper _output;
    private const string CS = "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOpts =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(CS).Options;

    public FixDuplicateSeferler(ITestOutputHelper o) => _output = o;

    [Fact]
    public async Task Fix_Guzergah45_DuplicateSeferler()
    {
        await using var ctx = new ApplicationDbContext(DbOpts);

        var all = await ctx.GuzergahSeferleri
            .IgnoreQueryFilters()
            .Where(s => s.GuzergahId == 45 && !s.IsDeleted)
            .OrderBy(s => s.Sira)
            .ThenBy(s => s.Id)
            .ToListAsync();

        _output.WriteLine($"Id=45 aktif sefer: {all.Count}");
        _output.WriteLine($"SiraNo'lar: {string.Join(",", all.Select(s => s.Sira))}");

        // Her SiraNo için sadece en düşük Id'li olanı bırak
        var keep = all.GroupBy(s => s.Sira)
            .Select(g => g.OrderBy(s => s.Id).First())
            .ToList();

        var delete = all.Except(keep).ToList();

        _output.WriteLine($"Korunacak: {keep.Count} (Id'ler: {string.Join(",", keep.Select(s => s.Id))})");
        _output.WriteLine($"Silinecek: {delete.Count} (Id'ler: {string.Join(",", delete.Select(s => s.Id))})");

        foreach (var s in delete)
        {
            s.IsDeleted = true;
            s.UpdatedAt = DateTime.UtcNow;
        }

        await ctx.SaveChangesAsync();

        // Verify
        var after = await ctx.GuzergahSeferleri
            .IgnoreQueryFilters()
            .Where(s => s.GuzergahId == 45 && !s.IsDeleted)
            .ToListAsync();

        _output.WriteLine($"Temizlik sonrası aktif: {after.Count}");
        _output.WriteLine($"SiraNo'lar: {string.Join(",", after.Select(s => s.Sira))}");

        after.Count.Should().Be(2, "Sadece 2 aktif sefer kalmalı (1 ve 2)");
    }
}

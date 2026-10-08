using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class BankAccountCreateSqliteTests
{
    [Fact]
    public async Task Create_uses_selected_firm_and_rejects_foreign_firm_without_a_partial_row()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var service = new BankaHesapService(fixture.Factory,
            new NumaraSerisiService(fixture.Factory), fixture.Guard, fixture.ActiveFirm);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(new BankaHesap
        {
            FirmaId = 2, HesapKodu = "FOREIGN", HesapAdi = "Other firm", HesapTipi = HesapTipi.Kasa
        }));

        await using (var db = fixture.Factory.CreateDbContext())
            Assert.False(await db.BankaHesaplari.IgnoreQueryFilters().AnyAsync(h => h.HesapKodu == "FOREIGN"));

        var created = await service.CreateAsync(new BankaHesap
        {
            HesapKodu = " local ", HesapAdi = " Local ", HesapTipi = HesapTipi.Kasa
        });
        Assert.Equal(1, created.FirmaId);
        Assert.Equal("LOCAL", created.HesapKodu);
        await using var verify = fixture.Factory.CreateDbContext();
        Assert.Equal(1, await verify.BankaHesaplari.IgnoreQueryFilters()
            .CountAsync(h => h.Id == created.Id && h.FirmaId == 1));
    }

    [Fact]
    public async Task Create_rechecks_current_database_permission_before_writing()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var service = new BankaHesapService(fixture.Factory,
            new NumaraSerisiService(fixture.Factory), fixture.Guard, fixture.ActiveFirm);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var role = await db.Roller.SingleAsync(r => r.RolAdi == "Admin");
            role.RolAdi = "NoLongerAdmin";
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(new BankaHesap
        {
            HesapKodu = "DENIED", HesapAdi = "Denied", HesapTipi = HesapTipi.Kasa
        }));
        await using var verify = fixture.Factory.CreateDbContext();
        Assert.False(await verify.BankaHesaplari.IgnoreQueryFilters()
            .AnyAsync(h => h.HesapKodu == "DENIED"));
    }
}

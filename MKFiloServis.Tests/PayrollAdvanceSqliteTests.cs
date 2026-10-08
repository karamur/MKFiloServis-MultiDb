using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class PayrollAdvanceSqliteTests
{
    [Fact]
    public async Task Automatic_batch_replay_and_reversal_preserve_salary_and_advance_balances()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        int advanceId;
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var salary = await db.PersonelMaaslari.SingleAsync(m => m.Id == fixture.SalaryId);
            var advance = new PersonelAvans { PersonelId = salary.SoforId, FirmaId = 1, Tutar = 100 };
            db.PersonelAvanslar.Add(advance);
            await db.SaveChangesAsync();
            advanceId = advance.Id;
        }
        var finance = new PersonelFinansService(fixture.Factory, new MuhasebeService(fixture.Factory, fixture.Guard), fixture.Guard, fixture.ActiveFirm);
        Assert.Equal(100, await finance.MaasaAcikAvansMahsupEtAsync(fixture.SalaryId));
        Assert.Equal(100, await finance.MaasaAcikAvansMahsupEtAsync(fixture.SalaryId, DateTime.Today.AddDays(1), "Retry"));
        int offsetId;
        await using (var db = fixture.Factory.CreateDbContext())
        {
            Assert.Equal(100, (await db.PersonelMaaslari.SingleAsync(m => m.Id == fixture.SalaryId)).Avans);
            Assert.Equal(100, (await db.PersonelAvanslar.SingleAsync(a => a.Id == advanceId)).MahsupEdilen);
            offsetId = (await db.PersonelAvansMahsuplar.SingleAsync()).Id;
        }
        await finance.DeleteMahsupAsync(offsetId);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            Assert.Equal(0, (await db.PersonelMaaslari.SingleAsync(m => m.Id == fixture.SalaryId)).Avans);
            Assert.Equal(0, (await db.PersonelAvanslar.SingleAsync(a => a.Id == advanceId)).MahsupEdilen);
            Assert.True((await db.PersonelAvansMahsuplar.IgnoreQueryFilters().SingleAsync()).IsDeleted);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => finance.MaasaAcikAvansMahsupEtAsync(fixture.SalaryId));
    }
}

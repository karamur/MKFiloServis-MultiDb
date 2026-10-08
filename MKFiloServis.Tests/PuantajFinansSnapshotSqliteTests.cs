using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class PuantajFinansSnapshotSqliteTests
{
    [Fact]
    public async Task Snapshot_creation_is_limited_to_selected_firm_and_is_repeat_safe()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var service = new PuantajFinansService(fixture.Factory, null!,
            NullLogger<PuantajFinansService>.Instance, fixture.ActiveFirm);

        int localPeriodId;
        int foreignPeriodId;
        await using (var db = fixture.Factory.CreateDbContext())
        {
            db.Firmalar.Add(new Firma
            {
                Id = 2, FirmaKodu = "OTHER", FirmaAdi = "Other firm", OrganizasyonId = 1
            });
            var local = new PuantajHesapDonemi
            {
                FirmaId = 1, Yil = 2026, Ay = 10, Versiyon = 1,
                Durum = PuantajHesapDurum.Aktif, OnayDurum = PuantajDonemOnayDurum.Kilitli
            };
            var foreign = new PuantajHesapDonemi
            {
                FirmaId = 2, Yil = 2026, Ay = 10, Versiyon = 1,
                Durum = PuantajHesapDurum.Aktif, OnayDurum = PuantajDonemOnayDurum.Kilitli
            };
            db.PuantajHesapDonemleri.AddRange(local, foreign);
            await db.SaveChangesAsync();
            db.PuantajKayitlar.Add(new PuantajKayit
            {
                HesapDonemiId = local.Id, Yil = 2026, Ay = 10,
                BirimGelir = 100, ToplamGelir = 100, Gun = 1
            });
            await db.SaveChangesAsync();
            localPeriodId = local.Id;
            foreignPeriodId = foreign.Id;
        }

        Assert.False(await service.FinansalKayitOlusturulabilirMiAsync(foreignPeriodId));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.FinansalKayitOlusturAsync(foreignPeriodId));
        await service.FinansalKayitOlusturAsync(localPeriodId);
        await service.FinansalKayitOlusturAsync(localPeriodId);

        await using var verify = fixture.Factory.CreateDbContext();
        var rows = await verify.PuantajFinansalKayitlar.IgnoreQueryFilters().ToListAsync();
        var row = Assert.Single(rows);
        Assert.Equal(1, row.FirmaId);
        Assert.Equal(localPeriodId, row.HesapDonemiId);
    }
}

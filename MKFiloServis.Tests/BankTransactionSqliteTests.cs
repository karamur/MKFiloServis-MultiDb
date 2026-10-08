using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class BankTransactionSqliteTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Transfer_commits_all_links_or_rolls_back_every_row_on_posting_failure(bool failPosting)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, target, _) = await SeedAsync(fixture);
        if (failPosting)
        {
            await using var db = fixture.Factory.CreateDbContext();
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_test_posting BEFORE INSERT ON MuhasebeFisleri BEGIN SELECT RAISE(ABORT, 'Injected posting failure'); END;");
        }
        var result = await service.HesaplarArasiTransferAsync(source, target, 100, new DateTime(2026, 10, 8), "Test transfer", islemKimligi: Guid.NewGuid().ToString("N"));
        Assert.True(result.Basarili == !failPosting, result.Hata);
        if (failPosting) Assert.Contains("saving the entity", result.Hata); // EF wraps the trigger failure in DbUpdateException.
        await using var verification = fixture.Factory.CreateDbContext();
        var movements = await verification.BankaKasaHareketleri.ToListAsync();
        Assert.Equal(failPosting ? 0 : 2, movements.Count);
        Assert.Equal(failPosting ? 0 : 1, await verification.MuhasebeFisleri.CountAsync());
        Assert.Equal(failPosting ? 1000 : 900, await service.GetHesapBakiyeAsync(source));
        if (!failPosting)
        {
            Assert.All(movements, m => Assert.NotNull(m.MuhasebeFisId));
            Assert.Equal(movements[0].MuhasebeFisId, movements[1].MuhasebeFisId);
            Assert.Equal(movements[1].Id, movements[0].MahsupHareketId);
            Assert.Equal(movements[0].Id, movements[1].MahsupHareketId);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cari_posting_and_new_account_roll_back_together(bool failPosting)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, _, cari) = await SeedAsync(fixture);
        if (failPosting)
        {
            await using var db = fixture.Factory.CreateDbContext();
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_test_posting BEFORE INSERT ON MuhasebeFisleri BEGIN SELECT RAISE(ABORT, 'Injected posting failure'); END;");
        }
        var result = await service.CariMahsupAsync(cari, source, 100, new DateTime(2026, 10, 8), "Test cari", true, islemKimligi: Guid.NewGuid().ToString("N"));
        Assert.True(result.Basarili == !failPosting, result.Hata);
        if (failPosting) Assert.Contains("saving the entity", result.Hata); // EF wraps the trigger failure in DbUpdateException.
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Equal(failPosting ? 0 : 1, await verification.BankaKasaHareketleri.CountAsync());
        Assert.Equal(failPosting ? 0 : 1, await verification.MuhasebeFisleri.CountAsync());
        Assert.Equal(failPosting ? 4 : 5, await verification.MuhasebeHesaplari.CountAsync());
        Assert.Equal(!failPosting, (await verification.MuhasebeHesaplari.SingleAsync(h => h.HesapKodu == "120")).AltHesapVar);
    }

    [Fact]
    public async Task Currency_mismatch_or_missing_mapping_cannot_leave_movements()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, target, _) = await SeedAsync(fixture);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            (await db.BankaHesaplari.SingleAsync(h => h.Id == target)).ParaBirimi = "USD";
            await db.SaveChangesAsync();
        }
        Assert.False((await service.HesaplarArasiTransferAsync(source, target, 100, DateTime.Today, "Test", islemKimligi: Guid.NewGuid().ToString("N"))).Basarili);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var account = await db.BankaHesaplari.SingleAsync(h => h.Id == target);
            account.ParaBirimi = "TRY";
            account.VarsayilanMuhasebeKodu = "999";
            await db.SaveChangesAsync();
        }
        Assert.False((await service.HesaplarArasiTransferAsync(source, target, 100, DateTime.Today, "Test", islemKimligi: Guid.NewGuid().ToString("N"))).Basarili);
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Empty(await verification.BankaKasaHareketleri.ToListAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Cancellation_is_atomic_preserves_history_and_nets_ledger_to_zero(bool cariPosting, bool failPosting)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, target, cari) = await SeedAsync(fixture);
        var result = cariPosting
            ? await service.CariMahsupAsync(cari, source, 100, DateTime.Today, "Cancel test", true, islemKimligi: Guid.NewGuid().ToString("N"))
            : await service.HesaplarArasiTransferAsync(source, target, 100, DateTime.Today, "Cancel test", islemKimligi: Guid.NewGuid().ToString("N"));
        Assert.True(result.Basarili, result.Hata);
        if (failPosting)
        {
            await using var db = fixture.Factory.CreateDbContext();
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_test_posting BEFORE INSERT ON MuhasebeFisleri BEGIN SELECT RAISE(ABORT, 'Injected reversal failure'); END;");
            await Assert.ThrowsAsync<DbUpdateException>(() => service.MahsupIptalAsync(result.MahsupGrupId!.Value));
        }
        else
        {
            await service.MahsupIptalAsync(result.MahsupGrupId!.Value);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.MahsupIptalAsync(result.MahsupGrupId!.Value));
        }
        await using var verification = fixture.Factory.CreateDbContext();
        var history = await verification.BankaKasaHareketleri.IgnoreQueryFilters().ToListAsync();
        Assert.Equal(cariPosting ? 1 : 2, history.Count);
        Assert.All(history, h => Assert.Equal(!failPosting, h.IsDeleted));
        Assert.All(history, h => Assert.NotNull(h.MuhasebeFisId));
        Assert.Equal(failPosting ? 1 : 2, await verification.MuhasebeFisleri.CountAsync());
        Assert.Equal(failPosting ? (cariPosting ? 1100 : 900) : 1000, await service.GetHesapBakiyeAsync(source));
        var ledger = await verification.MuhasebeFisKalemleri.Include(k => k.Fis)
            .Where(k => k.Fis.Durum == FisDurum.Onaylandi).ToListAsync();
        if (!failPosting)
        {
            Assert.All(ledger.GroupBy(k => k.HesapId), g => Assert.Equal(0, g.Sum(k => k.Borc - k.Alacak)));
            var accounting = new MuhasebeService(fixture.Factory, fixture.Guard);
            foreach (var posting in await verification.MuhasebeFisleri.ToListAsync())
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => accounting.OnayGeriAlFisAsync(posting.Id));
                await Assert.ThrowsAsync<InvalidOperationException>(() => accounting.OnayliFisAsync(posting.Id));
                await Assert.ThrowsAsync<InvalidOperationException>(() => accounting.DeleteFisAsync(posting.Id));
                await Assert.ThrowsAsync<InvalidOperationException>(() => accounting.UpdateFisAsync(posting));
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_rejects_incomplete_transfer_or_amount_mismatch_without_mutation(bool amountMismatch)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, target, _) = await SeedAsync(fixture);
        var result = await service.HesaplarArasiTransferAsync(source, target, 100, DateTime.Today, "Broken link test", islemKimligi: Guid.NewGuid().ToString("N"));
        Assert.True(result.Basarili, result.Hata);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var movement = await db.BankaKasaHareketleri.FirstAsync();
            if (amountMismatch)
            {
                foreach (var item in await db.BankaKasaHareketleri.ToListAsync()) item.Tutar = 200;
            }
            else movement.MahsupHareketId = null;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MahsupIptalAsync(result.MahsupGrupId!.Value));
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Equal(2, await verification.BankaKasaHareketleri.CountAsync());
        Assert.Equal(1, await verification.MuhasebeFisleri.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Personnel_repayment_closes_all_selected_expenses_or_rolls_back(bool failClosing)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, _, _) = await SeedAsync(fixture);
        var (person, ids) = await SeedPersonnelExpensesAsync(fixture, source);
        if (failClosing)
        {
            await using var db = fixture.Factory.CreateDbContext();
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_expense_close BEFORE UPDATE ON BankaKasaHareketleri WHEN NEW.PersoneleOdendi = 1 BEGIN SELECT RAISE(ABORT, 'Injected closing failure'); END;");
        }
        var result = await service.PersonelGeriOdemeYapAsync(person, ids, source, DateTime.Today);
        Assert.Equal(!failClosing, result.Basarili);
        await using var verification = fixture.Factory.CreateDbContext();
        var expenses = await verification.BankaKasaHareketleri.Where(h => ids.Contains(h.Id)).ToListAsync();
        Assert.All(expenses, h => Assert.Equal(!failClosing, h.PersoneleOdendi));
        Assert.Equal(failClosing ? 2 : 3, await verification.BankaKasaHareketleri.CountAsync());
        Assert.Equal(failClosing ? 850 : 700, await service.GetHesapBakiyeAsync(source));
        if (!failClosing)
        {
            Assert.Equal(150, result.ToplamTutar);
            Assert.Equal(2, result.KapatilanKayitSayisi);
            Assert.All(expenses, h => Assert.Equal(result.OdemeHareketi!.Id, h.PersonelGeriOdemeHareketId));
            Assert.False((await service.PersonelGeriOdemeYapAsync(person, ids, source, DateTime.Today)).Basarili);
            Assert.Equal(3, await verification.BankaKasaHareketleri.CountAsync());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Personnel_shared_payment_cancellation_reopens_whole_group_atomically(bool failReopening)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, _, _) = await SeedAsync(fixture);
        var (person, ids) = await SeedPersonnelExpensesAsync(fixture, source);
        var paid = await service.PersonelGeriOdemeYapAsync(person, ids, source, DateTime.Today);
        Assert.True(paid.Basarili, paid.Hata);
        if (failReopening)
        {
            await using var db = fixture.Factory.CreateDbContext();
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_expense_reopen BEFORE UPDATE ON BankaKasaHareketleri WHEN OLD.PersoneleOdendi = 1 AND NEW.PersoneleOdendi = 0 BEGIN SELECT RAISE(ABORT, 'Injected reopening failure'); END;");
            await Assert.ThrowsAsync<DbUpdateException>(() => service.PersonelGeriOdemeIptalAsync(ids[0]));
        }
        else
        {
            await service.PersonelGeriOdemeIptalAsync(ids[0]);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.PersonelGeriOdemeIptalAsync(ids[1]));
        }
        await using var verification = fixture.Factory.CreateDbContext();
        var expenses = await verification.BankaKasaHareketleri.Where(h => ids.Contains(h.Id)).ToListAsync();
        Assert.All(expenses, h => Assert.Equal(failReopening, h.PersoneleOdendi));
        Assert.All(expenses, h => Assert.Equal(failReopening ? paid.OdemeHareketi!.Id : (int?)null, h.PersonelGeriOdemeHareketId));
        var payment = await verification.BankaKasaHareketleri.IgnoreQueryFilters().SingleAsync(h => h.Id == paid.OdemeHareketi!.Id);
        Assert.Equal(!failReopening, payment.IsDeleted);
        Assert.Equal(150, payment.Tutar);
        Assert.Equal(failReopening ? 700 : 850, await service.GetHesapBakiyeAsync(source));
        Assert.Equal(3, await verification.BankaKasaHareketleri.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Personnel_repayment_does_not_pay_valid_subset_of_invalid_selection()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, _, _) = await SeedAsync(fixture);
        var (person, ids) = await SeedPersonnelExpensesAsync(fixture, source);
        Assert.False((await service.PersonelGeriOdemeYapAsync(person, [ids[0], int.MaxValue], source, DateTime.Today)).Basarili);
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Equal(2, await verification.BankaKasaHareketleri.CountAsync());
        Assert.False(await verification.BankaKasaHareketleri.AnyAsync(h => h.PersoneleOdendi));
    }

    [Fact]
    public async Task Personnel_repayment_cannot_cancel_posted_payment()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, _, _) = await SeedAsync(fixture);
        var (person, ids) = await SeedPersonnelExpensesAsync(fixture, source);
        var paid = await service.PersonelGeriOdemeYapAsync(person, ids, source, DateTime.Today);
        Assert.True(paid.Basarili, paid.Hata);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            db.MuhasebeFisleri.Add(new MuhasebeFis { FisNo = "TEST-PERSONNEL", FisTarihi = DateTime.UtcNow,
                KaynakTip = "BankaKasaHareket", KaynakId = paid.OdemeHareketi!.Id, Durum = FisDurum.Onaylandi });
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PersonelGeriOdemeIptalAsync(ids[0]));
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Equal(2, await verification.BankaKasaHareketleri.CountAsync(h => h.PersoneleOdendi));
        Assert.Equal(700, await service.GetHesapBakiyeAsync(source));
    }

    [Fact]
    public async Task Personnel_cash_marker_can_be_reversed_without_creating_bank_payment()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, _, _) = await SeedAsync(fixture);
        var (person, ids) = await SeedPersonnelExpensesAsync(fixture, source);
        var result = await service.PersonelGeriOdemeYapAsync(person, ids, null, DateTime.Today);
        Assert.True(result.Basarili, result.Hata);
        Assert.Null(result.OdemeHareketi);
        await service.PersonelGeriOdemeIptalAsync(ids[0]);
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.False((await verification.BankaKasaHareketleri.SingleAsync(h => h.Id == ids[0])).PersoneleOdendi);
        Assert.True((await verification.BankaKasaHareketleri.SingleAsync(h => h.Id == ids[1])).PersoneleOdendi);
        Assert.Equal(2, await verification.BankaKasaHareketleri.CountAsync());
        Assert.Equal(850, await service.GetHesapBakiyeAsync(source));
    }

    [Fact]
    public async Task Personnel_repayment_rejects_currency_mismatch_before_writing()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, target, _) = await SeedAsync(fixture);
        var (person, ids) = await SeedPersonnelExpensesAsync(fixture, source);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            (await db.BankaHesaplari.SingleAsync(h => h.Id == target)).ParaBirimi = "USD";
            await db.SaveChangesAsync();
        }
        Assert.False((await service.PersonelGeriOdemeYapAsync(person, ids, target, DateTime.Today)).Basarili);
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Equal(2, await verification.BankaKasaHareketleri.CountAsync());
        Assert.False(await verification.BankaKasaHareketleri.AnyAsync(h => h.PersoneleOdendi));
    }

    [Fact]
    public async Task Personnel_repayment_cancellation_rejects_inconsistent_group_total()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, _, _) = await SeedAsync(fixture);
        var (person, ids) = await SeedPersonnelExpensesAsync(fixture, source);
        var paid = await service.PersonelGeriOdemeYapAsync(person, ids, source, DateTime.Today);
        Assert.True(paid.Basarili, paid.Hata);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            (await db.BankaKasaHareketleri.SingleAsync(h => h.Id == ids[0])).Tutar++;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PersonelGeriOdemeIptalAsync(ids[0]));
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Equal(2, await verification.BankaKasaHareketleri.CountAsync(h => h.PersoneleOdendi));
        Assert.False((await verification.BankaKasaHareketleri.SingleAsync(h => h.Id == paid.OdemeHareketi!.Id)).IsDeleted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stable_bank_request_key_returns_existing_operation_and_rejects_changed_payload(bool cariPosting)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, target, cari) = await SeedAsync(fixture);
        var key = Guid.NewGuid().ToString("N");
        var date = new DateTime(2026, 10, 8);
        async Task<MKFiloServis.Web.Services.Interfaces.MahsupSonuc> Send(decimal amount, string operationKey)
            => cariPosting
                ? await service.CariMahsupAsync(cari, source, amount, date, "Stable key", true, islemKimligi: operationKey)
                : await service.HesaplarArasiTransferAsync(source, target, amount, date, "Stable key", islemKimligi: operationKey);
        var first = await Send(100, key);
        Assert.True(first.Basarili, first.Hata);
        // Different GUID spelling resolves to the same persisted request key.
        var repeat = await Send(100.00m, Guid.Parse(key).ToString("D").ToUpperInvariant());
        Assert.True(repeat.Basarili, repeat.Hata);
        Assert.Equal(first.MahsupGrupId, repeat.MahsupGrupId);
        Assert.Equal(first.KaynakHareket!.Id, repeat.KaynakHareket!.Id);
        Assert.False((await Send(101, key)).Basarili);
        var otherOperation = cariPosting
            ? await service.HesaplarArasiTransferAsync(source, target, 100, date, "Stable key", islemKimligi: key)
            : await service.CariMahsupAsync(cari, source, 100, date, "Stable key", true, islemKimligi: key);
        Assert.False(otherOperation.Basarili);
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Equal(cariPosting ? 1 : 2, await verification.BankaKasaHareketleri.CountAsync());
        Assert.Equal(1, await verification.MuhasebeFisleri.CountAsync());
        Assert.Equal(1, await verification.BankaKasaHareketleri.CountAsync(h => h.IslemKimligi == key));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancelled_bank_request_key_remains_consumed(bool cariPosting)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, target, cari) = await SeedAsync(fixture);
        var key = Guid.NewGuid().ToString("N");
        var date = new DateTime(2026, 10, 8);
        async Task<MKFiloServis.Web.Services.Interfaces.MahsupSonuc> Send()
            => cariPosting
                ? await service.CariMahsupAsync(cari, source, 100, date, "Cancelled key", true, islemKimligi: key)
                : await service.HesaplarArasiTransferAsync(source, target, 100, date, "Cancelled key", islemKimligi: key);
        var first = await Send();
        Assert.True(first.Basarili, first.Hata);
        await service.MahsupIptalAsync(first.MahsupGrupId!.Value);
        Assert.False((await Send()).Basarili);
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Equal(2, await verification.MuhasebeFisleri.CountAsync());
        Assert.False(await verification.BankaKasaHareketleri.AnyAsync());
        Assert.Equal(1, await verification.BankaKasaHareketleri.IgnoreQueryFilters().CountAsync(h => h.IslemKimligi == key));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_bank_request_can_retry_same_key_after_transaction_rollback(bool cariPosting)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, target, cari) = await SeedAsync(fixture);
        var key = Guid.NewGuid().ToString("N");
        var date = new DateTime(2026, 10, 8);
        async Task<MKFiloServis.Web.Services.Interfaces.MahsupSonuc> Send()
            => cariPosting
                ? await service.CariMahsupAsync(cari, source, 100, date, "Retry key", true, islemKimligi: key)
                : await service.HesaplarArasiTransferAsync(source, target, 100, date, "Retry key", islemKimligi: key);
        await using (var db = fixture.Factory.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_key_posting BEFORE INSERT ON MuhasebeFisleri BEGIN SELECT RAISE(ABORT, 'Injected keyed failure'); END;");
        Assert.False((await Send()).Basarili);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            Assert.False(await db.BankaKasaHareketleri.IgnoreQueryFilters().AnyAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_key_posting;");
        }
        var retry = await Send();
        Assert.True(retry.Basarili, retry.Hata);
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Equal(1, await verification.MuhasebeFisleri.CountAsync());
        Assert.Equal(1, await verification.BankaKasaHareketleri.CountAsync(h => h.IslemKimligi == key));
    }

    [Fact]
    public async Task Bank_operations_require_a_valid_nonempty_request_key()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, source, target, cari) = await SeedAsync(fixture);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.HesaplarArasiTransferAsync(source, target, 100, DateTime.Today, "Missing key"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CariMahsupAsync(cari, source, 100, DateTime.Today, "Invalid key", true, islemKimligi: "invalid"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CariMahsupAsync(cari, source, 100, DateTime.Today, "Empty key", true, islemKimligi: Guid.Empty.ToString("N")));
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.False(await verification.BankaKasaHareketleri.AnyAsync());
    }

    [Theory]
    [InlineData("person", false)]
    [InlineData("person", true)]
    [InlineData("counterpart", false)]
    [InlineData("counterpart", true)]
    [InlineData("repayment", false)]
    [InlineData("repayment", true)]
    public async Task Bank_auxiliary_links_cannot_reference_another_firm(string relation, bool asynchronous)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (_, source, _, _) = await SeedAsync(fixture);
        await using var db = fixture.Factory.CreateDbContext();
        var otherFirm = new Firma { FirmaKodu = "OTHER", FirmaAdi = "Other", OrganizasyonId = 1 };
        db.Firmalar.Add(otherFirm);
        await db.SaveChangesAsync();
        var person = new Sofor { FirmaId = otherFirm.Id, SoforKodu = "OTHER", Ad = "Other", Soyad = "Person" };
        var account = new BankaHesap { FirmaId = otherFirm.Id, HesapKodu = "OTHER", HesapAdi = "Other" };
        db.Soforler.Add(person);
        db.BankaHesaplari.Add(account);
        await db.SaveChangesAsync();
        var otherMovement = new BankaKasaHareket { FirmaId = otherFirm.Id, BankaHesapId = account.Id,
            IslemNo = "OTHER", IslemTarihi = DateTime.UtcNow, Tutar = 10, HareketTipi = HareketTipi.Cikis };
        db.BankaKasaHareketleri.Add(otherMovement);
        await db.SaveChangesAsync();
        var movement = new BankaKasaHareket { FirmaId = 1, BankaHesapId = source,
            IslemNo = "CROSS", IslemTarihi = DateTime.UtcNow, Tutar = 10, HareketTipi = HareketTipi.Cikis };
        if (relation == "person") movement.PersonelCebindenId = person.Id;
        else if (relation == "counterpart") movement.MahsupHareketId = otherMovement.Id;
        else movement.PersonelGeriOdemeHareketId = otherMovement.Id;
        db.BankaKasaHareketleri.Add(movement);
        if (asynchronous) await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        else Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        await using var verification = fixture.Factory.CreateDbContext();
        Assert.False(await verification.BankaKasaHareketleri.AnyAsync(h => h.IslemNo == "CROSS"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Payment_match_only_links_invoice_and_bank_movement_with_same_firm(bool asynchronous, bool crossFirm)
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (_, sourceAccountId, _, _) = await SeedAsync(fixture);
        int invoiceId;
        int homeMovementId;
        int otherMovementId;
        await using (var seed = fixture.Factory.CreateDbContext())
        {
            var otherFirm = new Firma { Id = 2, FirmaKodu = "OTHER", FirmaAdi = "Other", OrganizasyonId = 1 };
            var otherCari = new Cari { Firma = otherFirm, FirmaId = 2, CariKodu = "OTHER", Unvan = "Other Cari" };
            var otherAccount = new BankaHesap { Firma = otherFirm, FirmaId = 2, HesapKodu = "OTHER", HesapAdi = "Other" };
            var invoice = new Fatura { Firma = otherFirm, FirmaId = 2, Cari = otherCari, CariId = otherCari.Id,
                FaturaNo = "OTHER-INV", FaturaTarihi = DateTime.Today, GenelToplam = 100 };
            var otherMovement = new BankaKasaHareket { Firma = otherFirm, FirmaId = otherFirm.Id, BankaHesap = otherAccount,
                BankaHesapId = otherAccount.Id, IslemNo = "OTHER-MOVE", IslemTarihi = DateTime.Today,
                Tutar = 100, HareketTipi = HareketTipi.Giris };
            var homeMovement = new BankaKasaHareket { FirmaId = 1, BankaHesapId = sourceAccountId,
                IslemNo = "HOME-MOVE", IslemTarihi = DateTime.Today, Tutar = 100, HareketTipi = HareketTipi.Giris };
            seed.AddRange(otherFirm, otherCari, otherAccount, invoice, otherMovement, homeMovement);
            await seed.SaveChangesAsync();
            invoiceId = invoice.Id;
            homeMovementId = homeMovement.Id;
            otherMovementId = otherMovement.Id;
        }

        await using var db = fixture.Factory.CreateDbContext();
        Assert.NotEqual(
            await db.Faturalar.IgnoreQueryFilters().Where(f => f.Id == invoiceId).Select(f => f.FirmaId).SingleAsync(),
            await db.BankaKasaHareketleri.IgnoreQueryFilters().Where(h => h.Id == homeMovementId).Select(h => h.FirmaId).SingleAsync());
        db.OdemeEslestirmeleri.Add(new OdemeEslestirme
        {
            FaturaId = invoiceId,
            BankaKasaHareketId = crossFirm ? homeMovementId : otherMovementId,
            EslestirmeTarihi = DateTime.Today,
            EslestirilenTutar = 100
        });

        if (crossFirm)
        {
            if (asynchronous)
                await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
            else
                Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        }
        else if (asynchronous)
            await db.SaveChangesAsync();
        else
            db.SaveChanges();

        await using var verification = fixture.Factory.CreateDbContext();
        Assert.Equal(crossFirm ? 0 : 1, await verification.OdemeEslestirmeleri.IgnoreQueryFilters().CountAsync());
    }

    private static async Task<(int person, int[] ids)> SeedPersonnelExpensesAsync(PayrollWriteSqliteTests.Fixture fixture, int account)
    {
        await using var db = fixture.Factory.CreateDbContext();
        var person = await db.Soforler.Select(p => p.Id).SingleAsync();
        var expenses = new[] { 100m, 50m }.Select((amount, index) => new BankaKasaHareket
        {
            FirmaId = 1, PersonelCebindenId = person, BankaHesapId = account,
            IslemNo = $"EXPENSE-{index}", IslemTarihi = DateTime.UtcNow, Tutar = amount,
            HareketTipi = HareketTipi.Cikis, IslemKaynak = IslemKaynak.PersonelCebinden
        }).ToArray();
        db.BankaKasaHareketleri.AddRange(expenses);
        await db.SaveChangesAsync();
        return (person, expenses.Select(h => h.Id).ToArray());
    }

    private static async Task<(BankaKasaHareketService service, int source, int target, int cari)> SeedAsync(PayrollWriteSqliteTests.Fixture fixture)
    {
        await using var db = fixture.Factory.CreateDbContext();
        // Startup schema helper owns this table; it is not an EF entity.
        await MKFiloServis.Web.Data.Migrations.SchemaSyncHelper.EnsureFisNoCountersSchemaAsync(db);
        var source = new BankaHesap { HesapKodu = "SOURCE", HesapAdi = "Source", FirmaId = 1, HesapTipi = HesapTipi.Kasa, AcilisBakiye = 1000, VarsayilanMuhasebeKodu = "100" };
        var target = new BankaHesap { HesapKodu = "TARGET", HesapAdi = "Target", FirmaId = 1, HesapTipi = HesapTipi.VadesizHesap, VarsayilanMuhasebeKodu = "102" };
        var cari = new Cari { CariKodu = "TEST", Unvan = "Test Cari", FirmaId = 1 };
        db.BankaHesaplari.AddRange(source, target);
        db.Cariler.Add(cari);
        foreach (var code in new[] { "100", "102", "120", "320" })
            db.MuhasebeHesaplari.Add(new MuhasebeHesap { HesapKodu = code, HesapAdi = code });
        await db.SaveChangesAsync();
        var numbers = new NumaraSerisiService(fixture.Factory);
        var accounts = new BankaHesapService(fixture.Factory, numbers, fixture.Guard, fixture.ActiveFirm);
        var accounting = new MuhasebeService(fixture.Factory, fixture.Guard);
        return (new BankaKasaHareketService(fixture.Factory, accounting, accounts, numbers, fixture.ActiveFirm, fixture.Guard), source.Id, target.Id, cari.Id);
    }
}

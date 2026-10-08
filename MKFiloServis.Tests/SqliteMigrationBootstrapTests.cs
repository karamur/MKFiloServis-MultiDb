using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class SqliteMigrationBootstrapTests
{
    [Fact]
    public void Legacy_baseline_never_marks_newer_schema_changes_as_applied()
    {
        var oldMigration = "20261006194000_AddUniqueDefaultInvoiceTemplateIndexes";
        var newMigration = "20261008122000_AddBankOperationKeys";

        var selected = DbInitializer.SelectSqliteLegacyMigrationsToBaseline(
            [oldMigration, newMigration],
            [oldMigration, newMigration],
            []);

        Assert.Equal([oldMigration], selected);
    }

    [Fact]
    public async Task Bank_key_schema_repair_adds_missing_columns_and_index_without_changing_rows()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE \"BankaKasaHareketleri\" (\"Id\" INTEGER PRIMARY KEY, \"Aciklama\" TEXT); INSERT INTO \"BankaKasaHareketleri\" (\"Id\", \"Aciklama\") VALUES (17, 'mevcut hareket')";
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await DbInitializer.EnsureSqliteBankOperationKeySchemaAsync(context);
        await DbInitializer.EnsureSqliteBankOperationKeySchemaAsync(context);

        await using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT \"Id\", \"Aciklama\", \"IslemKimligi\", \"IslemOzeti\" FROM \"BankaKasaHareketleri\"; SELECT COUNT(*) FROM pragma_index_list('BankaKasaHareketleri') WHERE name = 'IX_BankaKasaHareketleri_IslemKimligi' AND \"unique\" = 1";
        await using var reader = await verify.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(17, reader.GetInt32(0));
        Assert.Equal("mevcut hareket", reader.GetString(1));
        Assert.True(reader.IsDBNull(2));
        Assert.True(reader.IsDBNull(3));
        Assert.True(await reader.NextResultAsync());
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1, reader.GetInt32(0));
    }

    [Fact]
    public async Task Finance_dashboard_query_recovers_from_legacy_bank_schema_after_startup_repair()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var numbers = new NumaraSerisiService(fixture.Factory);
        var accountService = new BankaHesapService(fixture.Factory, numbers, fixture.Guard, fixture.ActiveFirm);
        var accountingService = new MuhasebeService(fixture.Factory, fixture.Guard);
        var dashboardService = new BankaKasaHareketService(
            fixture.Factory, accountingService, accountService, numbers, fixture.ActiveFirm, fixture.Guard);

        await using (var legacy = fixture.Factory.CreateDbContext())
        {
            await legacy.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS \"IX_BankaKasaHareketleri_IslemKimligi\"");
            await legacy.Database.ExecuteSqlRawAsync("ALTER TABLE \"BankaKasaHareketleri\" DROP COLUMN \"IslemKimligi\"");
            await legacy.Database.ExecuteSqlRawAsync("ALTER TABLE \"BankaKasaHareketleri\" DROP COLUMN \"IslemOzeti\"");
        }

        await Assert.ThrowsAsync<SqliteException>(() => dashboardService.GetRecentAsync(5));

        await using (var startup = fixture.Factory.CreateDbContext())
            await DbInitializer.EnsureSqliteBankOperationKeySchemaAsync(startup);

        var recent = await dashboardService.GetRecentAsync(5);
        Assert.Empty(recent);
    }
}

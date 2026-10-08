using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Data.Migrations;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class FinancePersistenceSqliteTests
{
    [Fact]
    public async Task Counter_uses_current_transaction_and_rollback_restores_sequence()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE "FisNoCounters" (
                "Prefix" TEXT NOT NULL, "FirmaId" INTEGER NOT NULL,
                "YilAy" TEXT NOT NULL, "SonNo" INTEGER NOT NULL,
                PRIMARY KEY ("Prefix", "FirmaId", "YilAy"));
            """);
        Assert.Equal(1, await MuhasebeService.NextFisNoCounterAsync(db, "HRK", "202610", 1));
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            Assert.Equal(2, await MuhasebeService.NextFisNoCounterAsync(db, "HRK", "202610", 1));
            await transaction.RollbackAsync();
        }
        Assert.Equal(2, await MuhasebeService.NextFisNoCounterAsync(db, "HRK", "202610", 1));
        Assert.Equal(1, await MuhasebeService.NextFisNoCounterAsync(db, "HRK", "202610", 2));
        Assert.Equal(1, await MuhasebeService.NextFisNoCounterAsync(db, "MH", "202610", 1));
        Assert.Equal(1, await MuhasebeService.NextFisNoCounterAsync(db, "HRK", "202611", 1));
    }

    [Theory]
    [InlineData("PersonelAvansMahsuplar")]
    [InlineData("PersonelBorcOdemeler")]
    [InlineData("PersonelAvanslar")]
    [InlineData("PersonelBorclar")]
    [InlineData("BankaKasaHareketleri")]
    public async Task Operation_key_migration_preserves_legacy_nulls_and_consumed_keys(string table)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        foreach (var name in new[] { "PersonelAvansMahsuplar", "PersonelBorcOdemeler", "PersonelAvanslar", "PersonelBorclar", "BankaKasaHareketleri" })
            Execute(connection, $"CREATE TABLE \"{name}\" (\"Id\" INTEGER PRIMARY KEY, \"IsDeleted\" INTEGER NOT NULL DEFAULT 0)");
        Execute(connection, $"INSERT INTO \"{table}\" (\"Id\") VALUES (1), (2)");
        var generator = db.GetService<IMigrationsSqlGenerator>();
        foreach (Migration migration in new Migration[] { new AddPersonnelPaymentOperationKeys(), new AddPersonnelCreationOperationKeys(), new AddBankOperationKeys() })
        {
            migration.ActiveProvider = db.Database.ProviderName!;
            foreach (var command in generator.Generate(migration.UpOperations))
                Execute(connection, command.CommandText);
        }
        Execute(connection, $"INSERT INTO \"{table}\" (\"Id\", \"IslemKimligi\") VALUES (3, '00000000000000000000000000000001')");
        Execute(connection, $"UPDATE \"{table}\" SET \"IsDeleted\" = 1 WHERE \"Id\" = 3");
        Assert.Throws<SqliteException>(() => Execute(connection,
            $"INSERT INTO \"{table}\" (\"Id\", \"IslemKimligi\") VALUES (4, '00000000000000000000000000000001')"));
        using var count = connection.CreateCommand();
        count.CommandText = $"SELECT COUNT(*) FROM \"{table}\" WHERE \"IslemKimligi\" IS NULL";
        Assert.Equal(2L, count.ExecuteScalar());
    }

    private static ApplicationDbContext CreateContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

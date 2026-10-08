using System.Reflection;
using Microsoft.Data.Sqlite;
using MKFiloServis.Web.Data.Migrations;

namespace MKFiloServis.Tests;

public sealed class BankMovementTenantMigrationTests
{
    [Fact]
    public void Migration_preflight_rejects_existing_cross_firm_movement()
    {
        using var db = CreateDatabase();
        Execute(db, "INSERT INTO \"BankaKasaHareketleri\" VALUES (10, 1, 2, NULL, NULL)");

        Assert.Throws<SqliteException>(() => Execute(db, MigrationSql("SqlitePreflight")));
    }

    [Fact]
    public void Same_firm_movement_remains_writable()
    {
        using var db = CreateProtectedDatabase();

        Execute(db, "INSERT INTO \"BankaKasaHareketleri\" VALUES (10, 1, 1, 1, 1)");

        using var command = db.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"BankaKasaHareketleri\"";
        Assert.Equal(1L, (long)command.ExecuteScalar()!);
    }

    [Theory]
    [InlineData("INSERT INTO \"BankaKasaHareketleri\" VALUES (11, 1, 2, NULL, NULL)")]
    [InlineData("INSERT INTO \"BankaKasaHareketleri\" VALUES (11, 1, 1, 2, NULL)")]
    [InlineData("INSERT INTO \"BankaKasaHareketleri\" VALUES (11, 1, 1, NULL, 2)")]
    [InlineData("UPDATE \"BankaKasaHareketleri\" SET \"BankaHesapId\" = 2 WHERE \"Id\" = 10")]
    [InlineData("UPDATE \"BankaKasaHareketleri\" SET \"FirmaId\" = 2, \"BankaHesapId\" = 2, \"CariId\" = 2 WHERE \"Id\" = 10")]
    [InlineData("UPDATE \"BankaHesaplari\" SET \"FirmaId\" = 2 WHERE \"Id\" = 1")]
    [InlineData("UPDATE \"Cariler\" SET \"FirmaId\" = 2 WHERE \"Id\" = 1")]
    public void Cross_firm_links_and_parent_mutations_are_rejected(string sql)
    {
        using var db = CreateProtectedDatabase();
        Execute(db, "INSERT INTO \"BankaKasaHareketleri\" VALUES (10, 1, 1, 1, NULL)");

        Assert.Throws<SqliteException>(() => Execute(db, sql));
    }

    private static SqliteConnection CreateProtectedDatabase()
    {
        var db = CreateDatabase();
        Execute(db, MigrationSql("SqlitePreflight"));
        foreach (var sqlField in new[] { "SqliteChildInsert", "SqliteChildUpdate", "SqliteAccountUpdate", "SqliteCariUpdate" })
            Execute(db, MigrationSql(sqlField));
        return db;
    }

    private static SqliteConnection CreateDatabase()
    {
        var db = new SqliteConnection("Data Source=:memory:");
        db.Open();
        Execute(db, """
            CREATE TABLE "BankaHesaplari" ("Id" INTEGER PRIMARY KEY, "FirmaId" INTEGER NOT NULL);
            CREATE TABLE "Cariler" ("Id" INTEGER PRIMARY KEY, "FirmaId" INTEGER NOT NULL);
            CREATE TABLE "BankaKasaHareketleri" (
                "Id" INTEGER PRIMARY KEY, "FirmaId" INTEGER NOT NULL,
                "BankaHesapId" INTEGER NOT NULL, "CariId" INTEGER,
                "PersonelOdemeHesapId" INTEGER);
            INSERT INTO "BankaHesaplari" VALUES (1, 1), (2, 2);
            INSERT INTO "Cariler" VALUES (1, 1), (2, 2);
            """);
        return db;
    }

    private static string MigrationSql(string fieldName) =>
        (string?)typeof(GuardBankMovementTenantLinks)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)?
            .GetRawConstantValue()
        ?? throw new InvalidOperationException($"Migration SQL bulunamadı: {fieldName}");

    private static void Execute(SqliteConnection db, string sql)
    {
        using var command = db.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

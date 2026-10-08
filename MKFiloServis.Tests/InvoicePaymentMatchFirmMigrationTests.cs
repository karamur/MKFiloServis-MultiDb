using System.Reflection;
using Microsoft.Data.Sqlite;
using MKFiloServis.Web.Data.Migrations;

namespace MKFiloServis.Tests;

public sealed class InvoicePaymentMatchFirmMigrationTests
{
    [Fact]
    public void Preflight_rejects_existing_cross_firm_matches()
    {
        using var db = CreateDatabase();
        Execute(db, "INSERT INTO \"OdemeEslestirmeleri\" VALUES (10, 1, 2)");

        Assert.Throws<SqliteException>(() => Execute(db, MigrationSql("SqlitePreflight")));
    }

    [Fact]
    public void Same_firm_match_can_be_created_and_keeps_its_link()
    {
        using var db = CreateProtectedDatabase();
        Execute(db, "INSERT INTO \"OdemeEslestirmeleri\" VALUES (10, 1, 1)");
        Execute(db, "UPDATE \"OdemeEslestirmeleri\" SET \"FaturaId\" = 2 WHERE \"Id\" = 10");

        using var command = db.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"OdemeEslestirmeleri\" WHERE \"FaturaId\" = 2";
        Assert.Equal(1L, (long)command.ExecuteScalar()!);
    }

    [Theory]
    [InlineData("INSERT INTO \"OdemeEslestirmeleri\" VALUES (11, 1, 2)")]
    [InlineData("UPDATE \"OdemeEslestirmeleri\" SET \"BankaKasaHareketId\" = 2 WHERE \"Id\" = 10")]
    [InlineData("UPDATE \"Faturalar\" SET \"FirmaId\" = 2 WHERE \"Id\" = 1")]
    [InlineData("UPDATE \"BankaKasaHareketleri\" SET \"FirmaId\" = 2 WHERE \"Id\" = 1")]
    public void Cross_firm_match_and_endpoint_mutations_are_rejected(string sql)
    {
        using var db = CreateProtectedDatabase();
        Execute(db, "INSERT INTO \"OdemeEslestirmeleri\" VALUES (10, 1, 1)");

        Assert.Throws<SqliteException>(() => Execute(db, sql));
    }

    private static SqliteConnection CreateProtectedDatabase()
    {
        var db = CreateDatabase();
        Execute(db, MigrationSql("SqlitePreflight"));
        Execute(db, MigrationSql("SqliteMatchGuards"));
        Execute(db, MigrationSql("SqliteEndpointGuards"));
        return db;
    }

    private static SqliteConnection CreateDatabase()
    {
        var db = new SqliteConnection("Data Source=:memory:");
        db.Open();
        Execute(db, """
            CREATE TABLE "Faturalar" ("Id" INTEGER PRIMARY KEY, "FirmaId" INTEGER);
            CREATE TABLE "BankaKasaHareketleri" ("Id" INTEGER PRIMARY KEY, "FirmaId" INTEGER);
            CREATE TABLE "OdemeEslestirmeleri" (
                "Id" INTEGER PRIMARY KEY, "FaturaId" INTEGER NOT NULL,
                "BankaKasaHareketId" INTEGER NOT NULL);
            INSERT INTO "Faturalar" VALUES (1, 1), (2, 1);
            INSERT INTO "BankaKasaHareketleri" VALUES (1, 1), (2, 2);
            """);
        return db;
    }

    private static string MigrationSql(string fieldName) =>
        (string?)typeof(GuardInvoicePaymentMatchFirm)
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

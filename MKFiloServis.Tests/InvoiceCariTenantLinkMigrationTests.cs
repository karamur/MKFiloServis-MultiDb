using System.Reflection;
using Microsoft.Data.Sqlite;
using MKFiloServis.Web.Data.Migrations;

namespace MKFiloServis.Tests;

public sealed class InvoiceCariTenantLinkMigrationTests
{
    [Fact]
    public void Preflight_rejects_existing_cross_firm_invoices()
    {
        using var db = CreateDatabase();
        Execute(db, "INSERT INTO \"Faturalar\" VALUES (10, 1, 2, 0)");

        Assert.Throws<SqliteException>(() => Execute(db, MigrationSql("SqlitePreflight")));
    }

    [Fact]
    public void Same_firm_invoice_can_be_created_and_updated()
    {
        using var db = CreateProtectedDatabase();
        Execute(db, "INSERT INTO \"Faturalar\" VALUES (10, 1, 1, 0)");
        Execute(db, "UPDATE \"Faturalar\" SET \"CariId\" = 3 WHERE \"Id\" = 10");

        using var command = db.CreateCommand();
        command.CommandText = "SELECT \"CariId\" FROM \"Faturalar\" WHERE \"Id\" = 10";
        Assert.Equal(3L, (long)command.ExecuteScalar()!);
    }

    [Theory]
    [InlineData("INSERT INTO \"Faturalar\" VALUES (10, 1, 2, 0)")]
    [InlineData("UPDATE \"Faturalar\" SET \"FirmaId\" = 2 WHERE \"Id\" = 1")]
    [InlineData("UPDATE \"Cariler\" SET \"FirmaId\" = 2 WHERE \"Id\" = 1")]
    public void Cross_firm_invoice_and_endpoint_mutations_are_rejected(string sql)
    {
        using var db = CreateProtectedDatabase();
        Assert.Throws<SqliteException>(() => Execute(db, sql));
    }

    private static SqliteConnection CreateProtectedDatabase()
    {
        var db = CreateDatabase();
        Execute(db, MigrationSql("SqlitePreflight"));
        Execute(db, MigrationSql("SqliteInvoiceInsert"));
        Execute(db, MigrationSql("SqliteInvoiceUpdate"));
        Execute(db, MigrationSql("SqliteCariUpdate"));
        return db;
    }

    private static SqliteConnection CreateDatabase()
    {
        var db = new SqliteConnection("Data Source=:memory:");
        db.Open();
        Execute(db, """
            CREATE TABLE "Cariler" ("Id" INTEGER PRIMARY KEY, "FirmaId" INTEGER);
            CREATE TABLE "Faturalar" (
                "Id" INTEGER PRIMARY KEY, "CariId" INTEGER NOT NULL,
                "FirmaId" INTEGER, "IsDeleted" INTEGER NOT NULL);
            INSERT INTO "Cariler" VALUES (1, 1), (2, 2), (3, 1);
            INSERT INTO "Faturalar" VALUES (1, 1, 1, 0);
            """);
        return db;
    }

    private static string MigrationSql(string fieldName) =>
        (string?)typeof(GuardInvoiceCariTenantLink)
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

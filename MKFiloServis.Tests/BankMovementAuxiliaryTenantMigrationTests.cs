using System.Reflection;
using Microsoft.Data.Sqlite;
using MKFiloServis.Web.Data.Migrations;

namespace MKFiloServis.Tests;

public sealed class BankMovementAuxiliaryTenantMigrationTests
{
    [Fact]
    public void PostgreSql_migration_locks_tables_before_preflight_and_trigger_creation()
    {
        var sql = PostgresMigrationSql();
        var lockIndex = sql.IndexOf("LOCK TABLE", StringComparison.Ordinal);
        var preflightIndex = sql.IndexOf("DO $a15_aux_preflight$", StringComparison.Ordinal);
        var triggerIndex = sql.IndexOf("CREATE TRIGGER", StringComparison.Ordinal);

        Assert.True(lockIndex >= 0 && lockIndex < preflightIndex && preflightIndex < triggerIndex);
        Assert.Contains("IN SHARE ROW EXCLUSIVE MODE", sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("PersonelCebindenId", "Soforler")]
    [InlineData("AracId", "Araclar")]
    [InlineData("AracMasrafId", "AracMasraflari")]
    [InlineData("MahsupHareketId", "BankaKasaHareketleri")]
    [InlineData("PersonelGeriOdemeHareketId", "BankaKasaHareketleri")]
    public void Preflight_rejects_existing_cross_firm_auxiliary_links(string property, string targetTable)
    {
        using var db = CreateDatabase();
        if (targetTable == "BankaKasaHareketleri")
            Execute(db, "INSERT INTO \"BankaKasaHareketleri\" (\"Id\", \"FirmaId\") VALUES (2, 2)");
        Execute(db, $"INSERT INTO \"BankaKasaHareketleri\" (\"Id\", \"FirmaId\", \"{property}\") VALUES (1, 1, 2)");

        Assert.Throws<SqliteException>(() => Execute(db, MigrationSql("SqlitePreflight")));
    }

    [Fact]
    public void Same_firm_auxiliary_links_remain_writable()
    {
        using var db = CreateProtectedDatabase();
        Execute(db, "INSERT INTO \"BankaKasaHareketleri\" (\"Id\", \"FirmaId\") VALUES (9, 1)");
        Execute(db, """
            INSERT INTO "BankaKasaHareketleri"
                ("Id", "FirmaId", "PersonelCebindenId", "AracId", "AracMasrafId", "MahsupHareketId", "PersonelGeriOdemeHareketId")
            VALUES (10, 1, 1, 1, 1, 9, 9);
            """);

        using var command = db.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"BankaKasaHareketleri\" WHERE \"Id\" = 10";
        Assert.Equal(1L, (long)command.ExecuteScalar()!);
    }

    [Theory]
    [InlineData("PersonelCebindenId", 2)]
    [InlineData("AracId", 2)]
    [InlineData("AracMasrafId", 2)]
    public void Cross_firm_tenant_links_are_rejected_on_insert_and_update(string property, int foreignFirmTargetId)
    {
        using var db = CreateProtectedDatabase();

        Assert.Throws<SqliteException>(() =>
            Execute(db, $"INSERT INTO \"BankaKasaHareketleri\" (\"Id\", \"FirmaId\", \"{property}\") VALUES (10, 1, {foreignFirmTargetId})"));

        Execute(db, "INSERT INTO \"BankaKasaHareketleri\" (\"Id\", \"FirmaId\") VALUES (11, 1)");
        Assert.Throws<SqliteException>(() =>
            Execute(db, $"UPDATE \"BankaKasaHareketleri\" SET \"{property}\" = {foreignFirmTargetId} WHERE \"Id\" = 11"));
    }

    [Theory]
    [InlineData("MahsupHareketId")]
    [InlineData("PersonelGeriOdemeHareketId")]
    public void Cross_firm_movement_links_are_rejected_on_insert_and_update(string property)
    {
        using var db = CreateProtectedDatabase();
        Execute(db, "INSERT INTO \"BankaKasaHareketleri\" (\"Id\", \"FirmaId\") VALUES (2, 2), (3, 1)");

        Assert.Throws<SqliteException>(() =>
            Execute(db, $"INSERT INTO \"BankaKasaHareketleri\" (\"Id\", \"FirmaId\", \"{property}\") VALUES (10, 1, 2)"));
        Assert.Throws<SqliteException>(() =>
            Execute(db, $"UPDATE \"BankaKasaHareketleri\" SET \"{property}\" = 2 WHERE \"Id\" = 3"));
    }

    [Theory]
    [InlineData("Araclar", "AracId")]
    [InlineData("AracMasraflari", "AracMasrafId")]
    [InlineData("Soforler", "PersonelCebindenId")]
    public void Firm_of_referenced_parent_cannot_be_changed(string table, string property)
    {
        using var db = CreateProtectedDatabase();
        Execute(db, $"INSERT INTO \"BankaKasaHareketleri\" (\"Id\", \"FirmaId\", \"{property}\") VALUES (10, 1, 1)");

        Assert.Throws<SqliteException>(() =>
            Execute(db, $"UPDATE \"{table}\" SET \"FirmaId\" = 2 WHERE \"Id\" = 1"));
    }

    private static SqliteConnection CreateProtectedDatabase()
    {
        var db = CreateDatabase();
        Execute(db, MigrationSql("SqlitePreflight"));
        foreach (var field in new[]
                 {
                     "SqliteChildInsert", "SqliteChildUpdate", "SqliteVehicleUpdate",
                     "SqliteExpenseUpdate", "SqliteDriverUpdate"
                 })
            Execute(db, MigrationSql(field));
        return db;
    }

    private static SqliteConnection CreateDatabase()
    {
        var db = new SqliteConnection("Data Source=:memory:");
        db.Open();
        Execute(db, """
            CREATE TABLE "Soforler" ("Id" INTEGER PRIMARY KEY, "FirmaId" INTEGER NOT NULL);
            CREATE TABLE "Araclar" ("Id" INTEGER PRIMARY KEY, "FirmaId" INTEGER NOT NULL);
            CREATE TABLE "AracMasraflari" ("Id" INTEGER PRIMARY KEY, "FirmaId" INTEGER NOT NULL);
            CREATE TABLE "BankaKasaHareketleri" (
                "Id" INTEGER PRIMARY KEY, "FirmaId" INTEGER NOT NULL,
                "PersonelCebindenId" INTEGER, "AracId" INTEGER, "AracMasrafId" INTEGER,
                "MahsupHareketId" INTEGER, "PersonelGeriOdemeHareketId" INTEGER);
            INSERT INTO "Soforler" VALUES (1, 1), (2, 2);
            INSERT INTO "Araclar" VALUES (1, 1), (2, 2);
            INSERT INTO "AracMasraflari" VALUES (1, 1), (2, 2);
            """);
        return db;
    }

    private static string MigrationSql(string fieldName) =>
        (string?)typeof(GuardBankMovementAuxiliaryTenantLinks)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)?
            .GetRawConstantValue()
        ?? throw new InvalidOperationException($"Migration SQL bulunamadı: {fieldName}");

    private static string PostgresMigrationSql() =>
        (string?)typeof(GuardBankMovementAuxiliaryTenantLinks)
            .GetField("PostgresUp", BindingFlags.NonPublic | BindingFlags.Static)?
            .GetRawConstantValue()
        ?? throw new InvalidOperationException("PostgreSQL migration SQL bulunamadı.");

    private static void Execute(SqliteConnection db, string sql)
    {
        using var command = db.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

using System.Data;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

public static class TwoFactorMigrationHelper
{
    public static async Task ApplyTwoFactorColumnsAsync(ApplicationDbContext context)
    {
        try
        {
            const string tableName = "Kullanicilar";

            if (context.Database.IsNpgsql())
            {
                var sql = @"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Kullanicilar') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'IkiFaktorAktif') THEN
            ALTER TABLE ""Kullanicilar"" ADD COLUMN ""IkiFaktorAktif"" boolean NOT NULL DEFAULT false;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'IkiFaktorSecretKey') THEN
            ALTER TABLE ""Kullanicilar"" ADD COLUMN ""IkiFaktorSecretKey"" character varying(200) NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'IkiFaktorEtkinlestirmeTarihi') THEN
            ALTER TABLE ""Kullanicilar"" ADD COLUMN ""IkiFaktorEtkinlestirmeTarihi"" timestamp without time zone NULL;
        END IF;
    END IF;
END $$;";

                await context.Database.ExecuteSqlRawAsync(sql);
                return;
            }

            if (context.Database.IsSqlite())
            {
                await EnsureSqliteColumnAsync(context, tableName, "IkiFaktorAktif", "INTEGER NOT NULL DEFAULT 0");
                await EnsureSqliteColumnAsync(context, tableName, "IkiFaktorSecretKey", "TEXT NULL");
                await EnsureSqliteColumnAsync(context, tableName, "IkiFaktorEtkinlestirmeTarihi", "TEXT NULL");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TwoFactor migration hatasi: {ex.Message}");
        }
    }

    private static async Task EnsureSqliteColumnAsync(ApplicationDbContext context, string tableName, string columnName, string columnDefinition)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = $"SELECT 1 FROM pragma_table_info('{tableName}') WHERE name = $columnName LIMIT 1";

            var parameter = checkCommand.CreateParameter();
            parameter.ParameterName = "$columnName";
            parameter.Value = columnName;
            checkCommand.Parameters.Add(parameter);

            var exists = await checkCommand.ExecuteScalarAsync() is not null;
            if (exists)
            {
                return;
            }

            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnDefinition}";
            await alterCommand.ExecuteNonQueryAsync();
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}




using System.Data;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

public static class PersonelPuantajOnayMigrationHelper
{
    public static async Task ApplyPersonelPuantajOnayAsync(ApplicationDbContext context)
    {
        try
        {
            const string tableName = "PersonelPuantajlar";

            if (context.Database.IsNpgsql())
            {
                var sql = @"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PersonelPuantajlar') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'OnayDurumu') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""OnayDurumu"" integer NOT NULL DEFAULT 0;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'OnaylayanKullanici') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""OnaylayanKullanici"" text NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'OnayTarihi') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""OnayTarihi"" timestamp without time zone NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'OnayNotu') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""OnayNotu"" text NULL;
        END IF;
    END IF;
END $$;";

                await context.Database.ExecuteSqlRawAsync(sql);
                return;
            }

            if (context.Database.IsSqlite())
            {
                await EnsureSqliteColumnAsync(context, tableName, "OnayDurumu", "INTEGER NOT NULL DEFAULT 0");
                await EnsureSqliteColumnAsync(context, tableName, "OnaylayanKullanici", "TEXT NULL");
                await EnsureSqliteColumnAsync(context, tableName, "OnayTarihi", "TEXT NULL");
                await EnsureSqliteColumnAsync(context, tableName, "OnayNotu", "TEXT NULL");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Personel puantaj onay migration hatası: {ex.Message}");
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




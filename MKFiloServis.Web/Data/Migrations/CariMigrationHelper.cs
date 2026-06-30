using System.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Data.Migrations;

public static class CariMigrationHelper
{
    public static async Task ApplyCariAlanGenisletmeAsync(ApplicationDbContext context)
    {
        if (context.Database.IsNpgsql())
        {
            var sql = @"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cariler' AND column_name = 'Il') THEN
                        ALTER TABLE ""Cariler"" ADD COLUMN ""Il"" TEXT NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cariler' AND column_name = 'Ilce') THEN
                        ALTER TABLE ""Cariler"" ADD COLUMN ""Ilce"" TEXT NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cariler' AND column_name = 'PostaKodu') THEN
                        ALTER TABLE ""Cariler"" ADD COLUMN ""PostaKodu"" TEXT NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cariler' AND column_name = 'Telefon2') THEN
                        ALTER TABLE ""Cariler"" ADD COLUMN ""Telefon2"" TEXT NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cariler' AND column_name = 'Fax') THEN
                        ALTER TABLE ""Cariler"" ADD COLUMN ""Fax"" TEXT NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cariler' AND column_name = 'WebSitesi') THEN
                        ALTER TABLE ""Cariler"" ADD COLUMN ""WebSitesi"" TEXT NULL;
                    END IF;
                END $$;
            ";

            await context.Database.ExecuteSqlRawAsync(sql);
            return;
        }

        if (context.Database.IsSqlite())
        {
            await EnsureSqliteColumnAsync(context, "Il", "TEXT NULL");
            await EnsureSqliteColumnAsync(context, "Ilce", "TEXT NULL");
            await EnsureSqliteColumnAsync(context, "PostaKodu", "TEXT NULL");
            await EnsureSqliteColumnAsync(context, "Telefon2", "TEXT NULL");
            await EnsureSqliteColumnAsync(context, "Fax", "TEXT NULL");
            await EnsureSqliteColumnAsync(context, "WebSitesi", "TEXT NULL");
        }
    }

    private static async Task EnsureSqliteColumnAsync(ApplicationDbContext context, string columnName, string columnDefinition)
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
            checkCommand.CommandText = "SELECT 1 FROM pragma_table_info('Cariler') WHERE name = $columnName LIMIT 1";

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
            alterCommand.CommandText = $"ALTER TABLE \"Cariler\" ADD COLUMN \"{columnName}\" {columnDefinition}";
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




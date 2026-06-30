using System.Data;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

public static class AracMasrafMuhasebeMigrationHelper
{
    public static async Task ApplyAracMasrafMuhasebeAlanlariAsync(ApplicationDbContext context)
    {
        try
        {
            if (context.Database.IsNpgsql())
            {
                var sql = @"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'AracMasraflari' AND column_name = 'SoforId') THEN
                            ALTER TABLE ""AracMasraflari"" ADD COLUMN ""SoforId"" integer NULL;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'AracMasraflari' AND column_name = 'CariId') THEN
                            ALTER TABLE ""AracMasraflari"" ADD COLUMN ""CariId"" integer NULL;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'AracMasraflari' AND column_name = 'MuhasebeFisId') THEN
                            ALTER TABLE ""AracMasraflari"" ADD COLUMN ""MuhasebeFisId"" integer NULL;
                        END IF;
                    END $$;
                ";

                await context.Database.ExecuteSqlRawAsync(sql);
                return;
            }

            if (context.Database.IsSqlite())
            {
                await EnsureSqliteColumnAsync(context, "SoforId", "INTEGER NULL");
                await EnsureSqliteColumnAsync(context, "CariId", "INTEGER NULL");
                await EnsureSqliteColumnAsync(context, "MuhasebeFisId", "INTEGER NULL");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Araç masraf muhasebe alanları migration hatası: {ex.Message}");
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
            checkCommand.CommandText = "SELECT 1 FROM pragma_table_info('AracMasraflari') WHERE name = $columnName LIMIT 1";

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
            alterCommand.CommandText = $"ALTER TABLE \"AracMasraflari\" ADD COLUMN \"{columnName}\" {columnDefinition}";
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




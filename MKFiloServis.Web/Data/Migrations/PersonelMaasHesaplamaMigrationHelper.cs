using System.Data;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

public static class PersonelMaasHesaplamaMigrationHelper
{
    public static async Task ApplyPersonelMaasHesaplamaAsync(ApplicationDbContext context)
    {
        try
        {
            if (context.Database.IsNpgsql())
            {
                var sql = @"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Personeller' AND column_name = 'BrutMaasHesaplamaTipi') THEN
        ALTER TABLE ""Personeller"" ADD COLUMN ""BrutMaasHesaplamaTipi"" integer NOT NULL DEFAULT 0;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Personeller' AND column_name = 'CalismaMiktari') THEN
        ALTER TABLE ""Personeller"" ADD COLUMN ""CalismaMiktari"" numeric(18,2) NOT NULL DEFAULT 0;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Personeller' AND column_name = 'BirimUcret') THEN
        ALTER TABLE ""Personeller"" ADD COLUMN ""BirimUcret"" numeric(18,2) NOT NULL DEFAULT 0;
    END IF;
END $$;";

                await context.Database.ExecuteSqlRawAsync(sql);
                return;
            }

            if (context.Database.IsSqlite())
            {
                await EnsureSqliteColumnAsync(context, "BrutMaasHesaplamaTipi", "INTEGER NOT NULL DEFAULT 0");
                await EnsureSqliteColumnAsync(context, "CalismaMiktari", "TEXT NOT NULL DEFAULT '0'");
                await EnsureSqliteColumnAsync(context, "BirimUcret", "TEXT NOT NULL DEFAULT '0'");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Personel maas hesaplama migration hatası: {ex.Message}");
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
            checkCommand.CommandText = "SELECT 1 FROM pragma_table_info('Personeller') WHERE name = $columnName LIMIT 1";
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
            alterCommand.CommandText = $"ALTER TABLE \"Personeller\" ADD COLUMN \"{columnName}\" {columnDefinition}";
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




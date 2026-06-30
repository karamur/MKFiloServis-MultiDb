using System.Data;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

public static class MuhasebeAyarMigrationHelper
{
    public static async Task ApplyStokMasrafAyarlariAsync(ApplicationDbContext context)
    {
        try
        {
            if (context.Database.IsNpgsql())
            {
                var sql = @"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'MuhasebeAyarlari' AND column_name = 'MalMasrafHesabi') THEN
        ALTER TABLE ""MuhasebeAyarlari"" ADD COLUMN ""MalMasrafHesabi"" VARCHAR(50) DEFAULT '740.99.001';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'MuhasebeAyarlari' AND column_name = 'SarfMalzemeMasrafHesabi') THEN
        ALTER TABLE ""MuhasebeAyarlari"" ADD COLUMN ""SarfMalzemeMasrafHesabi"" VARCHAR(50) DEFAULT '740.99.002';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'MuhasebeAyarlari' AND column_name = 'StokCikisHesabi') THEN
        ALTER TABLE ""MuhasebeAyarlari"" ADD COLUMN ""StokCikisHesabi"" VARCHAR(50) DEFAULT '153';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'MuhasebeAyarlari' AND column_name = 'StokMasrafAktarimiOtomatik') THEN
        ALTER TABLE ""MuhasebeAyarlari"" ADD COLUMN ""StokMasrafAktarimiOtomatik"" BOOLEAN DEFAULT TRUE;
    END IF;
END $$;";

                await context.Database.ExecuteSqlRawAsync(sql);
                return;
            }

            if (context.Database.IsSqlite())
            {
                await EnsureSqliteColumnAsync(context, "MalMasrafHesabi", "TEXT DEFAULT '740.99.001'");
                await EnsureSqliteColumnAsync(context, "SarfMalzemeMasrafHesabi", "TEXT DEFAULT '740.99.002'");
                await EnsureSqliteColumnAsync(context, "StokCikisHesabi", "TEXT DEFAULT '153'");
                await EnsureSqliteColumnAsync(context, "StokMasrafAktarimiOtomatik", "INTEGER DEFAULT 1");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Muhasebe Ayar migration hatası: {ex.Message}");
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
            checkCommand.CommandText = "SELECT 1 FROM pragma_table_info('MuhasebeAyarlari') WHERE name = $columnName LIMIT 1";
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
            alterCommand.CommandText = $"ALTER TABLE \"MuhasebeAyarlari\" ADD COLUMN \"{columnName}\" {columnDefinition}";
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




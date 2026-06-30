using System.Data;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

public static class FaturaGibDurumMigrationHelper
{
    public static async Task ApplyFaturaGibDurumAsync(ApplicationDbContext context)
    {
        try
        {
            const string tableName = "Faturalar";

            if (context.Database.IsNpgsql())
            {
                var sql = @"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'GibDurumu') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""GibDurumu"" integer NOT NULL DEFAULT 0;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'GibGonderimTarihi') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""GibGonderimTarihi"" timestamp without time zone NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'GibDurumGuncellemeTarihi') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""GibDurumGuncellemeTarihi"" timestamp without time zone NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'GibDurumMesaji') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""GibDurumMesaji"" text NULL;
    END IF;
    -- Tevkifat / Muhasebe Fisi alanlari (20260329235736)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'MuhasebeFisId') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""MuhasebeFisId"" integer NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'MuhasebeFisiOlusturuldu') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""MuhasebeFisiOlusturuldu"" boolean NOT NULL DEFAULT FALSE;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'TevkifatKodu') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""TevkifatKodu"" text NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'TevkifatOrani') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""TevkifatOrani"" numeric NOT NULL DEFAULT 0;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'TevkifatTutar') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""TevkifatTutar"" numeric NOT NULL DEFAULT 0;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'TevkifatliMi') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""TevkifatliMi"" boolean NOT NULL DEFAULT FALSE;
    END IF;
    -- Firmalar arasi fatura (20260407140609)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'FirmalarArasiFatura') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""FirmalarArasiFatura"" boolean NOT NULL DEFAULT FALSE;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'KarsiFirmaId') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""KarsiFirmaId"" integer NULL;
    END IF;
    -- Fatura eslestirme / mahsup (20260407141319)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'EslesenFaturaId') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""EslesenFaturaId"" integer NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'MahsupKapatildi') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""MahsupKapatildi"" boolean NOT NULL DEFAULT FALSE;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'MahsupTarihi') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""MahsupTarihi"" timestamp without time zone NULL;
    END IF;
END $$;";

                await context.Database.ExecuteSqlRawAsync(sql);
                return;
            }

            if (context.Database.IsSqlite())
            {
                // GiB durum alanlari
                await EnsureSqliteColumnAsync(context, tableName, "GibDurumu", "INTEGER NOT NULL DEFAULT 0");
                await EnsureSqliteColumnAsync(context, tableName, "GibGonderimTarihi", "TEXT NULL");
                await EnsureSqliteColumnAsync(context, tableName, "GibDurumGuncellemeTarihi", "TEXT NULL");
                await EnsureSqliteColumnAsync(context, tableName, "GibDurumMesaji", "TEXT NULL");

                // Tevkifat / Muhasebe Fisi alanlari (20260329235736)
                await EnsureSqliteColumnAsync(context, tableName, "MuhasebeFisId", "INTEGER NULL");
                await EnsureSqliteColumnAsync(context, tableName, "MuhasebeFisiOlusturuldu", "INTEGER NOT NULL DEFAULT 0");
                await EnsureSqliteColumnAsync(context, tableName, "TevkifatKodu", "TEXT NULL");
                await EnsureSqliteColumnAsync(context, tableName, "TevkifatOrani", "TEXT NOT NULL DEFAULT '0'");
                await EnsureSqliteColumnAsync(context, tableName, "TevkifatTutar", "TEXT NOT NULL DEFAULT '0'");
                await EnsureSqliteColumnAsync(context, tableName, "TevkifatliMi", "INTEGER NOT NULL DEFAULT 0");

                // Firmalar arasi fatura (20260407140609)
                await EnsureSqliteColumnAsync(context, tableName, "FirmalarArasiFatura", "INTEGER NOT NULL DEFAULT 0");
                await EnsureSqliteColumnAsync(context, tableName, "KarsiFirmaId", "INTEGER NULL");

                // Fatura eslestirme / mahsup (20260407141319)
                await EnsureSqliteColumnAsync(context, tableName, "EslesenFaturaId", "INTEGER NULL");
                await EnsureSqliteColumnAsync(context, tableName, "MahsupKapatildi", "INTEGER NOT NULL DEFAULT 0");
                await EnsureSqliteColumnAsync(context, tableName, "MahsupTarihi", "TEXT NULL");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatura GİB durum migration hatası: {ex.Message}");
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




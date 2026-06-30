using System.Data;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

#pragma warning disable EF1002 // Migration helper - tablo isimleri güvenli kaynaktan geliyor

namespace MKFiloServis.Web.Data.Migrations;

public static class SoforMaasMigrationHelper
{
    public static async Task ApplySoforMaasAlanlariAsync(ApplicationDbContext context)
    {
        try
        {
            var tableName = await ResolvePersonelTableNameAsync(context);
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return;
            }

            if (context.Database.IsNpgsql())
            {
                var sql = $@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = 'ArgePersoneli') THEN
                            ALTER TABLE ""{tableName}"" ADD COLUMN ""ArgePersoneli"" boolean NOT NULL DEFAULT FALSE;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = 'TopluMaas') THEN
                            ALTER TABLE ""{tableName}"" ADD COLUMN ""TopluMaas"" numeric(18,2) NOT NULL DEFAULT 0;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = 'SgkMaasi') THEN
                            ALTER TABLE ""{tableName}"" ADD COLUMN ""SgkMaasi"" numeric(18,2) NOT NULL DEFAULT 0;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = 'ResmiNetMaas') THEN
                            ALTER TABLE ""{tableName}"" ADD COLUMN ""ResmiNetMaas"" numeric(18,2) NOT NULL DEFAULT 0;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = 'DigerMaas') THEN
                            ALTER TABLE ""{tableName}"" ADD COLUMN ""DigerMaas"" numeric(18,2) NOT NULL DEFAULT 0;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = 'SGKBordroDahilMi') THEN
                            ALTER TABLE ""{tableName}"" ADD COLUMN ""SGKBordroDahilMi"" boolean NOT NULL DEFAULT FALSE;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = 'BordroTipiPersonel') THEN
                            ALTER TABLE ""{tableName}"" ADD COLUMN ""BordroTipiPersonel"" integer NOT NULL DEFAULT 0;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = 'SgkCikisTarihi') THEN
                            ALTER TABLE ""{tableName}"" ADD COLUMN ""SgkCikisTarihi"" timestamp without time zone NULL;
                        END IF;
                    END $$;
                ";

                await context.Database.ExecuteSqlRawAsync(sql);
                await context.Database.ExecuteSqlRawAsync($@"UPDATE ""{tableName}"" SET ""ResmiNetMaas"" = ""NetMaas"" WHERE COALESCE(""ResmiNetMaas"", 0) = 0 AND COALESCE(""DigerMaas"", 0) = 0 AND COALESCE(""NetMaas"", 0) > 0");
                // Mevcut ArgePersoneli = true olanları SGKBordroDahilMi = true, BordroTipiPersonel = 2 (Arge) yap
                await context.Database.ExecuteSqlRawAsync($@"UPDATE ""{tableName}"" SET ""SGKBordroDahilMi"" = TRUE, ""BordroTipiPersonel"" = 2 WHERE ""ArgePersoneli"" = TRUE AND ""SGKBordroDahilMi"" = FALSE");
                return;
            }

            if (context.Database.IsSqlite())
            {
                await EnsureSqliteColumnAsync(context, tableName, "ArgePersoneli", "INTEGER NOT NULL DEFAULT 0");
                await EnsureSqliteColumnAsync(context, tableName, "TopluMaas", "TEXT NOT NULL DEFAULT '0'");
                await EnsureSqliteColumnAsync(context, tableName, "SgkMaasi", "TEXT NOT NULL DEFAULT '0'");
                await EnsureSqliteColumnAsync(context, tableName, "ResmiNetMaas", "TEXT NOT NULL DEFAULT '0'");
                await EnsureSqliteColumnAsync(context, tableName, "DigerMaas", "TEXT NOT NULL DEFAULT '0'");
                await EnsureSqliteColumnAsync(context, tableName, "SgkCikisTarihi", "TEXT NULL");
                await context.Database.ExecuteSqlRawAsync($@"UPDATE ""{tableName}"" SET ""ResmiNetMaas"" = ""NetMaas"" WHERE IFNULL(""ResmiNetMaas"", '0') = '0' AND IFNULL(""DigerMaas"", '0') = '0' AND IFNULL(""NetMaas"", '0') <> '0'");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Şoför maaş alanları migration hatası: {ex.Message}");
        }
    }

    private static async Task<string?> ResolvePersonelTableNameAsync(ApplicationDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();

            if (context.Database.IsNpgsql())
            {
                command.CommandText = @"
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = current_schema()
                  AND table_name IN ('Personeller', 'Soforler')
                ORDER BY CASE WHEN table_name = 'Personeller' THEN 0 ELSE 1 END
                LIMIT 1";
            }
            else if (context.Database.IsSqlite())
            {
                command.CommandText = @"
                SELECT name
                FROM sqlite_master
                WHERE type = 'table'
                  AND name IN ('Personeller', 'Soforler')
                ORDER BY CASE WHEN name = 'Personeller' THEN 0 ELSE 1 END
                LIMIT 1";
            }
            else
            {
                return null;
            }

            return await command.ExecuteScalarAsync() as string;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
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




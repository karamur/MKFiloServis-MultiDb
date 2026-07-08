using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// Guzergah entity'sine PuantajCarpani kolonu eklemek için idempotent migration helper.
/// Multi-tenant deployment'da race-condition safe.
/// </summary>
public static class PuantajCarpaniMigrationHelper
{
    public static async Task ApplyAsync(DbContext context, ILogger logger)
    {
        if (context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            // SQLite: PRAGMA ile kolon kontrolü
            var conn = context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(\"Guzergahlar\")";
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync()) cols.Add(reader.GetString(1));
            if (!cols.Contains("PuantajCarpani"))
            {
                using var addCmd = conn.CreateCommand();
                addCmd.CommandText = "ALTER TABLE \"Guzergahlar\" ADD COLUMN \"PuantajCarpani\" NUMERIC NOT NULL DEFAULT 1.0";
                await addCmd.ExecuteNonQueryAsync();
            }
            logger.LogInformation("PuantajCarpaniMigrationHelper: Guzergahlar.PuantajCarpani kolonu kontrol edildi/eklendi (SQLite)");
            return;
        }

        var sql = @"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name = 'Guzergahlar' AND column_name = 'PuantajCarpani') THEN
                    ALTER TABLE ""Guzergahlar"" ADD COLUMN ""PuantajCarpani"" numeric(10,2) NOT NULL DEFAULT 1.0;
                END IF;
            END $$;
        ";

        await context.Database.ExecuteSqlRawAsync(sql);
        logger.LogInformation("PuantajCarpaniMigrationHelper: Guzergahlar.PuantajCarpani kolonu kontrol edildi/eklendi");
    }
}




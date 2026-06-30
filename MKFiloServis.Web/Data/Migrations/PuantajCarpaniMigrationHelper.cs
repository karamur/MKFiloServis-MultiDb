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




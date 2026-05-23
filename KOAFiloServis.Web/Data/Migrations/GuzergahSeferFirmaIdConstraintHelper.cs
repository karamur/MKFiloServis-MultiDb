using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Data.Migrations;

/// <summary>
/// GuzergahSeferleri ve Guzergahlar tablolarindaki FirmaId FK constraint'lerini
/// kaldirir. Tenant DB'lerde FK ihlaline yol actigi icin gereksizdir.
/// </summary>
public static class GuzergahSeferFirmaIdConstraintHelper
{
    public static async Task ApplyAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        var constraints = new[] {
            ("GuzergahSeferleri", "FK_GuzergahSeferleri_Firmalar_FirmaId"),
            ("Guzergahlar", "FK_Guzergahlar_Firmalar_FirmaId")
        };

        foreach (var (table, constraint) in constraints)
        {
            try
            {
                var sql = $"""
                    DO $$
                    BEGIN
                        IF EXISTS (
                            SELECT 1 FROM information_schema.table_constraints
                            WHERE constraint_name = '{constraint}'
                            AND table_name = '{table}'
                        ) THEN
                            ALTER TABLE "{table}" DROP CONSTRAINT "{constraint}";
                        END IF;
                    END $$;
                    """;
                await context.Database.ExecuteSqlRawAsync(sql);
                logger?.LogInformation("FK constraint {Constraint} kaldirildi.", constraint);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "FK {Constraint} kaldirilirken hata (kritik degil)", constraint);
            }
        }
    }
}

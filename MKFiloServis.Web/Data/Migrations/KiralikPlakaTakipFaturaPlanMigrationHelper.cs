using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// KiralikPlakaTakipler ve KiralikPlakaTakipFaturalar tablolarina plan kolonlarini ekler.
/// Idempotent - her startup'ta guvenle calisir.
/// </summary>
public static class KiralikPlakaTakipFaturaPlanMigrationHelper
{
    public static async Task ApplyAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        var takipKolonlari = await GetColumnNamesAsync(context, "KiralikPlakaTakipler");
        var takipColumnsToAdd = new Dictionary<string, string>
        {
            ["OdemeSayisi"] = "integer NOT NULL DEFAULT 12"
        };

        foreach (var (col, type) in takipColumnsToAdd)
        {
            if (!takipKolonlari.Contains(col))
            {
#pragma warning disable EF1002
                await context.Database.ExecuteSqlRawAsync($"ALTER TABLE \"KiralikPlakaTakipler\" ADD COLUMN \"{col}\" {type}");
#pragma warning restore EF1002
                logger?.LogInformation("KiralikPlakaTakipFaturaPlanMigration: KiralikPlakaTakipler.{Col} kolonu eklendi.", col);
            }
        }

        var detayKolonlari = await GetColumnNamesAsync(context, "KiralikPlakaTakipFaturalar");
        var detayColumnsToAdd = new Dictionary<string, string>
        {
            ["BazPlanTutari"] = "numeric NOT NULL DEFAULT 0",
            ["EkOdemeTutari"] = "numeric NOT NULL DEFAULT 0",
            ["PlanTutari"] = "numeric NOT NULL DEFAULT 0"
        };

        foreach (var (col, type) in detayColumnsToAdd)
        {
            if (!detayKolonlari.Contains(col))
            {
#pragma warning disable EF1002
                await context.Database.ExecuteSqlRawAsync($"ALTER TABLE \"KiralikPlakaTakipFaturalar\" ADD COLUMN \"{col}\" {type}");
#pragma warning restore EF1002
                logger?.LogInformation("KiralikPlakaTakipFaturaPlanMigration: KiralikPlakaTakipFaturalar.{Col} kolonu eklendi.", col);
            }
        }

        logger?.LogInformation("KiralikPlakaTakipFaturaPlanMigration: Tum kolonlar mevcut.");
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(ApplicationDbContext context, string tableName)
    {
        var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        var conn = context.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        if (isSqlite)
            cmd.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        else
            cmd.CommandText = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}'";
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync())
            cols.Add(isSqlite ? reader.GetString(1) : reader.GetString(0));
        return cols;
    }
}




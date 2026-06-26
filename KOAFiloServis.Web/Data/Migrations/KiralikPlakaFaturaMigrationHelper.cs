using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Data.Migrations;

/// <summary>
/// KiralikPlakaTakipler tablosuna fatura ve odeme takip kolonlarini ekler.
/// Idempotent - her startup'ta guvenle calisir.
/// </summary>
public static class KiralikPlakaFaturaMigrationHelper
{
    public static async Task ApplyAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        var cols = await GetColumnNamesAsync(context);

        var columnsToAdd = new Dictionary<string, string>
        {
            ["KesilenFaturaNo"] = "varchar(50) NULL",
            ["KesilenFaturaTarih"] = "timestamp NULL",
            ["KesilenFaturaTutar"] = "numeric NOT NULL DEFAULT 0",
            ["KalanFaturaTutar"] = "numeric NOT NULL DEFAULT 0",
            ["GelenFaturaId"] = "integer NULL",
            ["ToplamOdeme"] = "numeric NOT NULL DEFAULT 0",
            ["OdenenTutar"] = "numeric NOT NULL DEFAULT 0",
            ["SonOdemeTarihi"] = "timestamp NULL",
            ["OdemeSayisi"] = "integer NOT NULL DEFAULT 12"
        };

        foreach (var (col, type) in columnsToAdd)
        {
            if (!cols.Contains(col))
            {
#pragma warning disable EF1002
                await context.Database.ExecuteSqlRawAsync(
                    $"ALTER TABLE \"KiralikPlakaTakipler\" ADD COLUMN \"{col}\" {type}");
#pragma warning restore EF1002
                logger?.LogInformation("KiralikPlakaFaturaMigration: {Col} kolonu eklendi.", col);
            }
        }

        logger?.LogInformation("KiralikPlakaFaturaMigration: Tum kolonlar mevcut.");
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(ApplicationDbContext context)
    {
        var conn = context.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT column_name FROM information_schema.columns
            WHERE table_name = 'KiralikPlakaTakipler'
            """;
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        var cols = new HashSet<string>();
        while (await reader.ReadAsync())
            cols.Add(reader.GetString(0));
        return cols;
    }
}

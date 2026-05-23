using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Data.Migrations;

/// <summary>
/// Sprint S1: PuantajKayitlar tablosuna SeferSlot, KurumId ve IsverenFirmaId
/// kolonlarını ekler. Idempotent - her startup'ta güvenle çalışır.
/// </summary>
public static class PuantajSlotMigrationHelper
{
    public static async Task ApplyAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        var cols = await GetColumnNamesAsync(context);

        if (!cols.Contains("Slot"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"Slot\" integer NOT NULL DEFAULT 1");
            logger?.LogInformation("PuantajSlotMigration: Slot kolonu eklendi.");
        }

        if (!cols.Contains("KurumId"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"KurumId\" integer NULL");
            logger?.LogInformation("PuantajSlotMigration: KurumId kolonu eklendi.");
        }

        if (!cols.Contains("IsverenFirmaId"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"IsverenFirmaId\" integer NULL");
            logger?.LogInformation("PuantajSlotMigration: IsverenFirmaId kolonu eklendi.");
        }

        if (!cols.Contains("KaynakTipi"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"KaynakTipi\" integer NOT NULL DEFAULT 1");
            logger?.LogInformation("PuantajSlotMigration: KaynakTipi kolonu eklendi.");
        }

        if (!cols.Contains("FinansYonu"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"FinansYonu\" integer NOT NULL DEFAULT 2");
            logger?.LogInformation("PuantajSlotMigration: FinansYonu kolonu eklendi.");
        }

        if (!cols.Contains("BelgeNo"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"BelgeNo\" varchar(50) NULL");
            logger?.LogInformation("PuantajSlotMigration: BelgeNo kolonu eklendi.");
        }

        if (!cols.Contains("TransferDurum"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"TransferDurum\" varchar(50) NULL");
            logger?.LogInformation("PuantajSlotMigration: TransferDurum kolonu eklendi.");
        }

        if (!cols.Contains("SlotAdi"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"SlotAdi\" varchar(50) NULL");
            logger?.LogInformation("PuantajSlotMigration: SlotAdi kolonu eklendi.");
        }

        logger?.LogInformation("PuantajSlotMigration: Tum kolonlar mevcut.");
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(ApplicationDbContext context)
    {
        var conn = context.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT column_name FROM information_schema.columns
            WHERE table_name = 'PuantajKayitlar'
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

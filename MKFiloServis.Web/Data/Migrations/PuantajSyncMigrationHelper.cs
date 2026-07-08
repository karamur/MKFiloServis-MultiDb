using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// Puantaj-Operasyon senkronizasyonu için OperasyonKayitlari tablosuna
/// yeni kolonlar ve unique index ekler (idempotent).
/// </summary>
public static class PuantajSyncMigrationHelper
{
    public static async Task ApplyAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        logger?.LogInformation("PuantajSyncMigration: basliyor...");

        // OperasyonKayitlari tablosu SyncPuantajSchemaMigrationHelper tarafından yönetilir;
        // SQLite'ta o helper erken return yapar dolayısıyla tablo yoktur — burada da atlıyoruz.
        if (context.Database.IsSqlite())
        {
            logger?.LogInformation("PuantajSyncMigration: SQLite için atlanıyor.");
            return;
        }

        var cols = await GetColumnNamesAsync(context, "OperasyonKayitlari");

        // ── KaynakPuantajId ────────────────────────────────────────────────
        if (!cols.Contains("KaynakPuantajId"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"OperasyonKayitlari\" ADD COLUMN \"KaynakPuantajId\" integer NULL");
            logger?.LogInformation("PuantajSyncMigration: KaynakPuantajId eklendi.");
        }

        // ── KullaniciKilitliMi ─────────────────────────────────────────────
        if (!cols.Contains("KullaniciKilitliMi"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"OperasyonKayitlari\" ADD COLUMN \"KullaniciKilitliMi\" boolean NOT NULL DEFAULT false");
            logger?.LogInformation("PuantajSyncMigration: KullaniciKilitliMi eklendi.");
        }

        // ── KilitTarihi ────────────────────────────────────────────────────
        if (!cols.Contains("KilitTarihi"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"OperasyonKayitlari\" ADD COLUMN \"KilitTarihi\" timestamp without time zone NULL");
            logger?.LogInformation("PuantajSyncMigration: KilitTarihi eklendi.");
        }

        // ── KilitleyenKullaniciId ──────────────────────────────────────────
        if (!cols.Contains("KilitleyenKullaniciId"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"OperasyonKayitlari\" ADD COLUMN \"KilitleyenKullaniciId\" integer NULL");
            logger?.LogInformation("PuantajSyncMigration: KilitleyenKullaniciId eklendi.");
        }

        // ── Unique index: (KaynakPuantajId, Tarih) partial ─────────────────
        await CreateIndexIfNotExists(context,
            "IX_OperasyonKayitlari_KaynakPuantajId_Tarih",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_OperasyonKayitlari_KaynakPuantajId_Tarih\" " +
            "ON \"OperasyonKayitlari\" (\"KaynakPuantajId\", \"Tarih\") " +
            "WHERE \"IsDeleted\" = false AND \"KaynakPuantajId\" IS NOT NULL",
            logger);

        logger?.LogInformation("PuantajSyncMigration: tamamlandi.");
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(ApplicationDbContext context, string tableName)
    {
        var conn = context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        if (context.Database.IsSqlite())
        {
            cmd.CommandText = $"SELECT name FROM pragma_table_info('{tableName}')";
        }
        else
        {
            cmd.CommandText = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}'";
        }
        await using var reader = await cmd.ExecuteReaderAsync();
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync())
            cols.Add(reader.GetString(0));
        return cols;
    }

    private static async Task CreateIndexIfNotExists(
        ApplicationDbContext context,
        string indexName, string rawSql,
        ILogger? logger)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(rawSql);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex,
                "PuantajSyncMigration: {Index} indeksi olusturulamadi (kritik degil)", indexName);
        }
    }
}




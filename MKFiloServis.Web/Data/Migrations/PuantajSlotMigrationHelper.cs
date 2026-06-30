using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MKFiloServis.Web.Data.Migrations;

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

        // ── Sprint S1b (commit 6b29328 restore): HesapDonemi + Revizyon kolonları ──
        // Bu kolonlar [NotMapped] idi, EF mapping restore sonrası tenant DB'lerde eksik.
        // SyncPuantajSchema migration'ı shared DB'ye uygulandı fakat tenant DB'lere uygulanmadı.
        // Root cause: KurumPuantaj SELECT sırasında PostgreSQL 42703 (column does not exist).

        if (!cols.Contains("HesapDonemiId"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"HesapDonemiId\" integer NULL");
            logger?.LogInformation("PuantajSlotMigration: HesapDonemiId kolonu eklendi.");
        }

        if (!cols.Contains("OncekiVersiyonId"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"OncekiVersiyonId\" integer NULL");
            logger?.LogInformation("PuantajSlotMigration: OncekiVersiyonId kolonu eklendi.");
        }

        if (!cols.Contains("Versiyon"))
        {
            // NOT NULL + DEFAULT 1: mevcut satırlar otomatik 1 alır, backfill gerekmez.
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"PuantajKayitlar\" ADD COLUMN \"Versiyon\" integer NOT NULL DEFAULT 1");
            logger?.LogInformation("PuantajSlotMigration: Versiyon kolonu eklendi (DEFAULT 1).");
        }

        logger?.LogInformation("PuantajSlotMigration: Tum kolonlar mevcut.");

        // ── PuantajHesapDonemleri: idempotent column adds ────────────────

        var hesapDonemCols = await GetColumnNamesAsync(context, "PuantajHesapDonemleri");
        var hesapDonemMissingCols = new Dictionary<string, string>
        {
            ["OnayDurum"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"OnayDurum\" integer NOT NULL DEFAULT 0",
            ["HesaplayanKullanici"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"HesaplayanKullanici\" character varying(100) NULL",
            ["HesaplamaTarihi"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"HesaplamaTarihi\" timestamp without time zone NOT NULL DEFAULT now()",
            ["Notlar"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"Notlar\" character varying(500) NULL",
            ["FinansOnaylayan"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"FinansOnaylayan\" character varying(100) NULL",
            ["FinansOnayTarihi"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"FinansOnayTarihi\" timestamp without time zone NULL",
            ["MuhasebeOnaylayan"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"MuhasebeOnaylayan\" character varying(100) NULL",
            ["MuhasebeOnayTarihi"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"MuhasebeOnayTarihi\" timestamp without time zone NULL",
            ["KilitTarihi"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"KilitTarihi\" timestamp without time zone NULL",
            ["KilitAciklama"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"KilitAciklama\" character varying(100) NULL",
            ["CreatedBy"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"CreatedBy\" character varying(100) NULL",
            ["UpdatedBy"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"UpdatedBy\" character varying(100) NULL",
            ["DeletedAt"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"DeletedAt\" timestamp without time zone NULL",
            ["DeletedBy"] = "ALTER TABLE \"PuantajHesapDonemleri\" ADD COLUMN \"DeletedBy\" character varying(100) NULL",
        };

        foreach (var (colName, sql) in hesapDonemMissingCols)
        {
            if (!hesapDonemCols.Contains(colName))
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync(sql);
                    logger?.LogInformation("PuantajSlotMigration: PuantajHesapDonemleri.{Col} kolonu eklendi.", colName);
                }
                catch { /* sütun zaten varsa hata yut */ }
            }
        }

        // ── Eksik olabilecek index'leri idempotent ekle ──
        await CreateIndexIfNotExists(context,
            "IX_PuantajKayitlar_HesapDonemiId", "PuantajKayitlar", "HesapDonemiId", logger);
        await CreateIndexIfNotExists(context,
            "IX_PuantajKayitlar_OncekiVersiyonId", "PuantajKayitlar", "OncekiVersiyonId", logger);
        await CreateIndexIfNotExists(context,
            "IX_PuantajKayitlar_IsverenFirmaId", "PuantajKayitlar", "IsverenFirmaId", logger);
        await CreateIndexIfNotExists(context,
            "IX_PuantajKayitlar_KurumId", "PuantajKayitlar", "KurumId", logger);

        // ── Eksik olabilecek FK'ları idempotent ekle (DO block ile) ──
        await CreateFkIfNotExists(context,
            "FK_PuantajKayitlar_PuantajHesapDonemleri_HesapDonemiId",
            "PuantajKayitlar", "HesapDonemiId",
            "PuantajHesapDonemleri", "Id", "SET NULL", logger);
        await CreateFkIfNotExists(context,
            "FK_PuantajKayitlar_PuantajKayitlar_OncekiVersiyonId",
            "PuantajKayitlar", "OncekiVersiyonId",
            "PuantajKayitlar", "Id", "SET NULL", logger);
        await CreateFkIfNotExists(context,
            "FK_PuantajKayitlar_Firmalar_IsverenFirmaId",
            "PuantajKayitlar", "IsverenFirmaId",
            "Firmalar", "Id", "SET NULL", logger);
        await CreateFkIfNotExists(context,
            "FK_PuantajKayitlar_Kurumlar_KurumId",
            "PuantajKayitlar", "KurumId",
            "Kurumlar", "Id", "SET NULL", logger);
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(ApplicationDbContext context)
        => await GetColumnNamesAsync(context, "PuantajKayitlar");

    private static async Task<HashSet<string>> GetColumnNamesAsync(ApplicationDbContext context, string tableName)
    {
        var conn = context.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT column_name FROM information_schema.columns
            WHERE table_name = '{tableName}'
            """;
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        var cols = new HashSet<string>();
        while (await reader.ReadAsync())
            cols.Add(reader.GetString(0));
        return cols;
    }

    private static async Task CreateIndexIfNotExists(
        ApplicationDbContext context,
        string indexName, string tableName, string columnName,
        ILogger? logger)
    {
        try
        {
            // EF1002: DDL ile sistem tarafindan uretilen tablo/kolon adi kullaniliyor, kullanici girdisi yok
#pragma warning disable EF1002
            await context.Database.ExecuteSqlRawAsync(
                $"CREATE INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" (\"{columnName}\")");
#pragma warning restore EF1002
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex,
                "PuantajSlotMigration: {Index} indeksi olusturulamadi (kritik degil)", indexName);
        }
    }

    private static async Task CreateFkIfNotExists(
        ApplicationDbContext context,
        string constraintName, string tableName, string columnName,
        string refTable, string refColumn, string onDeleteAction,
        ILogger? logger)
    {
        try
        {
            // Önce hedef tablo var mı kontrol et
            var refTableExists = await TableExistsAsync(context, refTable);
            if (!refTableExists)
            {
                logger?.LogInformation(
                    "PuantajSlotMigration: {Constraint} atlandı — {RefTable} tablosu yok.",
                    constraintName, refTable);
                return;
            }

            var sql = $"""
                DO $$ BEGIN
                    ALTER TABLE "{tableName}" ADD CONSTRAINT "{constraintName}"
                        FOREIGN KEY ("{columnName}") REFERENCES "{refTable}"("{refColumn}")
                        ON DELETE {onDeleteAction};
                EXCEPTION WHEN duplicate_object THEN END; $$;
                """;
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            // FK başarısız olabilir (tenant DB'de eski veri varsa, vb.)
            // Kritik değil — sadece log'la, devam et.
            logger?.LogWarning(ex,
                "PuantajSlotMigration: {Constraint} FK'si olusturulamadi (kritik degil)", constraintName);
        }
    }

    private static async Task<bool> TableExistsAsync(ApplicationDbContext context, string tableName)
    {
        var conn = context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_name = @tableName
            )
            """;
        var p = cmd.CreateParameter();
        p.ParameterName = "tableName";
        p.Value = tableName;
        cmd.Parameters.Add(p);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }
}




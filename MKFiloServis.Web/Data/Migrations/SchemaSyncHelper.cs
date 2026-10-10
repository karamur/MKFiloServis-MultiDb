using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// Eski FisNoCounters tablosu için sağlayıcıya özel geriye dönük uyumluluk.
/// Genel model şema eşitlemesi EF migration'larının yerine geçmez.
/// </summary>
public static class SchemaSyncHelper
{
    /// <summary>
    /// FisNoCounters tablosunun doğru PK/unique constraint ile var olduğundan emin olur.
    /// Eski 2-kolonlu PK (Prefix, YilAy) → yeni 3-kolonlu unique (Prefix, FirmaId, YilAy) geçişini yapar.
    /// Bu olmadan NumaraSerisiService 42P10 hatası verir.
    /// </summary>
    public static async Task EnsureFisNoCountersSchemaAsync(ApplicationDbContext context)
    {
        var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        var previousTimeout = context.Database.GetCommandTimeout();
        try
        {
            // Bu migration bazı ortamlarda lock beklemesi nedeniyle 30sn varsayılan timeout'u aşabiliyor.
            context.Database.SetCommandTimeout(180);

            if (isSqlite)
            {
                // SQLite: tablo oluştur ve FirmaId kolonu ekle (idempotent)
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""FisNoCounters"" (
                        ""Prefix""  TEXT NOT NULL,
                        ""FirmaId"" INTEGER NOT NULL DEFAULT 0,
                        ""YilAy""   TEXT NOT NULL,
                        ""SonNo""   INTEGER NOT NULL DEFAULT 0,
                        PRIMARY KEY (""Prefix"", ""FirmaId"", ""YilAy"")
                    );
                ");
                // FirmaId kolonunu ekle (eski şema için) — kolon yoksa ekle, varsa atla
                var firmaIdExists = false;
                try
                {
                    // Bağlantının sahibi DbContext'tir; burada dispose etmek SQLite
                    // :memory: veritabanını ve çağıranın mevcut bağlantısını kapatır.
                    var checkConn = context.Database.GetDbConnection();
                    if (checkConn.State != System.Data.ConnectionState.Open)
                        await checkConn.OpenAsync();
                    await using var checkCmd = checkConn.CreateCommand();
                    checkCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('FisNoCounters') WHERE name='FirmaId'";
                    firmaIdExists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                }
                catch { /* kontrol başarısız olursa eklemeyi dene */ }

                if (!firmaIdExists)
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE ""FisNoCounters"" ADD COLUMN ""FirmaId"" INTEGER NOT NULL DEFAULT 0;
                    ");
                }
                await MKFiloServis.Shared.Auditing.DatabaseWriteAudit.EnsureAsync(context.Database.GetDbConnection());
                // NULL FirmaId'leri düzelt
                await context.Database.ExecuteSqlRawAsync(@"
                    UPDATE ""FisNoCounters"" SET ""FirmaId"" = 0 WHERE ""FirmaId"" IS NULL;
                ");
                return;
            }

            await context.Database.ExecuteSqlRawAsync(@"
                -- Tablo yoksa doğru şemayla oluştur
                CREATE TABLE IF NOT EXISTS ""FisNoCounters"" (
                    ""Prefix""  TEXT NOT NULL,
                    ""FirmaId"" INTEGER NOT NULL DEFAULT 0,
                    ""YilAy""   TEXT NOT NULL,
                    ""SonNo""   INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (""Prefix"", ""FirmaId"", ""YilAy"")
                );

                -- Eski tabloyu yeni şemaya yükselt (idempotent)
                DO $$ DECLARE
                    pk_name text;
                    has_expected_unique boolean;
                BEGIN
                    -- 1) FirmaId kolonunu ekle (yoksa)
                    ALTER TABLE ""FisNoCounters"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NOT NULL DEFAULT 0;

                    -- 2) Beklenen unique index var mı kontrol et
                    SELECT EXISTS (
                        SELECT 1 FROM pg_index i
                        JOIN pg_class t ON t.oid = i.indrelid
                        JOIN pg_namespace n ON n.oid = t.relnamespace
                        WHERE i.indisunique
                          AND n.nspname = 'public'
                          AND t.relname = 'FisNoCounters'
                          AND (SELECT array_agg(a.attname ORDER BY x.n)
                               FROM unnest(i.indkey) WITH ORDINALITY AS x(attnum, n)
                               JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = x.attnum
                              ) = ARRAY['Prefix', 'FirmaId', 'YilAy']::name[]
                    ) INTO has_expected_unique;

                    -- 3) Beklenen unique yoksa: eski PK'yi kaldır, yeni unique index oluştur
                    IF NOT has_expected_unique THEN
                        -- 3a) Mevcut PK constraint adını bul
                        SELECT c.conname INTO pk_name
                        FROM pg_constraint c
                        JOIN pg_class t ON t.oid = c.conrelid
                        JOIN pg_namespace n ON n.oid = t.relnamespace
                        WHERE c.contype = 'p'
                          AND n.nspname = 'public'
                          AND t.relname = 'FisNoCounters'
                        LIMIT 1;

                        -- 3b) PK'yi kaldır
                        IF pk_name IS NOT NULL THEN
                            EXECUTE format('ALTER TABLE %I DROP CONSTRAINT %I', 'FisNoCounters', pk_name);
                        END IF;

                        -- 3c) NULL FirmaId'leri düzelt (eski kayıtlar için)
                        UPDATE ""FisNoCounters"" SET ""FirmaId"" = 0 WHERE ""FirmaId"" IS NULL;

                        -- 3d) Yeni unique index'i oluştur
                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_FisNoCounters_Prefix_FirmaId_YilAy""
                            ON ""FisNoCounters"" (""Prefix"", ""FirmaId"", ""YilAy"");
                    END IF;
                END $$;
            ");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "FisNoCounters eski şema uyarlaması tamamlanamadı; uygulama başlatılmadı.", ex);
        }
        finally
        {
            context.Database.SetCommandTimeout(previousTimeout);
        }
    }


}




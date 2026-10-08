using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261008130000_GuardInvoicePaymentMatchFirm")]
public sealed class GuardInvoicePaymentMatchFirm : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql(PostgresUp);
            return;
        }

        if (ActiveProvider?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql(SqlitePreflight);
            migrationBuilder.Sql(SqliteMatchGuards);
            migrationBuilder.Sql(SqliteEndpointGuards);
            return;
        }

        throw new NotSupportedException("Ödeme eşleştirme firma bağı PostgreSQL ve SQLite için uygulanmıştır.");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_A15_OdemeEslestirme_Firma_Insert" ON "OdemeEslestirmeleri";
                DROP TRIGGER IF EXISTS "TR_A15_OdemeEslestirme_Firma_Update" ON "OdemeEslestirmeleri";
                DROP TRIGGER IF EXISTS "TR_A15_OdemeEslestirme_FaturaFirma" ON "Faturalar";
                DROP TRIGGER IF EXISTS "TR_A15_OdemeEslestirme_HareketFirma" ON "BankaKasaHareketleri";
                DROP FUNCTION IF EXISTS a15_guard_odeme_eslestirme_firma();
                DROP FUNCTION IF EXISTS a15_guard_odeme_eslestirme_fatura_firma();
                DROP FUNCTION IF EXISTS a15_guard_odeme_eslestirme_hareket_firma();
                """);
            return;
        }

        if (ActiveProvider?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_A15_OdemeEslestirme_Firma_Insert";
                DROP TRIGGER IF EXISTS "TR_A15_OdemeEslestirme_Firma_Update";
                DROP TRIGGER IF EXISTS "TR_A15_OdemeEslestirme_FaturaFirma";
                DROP TRIGGER IF EXISTS "TR_A15_OdemeEslestirme_HareketFirma";
                """);
            return;
        }

        throw new NotSupportedException("Ödeme eşleştirme firma bağı PostgreSQL ve SQLite için uygulanmıştır.");
    }

    private const string PostgresUp = """
        DO $a15_match_preflight$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM "OdemeEslestirmeleri" m
                LEFT JOIN "Faturalar" f ON f."Id" = m."FaturaId"
                LEFT JOIN "BankaKasaHareketleri" b ON b."Id" = m."BankaKasaHareketId"
                WHERE f."Id" IS NULL OR b."Id" IS NULL OR f."FirmaId" IS NULL
                   OR f."FirmaId" <= 0 OR f."FirmaId" IS DISTINCT FROM b."FirmaId"
            ) THEN
                RAISE EXCEPTION 'A-15: Fatura/ödeme eşleştirmelerinde firma uyuşmazlığı var. Verileri onarıp migration işlemini yeniden başlatın.';
            END IF;
        END $a15_match_preflight$;

        CREATE FUNCTION a15_guard_odeme_eslestirme_firma() RETURNS trigger LANGUAGE plpgsql AS $a15_match$
        DECLARE invoice_firm integer; movement_firm integer;
        BEGIN
            SELECT f."FirmaId", b."FirmaId" INTO invoice_firm, movement_firm
            FROM "Faturalar" f JOIN "BankaKasaHareketleri" b ON b."Id" = NEW."BankaKasaHareketId"
            WHERE f."Id" = NEW."FaturaId" FOR SHARE OF f, b;
            IF NOT FOUND OR invoice_firm IS NULL OR invoice_firm <= 0 OR invoice_firm IS DISTINCT FROM movement_firm THEN
                RAISE EXCEPTION 'A-15: Ödeme eşleştirmesindeki fatura ve banka hareketi aynı firmaya ait olmalıdır.';
            END IF;
            RETURN NEW;
        END $a15_match$;
        CREATE TRIGGER "TR_A15_OdemeEslestirme_Firma_Insert"
        BEFORE INSERT ON "OdemeEslestirmeleri"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_odeme_eslestirme_firma();
        CREATE TRIGGER "TR_A15_OdemeEslestirme_Firma_Update"
        BEFORE UPDATE OF "FaturaId", "BankaKasaHareketId" ON "OdemeEslestirmeleri"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_odeme_eslestirme_firma();

        CREATE FUNCTION a15_guard_odeme_eslestirme_fatura_firma() RETURNS trigger LANGUAGE plpgsql AS $a15_invoice$
        BEGIN
            IF NEW."FirmaId" IS DISTINCT FROM OLD."FirmaId" AND EXISTS (
                SELECT 1 FROM "OdemeEslestirmeleri" m
                JOIN "BankaKasaHareketleri" b ON b."Id" = m."BankaKasaHareketId"
                WHERE m."FaturaId" = OLD."Id" AND b."FirmaId" IS DISTINCT FROM NEW."FirmaId"
            ) THEN
                RAISE EXCEPTION 'A-15: Ödeme eşleştirmesi bulunan faturanın firması, banka hareketiyle uyumsuz hale getirilemez.';
            END IF;
            RETURN NEW;
        END $a15_invoice$;
        CREATE TRIGGER "TR_A15_OdemeEslestirme_FaturaFirma"
        BEFORE UPDATE OF "FirmaId" ON "Faturalar"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_odeme_eslestirme_fatura_firma();

        CREATE FUNCTION a15_guard_odeme_eslestirme_hareket_firma() RETURNS trigger LANGUAGE plpgsql AS $a15_movement$
        BEGIN
            IF NEW."FirmaId" IS DISTINCT FROM OLD."FirmaId" AND EXISTS (
                SELECT 1 FROM "OdemeEslestirmeleri" m
                JOIN "Faturalar" f ON f."Id" = m."FaturaId"
                WHERE m."BankaKasaHareketId" = OLD."Id" AND f."FirmaId" IS DISTINCT FROM NEW."FirmaId"
            ) THEN
                RAISE EXCEPTION 'A-15: Ödeme eşleştirmesi bulunan banka hareketi başka firmaya taşınamaz.';
            END IF;
            RETURN NEW;
        END $a15_movement$;
        CREATE TRIGGER "TR_A15_OdemeEslestirme_HareketFirma"
        BEFORE UPDATE OF "FirmaId" ON "BankaKasaHareketleri"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_odeme_eslestirme_hareket_firma();
        """;

    private const string SqlitePreflight = """
        CREATE TEMP TABLE "__a15_match_preflight" ("Valid" INTEGER NOT NULL CHECK ("Valid" = 1));
        INSERT INTO "__a15_match_preflight" ("Valid")
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM "OdemeEslestirmeleri" m
            LEFT JOIN "Faturalar" f ON f."Id" = m."FaturaId"
            LEFT JOIN "BankaKasaHareketleri" b ON b."Id" = m."BankaKasaHareketId"
            WHERE f."Id" IS NULL OR b."Id" IS NULL OR f."FirmaId" IS NULL
               OR f."FirmaId" <= 0 OR f."FirmaId" IS NOT b."FirmaId"
        ) THEN 0 ELSE 1 END;
        DROP TABLE "__a15_match_preflight";
        """;

    private const string SqliteMatchGuards = """
        CREATE TRIGGER "TR_A15_OdemeEslestirme_Firma_Insert"
        BEFORE INSERT ON "OdemeEslestirmeleri"
        WHEN NOT EXISTS (
            SELECT 1 FROM "Faturalar" f JOIN "BankaKasaHareketleri" b ON b."Id" = NEW."BankaKasaHareketId"
            WHERE f."Id" = NEW."FaturaId" AND f."FirmaId" IS NOT NULL
              AND f."FirmaId" > 0 AND f."FirmaId" = b."FirmaId"
        )
        BEGIN SELECT RAISE(ABORT, 'A-15: Fatura ve banka hareketi aynı firmaya ait olmalıdır.'); END;

        CREATE TRIGGER "TR_A15_OdemeEslestirme_Firma_Update"
        BEFORE UPDATE OF "FaturaId", "BankaKasaHareketId" ON "OdemeEslestirmeleri"
        WHEN NOT EXISTS (
            SELECT 1 FROM "Faturalar" f JOIN "BankaKasaHareketleri" b ON b."Id" = NEW."BankaKasaHareketId"
            WHERE f."Id" = NEW."FaturaId" AND f."FirmaId" IS NOT NULL
              AND f."FirmaId" > 0 AND f."FirmaId" = b."FirmaId"
        )
        BEGIN SELECT RAISE(ABORT, 'A-15: Fatura ve banka hareketi aynı firmaya ait olmalıdır.'); END;
        """;

    private const string SqliteEndpointGuards = """
        CREATE TRIGGER "TR_A15_OdemeEslestirme_FaturaFirma"
        BEFORE UPDATE OF "FirmaId" ON "Faturalar"
        WHEN NEW."FirmaId" IS NOT OLD."FirmaId" AND EXISTS (
            SELECT 1 FROM "OdemeEslestirmeleri" m
            JOIN "BankaKasaHareketleri" b ON b."Id" = m."BankaKasaHareketId"
            WHERE m."FaturaId" = OLD."Id" AND b."FirmaId" IS NOT NEW."FirmaId"
        )
        BEGIN SELECT RAISE(ABORT, 'A-15: Eşleştirilmiş faturanın firması ödeme hareketiyle uyumsuz hale getirilemez.'); END;

        CREATE TRIGGER "TR_A15_OdemeEslestirme_HareketFirma"
        BEFORE UPDATE OF "FirmaId" ON "BankaKasaHareketleri"
        WHEN NEW."FirmaId" IS NOT OLD."FirmaId" AND EXISTS (
            SELECT 1 FROM "OdemeEslestirmeleri" m
            JOIN "Faturalar" f ON f."Id" = m."FaturaId"
            WHERE m."BankaKasaHareketId" = OLD."Id" AND f."FirmaId" IS NOT NEW."FirmaId"
        )
        BEGIN SELECT RAISE(ABORT, 'A-15: Eşleştirilmiş banka hareketi başka firmaya taşınamaz.'); END;
        """;
}

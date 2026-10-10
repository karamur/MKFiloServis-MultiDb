using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261009170000_GuardInvoiceCariTenantLink")]
public sealed class GuardInvoiceCariTenantLink : Migration
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
            migrationBuilder.Sql(SqliteInvoiceInsert);
            migrationBuilder.Sql(SqliteInvoiceUpdate);
            migrationBuilder.Sql(SqliteCariUpdate);
            return;
        }

        throw new NotSupportedException("Fatura-cari firma bağı PostgreSQL ve SQLite için uygulanmıştır.");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_A15_Fatura_CariFirma" ON "Faturalar";
                DROP TRIGGER IF EXISTS "TR_A15_Cari_Firma_Fatura" ON "Cariler";
                DROP FUNCTION IF EXISTS a15_guard_fatura_cari_firma();
                DROP FUNCTION IF EXISTS a15_guard_cari_firma_fatura();
                """);
            return;
        }

        if (ActiveProvider?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_A15_Fatura_CariFirma_Insert";
                DROP TRIGGER IF EXISTS "TR_A15_Fatura_CariFirma_Update";
                DROP TRIGGER IF EXISTS "TR_A15_Cari_Firma_Fatura";
                """);
            return;
        }

        throw new NotSupportedException("Fatura-cari firma bağı PostgreSQL ve SQLite için uygulanmıştır.");
    }

    private const string PostgresUp = """
        LOCK TABLE "Faturalar", "Cariler" IN SHARE ROW EXCLUSIVE MODE;

        DO $a15_invoice_cari_preflight$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM "Faturalar" f
                LEFT JOIN "Cariler" c ON c."Id" = f."CariId"
                WHERE f."IsDeleted" = FALSE AND (
                    f."FirmaId" IS NULL OR f."FirmaId" <= 0 OR c."Id" IS NULL
                    OR c."FirmaId" IS NULL OR c."FirmaId" <= 0
                    OR f."FirmaId" IS DISTINCT FROM c."FirmaId"
                )
            ) THEN
                RAISE EXCEPTION 'A-15: Fatura-cari firma uyuşmazlığı var. Verileri inceleyip düzeltin, sonra migration işlemini yeniden başlatın.';
            END IF;
        END $a15_invoice_cari_preflight$;

        CREATE FUNCTION a15_guard_fatura_cari_firma() RETURNS trigger LANGUAGE plpgsql AS $a15_invoice_cari$
        DECLARE cari_firm integer;
        BEGIN
            SELECT c."FirmaId" INTO cari_firm FROM "Cariler" c
            WHERE c."Id" = NEW."CariId" FOR SHARE;
            IF NOT FOUND OR NEW."FirmaId" IS NULL OR NEW."FirmaId" <= 0
               OR cari_firm IS NULL OR cari_firm <= 0 OR cari_firm IS DISTINCT FROM NEW."FirmaId" THEN
                RAISE EXCEPTION 'A-15: Fatura ve cari aynı geçerli firmaya ait olmalıdır.';
            END IF;
            RETURN NEW;
        END $a15_invoice_cari$;
        CREATE TRIGGER "TR_A15_Fatura_CariFirma"
        BEFORE INSERT OR UPDATE OF "CariId", "FirmaId" ON "Faturalar"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_fatura_cari_firma();

        CREATE FUNCTION a15_guard_cari_firma_fatura() RETURNS trigger LANGUAGE plpgsql AS $a15_cari_invoice$
        BEGIN
            IF NEW."FirmaId" IS DISTINCT FROM OLD."FirmaId" AND EXISTS (
                SELECT 1 FROM "Faturalar" f
                WHERE f."CariId" = OLD."Id" AND f."IsDeleted" = FALSE
                  AND (f."FirmaId" IS DISTINCT FROM NEW."FirmaId" OR NEW."FirmaId" IS NULL OR NEW."FirmaId" <= 0)
            ) THEN
                RAISE EXCEPTION 'A-15: Aktif faturası bulunan carinin firması faturalarla uyumsuz hale getirilemez.';
            END IF;
            RETURN NEW;
        END $a15_cari_invoice$;
        CREATE TRIGGER "TR_A15_Cari_Firma_Fatura"
        BEFORE UPDATE OF "FirmaId" ON "Cariler"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_cari_firma_fatura();
        """;

    private const string SqlitePreflight = """
        CREATE TEMP TABLE "__a15_invoice_cari_preflight" ("Valid" INTEGER NOT NULL CHECK ("Valid" = 1));
        INSERT INTO "__a15_invoice_cari_preflight" ("Valid")
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM "Faturalar" f
            LEFT JOIN "Cariler" c ON c."Id" = f."CariId"
            WHERE f."IsDeleted" = 0 AND (
                f."FirmaId" IS NULL OR f."FirmaId" <= 0 OR c."Id" IS NULL
                OR c."FirmaId" IS NULL OR c."FirmaId" <= 0 OR f."FirmaId" IS NOT c."FirmaId"
            )
        ) THEN 0 ELSE 1 END;
        DROP TABLE "__a15_invoice_cari_preflight";
        """;

    private const string SqliteInvoiceInsert = """
        CREATE TRIGGER "TR_A15_Fatura_CariFirma_Insert"
        BEFORE INSERT ON "Faturalar"
        WHEN NOT EXISTS (
            SELECT 1 FROM "Cariler" c WHERE c."Id" = NEW."CariId"
              AND c."FirmaId" IS NOT NULL AND c."FirmaId" > 0
              AND NEW."FirmaId" IS NOT NULL AND NEW."FirmaId" > 0
              AND c."FirmaId" = NEW."FirmaId"
        )
        BEGIN SELECT RAISE(ABORT, 'A-15: Fatura ve cari aynı geçerli firmaya ait olmalıdır.'); END;
        """;

    private const string SqliteInvoiceUpdate = """
        CREATE TRIGGER "TR_A15_Fatura_CariFirma_Update"
        BEFORE UPDATE OF "CariId", "FirmaId" ON "Faturalar"
        WHEN NOT EXISTS (
            SELECT 1 FROM "Cariler" c WHERE c."Id" = NEW."CariId"
              AND c."FirmaId" IS NOT NULL AND c."FirmaId" > 0
              AND NEW."FirmaId" IS NOT NULL AND NEW."FirmaId" > 0
              AND c."FirmaId" = NEW."FirmaId"
        )
        BEGIN SELECT RAISE(ABORT, 'A-15: Fatura ve cari aynı geçerli firmaya ait olmalıdır.'); END;
        """;

    private const string SqliteCariUpdate = """
        CREATE TRIGGER "TR_A15_Cari_Firma_Fatura"
        BEFORE UPDATE OF "FirmaId" ON "Cariler"
        WHEN NEW."FirmaId" IS NOT OLD."FirmaId" AND EXISTS (
            SELECT 1 FROM "Faturalar" f WHERE f."CariId" = OLD."Id" AND f."IsDeleted" = 0
              AND (f."FirmaId" IS NOT NEW."FirmaId" OR NEW."FirmaId" IS NULL OR NEW."FirmaId" <= 0)
        )
        BEGIN SELECT RAISE(ABORT, 'A-15: Aktif faturası bulunan carinin firması faturalarla uyumsuz hale getirilemez.'); END;
        """;
}

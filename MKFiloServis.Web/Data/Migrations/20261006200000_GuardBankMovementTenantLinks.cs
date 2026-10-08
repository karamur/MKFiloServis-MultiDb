using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261006200000_GuardBankMovementTenantLinks")]
public sealed class GuardBankMovementTenantLinks : Migration
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
            migrationBuilder.Sql(SqliteChildInsert);
            migrationBuilder.Sql(SqliteChildUpdate);
            migrationBuilder.Sql(SqliteAccountUpdate);
            migrationBuilder.Sql(SqliteCariUpdate);
            return;
        }

        throw new NotSupportedException("Banka hareketi firma bağı PostgreSQL ve SQLite için uygulanmıştır.");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_A15_BankaHesap_Firma" ON "BankaHesaplari";
                DROP TRIGGER IF EXISTS "TR_A15_Cari_Firma" ON "Cariler";
                DROP TRIGGER IF EXISTS "TR_A15_BankaKasaHareket_Firma" ON "BankaKasaHareketleri";
                DROP FUNCTION IF EXISTS a15_guard_banka_hesap_firma();
                DROP FUNCTION IF EXISTS a15_guard_cari_firma();
                DROP FUNCTION IF EXISTS a15_guard_banka_hareket_firma();
                """);
            return;
        }

        if (ActiveProvider?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_A15_BankaHesap_Firma";
                DROP TRIGGER IF EXISTS "TR_A15_Cari_Firma";
                DROP TRIGGER IF EXISTS "TR_A15_BankaKasaHareket_Firma_Insert";
                DROP TRIGGER IF EXISTS "TR_A15_BankaKasaHareket_Firma_Update";
                """);
            return;
        }

        throw new NotSupportedException("Banka hareketi firma bağı PostgreSQL ve SQLite için uygulanmıştır.");
    }

    private const string PostgresUp = """
        DO $a15_preflight$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM "BankaKasaHareketleri" h
                LEFT JOIN "BankaHesaplari" a ON a."Id" = h."BankaHesapId"
                LEFT JOIN "Cariler" c ON c."Id" = h."CariId"
                LEFT JOIN "BankaHesaplari" p ON p."Id" = h."PersonelOdemeHesapId"
                WHERE a."Id" IS NULL OR a."FirmaId" IS DISTINCT FROM h."FirmaId"
                   OR (h."CariId" IS NOT NULL AND (c."Id" IS NULL OR c."FirmaId" IS DISTINCT FROM h."FirmaId"))
                   OR (h."PersonelOdemeHesapId" IS NOT NULL AND (p."Id" IS NULL OR p."FirmaId" IS DISTINCT FROM h."FirmaId"))
            ) THEN
                RAISE EXCEPTION 'A-15: Banka/Kasa hareketlerinde firma bağı uyuşmazlığı var. Kayıtları onarıp migration işlemini yeniden başlatın.';
            END IF;
        END $a15_preflight$;

        CREATE FUNCTION a15_guard_banka_hareket_firma() RETURNS trigger LANGUAGE plpgsql AS $a15_child$
        BEGIN
            IF TG_OP = 'UPDATE' AND NEW."FirmaId" IS DISTINCT FROM OLD."FirmaId" THEN
                RAISE EXCEPTION 'A-15: Mevcut Banka/Kasa hareketinin firması değiştirilemez.';
            END IF;
            IF NOT EXISTS (SELECT 1 FROM "BankaHesaplari" a
                           WHERE a."Id" = NEW."BankaHesapId" AND a."FirmaId" = NEW."FirmaId" FOR SHARE) THEN
                RAISE EXCEPTION 'A-15: Banka/Kasa hareketinin hesabı aynı firmaya ait olmalıdır.';
            END IF;
            IF NEW."CariId" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM "Cariler" c
                           WHERE c."Id" = NEW."CariId" AND c."FirmaId" = NEW."FirmaId" FOR SHARE) THEN
                RAISE EXCEPTION 'A-15: Banka/Kasa hareketinin carisi aynı firmaya ait olmalıdır.';
            END IF;
            IF NEW."PersonelOdemeHesapId" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM "BankaHesaplari" p
                           WHERE p."Id" = NEW."PersonelOdemeHesapId" AND p."FirmaId" = NEW."FirmaId" FOR SHARE) THEN
                RAISE EXCEPTION 'A-15: Personel geri ödeme hesabı aynı firmaya ait olmalıdır.';
            END IF;
            RETURN NEW;
        END $a15_child$;

        CREATE TRIGGER "TR_A15_BankaKasaHareket_Firma"
        BEFORE INSERT OR UPDATE OF "FirmaId", "BankaHesapId", "CariId", "PersonelOdemeHesapId"
        ON "BankaKasaHareketleri" FOR EACH ROW EXECUTE FUNCTION a15_guard_banka_hareket_firma();

        CREATE FUNCTION a15_guard_banka_hesap_firma() RETURNS trigger LANGUAGE plpgsql AS $a15_account$
        BEGIN
            IF NEW."FirmaId" IS DISTINCT FROM OLD."FirmaId" AND EXISTS (
                SELECT 1 FROM "BankaKasaHareketleri" h
                WHERE h."BankaHesapId" = OLD."Id" OR h."PersonelOdemeHesapId" = OLD."Id"
            ) THEN
                RAISE EXCEPTION 'A-15: Hareketlerle ilişkili banka hesabının firması değiştirilemez.';
            END IF;
            RETURN NEW;
        END $a15_account$;

        CREATE TRIGGER "TR_A15_BankaHesap_Firma"
        BEFORE UPDATE OF "FirmaId" ON "BankaHesaplari"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_banka_hesap_firma();

        CREATE FUNCTION a15_guard_cari_firma() RETURNS trigger LANGUAGE plpgsql AS $a15_cari$
        BEGIN
            IF NEW."FirmaId" IS DISTINCT FROM OLD."FirmaId" AND EXISTS (
                SELECT 1 FROM "BankaKasaHareketleri" h WHERE h."CariId" = OLD."Id"
            ) THEN
                RAISE EXCEPTION 'A-15: Hareketlerle ilişkili carinin firması değiştirilemez.';
            END IF;
            RETURN NEW;
        END $a15_cari$;

        CREATE TRIGGER "TR_A15_Cari_Firma"
        BEFORE UPDATE OF "FirmaId" ON "Cariler"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_cari_firma();
        """;

    private const string SqlitePreflight = """
        CREATE TEMP TABLE "__a15_firma_preflight" ("Valid" INTEGER NOT NULL CHECK ("Valid" = 1));
        INSERT INTO "__a15_firma_preflight" ("Valid")
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM "BankaKasaHareketleri" h
            LEFT JOIN "BankaHesaplari" a ON a."Id" = h."BankaHesapId"
            LEFT JOIN "Cariler" c ON c."Id" = h."CariId"
            LEFT JOIN "BankaHesaplari" p ON p."Id" = h."PersonelOdemeHesapId"
            WHERE a."Id" IS NULL OR a."FirmaId" IS NOT h."FirmaId"
               OR (h."CariId" IS NOT NULL AND (c."Id" IS NULL OR c."FirmaId" IS NOT h."FirmaId"))
               OR (h."PersonelOdemeHesapId" IS NOT NULL AND (p."Id" IS NULL OR p."FirmaId" IS NOT h."FirmaId"))
        ) THEN 0 ELSE 1 END;
        DROP TABLE "__a15_firma_preflight";
        """;

    private const string SqliteChildInsert = """
        CREATE TRIGGER "TR_A15_BankaKasaHareket_Firma_Insert"
        BEFORE INSERT ON "BankaKasaHareketleri"
        BEGIN
            SELECT RAISE(ABORT, 'A-15: Banka/Kasa hesabı farklı firmada.')
            WHERE NOT EXISTS (SELECT 1 FROM "BankaHesaplari" a
                              WHERE a."Id" = NEW."BankaHesapId" AND a."FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Cari farklı firmada.')
            WHERE NEW."CariId" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM "Cariler" c
                              WHERE c."Id" = NEW."CariId" AND c."FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Personel geri ödeme hesabı farklı firmada.')
            WHERE NEW."PersonelOdemeHesapId" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM "BankaHesaplari" p
                              WHERE p."Id" = NEW."PersonelOdemeHesapId" AND p."FirmaId" IS NEW."FirmaId");
        END;
        """;

    private const string SqliteChildUpdate = """
        CREATE TRIGGER "TR_A15_BankaKasaHareket_Firma_Update"
        BEFORE UPDATE OF "FirmaId", "BankaHesapId", "CariId", "PersonelOdemeHesapId"
        ON "BankaKasaHareketleri"
        BEGIN
            SELECT RAISE(ABORT, 'A-15: Mevcut Banka/Kasa hareketinin firması değiştirilemez.')
            WHERE NEW."FirmaId" IS NOT OLD."FirmaId";
            SELECT RAISE(ABORT, 'A-15: Banka/Kasa hesabı farklı firmada.')
            WHERE NOT EXISTS (SELECT 1 FROM "BankaHesaplari" a
                              WHERE a."Id" = NEW."BankaHesapId" AND a."FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Cari farklı firmada.')
            WHERE NEW."CariId" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM "Cariler" c
                              WHERE c."Id" = NEW."CariId" AND c."FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Personel geri ödeme hesabı farklı firmada.')
            WHERE NEW."PersonelOdemeHesapId" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM "BankaHesaplari" p
                              WHERE p."Id" = NEW."PersonelOdemeHesapId" AND p."FirmaId" IS NEW."FirmaId");
        END;
        """;

    private const string SqliteAccountUpdate = """
        CREATE TRIGGER "TR_A15_BankaHesap_Firma"
        BEFORE UPDATE OF "FirmaId" ON "BankaHesaplari"
        WHEN NEW."FirmaId" IS NOT OLD."FirmaId"
        BEGIN
            SELECT RAISE(ABORT, 'A-15: Hareketlerle ilişkili banka hesabının firması değiştirilemez.')
            WHERE EXISTS (SELECT 1 FROM "BankaKasaHareketleri" h
                          WHERE h."BankaHesapId" = OLD."Id" OR h."PersonelOdemeHesapId" = OLD."Id");
        END;
        """;

    private const string SqliteCariUpdate = """
        CREATE TRIGGER "TR_A15_Cari_Firma"
        BEFORE UPDATE OF "FirmaId" ON "Cariler"
        WHEN NEW."FirmaId" IS NOT OLD."FirmaId"
        BEGIN
            SELECT RAISE(ABORT, 'A-15: Hareketlerle ilişkili carinin firması değiştirilemez.')
            WHERE EXISTS (SELECT 1 FROM "BankaKasaHareketleri" h WHERE h."CariId" = OLD."Id");
        END;
        """;
}

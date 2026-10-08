using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261009150000_GuardBankMovementAuxiliaryTenantLinks")]
public sealed class GuardBankMovementAuxiliaryTenantLinks : Migration
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
            migrationBuilder.Sql(SqliteVehicleUpdate);
            migrationBuilder.Sql(SqliteExpenseUpdate);
            migrationBuilder.Sql(SqliteDriverUpdate);
            return;
        }

        throw new NotSupportedException("Banka hareketi yardımcı firma bağları PostgreSQL ve SQLite için uygulanmıştır.");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_A15_BankaKasaHareket_YardimciFirma" ON "BankaKasaHareketleri";
                DROP TRIGGER IF EXISTS "TR_A15_Arac_Firma_BankaHareketi" ON "Araclar";
                DROP TRIGGER IF EXISTS "TR_A15_AracMasraf_Firma_BankaHareketi" ON "AracMasraflari";
                DROP TRIGGER IF EXISTS "TR_A15_Sofor_Firma_BankaHareketi" ON "Soforler";
                DROP FUNCTION IF EXISTS a15_guard_banka_hareket_yardimci_firma();
                DROP FUNCTION IF EXISTS a15_guard_arac_firma_banka_hareketi();
                DROP FUNCTION IF EXISTS a15_guard_arac_masraf_firma_banka_hareketi();
                DROP FUNCTION IF EXISTS a15_guard_sofor_firma_banka_hareketi();
                """);
            return;
        }

        if (ActiveProvider?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_A15_BankaKasaHareket_YardimciFirma_Insert";
                DROP TRIGGER IF EXISTS "TR_A15_BankaKasaHareket_YardimciFirma_Update";
                DROP TRIGGER IF EXISTS "TR_A15_Arac_Firma_BankaHareketi";
                DROP TRIGGER IF EXISTS "TR_A15_AracMasraf_Firma_BankaHareketi";
                DROP TRIGGER IF EXISTS "TR_A15_Sofor_Firma_BankaHareketi";
                """);
            return;
        }

        throw new NotSupportedException("Banka hareketi yardımcı firma bağları PostgreSQL ve SQLite için uygulanmıştır.");
    }

    private const string PostgresUp = """
        LOCK TABLE "BankaKasaHareketleri", "Soforler", "Araclar", "AracMasraflari"
        IN SHARE ROW EXCLUSIVE MODE;

        DO $a15_aux_preflight$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM "BankaKasaHareketleri" h
                LEFT JOIN "Soforler" s ON s."Id" = h."PersonelCebindenId"
                LEFT JOIN "Araclar" a ON a."Id" = h."AracId"
                LEFT JOIN "AracMasraflari" m ON m."Id" = h."AracMasrafId"
                LEFT JOIN "BankaKasaHareketleri" mahsup ON mahsup."Id" = h."MahsupHareketId"
                LEFT JOIN "BankaKasaHareketleri" geri ON geri."Id" = h."PersonelGeriOdemeHareketId"
                WHERE (h."PersonelCebindenId" IS NOT NULL AND (s."Id" IS NULL OR s."FirmaId" IS DISTINCT FROM h."FirmaId"))
                   OR (h."AracId" IS NOT NULL AND (a."Id" IS NULL OR a."FirmaId" IS DISTINCT FROM h."FirmaId"))
                   OR (h."AracMasrafId" IS NOT NULL AND (m."Id" IS NULL OR m."FirmaId" IS DISTINCT FROM h."FirmaId"))
                   OR (h."MahsupHareketId" IS NOT NULL AND (mahsup."Id" IS NULL OR mahsup."FirmaId" IS DISTINCT FROM h."FirmaId"))
                   OR (h."PersonelGeriOdemeHareketId" IS NOT NULL AND (geri."Id" IS NULL OR geri."FirmaId" IS DISTINCT FROM h."FirmaId"))
            ) THEN
                RAISE EXCEPTION 'A-15: Banka/Kasa hareketlerinde yardımcı firma bağı uyuşmazlığı var. Kayıtları onarıp migration işlemini yeniden başlatın.';
            END IF;
        END $a15_aux_preflight$;

        CREATE FUNCTION a15_guard_banka_hareket_yardimci_firma() RETURNS trigger LANGUAGE plpgsql AS $a15_aux$
        DECLARE target_firm integer;
        BEGIN
            IF NEW."PersonelCebindenId" IS NOT NULL THEN
                SELECT "FirmaId" INTO target_firm FROM "Soforler" WHERE "Id" = NEW."PersonelCebindenId" FOR SHARE;
                IF NOT FOUND OR target_firm IS DISTINCT FROM NEW."FirmaId" THEN
                    RAISE EXCEPTION 'A-15: Personel cebinden kaydı farklı firmada.';
                END IF;
            END IF;
            IF NEW."AracId" IS NOT NULL THEN
                SELECT "FirmaId" INTO target_firm FROM "Araclar" WHERE "Id" = NEW."AracId" FOR SHARE;
                IF NOT FOUND OR target_firm IS DISTINCT FROM NEW."FirmaId" THEN
                    RAISE EXCEPTION 'A-15: Araç farklı firmada.';
                END IF;
            END IF;
            IF NEW."AracMasrafId" IS NOT NULL THEN
                SELECT "FirmaId" INTO target_firm FROM "AracMasraflari" WHERE "Id" = NEW."AracMasrafId" FOR SHARE;
                IF NOT FOUND OR target_firm IS DISTINCT FROM NEW."FirmaId" THEN
                    RAISE EXCEPTION 'A-15: Araç masrafı farklı firmada.';
                END IF;
            END IF;
            IF NEW."MahsupHareketId" IS NOT NULL THEN
                SELECT "FirmaId" INTO target_firm FROM "BankaKasaHareketleri" WHERE "Id" = NEW."MahsupHareketId" FOR SHARE;
                IF NOT FOUND OR target_firm IS DISTINCT FROM NEW."FirmaId" THEN
                    RAISE EXCEPTION 'A-15: Mahsup hareketi farklı firmada.';
                END IF;
            END IF;
            IF NEW."PersonelGeriOdemeHareketId" IS NOT NULL THEN
                SELECT "FirmaId" INTO target_firm FROM "BankaKasaHareketleri" WHERE "Id" = NEW."PersonelGeriOdemeHareketId" FOR SHARE;
                IF NOT FOUND OR target_firm IS DISTINCT FROM NEW."FirmaId" THEN
                    RAISE EXCEPTION 'A-15: Geri ödeme hareketi farklı firmada.';
                END IF;
            END IF;
            RETURN NEW;
        END $a15_aux$;
        CREATE TRIGGER "TR_A15_BankaKasaHareket_YardimciFirma"
        BEFORE INSERT OR UPDATE OF "PersonelCebindenId", "AracId", "AracMasrafId", "MahsupHareketId", "PersonelGeriOdemeHareketId"
        ON "BankaKasaHareketleri" FOR EACH ROW EXECUTE FUNCTION a15_guard_banka_hareket_yardimci_firma();

        CREATE FUNCTION a15_guard_arac_firma_banka_hareketi() RETURNS trigger LANGUAGE plpgsql AS $a15_vehicle$
        BEGIN
            IF NEW."FirmaId" IS DISTINCT FROM OLD."FirmaId" AND EXISTS
                (SELECT 1 FROM "BankaKasaHareketleri" WHERE "AracId" = OLD."Id") THEN
                RAISE EXCEPTION 'A-15: Banka/Kasa hareketine bağlı aracın firması değiştirilemez.';
            END IF;
            RETURN NEW;
        END $a15_vehicle$;
        CREATE TRIGGER "TR_A15_Arac_Firma_BankaHareketi" BEFORE UPDATE OF "FirmaId" ON "Araclar"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_arac_firma_banka_hareketi();

        CREATE FUNCTION a15_guard_arac_masraf_firma_banka_hareketi() RETURNS trigger LANGUAGE plpgsql AS $a15_expense$
        BEGIN
            IF NEW."FirmaId" IS DISTINCT FROM OLD."FirmaId" AND EXISTS
                (SELECT 1 FROM "BankaKasaHareketleri" WHERE "AracMasrafId" = OLD."Id") THEN
                RAISE EXCEPTION 'A-15: Banka/Kasa hareketine bağlı masrafın firması değiştirilemez.';
            END IF;
            RETURN NEW;
        END $a15_expense$;
        CREATE TRIGGER "TR_A15_AracMasraf_Firma_BankaHareketi" BEFORE UPDATE OF "FirmaId" ON "AracMasraflari"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_arac_masraf_firma_banka_hareketi();

        CREATE FUNCTION a15_guard_sofor_firma_banka_hareketi() RETURNS trigger LANGUAGE plpgsql AS $a15_driver$
        BEGIN
            IF NEW."FirmaId" IS DISTINCT FROM OLD."FirmaId" AND EXISTS
                (SELECT 1 FROM "BankaKasaHareketleri" WHERE "PersonelCebindenId" = OLD."Id") THEN
                RAISE EXCEPTION 'A-15: Banka/Kasa hareketine bağlı personelin firması değiştirilemez.';
            END IF;
            RETURN NEW;
        END $a15_driver$;
        CREATE TRIGGER "TR_A15_Sofor_Firma_BankaHareketi" BEFORE UPDATE OF "FirmaId" ON "Soforler"
        FOR EACH ROW EXECUTE FUNCTION a15_guard_sofor_firma_banka_hareketi();
        """;

    private const string SqlitePreflight = """
        CREATE TEMP TABLE "__a15_aux_preflight" ("Valid" INTEGER NOT NULL CHECK ("Valid" = 1));
        INSERT INTO "__a15_aux_preflight" ("Valid")
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM "BankaKasaHareketleri" h
            LEFT JOIN "Soforler" s ON s."Id" = h."PersonelCebindenId"
            LEFT JOIN "Araclar" a ON a."Id" = h."AracId"
            LEFT JOIN "AracMasraflari" m ON m."Id" = h."AracMasrafId"
            LEFT JOIN "BankaKasaHareketleri" mahsup ON mahsup."Id" = h."MahsupHareketId"
            LEFT JOIN "BankaKasaHareketleri" geri ON geri."Id" = h."PersonelGeriOdemeHareketId"
            WHERE (h."PersonelCebindenId" IS NOT NULL AND (s."Id" IS NULL OR s."FirmaId" IS NOT h."FirmaId"))
               OR (h."AracId" IS NOT NULL AND (a."Id" IS NULL OR a."FirmaId" IS NOT h."FirmaId"))
               OR (h."AracMasrafId" IS NOT NULL AND (m."Id" IS NULL OR m."FirmaId" IS NOT h."FirmaId"))
               OR (h."MahsupHareketId" IS NOT NULL AND (mahsup."Id" IS NULL OR mahsup."FirmaId" IS NOT h."FirmaId"))
               OR (h."PersonelGeriOdemeHareketId" IS NOT NULL AND (geri."Id" IS NULL OR geri."FirmaId" IS NOT h."FirmaId"))
        ) THEN 0 ELSE 1 END;
        DROP TABLE "__a15_aux_preflight";
        """;

    private const string SqliteChildInsert = """
        CREATE TRIGGER "TR_A15_BankaKasaHareket_YardimciFirma_Insert" BEFORE INSERT ON "BankaKasaHareketleri"
        BEGIN
            SELECT RAISE(ABORT, 'A-15: Personel cebinden kaydı farklı firmada.')
            WHERE NEW."PersonelCebindenId" IS NOT NULL AND NOT EXISTS
                (SELECT 1 FROM "Soforler" WHERE "Id" = NEW."PersonelCebindenId" AND "FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Araç farklı firmada.')
            WHERE NEW."AracId" IS NOT NULL AND NOT EXISTS
                (SELECT 1 FROM "Araclar" WHERE "Id" = NEW."AracId" AND "FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Araç masrafı farklı firmada.')
            WHERE NEW."AracMasrafId" IS NOT NULL AND NOT EXISTS
                (SELECT 1 FROM "AracMasraflari" WHERE "Id" = NEW."AracMasrafId" AND "FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Mahsup hareketi farklı firmada.')
            WHERE NEW."MahsupHareketId" IS NOT NULL AND NOT EXISTS
                (SELECT 1 FROM "BankaKasaHareketleri" WHERE "Id" = NEW."MahsupHareketId" AND "FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Geri ödeme hareketi farklı firmada.')
            WHERE NEW."PersonelGeriOdemeHareketId" IS NOT NULL AND NOT EXISTS
                (SELECT 1 FROM "BankaKasaHareketleri" WHERE "Id" = NEW."PersonelGeriOdemeHareketId" AND "FirmaId" IS NEW."FirmaId");
        END;
        """;

    private const string SqliteChildUpdate = """
        CREATE TRIGGER "TR_A15_BankaKasaHareket_YardimciFirma_Update"
        BEFORE UPDATE OF "PersonelCebindenId", "AracId", "AracMasrafId", "MahsupHareketId", "PersonelGeriOdemeHareketId"
        ON "BankaKasaHareketleri"
        BEGIN
            SELECT RAISE(ABORT, 'A-15: Personel cebinden kaydı farklı firmada.')
            WHERE NEW."PersonelCebindenId" IS NOT NULL AND NOT EXISTS
                (SELECT 1 FROM "Soforler" WHERE "Id" = NEW."PersonelCebindenId" AND "FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Araç farklı firmada.')
            WHERE NEW."AracId" IS NOT NULL AND NOT EXISTS
                (SELECT 1 FROM "Araclar" WHERE "Id" = NEW."AracId" AND "FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Araç masrafı farklı firmada.')
            WHERE NEW."AracMasrafId" IS NOT NULL AND NOT EXISTS
                (SELECT 1 FROM "AracMasraflari" WHERE "Id" = NEW."AracMasrafId" AND "FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Mahsup hareketi farklı firmada.')
            WHERE NEW."MahsupHareketId" IS NOT NULL AND NOT EXISTS
                (SELECT 1 FROM "BankaKasaHareketleri" WHERE "Id" = NEW."MahsupHareketId" AND "FirmaId" IS NEW."FirmaId");
            SELECT RAISE(ABORT, 'A-15: Geri ödeme hareketi farklı firmada.')
            WHERE NEW."PersonelGeriOdemeHareketId" IS NOT NULL AND NOT EXISTS
                (SELECT 1 FROM "BankaKasaHareketleri" WHERE "Id" = NEW."PersonelGeriOdemeHareketId" AND "FirmaId" IS NEW."FirmaId");
        END;
        """;

    private const string SqliteVehicleUpdate = """
        CREATE TRIGGER "TR_A15_Arac_Firma_BankaHareketi" BEFORE UPDATE OF "FirmaId" ON "Araclar"
        WHEN NEW."FirmaId" IS NOT OLD."FirmaId"
        BEGIN
            SELECT RAISE(ABORT, 'A-15: Banka/Kasa hareketine bağlı aracın firması değiştirilemez.')
            WHERE EXISTS (SELECT 1 FROM "BankaKasaHareketleri" WHERE "AracId" = OLD."Id");
        END;
        """;

    private const string SqliteExpenseUpdate = """
        CREATE TRIGGER "TR_A15_AracMasraf_Firma_BankaHareketi" BEFORE UPDATE OF "FirmaId" ON "AracMasraflari"
        WHEN NEW."FirmaId" IS NOT OLD."FirmaId"
        BEGIN
            SELECT RAISE(ABORT, 'A-15: Banka/Kasa hareketine bağlı masrafın firması değiştirilemez.')
            WHERE EXISTS (SELECT 1 FROM "BankaKasaHareketleri" WHERE "AracMasrafId" = OLD."Id");
        END;
        """;

    private const string SqliteDriverUpdate = """
        CREATE TRIGGER "TR_A15_Sofor_Firma_BankaHareketi" BEFORE UPDATE OF "FirmaId" ON "Soforler"
        WHEN NEW."FirmaId" IS NOT OLD."FirmaId"
        BEGIN
            SELECT RAISE(ABORT, 'A-15: Banka/Kasa hareketine bağlı personelin firması değiştirilemez.')
            WHERE EXISTS (SELECT 1 FROM "BankaKasaHareketleri" WHERE "PersonelCebindenId" = OLD."Id");
        END;
        """;
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRentACarOdemeEnumColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    DO $$
                    BEGIN
                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = current_schema()
                              AND table_name = 'RentACarOdemeHareketleri'
                              AND column_name = 'HareketTuru'
                              AND data_type IN ('text', 'character varying')) THEN
                            ALTER TABLE "RentACarOdemeHareketleri"
                            ALTER COLUMN "HareketTuru" TYPE integer
                            USING CASE lower(trim("HareketTuru"))
                                WHEN 'kiratahsilati' THEN 0
                                WHEN 'depozitotahsilati' THEN 1
                                WHEN 'kiraiadesi' THEN 2
                                WHEN 'depozitoiadesi' THEN 3
                                WHEN 'kira tahsilatı' THEN 0
                                WHEN 'depozito tahsilatı' THEN 1
                                WHEN 'kira iadesi' THEN 2
                                WHEN 'depozito iadesi' THEN 3
                                ELSE trim("HareketTuru")::integer
                            END;
                        END IF;

                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = current_schema()
                              AND table_name = 'RentACarOdemeHareketleri'
                              AND column_name = 'OdemeYontemi'
                              AND data_type IN ('text', 'character varying')) THEN
                            ALTER TABLE "RentACarOdemeHareketleri"
                            ALTER COLUMN "OdemeYontemi" TYPE integer
                            USING CASE lower(trim("OdemeYontemi"))
                                WHEN 'nakit' THEN 0
                                WHEN 'kredikarti' THEN 1
                                WHEN 'kredi kartı' THEN 1
                                WHEN 'bankakarti' THEN 2
                                WHEN 'banka kartı' THEN 2
                                WHEN 'havale' THEN 3
                                WHEN 'eft' THEN 4
                                WHEN 'carimahsup' THEN 5
                                WHEN 'cari mahsup' THEN 5
                                WHEN 'diger' THEN 6
                                WHEN 'diğer' THEN 6
                                ELSE trim("OdemeYontemi")::integer
                            END;
                        END IF;
                    END $$;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    ALTER TABLE IF EXISTS "RentACarOdemeHareketleri"
                    ALTER COLUMN "HareketTuru" TYPE text USING "HareketTuru"::text;
                    ALTER TABLE IF EXISTS "RentACarOdemeHareketleri"
                    ALTER COLUMN "OdemeYontemi" TYPE text USING "OdemeYontemi"::text;
                    """);
            }
        }
    }
}

using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261012120000_NormalizeRentACarKiralamaPlani")]
public sealed class NormalizeRentACarKiralamaPlani : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = current_schema()
                          AND table_name = 'MusteriKiralamalar'
                          AND column_name = 'KiralamaPlani'
                          AND data_type IN ('text', 'character varying')) THEN
                        ALTER TABLE "MusteriKiralamalar"
                        ALTER COLUMN "KiralamaPlani" TYPE integer
                        USING CASE
                            WHEN "KiralamaPlani" IS NULL THEN 0
                            WHEN lower(trim("KiralamaPlani"::text)) = '' THEN 0
                            WHEN lower(trim("KiralamaPlani"::text)) IN ('gunluk', 'günlük') THEN 0
                            WHEN lower(trim("KiralamaPlani"::text)) = 'saatlik' THEN 1
                            ELSE trim("KiralamaPlani"::text)::integer
                        END;
                    END IF;

                    UPDATE "MusteriKiralamalar"
                    SET "KiralamaPlani" = 0
                    WHERE "KiralamaPlani" IS NULL;

                    ALTER TABLE "MusteriKiralamalar"
                    ALTER COLUMN "KiralamaPlani" SET DEFAULT 0;
                    ALTER TABLE "MusteriKiralamalar"
                    ALTER COLUMN "KiralamaPlani" SET NOT NULL;
                END $$;
                """);
            return;
        }

        if (ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            migrationBuilder.Sql("""
                UPDATE "MusteriKiralamalar"
                SET "KiralamaPlani" = CASE
                    WHEN "KiralamaPlani" IS NULL THEN '0'
                    WHEN lower(trim(CAST("KiralamaPlani" AS TEXT))) = '' THEN '0'
                    WHEN lower(trim(CAST("KiralamaPlani" AS TEXT))) IN ('gunluk', 'günlük') THEN '0'
                    WHEN lower(trim(CAST("KiralamaPlani" AS TEXT))) = 'saatlik' THEN '1'
                    ELSE trim(CAST("KiralamaPlani" AS TEXT))
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "KiralamaPlani",
                table: "MusteriKiralamalar",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "MusteriKiralamalar"
                ALTER COLUMN "KiralamaPlani" DROP NOT NULL;
                ALTER TABLE IF EXISTS "MusteriKiralamalar"
                ALTER COLUMN "KiralamaPlani" DROP DEFAULT;
                ALTER TABLE IF EXISTS "MusteriKiralamalar"
                ALTER COLUMN "KiralamaPlani" TYPE text
                USING "KiralamaPlani"::text;
                """);
            return;
        }

        if (ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            migrationBuilder.AlterColumn<string>(
                name: "KiralamaPlani",
                table: "MusteriKiralamalar",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);
        }
    }
}

using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260924230000_RentACarTeslimIadeKontrolleri")]
public sealed class RentACarTeslimIadeKontrolleri : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            migrationBuilder.Sql("""
                ALTER TABLE "MusteriKiralamalar" ADD COLUMN IF NOT EXISTS "GercekBaslangicTarihi" timestamp without time zone;
                ALTER TABLE "MusteriKiralamalar" ADD COLUMN IF NOT EXISTS "TeslimYakitSeviyesi" character varying(50);
                ALTER TABLE "MusteriKiralamalar" ADD COLUMN IF NOT EXISTS "IadeYakitSeviyesi" character varying(50);
                ALTER TABLE "MusteriKiralamalar" ADD COLUMN IF NOT EXISTS "TeslimHasarNotlari" character varying(1000);
                ALTER TABLE "MusteriKiralamalar" ADD COLUMN IF NOT EXISTS "IadeHasarNotlari" character varying(1000);
                ALTER TABLE "MusteriKiralamalar" ADD COLUMN IF NOT EXISTS "TeslimAksesuarlar" character varying(1000);
                ALTER TABLE "MusteriKiralamalar" ADD COLUMN IF NOT EXISTS "IadeAksesuarlar" character varying(1000);
                ALTER TABLE "MusteriKiralamalar" ADD COLUMN IF NOT EXISTS "TeslimNotlari" character varying(1000);
                ALTER TABLE "MusteriKiralamalar" ADD COLUMN IF NOT EXISTS "IadeNotlari" character varying(1000);
                """);
            return;
        }

        migrationBuilder.AddColumn<DateTime>(name: "GercekBaslangicTarihi", table: "MusteriKiralamalar", nullable: true);
        migrationBuilder.AddColumn<string>(name: "TeslimYakitSeviyesi", table: "MusteriKiralamalar", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<string>(name: "IadeYakitSeviyesi", table: "MusteriKiralamalar", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<string>(name: "TeslimHasarNotlari", table: "MusteriKiralamalar", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "IadeHasarNotlari", table: "MusteriKiralamalar", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "TeslimAksesuarlar", table: "MusteriKiralamalar", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "IadeAksesuarlar", table: "MusteriKiralamalar", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "TeslimNotlari", table: "MusteriKiralamalar", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "IadeNotlari", table: "MusteriKiralamalar", maxLength: 1000, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "GercekBaslangicTarihi", table: "MusteriKiralamalar");
        migrationBuilder.DropColumn(name: "TeslimYakitSeviyesi", table: "MusteriKiralamalar");
        migrationBuilder.DropColumn(name: "IadeYakitSeviyesi", table: "MusteriKiralamalar");
        migrationBuilder.DropColumn(name: "TeslimHasarNotlari", table: "MusteriKiralamalar");
        migrationBuilder.DropColumn(name: "IadeHasarNotlari", table: "MusteriKiralamalar");
        migrationBuilder.DropColumn(name: "TeslimAksesuarlar", table: "MusteriKiralamalar");
        migrationBuilder.DropColumn(name: "IadeAksesuarlar", table: "MusteriKiralamalar");
        migrationBuilder.DropColumn(name: "TeslimNotlari", table: "MusteriKiralamalar");
        migrationBuilder.DropColumn(name: "IadeNotlari", table: "MusteriKiralamalar");
    }
}

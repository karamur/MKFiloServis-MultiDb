using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations;

public partial class AddRentACarKiralamaPlani : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Older PostgreSQL installations may already have one or both compatibility
        // columns even though this migration is absent from __EFMigrationsHistory.
        // Add them independently so the migration can safely reconcile that partial
        // schema without changing existing values.
        if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"MusteriKiralamalar\" ADD COLUMN IF NOT EXISTS \"KiralamaPlani\" integer NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"MusteriKiralamalar\" ADD COLUMN IF NOT EXISTS \"SaatlikFiyat\" numeric(18,2) NOT NULL DEFAULT 0;");
            return;
        }

        migrationBuilder.AddColumn<int>(
            name: "KiralamaPlani",
            table: "MusteriKiralamalar",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            name: "SaatlikFiyat",
            table: "MusteriKiralamalar",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "KiralamaPlani",
            table: "MusteriKiralamalar");

        migrationBuilder.DropColumn(
            name: "SaatlikFiyat",
            table: "MusteriKiralamalar");
    }
}

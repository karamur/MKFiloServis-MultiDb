using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations;

public partial class AddRentACarKiralamaPlani : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

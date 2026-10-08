using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261006191000_AddFinansHareketImportUniqueKey")]
public sealed class AddFinansHareketImportUniqueKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IthalatTekillikAnahtari",
            table: "FinansHareketler",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_FinansHareketler_FirmaId_IthalatTekillikAnahtari",
            table: "FinansHareketler",
            columns: new[] { "FirmaId", "IthalatTekillikAnahtari" },
            unique: true,
            filter: "\"IsDeleted\" = false AND \"IthalatTekillikAnahtari\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FinansHareketler_FirmaId_IthalatTekillikAnahtari",
            table: "FinansHareketler");

        migrationBuilder.DropColumn(
            name: "IthalatTekillikAnahtari",
            table: "FinansHareketler");
    }
}

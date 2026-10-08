using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261006190000_AddUniqueActiveVehiclePlateIndex")]
public sealed class AddUniqueActiveVehiclePlateIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_AracPlakalar_Plaka_CikisTarihi",
            table: "AracPlakalar",
            columns: new[] { "Plaka", "CikisTarihi" },
            unique: true,
            filter: "\"CikisTarihi\" IS NULL AND \"IsDeleted\" = false");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AracPlakalar_Plaka_CikisTarihi",
            table: "AracPlakalar");
    }
}

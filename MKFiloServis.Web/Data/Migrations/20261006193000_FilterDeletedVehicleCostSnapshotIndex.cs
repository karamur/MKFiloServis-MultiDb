using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261006193000_FilterDeletedVehicleCostSnapshotIndex")]
public sealed class FilterDeletedVehicleCostSnapshotIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AracMaliyetSnapshot_Arac_Donem",
            table: "AracMaliyetSnapshotlari");

        migrationBuilder.CreateIndex(
            name: "IX_AracMaliyetSnapshot_Arac_Donem",
            table: "AracMaliyetSnapshotlari",
            columns: new[] { "AracId", "Yil", "Ay" },
            unique: true,
            filter: "\"IsDeleted\" = false");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AracMaliyetSnapshot_Arac_Donem",
            table: "AracMaliyetSnapshotlari");

        migrationBuilder.CreateIndex(
            name: "IX_AracMaliyetSnapshot_Arac_Donem",
            table: "AracMaliyetSnapshotlari",
            columns: new[] { "AracId", "Yil", "Ay" },
            unique: true);
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261006192000_AddUniqueMonthlyPayrollSnapshotIndex")]
public sealed class AddUniqueMonthlyPayrollSnapshotIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MaasOdemeSnapshotlar_FirmaId_Yil_Ay",
            table: "MaasOdemeSnapshotlar");

        migrationBuilder.CreateIndex(
            name: "IX_MaasOdemeSnapshotlar_FirmaId_Yil_Ay_PersonelId",
            table: "MaasOdemeSnapshotlar",
            columns: new[] { "FirmaId", "Yil", "Ay", "PersonelId" },
            unique: true,
            filter: "\"IsDeleted\" = false");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MaasOdemeSnapshotlar_FirmaId_Yil_Ay_PersonelId",
            table: "MaasOdemeSnapshotlar");

        migrationBuilder.CreateIndex(
            name: "IX_MaasOdemeSnapshotlar_FirmaId_Yil_Ay",
            table: "MaasOdemeSnapshotlar",
            columns: new[] { "FirmaId", "Yil", "Ay" });
    }
}

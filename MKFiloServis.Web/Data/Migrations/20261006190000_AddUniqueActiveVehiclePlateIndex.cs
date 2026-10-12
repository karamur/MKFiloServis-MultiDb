using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261006190000_AddUniqueActiveVehiclePlateIndex")]
public sealed class AddUniqueActiveVehiclePlateIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            // Older schemas can already have this exact uniqueness rule while the
            // migration history is behind. Avoid failing startup on the duplicate
            // object; duplicate active plates are checked before migrations run.
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_AracPlakalar_Plaka_CikisTarihi\" ON \"AracPlakalar\" (\"Plaka\", \"CikisTarihi\") WHERE \"CikisTarihi\" IS NULL AND \"IsDeleted\" = false;");
            return;
        }

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

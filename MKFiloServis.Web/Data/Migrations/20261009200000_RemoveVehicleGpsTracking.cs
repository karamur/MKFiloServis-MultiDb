using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261009200000_RemoveVehicleGpsTracking")]
public sealed class RemoveVehicleGpsTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Dependent tables first; EF chooses provider-specific quoting and SQL.
        migrationBuilder.DropTable(name: "AracBolgeAtamalar");
        migrationBuilder.DropTable(name: "AracKonumlar");
        migrationBuilder.DropTable(name: "AracTakipAlarmlar");
        migrationBuilder.DropTable(name: "AracTakipCihazlar");
        migrationBuilder.DropTable(name: "AracBolgeler");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "Araç GPS tabloları ve içerdikleri konum verileri kaldırılmıştır; otomatik geri yükleme desteklenmez.");
    }
}

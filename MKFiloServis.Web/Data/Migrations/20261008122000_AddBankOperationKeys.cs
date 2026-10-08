using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261008122000_AddBankOperationKeys")]
public sealed class AddBankOperationKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "IslemKimligi", table: "BankaKasaHareketleri", maxLength: 32, nullable: true);
        migrationBuilder.AddColumn<string>(name: "IslemOzeti", table: "BankaKasaHareketleri", maxLength: 64, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_BankaKasaHareketleri_IslemKimligi", table: "BankaKasaHareketleri", column: "IslemKimligi", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_BankaKasaHareketleri_IslemKimligi", table: "BankaKasaHareketleri");
        migrationBuilder.DropColumn(name: "IslemKimligi", table: "BankaKasaHareketleri");
        migrationBuilder.DropColumn(name: "IslemOzeti", table: "BankaKasaHareketleri");
    }
}

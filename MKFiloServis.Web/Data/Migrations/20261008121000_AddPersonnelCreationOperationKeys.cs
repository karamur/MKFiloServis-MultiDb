using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261008121000_AddPersonnelCreationOperationKeys")]
public sealed class AddPersonnelCreationOperationKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "PersonelAvanslar", "PersonelBorclar" })
        {
            migrationBuilder.AddColumn<string>(name: "IslemKimligi", table: table,
                maxLength: 32, nullable: true);
            migrationBuilder.AddColumn<string>(name: "IslemOzeti", table: table,
                maxLength: 64, nullable: true);
            // Silinmiş kayıtlar da anahtarı tüketmiş olarak kalır; yeniden ödeme üretilemez.
            migrationBuilder.CreateIndex(name: $"IX_{table}_IslemKimligi", table: table,
                column: "IslemKimligi", unique: true);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "PersonelAvanslar", "PersonelBorclar" })
        {
            migrationBuilder.DropIndex(name: $"IX_{table}_IslemKimligi", table: table);
            migrationBuilder.DropColumn(name: "IslemKimligi", table: table);
            migrationBuilder.DropColumn(name: "IslemOzeti", table: table);
        }
    }
}

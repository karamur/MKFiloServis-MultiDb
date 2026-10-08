using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261006194000_AddUniqueDefaultInvoiceTemplateIndexes")]
public sealed class AddUniqueDefaultInvoiceTemplateIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_FaturaSablonlari_FirmaId_Varsayilan",
            table: "FaturaSablonlari",
            column: "FirmaId",
            unique: true,
            filter: "\"IsDeleted\" = false AND \"Varsayilan\" = true");

        migrationBuilder.CreateIndex(
            name: "IX_FaturaGrupSablonlari_FirmaId_FirmaVarsayilan",
            table: "FaturaGrupSablonlari",
            column: "FirmaId",
            unique: true,
            filter: "\"IsDeleted\" = false AND \"VarsayilanMi\" = true AND \"KullaniciId\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_FaturaGrupSablonlari_FirmaId_KullaniciId_Varsayilan",
            table: "FaturaGrupSablonlari",
            columns: new[] { "FirmaId", "KullaniciId" },
            unique: true,
            filter: "\"IsDeleted\" = false AND \"VarsayilanMi\" = true AND \"KullaniciId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FaturaGrupSablonlari_FirmaId_FirmaVarsayilan",
            table: "FaturaGrupSablonlari");

        migrationBuilder.DropIndex(
            name: "IX_FaturaGrupSablonlari_FirmaId_KullaniciId_Varsayilan",
            table: "FaturaGrupSablonlari");

        migrationBuilder.DropIndex(
            name: "IX_FaturaSablonlari_FirmaId_Varsayilan",
            table: "FaturaSablonlari");
    }
}

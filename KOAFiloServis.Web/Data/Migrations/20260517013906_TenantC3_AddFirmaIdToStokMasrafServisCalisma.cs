using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantC3_AddFirmaIdToStokMasrafServisCalisma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "StokKategoriler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "StokKartlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "StokHareketler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "ServisCalismalari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "MasrafKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StokKategoriler_FirmaId",
                table: "StokKategoriler",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_StokKartlari_FirmaId",
                table: "StokKartlari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketler_FirmaId",
                table: "StokHareketler",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisCalismalari_FirmaId",
                table: "ServisCalismalari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_MasrafKalemleri_FirmaId",
                table: "MasrafKalemleri",
                column: "FirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MasrafKalemleri_Firmalar_FirmaId",
                table: "MasrafKalemleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServisCalismalari_Firmalar_FirmaId",
                table: "ServisCalismalari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StokHareketler_Firmalar_FirmaId",
                table: "StokHareketler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StokKartlari_Firmalar_FirmaId",
                table: "StokKartlari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StokKategoriler_Firmalar_FirmaId",
                table: "StokKategoriler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MasrafKalemleri_Firmalar_FirmaId",
                table: "MasrafKalemleri");

            migrationBuilder.DropForeignKey(
                name: "FK_ServisCalismalari_Firmalar_FirmaId",
                table: "ServisCalismalari");

            migrationBuilder.DropForeignKey(
                name: "FK_StokHareketler_Firmalar_FirmaId",
                table: "StokHareketler");

            migrationBuilder.DropForeignKey(
                name: "FK_StokKartlari_Firmalar_FirmaId",
                table: "StokKartlari");

            migrationBuilder.DropForeignKey(
                name: "FK_StokKategoriler_Firmalar_FirmaId",
                table: "StokKategoriler");

            migrationBuilder.DropIndex(
                name: "IX_StokKategoriler_FirmaId",
                table: "StokKategoriler");

            migrationBuilder.DropIndex(
                name: "IX_StokKartlari_FirmaId",
                table: "StokKartlari");

            migrationBuilder.DropIndex(
                name: "IX_StokHareketler_FirmaId",
                table: "StokHareketler");

            migrationBuilder.DropIndex(
                name: "IX_ServisCalismalari_FirmaId",
                table: "ServisCalismalari");

            migrationBuilder.DropIndex(
                name: "IX_MasrafKalemleri_FirmaId",
                table: "MasrafKalemleri");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "StokKategoriler");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "StokKartlari");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "StokHareketler");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "ServisCalismalari");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "MasrafKalemleri");
        }
    }
}

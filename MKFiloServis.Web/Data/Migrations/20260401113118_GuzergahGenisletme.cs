using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class GuzergahGenisletme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FaturaKalemId",
                table: "Guzergahlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "Guzergahlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonelSayisi",
                table: "Guzergahlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeferTipi",
                table: "Guzergahlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VarsayilanAracId",
                table: "Guzergahlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VarsayilanSoforId",
                table: "Guzergahlar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guzergahlar_FirmaId",
                table: "Guzergahlar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Guzergahlar_VarsayilanAracId",
                table: "Guzergahlar",
                column: "VarsayilanAracId");

            migrationBuilder.CreateIndex(
                name: "IX_Guzergahlar_VarsayilanSoforId",
                table: "Guzergahlar",
                column: "VarsayilanSoforId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Araclar_VarsayilanAracId",
                table: "Guzergahlar",
                column: "VarsayilanAracId",
                principalTable: "Araclar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Firmalar_FirmaId",
                table: "Guzergahlar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Soforler_VarsayilanSoforId",
                table: "Guzergahlar",
                column: "VarsayilanSoforId",
                principalTable: "Soforler",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guzergahlar_Araclar_VarsayilanAracId",
                table: "Guzergahlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Guzergahlar_Firmalar_FirmaId",
                table: "Guzergahlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Guzergahlar_Soforler_VarsayilanSoforId",
                table: "Guzergahlar");

            migrationBuilder.DropIndex(
                name: "IX_Guzergahlar_FirmaId",
                table: "Guzergahlar");

            migrationBuilder.DropIndex(
                name: "IX_Guzergahlar_VarsayilanAracId",
                table: "Guzergahlar");

            migrationBuilder.DropIndex(
                name: "IX_Guzergahlar_VarsayilanSoforId",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "FaturaKalemId",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "PersonelSayisi",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "SeferTipi",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "VarsayilanAracId",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "VarsayilanSoforId",
                table: "Guzergahlar");
        }
    }
}



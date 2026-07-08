using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class KurumFirmaId_ToCariler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiloGunlukPuantajlar_Kurumlar_KurumFirmaId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Kurumlar_KurumFirmaId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.AddForeignKey(
                name: "FK_FiloGunlukPuantajlar_Cariler_KurumFirmaId",
                table: "FiloGunlukPuantajlar",
                column: "KurumFirmaId",
                principalTable: "Cariler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Cariler_KurumFirmaId",
                table: "FiloGuzergahEslestirmeleri",
                column: "KurumFirmaId",
                principalTable: "Cariler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiloGunlukPuantajlar_Cariler_KurumFirmaId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Cariler_KurumFirmaId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.AddForeignKey(
                name: "FK_FiloGunlukPuantajlar_Kurumlar_KurumFirmaId",
                table: "FiloGunlukPuantajlar",
                column: "KurumFirmaId",
                principalTable: "Kurumlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Kurumlar_KurumFirmaId",
                table: "FiloGuzergahEslestirmeleri",
                column: "KurumFirmaId",
                principalTable: "Kurumlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

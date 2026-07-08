using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class KurumFirmaFK_ToKurumlar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite does not enforce FK constraints by default.
            // KurumFirmaId column stays int; model snapshot now points to Kurumlar table.
            // Re-create indexes so EF model stays consistent.
            migrationBuilder.DropIndex(
                name: "IX_FiloGuzergahEslestirmeleri_KurumFirmaId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropIndex(
                name: "IX_FiloGunlukPuantajlar_KurumFirmaId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGuzergahEslestirmeleri_KurumFirmaId",
                table: "FiloGuzergahEslestirmeleri",
                column: "KurumFirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGunlukPuantajlar_KurumFirmaId",
                table: "FiloGunlukPuantajlar",
                column: "KurumFirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FiloGuzergahEslestirmeleri_KurumFirmaId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropIndex(
                name: "IX_FiloGunlukPuantajlar_KurumFirmaId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGuzergahEslestirmeleri_KurumFirmaId",
                table: "FiloGuzergahEslestirmeleri",
                column: "KurumFirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGunlukPuantajlar_KurumFirmaId",
                table: "FiloGunlukPuantajlar",
                column: "KurumFirmaId");
        }
    }
}

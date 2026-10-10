using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHakedisPuantajFaturaFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddMissingTables introduces both nullable columns earlier in the ordered
            // migration chain. Repeating the guarded PostgreSQL-only ALTER TABLE here
            // broke SQLite upgrades and made a normal non-idempotent chain look partial.

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_GelirFaturaId",
                table: "HakedisPuantajlar",
                column: "GelirFaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_GiderFaturaId",
                table: "HakedisPuantajlar",
                column: "GiderFaturaId");

            migrationBuilder.AddForeignKey(
                name: "FK_HakedisPuantajlar_Faturalar_GelirFaturaId",
                table: "HakedisPuantajlar",
                column: "GelirFaturaId",
                principalTable: "Faturalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HakedisPuantajlar_Faturalar_GiderFaturaId",
                table: "HakedisPuantajlar",
                column: "GiderFaturaId",
                principalTable: "Faturalar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HakedisPuantajlar_Faturalar_GelirFaturaId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_HakedisPuantajlar_Faturalar_GiderFaturaId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_GelirFaturaId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_GiderFaturaId",
                table: "HakedisPuantajlar");
        }
    }
}



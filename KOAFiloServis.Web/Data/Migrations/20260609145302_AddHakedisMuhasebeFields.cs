using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHakedisMuhasebeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GelirFisId",
                table: "HakedisPuantajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GiderFisId",
                table: "HakedisPuantajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MuhasebeyeAktarildiMi",
                table: "HakedisPuantajlar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_GelirFisId",
                table: "HakedisPuantajlar",
                column: "GelirFisId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_GiderFisId",
                table: "HakedisPuantajlar",
                column: "GiderFisId");

            migrationBuilder.AddForeignKey(
                name: "FK_HakedisPuantajlar_MuhasebeFisleri_GelirFisId",
                table: "HakedisPuantajlar",
                column: "GelirFisId",
                principalTable: "MuhasebeFisleri",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HakedisPuantajlar_MuhasebeFisleri_GiderFisId",
                table: "HakedisPuantajlar",
                column: "GiderFisId",
                principalTable: "MuhasebeFisleri",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HakedisPuantajlar_MuhasebeFisleri_GelirFisId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_HakedisPuantajlar_MuhasebeFisleri_GiderFisId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_GelirFisId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_GiderFisId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropColumn(
                name: "GelirFisId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropColumn(
                name: "GiderFisId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropColumn(
                name: "MuhasebeyeAktarildiMi",
                table: "HakedisPuantajlar");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class BankaKasaHareket_MuhasebeFisId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MuhasebeFisId",
                table: "BankaKasaHareketleri",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_MuhasebeFisId",
                table: "BankaKasaHareketleri",
                column: "MuhasebeFisId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankaKasaHareketleri_MuhasebeFisleri_MuhasebeFisId",
                table: "BankaKasaHareketleri",
                column: "MuhasebeFisId",
                principalTable: "MuhasebeFisleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankaKasaHareketleri_MuhasebeFisleri_MuhasebeFisId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_MuhasebeFisId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "MuhasebeFisId",
                table: "BankaKasaHareketleri");
        }
    }
}



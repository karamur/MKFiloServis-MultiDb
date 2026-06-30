using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAracIdToBankaKasaHareket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AracId",
                table: "BankaKasaHareketleri",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_AracId",
                table: "BankaKasaHareketleri",
                column: "AracId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankaKasaHareketleri_Araclar_AracId",
                table: "BankaKasaHareketleri",
                column: "AracId",
                principalTable: "Araclar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankaKasaHareketleri_Araclar_AracId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_AracId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "AracId",
                table: "BankaKasaHareketleri");
        }
    }
}



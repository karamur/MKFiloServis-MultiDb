using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CariPersonelSoforIliski : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KullaniciCariler_KullaniciId_CariId",
                table: "KullaniciCariler");

            migrationBuilder.AddColumn<int>(
                name: "SoforId",
                table: "Cariler",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciCariler_KullaniciId_CariId",
                table: "KullaniciCariler",
                columns: new[] { "KullaniciId", "CariId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_SoforId",
                table: "Cariler",
                column: "SoforId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_Soforler_SoforId",
                table: "Cariler",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cariler_Soforler_SoforId",
                table: "Cariler");

            migrationBuilder.DropIndex(
                name: "IX_KullaniciCariler_KullaniciId_CariId",
                table: "KullaniciCariler");

            migrationBuilder.DropIndex(
                name: "IX_Cariler_SoforId",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "SoforId",
                table: "Cariler");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciCariler_KullaniciId_CariId",
                table: "KullaniciCariler",
                columns: new[] { "KullaniciId", "CariId" },
                unique: true);
        }
    }
}



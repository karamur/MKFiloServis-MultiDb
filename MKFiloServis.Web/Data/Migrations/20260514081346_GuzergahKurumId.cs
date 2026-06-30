using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class GuzergahKurumId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KurumId",
                table: "Guzergahlar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guzergahlar_KurumId",
                table: "Guzergahlar",
                column: "KurumId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Kurumlar_KurumId",
                table: "Guzergahlar",
                column: "KurumId",
                principalTable: "Kurumlar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guzergahlar_Kurumlar_KurumId",
                table: "Guzergahlar");

            migrationBuilder.DropIndex(
                name: "IX_Guzergahlar_KurumId",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "KurumId",
                table: "Guzergahlar");
        }
    }
}



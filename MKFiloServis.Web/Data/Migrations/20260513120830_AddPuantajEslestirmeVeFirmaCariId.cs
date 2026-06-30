using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPuantajEslestirmeVeFirmaCariId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CariId",
                table: "Firmalar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Firmalar_CariId",
                table: "Firmalar",
                column: "CariId");

            migrationBuilder.AddForeignKey(
                name: "FK_Firmalar_Cariler_CariId",
                table: "Firmalar",
                column: "CariId",
                principalTable: "Cariler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Firmalar_Cariler_CariId",
                table: "Firmalar");

            migrationBuilder.DropIndex(
                name: "IX_Firmalar_CariId",
                table: "Firmalar");

            migrationBuilder.DropColumn(
                name: "CariId",
                table: "Firmalar");
        }
    }
}



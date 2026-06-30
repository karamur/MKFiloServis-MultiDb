using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFirmalarArasiFatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FirmalarArasiFatura",
                table: "Faturalar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "KarsiFirmaId",
                table: "Faturalar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_KarsiFirmaId",
                table: "Faturalar",
                column: "KarsiFirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Firmalar_KarsiFirmaId",
                table: "Faturalar",
                column: "KarsiFirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_Firmalar_KarsiFirmaId",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_KarsiFirmaId",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "FirmalarArasiFatura",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "KarsiFirmaId",
                table: "Faturalar");
        }
    }
}



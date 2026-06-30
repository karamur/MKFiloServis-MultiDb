using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantB1_AddFirmaIdToCariSeferUcreti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CariSeferUcretleri_Sirketler_SirketId",
                table: "CariSeferUcretleri");

            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "CariSeferUcretleri",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CariSeferUcretleri_FirmaId",
                table: "CariSeferUcretleri",
                column: "FirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_CariSeferUcretleri_Firmalar_FirmaId",
                table: "CariSeferUcretleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CariSeferUcretleri_Sirketler_SirketId",
                table: "CariSeferUcretleri",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CariSeferUcretleri_Firmalar_FirmaId",
                table: "CariSeferUcretleri");

            migrationBuilder.DropForeignKey(
                name: "FK_CariSeferUcretleri_Sirketler_SirketId",
                table: "CariSeferUcretleri");

            migrationBuilder.DropIndex(
                name: "IX_CariSeferUcretleri_FirmaId",
                table: "CariSeferUcretleri");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "CariSeferUcretleri");

            migrationBuilder.AddForeignKey(
                name: "FK_CariSeferUcretleri_Sirketler_SirketId",
                table: "CariSeferUcretleri",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}



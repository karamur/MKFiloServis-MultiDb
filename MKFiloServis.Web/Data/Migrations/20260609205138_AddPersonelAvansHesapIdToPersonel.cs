using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonelAvansHesapIdToPersonel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonelAvansHesapId",
                table: "Personeller",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personeller_PersonelAvansHesapId",
                table: "Personeller",
                column: "PersonelAvansHesapId");

            migrationBuilder.AddForeignKey(
                name: "FK_Personeller_MuhasebeHesaplari_PersonelAvansHesapId",
                table: "Personeller",
                column: "PersonelAvansHesapId",
                principalTable: "MuhasebeHesaplari",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Personeller_MuhasebeHesaplari_PersonelAvansHesapId",
                table: "Personeller");

            migrationBuilder.DropIndex(
                name: "IX_Personeller_PersonelAvansHesapId",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "PersonelAvansHesapId",
                table: "Personeller");
        }
    }
}



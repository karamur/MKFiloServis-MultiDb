using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonelAvansHesap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cariler_MuhasebeHesaplari_MuhasebeHesapId",
                table: "Cariler");

            migrationBuilder.AddColumn<string>(
                name: "PersonelAvansPrefix",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PersonelAvansHesapId",
                table: "Cariler",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_PersonelAvansHesapId",
                table: "Cariler",
                column: "PersonelAvansHesapId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_MuhasebeHesaplari_MuhasebeHesapId",
                table: "Cariler",
                column: "MuhasebeHesapId",
                principalTable: "MuhasebeHesaplari",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_MuhasebeHesaplari_PersonelAvansHesapId",
                table: "Cariler",
                column: "PersonelAvansHesapId",
                principalTable: "MuhasebeHesaplari",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cariler_MuhasebeHesaplari_MuhasebeHesapId",
                table: "Cariler");

            migrationBuilder.DropForeignKey(
                name: "FK_Cariler_MuhasebeHesaplari_PersonelAvansHesapId",
                table: "Cariler");

            migrationBuilder.DropIndex(
                name: "IX_Cariler_PersonelAvansHesapId",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "PersonelAvansPrefix",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "PersonelAvansHesapId",
                table: "Cariler");

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_MuhasebeHesaplari_MuhasebeHesapId",
                table: "Cariler",
                column: "MuhasebeHesapId",
                principalTable: "MuhasebeHesaplari",
                principalColumn: "Id");
        }
    }
}



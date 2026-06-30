using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBakimPeriyot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DigerYukumlulukHesapId",
                table: "PersonelFinansAyarlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OdenecekSgkHesapId",
                table: "PersonelFinansAyarlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OdenecekVergiHesapId",
                table: "PersonelFinansAyarlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonelGiderHesapId",
                table: "PersonelFinansAyarlar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonelFinansAyarlar_DigerYukumlulukHesapId",
                table: "PersonelFinansAyarlar",
                column: "DigerYukumlulukHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelFinansAyarlar_OdenecekSgkHesapId",
                table: "PersonelFinansAyarlar",
                column: "OdenecekSgkHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelFinansAyarlar_OdenecekVergiHesapId",
                table: "PersonelFinansAyarlar",
                column: "OdenecekVergiHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelFinansAyarlar_PersonelGiderHesapId",
                table: "PersonelFinansAyarlar",
                column: "PersonelGiderHesapId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_DigerYukumlulukHesa~",
                table: "PersonelFinansAyarlar",
                column: "DigerYukumlulukHesapId",
                principalTable: "MuhasebeHesaplari",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_OdenecekSgkHesapId",
                table: "PersonelFinansAyarlar",
                column: "OdenecekSgkHesapId",
                principalTable: "MuhasebeHesaplari",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_OdenecekVergiHesapId",
                table: "PersonelFinansAyarlar",
                column: "OdenecekVergiHesapId",
                principalTable: "MuhasebeHesaplari",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_PersonelGiderHesapId",
                table: "PersonelFinansAyarlar",
                column: "PersonelGiderHesapId",
                principalTable: "MuhasebeHesaplari",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_DigerYukumlulukHesa~",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_OdenecekSgkHesapId",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_OdenecekVergiHesapId",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_PersonelGiderHesapId",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropIndex(
                name: "IX_PersonelFinansAyarlar_DigerYukumlulukHesapId",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropIndex(
                name: "IX_PersonelFinansAyarlar_OdenecekSgkHesapId",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropIndex(
                name: "IX_PersonelFinansAyarlar_OdenecekVergiHesapId",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropIndex(
                name: "IX_PersonelFinansAyarlar_PersonelGiderHesapId",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropColumn(
                name: "DigerYukumlulukHesapId",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropColumn(
                name: "OdenecekSgkHesapId",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropColumn(
                name: "OdenecekVergiHesapId",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropColumn(
                name: "PersonelGiderHesapId",
                table: "PersonelFinansAyarlar");
        }
    }
}



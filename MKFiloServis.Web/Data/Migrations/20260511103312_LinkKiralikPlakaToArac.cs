using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkKiralikPlakaToArac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "FaturaOdemesi",
                table: "KiralikPlakaTakipler",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "AylikVeyaYillikTutar",
                table: "KiralikPlakaTakipler",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "AracId",
                table: "KiralikPlakaTakipler",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KiralikPlakaTakipler_AracId",
                table: "KiralikPlakaTakipler",
                column: "AracId");

            migrationBuilder.AddForeignKey(
                name: "FK_KiralikPlakaTakipler_Araclar_AracId",
                table: "KiralikPlakaTakipler",
                column: "AracId",
                principalTable: "Araclar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KiralikPlakaTakipler_Araclar_AracId",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropIndex(
                name: "IX_KiralikPlakaTakipler_AracId",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "AracId",
                table: "KiralikPlakaTakipler");

            migrationBuilder.AlterColumn<decimal>(
                name: "FaturaOdemesi",
                table: "KiralikPlakaTakipler",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "AylikVeyaYillikTutar",
                table: "KiralikPlakaTakipler",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}



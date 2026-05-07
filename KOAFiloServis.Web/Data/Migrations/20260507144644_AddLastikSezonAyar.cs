using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLastikSezonAyar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KalemTipi",
                table: "ServisParcalar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvOrani",
                table: "ServisParcalar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "StogaKaydet",
                table: "ServisParcalar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "KdvManuelMi",
                table: "ServisKayitlari",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KalemTipi",
                table: "ServisParcalar");

            migrationBuilder.DropColumn(
                name: "KdvOrani",
                table: "ServisParcalar");

            migrationBuilder.DropColumn(
                name: "StogaKaydet",
                table: "ServisParcalar");

            migrationBuilder.DropColumn(
                name: "KdvManuelMi",
                table: "ServisKayitlari");
        }
    }
}

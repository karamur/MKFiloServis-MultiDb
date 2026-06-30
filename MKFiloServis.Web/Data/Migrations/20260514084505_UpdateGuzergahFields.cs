using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGuzergahFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GelirFiyati",
                table: "Guzergahlar",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GiderFiyati",
                table: "Guzergahlar",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YonBilgisi",
                table: "Guzergahlar",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GelirFiyati",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "GiderFiyati",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "YonBilgisi",
                table: "Guzergahlar");
        }
    }
}



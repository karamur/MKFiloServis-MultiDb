using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CariAlanGenisletme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Il",
                table: "Cariler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ilce",
                table: "Cariler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostaKodu",
                table: "Cariler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefon2",
                table: "Cariler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fax",
                table: "Cariler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebSitesi",
                table: "Cariler",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Il",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "Ilce",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "PostaKodu",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "Telefon2",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "Fax",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "WebSitesi",
                table: "Cariler");
        }
    }
}



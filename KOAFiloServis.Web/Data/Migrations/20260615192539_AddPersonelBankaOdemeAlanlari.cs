using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonelBankaOdemeAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankaHesapNo",
                table: "Personeller",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankaSube",
                table: "Personeller",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankaSubeKodu",
                table: "Personeller",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaasOdemeTipi",
                table: "Personeller",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankaHesapNo",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "BankaSube",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "BankaSubeKodu",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "MaasOdemeTipi",
                table: "Personeller");
        }
    }
}

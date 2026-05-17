using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantF1_AddKopyalanabilirTenantAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KaynakFirmaId",
                table: "Personeller",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakKayitId",
                table: "Personeller",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakFirmaId",
                table: "Kurumlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakKayitId",
                table: "Kurumlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakFirmaId",
                table: "Guzergahlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakKayitId",
                table: "Guzergahlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakFirmaId",
                table: "Cariler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakKayitId",
                table: "Cariler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakFirmaId",
                table: "Araclar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakKayitId",
                table: "Araclar",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KaynakFirmaId",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "KaynakKayitId",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "KaynakFirmaId",
                table: "Kurumlar");

            migrationBuilder.DropColumn(
                name: "KaynakKayitId",
                table: "Kurumlar");

            migrationBuilder.DropColumn(
                name: "KaynakFirmaId",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "KaynakKayitId",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "KaynakFirmaId",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "KaynakKayitId",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "KaynakFirmaId",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "KaynakKayitId",
                table: "Araclar");
        }
    }
}

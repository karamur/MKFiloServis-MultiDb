using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailInboxSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GelenKlasoru",
                table: "EmailAyarlari",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "GelenKutusuAktif",
                table: "EmailAyarlari",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ImapPort",
                table: "EmailAyarlari",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ImapSslKullan",
                table: "EmailAyarlari",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ImapSunucu",
                table: "EmailAyarlari",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GelenKlasoru",
                table: "EmailAyarlari");

            migrationBuilder.DropColumn(
                name: "GelenKutusuAktif",
                table: "EmailAyarlari");

            migrationBuilder.DropColumn(
                name: "ImapPort",
                table: "EmailAyarlari");

            migrationBuilder.DropColumn(
                name: "ImapSslKullan",
                table: "EmailAyarlari");

            migrationBuilder.DropColumn(
                name: "ImapSunucu",
                table: "EmailAyarlari");
        }
    }
}



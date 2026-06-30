using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonelBelgeTarihleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdliSicilGecerlilikTarihi",
                table: "Personeller",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KimlikGecerlilikTarihi",
                table: "Personeller",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuruculCezaBarkodluBelgeTarihi",
                table: "Personeller",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdliSicilGecerlilikTarihi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "KimlikGecerlilikTarihi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "SuruculCezaBarkodluBelgeTarihi",
                table: "Personeller");
        }
    }
}



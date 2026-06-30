using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKoltukSigortasiTarihlerine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "KoltukSigortasiBaslangiçTarihi",
                table: "Araclar",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KoltukSigortasiBitisTarihi",
                table: "Araclar",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KoltukSigortasiBaslangiçTarihi",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "KoltukSigortasiBitisTarihi",
                table: "Araclar");
        }
    }
}



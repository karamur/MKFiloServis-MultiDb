using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGecerlilikTarihiToEvrakDosya : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GecerlilikTarihi",
                table: "EvrakDosyalari",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GecerlilikTarihi",
                table: "EvrakDosyalari");
        }
    }
}

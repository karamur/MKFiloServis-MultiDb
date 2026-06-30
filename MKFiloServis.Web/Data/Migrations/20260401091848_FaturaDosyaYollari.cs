using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class FaturaDosyaYollari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfDosyaYolu",
                table: "Faturalar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XmlDosyaYolu",
                table: "Faturalar",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfDosyaYolu",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "XmlDosyaYolu",
                table: "Faturalar");
        }
    }
}



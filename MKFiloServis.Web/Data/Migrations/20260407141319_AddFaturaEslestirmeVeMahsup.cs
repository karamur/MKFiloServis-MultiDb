using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaturaEslestirmeVeMahsup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EslesenFaturaId",
                table: "Faturalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MahsupKapatildi",
                table: "Faturalar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MahsupTarihi",
                table: "Faturalar",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_EslesenFaturaId",
                table: "Faturalar",
                column: "EslesenFaturaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Faturalar_EslesenFaturaId",
                table: "Faturalar",
                column: "EslesenFaturaId",
                principalTable: "Faturalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_Faturalar_EslesenFaturaId",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_EslesenFaturaId",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "EslesenFaturaId",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "MahsupKapatildi",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "MahsupTarihi",
                table: "Faturalar");
        }
    }
}



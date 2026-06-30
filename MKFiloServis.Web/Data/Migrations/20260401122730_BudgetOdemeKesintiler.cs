using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class BudgetOdemeKesintiler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CezaKesintisi",
                table: "BudgetOdemeler",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DigerKesinti",
                table: "BudgetOdemeler",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "KesintiAciklamasi",
                table: "BudgetOdemeler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MasrafKesintisi",
                table: "BudgetOdemeler",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "MahsupGrupId",
                table: "BankaKasaHareketleri",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MahsupHareketId",
                table: "BankaKasaHareketleri",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CezaKesintisi",
                table: "BudgetOdemeler");

            migrationBuilder.DropColumn(
                name: "DigerKesinti",
                table: "BudgetOdemeler");

            migrationBuilder.DropColumn(
                name: "KesintiAciklamasi",
                table: "BudgetOdemeler");

            migrationBuilder.DropColumn(
                name: "MasrafKesintisi",
                table: "BudgetOdemeler");

            migrationBuilder.DropColumn(
                name: "MahsupGrupId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "MahsupHareketId",
                table: "BankaKasaHareketleri");
        }
    }
}



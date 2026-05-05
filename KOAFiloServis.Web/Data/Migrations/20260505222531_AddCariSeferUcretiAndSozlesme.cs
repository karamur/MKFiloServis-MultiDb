using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCariSeferUcretiAndSozlesme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SozlesmeBaslangicTarihi",
                table: "Cariler",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SozlesmeBitisTarihi",
                table: "Cariler",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SozlesmeNo",
                table: "Cariler",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CariSeferUcretleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    CariId = table.Column<int>(type: "integer", nullable: false),
                    GuzergahId = table.Column<int>(type: "integer", nullable: true),
                    Tanim = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SeferUcreti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GecerlilikBaslangic = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GecerlilikBitis = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariSeferUcretleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CariSeferUcretleri_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CariSeferUcretleri_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CariSeferUcretleri_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CariSeferUcretleri_CariId_GuzergahId_GecerlilikBaslangic",
                table: "CariSeferUcretleri",
                columns: new[] { "CariId", "GuzergahId", "GecerlilikBaslangic" });

            migrationBuilder.CreateIndex(
                name: "IX_CariSeferUcretleri_GuzergahId",
                table: "CariSeferUcretleri",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_CariSeferUcretleri_SirketId",
                table: "CariSeferUcretleri",
                column: "SirketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CariSeferUcretleri");

            migrationBuilder.DropColumn(
                name: "SozlesmeBaslangicTarihi",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "SozlesmeBitisTarihi",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "SozlesmeNo",
                table: "Cariler");
        }
    }
}

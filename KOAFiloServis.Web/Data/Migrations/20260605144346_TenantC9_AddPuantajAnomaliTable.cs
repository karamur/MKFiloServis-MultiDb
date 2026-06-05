using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantC9_AddPuantajAnomaliTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PuantajAnomaliler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnomaliTipi = table.Column<int>(type: "integer", nullable: false),
                    TespitYontemi = table.Column<int>(type: "integer", nullable: false),
                    OnemSeviyesi = table.Column<int>(type: "integer", nullable: false),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    PuantajKayitId = table.Column<int>(type: "integer", nullable: true),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    Baslik = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AnomaliDetay = table.Column<string>(type: "jsonb", nullable: true),
                    Oneri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GuvenSkoru = table.Column<int>(type: "integer", nullable: true),
                    TespitTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CozumDurumu = table.Column<int>(type: "integer", nullable: false),
                    CozumTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CozenKullanici = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CozumAciklamasi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajAnomaliler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuantajAnomaliler_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PuantajAnomaliler_PuantajKayitlar_PuantajKayitId",
                        column: x => x.PuantajKayitId,
                        principalTable: "PuantajKayitlar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajAnomaliler_FirmaId",
                table: "PuantajAnomaliler",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajAnomaliler_PuantajKayitId",
                table: "PuantajAnomaliler",
                column: "PuantajKayitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PuantajAnomaliler");
        }
    }
}

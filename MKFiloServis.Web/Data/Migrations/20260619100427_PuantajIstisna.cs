using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PuantajIstisna : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CezaTutar",
                table: "PuantajFaturaHazirlikSatirlar",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EkGelir",
                table: "PuantajFaturaHazirlikSatirlar",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EkGider",
                table: "PuantajFaturaHazirlikSatirlar",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MasrafTutar",
                table: "PuantajFaturaHazirlikSatirlar",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PuantajIstisnalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    PuantajKayitId = table.Column<int>(type: "integer", nullable: false),
                    OperasyonKaydiId = table.Column<int>(type: "integer", nullable: true),
                    Gun = table.Column<int>(type: "integer", nullable: false),
                    IstisnaTipi = table.Column<int>(type: "integer", nullable: false),
                    KararTipi = table.Column<int>(type: "integer", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EskiAracId = table.Column<int>(type: "integer", nullable: true),
                    YeniAracId = table.Column<int>(type: "integer", nullable: true),
                    FisNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajIstisnalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuantajIstisnalar_Araclar_EskiAracId",
                        column: x => x.EskiAracId,
                        principalTable: "Araclar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PuantajIstisnalar_Araclar_YeniAracId",
                        column: x => x.YeniAracId,
                        principalTable: "Araclar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PuantajIstisnalar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PuantajIstisnalar_OperasyonKayitlari_OperasyonKaydiId",
                        column: x => x.OperasyonKaydiId,
                        principalTable: "OperasyonKayitlari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PuantajIstisnalar_PuantajKayitlar_PuantajKayitId",
                        column: x => x.PuantajKayitId,
                        principalTable: "PuantajKayitlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajIstisnalar_EskiAracId",
                table: "PuantajIstisnalar",
                column: "EskiAracId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajIstisnalar_FirmaId",
                table: "PuantajIstisnalar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajIstisnalar_OperasyonKaydiId",
                table: "PuantajIstisnalar",
                column: "OperasyonKaydiId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajIstisnalar_PuantajKayitId",
                table: "PuantajIstisnalar",
                column: "PuantajKayitId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajIstisnalar_YeniAracId",
                table: "PuantajIstisnalar",
                column: "YeniAracId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PuantajIstisnalar");

            migrationBuilder.DropColumn(
                name: "CezaTutar",
                table: "PuantajFaturaHazirlikSatirlar");

            migrationBuilder.DropColumn(
                name: "EkGelir",
                table: "PuantajFaturaHazirlikSatirlar");

            migrationBuilder.DropColumn(
                name: "EkGider",
                table: "PuantajFaturaHazirlikSatirlar");

            migrationBuilder.DropColumn(
                name: "MasrafTutar",
                table: "PuantajFaturaHazirlikSatirlar");
        }
    }
}



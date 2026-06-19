using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PuantajFaturaHazirlik : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PuantajFaturaHazirliklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    KurumId = table.Column<int>(type: "integer", nullable: true),
                    AgacYapisi = table.Column<int>(type: "integer", nullable: false),
                    FaturaYonu = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ToplamGelir = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ToplamGider = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ToplamKdv = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ToplamKesinti = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NetTutar = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ToplamSefer = table.Column<int>(type: "integer", nullable: false),
                    SatirSayisi = table.Column<int>(type: "integer", nullable: false),
                    OnaylayanKullanici = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OnayTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajFaturaHazirliklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuantajFaturaHazirliklar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PuantajFaturaHazirlikSatirlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    HazirlikId = table.Column<int>(type: "integer", nullable: false),
                    PuantajKayitId = table.Column<int>(type: "integer", nullable: true),
                    KurumId = table.Column<int>(type: "integer", nullable: true),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    AracId = table.Column<int>(type: "integer", nullable: true),
                    GuzergahId = table.Column<int>(type: "integer", nullable: true),
                    SoforId = table.Column<int>(type: "integer", nullable: true),
                    Plaka = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SoforAdi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CariUnvan = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    GuzergahAdi = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Gun01 = table.Column<int>(type: "integer", nullable: false),
                    Gun02 = table.Column<int>(type: "integer", nullable: false),
                    Gun03 = table.Column<int>(type: "integer", nullable: false),
                    Gun04 = table.Column<int>(type: "integer", nullable: false),
                    Gun05 = table.Column<int>(type: "integer", nullable: false),
                    Gun06 = table.Column<int>(type: "integer", nullable: false),
                    Gun07 = table.Column<int>(type: "integer", nullable: false),
                    Gun08 = table.Column<int>(type: "integer", nullable: false),
                    Gun09 = table.Column<int>(type: "integer", nullable: false),
                    Gun10 = table.Column<int>(type: "integer", nullable: false),
                    Gun11 = table.Column<int>(type: "integer", nullable: false),
                    Gun12 = table.Column<int>(type: "integer", nullable: false),
                    Gun13 = table.Column<int>(type: "integer", nullable: false),
                    Gun14 = table.Column<int>(type: "integer", nullable: false),
                    Gun15 = table.Column<int>(type: "integer", nullable: false),
                    Gun16 = table.Column<int>(type: "integer", nullable: false),
                    Gun17 = table.Column<int>(type: "integer", nullable: false),
                    Gun18 = table.Column<int>(type: "integer", nullable: false),
                    Gun19 = table.Column<int>(type: "integer", nullable: false),
                    Gun20 = table.Column<int>(type: "integer", nullable: false),
                    Gun21 = table.Column<int>(type: "integer", nullable: false),
                    Gun22 = table.Column<int>(type: "integer", nullable: false),
                    Gun23 = table.Column<int>(type: "integer", nullable: false),
                    Gun24 = table.Column<int>(type: "integer", nullable: false),
                    Gun25 = table.Column<int>(type: "integer", nullable: false),
                    Gun26 = table.Column<int>(type: "integer", nullable: false),
                    Gun27 = table.Column<int>(type: "integer", nullable: false),
                    Gun28 = table.Column<int>(type: "integer", nullable: false),
                    Gun29 = table.Column<int>(type: "integer", nullable: false),
                    Gun30 = table.Column<int>(type: "integer", nullable: false),
                    Gun31 = table.Column<int>(type: "integer", nullable: false),
                    ToplamGun = table.Column<int>(type: "integer", nullable: false),
                    ToplamSefer = table.Column<int>(type: "integer", nullable: false),
                    BirimGelir = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ToplamGelir = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BirimGider = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ToplamGider = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Kdv10Tutar = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Kdv20Tutar = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    KesintiTutar = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TahsilEdilecek = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Odenecek = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ManuelDuzeltmeMi = table.Column<bool>(type: "boolean", nullable: false),
                    OrijinalTutar = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    DuzeltmeAciklamasi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajFaturaHazirlikSatirlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuantajFaturaHazirlikSatirlar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PuantajFaturaHazirlikSatirlar_PuantajFaturaHazirliklar_Hazi~",
                        column: x => x.HazirlikId,
                        principalTable: "PuantajFaturaHazirliklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajFaturaHazirliklar_FirmaId",
                table: "PuantajFaturaHazirliklar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajFaturaHazirlikSatirlar_FirmaId",
                table: "PuantajFaturaHazirlikSatirlar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajFaturaHazirlikSatirlar_HazirlikId",
                table: "PuantajFaturaHazirlikSatirlar",
                column: "HazirlikId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PuantajFaturaHazirlikSatirlar");

            migrationBuilder.DropTable(
                name: "PuantajFaturaHazirliklar");
        }
    }
}

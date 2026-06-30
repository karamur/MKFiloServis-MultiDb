using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIhaleRakipBenchmark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BakimPeriyotlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    BakimAdi = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    PeriyotKm = table.Column<int>(type: "integer", nullable: true),
                    SonBakimKm = table.Column<int>(type: "integer", nullable: true),
                    SonBakimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PeriyotGun = table.Column<int>(type: "integer", nullable: true),
                    UyariKmEsigi = table.Column<int>(type: "integer", nullable: false),
                    UyariGunEsigi = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakimPeriyotlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BakimPeriyotlar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IhaleRakipBenchmarklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IhaleProjeId = table.Column<int>(type: "integer", nullable: false),
                    RakipFirmaAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AylikTeklifTutari = table.Column<decimal>(type: "numeric", nullable: true),
                    ToplamTeklifTutari = table.Column<decimal>(type: "numeric", nullable: true),
                    PiyasaOrtalamaFiyati = table.Column<decimal>(type: "numeric", nullable: true),
                    MinPiyasaFiyati = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxPiyasaFiyati = table.Column<decimal>(type: "numeric", nullable: true),
                    VeriKaynagi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VeritarihiTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IhaleRakipBenchmarklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IhaleRakipBenchmarklar_IhaleProjeleri_IhaleProjeId",
                        column: x => x.IhaleProjeId,
                        principalTable: "IhaleProjeleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AracBakimUyarilari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BakimPeriyotId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    UyariTipi = table.Column<int>(type: "integer", nullable: false),
                    AracKm = table.Column<int>(type: "integer", nullable: true),
                    KalanKm = table.Column<int>(type: "integer", nullable: true),
                    KalanGun = table.Column<int>(type: "integer", nullable: true),
                    GonderimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EmailGonderildi = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppGonderildi = table.Column<bool>(type: "boolean", nullable: false),
                    HataMesaji = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracBakimUyarilari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracBakimUyarilari_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AracBakimUyarilari_BakimPeriyotlar_BakimPeriyotId",
                        column: x => x.BakimPeriyotId,
                        principalTable: "BakimPeriyotlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AracBakimUyarilari_AracId",
                table: "AracBakimUyarilari",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_AracBakimUyarilari_BakimPeriyotId",
                table: "AracBakimUyarilari",
                column: "BakimPeriyotId");

            migrationBuilder.CreateIndex(
                name: "IX_BakimPeriyotlar_AracId",
                table: "BakimPeriyotlar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleRakipBenchmarklar_IhaleProjeId",
                table: "IhaleRakipBenchmarklar",
                column: "IhaleProjeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AracBakimUyarilari");

            migrationBuilder.DropTable(
                name: "IhaleRakipBenchmarklar");

            migrationBuilder.DropTable(
                name: "BakimPeriyotlar");
        }
    }
}



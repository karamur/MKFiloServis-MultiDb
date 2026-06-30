using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveKomisyonculukIs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KomisyonculukIsAtamalar");

            migrationBuilder.DropTable(
                name: "KomisyonculukIsler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KomisyonculukIsler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AlinanIsFaturaId = table.Column<int>(type: "integer", nullable: true),
                    MusteriCariId = table.Column<int>(type: "integer", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    FaturaKesildi = table.Column<bool>(type: "boolean", nullable: false),
                    FaturaKesimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FiyatlamaTipi = table.Column<int>(type: "integer", nullable: false),
                    IsAciklamasi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsTipi = table.Column<int>(type: "integer", nullable: false),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ToplamGun = table.Column<int>(type: "integer", nullable: false),
                    ToplamSefer = table.Column<int>(type: "integer", nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KomisyonculukIsler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KomisyonculukIsler_Cariler_MusteriCariId",
                        column: x => x.MusteriCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KomisyonculukIsler_Faturalar_AlinanIsFaturaId",
                        column: x => x.AlinanIsFaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KomisyonculukIsAtamalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: true),
                    KomisyonculukIsId = table.Column<int>(type: "integer", nullable: false),
                    SoforId = table.Column<int>(type: "integer", nullable: true),
                    TedarikciCariId = table.Column<int>(type: "integer", nullable: true),
                    VerilenIsFaturaId = table.Column<int>(type: "integer", nullable: true),
                    AracKiraBedeli = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AtamaTipi = table.Column<int>(type: "integer", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DigerMasraflar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DisAracPlaka = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    DisSoforAdSoyad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisSoforTelefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OtoyolMaliyeti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SoforMaliyeti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TedarikciOdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TedarikciOdendi = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    YakitMaliyeti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KomisyonculukIsAtamalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KomisyonculukIsAtamalar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KomisyonculukIsAtamalar_Cariler_TedarikciCariId",
                        column: x => x.TedarikciCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KomisyonculukIsAtamalar_Faturalar_VerilenIsFaturaId",
                        column: x => x.VerilenIsFaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KomisyonculukIsAtamalar_KomisyonculukIsler_KomisyonculukIsId",
                        column: x => x.KomisyonculukIsId,
                        principalTable: "KomisyonculukIsler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KomisyonculukIsAtamalar_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KomisyonculukIsAtamalar_AracId",
                table: "KomisyonculukIsAtamalar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_KomisyonculukIsAtamalar_KomisyonculukIsId",
                table: "KomisyonculukIsAtamalar",
                column: "KomisyonculukIsId");

            migrationBuilder.CreateIndex(
                name: "IX_KomisyonculukIsAtamalar_SoforId",
                table: "KomisyonculukIsAtamalar",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_KomisyonculukIsAtamalar_TedarikciCariId",
                table: "KomisyonculukIsAtamalar",
                column: "TedarikciCariId");

            migrationBuilder.CreateIndex(
                name: "IX_KomisyonculukIsAtamalar_VerilenIsFaturaId",
                table: "KomisyonculukIsAtamalar",
                column: "VerilenIsFaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_KomisyonculukIsler_AlinanIsFaturaId",
                table: "KomisyonculukIsler",
                column: "AlinanIsFaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_KomisyonculukIsler_IsKodu",
                table: "KomisyonculukIsler",
                column: "IsKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KomisyonculukIsler_MusteriCariId",
                table: "KomisyonculukIsler",
                column: "MusteriCariId");
        }
    }
}



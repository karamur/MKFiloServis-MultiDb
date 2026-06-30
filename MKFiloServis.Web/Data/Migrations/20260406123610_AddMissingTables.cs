using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PuantajExcelImportlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DosyaAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImportTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ImportEdenKullanici = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ToplamSatir = table.Column<int>(type: "integer", nullable: false),
                    BasariliSatir = table.Column<int>(type: "integer", nullable: false),
                    HataliSatir = table.Column<int>(type: "integer", nullable: false),
                    OtoOlusturulanFirma = table.Column<int>(type: "integer", nullable: false),
                    OtoOlusturulanGuzergah = table.Column<int>(type: "integer", nullable: false),
                    OtoOlusturulanSofor = table.Column<int>(type: "integer", nullable: false),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    HataMesaji = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajExcelImportlar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PuantajEslestirmeOnerileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExcelImportId = table.Column<int>(type: "integer", nullable: false),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    ExcelDeger = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OnerilenId = table.Column<int>(type: "integer", nullable: true),
                    OnerilenAd = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BenzerlikPuani = table.Column<int>(type: "integer", nullable: false),
                    Onaylandi = table.Column<bool>(type: "boolean", nullable: false),
                    YeniOlusturulacak = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajEslestirmeOnerileri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuantajEslestirmeOnerileri_PuantajExcelImportlar_ExcelImpor~",
                        column: x => x.ExcelImportId,
                        principalTable: "PuantajExcelImportlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PuantajKayitlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    KurumCariId = table.Column<int>(type: "integer", nullable: true),
                    KurumAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GuzergahId = table.Column<int>(type: "integer", nullable: true),
                    GuzergahAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Yon = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: true),
                    Plaka = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SoforId = table.Column<int>(type: "integer", nullable: true),
                    SoforAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SoforTelefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SoforOdemeTipi = table.Column<int>(type: "integer", nullable: false),
                    OdemeYapilacakCariId = table.Column<int>(type: "integer", nullable: true),
                    FaturaKesiciCariId = table.Column<int>(type: "integer", nullable: true),
                    FaturaKesiciAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FaturaKesiciTelefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Gun = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SeferSayisi = table.Column<int>(type: "integer", nullable: false),
                    BirimGelir = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamGelir = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GelirKdvOrani = table.Column<int>(type: "integer", nullable: false),
                    GelirKdvTutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GelirToplam = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GelirKdvOrani20 = table.Column<int>(type: "integer", nullable: false),
                    GelirKdv20Tutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GelirKdvOrani10 = table.Column<int>(type: "integer", nullable: false),
                    GelirKdv10Tutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GelirKesinti = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Alinacak = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BirimGider = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamGider = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GiderKdvOrani20 = table.Column<int>(type: "integer", nullable: false),
                    GiderKdv20Tutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GiderKdvOrani10 = table.Column<int>(type: "integer", nullable: false),
                    GiderKdv10Tutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GiderKesinti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Odenecek = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GelirFaturaKesildi = table.Column<bool>(type: "boolean", nullable: false),
                    GelirFaturaNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GelirFaturaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GelirFaturaId = table.Column<int>(type: "integer", nullable: true),
                    GiderFaturaAlindi = table.Column<bool>(type: "boolean", nullable: false),
                    GiderFaturaNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GiderFaturaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GiderFaturaId = table.Column<int>(type: "integer", nullable: true),
                    GelirOdemeDurumu = table.Column<int>(type: "integer", nullable: false),
                    GelirOdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GelirOdenenTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GiderOdemeDurumu = table.Column<int>(type: "integer", nullable: false),
                    GiderOdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GiderOdenenTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OnayDurum = table.Column<int>(type: "integer", nullable: false),
                    OnaylayanKullanici = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OnayTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Kaynak = table.Column<int>(type: "integer", nullable: false),
                    ExcelImportId = table.Column<int>(type: "integer", nullable: true),
                    ExcelSatirNo = table.Column<int>(type: "integer", nullable: true),
                    Notlar = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PuantajExcelImportId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajKayitlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuantajKayitlar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PuantajKayitlar_Cariler_FaturaKesiciCariId",
                        column: x => x.FaturaKesiciCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PuantajKayitlar_Cariler_KurumCariId",
                        column: x => x.KurumCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PuantajKayitlar_Cariler_OdemeYapilacakCariId",
                        column: x => x.OdemeYapilacakCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PuantajKayitlar_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PuantajKayitlar_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PuantajKayitlar_PuantajExcelImportlar_PuantajExcelImportId",
                        column: x => x.PuantajExcelImportId,
                        principalTable: "PuantajExcelImportlar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajEslestirmeOnerileri_ExcelImportId_Tip_ExcelDeger",
                table: "PuantajEslestirmeOnerileri",
                columns: new[] { "ExcelImportId", "Tip", "ExcelDeger" });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajExcelImportlar_Yil_Ay",
                table: "PuantajExcelImportlar",
                columns: new[] { "Yil", "Ay" });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_AracId",
                table: "PuantajKayitlar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_FaturaKesiciCariId",
                table: "PuantajKayitlar",
                column: "FaturaKesiciCariId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_GuzergahId",
                table: "PuantajKayitlar",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_KurumCariId",
                table: "PuantajKayitlar",
                column: "KurumCariId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_OdemeYapilacakCariId",
                table: "PuantajKayitlar",
                column: "OdemeYapilacakCariId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_PuantajExcelImportId",
                table: "PuantajKayitlar",
                column: "PuantajExcelImportId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_SoforId",
                table: "PuantajKayitlar",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_Yil_Ay_GuzergahId_AracId",
                table: "PuantajKayitlar",
                columns: new[] { "Yil", "Ay", "GuzergahId", "AracId" });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_Yil_Ay_KurumCariId",
                table: "PuantajKayitlar",
                columns: new[] { "Yil", "Ay", "KurumCariId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PuantajEslestirmeOnerileri");

            migrationBuilder.DropTable(
                name: "PuantajKayitlar");

            migrationBuilder.DropTable(
                name: "PuantajExcelImportlar");
        }
    }
}



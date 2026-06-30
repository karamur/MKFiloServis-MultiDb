using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFiloOperasyonEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AracAlimSatimlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    IslemTipi = table.Column<int>(type: "integer", nullable: false),
                    KarsiTarafCariId = table.Column<int>(type: "integer", nullable: true),
                    KarsiTarafAdSoyad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    KarsiTarafTcKimlik = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    KarsiTarafTelefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IslemTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IslemTutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KDVTutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NoterAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NoterTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NoterYevmiyeNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NoterIslemTamam = table.Column<bool>(type: "boolean", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    FaturaKesildi = table.Column<bool>(type: "boolean", nullable: false),
                    FaturaKesimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FaturaUyumu = table.Column<int>(type: "integer", nullable: false),
                    FaturaUyumsuzlukAciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OdemeDurum = table.Column<int>(type: "integer", nullable: false),
                    OdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OdenenTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RuhsatTeslimAlindi = table.Column<bool>(type: "boolean", nullable: false),
                    SigortaTeslimAlindi = table.Column<bool>(type: "boolean", nullable: false),
                    MuayeneBelgesiTeslimAlindi = table.Column<bool>(type: "boolean", nullable: false),
                    AnahtarTeslimAlindi = table.Column<bool>(type: "boolean", nullable: false),
                    YedekAnahtarTeslimAlindi = table.Column<bool>(type: "boolean", nullable: false),
                    ServisBakimDefteri = table.Column<bool>(type: "boolean", nullable: false),
                    EksikBelgeler = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracAlimSatimlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracAlimSatimlar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AracAlimSatimlar_Cariler_KarsiTarafCariId",
                        column: x => x.KarsiTarafCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AracAlimSatimlar_Faturalar_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AracOperasyonDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    OperasyonTipi = table.Column<int>(type: "integer", nullable: false),
                    ToplamCalismaGunu = table.Column<int>(type: "integer", nullable: false),
                    ToplamSeferSayisi = table.Column<int>(type: "integer", nullable: false),
                    ToplamKm = table.Column<int>(type: "integer", nullable: false),
                    BrutGelir = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KomisyonKesintisi = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    YakitGideri = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SoforMaliyeti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KiraBedeli = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BakimOnarimGideri = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SigortaGideri = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VergiGideri = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OtoyolGideri = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DigerGiderler = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracOperasyonDurumlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracOperasyonDurumlari_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KomisyonculukIsler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MusteriCariId = table.Column<int>(type: "integer", nullable: false),
                    IsAciklamasi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsTipi = table.Column<int>(type: "integer", nullable: false),
                    FiyatlamaTipi = table.Column<int>(type: "integer", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamGun = table.Column<int>(type: "integer", nullable: false),
                    ToplamSefer = table.Column<int>(type: "integer", nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AlinanIsFaturaId = table.Column<int>(type: "integer", nullable: true),
                    FaturaKesildi = table.Column<bool>(type: "boolean", nullable: false),
                    FaturaKesimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "PlakaDonusumler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    EskiPlaka = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    EskiPlakaTipi = table.Column<int>(type: "integer", nullable: false),
                    YeniPlaka = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    YeniPlakaTipi = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    BasvuruTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OnayTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TamamlanmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PlakaBedeliMasrafi = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EmnivetHarci = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NoterMasrafi = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DigerMasraflar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlakaSatilacakMi = table.Column<bool>(type: "boolean", nullable: false),
                    PlakaSatisBedeli = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PlakaSatisCarisiId = table.Column<int>(type: "integer", nullable: true),
                    PlakaSatildi = table.Column<bool>(type: "boolean", nullable: false),
                    PlakaSatisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlakaDonusumler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlakaDonusumler_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlakaDonusumler_Cariler_PlakaSatisCarisiId",
                        column: x => x.PlakaSatisCarisiId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KomisyonculukIsAtamalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KomisyonculukIsId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: true),
                    TedarikciCariId = table.Column<int>(type: "integer", nullable: true),
                    DisAracPlaka = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    AtamaTipi = table.Column<int>(type: "integer", nullable: false),
                    SoforId = table.Column<int>(type: "integer", nullable: true),
                    DisSoforAdSoyad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisSoforTelefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AracKiraBedeli = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SoforMaliyeti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    YakitMaliyeti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OtoyolMaliyeti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DigerMasraflar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VerilenIsFaturaId = table.Column<int>(type: "integer", nullable: true),
                    TedarikciOdendi = table.Column<bool>(type: "boolean", nullable: false),
                    TedarikciOdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "IX_AracAlimSatimlar_AracId_IslemTarihi",
                table: "AracAlimSatimlar",
                columns: new[] { "AracId", "IslemTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_AracAlimSatimlar_FaturaId",
                table: "AracAlimSatimlar",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_AracAlimSatimlar_KarsiTarafCariId",
                table: "AracAlimSatimlar",
                column: "KarsiTarafCariId");

            migrationBuilder.CreateIndex(
                name: "IX_AracOperasyonDurumlari_AracId_Yil_Ay",
                table: "AracOperasyonDurumlari",
                columns: new[] { "AracId", "Yil", "Ay" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_PlakaDonusumler_AracId_EskiPlaka",
                table: "PlakaDonusumler",
                columns: new[] { "AracId", "EskiPlaka" });

            migrationBuilder.CreateIndex(
                name: "IX_PlakaDonusumler_PlakaSatisCarisiId",
                table: "PlakaDonusumler",
                column: "PlakaSatisCarisiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AracAlimSatimlar");

            migrationBuilder.DropTable(
                name: "AracOperasyonDurumlari");

            migrationBuilder.DropTable(
                name: "KomisyonculukIsAtamalar");

            migrationBuilder.DropTable(
                name: "PlakaDonusumler");

            migrationBuilder.DropTable(
                name: "KomisyonculukIsler");
        }
    }
}



using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPiyasaArastirmaModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AracMarkaModeller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Marka = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    Versiyon = table.Column<string>(type: "TEXT", nullable: true),
                    BaslangicYili = table.Column<int>(type: "INTEGER", nullable: true),
                    BitisYili = table.Column<int>(type: "INTEGER", nullable: true),
                    KasaTipi = table.Column<string>(type: "TEXT", nullable: true),
                    YakitTipleri = table.Column<string>(type: "TEXT", nullable: true),
                    VitesTipleri = table.Column<string>(type: "TEXT", nullable: true),
                    Segment = table.Column<string>(type: "TEXT", nullable: true),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false),
                    Sira = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracMarkaModeller", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PiyasaArastirmalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Marka = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Versiyon = table.Column<string>(type: "TEXT", nullable: true),
                    YilBaslangic = table.Column<int>(type: "INTEGER", nullable: true),
                    YilBitis = table.Column<int>(type: "INTEGER", nullable: true),
                    YakitTipi = table.Column<string>(type: "TEXT", nullable: true),
                    VitesTipi = table.Column<string>(type: "TEXT", nullable: true),
                    MinKilometre = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxKilometre = table.Column<int>(type: "INTEGER", nullable: true),
                    MinFiyat = table.Column<decimal>(type: "TEXT", nullable: true),
                    MaxFiyat = table.Column<decimal>(type: "TEXT", nullable: true),
                    Sehir = table.Column<string>(type: "TEXT", nullable: true),
                    ToplamIlanSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    OrtalamaFiyat = table.Column<decimal>(type: "TEXT", nullable: false),
                    EnDusukFiyat = table.Column<decimal>(type: "TEXT", nullable: false),
                    EnYuksekFiyat = table.Column<decimal>(type: "TEXT", nullable: false),
                    MedianFiyat = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrtalamaKilometre = table.Column<int>(type: "INTEGER", nullable: false),
                    ArastirmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Durum = table.Column<int>(type: "INTEGER", nullable: false),
                    HataMesaji = table.Column<string>(type: "TEXT", nullable: true),
                    AIAnalizi = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PiyasaArastirmalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PiyasaArastirmaIlanlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArastirmaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kaynak = table.Column<string>(type: "TEXT", nullable: false),
                    IlanNo = table.Column<string>(type: "TEXT", nullable: true),
                    IlanBasligi = table.Column<string>(type: "TEXT", nullable: false),
                    IlanUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Marka = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Versiyon = table.Column<string>(type: "TEXT", nullable: true),
                    ModelYili = table.Column<int>(type: "INTEGER", nullable: false),
                    Kilometre = table.Column<int>(type: "INTEGER", nullable: false),
                    Fiyat = table.Column<decimal>(type: "TEXT", nullable: false),
                    ParaBirimi = table.Column<string>(type: "TEXT", nullable: true),
                    YakitTipi = table.Column<string>(type: "TEXT", nullable: true),
                    VitesTipi = table.Column<string>(type: "TEXT", nullable: true),
                    KasaTipi = table.Column<string>(type: "TEXT", nullable: true),
                    MotorHacmi = table.Column<string>(type: "TEXT", nullable: true),
                    MotorGucu = table.Column<string>(type: "TEXT", nullable: true),
                    Renk = table.Column<string>(type: "TEXT", nullable: true),
                    BoyaliParcaSayisi = table.Column<int>(type: "INTEGER", nullable: true),
                    DegisenParcaSayisi = table.Column<int>(type: "INTEGER", nullable: true),
                    TramerTutari = table.Column<decimal>(type: "TEXT", nullable: true),
                    HasarKayitli = table.Column<bool>(type: "INTEGER", nullable: false),
                    Sehir = table.Column<string>(type: "TEXT", nullable: true),
                    Ilce = table.Column<string>(type: "TEXT", nullable: true),
                    SaticiTipi = table.Column<string>(type: "TEXT", nullable: true),
                    SaticiAdi = table.Column<string>(type: "TEXT", nullable: true),
                    IlanTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ToplanmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AktifMi = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notlar = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PiyasaArastirmaIlanlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PiyasaArastirmaIlanlar_PiyasaArastirmalar_ArastirmaId",
                        column: x => x.ArastirmaId,
                        principalTable: "PiyasaArastirmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PiyasaArastirmaIlanlar_ArastirmaId",
                table: "PiyasaArastirmaIlanlar",
                column: "ArastirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AracMarkaModeller");

            migrationBuilder.DropTable(
                name: "PiyasaArastirmaIlanlar");

            migrationBuilder.DropTable(
                name: "PiyasaArastirmalar");
        }
    }
}



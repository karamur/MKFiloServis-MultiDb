using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class IhaleHazirlikModulu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CariIletisimNotlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CariId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: true),
                    Konu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notlar = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IletisimTipi = table.Column<int>(type: "integer", nullable: false),
                    IletisimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IletisimYapanKisi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MuhatapKisi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SonrakiAksiyon = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SonrakiAksiyonTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AksiyonTamamlandi = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariIletisimNotlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CariIletisimNotlar_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CariIletisimNotlar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IhaleProjeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProjeAdi = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SozlesmeSuresiAy = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    EnflasyonOrani = table.Column<decimal>(type: "numeric", nullable: false),
                    YakitZamOrani = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikCalismGunu = table.Column<int>(type: "integer", nullable: false),
                    GunlukCalismaSaati = table.Column<int>(type: "integer", nullable: false),
                    AIAnaliz = table.Column<string>(type: "text", nullable: true),
                    AIAnalizTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IhaleProjeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IhaleProjeleri_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IhaleProjeleri_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IhaleGuzergahKalemleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IhaleProjeId = table.Column<int>(type: "integer", nullable: false),
                    GuzergahId = table.Column<int>(type: "integer", nullable: true),
                    HatAdi = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BaslangicNoktasi = table.Column<string>(type: "text", nullable: true),
                    BitisNoktasi = table.Column<string>(type: "text", nullable: true),
                    MesafeKm = table.Column<decimal>(type: "numeric", nullable: false),
                    TahminiSureDakika = table.Column<int>(type: "integer", nullable: false),
                    SeferTipi = table.Column<int>(type: "integer", nullable: false),
                    GunlukSeferSayisi = table.Column<int>(type: "integer", nullable: false),
                    AylikSeferGunu = table.Column<int>(type: "integer", nullable: false),
                    PersonelSayisi = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: true),
                    SahiplikDurumu = table.Column<int>(type: "integer", nullable: false),
                    AracModelBilgi = table.Column<string>(type: "text", nullable: true),
                    AracKoltukSayisi = table.Column<int>(type: "integer", nullable: false),
                    YakitTuketimi = table.Column<decimal>(type: "numeric", nullable: false),
                    YakitFiyati = table.Column<decimal>(type: "numeric", nullable: false),
                    GunlukYakitMaliyeti = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikYakitMaliyeti = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikBakimMasrafi = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikLastikMasrafi = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikSigortaMasrafi = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikKaskoMasrafi = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikMuayeneMasrafi = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikYedekParcaMasrafi = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikDigerMasraf = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikKiraBedeli = table.Column<decimal>(type: "numeric", nullable: false),
                    SeferBasiKomisyon = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikKomisyonToplam = table.Column<decimal>(type: "numeric", nullable: false),
                    SoforId = table.Column<int>(type: "integer", nullable: true),
                    SoforBrutMaas = table.Column<decimal>(type: "numeric", nullable: false),
                    SoforNetMaas = table.Column<decimal>(type: "numeric", nullable: false),
                    SoforSGKIsverenPay = table.Column<decimal>(type: "numeric", nullable: false),
                    SoforToplamMaliyet = table.Column<decimal>(type: "numeric", nullable: false),
                    AracDegeri = table.Column<decimal>(type: "numeric", nullable: false),
                    AmortismanYili = table.Column<int>(type: "integer", nullable: false),
                    AylikAmortisman = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikMaliyet = table.Column<decimal>(type: "numeric", nullable: false),
                    ToplamMaliyet = table.Column<decimal>(type: "numeric", nullable: false),
                    SeferBasiMaliyet = table.Column<decimal>(type: "numeric", nullable: false),
                    SaatlikMaliyet = table.Column<decimal>(type: "numeric", nullable: false),
                    KmBasiMaliyet = table.Column<decimal>(type: "numeric", nullable: false),
                    KarMarjiOrani = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikKarTutari = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikTeklifFiyati = table.Column<decimal>(type: "numeric", nullable: false),
                    SeferBasiTeklifFiyati = table.Column<decimal>(type: "numeric", nullable: false),
                    SaatlikTeklifFiyati = table.Column<decimal>(type: "numeric", nullable: false),
                    AITahminiKullanildi = table.Column<bool>(type: "boolean", nullable: false),
                    AITahminDetay = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IhaleGuzergahKalemleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IhaleGuzergahKalemleri_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IhaleGuzergahKalemleri_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IhaleGuzergahKalemleri_IhaleProjeleri_IhaleProjeId",
                        column: x => x.IhaleProjeId,
                        principalTable: "IhaleProjeleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IhaleGuzergahKalemleri_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CariIletisimNotlar_CariId",
                table: "CariIletisimNotlar",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_CariIletisimNotlar_KullaniciId",
                table: "CariIletisimNotlar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleGuzergahKalemleri_AracId",
                table: "IhaleGuzergahKalemleri",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleGuzergahKalemleri_GuzergahId",
                table: "IhaleGuzergahKalemleri",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleGuzergahKalemleri_IhaleProjeId",
                table: "IhaleGuzergahKalemleri",
                column: "IhaleProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleGuzergahKalemleri_SoforId",
                table: "IhaleGuzergahKalemleri",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleProjeleri_CariId",
                table: "IhaleProjeleri",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleProjeleri_FirmaId",
                table: "IhaleProjeleri",
                column: "FirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CariIletisimNotlar");

            migrationBuilder.DropTable(
                name: "IhaleGuzergahKalemleri");

            migrationBuilder.DropTable(
                name: "IhaleProjeleri");
        }
    }
}



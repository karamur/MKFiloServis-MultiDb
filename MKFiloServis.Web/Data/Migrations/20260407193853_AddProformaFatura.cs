using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProformaFatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProformaFaturalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProformaNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProformaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GecerlilikTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    CariId = table.Column<int>(type: "integer", nullable: false),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    AraToplam = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IskontoTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IskontoOrani = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    KdvTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GenelToplam = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OdemeKosulu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TeslimKosulu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VadeGun = table.Column<int>(type: "integer", nullable: true),
                    IlgiliKisi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    OzelNotlar = table.Column<string>(type: "text", nullable: true),
                    PdfDosyaYolu = table.Column<string>(type: "text", nullable: true),
                    FaturayaDonusturuldu = table.Column<bool>(type: "boolean", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    FaturaDonusumTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProformaFaturalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProformaFaturalar_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProformaFaturalar_Faturalar_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProformaFaturalar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProformaFaturaKalemler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProformaFaturaId = table.Column<int>(type: "integer", nullable: false),
                    StokKartiId = table.Column<int>(type: "integer", nullable: true),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    UrunAdi = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    UrunKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Miktar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Birim = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IskontoOrani = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IskontoTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    KdvTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AraToplam = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProformaFaturaKalemler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProformaFaturaKalemler_ProformaFaturalar_ProformaFaturaId",
                        column: x => x.ProformaFaturaId,
                        principalTable: "ProformaFaturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProformaFaturaKalemler_StokKartlari_StokKartiId",
                        column: x => x.StokKartiId,
                        principalTable: "StokKartlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProformaFaturaKalemler_ProformaFaturaId",
                table: "ProformaFaturaKalemler",
                column: "ProformaFaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaFaturaKalemler_StokKartiId",
                table: "ProformaFaturaKalemler",
                column: "StokKartiId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaFaturalar_CariId",
                table: "ProformaFaturalar",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaFaturalar_FaturaId",
                table: "ProformaFaturalar",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaFaturalar_FirmaId",
                table: "ProformaFaturalar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaFaturalar_ProformaNo",
                table: "ProformaFaturalar",
                column: "ProformaNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProformaFaturaKalemler");

            migrationBuilder.DropTable(
                name: "ProformaFaturalar");
        }
    }
}



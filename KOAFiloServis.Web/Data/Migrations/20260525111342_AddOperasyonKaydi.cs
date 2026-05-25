using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperasyonKaydi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PuantajKayitlar_Yil_Ay_GuzergahId_AracId",
                table: "PuantajKayitlar");

            migrationBuilder.AddColumn<string>(
                name: "BelgeNo",
                table: "PuantajKayitlar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinansYonu",
                table: "PuantajKayitlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsverenFirmaId",
                table: "PuantajKayitlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakTipi",
                table: "PuantajKayitlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KurumId",
                table: "PuantajKayitlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Slot",
                table: "PuantajKayitlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SlotAdi",
                table: "PuantajKayitlar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferDurum",
                table: "PuantajKayitlar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GelenFaturaId",
                table: "KiralikPlakaTakipler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KalanFaturaTutar",
                table: "KiralikPlakaTakipler",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "KesilenFaturaNo",
                table: "KiralikPlakaTakipler",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KesilenFaturaTarih",
                table: "KiralikPlakaTakipler",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KesilenFaturaTutar",
                table: "KiralikPlakaTakipler",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OdenenTutar",
                table: "KiralikPlakaTakipler",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonOdemeTarihi",
                table: "KiralikPlakaTakipler",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ToplamOdeme",
                table: "KiralikPlakaTakipler",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Slot",
                table: "GuzergahSeferleri",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OperasyonKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GuzergahId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    SoforId = table.Column<int>(type: "integer", nullable: true),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    SlotAdi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Yon = table.Column<int>(type: "integer", nullable: false),
                    KurumId = table.Column<int>(type: "integer", nullable: true),
                    IsverenFirmaId = table.Column<int>(type: "integer", nullable: true),
                    SeferSayisi = table.Column<int>(type: "integer", nullable: false),
                    PuantajCarpani = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    OperasyonDurumu = table.Column<int>(type: "integer", nullable: false),
                    KaynakTipi = table.Column<int>(type: "integer", nullable: false),
                    FinansYonu = table.Column<int>(type: "integer", nullable: false),
                    SoforOdemeTipi = table.Column<int>(type: "integer", nullable: false),
                    OdemeYapilacakCariId = table.Column<int>(type: "integer", nullable: true),
                    FaturaKesiciCariId = table.Column<int>(type: "integer", nullable: true),
                    BelgeNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransferDurum = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Kaynak = table.Column<int>(type: "integer", nullable: false),
                    ExcelImportId = table.Column<int>(type: "integer", nullable: true),
                    ExcelSatirNo = table.Column<int>(type: "integer", nullable: true),
                    Islendi = table.Column<bool>(type: "boolean", nullable: false),
                    IslenmeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PuantajKayitId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notlar = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperasyonKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Cariler_FaturaKesiciCariId",
                        column: x => x.FaturaKesiciCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Cariler_OdemeYapilacakCariId",
                        column: x => x.OdemeYapilacakCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Firmalar_IsverenFirmaId",
                        column: x => x.IsverenFirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Kurumlar_KurumId",
                        column: x => x.KurumId,
                        principalTable: "Kurumlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_PuantajKayitlar_PuantajKayitId",
                        column: x => x.PuantajKayitId,
                        principalTable: "PuantajKayitlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_IsverenFirmaId",
                table: "PuantajKayitlar",
                column: "IsverenFirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_KurumId",
                table: "PuantajKayitlar",
                column: "KurumId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_Yil_Ay_GuzergahId_AracId_Slot",
                table: "PuantajKayitlar",
                columns: new[] { "Yil", "Ay", "GuzergahId", "AracId", "Slot" });

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_AracId",
                table: "OperasyonKayitlari",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_FaturaKesiciCariId",
                table: "OperasyonKayitlari",
                column: "FaturaKesiciCariId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_FirmaId_Tarih",
                table: "OperasyonKayitlari",
                columns: new[] { "FirmaId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_GuzergahId",
                table: "OperasyonKayitlari",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Islendi",
                table: "OperasyonKayitlari",
                column: "Islendi");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_IsverenFirmaId",
                table: "OperasyonKayitlari",
                column: "IsverenFirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_KurumId",
                table: "OperasyonKayitlari",
                column: "KurumId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_OdemeYapilacakCariId",
                table: "OperasyonKayitlari",
                column: "OdemeYapilacakCariId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_OperasyonDurumu",
                table: "OperasyonKayitlari",
                column: "OperasyonDurumu");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_PuantajKayitId",
                table: "OperasyonKayitlari",
                column: "PuantajKayitId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Slot",
                table: "OperasyonKayitlari",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_SoforId",
                table: "OperasyonKayitlari",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Tarih",
                table: "OperasyonKayitlari",
                column: "Tarih");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Tarih_AracId",
                table: "OperasyonKayitlari",
                columns: new[] { "Tarih", "AracId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Tarih_GuzergahId_AracId_Slot",
                table: "OperasyonKayitlari",
                columns: new[] { "Tarih", "GuzergahId", "AracId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Tarih_KurumId",
                table: "OperasyonKayitlari",
                columns: new[] { "Tarih", "KurumId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PuantajKayitlar_Firmalar_IsverenFirmaId",
                table: "PuantajKayitlar",
                column: "IsverenFirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PuantajKayitlar_Kurumlar_KurumId",
                table: "PuantajKayitlar",
                column: "KurumId",
                principalTable: "Kurumlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PuantajKayitlar_Firmalar_IsverenFirmaId",
                table: "PuantajKayitlar");

            migrationBuilder.DropForeignKey(
                name: "FK_PuantajKayitlar_Kurumlar_KurumId",
                table: "PuantajKayitlar");

            migrationBuilder.DropTable(
                name: "OperasyonKayitlari");

            migrationBuilder.DropIndex(
                name: "IX_PuantajKayitlar_IsverenFirmaId",
                table: "PuantajKayitlar");

            migrationBuilder.DropIndex(
                name: "IX_PuantajKayitlar_KurumId",
                table: "PuantajKayitlar");

            migrationBuilder.DropIndex(
                name: "IX_PuantajKayitlar_Yil_Ay_GuzergahId_AracId_Slot",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "BelgeNo",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "FinansYonu",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "IsverenFirmaId",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "KaynakTipi",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "KurumId",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "SlotAdi",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "TransferDurum",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "GelenFaturaId",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "KalanFaturaTutar",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "KesilenFaturaNo",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "KesilenFaturaTarih",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "KesilenFaturaTutar",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "OdenenTutar",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "SonOdemeTarihi",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "ToplamOdeme",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "GuzergahSeferleri");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_Yil_Ay_GuzergahId_AracId",
                table: "PuantajKayitlar",
                columns: new[] { "Yil", "Ay", "GuzergahId", "AracId" });
        }
    }
}

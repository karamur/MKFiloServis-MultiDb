using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFiloKomisyonPuantaj : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Tarih",
                table: "MuhasebeFisKalemleri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FiloGuzergahEslestirmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    KurumFirmaId = table.Column<int>(type: "integer", nullable: false),
                    GuzergahId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    SoforId = table.Column<int>(type: "integer", nullable: false),
                    ServisTuru = table.Column<int>(type: "integer", nullable: false),
                    KurumaKesilecekUcret = table.Column<decimal>(type: "numeric", nullable: false),
                    TaseronaOdenenUcret = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiloGuzergahEslestirmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiloGuzergahEslestirmeleri_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FiloGuzergahEslestirmeleri_Firmalar_KurumFirmaId",
                        column: x => x.KurumFirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FiloGuzergahEslestirmeleri_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FiloGuzergahEslestirmeleri_Soforler_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Soforler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiloGunlukPuantajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FiloGuzergahEslestirmeId = table.Column<int>(type: "integer", nullable: true),
                    KurumFirmaId = table.Column<int>(type: "integer", nullable: false),
                    GuzergahId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    SoforId = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    PuantajCarpani = table.Column<decimal>(type: "numeric", nullable: false),
                    TahakkukEdenKurumUcreti = table.Column<decimal>(type: "numeric", nullable: false),
                    TahakkukEdenTaseronUcreti = table.Column<decimal>(type: "numeric", nullable: false),
                    TaksiKullanildiMi = table.Column<bool>(type: "boolean", nullable: false),
                    TaksiFisTutari = table.Column<decimal>(type: "numeric", nullable: true),
                    TaksiFisAciklama = table.Column<string>(type: "text", nullable: true),
                    ArizaYaptiMi = table.Column<bool>(type: "boolean", nullable: false),
                    ArizaAciklamasi = table.Column<string>(type: "text", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    KurumFaturaKesildiMi = table.Column<bool>(type: "boolean", nullable: false),
                    TaseronOdemeYapildiMi = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiloGunlukPuantajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiloGunlukPuantajlar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FiloGunlukPuantajlar_FiloGuzergahEslestirmeleri_FiloGuzerga~",
                        column: x => x.FiloGuzergahEslestirmeId,
                        principalTable: "FiloGuzergahEslestirmeleri",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FiloGunlukPuantajlar_Firmalar_KurumFirmaId",
                        column: x => x.KurumFirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FiloGunlukPuantajlar_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FiloGunlukPuantajlar_Soforler_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Soforler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiloGunlukPuantajlar_AracId",
                table: "FiloGunlukPuantajlar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGunlukPuantajlar_FiloGuzergahEslestirmeId",
                table: "FiloGunlukPuantajlar",
                column: "FiloGuzergahEslestirmeId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGunlukPuantajlar_GuzergahId",
                table: "FiloGunlukPuantajlar",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGunlukPuantajlar_KurumFirmaId",
                table: "FiloGunlukPuantajlar",
                column: "KurumFirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGunlukPuantajlar_SoforId",
                table: "FiloGunlukPuantajlar",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGuzergahEslestirmeleri_AracId",
                table: "FiloGuzergahEslestirmeleri",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGuzergahEslestirmeleri_GuzergahId",
                table: "FiloGuzergahEslestirmeleri",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGuzergahEslestirmeleri_KurumFirmaId",
                table: "FiloGuzergahEslestirmeleri",
                column: "KurumFirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGuzergahEslestirmeleri_SoforId",
                table: "FiloGuzergahEslestirmeleri",
                column: "SoforId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiloGunlukPuantajlar");

            migrationBuilder.DropTable(
                name: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropColumn(
                name: "Tarih",
                table: "MuhasebeFisKalemleri");
        }
    }
}



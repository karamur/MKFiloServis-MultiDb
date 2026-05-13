using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPuantajEslestirmeTablolari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonelGeriOdemeHareketId",
                table: "BankaKasaHareketleri",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FirmaAracSoforEslestirmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    KurumCariId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    SoforId = table.Column<int>(type: "integer", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VarsayilanBirimUcret = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirmaAracSoforEslestirmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirmaAracSoforEslestirmeleri_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FirmaAracSoforEslestirmeleri_Cariler_KurumCariId",
                        column: x => x.KurumCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FirmaAracSoforEslestirmeleri_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FirmaGuzergahEslestirmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    KurumCariId = table.Column<int>(type: "integer", nullable: false),
                    GuzergahId = table.Column<int>(type: "integer", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SeferUcreti = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvOrani = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirmaGuzergahEslestirmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirmaGuzergahEslestirmeleri_Cariler_KurumCariId",
                        column: x => x.KurumCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FirmaGuzergahEslestirmeleri_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_PersonelGeriOdemeHareketId",
                table: "BankaKasaHareketleri",
                column: "PersonelGeriOdemeHareketId");

            migrationBuilder.CreateIndex(
                name: "IX_FirmaAracSoforEslestirmeleri_AracId",
                table: "FirmaAracSoforEslestirmeleri",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_FirmaAracSoforEslestirmeleri_FirmaId_KurumCariId_AracId_Sof~",
                table: "FirmaAracSoforEslestirmeleri",
                columns: new[] { "FirmaId", "KurumCariId", "AracId", "SoforId" });

            migrationBuilder.CreateIndex(
                name: "IX_FirmaAracSoforEslestirmeleri_KurumCariId_Aktif",
                table: "FirmaAracSoforEslestirmeleri",
                columns: new[] { "KurumCariId", "Aktif" });

            migrationBuilder.CreateIndex(
                name: "IX_FirmaAracSoforEslestirmeleri_SoforId",
                table: "FirmaAracSoforEslestirmeleri",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_FirmaGuzergahEslestirmeleri_FirmaId_KurumCariId_GuzergahId",
                table: "FirmaGuzergahEslestirmeleri",
                columns: new[] { "FirmaId", "KurumCariId", "GuzergahId" });

            migrationBuilder.CreateIndex(
                name: "IX_FirmaGuzergahEslestirmeleri_GuzergahId",
                table: "FirmaGuzergahEslestirmeleri",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_FirmaGuzergahEslestirmeleri_KurumCariId_Aktif",
                table: "FirmaGuzergahEslestirmeleri",
                columns: new[] { "KurumCariId", "Aktif" });

            migrationBuilder.AddForeignKey(
                name: "FK_BankaKasaHareketleri_BankaKasaHareketleri_PersonelGeriOdeme~",
                table: "BankaKasaHareketleri",
                column: "PersonelGeriOdemeHareketId",
                principalTable: "BankaKasaHareketleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankaKasaHareketleri_BankaKasaHareketleri_PersonelGeriOdeme~",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropTable(
                name: "FirmaAracSoforEslestirmeleri");

            migrationBuilder.DropTable(
                name: "FirmaGuzergahEslestirmeleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_PersonelGeriOdemeHareketId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "PersonelGeriOdemeHareketId",
                table: "BankaKasaHareketleri");
        }
    }
}

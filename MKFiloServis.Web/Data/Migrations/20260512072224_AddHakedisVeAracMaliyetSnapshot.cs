using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHakedisVeAracMaliyetSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KurumFaturaId",
                table: "FiloGunlukPuantajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaliyetOzmalKiralik",
                table: "FiloGunlukPuantajlar",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SeferSayisi",
                table: "FiloGunlukPuantajlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ServisTuru",
                table: "FiloGunlukPuantajlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TedarikciOdemeFaturaId",
                table: "FiloGunlukPuantajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HakedisId",
                table: "Faturalar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AracMaliyetSnapshotlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    SahiplikTipi = table.Column<int>(type: "integer", nullable: false),
                    ToplamKm = table.Column<decimal>(type: "numeric", nullable: false),
                    ToplamSefer = table.Column<decimal>(type: "numeric", nullable: false),
                    YakitMasraf = table.Column<decimal>(type: "numeric", nullable: false),
                    BakimMasraf = table.Column<decimal>(type: "numeric", nullable: false),
                    LastikMasraf = table.Column<decimal>(type: "numeric", nullable: false),
                    SigortaMasraf = table.Column<decimal>(type: "numeric", nullable: false),
                    KaskoMasraf = table.Column<decimal>(type: "numeric", nullable: false),
                    PlakaKirasi = table.Column<decimal>(type: "numeric", nullable: false),
                    SoforMaasPayi = table.Column<decimal>(type: "numeric", nullable: false),
                    AmortismanPayi = table.Column<decimal>(type: "numeric", nullable: false),
                    DigerMasraf = table.Column<decimal>(type: "numeric", nullable: false),
                    ToplamGelir = table.Column<decimal>(type: "numeric", nullable: false),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracMaliyetSnapshotlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracMaliyetSnapshotlari_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AracMaliyetSnapshotlari_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Hakedisler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    ReferansId = table.Column<int>(type: "integer", nullable: false),
                    ToplamSeferSayisi = table.Column<decimal>(type: "numeric", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric", nullable: false),
                    KdvOran = table.Column<decimal>(type: "numeric", nullable: false),
                    KdvTutar = table.Column<decimal>(type: "numeric", nullable: false),
                    GenelToplam = table.Column<decimal>(type: "numeric", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    GenerationParams = table.Column<string>(type: "text", nullable: true),
                    OnaylayanKisi = table.Column<string>(type: "text", nullable: true),
                    OnayTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hakedisler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hakedisler_Faturalar_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Hakedisler_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HakedisDetaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HakedisId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ServisTuru = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: true),
                    SoforId = table.Column<int>(type: "integer", nullable: true),
                    GuzergahId = table.Column<int>(type: "integer", nullable: true),
                    FiloGunlukPuantajId = table.Column<int>(type: "integer", nullable: true),
                    SeferSayisi = table.Column<decimal>(type: "numeric", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HakedisDetaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HakedisDetaylari_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HakedisDetaylari_FiloGunlukPuantajlar_FiloGunlukPuantajId",
                        column: x => x.FiloGunlukPuantajId,
                        principalTable: "FiloGunlukPuantajlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HakedisDetaylari_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HakedisDetaylari_Hakedisler_HakedisId",
                        column: x => x.HakedisId,
                        principalTable: "Hakedisler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HakedisDetaylari_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AracMaliyetSnapshot_Arac_Donem",
                table: "AracMaliyetSnapshotlari",
                columns: new[] { "AracId", "Yil", "Ay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AracMaliyetSnapshotlari_SirketId",
                table: "AracMaliyetSnapshotlari",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisDetaylari_AracId",
                table: "HakedisDetaylari",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisDetaylari_FiloGunlukPuantajId",
                table: "HakedisDetaylari",
                column: "FiloGunlukPuantajId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisDetaylari_GuzergahId",
                table: "HakedisDetaylari",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisDetaylari_HakedisId",
                table: "HakedisDetaylari",
                column: "HakedisId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisDetaylari_SoforId",
                table: "HakedisDetaylari",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_Hakedis_Tip_Ref_Donem",
                table: "Hakedisler",
                columns: new[] { "Tip", "ReferansId", "Yil", "Ay" });

            migrationBuilder.CreateIndex(
                name: "IX_Hakedisler_FaturaId",
                table: "Hakedisler",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Hakedisler_SirketId",
                table: "Hakedisler",
                column: "SirketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AracMaliyetSnapshotlari");

            migrationBuilder.DropTable(
                name: "HakedisDetaylari");

            migrationBuilder.DropTable(
                name: "Hakedisler");

            migrationBuilder.DropColumn(
                name: "KurumFaturaId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "MaliyetOzmalKiralik",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "SeferSayisi",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "ServisTuru",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "TedarikciOdemeFaturaId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "HakedisId",
                table: "Faturalar");
        }
    }
}



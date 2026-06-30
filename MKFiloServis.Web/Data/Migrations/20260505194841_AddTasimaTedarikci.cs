using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTasimaTedarikci : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TasimaTedarikciId",
                table: "Personeller",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TasimaTedarikciId",
                table: "Araclar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TasimaTedarikciler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    TedarikciKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Unvan = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    YetkiliKisi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefon2 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Adres = table.Column<string>(type: "text", nullable: true),
                    Il = table.Column<string>(type: "text", nullable: true),
                    Ilce = table.Column<string>(type: "text", nullable: true),
                    VergiDairesi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VergiNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    SozlesmeBaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SozlesmeBitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SozlesmeNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VarsayilanSeferUcreti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasimaTedarikciler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TasimaTedarikciler_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TasimaTedarikciler_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TasimaTedarikciIsler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    TasimaTedarikciId = table.Column<int>(type: "integer", nullable: false),
                    GuzergahId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: true),
                    SoforId = table.Column<int>(type: "integer", nullable: true),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SeferUcreti = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AylikUcret = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasimaTedarikciIsler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TasimaTedarikciIsler_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TasimaTedarikciIsler_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TasimaTedarikciIsler_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TasimaTedarikciIsler_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TasimaTedarikciIsler_TasimaTedarikciler_TasimaTedarikciId",
                        column: x => x.TasimaTedarikciId,
                        principalTable: "TasimaTedarikciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Personeller_TasimaTedarikciId",
                table: "Personeller",
                column: "TasimaTedarikciId");

            migrationBuilder.CreateIndex(
                name: "IX_Araclar_TasimaTedarikciId",
                table: "Araclar",
                column: "TasimaTedarikciId");

            migrationBuilder.CreateIndex(
                name: "IX_TasimaTedarikciIsler_AracId",
                table: "TasimaTedarikciIsler",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_TasimaTedarikciIsler_GuzergahId",
                table: "TasimaTedarikciIsler",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_TasimaTedarikciIsler_SirketId",
                table: "TasimaTedarikciIsler",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_TasimaTedarikciIsler_SoforId",
                table: "TasimaTedarikciIsler",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_TasimaTedarikciIsler_TasimaTedarikciId_GuzergahId_Baslangic~",
                table: "TasimaTedarikciIsler",
                columns: new[] { "TasimaTedarikciId", "GuzergahId", "BaslangicTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_TasimaTedarikciler_CariId",
                table: "TasimaTedarikciler",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_TasimaTedarikciler_SirketId",
                table: "TasimaTedarikciler",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_TasimaTedarikciler_TedarikciKodu",
                table: "TasimaTedarikciler",
                column: "TedarikciKodu",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Araclar_TasimaTedarikciler_TasimaTedarikciId",
                table: "Araclar",
                column: "TasimaTedarikciId",
                principalTable: "TasimaTedarikciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Personeller_TasimaTedarikciler_TasimaTedarikciId",
                table: "Personeller",
                column: "TasimaTedarikciId",
                principalTable: "TasimaTedarikciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Araclar_TasimaTedarikciler_TasimaTedarikciId",
                table: "Araclar");

            migrationBuilder.DropForeignKey(
                name: "FK_Personeller_TasimaTedarikciler_TasimaTedarikciId",
                table: "Personeller");

            migrationBuilder.DropTable(
                name: "TasimaTedarikciIsler");

            migrationBuilder.DropTable(
                name: "TasimaTedarikciler");

            migrationBuilder.DropIndex(
                name: "IX_Personeller_TasimaTedarikciId",
                table: "Personeller");

            migrationBuilder.DropIndex(
                name: "IX_Araclar_TasimaTedarikciId",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "TasimaTedarikciId",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "TasimaTedarikciId",
                table: "Araclar");
        }
    }
}



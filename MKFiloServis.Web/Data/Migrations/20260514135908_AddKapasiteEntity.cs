using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKapasiteEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guzergahlar_Kurumlar_KurumId",
                table: "Guzergahlar");

            migrationBuilder.DropIndex(
                name: "IX_Guzergahlar_KurumId",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "GelirFiyati",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "GiderFiyati",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "KurumId",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "YonBilgisi",
                table: "Guzergahlar");

            migrationBuilder.AddColumn<decimal>(
                name: "GiderFiyat",
                table: "Guzergahlar",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "KullaniciId",
                table: "FiloGuzergahEslestirmeleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KullaniciId",
                table: "FiloGunlukPuantajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Kapasiteler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    KapasiteAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Carpan = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kapasiteler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kapasiteler_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiloGuzergahEslestirmeleri_KullaniciId",
                table: "FiloGuzergahEslestirmeleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloGunlukPuantajlar_KullaniciId",
                table: "FiloGunlukPuantajlar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Kapasiteler_SirketId_KapasiteAdi",
                table: "Kapasiteler",
                columns: new[] { "SirketId", "KapasiteAdi" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FiloGunlukPuantajlar_Kullanicilar_KullaniciId",
                table: "FiloGunlukPuantajlar",
                column: "KullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Kullanicilar_KullaniciId",
                table: "FiloGuzergahEslestirmeleri",
                column: "KullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiloGunlukPuantajlar_Kullanicilar_KullaniciId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Kullanicilar_KullaniciId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropTable(
                name: "Kapasiteler");

            migrationBuilder.DropIndex(
                name: "IX_FiloGuzergahEslestirmeleri_KullaniciId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropIndex(
                name: "IX_FiloGunlukPuantajlar_KullaniciId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "GiderFiyat",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "KullaniciId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropColumn(
                name: "KullaniciId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.AddColumn<decimal>(
                name: "GelirFiyati",
                table: "Guzergahlar",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GiderFiyati",
                table: "Guzergahlar",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KurumId",
                table: "Guzergahlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YonBilgisi",
                table: "Guzergahlar",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guzergahlar_KurumId",
                table: "Guzergahlar",
                column: "KurumId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Kurumlar_KurumId",
                table: "Guzergahlar",
                column: "KurumId",
                principalTable: "Kurumlar",
                principalColumn: "Id");
        }
    }
}



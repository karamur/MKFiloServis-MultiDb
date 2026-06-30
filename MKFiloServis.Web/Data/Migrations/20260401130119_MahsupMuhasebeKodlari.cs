using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MahsupMuhasebeKodlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KostMerkeziKodu",
                table: "BankaKasaHareketleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MuhasebeAciklama",
                table: "BankaKasaHareketleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MuhasebeAltHesapKodu",
                table: "BankaKasaHareketleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MuhasebeHesapKodu",
                table: "BankaKasaHareketleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjeKodu",
                table: "BankaKasaHareketleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VarsayilanKostMerkezi",
                table: "BankaHesaplari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VarsayilanMuhasebeKodu",
                table: "BankaHesaplari",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KostMerkezleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KostKodu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KostAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    UstKostMerkeziId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KostMerkezleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KostMerkezleri_KostMerkezleri_UstKostMerkeziId",
                        column: x => x.UstKostMerkeziId,
                        principalTable: "KostMerkezleri",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MuhasebeProjeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjeKodu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProjeAdi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    ButceTutar = table.Column<decimal>(type: "numeric", nullable: true),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuhasebeProjeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuhasebeProjeler_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MuhasebeProjeler_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_KostMerkezleri_UstKostMerkeziId",
                table: "KostMerkezleri",
                column: "UstKostMerkeziId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeProjeler_CariId",
                table: "MuhasebeProjeler",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeProjeler_FirmaId",
                table: "MuhasebeProjeler",
                column: "FirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KostMerkezleri");

            migrationBuilder.DropTable(
                name: "MuhasebeProjeler");

            migrationBuilder.DropColumn(
                name: "KostMerkeziKodu",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "MuhasebeAciklama",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "MuhasebeAltHesapKodu",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "MuhasebeHesapKodu",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "ProjeKodu",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "VarsayilanKostMerkezi",
                table: "BankaHesaplari");

            migrationBuilder.DropColumn(
                name: "VarsayilanMuhasebeKodu",
                table: "BankaHesaplari");
        }
    }
}



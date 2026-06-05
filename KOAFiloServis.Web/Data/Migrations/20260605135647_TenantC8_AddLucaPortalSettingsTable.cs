using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantC8_AddLucaPortalSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LucaPortalAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sifre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PortalUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefreshToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TokenGecerlilikTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OtomatikSenkron = table.Column<bool>(type: "boolean", nullable: false),
                    SenkronAralikSaat = table.Column<int>(type: "integer", nullable: false),
                    SonSenkronTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    LucaFirmaKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LucaPortalAyarlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LucaPortalAyarlari_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LucaPortalAyarlari_FirmaId",
                table: "LucaPortalAyarlari",
                column: "FirmaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HoldingVeriler_Firmalar_FirmaId",
                table: "HoldingVeriler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HoldingVeriler_Firmalar_FirmaId",
                table: "HoldingVeriler");

            migrationBuilder.DropTable(
                name: "LucaPortalAyarlari");
        }
    }
}

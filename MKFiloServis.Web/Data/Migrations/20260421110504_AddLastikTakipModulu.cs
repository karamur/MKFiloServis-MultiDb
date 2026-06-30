using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLastikTakipModulu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FirmaId, SgkCalismaTuru, IX_Personeller_FirmaId ve FK_Personeller_Firmalar_FirmaId zaten DB'de mevcut
            migrationBuilder.CreateTable(
                name: "LastikDepolar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    DepoAdi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Adres = table.Column<string>(type: "text", nullable: true),
                    SorumluKisi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LastikDepolar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LastikDepolar_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LastikStoklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    DepoId = table.Column<int>(type: "integer", nullable: false),
                    Marka = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Ebat = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Sezon = table.Column<int>(type: "integer", nullable: false),
                    SeriNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Adet = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LastikStoklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LastikStoklar_LastikDepolar_DepoId",
                        column: x => x.DepoId,
                        principalTable: "LastikDepolar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LastikStoklar_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LastikDegisimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    DegisimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    KmDurumu = table.Column<int>(type: "integer", nullable: true),
                    DegisimTipi = table.Column<int>(type: "integer", nullable: false),
                    SokulenStokId = table.Column<int>(type: "integer", nullable: true),
                    SokulenPozisyon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HedefDepoId = table.Column<int>(type: "integer", nullable: true),
                    TakilanStokId = table.Column<int>(type: "integer", nullable: true),
                    TakilanPozisyon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    KaynakDepoId = table.Column<int>(type: "integer", nullable: true),
                    YapilanYer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Ucret = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LastikDegisimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LastikDegisimler_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LastikDegisimler_LastikDepolar_HedefDepoId",
                        column: x => x.HedefDepoId,
                        principalTable: "LastikDepolar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LastikDegisimler_LastikDepolar_KaynakDepoId",
                        column: x => x.KaynakDepoId,
                        principalTable: "LastikDepolar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LastikDegisimler_LastikStoklar_SokulenStokId",
                        column: x => x.SokulenStokId,
                        principalTable: "LastikStoklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LastikDegisimler_LastikStoklar_TakilanStokId",
                        column: x => x.TakilanStokId,
                        principalTable: "LastikStoklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LastikDegisimler_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LastikDegisimler_AracId",
                table: "LastikDegisimler",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_LastikDegisimler_HedefDepoId",
                table: "LastikDegisimler",
                column: "HedefDepoId");

            migrationBuilder.CreateIndex(
                name: "IX_LastikDegisimler_KaynakDepoId",
                table: "LastikDegisimler",
                column: "KaynakDepoId");

            migrationBuilder.CreateIndex(
                name: "IX_LastikDegisimler_SirketId",
                table: "LastikDegisimler",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_LastikDegisimler_SokulenStokId",
                table: "LastikDegisimler",
                column: "SokulenStokId");

            migrationBuilder.CreateIndex(
                name: "IX_LastikDegisimler_TakilanStokId",
                table: "LastikDegisimler",
                column: "TakilanStokId");

            migrationBuilder.CreateIndex(
                name: "IX_LastikDepolar_SirketId",
                table: "LastikDepolar",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_LastikStoklar_DepoId",
                table: "LastikStoklar",
                column: "DepoId");

            migrationBuilder.CreateIndex(
                name: "IX_LastikStoklar_SirketId",
                table: "LastikStoklar",
                column: "SirketId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LastikDegisimler");

            migrationBuilder.DropTable(
                name: "LastikStoklar");

            migrationBuilder.DropTable(
                name: "LastikDepolar");

        }
    }
}



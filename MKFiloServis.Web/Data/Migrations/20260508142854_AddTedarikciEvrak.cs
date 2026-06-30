using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTedarikciEvrak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TedarikciEvraklari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TasimaTedarikciId = table.Column<int>(type: "integer", nullable: false),
                    EvrakKategorisi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EvrakAdi = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    HatirlatmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    SigortaSirketi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PoliceNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    HatirlatmaAktif = table.Column<bool>(type: "boolean", nullable: false),
                    HatirlatmaGunOnce = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikciEvraklari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikciEvraklari_TasimaTedarikciler_TasimaTedarikciId",
                        column: x => x.TasimaTedarikciId,
                        principalTable: "TasimaTedarikciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TedarikciEvrakDosyalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TedarikciEvrakId = table.Column<int>(type: "integer", nullable: false),
                    DosyaAdi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DosyaYolu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DosyaTipi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DosyaBoyutu = table.Column<long>(type: "bigint", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    VersiyonNo = table.Column<int>(type: "integer", nullable: false),
                    SonDegisiklikNotu = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikciEvrakDosyalari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikciEvrakDosyalari_TedarikciEvraklari_TedarikciEvrakId",
                        column: x => x.TedarikciEvrakId,
                        principalTable: "TedarikciEvraklari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciEvrakDosyalari_TedarikciEvrakId",
                table: "TedarikciEvrakDosyalari",
                column: "TedarikciEvrakId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciEvraklari_TasimaTedarikciId_EvrakKategorisi",
                table: "TedarikciEvraklari",
                columns: new[] { "TasimaTedarikciId", "EvrakKategorisi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TedarikciEvrakDosyalari");

            migrationBuilder.DropTable(
                name: "TedarikciEvraklari");
        }
    }
}



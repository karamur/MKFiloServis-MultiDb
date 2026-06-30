using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TekrarlayanOdeme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TekrarlayanOdemeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OdemeAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MasrafKalemi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Periyod = table.Column<int>(type: "integer", nullable: false),
                    OdemeGunu = table.Column<int>(type: "integer", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    HatirlatmaGunSayisi = table.Column<int>(type: "integer", nullable: false),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Renk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notlar = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TekrarlayanOdemeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TekrarlayanOdemeler_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TekrarlayanOdemeler_FirmaId",
                table: "TekrarlayanOdemeler",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_TekrarlayanOdemeler_MasrafKalemi",
                table: "TekrarlayanOdemeler",
                column: "MasrafKalemi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TekrarlayanOdemeler");
        }
    }
}



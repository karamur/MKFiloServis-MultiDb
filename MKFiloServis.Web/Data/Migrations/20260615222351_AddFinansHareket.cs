using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinansHareket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinansHareketler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BorcMu = table.Column<bool>(type: "boolean", nullable: false),
                    HesapId = table.Column<int>(type: "integer", nullable: false),
                    KarsiHesapId = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReferansNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AktarildiMi = table.Column<bool>(type: "boolean", nullable: false),
                    MuhasebeFisId = table.Column<int>(type: "integer", nullable: true),
                    IptalFisId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinansHareketler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinansHareketler_MuhasebeHesaplari_HesapId",
                        column: x => x.HesapId,
                        principalTable: "MuhasebeHesaplari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinansHareketler_MuhasebeHesaplari_KarsiHesapId",
                        column: x => x.KarsiHesapId,
                        principalTable: "MuhasebeHesaplari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinansHareketler_FirmaId_Tarih",
                table: "FinansHareketler",
                columns: new[] { "FirmaId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_FinansHareketler_HesapId",
                table: "FinansHareketler",
                column: "HesapId");

            migrationBuilder.CreateIndex(
                name: "IX_FinansHareketler_KarsiHesapId",
                table: "FinansHareketler",
                column: "KarsiHesapId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinansHareketler");
        }
    }
}



using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonelAracAtama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonelAracAtamalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SoforId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonelAracAtamalari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonelAracAtamalari_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonelAracAtamalari_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonelAracAtamalari_AracId",
                table: "PersonelAracAtamalari",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelAracAtamalari_SoforId_AracId_BaslangicTarihi",
                table: "PersonelAracAtamalari",
                columns: new[] { "SoforId", "AracId", "BaslangicTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonelAracAtamalari");
        }
    }
}



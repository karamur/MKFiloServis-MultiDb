using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIhaleSozlesmeRevizyonlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IhaleSozlesmeRevizyonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IhaleProjeId = table.Column<int>(type: "integer", nullable: false),
                    RevizyonTipi = table.Column<int>(type: "integer", nullable: false),
                    RevizyonNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Baslik = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RevizyonTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    YurutmeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BedelFarki = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SureFarkiAy = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IhaleSozlesmeRevizyonlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IhaleSozlesmeRevizyonlari_IhaleProjeleri_IhaleProjeId",
                        column: x => x.IhaleProjeId,
                        principalTable: "IhaleProjeleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IhaleSozlesmeRevizyonlari_IhaleProjeId_RevizyonNo",
                table: "IhaleSozlesmeRevizyonlari",
                columns: new[] { "IhaleProjeId", "RevizyonNo" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IhaleSozlesmeRevizyonlari");
        }
    }
}



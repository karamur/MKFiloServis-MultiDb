using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKiralikPlakaTakipFaturaDetay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KiralikPlakaTakipFaturalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KiralikPlakaTakipId = table.Column<int>(type: "integer", nullable: false),
                    Sira = table.Column<int>(type: "integer", nullable: false),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    FaturaNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FaturaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FaturaTutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BuAyOdenen = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiralikPlakaTakipFaturalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KiralikPlakaTakipFaturalar_KiralikPlakaTakipler_KiralikPlak~",
                        column: x => x.KiralikPlakaTakipId,
                        principalTable: "KiralikPlakaTakipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KiralikPlakaTakipFaturalar_KiralikPlakaTakipId_Yil_Ay_Sira",
                table: "KiralikPlakaTakipFaturalar",
                columns: new[] { "KiralikPlakaTakipId", "Yil", "Ay", "Sira" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KiralikPlakaTakipFaturalar");
        }
    }
}

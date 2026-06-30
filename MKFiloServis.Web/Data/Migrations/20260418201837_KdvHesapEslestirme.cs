using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class KdvHesapEslestirme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KdvHesapEslestirmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MuhasebeAyarId = table.Column<int>(type: "integer", nullable: false),
                    KdvOrani = table.Column<int>(type: "integer", nullable: false),
                    HesaplananKdvHesabi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IndirilecekKdvHesabi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KdvHesapEslestirmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KdvHesapEslestirmeleri_MuhasebeAyarlari_MuhasebeAyarId",
                        column: x => x.MuhasebeAyarId,
                        principalTable: "MuhasebeAyarlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KdvHesapEslestirmeleri_MuhasebeAyarId",
                table: "KdvHesapEslestirmeleri",
                column: "MuhasebeAyarId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KdvHesapEslestirmeleri");
        }
    }
}



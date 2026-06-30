using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveKiralikCPlakaTakipler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KiralikCPlakaTakipler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KiralikCPlakaTakipler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AylikYillik = table.Column<int>(type: "integer", nullable: false),
                    BaslamaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    FaturaBedeli = table.Column<decimal>(type: "numeric", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsimSoyisim = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KasaDurumu = table.Column<int>(type: "integer", nullable: false),
                    Plaka = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Toplam = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiralikCPlakaTakipler", x => x.Id);
                });
        }
    }
}



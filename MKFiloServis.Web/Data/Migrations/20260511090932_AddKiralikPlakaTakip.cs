using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKiralikPlakaTakip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KiralikCPlakaTakipler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Plaka = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsimSoyisim = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaslamaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    KasaDurumu = table.Column<int>(type: "integer", nullable: false),
                    FaturaBedeli = table.Column<decimal>(type: "numeric", nullable: false),
                    AylikYillik = table.Column<int>(type: "integer", nullable: false),
                    Toplam = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiralikCPlakaTakipler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KiralikPlakaTakipler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Plaka = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    IsimSoyisim = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaslamaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Durum = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KasaDurumu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FaturaOdemesi = table.Column<decimal>(type: "numeric", nullable: false),
                    Periyot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AylikVeyaYillikTutar = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiralikPlakaTakipler", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KiralikCPlakaTakipler");

            migrationBuilder.DropTable(
                name: "KiralikPlakaTakipler");
        }
    }
}



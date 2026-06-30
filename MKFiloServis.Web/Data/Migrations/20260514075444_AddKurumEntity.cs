using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKurumEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kurumlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KurumKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KurumAdi = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    UnvanTam = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    VergiNo = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    VergiDairesi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Adres = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Il = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Ilce = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefon2 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WebSite = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    YetkiliKisi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    YetkiliTelefon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    YetkiliEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notlar = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kurumlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kurumlar_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kurumlar_CariId",
                table: "Kurumlar",
                column: "CariId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kurumlar");
        }
    }
}



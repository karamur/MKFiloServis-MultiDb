using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IkiFaktorAktif",
                table: "Kullanicilar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "IkiFaktorEtkinlestirmeTarihi",
                table: "Kullanicilar",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IkiFaktorSecretKey",
                table: "Kullanicilar",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLoglar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IslemTipi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: true),
                    EntityGuid = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    KullaniciId = table.Column<int>(type: "integer", nullable: true),
                    KullaniciAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAdresi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EskiDeger = table.Column<string>(type: "jsonb", nullable: true),
                    YeniDeger = table.Column<string>(type: "jsonb", nullable: true),
                    DegisenAlanlar = table.Column<string>(type: "jsonb", nullable: true),
                    Aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    IslemTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IslemSuresiMs = table.Column<long>(type: "bigint", nullable: true),
                    Basarili = table.Column<bool>(type: "boolean", nullable: false),
                    HataMesaji = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Kategori = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Seviye = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLoglar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLoglar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditLoglar_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLoglar_KullaniciId",
                table: "AuditLoglar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLoglar_SirketId",
                table: "AuditLoglar",
                column: "SirketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLoglar");

            migrationBuilder.DropColumn(
                name: "IkiFaktorAktif",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "IkiFaktorEtkinlestirmeTarihi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "IkiFaktorSecretKey",
                table: "Kullanicilar");
        }
    }
}



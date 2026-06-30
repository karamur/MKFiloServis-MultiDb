using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppChatEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KullaniciCariler_KullaniciId_CariId",
                table: "KullaniciCariler");

            migrationBuilder.DropIndex(
                name: "IX_DashboardWidgetlar_KullaniciId_WidgetKodu",
                table: "DashboardWidgetlar");

            migrationBuilder.AlterColumn<string>(
                name: "Not",
                table: "KullaniciCariler",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Ayarlar",
                table: "DashboardWidgetlar",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "WhatsAppGruplar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GrupAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppGruplar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppKisiler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdSoyad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppKisiler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppKisiler_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppSablonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Baslik = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icerik = table.Column<string>(type: "text", nullable: false),
                    Parametreler = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppSablonlar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppGrupUyeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GrupId = table.Column<int>(type: "integer", nullable: false),
                    KisiId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppGrupUyeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppGrupUyeler_WhatsAppGruplar_GrupId",
                        column: x => x.GrupId,
                        principalTable: "WhatsAppGruplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WhatsAppGrupUyeler_WhatsAppKisiler_KisiId",
                        column: x => x.KisiId,
                        principalTable: "WhatsAppKisiler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppMesajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GonderenId = table.Column<int>(type: "integer", nullable: true),
                    KisiId = table.Column<int>(type: "integer", nullable: true),
                    GrupId = table.Column<int>(type: "integer", nullable: true),
                    Icerik = table.Column<string>(type: "text", nullable: false),
                    Tipi = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    MesajTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Okundu = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppMesajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppMesajlar_Kullanicilar_GonderenId",
                        column: x => x.GonderenId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WhatsAppMesajlar_WhatsAppGruplar_GrupId",
                        column: x => x.GrupId,
                        principalTable: "WhatsAppGruplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WhatsAppMesajlar_WhatsAppKisiler_KisiId",
                        column: x => x.KisiId,
                        principalTable: "WhatsAppKisiler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciCariler_KullaniciId",
                table: "KullaniciCariler",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgetlar_KullaniciId",
                table: "DashboardWidgetlar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppGrupUyeler_GrupId_KisiId",
                table: "WhatsAppGrupUyeler",
                columns: new[] { "GrupId", "KisiId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppGrupUyeler_KisiId",
                table: "WhatsAppGrupUyeler",
                column: "KisiId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppKisiler_CariId",
                table: "WhatsAppKisiler",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppKisiler_Telefon",
                table: "WhatsAppKisiler",
                column: "Telefon",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMesajlar_GonderenId",
                table: "WhatsAppMesajlar",
                column: "GonderenId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMesajlar_GrupId",
                table: "WhatsAppMesajlar",
                column: "GrupId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMesajlar_KisiId",
                table: "WhatsAppMesajlar",
                column: "KisiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppGrupUyeler");

            migrationBuilder.DropTable(
                name: "WhatsAppMesajlar");

            migrationBuilder.DropTable(
                name: "WhatsAppSablonlar");

            migrationBuilder.DropTable(
                name: "WhatsAppGruplar");

            migrationBuilder.DropTable(
                name: "WhatsAppKisiler");

            migrationBuilder.DropIndex(
                name: "IX_KullaniciCariler_KullaniciId",
                table: "KullaniciCariler");

            migrationBuilder.DropIndex(
                name: "IX_DashboardWidgetlar_KullaniciId",
                table: "DashboardWidgetlar");

            migrationBuilder.AlterColumn<string>(
                name: "Not",
                table: "KullaniciCariler",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Ayarlar",
                table: "DashboardWidgetlar",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciCariler_KullaniciId_CariId",
                table: "KullaniciCariler",
                columns: new[] { "KullaniciId", "CariId" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgetlar_KullaniciId_WidgetKodu",
                table: "DashboardWidgetlar",
                columns: new[] { "KullaniciId", "WidgetKodu" },
                unique: true);
        }
    }
}



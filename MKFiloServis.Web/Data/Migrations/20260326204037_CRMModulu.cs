using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CRMModulu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Renk",
                table: "Roller",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    Baslik = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Icerik = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    Oncelik = table.Column<int>(type: "integer", nullable: false),
                    Okundu = table.Column<bool>(type: "boolean", nullable: false),
                    OkunmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IliskiliTablo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IliskiliKayitId = table.Column<int>(type: "integer", nullable: true),
                    Link = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SonGosterimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Tekrarli = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bildirimler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DashboardWidgetlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    WidgetKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Sira = table.Column<int>(type: "integer", nullable: false),
                    Kolon = table.Column<int>(type: "integer", nullable: false),
                    Genislik = table.Column<int>(type: "integer", nullable: false),
                    Gorunur = table.Column<bool>(type: "boolean", nullable: false),
                    Kucultulmus = table.Column<bool>(type: "boolean", nullable: false),
                    Ayarlar = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardWidgetlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardWidgetlar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: true),
                    SmtpSunucu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SmtpPort = table.Column<int>(type: "integer", nullable: false),
                    SslKullan = table.Column<bool>(type: "boolean", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sifre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GonderenAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAyarlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAyarlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hatirlaticilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    Baslik = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TumGun = table.Column<bool>(type: "boolean", nullable: false),
                    TekrarTipi = table.Column<int>(type: "integer", nullable: false),
                    TekrarAraligi = table.Column<int>(type: "integer", nullable: false),
                    TekrarBitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BildirimDakikaOnce = table.Column<int>(type: "integer", nullable: false),
                    EmailBildirim = table.Column<bool>(type: "boolean", nullable: false),
                    PushBildirim = table.Column<bool>(type: "boolean", nullable: false),
                    IliskiliTablo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IliskiliKayitId = table.Column<int>(type: "integer", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Renk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hatirlaticilar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hatirlaticilar_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Hatirlaticilar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KullaniciCariler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    CariId = table.Column<int>(type: "integer", nullable: false),
                    EkstreGorebilir = table.Column<bool>(type: "boolean", nullable: false),
                    FaturaGorebilir = table.Column<bool>(type: "boolean", nullable: false),
                    OdemeYapabilir = table.Column<bool>(type: "boolean", nullable: false),
                    DuzenlemeYapabilir = table.Column<bool>(type: "boolean", nullable: false),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    Not = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciCariler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KullaniciCariler_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KullaniciCariler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mesajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GonderenId = table.Column<int>(type: "integer", nullable: false),
                    AliciId = table.Column<int>(type: "integer", nullable: true),
                    Konu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Icerik = table.Column<string>(type: "text", nullable: false),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Okundu = table.Column<bool>(type: "boolean", nullable: false),
                    OkunmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DisAlici = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisGonderimId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UstMesajId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Kullanicilar_AliciId",
                        column: x => x.AliciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Kullanicilar_GonderenId",
                        column: x => x.GonderenId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Mesajlar_UstMesajId",
                        column: x => x.UstMesajId,
                        principalTable: "Mesajlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: true),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WebhookUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppAyarlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppAyarlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_KullaniciId_Okundu",
                table: "Bildirimler",
                columns: new[] { "KullaniciId", "Okundu" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgetlar_KullaniciId_WidgetKodu",
                table: "DashboardWidgetlar",
                columns: new[] { "KullaniciId", "WidgetKodu" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailAyarlari_KullaniciId",
                table: "EmailAyarlari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Hatirlaticilar_CariId",
                table: "Hatirlaticilar",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_Hatirlaticilar_KullaniciId_BaslangicTarihi",
                table: "Hatirlaticilar",
                columns: new[] { "KullaniciId", "BaslangicTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciCariler_CariId",
                table: "KullaniciCariler",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciCariler_KullaniciId_CariId",
                table: "KullaniciCariler",
                columns: new[] { "KullaniciId", "CariId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_AliciId_Okundu",
                table: "Mesajlar",
                columns: new[] { "AliciId", "Okundu" });

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_GonderenId",
                table: "Mesajlar",
                column: "GonderenId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_UstMesajId",
                table: "Mesajlar",
                column: "UstMesajId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppAyarlari_KullaniciId",
                table: "WhatsAppAyarlari",
                column: "KullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bildirimler");

            migrationBuilder.DropTable(
                name: "DashboardWidgetlar");

            migrationBuilder.DropTable(
                name: "EmailAyarlari");

            migrationBuilder.DropTable(
                name: "Hatirlaticilar");

            migrationBuilder.DropTable(
                name: "KullaniciCariler");

            migrationBuilder.DropTable(
                name: "Mesajlar");

            migrationBuilder.DropTable(
                name: "WhatsAppAyarlari");

            migrationBuilder.DropColumn(
                name: "Renk",
                table: "Roller");
        }
    }
}



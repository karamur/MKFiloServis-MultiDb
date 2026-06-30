using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIlanYayinVeKullaniciTercihleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IlanPlatformlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlatformAdi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WebSiteUrl = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiUrl = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApiSecret = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    KullaniciAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Sifre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PlatformTipi = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Renk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OtomatikYayinDestegi = table.Column<bool>(type: "boolean", nullable: false),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IlanPlatformlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KullaniciSonIslemler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    SayfaYolu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SayfaBasligi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ErisimZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ErisimSayisi = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciSonIslemler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KullaniciSonIslemler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KullaniciTercihleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    VarsayilanAnasayfa = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AnasayfaWidgetGoster = table.Column<bool>(type: "boolean", nullable: false),
                    AnasayfaWidgetSirasi = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Tema = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SidebarDurum = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmailBildirimAktif = table.Column<bool>(type: "boolean", nullable: false),
                    TarayiciBildirimAktif = table.Column<bool>(type: "boolean", nullable: false),
                    SesBildirimAktif = table.Column<bool>(type: "boolean", nullable: false),
                    VarsayilanSayfaBoyutu = table.Column<int>(type: "integer", nullable: false),
                    VarsayilanSiralama = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DigerTercihler = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciTercihleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KullaniciTercihleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AracIlanIcerikleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    PlatformId = table.Column<int>(type: "integer", nullable: true),
                    IlanBasligi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IlanAciklamasi = table.Column<string>(type: "text", nullable: true),
                    OzellikListesi = table.Column<string>(type: "text", nullable: true),
                    FotografListesi = table.Column<string>(type: "text", nullable: true),
                    VitrinFotografi = table.Column<string>(type: "text", nullable: true),
                    MetaBaslik = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MetaAciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AnahtarKelimeler = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracIlanIcerikleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracIlanIcerikleri_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AracIlanIcerikleri_IlanPlatformlari_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "IlanPlatformlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AracIlanYayinlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    PlatformId = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    YayinBaslangic = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    YayinBitis = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PlatformIlanNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PlatformIlanUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    YayinFiyati = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FiyatGizli = table.Column<bool>(type: "boolean", nullable: false),
                    FiyatAciklama = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GoruntulenmeSayisi = table.Column<int>(type: "integer", nullable: false),
                    TiklamaSayisi = table.Column<int>(type: "integer", nullable: false),
                    FavorilenmeSayisi = table.Column<int>(type: "integer", nullable: false),
                    MesajSayisi = table.Column<int>(type: "integer", nullable: false),
                    SonGuncelleme = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OneCikarildiMi = table.Column<bool>(type: "boolean", nullable: false),
                    OneCikarmaBitis = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OneCikarmaBedeli = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    YayinlayanKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracIlanYayinlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracIlanYayinlar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AracIlanYayinlar_IlanPlatformlari_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "IlanPlatformlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AracIlanYayinlar_Kullanicilar_YayinlayanKullaniciId",
                        column: x => x.YayinlayanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AracIlanIcerikleri_AracId_PlatformId",
                table: "AracIlanIcerikleri",
                columns: new[] { "AracId", "PlatformId" });

            migrationBuilder.CreateIndex(
                name: "IX_AracIlanIcerikleri_PlatformId",
                table: "AracIlanIcerikleri",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_AracIlanYayinlar_AracId_PlatformId",
                table: "AracIlanYayinlar",
                columns: new[] { "AracId", "PlatformId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AracIlanYayinlar_PlatformId",
                table: "AracIlanYayinlar",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_AracIlanYayinlar_YayinlayanKullaniciId",
                table: "AracIlanYayinlar",
                column: "YayinlayanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_IlanPlatformlari_PlatformAdi",
                table: "IlanPlatformlari",
                column: "PlatformAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciSonIslemler_KullaniciId_SayfaYolu",
                table: "KullaniciSonIslemler",
                columns: new[] { "KullaniciId", "SayfaYolu" });

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciTercihleri_KullaniciId",
                table: "KullaniciTercihleri",
                column: "KullaniciId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AracIlanIcerikleri");

            migrationBuilder.DropTable(
                name: "AracIlanYayinlar");

            migrationBuilder.DropTable(
                name: "KullaniciSonIslemler");

            migrationBuilder.DropTable(
                name: "KullaniciTercihleri");

            migrationBuilder.DropTable(
                name: "IlanPlatformlari");
        }
    }
}



using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIhaleTeklifVersiyonlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IhaleTeklifVersiyonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IhaleProjeId = table.Column<int>(type: "integer", nullable: false),
                    VersiyonNo = table.Column<int>(type: "integer", nullable: false),
                    RevizyonKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    RevizyonNotu = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    KararNotu = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HazirlayanKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    OnaylayanKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    HazirlamaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OnayTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AktifVersiyon = table.Column<bool>(type: "boolean", nullable: false),
                    ToplamMaliyet = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TeklifTutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KarMarjiTutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KarMarjiOrani = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IhaleTeklifVersiyonlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IhaleTeklifVersiyonlari_IhaleProjeleri_IhaleProjeId",
                        column: x => x.IhaleProjeId,
                        principalTable: "IhaleProjeleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IhaleTeklifVersiyonlari_Kullanicilar_HazirlayanKullaniciId",
                        column: x => x.HazirlayanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IhaleTeklifVersiyonlari_Kullanicilar_OnaylayanKullaniciId",
                        column: x => x.OnaylayanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IhaleTeklifKararLoglari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IhaleTeklifVersiyonId = table.Column<int>(type: "integer", nullable: false),
                    IslemTipi = table.Column<int>(type: "integer", nullable: false),
                    OncekiDurum = table.Column<int>(type: "integer", nullable: true),
                    YeniDurum = table.Column<int>(type: "integer", nullable: false),
                    Not = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IslemYapanKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    IslemTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IhaleTeklifKararLoglari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IhaleTeklifKararLoglari_IhaleTeklifVersiyonlari_IhaleTeklif~",
                        column: x => x.IhaleTeklifVersiyonId,
                        principalTable: "IhaleTeklifVersiyonlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IhaleTeklifKararLoglari_Kullanicilar_IslemYapanKullaniciId",
                        column: x => x.IslemYapanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IhaleTeklifKararLoglari_IhaleTeklifVersiyonId_IslemTarihi",
                table: "IhaleTeklifKararLoglari",
                columns: new[] { "IhaleTeklifVersiyonId", "IslemTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_IhaleTeklifKararLoglari_IslemYapanKullaniciId",
                table: "IhaleTeklifKararLoglari",
                column: "IslemYapanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleTeklifVersiyonlari_AktifVersiyon",
                table: "IhaleTeklifVersiyonlari",
                column: "IhaleProjeId",
                unique: true,
                filter: "\"AktifVersiyon\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleTeklifVersiyonlari_HazirlayanKullaniciId",
                table: "IhaleTeklifVersiyonlari",
                column: "HazirlayanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleTeklifVersiyonlari_IhaleProjeId_VersiyonNo",
                table: "IhaleTeklifVersiyonlari",
                columns: new[] { "IhaleProjeId", "VersiyonNo" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_IhaleTeklifVersiyonlari_OnaylayanKullaniciId",
                table: "IhaleTeklifVersiyonlari",
                column: "OnaylayanKullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IhaleTeklifKararLoglari");

            migrationBuilder.DropTable(
                name: "IhaleTeklifVersiyonlari");
        }
    }
}



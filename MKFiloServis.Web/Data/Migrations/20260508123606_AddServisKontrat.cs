using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServisKontrat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServisKontratlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    KontratKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KurumCariId = table.Column<int>(type: "integer", nullable: true),
                    GuzergahId = table.Column<int>(type: "integer", nullable: false),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: true),
                    SoforId = table.Column<int>(type: "integer", nullable: true),
                    TasimaTedarikciId = table.Column<int>(type: "integer", nullable: true),
                    TasimaTedarikciIsId = table.Column<int>(type: "integer", nullable: true),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TahsilatTip = table.Column<int>(type: "integer", nullable: false),
                    TahsilatBirimFiyat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    OdemeTip = table.Column<int>(type: "integer", nullable: true),
                    OdemeBirimFiyat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notlar = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServisKontratlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServisKontratlar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServisKontratlar_Cariler_KurumCariId",
                        column: x => x.KurumCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServisKontratlar_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServisKontratlar_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServisKontratlar_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServisKontratlar_TasimaTedarikciIsler_TasimaTedarikciIsId",
                        column: x => x.TasimaTedarikciIsId,
                        principalTable: "TasimaTedarikciIsler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServisKontratlar_TasimaTedarikciler_TasimaTedarikciId",
                        column: x => x.TasimaTedarikciId,
                        principalTable: "TasimaTedarikciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ServisPuantajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    ServisKontratId = table.Column<int>(type: "integer", nullable: false),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    CalismaSayisi = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TahsilatBirimFiyat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TahsilatToplam = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OdemeBirimFiyat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    OdemeToplam = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    OnayanKisi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    OnayTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notlar = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServisPuantajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServisPuantajlar_ServisKontratlar_ServisKontratId",
                        column: x => x.ServisKontratId,
                        principalTable: "ServisKontratlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServisPuantajlar_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServisOdemeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    ServisPuantajId = table.Column<int>(type: "integer", nullable: false),
                    OdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OdemeSekli = table.Column<int>(type: "integer", nullable: false),
                    BelgeNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Odendi = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServisOdemeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServisOdemeler_ServisPuantajlar_ServisPuantajId",
                        column: x => x.ServisPuantajId,
                        principalTable: "ServisPuantajlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServisOdemeler_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServisTahsilatlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketId = table.Column<int>(type: "integer", nullable: true),
                    ServisPuantajId = table.Column<int>(type: "integer", nullable: false),
                    TahsilatTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TahsilatSekli = table.Column<int>(type: "integer", nullable: false),
                    BelgeNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Tahsil = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServisTahsilatlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServisTahsilatlar_ServisPuantajlar_ServisPuantajId",
                        column: x => x.ServisPuantajId,
                        principalTable: "ServisPuantajlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServisTahsilatlar_Sirketler_SirketId",
                        column: x => x.SirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServisKontratlar_AracId",
                table: "ServisKontratlar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKontratlar_GuzergahId",
                table: "ServisKontratlar",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKontratlar_KontratKodu",
                table: "ServisKontratlar",
                column: "KontratKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServisKontratlar_KurumCariId",
                table: "ServisKontratlar",
                column: "KurumCariId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKontratlar_SirketId",
                table: "ServisKontratlar",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKontratlar_SoforId",
                table: "ServisKontratlar",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKontratlar_TasimaTedarikciId",
                table: "ServisKontratlar",
                column: "TasimaTedarikciId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKontratlar_TasimaTedarikciIsId",
                table: "ServisKontratlar",
                column: "TasimaTedarikciIsId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisOdemeler_ServisPuantajId",
                table: "ServisOdemeler",
                column: "ServisPuantajId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisOdemeler_SirketId",
                table: "ServisOdemeler",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisPuantajlar_ServisKontratId_Yil_Ay",
                table: "ServisPuantajlar",
                columns: new[] { "ServisKontratId", "Yil", "Ay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServisPuantajlar_SirketId",
                table: "ServisPuantajlar",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisTahsilatlar_ServisPuantajId",
                table: "ServisTahsilatlar",
                column: "ServisPuantajId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisTahsilatlar_SirketId",
                table: "ServisTahsilatlar",
                column: "SirketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServisOdemeler");

            migrationBuilder.DropTable(
                name: "ServisTahsilatlar");

            migrationBuilder.DropTable(
                name: "ServisPuantajlar");

            migrationBuilder.DropTable(
                name: "ServisKontratlar");
        }
    }
}



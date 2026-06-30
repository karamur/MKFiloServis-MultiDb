using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStokEnvanterModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StokKategoriler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KategoriAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    UstKategoriId = table.Column<int>(type: "integer", nullable: true),
                    Renk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Sira = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokKategoriler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokKategoriler_StokKategoriler_UstKategoriId",
                        column: x => x.UstKategoriId,
                        principalTable: "StokKategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StokKartlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StokKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StokAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Barkod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StokTipi = table.Column<int>(type: "integer", nullable: false),
                    AltTipi = table.Column<int>(type: "integer", nullable: true),
                    KategoriId = table.Column<int>(type: "integer", nullable: true),
                    Birim = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AlisFiyati = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SatisFiyati = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    StokTakibiYapilsin = table.Column<bool>(type: "boolean", nullable: false),
                    MinStokMiktari = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MaksStokMiktari = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MevcutStok = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    VarsayilanTedarikciId = table.Column<int>(type: "integer", nullable: true),
                    MuhasebeHesapId = table.Column<int>(type: "integer", nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    ResimUrl = table.Column<string>(type: "text", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokKartlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokKartlari_Cariler_VarsayilanTedarikciId",
                        column: x => x.VarsayilanTedarikciId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StokKartlari_MuhasebeHesaplari_MuhasebeHesapId",
                        column: x => x.MuhasebeHesapId,
                        principalTable: "MuhasebeHesaplari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StokKartlari_StokKategoriler_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "StokKategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StokHareketler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StokKartiId = table.Column<int>(type: "integer", nullable: false),
                    IslemTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BelgeNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HareketTipi = table.Column<int>(type: "integer", nullable: false),
                    Miktar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    FaturaKalemId = table.Column<int>(type: "integer", nullable: true),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    AracId = table.Column<int>(type: "integer", nullable: true),
                    AracMasrafId = table.Column<int>(type: "integer", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    DepoId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokHareketler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokHareketler_AracMasraflari_AracMasrafId",
                        column: x => x.AracMasrafId,
                        principalTable: "AracMasraflari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StokHareketler_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StokHareketler_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StokHareketler_FaturaKalemleri_FaturaKalemId",
                        column: x => x.FaturaKalemId,
                        principalTable: "FaturaKalemleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StokHareketler_Faturalar_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StokHareketler_StokKartlari_StokKartiId",
                        column: x => x.StokKartiId,
                        principalTable: "StokKartlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AracIslemler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    IslemTipi = table.Column<int>(type: "integer", nullable: false),
                    IslemTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    KdvTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    StokHareketId = table.Column<int>(type: "integer", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    Kilometre = table.Column<int>(type: "integer", nullable: true),
                    NoterId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NoterTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracIslemler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracIslemler_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AracIslemler_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AracIslemler_Faturalar_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AracIslemler_StokHareketler_StokHareketId",
                        column: x => x.StokHareketId,
                        principalTable: "StokHareketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ServisKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    ServisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ServisciCariId = table.Column<int>(type: "integer", nullable: true),
                    ServisTipi = table.Column<int>(type: "integer", nullable: false),
                    ServisAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    IscilikTutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ParcaTutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    KdvTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Kilometre = table.Column<int>(type: "integer", nullable: true),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    AracMasrafId = table.Column<int>(type: "integer", nullable: true),
                    StokHareketId = table.Column<int>(type: "integer", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    GarantiKapsaminda = table.Column<bool>(type: "boolean", nullable: false),
                    GarantiBitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServisKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServisKayitlari_AracMasraflari_AracMasrafId",
                        column: x => x.AracMasrafId,
                        principalTable: "AracMasraflari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServisKayitlari_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServisKayitlari_Cariler_ServisciCariId",
                        column: x => x.ServisciCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServisKayitlari_Faturalar_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServisKayitlari_StokHareketler_StokHareketId",
                        column: x => x.StokHareketId,
                        principalTable: "StokHareketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ServisParcalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServisKaydiId = table.Column<int>(type: "integer", nullable: false),
                    StokKartiId = table.Column<int>(type: "integer", nullable: true),
                    ParcaAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Miktar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Birim = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServisParcalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServisParcalar_ServisKayitlari_ServisKaydiId",
                        column: x => x.ServisKaydiId,
                        principalTable: "ServisKayitlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServisParcalar_StokKartlari_StokKartiId",
                        column: x => x.StokKartiId,
                        principalTable: "StokKartlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AracIslemler_AracId_IslemTarihi",
                table: "AracIslemler",
                columns: new[] { "AracId", "IslemTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_AracIslemler_CariId",
                table: "AracIslemler",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_AracIslemler_FaturaId",
                table: "AracIslemler",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_AracIslemler_StokHareketId",
                table: "AracIslemler",
                column: "StokHareketId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKayitlari_AracId_ServisTarihi",
                table: "ServisKayitlari",
                columns: new[] { "AracId", "ServisTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_ServisKayitlari_AracMasrafId",
                table: "ServisKayitlari",
                column: "AracMasrafId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKayitlari_FaturaId",
                table: "ServisKayitlari",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKayitlari_ServisciCariId",
                table: "ServisKayitlari",
                column: "ServisciCariId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKayitlari_StokHareketId",
                table: "ServisKayitlari",
                column: "StokHareketId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisParcalar_ServisKaydiId",
                table: "ServisParcalar",
                column: "ServisKaydiId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisParcalar_StokKartiId",
                table: "ServisParcalar",
                column: "StokKartiId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketler_AracId",
                table: "StokHareketler",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketler_AracMasrafId",
                table: "StokHareketler",
                column: "AracMasrafId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketler_CariId",
                table: "StokHareketler",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketler_FaturaId",
                table: "StokHareketler",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketler_FaturaKalemId",
                table: "StokHareketler",
                column: "FaturaKalemId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketler_StokKartiId_IslemTarihi",
                table: "StokHareketler",
                columns: new[] { "StokKartiId", "IslemTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_StokKartlari_KategoriId",
                table: "StokKartlari",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_StokKartlari_MuhasebeHesapId",
                table: "StokKartlari",
                column: "MuhasebeHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_StokKartlari_StokKodu",
                table: "StokKartlari",
                column: "StokKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StokKartlari_VarsayilanTedarikciId",
                table: "StokKartlari",
                column: "VarsayilanTedarikciId");

            migrationBuilder.CreateIndex(
                name: "IX_StokKategoriler_UstKategoriId",
                table: "StokKategoriler",
                column: "UstKategoriId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AracIslemler");

            migrationBuilder.DropTable(
                name: "ServisParcalar");

            migrationBuilder.DropTable(
                name: "ServisKayitlari");

            migrationBuilder.DropTable(
                name: "StokHareketler");

            migrationBuilder.DropTable(
                name: "StokKartlari");

            migrationBuilder.DropTable(
                name: "StokKategoriler");
        }
    }
}



using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class EbysEvrakKategoriler_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                ApplyPostgreSqlSafeUp(migrationBuilder);
                return;
            }

            migrationBuilder.DropForeignKey(
                name: "FK_CariHatirlatmalar_Cariler_CariId",
                table: "CariHatirlatmalar");

            migrationBuilder.DropForeignKey(
                name: "FK_CariHatirlatmalar_Faturalar_FaturaId",
                table: "CariHatirlatmalar");

            migrationBuilder.DropForeignKey(
                name: "FK_CariHatirlatmalar_Firmalar_FirmaId",
                table: "CariHatirlatmalar");

            migrationBuilder.DropIndex(
                name: "IX_CariHatirlatmalar_CariId",
                table: "CariHatirlatmalar");

            migrationBuilder.AddColumn<int>(
                name: "OnayDurumu",
                table: "PersonelPuantajlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OnayNotu",
                table: "PersonelPuantajlar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnayTarihi",
                table: "PersonelPuantajlar",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnaylayanKullanici",
                table: "PersonelPuantajlar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DosyaAdi",
                table: "PersonelOzlukEvraklar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DosyaBoyutu",
                table: "PersonelOzlukEvraklar",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DosyaTipi",
                table: "PersonelOzlukEvraklar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SonDegisiklikNotu",
                table: "PersonelOzlukEvraklar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersiyonNo",
                table: "PersonelOzlukEvraklar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MuhasebeHesapId",
                table: "Personeller",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SirketId",
                table: "Personeller",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SirketId",
                table: "Kullanicilar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BaslangicLatitude",
                table: "Guzergahlar",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BaslangicLongitude",
                table: "Guzergahlar",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BitisLatitude",
                table: "Guzergahlar",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BitisLongitude",
                table: "Guzergahlar",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RotaRengi",
                table: "Guzergahlar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SirketId",
                table: "Guzergahlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GibDurumGuncellemeTarihi",
                table: "Faturalar",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GibDurumMesaji",
                table: "Faturalar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GibDurumu",
                table: "Faturalar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "GibGonderimTarihi",
                table: "Faturalar",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SirketId",
                table: "Faturalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SonDegisiklikNotu",
                table: "EbysEvrakDosyalar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersiyonNo",
                table: "EbysEvrakDosyalar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SirketId",
                table: "Cariler",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Tutar",
                table: "CariHatirlatmalar",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Baslik",
                table: "CariHatirlatmalar",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Aciklama",
                table: "CariHatirlatmalar",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SirketId",
                table: "BankaKasaHareketleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SirketId",
                table: "BankaHesaplari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SirketId",
                table: "Araclar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SonDegisiklikNotu",
                table: "AracEvrakDosyalari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersiyonNo",
                table: "AracEvrakDosyalari",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AracBolgeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BolgeAdi = table.Column<string>(type: "text", nullable: false),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    MerkezLatitude = table.Column<double>(type: "double precision", nullable: true),
                    MerkezLongitude = table.Column<double>(type: "double precision", nullable: true),
                    YaricapMetre = table.Column<double>(type: "double precision", nullable: true),
                    PoligonKoordinatlari = table.Column<string>(type: "text", nullable: true),
                    Renk = table.Column<string>(type: "text", nullable: true),
                    GirisBildirimi = table.Column<bool>(type: "boolean", nullable: false),
                    CikisBildirimi = table.Column<bool>(type: "boolean", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracBolgeler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AracEvrakDosyaVersiyonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracEvrakDosyaId = table.Column<int>(type: "integer", nullable: false),
                    VersiyonNo = table.Column<int>(type: "integer", nullable: false),
                    DosyaAdi = table.Column<string>(type: "text", nullable: false),
                    DosyaYolu = table.Column<string>(type: "text", nullable: false),
                    DosyaTipi = table.Column<string>(type: "text", nullable: true),
                    DosyaBoyutu = table.Column<long>(type: "bigint", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    DegisiklikNotu = table.Column<string>(type: "text", nullable: true),
                    OlusturanKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracEvrakDosyaVersiyonlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracEvrakDosyaVersiyonlar_AracEvrakDosyalari_AracEvrakDosya~",
                        column: x => x.AracEvrakDosyaId,
                        principalTable: "AracEvrakDosyalari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AracEvrakDosyaVersiyonlar_Kullanicilar_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AracTakipCihazlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    CihazId = table.Column<string>(type: "text", nullable: false),
                    CihazMarka = table.Column<string>(type: "text", nullable: true),
                    CihazModel = table.Column<string>(type: "text", nullable: true),
                    SimKartNo = table.Column<string>(type: "text", nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    KurulumTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SonIletisimZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BataryaSeviyesi = table.Column<int>(type: "integer", nullable: true),
                    SinyalGucu = table.Column<int>(type: "integer", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracTakipCihazlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracTakipCihazlar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BildirimAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    FaturaVadeUyarisi = table.Column<bool>(type: "boolean", nullable: false),
                    EhliyetBitisUyarisi = table.Column<bool>(type: "boolean", nullable: false),
                    SrcBelgesiUyarisi = table.Column<bool>(type: "boolean", nullable: false),
                    PsikoteknikUyarisi = table.Column<bool>(type: "boolean", nullable: false),
                    SaglikRaporuUyarisi = table.Column<bool>(type: "boolean", nullable: false),
                    TrafikSigortaUyarisi = table.Column<bool>(type: "boolean", nullable: false),
                    KaskoUyarisi = table.Column<bool>(type: "boolean", nullable: false),
                    MuayeneUyarisi = table.Column<bool>(type: "boolean", nullable: false),
                    DestekTalebiUyarisi = table.Column<bool>(type: "boolean", nullable: false),
                    SistemBildirimleri = table.Column<bool>(type: "boolean", nullable: false),
                    EpostaAlsin = table.Column<bool>(type: "boolean", nullable: false),
                    EpostaAdresi = table.Column<string>(type: "text", nullable: true),
                    SmsAlsin = table.Column<bool>(type: "boolean", nullable: false),
                    SmsTelefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SmsVadeHatirlatma = table.Column<bool>(type: "boolean", nullable: false),
                    SmsBelgeHatirlatma = table.Column<bool>(type: "boolean", nullable: false),
                    VadeUyariGunSayisi = table.Column<int>(type: "integer", nullable: false),
                    BelgeUyariGunSayisi = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BildirimAyarlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BildirimAyarlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EbysAramaGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    AramaMetni = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FiltreJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SonucSayisi = table.Column<int>(type: "integer", nullable: false),
                    AramaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbysAramaGecmisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EbysAramaGecmisleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EbysBelgeEmbeddingler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kaynak = table.Column<int>(type: "integer", nullable: false),
                    KaynakId = table.Column<int>(type: "integer", nullable: false),
                    DosyaId = table.Column<int>(type: "integer", nullable: true),
                    Metin = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    MetinOzet = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmbeddingJson = table.Column<string>(type: "text", nullable: false),
                    EmbeddingBoyutu = table.Column<int>(type: "integer", nullable: false),
                    ModelAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbysBelgeEmbeddingler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EbysEvrakDosyaVersiyonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvrakDosyaId = table.Column<int>(type: "integer", nullable: false),
                    VersiyonNo = table.Column<int>(type: "integer", nullable: false),
                    DosyaAdi = table.Column<string>(type: "text", nullable: false),
                    DosyaYolu = table.Column<string>(type: "text", nullable: false),
                    DosyaTipi = table.Column<string>(type: "text", nullable: true),
                    DosyaBoyutu = table.Column<long>(type: "bigint", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    DegisiklikNotu = table.Column<string>(type: "text", nullable: true),
                    OlusturanKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbysEvrakDosyaVersiyonlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EbysEvrakDosyaVersiyonlar_EbysEvrakDosyalar_EvrakDosyaId",
                        column: x => x.EvrakDosyaId,
                        principalTable: "EbysEvrakDosyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EbysEvrakDosyaVersiyonlar_Kullanicilar_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EbysKayitliAramalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    AramaAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    FiltreJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    BildirimAktif = table.Column<bool>(type: "boolean", nullable: false),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbysKayitliAramalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EbysKayitliAramalar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EpostaBildirimLoglari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    EpostaAdresi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UyariSayisi = table.Column<int>(type: "integer", nullable: false),
                    GonderimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Basarili = table.Column<bool>(type: "boolean", nullable: false),
                    HataMesaji = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpostaBildirimLoglari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpostaBildirimLoglari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonelOzlukEvrakVersiyonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonelOzlukEvrakId = table.Column<int>(type: "integer", nullable: false),
                    VersiyonNo = table.Column<int>(type: "integer", nullable: false),
                    DosyaYolu = table.Column<string>(type: "text", nullable: true),
                    DosyaAdi = table.Column<string>(type: "text", nullable: true),
                    DosyaTipi = table.Column<string>(type: "text", nullable: true),
                    DosyaBoyutu = table.Column<long>(type: "bigint", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    DegisiklikNotu = table.Column<string>(type: "text", nullable: true),
                    OlusturanKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonelOzlukEvrakVersiyonlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonelOzlukEvrakVersiyonlar_Kullanicilar_OlusturanKullani~",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelOzlukEvrakVersiyonlar_PersonelOzlukEvraklar_Persone~",
                        column: x => x.PersonelOzlukEvrakId,
                        principalTable: "PersonelOzlukEvraklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sirketler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SirketKodu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Unvan = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    KisaAd = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VergiDairesi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VergiNo = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    Adres = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Il = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Ilce = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PostaKodu = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WebSitesi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    AyarlarJson = table.Column<string>(type: "text", nullable: true),
                    LisansBitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MaxKullaniciSayisi = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sirketler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmsAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GondericiNumara = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ApiUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Bakiye = table.Column<decimal>(type: "numeric", nullable: true),
                    SonBakiyeSorguTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ToplamGonderilenSms = table.Column<int>(type: "integer", nullable: false),
                    ToplamBasarisizSms = table.Column<int>(type: "integer", nullable: false),
                    SonGonderimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsAyarlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsAyarlari_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SmsSablonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    Adi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Sablon = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Varsayilan = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsSablonlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsSablonlari_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WebhookEndpointler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Secret = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    MaxRetry = table.Column<int>(type: "integer", nullable: false),
                    RetryDelaySaniye = table.Column<int>(type: "integer", nullable: false),
                    OlayFiltresi = table.Column<string>(type: "text", nullable: true),
                    HttpMethod = table.Column<string>(type: "text", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    ToplamGonderim = table.Column<int>(type: "integer", nullable: false),
                    BasariliGonderim = table.Column<int>(type: "integer", nullable: false),
                    BasarisizGonderim = table.Column<int>(type: "integer", nullable: false),
                    SonGonderimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SonBasariliTarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEndpointler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AracBolgeAtamalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracBolgeId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracBolgeAtamalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracBolgeAtamalar_AracBolgeler_AracBolgeId",
                        column: x => x.AracBolgeId,
                        principalTable: "AracBolgeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AracBolgeAtamalar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AracKonumlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracTakipCihazId = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Hiz = table.Column<double>(type: "double precision", nullable: true),
                    Yon = table.Column<double>(type: "double precision", nullable: true),
                    Rakım = table.Column<double>(type: "double precision", nullable: true),
                    Hassasiyet = table.Column<double>(type: "double precision", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    KontakDurumu = table.Column<bool>(type: "boolean", nullable: true),
                    MotorDurumu = table.Column<bool>(type: "boolean", nullable: true),
                    YakitSeviyesi = table.Column<int>(type: "integer", nullable: true),
                    Kilometre = table.Column<int>(type: "integer", nullable: true),
                    Sicaklik = table.Column<double>(type: "double precision", nullable: true),
                    OlayTipi = table.Column<int>(type: "integer", nullable: false),
                    Adres = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracKonumlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracKonumlar_AracTakipCihazlar_AracTakipCihazId",
                        column: x => x.AracTakipCihazId,
                        principalTable: "AracTakipCihazlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AracTakipAlarmlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracTakipCihazId = table.Column<int>(type: "integer", nullable: false),
                    AlarmTipi = table.Column<int>(type: "integer", nullable: false),
                    AlarmZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Mesaj = table.Column<string>(type: "text", nullable: true),
                    Deger = table.Column<double>(type: "double precision", nullable: true),
                    Okundu = table.Column<bool>(type: "boolean", nullable: false),
                    Islendi = table.Column<bool>(type: "boolean", nullable: false),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracTakipAlarmlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracTakipAlarmlar_AracTakipCihazlar_AracTakipCihazId",
                        column: x => x.AracTakipCihazId,
                        principalTable: "AracTakipCihazlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SirketTransferLoglari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityTuru = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    EntityAciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    KaynakSirketId = table.Column<int>(type: "integer", nullable: false),
                    HedefSirketId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    TransferTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    HataMesaji = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IliskiliVerilerTransferEdildi = table.Column<bool>(type: "boolean", nullable: false),
                    IliskiliEntitySayisi = table.Column<int>(type: "integer", nullable: false),
                    Notlar = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SirketTransferLoglari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SirketTransferLoglari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SirketTransferLoglari_Sirketler_HedefSirketId",
                        column: x => x.HedefSirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SirketTransferLoglari_Sirketler_KaynakSirketId",
                        column: x => x.KaynakSirketId,
                        principalTable: "Sirketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SmsLoglari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SmsAyarId = table.Column<int>(type: "integer", nullable: true),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Mesaj = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    ProviderMesajId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HataMesaji = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GonderimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IletimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IliskiliTablo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IliskiliKayitId = table.Column<int>(type: "integer", nullable: true),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    GonderenKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsLoglari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsLoglari_Kullanicilar_GonderenKullaniciId",
                        column: x => x.GonderenKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SmsLoglari_SmsAyarlari_SmsAyarId",
                        column: x => x.SmsAyarId,
                        principalTable: "SmsAyarlari",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WebhookLoglar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WebhookEndpointId = table.Column<int>(type: "integer", nullable: false),
                    OlayTipi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    GonderimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    YanitTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SureMilisaniye = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    HataMesaji = table.Column<string>(type: "text", nullable: true),
                    IliskiliTablo = table.Column<string>(type: "text", nullable: true),
                    IliskiliKayitId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookLoglar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookLoglar_WebhookEndpointler_WebhookEndpointId",
                        column: x => x.WebhookEndpointId,
                        principalTable: "WebhookEndpointler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Personeller_MuhasebeHesapId",
                table: "Personeller",
                column: "MuhasebeHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_Personeller_SirketId",
                table: "Personeller",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_SirketId",
                table: "Kullanicilar",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_Guzergahlar_SirketId",
                table: "Guzergahlar",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_SirketId",
                table: "Faturalar",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_SirketId",
                table: "Cariler",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHatirlatmalar_CariId_Tip_CreatedAt",
                table: "CariHatirlatmalar",
                columns: new[] { "CariId", "Tip", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_SirketId",
                table: "BankaKasaHareketleri",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_BankaHesaplari_SirketId",
                table: "BankaHesaplari",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_Araclar_SirketId",
                table: "Araclar",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_AracBolgeAtamalar_AracBolgeId",
                table: "AracBolgeAtamalar",
                column: "AracBolgeId");

            migrationBuilder.CreateIndex(
                name: "IX_AracBolgeAtamalar_AracId",
                table: "AracBolgeAtamalar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_AracEvrakDosyaVersiyonlar_AracEvrakDosyaId",
                table: "AracEvrakDosyaVersiyonlar",
                column: "AracEvrakDosyaId");

            migrationBuilder.CreateIndex(
                name: "IX_AracEvrakDosyaVersiyonlar_OlusturanKullaniciId",
                table: "AracEvrakDosyaVersiyonlar",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_AracKonumlar_AracTakipCihazId",
                table: "AracKonumlar",
                column: "AracTakipCihazId");

            migrationBuilder.CreateIndex(
                name: "IX_AracTakipAlarmlar_AracTakipCihazId",
                table: "AracTakipAlarmlar",
                column: "AracTakipCihazId");

            migrationBuilder.CreateIndex(
                name: "IX_AracTakipCihazlar_AracId",
                table: "AracTakipCihazlar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_BildirimAyarlari_KullaniciId",
                table: "BildirimAyarlari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysAramaGecmisleri_KullaniciId",
                table: "EbysAramaGecmisleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvrakDosyaVersiyonlar_EvrakDosyaId",
                table: "EbysEvrakDosyaVersiyonlar",
                column: "EvrakDosyaId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvrakDosyaVersiyonlar_OlusturanKullaniciId",
                table: "EbysEvrakDosyaVersiyonlar",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysKayitliAramalar_KullaniciId",
                table: "EbysKayitliAramalar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_EpostaBildirimLoglari_KullaniciId",
                table: "EpostaBildirimLoglari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelOzlukEvrakVersiyonlar_OlusturanKullaniciId",
                table: "PersonelOzlukEvrakVersiyonlar",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelOzlukEvrakVersiyonlar_PersonelOzlukEvrakId",
                table: "PersonelOzlukEvrakVersiyonlar",
                column: "PersonelOzlukEvrakId");

            migrationBuilder.CreateIndex(
                name: "IX_Sirketler_SirketKodu",
                table: "Sirketler",
                column: "SirketKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SirketTransferLoglari_EntityTuru_EntityId",
                table: "SirketTransferLoglari",
                columns: new[] { "EntityTuru", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SirketTransferLoglari_HedefSirketId",
                table: "SirketTransferLoglari",
                column: "HedefSirketId");

            migrationBuilder.CreateIndex(
                name: "IX_SirketTransferLoglari_KaynakSirketId_HedefSirketId",
                table: "SirketTransferLoglari",
                columns: new[] { "KaynakSirketId", "HedefSirketId" });

            migrationBuilder.CreateIndex(
                name: "IX_SirketTransferLoglari_KullaniciId",
                table: "SirketTransferLoglari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SirketTransferLoglari_TransferTarihi",
                table: "SirketTransferLoglari",
                column: "TransferTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_SmsAyarlari_FirmaId",
                table: "SmsAyarlari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsLoglari_GonderenKullaniciId",
                table: "SmsLoglari",
                column: "GonderenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsLoglari_SmsAyarId",
                table: "SmsLoglari",
                column: "SmsAyarId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsSablonlari_FirmaId",
                table: "SmsSablonlari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLoglar_WebhookEndpointId",
                table: "WebhookLoglar",
                column: "WebhookEndpointId");

            migrationBuilder.AddForeignKey(
                name: "FK_Araclar_Sirketler_SirketId",
                table: "Araclar",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BankaHesaplari_Sirketler_SirketId",
                table: "BankaHesaplari",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BankaKasaHareketleri_Sirketler_SirketId",
                table: "BankaKasaHareketleri",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CariHatirlatmalar_Cariler_CariId",
                table: "CariHatirlatmalar",
                column: "CariId",
                principalTable: "Cariler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CariHatirlatmalar_Faturalar_FaturaId",
                table: "CariHatirlatmalar",
                column: "FaturaId",
                principalTable: "Faturalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CariHatirlatmalar_Firmalar_FirmaId",
                table: "CariHatirlatmalar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_Sirketler_SirketId",
                table: "Cariler",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Sirketler_SirketId",
                table: "Faturalar",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Sirketler_SirketId",
                table: "Guzergahlar",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Sirketler_SirketId",
                table: "Kullanicilar",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Personeller_MuhasebeHesaplari_MuhasebeHesapId",
                table: "Personeller",
                column: "MuhasebeHesapId",
                principalTable: "MuhasebeHesaplari",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Personeller_Sirketler_SirketId",
                table: "Personeller",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        private static void ApplyPostgreSqlSafeUp(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""CariHatirlatmalar"" DROP CONSTRAINT IF EXISTS ""FK_CariHatirlatmalar_Cariler_CariId"";
                ALTER TABLE IF EXISTS ""CariHatirlatmalar"" DROP CONSTRAINT IF EXISTS ""FK_CariHatirlatmalar_Faturalar_FaturaId"";
                ALTER TABLE IF EXISTS ""CariHatirlatmalar"" DROP CONSTRAINT IF EXISTS ""FK_CariHatirlatmalar_Firmalar_FirmaId"";
                DROP INDEX IF EXISTS ""IX_CariHatirlatmalar_CariId"";

                CREATE TABLE IF NOT EXISTS ""Sirketler"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""SirketKodu"" character varying(20) NOT NULL,
                    ""Unvan"" character varying(250) NOT NULL,
                    ""KisaAd"" character varying(100),
                    ""VergiDairesi"" character varying(100),
                    ""VergiNo"" character varying(11),
                    ""Adres"" character varying(500),
                    ""Il"" character varying(50),
                    ""Ilce"" character varying(50),
                    ""PostaKodu"" character varying(10),
                    ""Telefon"" character varying(20),
                    ""Email"" character varying(100),
                    ""WebSitesi"" character varying(200),
                    ""LogoUrl"" character varying(500),
                    ""Aktif"" boolean NOT NULL DEFAULT TRUE,
                    ""ParaBirimi"" character varying(5) NOT NULL DEFAULT 'TRY',
                    ""AyarlarJson"" text,
                    ""LisansBitisTarihi"" timestamp without time zone,
                    ""MaxKullaniciSayisi"" integer NOT NULL DEFAULT 0,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_Sirketler"" PRIMARY KEY (""Id"")
                );

                ALTER TABLE IF EXISTS ""Personeller"" ADD COLUMN IF NOT EXISTS ""MuhasebeHesapId"" integer;
                ALTER TABLE IF EXISTS ""Personeller"" ADD COLUMN IF NOT EXISTS ""SirketId"" integer;
                ALTER TABLE IF EXISTS ""Kullanicilar"" ADD COLUMN IF NOT EXISTS ""SirketId"" integer;
                ALTER TABLE IF EXISTS ""Guzergahlar"" ADD COLUMN IF NOT EXISTS ""SirketId"" integer;
                ALTER TABLE IF EXISTS ""Faturalar"" ADD COLUMN IF NOT EXISTS ""SirketId"" integer;
                ALTER TABLE IF EXISTS ""Cariler"" ADD COLUMN IF NOT EXISTS ""SirketId"" integer;
                ALTER TABLE IF EXISTS ""BankaKasaHareketleri"" ADD COLUMN IF NOT EXISTS ""SirketId"" integer;
                ALTER TABLE IF EXISTS ""BankaHesaplari"" ADD COLUMN IF NOT EXISTS ""SirketId"" integer;
                ALTER TABLE IF EXISTS ""Araclar"" ADD COLUMN IF NOT EXISTS ""SirketId"" integer;

                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Sirketler_SirketKodu"" ON ""Sirketler"" (""SirketKodu"");
                CREATE INDEX IF NOT EXISTS ""IX_Personeller_MuhasebeHesapId"" ON ""Personeller"" (""MuhasebeHesapId"");
                CREATE INDEX IF NOT EXISTS ""IX_Personeller_SirketId"" ON ""Personeller"" (""SirketId"");
                CREATE INDEX IF NOT EXISTS ""IX_Kullanicilar_SirketId"" ON ""Kullanicilar"" (""SirketId"");
                CREATE INDEX IF NOT EXISTS ""IX_Guzergahlar_SirketId"" ON ""Guzergahlar"" (""SirketId"");
                CREATE INDEX IF NOT EXISTS ""IX_Faturalar_SirketId"" ON ""Faturalar"" (""SirketId"");
                CREATE INDEX IF NOT EXISTS ""IX_Cariler_SirketId"" ON ""Cariler"" (""SirketId"");
                CREATE INDEX IF NOT EXISTS ""IX_BankaKasaHareketleri_SirketId"" ON ""BankaKasaHareketleri"" (""SirketId"");
                CREATE INDEX IF NOT EXISTS ""IX_BankaHesaplari_SirketId"" ON ""BankaHesaplari"" (""SirketId"");
                CREATE INDEX IF NOT EXISTS ""IX_Araclar_SirketId"" ON ""Araclar"" (""SirketId"");

                CREATE TABLE IF NOT EXISTS ""AracBolgeler"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""BolgeAdi"" text NOT NULL,
                    ""Tip"" integer NOT NULL,
                    ""MerkezLatitude"" double precision,
                    ""MerkezLongitude"" double precision,
                    ""YaricapMetre"" double precision,
                    ""PoligonKoordinatlari"" text,
                    ""Renk"" text,
                    ""GirisBildirimi"" boolean NOT NULL DEFAULT FALSE,
                    ""CikisBildirimi"" boolean NOT NULL DEFAULT FALSE,
                    ""Aktif"" boolean NOT NULL DEFAULT TRUE,
                    ""Notlar"" text,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_AracBolgeler"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""AracTakipCihazlar"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""AracId"" integer NOT NULL,
                    ""CihazId"" text NOT NULL,
                    ""CihazMarka"" text,
                    ""CihazModel"" text,
                    ""SimKartNo"" text,
                    ""Aktif"" boolean NOT NULL DEFAULT TRUE,
                    ""KurulumTarihi"" timestamp without time zone,
                    ""SonIletisimZamani"" timestamp without time zone,
                    ""BataryaSeviyesi"" integer,
                    ""SinyalGucu"" integer,
                    ""Notlar"" text,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_AracTakipCihazlar"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_AracTakipCihazlar_Araclar_AracId"" FOREIGN KEY (""AracId"") REFERENCES ""Araclar"" (""Id"") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ""AracBolgeAtamalar"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""AracBolgeId"" integer NOT NULL,
                    ""AracId"" integer NOT NULL,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_AracBolgeAtamalar"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_AracBolgeAtamalar_AracBolgeler_AracBolgeId"" FOREIGN KEY (""AracBolgeId"") REFERENCES ""AracBolgeler"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_AracBolgeAtamalar_Araclar_AracId"" FOREIGN KEY (""AracId"") REFERENCES ""Araclar"" (""Id"") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ""AracKonumlar"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""AracTakipCihazId"" integer NOT NULL,
                    ""Latitude"" double precision NOT NULL,
                    ""Longitude"" double precision NOT NULL,
                    ""Hiz"" double precision,
                    ""Yon"" double precision,
                    ""Rakım"" double precision,
                    ""Hassasiyet"" double precision,
                    ""KayitZamani"" timestamp without time zone NOT NULL,
                    ""KontakDurumu"" boolean,
                    ""MotorDurumu"" boolean,
                    ""YakitSeviyesi"" integer,
                    ""Kilometre"" integer,
                    ""Sicaklik"" double precision,
                    ""OlayTipi"" integer NOT NULL,
                    ""Adres"" text,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_AracKonumlar"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_AracKonumlar_AracTakipCihazlar_AracTakipCihazId"" FOREIGN KEY (""AracTakipCihazId"") REFERENCES ""AracTakipCihazlar"" (""Id"") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ""AracTakipAlarmlar"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""AracTakipCihazId"" integer NOT NULL,
                    ""AlarmTipi"" integer NOT NULL,
                    ""AlarmZamani"" timestamp without time zone NOT NULL,
                    ""Latitude"" double precision,
                    ""Longitude"" double precision,
                    ""Mesaj"" text,
                    ""Deger"" double precision,
                    ""Okundu"" boolean NOT NULL DEFAULT FALSE,
                    ""Islendi"" boolean NOT NULL DEFAULT FALSE,
                    ""Notlar"" text,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_AracTakipAlarmlar"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_AracTakipAlarmlar_AracTakipCihazlar_AracTakipCihazId"" FOREIGN KEY (""AracTakipCihazId"") REFERENCES ""AracTakipCihazlar"" (""Id"") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_AracTakipCihazlar_AracId"" ON ""AracTakipCihazlar"" (""AracId"");
                CREATE INDEX IF NOT EXISTS ""IX_AracBolgeAtamalar_AracBolgeId"" ON ""AracBolgeAtamalar"" (""AracBolgeId"");
                CREATE INDEX IF NOT EXISTS ""IX_AracBolgeAtamalar_AracId"" ON ""AracBolgeAtamalar"" (""AracId"");
                CREATE INDEX IF NOT EXISTS ""IX_AracKonumlar_AracTakipCihazId"" ON ""AracKonumlar"" (""AracTakipCihazId"");
                CREATE INDEX IF NOT EXISTS ""IX_AracTakipAlarmlar_AracTakipCihazId"" ON ""AracTakipAlarmlar"" (""AracTakipCihazId"");
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""PersonelPuantajlar"" ADD COLUMN IF NOT EXISTS ""OnayDurumu"" integer NOT NULL DEFAULT 0;
                ALTER TABLE IF EXISTS ""PersonelPuantajlar"" ADD COLUMN IF NOT EXISTS ""OnayNotu"" text;
                ALTER TABLE IF EXISTS ""PersonelPuantajlar"" ADD COLUMN IF NOT EXISTS ""OnayTarihi"" timestamp without time zone;
                ALTER TABLE IF EXISTS ""PersonelPuantajlar"" ADD COLUMN IF NOT EXISTS ""OnaylayanKullanici"" text;

                ALTER TABLE IF EXISTS ""PersonelOzlukEvraklar"" ADD COLUMN IF NOT EXISTS ""DosyaAdi"" text;
                ALTER TABLE IF EXISTS ""PersonelOzlukEvraklar"" ADD COLUMN IF NOT EXISTS ""DosyaBoyutu"" bigint;
                ALTER TABLE IF EXISTS ""PersonelOzlukEvraklar"" ADD COLUMN IF NOT EXISTS ""DosyaTipi"" text;
                ALTER TABLE IF EXISTS ""PersonelOzlukEvraklar"" ADD COLUMN IF NOT EXISTS ""SonDegisiklikNotu"" text;
                ALTER TABLE IF EXISTS ""PersonelOzlukEvraklar"" ADD COLUMN IF NOT EXISTS ""VersiyonNo"" integer NOT NULL DEFAULT 0;

                ALTER TABLE IF EXISTS ""Guzergahlar"" ADD COLUMN IF NOT EXISTS ""BaslangicLatitude"" double precision;
                ALTER TABLE IF EXISTS ""Guzergahlar"" ADD COLUMN IF NOT EXISTS ""BaslangicLongitude"" double precision;
                ALTER TABLE IF EXISTS ""Guzergahlar"" ADD COLUMN IF NOT EXISTS ""BitisLatitude"" double precision;
                ALTER TABLE IF EXISTS ""Guzergahlar"" ADD COLUMN IF NOT EXISTS ""BitisLongitude"" double precision;
                ALTER TABLE IF EXISTS ""Guzergahlar"" ADD COLUMN IF NOT EXISTS ""RotaRengi"" text;

                ALTER TABLE IF EXISTS ""Faturalar"" ADD COLUMN IF NOT EXISTS ""GibDurumGuncellemeTarihi"" timestamp without time zone;
                ALTER TABLE IF EXISTS ""Faturalar"" ADD COLUMN IF NOT EXISTS ""GibDurumMesaji"" text;
                ALTER TABLE IF EXISTS ""Faturalar"" ADD COLUMN IF NOT EXISTS ""GibDurumu"" integer NOT NULL DEFAULT 0;
                ALTER TABLE IF EXISTS ""Faturalar"" ADD COLUMN IF NOT EXISTS ""GibGonderimTarihi"" timestamp without time zone;

                ALTER TABLE IF EXISTS ""EbysEvrakDosyalar"" ADD COLUMN IF NOT EXISTS ""SonDegisiklikNotu"" text;
                ALTER TABLE IF EXISTS ""EbysEvrakDosyalar"" ADD COLUMN IF NOT EXISTS ""VersiyonNo"" integer NOT NULL DEFAULT 0;

                ALTER TABLE IF EXISTS ""AracEvrakDosyalari"" ADD COLUMN IF NOT EXISTS ""SonDegisiklikNotu"" text;
                ALTER TABLE IF EXISTS ""AracEvrakDosyalari"" ADD COLUMN IF NOT EXISTS ""VersiyonNo"" integer NOT NULL DEFAULT 0;

                CREATE TABLE IF NOT EXISTS ""EbysEvrakKategoriler"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""KategoriAdi"" character varying(100) NOT NULL,
                    ""Aciklama"" character varying(500),
                    ""SiraNo"" integer NOT NULL DEFAULT 0,
                    ""Aktif"" boolean NOT NULL DEFAULT TRUE,
                    ""Renk"" character varying(20),
                    ""Ikon"" character varying(50),
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_EbysEvrakKategoriler"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""BildirimAyarlari"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""KullaniciId"" integer NOT NULL,
                    ""FaturaVadeUyarisi"" boolean NOT NULL DEFAULT FALSE,
                    ""EhliyetBitisUyarisi"" boolean NOT NULL DEFAULT FALSE,
                    ""SrcBelgesiUyarisi"" boolean NOT NULL DEFAULT FALSE,
                    ""PsikoteknikUyarisi"" boolean NOT NULL DEFAULT FALSE,
                    ""SaglikRaporuUyarisi"" boolean NOT NULL DEFAULT FALSE,
                    ""TrafikSigortaUyarisi"" boolean NOT NULL DEFAULT FALSE,
                    ""KaskoUyarisi"" boolean NOT NULL DEFAULT FALSE,
                    ""MuayeneUyarisi"" boolean NOT NULL DEFAULT FALSE,
                    ""DestekTalebiUyarisi"" boolean NOT NULL DEFAULT FALSE,
                    ""SistemBildirimleri"" boolean NOT NULL DEFAULT FALSE,
                    ""EpostaAlsin"" boolean NOT NULL DEFAULT FALSE,
                    ""EpostaAdresi"" text,
                    ""SmsAlsin"" boolean NOT NULL DEFAULT FALSE,
                    ""SmsTelefon"" character varying(20),
                    ""SmsVadeHatirlatma"" boolean NOT NULL DEFAULT FALSE,
                    ""SmsBelgeHatirlatma"" boolean NOT NULL DEFAULT FALSE,
                    ""VadeUyariGunSayisi"" integer NOT NULL DEFAULT 0,
                    ""BelgeUyariGunSayisi"" integer NOT NULL DEFAULT 0,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_BildirimAyarlari"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""EbysAramaGecmisleri"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""KullaniciId"" integer NOT NULL,
                    ""AramaMetni"" character varying(500) NOT NULL,
                    ""FiltreJson"" character varying(2000),
                    ""SonucSayisi"" integer NOT NULL DEFAULT 0,
                    ""AramaTarihi"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_EbysAramaGecmisleri"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""EbysBelgeEmbeddingler"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""Kaynak"" integer NOT NULL,
                    ""KaynakId"" integer NOT NULL,
                    ""DosyaId"" integer,
                    ""Metin"" character varying(8000) NOT NULL,
                    ""MetinOzet"" character varying(500),
                    ""EmbeddingJson"" text NOT NULL,
                    ""EmbeddingBoyutu"" integer NOT NULL DEFAULT 0,
                    ""ModelAdi"" character varying(100),
                    ""OlusturmaTarihi"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""GuncellemeTarihi"" timestamp without time zone,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_EbysBelgeEmbeddingler"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""EbysKayitliAramalar"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""KullaniciId"" integer NOT NULL,
                    ""AramaAdi"" character varying(100) NOT NULL,
                    ""Aciklama"" character varying(250),
                    ""FiltreJson"" character varying(2000) NOT NULL,
                    ""BildirimAktif"" boolean NOT NULL DEFAULT FALSE,
                    ""SiraNo"" integer NOT NULL DEFAULT 0,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_EbysKayitliAramalar"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""EpostaBildirimLoglari"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""KullaniciId"" integer NOT NULL,
                    ""EpostaAdresi"" character varying(200) NOT NULL,
                    ""UyariSayisi"" integer NOT NULL DEFAULT 0,
                    ""GonderimTarihi"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""Basarili"" boolean NOT NULL DEFAULT FALSE,
                    ""HataMesaji"" character varying(500),
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_EpostaBildirimLoglari"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""EbysEvrakDosyaVersiyonlar"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""EvrakDosyaId"" integer NOT NULL,
                    ""VersiyonNo"" integer NOT NULL DEFAULT 0,
                    ""DosyaAdi"" text NOT NULL,
                    ""DosyaYolu"" text NOT NULL,
                    ""DosyaTipi"" text,
                    ""DosyaBoyutu"" bigint NOT NULL DEFAULT 0,
                    ""Aciklama"" text,
                    ""DegisiklikNotu"" text,
                    ""OlusturanKullaniciId"" integer,
                    ""OlusturmaTarihi"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_EbysEvrakDosyaVersiyonlar"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""PersonelOzlukEvrakVersiyonlar"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""PersonelOzlukEvrakId"" integer NOT NULL,
                    ""VersiyonNo"" integer NOT NULL DEFAULT 0,
                    ""DosyaYolu"" text,
                    ""DosyaAdi"" text,
                    ""DosyaTipi"" text,
                    ""DosyaBoyutu"" bigint,
                    ""Aciklama"" text,
                    ""DegisiklikNotu"" text,
                    ""OlusturanKullaniciId"" integer,
                    ""OlusturmaTarihi"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_PersonelOzlukEvrakVersiyonlar"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""SmsAyarlari"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""FirmaId"" integer,
                    ""Provider"" integer NOT NULL DEFAULT 0,
                    ""KullaniciAdi"" character varying(100),
                    ""ApiKey"" character varying(200),
                    ""GondericiNumara"" character varying(50),
                    ""ApiUrl"" character varying(200),
                    ""Aktif"" boolean NOT NULL DEFAULT FALSE,
                    ""Bakiye"" numeric,
                    ""SonBakiyeSorguTarihi"" timestamp without time zone,
                    ""ToplamGonderilenSms"" integer NOT NULL DEFAULT 0,
                    ""ToplamBasarisizSms"" integer NOT NULL DEFAULT 0,
                    ""SonGonderimTarihi"" timestamp without time zone,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_SmsAyarlari"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""SmsSablonlari"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""FirmaId"" integer,
                    ""Adi"" character varying(100) NOT NULL,
                    ""Aciklama"" character varying(200),
                    ""Sablon"" character varying(500) NOT NULL,
                    ""Tip"" integer NOT NULL DEFAULT 0,
                    ""Aktif"" boolean NOT NULL DEFAULT FALSE,
                    ""Varsayilan"" boolean NOT NULL DEFAULT FALSE,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_SmsSablonlari"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""SmsLoglari"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""SmsAyarId"" integer,
                    ""Telefon"" character varying(20) NOT NULL,
                    ""Mesaj"" character varying(500) NOT NULL,
                    ""Durum"" integer NOT NULL DEFAULT 0,
                    ""ProviderMesajId"" character varying(100),
                    ""HataMesaji"" character varying(500),
                    ""GonderimTarihi"" timestamp without time zone,
                    ""IletimTarihi"" timestamp without time zone,
                    ""IliskiliTablo"" character varying(50),
                    ""IliskiliKayitId"" integer,
                    ""Tip"" integer NOT NULL DEFAULT 0,
                    ""GonderenKullaniciId"" integer,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_SmsLoglari"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""WebhookEndpointler"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""Ad"" character varying(100) NOT NULL,
                    ""Aciklama"" character varying(500),
                    ""Url"" character varying(500) NOT NULL,
                    ""Secret"" character varying(100),
                    ""Aktif"" boolean NOT NULL DEFAULT FALSE,
                    ""MaxRetry"" integer NOT NULL DEFAULT 0,
                    ""RetryDelaySaniye"" integer NOT NULL DEFAULT 0,
                    ""OlayFiltresi"" text,
                    ""HttpMethod"" text NOT NULL DEFAULT 'POST',
                    ""Headers"" text,
                    ""ToplamGonderim"" integer NOT NULL DEFAULT 0,
                    ""BasariliGonderim"" integer NOT NULL DEFAULT 0,
                    ""BasarisizGonderim"" integer NOT NULL DEFAULT 0,
                    ""SonGonderimTarihi"" timestamp without time zone,
                    ""SonBasariliTarih"" timestamp without time zone,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_WebhookEndpointler"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""WebhookLoglar"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""WebhookEndpointId"" integer NOT NULL,
                    ""OlayTipi"" character varying(100) NOT NULL,
                    ""Payload"" text,
                    ""Durum"" integer NOT NULL DEFAULT 0,
                    ""HttpStatusCode"" integer NOT NULL DEFAULT 0,
                    ""ResponseBody"" text,
                    ""GonderimTarihi"" timestamp without time zone,
                    ""YanitTarihi"" timestamp without time zone,
                    ""SureMilisaniye"" integer NOT NULL DEFAULT 0,
                    ""RetryCount"" integer NOT NULL DEFAULT 0,
                    ""HataMesaji"" text,
                    ""IliskiliTablo"" text,
                    ""IliskiliKayitId"" integer,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_WebhookLoglar"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""SirketTransferLoglari"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""EntityTuru"" character varying(50) NOT NULL,
                    ""EntityId"" integer NOT NULL,
                    ""EntityAciklama"" character varying(500),
                    ""KaynakSirketId"" integer NOT NULL,
                    ""HedefSirketId"" integer NOT NULL,
                    ""KullaniciId"" integer NOT NULL,
                    ""TransferTarihi"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""Durum"" integer NOT NULL DEFAULT 0,
                    ""HataMesaji"" character varying(2000),
                    ""IliskiliVerilerTransferEdildi"" boolean NOT NULL DEFAULT FALSE,
                    ""IliskiliEntitySayisi"" integer NOT NULL DEFAULT 0,
                    ""Notlar"" character varying(1000),
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_SirketTransferLoglari"" PRIMARY KEY (""Id"")
                );

                CREATE INDEX IF NOT EXISTS ""IX_BildirimAyarlari_KullaniciId"" ON ""BildirimAyarlari"" (""KullaniciId"");
                CREATE INDEX IF NOT EXISTS ""IX_EbysAramaGecmisleri_KullaniciId"" ON ""EbysAramaGecmisleri"" (""KullaniciId"");
                CREATE INDEX IF NOT EXISTS ""IX_EbysKayitliAramalar_KullaniciId"" ON ""EbysKayitliAramalar"" (""KullaniciId"");
                CREATE INDEX IF NOT EXISTS ""IX_EpostaBildirimLoglari_KullaniciId"" ON ""EpostaBildirimLoglari"" (""KullaniciId"");
                CREATE INDEX IF NOT EXISTS ""IX_EbysEvrakDosyaVersiyonlar_EvrakDosyaId"" ON ""EbysEvrakDosyaVersiyonlar"" (""EvrakDosyaId"");
                CREATE INDEX IF NOT EXISTS ""IX_EbysEvrakDosyaVersiyonlar_OlusturanKullaniciId"" ON ""EbysEvrakDosyaVersiyonlar"" (""OlusturanKullaniciId"");
                CREATE INDEX IF NOT EXISTS ""IX_PersonelOzlukEvrakVersiyonlar_OlusturanKullaniciId"" ON ""PersonelOzlukEvrakVersiyonlar"" (""OlusturanKullaniciId"");
                CREATE INDEX IF NOT EXISTS ""IX_PersonelOzlukEvrakVersiyonlar_PersonelOzlukEvrakId"" ON ""PersonelOzlukEvrakVersiyonlar"" (""PersonelOzlukEvrakId"");
                CREATE INDEX IF NOT EXISTS ""IX_SmsAyarlari_FirmaId"" ON ""SmsAyarlari"" (""FirmaId"");
                CREATE INDEX IF NOT EXISTS ""IX_SmsSablonlari_FirmaId"" ON ""SmsSablonlari"" (""FirmaId"");
                CREATE INDEX IF NOT EXISTS ""IX_SmsLoglari_GonderenKullaniciId"" ON ""SmsLoglari"" (""GonderenKullaniciId"");
                CREATE INDEX IF NOT EXISTS ""IX_SmsLoglari_SmsAyarId"" ON ""SmsLoglari"" (""SmsAyarId"");
                CREATE INDEX IF NOT EXISTS ""IX_WebhookLoglar_WebhookEndpointId"" ON ""WebhookLoglar"" (""WebhookEndpointId"");
                CREATE INDEX IF NOT EXISTS ""IX_SirketTransferLoglari_EntityTuru_EntityId"" ON ""SirketTransferLoglari"" (""EntityTuru"", ""EntityId"");
                CREATE INDEX IF NOT EXISTS ""IX_SirketTransferLoglari_HedefSirketId"" ON ""SirketTransferLoglari"" (""HedefSirketId"");
                CREATE INDEX IF NOT EXISTS ""IX_SirketTransferLoglari_KaynakSirketId_HedefSirketId"" ON ""SirketTransferLoglari"" (""KaynakSirketId"", ""HedefSirketId"");
                CREATE INDEX IF NOT EXISTS ""IX_SirketTransferLoglari_KullaniciId"" ON ""SirketTransferLoglari"" (""KullaniciId"");
                CREATE INDEX IF NOT EXISTS ""IX_SirketTransferLoglari_TransferTarihi"" ON ""SirketTransferLoglari"" (""TransferTarihi"");
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Personeller')
                       AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MuhasebeHesaplari')
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Personeller_MuhasebeHesaplari_MuhasebeHesapId') THEN
                        ALTER TABLE ""Personeller"" ADD CONSTRAINT ""FK_Personeller_MuhasebeHesaplari_MuhasebeHesapId""
                            FOREIGN KEY (""MuhasebeHesapId"") REFERENCES ""MuhasebeHesaplari"" (""Id"") ON DELETE SET NULL;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Personeller')
                       AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Sirketler')
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Personeller_Sirketler_SirketId') THEN
                        ALTER TABLE ""Personeller"" ADD CONSTRAINT ""FK_Personeller_Sirketler_SirketId""
                            FOREIGN KEY (""SirketId"") REFERENCES ""Sirketler"" (""Id"") ON DELETE RESTRICT;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Kullanicilar')
                       AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Sirketler')
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Kullanicilar_Sirketler_SirketId') THEN
                        ALTER TABLE ""Kullanicilar"" ADD CONSTRAINT ""FK_Kullanicilar_Sirketler_SirketId""
                            FOREIGN KEY (""SirketId"") REFERENCES ""Sirketler"" (""Id"") ON DELETE SET NULL;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Guzergahlar')
                       AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Sirketler')
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Guzergahlar_Sirketler_SirketId') THEN
                        ALTER TABLE ""Guzergahlar"" ADD CONSTRAINT ""FK_Guzergahlar_Sirketler_SirketId""
                            FOREIGN KEY (""SirketId"") REFERENCES ""Sirketler"" (""Id"") ON DELETE RESTRICT;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Faturalar')
                       AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Sirketler')
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Faturalar_Sirketler_SirketId') THEN
                        ALTER TABLE ""Faturalar"" ADD CONSTRAINT ""FK_Faturalar_Sirketler_SirketId""
                            FOREIGN KEY (""SirketId"") REFERENCES ""Sirketler"" (""Id"") ON DELETE RESTRICT;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Cariler')
                       AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Sirketler')
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Cariler_Sirketler_SirketId') THEN
                        ALTER TABLE ""Cariler"" ADD CONSTRAINT ""FK_Cariler_Sirketler_SirketId""
                            FOREIGN KEY (""SirketId"") REFERENCES ""Sirketler"" (""Id"") ON DELETE RESTRICT;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'BankaKasaHareketleri')
                       AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Sirketler')
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_BankaKasaHareketleri_Sirketler_SirketId') THEN
                        ALTER TABLE ""BankaKasaHareketleri"" ADD CONSTRAINT ""FK_BankaKasaHareketleri_Sirketler_SirketId""
                            FOREIGN KEY (""SirketId"") REFERENCES ""Sirketler"" (""Id"") ON DELETE RESTRICT;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'BankaHesaplari')
                       AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Sirketler')
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_BankaHesaplari_Sirketler_SirketId') THEN
                        ALTER TABLE ""BankaHesaplari"" ADD CONSTRAINT ""FK_BankaHesaplari_Sirketler_SirketId""
                            FOREIGN KEY (""SirketId"") REFERENCES ""Sirketler"" (""Id"") ON DELETE RESTRICT;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Araclar')
                       AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Sirketler')
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Araclar_Sirketler_SirketId') THEN
                        ALTER TABLE ""Araclar"" ADD CONSTRAINT ""FK_Araclar_Sirketler_SirketId""
                            FOREIGN KEY (""SirketId"") REFERENCES ""Sirketler"" (""Id"") ON DELETE RESTRICT;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Araclar_Sirketler_SirketId",
                table: "Araclar");

            migrationBuilder.DropForeignKey(
                name: "FK_BankaHesaplari_Sirketler_SirketId",
                table: "BankaHesaplari");

            migrationBuilder.DropForeignKey(
                name: "FK_BankaKasaHareketleri_Sirketler_SirketId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropForeignKey(
                name: "FK_CariHatirlatmalar_Cariler_CariId",
                table: "CariHatirlatmalar");

            migrationBuilder.DropForeignKey(
                name: "FK_CariHatirlatmalar_Faturalar_FaturaId",
                table: "CariHatirlatmalar");

            migrationBuilder.DropForeignKey(
                name: "FK_CariHatirlatmalar_Firmalar_FirmaId",
                table: "CariHatirlatmalar");

            migrationBuilder.DropForeignKey(
                name: "FK_Cariler_Sirketler_SirketId",
                table: "Cariler");

            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_Sirketler_SirketId",
                table: "Faturalar");

            migrationBuilder.DropForeignKey(
                name: "FK_Guzergahlar_Sirketler_SirketId",
                table: "Guzergahlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Sirketler_SirketId",
                table: "Kullanicilar");

            migrationBuilder.DropForeignKey(
                name: "FK_Personeller_MuhasebeHesaplari_MuhasebeHesapId",
                table: "Personeller");

            migrationBuilder.DropForeignKey(
                name: "FK_Personeller_Sirketler_SirketId",
                table: "Personeller");

            migrationBuilder.DropTable(
                name: "AracBolgeAtamalar");

            migrationBuilder.DropTable(
                name: "AracEvrakDosyaVersiyonlar");

            migrationBuilder.DropTable(
                name: "AracKonumlar");

            migrationBuilder.DropTable(
                name: "AracTakipAlarmlar");

            migrationBuilder.DropTable(
                name: "BildirimAyarlari");

            migrationBuilder.DropTable(
                name: "EbysAramaGecmisleri");

            migrationBuilder.DropTable(
                name: "EbysBelgeEmbeddingler");

            migrationBuilder.DropTable(
                name: "EbysEvrakDosyaVersiyonlar");

            migrationBuilder.DropTable(
                name: "EbysKayitliAramalar");

            migrationBuilder.DropTable(
                name: "EpostaBildirimLoglari");

            migrationBuilder.DropTable(
                name: "PersonelOzlukEvrakVersiyonlar");

            migrationBuilder.DropTable(
                name: "SirketTransferLoglari");

            migrationBuilder.DropTable(
                name: "SmsLoglari");

            migrationBuilder.DropTable(
                name: "SmsSablonlari");

            migrationBuilder.DropTable(
                name: "WebhookLoglar");

            migrationBuilder.DropTable(
                name: "AracBolgeler");

            migrationBuilder.DropTable(
                name: "AracTakipCihazlar");

            migrationBuilder.DropTable(
                name: "Sirketler");

            migrationBuilder.DropTable(
                name: "SmsAyarlari");

            migrationBuilder.DropTable(
                name: "WebhookEndpointler");

            migrationBuilder.DropIndex(
                name: "IX_Personeller_MuhasebeHesapId",
                table: "Personeller");

            migrationBuilder.DropIndex(
                name: "IX_Personeller_SirketId",
                table: "Personeller");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_SirketId",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_Guzergahlar_SirketId",
                table: "Guzergahlar");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_SirketId",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_Cariler_SirketId",
                table: "Cariler");

            migrationBuilder.DropIndex(
                name: "IX_CariHatirlatmalar_CariId_Tip_CreatedAt",
                table: "CariHatirlatmalar");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_SirketId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaHesaplari_SirketId",
                table: "BankaHesaplari");

            migrationBuilder.DropIndex(
                name: "IX_Araclar_SirketId",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "OnayDurumu",
                table: "PersonelPuantajlar");

            migrationBuilder.DropColumn(
                name: "OnayNotu",
                table: "PersonelPuantajlar");

            migrationBuilder.DropColumn(
                name: "OnayTarihi",
                table: "PersonelPuantajlar");

            migrationBuilder.DropColumn(
                name: "OnaylayanKullanici",
                table: "PersonelPuantajlar");

            migrationBuilder.DropColumn(
                name: "DosyaAdi",
                table: "PersonelOzlukEvraklar");

            migrationBuilder.DropColumn(
                name: "DosyaBoyutu",
                table: "PersonelOzlukEvraklar");

            migrationBuilder.DropColumn(
                name: "DosyaTipi",
                table: "PersonelOzlukEvraklar");

            migrationBuilder.DropColumn(
                name: "SonDegisiklikNotu",
                table: "PersonelOzlukEvraklar");

            migrationBuilder.DropColumn(
                name: "VersiyonNo",
                table: "PersonelOzlukEvraklar");

            migrationBuilder.DropColumn(
                name: "MuhasebeHesapId",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "SirketId",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "SirketId",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "BaslangicLatitude",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "BaslangicLongitude",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "BitisLatitude",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "BitisLongitude",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "RotaRengi",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "SirketId",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "GibDurumGuncellemeTarihi",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "GibDurumMesaji",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "GibDurumu",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "GibGonderimTarihi",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "SirketId",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "SonDegisiklikNotu",
                table: "EbysEvrakDosyalar");

            migrationBuilder.DropColumn(
                name: "VersiyonNo",
                table: "EbysEvrakDosyalar");

            migrationBuilder.DropColumn(
                name: "SirketId",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "SirketId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "SirketId",
                table: "BankaHesaplari");

            migrationBuilder.DropColumn(
                name: "SirketId",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "SonDegisiklikNotu",
                table: "AracEvrakDosyalari");

            migrationBuilder.DropColumn(
                name: "VersiyonNo",
                table: "AracEvrakDosyalari");

            migrationBuilder.AlterColumn<decimal>(
                name: "Tutar",
                table: "CariHatirlatmalar",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Baslik",
                table: "CariHatirlatmalar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Aciklama",
                table: "CariHatirlatmalar",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CariHatirlatmalar_CariId",
                table: "CariHatirlatmalar",
                column: "CariId");

            migrationBuilder.AddForeignKey(
                name: "FK_CariHatirlatmalar_Cariler_CariId",
                table: "CariHatirlatmalar",
                column: "CariId",
                principalTable: "Cariler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CariHatirlatmalar_Faturalar_FaturaId",
                table: "CariHatirlatmalar",
                column: "FaturaId",
                principalTable: "Faturalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CariHatirlatmalar_Firmalar_FirmaId",
                table: "CariHatirlatmalar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");
        }
    }
}



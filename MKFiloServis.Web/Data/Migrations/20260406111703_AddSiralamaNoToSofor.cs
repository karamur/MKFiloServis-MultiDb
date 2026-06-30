using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSiralamaNoToSofor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AylikChecklistler_Soforler_SoforId",
                table: "AylikChecklistler");

            migrationBuilder.DropForeignKey(
                name: "FK_Cariler_Soforler_SoforId",
                table: "Cariler");

            migrationBuilder.DropForeignKey(
                name: "FK_FiloGunlukPuantajlar_Soforler_SoforId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Soforler_SoforId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropForeignKey(
                name: "FK_Guzergahlar_Soforler_VarsayilanSoforId",
                table: "Guzergahlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Soforler_SoforId",
                table: "Kullanicilar");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelIzinHaklari_Soforler_SoforId",
                table: "PersonelIzinHaklari");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelIzinleri_Soforler_SoforId",
                table: "PersonelIzinleri");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelMaaslari_Soforler_SoforId",
                table: "PersonelMaaslari");

            migrationBuilder.Sql(@"ALTER TABLE ""PersonelOzlukEvraklar"" DROP CONSTRAINT IF EXISTS ""FK_PersonelOzlukEvraklar_Soforler_SoforId"";");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelPuantajlar_Soforler_PersonelId",
                table: "PersonelPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_ServisCalismaKiralamalar_Soforler_SoforId",
                table: "ServisCalismaKiralamalar");

            migrationBuilder.DropForeignKey(
                name: "FK_ServisCalismalari_Soforler_SoforId",
                table: "ServisCalismalari");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Soforler",
                table: "Soforler");

            migrationBuilder.RenameTable(
                name: "Soforler",
                newName: "Personeller");

            migrationBuilder.RenameIndex(
                name: "IX_Soforler_SoforKodu",
                table: "Personeller",
                newName: "IX_Personeller_SoforKodu");

            migrationBuilder.AddColumn<int>(
                name: "BordroId",
                table: "MuhasebeFisleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CariId",
                table: "AracMasraflari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MuhasebeFisId",
                table: "AracMasraflari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoforId",
                table: "AracMasraflari",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "NetMaas",
                table: "Personeller",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "BrutMaas",
                table: "Personeller",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<bool>(
                name: "ArgePersoneli",
                table: "Personeller",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "BirimUcret",
                table: "Personeller",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BordroTipiPersonel",
                table: "Personeller",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BrutMaasHesaplamaTipi",
                table: "Personeller",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CalismaMiktari",
                table: "Personeller",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DigerMaas",
                table: "Personeller",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResmiNetMaas",
                table: "Personeller",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "SGKBordroDahilMi",
                table: "Personeller",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SgkMaasi",
                table: "Personeller",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SiralamaNo",
                table: "Personeller",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TopluMaas",
                table: "Personeller",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Personeller",
                table: "Personeller",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "BordroAyarlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    PersonelMaasHesapKodu = table.Column<string>(type: "text", nullable: false),
                    SgkPrimHesapKodu = table.Column<string>(type: "text", nullable: false),
                    GelirVergisiHesapKodu = table.Column<string>(type: "text", nullable: false),
                    KasaHesapKodu = table.Column<string>(type: "text", nullable: false),
                    BankaHesapKodu = table.Column<string>(type: "text", nullable: false),
                    PersonelAvansHesapKodu = table.Column<string>(type: "text", nullable: false),
                    SgkIsciPayiOrani = table.Column<decimal>(type: "numeric", nullable: false),
                    IssizlikIsciPayiOrani = table.Column<decimal>(type: "numeric", nullable: false),
                    DamgaVergisiOrani = table.Column<decimal>(type: "numeric", nullable: false),
                    ArgeSgkIsverenDestekVarMi = table.Column<bool>(type: "boolean", nullable: false),
                    ArgeSgkIsverenDestekOrani = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BordroAyarlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BordroAyarlar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Bordrolar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    BordroTipi = table.Column<int>(type: "integer", nullable: false),
                    HesaplamaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OnayTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Onaylandi = table.Column<bool>(type: "boolean", nullable: false),
                    OnaylayanKullanici = table.Column<string>(type: "text", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    ToplamPersonelSayisi = table.Column<int>(type: "integer", nullable: false),
                    ToplamBrutMaas = table.Column<decimal>(type: "numeric", nullable: false),
                    ToplamNetMaas = table.Column<decimal>(type: "numeric", nullable: false),
                    ToplamSgkMatrahi = table.Column<decimal>(type: "numeric", nullable: false),
                    ToplamEkOdeme = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bordrolar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bordrolar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PersonelAvanslar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonelId = table.Column<int>(type: "integer", nullable: false),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    AvansTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    OdemeSekli = table.Column<int>(type: "integer", nullable: false),
                    BankaHesapId = table.Column<int>(type: "integer", nullable: true),
                    MuhasebeFisId = table.Column<int>(type: "integer", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    MahsupEdilen = table.Column<decimal>(type: "numeric", nullable: false),
                    MahsupTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MahsupAciklamasi = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonelAvanslar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonelAvanslar_BankaHesaplari_BankaHesapId",
                        column: x => x.BankaHesapId,
                        principalTable: "BankaHesaplari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelAvanslar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelAvanslar_MuhasebeFisleri_MuhasebeFisId",
                        column: x => x.MuhasebeFisId,
                        principalTable: "MuhasebeFisleri",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelAvanslar_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonelBorclar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonelId = table.Column<int>(type: "integer", nullable: false),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    BorcTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric", nullable: false),
                    BorcNedeni = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    BorcTipi = table.Column<int>(type: "integer", nullable: false),
                    OdemeDurum = table.Column<int>(type: "integer", nullable: false),
                    PlanlananOdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GerceklesenOdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OdenenTutar = table.Column<decimal>(type: "numeric", nullable: false),
                    OdemeSekli = table.Column<int>(type: "integer", nullable: true),
                    BankaHesapId = table.Column<int>(type: "integer", nullable: true),
                    MuhasebeFisId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonelBorclar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonelBorclar_BankaHesaplari_BankaHesapId",
                        column: x => x.BankaHesapId,
                        principalTable: "BankaHesaplari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelBorclar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelBorclar_MuhasebeFisleri_MuhasebeFisId",
                        column: x => x.MuhasebeFisId,
                        principalTable: "MuhasebeFisleri",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelBorclar_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonelFinansAyarlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    PersonelAvanslariHesapId = table.Column<int>(type: "integer", nullable: true),
                    PersoneleBorclarHesapId = table.Column<int>(type: "integer", nullable: true),
                    KasaHesapId = table.Column<int>(type: "integer", nullable: true),
                    BankaHesapId = table.Column<int>(type: "integer", nullable: true),
                    OtomatikFisOlustur = table.Column<bool>(type: "boolean", nullable: false),
                    AvansVerildigindeFisOlustur = table.Column<bool>(type: "boolean", nullable: false),
                    AvansMahsupFisOlustur = table.Column<bool>(type: "boolean", nullable: false),
                    BorcOdendigindeFisOlustur = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonelFinansAyarlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonelFinansAyarlar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_BankaHesapId",
                        column: x => x.BankaHesapId,
                        principalTable: "MuhasebeHesaplari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_KasaHesapId",
                        column: x => x.KasaHesapId,
                        principalTable: "MuhasebeHesaplari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_PersonelAvanslariHe~",
                        column: x => x.PersonelAvanslariHesapId,
                        principalTable: "MuhasebeHesaplari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelFinansAyarlar_MuhasebeHesaplari_PersoneleBorclarHes~",
                        column: x => x.PersoneleBorclarHesapId,
                        principalTable: "MuhasebeHesaplari",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BordroDetaylar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BordroId = table.Column<int>(type: "integer", nullable: false),
                    PersonelId = table.Column<int>(type: "integer", nullable: false),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    BrutMaas = table.Column<decimal>(type: "numeric", nullable: false),
                    NetMaas = table.Column<decimal>(type: "numeric", nullable: false),
                    TopluMaas = table.Column<decimal>(type: "numeric", nullable: false),
                    SgkMaasi = table.Column<decimal>(type: "numeric", nullable: false),
                    EkOdeme = table.Column<decimal>(type: "numeric", nullable: false),
                    SgkIssizlikKesinti = table.Column<decimal>(type: "numeric", nullable: false),
                    GelirVergisi = table.Column<decimal>(type: "numeric", nullable: false),
                    DamgaVergisi = table.Column<decimal>(type: "numeric", nullable: false),
                    YemekYardimi = table.Column<decimal>(type: "numeric", nullable: false),
                    YolYardimi = table.Column<decimal>(type: "numeric", nullable: false),
                    PrimTutar = table.Column<decimal>(type: "numeric", nullable: false),
                    DigerEkOdeme = table.Column<decimal>(type: "numeric", nullable: false),
                    BankaOdemesiYapildi = table.Column<bool>(type: "boolean", nullable: false),
                    BankaOdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EkOdemeYapildi = table.Column<bool>(type: "boolean", nullable: false),
                    EkOdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BordroDetaylar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BordroDetaylar_Bordrolar_BordroId",
                        column: x => x.BordroId,
                        principalTable: "Bordrolar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BordroDetaylar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BordroDetaylar_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonelAvansMahsuplar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AvansId = table.Column<int>(type: "integer", nullable: false),
                    MahsupTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MahsupTutari = table.Column<decimal>(type: "numeric", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    MahsupSekli = table.Column<int>(type: "integer", nullable: false),
                    MaasId = table.Column<int>(type: "integer", nullable: true),
                    BankaHesapId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonelAvansMahsuplar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonelAvansMahsuplar_BankaHesaplari_BankaHesapId",
                        column: x => x.BankaHesapId,
                        principalTable: "BankaHesaplari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelAvansMahsuplar_PersonelAvanslar_AvansId",
                        column: x => x.AvansId,
                        principalTable: "PersonelAvanslar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonelAvansMahsuplar_PersonelMaaslari_MaasId",
                        column: x => x.MaasId,
                        principalTable: "PersonelMaaslari",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PersonelBorcOdemeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BorcId = table.Column<int>(type: "integer", nullable: false),
                    OdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OdemeTutari = table.Column<decimal>(type: "numeric", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    OdemeSekli = table.Column<int>(type: "integer", nullable: false),
                    BankaHesapId = table.Column<int>(type: "integer", nullable: true),
                    MuhasebeFisId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonelBorcOdemeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonelBorcOdemeler_BankaHesaplari_BankaHesapId",
                        column: x => x.BankaHesapId,
                        principalTable: "BankaHesaplari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelBorcOdemeler_MuhasebeFisleri_MuhasebeFisId",
                        column: x => x.MuhasebeFisId,
                        principalTable: "MuhasebeFisleri",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonelBorcOdemeler_PersonelBorclar_BorcId",
                        column: x => x.BorcId,
                        principalTable: "PersonelBorclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BordroOdemeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BordroDetayId = table.Column<int>(type: "integer", nullable: false),
                    OdemeTipi = table.Column<int>(type: "integer", nullable: false),
                    OdemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OdemeTutari = table.Column<decimal>(type: "numeric", nullable: false),
                    OdemeSekli = table.Column<int>(type: "integer", nullable: false),
                    BankaHesapId = table.Column<int>(type: "integer", nullable: true),
                    EvrakNo = table.Column<string>(type: "text", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    MuhasebeFisId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BordroOdemeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BordroOdemeler_BankaHesaplari_BankaHesapId",
                        column: x => x.BankaHesapId,
                        principalTable: "BankaHesaplari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BordroOdemeler_BordroDetaylar_BordroDetayId",
                        column: x => x.BordroDetayId,
                        principalTable: "BordroDetaylar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BordroOdemeler_MuhasebeFisleri_MuhasebeFisId",
                        column: x => x.MuhasebeFisId,
                        principalTable: "MuhasebeFisleri",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeFisleri_BordroId",
                table: "MuhasebeFisleri",
                column: "BordroId");

            migrationBuilder.CreateIndex(
                name: "IX_AracMasraflari_CariId",
                table: "AracMasraflari",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_AracMasraflari_MuhasebeFisId",
                table: "AracMasraflari",
                column: "MuhasebeFisId");

            migrationBuilder.CreateIndex(
                name: "IX_AracMasraflari_SoforId",
                table: "AracMasraflari",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_BordroAyarlar_FirmaId",
                table: "BordroAyarlar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_BordroDetaylar_BordroId",
                table: "BordroDetaylar",
                column: "BordroId");

            migrationBuilder.CreateIndex(
                name: "IX_BordroDetaylar_FirmaId",
                table: "BordroDetaylar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_BordroDetaylar_PersonelId",
                table: "BordroDetaylar",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_Bordrolar_FirmaId",
                table: "Bordrolar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_BordroOdemeler_BankaHesapId",
                table: "BordroOdemeler",
                column: "BankaHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_BordroOdemeler_BordroDetayId",
                table: "BordroOdemeler",
                column: "BordroDetayId");

            migrationBuilder.CreateIndex(
                name: "IX_BordroOdemeler_MuhasebeFisId",
                table: "BordroOdemeler",
                column: "MuhasebeFisId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelAvanslar_BankaHesapId",
                table: "PersonelAvanslar",
                column: "BankaHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelAvanslar_FirmaId",
                table: "PersonelAvanslar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelAvanslar_MuhasebeFisId",
                table: "PersonelAvanslar",
                column: "MuhasebeFisId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelAvanslar_PersonelId",
                table: "PersonelAvanslar",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelAvansMahsuplar_AvansId",
                table: "PersonelAvansMahsuplar",
                column: "AvansId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelAvansMahsuplar_BankaHesapId",
                table: "PersonelAvansMahsuplar",
                column: "BankaHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelAvansMahsuplar_MaasId",
                table: "PersonelAvansMahsuplar",
                column: "MaasId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelBorclar_BankaHesapId",
                table: "PersonelBorclar",
                column: "BankaHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelBorclar_FirmaId",
                table: "PersonelBorclar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelBorclar_MuhasebeFisId",
                table: "PersonelBorclar",
                column: "MuhasebeFisId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelBorclar_PersonelId",
                table: "PersonelBorclar",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelBorcOdemeler_BankaHesapId",
                table: "PersonelBorcOdemeler",
                column: "BankaHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelBorcOdemeler_BorcId",
                table: "PersonelBorcOdemeler",
                column: "BorcId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelBorcOdemeler_MuhasebeFisId",
                table: "PersonelBorcOdemeler",
                column: "MuhasebeFisId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelFinansAyarlar_BankaHesapId",
                table: "PersonelFinansAyarlar",
                column: "BankaHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelFinansAyarlar_FirmaId",
                table: "PersonelFinansAyarlar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelFinansAyarlar_KasaHesapId",
                table: "PersonelFinansAyarlar",
                column: "KasaHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelFinansAyarlar_PersonelAvanslariHesapId",
                table: "PersonelFinansAyarlar",
                column: "PersonelAvanslariHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelFinansAyarlar_PersoneleBorclarHesapId",
                table: "PersonelFinansAyarlar",
                column: "PersoneleBorclarHesapId");

            migrationBuilder.AddForeignKey(
                name: "FK_AracMasraflari_Cariler_CariId",
                table: "AracMasraflari",
                column: "CariId",
                principalTable: "Cariler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AracMasraflari_MuhasebeFisleri_MuhasebeFisId",
                table: "AracMasraflari",
                column: "MuhasebeFisId",
                principalTable: "MuhasebeFisleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AracMasraflari_Personeller_SoforId",
                table: "AracMasraflari",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AylikChecklistler_Personeller_SoforId",
                table: "AylikChecklistler",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_Personeller_SoforId",
                table: "Cariler",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FiloGunlukPuantajlar_Personeller_SoforId",
                table: "FiloGunlukPuantajlar",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Personeller_SoforId",
                table: "FiloGuzergahEslestirmeleri",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Personeller_VarsayilanSoforId",
                table: "Guzergahlar",
                column: "VarsayilanSoforId",
                principalTable: "Personeller",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Personeller_SoforId",
                table: "Kullanicilar",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MuhasebeFisleri_Bordrolar_BordroId",
                table: "MuhasebeFisleri",
                column: "BordroId",
                principalTable: "Bordrolar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelIzinHaklari_Personeller_SoforId",
                table: "PersonelIzinHaklari",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelIzinleri_Personeller_SoforId",
                table: "PersonelIzinleri",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelMaaslari_Personeller_SoforId",
                table: "PersonelMaaslari",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelOzlukEvraklar_Personeller_SoforId",
                table: "PersonelOzlukEvraklar",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelPuantajlar_Personeller_PersonelId",
                table: "PersonelPuantajlar",
                column: "PersonelId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServisCalismaKiralamalar_Personeller_SoforId",
                table: "ServisCalismaKiralamalar",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServisCalismalari_Personeller_SoforId",
                table: "ServisCalismalari",
                column: "SoforId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AracMasraflari_Cariler_CariId",
                table: "AracMasraflari");

            migrationBuilder.DropForeignKey(
                name: "FK_AracMasraflari_MuhasebeFisleri_MuhasebeFisId",
                table: "AracMasraflari");

            migrationBuilder.DropForeignKey(
                name: "FK_AracMasraflari_Personeller_SoforId",
                table: "AracMasraflari");

            migrationBuilder.DropForeignKey(
                name: "FK_AylikChecklistler_Personeller_SoforId",
                table: "AylikChecklistler");

            migrationBuilder.DropForeignKey(
                name: "FK_Cariler_Personeller_SoforId",
                table: "Cariler");

            migrationBuilder.DropForeignKey(
                name: "FK_FiloGunlukPuantajlar_Personeller_SoforId",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Personeller_SoforId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropForeignKey(
                name: "FK_Guzergahlar_Personeller_VarsayilanSoforId",
                table: "Guzergahlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Personeller_SoforId",
                table: "Kullanicilar");

            migrationBuilder.DropForeignKey(
                name: "FK_MuhasebeFisleri_Bordrolar_BordroId",
                table: "MuhasebeFisleri");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelIzinHaklari_Personeller_SoforId",
                table: "PersonelIzinHaklari");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelIzinleri_Personeller_SoforId",
                table: "PersonelIzinleri");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelMaaslari_Personeller_SoforId",
                table: "PersonelMaaslari");

            migrationBuilder.Sql(@"ALTER TABLE ""PersonelOzlukEvraklar"" DROP CONSTRAINT IF EXISTS ""FK_PersonelOzlukEvraklar_Personeller_SoforId"";");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelPuantajlar_Personeller_PersonelId",
                table: "PersonelPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_ServisCalismaKiralamalar_Personeller_SoforId",
                table: "ServisCalismaKiralamalar");

            migrationBuilder.DropForeignKey(
                name: "FK_ServisCalismalari_Personeller_SoforId",
                table: "ServisCalismalari");

            migrationBuilder.DropTable(
                name: "BordroAyarlar");

            migrationBuilder.DropTable(
                name: "BordroOdemeler");

            migrationBuilder.DropTable(
                name: "PersonelAvansMahsuplar");

            migrationBuilder.DropTable(
                name: "PersonelBorcOdemeler");

            migrationBuilder.DropTable(
                name: "PersonelFinansAyarlar");

            migrationBuilder.DropTable(
                name: "BordroDetaylar");

            migrationBuilder.DropTable(
                name: "PersonelAvanslar");

            migrationBuilder.DropTable(
                name: "PersonelBorclar");

            migrationBuilder.DropTable(
                name: "Bordrolar");

            migrationBuilder.DropIndex(
                name: "IX_MuhasebeFisleri_BordroId",
                table: "MuhasebeFisleri");

            migrationBuilder.DropIndex(
                name: "IX_AracMasraflari_CariId",
                table: "AracMasraflari");

            migrationBuilder.DropIndex(
                name: "IX_AracMasraflari_MuhasebeFisId",
                table: "AracMasraflari");

            migrationBuilder.DropIndex(
                name: "IX_AracMasraflari_SoforId",
                table: "AracMasraflari");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Personeller",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "BordroId",
                table: "MuhasebeFisleri");

            migrationBuilder.DropColumn(
                name: "CariId",
                table: "AracMasraflari");

            migrationBuilder.DropColumn(
                name: "MuhasebeFisId",
                table: "AracMasraflari");

            migrationBuilder.DropColumn(
                name: "SoforId",
                table: "AracMasraflari");

            migrationBuilder.DropColumn(
                name: "ArgePersoneli",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "BirimUcret",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "BordroTipiPersonel",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "BrutMaasHesaplamaTipi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "CalismaMiktari",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "DigerMaas",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "ResmiNetMaas",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "SGKBordroDahilMi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "SgkMaasi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "SiralamaNo",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "TopluMaas",
                table: "Personeller");

            migrationBuilder.RenameTable(
                name: "Personeller",
                newName: "Soforler");

            migrationBuilder.RenameIndex(
                name: "IX_Personeller_SoforKodu",
                table: "Soforler",
                newName: "IX_Soforler_SoforKodu");

            migrationBuilder.AlterColumn<decimal>(
                name: "NetMaas",
                table: "Soforler",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "BrutMaas",
                table: "Soforler",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Soforler",
                table: "Soforler",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AylikChecklistler_Soforler_SoforId",
                table: "AylikChecklistler",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_Soforler_SoforId",
                table: "Cariler",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FiloGunlukPuantajlar_Soforler_SoforId",
                table: "FiloGunlukPuantajlar",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Soforler_SoforId",
                table: "FiloGuzergahEslestirmeleri",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Soforler_VarsayilanSoforId",
                table: "Guzergahlar",
                column: "VarsayilanSoforId",
                principalTable: "Soforler",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Soforler_SoforId",
                table: "Kullanicilar",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelIzinHaklari_Soforler_SoforId",
                table: "PersonelIzinHaklari",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelIzinleri_Soforler_SoforId",
                table: "PersonelIzinleri",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelMaaslari_Soforler_SoforId",
                table: "PersonelMaaslari",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelOzlukEvraklar_Soforler_SoforId",
                table: "PersonelOzlukEvraklar",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelPuantajlar_Soforler_PersonelId",
                table: "PersonelPuantajlar",
                column: "PersonelId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServisCalismaKiralamalar_Soforler_SoforId",
                table: "ServisCalismaKiralamalar",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServisCalismalari_Soforler_SoforId",
                table: "ServisCalismalari",
                column: "SoforId",
                principalTable: "Soforler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}



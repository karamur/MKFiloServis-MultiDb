using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetHedef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CariId1 shadow property temizligi - IF EXISTS ile guvenlice
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_Hatirlaticilar_Cariler_CariId1' AND table_name = 'Hatirlaticilar') THEN
                        ALTER TABLE ""Hatirlaticilar"" DROP CONSTRAINT ""FK_Hatirlaticilar_Cariler_CariId1"";
                    END IF;
                END $$;
            ");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Hatirlaticilar_CariId1"";");
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Hatirlaticilar' AND column_name = 'CariId1') THEN
                        ALTER TABLE ""Hatirlaticilar"" DROP COLUMN ""CariId1"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"ALTER TABLE ""PuantajKayitlar"" ADD COLUMN IF NOT EXISTS ""AitFirmaAdi"" character varying(200);");
            migrationBuilder.Sql(@"ALTER TABLE ""PuantajKayitlar"" ADD COLUMN IF NOT EXISTS ""Bolge"" character varying(100);");
            migrationBuilder.Sql(@"ALTER TABLE ""PuantajKayitlar"" ADD COLUMN IF NOT EXISTS ""SiraNo"" integer NOT NULL DEFAULT 0;");

            for (var gun = 1; gun <= 31; gun++)
            {
                migrationBuilder.Sql($@"ALTER TABLE ""PuantajKayitlar"" ADD COLUMN IF NOT EXISTS ""Gun{gun:D2}"" integer NOT NULL DEFAULT 0;");
            }

            migrationBuilder.CreateTable(
                name: "BudgetHedefler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    MasrafKalemi = table.Column<string>(type: "text", nullable: false),
                    HedefTutar = table.Column<decimal>(type: "numeric", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetHedefler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetHedefler_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CariHatirlatmalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CariId = table.Column<int>(type: "integer", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    Tip = table.Column<int>(type: "integer", nullable: false),
                    Baslik = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Tutar = table.Column<decimal>(type: "numeric", nullable: true),
                    VadeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VadeGecenGun = table.Column<int>(type: "integer", nullable: true),
                    EmailGonderildi = table.Column<bool>(type: "boolean", nullable: false),
                    EmailGonderimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MusteriyeEmailGonderildi = table.Column<bool>(type: "boolean", nullable: false),
                    BildirimOlusturuldu = table.Column<bool>(type: "boolean", nullable: false),
                    BildirimId = table.Column<int>(type: "integer", nullable: true),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariHatirlatmalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CariHatirlatmalar_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CariHatirlatmalar_Faturalar_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CariHatirlatmalar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DestekAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Deger = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Grup = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekAyarlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DestekDepartmanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OtomatikAtama = table.Column<bool>(type: "boolean", nullable: false),
                    VarsayilanSlaSuresi = table.Column<int>(type: "integer", nullable: true),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    UstDepartmanId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekDepartmanlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekDepartmanlari_DestekDepartmanlari_UstDepartmanId",
                        column: x => x.UstDepartmanId,
                        principalTable: "DestekDepartmanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DestekSlaListesi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    IlkYanitSuresi = table.Column<int>(type: "integer", nullable: false),
                    CozumSuresi = table.Column<int>(type: "integer", nullable: false),
                    Oncelik = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    SadeceMesaiSaatleri = table.Column<bool>(type: "boolean", nullable: false),
                    SadeceHaftaIci = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekSlaListesi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EbysEvrakKategoriler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KategoriAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Renk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Ikon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbysEvrakKategoriler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaturaSablonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    SablonAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Varsayilan = table.Column<bool>(type: "boolean", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    SayfaBoyutu = table.Column<int>(type: "integer", nullable: false),
                    SayfaYonelimi = table.Column<int>(type: "integer", nullable: false),
                    SayfaKenarBoslukSol = table.Column<int>(type: "integer", nullable: false),
                    SayfaKenarBoslukSag = table.Column<int>(type: "integer", nullable: false),
                    SayfaKenarBoslukUst = table.Column<int>(type: "integer", nullable: false),
                    SayfaKenarBoslukAlt = table.Column<int>(type: "integer", nullable: false),
                    LogoGoster = table.Column<bool>(type: "boolean", nullable: false),
                    LogoKonumu = table.Column<int>(type: "integer", nullable: false),
                    LogoGenislik = table.Column<int>(type: "integer", nullable: false),
                    LogoYukseklik = table.Column<int>(type: "integer", nullable: false),
                    OzelLogo = table.Column<string>(type: "text", nullable: true),
                    FirmaAdiGoster = table.Column<bool>(type: "boolean", nullable: false),
                    FirmaAdiFontBoyutu = table.Column<int>(type: "integer", nullable: false),
                    FirmaAdresGoster = table.Column<bool>(type: "boolean", nullable: false),
                    FirmaTelefonGoster = table.Column<bool>(type: "boolean", nullable: false),
                    FirmaEmailGoster = table.Column<bool>(type: "boolean", nullable: false),
                    FirmaVergiGoster = table.Column<bool>(type: "boolean", nullable: false),
                    AnaPrimaryRenk = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AnaSecondaryRenk = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TabloBaslikArkaplanRenk = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TabloBaslikYaziRenk = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TabloSatirCizgiRenk = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ToplamArkaplanRenk = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FontAdi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VarsayilanFontBoyutu = table.Column<int>(type: "integer", nullable: false),
                    BaslikFontBoyutu = table.Column<int>(type: "integer", nullable: false),
                    FaturaBaslikMetni = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FaturaBaslikKonumu = table.Column<int>(type: "integer", nullable: false),
                    FaturaBilgiKutusuGoster = table.Column<bool>(type: "boolean", nullable: false),
                    CariBilgiKutusuGoster = table.Column<bool>(type: "boolean", nullable: false),
                    KutuCercevesiGoster = table.Column<bool>(type: "boolean", nullable: false),
                    KutuPadding = table.Column<int>(type: "integer", nullable: false),
                    TabloSiraNoGoster = table.Column<bool>(type: "boolean", nullable: false),
                    TabloKdvSutunuGoster = table.Column<bool>(type: "boolean", nullable: false),
                    TabloIskontoSutunuGoster = table.Column<bool>(type: "boolean", nullable: false),
                    TabloZebraDeseni = table.Column<bool>(type: "boolean", nullable: false),
                    TabloZebraRenk = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ToplamKonumu = table.Column<int>(type: "integer", nullable: false),
                    ToplamBolumGenislik = table.Column<int>(type: "integer", nullable: false),
                    AraToplamGoster = table.Column<bool>(type: "boolean", nullable: false),
                    KdvToplamGoster = table.Column<bool>(type: "boolean", nullable: false),
                    OdenenGoster = table.Column<bool>(type: "boolean", nullable: false),
                    KalanGoster = table.Column<bool>(type: "boolean", nullable: false),
                    BankaBilgileriGoster = table.Column<bool>(type: "boolean", nullable: false),
                    BankaBilgileri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NotlarGoster = table.Column<bool>(type: "boolean", nullable: false),
                    AltBilgiMetni = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SayfaNumarasiGoster = table.Column<bool>(type: "boolean", nullable: false),
                    KaseAlaniGoster = table.Column<bool>(type: "boolean", nullable: false),
                    ImzaAlaniGoster = table.Column<bool>(type: "boolean", nullable: false),
                    ImzaMetni = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    KaseResmi = table.Column<string>(type: "text", nullable: true),
                    QrKodGoster = table.Column<bool>(type: "boolean", nullable: false),
                    QrKodIcerik = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaturaSablonlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaturaSablonlari_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DestekDepartmanUyeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DepartmanId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    Yonetici = table.Column<bool>(type: "boolean", nullable: false),
                    OtomatikAtamaUygun = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekDepartmanUyeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekDepartmanUyeleri_DestekDepartmanlari_DepartmanId",
                        column: x => x.DepartmanId,
                        principalTable: "DestekDepartmanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DestekDepartmanUyeleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DestekKategorileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Renk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Simge = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    DepartmanId = table.Column<int>(type: "integer", nullable: true),
                    UstKategoriId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekKategorileri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekKategorileri_DestekDepartmanlari_DepartmanId",
                        column: x => x.DepartmanId,
                        principalTable: "DestekDepartmanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DestekKategorileri_DestekKategorileri_UstKategoriId",
                        column: x => x.UstKategoriId,
                        principalTable: "DestekKategorileri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EbysEvraklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvrakNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Yon = table.Column<int>(type: "integer", nullable: false),
                    EvrakTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Konu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ozet = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GonderenKurum = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    AliciKurum = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    GelisNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GelisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GidisNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GonderimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GonderimYontemi = table.Column<int>(type: "integer", nullable: false),
                    KategoriId = table.Column<int>(type: "integer", nullable: true),
                    Oncelik = table.Column<int>(type: "integer", nullable: false),
                    Gizlilik = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    SonIslemTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CevapSuresi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CevapGerekli = table.Column<bool>(type: "boolean", nullable: false),
                    UstEvrakId = table.Column<int>(type: "integer", nullable: true),
                    AtananKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    AtananDepartmanId = table.Column<int>(type: "integer", nullable: true),
                    Aciklama = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notlar = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbysEvraklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EbysEvraklar_EbysEvrakKategoriler_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "EbysEvrakKategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EbysEvraklar_EbysEvraklar_UstEvrakId",
                        column: x => x.UstEvrakId,
                        principalTable: "EbysEvraklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EbysEvraklar_Kullanicilar_AtananKullaniciId",
                        column: x => x.AtananKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DestekBilgiBankasiMakaleleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Baslik = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Icerik = table.Column<string>(type: "text", nullable: false),
                    Ozet = table.Column<string>(type: "text", nullable: true),
                    Etiketler = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SeoBaslik = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SeoAciklama = table.Column<string>(type: "text", nullable: true),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GoruntulemeSayisi = table.Column<int>(type: "integer", nullable: false),
                    YararliBulmaSayisi = table.Column<int>(type: "integer", nullable: false),
                    YararsizBulmaSayisi = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    YayinlanmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    KategoriId = table.Column<int>(type: "integer", nullable: true),
                    YazarKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekBilgiBankasiMakaleleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekBilgiBankasiMakaleleri_DestekKategorileri_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "DestekKategorileri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DestekBilgiBankasiMakaleleri_Kullanicilar_YazarKullaniciId",
                        column: x => x.YazarKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DestekHazirYanitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Icerik = table.Column<string>(type: "text", nullable: false),
                    KonuSablonu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    DepartmanId = table.Column<int>(type: "integer", nullable: true),
                    KategoriId = table.Column<int>(type: "integer", nullable: true),
                    KullanimSayisi = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekHazirYanitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekHazirYanitlari_DestekDepartmanlari_DepartmanId",
                        column: x => x.DepartmanId,
                        principalTable: "DestekDepartmanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DestekHazirYanitlari_DestekKategorileri_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "DestekKategorileri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DestekTalepleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TalepNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Konu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Oncelik = table.Column<int>(type: "integer", nullable: false),
                    Kaynak = table.Column<int>(type: "integer", nullable: false),
                    SlaSuresi = table.Column<int>(type: "integer", nullable: true),
                    SlaBitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SlaAsildi = table.Column<bool>(type: "boolean", nullable: false),
                    SonAktiviteTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    KapatilmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CozumSuresiDakika = table.Column<int>(type: "integer", nullable: true),
                    IlkYanitSuresiDakika = table.Column<int>(type: "integer", nullable: true),
                    MemnuniyetPuani = table.Column<int>(type: "integer", nullable: true),
                    MemnuniyetYorumu = table.Column<string>(type: "text", nullable: true),
                    DahiliNotlar = table.Column<string>(type: "text", nullable: true),
                    Etiketler = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DepartmanId = table.Column<int>(type: "integer", nullable: false),
                    KategoriId = table.Column<int>(type: "integer", nullable: true),
                    AtananKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    OlusturanKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    MusteriAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MusteriEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MusteriTelefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekTalepleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekTalepleri_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DestekTalepleri_DestekDepartmanlari_DepartmanId",
                        column: x => x.DepartmanId,
                        principalTable: "DestekDepartmanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DestekTalepleri_DestekKategorileri_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "DestekKategorileri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DestekTalepleri_Kullanicilar_AtananKullaniciId",
                        column: x => x.AtananKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DestekTalepleri_Kullanicilar_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EbysEvrakAtamalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvrakId = table.Column<int>(type: "integer", nullable: false),
                    AtananKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    AtananDepartmanId = table.Column<int>(type: "integer", nullable: true),
                    AtayanKullaniciId = table.Column<int>(type: "integer", nullable: false),
                    AtamaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Talimat = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TeslimTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Sonuc = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbysEvrakAtamalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EbysEvrakAtamalar_EbysEvraklar_EvrakId",
                        column: x => x.EvrakId,
                        principalTable: "EbysEvraklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EbysEvrakAtamalar_Kullanicilar_AtananKullaniciId",
                        column: x => x.AtananKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EbysEvrakAtamalar_Kullanicilar_AtayanKullaniciId",
                        column: x => x.AtayanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EbysEvrakDosyalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvrakId = table.Column<int>(type: "integer", nullable: false),
                    DosyaAdi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DosyaYolu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DosyaTipi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DosyaBoyutu = table.Column<long>(type: "bigint", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AsilNusha = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbysEvrakDosyalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EbysEvrakDosyalar_EbysEvraklar_EvrakId",
                        column: x => x.EvrakId,
                        principalTable: "EbysEvraklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EbysEvrakHareketler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvrakId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    HareketTipi = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IslemTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EskiDeger = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    YeniDeger = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbysEvrakHareketler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EbysEvrakHareketler_EbysEvraklar_EvrakId",
                        column: x => x.EvrakId,
                        principalTable: "EbysEvraklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EbysEvrakHareketler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DestekTalebiAktiviteleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DestekTalebiId = table.Column<int>(type: "integer", nullable: false),
                    AktiviteTuru = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EskiDeger = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    YeniDeger = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    KullaniciId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekTalebiAktiviteleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekTalebiAktiviteleri_DestekTalepleri_DestekTalebiId",
                        column: x => x.DestekTalebiId,
                        principalTable: "DestekTalepleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DestekTalebiAktiviteleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DestekTalebiIliskileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnaTalepId = table.Column<int>(type: "integer", nullable: false),
                    IliskiliTalepId = table.Column<int>(type: "integer", nullable: false),
                    IliskiTuru = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekTalebiIliskileri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekTalebiIliskileri_DestekTalepleri_AnaTalepId",
                        column: x => x.AnaTalepId,
                        principalTable: "DestekTalepleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DestekTalebiIliskileri_DestekTalepleri_IliskiliTalepId",
                        column: x => x.IliskiliTalepId,
                        principalTable: "DestekTalepleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DestekTalebiYanitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DestekTalebiId = table.Column<int>(type: "integer", nullable: false),
                    Icerik = table.Column<string>(type: "text", nullable: false),
                    DahiliNot = table.Column<bool>(type: "boolean", nullable: false),
                    YanitTuru = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: true),
                    MusteriYaniti = table.Column<bool>(type: "boolean", nullable: false),
                    MusteriAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HazirYanitId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekTalebiYanitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekTalebiYanitlari_DestekTalepleri_DestekTalebiId",
                        column: x => x.DestekTalebiId,
                        principalTable: "DestekTalepleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DestekTalebiYanitlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DestekTalebiEkleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DestekTalebiId = table.Column<int>(type: "integer", nullable: true),
                    YanitId = table.Column<int>(type: "integer", nullable: true),
                    DosyaAdi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OrijinalDosyaAdi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DosyaYolu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DosyaBoyutu = table.Column<long>(type: "bigint", nullable: false),
                    MimeTipi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    YukleyenKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekTalebiEkleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekTalebiEkleri_DestekTalebiYanitlari_YanitId",
                        column: x => x.YanitId,
                        principalTable: "DestekTalebiYanitlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DestekTalebiEkleri_DestekTalepleri_DestekTalebiId",
                        column: x => x.DestekTalebiId,
                        principalTable: "DestekTalepleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DestekTalebiEkleri_Kullanicilar_YukleyenKullaniciId",
                        column: x => x.YukleyenKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetHedefler_FirmaId",
                table: "BudgetHedefler",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHatirlatmalar_CariId",
                table: "CariHatirlatmalar",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHatirlatmalar_FaturaId",
                table: "CariHatirlatmalar",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHatirlatmalar_FirmaId",
                table: "CariHatirlatmalar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekAyarlari_Anahtar",
                table: "DestekAyarlari",
                column: "Anahtar",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DestekBilgiBankasiMakaleleri_Durum",
                table: "DestekBilgiBankasiMakaleleri",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_DestekBilgiBankasiMakaleleri_KategoriId",
                table: "DestekBilgiBankasiMakaleleri",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekBilgiBankasiMakaleleri_Slug",
                table: "DestekBilgiBankasiMakaleleri",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DestekBilgiBankasiMakaleleri_YazarKullaniciId",
                table: "DestekBilgiBankasiMakaleleri",
                column: "YazarKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekDepartmanlari_Ad",
                table: "DestekDepartmanlari",
                column: "Ad");

            migrationBuilder.CreateIndex(
                name: "IX_DestekDepartmanlari_UstDepartmanId",
                table: "DestekDepartmanlari",
                column: "UstDepartmanId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekDepartmanUyeleri_DepartmanId_KullaniciId",
                table: "DestekDepartmanUyeleri",
                columns: new[] { "DepartmanId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DestekDepartmanUyeleri_KullaniciId",
                table: "DestekDepartmanUyeleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekHazirYanitlari_DepartmanId",
                table: "DestekHazirYanitlari",
                column: "DepartmanId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekHazirYanitlari_KategoriId",
                table: "DestekHazirYanitlari",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekKategorileri_Ad",
                table: "DestekKategorileri",
                column: "Ad");

            migrationBuilder.CreateIndex(
                name: "IX_DestekKategorileri_DepartmanId",
                table: "DestekKategorileri",
                column: "DepartmanId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekKategorileri_UstKategoriId",
                table: "DestekKategorileri",
                column: "UstKategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekSlaListesi_Oncelik",
                table: "DestekSlaListesi",
                column: "Oncelik");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalebiAktiviteleri_DestekTalebiId_CreatedAt",
                table: "DestekTalebiAktiviteleri",
                columns: new[] { "DestekTalebiId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalebiAktiviteleri_KullaniciId",
                table: "DestekTalebiAktiviteleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalebiEkleri_DestekTalebiId",
                table: "DestekTalebiEkleri",
                column: "DestekTalebiId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalebiEkleri_YanitId",
                table: "DestekTalebiEkleri",
                column: "YanitId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalebiEkleri_YukleyenKullaniciId",
                table: "DestekTalebiEkleri",
                column: "YukleyenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalebiIliskileri_AnaTalepId_IliskiliTalepId",
                table: "DestekTalebiIliskileri",
                columns: new[] { "AnaTalepId", "IliskiliTalepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalebiIliskileri_IliskiliTalepId",
                table: "DestekTalebiIliskileri",
                column: "IliskiliTalepId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalebiYanitlari_DestekTalebiId_CreatedAt",
                table: "DestekTalebiYanitlari",
                columns: new[] { "DestekTalebiId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalebiYanitlari_KullaniciId",
                table: "DestekTalebiYanitlari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalepleri_AtananKullaniciId_Durum",
                table: "DestekTalepleri",
                columns: new[] { "AtananKullaniciId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalepleri_CariId",
                table: "DestekTalepleri",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalepleri_DepartmanId_Durum",
                table: "DestekTalepleri",
                columns: new[] { "DepartmanId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalepleri_KategoriId",
                table: "DestekTalepleri",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalepleri_OlusturanKullaniciId",
                table: "DestekTalepleri",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalepleri_SonAktiviteTarihi",
                table: "DestekTalepleri",
                column: "SonAktiviteTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_DestekTalepleri_TalepNo",
                table: "DestekTalepleri",
                column: "TalepNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvrakAtamalar_AtananKullaniciId",
                table: "EbysEvrakAtamalar",
                column: "AtananKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvrakAtamalar_AtayanKullaniciId",
                table: "EbysEvrakAtamalar",
                column: "AtayanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvrakAtamalar_EvrakId_Durum",
                table: "EbysEvrakAtamalar",
                columns: new[] { "EvrakId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvrakDosyalar_EvrakId",
                table: "EbysEvrakDosyalar",
                column: "EvrakId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvrakHareketler_EvrakId_IslemTarihi",
                table: "EbysEvrakHareketler",
                columns: new[] { "EvrakId", "IslemTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvrakHareketler_KullaniciId",
                table: "EbysEvrakHareketler",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvrakKategoriler_KategoriAdi",
                table: "EbysEvrakKategoriler",
                column: "KategoriAdi");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvraklar_AtananKullaniciId",
                table: "EbysEvraklar",
                column: "AtananKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvraklar_EvrakNo",
                table: "EbysEvraklar",
                column: "EvrakNo");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvraklar_EvrakTarihi",
                table: "EbysEvraklar",
                column: "EvrakTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvraklar_KategoriId",
                table: "EbysEvraklar",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvraklar_UstEvrakId",
                table: "EbysEvraklar",
                column: "UstEvrakId");

            migrationBuilder.CreateIndex(
                name: "IX_EbysEvraklar_Yon_Durum",
                table: "EbysEvraklar",
                columns: new[] { "Yon", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_FaturaSablonlari_FirmaId",
                table: "FaturaSablonlari",
                column: "FirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetHedefler");

            migrationBuilder.DropTable(
                name: "CariHatirlatmalar");

            migrationBuilder.DropTable(
                name: "DestekAyarlari");

            migrationBuilder.DropTable(
                name: "DestekBilgiBankasiMakaleleri");

            migrationBuilder.DropTable(
                name: "DestekDepartmanUyeleri");

            migrationBuilder.DropTable(
                name: "DestekHazirYanitlari");

            migrationBuilder.DropTable(
                name: "DestekSlaListesi");

            migrationBuilder.DropTable(
                name: "DestekTalebiAktiviteleri");

            migrationBuilder.DropTable(
                name: "DestekTalebiEkleri");

            migrationBuilder.DropTable(
                name: "DestekTalebiIliskileri");

            migrationBuilder.DropTable(
                name: "EbysEvrakAtamalar");

            migrationBuilder.DropTable(
                name: "EbysEvrakDosyalar");

            migrationBuilder.DropTable(
                name: "EbysEvrakHareketler");

            migrationBuilder.DropTable(
                name: "FaturaSablonlari");

            migrationBuilder.DropTable(
                name: "DestekTalebiYanitlari");

            migrationBuilder.DropTable(
                name: "EbysEvraklar");

            migrationBuilder.DropTable(
                name: "DestekTalepleri");

            migrationBuilder.DropTable(
                name: "EbysEvrakKategoriler");

            migrationBuilder.DropTable(
                name: "DestekKategorileri");

            migrationBuilder.DropTable(
                name: "DestekDepartmanlari");

            migrationBuilder.DropColumn(
                name: "AitFirmaAdi",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Bolge",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun01",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun02",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun03",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun04",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun05",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun06",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun07",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun08",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun09",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun10",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun11",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun12",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun13",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun14",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun15",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun16",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun17",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun18",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun19",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun20",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun21",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun22",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun23",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun24",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun25",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun26",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun27",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun28",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun29",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun30",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Gun31",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "SiraNo",
                table: "PuantajKayitlar");

            migrationBuilder.AddColumn<int>(
                name: "CariId1",
                table: "Hatirlaticilar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hatirlaticilar_CariId1",
                table: "Hatirlaticilar",
                column: "CariId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Hatirlaticilar_Cariler_CariId1",
                table: "Hatirlaticilar",
                column: "CariId1",
                principalTable: "Cariler",
                principalColumn: "Id");
        }
    }
}



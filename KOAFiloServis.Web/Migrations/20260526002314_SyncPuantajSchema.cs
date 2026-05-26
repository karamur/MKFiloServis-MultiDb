using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KOAFiloServis.Web.Migrations
{
    /// <inheritdoc />
    public partial class SyncPuantajSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── PuantajKayitlar: idempotent column adds (may already exist) ──
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""BelgeNo"" character varying(50) NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""FinansYonu"" integer NOT NULL DEFAULT 0';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""HesapDonemiId"" integer NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""IsverenFirmaId"" integer NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""KaynakTipi"" integer NOT NULL DEFAULT 0';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""KurumId"" integer NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""OncekiVersiyonId"" integer NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""Slot"" integer NOT NULL DEFAULT 0';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""SlotAdi"" character varying(50) NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""TransferDurum"" character varying(50) NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""PuantajKayitlar"" ADD COLUMN ""Versiyon"" integer NOT NULL DEFAULT 0';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                -- KiralikPlakaTakipler
                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""KiralikPlakaTakipler"" ADD COLUMN ""GelenFaturaId"" integer NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""KiralikPlakaTakipler"" ADD COLUMN ""KalanFaturaTutar"" numeric NOT NULL DEFAULT 0';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""KiralikPlakaTakipler"" ADD COLUMN ""KesilenFaturaNo"" character varying(50) NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""KiralikPlakaTakipler"" ADD COLUMN ""KesilenFaturaTarih"" timestamp without time zone NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""KiralikPlakaTakipler"" ADD COLUMN ""KesilenFaturaTutar"" numeric NOT NULL DEFAULT 0';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""KiralikPlakaTakipler"" ADD COLUMN ""OdenenTutar"" numeric NOT NULL DEFAULT 0';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""KiralikPlakaTakipler"" ADD COLUMN ""SonOdemeTarihi"" timestamp without time zone NULL';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""KiralikPlakaTakipler"" ADD COLUMN ""ToplamOdeme"" numeric NOT NULL DEFAULT 0';
                EXCEPTION WHEN duplicate_column THEN END; $$;

                -- GuzergahSeferleri
                DO $$ BEGIN
                    EXECUTE 'ALTER TABLE ""GuzergahSeferleri"" ADD COLUMN ""Slot"" integer NOT NULL DEFAULT 0';
                EXCEPTION WHEN duplicate_column THEN END; $$;
            ");

            // ── PuantajKayitlar: drop old index, add new indexes ──
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_PuantajKayitlar_Yil_Ay_GuzergahId_AracId"";");
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_PuantajKayitlar_HesapDonemiId"" ON ""PuantajKayitlar"" (""HesapDonemiId"");
                CREATE INDEX IF NOT EXISTS ""IX_PuantajKayitlar_IsverenFirmaId"" ON ""PuantajKayitlar"" (""IsverenFirmaId"");
                CREATE INDEX IF NOT EXISTS ""IX_PuantajKayitlar_KurumId"" ON ""PuantajKayitlar"" (""KurumId"");
                CREATE INDEX IF NOT EXISTS ""IX_PuantajKayitlar_OncekiVersiyonId"" ON ""PuantajKayitlar"" (""OncekiVersiyonId"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PuantajKayitlar_Yil_Ay_GuzergahId_AracId_Slot""
                    ON ""PuantajKayitlar"" (""Yil"", ""Ay"", ""GuzergahId"", ""AracId"", ""Slot"");
            ");

            // ── NEW TABLES ─────────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "OperasyonKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GuzergahId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    SoforId = table.Column<int>(type: "integer", nullable: true),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    SlotAdi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Yon = table.Column<int>(type: "integer", nullable: false),
                    KurumId = table.Column<int>(type: "integer", nullable: true),
                    IsverenFirmaId = table.Column<int>(type: "integer", nullable: true),
                    SeferSayisi = table.Column<int>(type: "integer", nullable: false),
                    PuantajCarpani = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    OperasyonDurumu = table.Column<int>(type: "integer", nullable: false),
                    KaynakTipi = table.Column<int>(type: "integer", nullable: false),
                    FinansYonu = table.Column<int>(type: "integer", nullable: false),
                    SoforOdemeTipi = table.Column<int>(type: "integer", nullable: false),
                    OdemeYapilacakCariId = table.Column<int>(type: "integer", nullable: true),
                    FaturaKesiciCariId = table.Column<int>(type: "integer", nullable: true),
                    BelgeNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransferDurum = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Kaynak = table.Column<int>(type: "integer", nullable: false),
                    ExcelImportId = table.Column<int>(type: "integer", nullable: true),
                    ExcelSatirNo = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notlar = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperasyonKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Cariler_FaturaKesiciCariId",
                        column: x => x.FaturaKesiciCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Cariler_OdemeYapilacakCariId",
                        column: x => x.OdemeYapilacakCariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Firmalar_IsverenFirmaId",
                        column: x => x.IsverenFirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Kurumlar_KurumId",
                        column: x => x.KurumId,
                        principalTable: "Kurumlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasyonKayitlari_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PuantajAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    HesapDonemiId = table.Column<int>(type: "integer", nullable: true),
                    Aksiyon = table.Column<int>(type: "integer", nullable: false),
                    Kullanici = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AksiyonTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OncekiDurum = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    YeniDurum = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PuantajHesapDonemleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    KurumId = table.Column<int>(type: "integer", nullable: true),
                    Versiyon = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    HesaplayanKullanici = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HesaplamaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OncekiDonemId = table.Column<int>(type: "integer", nullable: true),
                    Notlar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OnayDurum = table.Column<int>(type: "integer", nullable: false),
                    FinansOnaylayan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FinansOnayTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MuhasebeOnaylayan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MuhasebeOnayTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    KilitTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    KilitAciklama = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajHesapDonemleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuantajHesapDonemleri_PuantajHesapDonemleri_OncekiDonemId",
                        column: x => x.OncekiDonemId,
                        principalTable: "PuantajHesapDonemleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PuantajJobExecutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    KurumId = table.Column<int>(type: "integer", nullable: true),
                    Tetikleyen = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Baslangic = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Bitis = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Versiyon = table.Column<int>(type: "integer", nullable: false),
                    HesapDonemiId = table.Column<int>(type: "integer", nullable: true),
                    IslenenOperasyon = table.Column<int>(type: "integer", nullable: false),
                    UretilenPuantaj = table.Column<int>(type: "integer", nullable: false),
                    HataMesaji = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Hesaplayan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajJobExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PuantajDetaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    OperasyonKaydiId = table.Column<int>(type: "integer", nullable: false),
                    PuantajKayitId = table.Column<int>(type: "integer", nullable: false),
                    HesapDonemiId = table.Column<int>(type: "integer", nullable: false),
                    BirimGelir = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BirimGider = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SeferSayisi = table.Column<int>(type: "integer", nullable: false),
                    HesaplananTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajDetaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuantajDetaylari_OperasyonKayitlari_OperasyonKaydiId",
                        column: x => x.OperasyonKaydiId,
                        principalTable: "OperasyonKayitlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PuantajDetaylari_PuantajHesapDonemleri_HesapDonemiId",
                        column: x => x.HesapDonemiId,
                        principalTable: "PuantajHesapDonemleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PuantajDetaylari_PuantajKayitlar_PuantajKayitId",
                        column: x => x.PuantajKayitId,
                        principalTable: "PuantajKayitlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PuantajFinansalKayitlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    PuantajKayitId = table.Column<int>(type: "integer", nullable: false),
                    HesapDonemiId = table.Column<int>(type: "integer", nullable: false),
                    BirimGelir = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BirimGider = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamGelir = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamGider = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvTutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GenelToplam = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SeferGunu = table.Column<int>(type: "integer", nullable: false),
                    GelirCariId = table.Column<int>(type: "integer", nullable: true),
                    GiderCariId = table.Column<int>(type: "integer", nullable: true),
                    GelirFaturaId = table.Column<int>(type: "integer", nullable: true),
                    GiderFaturaId = table.Column<int>(type: "integer", nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuantajFinansalKayitlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuantajFinansalKayitlar_PuantajHesapDonemleri_HesapDonemiId",
                        column: x => x.HesapDonemiId,
                        principalTable: "PuantajHesapDonemleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PuantajFinansalKayitlar_PuantajKayitlar_PuantajKayitId",
                        column: x => x.PuantajKayitId,
                        principalTable: "PuantajKayitlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // PuantajKayitlar indexes already created via raw SQL above

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_AracId",
                table: "OperasyonKayitlari",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_FaturaKesiciCariId",
                table: "OperasyonKayitlari",
                column: "FaturaKesiciCariId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_FirmaId_Tarih",
                table: "OperasyonKayitlari",
                columns: new[] { "FirmaId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_GuzergahId",
                table: "OperasyonKayitlari",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_IsverenFirmaId",
                table: "OperasyonKayitlari",
                column: "IsverenFirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_KurumId",
                table: "OperasyonKayitlari",
                column: "KurumId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_OdemeYapilacakCariId",
                table: "OperasyonKayitlari",
                column: "OdemeYapilacakCariId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_OperasyonDurumu",
                table: "OperasyonKayitlari",
                column: "OperasyonDurumu");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Slot",
                table: "OperasyonKayitlari",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_SoforId",
                table: "OperasyonKayitlari",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Tarih",
                table: "OperasyonKayitlari",
                column: "Tarih");

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Tarih_AracId",
                table: "OperasyonKayitlari",
                columns: new[] { "Tarih", "AracId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Tarih_GuzergahId_AracId_Slot",
                table: "OperasyonKayitlari",
                columns: new[] { "Tarih", "GuzergahId", "AracId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperasyonKayitlari_Tarih_KurumId",
                table: "OperasyonKayitlari",
                columns: new[] { "Tarih", "KurumId" });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajAuditLogs_FirmaId",
                table: "PuantajAuditLogs",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajAuditLogs_HesapDonemiId_AksiyonTarihi",
                table: "PuantajAuditLogs",
                columns: new[] { "HesapDonemiId", "AksiyonTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajDetaylari_HesapDonemiId",
                table: "PuantajDetaylari",
                column: "HesapDonemiId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajDetaylari_OperasyonKaydiId_HesapDonemiId",
                table: "PuantajDetaylari",
                columns: new[] { "OperasyonKaydiId", "HesapDonemiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PuantajDetaylari_PuantajKayitId",
                table: "PuantajDetaylari",
                column: "PuantajKayitId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajFinansalKayitlar_Durum",
                table: "PuantajFinansalKayitlar",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajFinansalKayitlar_HesapDonemiId",
                table: "PuantajFinansalKayitlar",
                column: "HesapDonemiId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajFinansalKayitlar_PuantajKayitId_HesapDonemiId",
                table: "PuantajFinansalKayitlar",
                columns: new[] { "PuantajKayitId", "HesapDonemiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PuantajHesapDonemleri_Durum",
                table: "PuantajHesapDonemleri",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajHesapDonemleri_FirmaId_Yil_Ay_KurumId",
                table: "PuantajHesapDonemleri",
                columns: new[] { "FirmaId", "Yil", "Ay", "KurumId" });

            migrationBuilder.CreateIndex(
                name: "IX_PuantajHesapDonemleri_FirmaId_Yil_Ay_KurumId_Versiyon",
                table: "PuantajHesapDonemleri",
                columns: new[] { "FirmaId", "Yil", "Ay", "KurumId", "Versiyon" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PuantajHesapDonemleri_OncekiDonemId",
                table: "PuantajHesapDonemleri",
                column: "OncekiDonemId");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajJobExecutions_Durum",
                table: "PuantajJobExecutions",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajJobExecutions_FirmaId_Yil_Ay",
                table: "PuantajJobExecutions",
                columns: new[] { "FirmaId", "Yil", "Ay" },
                unique: true,
                filter: "\"Durum\" = 0");

            // Foreign keys — idempotent (may already exist)
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    ALTER TABLE ""PuantajKayitlar"" ADD CONSTRAINT ""FK_PuantajKayitlar_Firmalar_IsverenFirmaId""
                        FOREIGN KEY (""IsverenFirmaId"") REFERENCES ""Firmalar""(""Id"") ON DELETE SET NULL;
                EXCEPTION WHEN duplicate_object THEN END; $$;
                DO $$ BEGIN
                    ALTER TABLE ""PuantajKayitlar"" ADD CONSTRAINT ""FK_PuantajKayitlar_Kurumlar_KurumId""
                        FOREIGN KEY (""KurumId"") REFERENCES ""Kurumlar""(""Id"") ON DELETE SET NULL;
                EXCEPTION WHEN duplicate_object THEN END; $$;
                DO $$ BEGIN
                    ALTER TABLE ""PuantajKayitlar"" ADD CONSTRAINT ""FK_PuantajKayitlar_PuantajHesapDonemleri_HesapDonemiId""
                        FOREIGN KEY (""HesapDonemiId"") REFERENCES ""PuantajHesapDonemleri""(""Id"") ON DELETE SET NULL;
                EXCEPTION WHEN duplicate_object THEN END; $$;
                DO $$ BEGIN
                    ALTER TABLE ""PuantajKayitlar"" ADD CONSTRAINT ""FK_PuantajKayitlar_PuantajKayitlar_OncekiVersiyonId""
                        FOREIGN KEY (""OncekiVersiyonId"") REFERENCES ""PuantajKayitlar""(""Id"") ON DELETE SET NULL;
                EXCEPTION WHEN duplicate_object THEN END; $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PuantajKayitlar_Firmalar_IsverenFirmaId",
                table: "PuantajKayitlar");

            migrationBuilder.DropForeignKey(
                name: "FK_PuantajKayitlar_Kurumlar_KurumId",
                table: "PuantajKayitlar");

            migrationBuilder.DropForeignKey(
                name: "FK_PuantajKayitlar_PuantajHesapDonemleri_HesapDonemiId",
                table: "PuantajKayitlar");

            migrationBuilder.DropForeignKey(
                name: "FK_PuantajKayitlar_PuantajKayitlar_OncekiVersiyonId",
                table: "PuantajKayitlar");

            migrationBuilder.DropTable(
                name: "PuantajAuditLogs");

            migrationBuilder.DropTable(
                name: "PuantajDetaylari");

            migrationBuilder.DropTable(
                name: "PuantajFinansalKayitlar");

            migrationBuilder.DropTable(
                name: "PuantajJobExecutions");

            migrationBuilder.DropTable(
                name: "OperasyonKayitlari");

            migrationBuilder.DropTable(
                name: "PuantajHesapDonemleri");

            migrationBuilder.DropIndex(
                name: "IX_PuantajKayitlar_HesapDonemiId",
                table: "PuantajKayitlar");

            migrationBuilder.DropIndex(
                name: "IX_PuantajKayitlar_IsverenFirmaId",
                table: "PuantajKayitlar");

            migrationBuilder.DropIndex(
                name: "IX_PuantajKayitlar_KurumId",
                table: "PuantajKayitlar");

            migrationBuilder.DropIndex(
                name: "IX_PuantajKayitlar_OncekiVersiyonId",
                table: "PuantajKayitlar");

            migrationBuilder.DropIndex(
                name: "IX_PuantajKayitlar_Yil_Ay_GuzergahId_AracId_Slot",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "BelgeNo",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "FinansYonu",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "HesapDonemiId",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "IsverenFirmaId",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "KaynakTipi",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "KurumId",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "OncekiVersiyonId",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "SlotAdi",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "TransferDurum",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "Versiyon",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "GelenFaturaId",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "KalanFaturaTutar",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "KesilenFaturaNo",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "KesilenFaturaTarih",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "KesilenFaturaTutar",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "OdenenTutar",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "SonOdemeTarihi",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "ToplamOdeme",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "GuzergahSeferleri");

            migrationBuilder.CreateIndex(
                name: "IX_PuantajKayitlar_Yil_Ay_GuzergahId_AracId",
                table: "PuantajKayitlar",
                columns: new[] { "Yil", "Ay", "GuzergahId", "AracId" });
        }
    }
}

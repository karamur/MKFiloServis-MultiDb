using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantC5_AddIFirmaTenantToAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kural 4: FirmaId NOT NULL + backfill — onceki commit'lerde
            // IFirmaTenant eklenmis ama migration uretilmemis entity'ler toplu olarak burada.
            //
            // Eklenen entity'ler:
            //   ServisParcalar, ServisKayitlari, ProformaFaturaKalemler, PersonelMaaslari,
            //   PersonelIzinleri, PersonelIzinHaklari, GunlukPuantajlar, FaturaKalemleri,
            //   ChecklistKalemleri, AylikChecklistler, AracMasraflari, AracIslemler,
            //   AracEvraklari, AracEvrakDosyalari, AracBakimUyarilari
            // ALTER (mevcut kolon NOT NULL yapilan):
            //   AuditLoglar
            // Yeni nullable kolon:
            //   AktiviteLoglar.FirmaId, AktiviteLoglar.KullaniciId

            // ── Yeni FirmaId kolonlari (NOT NULL, once 0 default) ──────────────
            var yeniKolonluTablolar = new[]
            {
                "ServisParcalar", "ServisKayitlari", "ProformaFaturaKalemler",
                "PersonelMaaslari", "PersonelIzinleri", "PersonelIzinHaklari",
                "GunlukPuantajlar", "FaturaKalemleri", "ChecklistKalemleri",
                "AylikChecklistler", "AracMasraflari", "AracIslemler",
                "AracEvraklari", "AracEvrakDosyalari", "AracBakimUyarilari"
            };

            foreach (var table in yeniKolonluTablolar)
            {
                migrationBuilder.AddColumn<int>(
                    name: "FirmaId",
                    table: table,
                    type: "integer",
                    nullable: false,
                    defaultValue: 0);
            }

            // AuditLog: Mevcut nullable FirmaId → NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "AuditLoglar",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // AktiviteLog: Yeni nullable FirmaId + KullaniciId
            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "AktiviteLoglar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KullaniciId",
                table: "AktiviteLoglar",
                type: "integer",
                nullable: true);

            // ── Backfill: FirmaId=0 olan tum satirlari ilk gecerli firma ile doldur ──
            var backfillTablolari = new[]
            {
                "ServisParcalar", "ServisKayitlari", "ProformaFaturaKalemler",
                "PersonelMaaslari", "PersonelIzinleri", "PersonelIzinHaklari",
                "GunlukPuantajlar", "FaturaKalemleri", "ChecklistKalemleri",
                "AylikChecklistler", "AracMasraflari", "AracIslemler",
                "AracEvraklari", "AracEvrakDosyalari", "AracBakimUyarilari",
                "AuditLoglar"
            };

            migrationBuilder.Sql($@"
                DO $$ DECLARE first_firma_id integer;
                BEGIN
                    SELECT ""Id"" INTO first_firma_id FROM ""Firmalar"" WHERE NOT ""IsDeleted"" ORDER BY ""Id"" LIMIT 1;
                    IF first_firma_id IS NOT NULL THEN
                        {string.Join(" ", backfillTablolari.Select(t =>
                            $"UPDATE \"{t}\" SET \"FirmaId\" = first_firma_id WHERE \"FirmaId\" IS NULL OR \"FirmaId\" = 0;"))}
                    END IF;
                EXCEPTION WHEN others THEN NULL;
                END; $$;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ServisParcalar_FirmaId",
                table: "ServisParcalar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisKayitlari_FirmaId",
                table: "ServisKayitlari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaFaturaKalemler_FirmaId",
                table: "ProformaFaturaKalemler",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelMaaslari_FirmaId",
                table: "PersonelMaaslari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelIzinleri_FirmaId",
                table: "PersonelIzinleri",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelIzinHaklari_FirmaId",
                table: "PersonelIzinHaklari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_GunlukPuantajlar_FirmaId",
                table: "GunlukPuantajlar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_FaturaKalemleri_FirmaId",
                table: "FaturaKalemleri",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistKalemleri_FirmaId",
                table: "ChecklistKalemleri",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_AylikChecklistler_FirmaId",
                table: "AylikChecklistler",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_AracMasraflari_FirmaId",
                table: "AracMasraflari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_AracIslemler_FirmaId",
                table: "AracIslemler",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_AracEvraklari_FirmaId",
                table: "AracEvraklari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_AracEvrakDosyalari_FirmaId",
                table: "AracEvrakDosyalari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_AracBakimUyarilari_FirmaId",
                table: "AracBakimUyarilari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_AktiviteLoglar_FirmaId",
                table: "AktiviteLoglar",
                column: "FirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_AktiviteLoglar_Firmalar_FirmaId",
                table: "AktiviteLoglar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AracBakimUyarilari_Firmalar_FirmaId",
                table: "AracBakimUyarilari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AracEvrakDosyalari_Firmalar_FirmaId",
                table: "AracEvrakDosyalari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AracEvraklari_Firmalar_FirmaId",
                table: "AracEvraklari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AracIslemler_Firmalar_FirmaId",
                table: "AracIslemler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AracMasraflari_Firmalar_FirmaId",
                table: "AracMasraflari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AylikChecklistler_Firmalar_FirmaId",
                table: "AylikChecklistler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChecklistKalemleri_Firmalar_FirmaId",
                table: "ChecklistKalemleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FaturaKalemleri_Firmalar_FirmaId",
                table: "FaturaKalemleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GunlukPuantajlar_Firmalar_FirmaId",
                table: "GunlukPuantajlar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelIzinHaklari_Firmalar_FirmaId",
                table: "PersonelIzinHaklari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelIzinleri_Firmalar_FirmaId",
                table: "PersonelIzinleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelMaaslari_Firmalar_FirmaId",
                table: "PersonelMaaslari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProformaFaturaKalemler_Firmalar_FirmaId",
                table: "ProformaFaturaKalemler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServisKayitlari_Firmalar_FirmaId",
                table: "ServisKayitlari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServisParcalar_Firmalar_FirmaId",
                table: "ServisParcalar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AktiviteLoglar_Firmalar_FirmaId",
                table: "AktiviteLoglar");

            migrationBuilder.DropForeignKey(
                name: "FK_AracBakimUyarilari_Firmalar_FirmaId",
                table: "AracBakimUyarilari");

            migrationBuilder.DropForeignKey(
                name: "FK_AracEvrakDosyalari_Firmalar_FirmaId",
                table: "AracEvrakDosyalari");

            migrationBuilder.DropForeignKey(
                name: "FK_AracEvraklari_Firmalar_FirmaId",
                table: "AracEvraklari");

            migrationBuilder.DropForeignKey(
                name: "FK_AracIslemler_Firmalar_FirmaId",
                table: "AracIslemler");

            migrationBuilder.DropForeignKey(
                name: "FK_AracMasraflari_Firmalar_FirmaId",
                table: "AracMasraflari");

            migrationBuilder.DropForeignKey(
                name: "FK_AylikChecklistler_Firmalar_FirmaId",
                table: "AylikChecklistler");

            migrationBuilder.DropForeignKey(
                name: "FK_ChecklistKalemleri_Firmalar_FirmaId",
                table: "ChecklistKalemleri");

            migrationBuilder.DropForeignKey(
                name: "FK_FaturaKalemleri_Firmalar_FirmaId",
                table: "FaturaKalemleri");

            migrationBuilder.DropForeignKey(
                name: "FK_GunlukPuantajlar_Firmalar_FirmaId",
                table: "GunlukPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelIzinHaklari_Firmalar_FirmaId",
                table: "PersonelIzinHaklari");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelIzinleri_Firmalar_FirmaId",
                table: "PersonelIzinleri");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonelMaaslari_Firmalar_FirmaId",
                table: "PersonelMaaslari");

            migrationBuilder.DropForeignKey(
                name: "FK_ProformaFaturaKalemler_Firmalar_FirmaId",
                table: "ProformaFaturaKalemler");

            migrationBuilder.DropForeignKey(
                name: "FK_ServisKayitlari_Firmalar_FirmaId",
                table: "ServisKayitlari");

            migrationBuilder.DropForeignKey(
                name: "FK_ServisParcalar_Firmalar_FirmaId",
                table: "ServisParcalar");

            migrationBuilder.DropIndex(
                name: "IX_ServisParcalar_FirmaId",
                table: "ServisParcalar");

            migrationBuilder.DropIndex(
                name: "IX_ServisKayitlari_FirmaId",
                table: "ServisKayitlari");

            migrationBuilder.DropIndex(
                name: "IX_ProformaFaturaKalemler_FirmaId",
                table: "ProformaFaturaKalemler");

            migrationBuilder.DropIndex(
                name: "IX_PersonelMaaslari_FirmaId",
                table: "PersonelMaaslari");

            migrationBuilder.DropIndex(
                name: "IX_PersonelIzinleri_FirmaId",
                table: "PersonelIzinleri");

            migrationBuilder.DropIndex(
                name: "IX_PersonelIzinHaklari_FirmaId",
                table: "PersonelIzinHaklari");

            migrationBuilder.DropIndex(
                name: "IX_GunlukPuantajlar_FirmaId",
                table: "GunlukPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_FaturaKalemleri_FirmaId",
                table: "FaturaKalemleri");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistKalemleri_FirmaId",
                table: "ChecklistKalemleri");

            migrationBuilder.DropIndex(
                name: "IX_AylikChecklistler_FirmaId",
                table: "AylikChecklistler");

            migrationBuilder.DropIndex(
                name: "IX_AracMasraflari_FirmaId",
                table: "AracMasraflari");

            migrationBuilder.DropIndex(
                name: "IX_AracIslemler_FirmaId",
                table: "AracIslemler");

            migrationBuilder.DropIndex(
                name: "IX_AracEvraklari_FirmaId",
                table: "AracEvraklari");

            migrationBuilder.DropIndex(
                name: "IX_AracEvrakDosyalari_FirmaId",
                table: "AracEvrakDosyalari");

            migrationBuilder.DropIndex(
                name: "IX_AracBakimUyarilari_FirmaId",
                table: "AracBakimUyarilari");

            migrationBuilder.DropIndex(
                name: "IX_AktiviteLoglar_FirmaId",
                table: "AktiviteLoglar");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "ServisParcalar");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "ServisKayitlari");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "ProformaFaturaKalemler");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "PersonelMaaslari");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "PersonelIzinleri");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "PersonelIzinHaklari");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "GunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "ChecklistKalemleri");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "AylikChecklistler");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "AracMasraflari");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "AracIslemler");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "AracEvraklari");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "AracEvrakDosyalari");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "AracBakimUyarilari");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "AktiviteLoglar");

            migrationBuilder.DropColumn(
                name: "KullaniciId",
                table: "AktiviteLoglar");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "AuditLoglar",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}

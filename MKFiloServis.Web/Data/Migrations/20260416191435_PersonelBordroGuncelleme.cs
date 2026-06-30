using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonelBordroGuncelleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Faturalar_FaturaNo\";");

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Faturalar_FirmaId\";");

            migrationBuilder.AddColumn<decimal>(
                name: "AileYardimi",
                table: "Personeller",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BESKesintisi",
                table: "Personeller",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BireyselEmeklilik",
                table: "Personeller",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DigerOzelKesinti",
                table: "Personeller",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HayatSigortasi",
                table: "Personeller",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IcraKesintisi",
                table: "Personeller",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SendikaKesintisi",
                table: "Personeller",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "YemekYardimi",
                table: "Personeller",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "YolYardimi",
                table: "Personeller",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "HesapKodu",
                table: "MuhasebeHesaplari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<decimal>(
                name: "FazlaMesaiSaat",
                table: "GunlukPuantajlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            if (ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(@"ALTER TABLE ""GunlukPuantajlar"" ADD COLUMN IF NOT EXISTS ""CalismaSaati"" numeric NOT NULL DEFAULT 0.0;");
                migrationBuilder.Sql(@"ALTER TABLE ""GunlukPuantajlar"" ADD COLUMN IF NOT EXISTS ""Durum"" integer NOT NULL DEFAULT 0;");
                migrationBuilder.Sql(@"ALTER TABLE ""GunlukPuantajlar"" ADD COLUMN IF NOT EXISTS ""Gun"" integer NOT NULL DEFAULT 0;");
            }
            else
            {
                migrationBuilder.AddColumn<decimal>(
                    name: "CalismaSaati",
                    table: "GunlukPuantajlar",
                    type: "numeric",
                    nullable: false,
                    defaultValue: 0m);

                migrationBuilder.AddColumn<int>(
                    name: "Durum",
                    table: "GunlukPuantajlar",
                    type: "integer",
                    nullable: false,
                    defaultValue: 0);

                migrationBuilder.AddColumn<int>(
                    name: "Gun",
                    table: "GunlukPuantajlar",
                    type: "integer",
                    nullable: false,
                    defaultValue: 0);
            }

            migrationBuilder.AddColumn<bool>(
                name: "KalanSonrakiDonemeAktarilsin",
                table: "BudgetOdemeler",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "KismiOdemeMi",
                table: "BudgetOdemeler",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OncekiDonemOdemeId",
                table: "BudgetOdemeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SonrakiDonemOdemeId",
                table: "BudgetOdemeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ToplamKismiOdenen",
                table: "BudgetOdemeler",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AileYardimi",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BESKesintisi",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BireyselEmeklilik",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DigerOzelKesinti",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HayatSigortasi",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IcraKesintisi",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Ikramiye",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IssizlikIsciPrim",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IssizlikIsverenPrim",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KumulatifVergiMatrahi",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SendikaKesintisi",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SgkIsciPrim",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SgkIsverenPrim",
                table: "BordroDetaylar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UygulananVergiDilimi",
                table: "BordroDetaylar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AgiTutari",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "AgiUygulaniyor",
                table: "BordroAyarlar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ArgeGelirVergisiStopajDestekOrani",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ArgeGelirVergisiStopajDestekVarMi",
                table: "BordroAyarlar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "GelirVergisiDilim1Oran",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GelirVergisiDilim1Sinir",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GelirVergisiDilim2Oran",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GelirVergisiDilim2Sinir",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GelirVergisiDilim3Oran",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GelirVergisiDilim3Sinir",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GelirVergisiDilim4Oran",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GelirVergisiDilim4Sinir",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GelirVergisiDilim5Oran",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IssizlikIsverenPayiOrani",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Sgk5PuanIndirimVarMi",
                table: "BordroAyarlar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SgkIsverenPayiOrani",
                table: "BordroAyarlar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PersonelCebindenId",
                table: "BankaKasaHareketleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonelOdemeHesapId",
                table: "BankaKasaHareketleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PersonelOdemeTarihi",
                table: "BankaKasaHareketleri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PersoneleOdendi",
                table: "BankaKasaHareketleri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BankaHesapId",
                table: "AracMasraflari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OdemeKaynak",
                table: "AracMasraflari",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PersonelCebindenId",
                table: "AracMasraflari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PersonelOdemeTarihi",
                table: "AracMasraflari",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PersoneleOdendi",
                table: "AracMasraflari",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_FirmaId_FaturaYonu_FaturaNo",
                table: "Faturalar",
                columns: new[] { "FirmaId", "FaturaYonu", "FaturaNo" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetOdemeler_OncekiDonemOdemeId",
                table: "BudgetOdemeler",
                column: "OncekiDonemOdemeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetOdemeler_SonrakiDonemOdemeId",
                table: "BudgetOdemeler",
                column: "SonrakiDonemOdemeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_PersonelCebindenId_PersoneleOdendi",
                table: "BankaKasaHareketleri",
                columns: new[] { "PersonelCebindenId", "PersoneleOdendi" });

            migrationBuilder.CreateIndex(
                name: "IX_AracMasraflari_BankaHesapId",
                table: "AracMasraflari",
                column: "BankaHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_AracMasraflari_PersonelCebindenId_PersoneleOdendi",
                table: "AracMasraflari",
                columns: new[] { "PersonelCebindenId", "PersoneleOdendi" });

            migrationBuilder.AddForeignKey(
                name: "FK_AracMasraflari_BankaHesaplari_BankaHesapId",
                table: "AracMasraflari",
                column: "BankaHesapId",
                principalTable: "BankaHesaplari",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AracMasraflari_Personeller_PersonelCebindenId",
                table: "AracMasraflari",
                column: "PersonelCebindenId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BankaKasaHareketleri_Personeller_PersonelCebindenId",
                table: "BankaKasaHareketleri",
                column: "PersonelCebindenId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetOdemeler_BudgetOdemeler_OncekiDonemOdemeId",
                table: "BudgetOdemeler",
                column: "OncekiDonemOdemeId",
                principalTable: "BudgetOdemeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetOdemeler_BudgetOdemeler_SonrakiDonemOdemeId",
                table: "BudgetOdemeler",
                column: "SonrakiDonemOdemeId",
                principalTable: "BudgetOdemeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AracMasraflari_BankaHesaplari_BankaHesapId",
                table: "AracMasraflari");

            migrationBuilder.DropForeignKey(
                name: "FK_AracMasraflari_Personeller_PersonelCebindenId",
                table: "AracMasraflari");

            migrationBuilder.DropForeignKey(
                name: "FK_BankaKasaHareketleri_Personeller_PersonelCebindenId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetOdemeler_BudgetOdemeler_OncekiDonemOdemeId",
                table: "BudgetOdemeler");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetOdemeler_BudgetOdemeler_SonrakiDonemOdemeId",
                table: "BudgetOdemeler");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_FirmaId_FaturaYonu_FaturaNo",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_BudgetOdemeler_OncekiDonemOdemeId",
                table: "BudgetOdemeler");

            migrationBuilder.DropIndex(
                name: "IX_BudgetOdemeler_SonrakiDonemOdemeId",
                table: "BudgetOdemeler");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_PersonelCebindenId_PersoneleOdendi",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_AracMasraflari_BankaHesapId",
                table: "AracMasraflari");

            migrationBuilder.DropIndex(
                name: "IX_AracMasraflari_PersonelCebindenId_PersoneleOdendi",
                table: "AracMasraflari");

            migrationBuilder.DropColumn(
                name: "AileYardimi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "BESKesintisi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "BireyselEmeklilik",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "DigerOzelKesinti",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "HayatSigortasi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "IcraKesintisi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "SendikaKesintisi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "YemekYardimi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "YolYardimi",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "CalismaSaati",
                table: "GunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "Durum",
                table: "GunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "Gun",
                table: "GunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "KalanSonrakiDonemeAktarilsin",
                table: "BudgetOdemeler");

            migrationBuilder.DropColumn(
                name: "KismiOdemeMi",
                table: "BudgetOdemeler");

            migrationBuilder.DropColumn(
                name: "OncekiDonemOdemeId",
                table: "BudgetOdemeler");

            migrationBuilder.DropColumn(
                name: "SonrakiDonemOdemeId",
                table: "BudgetOdemeler");

            migrationBuilder.DropColumn(
                name: "ToplamKismiOdenen",
                table: "BudgetOdemeler");

            migrationBuilder.DropColumn(
                name: "AileYardimi",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "BESKesintisi",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "BireyselEmeklilik",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "DigerOzelKesinti",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "HayatSigortasi",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "IcraKesintisi",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "Ikramiye",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "IssizlikIsciPrim",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "IssizlikIsverenPrim",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "KumulatifVergiMatrahi",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "SendikaKesintisi",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "SgkIsciPrim",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "SgkIsverenPrim",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "UygulananVergiDilimi",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "AgiTutari",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "AgiUygulaniyor",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "ArgeGelirVergisiStopajDestekOrani",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "ArgeGelirVergisiStopajDestekVarMi",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "GelirVergisiDilim1Oran",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "GelirVergisiDilim1Sinir",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "GelirVergisiDilim2Oran",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "GelirVergisiDilim2Sinir",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "GelirVergisiDilim3Oran",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "GelirVergisiDilim3Sinir",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "GelirVergisiDilim4Oran",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "GelirVergisiDilim4Sinir",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "GelirVergisiDilim5Oran",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "IssizlikIsverenPayiOrani",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "Sgk5PuanIndirimVarMi",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "SgkIsverenPayiOrani",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "PersonelCebindenId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "PersonelOdemeHesapId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "PersonelOdemeTarihi",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "PersoneleOdendi",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "BankaHesapId",
                table: "AracMasraflari");

            migrationBuilder.DropColumn(
                name: "OdemeKaynak",
                table: "AracMasraflari");

            migrationBuilder.DropColumn(
                name: "PersonelCebindenId",
                table: "AracMasraflari");

            migrationBuilder.DropColumn(
                name: "PersonelOdemeTarihi",
                table: "AracMasraflari");

            migrationBuilder.DropColumn(
                name: "PersoneleOdendi",
                table: "AracMasraflari");

            migrationBuilder.AlterColumn<string>(
                name: "HesapKodu",
                table: "MuhasebeHesaplari",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "FazlaMesaiSaat",
                table: "GunlukPuantajlar",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_FaturaNo",
                table: "Faturalar",
                column: "FaturaNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_FirmaId",
                table: "Faturalar",
                column: "FirmaId");
        }
    }
}






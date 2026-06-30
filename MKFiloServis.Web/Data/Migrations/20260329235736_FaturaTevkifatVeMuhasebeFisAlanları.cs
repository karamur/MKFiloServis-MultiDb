using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class FaturaTevkifatVeMuhasebeFisAlanları : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlisGiderHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "FaturaOtomatikMuhasebeFisi",
                table: "MuhasebeAyarlari",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HesaplananKdvHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IndirilecekKdvHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SatisGelirHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TevkifatAlacakHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TevkifatKdvHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "XmlImportOtomatikCariOlustur",
                table: "MuhasebeAyarlari",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "XmlImportOtomatikHesapKoduOlustur",
                table: "MuhasebeAyarlari",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MuhasebeFisId",
                table: "Faturalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MuhasebeFisiOlusturuldu",
                table: "Faturalar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TevkifatKodu",
                table: "Faturalar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TevkifatOrani",
                table: "Faturalar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TevkifatTutar",
                table: "Faturalar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "TevkifatliMi",
                table: "Faturalar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "IskontoOrani",
                table: "FaturaKalemleri",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IskontoTutar",
                table: "FaturaKalemleri",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MuhasebeHesapId",
                table: "FaturaKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TevkifatOrani",
                table: "FaturaKalemleri",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TevkifatTutar",
                table: "FaturaKalemleri",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UrunKodu",
                table: "FaturaKalemleri",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaturaKalemleri_MuhasebeHesapId",
                table: "FaturaKalemleri",
                column: "MuhasebeHesapId");

            migrationBuilder.AddForeignKey(
                name: "FK_FaturaKalemleri_MuhasebeHesaplari_MuhasebeHesapId",
                table: "FaturaKalemleri",
                column: "MuhasebeHesapId",
                principalTable: "MuhasebeHesaplari",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaturaKalemleri_MuhasebeHesaplari_MuhasebeHesapId",
                table: "FaturaKalemleri");

            migrationBuilder.DropIndex(
                name: "IX_FaturaKalemleri_MuhasebeHesapId",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "AlisGiderHesabi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "FaturaOtomatikMuhasebeFisi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "HesaplananKdvHesabi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "IndirilecekKdvHesabi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "SatisGelirHesabi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "TevkifatAlacakHesabi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "TevkifatKdvHesabi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "XmlImportOtomatikCariOlustur",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "XmlImportOtomatikHesapKoduOlustur",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "MuhasebeFisId",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "MuhasebeFisiOlusturuldu",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "TevkifatKodu",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "TevkifatOrani",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "TevkifatTutar",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "TevkifatliMi",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "IskontoOrani",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "IskontoTutar",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "MuhasebeHesapId",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "TevkifatOrani",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "TevkifatTutar",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "UrunKodu",
                table: "FaturaKalemleri");
        }
    }
}



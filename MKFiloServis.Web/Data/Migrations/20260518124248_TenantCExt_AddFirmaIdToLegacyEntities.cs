using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantCExt_AddFirmaIdToLegacyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // K9 GÜVENLİK: Eski NOT NULL FirmaId default=0 ile dolmuş satırları NULL'a çevir
            // (sonradan TenantFirmaIdBackfillMigrationHelper startup'ta varsayılan firma ile doldurur).
            // Restrict FK eklenmeden ÖNCE çalışmalı; aksi halde Firmalar.Id=0 yoksa FK violation.
            migrationBuilder.Sql(@"
                UPDATE ""Araclar""              SET ""FirmaId"" = NULL WHERE ""FirmaId"" = 0;
                UPDATE ""BankaHesaplari""       SET ""FirmaId"" = NULL WHERE ""FirmaId"" = 0;
                UPDATE ""BankaKasaHareketleri"" SET ""FirmaId"" = NULL WHERE ""FirmaId"" = 0;
                UPDATE ""Guzergahlar""          SET ""FirmaId"" = NULL WHERE ""FirmaId"" = 0;
                UPDATE ""Personeller""          SET ""FirmaId"" = NULL WHERE ""FirmaId"" = 0;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Araclar_Firmalar_FirmaId",
                table: "Araclar");

            migrationBuilder.DropForeignKey(
                name: "FK_BankaHesaplari_Firmalar_FirmaId",
                table: "BankaHesaplari");

            migrationBuilder.DropForeignKey(
                name: "FK_BankaKasaHareketleri_Firmalar_FirmaId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropForeignKey(
                name: "FK_Guzergahlar_Firmalar_FirmaId",
                table: "Guzergahlar");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Personeller",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Guzergahlar",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "BankaKasaHareketleri",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "BankaHesaplari",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Araclar",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Araclar_Firmalar_FirmaId",
                table: "Araclar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BankaHesaplari_Firmalar_FirmaId",
                table: "BankaHesaplari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BankaKasaHareketleri_Firmalar_FirmaId",
                table: "BankaKasaHareketleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Firmalar_FirmaId",
                table: "Guzergahlar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Araclar_Firmalar_FirmaId",
                table: "Araclar");

            migrationBuilder.DropForeignKey(
                name: "FK_BankaHesaplari_Firmalar_FirmaId",
                table: "BankaHesaplari");

            migrationBuilder.DropForeignKey(
                name: "FK_BankaKasaHareketleri_Firmalar_FirmaId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropForeignKey(
                name: "FK_Guzergahlar_Firmalar_FirmaId",
                table: "Guzergahlar");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Personeller",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Guzergahlar",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "BankaKasaHareketleri",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "BankaHesaplari",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Araclar",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Araclar_Firmalar_FirmaId",
                table: "Araclar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BankaHesaplari_Firmalar_FirmaId",
                table: "BankaHesaplari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BankaKasaHareketleri_Firmalar_FirmaId",
                table: "BankaKasaHareketleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Firmalar_FirmaId",
                table: "Guzergahlar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}



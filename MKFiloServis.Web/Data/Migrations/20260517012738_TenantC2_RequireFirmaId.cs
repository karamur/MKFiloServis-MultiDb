using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantC2_RequireFirmaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropForeignKey(
                name: "FK_Kurumlar_Firmalar_FirmaId",
                table: "Kurumlar");

            // K9 backfill: NULL/0 FirmaId satırlarını varsayılan firma ile doldur.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE def_firma_id int;
                BEGIN
                    SELECT COALESCE(
                        (SELECT ""Id"" FROM ""Firmalar"" WHERE ""VarsayilanFirma"" = true AND ""Aktif"" = true ORDER BY ""Id"" LIMIT 1),
                        (SELECT ""Id"" FROM ""Firmalar"" WHERE ""Aktif"" = true ORDER BY ""Id"" LIMIT 1)
                    ) INTO def_firma_id;
                    IF def_firma_id IS NOT NULL THEN
                        UPDATE ""Personeller""           SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                        UPDATE ""Kurumlar""              SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                        UPDATE ""Guzergahlar""           SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                        UPDATE ""Cariler""               SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                        UPDATE ""BankaKasaHareketleri""  SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                        UPDATE ""BankaHesaplari""        SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                        UPDATE ""Araclar""               SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                    END IF;
                END $$;
            ");

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
                table: "Kurumlar",
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
                table: "Cariler",
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

            migrationBuilder.AddForeignKey(
                name: "FK_Kurumlar_Firmalar_FirmaId",
                table: "Kurumlar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.DropForeignKey(
                name: "FK_Kurumlar_Firmalar_FirmaId",
                table: "Kurumlar");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Personeller",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Kurumlar",
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
                table: "Cariler",
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
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BankaHesaplari_Firmalar_FirmaId",
                table: "BankaHesaplari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BankaKasaHareketleri_Firmalar_FirmaId",
                table: "BankaKasaHareketleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guzergahlar_Firmalar_FirmaId",
                table: "Guzergahlar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Kurumlar_Firmalar_FirmaId",
                table: "Kurumlar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");
        }
    }
}



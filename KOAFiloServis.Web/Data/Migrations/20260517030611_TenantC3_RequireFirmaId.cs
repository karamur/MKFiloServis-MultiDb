using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantC3_RequireFirmaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_Firmalar_FirmaId",
                table: "Faturalar");

            migrationBuilder.DropForeignKey(
                name: "FK_MasrafKalemleri_Firmalar_FirmaId",
                table: "MasrafKalemleri");

            migrationBuilder.DropForeignKey(
                name: "FK_ServisCalismalari_Firmalar_FirmaId",
                table: "ServisCalismalari");

            migrationBuilder.DropForeignKey(
                name: "FK_StokHareketler_Firmalar_FirmaId",
                table: "StokHareketler");

            migrationBuilder.DropForeignKey(
                name: "FK_StokKartlari_Firmalar_FirmaId",
                table: "StokKartlari");

            migrationBuilder.DropForeignKey(
                name: "FK_StokKategoriler_Firmalar_FirmaId",
                table: "StokKategoriler");

            // K9 backfill (C3-a tabloları): startup helper'ı atlanırsa diye migration kendi başına da güvenli olsun.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE def_firma_id int;
                BEGIN
                    SELECT COALESCE(
                        (SELECT ""Id"" FROM ""Firmalar"" WHERE ""VarsayilanFirma"" = true AND ""Aktif"" = true ORDER BY ""Id"" LIMIT 1),
                        (SELECT ""Id"" FROM ""Firmalar"" WHERE ""Aktif"" = true ORDER BY ""Id"" LIMIT 1)
                    ) INTO def_firma_id;
                    IF def_firma_id IS NOT NULL THEN
                        UPDATE ""StokKartlari""        SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL;
                        UPDATE ""StokKategoriler""     SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL;
                        UPDATE ""StokHareketler""      SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL;
                        UPDATE ""MasrafKalemleri""     SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL;
                        UPDATE ""Faturalar""           SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL;
                        UPDATE ""ServisCalismalari""   SET ""FirmaId"" = def_firma_id WHERE ""FirmaId"" IS NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "StokKategoriler",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "StokKartlari",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "StokHareketler",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "ServisCalismalari",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "MasrafKalemleri",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "Hakedisler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "HakedisDetaylari",
                type: "integer",
                nullable: true);

            // K9 backfill: varsayılan firma ile doldur (yoksa en eski aktif firmayı kullan).
            migrationBuilder.Sql(@"
                UPDATE ""Hakedisler""
                   SET ""FirmaId"" = COALESCE(
                       (SELECT ""Id"" FROM ""Firmalar"" WHERE ""VarsayilanFirma"" = true AND ""Aktif"" = true ORDER BY ""Id"" LIMIT 1),
                       (SELECT ""Id"" FROM ""Firmalar"" WHERE ""Aktif"" = true ORDER BY ""Id"" LIMIT 1)
                   )
                 WHERE ""FirmaId"" IS NULL;

                UPDATE ""HakedisDetaylari""
                   SET ""FirmaId"" = COALESCE(
                       (SELECT ""Id"" FROM ""Firmalar"" WHERE ""VarsayilanFirma"" = true AND ""Aktif"" = true ORDER BY ""Id"" LIMIT 1),
                       (SELECT ""Id"" FROM ""Firmalar"" WHERE ""Aktif"" = true ORDER BY ""Id"" LIMIT 1)
                   )
                 WHERE ""FirmaId"" IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Hakedisler",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "HakedisDetaylari",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Faturalar",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hakedisler_FirmaId",
                table: "Hakedisler",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisDetaylari_FirmaId",
                table: "HakedisDetaylari",
                column: "FirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Firmalar_FirmaId",
                table: "Faturalar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HakedisDetaylari_Firmalar_FirmaId",
                table: "HakedisDetaylari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Hakedisler_Firmalar_FirmaId",
                table: "Hakedisler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MasrafKalemleri_Firmalar_FirmaId",
                table: "MasrafKalemleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServisCalismalari_Firmalar_FirmaId",
                table: "ServisCalismalari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StokHareketler_Firmalar_FirmaId",
                table: "StokHareketler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StokKartlari_Firmalar_FirmaId",
                table: "StokKartlari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StokKategoriler_Firmalar_FirmaId",
                table: "StokKategoriler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_Firmalar_FirmaId",
                table: "Faturalar");

            migrationBuilder.DropForeignKey(
                name: "FK_HakedisDetaylari_Firmalar_FirmaId",
                table: "HakedisDetaylari");

            migrationBuilder.DropForeignKey(
                name: "FK_Hakedisler_Firmalar_FirmaId",
                table: "Hakedisler");

            migrationBuilder.DropForeignKey(
                name: "FK_MasrafKalemleri_Firmalar_FirmaId",
                table: "MasrafKalemleri");

            migrationBuilder.DropForeignKey(
                name: "FK_ServisCalismalari_Firmalar_FirmaId",
                table: "ServisCalismalari");

            migrationBuilder.DropForeignKey(
                name: "FK_StokHareketler_Firmalar_FirmaId",
                table: "StokHareketler");

            migrationBuilder.DropForeignKey(
                name: "FK_StokKartlari_Firmalar_FirmaId",
                table: "StokKartlari");

            migrationBuilder.DropForeignKey(
                name: "FK_StokKategoriler_Firmalar_FirmaId",
                table: "StokKategoriler");

            migrationBuilder.DropIndex(
                name: "IX_Hakedisler_FirmaId",
                table: "Hakedisler");

            migrationBuilder.DropIndex(
                name: "IX_HakedisDetaylari_FirmaId",
                table: "HakedisDetaylari");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "Hakedisler");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "HakedisDetaylari");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "StokKategoriler",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "StokKartlari",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "StokHareketler",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "ServisCalismalari",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "MasrafKalemleri",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "Faturalar",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Firmalar_FirmaId",
                table: "Faturalar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MasrafKalemleri_Firmalar_FirmaId",
                table: "MasrafKalemleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServisCalismalari_Firmalar_FirmaId",
                table: "ServisCalismalari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StokHareketler_Firmalar_FirmaId",
                table: "StokHareketler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StokKartlari_Firmalar_FirmaId",
                table: "StokKartlari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StokKategoriler_Firmalar_FirmaId",
                table: "StokKategoriler",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");
        }
    }
}

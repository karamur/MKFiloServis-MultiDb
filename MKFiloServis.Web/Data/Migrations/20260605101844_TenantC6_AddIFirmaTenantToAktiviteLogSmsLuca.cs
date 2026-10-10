using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantC6_AddIFirmaTenantToAktiviteLogSmsLuca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kural 4: AktiviteLog + SmsAyar + SmsSablon → FirmaId NOT NULL

            // Once mevcut FK'lari kaldir (nullable → non-nullable degisimi icin)
            migrationBuilder.DropForeignKey(
                name: "FK_AktiviteLoglar_Firmalar_FirmaId",
                table: "AktiviteLoglar");

            migrationBuilder.DropForeignKey(
                name: "FK_SmsAyarlari_Firmalar_FirmaId",
                table: "SmsAyarlari");

            migrationBuilder.DropForeignKey(
                name: "FK_SmsSablonlari_Firmalar_FirmaId",
                table: "SmsSablonlari");

            // FirmaId: nullable → NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "SmsSablonlari",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "SmsAyarlari",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "AktiviteLoglar",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // Backfill: NULL/0 FirmaId → ilk gecerli firma
            var backfillTablolari = new[] { "SmsSablonlari", "SmsAyarlari", "AktiviteLoglar" };
            foreach (var table in backfillTablolari)
            {
                migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + $@"
                    UPDATE ""{table}""
                    SET ""FirmaId"" = (SELECT ""Id"" FROM ""Firmalar"" WHERE NOT ""IsDeleted"" ORDER BY ""Id"" LIMIT 1)
                    WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                ");
            }

            // FK'lari CASCADE ile geri ekle
            migrationBuilder.AddForeignKey(
                name: "FK_AktiviteLoglar_Firmalar_FirmaId",
                table: "AktiviteLoglar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SmsAyarlari_Firmalar_FirmaId",
                table: "SmsAyarlari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SmsSablonlari_Firmalar_FirmaId",
                table: "SmsSablonlari",
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
                name: "FK_SmsAyarlari_Firmalar_FirmaId",
                table: "SmsAyarlari");

            migrationBuilder.DropForeignKey(
                name: "FK_SmsSablonlari_Firmalar_FirmaId",
                table: "SmsSablonlari");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "SmsSablonlari",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "SmsAyarlari",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "AktiviteLoglar",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_AktiviteLoglar_Firmalar_FirmaId",
                table: "AktiviteLoglar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SmsAyarlari_Firmalar_FirmaId",
                table: "SmsAyarlari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SmsSablonlari_Firmalar_FirmaId",
                table: "SmsSablonlari",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id");
        }
    }
}



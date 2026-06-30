using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class FaturaKalemTipiVeAracIliskisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TevkifatTutar",
                table: "Faturalar",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TevkifatOrani",
                table: "Faturalar",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "TevkifatKodu",
                table: "Faturalar",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AracFaturasi",
                table: "Faturalar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AracId",
                table: "Faturalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UrunKodu",
                table: "FaturaKalemleri",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TevkifatTutar",
                table: "FaturaKalemleri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TevkifatOrani",
                table: "FaturaKalemleri",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "IskontoTutar",
                table: "FaturaKalemleri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "IskontoOrani",
                table: "FaturaKalemleri",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "AltTipi",
                table: "FaturaKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AracId",
                table: "FaturaKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DemirbasId",
                table: "FaturaKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KalemTipi",
                table: "FaturaKalemleri",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_AracId",
                table: "Faturalar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_FaturaKalemleri_AracId",
                table: "FaturaKalemleri",
                column: "AracId");

            migrationBuilder.AddForeignKey(
                name: "FK_FaturaKalemleri_Araclar_AracId",
                table: "FaturaKalemleri",
                column: "AracId",
                principalTable: "Araclar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Araclar_AracId",
                table: "Faturalar",
                column: "AracId",
                principalTable: "Araclar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaturaKalemleri_Araclar_AracId",
                table: "FaturaKalemleri");

            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_Araclar_AracId",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_AracId",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_FaturaKalemleri_AracId",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "AracFaturasi",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "AracId",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "AltTipi",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "AracId",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "DemirbasId",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "KalemTipi",
                table: "FaturaKalemleri");

            migrationBuilder.AlterColumn<decimal>(
                name: "TevkifatTutar",
                table: "Faturalar",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TevkifatOrani",
                table: "Faturalar",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "TevkifatKodu",
                table: "Faturalar",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UrunKodu",
                table: "FaturaKalemleri",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TevkifatTutar",
                table: "FaturaKalemleri",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TevkifatOrani",
                table: "FaturaKalemleri",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "IskontoTutar",
                table: "FaturaKalemleri",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "IskontoOrani",
                table: "FaturaKalemleri",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }
    }
}



using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LastikStokBireyselTakip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adet",
                table: "LastikStoklar");

            migrationBuilder.AlterColumn<int>(
                name: "DepoId",
                table: "LastikStoklar",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "LastikStoklar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AracId",
                table: "LastikStoklar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "YedekMi",
                table: "LastikStoklar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_LastikStoklar_AracId",
                table: "LastikStoklar",
                column: "AracId");

            migrationBuilder.AddForeignKey(
                name: "FK_LastikStoklar_Araclar_AracId",
                table: "LastikStoklar",
                column: "AracId",
                principalTable: "Araclar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LastikStoklar_Araclar_AracId",
                table: "LastikStoklar");

            migrationBuilder.DropIndex(
                name: "IX_LastikStoklar_AracId",
                table: "LastikStoklar");

            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "LastikStoklar");

            migrationBuilder.DropColumn(
                name: "AracId",
                table: "LastikStoklar");

            migrationBuilder.DropColumn(
                name: "YedekMi",
                table: "LastikStoklar");

            migrationBuilder.AlterColumn<int>(
                name: "DepoId",
                table: "LastikStoklar",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Adet",
                table: "LastikStoklar",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}



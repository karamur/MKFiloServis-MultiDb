using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLastikKaynakAracId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KaynakAracId",
                table: "LastikStoklar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LastikStoklar_KaynakAracId",
                table: "LastikStoklar",
                column: "KaynakAracId");

            migrationBuilder.AddForeignKey(
                name: "FK_LastikStoklar_Araclar_KaynakAracId",
                table: "LastikStoklar",
                column: "KaynakAracId",
                principalTable: "Araclar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LastikStoklar_Araclar_KaynakAracId",
                table: "LastikStoklar");

            migrationBuilder.DropIndex(
                name: "IX_LastikStoklar_KaynakAracId",
                table: "LastikStoklar");

            migrationBuilder.DropColumn(
                name: "KaynakAracId",
                table: "LastikStoklar");
        }
    }
}



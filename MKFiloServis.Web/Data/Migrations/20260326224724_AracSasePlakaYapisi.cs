using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AracSasePlakaYapisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Araclar_Plaka",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "Plaka",
                table: "Araclar");

            migrationBuilder.AlterColumn<string>(
                name: "SaseNo",
                table: "Araclar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Renk",
                table: "Araclar",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MotorNo",
                table: "Araclar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AktifPlaka",
                table: "Araclar",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SatisAciklamasi",
                table: "Araclar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SatisFiyati",
                table: "Araclar",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SatisaAcik",
                table: "Araclar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SatisaAcilmaTarihi",
                table: "Araclar",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AracPlakalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    Plaka = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    GirisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CikisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IslemTipi = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IslemTutari = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CariId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracPlakalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AracPlakalar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AracPlakalar_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Araclar_SaseNo",
                table: "Araclar",
                column: "SaseNo",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AracPlakalar_AracId",
                table: "AracPlakalar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_AracPlakalar_CariId",
                table: "AracPlakalar",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_AracPlakalar_Plaka_CikisTarihi",
                table: "AracPlakalar",
                columns: new[] { "Plaka", "CikisTarihi" },
                filter: "\"CikisTarihi\" IS NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AracPlakalar");

            migrationBuilder.DropIndex(
                name: "IX_Araclar_SaseNo",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "AktifPlaka",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "SatisAciklamasi",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "SatisFiyati",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "SatisaAcik",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "SatisaAcilmaTarihi",
                table: "Araclar");

            migrationBuilder.AlterColumn<string>(
                name: "SaseNo",
                table: "Araclar",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Renk",
                table: "Araclar",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MotorNo",
                table: "Araclar",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plaka",
                table: "Araclar",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Araclar_Plaka",
                table: "Araclar",
                column: "Plaka",
                unique: true);
        }
    }
}



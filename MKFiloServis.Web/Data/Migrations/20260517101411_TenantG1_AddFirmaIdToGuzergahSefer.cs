using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantG1_AddFirmaIdToGuzergahSefer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // K9: nullable ekle → varsayılan firmaya backfill (parent Guzergah'tan miras) → NOT NULL'a al.
            // Hakedis/HakedisDetay'da kullanılan tek migration K9 deseni ile aynı.
            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "GuzergahSeferleri",
                type: "integer",
                nullable: true);

            // Backfill from the parent route; use scalar subqueries supported by both providers.
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql);
            }

            migrationBuilder.Sql(@"
                UPDATE ""GuzergahSeferleri""
                   SET ""FirmaId"" = (SELECT g.""FirmaId"" FROM ""Guzergahlar"" g WHERE g.""Id"" = ""GuzergahSeferleri"".""GuzergahId"")
                 WHERE ""FirmaId"" IS NULL
                   AND EXISTS (SELECT 1 FROM ""Guzergahlar"" g WHERE g.""Id"" = ""GuzergahSeferleri"".""GuzergahId"" AND g.""FirmaId"" IS NOT NULL);

                UPDATE ""GuzergahSeferleri""
                   SET ""FirmaId"" = COALESCE(
                       (SELECT ""Id"" FROM ""Firmalar"" WHERE ""VarsayilanFirma"" = TRUE AND ""Aktif"" = TRUE ORDER BY ""Id"" LIMIT 1),
                       (SELECT ""Id"" FROM ""Firmalar"" WHERE ""Aktif"" = TRUE ORDER BY ""Id"" LIMIT 1))
                 WHERE ""FirmaId"" IS NULL
                   AND EXISTS (SELECT 1 FROM ""Firmalar"" WHERE ""Aktif"" = TRUE);
            ");

            // NOT NULL'a al (IFirmaTenant marker'ı için ApplicationDbContext zorlar).
            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "GuzergahSeferleri",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuzergahSeferleri_FirmaId",
                table: "GuzergahSeferleri",
                column: "FirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_GuzergahSeferleri_Firmalar_FirmaId",
                table: "GuzergahSeferleri",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuzergahSeferleri_Firmalar_FirmaId",
                table: "GuzergahSeferleri");

            migrationBuilder.DropIndex(
                name: "IX_GuzergahSeferleri_FirmaId",
                table: "GuzergahSeferleri");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "GuzergahSeferleri");
        }
    }
}



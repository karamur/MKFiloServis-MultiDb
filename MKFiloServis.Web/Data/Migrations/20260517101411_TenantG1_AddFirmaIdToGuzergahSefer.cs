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

            // Backfill: parent Guzergah.FirmaId zaten zorunlu (Aşama C2), oradan al.
            // Eğer parent FirmaId NULL kalmışsa (olmamalı) varsayılan firmaya düş.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    v_default_firma_id integer;
BEGIN
    SELECT ""Id"" INTO v_default_firma_id
    FROM ""Firmalar""
    WHERE ""VarsayilanFirma"" = TRUE AND ""Aktif"" = TRUE
    ORDER BY ""Id""
    LIMIT 1;

    IF v_default_firma_id IS NULL THEN
        SELECT ""Id"" INTO v_default_firma_id
        FROM ""Firmalar""
        WHERE ""Aktif"" = TRUE
        ORDER BY ""Id""
        LIMIT 1;
    END IF;

    IF v_default_firma_id IS NULL THEN
        RAISE NOTICE 'TenantG1: aktif firma bulunamadı, GuzergahSeferleri backfill atlandı.';
    ELSE
        -- 1) Parent Guzergah'tan miras al.
        UPDATE ""GuzergahSeferleri"" gs
           SET ""FirmaId"" = g.""FirmaId""
          FROM ""Guzergahlar"" g
         WHERE gs.""GuzergahId"" = g.""Id""
           AND gs.""FirmaId"" IS NULL
           AND g.""FirmaId"" IS NOT NULL;

        -- 2) Hâlâ NULL kalan varsa varsayılan firmaya çek.
        UPDATE ""GuzergahSeferleri""
           SET ""FirmaId"" = v_default_firma_id
         WHERE ""FirmaId"" IS NULL;
    END IF;
END $$;
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



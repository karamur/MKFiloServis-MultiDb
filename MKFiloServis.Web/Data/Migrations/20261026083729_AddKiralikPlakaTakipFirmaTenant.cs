using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKiralikPlakaTakipFirmaTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "KiralikPlakaTakipler",
                type: "integer",
                nullable: true);

            // Kayıt zaten bir araca bağlıysa aracın firması otoritatif kaynaktır.
            migrationBuilder.Sql("""
                UPDATE "KiralikPlakaTakipler"
                SET "FirmaId" = (
                    SELECT "FirmaId" FROM "Araclar"
                    WHERE "Araclar"."Id" = "KiralikPlakaTakipler"."AracId"
                )
                WHERE "FirmaId" IS NULL AND "AracId" IS NOT NULL
                  AND EXISTS (
                    SELECT 1 FROM "Araclar"
                    WHERE "Araclar"."Id" = "KiralikPlakaTakipler"."AracId"
                      AND "Araclar"."FirmaId" IS NOT NULL
                  );
                """);

            // Eski plaka takip kayıtları araçla eşleşmemiş olabilir. Ekrandaki eski
            // kayıtların sahibi ÜSTÜN GRUP SEYAHAT'tir; onları bu firmaya bağla.
            migrationBuilder.Sql("""
                UPDATE "KiralikPlakaTakipler"
                SET "FirmaId" = (
                    SELECT "Id" FROM "Firmalar"
                    WHERE UPPER("FirmaAdi") = UPPER('ÜSTÜN GRUP SEYAHAT')
                      AND "Aktif" = TRUE
                    ORDER BY "Id"
                    LIMIT 1
                )
                WHERE "FirmaId" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "FirmaId",
                table: "KiralikPlakaTakipler",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "KiralikPlakaTakipler");
        }
    }
}

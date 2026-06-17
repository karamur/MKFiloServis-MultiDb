using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHakedisPuantajFaturaFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GelirFaturaId ve GiderFaturaId kolonları zaten AddMissingTables ile eklendi.
            // Bu migration sadece index ve FK constraint'lerini oluşturur.
            // Kolon yoksa ekle (yeni kurulum), varsa atla.
            // SQLite/PG idempotent kontrolü: kolon kontrolü yapmadan index dene,
            // eğer kolon yoksa hata alırız. O yüzden önce kolon garantisi:

            // Idempotent: önce kolonu dene (varsa zaten var), sonra index ve FK
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    ALTER TABLE ""HakedisPuantajlar"" ADD COLUMN IF NOT EXISTS ""GelirFaturaId"" integer NULL;
                EXCEPTION WHEN duplicate_column THEN NULL;
                END $$;
                DO $$ BEGIN
                    ALTER TABLE ""HakedisPuantajlar"" ADD COLUMN IF NOT EXISTS ""GiderFaturaId"" integer NULL;
                EXCEPTION WHEN duplicate_column THEN NULL;
                END $$;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_GelirFaturaId",
                table: "HakedisPuantajlar",
                column: "GelirFaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_GiderFaturaId",
                table: "HakedisPuantajlar",
                column: "GiderFaturaId");

            migrationBuilder.AddForeignKey(
                name: "FK_HakedisPuantajlar_Faturalar_GelirFaturaId",
                table: "HakedisPuantajlar",
                column: "GelirFaturaId",
                principalTable: "Faturalar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HakedisPuantajlar_Faturalar_GiderFaturaId",
                table: "HakedisPuantajlar",
                column: "GiderFaturaId",
                principalTable: "Faturalar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HakedisPuantajlar_Faturalar_GelirFaturaId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropForeignKey(
                name: "FK_HakedisPuantajlar_Faturalar_GiderFaturaId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_GelirFaturaId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_GiderFaturaId",
                table: "HakedisPuantajlar");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHakedisIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // === FK indexes on HakedisPuantajlar ===
            // These support the Foreign Key lookups: Arac, Cari, Fatura, Firma, Guzergah, Sofor.
            // The FK itself is enforced by the model, not SQL; these indexes make joins fast.
            // POOL-003: DB'de eksik FK indeksleri (opsiyonel ama performans için kritik)

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_AracId",
                table: "HakedisPuantajlar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_CariId",
                table: "HakedisPuantajlar",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_FaturaId",
                table: "HakedisPuantajlar",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_FirmaId",
                table: "HakedisPuantajlar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_GuzergahId",
                table: "HakedisPuantajlar",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_SoforId",
                table: "HakedisPuantajlar",
                column: "SoforId");

            // Composite index for filtered queries (FirmaId + Yil + Ay)
            // POOL-003: Dashboard ve raporlamada kullanılır
            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_FirmaId_Yil_Ay",
                table: "HakedisPuantajlar",
                columns: new[] { "FirmaId", "Yil", "Ay" });

            // === FK indexes on HakedisPuantajDetaylar ===
            // POOL-005: Detaylar FK indeksleri — hesaplama ve grid yükleme için kritik
            // (FK constraint EF tarafından yönetilir, indeks SQL seviyesinde)

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajDetaylar_HakedisPuantajId",
                table: "HakedisPuantajDetaylar",
                column: "HakedisPuantajId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajDetaylar_SeferTuruId",
                table: "HakedisPuantajDetaylar",
                column: "SeferTuruId");

            // === FK index on HakedisKesintiler ===
            // POOL-005: Kesinti başlık FK indeksi

            migrationBuilder.CreateIndex(
                name: "IX_HakedisKesintiler_HakedisPuantajId",
                table: "HakedisKesintiler",
                column: "HakedisPuantajId");

            // === FK index on HakedisSeferTurleri ===
            // POOL-005: Sefer türü firma filtreleme indeksi

            migrationBuilder.CreateIndex(
                name: "IX_HakedisSeferTurleri_FirmaId",
                table: "HakedisSeferTurleri",
                column: "FirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // HakedisPuantajlar FK indexes
            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_AracId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_CariId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_FaturaId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_FirmaId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_GuzergahId",
                table: "HakedisPuantajlar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_SoforId",
                table: "HakedisPuantajlar");

            // Composite index
            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajlar_FirmaId_Yil_Ay",
                table: "HakedisPuantajlar");

            // HakedisPuantajDetaylar FK indexes
            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajDetaylar_HakedisPuantajId",
                table: "HakedisPuantajDetaylar");

            migrationBuilder.DropIndex(
                name: "IX_HakedisPuantajDetaylar_SeferTuruId",
                table: "HakedisPuantajDetaylar");

            // HakedisKesintiler FK index
            migrationBuilder.DropIndex(
                name: "IX_HakedisKesintiler_HakedisPuantajId",
                table: "HakedisKesintiler");

            // HakedisSeferTurleri FK index
            migrationBuilder.DropIndex(
                name: "IX_HakedisSeferTurleri_FirmaId",
                table: "HakedisSeferTurleri");
        }
    }
}



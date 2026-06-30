using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseIndexesOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServisCalismalari_AracId",
                table: "ServisCalismalari");

            migrationBuilder.DropIndex(
                name: "IX_ServisCalismalari_GuzergahId",
                table: "ServisCalismalari");

            migrationBuilder.DropIndex(
                name: "IX_ServisCalismalari_SoforId",
                table: "ServisCalismalari");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_CariId",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_SirketId",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_BankaHesapId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_CariId",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_SirketId",
                table: "BankaKasaHareketleri");

            migrationBuilder.CreateIndex(
                name: "IX_ServisCalismalari_AracId_CalismaTarihi",
                table: "ServisCalismalari",
                columns: new[] { "AracId", "CalismaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_ServisCalismalari_CalismaTarihi",
                table: "ServisCalismalari",
                column: "CalismaTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_ServisCalismalari_GuzergahId_CalismaTarihi",
                table: "ServisCalismalari",
                columns: new[] { "GuzergahId", "CalismaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_ServisCalismalari_SoforId_CalismaTarihi",
                table: "ServisCalismalari",
                columns: new[] { "SoforId", "CalismaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_CariId_FaturaTarihi",
                table: "Faturalar",
                columns: new[] { "CariId", "FaturaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_Durum_VadeTarihi",
                table: "Faturalar",
                columns: new[] { "Durum", "VadeTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_FaturaTarihi",
                table: "Faturalar",
                column: "FaturaTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_FaturaTipi_FaturaTarihi",
                table: "Faturalar",
                columns: new[] { "FaturaTipi", "FaturaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_SirketId_FaturaTarihi",
                table: "Faturalar",
                columns: new[] { "SirketId", "FaturaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_BankaHesapId_IslemTarihi",
                table: "BankaKasaHareketleri",
                columns: new[] { "BankaHesapId", "IslemTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_CariId_IslemTarihi",
                table: "BankaKasaHareketleri",
                columns: new[] { "CariId", "IslemTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_HareketTipi_IslemTarihi",
                table: "BankaKasaHareketleri",
                columns: new[] { "HareketTipi", "IslemTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_SirketId_IslemTarihi",
                table: "BankaKasaHareketleri",
                columns: new[] { "SirketId", "IslemTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServisCalismalari_AracId_CalismaTarihi",
                table: "ServisCalismalari");

            migrationBuilder.DropIndex(
                name: "IX_ServisCalismalari_CalismaTarihi",
                table: "ServisCalismalari");

            migrationBuilder.DropIndex(
                name: "IX_ServisCalismalari_GuzergahId_CalismaTarihi",
                table: "ServisCalismalari");

            migrationBuilder.DropIndex(
                name: "IX_ServisCalismalari_SoforId_CalismaTarihi",
                table: "ServisCalismalari");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_CariId_FaturaTarihi",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_Durum_VadeTarihi",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_FaturaTarihi",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_FaturaTipi_FaturaTarihi",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_SirketId_FaturaTarihi",
                table: "Faturalar");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_BankaHesapId_IslemTarihi",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_CariId_IslemTarihi",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_HareketTipi_IslemTarihi",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaKasaHareketleri_SirketId_IslemTarihi",
                table: "BankaKasaHareketleri");

            migrationBuilder.CreateIndex(
                name: "IX_ServisCalismalari_AracId",
                table: "ServisCalismalari",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisCalismalari_GuzergahId",
                table: "ServisCalismalari",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisCalismalari_SoforId",
                table: "ServisCalismalari",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_CariId",
                table: "Faturalar",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_SirketId",
                table: "Faturalar",
                column: "SirketId");

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_BankaHesapId",
                table: "BankaKasaHareketleri",
                column: "BankaHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_CariId",
                table: "BankaKasaHareketleri",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_BankaKasaHareketleri_SirketId",
                table: "BankaKasaHareketleri",
                column: "SirketId");
        }
    }
}



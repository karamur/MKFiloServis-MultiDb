using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantC4_MakeFirmaIdNotNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TenantNullableFirmaId kaldırıldı — 7 entity: FirmaId NOT NULL (Kural 4)
            var tables = new[] { "Personeller", "Araclar", "BankaHesaplari", "BankaKasaHareketleri",
                "CariSeferUcretleri", "Guzergahlar", "Kapasiteler" };

            foreach (var table in tables)
            {
                migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + $@"
                    UPDATE ""{table}""
                    SET ""FirmaId"" = (SELECT ""Id"" FROM ""Firmalar"" WHERE NOT ""IsDeleted"" ORDER BY ""Id"" LIMIT 1)
                    WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                ");
                migrationBuilder.AlterColumn<int>(name: "FirmaId", table: table, type: "integer", nullable: false,
                    oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var tables = new[] { "Personeller", "Araclar", "BankaHesaplari", "BankaKasaHareketleri",
                "CariSeferUcretleri", "Guzergahlar", "Kapasiteler" };

            foreach (var table in tables)
            {
                migrationBuilder.AlterColumn<int>(name: "FirmaId", table: table, type: "integer", nullable: true,
                    oldClrType: typeof(int), oldType: "integer");
            }
        }
    }
}



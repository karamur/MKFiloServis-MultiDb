using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaturaIptalFisId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IptalFisId",
                table: "Faturalar",
                type: "integer",
                nullable: true);

            // UUID unique index — mükerrer e-fatura engelle (DB seviyesinde garanti)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Faturalar_EttnNo""
                ON ""Faturalar"" (""EttnNo"")
                WHERE ""EttnNo"" IS NOT NULL AND ""EttnNo"" <> '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Faturalar_EttnNo""");

            migrationBuilder.DropColumn(
                name: "IptalFisId",
                table: "Faturalar");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotMuhasebeFisId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IptalFisId",
                table: "MaasOdemeSnapshotlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MuhasebeFisId",
                table: "MaasOdemeSnapshotlar",
                type: "integer",
                nullable: true);

            // Mükerrer maaş fişi engelle — her firma/ay için sadece 1 fiş
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MuhasebeFisleri_MaasSnapshot""
                ON ""MuhasebeFisleri"" (""KaynakTip"", ""KaynakId"")
                WHERE ""KaynakTip"" IN ('MaasSnapshot', 'MaasSnapshotReverse');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_MuhasebeFisleri_MaasSnapshot""");

            migrationBuilder.DropColumn(
                name: "IptalFisId",
                table: "MaasOdemeSnapshotlar");

            migrationBuilder.DropColumn(
                name: "MuhasebeFisId",
                table: "MaasOdemeSnapshotlar");
        }
    }
}

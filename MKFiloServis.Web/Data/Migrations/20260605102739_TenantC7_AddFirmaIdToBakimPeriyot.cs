using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantC7_AddFirmaIdToBakimPeriyot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kural 4: BakimPeriyot → FirmaId + IFirmaTenant

            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "BakimPeriyotlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: FirmaId=0 → ilk gecerli firma
            migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + @"
                UPDATE ""BakimPeriyotlar""
                SET ""FirmaId"" = (SELECT ""Id"" FROM ""Firmalar"" WHERE NOT ""IsDeleted"" ORDER BY ""Id"" LIMIT 1)
                WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_BakimPeriyotlar_FirmaId",
                table: "BakimPeriyotlar",
                column: "FirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_BakimPeriyotlar_Firmalar_FirmaId",
                table: "BakimPeriyotlar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BakimPeriyotlar_Firmalar_FirmaId",
                table: "BakimPeriyotlar");

            migrationBuilder.DropIndex(
                name: "IX_BakimPeriyotlar_FirmaId",
                table: "BakimPeriyotlar");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "BakimPeriyotlar");
        }
    }
}



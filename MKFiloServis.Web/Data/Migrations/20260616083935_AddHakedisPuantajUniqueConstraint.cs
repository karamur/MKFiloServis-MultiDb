using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHakedisPuantajUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adım 1: Duplicate kayıtları temizle — en eski kayıt (min Id) kalsın
            migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + @"
                UPDATE ""HakedisPuantajlar""
                SET ""IsDeleted"" = true, ""DeletedAt"" = NOW()
                WHERE ""Id"" NOT IN (
                    SELECT MIN(""Id"")
                    FROM ""HakedisPuantajlar""
                    WHERE ""IsDeleted"" = false
                    GROUP BY ""Yil"", ""Ay"", ""GuzergahId"", ""AracId"", ""SoforId"", COALESCE(""FirmaId"", 0)
                )
                AND ""IsDeleted"" = false;
            ");

            // Adım 2: UNIQUE index — duplicate engeli
            // COALESCE ile nullable FirmaId: PostgreSQL NULL'ları distinct sayar
            migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + @"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_HakedisPuantajlar_UniqueKey""
                ON ""HakedisPuantajlar"" (""Yil"", ""Ay"", ""GuzergahId"", ""AracId"", ""SoforId"", COALESCE(""FirmaId"", 0))
                WHERE ""IsDeleted"" = false;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + @"DROP INDEX IF EXISTS ""IX_HakedisPuantajlar_UniqueKey"";");
        }
    }
}



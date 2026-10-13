using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261013130000_AllowCrossFirmInvoiceCari")]
public sealed class AllowCrossFirmInvoiceCari : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_A15_Fatura_CariFirma" ON "Faturalar";
                DROP TRIGGER IF EXISTS "TR_A15_Cari_Firma_Fatura" ON "Cariler";
                DROP FUNCTION IF EXISTS a15_guard_fatura_cari_firma();
                DROP FUNCTION IF EXISTS a15_guard_cari_firma_fatura();
                """);
            return;
        }

        if (ActiveProvider?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_A15_Fatura_CariFirma_Insert";
                DROP TRIGGER IF EXISTS "TR_A15_Fatura_CariFirma_Update";
                DROP TRIGGER IF EXISTS "TR_A15_Cari_Firma_Fatura";
                """);
            return;
        }

        throw new NotSupportedException("Fatura-cari firma bağı PostgreSQL ve SQLite için uygulanmıştır.");
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261009170000_GuardInvoiceCariTenantLink")]
public sealed class GuardInvoiceCariTenantLink : Migration
{
    // The migration ID stays in place for databases that already recorded it.
    // Invoices may belong to a different firm from the counterparty.
    protected override void Up(MigrationBuilder migrationBuilder) { }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}

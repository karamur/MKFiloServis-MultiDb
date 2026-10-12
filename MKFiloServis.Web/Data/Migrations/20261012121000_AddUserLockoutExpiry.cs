using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLockoutExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                // Older startup code may already have added this nullable column
                // without recording a migration. Adopt that compatible schema safely.
                migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Kullanicilar\" ADD COLUMN IF NOT EXISTS \"KilitlenmeBitisUtc\" timestamp without time zone NULL;");
                return;
            }

            if (ActiveProvider != "Microsoft.EntityFrameworkCore.Sqlite")
                throw new NotSupportedException($"Unsupported migration provider: {ActiveProvider}");

            var columnType = ActiveProvider switch
            {
                "Microsoft.EntityFrameworkCore.Sqlite" => "TEXT",
                _ => throw new NotSupportedException($"Unsupported migration provider: {ActiveProvider}")
            };

            migrationBuilder.AddColumn<DateTime>(
                name: "KilitlenmeBitisUtc",
                table: "Kullanicilar",
                type: columnType,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KilitlenmeBitisUtc",
                table: "Kullanicilar");
        }
    }
}

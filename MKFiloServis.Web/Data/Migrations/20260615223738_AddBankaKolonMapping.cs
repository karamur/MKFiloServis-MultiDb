using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankaKolonMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankaKolonMappingler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    TarihKolon = table.Column<int>(type: "integer", nullable: false),
                    AciklamaKolon = table.Column<int>(type: "integer", nullable: false),
                    TutarKolon = table.Column<int>(type: "integer", nullable: false),
                    BorcAlacakKolon = table.Column<int>(type: "integer", nullable: false),
                    ReferansKolon = table.Column<int>(type: "integer", nullable: false),
                    DosyaTipi = table.Column<string>(type: "text", nullable: false),
                    Ayrac = table.Column<string>(type: "text", nullable: false),
                    BaslikVarMi = table.Column<bool>(type: "boolean", nullable: false),
                    AtlanacakSatir = table.Column<int>(type: "integer", nullable: false),
                    BorcGostergesi = table.Column<string>(type: "text", nullable: true),
                    AlacakGostergesi = table.Column<string>(type: "text", nullable: true),
                    TarihFormati = table.Column<string>(type: "text", nullable: false),
                    SayiAyraci = table.Column<string>(type: "text", nullable: false),
                    Varsayilan = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankaKolonMappingler", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankaKolonMappingler");
        }
    }
}



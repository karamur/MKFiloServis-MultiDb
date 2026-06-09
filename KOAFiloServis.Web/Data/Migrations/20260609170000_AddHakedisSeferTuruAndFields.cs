using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    public partial class AddHakedisSeferTuruAndFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HakedisSeferTurleri table
            migrationBuilder.CreateTable(
                name: "HakedisSeferTurleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    Kod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VarsayilanSeferSayisi = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 1m),
                    FiyatCarpani = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 1m),
                    MesaiMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EkSeferMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SistemTanimliMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HakedisSeferTurleri", x => x.Id);
                    table.ForeignKey(name: "FK_HakedisSeferTurleri_Firmalar_FirmaId", column: x => x.FirmaId,
                        principalTable: "Firmalar", principalColumn: "Id");
                });
            migrationBuilder.CreateIndex(name: "IX_HakedisSeferTurleri_FirmaId", table: "HakedisSeferTurleri", column: "FirmaId");

            // HakedisPuantaj — rename + new columns
            migrationBuilder.RenameColumn(name: "BirimFiyat", table: "HakedisPuantajlar", newName: "GelirBirimFiyat");
            migrationBuilder.AddColumn<decimal>(name: "GiderBirimFiyat", table: "HakedisPuantajlar", type: "numeric(18,4)", nullable: false, defaultValue: 0m);
            // Copy GelirBirimFiyat → GiderBirimFiyat for existing records
            migrationBuilder.Sql("UPDATE \"HakedisPuantajlar\" SET \"GiderBirimFiyat\" = \"GelirBirimFiyat\"");
            migrationBuilder.AddColumn<decimal>(name: "GelirToplam", table: "HakedisPuantajlar", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "GiderToplam", table: "HakedisPuantajlar", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "TahsilEdilecekTutar", table: "HakedisPuantajlar", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.DropColumn(name: "HakedisTutari", table: "HakedisPuantajlar");
            migrationBuilder.DropColumn(name: "ToplamEkSefer", table: "HakedisPuantajlar");

            // HakedisPuantajDetay — new columns
            migrationBuilder.AddColumn<int>(name: "SeferTuruId", table: "HakedisPuantajDetaylar", type: "integer", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "FiyatCarpani", table: "HakedisPuantajDetaylar", type: "numeric(18,4)", nullable: false, defaultValue: 1m);
            migrationBuilder.AddColumn<bool>(name: "MesaiMi", table: "HakedisPuantajDetaylar", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.CreateIndex(name: "IX_HakedisPuantajDetaylar_SeferTuruId", table: "HakedisPuantajDetaylar", column: "SeferTuruId");
            migrationBuilder.AddForeignKey(name: "FK_HakedisPuantajDetaylar_HakedisSeferTurleri_SeferTuruId",
                table: "HakedisPuantajDetaylar", column: "SeferTuruId",
                principalTable: "HakedisSeferTurleri", principalColumn: "Id");

            // Seed default sefer türleri (FirmaId=NULL → tüm firmalar için)
            var seedSql = @"
INSERT INTO ""HakedisSeferTurleri"" (""FirmaId"", ""Kod"", ""Ad"", ""VarsayilanSeferSayisi"", ""FiyatCarpani"", ""MesaiMi"", ""EkSeferMi"", ""SistemTanimliMi"", ""Aktif"", ""CreatedAt"", ""IsDeleted"")
VALUES
(NULL, 'S',   'Sabah',          1, 1,   false, false, true, true, NOW(), false),
(NULL, 'A',   'Akşam',          1, 1,   false, false, true, true, NOW(), false),
(NULL, 'SA',  'Sabah + Akşam',  2, 1,   false, false, true, true, NOW(), false),
(NULL, 'EK',  'Ek Sefer',       1, 1,   false, true,  true, true, NOW(), false),
(NULL, 'MES', 'Mesai',          1, 1.5, true,  false, true, true, NOW(), false),
(NULL, 'GEC', 'Gece Seferi',    1, 1.5, true,  false, true, true, NOW(), false),
(NULL, 'HT',  'Hafta Sonu',     1, 2,   true,  false, true, true, NOW(), false),
(NULL, 'RT',  'Resmi Tatil',    1, 2,   true,  false, true, true, NOW(), false),
(NULL, 'OZL', 'Özel Servis',    1, 1,   false, true,  true, true, NOW(), false),
(NULL, 'TRF', 'Transfer',       1, 1,   false, true,  true, true, NOW(), false);
";
            migrationBuilder.Sql(seedSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("HakedisSeferTurleri");
            migrationBuilder.DropColumn("GiderBirimFiyat", "HakedisPuantajlar");
            migrationBuilder.DropColumn("GelirToplam", "HakedisPuantajlar");
            migrationBuilder.DropColumn("GiderToplam", "HakedisPuantajlar");
            migrationBuilder.DropColumn("TahsilEdilecekTutar", "HakedisPuantajlar");
            migrationBuilder.RenameColumn("GelirBirimFiyat", "HakedisPuantajlar", "BirimFiyat");
            migrationBuilder.AddColumn<decimal>("HakedisTutari", "HakedisPuantajlar", "numeric(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<int>("ToplamEkSefer", "HakedisPuantajlar", "integer", nullable: false, defaultValue: 0);
            migrationBuilder.DropColumn("SeferTuruId", "HakedisPuantajDetaylar");
            migrationBuilder.DropColumn("FiyatCarpani", "HakedisPuantajDetaylar");
            migrationBuilder.DropColumn("MesaiMi", "HakedisPuantajDetaylar");
        }
    }
}

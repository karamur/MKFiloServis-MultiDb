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
                migrationBuilder.Sql($@"
                    DO $$ DECLARE first_firma_id integer;
                    BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '{table}') THEN
                            -- NULL FirmaId'leri ilk gecerli firma ile doldur
                            SELECT ""Id"" INTO first_firma_id FROM ""Firmalar"" WHERE NOT ""IsDeleted"" ORDER BY ""Id"" LIMIT 1;
                            IF first_firma_id IS NOT NULL THEN
                                EXECUTE 'UPDATE ""{table}"" SET ""FirmaId"" = ' || first_firma_id || ' WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0';
                            END IF;
                            -- NOT NULL constraint ekle (zaten NOT NULL ise hata vermez)
                            EXECUTE 'ALTER TABLE ""{table}"" ALTER COLUMN ""FirmaId"" SET NOT NULL';
                        END IF;
                    EXCEPTION WHEN others THEN
                        -- Zaten NOT NULL veya tablo yok — gec
                    END; $$;
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var tables = new[] { "Personeller", "Araclar", "BankaHesaplari", "BankaKasaHareketleri",
                "CariSeferUcretleri", "Guzergahlar", "Kapasiteler" };

            foreach (var table in tables)
            {
                migrationBuilder.Sql($@"
                    DO $$ BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '{table}') THEN
                            EXECUTE 'ALTER TABLE ""{table}"" ALTER COLUMN ""FirmaId"" DROP NOT NULL';
                        END IF;
                    EXCEPTION WHEN others THEN END; $$;
                ");
            }
        }
    }
}



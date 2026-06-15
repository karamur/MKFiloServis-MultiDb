using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonelBankaOdemeAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BankaSube
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Personeller' AND column_name = 'BankaSube'
                    ) THEN
                        ALTER TABLE ""Personeller"" ADD COLUMN ""BankaSube"" VARCHAR(100) NULL;
                    END IF;
                END $$;
            ");

            // BankaSubeKodu
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Personeller' AND column_name = 'BankaSubeKodu'
                    ) THEN
                        ALTER TABLE ""Personeller"" ADD COLUMN ""BankaSubeKodu"" VARCHAR(20) NULL;
                    END IF;
                END $$;
            ");

            // BankaHesapNo
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Personeller' AND column_name = 'BankaHesapNo'
                    ) THEN
                        ALTER TABLE ""Personeller"" ADD COLUMN ""BankaHesapNo"" VARCHAR(50) NULL;
                    END IF;
                END $$;
            ");

            // MaasOdemeTipi — idempotent: kolon yoksa integer ekle, text ise integer'a çevir
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    col_type TEXT;
                BEGIN
                    SELECT data_type INTO col_type
                    FROM information_schema.columns
                    WHERE table_name = 'Personeller' AND column_name = 'MaasOdemeTipi';

                    IF col_type IS NULL THEN
                        -- Kolon yok → integer olarak ekle
                        ALTER TABLE ""Personeller"" ADD COLUMN ""MaasOdemeTipi"" INTEGER NOT NULL DEFAULT 0;
                    ELSIF col_type <> 'integer' THEN
                        -- Kolon var ama integer değil → dönüştür
                        ALTER TABLE ""Personeller""
                        ALTER COLUMN ""MaasOdemeTipi"" TYPE integer
                        USING
                            CASE
                                WHEN ""MaasOdemeTipi"" IS NULL OR ""MaasOdemeTipi"" = '' THEN 0
                                WHEN ""MaasOdemeTipi"" ~ '^[0-9]+$' THEN ""MaasOdemeTipi""::integer
                                WHEN lower(""MaasOdemeTipi"") = 'banka' THEN 0
                                WHEN lower(""MaasOdemeTipi"") = 'nakit' THEN 1
                                WHEN lower(""MaasOdemeTipi"") = 'cek' THEN 2
                                WHEN lower(""MaasOdemeTipi"") = 'diğer' THEN 3
                                WHEN lower(""MaasOdemeTipi"") = 'diger' THEN 3
                                ELSE 0
                            END;

                        ALTER TABLE ""Personeller"" ALTER COLUMN ""MaasOdemeTipi"" SET DEFAULT 0;
                        ALTER TABLE ""Personeller"" ALTER COLUMN ""MaasOdemeTipi"" SET NOT NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Personeller"" DROP COLUMN IF EXISTS ""BankaHesapNo"";
                ALTER TABLE ""Personeller"" DROP COLUMN IF EXISTS ""BankaSube"";
                ALTER TABLE ""Personeller"" DROP COLUMN IF EXISTS ""BankaSubeKodu"";
                ALTER TABLE ""Personeller"" DROP COLUMN IF EXISTS ""MaasOdemeTipi"";
            ");
        }
    }
}

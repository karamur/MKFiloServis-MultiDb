using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <summary>
    /// HOTFİX: Kapasiteler tablosuna FirmaId kolonu eksikti.
    /// Faz C-extend sırasında snapshot zaten Kapasite.FirmaId içerdiği için (entity önceki
    /// oturumda hazırlanmış, ama kolon migration'ı üretilmemiş) EF "fark yok" diyerek
    /// TenantCExt migration'ına Kapasiteler'i dahil etmedi.
    /// Sonuç: runtime'da "column k.FirmaId does not exist" (Npgsql 42703).
    /// Bu migration eksikliği kapatır. PL/pgSQL idempotent.
    /// </summary>
    public partial class TenantCExt2_AddFirmaIdToKapasite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) FirmaId kolonu (nullable, K9)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Kapasiteler' AND column_name = 'FirmaId'
                    ) THEN
                        ALTER TABLE ""Kapasiteler"" ADD COLUMN ""FirmaId"" integer NULL;
                    END IF;
                END $$;
            ");

            // 2) Index (FirmaId, KapasiteAdi) — snapshot ile uyumlu
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = current_schema()
                          AND tablename = 'Kapasiteler'
                          AND indexname = 'IX_Kapasiteler_FirmaId_KapasiteAdi'
                    ) THEN
                        CREATE INDEX ""IX_Kapasiteler_FirmaId_KapasiteAdi""
                            ON ""Kapasiteler"" (""FirmaId"", ""KapasiteAdi"");
                    END IF;
                END $$;
            ");

            // 3) FK Kapasiteler -> Firmalar (Restrict)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE table_name = 'Kapasiteler'
                          AND constraint_name = 'FK_Kapasiteler_Firmalar_FirmaId'
                          AND constraint_type = 'FOREIGN KEY'
                    ) THEN
                        ALTER TABLE ""Kapasiteler""
                            ADD CONSTRAINT ""FK_Kapasiteler_Firmalar_FirmaId""
                            FOREIGN KEY (""FirmaId"") REFERENCES ""Firmalar"" (""Id"") ON DELETE RESTRICT;
                    END IF;
                END $$;
            ");

            // 4) Backfill: NULL/0 satırları varsayılan firma ile doldur
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    v_firma_id integer;
                BEGIN
                    SELECT ""Id"" INTO v_firma_id
                    FROM ""Firmalar""
                    WHERE COALESCE(""VarsayilanFirma"", false) = true AND COALESCE(""Aktif"", true) = true
                    ORDER BY ""Id"" LIMIT 1;

                    IF v_firma_id IS NULL THEN
                        SELECT ""Id"" INTO v_firma_id
                        FROM ""Firmalar""
                        WHERE COALESCE(""Aktif"", true) = true
                        ORDER BY ""Id"" LIMIT 1;
                    END IF;

                    IF v_firma_id IS NOT NULL THEN
                        UPDATE ""Kapasiteler""
                        SET ""FirmaId"" = v_firma_id
                        WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Kapasiteler"" DROP CONSTRAINT IF EXISTS ""FK_Kapasiteler_Firmalar_FirmaId"";
                DROP INDEX IF EXISTS ""IX_Kapasiteler_FirmaId_KapasiteAdi"";
                ALTER TABLE ""Kapasiteler"" DROP COLUMN IF EXISTS ""FirmaId"";
            ");
        }
    }
}



using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <summary>
    /// Faz 5.3-B3-i: Tüm entity'lerden Sirket navigation property'leri silindi, DbContext'ten
    /// HasOne(Sirket) FK mapping'leri ve DbSet'ler kaldırıldı, Sirket/SirketTransferLog entity
    /// dosyaları silindi. Bu migration:
    ///   • 21 tablo için FK_*_Sirketler_SirketId constraint'lerini drop eder
    ///   • İlgili IX_*_SirketId indekslerini drop eder
    ///   • Sirketler ve SirketTransferLoglari tablolarını DROP yerine RENAME eder (_LEGACY_ prefix)
    ///     → veri korunur; Faz 5.3-B4'te (yedek alındıktan sonra) DROP edilecek
    ///   • int? SirketId kolonları korunur (B4'te kolon drop'u)
    /// PL/pgSQL idempotent: tüm operasyonlar tekrarlanabilir.
    /// </summary>
    public partial class TenantB3i_DropSirketNavigationAndEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── 1) Foreign Key'leri drop et (21 tablo) ───
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    fk_record record;
                BEGIN
                    FOR fk_record IN
                        SELECT tc.constraint_name, tc.table_name
                        FROM information_schema.table_constraints tc
                        WHERE tc.constraint_type = 'FOREIGN KEY'
                          AND tc.constraint_name ILIKE 'FK_%_Sirketler_SirketId'
                    LOOP
                        EXECUTE format('ALTER TABLE %I DROP CONSTRAINT IF EXISTS %I',
                                       fk_record.table_name, fk_record.constraint_name);
                    END LOOP;
                END $$;
            ");

            // ─── 2) IX_*_SirketId index'lerini drop et ───
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    idx_record record;
                BEGIN
                    FOR idx_record IN
                        SELECT indexname
                        FROM pg_indexes
                        WHERE schemaname = current_schema()
                          AND (indexname ILIKE 'IX_%_SirketId'
                            OR indexname ILIKE 'IX_%_SirketId_%')
                          AND indexname NOT LIKE 'IX_Firmalar_%'
                    LOOP
                        EXECUTE format('DROP INDEX IF EXISTS %I', idx_record.indexname);
                    END LOOP;
                END $$;
            ");

            // ─── 3) Sirketler ve SirketTransferLoglari tablolarını RENAME et (_LEGACY_ prefix) ───
            // DROP değil RENAME: veri korunur, B4'te yedek alındıktan sonra DROP edilecek.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SirketTransferLoglari')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '_LEGACY_SirketTransferLoglari') THEN
                        ALTER TABLE ""SirketTransferLoglari"" RENAME TO ""_LEGACY_SirketTransferLoglari"";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Sirketler')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '_LEGACY_Sirketler') THEN
                        ALTER TABLE ""Sirketler"" RENAME TO ""_LEGACY_Sirketler"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sadece tabloları geri rename eder. FK ve indeksler manuel kurulmalıdır
            // (önceki migration'ların Down'larından çekilebilir).
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '_LEGACY_Sirketler')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Sirketler') THEN
                        ALTER TABLE ""_LEGACY_Sirketler"" RENAME TO ""Sirketler"";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '_LEGACY_SirketTransferLoglari')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SirketTransferLoglari') THEN
                        ALTER TABLE ""_LEGACY_SirketTransferLoglari"" RENAME TO ""SirketTransferLoglari"";
                    END IF;
                END $$;
            ");
        }
    }
}



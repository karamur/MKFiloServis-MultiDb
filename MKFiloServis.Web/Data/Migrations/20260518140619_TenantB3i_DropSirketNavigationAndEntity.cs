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
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // A contiguous migration history fixes the schema version here. SQLite's
                // EF generator rebuilds affected tables for FK removal while copying rows.
                var legacyForeignKeys = new (string Table, string Name)[]
                {
                    ("Araclar", "FK_Araclar_Sirketler_SirketId"),
                    ("AracMaliyetSnapshotlari", "FK_AracMaliyetSnapshotlari_Sirketler_SirketId"),
                    ("AuditLoglar", "FK_AuditLoglar_Sirketler_SirketId"),
                    ("BankaHesaplari", "FK_BankaHesaplari_Sirketler_SirketId"),
                    ("BankaKasaHareketleri", "FK_BankaKasaHareketleri_Sirketler_SirketId"),
                    ("CariSeferUcretleri", "FK_CariSeferUcretleri_Sirketler_SirketId"),
                    ("Guzergahlar", "FK_Guzergahlar_Sirketler_SirketId"),
                    ("Hakedisler", "FK_Hakedisler_Sirketler_SirketId"),
                    ("Kapasiteler", "FK_Kapasiteler_Sirketler_SirketId"),
                    ("Kullanicilar", "FK_Kullanicilar_Sirketler_SirketId"),
                    ("LastikDegisimler", "FK_LastikDegisimler_Sirketler_SirketId"),
                    ("LastikDepolar", "FK_LastikDepolar_Sirketler_SirketId"),
                    ("LastikStoklar", "FK_LastikStoklar_Sirketler_SirketId"),
                    ("Personeller", "FK_Personeller_Sirketler_SirketId"),
                    ("ServisKontratlar", "FK_ServisKontratlar_Sirketler_SirketId"),
                    ("ServisOdemeler", "FK_ServisOdemeler_Sirketler_SirketId"),
                    ("ServisPuantajlar", "FK_ServisPuantajlar_Sirketler_SirketId"),
                    ("ServisTahsilatlar", "FK_ServisTahsilatlar_Sirketler_SirketId"),
                    ("TasimaTedarikciIsler", "FK_TasimaTedarikciIsler_Sirketler_SirketId"),
                    ("TasimaTedarikciler", "FK_TasimaTedarikciler_Sirketler_SirketId")
                };

                foreach (var foreignKey in legacyForeignKeys)
                {
                    migrationBuilder.DropForeignKey(name: foreignKey.Name, table: foreignKey.Table);
                }

                var legacyIndexes = new (string Table, string Name)[]
                {
                    ("Araclar", "IX_Araclar_SirketId"),
                    ("AracMaliyetSnapshotlari", "IX_AracMaliyetSnapshotlari_SirketId"),
                    ("AuditLoglar", "IX_AuditLoglar_SirketId"),
                    ("BankaHesaplari", "IX_BankaHesaplari_SirketId"),
                    ("BankaKasaHareketleri", "IX_BankaKasaHareketleri_SirketId"),
                    ("BankaKasaHareketleri", "IX_BankaKasaHareketleri_SirketId_IslemTarihi"),
                    ("CariSeferUcretleri", "IX_CariSeferUcretleri_SirketId"),
                    ("Guzergahlar", "IX_Guzergahlar_SirketId"),
                    ("Hakedisler", "IX_Hakedisler_SirketId"),
                    ("Kapasiteler", "IX_Kapasiteler_SirketId_KapasiteAdi"),
                    ("Kullanicilar", "IX_Kullanicilar_SirketId"),
                    ("LastikDegisimler", "IX_LastikDegisimler_SirketId"),
                    ("LastikDepolar", "IX_LastikDepolar_SirketId"),
                    ("LastikStoklar", "IX_LastikStoklar_SirketId"),
                    ("Personeller", "IX_Personeller_SirketId"),
                    ("ServisKontratlar", "IX_ServisKontratlar_SirketId"),
                    ("ServisOdemeler", "IX_ServisOdemeler_SirketId"),
                    ("ServisPuantajlar", "IX_ServisPuantajlar_SirketId"),
                    ("ServisTahsilatlar", "IX_ServisTahsilatlar_SirketId"),
                    ("TasimaTedarikciIsler", "IX_TasimaTedarikciIsler_SirketId"),
                    ("TasimaTedarikciler", "IX_TasimaTedarikciler_SirketId")
                };

                foreach (var index in legacyIndexes)
                {
                    migrationBuilder.DropIndex(name: index.Name, table: index.Table);
                }

                migrationBuilder.RenameTable(name: "SirketTransferLoglari", newName: "_LEGACY_SirketTransferLoglari");
                migrationBuilder.RenameTable(name: "Sirketler", newName: "_LEGACY_Sirketler");
                return;
            }

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
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.RenameTable(name: "_LEGACY_Sirketler", newName: "Sirketler");
                migrationBuilder.RenameTable(name: "_LEGACY_SirketTransferLoglari", newName: "SirketTransferLoglari");
                return;
            }

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



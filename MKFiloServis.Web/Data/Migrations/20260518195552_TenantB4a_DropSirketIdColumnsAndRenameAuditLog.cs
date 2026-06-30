using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantB4a_DropSirketIdColumnsAndRenameAuditLog : Migration
    {
        // Faz 5.3-B4a: Legacy Sirket tenant mimarisinin son kapanış adımı.
        // - 20 tablodaki nullable SirketId kolonları PL/pgSQL idempotent şekilde drop edilir
        //   (FK + index önce dinamik temizlenir, sonra DROP COLUMN IF EXISTS).
        // - AuditLoglar.SirketId kolonu FirmaId'ye RENAME edilir (içerik semantiği zaten
        //   aktif firma id'sidir; AuditLogService Faz 5.3-A'da rename edildi).
        // Veri kaybı: SirketId kolon içerikleri kalıcı olarak silinir. _LEGACY_Sirketler ve
        // _LEGACY_SirketTransferLoglari tabloları B4b migration'ında drop edilecek.
        private static readonly string[] SirketIdDropTables = new[]
        {
            "Araclar",
            "AracMaliyetSnapshotlari",
            "BankaHesaplari",
            "BankaKasaHareketleri",
            "CariSeferUcretleri",
            "Guzergahlar",
            "Hakedisler",
            "Kapasiteler",
            "Kullanicilar",
            "LastikDegisimler",
            "LastikDepolar",
            "LastikSezonAyarlari",
            "LastikStoklar",
            "Personeller",
            "ServisKontratlar",
            "ServisOdemeler",
            "ServisPuantajlar",
            "ServisTahsilatlar",
            "TasimaTedarikciIsler",
            "TasimaTedarikciler"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) 20 tablodan SirketId kolonu drop — FK ve index'ler dinamik temizlenir.
            foreach (var table in SirketIdDropTables)
            {
                migrationBuilder.Sql($@"
DO $$
DECLARE
    r RECORD;
BEGIN
    -- {table}.SirketId'ye bağlı tüm FK'leri sil
    FOR r IN
        SELECT conname FROM pg_constraint
        WHERE conrelid = '""{table}""'::regclass
          AND contype = 'f'
          AND conname ILIKE '%SirketId%'
    LOOP
        EXECUTE format('ALTER TABLE ""{table}"" DROP CONSTRAINT IF EXISTS %I', r.conname);
    END LOOP;

    -- {table}.SirketId index'lerini sil
    FOR r IN
        SELECT indexname FROM pg_indexes
        WHERE tablename = '{table}' AND indexname ILIKE '%SirketId%'
    LOOP
        EXECUTE format('DROP INDEX IF EXISTS %I', r.indexname);
    END LOOP;

    -- Kolonu sil (varsa)
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = '{table}' AND column_name = 'SirketId') THEN
        ALTER TABLE ""{table}"" DROP COLUMN ""SirketId"";
    END IF;
END $$;
");
            }

            // 2) AuditLoglar.SirketId → FirmaId rename (idempotent)
            migrationBuilder.Sql(@"
DO $$
DECLARE
    r RECORD;
BEGIN
    -- AuditLoglar üzerindeki SirketId'ye bağlı FK/index'leri önce temizle
    FOR r IN
        SELECT conname FROM pg_constraint
        WHERE conrelid = '""AuditLoglar""'::regclass
          AND contype = 'f'
          AND conname ILIKE '%SirketId%'
    LOOP
        EXECUTE format('ALTER TABLE ""AuditLoglar"" DROP CONSTRAINT IF EXISTS %I', r.conname);
    END LOOP;

    FOR r IN
        SELECT indexname FROM pg_indexes
        WHERE tablename = 'AuditLoglar' AND indexname ILIKE '%SirketId%'
    LOOP
        EXECUTE format('DROP INDEX IF EXISTS %I', r.indexname);
    END LOOP;

    -- SirketId hâlâ varsa ve FirmaId yoksa rename et
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'AuditLoglar' AND column_name = 'SirketId')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns
                       WHERE table_name = 'AuditLoglar' AND column_name = 'FirmaId') THEN
        ALTER TABLE ""AuditLoglar"" RENAME COLUMN ""SirketId"" TO ""FirmaId"";
    END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri al: AuditLoglar.FirmaId → SirketId, sonra 20 tabloya SirketId nullable eklenir.
            migrationBuilder.RenameColumn(
                name: "FirmaId",
                table: "AuditLoglar",
                newName: "SirketId");

            foreach (var table in SirketIdDropTables)
            {
                migrationBuilder.AddColumn<int>(
                    name: "SirketId",
                    table: table,
                    type: "integer",
                    nullable: true);
            }
        }
    }
}



using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantZ1_DropLegacyCariFaturaSirketColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL üzerinde geçmişte üretilmiş FK/Index isimleri farklı olabilir
            // (örn. snake_case veya başka migration tarafından oluşturulmuş). Bu yüzden
            // önce SirketId kolonuna bağlı tüm FK ve index'leri PL/pgSQL ile güvenli sil,
            // sonra kolonları drop et. Down() yine standart EF yoluyla geri ekler.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    r RECORD;
BEGIN
    -- Cariler.SirketId'ye bağlı tüm FK'leri sil
    FOR r IN
        SELECT conname FROM pg_constraint
        WHERE conrelid = '""Cariler""'::regclass
          AND contype = 'f'
          AND conname ILIKE '%SirketId%'
    LOOP
        EXECUTE format('ALTER TABLE ""Cariler"" DROP CONSTRAINT IF EXISTS %I', r.conname);
    END LOOP;

    -- Faturalar.SirketId'ye bağlı tüm FK'leri sil
    FOR r IN
        SELECT conname FROM pg_constraint
        WHERE conrelid = '""Faturalar""'::regclass
          AND contype = 'f'
          AND conname ILIKE '%SirketId%'
    LOOP
        EXECUTE format('ALTER TABLE ""Faturalar"" DROP CONSTRAINT IF EXISTS %I', r.conname);
    END LOOP;

    -- Cariler.SirketId index'lerini sil
    FOR r IN
        SELECT indexname FROM pg_indexes
        WHERE tablename = 'Cariler' AND indexname ILIKE '%SirketId%'
    LOOP
        EXECUTE format('DROP INDEX IF EXISTS %I', r.indexname);
    END LOOP;

    -- Faturalar.SirketId index'lerini sil
    FOR r IN
        SELECT indexname FROM pg_indexes
        WHERE tablename = 'Faturalar' AND indexname ILIKE '%SirketId%'
    LOOP
        EXECUTE format('DROP INDEX IF EXISTS %I', r.indexname);
    END LOOP;

    -- Kolonları sil (varsa)
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'SirketId') THEN
        ALTER TABLE ""Faturalar"" DROP COLUMN ""SirketId"";
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cariler' AND column_name = 'SirketId') THEN
        ALTER TABLE ""Cariler"" DROP COLUMN ""SirketId"";
    END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SirketId",
                table: "Faturalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SirketId",
                table: "Cariler",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_SirketId_FaturaTarihi",
                table: "Faturalar",
                columns: new[] { "SirketId", "FaturaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_SirketId",
                table: "Cariler",
                column: "SirketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_Sirketler_SirketId",
                table: "Cariler",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Sirketler_SirketId",
                table: "Faturalar",
                column: "SirketId",
                principalTable: "Sirketler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}



using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Data.Migrations;

/// <summary>
/// BaseEntity.DeletedAt kolonunu, henüz sahip olmayan tablolara ekler (Kural 16).
/// Startup sırasında idempotent olarak çalışır.
/// </summary>
public static class DeletedAtColumnMigrationHelper
{
    public static async Task EnsureDeletedAtColumnAsync(ApplicationDbContext context)
    {
        var sql = @"
DO $$ DECLARE t record;
BEGIN
    FOR t IN
        SELECT table_name
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND column_name = 'IsDeleted'
          AND table_name NOT IN (
              SELECT table_name
              FROM information_schema.columns
              WHERE table_schema = 'public' AND column_name = 'DeletedAt'
          )
    LOOP
        EXECUTE 'ALTER TABLE ""' || t.table_name || '"" ADD COLUMN ""DeletedAt"" TIMESTAMP NULL';
        RAISE NOTICE 'DeletedAt eklendi: %', t.table_name;
    END LOOP;
END; $$;";

        await context.Database.ExecuteSqlRawAsync(sql);
    }
}

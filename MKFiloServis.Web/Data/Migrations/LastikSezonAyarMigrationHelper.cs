using System.Data;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

public static class LastikSezonAyarMigrationHelper
{
    public static async Task ApplyLastikSezonAyarAsync(ApplicationDbContext context)
    {
        try
        {
            if (context.Database.IsNpgsql())
            {
                var sql = @"
CREATE TABLE IF NOT EXISTS ""LastikSezonAyarlari"" (
    ""Id"" SERIAL PRIMARY KEY,
    ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NULL,
    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
    ""Ad"" VARCHAR(200) NOT NULL,
    ""SezonTipi"" INTEGER NOT NULL,
    ""BaslangicAyi"" INTEGER NOT NULL,
    ""BaslangicGunu"" INTEGER NOT NULL,
    ""BitisAyi"" INTEGER NOT NULL,
    ""BitisGunu"" INTEGER NOT NULL,
    ""UyariOncesiGun"" INTEGER NOT NULL DEFAULT 14,
    ""Notlar"" TEXT NULL,
    ""Aktif"" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS ""IX_LastikSezonAyarlari_IsDeleted"" ON ""LastikSezonAyarlari"" (""IsDeleted"");
";

                await context.Database.ExecuteSqlRawAsync(sql);
                Console.WriteLine("LastikSezonAyarlari tablosu PostgreSQL için başarıyla oluşturuldu.");
            }
            else if (context.Database.IsSqlite())
            {
                var sql = @"
CREATE TABLE IF NOT EXISTS ""LastikSezonAyarlari"" (
    ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    ""CreatedAt"" TEXT NOT NULL,
    ""UpdatedAt"" TEXT NULL,
    ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
    ""Ad"" TEXT NOT NULL,
    ""SezonTipi"" INTEGER NOT NULL,
    ""BaslangicAyi"" INTEGER NOT NULL,
    ""BaslangicGunu"" INTEGER NOT NULL,
    ""BitisAyi"" INTEGER NOT NULL,
    ""BitisGunu"" INTEGER NOT NULL,
    ""UyariOncesiGun"" INTEGER NOT NULL DEFAULT 14,
    ""Notlar"" TEXT NULL,
    ""Aktif"" INTEGER NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS ""IX_LastikSezonAyarlari_IsDeleted"" ON ""LastikSezonAyarlari"" (""IsDeleted"");
";

                await context.Database.ExecuteSqlRawAsync(sql);
                Console.WriteLine("LastikSezonAyarlari tablosu SQLite için başarıyla oluşturuldu.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LastikSezonAyar migration uyarisi (kritik degil): {ex.Message}");
        }
    }
}




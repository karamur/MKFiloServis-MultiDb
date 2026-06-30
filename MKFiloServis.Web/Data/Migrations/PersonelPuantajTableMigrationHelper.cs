using System.Data;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// PersonelPuantajlar ve GunlukPuantajlar tablolarini olusturan migration helper
/// </summary>
public static class PersonelPuantajTableMigrationHelper
{
    public static async Task EnsurePersonelPuantajTablesAsync(ApplicationDbContext context)
    {
        try
        {
            if (context.Database.IsNpgsql())
            {
                await EnsurePostgresTablesAsync(context);
            }
            else if (context.Database.IsSqlite())
            {
                await EnsureSqliteTablesAsync(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PersonelPuantaj tablo olusturma hatasi: {ex.Message}");
        }
    }

    private static async Task EnsurePostgresTablesAsync(ApplicationDbContext context)
    {
        // PersonelPuantajlar tablosu
        var createPersonelPuantajSql = @"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PersonelPuantajlar') THEN
        CREATE TABLE ""PersonelPuantajlar"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""SirketId"" integer NOT NULL DEFAULT 1,
            ""FirmaId"" integer NOT NULL,
            ""PersonelId"" integer NOT NULL,
            ""Yil"" integer NOT NULL,
            ""Ay"" integer NOT NULL,
            ""CalisilanGun"" integer NOT NULL DEFAULT 0,
            ""FazlaMesaiSaat"" numeric(18,2) NOT NULL DEFAULT 0,
            ""IzinGunu"" integer NOT NULL DEFAULT 0,
            ""MazeretGunu"" integer NOT NULL DEFAULT 0,
            ""BrutMaas"" numeric(18,2) NOT NULL DEFAULT 0,
            ""YemekUcreti"" numeric(18,2) NOT NULL DEFAULT 0,
            ""YolUcreti"" numeric(18,2) NOT NULL DEFAULT 0,
            ""Prim"" numeric(18,2) NOT NULL DEFAULT 0,
            ""DigerOdeme"" numeric(18,2) NOT NULL DEFAULT 0,
            ""SgkKesinti"" numeric(18,2) NOT NULL DEFAULT 0,
            ""GelirVergisi"" numeric(18,2) NOT NULL DEFAULT 0,
            ""DamgaVergisi"" numeric(18,2) NOT NULL DEFAULT 0,
            ""DigerKesinti"" numeric(18,2) NOT NULL DEFAULT 0,
            ""NetOdeme"" numeric(18,2) NOT NULL DEFAULT 0,
            ""OdemeTarihi"" timestamp without time zone NULL,
            ""Odendi"" boolean NOT NULL DEFAULT FALSE,
            ""Aciklama"" text NULL,
            ""OnayDurumu"" integer NOT NULL DEFAULT 0,
            ""OnaylayanKullanici"" text NULL,
            ""OnayTarihi"" timestamp without time zone NULL,
            ""OnayNotu"" text NULL,
            ""BankaHesapNo"" text NULL,
            ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
            ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT NOW(),
            ""UpdatedAt"" timestamp without time zone NULL,
            ""CreatedBy"" text NULL,
            ""UpdatedBy"" text NULL,
            CONSTRAINT ""FK_PersonelPuantajlar_Firmalar"" FOREIGN KEY (""FirmaId"") REFERENCES ""Firmalar"" (""Id"") ON DELETE CASCADE,
            CONSTRAINT ""FK_PersonelPuantajlar_Personeller"" FOREIGN KEY (""PersonelId"") REFERENCES ""Personeller"" (""Id"") ON DELETE CASCADE
        );

        CREATE INDEX ""IX_PersonelPuantajlar_FirmaId"" ON ""PersonelPuantajlar"" (""FirmaId"");
        CREATE INDEX ""IX_PersonelPuantajlar_PersonelId"" ON ""PersonelPuantajlar"" (""PersonelId"");
        CREATE INDEX ""IX_PersonelPuantajlar_Yil_Ay"" ON ""PersonelPuantajlar"" (""Yil"", ""Ay"");
        CREATE INDEX ""IX_PersonelPuantajlar_SirketId"" ON ""PersonelPuantajlar"" (""SirketId"");
    END IF;
END $$;";

        await context.Database.ExecuteSqlRawAsync(createPersonelPuantajSql);

        var ensurePersonelPuantajColumnsSql = @"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PersonelPuantajlar') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'OdemeTarihi') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""OdemeTarihi"" timestamp without time zone NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'Odendi') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""Odendi"" boolean NOT NULL DEFAULT FALSE;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'BankaHesapNo') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""BankaHesapNo"" text NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'Aciklama') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""Aciklama"" text NULL;
        END IF;
    END IF;
END $$;";

        await context.Database.ExecuteSqlRawAsync(ensurePersonelPuantajColumnsSql);

        // GunlukPuantajlar tablosu
        var createGunlukPuantajSql = @"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'GunlukPuantajlar') THEN
        CREATE TABLE ""GunlukPuantajlar"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""SirketId"" integer NOT NULL DEFAULT 1,
            ""PersonelPuantajId"" integer NOT NULL,
            ""Tarih"" date NOT NULL,
            ""Calisti"" boolean NOT NULL DEFAULT FALSE,
            ""FazlaMesaiSaat"" numeric(18,2) NULL,
            ""Izinli"" boolean NOT NULL DEFAULT FALSE,
            ""Mazeret"" boolean NOT NULL DEFAULT FALSE,
            ""ServisCalismaId"" integer NULL,
            ""Notlar"" text NULL,
            ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
            ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT NOW(),
            ""UpdatedAt"" timestamp without time zone NULL,
            ""CreatedBy"" text NULL,
            ""UpdatedBy"" text NULL,
            CONSTRAINT ""FK_GunlukPuantajlar_PersonelPuantajlar"" FOREIGN KEY (""PersonelPuantajId"") REFERENCES ""PersonelPuantajlar"" (""Id"") ON DELETE CASCADE
        );

        CREATE INDEX ""IX_GunlukPuantajlar_PersonelPuantajId"" ON ""GunlukPuantajlar"" (""PersonelPuantajId"");
        CREATE INDEX ""IX_GunlukPuantajlar_Tarih"" ON ""GunlukPuantajlar"" (""Tarih"");
        CREATE INDEX ""IX_GunlukPuantajlar_SirketId"" ON ""GunlukPuantajlar"" (""SirketId"");
    END IF;
END $$;";

        await context.Database.ExecuteSqlRawAsync(createGunlukPuantajSql);

        var ensureGunlukPuantajColumnsSql = @"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'GunlukPuantajlar') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'GunlukPuantajlar' AND column_name = 'Calisti') THEN
            ALTER TABLE ""GunlukPuantajlar"" ADD COLUMN ""Calisti"" boolean NOT NULL DEFAULT FALSE;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'GunlukPuantajlar' AND column_name = 'Izinli') THEN
            ALTER TABLE ""GunlukPuantajlar"" ADD COLUMN ""Izinli"" boolean NOT NULL DEFAULT FALSE;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'GunlukPuantajlar' AND column_name = 'Mazeret') THEN
            ALTER TABLE ""GunlukPuantajlar"" ADD COLUMN ""Mazeret"" boolean NOT NULL DEFAULT FALSE;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'GunlukPuantajlar' AND column_name = 'ServisCalismaId') THEN
            ALTER TABLE ""GunlukPuantajlar"" ADD COLUMN ""ServisCalismaId"" integer NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'GunlukPuantajlar' AND column_name = 'Notlar') THEN
            ALTER TABLE ""GunlukPuantajlar"" ADD COLUMN ""Notlar"" text NULL;
        END IF;
    END IF;
END $$;";

        await context.Database.ExecuteSqlRawAsync(ensureGunlukPuantajColumnsSql);
    }

    private static async Task EnsureSqliteTablesAsync(ApplicationDbContext context)
    {
        // PersonelPuantajlar tablosu (SQLite)
        var createPersonelPuantajSql = @"
CREATE TABLE IF NOT EXISTS ""PersonelPuantajlar"" (
    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
    ""SirketId"" INTEGER NOT NULL DEFAULT 1,
    ""FirmaId"" INTEGER NOT NULL,
    ""PersonelId"" INTEGER NOT NULL,
    ""Yil"" INTEGER NOT NULL,
    ""Ay"" INTEGER NOT NULL,
    ""CalisilanGun"" INTEGER NOT NULL DEFAULT 0,
    ""FazlaMesaiSaat"" REAL NOT NULL DEFAULT 0,
    ""IzinGunu"" INTEGER NOT NULL DEFAULT 0,
    ""MazeretGunu"" INTEGER NOT NULL DEFAULT 0,
    ""BrutMaas"" REAL NOT NULL DEFAULT 0,
    ""YemekUcreti"" REAL NOT NULL DEFAULT 0,
    ""YolUcreti"" REAL NOT NULL DEFAULT 0,
    ""Prim"" REAL NOT NULL DEFAULT 0,
    ""DigerOdeme"" REAL NOT NULL DEFAULT 0,
    ""SgkKesinti"" REAL NOT NULL DEFAULT 0,
    ""GelirVergisi"" REAL NOT NULL DEFAULT 0,
    ""DamgaVergisi"" REAL NOT NULL DEFAULT 0,
    ""DigerKesinti"" REAL NOT NULL DEFAULT 0,
    ""NetOdeme"" REAL NOT NULL DEFAULT 0,
    ""OdemeTarihi"" TEXT NULL,
    ""Odendi"" INTEGER NOT NULL DEFAULT 0,
    ""Aciklama"" TEXT NULL,
    ""OnayDurumu"" INTEGER NOT NULL DEFAULT 0,
    ""OnaylayanKullanici"" TEXT NULL,
    ""OnayTarihi"" TEXT NULL,
    ""OnayNotu"" TEXT NULL,
    ""BankaHesapNo"" TEXT NULL,
    ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
    ""CreatedAt"" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""UpdatedAt"" TEXT NULL,
    ""CreatedBy"" TEXT NULL,
    ""UpdatedBy"" TEXT NULL,
    FOREIGN KEY (""FirmaId"") REFERENCES ""Firmalar"" (""Id"") ON DELETE CASCADE,
    FOREIGN KEY (""PersonelId"") REFERENCES ""Personeller"" (""Id"") ON DELETE CASCADE
);";

        await context.Database.ExecuteSqlRawAsync(createPersonelPuantajSql);
        await EnsureSqliteColumnAsync(context, "PersonelPuantajlar", "OdemeTarihi", "TEXT NULL");
        await EnsureSqliteColumnAsync(context, "PersonelPuantajlar", "Odendi", "INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnAsync(context, "PersonelPuantajlar", "BankaHesapNo", "TEXT NULL");
        await EnsureSqliteColumnAsync(context, "PersonelPuantajlar", "Aciklama", "TEXT NULL");

        // Index'ler (SQLite)
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS ""IX_PersonelPuantajlar_FirmaId"" ON ""PersonelPuantajlar"" (""FirmaId"");");
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS ""IX_PersonelPuantajlar_PersonelId"" ON ""PersonelPuantajlar"" (""PersonelId"");");
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS ""IX_PersonelPuantajlar_Yil_Ay"" ON ""PersonelPuantajlar"" (""Yil"", ""Ay"");");

        // GunlukPuantajlar tablosu (SQLite)
        var createGunlukPuantajSql = @"
CREATE TABLE IF NOT EXISTS ""GunlukPuantajlar"" (
    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
    ""SirketId"" INTEGER NOT NULL DEFAULT 1,
    ""PersonelPuantajId"" INTEGER NOT NULL,
    ""Tarih"" TEXT NOT NULL,
    ""Calisti"" INTEGER NOT NULL DEFAULT 0,
    ""FazlaMesaiSaat"" REAL NULL,
    ""Izinli"" INTEGER NOT NULL DEFAULT 0,
    ""Mazeret"" INTEGER NOT NULL DEFAULT 0,
    ""ServisCalismaId"" INTEGER NULL,
    ""Notlar"" TEXT NULL,
    ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
    ""CreatedAt"" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""UpdatedAt"" TEXT NULL,
    ""CreatedBy"" TEXT NULL,
    ""UpdatedBy"" TEXT NULL,
    FOREIGN KEY (""PersonelPuantajId"") REFERENCES ""PersonelPuantajlar"" (""Id"") ON DELETE CASCADE
);";

        await context.Database.ExecuteSqlRawAsync(createGunlukPuantajSql);
        await EnsureSqliteColumnAsync(context, "GunlukPuantajlar", "Calisti", "INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnAsync(context, "GunlukPuantajlar", "Izinli", "INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnAsync(context, "GunlukPuantajlar", "Mazeret", "INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnAsync(context, "GunlukPuantajlar", "ServisCalismaId", "INTEGER NULL");
        await EnsureSqliteColumnAsync(context, "GunlukPuantajlar", "Notlar", "TEXT NULL");

        // Index'ler (SQLite)
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS ""IX_GunlukPuantajlar_PersonelPuantajId"" ON ""GunlukPuantajlar"" (""PersonelPuantajId"");");
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS ""IX_GunlukPuantajlar_Tarih"" ON ""GunlukPuantajlar"" (""Tarih"");");
    }

    private static async Task EnsureSqliteColumnAsync(ApplicationDbContext context, string tableName, string columnName, string columnDefinition)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = $"SELECT 1 FROM pragma_table_info('{tableName}') WHERE name = $columnName LIMIT 1";

            var parameter = checkCommand.CreateParameter();
            parameter.ParameterName = "$columnName";
            parameter.Value = columnName;
            checkCommand.Parameters.Add(parameter);

            var exists = await checkCommand.ExecuteScalarAsync() is not null;
            if (exists)
            {
                return;
            }

            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnDefinition}";
            await alterCommand.ExecuteNonQueryAsync();
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}




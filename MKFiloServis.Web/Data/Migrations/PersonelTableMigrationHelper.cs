using System.Data;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

public static class PersonelTableMigrationHelper
{
    public static async Task ApplyPersonelTableMigrationAsync(ApplicationDbContext context)
    {
        try
        {
            if (context.Database.IsNpgsql())
            {
                var sql = @"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Soforler')
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Personeller') THEN
        ALTER TABLE ""Soforler"" RENAME TO ""Personeller"";
    END IF;
END $$;";

                await context.Database.ExecuteSqlRawAsync(sql);
                await NormalizePersonellerEnumColumnsForPostgresAsync(context);
                return;
            }

            if (context.Database.IsSqlite())
            {
                var connection = context.Database.GetDbConnection();
                var shouldClose = connection.State != ConnectionState.Open;

                if (shouldClose)
                {
                    await connection.OpenAsync();
                }

                try
                {
                    var hasSoforler = await TableExistsAsync(connection, "Soforler");
                    var hasPersoneller = await TableExistsAsync(connection, "Personeller");

                    if (hasSoforler && !hasPersoneller)
                    {
                        await using var command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE \"Soforler\" RENAME TO \"Personeller\"";
                        await command.ExecuteNonQueryAsync();
                    }

                    if (await TableExistsAsync(connection, "Personeller") &&
                        !await ColumnExistsAsync(connection, "Personeller", "SiralamaNo"))
                    {
                        await using var addColumnCommand = connection.CreateCommand();
                        addColumnCommand.CommandText = "ALTER TABLE \"Personeller\" ADD COLUMN \"SiralamaNo\" INTEGER NOT NULL DEFAULT 0";
                        await addColumnCommand.ExecuteNonQueryAsync();
                    }
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
        catch (Exception ex)
        {
            Console.WriteLine($"Personel tablo migration hatası: {ex.Message}");
        }
    }

    private static async Task<bool> TableExistsAsync(System.Data.Common.DbConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $tableName LIMIT 1";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync() is not null;
    }

    private static async Task<bool> ColumnExistsAsync(System.Data.Common.DbConnection connection, string tableName, string columnName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT 1 FROM pragma_table_info('{tableName}') WHERE name = $columnName LIMIT 1";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$columnName";
        parameter.Value = columnName;
        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync() is not null;
    }

    private static async Task NormalizePersonellerEnumColumnsForPostgresAsync(ApplicationDbContext context)
    {
        var sql = """
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'Personeller' AND column_name = 'MaasOdemeTipi'
          AND data_type IN ('text', 'character varying')
    ) THEN
        ALTER TABLE "Personeller"
        ALTER COLUMN "MaasOdemeTipi" TYPE integer
        USING (
            CASE
                WHEN trim(coalesce("MaasOdemeTipi", '')) ~ '^[0-9]+$' THEN trim("MaasOdemeTipi")::integer
                WHEN lower(trim(coalesce("MaasOdemeTipi", ''))) = 'banka' THEN 0
                WHEN lower(trim(coalesce("MaasOdemeTipi", ''))) = 'nakit' THEN 1
                WHEN lower(trim(coalesce("MaasOdemeTipi", ''))) IN ('cek', 'çek') THEN 2
                WHEN lower(trim(coalesce("MaasOdemeTipi", ''))) = 'diger' THEN 3
                ELSE 0
            END
        );

        ALTER TABLE "Personeller" ALTER COLUMN "MaasOdemeTipi" SET DEFAULT 0;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'Personeller' AND column_name = 'BordroTipiPersonel'
          AND data_type IN ('text', 'character varying')
    ) THEN
        ALTER TABLE "Personeller"
        ALTER COLUMN "BordroTipiPersonel" TYPE integer
        USING (
            CASE
                WHEN trim(coalesce("BordroTipiPersonel", '')) ~ '^[0-9]+$' THEN trim("BordroTipiPersonel")::integer
                WHEN lower(trim(coalesce("BordroTipiPersonel", ''))) = 'yok' THEN 0
                WHEN lower(trim(coalesce("BordroTipiPersonel", ''))) = 'normal' THEN 1
                WHEN lower(trim(coalesce("BordroTipiPersonel", ''))) IN ('arge', 'ar-ge') THEN 2
                ELSE 0
            END
        );

        ALTER TABLE "Personeller" ALTER COLUMN "BordroTipiPersonel" SET DEFAULT 0;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'Personeller' AND column_name = 'BrutMaasHesaplamaTipi'
          AND data_type IN ('text', 'character varying')
    ) THEN
        ALTER TABLE "Personeller"
        ALTER COLUMN "BrutMaasHesaplamaTipi" TYPE integer
        USING (
            CASE
                WHEN trim(coalesce("BrutMaasHesaplamaTipi", '')) ~ '^[0-9]+$' THEN trim("BrutMaasHesaplamaTipi")::integer
                WHEN lower(trim(coalesce("BrutMaasHesaplamaTipi", ''))) = 'manuel' THEN 0
                WHEN lower(trim(coalesce("BrutMaasHesaplamaTipi", ''))) = 'saatlik' THEN 1
                WHEN lower(trim(coalesce("BrutMaasHesaplamaTipi", ''))) IN ('aylik', 'aylık') THEN 2
                WHEN lower(trim(coalesce("BrutMaasHesaplamaTipi", ''))) IN ('gunluk', 'günlük') THEN 3
                ELSE 0
            END
        );
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'Personeller' AND column_name = 'Gorev'
          AND data_type IN ('text', 'character varying')
    ) THEN
        ALTER TABLE "Personeller"
        ALTER COLUMN "Gorev" TYPE integer
        USING (
            CASE
                WHEN trim(coalesce("Gorev", '')) ~ '^[0-9]+$' THEN trim("Gorev")::integer
                WHEN lower(trim(coalesce("Gorev", ''))) IN ('sofor', 'şoför') THEN 1
                WHEN lower(trim(coalesce("Gorev", ''))) IN ('ofiscalisani', 'ofis çalışanı', 'ofis') THEN 2
                WHEN lower(trim(coalesce("Gorev", ''))) = 'muhasebe' THEN 3
                WHEN lower(trim(coalesce("Gorev", ''))) IN ('yonetici', 'yönetici') THEN 4
                WHEN lower(trim(coalesce("Gorev", ''))) = 'teknik' THEN 5
                WHEN lower(trim(coalesce("Gorev", ''))) = 'diger' THEN 99
                ELSE 99
            END
        );
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'Personeller' AND column_name = 'SgkCalismaTuru'
          AND data_type IN ('text', 'character varying')
    ) THEN
        ALTER TABLE "Personeller"
        ALTER COLUMN "SgkCalismaTuru" TYPE integer
        USING (
            CASE
                WHEN trim(coalesce("SgkCalismaTuru", '')) = '' THEN NULL
                WHEN trim(coalesce("SgkCalismaTuru", '')) ~ '^[0-9]+$' THEN trim("SgkCalismaTuru")::integer
                WHEN lower(trim(coalesce("SgkCalismaTuru", ''))) = 'tamzamanli' THEN 1
                WHEN lower(trim(coalesce("SgkCalismaTuru", ''))) = 'kismizamanli' THEN 2
                ELSE NULL
            END
        );

        ALTER TABLE "Personeller" ALTER COLUMN "SgkCalismaTuru" SET DEFAULT 1;
    END IF;
END $$;
""";

        await context.Database.ExecuteSqlRawAsync(sql);
    }
}




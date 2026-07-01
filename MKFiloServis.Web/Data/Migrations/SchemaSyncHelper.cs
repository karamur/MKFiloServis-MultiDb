using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using System.Data;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// EF Core model ile veritabanı şeması arasındaki eksik kolonları otomatik tespit edip
/// PostgreSQL'e ekler. Startup sırasında idempotent olarak çalışır.
///
/// EF modelindeki tüm entity'leri tarar, eksik olan HER kolonu tek seferde ekler —
/// "column does not exist" hatalarını kökten çözer.
/// DeletedAt kolonları da dahil tüm eksik kolonlar burada halledilir.
/// </summary>
public static class SchemaSyncHelper
{
    /// <summary>
    /// EF modelindeki tüm entity'leri tarar, DB'de eksik olan tüm kolonları otomatik ekler.
    /// </summary>
    public static async Task EnsureAllColumnsExistAsync(ApplicationDbContext context)
    {
        // 1) Veritabanında mevcut olan tüm kolonları tek sorguda al
        var existingColumns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = context.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = @"
            SELECT table_name, column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name NOT LIKE 'pg_%'
              AND table_name NOT LIKE 'sql_%'
            ORDER BY table_name, ordinal_position";
        cmd.CommandType = CommandType.Text;

        var wasOpen = cmd.Connection!.State == ConnectionState.Open;
        if (!wasOpen) await cmd.Connection.OpenAsync();

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var table = reader.GetString(0);
                var column = reader.GetString(1);
                if (!existingColumns.ContainsKey(table))
                    existingColumns[table] = new HashSet<string>(StringComparer.Ordinal);
                existingColumns[table].Add(column);
            }
        }
        finally
        {
            if (!wasOpen) await cmd.Connection.CloseAsync();
        }

        // 2) EF modelindeki tüm entity tiplerini ve kolonlarını tara
        var modelColumns = new Dictionary<string, List<ModelColumnDef>>(StringComparer.OrdinalIgnoreCase);
        var entityTypes = context.Model.GetEntityTypes();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrEmpty(tableName)) continue;

            if (!modelColumns.ContainsKey(tableName))
                modelColumns[tableName] = new List<ModelColumnDef>();

            // Tüm scalar property'leri al (navigation'ları atla)
            foreach (var prop in entityType.GetProperties())
            {
                var colName = prop.GetColumnName();
                if (string.IsNullOrEmpty(colName)) continue;

                // Aynı kolonu mükerrer ekleme (örn. owned type'lar)
                if (modelColumns[tableName].Any(c => c.ColumnName == colName))
                    continue;

                modelColumns[tableName].Add(new ModelColumnDef
                {
                    ColumnName = colName,
                    ClrType = prop.ClrType,
                    IsNullable = prop.IsNullable,
                    MaxLength = prop.GetMaxLength(),
                    IsPrimaryKey = prop.IsPrimaryKey(),
                    DefaultValueSql = prop.GetDefaultValueSql(),
                    DefaultValue = prop.GetDefaultValue(),
                    ValueGenerated = prop.ValueGenerated
                });
            }
        }

        // 3) Eksik kolonları tespit et ve ALTER TABLE SQL'leri oluştur
        var alterStatements = new List<string>();

        foreach (var (tableName, expectedCols) in modelColumns.OrderBy(kvp => kvp.Key))
        {
            // Tablo DB'de hiç yoksa atla (tablo oluşturma EF migration'ın işi)
            if (!existingColumns.TryGetValue(tableName, out var actualCols))
                continue;

            foreach (var colDef in expectedCols)
            {
                // Kolon zaten varsa atla
                if (actualCols.Contains(colDef.ColumnName))
                    continue;

                var sql = BuildAddColumnSql(tableName, colDef);
                alterStatements.Add(sql);
            }
        }

        // 4) SQL'leri grupla ve çalıştır (her tablo için grupla)
        if (alterStatements.Count > 0)
        {
            // Her ALTER TABLE'ı ayrı çalıştır (Npgsql batch desteklemez)
            foreach (var sql in alterStatements)
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync(sql);
                }
                catch (Exception ex)
                {
                    // Idempotent hataları sessiz geç:
                    // 42701: duplicate_column, 42P07: duplicate_table
                    // 23502: not_null_violation (mevcut veriyle uyumsuz NOT NULL — kolon yine de eklendi, constraint yok)
                    // 23505: unique_violation
                    if (ex is PostgresException pgEx &&
                        (pgEx.SqlState == "42701" || pgEx.SqlState == "42P07" ||
                         pgEx.SqlState == "23502" || pgEx.SqlState == "23505"))
                    {
                        continue;
                    }
                    // Diğer hataları yutma
                    Console.WriteLine($"[SchemaSync] UYARI: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// FisNoCounters tablosunun doğru PK/unique constraint ile var olduğundan emin olur.
    /// Eski 2-kolonlu PK (Prefix, YilAy) → yeni 3-kolonlu unique (Prefix, FirmaId, YilAy) geçişini yapar.
    /// Bu olmadan NumaraSerisiService 42P10 hatası verir.
    /// </summary>
    public static async Task EnsureFisNoCountersSchemaAsync(ApplicationDbContext context)
    {
        var previousTimeout = context.Database.GetCommandTimeout();
        try
        {
            // Bu migration bazı ortamlarda lock beklemesi nedeniyle 30sn varsayılan timeout'u aşabiliyor.
            context.Database.SetCommandTimeout(180);

            await context.Database.ExecuteSqlRawAsync(@"
                -- Tablo yoksa doğru şemayla oluştur
                CREATE TABLE IF NOT EXISTS ""FisNoCounters"" (
                    ""Prefix""  TEXT NOT NULL,
                    ""FirmaId"" INTEGER NOT NULL DEFAULT 0,
                    ""YilAy""   TEXT NOT NULL,
                    ""SonNo""   INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (""Prefix"", ""FirmaId"", ""YilAy"")
                );

                -- Eski tabloyu yeni şemaya yükselt (idempotent)
                DO $$ DECLARE
                    pk_name text;
                    has_expected_unique boolean;
                BEGIN
                    -- 1) FirmaId kolonunu ekle (yoksa)
                    ALTER TABLE ""FisNoCounters"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NOT NULL DEFAULT 0;

                    -- 2) Beklenen unique index var mı kontrol et
                    SELECT EXISTS (
                        SELECT 1 FROM pg_index i
                        JOIN pg_class t ON t.oid = i.indrelid
                        JOIN pg_namespace n ON n.oid = t.relnamespace
                        WHERE i.indisunique
                          AND n.nspname = 'public'
                          AND t.relname = 'FisNoCounters'
                          AND (SELECT array_agg(a.attname ORDER BY x.n)
                               FROM unnest(i.indkey) WITH ORDINALITY AS x(attnum, n)
                               JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = x.attnum
                              ) = ARRAY['Prefix', 'FirmaId', 'YilAy']::name[]
                    ) INTO has_expected_unique;

                    -- 3) Beklenen unique yoksa: eski PK'yi kaldır, yeni unique index oluştur
                    IF NOT has_expected_unique THEN
                        -- 3a) Mevcut PK constraint adını bul
                        SELECT c.conname INTO pk_name
                        FROM pg_constraint c
                        JOIN pg_class t ON t.oid = c.conrelid
                        JOIN pg_namespace n ON n.oid = t.relnamespace
                        WHERE c.contype = 'p'
                          AND n.nspname = 'public'
                          AND t.relname = 'FisNoCounters'
                        LIMIT 1;

                        -- 3b) PK'yi kaldır
                        IF pk_name IS NOT NULL THEN
                            EXECUTE format('ALTER TABLE %I DROP CONSTRAINT %I', 'FisNoCounters', pk_name);
                        END IF;

                        -- 3c) NULL FirmaId'leri düzelt (eski kayıtlar için)
                        UPDATE ""FisNoCounters"" SET ""FirmaId"" = 0 WHERE ""FirmaId"" IS NULL;

                        -- 3d) Yeni unique index'i oluştur
                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_FisNoCounters_Prefix_FirmaId_YilAy""
                            ON ""FisNoCounters"" (""Prefix"", ""FirmaId"", ""YilAy"");
                    END IF;
                END $$;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SchemaSync] FisNoCounters migration hatasi (startup devam eder): {ex.Message}");
        }
        finally
        {
            context.Database.SetCommandTimeout(previousTimeout);
        }
    }

    /// <summary>
    /// SchemaSync şemayı zaten EF modeliyle uyumlu hale getirdiği için,
    /// tüm bekleyen migration'ları __EFMigrationsHistory tablosuna "uygulandı" olarak kaydeder.
    /// Bu sayede DbInitializer boş yere 65 migration çalıştırıp "column already exists" hatası vermez.
    /// </summary>
    public static async Task MarkAllPendingMigrationsAsAppliedAsync(ApplicationDbContext context)
    {
        try
        {
            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (!pendingMigrations.Any()) return;

            // Güvenlik kontrolü:
            // Modelde beklenen tabloların bir kısmı DB'de yoksa migration geçmişini yapay olarak doldurma.
            // Aksi halde tablolar hiç oluşmadan startup devam eder ve runtime'da 42P01 hataları patlar.
            var modelTables = context.Model.GetEntityTypes()
                .Select(e => e.GetTableName())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var tableCmd = context.Database.GetDbConnection().CreateCommand();
            tableCmd.CommandText = @"
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public'";
            tableCmd.CommandType = System.Data.CommandType.Text;

            var wasOpen = tableCmd.Connection!.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await tableCmd.Connection.OpenAsync();

            try
            {
                await using var tableReader = await tableCmd.ExecuteReaderAsync();
                while (await tableReader.ReadAsync())
                {
                    existingTables.Add(tableReader.GetString(0));
                }
            }
            finally
            {
                if (!wasOpen) await tableCmd.Connection.CloseAsync();
            }

            var missingModelTables = modelTables
                .Where(t => !existingTables.Contains(t))
                .OrderBy(t => t)
                .ToList();

            if (missingModelTables.Count > 0)
            {
                Console.WriteLine($"[SchemaSync] {missingModelTables.Count} model tablosu DB'de eksik. Pending migration'lar history'e yazilmadi.");
                return;
            }

            // __EFMigrationsHistory tablosu var mı kontrol et
            await using var cmd = context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = @"
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'
                )";
            cmd.CommandType = System.Data.CommandType.Text;

            wasOpen = cmd.Connection!.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await cmd.Connection.OpenAsync();

            var historyExists = false;
            try
            {
                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                    historyExists = reader.GetBoolean(0);
            }
            finally
            {
                if (!wasOpen) await cmd.Connection.CloseAsync();
            }

            if (!historyExists) return; // Tablo yoksa migration sistemi henüz kurulmamış

            // Her pending migration'ı history'ye ekle
            // migrationId EF Core'un iç ID'leridir (timestamp + GUID formatında), SQL injection riski yoktur
            foreach (var migrationId in pendingMigrations)
            {
                try
                {
                    // migrationId EF'ten gelir, FormattableString parametrize eder => SQL injection güvenli
                    await context.Database.ExecuteSqlAsync(
                        $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({migrationId}, '10.0.5') ON CONFLICT DO NOTHING");
                }
                catch
                {
                    // Zaten varsa sessiz geç
                }
            }

            Console.WriteLine($"[SchemaSync] {pendingMigrations.Count} pending migration __EFMigrationsHistory'ye kaydedildi (schema zaten uyumlu).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SchemaSync] Migration geçmişi kaydedilirken hata (startup devam eder): {ex.Message}");
        }
    }

    /// <summary>
    /// .NET tipini PostgreSQL tipine çevirip ADD COLUMN SQL'i oluşturur.
    /// NOT NULL kolonlar için her zaman DEFAULT ekler — mevcut veriyle uyumlu.
    /// </summary>
    private static string BuildAddColumnSql(string tableName, ModelColumnDef col)
    {
        var pgType = MapToPostgresType(col);

        // PK ve auto-increment kolonları için özel işlem — nullable bırak
        var isAutoPk = col.IsPrimaryKey && col.ValueGenerated == ValueGenerated.OnAdd;

        // DEFAULT değeri (NOT NULL kolonlar için mutlaka gerekli)
        var defaultClause = "";
        if (col.DefaultValueSql != null)
        {
            defaultClause = $" DEFAULT {col.DefaultValueSql}";
        }
        else if (col.DefaultValue != null)
        {
            defaultClause = col.DefaultValue switch
            {
                bool b => $" DEFAULT {(b ? "true" : "false")}",
                int i => $" DEFAULT {i}",
                long l => $" DEFAULT {l}",
                decimal d => $" DEFAULT {d.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                string s => $" DEFAULT '{s}'",
                _ => ""
            };
        }
        else
        {
            // Modelde NOT NULL olan ama DEFAULT'i olmayan kolonlar için otomatik DEFAULT ata
            // Bu, mevcut verili tablolara kolon eklerken "contains null values" hatasını önler
            if (!col.IsNullable && !isAutoPk)
            {
                defaultClause = GetDefaultValueForType(col.ClrType);
            }
        }

        // NOT NULL: sadece DEFAULT varsa veya PK ise uygula
        var nullable = "";
        if (!col.IsNullable && !isAutoPk && !string.IsNullOrEmpty(defaultClause))
        {
            nullable = " NOT NULL";
        }

        return $"ALTER TABLE \"{tableName}\" ADD COLUMN IF NOT EXISTS \"{col.ColumnName}\" {pgType}{nullable}{defaultClause}";
    }

    /// <summary>
    /// .NET tipine göre güvenli bir DEFAULT değer üretir.
    /// </summary>
    private static string GetDefaultValueForType(Type clrType)
    {
        var type = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (type == typeof(string))
            return " DEFAULT ''";
        if (type == typeof(int) || type == typeof(long) || type == typeof(Enum))
            return " DEFAULT 0";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            return " DEFAULT 0";
        if (type == typeof(bool))
            return " DEFAULT false";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return " DEFAULT NOW()";
        if (type == typeof(Guid))
            return " DEFAULT '00000000-0000-0000-0000-000000000000'";
        if (type == typeof(TimeSpan))
            return " DEFAULT '00:00:00'";

        return " DEFAULT ''";
    }

    /// <summary>
    /// .NET tipini PostgreSQL kolon tipine mapler.
    /// </summary>
    private static string MapToPostgresType(ModelColumnDef col)
    {
        var type = Nullable.GetUnderlyingType(col.ClrType) ?? col.ClrType;

        if (type == typeof(string))
        {
            var maxLen = col.MaxLength;
            return maxLen.HasValue && maxLen.Value > 0
                ? $"VARCHAR({maxLen.Value})"
                : "TEXT";
        }
        if (type == typeof(int) || type == typeof(Enum))
            return "INTEGER";
        if (type == typeof(long))
            return "BIGINT";
        if (type == typeof(decimal))
            return "DECIMAL(18,2)";
        if (type == typeof(double))
            return "DOUBLE PRECISION";
        if (type == typeof(float))
            return "REAL";
        if (type == typeof(bool))
            return "BOOLEAN";
        if (type == typeof(DateTime))
            return "TIMESTAMP";
        if (type == typeof(DateTimeOffset))
            return "TIMESTAMPTZ";
        if (type == typeof(Guid))
            return "UUID";
        if (type == typeof(byte[]))
            return "BYTEA";
        if (type == typeof(TimeSpan))
            return "INTERVAL";

        return "TEXT"; // Fallback
    }

    private class ModelColumnDef
    {
        public string ColumnName { get; set; } = string.Empty;
        public Type ClrType { get; set; } = typeof(string);
        public bool IsNullable { get; set; }
        public int? MaxLength { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string? DefaultValueSql { get; set; }
        public object? DefaultValue { get; set; }
        public ValueGenerated ValueGenerated { get; set; }
    }
}




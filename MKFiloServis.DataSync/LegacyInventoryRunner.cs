using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace MKFiloServis.DataSync;

/// <summary>
/// Read-only preflight for legacy records. It reports record IDs, never mutates data.
/// A missing table/column makes the inventory incomplete rather than clean.
/// </summary>
internal static class LegacyInventoryRunner
{
    private sealed record Check(string Code, string Description, string Sql,
        params (string Table, string[] Columns)[] Required);

    private sealed record CheckResult(string Code, string Description, string Status,
        long? Count, List<object> SampleIds, string? Detail);

    private static readonly Check[] Checks =
    [
        new("A16-01", "Aktif araçta firma bağı eksik",
            "SELECT \"Id\" FROM \"Araclar\" WHERE \"IsDeleted\" = FALSE AND (\"FirmaId\" IS NULL OR \"FirmaId\" <= 0)",
            ("Araclar", ["Id", "IsDeleted", "FirmaId"])),
        new("A16-02", "Aynı firmada yinelenen aktif şase",
            "SELECT \"Id\" FROM (SELECT \"Id\", ROW_NUMBER() OVER (PARTITION BY \"FirmaId\", UPPER(TRIM(\"SaseNo\")) ORDER BY \"Id\") AS rn FROM \"Araclar\" WHERE \"IsDeleted\" = FALSE AND \"SaseNo\" IS NOT NULL AND TRIM(\"SaseNo\") <> '') AS ranked WHERE rn > 1",
            ("Araclar", ["Id", "FirmaId", "SaseNo", "IsDeleted"])),
        new("A16-03", "Aynı firmada yinelenen güncel plaka",
            "SELECT \"Id\" FROM (SELECT p.\"Id\", ROW_NUMBER() OVER (PARTITION BY a.\"FirmaId\", UPPER(TRIM(p.\"Plaka\")) ORDER BY p.\"Id\") AS rn FROM \"AracPlakalar\" p JOIN \"Araclar\" a ON a.\"Id\" = p.\"AracId\" WHERE p.\"IsDeleted\" = FALSE AND a.\"IsDeleted\" = FALSE AND (p.\"CikisTarihi\" IS NULL OR p.\"CikisTarihi\" > CURRENT_DATE) AND p.\"Plaka\" IS NOT NULL AND TRIM(p.\"Plaka\") <> '') AS ranked WHERE rn > 1",
            ("AracPlakalar", ["Id", "AracId", "Plaka", "CikisTarihi", "IsDeleted"]),
            ("Araclar", ["Id", "FirmaId", "IsDeleted"])),
        new("A16-04", "Araçtaki kiralık/komisyoncu cari başka firmada veya bulunamıyor",
            "SELECT DISTINCT a.\"Id\" FROM \"Araclar\" a LEFT JOIN \"Cariler\" k ON k.\"Id\" = a.\"KiralikCariId\" LEFT JOIN \"Cariler\" c ON c.\"Id\" = a.\"KomisyoncuCariId\" WHERE a.\"IsDeleted\" = FALSE AND (a.\"FirmaId\" IS NULL OR a.\"FirmaId\" <= 0 OR (a.\"KiralikCariId\" IS NOT NULL AND (a.\"KiralikCariId\" <= 0 OR k.\"Id\" IS NULL OR k.\"FirmaId\" IS NULL OR k.\"FirmaId\" <= 0 OR a.\"FirmaId\" <> k.\"FirmaId\")) OR (a.\"KomisyoncuCariId\" IS NOT NULL AND (a.\"KomisyoncuCariId\" <= 0 OR c.\"Id\" IS NULL OR c.\"FirmaId\" IS NULL OR c.\"FirmaId\" <= 0 OR a.\"FirmaId\" <> c.\"FirmaId\")))",
            ("Araclar", ["Id", "IsDeleted", "FirmaId", "KiralikCariId", "KomisyoncuCariId"]),
            ("Cariler", ["Id", "FirmaId"])),
        new("A16-05", "Banka hareketi hesabı geçersiz, yok veya farklı firmada",
            "SELECT b.\"Id\" FROM \"BankaKasaHareketleri\" b LEFT JOIN \"BankaHesaplari\" h ON h.\"Id\" = b.\"BankaHesapId\" WHERE b.\"IsDeleted\" = FALSE AND (b.\"BankaHesapId\" IS NULL OR b.\"BankaHesapId\" <= 0 OR h.\"Id\" IS NULL OR b.\"FirmaId\" IS NULL OR b.\"FirmaId\" <= 0 OR h.\"FirmaId\" IS NULL OR h.\"FirmaId\" <= 0 OR b.\"FirmaId\" <> h.\"FirmaId\")",
            ("BankaKasaHareketleri", ["Id", "IsDeleted", "BankaHesapId", "FirmaId"]),
            ("BankaHesaplari", ["Id", "FirmaId"])),
        new("A16-06", "Fatura ile cari firma bağı uyuşmuyor",
            "SELECT f.\"Id\" FROM \"Faturalar\" f LEFT JOIN \"Cariler\" c ON c.\"Id\" = f.\"CariId\" WHERE f.\"IsDeleted\" = FALSE AND (c.\"Id\" IS NULL OR f.\"FirmaId\" IS NULL OR f.\"FirmaId\" <= 0 OR c.\"FirmaId\" IS NULL OR c.\"FirmaId\" <= 0 OR f.\"FirmaId\" <> c.\"FirmaId\")",
            ("Faturalar", ["Id", "IsDeleted", "CariId", "FirmaId"]),
            ("Cariler", ["Id", "FirmaId"])),
        new("A16-07", "Aynı firma/personel/dönemde yinelenen maaş snapshot",
            "SELECT \"Id\" FROM (SELECT \"Id\", ROW_NUMBER() OVER (PARTITION BY \"FirmaId\", \"PersonelId\", \"Yil\", \"Ay\" ORDER BY \"Id\") AS rn FROM \"MaasOdemeSnapshotlar\" WHERE \"IsDeleted\" = FALSE) AS ranked WHERE rn > 1",
            ("MaasOdemeSnapshotlar", ["Id", "IsDeleted", "FirmaId", "PersonelId", "Yil", "Ay"])),
        new("A16-08", "Aynı araç/dönemde yinelenen maliyet snapshot",
            "SELECT \"Id\" FROM (SELECT \"Id\", ROW_NUMBER() OVER (PARTITION BY \"AracId\", \"Yil\", \"Ay\" ORDER BY \"Id\") AS rn FROM \"AracMaliyetSnapshotlari\" WHERE \"IsDeleted\" = FALSE) AS ranked WHERE rn > 1",
            ("AracMaliyetSnapshotlari", ["Id", "IsDeleted", "AracId", "Yil", "Ay"])),
        new("A16-09", "Aynı firmada birden çok varsayılan fatura şablonu",
            "SELECT \"Id\" FROM (SELECT \"Id\", ROW_NUMBER() OVER (PARTITION BY \"FirmaId\" ORDER BY \"Id\") AS rn FROM \"FaturaSablonlari\" WHERE \"IsDeleted\" = FALSE AND \"Aktif\" = TRUE AND \"Varsayilan\" = TRUE) AS ranked WHERE rn > 1",
            ("FaturaSablonlari", ["Id", "IsDeleted", "FirmaId", "Aktif", "Varsayilan"])),
        new("A16-10", "Aynı firma/kullanıcı için birden çok varsayılan grup şablonu",
            "SELECT \"Id\" FROM (SELECT \"Id\", ROW_NUMBER() OVER (PARTITION BY \"FirmaId\", \"KullaniciId\" ORDER BY \"Id\") AS rn FROM \"FaturaGrupSablonlari\" WHERE \"IsDeleted\" = FALSE AND \"VarsayilanMi\" = TRUE) AS ranked WHERE rn > 1",
            ("FaturaGrupSablonlari", ["Id", "IsDeleted", "FirmaId", "KullaniciId", "VarsayilanMi"])),
        new("A16-11", "Banka hareketi ile personel firma bağı uyuşmuyor",
            "SELECT h.\"Id\" FROM \"BankaKasaHareketleri\" h LEFT JOIN \"Soforler\" s ON s.\"Id\" = h.\"PersonelCebindenId\" WHERE h.\"PersonelCebindenId\" IS NOT NULL AND (s.\"Id\" IS NULL OR h.\"FirmaId\" IS NULL OR h.\"FirmaId\" <= 0 OR s.\"FirmaId\" IS NULL OR s.\"FirmaId\" <= 0 OR h.\"FirmaId\" <> s.\"FirmaId\")",
            ("BankaKasaHareketleri", ["Id", "FirmaId", "PersonelCebindenId"]),
            ("Soforler", ["Id", "FirmaId"])),
        new("A16-12", "Banka hareketi ile araç firma bağı uyuşmuyor",
            "SELECT h.\"Id\" FROM \"BankaKasaHareketleri\" h LEFT JOIN \"Araclar\" a ON a.\"Id\" = h.\"AracId\" WHERE h.\"AracId\" IS NOT NULL AND (a.\"Id\" IS NULL OR h.\"FirmaId\" IS NULL OR h.\"FirmaId\" <= 0 OR a.\"FirmaId\" IS NULL OR a.\"FirmaId\" <= 0 OR h.\"FirmaId\" <> a.\"FirmaId\")",
            ("BankaKasaHareketleri", ["Id", "FirmaId", "AracId"]),
            ("Araclar", ["Id", "FirmaId"])),
        new("A16-13", "Banka hareketi ile araç masrafı firma bağı uyuşmuyor",
            "SELECT h.\"Id\" FROM \"BankaKasaHareketleri\" h LEFT JOIN \"AracMasraflari\" m ON m.\"Id\" = h.\"AracMasrafId\" WHERE h.\"AracMasrafId\" IS NOT NULL AND (m.\"Id\" IS NULL OR h.\"FirmaId\" IS NULL OR h.\"FirmaId\" <= 0 OR m.\"FirmaId\" IS NULL OR m.\"FirmaId\" <= 0 OR h.\"FirmaId\" <> m.\"FirmaId\")",
            ("BankaKasaHareketleri", ["Id", "FirmaId", "AracMasrafId"]),
            ("AracMasraflari", ["Id", "FirmaId"])),
        new("A16-14", "Banka hareketinin mahsup/geri ödeme hareketi farklı firmada",
            "SELECT h.\"Id\" FROM \"BankaKasaHareketleri\" h LEFT JOIN \"BankaKasaHareketleri\" m ON m.\"Id\" = h.\"MahsupHareketId\" LEFT JOIN \"BankaKasaHareketleri\" r ON r.\"Id\" = h.\"PersonelGeriOdemeHareketId\" WHERE (h.\"MahsupHareketId\" IS NOT NULL AND (m.\"Id\" IS NULL OR h.\"FirmaId\" IS NULL OR h.\"FirmaId\" <= 0 OR m.\"FirmaId\" IS NULL OR m.\"FirmaId\" <= 0 OR h.\"FirmaId\" <> m.\"FirmaId\")) OR (h.\"PersonelGeriOdemeHareketId\" IS NOT NULL AND (r.\"Id\" IS NULL OR h.\"FirmaId\" IS NULL OR h.\"FirmaId\" <= 0 OR r.\"FirmaId\" IS NULL OR r.\"FirmaId\" <= 0 OR h.\"FirmaId\" <> r.\"FirmaId\"))",
            ("BankaKasaHareketleri", ["Id", "FirmaId", "MahsupHareketId", "PersonelGeriOdemeHareketId"])),
        new("A16-15", "Fatura ödeme eşleştirmesi banka hareketiyle aynı firmada değil",
            "SELECT m.\"Id\" FROM \"OdemeEslestirmeleri\" m LEFT JOIN \"Faturalar\" f ON f.\"Id\" = m.\"FaturaId\" LEFT JOIN \"BankaKasaHareketleri\" b ON b.\"Id\" = m.\"BankaKasaHareketId\" WHERE f.\"Id\" IS NULL OR b.\"Id\" IS NULL OR f.\"FirmaId\" IS NULL OR f.\"FirmaId\" <= 0 OR b.\"FirmaId\" IS NULL OR b.\"FirmaId\" <= 0 OR f.\"FirmaId\" <> b.\"FirmaId\"",
            ("OdemeEslestirmeleri", ["Id", "FaturaId", "BankaKasaHareketId"]),
            ("Faturalar", ["Id", "FirmaId"]),
            ("BankaKasaHareketleri", ["Id", "FirmaId"])),
        new("A16-16", "Banka hareketi carisi geçersiz, yok veya farklı firmada",
            "SELECT h.\"Id\" FROM \"BankaKasaHareketleri\" h LEFT JOIN \"Cariler\" c ON c.\"Id\" = h.\"CariId\" WHERE h.\"CariId\" IS NOT NULL AND (h.\"CariId\" <= 0 OR c.\"Id\" IS NULL OR h.\"FirmaId\" IS NULL OR h.\"FirmaId\" <= 0 OR c.\"FirmaId\" IS NULL OR c.\"FirmaId\" <= 0 OR h.\"FirmaId\" <> c.\"FirmaId\")",
            ("BankaKasaHareketleri", ["Id", "CariId", "FirmaId"]),
            ("Cariler", ["Id", "FirmaId"])),
        new("A16-17", "Banka hareketinin personel ödeme hesabı geçersiz, yok veya farklı firmada",
            "SELECT h.\"Id\" FROM \"BankaKasaHareketleri\" h LEFT JOIN \"BankaHesaplari\" p ON p.\"Id\" = h.\"PersonelOdemeHesapId\" WHERE h.\"PersonelOdemeHesapId\" IS NOT NULL AND (h.\"PersonelOdemeHesapId\" <= 0 OR p.\"Id\" IS NULL OR h.\"FirmaId\" IS NULL OR h.\"FirmaId\" <= 0 OR p.\"FirmaId\" IS NULL OR p.\"FirmaId\" <= 0 OR h.\"FirmaId\" <> p.\"FirmaId\")",
            ("BankaKasaHareketleri", ["Id", "PersonelOdemeHesapId", "FirmaId"]),
            ("BankaHesaplari", ["Id", "FirmaId"])),
        new("A16-18", "Maaş snapshot personeli eksik veya firma bağı uyuşmuyor",
            "SELECT m.\"Id\" FROM \"MaasOdemeSnapshotlar\" m LEFT JOIN \"Soforler\" s ON s.\"Id\" = m.\"PersonelId\" WHERE m.\"IsDeleted\" = FALSE AND (m.\"PersonelId\" IS NULL OR m.\"PersonelId\" <= 0 OR s.\"Id\" IS NULL OR m.\"FirmaId\" IS NULL OR m.\"FirmaId\" <= 0 OR s.\"FirmaId\" IS NULL OR s.\"FirmaId\" <= 0 OR m.\"FirmaId\" <> s.\"FirmaId\")",
            ("MaasOdemeSnapshotlar", ["Id", "IsDeleted", "PersonelId", "FirmaId"]),
            ("Soforler", ["Id", "FirmaId"]))
    ];

    public static async Task<int> RunAsync(string? source, string? provider, string? output,
        string? sourceEnvName)
    {
        if (!string.IsNullOrWhiteSpace(sourceEnvName))
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                Console.Error.WriteLine("--source ve --source-env birlikte kullanılamaz.");
                return 2;
            }
            source = Environment.GetEnvironmentVariable(sourceEnvName);
        }
        provider = provider?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(source) || provider is not ("sqlite" or "postgresql"))
        {
            Console.Error.WriteLine("inventory için --provider sqlite|postgresql ve --source gereklidir.");
            return 2;
        }

        try
        {
            if (provider == "sqlite" && !string.IsNullOrWhiteSpace(output) &&
                string.Equals(Path.GetFullPath(source), Path.GetFullPath(output),
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new InvalidOperationException("Rapor yolu SQLite kaynak dosyasıyla aynı olamaz.");

            await using DbConnection connection = provider == "sqlite"
                ? new SqliteConnection(new SqliteConnectionStringBuilder
                    { DataSource = Path.GetFullPath(source), Mode = SqliteOpenMode.ReadOnly }.ToString())
                : new NpgsqlConnection(source);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync(
                provider == "sqlite" ? IsolationLevel.Serializable : IsolationLevel.RepeatableRead);
            if (provider == "postgresql")
                await ExecuteAsync(connection, transaction, "SET TRANSACTION READ ONLY");

            var schema = await ReadSchemaAsync(connection, transaction, provider);
            var results = new List<CheckResult>();
            IReadOnlyList<Check> providerChecks = provider == "postgresql"
                ? await ReadPostgresForeignKeyChecksAsync(connection, transaction, schema)
                : [];
            var checks = Checks.Concat(providerChecks)
                .Concat(await ReadTenantBoundaryChecksAsync(connection, transaction, provider, schema))
                .ToArray();
            foreach (var check in checks)
            {
                var missing = check.Required.SelectMany(requirement =>
                    !schema.TryGetValue(requirement.Table, out var columns)
                        ? [$"{requirement.Table} tablosu"]
                        : requirement.Columns.Where(column => !columns.Contains(column))
                            .Select(column => $"{requirement.Table}.{column}"))
                    .ToArray();
                if (missing.Length > 0)
                {
                    results.Add(new(check.Code, check.Description, "schema_missing", null, [],
                        string.Join(", ", missing)));
                    continue;
                }

                try
                {
                    var count = await ScalarLongAsync(connection, transaction,
                        $"SELECT COUNT(*) FROM ({check.Sql}) AS findings");
                    var sample = await SampleIdsAsync(connection, transaction,
                        $"SELECT \"Id\" FROM ({check.Sql}) AS findings ORDER BY \"Id\" LIMIT 20");
                    results.Add(new(check.Code, check.Description,
                        count == 0 ? "clean" : "finding", count, sample, null));
                }
                catch (Exception ex)
                {
                    results.Add(new(check.Code, check.Description, "scan_error", null, [],
                        ex.Message));
                }
            }

            await transaction.CommitAsync();
            var incomplete = results.Any(result => result.Status is "schema_missing" or "scan_error");
            var report = new
            {
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                Provider = provider,
                ReadOnly = true,
                Complete = !incomplete,
                FindingCount = results.Sum(result => result.Count ?? 0),
                Checks = results
            };
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            if (string.IsNullOrWhiteSpace(output))
                Console.WriteLine(json);
            else
            {
                var reportPath = Path.GetFullPath(output);
                var temporaryPath = reportPath + ".writing-" + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    await File.WriteAllTextAsync(temporaryPath, json);
                    File.Move(temporaryPath, reportPath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }
            }
            return incomplete ? 3 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Envanter oluşturulamadı: {ex.Message}");
            return 1;
        }
    }

    private static async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, string sql)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> ScalarLongAsync(DbConnection connection, DbTransaction transaction, string sql)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = 120;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<List<object>> SampleIdsAsync(DbConnection connection, DbTransaction transaction, string sql)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = 120;
        var ids = new List<object>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) ids.Add(reader.GetValue(0));
        return ids;
    }

    private static async Task<Dictionary<string, HashSet<string>>> ReadSchemaAsync(
        DbConnection connection, DbTransaction transaction, string provider)
    {
        var tables = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (provider == "postgresql")
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = current_schema()";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var table = reader.GetString(0);
                if (!tables.TryGetValue(table, out var columns))
                    tables[table] = columns = new(StringComparer.OrdinalIgnoreCase);
                columns.Add(reader.GetString(1));
            }
        }
        else
        {
            var names = new List<string>();
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) names.Add(reader.GetString(0));
            }
            foreach (var name in names)
            {
                var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"PRAGMA table_info(\"{name.Replace("\"", "\"\"")}\")";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
                tables[name] = columns;
            }
        }
        return tables;
    }

    private sealed record TenantForeignKey(string SourceTable, string ConstraintName, int Ordinal,
        string SourceColumn, string TargetTable, string TargetColumn);
    private sealed record ForeignKeyColumn(string SourceSchema, string SourceTable,
        string ConstraintName, string TargetSchema, string TargetTable,
        string SourceColumn, string TargetColumn, string MatchOption, int Ordinal);

    /// <summary>
    /// Adds a review finding for each source table whose simple FK targets an Id column
    /// in another tenant-owned table. It deliberately reports mismatches; it never repairs.
    /// </summary>
    private static async Task<IReadOnlyList<Check>> ReadTenantBoundaryChecksAsync(
        DbConnection connection, DbTransaction transaction, string provider,
        IReadOnlyDictionary<string, HashSet<string>> schema)
    {
        var foreignKeys = new List<TenantForeignKey>();
        var checks = new List<Check>();
        var sqliteTablesWithForeignKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (provider == "postgresql")
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                SELECT src.table_name, src.constraint_name, src.ordinal_position,
                       src.column_name, target.table_name, target.column_name
                FROM information_schema.table_constraints constraint_row
                JOIN information_schema.key_column_usage src
                  ON src.constraint_catalog = constraint_row.constraint_catalog
                 AND src.constraint_schema = constraint_row.constraint_schema
                 AND src.constraint_name = constraint_row.constraint_name
                 AND src.table_schema = constraint_row.table_schema
                JOIN information_schema.referential_constraints rc
                  ON rc.constraint_catalog = constraint_row.constraint_catalog
                 AND rc.constraint_schema = constraint_row.constraint_schema
                 AND rc.constraint_name = constraint_row.constraint_name
                JOIN information_schema.key_column_usage target
                  ON target.constraint_catalog = rc.unique_constraint_catalog
                 AND target.constraint_schema = rc.unique_constraint_schema
                 AND target.constraint_name = rc.unique_constraint_name
                 AND target.ordinal_position = src.position_in_unique_constraint
                WHERE constraint_row.constraint_type = 'FOREIGN KEY'
                  AND constraint_row.table_schema = current_schema()
                  AND target.table_schema = current_schema()
                ORDER BY src.table_name, src.constraint_name, src.ordinal_position
                """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                foreignKeys.Add(new(reader.GetString(0), reader.GetString(1), reader.GetInt32(2),
                    reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        }
        else
        {
            foreach (var table in schema.Keys)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"PRAGMA foreign_key_list({QuoteIdentifier(table)})";
                await using var reader = await command.ExecuteReaderAsync();
                var tableForeignKeys = new List<(long ConstraintId, int Ordinal, TenantForeignKey ForeignKey)>();
                while (await reader.ReadAsync())
                {
                    if (reader.IsDBNull(3) || reader.IsDBNull(2) || reader.IsDBNull(4))
                        continue;
                    var constraintId = reader.GetInt64(0);
                    var ordinal = reader.GetInt32(1);
                    tableForeignKeys.Add((constraintId, ordinal, new(table, constraintId.ToString(), ordinal,
                        reader.GetString(3), reader.GetString(2), reader.GetString(4))));
                }
                if (tableForeignKeys.Count > 0)
                    sqliteTablesWithForeignKeys.Add(table);
                foreignKeys.AddRange(tableForeignKeys.GroupBy(item => item.ConstraintId)
                    .SelectMany(group => group.OrderBy(item => item.Ordinal).Select(item => item.ForeignKey)));
            }

            var tablesWithForeignKeys = sqliteTablesWithForeignKeys
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
            var sqliteCheckIndex = 1;
            foreach (var table in tablesWithForeignKeys)
            {
                var literal = table.Replace("'", "''", StringComparison.Ordinal);
                checks.Add(new($"A16-SQLITE-FK-{sqliteCheckIndex++:D3}",
                    $"Foreign key kayıt ihlali: {table} (rowid -1 ise kimlik alınamadı)",
                    $"SELECT COALESCE(rowid, -1) AS \"Id\" FROM pragma_foreign_key_check('{literal}')",
                    (table, [])));
            }
        }

        var eligible = foreignKeys
            .Where(fk => schema.TryGetValue(fk.SourceTable, out var sourceColumns)
                && sourceColumns.Contains("Id") && sourceColumns.Contains("FirmaId")
                && sourceColumns.Contains(fk.SourceColumn)
                && schema.TryGetValue(fk.TargetTable, out var targetColumns)
                && targetColumns.Contains("FirmaId") && targetColumns.Contains(fk.TargetColumn))
            .GroupBy(fk => (fk.SourceTable, fk.ConstraintName, fk.TargetTable))
            .GroupBy(group => group.Key.SourceTable, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        var index = 1;
        foreach (var group in eligible)
        {
            var conditions = group.Select(foreignKey =>
            {
                var keyColumns = foreignKey.OrderBy(fk => fk.Ordinal).ToArray();
                var sourceValuesPresent = string.Join(" AND ", keyColumns.Select(fk =>
                    $"s.{QuoteIdentifier(fk.SourceColumn)} IS NOT NULL"));
                var targetMatches = string.Join(" AND ", keyColumns.Select(fk =>
                    $"p.{QuoteIdentifier(fk.TargetColumn)} = s.{QuoteIdentifier(fk.SourceColumn)}"));
                var targetTable = $"{QuoteIdentifier(foreignKey.Key.TargetTable)} p";
                return $"(({sourceValuesPresent}) AND EXISTS (SELECT 1 FROM {targetTable} WHERE {targetMatches} " +
                       "AND (s.\"FirmaId\" IS NULL OR p.\"FirmaId\" IS NULL OR s.\"FirmaId\" <> p.\"FirmaId\")))";
            }).Distinct(StringComparer.Ordinal).ToArray();
            if (conditions.Length == 0) continue;

            var sourceTable = QuoteIdentifier(group.Key);
            var sql = $"SELECT s.\"Id\" AS \"Id\" FROM {sourceTable} s WHERE {string.Join(" OR ", conditions)}";
            var targetNames = group.Select(fk => fk.Key.TargetTable)
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name);
            checks.Add(new($"A16-TENANT-{index++:D3}",
                $"Firma bağı uyuşmazlığı inceleme adayı: {group.Key} → {string.Join(", ", targetNames)}",
                sql, (group.Key, ["Id", "FirmaId"])));
        }
        return checks;
    }

    /// <summary>
    /// Discovers PostgreSQL FK constraints, including composite keys, and reports
    /// referencing rows that violate them inside the caller's read-only snapshot.
    /// </summary>
    private static async Task<IReadOnlyList<Check>> ReadPostgresForeignKeyChecksAsync(
        DbConnection connection, DbTransaction transaction,
        IReadOnlyDictionary<string, HashSet<string>> schema)
    {
        var columns = new List<ForeignKeyColumn>();
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                SELECT src.table_schema, src.table_name, src.constraint_name,
                       target.table_schema, target.table_name, src.column_name,
                       target.column_name, rc.match_option,
                       src.ordinal_position
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage src
                  ON src.constraint_catalog = tc.constraint_catalog
                 AND src.constraint_schema = tc.constraint_schema
                 AND src.constraint_name = tc.constraint_name
                 AND src.table_schema = tc.table_schema
                 AND src.table_name = tc.table_name
                JOIN information_schema.referential_constraints rc
                  ON rc.constraint_catalog = tc.constraint_catalog
                 AND rc.constraint_schema = tc.constraint_schema
                 AND rc.constraint_name = tc.constraint_name
                JOIN information_schema.key_column_usage target
                  ON target.constraint_catalog = rc.unique_constraint_catalog
                 AND target.constraint_schema = rc.unique_constraint_schema
                 AND target.constraint_name = rc.unique_constraint_name
                 AND target.ordinal_position = src.position_in_unique_constraint
                WHERE tc.constraint_type = 'FOREIGN KEY'
                  AND tc.table_schema = current_schema()
                  AND target.table_schema = current_schema()
                ORDER BY src.table_schema, src.table_name, src.constraint_name,
                         src.ordinal_position
                """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                columns.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                    reader.GetString(7), reader.GetInt32(8)));
        }

        var checks = new List<Check>();
        var index = 1;
        foreach (var group in columns.GroupBy(column =>
                     (column.SourceSchema, column.SourceTable, column.ConstraintName,
                      column.TargetSchema, column.TargetTable)))
        {
            var ordered = group.OrderBy(column => column.Ordinal).ToArray();
            if (ordered.Length == 0 || !schema.TryGetValue(ordered[0].SourceTable, out var sourceColumns)
                || !schema.ContainsKey(ordered[0].TargetTable))
                continue;

            var pairs = ordered.Select(column =>
                (Source: QuoteIdentifier(column.SourceColumn), Target: QuoteIdentifier(column.TargetColumn)))
                .ToArray();
            var allPresent = string.Join(" AND ", pairs.Select(pair => $"s.{pair.Source} IS NOT NULL"));
            var anyPresent = string.Join(" OR ", pairs.Select(pair => $"s.{pair.Source} IS NOT NULL"));
            var matches = string.Join(" AND ", pairs.Select(pair => $"t.{pair.Target} = s.{pair.Source}"));
            var sourceTable = $"{QuoteIdentifier(ordered[0].SourceSchema)}.{QuoteIdentifier(ordered[0].SourceTable)}";
            var targetTable = $"{QuoteIdentifier(ordered[0].TargetSchema)}.{QuoteIdentifier(ordered[0].TargetTable)}";
            var invalidReference = $"(({allPresent}) AND NOT EXISTS (SELECT 1 FROM {targetTable} t WHERE {matches}))";
            if (string.Equals(ordered[0].MatchOption, "FULL", StringComparison.OrdinalIgnoreCase)
                && pairs.Length > 1)
                invalidReference = $"({invalidReference} OR (({anyPresent}) AND NOT ({allPresent})))";

            var idColumn = sourceColumns.FirstOrDefault(name =>
                string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase));
            var sampleIdentity = idColumn is not null
                ? $"s.{QuoteIdentifier(idColumn)}"
                : "-1::bigint";
            var sql = $"SELECT {sampleIdentity} AS \"Id\" FROM {sourceTable} s WHERE {invalidReference}";
            checks.Add(new($"A16-PG-FK-{index++:D3}",
                $"Foreign key kayıt ihlali: {ordered[0].SourceTable}.{ordered[0].ConstraintName} " +
                $"→ {ordered[0].TargetTable} (Id yoksa örnek -1)",
                sql, (ordered[0].SourceTable, [])));
        }
        return checks;
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}

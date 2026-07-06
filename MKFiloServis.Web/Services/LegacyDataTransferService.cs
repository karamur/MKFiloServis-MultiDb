using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Legacy veritabanından MKFiloServis veritabanına veri aktarım servisi.
/// Talimat Bölüm 16-23: Legacy DB READ ONLY, FirmaId=1 varsayılan.
/// </summary>
public class LegacyDataTransferService
{
    private readonly string _sourceConnStr;
    private readonly string _targetConnStr;
    private readonly ILogger<LegacyDataTransferService> _logger;

    public LegacyDataTransferService(
        IConfiguration configuration,
        ILogger<LegacyDataTransferService> logger)
    {
        _logger = logger;
        _targetConnStr = configuration.GetConnectionString("DefaultConnection")!;

        // Öncelik: appsettings'te açıkça verilen LegacySourceConnection.
        // Yoksa DefaultConnection'dan türet: MKFiloServis -> KOAFiloServis.
        _sourceConnStr = configuration.GetConnectionString("LegacySourceConnection")
            ?? BuildLegacySourceConnectionString(_targetConnStr);

        // Kaynak ve hedef aynıysa aktarım metotları sessizce atlanır; burada log üretilmez.
    }

    private static string BuildLegacySourceConnectionString(string targetConnStr)
    {
        var builder = new NpgsqlConnectionStringBuilder(targetConnStr);
        var targetDb = builder.Database ?? string.Empty;

        if (targetDb.Contains("MKFiloServis", StringComparison.OrdinalIgnoreCase))
        {
            builder.Database = targetDb.Replace("MKFiloServis", "KOAFiloServis", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // Fallback: hedef adı MK pattern'i taşımıyorsa yine de legacy varsayılan DB adına dön.
            builder.Database = "KOAFiloServis";
        }

        return builder.ConnectionString;
    }

    private static bool IsSameDatabase(string sourceConnStr, string targetConnStr)
    {
        var source = new NpgsqlConnectionStringBuilder(sourceConnStr);
        var target = new NpgsqlConnectionStringBuilder(targetConnStr);

        return string.Equals(source.Host, target.Host, StringComparison.OrdinalIgnoreCase)
               && source.Port == target.Port
               && string.Equals(source.Database, target.Database, StringComparison.OrdinalIgnoreCase)
               && string.Equals(source.Username, target.Username, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Hedef veritabanı şemasını hazırlar.
    /// 1) Model create script'inden eksik tabloları tamamlamayı dener
    /// 2) Son olarak EnsureCreated fallback'i çalıştırır
    /// </summary>
    public async Task EnsureSchemaAsync()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(_targetConnStr);
        using var ctx = new ApplicationDbContext(optionsBuilder.Options);

        await EnsureSchemaFromModelScriptAsync(ctx);

        _logger.LogInformation("EnsureCreated basliyor...");
        await ctx.Database.EnsureCreatedAsync();
        _logger.LogInformation("EnsureCreated tamamlandi.");
    }

    private async Task EnsureSchemaFromModelScriptAsync(ApplicationDbContext ctx)
    {
        try
        {
            var script = ctx.Database.GenerateCreateScript();
            if (string.IsNullOrWhiteSpace(script))
                return;

            // PostgreSQL'de idempotent hale getir
            script = script
                .Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ", StringComparison.OrdinalIgnoreCase)
                .Replace("CREATE INDEX ", "CREATE INDEX IF NOT EXISTS ", StringComparison.OrdinalIgnoreCase)
                .Replace("CREATE UNIQUE INDEX ", "CREATE UNIQUE INDEX IF NOT EXISTS ", StringComparison.OrdinalIgnoreCase);

            var commands = script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var executed = 0;

            foreach (var cmdText in commands)
            {
                if (string.IsNullOrWhiteSpace(cmdText))
                    continue;

                // Model create script'indeki seed INSERT'leri FK ihlali üretebilir.
                // Burada yalnızca şema (DDL) komutlarını çalıştırıyoruz.
                var normalized = cmdText.TrimStart();
                if (!(normalized.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase)
                      || normalized.StartsWith("CREATE INDEX", StringComparison.OrdinalIgnoreCase)
                      || normalized.StartsWith("CREATE UNIQUE INDEX", StringComparison.OrdinalIgnoreCase)
                      || normalized.StartsWith("ALTER TABLE", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                try
                {
                    await ctx.Database.ExecuteSqlRawAsync(cmdText + ";");
                    executed++;
                }
                catch (PostgresException ex) when (
                    ex.SqlState == "42P07" || // duplicate_table
                    ex.SqlState == "42710" || // duplicate_object
                    ex.SqlState == "42701" || // duplicate_column
                    ex.SqlState == "23505" || // unique_violation
                    ex.SqlState == "23503")   // foreign_key_violation (fallback'te constraint validate hataları)
                {
                    // idempotent/uyumluluk çakışmalarını geç
                }
            }

            _logger.LogInformation("Schema hazirlik: model script DDL komutlari calistirildi ({Count}).", executed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Schema hazirlik: model script fallback adimi basarisiz.");
        }
    }

    /// <summary>
    /// Tüm verileri sırayla aktarır (Talimat Bölüm 21).
    /// Önce kritik bağımlı tablolar, sonra kaynak DB'deki TÜM diğer tablolar otomatik aktarılır.
    /// </summary>
    public async Task<TransferResult> TransferAllAsync()
    {
        var result = new TransferResult();
        var firmaIdVarsayilan = 1;

        if (IsSameDatabase(_sourceConnStr, _targetConnStr))
        {
            return result;
        }

        _logger.LogInformation("=== Veri aktarimi basladi ===");
        _logger.LogInformation("LegacyDataTransfer source DB: {SourceDb}", new NpgsqlConnectionStringBuilder(_sourceConnStr).Database);
        _logger.LogInformation("LegacyDataTransfer target DB: {TargetDb}", new NpgsqlConnectionStringBuilder(_targetConnStr).Database);

        if (!await SourceDatabaseExistsAsync())
        {
            _logger.LogInformation("LegacyDataTransfer source DB bulunamadi, aktarim atlandi.");
            return result;
        }

        async Task<TransferResult> SafeTransferAsync(Func<Task<TransferResult>> transfer, string name)
        {
            try { return await transfer(); }
            catch (PostgresException ex) when (ex.SqlState == "42P01")
            { _logger.LogInformation("{Name}: kaynak tablo yok, atlandi", name); return new(); }
            catch (Exception ex)
            { _logger.LogWarning(ex, "{Name}: aktarim hatasi", name); return new(); }
        }

        // ── Öncelikli kritik tablolar (FK bağımlılık sırası, generic) ──
        result.Add(await SafeTransferAsync(() => TransferSimpleWithFirmaIdAsync("Organizasyonlar", firmaIdVarsayilan), "Organizasyonlar"));
        result.Add(await SafeTransferAsync(() => TransferSimpleWithFirmaIdAsync("Firmalar", firmaIdVarsayilan), "Firmalar"));
        result.Add(await SafeTransferAsync(TransferRollerAsync, "Roller"));
        result.Add(await SafeTransferAsync(TransferKullanicilarAsync, "Kullanicilar"));
        result.Add(await SafeTransferAsync(TransferRolYetkileriAsync, "RolYetkileri"));
        // Cariler — generic transfer (ortak kolonlar otomatik keşfedilir)
        result.Add(await SafeTransferAsync(() => TransferSimpleWithFirmaIdAsync("Cariler", firmaIdVarsayilan), "Cariler"));
        result.Add(await SafeTransferAsync(TransferMuhasebeHesaplariAsync, "MuhasebeHesaplari"));

        // ── Kaynak DB'deki TÜM diğer tablolar (otomatik keşif) ────────
        var allSourceTables = await DiscoverSourceTablesAsync();
        var skipTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "__EFMigrationsHistory", "Organizasyonlar", "Firmalar", "Roller",
            "Kullanicilar", "RolYetkileri", "Cariler", "Sirketler",
            "MuhasebeHesaplari",
            "SirketTransferLoglari", "PlanlamaKayitlar", "Randevular",
            "KullaniciBildirimleri", "KullaniciMesajlari", "KullaniciOturumlari",
            "MesajKonusmalari", "MailGonderimleri", "ModulYetkileri",
            "FisNoCounters", // sistem tablosu, seed ile oluşacak
        };

        foreach (var tableName in allSourceTables.Where(t => !skipTables.Contains(t)))
        {
            result.Add(await SafeTransferAsync(
                () => TransferSimpleWithFirmaIdAsync(tableName, firmaIdVarsayilan), tableName));
        }

        _logger.LogInformation("=== Veri aktarimi tamamlandi: {Total} kayit, {Tables} tablo ===",
            result.TotalTransferred, result.TableCount);
        return result;
    }

    private async Task<List<string>> DiscoverSourceTablesAsync()
    {
        using var source = await OpenSourceAsync();
        using var cmd = new NpgsqlCommand(
            @"SELECT table_name FROM information_schema.tables
              WHERE table_schema='public' AND table_type='BASE TABLE'
              ORDER BY table_name", source);
        var tables = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));
        return tables;
    }

    // ── Manuel transfer metodları (özel conflict handling gerektirenler) ─

    private async Task<TransferResult> TransferRollerAsync()
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        using var cmd = new NpgsqlCommand(
            @"SELECT ""Id"", ""RolAdi"", ""Aciklama"", ""Renk"", ""SistemRolu"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""
              FROM ""Roller""", source);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            try
            {
                await InsertIfNotExistsAsync(target,
                    @"INSERT INTO ""Roller"" (""Id"", ""RolAdi"", ""Aciklama"", ""Renk"", ""SistemRolu"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"")
                      VALUES (@id,@ad,@aciklama,@renk,@sistem,@isdel,@ca,@ua)
                      ON CONFLICT (""RolAdi"") DO UPDATE SET
                        ""Aciklama"" = EXCLUDED.""Aciklama"",
                        ""Renk"" = EXCLUDED.""Renk"",
                        ""SistemRolu"" = EXCLUDED.""SistemRolu"",
                        ""IsDeleted"" = EXCLUDED.""IsDeleted"",
                        ""UpdatedAt"" = EXCLUDED.""UpdatedAt""",
                    new NpgsqlParameter("@id", reader.GetInt32(0)),
                    new NpgsqlParameter("@ad", reader.GetString(1)),
                    new NpgsqlParameter("@aciklama", reader.IsDBNull(2) ? DBNull.Value : reader.GetString(2)),
                    new NpgsqlParameter("@renk", reader.IsDBNull(3) ? DBNull.Value : reader.GetString(3)),
                    new NpgsqlParameter("@sistem", reader.GetBoolean(4)),
                    new NpgsqlParameter("@isdel", reader.GetBoolean(5)),
                    new NpgsqlParameter("@ca", reader.GetDateTime(6)),
                    new NpgsqlParameter("@ua", reader.IsDBNull(7) ? DBNull.Value : reader.GetDateTime(7)));
                result.Transferred++;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                // Farklı unique constraint (ör. Roller_pkey) çakışmalarında kayıt zaten mevcut kabul edilir.
            }
        }
        _logger.LogInformation("Roller: {Count} kayit", result.Transferred);
        return result;
    }

    private async Task<TransferResult> TransferKullanicilarAsync()
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        using var cmd = new NpgsqlCommand(
            @"SELECT ""Id"", ""KullaniciAdi"", ""AdSoyad"", ""SifreHash"", ""Email"", ""RolId"",
                     ""Aktif"", ""Kilitli"", ""BasarisizGirisSayisi"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""
              FROM ""Kullanicilar""", source);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            await InsertIfNotExistsAsync(target,
                @"INSERT INTO ""Kullanicilar"" (""Id"", ""KullaniciAdi"", ""AdSoyad"", ""SifreHash"", ""Email"", ""RolId"",
                  ""Aktif"", ""Kilitli"", ""BasarisizGirisSayisi"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"")
                  VALUES (@id,@ka,@ad,@sh,@em,@rid,@aktif,@kilit,@bgs,@isdel,@ca,@ua)
                  ON CONFLICT (""Id"") DO NOTHING",
                new NpgsqlParameter("@id", reader.GetInt32(0)),
                new NpgsqlParameter("@ka", reader.GetString(1)),
                new NpgsqlParameter("@ad", reader.GetString(2)),
                new NpgsqlParameter("@sh", reader.GetString(3)),
                new NpgsqlParameter("@em", reader.IsDBNull(4) ? DBNull.Value : reader.GetString(4)),
                new NpgsqlParameter("@rid", reader.GetInt32(5)),
                new NpgsqlParameter("@aktif", reader.GetBoolean(6)),
                new NpgsqlParameter("@kilit", reader.GetBoolean(7)),
                new NpgsqlParameter("@bgs", reader.GetInt32(8)),
                new NpgsqlParameter("@isdel", reader.GetBoolean(9)),
                new NpgsqlParameter("@ca", reader.GetDateTime(10)),
                new NpgsqlParameter("@ua", reader.IsDBNull(11) ? DBNull.Value : reader.GetDateTime(11)));
            result.Transferred++;
        }
        _logger.LogInformation("Kullanicilar: {Count} kayit", result.Transferred);
        return result;
    }

    private async Task<TransferResult> TransferRolYetkileriAsync()
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        using var cmd = new NpgsqlCommand(
            @"SELECT ""Id"", ""RolId"", ""YetkiKodu"", ""Izin"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""
              FROM ""RolYetkileri""", source);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            await InsertIfNotExistsAsync(target,
                @"INSERT INTO ""RolYetkileri"" (""Id"", ""RolId"", ""YetkiKodu"", ""Izin"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"")
                  VALUES (@id,@rid,@yk,@izin,@isdel,@ca,@ua)
                  ON CONFLICT (""Id"") DO NOTHING",
                new NpgsqlParameter("@id", reader.GetInt32(0)),
                new NpgsqlParameter("@rid", reader.GetInt32(1)),
                new NpgsqlParameter("@yk", reader.GetString(2)),
                new NpgsqlParameter("@izin", reader.GetBoolean(3)),
                new NpgsqlParameter("@isdel", reader.GetBoolean(4)),
                new NpgsqlParameter("@ca", reader.GetDateTime(5)),
                new NpgsqlParameter("@ua", reader.IsDBNull(6) ? DBNull.Value : reader.GetDateTime(6)));
            result.Transferred++;
        }
        _logger.LogInformation("RolYetkileri: {Count} kayit", result.Transferred);
        return result;
    }

    private async Task<TransferResult> TransferMuhasebeHesaplariAsync()
    {
        const string tableName = "MuhasebeHesaplari";

        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        var sourceCols = await GetColumnNamesAsync(source, tableName);
        if (sourceCols.Count == 0) return result;

        var targetCols = await GetColumnNamesAsync(target, tableName);
        if (targetCols.Count == 0)
        {
            _logger.LogInformation("{Table}: hedef tablo yok, atlandi", tableName);
            return result;
        }

        if (!sourceCols.Contains("Id", StringComparer.OrdinalIgnoreCase) ||
            !sourceCols.Contains("HesapKodu", StringComparer.OrdinalIgnoreCase) ||
            !targetCols.Contains("HesapKodu", StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("{Table}: zorunlu kolonlar bulunamadi, generic aktarim deneniyor", tableName);
            return await TransferSimpleWithFirmaIdAsync(tableName, 1);
        }

        var targetColsByName = targetCols.ToDictionary(c => c, c => c, StringComparer.OrdinalIgnoreCase);
        var commonCols = sourceCols
            .Where(c => !c.Equals("UstHesapId", StringComparison.OrdinalIgnoreCase))
            .Where(c => targetColsByName.ContainsKey(c))
            .Select(c => (sourceName: c, targetName: targetColsByName[c]))
            .ToList();

        var sourceColList = string.Join(", ", sourceCols.Select(QuoteIdentifier));
        var rows = new List<Dictionary<string, object?>>();
        var legacyIdToKod = new Dictionary<int, string>();

        using (var cmd = new NpgsqlCommand($"SELECT {sourceColList} FROM {QuoteIdentifier(tableName)}", source))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < sourceCols.Count; i++)
                    row[sourceCols[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                rows.Add(row);

                if (TryGetInt(row, "Id", out var id) && row.TryGetValue("HesapKodu", out var kodValue) && kodValue is string kod)
                    legacyIdToKod[id] = kod;
            }
        }

        foreach (var row in rows)
        {
            try
            {
                await UpsertMuhasebeHesapAsync(target, commonCols, row);
                result.Transferred++;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505" && commonCols.Any(c => c.targetName.Equals("Id", StringComparison.OrdinalIgnoreCase)))
            {
                // Hedefte seed ile ayni Id baska bir hesapta olabilir. Bu durumda Id'yi hedef DB uretsin,
                // sonraki parent baglantisini HesapKodu uzerinden kuracagiz.
                var withoutId = commonCols
                    .Where(c => !c.targetName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                await UpsertMuhasebeHesapAsync(target, withoutId, row);
                result.Transferred++;
            }
        }

        var parentUpdates = 0;
        foreach (var row in rows)
        {
            if (!row.TryGetValue("HesapKodu", out var kodValue) || kodValue is not string childKod)
                continue;

            if (!TryGetInt(row, "UstHesapId", out var legacyParentId))
            {
                using var clearCmd = new NpgsqlCommand(
                    $@"UPDATE {QuoteIdentifier(tableName)}
                       SET ""UstHesapId"" = NULL
                       WHERE ""HesapKodu"" = @childKod AND ""UstHesapId"" IS NOT NULL", target);
                clearCmd.Parameters.AddWithValue("@childKod", childKod);
                parentUpdates += await clearCmd.ExecuteNonQueryAsync();
                continue;
            }

            if (!legacyIdToKod.TryGetValue(legacyParentId, out var parentKod))
            {
                _logger.LogWarning("{Table}: {HesapKodu} icin legacy parent bulunamadi: {ParentId}", tableName, childKod, legacyParentId);
                continue;
            }

            using var updateCmd = new NpgsqlCommand(
                $@"UPDATE {QuoteIdentifier(tableName)} AS child
                   SET ""UstHesapId"" = parent.""Id""
                   FROM {QuoteIdentifier(tableName)} AS parent
                   WHERE child.""HesapKodu"" = @childKod
                     AND parent.""HesapKodu"" = @parentKod
                     AND child.""UstHesapId"" IS DISTINCT FROM parent.""Id""", target);
            updateCmd.Parameters.AddWithValue("@childKod", childKod);
            updateCmd.Parameters.AddWithValue("@parentKod", parentKod);
            parentUpdates += await updateCmd.ExecuteNonQueryAsync();
        }

        await ResetSequenceAsync(target, tableName);

        _logger.LogInformation("{Table}: {Count} kayit, {ParentUpdates} ust hesap baglantisi", tableName, result.Transferred, parentUpdates);
        return result;
    }

    /// <summary>
    /// Kaynak ve hedef tablodaki ORTAK kolonları keşfeder, sadece onları transfer eder.
    /// Hedefte FirmaId varsa ekler, yoksa eklemez.
    /// Kolon uyuşmazlıklarında hata vermez — sadece ortak kolonları aktarır.
    /// </summary>
    private async Task<TransferResult> TransferSimpleWithFirmaIdAsync(string tableName, int firmaId)
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        try
        {
            // Kaynak kolonlar
            var sourceCols = new List<string>();
            using (var sc = new NpgsqlCommand(
                $@"SELECT column_name FROM information_schema.columns
                   WHERE table_schema='public' AND table_name='{tableName}' ORDER BY ordinal_position", source))
            using (var r = await sc.ExecuteReaderAsync())
                while (await r.ReadAsync()) sourceCols.Add(r.GetString(0));

            if (sourceCols.Count == 0) return result;

            // Hedef kolonlar (case-insensitive lookup + gerçek adıyla map)
            var targetColsLower = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var tc = new NpgsqlCommand(
                $@"SELECT column_name FROM information_schema.columns
                   WHERE table_schema='public' AND table_name='{tableName}'", target))
            using (var r = await tc.ExecuteReaderAsync())
                while (await r.ReadAsync()) { var n = r.GetString(0); targetColsLower[n] = n; }

            if (targetColsLower.Count == 0)
            {
                // Kaynak DB'deki bu tablo hedef şemada bulunmuyor; beklenen bir durum.
                _logger.LogInformation("{Table}: hedef tablo yok, atlandi", tableName);
                return result;
            }

            // Ortak kolonlar — HEDEFin gerçek adıyla (PostgreSQL case-sensitive)
            var commonCols = new List<(string sourceName, string targetName)>();
            foreach (var sc in sourceCols)
            {
                if (targetColsLower.TryGetValue(sc, out var tn))
                    commonCols.Add((sc, tn));
            }

            if (commonCols.Count == 0)
            {
                _logger.LogWarning("{Table}: ortak kolon yok, atlandi", tableName);
                return result;
            }

            // Hedefte FirmaId varsa ve kaynakta yoksa ekle
            var hasFirmaIdInTarget = targetColsLower.ContainsKey("FirmaId");
            var hasFirmaIdInSource = sourceCols.Contains("FirmaId", StringComparer.OrdinalIgnoreCase);
            var addFirmaId = hasFirmaIdInTarget && !hasFirmaIdInSource;

            var sourceColList = string.Join(", ", commonCols.Select(c => $"\"{c.sourceName}\""));
            var targetColList = string.Join(", ", commonCols.Select(c => $"\"{c.targetName}\""));
            if (addFirmaId) targetColList += ", \"FirmaId\"";

            using var cmd = new NpgsqlCommand($"SELECT {sourceColList} FROM \"{tableName}\"", source);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var parms = new List<NpgsqlParameter>();
                var values = new List<string>();
                for (int i = 0; i < commonCols.Count; i++)
                {
                    var name = $"@p{i}";
                    values.Add(name);
                    parms.Add(new NpgsqlParameter(name, reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i)));
                }
                if (addFirmaId)
                {
                    values.Add("@fid");
                    parms.Add(new NpgsqlParameter("@fid", firmaId));
                }

                try
                {
                    await InsertIfNotExistsAsync(target,
                        $"INSERT INTO \"{tableName}\" ({targetColList}) VALUES ({string.Join(",", values)}) ON CONFLICT (\"Id\") DO NOTHING",
                        parms.ToArray());
                    result.Transferred++;
                }
                catch (PostgresException ex) when (ex.SqlState == "23505") { /* duplicate */ }
                catch (PostgresException ex) when (ex.SqlState == "42703")
                {
                    _logger.LogWarning("{Table}: kolon uyusmazligi — {Msg}, ilk hatada durduruldu", tableName, ex.MessageText);
                    break; // Kolon hatası tekrar edecek, döngüyü kır
                }
            }
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            _logger.LogInformation("{Table}: kaynak tablo yok", tableName);
        }

        _logger.LogInformation("{Table}: {Count} kayit", tableName, result.Transferred);
        return result;
    }

    // ── Yardımcılar ──────────────────────────────────────────────────

    private async Task<bool> SourceDatabaseExistsAsync()
    {
        try
        {
            var sourceBuilder = new NpgsqlConnectionStringBuilder(_sourceConnStr);
            var probeBuilder = new NpgsqlConnectionStringBuilder(_sourceConnStr)
            {
                Database = string.IsNullOrWhiteSpace(sourceBuilder.Database) ? "postgres" : "postgres"
            };

            await using var conn = new NpgsqlConnection(probeBuilder.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @db", conn);
            cmd.Parameters.AddWithValue("@db", sourceBuilder.Database ?? string.Empty);
            return await cmd.ExecuteScalarAsync() != null;
        }
        catch (PostgresException ex) when (ex.SqlState == "3D000")
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LegacyDataTransfer source DB kontrolu basarisiz oldu.");
            return false;
        }
    }

    private async Task<NpgsqlConnection> OpenSourceAsync()
    {
        var conn = new NpgsqlConnection(_sourceConnStr);
        await conn.OpenAsync();
        return conn;
    }

    private async Task<NpgsqlConnection> OpenTargetAsync()
    {
        var conn = new NpgsqlConnection(_targetConnStr);
        await conn.OpenAsync();
        return conn;
    }

    private static async Task InsertIfNotExistsAsync(NpgsqlConnection conn, string sql, params NpgsqlParameter[] parameters)
    {
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> GetColumnNamesAsync(NpgsqlConnection conn, string tableName)
    {
        using var cmd = new NpgsqlCommand(
            @"SELECT column_name FROM information_schema.columns
              WHERE table_schema='public' AND table_name=@tableName
              ORDER BY ordinal_position", conn);
        cmd.Parameters.AddWithValue("@tableName", tableName);

        var columns = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            columns.Add(reader.GetString(0));

        return columns;
    }

    private static async Task UpsertMuhasebeHesapAsync(
        NpgsqlConnection target,
        List<(string sourceName, string targetName)> columns,
        Dictionary<string, object?> row)
    {
        var insertColumns = string.Join(", ", columns.Select(c => QuoteIdentifier(c.targetName)));
        var valueNames = columns.Select((_, i) => $"@p{i}").ToList();
        var updateColumns = columns
            .Where(c => !c.targetName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .Where(c => !c.targetName.Equals("HesapKodu", StringComparison.OrdinalIgnoreCase))
            .Select(c => $"{QuoteIdentifier(c.targetName)} = EXCLUDED.{QuoteIdentifier(c.targetName)}")
            .ToList();

        var conflictSql = updateColumns.Count > 0
            ? $"DO UPDATE SET {string.Join(", ", updateColumns)}"
            : "DO NOTHING";

        using var cmd = new NpgsqlCommand(
            $@"INSERT INTO ""MuhasebeHesaplari"" ({insertColumns})
               VALUES ({string.Join(", ", valueNames)})
               ON CONFLICT (""HesapKodu"") {conflictSql}", target);

        for (var i = 0; i < columns.Count; i++)
        {
            var value = row.TryGetValue(columns[i].sourceName, out var rawValue) && rawValue is not null
                ? rawValue
                : DBNull.Value;
            cmd.Parameters.Add(new NpgsqlParameter(valueNames[i], value));
        }

        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ResetSequenceAsync(NpgsqlConnection conn, string tableName)
    {
        using var cmd = new NpgsqlCommand(
            @"SELECT setval(
                  pg_get_serial_sequence(@tableName, 'Id'),
                  COALESCE((SELECT MAX(""Id"") FROM ""MuhasebeHesaplari""), 1),
                  true)", conn);
        cmd.Parameters.AddWithValue("@tableName", QuoteIdentifier(tableName));
        await cmd.ExecuteNonQueryAsync();
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    private static bool TryGetInt(Dictionary<string, object?> row, string key, out int value)
    {
        value = 0;
        if (!row.TryGetValue(key, out var rawValue) || rawValue is null || rawValue is DBNull)
            return false;

        value = Convert.ToInt32(rawValue);
        return true;
    }

}

public class TransferResult
{
    public int Transferred { get; set; }
    public int TotalTransferred { get; set; }
    public int TableCount { get; set; }
    public List<string> Errors { get; set; } = new();

    public void Add(TransferResult other)
    {
        Transferred += other.Transferred;
        TotalTransferred += other.Transferred;
        if (other.Transferred > 0) TableCount++;
        Errors.AddRange(other.Errors);
    }
}



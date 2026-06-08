using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// DestekCRMServisBlazorDb → KOAFiloServis veri aktarım servisi.
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
        _targetConnStr = configuration.GetConnectionString("DefaultConnection")!;
        _sourceConnStr = _targetConnStr.Replace("Database=KOAFiloServis", "Database=DestekCRMServisBlazorDb");
        _logger = logger;
    }

    /// <summary>
    /// KOAFiloServis veritabanını mevcut model snapshot'ından oluşturur (EnsureCreated).
    /// Migration zincirini bypass eder.
    /// </summary>
    public async Task EnsureSchemaAsync()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(_targetConnStr);
        using var ctx = new ApplicationDbContext(optionsBuilder.Options);

        _logger.LogInformation("EnsureCreated basliyor...");
        await ctx.Database.EnsureCreatedAsync();
        _logger.LogInformation("EnsureCreated tamamlandi.");
    }

    /// <summary>
    /// Tüm verileri sırayla aktarır (Talimat Bölüm 21).
    /// Önce kritik bağımlı tablolar, sonra kaynak DB'deki TÜM diğer tablolar otomatik aktarılır.
    /// </summary>
    public async Task<TransferResult> TransferAllAsync()
    {
        var result = new TransferResult();
        var firmaIdVarsayilan = 1;

        _logger.LogInformation("=== Veri aktarimi basladi ===");

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

        // ── Kaynak DB'deki TÜM diğer tablolar (otomatik keşif) ────────
        var allSourceTables = await DiscoverSourceTablesAsync();
        var skipTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "__EFMigrationsHistory", "Organizasyonlar", "Firmalar", "Roller",
            "Kullanicilar", "RolYetkileri", "Cariler", "Sirketler",
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

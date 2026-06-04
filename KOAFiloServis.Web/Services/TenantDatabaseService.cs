using System.Reflection;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace KOAFiloServis.Web.Services;

public sealed class TenantDatabaseService : ITenantDatabaseService
{
    private readonly ITenantConnectionStringProvider _connProvider;
    private readonly IDbContextFactory<ApplicationDbContext> _masterFactory;
    private readonly IDbContextFactory<ApplicationDbContext> _tenantFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantDatabaseService> _logger;

    public TenantDatabaseService(
        ITenantConnectionStringProvider connProvider,
        IDbContextFactory<ApplicationDbContext> masterFactory,
        IDbContextFactory<ApplicationDbContext> tenantFactory,
        IConfiguration configuration,
        ILogger<TenantDatabaseService> logger)
    {
        _connProvider = connProvider;
        _masterFactory = masterFactory;
        _tenantFactory = tenantFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task CreateTenantDatabaseAsync(int firmaId, bool migrateData = true)
    {
        // Firma bilgisini Master DB'den al
        await using var masterCtx = await _masterFactory.CreateDbContextAsync();
        var firma = await masterCtx.Firmalar.FindAsync(firmaId);
        if (firma == null)
            throw new InvalidOperationException($"Firma bulunamadi: {firmaId}");

        var databaseName = BuildTenantDbName(firma);

        if (await TenantDatabaseExistsAsync(databaseName))
        {
            _logger.LogInformation("Tenant DB zaten mevcut: {DbName}", databaseName);
        }
        else
        {
            var masterConnStr = _connProvider.GetMasterConnectionString();
            var serverConnStr = new NpgsqlConnectionStringBuilder(masterConnStr)
            {
                Database = "postgres"
            }.ConnectionString;

            await using var serverConn = new NpgsqlConnection(serverConnStr);
            await serverConn.OpenAsync();
            await using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", serverConn);
            await createCmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Tenant DB olusturuldu: {DbName}", databaseName);
        }

        // Tenant DB'ye EF Core semasini olustur
        var tenantConnStr = _connProvider.GetConnectionStringForFirma(firmaId, databaseName);
        if (tenantConnStr == null)
            throw new InvalidOperationException($"Tenant connection string alinamadi: FirmaId={firmaId}");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(tenantConnStr);
        await using var context = new ApplicationDbContext(optionsBuilder.Options);

        var created = await context.Database.EnsureCreatedAsync();
        _logger.LogInformation("Tenant DB sema olusturuldu (yeni={IsNew}): {DbName}", created, databaseName);

        // Mevcut tum migration'lari __EFMigrationsHistory'ye kaydet (baseline)
        // Boylece gelecekteki Migrate() cagrilari sadece yeni migration'lari uygular
        if (created)
        {
            await BaselineMigrationsAsync(context, tenantConnStr);
        }

        // Master DB'de Firma.DatabaseName guncelle
        firma.DatabaseName = databaseName;
        await masterCtx.SaveChangesAsync();
        _logger.LogInformation("Firma {FirmaId} DatabaseName = {DbName}", firmaId, databaseName);

        // Veri gocu: shared DB'den tenant DB'ye
        if (migrateData)
        {
            await MigrateFirmaDataAsync(firmaId);
        }
    }

    private async Task BaselineMigrationsAsync(DbContext context, string connectionString)
    {
        try
        {
            var migrationType = typeof(Migration);
            var migrationTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && migrationType.IsAssignableFrom(t))
                .ToList();

            if (!migrationTypes.Any()) return;

            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            var inserted = 0;
            foreach (var type in migrationTypes)
            {
                var attr = type.GetCustomAttribute<MigrationAttribute>();
                if (attr?.Id == null) continue;

                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (@id, '10.0.0') ON CONFLICT DO NOTHING",
                    conn);
                cmd.Parameters.AddWithValue("@id", attr.Id);
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows > 0) inserted++;
            }

            _logger.LogInformation("Migration baseline: {Inserted}/{Total} migration kaydedildi", inserted, migrationTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Migration baseline olusturulamadi (kritik degil, manuel mudahale gerekebilir)");
        }
    }

    public async Task MigrateFirmaDataAsync(int firmaId)
    {
        await using var masterCtx = await _masterFactory.CreateDbContextAsync();
        var firma = await masterCtx.Firmalar.FindAsync(firmaId);
        if (firma == null) return;

        var databaseName = firma.DatabaseName ?? BuildTenantDbName(firma);

        var sharedConnStr = _configuration.GetConnectionString("DefaultConnection");
        var tenantConnStr = _connProvider.GetConnectionStringForFirma(firmaId, databaseName);
        if (string.IsNullOrWhiteSpace(sharedConnStr) || string.IsNullOrWhiteSpace(tenantConnStr))
            return;

        // Ayni DB ise gec
        if (string.Equals(sharedConnStr, tenantConnStr, StringComparison.OrdinalIgnoreCase)) return;

        await using var sharedConn = new NpgsqlConnection(sharedConnStr);
        await sharedConn.OpenAsync();
        await using var tenantConn = new NpgsqlConnection(tenantConnStr);
        await tenantConn.OpenAsync();

        // FK kisitlamalarini gecici olarak devre disi birak
        await using var disableFkCmd = new NpgsqlCommand("SET session_replication_role = 'replica';", tenantConn);
        await disableFkCmd.ExecuteNonQueryAsync();

        // ADIM 0: Tenant DB'ye kendi Firma kaydini ekle.
        // 44 FK constraint Firmalar tablosuna referans verir. Tenant mimaride Firmalar
        // Master DB'de yonetilir, ancak tenant DB'de referans butunlugu icin
        // bu firmaya ait tek bir kayit bulunmalidir. ON CONFLICT ile tekrarli calistirmada guvenli.
        _logger.LogInformation("Veri gocu Adim 0: Firma kaydi ekleniyor (FirmaId={FirmaId})...", firmaId);
        await using var firmaInsertCmd = new NpgsqlCommand(
            @"INSERT INTO ""Firmalar"" (""Id"", ""FirmaKodu"", ""FirmaAdi"", ""Aktif"", ""VarsayilanFirma"", ""SiraNo"", ""AktifDonemYil"", ""AktifDonemAy"", ""CreatedAt"", ""IsDeleted"", ""DatabaseName"")
              VALUES (@id, @kod, @adi, true, @varsayilan, @sirano, @donemYil, @donemAy, @createdAt, false, @db)
              ON CONFLICT (""Id"") DO UPDATE SET
                ""FirmaKodu"" = EXCLUDED.""FirmaKodu"",
                ""FirmaAdi"" = EXCLUDED.""FirmaAdi"",
                ""DatabaseName"" = EXCLUDED.""DatabaseName"",
                ""AktifDonemYil"" = EXCLUDED.""AktifDonemYil"",
                ""AktifDonemAy"" = EXCLUDED.""AktifDonemAy""",
            tenantConn);
        firmaInsertCmd.Parameters.AddWithValue("@id", firma.Id);
        firmaInsertCmd.Parameters.AddWithValue("@kod", firma.FirmaKodu ?? "FIRMA");
        firmaInsertCmd.Parameters.AddWithValue("@adi", firma.FirmaAdi);
        firmaInsertCmd.Parameters.AddWithValue("@varsayilan", firma.VarsayilanFirma);
        firmaInsertCmd.Parameters.AddWithValue("@sirano", firma.SiraNo);
        firmaInsertCmd.Parameters.AddWithValue("@donemYil", firma.AktifDonemYil);
        firmaInsertCmd.Parameters.AddWithValue("@donemAy", firma.AktifDonemAy);
        firmaInsertCmd.Parameters.AddWithValue("@createdAt", firma.CreatedAt);
        firmaInsertCmd.Parameters.AddWithValue("@db", firma.DatabaseName ?? (object)DBNull.Value);
        await firmaInsertCmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Veri gocu Adim 0 tamam: Firma kaydi eklendi/guncellendi.");

        // Master DB'ye ait diger tablolar (tenant DB'de sema olarak olmali ama veri kopyalanmayacak)
        var masterTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Firmalar", "Kullanicilar", "Lisanslar", "Roller", "RolYetkileri", "AppAyarlari" };

        // Tenant DB'deki TUM tablolari al
        var allTenantTables = new List<string>();
        await using (var listCmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE' ORDER BY table_name", tenantConn))
        {
            await using var reader = await listCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) allTenantTables.Add(reader.GetString(0));
        }

        // 1. ADIM: FirmaId'siz lookup tablolarini kopyala (FK kisitlamalari icin gerekli)
        _logger.LogInformation("Veri gocu Adim 1: Lookup tablolari kopyalaniyor...");
        var lookupTotal = 0;
        foreach (var table in allTenantTables)
        {
            if (masterTables.Contains(table)) continue;

            // Bu tabloda FirmaId kolonu var mi?
            await using var fidCheckCmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT FROM information_schema.columns WHERE table_name=@t AND column_name='FirmaId')", tenantConn);
            fidCheckCmd.Parameters.AddWithValue("@t", table);
            var hasFirmaId = (bool)(await fidCheckCmd.ExecuteScalarAsync())!;
            if (hasFirmaId) continue; // FirmaId'li tablolar 2. adimda

            // Shared DB'de bu tablo var mi?
            await using var existsCmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name=@t)", sharedConn);
            existsCmd.Parameters.AddWithValue("@t", table);
            if (!(bool)(await existsCmd.ExecuteScalarAsync())!) continue;

            var rows = await CopyTableDataAsync(sharedConn, tenantConn, table, null);
            if (rows > 0) { lookupTotal += rows; _logger.LogInformation("  Lookup: {Table} -> {Rows} satir", table, rows); }
        }
        _logger.LogInformation("Veri gocu Adim 1 tamam: {Total} lookup satiri", lookupTotal);

        // 2. ADIM: FirmaId'li tenant tablolarini kopyala (sadece bu firmaya ait)
        _logger.LogInformation("Veri gocu Adim 2: FirmaId={FirmaId} tenant verileri...", firmaId);
        var tenantTotal = 0;
        foreach (var table in allTenantTables)
        {
            if (masterTables.Contains(table)) continue;

            await using var fidCheckCmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT FROM information_schema.columns WHERE table_name=@t AND column_name='FirmaId')", tenantConn);
            fidCheckCmd.Parameters.AddWithValue("@t", table);
            if (!(bool)(await fidCheckCmd.ExecuteScalarAsync())!) continue;

            var rows = await CopyTableDataAsync(sharedConn, tenantConn, table, firmaId);
            if (rows > 0) { tenantTotal += rows; _logger.LogInformation("  Tenant: {Table} -> {Rows} satir", table, rows); }
        }
        // FK kisitlamalarini tekrar aktif et
        await using var enableFkCmd = new NpgsqlCommand("SET session_replication_role = 'origin';", tenantConn);
        await enableFkCmd.ExecuteNonQueryAsync();

        // Sequence reset: veri gocu sirasinda row'lar orijinal Id degerleriyle kopyalanir.
        // PostgreSQL sequence'leri hala 1'den baslar → yeni INSERT'ler PK ihlali (23505) uretir.
        await ResetAllSequencesAsync(tenantConn);

        _logger.LogInformation("Veri gocu tamamlandi: {LookupTotal} lookup + {TenantTotal} tenant = {Total} satir",
            lookupTotal, tenantTotal, lookupTotal + tenantTotal);
    }

    public async Task<bool> TenantDatabaseExistsAsync(string databaseName)
    {
        var masterConnStr = _connProvider.GetMasterConnectionString();
        var serverConnStr = new NpgsqlConnectionStringBuilder(masterConnStr)
        {
            Database = "postgres"
        }.ConnectionString;

        await using var conn = new NpgsqlConnection(serverConnStr);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name", conn);
        cmd.Parameters.AddWithValue("@name", databaseName);
        return await cmd.ExecuteScalarAsync() != null;
    }

    /// <inheritdoc />
    public async Task<int> ApplyPendingMigrationsAsync(int firmaId)
    {
        await using var masterCtx = await _masterFactory.CreateDbContextAsync();
        var firma = await masterCtx.Firmalar.FindAsync(firmaId);
        if (firma == null)
            throw new InvalidOperationException($"Firma bulunamadi: {firmaId}");

        if (string.IsNullOrWhiteSpace(firma.DatabaseName))
        {
            _logger.LogWarning("Firma {FirmaId} icin DatabaseName tanimli degil, migration uygulanamadi.", firmaId);
            return 0;
        }

        var tenantConnStr = _connProvider.GetConnectionStringForFirma(firmaId, firma.DatabaseName);
        if (string.IsNullOrWhiteSpace(tenantConnStr))
            throw new InvalidOperationException($"Tenant connection string alinamadi: FirmaId={firmaId}");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(tenantConnStr);
        await using var ctx = new ApplicationDbContext(optionsBuilder.Options);

        var pending = (await ctx.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            _logger.LogInformation("Firma {FirmaId} ({DbName}): bekleyen migration yok.", firmaId, firma.DatabaseName);
            return 0;
        }

        _logger.LogInformation("Firma {FirmaId} ({DbName}): {Count} bekleyen migration uygulanıyor: {Migrations}",
            firmaId, firma.DatabaseName, pending.Count, string.Join(", ", pending));

        await ctx.Database.MigrateAsync();

        _logger.LogInformation("Firma {FirmaId} ({DbName}): {Count} migration basariyla uygulandi.",
            firmaId, firma.DatabaseName, pending.Count);

        return pending.Count;
    }

    /// <inheritdoc />
    public async Task<(int Total, int Updated, int Errors)> ApplyPendingMigrationsToAllTenantsAsync()
    {
        await using var masterCtx = await _masterFactory.CreateDbContextAsync();
        var firmalar = await masterCtx.Firmalar
            .Where(f => !string.IsNullOrEmpty(f.DatabaseName))
            .ToListAsync();

        int total = firmalar.Count;
        int updated = 0;
        int errors = 0;

        _logger.LogInformation("Tum tenant migration: {Total} firma isleniyor...", total);

        foreach (var firma in firmalar)
        {
            try
            {
                var count = await ApplyPendingMigrationsAsync(firma.Id);
                if (count > 0) updated++;
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(ex, "Firma {FirmaId} ({DbName}) migration hatasi.", firma.Id, firma.DatabaseName);
            }
        }

        _logger.LogInformation("Tum tenant migration tamamlandi: {Total} toplam, {Updated} guncellendi, {Errors} hata.",
            total, updated, errors);

        return (total, updated, errors);
    }

    private async Task<int> CopyTableDataAsync(
        NpgsqlConnection sourceConn, NpgsqlConnection targetConn,
        string table, int? firmaId)
    {
        try
        {
            // Kaynakta tablo var mi?
            await using var srcCheckCmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name=@t)", sourceConn);
            srcCheckCmd.Parameters.AddWithValue("@t", table);
            if (!(bool)(await srcCheckCmd.ExecuteScalarAsync())!)
                return 0; // Kaynakta yok, sessiz gec

            // Hedef sutun listesi
            var targetColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var colCmd = new NpgsqlCommand(
                "SELECT column_name FROM information_schema.columns WHERE table_name = @t", targetConn))
            {
                colCmd.Parameters.AddWithValue("@t", table);
                await using var colReader = await colCmd.ExecuteReaderAsync();
                while (await colReader.ReadAsync()) targetColumns.Add(colReader.GetString(0));
            }

            if (targetColumns.Count == 0) return 0;

            // Kaynaktan oku (FirmaId filtresi opsiyonel)
            var sql = firmaId.HasValue
                ? $"SELECT * FROM \"{table}\" WHERE \"FirmaId\" = @fid"
                : $"SELECT * FROM \"{table}\"";
            await using var readCmd = new NpgsqlCommand(sql, sourceConn);
            if (firmaId.HasValue) readCmd.Parameters.AddWithValue("@fid", firmaId.Value);

            await using var reader = await readCmd.ExecuteReaderAsync();

            var sourceColumns = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                sourceColumns.Add(reader.GetName(i));

            var commonColumns = sourceColumns.Where(c => targetColumns.Contains(c)).ToList();
            if (commonColumns.Count == 0)
            {
                _logger.LogWarning("[VeriGocu] {Table}: ortak sutun yok. Kaynak={SrcCols}, Hedef={TgtCols}",
                    table,
                    string.Join(",", sourceColumns.Take(10)),
                    string.Join(",", targetColumns.Take(10)));
                return 0;
            }

            // Delta sync: ON CONFLICT DO NOTHING zaten duplicate'leri handle eder.
            // Hedefte veri olsa bile yeni kayitlari eklemeye devam et.

            var inserted = 0;
            var skipped = 0;
            var firstError = (string?)null;
            while (await reader.ReadAsync())
            {
                var colIdxMap = commonColumns.Select(c => sourceColumns.IndexOf(c)).ToList();
                var values = colIdxMap.Select(idx =>
                    reader.IsDBNull(idx) ? DBNull.Value : reader.GetValue(idx)).ToList();

                var paramNames = string.Join(", ", values.Select((_, idx) => $"@p{idx}"));
                var colNames = string.Join("\", \"", commonColumns);
                var insertSql = $"INSERT INTO \"{table}\" (\"{colNames}\") VALUES ({paramNames}) ON CONFLICT DO NOTHING";

                await using var insertCmd = new NpgsqlCommand(insertSql, targetConn);
                for (var i = 0; i < values.Count; i++)
                    insertCmd.Parameters.AddWithValue($"@p{i}", values[i]);

                try { await insertCmd.ExecuteNonQueryAsync(); inserted++; }
                catch (Exception ex)
                {
                    skipped++;
                    firstError ??= ex.Message;
                }
            }
            await reader.CloseAsync();

            if (skipped > 0)
                _logger.LogWarning("[VeriGocu] {Table}: {Inserted} basarili, {Skipped} atlandi. Ilk hata: {Error}",
                    table, inserted, skipped, firstError);

            return inserted;
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            _logger.LogDebug("[VeriGocu] {Table} hedefte yok (legacy), atlaniyor", table);
            return 0;
        }
    }

    /// <summary>
    /// Veri gocu sirasinda row'lar orijinal Id degerleriyle INSERT edilir.
    /// PostgreSQL sequence'leri guncellenmedigi icin yeni kayitlarda PK ihlali (23505) olusur.
    /// Tum SERIAL/IDENTITY kolonlarinin sequence'lerini max(Id) degerine resetler.
    /// </summary>
    private static async Task ResetAllSequencesAsync(NpgsqlConnection conn)
    {
        try
        {
            var sql = """
                DO $$
                DECLARE
                    r RECORD;
                    max_id BIGINT;
                BEGIN
                    FOR r IN
                        SELECT table_name
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND column_name = 'Id'
                          AND column_default LIKE 'nextval%'
                    LOOP
                        EXECUTE format('SELECT COALESCE(MAX("Id"), 0) FROM %I', r.table_name) INTO max_id;
                        IF max_id > 0 THEN
                            EXECUTE format('SELECT setval(pg_get_serial_sequence(''%I'', ''Id''), %s, true)',
                                           r.table_name, max_id);
                        END IF;
                    END LOOP;
                END $$;
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // Kritik degil: sequence reset basarisiz olsa bile goc tamamlanmis sayilir.
            // Bu durumda ilk yeni kayitta duplicate key alinirsa manuel mudahele gerekir.
            System.Console.WriteLine($"[TenantDb] Sequence reset basarisiz: {ex.Message}");
        }
    }

    public static string BuildTenantDbName(Shared.Entities.Firma firma)
    {
        var kod = SanitizeForDbName(firma.FirmaKodu ?? $"F{firma.Id}");
        return $"Koa_{kod}_{firma.Id:D3}";
    }

    private static string SanitizeForDbName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "FIRMA";

        // Once Turkce karakterleri donustur, sonra upper yap
        var replaced = input.Trim()
            .Replace('Ü', 'U').Replace('ü', 'u')
            .Replace('Ğ', 'G').Replace('ğ', 'g')
            .Replace('İ', 'I').Replace('ı', 'i')
            .Replace('Ş', 'S').Replace('ş', 's')
            .Replace('Ç', 'C').Replace('ç', 'c')
            .Replace('Ö', 'O').Replace('ö', 'o');

        var sb = new System.Text.StringBuilder();
        foreach (var c in replaced.ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c == ' ' || c == '-' || c == '_')
                sb.Append('_');
        }

        var result = sb.ToString().Trim('_');
        if (result.Length > 50) result = result.Substring(0, 50);
        return string.IsNullOrWhiteSpace(result) ? "FIRMA" : result;
    }
}

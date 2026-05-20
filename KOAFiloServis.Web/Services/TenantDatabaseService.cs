using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KOAFiloServis.Web.Services;

public sealed class TenantDatabaseService : ITenantDatabaseService
{
    private readonly ITenantConnectionStringProvider _connProvider;
    private readonly IDbContextFactory<MasterDbContext> _masterFactory;
    private readonly IDbContextFactory<ApplicationDbContext> _tenantFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantDatabaseService> _logger;

    public TenantDatabaseService(
        ITenantConnectionStringProvider connProvider,
        IDbContextFactory<MasterDbContext> masterFactory,
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
        var prefix = _configuration.GetValue<string>("TenantDatabase:NamingPrefix") ?? "kofa_firma_";
        var databaseName = $"{prefix}{firmaId:D3}";

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

        // Tenant DB'ye EF Core migration'larini uygula
        var tenantConnStr = _connProvider.GetConnectionStringForFirma(firmaId, databaseName);
        if (tenantConnStr == null)
            throw new InvalidOperationException($"Tenant connection string alinamadi: FirmaId={firmaId}");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(tenantConnStr);
        await using var context = new ApplicationDbContext(optionsBuilder.Options);

        // EnsureCreated: mevcut modelden semayi direkt olusturur.
        // MigrateAsync yerine kullaniyoruz cunku 80+ migration'in bazilari
        // legacy kolonlara (orn. KiralayiciCariId) referans verip kiriliyor.
        var created = await context.Database.EnsureCreatedAsync();
        _logger.LogInformation("Tenant DB sema olusturuldu (yeni={IsNew}): {DbName}", created, databaseName);

        // Master DB'de Firma.DatabaseName guncelle
        await using var masterCtx = await _masterFactory.CreateDbContextAsync();
        var firma = await masterCtx.Firmalar.FindAsync(firmaId);
        if (firma != null)
        {
            firma.DatabaseName = databaseName;
            await masterCtx.SaveChangesAsync();
            _logger.LogInformation("Firma {FirmaId} DatabaseName = {DbName}", firmaId, databaseName);
        }

        // Veri gocu: shared DB'den tenant DB'ye
        if (migrateData)
        {
            await MigrateFirmaDataAsync(firmaId);
        }
    }

    public async Task MigrateFirmaDataAsync(int firmaId)
    {
        var prefix = _configuration.GetValue<string>("TenantDatabase:NamingPrefix") ?? "kofa_firma_";
        var databaseName = $"{prefix}{firmaId:D3}";

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

        // FirmaId kolonu iceren tum tenant tablolarini bul
        var tables = new List<string>();
        await using (var listCmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.columns WHERE column_name = 'FirmaId' AND table_schema = 'public' ORDER BY table_name", sharedConn))
        {
            await using var reader = await listCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));
        }

        _logger.LogInformation("Veri gocu: {TableCount} tablo taranacak, FirmaId={FirmaId}", tables.Count, firmaId);

        var totalRows = 0;
        foreach (var table in tables)
        {
            // Tenant DB'de tablo var mi?
            await using var checkCmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = @t)", tenantConn);
            checkCmd.Parameters.AddWithValue("@t", table);
            var exists = (bool)(await checkCmd.ExecuteScalarAsync())!;
            if (!exists) continue;

            // Hedef sutun listesi
            var targetColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var colCmd = new NpgsqlCommand(
                "SELECT column_name FROM information_schema.columns WHERE table_name = @t", tenantConn))
            {
                colCmd.Parameters.AddWithValue("@t", table);
                await using var colReader = await colCmd.ExecuteReaderAsync();
                while (await colReader.ReadAsync())
                    targetColumns.Add(colReader.GetString(0));
            }

            // Shared DB'den FirmaId'ye gore oku
            await using var readCmd = new NpgsqlCommand(
                $"SELECT * FROM \"{table}\" WHERE \"FirmaId\" = @fid", sharedConn);
            readCmd.Parameters.AddWithValue("@fid", firmaId);
            await using var reader = await readCmd.ExecuteReaderAsync();

            var sourceColumns = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                sourceColumns.Add(reader.GetName(i));

            var commonColumns = sourceColumns.Where(c => targetColumns.Contains(c)).ToList();
            if (commonColumns.Count == 0) continue;

            var inserted = 0;
            while (await reader.ReadAsync())
            {
                var colIdxMap = commonColumns.Select(c => sourceColumns.IndexOf(c)).ToList();
                var values = colIdxMap.Select(idx =>
                    reader.IsDBNull(idx) ? DBNull.Value : reader.GetValue(idx)).ToList();

                var paramNames = string.Join(", ", values.Select((_, idx) => $"@p{idx}"));
                var colNames = string.Join("\", \"", commonColumns);
                var insertSql = $"INSERT INTO \"{table}\" (\"{colNames}\") VALUES ({paramNames})";

                await using var insertCmd = new NpgsqlCommand(insertSql, tenantConn);
                for (var i = 0; i < values.Count; i++)
                    insertCmd.Parameters.AddWithValue($"@p{i}", values[i]);

                try { await insertCmd.ExecuteNonQueryAsync(); inserted++; }
                catch (Exception ex) { _logger.LogWarning(ex, "Veri gocu {Table} INSERT hatasi", table); }
            }
            await reader.CloseAsync();

            if (inserted > 0)
            {
                totalRows += inserted;
                _logger.LogInformation("Veri gocu: {Table} -> {Rows} satir", table, inserted);
            }
        }

        _logger.LogInformation("Veri gocu tamamlandi: {TotalRows} toplam satir, FirmaId={FirmaId}", totalRows, firmaId);
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
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MKFiloServis.Web.Data;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;
using System.Data.Common;

namespace MKFiloServis.Tests;

public sealed class SqliteMigrationBootstrapTests
{
    [Fact]
    public async Task Empty_sqlite_database_runs_the_supported_initializer()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(warnings => warnings.Log(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;
        await using var context = new ApplicationDbContext(options);

        await DbInitializer.InitializeAsync(
            context,
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.True(await context.Database.CanConnectAsync());
        Assert.Equal(4, await context.Organizasyonlar.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Fresh_sqlite_baseline_rolls_back_schema_and_history_when_history_write_fails()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new FailFreshBaselineHistoryInsertInterceptor())
            .Options;
        await using var context = new ApplicationDbContext(options);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => DbInitializer.InitializeFreshDatabaseBaselineIfEmptyAsync(context));

        await using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        Assert.Equal(0L, (long)(await verify.ExecuteScalarAsync() ?? -1L));
    }

    [Fact]
    public async Task Partial_sqlite_schema_is_never_marked_as_a_fresh_baseline()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE Cariler (Id INTEGER PRIMARY KEY, CariKodu TEXT NOT NULL); INSERT INTO Cariler (Id, CariKodu) VALUES (1, 'KEEP')";
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);

        Assert.False(await DbInitializer.InitializeFreshDatabaseBaselineIfEmptyAsync(context));
        await using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT CariKodu FROM Cariler WHERE Id = 1; SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
        await using var reader = await verify.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("KEEP", reader.GetString(0));
        Assert.True(await reader.NextResultAsync());
        Assert.True(await reader.ReadAsync());
        Assert.Equal(0, reader.GetInt32(0));
    }

    [RequiresPostgresTest]
    public async Task Existing_postgresql_schema_with_empty_migration_history_is_rejected()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("MKFILOSERVIS_TEST_POSTGRES_CONNECTION")!;
        var schema = $"a18_{Guid.NewGuid():N}";
        await using (var setup = new Npgsql.NpgsqlConnection(baseConnectionString))
        {
            await setup.OpenAsync();
            await using var createSchema = setup.CreateCommand();
            createSchema.CommandText = $"CREATE SCHEMA \"{schema}\"";
            await createSchema.ExecuteNonQueryAsync();
        }

        try
        {
            var connectionString = new Npgsql.NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                SearchPath = schema
            }.ConnectionString;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE \"A18LegacyFixture\" (\"Id\" integer PRIMARY KEY)");
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" character varying(150) PRIMARY KEY, \"ProductVersion\" character varying(32) NOT NULL)");

            var configuration = new Microsoft.Extensions.Configuration.ConfigurationManager();
            configuration["ConnectionStrings:DefaultConnection"] = connectionString;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => DbInitializer.InitializeAsync(context, configuration));

            Assert.Contains("migration geçmişi boş", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await using var cleanup = new Npgsql.NpgsqlConnection(baseConnectionString);
            await cleanup.OpenAsync();
            await using var dropSchema = cleanup.CreateCommand();
            dropSchema.CommandText = $"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE";
            await dropSchema.ExecuteNonQueryAsync();
        }
    }

    [RequiresPostgresTest]
    public async Task Empty_postgresql_database_runs_the_supported_initializer()
    {
        var connectionString = Environment.GetEnvironmentVariable("MKFILOSERVIS_TEST_POSTGRES_CONNECTION")!;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "public"))
            .ConfigureWarnings(warnings => warnings.Log(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationManager();
        configuration["ConnectionStrings:DefaultConnection"] = connectionString;
        await using var context = new ApplicationDbContext(options);

        Assert.True(await DbInitializer.InitializeFreshDatabaseBaselineIfEmptyAsync(context));
        await DbInitializer.InitializeAsync(context, configuration);

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(4, await context.Organizasyonlar.IgnoreQueryFilters().CountAsync());
    }

    [RequiresPostgresTest]
    public async Task Partial_postgresql_schema_is_never_marked_as_a_fresh_baseline()
    {
        var connectionString = Environment.GetEnvironmentVariable("MKFILOSERVIS_TEST_POSTGRES_CONNECTION")!;
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE \"Cariler\" (\"Id\" integer PRIMARY KEY, \"CariKodu\" text NOT NULL); INSERT INTO \"Cariler\" (\"Id\", \"CariKodu\") VALUES (1, 'KEEP')";
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "public"))
            .Options;
        await using var context = new ApplicationDbContext(options);

        Assert.False(await DbInitializer.InitializeFreshDatabaseBaselineIfEmptyAsync(context));
        await using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT \"CariKodu\" FROM \"Cariler\" WHERE \"Id\" = 1; SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='__EFMigrationsHistory'";
        await using var reader = await verify.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("KEEP", reader.GetString(0));
        Assert.True(await reader.NextResultAsync());
        Assert.True(await reader.ReadAsync());
        Assert.Equal(0, reader.GetInt32(0));
    }

    [Fact]
    public void Legacy_baseline_never_marks_newer_schema_changes_as_applied()
    {
        var supportedLegacyWatermark = "20260925192810_NormalizeRentACarOdemeEnumColumns";
        var postWatermarkMigration = "20261006190000_AddUniqueActiveVehiclePlateIndex";

        var selected = DbInitializer.SelectSqliteLegacyMigrationsToBaseline(
            [supportedLegacyWatermark, postWatermarkMigration],
            [supportedLegacyWatermark, postWatermarkMigration],
            []);

        Assert.Equal([supportedLegacyWatermark], selected);
    }

    [Fact]
    public async Task Bank_key_schema_repair_adds_missing_columns_and_index_without_changing_rows()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE \"BankaKasaHareketleri\" (\"Id\" INTEGER PRIMARY KEY, \"Aciklama\" TEXT); INSERT INTO \"BankaKasaHareketleri\" (\"Id\", \"Aciklama\") VALUES (17, 'mevcut hareket')";
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await DbInitializer.EnsureSqliteBankOperationKeySchemaAsync(context);
        await DbInitializer.EnsureSqliteBankOperationKeySchemaAsync(context);

        await using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT \"Id\", \"Aciklama\", \"IslemKimligi\", \"IslemOzeti\" FROM \"BankaKasaHareketleri\"; SELECT COUNT(*) FROM pragma_index_list('BankaKasaHareketleri') WHERE name = 'IX_BankaKasaHareketleri_IslemKimligi' AND \"unique\" = 1";
        await using var reader = await verify.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(17, reader.GetInt32(0));
        Assert.Equal("mevcut hareket", reader.GetString(1));
        Assert.True(reader.IsDBNull(2));
        Assert.True(reader.IsDBNull(3));
        Assert.True(await reader.NextResultAsync());
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1, reader.GetInt32(0));
    }

    [Fact]
    public async Task Finance_dashboard_query_recovers_from_legacy_bank_schema_after_startup_repair()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var numbers = new NumaraSerisiService(fixture.Factory);
        var accountService = new BankaHesapService(fixture.Factory, numbers, fixture.Guard, fixture.ActiveFirm);
        var accountingService = new MuhasebeService(fixture.Factory, fixture.Guard);
        var dashboardService = new BankaKasaHareketService(
            fixture.Factory, accountingService, accountService, numbers, fixture.ActiveFirm, fixture.Guard);

        await using (var legacy = fixture.Factory.CreateDbContext())
        {
            await legacy.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS \"IX_BankaKasaHareketleri_IslemKimligi\"");
            await legacy.Database.ExecuteSqlRawAsync("ALTER TABLE \"BankaKasaHareketleri\" DROP COLUMN \"IslemKimligi\"");
            await legacy.Database.ExecuteSqlRawAsync("ALTER TABLE \"BankaKasaHareketleri\" DROP COLUMN \"IslemOzeti\"");
        }

        await Assert.ThrowsAsync<SqliteException>(() => dashboardService.GetRecentAsync(5));

        await using (var startup = fixture.Factory.CreateDbContext())
            await DbInitializer.EnsureSqliteBankOperationKeySchemaAsync(startup);

        var recent = await dashboardService.GetRecentAsync(5);
        Assert.Empty(recent);
    }
}

internal sealed class FailFreshBaselineHistoryInsertInterceptor : DbCommandInterceptor
{
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("INSERT INTO \"__EFMigrationsHistory\"", StringComparison.OrdinalIgnoreCase)
            || command.CommandText.Contains("INSERT INTO \"__MKWriteJournal\"", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Injected migration history write failure.");

        return ValueTask.FromResult(result);
    }
}

public sealed class RequiresPostgresTestAttribute : FactAttribute
{
    public RequiresPostgresTestAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MKFILOSERVIS_TEST_POSTGRES_CONNECTION")))
            Skip = "Set MKFILOSERVIS_TEST_POSTGRES_CONNECTION to an isolated empty PostgreSQL database.";
    }
}

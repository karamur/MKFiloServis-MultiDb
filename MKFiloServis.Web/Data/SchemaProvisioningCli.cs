using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data;

/// <summary>
/// Başsız şema hazırlama modu: <c>MKFiloServis.Web.exe --ensure-schema</c>.
/// Web sunucusu başlatılmadan, uygulamanın açılışta kullandığı aynı akışla
/// (boş veritabanında model baseline'ı, mevcut veritabanında bekleyen migration'lar)
/// hedef PostgreSQL şemasını hazırlar. Veri Aktarım aracı (DataSync) hedefte eksik
/// tablo gördüğünde bu modu çağırır.
/// Bağlantı dizesi <see cref="ConnectionEnvironmentVariable"/> ortam değişkeninden okunur;
/// komut satırında parola görünmesin diye argüman olarak alınmaz.
/// </summary>
public static class SchemaProvisioningCli
{
    public const string Switch = "--ensure-schema";
    public const string ConnectionEnvironmentVariable = "MKFILO_SCHEMA_CONNECTION";

    public static bool IsRequested(string[] args) =>
        args.Any(a => string.Equals(a, Switch, StringComparison.OrdinalIgnoreCase));

    public static async Task<int> RunAsync()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var connectionString = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine($"HATA: {ConnectionEnvironmentVariable} ortam değişkeni boş. PostgreSQL bağlantı dizesi gerekli.");
            return 2;
        }

        // Web ile aynı varsayılan: Program.cs bu anahtarı UseNpgsql'den önce true yapar.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        try
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
            builder.Options = $"{builder.Options} -c lock_timeout=15000".Trim();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(builder.ConnectionString, npgsql =>
                {
                    npgsql.CommandTimeout(240);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                })
                .Options;
            await using var context = new ApplicationDbContext(options);
            Console.WriteLine($"Şema hazırlanıyor: {builder.Host}:{builder.Port}/{builder.Database}");

            await DbInitializer.ApplyDatabaseMigrationsAsync(context);
            // Program.cs açılışıyla aynı: model dışı sayaç tablosu da hazırlanır.
            await Migrations.SchemaSyncHelper.EnsureFisNoCountersSchemaAsync(context);

            var tableCount = await context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'")
                .SingleAsync();
            Console.WriteLine($"Şema hazır. public şemasında {tableCount} tablo var.");
            return 0;
        }
        catch (Exception ex) when (ex.GetBaseException() is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.LockNotAvailable })
        {
            Console.Error.WriteLine("HATA: Hedef PostgreSQL şeması kilitli. IIS uygulama havuzunu ve hedefi kullanan diğer işlemleri durdurup yeniden deneyin.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"HATA: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}

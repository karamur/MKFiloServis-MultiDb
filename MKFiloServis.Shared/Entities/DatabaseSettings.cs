namespace MKFiloServis.Shared.Entities;

public class DatabaseSettings
{
    public int Id { get; set; }
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.PostgreSQL;
    public DatabaseProvider CanonicalProvider { get; set; } = DatabaseProvider.PostgreSQL;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string DatabaseName { get; set; } = "MKFiloServis";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "Fast123";
    public bool UseIntegratedSecurity { get; set; } = false;
    public string? AdditionalOptions { get; set; }
    public string? TransitionManifestPath { get; set; }
    public DateTime? LastTransitionAtUtc { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public bool IsCanonicalProvider => CanonicalProvider == DatabaseProvider.PostgreSQL;

    public string GetConnectionString()
    {
        return Provider switch
        {
            DatabaseProvider.SQLite => $"Data Source={GetNormalizedSqliteDatabaseName()};",
            DatabaseProvider.PostgreSQL => $"Host={Host};Port={Port};Database={DatabaseName};Username={Username};Password={Password};Pooling=true;MinPoolSize=1;MaxPoolSize=20;",
            DatabaseProvider.MySQL => $"Server={Host};Port={Port};Database={DatabaseName};User={Username};Password={Password};",
            DatabaseProvider.SQLServer => UseIntegratedSecurity
                ? $"Server={Host},{Port};Database={DatabaseName};Integrated Security=True;TrustServerCertificate=True;"
                : $"Server={Host},{Port};Database={DatabaseName};User Id={Username};Password={Password};TrustServerCertificate=True;",
            _ => ""
        };
    }

    public int GetDefaultPort()
    {
        return Provider switch
        {
            DatabaseProvider.PostgreSQL => 5432,
            DatabaseProvider.MySQL => 3306,
            DatabaseProvider.SQLServer => 1433,
            DatabaseProvider.SQLite => 0,
            _ => 0
        };
    }

    public string GetProviderDisplayName()
    {
        return Provider switch
        {
            DatabaseProvider.SQLite => "SQLite",
            DatabaseProvider.MySQL => "MySQL",
            DatabaseProvider.SQLServer => "SQLServer",
            _ => "PostgreSQL"
        };
    }

    public bool UsesSupportedRuntimeProvider()
    {
        return Provider is DatabaseProvider.PostgreSQL or DatabaseProvider.SQLite or DatabaseProvider.MySQL or DatabaseProvider.SQLServer;
    }

    public string GetNormalizedSqliteDatabaseName()
    {
        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            return "MKFiloServis.db";
        }

        return Path.HasExtension(DatabaseName)
            ? DatabaseName
            : $"{DatabaseName}.db";
    }

    public static DatabaseProvider NormalizeRuntimeProvider(DatabaseProvider provider)
    {
        return provider is DatabaseProvider.PostgreSQL or DatabaseProvider.SQLite or DatabaseProvider.MySQL or DatabaseProvider.SQLServer
            ? provider
            : DatabaseProvider.PostgreSQL;
    }

    public static DatabaseProvider ParseProvider(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return DatabaseProvider.PostgreSQL;
        }

        return providerName.Trim().ToLowerInvariant() switch
        {
            "sqlite" => DatabaseProvider.SQLite,
            "postgresql" or "postgres" or "npgsql" => DatabaseProvider.PostgreSQL,
            "mysql" => DatabaseProvider.MySQL,
            "sqlserver" or "sql server" or "mssql" => DatabaseProvider.SQLServer,
            _ => DatabaseProvider.PostgreSQL
        };
    }
}

public enum DatabaseProvider
{
    SQLite = 1,
    PostgreSQL = 2,
    MySQL = 3,
    SQLServer = 4
}



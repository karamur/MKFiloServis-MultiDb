using Npgsql;

namespace KOAFiloServis.Web.Services;

public sealed class TenantConnectionStringProvider : ITenantConnectionStringProvider
{
    private readonly IAktifFirmaProvider _firmaProvider;
    private readonly string _masterConnectionString;
    private readonly string _sharedLegacyConnectionString;
    private readonly string _dbHost;
    private readonly int _dbPort;
    private readonly string _dbUsername;
    private readonly string _dbPassword;

    public TenantConnectionStringProvider(
        IAktifFirmaProvider firmaProvider,
        IConfiguration configuration)
    {
        _firmaProvider = firmaProvider;
        _masterConnectionString = configuration.GetConnectionString("MasterConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("MasterConnection veya DefaultConnection bulunamadı.");

        _sharedLegacyConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? _masterConnectionString;

        var builder = new NpgsqlConnectionStringBuilder(_sharedLegacyConnectionString);
        _dbHost = builder.Host ?? "localhost";
        _dbPort = builder.Port;
        _dbUsername = builder.Username ?? "postgres";
        _dbPassword = builder.Password ?? "";
    }

    public string? GetTenantConnectionString()
    {
        var firmaId = _firmaProvider.AktifFirmaId;
        if (firmaId == null || firmaId.Value == 0)
            return _sharedLegacyConnectionString;

        var databaseName = _firmaProvider.Mevcut.DatabaseName;
        return GetConnectionStringForFirma(firmaId.Value, databaseName);
    }

    public string? GetConnectionStringForFirma(int firmaId, string? databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return _sharedLegacyConnectionString;

        return new NpgsqlConnectionStringBuilder
        {
            Host = _dbHost,
            Port = _dbPort,
            Database = databaseName,
            Username = _dbUsername,
            Password = _dbPassword,
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 10,
            CommandTimeout = 30
        }.ConnectionString;
    }

    public string GetMasterConnectionString() => _masterConnectionString;
}

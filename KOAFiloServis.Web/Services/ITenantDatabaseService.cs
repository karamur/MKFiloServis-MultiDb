namespace KOAFiloServis.Web.Services;

[System.Obsolete("Nihai mimari: Tenant DB yaklasimi terk edildi. Tek KOAFiloServis DB kullaniliyor.")]
public interface ITenantDatabaseService
{
    Task CreateTenantDatabaseAsync(int firmaId, bool migrateData = true);
    Task<bool> TenantDatabaseExistsAsync(string databaseName);
    Task MigrateFirmaDataAsync(int firmaId);

    /// <summary>
    /// Belirtilen tenant DB'ye bekleyen EF Core migration'larini uygular.
    /// </summary>
    Task<int> ApplyPendingMigrationsAsync(int firmaId);

    /// <summary>
    /// DatabaseName atanmis tum tenant DB'lere bekleyen migration'lari uygular.
    /// Returns: (toplam firma, guncellenen firma, hata sayisi)
    /// </summary>
    Task<(int Total, int Updated, int Errors)> ApplyPendingMigrationsToAllTenantsAsync();
}

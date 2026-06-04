namespace KOAFiloServis.Web.Services;

[System.Obsolete("Nihai mimari (2026): Tenant DB yaklasimi terk edildi. Tek connection string kullanin.")]
public interface ITenantConnectionStringProvider
{
    string? GetTenantConnectionString();
    string? GetConnectionStringForFirma(int firmaId, string? databaseName);
    string GetMasterConnectionString();
}

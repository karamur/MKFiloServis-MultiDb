namespace KOAFiloServis.Web.Services;

public interface ITenantConnectionStringProvider
{
    string? GetTenantConnectionString();
    string? GetConnectionStringForFirma(int firmaId, string? databaseName);
    string GetMasterConnectionString();
}

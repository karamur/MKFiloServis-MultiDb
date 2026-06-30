namespace MKFiloServis.Web.Services.Interfaces;

public interface IPortalProjectCatalogService
{
    PortalProjectCatalogOptions GetCatalog();
    IReadOnlyList<PortalProjectDefinition> GetProjects();
    PortalProjectDefinition? GetProjectBySlug(string? slug);
    PortalProjectDefinition GetDefaultProject();
    PortalProjectCatalogOptions CreateEditableCopy();
    Task SaveAsync(PortalProjectCatalogOptions settings);
}




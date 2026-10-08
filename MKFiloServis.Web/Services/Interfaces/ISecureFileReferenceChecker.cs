namespace MKFiloServis.Web.Services.Interfaces;

public interface ISecureFileReferenceChecker
{
    Task<bool> IsReferencedAsync(string relativePath, CancellationToken cancellationToken = default);
}

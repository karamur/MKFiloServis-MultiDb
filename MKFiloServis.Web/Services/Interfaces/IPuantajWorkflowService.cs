using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Puantaj onay workflow yönetimi.
/// Finans → Muhasebe → Kilit zinciri.
/// </summary>
public interface IPuantajWorkflowService
{
    Task FinansOnaylaAsync(int hesapDonemiId, string onaylayan, CancellationToken ct = default);
    Task MuhasebeOnaylaAsync(int hesapDonemiId, string onaylayan, CancellationToken ct = default);
    Task KilitleAsync(int hesapDonemiId, string kilitleyen, string? aciklama = null, CancellationToken ct = default);
    Task KilitAcAsync(int hesapDonemiId, string acan, CancellationToken ct = default);

    Task<List<PuantajAuditLog>> GetAuditLogsAsync(int hesapDonemiId, CancellationToken ct = default);

    Task<bool> FinansOnaylanabilirMiAsync(int hesapDonemiId);
    Task<bool> MuhasebeOnaylanabilirMiAsync(int hesapDonemiId);
    Task<bool> KilitlenebilirMiAsync(int hesapDonemiId);
    Task<bool> RevizyonYapilabilirMiAsync(int hesapDonemiId);
}





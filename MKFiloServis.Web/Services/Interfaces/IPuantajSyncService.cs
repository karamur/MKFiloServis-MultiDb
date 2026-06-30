using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

public enum PuantajSyncMode
{
    CreateUpdate,
    ForceOverwrite
}

public interface IPuantajSyncService
{
    Task SyncFromPuantajAsync(PuantajKayit puantaj, PuantajSyncMode mode, Guid? batchId = null);
    Task<SyncOzetRaporu> SyncFromPuantajTopluAsync(int yil, int ay, List<PuantajKayit>? onlyThese = null, PuantajSyncMode mode = PuantajSyncMode.CreateUpdate);
    Task<SyncOzetRaporu> ForceSyncForDonemAsync(int kurumId, int yil, int ay);
    Task DeleteLinkedOpsAsync(int puantajKayitId);
    Task<SyncOzetRaporu> BackfillAsync(int yil, int ay, int? kurumId = null);
}





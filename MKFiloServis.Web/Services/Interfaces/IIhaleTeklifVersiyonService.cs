using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IIhaleTeklifVersiyonService
{
    Task<IhaleTeklifVersiyon?> GetByIdAsync(int versiyonId);
    Task<List<IhaleTeklifVersiyon>> GetListByIhaleProjeIdAsync(int ihaleProjeId);
    Task<IhaleTeklifVersiyon?> GetAktifVersiyonAsync(int ihaleProjeId);
    Task<List<IhaleTeklifKararLog>> GetKararLoglariAsync(int versiyonId);

    Task<IhaleTeklifVersiyon> CreateInitialAsync(int ihaleProjeId);
    Task<IhaleTeklifVersiyon> CreateRevisionAsync(int kaynakVersiyonId, string? revizyonNotu);
    Task<IhaleTeklifVersiyon> SetActiveAsync(int versiyonId);
    Task<IhaleTeklifVersiyon> SendToReviewAsync(int versiyonId);
    Task<IhaleTeklifVersiyon> ApproveAsync(int versiyonId, string? kararNotu);
    Task<IhaleTeklifVersiyon> RejectAsync(int versiyonId, string kararNotu);
}




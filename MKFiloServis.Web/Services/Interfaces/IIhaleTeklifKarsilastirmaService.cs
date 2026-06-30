using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IIhaleTeklifKarsilastirmaService
{
    Task<IhaleTeklifKarsilastirmaDto?> CompareAsync(int solVersiyonId, int sagVersiyonId);
}




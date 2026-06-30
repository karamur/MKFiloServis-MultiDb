using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface ITekrarlayanOdemeService
{
    Task<List<TekrarlayanOdeme>> GetTekrarlayanOdemelerAsync(int? firmaId = null);
    Task<List<TekrarlayanOdeme>> GetAktifTekrarlayanOdemelerAsync(int? firmaId = null);
    Task<TekrarlayanOdeme?> GetTekrarlayanOdemeByIdAsync(int id);
    Task<TekrarlayanOdeme> CreateTekrarlayanOdemeAsync(TekrarlayanOdeme odeme);
    Task<TekrarlayanOdeme> UpdateTekrarlayanOdemeAsync(TekrarlayanOdeme odeme);
    Task DeleteTekrarlayanOdemeAsync(int id);
    Task<int> TekrarlayanOdemelerdenKayitOlusturAsync(int yil, int ay, int? firmaId = null);
}




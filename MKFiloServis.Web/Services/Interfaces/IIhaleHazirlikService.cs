using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IIhaleHazirlikService
{
    // Proje CRUD
    Task<List<IhaleProje>> GetProjelerAsync();
    Task<IhaleProje?> GetProjeByIdAsync(int id);
    Task<IhaleProje> CreateProjeAsync(IhaleProje proje);
    Task<IhaleProje> UpdateProjeAsync(IhaleProje proje);
    Task<bool> DeleteProjeAsync(int id);
    Task<IhaleProje> KopyalaProjeAsync(int projeId, string yeniProjeAdi);
    Task<string> GenerateProjeKoduAsync();

    // Güzergah kalem CRUD
    Task<IhaleGuzergahKalem> AddKalemAsync(IhaleGuzergahKalem kalem);
    Task<IhaleGuzergahKalem> UpdateKalemAsync(IhaleGuzergahKalem kalem);
    Task<bool> DeleteKalemAsync(int kalemId);

    // Sözleşme revizyon / ek protokol
    Task<List<IhaleSozlesmeRevizyon>> GetSozlesmeRevizyonlariAsync(int ihaleProjeId);
    Task<IhaleSozlesmeRevizyon> AddSozlesmeRevizyonAsync(IhaleSozlesmeRevizyon revizyon);
    Task<IhaleSozlesmeRevizyon> UpdateSozlesmeRevizyonAsync(IhaleSozlesmeRevizyon revizyon);
    Task<bool> DeleteSozlesmeRevizyonAsync(int revizyonId);

    // Hesaplamalar
    Task HesaplaKalemMaliyetAsync(IhaleGuzergahKalem kalem, IhaleProje proje);
    Task<IhaleProjeOzet> GetProjeOzetAsync(int projeId);
    Task<IhaleGerceklesenAnalizOzet> GetProjeGerceklesenAnalizAsync(int projeId);
    Task<IhaleOperasyonDashboardOzet> GetOperasyonDashboardOzetAsync();
    List<AylikProjeksiyon> HesaplaEnflasyonluProjeksiyon(IhaleGuzergahKalem kalem, IhaleProje proje);

    // AI Tahmin
    Task<IhaleMaliyetTahminSonuc> AIAracMasrafTahminAsync(IhaleMaliyetTahminIstek istek);
    Task<IhaleSoforMaasTahmin> AISoforMaasTahminAsync(string aracTipi, decimal mesafeKm, int seferSayisi, decimal enflasyonOrani, int sozlesmeSuresiAy);
    Task<string> AIProjeAnalizAsync(int projeId);

    // Veri yardımcıları
    Task<decimal> GetGecmisMasrafOrtalamaAsync(int? aracId, MasrafKategori kategori, int aySayisi = 12);
    Task<decimal> GetGecmisSoforMaasOrtalamaAsync(int aySayisi = 6);

    // Örnek veri oluşturma
    Task<IhaleProje> OrnekProjeOlusturAsync();
}




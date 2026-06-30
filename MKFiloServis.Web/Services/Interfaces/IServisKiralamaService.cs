using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IServisKiralamaService
{
    // Kiralama Ara� ��lemleri
    Task<List<KiralamaArac>> GetTumKiralamaAraclarAsync(int firmaId);
    Task<List<KiralamaArac>> GetAktifKiralamaAraclarAsync(int firmaId);
    Task<KiralamaArac?> GetKiralamaAracByIdAsync(int id);
    Task<KiralamaArac> CreateKiralamaAracAsync(KiralamaArac arac);
    Task<KiralamaArac> UpdateKiralamaAracAsync(KiralamaArac arac);
    Task DeleteKiralamaAracAsync(int id);

    // Servis �al��ma ��lemleri
    Task<List<ServisCalismaKiralama>> GetServisCalismalariAsync(int firmaId, DateTime baslangic, DateTime bitis);
    Task<ServisCalismaKiralama?> GetServisCalismaByIdAsync(int id);
    Task<ServisCalismaKiralama> CreateServisCalismaAsync(ServisCalismaKiralama calisma);
    Task<ServisCalismaKiralama> UpdateServisCalismaAsync(ServisCalismaKiralama calisma);
    Task DeleteServisCalismaAsync(int id);

    // Toplu ��lemler
    Task<List<ServisCalismaKiralama>> HaftalikPlanOlusturAsync(int firmaId, DateTime haftaBaslangic);
    Task<ServisCalismaKiralama> HesaplaAsync(int id); // Net kazan� hesaplama

    // Raporlar
    Task<List<ServisCalismaRapor>> GetServisCalismaRaporuAsync(int? firmaId, DateTime baslangic, DateTime bitis, 
        AracSahiplikTuru? sahiplikTuru = null);
    Task<Dictionary<string, decimal>> GetAracBazindaKazancAsync(int firmaId, DateTime baslangic, DateTime bitis);
    Task<Dictionary<string, decimal>> GetGuzergahBazindaKazancAsync(int firmaId, DateTime baslangic, DateTime bitis);
    Task<Dictionary<int, int>> GetAylikServisSayisiAsync(int firmaId, int yil);

    // Excel Export
    Task<byte[]> ExportServisCalismaRaporuAsync(int? firmaId, DateTime baslangic, DateTime bitis);
    Task<byte[]> ExportKiralamaAracListesiAsync(int firmaId);
    Task<byte[]> ExportAylikOzetAsync(int firmaId, int yil, int ay);

    // �statistikler
    Task<int> GetToplamKiralamaAracSayisiAsync(int firmaId);
    Task<int> GetAylikServisSayisiAsync(int firmaId, int yil, int ay);
    Task<decimal> GetAylikToplamKazancAsync(int firmaId, int yil, int ay);
    Task<decimal> GetAylikKiraBedeliAsync(int firmaId, int yil, int ay);
}




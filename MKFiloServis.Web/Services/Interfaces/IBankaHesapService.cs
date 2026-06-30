using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IBankaHesapService
{
    Task<List<BankaHesap>> GetAllAsync();
    Task<List<BankaHesap>> GetActiveAsync();
    Task<List<BankaHesap>> GetByTipAsync(HesapTipi tip);
    Task<BankaHesap?> GetByIdAsync(int id);
    Task<BankaHesap> CreateAsync(BankaHesap bankaHesap);
    Task<BankaHesap> UpdateAsync(BankaHesap bankaHesap);
    Task DeleteAsync(int id);
    Task<string> GenerateNextKodAsync();
    Task<decimal> GetBakiyeAsync(int hesapId);
    Task<Dictionary<int, decimal>> GetTumHesapBakiyeleriAsync();

    /// <summary>
    /// FirmaId değeri NULL olan banka/kasa hesaplarını döndürür (tenant filtresi devre dışı).
    /// Kullanıcı bunları görüp aktif firmaya/seçtiği firmaya ilişkilendirebilsin diye.
    /// </summary>
    Task<List<BankaHesap>> GetFirmasizHesaplarAsync();

    /// <summary>
    /// Verilen hesabın FirmaId'sini ayarlar (tenant filtresi bypass edilir).
    /// </summary>
    Task AssignFirmaAsync(int hesapId, int firmaId);

    /// <summary>FirmaId null olan banka hesap sayısı.</summary>
    Task<int> GetFirmaIdYokSayisiAsync();
}




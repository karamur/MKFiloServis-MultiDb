using MKFiloServis.Shared.Entities;
using Microsoft.AspNetCore.Components.Forms;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IAracService
{
    // Araç CRUD İşlemleri
    Task<List<Arac>> GetAllAsync();
    Task<List<Arac>> GetActiveAsync();
    Task<int> GetActiveCountAsync();
    Task<Arac?> GetByIdAsync(int id);
    Task<Arac?> GetByPlakaAsync(string plaka);
    Task<Arac?> GetBySaseNoAsync(string saseNo);
    Task<bool> SaseNoMevcutMu(string saseNo, int? haricAracId = null);
    Task<bool> PlakaMevcutMu(string plaka, int? haricAracPlakaId = null);
    Task<Arac> CreateAsync(Arac arac, string plaka, PlakaIslemTipi islemTipi = PlakaIslemTipi.Alis,
        decimal? islemTutari = null, int? cariId = null, string? aciklama = null);
    Task<Arac> UpdateAsync(Arac arac);
    Task DeleteAsync(int id);

    /// <summary>
    /// FirmaId değeri null olan araçların FirmaId'sini verilen firmaya atar.
    /// Eski (multi-tenant öncesi) kayıtların puantaj/fatura akışlarında hata vermemesi için kullanılır.
    /// </summary>
    /// <returns>Güncellenen araç sayısı</returns>
    Task<int> BackfillFirmaIdAsync(int firmaId);

    /// <summary>FirmaId değeri null olan araç sayısı.</summary>
    Task<int> GetFirmaIdYokSayisiAsync();

    // Plaka İşlemleri
    Task<List<AracPlaka>> GetPlakaGecmisiAsync(int aracId);
    Task<AracPlaka> PlakaEkle(int aracId, string yeniPlaka, PlakaIslemTipi islemTipi,
        decimal? islemTutari = null, int? cariId = null, string? aciklama = null);
    Task PlakaCikis(int aracPlakaId, PlakaIslemTipi cikisIslemTipi,
        decimal? islemTutari = null, int? cariId = null, string? aciklama = null);
    Task<bool> AddPlakaToAracAsync(AracPlaka yeniPlaka);
    Task<bool> DeletePlakaFromAracAsync(int aracPlakaId);
    Task ClosePlakaAsync(int aracPlakaId, DateTime cikisTarihi);

    // Satışa Açık Araçlar
    Task<List<Arac>> GetSatisaAcikAraclarAsync();
    Task AracSatisaAc(int aracId, decimal satisFiyati, string? aciklama = null);
    Task AracSatisKapat(int aracId);

    // Arac Evrak Islemleri
    Task<List<AracEvrak>> GetAracEvraklariAsync(int aracId);
    Task<AracEvrak?> GetAracEvrakByIdAsync(int evrakId);
    Task<AracEvrak> CreateAracEvrakAsync(AracEvrak evrak);
    Task<AracEvrak> UpdateAracEvrakAsync(AracEvrak evrak);
    Task DeleteAracEvrakAsync(int evrakId);

    // Evrak Dosya Islemleri
    Task<AracEvrakDosya> UploadEvrakDosyaAsync(int evrakId, IBrowserFile file);
    Task<byte[]> GetEvrakDosyaAsync(int dosyaId);
    Task DeleteEvrakDosyaAsync(int dosyaId);

    // Evrak Uyarilari
    Task<List<AracEvrak>> GetSuresiDolacakEvraklarAsync(int gunSayisi = 30);

    // Excel Import/Export
    Task<byte[]> GetExcelSablonAsync();
    Task<AracImportResult> ImportFromExcelAsync(byte[] fileContent);
}

// Araç Import Result
public class AracImportResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> SkippedRecords { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}




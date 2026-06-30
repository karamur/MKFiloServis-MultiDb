using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IFaturaService
{
    Task<List<Fatura>> GetAllAsync();
    Task<PagedResult<Fatura>> GetPagedAsync(FaturaFilterParams filter); // Sayfalı ve filtrelenmiş
    Task<List<Fatura>> GetByCariIdAsync(int cariId);
    Task<List<Fatura>> GetByTipAsync(FaturaTipi tip);
    Task<List<Fatura>> GetByDurumAsync(FaturaDurum durum);
    Task<List<Fatura>> GetOdenmemisFaturalarAsync();
    Task<List<Fatura>> GetOdenmisFaturalarAsync();
    Task<List<Fatura>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<Fatura?> GetByIdAsync(int id);
    Task<Fatura?> GetByIdWithKalemlerAsync(int id);
    Task<Fatura> CreateAsync(Fatura fatura);
    Task<Fatura> UpdateAsync(Fatura fatura);
    Task DeleteAsync(int id);
    Task<string> GenerateNextFaturaNoAsync(FaturaTipi tip, FaturaYonu? yon = null, int? firmaId = null);
    Task UpdateOdenenTutarAsync(int faturaId);
    
    // Muhasebe Fişi
    Task<MuhasebeFis> CreateMuhasebeFisiAsync(int faturaId);
    
    // E-Fatura / E-Arsiv metodlari
    Task<List<Fatura>> GetByYonAsync(FaturaYonu yon, int? firmaId = null);
    Task<List<Fatura>> GetByYonAndDateRangeAsync(FaturaYonu yon, DateTime? baslangic, DateTime? bitis, int? firmaId = null);
    Task<List<Fatura>> GetByEFaturaTipiAsync(EFaturaTipi tip);
    Task<EFaturaImportResult> ImportFromExcelAsync(byte[] fileContent, FaturaYonu yon, int? firmaId = null, EFaturaTipi? eFaturaTipi = null);
    Task<EFaturaImportResult> ImportFromXmlAsync(List<XmlFileContent> xmlFiles, FaturaYonu yon, int? firmaId = null, EFaturaTipi? eFaturaTipi = null);
    Task<EFaturaImportResult> ImportFromXmlWithPdfAsync(List<XmlPdfFileContent> files, FaturaYonu yon, int? firmaId = null, EFaturaTipi? eFaturaTipi = null);
    Task<bool> UploadFaturaPdfAsync(int faturaId, string fileName, byte[] pdfContent);
    Task<FaturaStoredFile?> GetFaturaDosyaAsync(int faturaId, FaturaDosyaTuru dosyaTuru);

    // Excel Sablon ve Export - Yeni format (ornek dosya ile uyumlu)
    Task<byte[]> GetExcelSablonAsync(FaturaYonu yon);
    Task<byte[]> ExportToExcelAsync(List<Fatura> faturalar);

    // Dashboard optimized methods
    Task<DashboardFaturaStats> GetDashboardStatsAsync();
    
    // Fatura Kalemleri - Stok Türü Eşleştirme
    Task<List<FaturaKalem>> GetFaturaKalemleriAsync(DateTime? baslangic = null, DateTime? bitis = null);
    Task<List<FaturaKalem>> GetEslesmemisKalemleriAsync(DateTime? baslangic = null, DateTime? bitis = null);
    Task<List<FaturaKalem>> GetEslesmisKalemleriAsync(DateTime? baslangic = null, DateTime? bitis = null);
    Task<StokKartiOlusturSonuc> UpdateFaturaKalemleriVeStokKartiOlusturAsync(List<FaturaKalem> kalemler, bool stokKartiOlustur = true);
    Task UpdateFaturaKalemleriAsync(List<FaturaKalem> kalemler);

    // Firmalar Arası Fatura - Mahsup İşlemleri
    Task<bool> MahsupKapatAsync(int faturaId);
    Task<List<Fatura>> GetFirmalarArasiEslesmemisFaturalarAsync(int? firmaId = null);
    Task<bool> FaturalariEslestirAsync(int fatura1Id, int fatura2Id);
}

public class StokKartiOlusturSonuc
{
    public int GuncellenenKalemSayisi { get; set; }
    public int OlusturulanStokKartiSayisi { get; set; }
    public int AtlananStokKartiSayisi { get; set; }
    public int OlusturulanStokHareketSayisi { get; set; }
    public int OlusturulanGiderKayitSayisi { get; set; }
    public List<string> Hatalar { get; set; } = new();
}

public class DashboardFaturaStats
{
    public int BekleyenFaturaSayisi { get; set; }
    public decimal BuAyGelir { get; set; }
    public decimal BuAyGider { get; set; }
    public List<Fatura> VadeGecmisFaturalar { get; set; } = [];
    public List<Fatura> VadeYaklasanFaturalar { get; set; } = [];
}

public class EFaturaImportResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Fatura> ImportedItems { get; set; } = new();
    public Dictionary<int, string> FaturaXmlMapping { get; set; } = new(); // FaturaId -> XmlFileName
}

public class XmlFileContent
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public class XmlPdfFileContent
{
    public string XmlFileName { get; set; } = string.Empty;
    public byte[] XmlContent { get; set; } = Array.Empty<byte>();
    public string? PdfFileName { get; set; }
    public byte[]? PdfContent { get; set; }
}

/// <summary>
/// Fatura listesi için filtre parametreleri
/// </summary>
public class FaturaFilterParams : PagingParameters
{
    public string? SearchTerm { get; set; }
    public FaturaTipi? FaturaTipi { get; set; }
    public FaturaDurum? Durum { get; set; }
    public FaturaYonu? Yon { get; set; }
    public int? FirmaId { get; set; }
    public int? CariId { get; set; }
    public DateTime? BaslangicTarih { get; set; }
    public DateTime? BitisTarih { get; set; }
}

public enum FaturaDosyaTuru
{
    Pdf = 1,
    Xml = 2
}

public class FaturaStoredFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}




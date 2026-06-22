using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services.Interfaces;

public interface IKiralikPlakaTakipService
{
    Task<List<KiralikPlakaTakip>> GetAllAsync();
    Task<KiralikPlakaTakip?> GetByIdAsync(int id);
    Task<KiralikPlakaTakip> CreateAsync(KiralikPlakaTakip entity);
    Task<KiralikPlakaTakip> UpdateAsync(KiralikPlakaTakip entity);
    Task DeleteAsync(int id);
    Task<byte[]> GetExcelSablonAsync();
    Task<KiralikPlakaImportResult> ImportFromExcelAsync(byte[] fileContent);
    Task<AracEslestirmeSonuc> EslestirmeYapAsync();
    Task<int> FaturaEslestirmeYapAsync();
}

public class KiralikPlakaImportResult
{
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount => Errors.Count;
    public List<string> Errors { get; set; } = new();
    public List<string> SkippedRecords { get; set; } = new();
    public bool Success { get; set; }
}

public class AracEslestirmeSonuc
{
    public int Eslesen { get; set; }
    public int CokluAdayNedeniyleAtlanan { get; set; }
    public int PasifAracNedeniyleAtlanan { get; set; }
    public int EslesmeBulunamayan { get; set; }
}

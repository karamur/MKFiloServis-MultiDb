using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IGuzergahService
{
    Task<List<Guzergah>> GetAllAsync();
    Task<List<Guzergah>> GetActiveAsync();
    Task<List<Guzergah>> GetByCariIdAsync(int cariId);
    Task<List<Guzergah>> GetByFirmaIdAsync(int firmaId);
    Task<Guzergah?> GetByIdAsync(int id);
    Task<Guzergah> CreateAsync(Guzergah guzergah);
    Task<Guzergah> AddAsync(Guzergah guzergah);
    Task<Guzergah> UpdateAsync(Guzergah guzergah);
    Task UpdateWithSeferlerAsync(Guzergah guzergah, List<GuzergahSefer> seferler);
    Task DeleteAsync(int id);
    Task<string> GenerateNextKodAsync();
    Task<string> GenerateGuzergahKoduAsync(int firmaId);

    // Doğrulama metodları
    Task<bool> FaturaKalemdenGuzergahVarMiAsync(int faturaKalemId);
    Task<Guzergah?> GetByFaturaKalemIdAsync(int faturaKalemId);
    Task<bool> BenzersizGuzergahMiAsync(int firmaId, string guzergahAdi, int? haricId = null);

    // Excel import
    Task<GuzergahImportSonuc> ImportFromExcelAsync(Stream excelStream, int firmaId);
}

public sealed class GuzergahImportSonuc
{
    public int ToplamSatir { get; init; }
    public int Basarili { get; init; }
    public int Atlandi { get; init; }
    public int Hatali { get; init; }
    public List<GuzergahImportSatir> Satirlar { get; init; } = new();
}

public sealed class GuzergahImportSatir
{
    public int SatirNo { get; init; }
    public string? GuzergahKodu { get; init; }
    public string? GuzergahAdi { get; init; }
    public bool Basarili { get; init; }
    public string? Mesaj { get; init; }
}




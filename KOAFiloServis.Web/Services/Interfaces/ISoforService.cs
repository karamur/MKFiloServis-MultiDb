using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services;

public interface ISoforService
{
    // Tüm Personel İşlemleri
    Task<List<Sofor>> GetAllAsync();
    Task<List<Sofor>> GetActiveAsync();
    Task<int> GetActiveCountAsync();
    Task<Sofor?> GetByIdAsync(int id);
    Task<Sofor> CreateAsync(Sofor sofor);
    Task<Sofor> UpdateAsync(Sofor sofor, DateTime? expectedUpdatedAt = null);
    Task DeleteAsync(int id, int? deletedBy = null);
    Task<Sofor> RestoreAsync(int id);
    Task<string> GenerateNextKodAsync();
    Task<string> GenerateNextKodAsync(PersonelGorev gorev);

    // Görev bazlı filtreleme
    Task<List<Sofor>> GetByGorevAsync(PersonelGorev gorev);
    Task<List<Sofor>> GetActiveSoforlerAsync(); // Sadece aktif şoförler
    Task<List<Sofor>> GetActiveByGorevAsync(PersonelGorev gorev);
    Task<int> GetActiveByGorevCountAsync(PersonelGorev gorev);

    // Muhasebe Hesap Entegrasyonu
    Task<int> TopluMuhasebeHesabiOlusturAsync();
    Task<List<MuhasebeHesap>> GetPersonelMuhasebeHesaplariAsync();
    Task<List<MuhasebeHesap>> GetPersonelAvansHesaplariAsync();
    Task<MuhasebeHesap?> GetPersonelAvansHesabiAsync(int soforId);
    Task<MuhasebeHesap?> GetPersonelBorcHesabiAsync(int soforId);
    Task EnsurePersonelBorcHesabiAsync(Sofor sofor);
    Task EnsurePersonelAvansHesabiAsync(Sofor sofor);
    Task EnsurePersonelCariKaydiAsync(Sofor sofor);
    Task AvansHesabiAtaAsync(int soforId, int avansHesapId);

    // Excel Import/Export
    Task<byte[]> GetImportSablonAsync();
    Task<PersonelImportSonuc> ImportFromExcelAsync(byte[] excelData, bool mevcutGuncelle = false);
    Task<byte[]> ExportToExcelAsync();
}

/// <summary>
/// Excel import işlem sonucu
/// </summary>
public class PersonelImportSonuc
{
    public int ToplamSatir { get; set; }
    public int BasariliEklenen { get; set; }
    public int BasariliGuncellenen { get; set; }
    public int Atlanan { get; set; }
    public List<PersonelImportHata> Hatalar { get; set; } = new();
    public bool Basarili => !Hatalar.Any(h => h.Kritik);
}

public class PersonelImportHata
{
    public int SatirNo { get; set; }
    public string Kolon { get; set; } = string.Empty;
    public string Mesaj { get; set; } = string.Empty;
    public bool Kritik { get; set; }
}

using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Destek Talebi (Ticket) Servisi Interface - osTicket benzeri
/// </summary>
public interface IDestekTalebiService
{
    // === Talep CRUD ===
    Task<List<DestekTalebi>> GetAllAsync();
    Task<PagedResult<DestekTalebi>> GetPagedAsync(DestekTalebiFilterParams filter);
    Task<DestekTalebi?> GetByIdAsync(int id);
    Task<DestekTalebi?> GetByIdWithDetailsAsync(int id);
    Task<DestekTalebi?> GetByTalepNoAsync(string talepNo);
    Task<DestekTalebi> CreateAsync(DestekTalebi talep);
    Task<DestekTalebi> UpdateAsync(DestekTalebi talep);
    Task DeleteAsync(int id);
    Task<string> GenerateNextTalepNoAsync();
    
    // === Durum ve Atama ===
    Task<bool> UpdateDurumAsync(int talepId, DestekDurum yeniDurum, int? kullaniciId = null);
    Task<bool> UpdateOncelikAsync(int talepId, DestekOncelik yeniOncelik, int? kullaniciId = null);
    Task<bool> AtaAsync(int talepId, int atananKullaniciId, int? atayanKullaniciId = null);
    Task<bool> TransferEtAsync(int talepId, int yeniDepartmanId, int? kullaniciId = null);
    Task<bool> KapatAsync(int talepId, int? kullaniciId = null, string? kapatmaNotu = null);
    Task<bool> YenidenAcAsync(int talepId, int? kullaniciId = null, string? aciklamaNotu = null);
    Task<bool> BirlestirAsync(int anaTalepId, int birlestirilecekTalepId, int? kullaniciId = null);
    
    // === Yanıt İşlemleri ===
    Task<DestekTalebiYanit> AddYanitAsync(int talepId, DestekTalebiYanit yanit);
    Task<List<DestekTalebiYanit>> GetYanitlarAsync(int talepId);
    Task<bool> DeleteYanitAsync(int yanitId);
    
    // === Dosya Ekleri ===
    Task<DestekTalebiEk> AddEkAsync(int talepId, DestekTalebiEk ek, Stream fileStream);
    Task<DestekTalebiEk> AddYanitEkAsync(int yanitId, DestekTalebiEk ek, Stream fileStream);
    Task<List<DestekTalebiEk>> GetEklerAsync(int talepId);
    Task<DestekTalebiEk?> GetEkByIdAsync(int ekId);
    Task<Stream?> GetEkDosyaStreamAsync(int ekId);
    Task<bool> DeleteEkAsync(int ekId);
    
    // === Filtre ve Arama ===
    Task<List<DestekTalebi>> GetByDurumAsync(DestekDurum durum);
    Task<List<DestekTalebi>> GetByDepartmanAsync(int departmanId);
    Task<List<DestekTalebi>> GetByKategoriAsync(int kategoriId);
    Task<List<DestekTalebi>> GetByAtananKullaniciAsync(int kullaniciId);
    Task<List<DestekTalebi>> GetByCariAsync(int cariId);
    Task<List<DestekTalebi>> AramaAsync(string aramaMetni);
    Task<List<DestekTalebi>> GetSlaAsildiBekleyenlerAsync();
    
    // === Departman CRUD ===
    Task<List<DestekDepartman>> GetDepartmanlarAsync();
    Task<DestekDepartman?> GetDepartmanByIdAsync(int id);
    Task<DestekDepartman> CreateDepartmanAsync(DestekDepartman departman);
    Task<DestekDepartman> UpdateDepartmanAsync(DestekDepartman departman);
    Task DeleteDepartmanAsync(int id);
    Task<List<DestekDepartmanUye>> GetDepartmanUyeleriAsync(int departmanId);
    Task<bool> AddDepartmanUyeAsync(int departmanId, int kullaniciId, bool yonetici = false);
    Task<bool> RemoveDepartmanUyeAsync(int departmanId, int kullaniciId);
    
    // === Kategori CRUD ===
    Task<List<DestekKategori>> GetKategorilerAsync();
    Task<List<DestekKategori>> GetKategorilerByDepartmanAsync(int departmanId);
    Task<DestekKategori?> GetKategoriByIdAsync(int id);
    Task<DestekKategori> CreateKategoriAsync(DestekKategori kategori);
    Task<DestekKategori> UpdateKategoriAsync(DestekKategori kategori);
    Task DeleteKategoriAsync(int id);
    
    // === Hazır Yanıtlar ===
    Task<List<DestekHazirYanit>> GetHazirYanitlarAsync(int? departmanId = null, int? kategoriId = null);
    Task<DestekHazirYanit?> GetHazirYanitByIdAsync(int id);
    Task<DestekHazirYanit> CreateHazirYanitAsync(DestekHazirYanit hazirYanit);
    Task<DestekHazirYanit> UpdateHazirYanitAsync(DestekHazirYanit hazirYanit);
    Task DeleteHazirYanitAsync(int id);
    Task IncrementHazirYanitKullanimAsync(int hazirYanitId);
    
    // === Bilgi Bankası ===
    Task<PagedResult<DestekBilgiBankasi>> GetBilgiBankasiPagedAsync(BilgiBankasiFilterParams filter);
    Task<List<DestekBilgiBankasi>> GetBilgiBankasiAramaAsync(string aramaMetni);
    Task<DestekBilgiBankasi?> GetBilgiBankasiByIdAsync(int id);
    Task<DestekBilgiBankasi?> GetBilgiBankasiBySlugAsync(string slug);
    Task<DestekBilgiBankasi> CreateBilgiBankasiAsync(DestekBilgiBankasi makale);
    Task<DestekBilgiBankasi> UpdateBilgiBankasiAsync(DestekBilgiBankasi makale);
    Task DeleteBilgiBankasiAsync(int id);
    Task IncrementBilgiBankasiGoruntulenmeAsync(int makaleId);
    Task<bool> YararliBulAsync(int makaleId, bool yararli);
    
    // === SLA ===
    Task<List<DestekSla>> GetSlaListesiAsync();
    Task<DestekSla?> GetSlaByIdAsync(int id);
    Task<DestekSla?> GetSlaByOncelikAsync(DestekOncelik oncelik);
    Task<DestekSla> CreateSlaAsync(DestekSla sla);
    Task<DestekSla> UpdateSlaAsync(DestekSla sla);
    Task DeleteSlaAsync(int id);
    Task<int> CheckAndUpdateSlaViolationsAsync();
    
    // === Aktivite ve Raporlama ===
    Task<List<DestekTalebiAktivite>> GetAktivitelerAsync(int talepId);
    Task LogAktiviteAsync(int talepId, AktiviteTuru aktiviteTuru, string aciklama, int? kullaniciId = null, string? eskiDeger = null, string? yeniDeger = null);
    
    // === Dashboard ve İstatistikler ===
    Task<DestekDashboardStats> GetDashboardStatsAsync();
    Task<DestekRaporStats> GetRaporStatsAsync(DateTime baslangic, DateTime bitis, int? departmanId = null);
    Task<List<DestekPerformansRapor>> GetPersonelPerformansRaporuAsync(DateTime baslangic, DateTime bitis, int? departmanId = null);
    
    // === Ayarlar ===
    Task<string?> GetAyarAsync(string anahtar);
    Task SetAyarAsync(string anahtar, string deger, string? aciklama = null, string? grup = null);
    Task<Dictionary<string, string>> GetAyarlarByGrupAsync(string grup);
}

/// <summary>
/// Destek talebi filtreleme parametreleri
/// </summary>
public class DestekTalebiFilterParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? AramaMetni { get; set; }
    public DestekDurum? Durum { get; set; }
    public DestekOncelik? Oncelik { get; set; }
    public DestekKaynak? Kaynak { get; set; }
    public int? DepartmanId { get; set; }
    public int? KategoriId { get; set; }
    public int? AtananKullaniciId { get; set; }
    public int? CariId { get; set; }
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public bool? SlaAsildi { get; set; }
    public bool? SadeceAcik { get; set; }
    public string? SortBy { get; set; } = "SonAktiviteTarihi";
    public bool SortDesc { get; set; } = true;
}

/// <summary>
/// Bilgi bankası filtreleme parametreleri
/// </summary>
public class BilgiBankasiFilterParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? AramaMetni { get; set; }
    public int? KategoriId { get; set; }
    public BilgiBankasiDurum? Durum { get; set; }
    public string? Etiket { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDesc { get; set; } = true;
}

/// <summary>
/// Dashboard istatistikleri
/// </summary>
public class DestekDashboardStats
{
    // Genel İstatistikler
    public int ToplamTalepSayisi { get; set; }
    public int AcikTalepSayisi { get; set; }
    public int BugunAcilanTalepSayisi { get; set; }
    public int BugunKapatilanTalepSayisi { get; set; }
    public int SlaAsildiSayisi { get; set; }
    
    // Öncelik Dağılımı
    public Dictionary<DestekOncelik, int> OncelikDagilimi { get; set; } = new();
    
    // Durum Dağılımı
    public Dictionary<DestekDurum, int> DurumDagilimi { get; set; } = new();
    
    // Departman Dağılımı
    public Dictionary<string, int> DepartmanDagilimi { get; set; } = new();
    
    // Kaynak Dağılımı
    public Dictionary<DestekKaynak, int> KaynakDagilimi { get; set; } = new();
    
    // Son Talepler
    public List<DestekTalebi> SonTalepler { get; set; } = new();
    
    // Performans Metrikleri
    public double OrtalamaIlkYanitSuresiSaat { get; set; }
    public double OrtalamaCozumSuresiSaat { get; set; }
    public double OrtalamaMemuniyetPuani { get; set; }
}

/// <summary>
/// Rapor istatistikleri
/// </summary>
public class DestekRaporStats
{
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    
    public int ToplamAcilanTalep { get; set; }
    public int ToplamKapatilanTalep { get; set; }
    public int ToplamSlaAsimi { get; set; }
    
    public double OrtalamaIlkYanitSuresiDakika { get; set; }
    public double OrtalamaCozumSuresiDakika { get; set; }
    public double OrtalamaMemuniyetPuani { get; set; }
    
    // Günlük trend
    public List<GunlukTalepTrend> GunlukTrend { get; set; } = new();
    
    // Kategori bazlı
    public List<KategoriTalepStats> KategoriBazli { get; set; } = new();
}

/// <summary>
/// Günlük talep trendi
/// </summary>
public class GunlukTalepTrend
{
    public DateTime Tarih { get; set; }
    public int AcilanTalep { get; set; }
    public int KapatilanTalep { get; set; }
}

/// <summary>
/// Kategori bazlı istatistikler
/// </summary>
public class KategoriTalepStats
{
    public int KategoriId { get; set; }
    public string KategoriAdi { get; set; } = string.Empty;
    public int TalepSayisi { get; set; }
    public double OrtalamaСozumSuresi { get; set; }
}

/// <summary>
/// Personel performans raporu
/// </summary>
public class DestekPerformansRapor
{
    public int KullaniciId { get; set; }
    public string KullaniciAdi { get; set; } = string.Empty;
    public int AtananTalepSayisi { get; set; }
    public int CozulenTalepSayisi { get; set; }
    public int SlaAsimSayisi { get; set; }
    public double OrtalamaYanitSuresiDakika { get; set; }
    public double OrtalamaCozumSuresiDakika { get; set; }
    public double OrtalamaMemuniyetPuani { get; set; }
    public int ToplamYanitSayisi { get; set; }
}




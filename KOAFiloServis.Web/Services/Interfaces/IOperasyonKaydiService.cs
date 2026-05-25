using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services.Interfaces;

/// <summary>
/// Günlük ham operasyon kayıtları için CRUD servisi.
/// </summary>
public interface IOperasyonKaydiService
{
    // ── Sorgulama ──────────────────────────────────────────────────────────

    /// <summary>Tarih aralığındaki operasyon kayıtlarını döner.</summary>
    Task<List<OperasyonKaydi>> GetByDateRangeAsync(DateTime baslangic, DateTime bitis, int? kurumId = null);

    /// <summary>Belirli bir araç + güzergah + dönem için kayıtları döner (aylık grid).</summary>
    Task<List<OperasyonKaydi>> GetByAracGuzergahAsync(int aracId, int guzergahId, int yil, int ay);

    /// <summary>Dönem + kurum için tüm operasyon kayıtlarını döner.</summary>
    Task<List<OperasyonKaydi>> GetByDonemAsync(int yil, int ay, int? kurumId = null);

    Task<OperasyonKaydi?> GetByIdAsync(int id);

    // ── CRUD ────────────────────────────────────────────────────────────────

    /// <summary>Tek kayıt upsert (Tarih + GuzergahId + AracId + Slot unique).</summary>
    Task<OperasyonKaydi> SaveAsync(OperasyonKaydi kayit);

    /// <summary>Toplu upsert: aynı Tarih+Guzergah+Arac+Slot kombinasyonunda günceller.</summary>
    Task TopluSaveAsync(IEnumerable<OperasyonKaydi> kayitlar);

    Task DeleteAsync(int id, string? deletedBy = null);

    // ── Şablon ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Kurum + dönem için güzergah × araç kombinasyonlarından her gün için boş şablon oluşturur.
    /// Mevcut kayıtlar varsa onları döner.
    /// </summary>
    Task<List<OperasyonKaydi>> SablonOlusturAsync(int kurumId, int yil, int ay);

    // ── Migrasyon ───────────────────────────────────────────────────────────

    /// <summary>
    /// Mevcut PuantajKayit'ları OperasyonKaydi'ya dönüştürür.
    /// Gun01-Gun31 değerlerini günlük kayıtlara açar.
    /// </summary>
    Task<int> ImportFromPuantajKayitAsync(int yil, int ay, int? kurumId = null);

    // ── Excel Import (Sprint 7) ────────────────────────────────────────────

    /// <summary>Excel dosyasından OperasyonKaydi import eder. dryRun=true ise kaydetmez.</summary>
    Task<List<OperasyonImportSonuc>> ImportFromExcelAsync(Stream excelStream, int? kurumId = null, bool dryRun = false);

    /// <summary>Excel şablonunu byte dizisi olarak üretir.</summary>
    byte[] ExcelSablonUret();
}

public sealed class OperasyonImportSatiri
{
    public int SatirNo { get; init; }
    public string Tarih { get; init; } = "";
    public string Plaka { get; init; } = "";
    public string GuzergahAdi { get; init; } = "";
    public string? SoforAdi { get; init; }
    public string Slot { get; init; } = "Sabah";
    public string SeferSayisi { get; init; } = "1";
    public string Durum { get; init; } = "Gitti";
    public string? KurumAdi { get; init; }
}

public sealed class OperasyonImportSonuc
{
    public int SatirNo { get; init; }
    public bool Basarili { get; init; }
    public bool Atlandi { get; init; }
    public string? HataMesaji { get; init; }
    public string Plaka { get; init; } = "";
    public string GuzergahAdi { get; init; } = "";
}

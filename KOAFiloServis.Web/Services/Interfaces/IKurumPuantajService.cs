using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Kurum bazlı güzergah → araç → günlük puantaj yönetimi.
/// </summary>
public interface IKurumPuantajService
{
    // ── Kurum / Güzergah / Araç listeleri ─────────────────────────────────────
    Task<List<Kurum>> GetAktifKurumlarAsync();

    Task<List<Guzergah>> GetKurumGuzergahlariAsync(int kurumId);

    /// <summary>
    /// Bir güzergaha atanmış aktif araçları döner.
    /// FiloGuzergahEslestirme tablosundaki + güzergahın VarsayilanArac'ı birleştirilir.
    /// </summary>
    Task<List<AracGuzergahSatiri>> GetGuzergahAraclariAsync(int guzergahId);

    // ── PuantajKayit CRUD ──────────────────────────────────────────────────────
    /// <summary>Belirtilen dönem + kurum için mevcut puantaj kayıtlarını döner.</summary>
    Task<List<PuantajKayit>> GetPuantajlarAsync(int yil, int ay, int? kurumId = null);

    /// <summary>Tek PuantajKayit satırını döner.</summary>
    Task<PuantajKayit?> GetPuantajByIdAsync(int id);

    /// <summary>Yeni veya güncel tek kayıt kaydeder (upsert: Guzergah+Arac+Yil+Ay unique).</summary>
    Task<PuantajKayit> SavePuantajAsync(PuantajKayit kayit);

    /// <summary>
    /// Toplu kaydet: verilen listedeki tüm satırları tek transaction'da upsert eder.
    /// </summary>
    Task TopluSavePuantajAsync(IEnumerable<PuantajKayit> kayitlar);

    Task DeletePuantajAsync(int id);

    // ── Çakışma Kontrolü ─────────────────────────────────────────────────────
    /// <summary>Tek kayıt için çakışma kontrolü yapar (kaydetme öncesi).</summary>
    Task<ConflictResult> CheckKayitConflictsAsync(PuantajKayit kayit);

    /// <summary>Toplu kayıt listesi için çakışma kontrolü yapar.</summary>
    Task<ConflictResult> CheckConflictsAsync(List<PuantajKayit> kayitlar);

    // ── Yardımcı ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Kurum + dönem için varsayılan satır şablonlarını oluşturur:
    /// her güzergah × her araç kombinasyonu için boş PuantajKayit nesnesi.
    /// Kayıtlarda zaten varsa mevcut kayıtları döner.
    /// </summary>
    Task<List<PuantajKayit>> SablonOlusturAsync(int kurumId, int yil, int ay);

    // ── Excel Import ──────────────────────────────────────────────────────────
    /// <summary>Excel'den okunan puantaj satırlarını toplu olarak kaydeder.</summary>
    Task<List<PuantajImportSonuc>> TopluImportAsync(List<PuantajImportSatiri> satirlar);
}

/// <summary>
/// Excel import için satır DTO'su.
/// </summary>
public sealed class PuantajImportSatiri
{
    public string Plaka { get; init; } = "";
    public string GuzergahAdi { get; init; } = "";
    public string KurumAdi { get; init; } = "";
    public string? SoforAdi { get; init; }
    public SeferSlot Slot { get; init; } = SeferSlot.Sabah;
    public string? SlotAdi { get; init; }
    public PuantajYon Yon { get; init; } = PuantajYon.SabahAksam;
    public decimal Gun { get; init; }
    public int SeferSayisi { get; init; } = 1;
    public decimal BirimGelir { get; init; }
    public decimal BirimGider { get; init; }
    public string? FaturaKesiciAdi { get; init; }
    public string? Notlar { get; init; }
    public int Yil { get; init; }
    public int Ay { get; init; }
    public int? KurumId { get; init; }
}

/// <summary>
/// Excel import sonuç satırı.
/// </summary>
public sealed class PuantajImportSonuc
{
    public bool Basarili { get; init; }
    public bool Atlandi { get; init; }
    public string? HataMesaji { get; init; }
}

/// <summary>
/// Güzergah satırında gösterilecek araç + eşleştirme bilgisi.
/// </summary>
public sealed class AracGuzergahSatiri
{
    public int AracId { get; init; }
    public string Plaka { get; init; } = string.Empty;
    public string? SoforAdi { get; init; }
    public int? SoforId { get; init; }
    public decimal KurumaKesilecekUcret { get; init; }
    public decimal TaseronaOdenenUcret { get; init; }
    /// <summary>FiloGuzergahEslestirme.Id; varsayılan araç için null.</summary>
    public int? EslestirmeId { get; init; }
}

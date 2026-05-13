using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services.Interfaces;

public interface IPuantajEslestirmeService
{
    // ============== Firma + Araç + Şoför Eşleştirme ==============
    Task<List<FirmaAracSoforEslestirme>> GetAracSoforEslestirmeleriAsync(int firmaId, bool sadeceAktif = false);
    Task<FirmaAracSoforEslestirme?> GetAracSoforEslestirmeByIdAsync(int id);
    Task<FirmaAracSoforEslestirme> CreateAracSoforEslestirmeAsync(FirmaAracSoforEslestirme model);
    Task<FirmaAracSoforEslestirme> UpdateAracSoforEslestirmeAsync(FirmaAracSoforEslestirme model);
    Task DeleteAracSoforEslestirmeAsync(int id);

    /// <summary>
    /// Henüz hiçbir kuruma atanmamış araçlar (sol panel)
    /// </summary>
    Task<List<Arac>> GetEslesmeyenAraclarAsync(int firmaId);

    /// <summary>
    /// Henüz hiçbir araca/şoföre atanmamış kurumlar (sağ panel)
    /// </summary>
    Task<List<Cari>> GetEslesmeyenKurumlarAsync(int firmaId);

    /// <summary>
    /// Toplu eşleştirme: tek araç-şoför + birden fazla kurum (1-N)
    /// veya tek kurum + birden fazla araç-şoför (N-1) ya da N-N kombinasyonu
    /// </summary>
    Task<int> TopluAracSoforEslestirAsync(
        int firmaId,
        List<(int AracId, int SoforId)> aracSoforListesi,
        List<int> kurumCariIdleri,
        decimal varsayilanBirimUcret);

    // ============== Firma + Güzergah Eşleştirme ==============
    Task<List<FirmaGuzergahEslestirme>> GetGuzergahEslestirmeleriAsync(int firmaId, bool sadeceAktif = false);
    Task<FirmaGuzergahEslestirme?> GetGuzergahEslestirmeByIdAsync(int id);
    Task<FirmaGuzergahEslestirme> CreateGuzergahEslestirmeAsync(FirmaGuzergahEslestirme model);
    Task<FirmaGuzergahEslestirme> UpdateGuzergahEslestirmeAsync(FirmaGuzergahEslestirme model);
    Task DeleteGuzergahEslestirmeAsync(int id);

    Task<List<Guzergah>> GetEslesmeyenGuzergahlarAsync(int firmaId);
    Task<List<Cari>> GetGuzergahsiZKurumlarAsync(int firmaId);

    Task<int> TopluGuzergahEslestirAsync(
        int firmaId,
        List<int> guzergahIdleri,
        List<int> kurumCariIdleri,
        decimal seferUcreti,
        int kdvOrani = 20);

    // ============== Mutabakat / Fark Tablosu ==============
    /// <summary>
    /// Cari (kurum/müşteri) bazlı kesilen vs. tahakkuk eden fatura mutabakatı
    /// </summary>
    Task<List<CariFaturaMutabakatRow>> GetCariMutabakatAsync(int firmaId, DateTime baslangic, DateTime bitis);

    /// <summary>
    /// Taşıma Tedarikçisi bazlı gelen vs. tahakkuk eden fatura mutabakatı
    /// </summary>
    Task<List<TasimaTedarikciMutabakatRow>> GetTasimaTedarikciMutabakatAsync(int firmaId, DateTime baslangic, DateTime bitis);
}

/// <summary>
/// Cari (müşteri) bazlı mutabakat satırı.
/// Tahakkuk = puantajdan üretilen "kesilmesi gereken" tutar.
/// Fatura = gerçekten kesilmiş fatura tutarları.
/// </summary>
public class CariFaturaMutabakatRow
{
    public int CariId { get; set; }
    public string CariUnvan { get; set; } = string.Empty;
    public decimal TahakkukEdenTutar { get; set; }   // Puantajdan beklenen
    public decimal KesilenFaturaTutari { get; set; } // Gerçek faturalar
    public decimal Fark => KesilenFaturaTutari - TahakkukEdenTutar;
    public int PuantajSayisi { get; set; }
    public int FaturaSayisi { get; set; }
    public string Durum =>
        Fark == 0 ? "Mutabık"
        : Fark > 0 ? "Fazla Faturalanmış"
        : "Eksik Faturalanmış";
}

/// <summary>
/// Taşıma Tedarikçisi bazlı mutabakat satırı (tedarikçiye ödenmesi/gelen fatura)
/// </summary>
public class TasimaTedarikciMutabakatRow
{
    public int TedarikciId { get; set; }
    public string TedarikciUnvan { get; set; } = string.Empty;
    public decimal TahakkukEdenTutar { get; set; }   // Puantajdan beklenen ödeme
    public decimal GelenFaturaTutari { get; set; }   // Tedarikçinin kestiği gerçek faturalar
    public decimal Fark => GelenFaturaTutari - TahakkukEdenTutar;
    public int PuantajSayisi { get; set; }
    public int FaturaSayisi { get; set; }
    public string Durum =>
        Fark == 0 ? "Mutabık"
        : Fark > 0 ? "Fazla Fatura Gelmiş"
        : "Eksik Fatura Gelmiş";
}

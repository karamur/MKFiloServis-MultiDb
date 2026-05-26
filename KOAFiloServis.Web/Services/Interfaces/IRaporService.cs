using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Models;

namespace KOAFiloServis.Web.Services;

public interface IRaporService
{
    Task<List<ServisCalismaRaporItem>> GetServisCalismaRaporuAsync(
        DateTime startDate,
        DateTime endDate,
        int? aracId = null,
        int? soforId = null,
        int? guzergahId = null,
        int? cariId = null,
        AracSahiplikTipi? sahiplikTipi = null);

    Task<List<FaturaOdemeRaporItem>> GetFaturaOdemeRaporuAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? cariId = null,
        bool? sadeceBekleyenler = null);

    Task<List<AracMasrafRaporItem>> GetAracMasrafRaporuAsync(
        DateTime startDate,
        DateTime endDate,
        int? aracId = null,
        AracSahiplikTipi? sahiplikTipi = null);

    Task<CariEkstre> GetCariEkstreAsync(
        int cariId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    // Şoför Performans Raporu
    Task<SoforPerformansOzet> GetSoforPerformansAsync(
        int soforId,
        DateTime startDate,
        DateTime endDate,
        AracSahiplikTipi? sahiplikTipi = null);

    Task<List<SoforKarsilastirmaOzeti>> GetSoforKarsilastirmaAsync(
        DateTime startDate,
        DateTime endDate,
        AracSahiplikTipi? sahiplikTipi = null);

    // Araç Karlılık Raporu
    Task<AracKarlilikOzet> GetAracKarlilikAsync(
        int aracId,
        DateTime startDate,
        DateTime endDate);

    Task<List<AracKarsilastirmaOzeti>> GetAracKarsilastirmaAsync(
        DateTime startDate,
        DateTime endDate,
        AracSahiplikTipi? sahiplikTipi = null);

    // Cari Bakiye Yaşlandırma Raporu
    Task<CariYaslandirmaRapor> GetCariYaslandirmaAsync(
        DateTime? raporTarihi = null,
        int? cariId = null,
        KOAFiloServis.Shared.Entities.CariTipi? cariTipi = null,
        bool sadeceBorcluCariler = false);

    Task<CariYaslandirmaOzet> GetCariYaslandirmaDetayAsync(
        int cariId,
        DateTime? raporTarihi = null);

    Task<IseGirisCikisBildirgeRaporu> GetIseGirisCikisBildirgeAsync(
        DateTime baslangicTarihi,
        DateTime bitisTarihi,
        IseGirisCikisFiltreTipi filtreTipi = IseGirisCikisFiltreTipi.Tumu,
        KOAFiloServis.Shared.Entities.PersonelGorev? gorev = null);

    Task<byte[]> ExportIseGirisCikisBildirgeExcelAsync(
        DateTime baslangicTarihi,
        DateTime bitisTarihi,
        IseGirisCikisFiltreTipi filtreTipi = IseGirisCikisFiltreTipi.Tumu,
        KOAFiloServis.Shared.Entities.PersonelGorev? gorev = null);

    // Operasyon Kar Raporu
    Task<List<OperasyonKarRaporuSatir>> GetOperasyonKarRaporuAsync(
        DateTime baslangic,
        DateTime bitis,
        AracSahiplikTipi? sahiplikTipi = null,
        int? aracId = null,
        int? guzergahId = null);
}

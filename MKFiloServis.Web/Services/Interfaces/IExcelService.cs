using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IExcelService
{
    byte[] ExportToExcel<T>(List<T> data, string sheetName = "Rapor");
    byte[] ExportServisCalismaRaporu(List<Models.ServisCalismaRaporItem> data);
    byte[] ExportFaturaOdemeRaporu(List<Models.FaturaOdemeRaporItem> data);
    byte[] ExportAracMasrafRaporu(List<Models.AracMasrafRaporItem> data);
    byte[] ExportSoforPerformansRaporu(SoforPerformansOzet data);
    byte[] ExportSoforKarsilastirmaRaporu(List<SoforKarsilastirmaOzeti> data);
    byte[] ExportAracKarlilikRaporu(AracKarlilikOzet data);
    byte[] ExportAracKarlilikKarsilastirmaRaporu(List<AracKarsilastirmaOzeti> data);
    
    // Yeni Export Metodları
    byte[] ExportCariler(List<Cari> data);
    byte[] ExportAraclar(List<Arac> data);
    byte[] ExportPersonel(List<Sofor> data);
    byte[] ExportBelgeUyarilari(List<BelgeUyari> data);
    byte[] ExportAracPerformans(List<AracPerformansData> data, int yil, int ay);
    byte[] ExportCariPerformans(List<CariPerformansData> data, int yil, int ay);

    // Genel Excel Oluşturma
    byte[] CreateExcel(string[] headers, List<object[]> data, string sheetName = "Rapor");
}




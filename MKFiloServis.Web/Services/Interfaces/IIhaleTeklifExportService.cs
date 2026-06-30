namespace MKFiloServis.Web.Services.Interfaces;

public interface IIhaleTeklifExportService
{
    Task<byte[]> ExportPdfAsync(int versiyonId);
    Task<byte[]> ExportExcelAsync(int versiyonId);
}




using System.Text.Json;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IDataExportService
{
    byte[] ExportToCsv<T>(IEnumerable<T> data);
    byte[] ExportToJson<T>(T data, JsonSerializerOptions? options = null);
    Task<byte[]> ExportToParquetAsync<T>(IEnumerable<T> data, CancellationToken cancellationToken = default);
}





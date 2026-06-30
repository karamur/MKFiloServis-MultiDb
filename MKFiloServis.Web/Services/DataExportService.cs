using System.Reflection;
using System.Text;
using System.Text.Json;
using Parquet.Serialization;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class DataExportService : IDataExportService
{
    public byte[] ExportToCsv<T>(IEnumerable<T> data)
    {
        var items = data?.ToList() ?? new List<T>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", properties.Select(p => Escape(p.Name))));

        foreach (var item in items)
        {
            builder.AppendLine(string.Join(",", properties.Select(p => Escape(FormatValue(p.GetValue(item))))));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public byte[] ExportToJson<T>(T data, JsonSerializerOptions? options = null)
    {
        var jsonOptions = options ?? new JsonSerializerOptions
        {
            WriteIndented = true
        };

        return JsonSerializer.SerializeToUtf8Bytes(data, jsonOptions);
    }

    public async Task<byte[]> ExportToParquetAsync<T>(IEnumerable<T> data, CancellationToken cancellationToken = default)
    {
        var items = data?.ToList() ?? new List<T>();

        await using var stream = new MemoryStream();
        await ParquetSerializer.SerializeAsync(items, stream, cancellationToken: cancellationToken);
        return stream.ToArray();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss zzz"),
            bool booleanValue => booleanValue ? "Evet" : "Hayır",
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string Escape(string value)
    {
        if (value.Contains('"'))
        {
            value = value.Replace("\"", "\"\"");
        }

        if (value.Contains(',') || value.Contains('\n') || value.Contains('\r') || value.Contains('"'))
        {
            return $"\"{value}\"";
        }

        return value;
    }
}



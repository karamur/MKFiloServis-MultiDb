using System.Text;
using System.Text.RegularExpressions;

namespace KOAFiloServis.Web.Helpers;

/// <summary>
/// Dosya ve klasör adları için temizleme yardımcısı.
/// Türkçe karakterleri sadeleştirir, invalid karakterleri temizler,
/// boşlukları "-" yapar, çoklu "-" karakterlerini tekilleştirir.
/// </summary>
public static class FileNameHelper
{
    public static string NormalizeFileName(string? value, string fallback = "EVRAK")
    {
        if (string.IsNullOrWhiteSpace(value))
            value = fallback;

        value = value.Trim();

        var map = new Dictionary<char, char>
        {
            ['ç'] = 'c', ['Ç'] = 'C',
            ['ğ'] = 'g', ['Ğ'] = 'G',
            ['ı'] = 'i', ['I'] = 'I',
            ['İ'] = 'I',
            ['ö'] = 'o', ['Ö'] = 'O',
            ['ş'] = 's', ['Ş'] = 'S',
            ['ü'] = 'u', ['Ü'] = 'U'
        };

        var sb = new StringBuilder();

        foreach (var ch in value)
        {
            if (map.TryGetValue(ch, out var mapped))
            {
                sb.Append(mapped);
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_' || ch == '/' || ch == '\\')
            {
                sb.Append('-');
                continue;
            }

            if (Path.GetInvalidFileNameChars().Contains(ch))
            {
                sb.Append('-');
                continue;
            }

            sb.Append('-');
        }

        var normalized = sb.ToString().ToUpperInvariant();
        normalized = Regex.Replace(normalized, "-+", "-");
        normalized = normalized.Trim('-');

        return string.IsNullOrWhiteSpace(normalized)
            ? fallback.ToUpperInvariant()
            : normalized;
    }

    public static string NormalizeExtension(string? originalFileName)
    {
        var ext = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(ext))
            return ".pdf";

        ext = ext.Trim().ToLowerInvariant();

        foreach (var invalid in Path.GetInvalidFileNameChars())
            ext = ext.Replace(invalid.ToString(), "");

        return ext.StartsWith('.') ? ext : "." + ext;
    }
}

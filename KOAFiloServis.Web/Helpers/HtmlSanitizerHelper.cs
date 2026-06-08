using System.Text.RegularExpressions;

namespace KOAFiloServis.Web.Helpers;

/// <summary>
/// Hafif HTML sanitizer — kullanıcı girdisindeki tehlikeli HTML/script etiketlerini temizler.
/// Harici NuGet bağımlılığı yok. MarkupString render öncesi çağrılmalıdır.
/// </summary>
public static class HtmlSanitizerHelper
{
    // Tehlikeli etiketler — tamamen kaldırılır (içeriğiyle birlikte)
    private static readonly Regex DangerousTagRegex = new(
        @"<\s*(script|iframe|object|embed|form|input|button|link|meta|style|base|applet|frame|frameset|ilayer|layer|bgsound|title|head|html|body)\b[^>]*>.*?<\s*/\s*\1\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Kendiliğinden kapanan tehlikeli etiketler
    private static readonly Regex DangerousSelfClosingRegex = new(
        @"<\s*(script|iframe|object|embed|link|meta|base|input)\b[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Event handler attribute'ları (onclick, onerror, onload vb.)
    private static readonly Regex EventHandlerRegex = new(
        @"\s+on\w+\s*=\s*""[^""]*""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EventHandlerSingleQuoteRegex = new(
        @"\s+on\w+\s*=\s*'[^']*'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // javascript: URL'leri
    private static readonly Regex JavascriptUrlRegex = new(
        @"\b(javascript|vbscript|data)\s*:",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Kullanıcı girdisini güvenli hale getirir.
    /// - &lt;script&gt;, &lt;iframe&gt; vb. tamamen kaldırılır
    /// - on* event handler'ları kaldırılır
    /// - javascript: URL'leri kaldırılır
    /// - &lt;br&gt;, &lt;b&gt;, &lt;i&gt;, &lt;u&gt;, &lt;p&gt;, &lt;ul&gt;, &lt;li&gt; gibi güvenli etiketler korunur
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var result = input;

        // 1) Tehlikeli etiketleri içeriğiyle birlikte kaldır
        result = DangerousTagRegex.Replace(result, string.Empty);

        // 2) Kendiliğinden kapanan tehlikeli etiketleri kaldır
        result = DangerousSelfClosingRegex.Replace(result, string.Empty);

        // 3) Event handler attribute'larını kaldır
        result = EventHandlerRegex.Replace(result, string.Empty);
        result = EventHandlerSingleQuoteRegex.Replace(result, string.Empty);

        // 4) javascript: URL'lerini kaldır
        result = JavascriptUrlRegex.Replace(result, "yasak:");

        return result;
    }
}

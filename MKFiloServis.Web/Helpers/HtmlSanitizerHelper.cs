using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MKFiloServis.Web.Helpers;

/// <summary>
/// Kullanıcı girdisini <c>MarkupString</c> olarak basmadan önce güvenli hale getirir.
/// <para>
/// İzin listesi yaklaşımı: yalnız biçim etiketleri (p, br, b, strong, i, em, u, ul, ol, li, başlıklar, alıntı, kod)
/// özniteliksiz yeniden yazılır; diğer bütün etiketler atılır, script/style/iframe gibi kapsayıcıların içeriği de
/// atılır, metin her zaman HTML kodlanır. Öznitelik hiç yazılmadığı için <c>onerror=</c>, <c>href="javascript:"</c>,
/// <c>&lt;svg/onload&gt;</c> gibi kalıplar çalışamaz.
/// </para>
/// </summary>
public static class HtmlSanitizerHelper
{
    private static readonly Regex YorumRegex = new(@"<!--.*?(-->|$)", RegexOptions.Singleline | RegexOptions.Compiled);

    // < ile başlayıp > ile biten etiket: </b>, <p class="x">, <svg/onload=...>, <br/>
    private static readonly Regex EtiketRegex = new(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)\b[^<>]*>", RegexOptions.Compiled);

    private static readonly HashSet<string> IzinliEtiketler = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "b", "strong", "i", "em", "u", "ul", "ol", "li", "h3", "h4", "h5", "h6", "blockquote", "code", "pre", "hr"
    };

    private static readonly HashSet<string> BosEtiketler = new(StringComparer.OrdinalIgnoreCase) { "br", "hr" };

    // İçeriğiyle birlikte atılan kapsayıcılar
    private static readonly HashSet<string> IcerigiAtilanEtiketler = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "object", "embed", "template", "noscript", "textarea", "title", "xmp", "svg", "math", "head"
    };

    /// <summary>
    /// Girdiyi izin listesine göre temizler.
    /// </summary>
    /// <param name="input">Düz metin ya da basit HTML.</param>
    /// <param name="satirSonlariniKoru">Metindeki satır sonlarını <c>&lt;br&gt;</c> yapar (textarea'dan gelen içerik için).</param>
    public static string Sanitize(string? input, bool satirSonlariniKoru = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var kaynak = YorumRegex.Replace(input.Replace("\r\n", "\n"), string.Empty);
        var sonuc = new StringBuilder(kaynak.Length + 16);
        var konum = 0;
        string? atlananKapsayici = null;

        foreach (Match etiket in EtiketRegex.Matches(kaynak))
        {
            var kapanis = etiket.Groups[1].Value == "/";
            var ad = etiket.Groups[2].Value;

            if (atlananKapsayici != null)
            {
                // Tehlikeli kapsayıcının içindeyiz: yalnız kendi kapanışını ara, aradaki her şeyi at.
                if (kapanis && string.Equals(ad, atlananKapsayici, StringComparison.OrdinalIgnoreCase))
                {
                    atlananKapsayici = null;
                    konum = etiket.Index + etiket.Length;
                }
                continue;
            }

            MetinEkle(sonuc, kaynak.AsSpan(konum, etiket.Index - konum), satirSonlariniKoru);
            konum = etiket.Index + etiket.Length;

            if (!kapanis && IcerigiAtilanEtiketler.Contains(ad))
            {
                if (!etiket.Value.EndsWith("/>", StringComparison.Ordinal))
                    atlananKapsayici = ad;
                continue;
            }

            if (!IzinliEtiketler.Contains(ad))
                continue;

            var kucukAd = ad.ToLowerInvariant();
            if (BosEtiketler.Contains(kucukAd))
            {
                if (!kapanis) sonuc.Append('<').Append(kucukAd).Append('>');
                continue;
            }

            sonuc.Append(kapanis ? "</" : "<").Append(kucukAd).Append('>');
        }

        if (atlananKapsayici == null && konum < kaynak.Length)
            MetinEkle(sonuc, kaynak.AsSpan(konum), satirSonlariniKoru);

        return sonuc.ToString();
    }

    /// <summary>
    /// Metni olduğu gibi (HTML yorumlamadan) gösterilecek biçime getirir: her karakter kodlanır, satır sonları
    /// <c>&lt;br&gt;</c> olur. Yığın izi gibi <c>&lt;Metot&gt;</c> içeren teknik metinler için.
    /// </summary>
    public static string MetinOlarak(string? metin)
    {
        if (string.IsNullOrEmpty(metin))
            return string.Empty;

        return WebUtility.HtmlEncode(metin.Replace("\r\n", "\n")).Replace("\n", "<br>");
    }

    /// <summary>
    /// HTML'i düz metne çevirir (hazır yanıt şablonunu metin kutusuna koymak için):
    /// paragraf/satır/madde sonları satır sonu olur, etiketler atılır, karakter varlıkları çözülür.
    /// </summary>
    public static string DuzMetin(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var metin = YorumRegex.Replace(html.Replace("\r\n", "\n"), string.Empty);
        metin = Regex.Replace(metin, @"<\s*br\s*/?\s*>", "\n", RegexOptions.IgnoreCase);
        metin = Regex.Replace(metin, @"<\s*li\b[^>]*>", "\n• ", RegexOptions.IgnoreCase);
        metin = Regex.Replace(metin, @"</\s*(p|div|h[1-6]|ul|ol|blockquote|pre)\s*>", "\n", RegexOptions.IgnoreCase);
        metin = Regex.Replace(metin, @"<(script|style)\b.*?</\1\s*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        metin = EtiketRegex.Replace(metin, string.Empty);
        metin = WebUtility.HtmlDecode(metin);
        metin = Regex.Replace(metin, @"[ \t]+\n", "\n");
        metin = Regex.Replace(metin, @"\n{3,}", "\n\n");
        return metin.Trim();
    }

    private static void MetinEkle(StringBuilder hedef, ReadOnlySpan<char> parca, bool satirSonlariniKoru)
    {
        if (parca.IsEmpty) return;

        // Önce varlıkları çöz, sonra yeniden kodla: "&#60;script&#62;" metin olarak görünür, çift kodlama olmaz.
        var kodlu = WebUtility.HtmlEncode(WebUtility.HtmlDecode(parca.ToString()));
        hedef.Append(satirSonlariniKoru ? kodlu.Replace("\n", "<br>") : kodlu);
    }
}

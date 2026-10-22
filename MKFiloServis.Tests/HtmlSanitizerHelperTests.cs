using System.Net;
using MKFiloServis.Web.Helpers;

namespace MKFiloServis.Tests;

public sealed class HtmlSanitizerHelperTests
{
    [Theory]
    [InlineData("<img src=x onerror=alert(document.cookie)>")]
    [InlineData("<svg/onload=alert(1)>merhaba")]
    [InlineData("<a href=\"java&#115;cript:alert(1)\">tıkla</a>")]
    [InlineData("<iframe src=\"https://kotu.example\"></iframe>")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<p style=\"background:url(javascript:alert(1))\" onclick='x()'>metin</p>")]
    public void Calistirilabilir_icerik_kalmaz(string girdi)
    {
        var cikti = HtmlSanitizerHelper.Sanitize(girdi);

        Assert.DoesNotContain("<img", cikti, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<svg", cikti, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<a ", cikti, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", cikti, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", cikti, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror=", cikti, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload=", cikti, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", cikti, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", cikti, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href", cikti, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Karakter_varliklariyla_yazilan_etiket_metin_olarak_kalir()
    {
        var cikti = HtmlSanitizerHelper.Sanitize("&lt;img src=x onerror=alert(1)&gt;");

        Assert.StartsWith("&lt;img", cikti);
        Assert.DoesNotContain("<img", cikti, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bicim_etiketleri_ozniteliksiz_korunur()
    {
        var cikti = HtmlSanitizerHelper.Sanitize("<p class=\"x\" onclick=\"y()\">Merhaba <b>dünya</b></p><ul><li>bir</li></ul>");

        Assert.Equal("<p>Merhaba <b>dünya</b></p><ul><li>bir</li></ul>", WebUtility.HtmlDecode(cikti));
        Assert.StartsWith("<p>Merhaba <b>", cikti);
    }

    [Fact]
    public void Script_icerigi_atilir_metin_korunur()
    {
        var cikti = HtmlSanitizerHelper.Sanitize("önce<script>alert('x')</script>sonra");

        Assert.Equal("öncesonra", WebUtility.HtmlDecode(cikti));
    }

    [Fact]
    public void Satir_sonlari_istenince_br_olur()
    {
        Assert.Equal("a<br>b", HtmlSanitizerHelper.Sanitize("a\nb", satirSonlariniKoru: true));
        Assert.Equal("a\nb", HtmlSanitizerHelper.Sanitize("a\nb"));
        Assert.Equal("a<br>b", HtmlSanitizerHelper.Sanitize("a<br/>b"));
    }

    [Fact]
    public void Metin_olarak_her_seyi_kodlar()
    {
        var cikti = HtmlSanitizerHelper.MetinOlarak("at Sayfa.<OnInitializedAsync>d__5.MoveNext()\nikinci");

        Assert.Contains("&lt;OnInitializedAsync&gt;", cikti);
        Assert.Contains("<br>", cikti);
        Assert.DoesNotContain("<OnInitializedAsync>", cikti);
    }

    [Fact]
    public void Hazir_yanit_html_duz_metne_cevrilir()
    {
        var metin = HtmlSanitizerHelper.DuzMetin("<p>Merhaba,</p><p>Şunları paylaşın:</p><ul><li>Ekran görüntüsü</li><li>Saat</li></ul><p>Teşekkürler&amp;iyi günler</p>");

        Assert.DoesNotContain("<", metin);
        Assert.Contains("Merhaba,", metin);
        Assert.Contains("• Ekran görüntüsü", metin);
        Assert.Contains("Teşekkürler&iyi günler", metin);
    }
}

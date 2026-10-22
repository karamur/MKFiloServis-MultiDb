using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MKFiloServis.Web.Components.Layout;

namespace MKFiloServis.Tests;

/// <summary>
/// Ana menü bileşeninin çıktısı: gruplar, alt gruplar, tek vurgulu bağlantı ve açık sayfanın grubunun otomatik açılması.
/// Bileşen statik HTML'e çizilir; tıklamalar, bileşendeki işleyicilerin yansımayla çağrılmasıyla taklit edilir.
/// </summary>
public sealed class AnaMenuAgaciTests
{
    private static MenuGorunurluk Baglam(IEnumerable<string> yetkiler, bool yonetici = false)
    {
        var set = yetkiler.ToHashSet(StringComparer.Ordinal);
        var onekler = set.Select(y => { var i = y.IndexOf('.'); return i > 0 ? y[..i] : y; }).ToHashSet(StringComparer.Ordinal);
        return new MenuGorunurluk(_ => true, yonetici, true, set, onekler, yonetici ? "Admin" : "Kullanici");
    }

    private static MenuGorunurluk Yonetici() => Baglam([], yonetici: true);

    private sealed class SahteNavigation : NavigationManager
    {
        public SahteNavigation(string goreliAdres) => Initialize("http://localhost/", "http://localhost/" + goreliAdres.TrimStart('/'));

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }

    private sealed class BilesenYakalayici : IComponentActivator
    {
        public AnaMenuAgaci? Agac;

        public IComponent CreateInstance(Type componentType)
        {
            var c = (IComponent)Activator.CreateInstance(componentType)!;
            if (c is AnaMenuAgaci a)
                Agac = a;
            return c;
        }
    }

    /// <summary>Menüyü verilen adreste çizer; <paramref name="sonra"/> ilk çizimden sonra bileşende işlem yapıp yeniden çizdirir.</summary>
    private static async Task<string> CizAsync(string goreliAdres, MenuGorunurluk baglam, Action<AnaMenuAgaci>? sonra = null)
    {
        var yakalayici = new BilesenYakalayici();
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<NavigationManager>(new SahteNavigation(goreliAdres))
            .AddSingleton<IComponentActivator>(yakalayici)
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var kok = await renderer.RenderComponentAsync<AnaMenuAgaci>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Kokler"] = AnaMenuYapisi.Kokler,
                ["Gorunurluk"] = baglam
            }));

            if (sonra != null)
            {
                sonra(yakalayici.Agac!);
                typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(yakalayici.Agac, null);
            }

            // Dinamik metinler HTML kodlanır (ü → &#xFC;) ve kapsamlı CSS her öğeye b-xxxx özniteliği ekler;
            // karşılaştırmayı okunur tutmak için ikisi de temizlenir.
            var cikti = WebUtility.HtmlDecode(kok.ToHtmlString());
            return Regex.Replace(cikti, @" b-[a-z0-9]+(?=[\s>/])", string.Empty);
        });
    }

    private static int Say(string html, string desen) => Regex.Matches(html, desen).Count;

    private static bool BaglantiVar(string html, string href) => html.Contains($@"href=""{href}""", StringComparison.Ordinal);

    private static void Cagir(AnaMenuAgaci agac, string metot, params object[] argumanlar)
        => typeof(AnaMenuAgaci).GetMethod(metot, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(agac, argumanlar);

    private static HashSet<string> Kume(AnaMenuAgaci agac, string alan)
        => (HashSet<string>)typeof(AnaMenuAgaci).GetField(alan, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(agac)!;

    [Fact]
    public async Task Acik_sayfanin_grubu_ve_alt_grubu_acilir_ve_yalniz_o_baglanti_vurgulanir()
    {
        var html = await CizAsync("efatura/giden", Yonetici());

        Assert.Equal(1, Say(html, @"class=""nav-link active"""));
        Assert.Contains(@"class=""nav-link active"" href=""/efatura/giden""", html);
        Assert.Equal(1, Say(html, "nav-section-aktif"));

        // Açık alt grup (Listeler) kardeşleriyle görünür; kapalı alt grup (Hazırlık) ve başka gruplar çizilmez.
        Assert.True(BaglantiVar(html, "/efatura/gelen"));
        Assert.True(BaglantiVar(html, "/faturalar"));
        Assert.False(BaglantiVar(html, "/proforma-faturalar"));
        Assert.False(BaglantiVar(html, "/cariler"));
        Assert.Contains("Hazırlık", html);          // kapalı alt grubun başlığı yine de görünür
    }

    [Fact]
    public async Task Ust_baglanti_alt_sayfada_degil_en_uzun_eslesen_baglanti_vurgulanir()
    {
        var html = await CizAsync("butce/odemeler", Yonetici());

        Assert.Equal(1, Say(html, @"class=""nav-link active"""));
        Assert.Contains(@"class=""nav-link active"" href=""/butce/odemeler""", html);
        Assert.Matches(@"class=""nav-link\s*"" href=""/butce""", html);          // "Bütçe Analiz" vurgulanmaz

        // Etkin alt grup (Bütçe) ve ana alt grup (Banka / Kasa) açık; Muhasebe kapalı.
        Assert.True(BaglantiVar(html, "/banka-hesaplari"));
        Assert.False(BaglantiVar(html, "/muhasebe/hesap-plani"));
    }

    [Fact]
    public async Task Ana_sayfada_yalniz_Ana_Sayfa_vurgulanir_ve_hicbir_grup_acik_degildir()
    {
        var html = await CizAsync("dashboard", Yonetici());

        Assert.Equal(1, Say(html, @"class=""nav-link active"""));
        Assert.Contains(@"class=""nav-link active"" href=""/dashboard""", html);
        Assert.Equal(0, Say(html, @"aria-expanded=""true"""));
        Assert.Equal(12, Say(html, @"class=""nav-section-header"));   // 12 grup başlığı, hiçbiri etkin değil
        Assert.False(BaglantiVar(html, "/efatura/giden"));
    }

    [Fact]
    public async Task Menude_karsiligi_olmayan_sayfada_hicbir_baglanti_vurgulanmaz()
    {
        var html = await CizAsync("bilinmeyen/sayfa", Yonetici());

        Assert.Equal(0, Say(html, @"class=""nav-link active"""));
        Assert.Equal(0, Say(html, "nav-section-aktif"));
    }

    [Fact]
    public async Task Eski_adreste_bile_menu_dogru_baglantiyi_vurgular()
    {
        var html = await CizAsync("muhasebe/arac-karlilik", Yonetici());

        Assert.Contains(@"class=""nav-link active"" href=""/raporlar/arac-karlilik""", html);
        Assert.Equal(1, Say(html, @"class=""nav-link active"""));
    }

    [Fact]
    public async Task Grup_ve_alt_grup_basliklari_tiklaninca_acilir_kapanir()
    {
        var html = await CizAsync("dashboard", Yonetici(), agac =>
        {
            Cagir(agac, "GrupDegistir", "cari");
            Cagir(agac, "GrupDegistir", "fatura");
            Cagir(agac, "AltGrupDegistir", "fatura/Hazırlık");
        });

        Assert.True(BaglantiVar(html, "/cariler"));                 // açılan grup
        Assert.True(BaglantiVar(html, "/efatura/giden"));           // fatura: ana alt grup hazır açık
        Assert.True(BaglantiVar(html, "/proforma-faturalar"));      // elle açılan alt grup

        var kapali = await CizAsync("efatura/giden", Yonetici(), agac => Cagir(agac, "GrupDegistir", "fatura"));
        Assert.False(BaglantiVar(kapali, "/efatura/giden"));        // etkin grup kullanıcı tarafından kapatıldı
    }

    [Fact]
    public async Task Gorunur_baglantisi_olmayan_grup_hic_cizilmez()
    {
        var html = await CizAsync("dashboard", Baglam(["kesilenfatura.oku"]));

        Assert.Contains("FATURALAR", html);
        Assert.DoesNotContain("CARİ YÖNETİMİ", html);
        Assert.DoesNotContain("FİNANS", html);
        Assert.DoesNotContain("AYARLAR", html);
    }

    [Fact]
    public async Task Her_grup_acildiginda_menudeki_her_baglanti_bir_kez_cizilir()
    {
        var html = await CizAsync("dashboard", Yonetici(), agac =>
        {
            foreach (var grup in AnaMenuYapisi.Kokler.OfType<MenuGrubu>())
            {
                Kume(agac, "acikGruplar").Add(grup.Anahtar);
                foreach (var alt in grup.AltGruplar.Where(a => a.Baslik is not null))
                    Kume(agac, "acikAltGruplar").Add(MenuFiltresi.AltAnahtar(grup, alt));
            }
        });

        var hrefler = Regex.Matches(html, @"<a [^>]*href=""([^""]+)""").Select(m => m.Groups[1].Value).ToList();

        Assert.Equal(115, hrefler.Count);
        Assert.Equal(hrefler.Count, hrefler.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Sabit_html_kimlikleri_korunur()
    {
        var html = await CizAsync("dashboard", Yonetici(), agac =>
        {
            Kume(agac, "acikGruplar").Add("filo");
            Kume(agac, "acikAltGruplar").Add("filo/Araç Yönetimi");
        });

        Assert.Contains(@"id=""nav-section-filo-toggle""", html);
        Assert.Contains(@"id=""nav-link-araclar""", html);
    }
}

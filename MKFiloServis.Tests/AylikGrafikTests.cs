using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MKFiloServis.Web.Components.Shared;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

/// <summary>
/// Aylık grafik raporunun saf hesapları (kovalama, kesim günü, geçen yıl karşılaştırması, eksen ölçeği)
/// ve grafik bileşeninin çıktısı. Veritabanı gerektirmez.
/// </summary>
public sealed class AylikGrafikTests
{
    // 19 Ekim 2026 Pazartesi
    private static readonly DateTime Bugun = new(2026, 10, 19);

    private static AylikSatir S(int y, int m, int d, decimal v1, decimal v2 = 0m) => new(new DateTime(y, m, d), v1, v2);

    // ---- kesim günü ----

    [Fact]
    public void Kesim_gunu_icinde_bulunulan_yilda_bugun_gecmis_yilda_31_aralik_gelecek_yilda_yoktur()
    {
        Assert.Equal(new DateTime(2026, 10, 19), AylikSeriHesaplayici.KesimGunu(2026, Bugun));
        Assert.Equal(new DateTime(2025, 12, 31), AylikSeriHesaplayici.KesimGunu(2025, Bugun));
        Assert.Null(AylikSeriHesaplayici.KesimGunu(2027, Bugun));
    }

    [Fact]
    public void Kesim_gunu_saat_bilgisini_yok_sayar()
    {
        Assert.Equal(new DateTime(2026, 10, 19), AylikSeriHesaplayici.KesimGunu(2026, new DateTime(2026, 10, 19, 23, 59, 0)));
    }

    [Fact]
    public void Onceki_yil_kesimi_ayni_gun_olur_29_subat_28_subata_iner_gecmis_yilda_yilin_tamamidir()
    {
        Assert.Equal(new DateTime(2025, 10, 19), AylikSeriHesaplayici.OncekiYilKesimi(2026, Bugun));
        Assert.Equal(new DateTime(2027, 2, 28), AylikSeriHesaplayici.OncekiYilKesimi(2028, new DateTime(2028, 2, 29)));
        Assert.Equal(new DateTime(2024, 12, 31), AylikSeriHesaplayici.OncekiYilKesimi(2025, Bugun));
    }

    // ---- seri ----

    [Fact]
    public void Seri_on_iki_ay_uretir_yalniz_secilen_yilin_kayitlarini_toplar_ve_etiketler()
    {
        var seri = AylikSeriHesaplayici.Seri(
            2026, Bugun,
            [S(2026, 1, 5, 10), S(2026, 1, 31, 5, 1), S(2026, 10, 1, 7), S(2025, 1, 5, 999), S(2027, 1, 1, 999)],
            "Tahsilat", "Ödeme");

        Assert.True(seri.Aylik);
        Assert.Equal(12, seri.Noktalar.Count);
        Assert.Equal("Tahsilat", seri.Etiket1);
        Assert.Equal("Ödeme", seri.Etiket2);

        Assert.Equal(15m, seri.Noktalar[0].Deger1);
        Assert.Equal(1m, seri.Noktalar[0].Deger2);
        Assert.Equal(7m, seri.Noktalar[9].Deger1);
        Assert.Equal(0m, seri.Noktalar[5].Deger1);

        Assert.Equal("Oca", seri.Noktalar[0].Etiket);
        Assert.Equal("Ekim 2026", seri.Noktalar[9].Aciklama);
        Assert.Equal(new DateTime(2026, 10, 1), seri.Noktalar[9].Baslangic);
        Assert.Equal(
            new[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" },
            seri.Noktalar.Select(n => n.Etiket).ToArray());
    }

    [Fact]
    public void Seri_icinde_bulunulan_aydan_sonraki_aylari_gelecek_isaretler()
    {
        var seri = AylikSeriHesaplayici.Seri(2026, Bugun, [], "A", "B");

        Assert.False(seri.Noktalar[8].Gelecek);        // Eylül
        Assert.False(seri.Noktalar[9].Gelecek);        // Ekim: içinde bulunulan ay
        Assert.True(seri.Noktalar[10].Gelecek);        // Kasım
        Assert.False(seri.VeriVar);

        var gecmis = AylikSeriHesaplayici.Seri(2025, Bugun, [], "A", "B");
        Assert.All(gecmis.Noktalar, n => Assert.False(n.Gelecek));

        var gelecek = AylikSeriHesaplayici.Seri(2027, Bugun, [], "A", "B");
        Assert.All(gelecek.Noktalar, n => Assert.True(n.Gelecek));
    }

    // ---- toplam ve karşılaştırma ----

    [Fact]
    public void Toplam_uc_gunleri_dahil_toplar()
    {
        var satirlar = new[] { S(2026, 1, 1, 1, 10), S(2026, 10, 19, 2, 20), S(2026, 10, 20, 4, 40), S(2025, 12, 31, 8, 80) };

        var (t1, t2) = AylikSeriHesaplayici.Toplam(satirlar, new DateTime(2026, 1, 1), new DateTime(2026, 10, 19));

        Assert.Equal(3m, t1);
        Assert.Equal(30m, t2);
    }

    [Fact]
    public void Kart_toplami_bugune_kadardir_gelecek_gunler_ve_planli_degerler_toplama_girmez()
    {
        var tanim = Tanim(yigili: true);
        var satirlar = new[]
        {
            S(2026, 10, 1, 4),                 // yapılan
            S(2026, 10, 25, 0, 3),             // planlı (bugünden sonra)
            S(2026, 9, 1, 9)
        };

        var kart = AylikSeriHesaplayici.KartKur(tanim, 2026, Bugun, satirlar);

        Assert.Equal(13m, kart.Toplam1);
        Assert.Equal(0m, kart.Toplam2);
        Assert.Equal(3m, kart.Seri.Noktalar[9].Deger2);   // grafikte yine görünür
        Assert.True(kart.KarsilastirmaVar);
        Assert.True(kart.Yigili);
    }

    [Fact]
    public void Kart_gecen_yilin_ayni_donemiyle_karsilastirir_sonraki_aylar_karsilastirmaya_girmez()
    {
        var satirlar = new[]
        {
            S(2026, 2, 1, 120),
            S(2026, 10, 19, 80),
            S(2025, 2, 1, 100),                // karşılaştırmaya girer
            S(2025, 10, 19, 50),               // kesim günü dahil
            S(2025, 10, 20, 7000),             // geçen yılın 20 Ekim'i: henüz o güne gelinmedi
            S(2025, 12, 1, 9000)               // geçen yılın aralığı: karşılaştırmaya girmez
        };

        var kart = AylikSeriHesaplayici.KartKur(Tanim(), 2026, Bugun, satirlar);

        Assert.Equal(200m, kart.Toplam1);
        Assert.Equal(150m, kart.OncekiYilToplam1);
        Assert.Equal(DegisimYonu.Artti, kart.Degisim1.Yon);
        Assert.Equal(33, kart.Degisim1.Yuzde);       // (200-150)/150
    }

    [Fact]
    public void Gecmis_yilda_toplam_yilin_tamami_karsilastirma_onceki_yilin_tamamidir()
    {
        var satirlar = new[] { S(2025, 12, 30, 40), S(2025, 3, 1, 60), S(2024, 12, 31, 100), S(2024, 1, 1, 100), S(2026, 1, 5, 5000) };

        var kart = AylikSeriHesaplayici.KartKur(Tanim(), 2025, Bugun, satirlar);

        Assert.Equal(100m, kart.Toplam1);
        Assert.Equal(200m, kart.OncekiYilToplam1);
        Assert.Equal(DegisimYonu.Azaldi, kart.Degisim1.Yon);
        Assert.Equal(50, kart.Degisim1.Yuzde);
        Assert.False(kart.Seri.Noktalar[11].Gelecek);
    }

    [Fact]
    public void Gelecek_yilda_toplam_ve_karsilastirma_yoktur_ama_planli_degerler_grafikte_gorunur()
    {
        var kart = AylikSeriHesaplayici.KartKur(Tanim(), 2027, Bugun, [S(2027, 2, 10, 1500), S(2026, 5, 1, 70)]);

        Assert.False(kart.KarsilastirmaVar);
        Assert.Equal(0m, kart.Toplam1);
        Assert.Equal(0m, kart.OncekiYilToplam1);
        Assert.Equal(DegisimYonu.Yok, kart.Degisim1.Yon);
        Assert.Equal(1500m, kart.Seri.Noktalar[1].Deger1);
        Assert.True(kart.Seri.Noktalar[1].Gelecek);
        Assert.False(kart.Bos);
    }

    [Fact]
    public void Kart_tanimdaki_gorunum_bilgilerini_aynen_tasir()
    {
        var kart = AylikSeriHesaplayici.KartKur(Tanim(), 2026, Bugun, []);

        Assert.Equal("test", kart.Anahtar);
        Assert.Equal("Test kartı", kart.Baslik);
        Assert.Equal(AylikBirim.Para, kart.Birim);
        Assert.Equal(GrafikRengi.Yesil, kart.Renk1);
        Assert.Equal(GrafikRengi.Kirmizi, kart.Renk2);
        Assert.True(kart.YuksekIyi1);
        Assert.True(kart.IkiSeri);
        Assert.True(kart.Bos);
    }

    private static AylikKartTanimi Tanim(bool yigili = false) => new(
        "test", "Test kartı", "alt başlık", "bi-bar-chart", "primary", "/test",
        AylikBirim.Para, "Birinci", "İkinci", GrafikRengi.Yesil, GrafikRengi.Kirmizi, true, false, Yigili: yigili);

    // ---- eksen ölçeği ----

    [Theory]
    [InlineData(0, AylikBirim.Adet, 1, 1, 1)]
    [InlineData(-5, AylikBirim.Para, 1, 1, 1)]
    [InlineData(1, AylikBirim.Adet, 1, 1, 1)]
    [InlineData(3, AylikBirim.Adet, 3, 1, 3)]
    [InlineData(5, AylikBirim.Adet, 5, 1, 5)]
    [InlineData(7, AylikBirim.Adet, 8, 2, 4)]
    [InlineData(12, AylikBirim.Adet, 15, 5, 3)]
    [InlineData(52, AylikBirim.Adet, 60, 20, 3)]
    [InlineData(1234, AylikBirim.Para, 1250, 250, 5)]
    [InlineData(1000000, AylikBirim.Para, 1000000, 200000, 5)]
    [InlineData(2300, AylikBirim.Para, 2500, 500, 5)]
    [InlineData(100000000, AylikBirim.Para, 100000000, 20000000, 5)]
    [InlineData(4567890123, AylikBirim.Para, 5000000000, 1000000000, 5)]
    [InlineData(78000000000000, AylikBirim.Para, 80000000000000, 20000000000000, 4)]
    public void Olcek_en_yuksek_degeri_asan_yuvarlak_adimli_ust_sinir_secer(double azami, AylikBirim birim, double ust, double adim, int aralik)
    {
        var olcek = GrafikOlcegi.Hesapla((decimal)azami, birim);

        Assert.Equal((decimal)ust, olcek.Ust);
        Assert.Equal((decimal)adim, olcek.Adim);
        Assert.Equal(aralik, olcek.AralikSayisi);
        Assert.True(olcek.Ust >= (decimal)azami);
    }

    [Fact]
    public void Adet_olceginde_adim_hic_tam_sayi_olmayan_deger_uretmez()
    {
        for (var azami = 1; azami <= 500; azami++)
        {
            var olcek = GrafikOlcegi.Hesapla(azami, AylikBirim.Adet);
            Assert.True(olcek.Adim >= 1m && olcek.Adim == Math.Truncate(olcek.Adim), $"azami {azami}: adım {olcek.Adim}");
            Assert.True(olcek.Ust >= azami, $"azami {azami}: üst {olcek.Ust}");
            Assert.InRange(olcek.AralikSayisi, 1, 5);
        }
    }

    [Fact]
    public void Olcek_isaretleri_sifirdan_ust_sinira_esit_aralikli_gider()
    {
        var olcek = GrafikOlcegi.Hesapla(1234m, AylikBirim.Para);

        Assert.Equal(new[] { 0m, 250m, 500m, 750m, 1000m, 1250m }, olcek.Isaretler.ToArray());
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(250, "250")]
    [InlineData(1500, "1,5 Bin")]
    [InlineData(250000, "250 Bin")]
    [InlineData(2500000, "2,5 Mn")]
    [InlineData(1200000000, "1,2 Mr")]
    public void Eksen_metni_kisadir_ve_para_birimi_icermez(double deger, string beklenen)
        => Assert.Equal(beklenen, GrafikOlcegi.EksenMetni((decimal)deger));

    // ---- grafik bileşeni ----

    private static ZamanSerisi OrnekSeri(bool ikiSeri = true) => AylikSeriHesaplayici.Seri(
        2026, Bugun,
        [S(2026, 3, 5, 1200, 300), S(2026, 9, 5, 400, 900), S(2026, 10, 5, 800, 100)],
        "Tahsilat", ikiSeri ? "Ödeme" : string.Empty);

    /// <summary>Bileşeni statik HTML'e çizer; <paramref name="sonra"/> ilk çizimden sonra bileşende bir işlem yapıp yeniden çizdirir.</summary>
    private static async Task<string> CizAsync(Dictionary<string, object?> parametreler, Action<AylikGrafik>? sonra = null)
    {
        var yakalayici = new BilesenYakalayici();
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IComponentActivator>(yakalayici)
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var kok = await renderer.RenderComponentAsync<AylikGrafik>(ParameterView.FromDictionary(parametreler));
            if (sonra != null)
            {
                sonra(yakalayici.Grafik!);
                typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(yakalayici.Grafik, null);
            }

            // Dinamik metinler HTML kodlanır (ü → &#xFC;) ve kapsamlı CSS her öğeye b-xxxx özniteliği ekler;
            // karşılaştırmayı okunur tutmak için ikisi de temizlenir.
            var cikti = WebUtility.HtmlDecode(kok.ToHtmlString());
            return Regex.Replace(cikti, @" b-[a-z0-9]+(?=[\s>/])", string.Empty);
        });
    }

    private sealed class BilesenYakalayici : IComponentActivator
    {
        public AylikGrafik? Grafik;

        public IComponent CreateInstance(Type componentType)
        {
            var c = (IComponent)Activator.CreateInstance(componentType)!;
            if (c is AylikGrafik g)
                Grafik = g;
            return c;
        }
    }

    private static void AyaTikla(AylikGrafik grafik, int index)
        => typeof(AylikGrafik).GetMethod("Sec", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(grafik, [index]);

    [Fact]
    public async Task Grafik_on_iki_ay_dugmesi_eksen_ve_zirve_ayini_cizer()
    {
        var html = await CizAsync(new()
        {
            ["Seri"] = OrnekSeri(),
            ["Birim"] = AylikBirim.Para,
            ["Renk1"] = GrafikRengi.Yesil,
            ["Renk2"] = GrafikRengi.Kirmizi,
            ["Bugun"] = (DateTime?)Bugun
        });

        Assert.Equal(12, Count(html, "class=\"ag-ay "));
        Assert.Equal(12, Count(html, "class=\"ag-etiket "));
        Assert.Equal(1, Count(html, "ag-bu-ay"));                       // yalnız Ekim vurgulu
        Assert.Contains("En yüksek ay", html);
        Assert.Contains("Tahsilat: <strong>Mart 2026</strong> · 1.200 TL", html);
        Assert.Contains("Ödeme: <strong>Eylül 2026</strong> · 900 TL", html);       // ikinci serinin zirvesi ayrı gösterilir
        Assert.Contains(">0<", html);                                    // eksen tabanı
        Assert.Contains("Tahsilat", html);
        Assert.Contains("Ödeme", html);
        Assert.Contains("ag-yesil", html);
        Assert.Contains("ag-kirmizi", html);
        Assert.DoesNotContain("ag-yigin", html);
    }

    [Fact]
    public async Task Grafik_ay_dugmesine_tiklaninca_ayrinti_gosterir_tekrar_tiklaninca_zirveye_doner()
    {
        var parametreler = new Dictionary<string, object?>
        {
            ["Seri"] = OrnekSeri(),
            ["Birim"] = AylikBirim.Para,
            ["Bugun"] = (DateTime?)Bugun
        };

        var secili = await CizAsync(parametreler, g => AyaTikla(g, 8));
        Assert.Contains("<strong>Eylül 2026</strong>", secili);
        Assert.Contains("400 TL", secili);
        Assert.Contains("900 TL", secili);
        Assert.Contains("ag-secili", secili);
        Assert.DoesNotContain("En yüksek ay", secili);

        var tekrar = await CizAsync(parametreler, g =>
        {
            AyaTikla(g, 8);
            AyaTikla(g, 8);
        });
        Assert.Contains("Tahsilat: <strong>Mart 2026</strong>", tekrar);
        Assert.Contains("En yüksek ay", tekrar);
        Assert.DoesNotContain("ag-secili", tekrar);
    }

    [Fact]
    public async Task Grafik_yigili_modda_ikinci_seriyi_desenli_parca_olarak_ayni_sutunda_cizer()
    {
        var seri = AylikSeriHesaplayici.Seri(2026, Bugun, [S(2026, 10, 1, 4), S(2026, 10, 25, 0, 3)], "Yapılan sefer", "Planlı sefer");

        var html = await CizAsync(new()
        {
            ["Seri"] = seri,
            ["Birim"] = AylikBirim.Adet,
            ["AdetEki"] = "sefer",
            ["Yigili"] = true,
            ["Bugun"] = (DateTime?)Bugun
        });

        Assert.Equal(1, Count(html, "class=\"ag-yigin\""));              // yalnız Ekim sütunu dolu
        Assert.Equal(1, Count(html, "class=\"ag-parca ag-mavi\""));
        Assert.Equal(1, Count(html, "class=\"ag-parca ag-desen ag-mavi\""));
        Assert.Contains("En yüksek ay: <strong>Ekim 2026</strong>", html);
        Assert.Contains("7 sefer", html);                                // yapılan + planlı
    }

    [Fact]
    public async Task Grafik_tek_seride_ikinci_sutun_ve_ikinci_aciklama_cizmez()
    {
        var html = await CizAsync(new() { ["Seri"] = OrnekSeri(ikiSeri: false), ["Birim"] = AylikBirim.Para });

        Assert.Equal(12, Count(html, "class=\"ag-cubuk "));
        Assert.DoesNotContain("Ödeme", html);
    }

    [Fact]
    public async Task Grafik_veri_yoksa_bos_mesaj_gosterir_ve_hata_vermez()
    {
        var bos = AylikSeriHesaplayici.Seri(2026, Bugun, [], "Masraf", string.Empty);

        var html = await CizAsync(new() { ["Seri"] = bos, ["Birim"] = AylikBirim.Para });

        Assert.Contains("Bu yıl için grafik verisi yok", html);
        Assert.Equal(12, Count(html, "class=\"ag-ay "));
    }

    private static int Count(string metin, string aranan)
    {
        var adet = 0;
        for (var i = metin.IndexOf(aranan, StringComparison.Ordinal); i >= 0; i = metin.IndexOf(aranan, i + aranan.Length, StringComparison.Ordinal))
            adet++;
        return adet;
    }
}

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MKFiloServis.Shared.Time;
using MKFiloServis.Web.Components.Pages.Holding.Bilesenler;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using static MKFiloServis.Tests.HoldingTestAltyapisi;

namespace MKFiloServis.Tests;

/// <summary>
/// Holding grafik/kart bileşenlerinin çıktısı: konumlar, ölçek, negatif değerler, ondalık ayırıcı (CSS/SVG'de nokta),
/// boş durumlar, seçim ve geri çağırımlar. Veritabanı gerektirmez.
/// </summary>
public sealed class HoldingBilesenTests
{
    // ---------------------------------------------------------------- sütun / çizgi grafiği

    private static Dictionary<string, object?> Grafik(IReadOnlyList<HoldingKategori> k, IReadOnlyList<HoldingSeri> s, bool yigili = false)
        => new() { ["Kategoriler"] = k, ["Seriler"] = s, ["Yigili"] = yigili };

    [Fact]
    public async Task Sutun_grafigi_sutun_cizgi_nokta_eksen_ve_aciklamayi_cizer_verisiz_kategoriyi_isaretler()
    {
        var kategoriler = new[]
        {
            new HoldingKategori("Oca", "Ocak 2026"),
            new HoldingKategori("Şub", "Şubat 2026"),
            new HoldingKategori("Mar", "Mart 2026", VeriVar: false)
        };
        var seriler = new[]
        {
            new HoldingSeri("Gelir", HoldingRenk.Gelir, new decimal?[] { 100, 200, null }),
            new HoldingSeri("Gider", HoldingRenk.Gider, new decimal?[] { 50, 80, null }),
            new HoldingSeri("Net kâr", HoldingRenk.Kar, new decimal?[] { 50, 120, null }, HoldingSeriTuru.Cizgi)
        };

        var html = await CizAsync<HoldingSutunGrafigi>(Grafik(kategoriler, seriler));

        Assert.Equal(3, Say(html, @"class=""hsg-kat[\s""]"));                  // üç kategori düğmesi (hsg-kategoriler sayılmaz)
        Assert.Equal(1, Say(html, "hsg-veriyok"));
        Assert.Contains("bottom:0%;height:50%;background:#10b981", html);       // gelir 100 / 200
        Assert.Contains("bottom:0%;height:25%;background:#ef4444", html);       // gider 50 / 200
        Assert.Contains("bottom:0%;height:100%;background:#10b981", html);      // gelir 200 / 200
        Assert.Equal(1, Say(html, "<polyline"));                                // verisi olan iki kategori tek çizgi
        Assert.Contains("points=\"16.67,75 50,40\"", html);
        Assert.Contains("left:16.67%;bottom:25%;background:#2563eb", html);     // çizgi noktası
        Assert.Contains("title=\"Ocak 2026 — Gelir: 100 TL, Gider: 50 TL, Net kâr: 50 TL\"", html);
        Assert.Contains("title=\"Mart 2026 — veri toplanmamış\"", html);
        Assert.Contains(">200<", html);                                         // eksen üst sınırı
        Assert.Contains("En yüksek gelir: Şubat 2026 · 200 TL", html);
    }

    [Fact]
    public async Task Sutun_grafigi_negatif_degeri_sifir_cizgisinin_altina_indirir()
    {
        var kategoriler = new[] { new HoldingKategori("A", "A ayı"), new HoldingKategori("B", "B ayı") };
        var seriler = new[] { new HoldingSeri("Kâr", HoldingRenk.Kar, new decimal?[] { -50, 100 }) };

        var html = await CizAsync<HoldingSutunGrafigi>(Grafik(kategoriler, seriler));

        // Ölçek −50…100: sıfır çizgisi %33,33'te; pozitif sütun üstüne, negatif sütun altına uzanır.
        Assert.Contains("bottom:33.33%;height:66.67%", html);
        Assert.Contains("bottom:0%;height:33.33%", html);
        Assert.Contains(">-50<", html);
        Assert.Contains("hsg-taban", html);
    }

    [Fact]
    public async Task Sutun_grafigi_css_sayilarinda_virgul_degil_nokta_kullanir()
    {
        var kategoriler = new[] { new HoldingKategori("A", "A"), new HoldingKategori("B", "B"), new HoldingKategori("C", "C") };
        var seriler = new[]
        {
            new HoldingSeri("Değer", HoldingRenk.Arac, new decimal?[] { 1, 3, 7 }),
            new HoldingSeri("Çizgi", HoldingRenk.Kar, new decimal?[] { 2, 5, 6 }, HoldingSeriTuru.Cizgi)
        };

        var html = await CizAsync<HoldingSutunGrafigi>(Grafik(kategoriler, seriler));

        Assert.Contains("height:12.5%", html);                                  // 1 / 8
        Assert.Contains("height:87.5%", html);                                  // 7 / 8
        Assert.DoesNotMatch(@"style=""[^""]*\d,\d[^""]*""", html);              // yerel ondalık ayırıcı CSS'i bozardı
    }

    [Fact]
    public async Task Sutun_grafigi_yigili_modda_pozitif_degerleri_ust_uste_dizer()
    {
        var kategoriler = new[] { new HoldingKategori("A", "A"), new HoldingKategori("B", "B") };
        var seriler = new[]
        {
            new HoldingSeri("X", HoldingRenk.Firma(0), new decimal?[] { 30, 70 }),
            new HoldingSeri("Y", HoldingRenk.Firma(1), new decimal?[] { 10, 0 })
        };

        var html = await CizAsync<HoldingSutunGrafigi>(Grafik(kategoriler, seriler, yigili: true));

        Assert.Equal(2, Say(html, "hsg-yuva-yigin"));                           // kategori başına tek yığın
        Assert.Contains("bottom:0%;height:37.5%", html);                        // X: 30 / 80
        Assert.Contains("bottom:37.5%;height:12.5%", html);                     // Y: X'in üstünde
        Assert.Contains("bottom:0%;height:87.5%", html);                        // ikinci kategoride X: 70 / 80
    }

    [Fact]
    public async Task Sutun_grafigi_veri_yoksa_bos_mesaj_gosterir()
    {
        var kategoriler = new[] { new HoldingKategori("A", "A") };
        var sifir = new[] { new HoldingSeri("Gelir", HoldingRenk.Gelir, new decimal?[] { 0 }) };

        var html = await CizAsync<HoldingSutunGrafigi>(new()
        {
            ["Kategoriler"] = kategoriler, ["Seriler"] = sifir, ["BosMesaj"] = "Bu dönem için grafik verisi yok."
        });

        Assert.Contains("hsg-bos", html);
        Assert.Contains("Bu dönem için grafik verisi yok.", html);
        Assert.DoesNotContain("hsg-govde", html);

        var kategorisiz = await CizAsync<HoldingSutunGrafigi>(Grafik([], []));
        Assert.Contains("hsg-bos", kategorisiz);
    }

    [Fact]
    public async Task Sutun_grafigi_kategoriye_tiklayinca_ayrinti_gosterir_tekrar_tiklayinca_kapatir()
    {
        var kategoriler = new[] { new HoldingKategori("Oca", "Ocak 2026"), new HoldingKategori("Şub", "Şubat 2026") };
        var seriler = new[] { new HoldingSeri("Gelir", HoldingRenk.Gelir, new decimal?[] { 100, 200 }) };

        var secili = await CizAsync<HoldingSutunGrafigi>(Grafik(kategoriler, seriler), sonra: g => Cagir(g, "Sec", 1));
        Assert.Equal(1, Say(secili, "hsg-secili"));
        Assert.Contains("aria-pressed=\"true\"", secili);
        Assert.Contains("<strong>Şubat 2026</strong>", secili);
        Assert.Contains("Gelir <strong>200 TL</strong>", secili);

        var kapali = await CizAsync<HoldingSutunGrafigi>(Grafik(kategoriler, seriler), sonra: g => { Cagir(g, "Sec", 1); Cagir(g, "Sec", 1); });
        Assert.DoesNotContain("hsg-secili", kapali);
        Assert.Contains("En yüksek gelir: Şubat 2026", kapali);
    }

    // ---------------------------------------------------------------- halka grafik

    [Fact]
    public async Task Halka_grafik_dilimleri_yuzdeye_gore_cizer_sifir_ve_negatifi_atar()
    {
        var dilimler = new[]
        {
            new HoldingDilim("A", 50, "f0"), new HoldingDilim("B", 30, "f1"), new HoldingDilim("C", 20, "f2"),
            new HoldingDilim("Sıfır", 0, "f3"), new HoldingDilim("Negatif", -5, "f4")
        };

        var html = await CizAsync<HoldingHalkaGrafik>(new() { ["Dilimler"] = dilimler, ["MerkezDeger"] = "100 TL", ["MerkezAlt"] = "Toplam" });

        Assert.Equal(3, Say(html, "stroke-dasharray"));                         // yalnız pozitif üç dilim
        Assert.Contains("stroke-dasharray=\"49.6 50.4\"", html);                // %50 − ince ayrım payı
        Assert.Contains("stroke-dashoffset=\"25\"", html);                      // üstten başlar
        Assert.Contains("stroke-dashoffset=\"-25\"", html);                     // ikinci dilim %50'den sonra
        Assert.Contains("stroke-dashoffset=\"-55\"", html);
        Assert.Contains("stroke=\"#6366f1\"", html);                            // f0 paleti
        Assert.Contains("<strong>%50</strong>", html);
        Assert.Contains("<strong>%30</strong>", html);
        Assert.Contains("<strong>%20</strong>", html);
        Assert.DoesNotContain("Negatif", html);
        Assert.Contains(">100 TL<", html);                                      // halka ortası
    }

    [Fact]
    public async Task Halka_grafik_tek_dilimde_halkayi_tamamen_doldurur_bos_veride_mesaj_verir()
    {
        var tek = await CizAsync<HoldingHalkaGrafik>(new() { ["Dilimler"] = new[] { new HoldingDilim("A", 7, "gelir") } });
        Assert.Contains("stroke-dasharray=\"100 0\"", tek);

        var bos = await CizAsync<HoldingHalkaGrafik>(new() { ["Dilimler"] = Array.Empty<HoldingDilim>(), ["BosMesaj"] = "Dağılım yok." });
        Assert.Equal(0, Say(bos, "stroke-dasharray"));
        Assert.Contains("Dağılım yok.", bos);
    }

    // ---------------------------------------------------------------- çubuk listesi

    private static HoldingCubukSatiri Cubuk(string etiket, decimal deger, string metin, string? renk = null)
        => new(etiket, null, [new HoldingCubukDegeri("Değer", deger, metin, HoldingRenk.Gelir)], renk);

    [Fact]
    public async Task Cubuk_listesi_ortak_olcekte_genislik_verir()
    {
        var html = await CizAsync<HoldingCubukListesi>(new()
        {
            ["Satirlar"] = new[] { Cubuk("A", 100, "100 TL", "f0"), Cubuk("B", 50, "50 TL") }
        });

        Assert.Contains("left:0%;width:100%;background:#10b981", html);
        Assert.Contains("left:0%;width:50%;background:#10b981", html);
        Assert.DoesNotContain("hcl-sifir", html);                               // negatif yok: sıfır çizgisi çizilmez
        Assert.Contains("background:#6366f1", html);                            // satırın firma noktası
        Assert.DoesNotContain("hcl-ad", html);                                  // tek çubuklu satırda seri adı yazılmaz
    }

    [Fact]
    public async Task Cubuk_listesi_negatif_degeri_sifir_cizgisinin_soluna_cizer()
    {
        var html = await CizAsync<HoldingCubukListesi>(new() { ["Satirlar"] = new[] { Cubuk("A", -50, "-50 TL"), Cubuk("B", 100, "100 TL") } });

        Assert.Contains("left:0%;width:33.33%", html);                          // negatif: sıfırdan sola
        Assert.Contains("left:33.33%;width:66.67%", html);                      // pozitif: sıfırdan sağa
        Assert.Equal(2, Say(html, "class=\"hcl-sifir\" style=\"left:33.33%\""));
    }

    [Fact]
    public async Task Cubuk_listesi_cok_cubuklu_satirda_seri_adlarini_yazar_ve_bos_listede_mesaj_verir()
    {
        var satir = new HoldingCubukSatiri("Firma", "Marj %10",
        [
            new HoldingCubukDegeri("Gelir", 10, "10 TL", HoldingRenk.Gelir),
            new HoldingCubukDegeri("Gider", 5, "5 TL", HoldingRenk.Gider)
        ]);

        var html = await CizAsync<HoldingCubukListesi>(new() { ["Satirlar"] = new[] { satir } });
        Assert.Equal(2, Say(html, "class=\"hcl-ad\""));
        Assert.Contains("Marj %10", html);

        var bos = await CizAsync<HoldingCubukListesi>(new() { ["Satirlar"] = Array.Empty<HoldingCubukSatiri>(), ["BosMesaj"] = "Veri yok." });
        Assert.Contains("Veri yok.", bos);
    }

    // ---------------------------------------------------------------- gösterge

    [Fact]
    public async Task Gosterge_yayi_oranda_doldurur_ustunu_kirpar_isareti_dogru_yere_koyar()
    {
        var html = await CizAsync<HoldingGosterge>(new() { ["Yuzde"] = 45m, ["Renk"] = HoldingRenk.Gelir, ["DegerMetni"] = "%45", ["Isaret"] = 50m });

        Assert.Contains("stroke-dasharray=\"45 100\"", html);
        Assert.Contains("stroke=\"#10b981\"", html);
        Assert.Contains("%45", html);
        // %50 yayın tam tepesi: x = merkez (60), iç/dış yarıçapta y = 22 ve 1
        Assert.Contains("x1=\"60\" y1=\"22\" x2=\"60\" y2=\"1\"", html);

        var tasan = await CizAsync<HoldingGosterge>(new() { ["Yuzde"] = 150m, ["DegerMetni"] = "%150" });
        Assert.Contains("stroke-dasharray=\"100 100\"", tasan);
        Assert.DoesNotContain("hgs-isaret", tasan);

        var sifir = await CizAsync<HoldingGosterge>(new() { ["Yuzde"] = 0m, ["DegerMetni"] = "%0" });
        Assert.DoesNotContain("stroke-dasharray", sifir);                       // sıfır oranda yay çizilmez
    }

    // ---------------------------------------------------------------- rozet, kart, mini çizgi

    [Fact]
    public async Task Rozet_rengi_anlama_oku_yone_gore_cizilir_degisim_yoksa_hic_cizilmez()
    {
        var iyi = await CizAsync<HoldingRozetEtiketi>(new() { ["Rozet"] = HoldingRozet.Degisim(OzetDegisim.Hesapla(120m, 100m), true, "Mart ile karşılaştırma") });
        Assert.Contains("hrz-iyi", iyi);
        Assert.Contains("bi-arrow-up-short", iyi);
        Assert.Contains("artış", iyi);                                          // ekran okuyucu için
        Assert.Contains("%20", iyi);
        Assert.Contains("title=\"Mart ile karşılaştırma\"", iyi);

        var kotu = await CizAsync<HoldingRozetEtiketi>(new() { ["Rozet"] = HoldingRozet.Degisim(OzetDegisim.Hesapla(80m, 100m), true) });
        Assert.Contains("hrz-kotu", kotu);
        Assert.Contains("bi-arrow-down-short", kotu);

        var esit = await CizAsync<HoldingRozetEtiketi>(new() { ["Rozet"] = HoldingRozet.Degisim(OzetDegisim.Hesapla(100m, 100m), true) });
        Assert.Contains("hrz-notr", esit);
        Assert.DoesNotContain("bi-arrow", esit);

        var yok = await CizAsync<HoldingRozetEtiketi>(new() { ["Rozet"] = HoldingRozet.Yok });
        Assert.DoesNotContain("hrz", yok);
    }

    [Fact]
    public async Task Kpi_karti_degeri_rozeti_ve_mini_cizgiyi_gosterir()
    {
        var html = await CizAsync<HoldingKpiKarti>(new()
        {
            ["Baslik"] = "Net Kâr", ["Deger"] = "-5 Mn TL", ["Ikon"] = "bi-graph-up", ["Renk"] = HoldingRenk.Kar, ["Olumsuz"] = true,
            ["Ipucu"] = "-5.000.000 TL", ["AltMetin"] = "Mart 2026: 3 Mn TL",
            ["Rozet"] = HoldingRozet.Degisim(OzetDegisim.Hesapla(-5m, 3m), true),
            ["Seri"] = new decimal?[] { 1, 2, 3 }
        });

        Assert.Contains("Net Kâr", html);
        Assert.Contains("kpi-olumsuz", html);
        Assert.Contains("title=\"-5.000.000 TL\"", html);
        Assert.Contains("Mart 2026: 3 Mn TL", html);
        Assert.Contains("hrz-kotu", html);
        Assert.Contains("class=\"spk\"", html);
        Assert.Contains("color:#2563eb;background:#2563eb22", html);

        var sade = await CizAsync<HoldingKpiKarti>(new() { ["Baslik"] = "Araç", ["Deger"] = "61", ["Kucuk"] = true });
        Assert.Contains("kpi-kucuk", sade);
        Assert.DoesNotContain("class=\"spk\"", sade);
        Assert.DoesNotContain("kpi-olumsuz", sade);
    }

    [Fact]
    public async Task Mini_cizgi_kesintiyi_boler_nokta_ayiricisi_kullanir_veri_yoksa_kesik_cizgi_cizer()
    {
        var tam = await CizAsync<HoldingSparkline>(new() { ["Degerler"] = new decimal?[] { 10, 20, 30 } });
        Assert.Equal(1, Say(tam, "<polyline"));
        Assert.Contains("points=\"0,27 50,15 100,3\"", tam);                    // en küçük altta, en büyük üstte

        var bolunmus = await CizAsync<HoldingSparkline>(new() { ["Degerler"] = new decimal?[] { 10, null, 30, 40 } });
        Assert.Equal(1, Say(bolunmus, "<polyline"));                            // tek noktalık parça çizilmez
        Assert.Contains("points=\"66.67,11 100,3\"", bolunmus);                 // yalnız verisi ardışık olan kısım çizilir

        var sabit = await CizAsync<HoldingSparkline>(new() { ["Degerler"] = new decimal?[] { 5, 5, 5 } });
        Assert.Contains("points=\"0,15 50,15 100,15\"", sabit);                 // aralık yoksa orta çizgi

        var bos = await CizAsync<HoldingSparkline>(new() { ["Degerler"] = new decimal?[] { null, null } });
        Assert.Equal(0, Say(bos, "<polyline"));
        Assert.Contains("stroke-dasharray=\"3 3\"", bos);
    }

    // ---------------------------------------------------------------- dönem seçici

    [Fact]
    public async Task Donem_secici_gelecek_ayi_kilitler_verili_ayi_isaretler_tum_yili_istege_bagli_gosterir()
    {
        var bugun = BusinessTime.Today;

        var html = await CizAsync<HoldingDonemSecici>(new()
        {
            ["Yil"] = bugun.Year, ["Ay"] = 0, ["VeriVarAylar"] = new[] { 1, 2 }
        });

        Assert.Equal(13, Say(html, @"class=""hds-ay[\s""]"));                  // Tüm Yıl + 12 ay
        Assert.Contains("Tüm Yıl", html);
        Assert.Equal(12 - bugun.Month, Say(html, "title=\"Henüz başlamadı\""));
        Assert.Equal(2, Say(html, "hds-verili"));
        Assert.Contains("hds-tum hds-aktif", html);

        var gecmis = await CizAsync<HoldingDonemSecici>(new() { ["Yil"] = bugun.Year - 1, ["Ay"] = 5, ["TumYilVar"] = false });
        Assert.DoesNotContain("Henüz başlamadı", gecmis);
        Assert.DoesNotContain("Tüm Yıl", gecmis);
        Assert.Equal(12, Say(gecmis, @"class=""hds-ay[\s""]"));
        Assert.Equal(1, Say(gecmis, "hds-aktif"));
    }

    [Fact]
    public async Task Donem_secici_secimi_geri_cagirir_ayni_secimde_cagirmaz_yil_degisince_gelecek_ayi_kirpar()
    {
        var bugun = BusinessTime.Today;
        var secimler = new List<HoldingDonemSecimi>();
        var geriCagirim = new EventCallbackFactory().Create<HoldingDonemSecimi>(new object(), (Action<HoldingDonemSecimi>)secimler.Add);

        await CizAsync<HoldingDonemSecici>(
            new() { ["Yil"] = bugun.Year - 1, ["Ay"] = 12, ["OnDegisti"] = geriCagirim },
            sonraAsync: async s =>
            {
                await CagirAsync(s, "AySecAsync", 3);              // ay değişti
                await CagirAsync(s, "AySecAsync", 12);             // aynı ay: çağrılmaz
                await CagirAsync(s, "YilaGitAsync", bugun.Year);   // yıl değişti: içinde bulunulan yılda gelecek ay seçili kalmaz
                await CagirAsync(s, "YilaGitAsync", bugun.Year - 1); // aynı yıl: çağrılmaz
            });

        Assert.Equal(2, secimler.Count);
        Assert.Equal(new HoldingDonemSecimi(bugun.Year - 1, 3), secimler[0]);
        Assert.Equal(new HoldingDonemSecimi(bugun.Year, Math.Min(12, bugun.Month)), secimler[1]);
    }

    // ---------------------------------------------------------------- liste, not, kart, tablo, hücre

    [Fact]
    public async Task Bulgular_onem_siniflarini_baglantiyi_ve_tamam_mesajini_cizer()
    {
        var bulgular = new[]
        {
            new HoldingBulgu(HoldingBulguOnemi.Kritik, "Zarar", "Net zarar var.", "/holding/karsilastirma"),
            new HoldingBulgu(HoldingBulguOnemi.Uyari, "Uyarı", "Dikkat."),
            new HoldingBulgu(HoldingBulguOnemi.Bilgi, "Bilgi", "Not."),
            new HoldingBulgu(HoldingBulguOnemi.Iyi, "İyi", "Güzel.")
        };

        var html = await CizAsync<HoldingBulgular>(new() { ["Bulgular"] = bulgular, ["Sutunlu"] = true });

        Assert.Contains("hbl-madde hbl-kritik", html);
        Assert.Contains("hbl-madde hbl-uyari", html);
        Assert.Contains("hbl-madde hbl-bilgi", html);
        Assert.Contains("hbl-madde hbl-iyi", html);
        Assert.Contains("<a class=\"hbl-baslik\" href=\"/holding/karsilastirma\">Zarar</a>", html);
        Assert.Contains("<span class=\"hbl-baslik\">Uyarı</span>", html);
        Assert.Contains("hbl-sutunlu", html);

        var bos = await CizAsync<HoldingBulgular>(new() { ["Bulgular"] = Array.Empty<HoldingBulgu>() });
        Assert.Contains("Dikkat gerektiren bir durum görünmüyor.", bos);
    }

    [Fact]
    public async Task Not_kart_tablo_hucre_ve_firma_etiketi_beklenen_isaretlemeyi_uretir()
    {
        RenderFragment icerik(string metin) => b => b.AddContent(0, metin);

        var not = await CizAsync<HoldingNot>(new() { ["Uyari"] = true, ["ChildContent"] = icerik("Dikkat edin") });
        Assert.Contains("hnt hnt-uyari", not);
        Assert.Contains("bi-exclamation-triangle", not);
        Assert.Contains("Dikkat edin", not);
        Assert.Contains("bi-info-circle", await CizAsync<HoldingNot>(new() { ["ChildContent"] = icerik("Bilgi") }));

        var kart = await CizAsync<HoldingKart>(new()
        {
            ["Baslik"] = "Başlık", ["AltBaslik"] = "Alt", ["Ikon"] = "bi-bar-chart", ["Renk"] = HoldingRenk.Gelir, ["ChildContent"] = icerik("gövde")
        });
        Assert.Contains("<h3 class=\"hk-baslik\">Başlık</h3>", kart);
        Assert.Contains("color:#10b981;background:#10b98122", kart);
        Assert.Contains("gövde", kart);
        Assert.DoesNotContain("hk-eylemler", kart);

        var tablo = await CizAsync<HoldingTablo>(new() { ["ChildContent"] = icerik("<tr>") });
        Assert.Contains("table-responsive", tablo);

        var hucre = await CizAsync<HoldingHucreCubugu>(new() { ["Oran"] = 150m, ["Metin"] = "10 TL", ["Renk"] = HoldingRenk.Arac });
        Assert.Contains("width:100%;background:#f59e0b", hucre);                // 100'ü aşan oran kırpılır
        Assert.Contains("width:37.5%", await CizAsync<HoldingHucreCubugu>(new() { ["Oran"] = 37.5m, ["Metin"] = "x" }));
        Assert.Contains("width:0%", await CizAsync<HoldingHucreCubugu>(new() { ["Oran"] = -5m, ["Metin"] = "x" }));

        var firma = await CizAsync<HoldingFirmaEtiketi>(new() { ["Ad"] = "ÜSTÜN GRUP", ["Sira"] = 2 });
        Assert.Contains("background:#fb923c", firma);                           // firma paleti, sıra 2
        Assert.Contains("ÜSTÜN GRUP", firma);
    }

    [Fact]
    public async Task Sayfa_basligi_aciklamayi_kontrolleri_ve_bilgi_satirini_cizer()
    {
        RenderFragment parca(string metin) => b => b.AddContent(0, metin);

        var html = await CizAsync<HoldingSayfaBasligi>(new()
        {
            ["Baslik"] = "Holding Dashboard", ["Ikon"] = "bi-buildings", ["Aciklama"] = "Tek ekranda özet",
            ["Kontroller"] = parca("DONEM-SECICI"), ["BilgiSatiri"] = parca("BILGI-SATIRI")
        });

        Assert.Contains("Holding Dashboard", html);
        Assert.Contains("Tek ekranda özet", html);
        Assert.Contains("DONEM-SECICI", html);
        Assert.Contains("BILGI-SATIRI", html);

        var yalin = await CizAsync<HoldingSayfaBasligi>(new() { ["Baslik"] = "Sade" });
        Assert.DoesNotContain("hsb-kontroller", yalin);
        Assert.DoesNotContain("hsb-bilgi", yalin);
    }

    // ---------------------------------------------------------------- içerik kabuğu

    private static RenderFragment<string> Icerik => s => b => b.AddContent(0, "içerik:" + s);

    [Fact]
    public async Task Icerik_kabugu_iskelet_hata_ve_veri_durumlarini_ayirir()
    {
        var iskelet = await CizAsync<HoldingIcerik<string>>(new() { ["ChildContent"] = Icerik });
        Assert.Contains("placeholder", iskelet);
        Assert.DoesNotContain("içerik:", iskelet);

        var hata = await CizAsync<HoldingIcerik<string>>(new() { ["Hata"] = "Yüklenemedi.", ["ChildContent"] = Icerik });
        Assert.Contains("Yüklenemedi.", hata);
        Assert.Contains("Yeniden dene", hata);
        Assert.DoesNotContain("içerik:", hata);

        var veri = await CizAsync<HoldingIcerik<string>>(new() { ["Veri"] = "A", ["ChildContent"] = Icerik });
        Assert.Contains("içerik:A", veri);
        Assert.DoesNotContain("hic-yenileniyor", veri);

        // Yeniden yükleme sürerken içerik silinmez, soluklaşır.
        var yukleniyor = await CizAsync<HoldingIcerik<string>>(new() { ["Veri"] = "A", ["Yukleniyor"] = true, ["ChildContent"] = Icerik });
        Assert.Contains("hic-yenileniyor", yukleniyor);
        Assert.Contains("içerik:A", yukleniyor);

        // Veri varken yenileme hatası: içerik korunur, üstte uyarı çıkar.
        var kismi = await CizAsync<HoldingIcerik<string>>(new() { ["Veri"] = "A", ["Hata"] = "Yenilenemedi.", ["ChildContent"] = Icerik });
        Assert.Contains("içerik:A", kismi);
        Assert.Contains("Yenilenemedi.", kismi);
    }

    // ---------------------------------------------------------------- eski veri notu

    [Fact]
    public async Task Eski_veri_notu_yalniz_secilen_donemde_eski_yontemli_ay_varsa_gorunur()
    {
        var satirlar = new[]
        {
            Satir(1, 2026, 1, 100, 50, eski: true), Satir(1, 2026, 2, 100, 50, eski: true),
            Satir(1, 2026, 3, 100, 50, eski: true), Satir(1, 2026, 4, 100, 50)
        };

        var tumYil = await CizAsync<HoldingEskiVeriNotu>(new() { ["Analiz"] = HoldingAnalizi.Olustur(satirlar, [], 2026, 0) });
        Assert.Contains("Ocak–Mart", tumYil);
        Assert.Contains("eski yöntemle toplanmış", tumYil);

        var yeniAy = await CizAsync<HoldingEskiVeriNotu>(new() { ["Analiz"] = HoldingAnalizi.Olustur(satirlar, [], 2026, 4) });
        Assert.DoesNotContain("eski yöntemle", yeniAy);

        var eskiAy = await CizAsync<HoldingEskiVeriNotu>(new() { ["Analiz"] = HoldingAnalizi.Olustur(satirlar, [], 2026, 2) });
        Assert.Contains("<strong>Şubat</strong>", eskiAy);

        Assert.DoesNotContain("eski yöntemle", await CizAsync<HoldingEskiVeriNotu>());
    }

    // ---------------------------------------------------------------- veri toplama düğmesi

    private static Action<IServiceCollection> Servisler(SahteHoldingServisi servis, ToastService toast)
        => s => s.AddSingleton<IHoldingService>(servis).AddSingleton<IToastService>(toast);

    [Fact]
    public async Task Veri_toplama_dugmesi_secili_ayi_ya_da_yilin_aylarini_toplar_bitince_geri_cagirir()
    {
        var bugun = BusinessTime.Today;
        var gecenYil = bugun.Year - 1;

        // Seçili ay: yalnız o ay toplanır; "Yılı güncelle" ayrıca sunulur.
        var servis = new SahteHoldingServisi();
        var toast = new ToastService();
        var mesajlar = new List<ToastMessage>();
        toast.OnShow += mesajlar.Add;
        var bitti = 0;
        var geriCagirim = new EventCallbackFactory().Create(new object(), () => bitti++);

        var html = await CizAsync<HoldingVeriTopla>(
            new() { ["Yil"] = gecenYil, ["Ay"] = 4, ["OnTamamlandi"] = geriCagirim },
            Servisler(servis, toast),
            sonraAsync: s => CagirAsync(s, "DonemiTopla"));

        Assert.Equal([(gecenYil, 4)], servis.Toplamalar);
        Assert.Equal(1, bitti);
        Assert.Contains(mesajlar, m => m.Message.Contains("Nisan"));
        Assert.Contains("Yılı güncelle", html);

        // Tüm yıl: geçmiş yılda 12 ayın hepsi sırayla.
        var yil = new SahteHoldingServisi();
        await CizAsync<HoldingVeriTopla>(
            new() { ["Yil"] = gecenYil, ["Ay"] = 0 },
            Servisler(yil, new ToastService()),
            sonraAsync: s => CagirAsync(s, "DonemiTopla"));
        Assert.Equal(Enumerable.Range(1, 12).Select(a => (gecenYil, a)).ToArray(), yil.Toplamalar.ToArray());

        // İçinde bulunulan yıl: Ocak'tan bugünkü aya kadar; gelecek yıl: toplanacak bir şey yok.
        var buYil = new SahteHoldingServisi();
        await CizAsync<HoldingVeriTopla>(
            new() { ["Yil"] = bugun.Year, ["Ay"] = 5 },
            Servisler(buYil, new ToastService()),
            sonraAsync: s => CagirAsync(s, "YiliTopla"));
        Assert.Equal(bugun.Month, buYil.Toplamalar.Count);
        Assert.Equal((bugun.Year, 1), buYil.Toplamalar.First());

        var gelecek = new SahteHoldingServisi();
        await CizAsync<HoldingVeriTopla>(
            new() { ["Yil"] = bugun.Year + 1, ["Ay"] = 0 },
            Servisler(gelecek, new ToastService()),
            sonraAsync: s => CagirAsync(s, "DonemiTopla"));
        Assert.Empty(gelecek.Toplamalar);
    }

    [Fact]
    public async Task Veri_toplama_hatasi_kullaniciya_bildirilir_geri_cagirim_calismaz()
    {
        var servis = new SahteHoldingServisi { ToplamaHatasi = true };
        var toast = new ToastService();
        var mesajlar = new List<ToastMessage>();
        toast.OnShow += mesajlar.Add;
        var bitti = 0;

        await CizAsync<HoldingVeriTopla>(
            new() { ["Yil"] = 2020, ["Ay"] = 4, ["OnTamamlandi"] = new EventCallbackFactory().Create(new object(), () => bitti++) },
            Servisler(servis, toast),
            sonraAsync: s => CagirAsync(s, "DonemiTopla"));

        Assert.Equal(0, bitti);
        Assert.Contains(mesajlar, m => m.Message.Contains("Veri toplama hatası"));
    }

    [Fact]
    public async Task Bos_durum_donemi_adlandirir_ve_guncelle_dugmesini_gosterir()
    {
        var html = await CizAsync<HoldingBosDurum>(
            new() { ["Yil"] = 2026, ["Ay"] = 4 },
            Servisler(new SahteHoldingServisi(), new ToastService()));

        Assert.Contains("Bu dönem için veri yok", html);
        Assert.Contains("Nisan 2026 için henüz holding verisi toplanmamış", html);
        Assert.Contains("Güncelle", html);
        Assert.DoesNotContain("Yılı güncelle", html);                          // boş durumda tek düğme (Kompakt)

        var ozel = await CizAsync<HoldingBosDurum>(
            new() { ["Yil"] = 2026, ["Ay"] = 4, ["Mesaj"] = "Özel mesaj." },
            Servisler(new SahteHoldingServisi(), new ToastService()));
        Assert.Contains("Özel mesaj.", ozel);
    }
}

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Components.Layout;

namespace MKFiloServis.Tests;

/// <summary>
/// Ana menünün yapısı ve görünürlük kuralları. Veritabanı, lisans servisi ya da tarayıcı gerektirmez.
/// Menü kuralı: her sayfa menüde bir kez geçer, her bağlantının bir sayfası vardır, kimse eskiden gördüğü
/// bağlantıyı kaybetmez.
/// </summary>
public sealed class AnaMenuYapisiTests
{
    private static IReadOnlyList<MenuOgesi> TumOgeler { get; } = MenuFiltresi.TumOgeler(AnaMenuYapisi.Kokler).ToList();

    // ── yapı ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Hicbir_baglanti_menude_iki_kez_gecmez()
    {
        var tekrarlar = TumOgeler
            .GroupBy(o => o.Href, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(tekrarlar.Count == 0, "Menüde tekrar eden bağlantı var: " + string.Join(", ", tekrarlar));
    }

    [Fact]
    public void Her_baglantinin_bir_sayfasi_vardir()
    {
        var sayfaDosyalari = SayfaDosyalari();

        var oluBaglantilar = TumOgeler
            .Select(o => (Oge: o, Yol: MenuEslestirici.AdresiAyir(o.Href).Yol))
            .Where(x => !sayfaDosyalari.ContainsKey(x.Yol))
            .Select(x => $"{x.Oge.Etiket} → {x.Oge.Href}")
            .ToList();

        Assert.True(oluBaglantilar.Count == 0, "Sayfası olmayan menü bağlantıları: " + string.Join("; ", oluBaglantilar));
    }

    [Fact]
    public void Iki_farkli_menu_yolu_ayni_sayfa_dosyasina_gitmez()
    {
        var sayfaDosyalari = SayfaDosyalari();

        // Aynı yola sorguyla giden bağlantılar (rent-a-car?sekme=...) tek sayfanın sekmeleridir; yol bir kez sayılır.
        var yollar = TumOgeler
            .Select(o => MenuEslestirici.AdresiAyir(o.Href).Yol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(sayfaDosyalari.ContainsKey);

        var kopyalar = yollar
            .GroupBy(y => sayfaDosyalari[y], StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"{Path.GetFileName(g.Key)}: {string.Join(" + ", g)}")
            .ToList();

        Assert.True(kopyalar.Count == 0, "Aynı sayfaya giden birden çok menü girişi: " + string.Join("; ", kopyalar));
    }

    [Fact]
    public void Grup_anahtarlari_ve_alt_grup_basliklari_benzersizdir()
    {
        var gruplar = AnaMenuYapisi.Kokler.OfType<MenuGrubu>().ToList();

        Assert.Equal(gruplar.Count, gruplar.Select(g => g.Anahtar).Distinct(StringComparer.Ordinal).Count());
        foreach (var grup in gruplar)
        {
            var basliklar = grup.AltGruplar.Select(a => a.Baslik ?? string.Empty).ToList();
            Assert.Equal(basliklar.Count, basliklar.Distinct(StringComparer.Ordinal).Count());
            Assert.All(grup.AltGruplar, a => Assert.NotEmpty(a.Ogeler));
        }
    }

    [Fact]
    public void Tum_faturalar_tek_Faturalar_grubundadir_Cari_Yonetimi_yalniz_cari_sayfalarini_tasir()
    {
        var cari = AnaMenuYapisi.Kokler.OfType<MenuGrubu>().Single(g => g.Anahtar == "cari");
        var fatura = AnaMenuYapisi.Kokler.OfType<MenuGrubu>().Single(g => g.Anahtar == "fatura");
        var cariHref = cari.AltGruplar.SelectMany(a => a.Ogeler).Select(o => o.Href).ToList();
        var faturaHref = fatura.AltGruplar.SelectMany(a => a.Ogeler).Select(o => o.Href).ToList();

        Assert.DoesNotContain(cariHref, h => h.Contains("fatura", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("/efatura/giden", faturaHref);
        Assert.Contains("/efatura/gelen", faturaHref);
        Assert.Contains("/efatura/stok-turu-eslestir", faturaHref);
        Assert.Contains("/faturalar", faturaHref);
        Assert.Contains("/proforma-faturalar", faturaHref);
        Assert.Contains("/faturalar/toplu-fatura", faturaHref);
        Assert.Contains("/faturalar/efatura-xml", faturaHref);
        Assert.Contains("/faturalar/luca-portal", faturaHref);
    }

    [Fact]
    public void Eski_kopya_girisler_tek_kalir_ve_sayfasi_olmayan_AI_Fatura_Import_yoktur()
    {
        Assert.Single(TumOgeler, o => KapsarAdres(o, "/araclar/plaka-takip"));
        Assert.Single(TumOgeler, o => KapsarAdres(o, "/raporlar/arac-karlilik"));
        Assert.DoesNotContain(TumOgeler, o => o.Href.Contains("fatura-ai-import", StringComparison.OrdinalIgnoreCase));

        // Tekilleştirilen sayfaların eski adresleri bağlantıya takma ad olarak bağlıdır (eski adreste de menü doğru vurgulanır).
        var plaka = TumOgeler.Single(o => KapsarAdres(o, "/araclar/plaka-takip"));
        Assert.Contains("/filo-operasyon/kiralik-c-plaka-takip", plaka.EkYollar!);
        var rapor = TumOgeler.Single(o => KapsarAdres(o, "/raporlar/arac-karlilik"));
        Assert.Contains("/rent-a-car/arac-karlilik", rapor.EkYollar!);
        Assert.Contains("/muhasebe/arac-karlilik", rapor.EkYollar!);
    }

    private static bool KapsarAdres(MenuOgesi oge, string yol)
        => string.Equals(MenuEslestirici.AdresiAyir(oge.Href).Yol, yol, StringComparison.OrdinalIgnoreCase)
           || (oge.EkYollar?.Any(y => string.Equals(y, yol, StringComparison.OrdinalIgnoreCase)) ?? false);

    // ── görünürlük ──────────────────────────────────────────────────────────────

    private static MenuGorunurluk Bagla(
        IEnumerable<string> yetkiler,
        bool yonetici = false,
        bool yuklendi = true,
        Func<string, bool>? lisansli = null,
        string? rol = null)
    {
        var set = yetkiler.ToHashSet(StringComparer.Ordinal);
        var onekler = set.Select(y => { var i = y.IndexOf('.'); return i > 0 ? y[..i] : y; }).ToHashSet(StringComparer.Ordinal);
        return new MenuGorunurluk(lisansli ?? (_ => true), yonetici, yuklendi, set, onekler, rol ?? (yonetici ? "Admin" : "Kullanici"));
    }

    private static List<string> Gorunenler(MenuGorunurluk baglam)
        => MenuFiltresi.Filtrele(AnaMenuYapisi.Kokler, baglam).SelectMany(k => k.TumOgeler).Select(o => o.Href).ToList();

    private static List<string> GorunenGruplar(MenuGorunurluk baglam)
        => MenuFiltresi.Filtrele(AnaMenuYapisi.Kokler, baglam).Where(k => k.Grup is not null).Select(k => k.Grup!.Grup.Anahtar).ToList();

    [Fact]
    public void Lisansli_yonetici_menudeki_her_baglantiyi_gorur()
    {
        var gorunen = Gorunenler(Bagla([], yonetici: true));

        Assert.Equal(TumOgeler.Count, gorunen.Count);
        Assert.Equal(116, TumOgeler.Count);   // 115 + Destek Ayarları
    }

    [Fact]
    public void Yetkileri_yuklenmemis_kullanici_yalniz_yetki_istemeyen_baglantilari_gorur()
    {
        var gorunen = Gorunenler(Bagla(["cariler.oku", "kesilenfatura.oku"], yuklendi: false));

        // Yetki istemeyen ama grubu kural taşıyanlar (örn. Ayarlar > Kişisel) da gizli: grup kuralı yüklenmeden geçmez.
        Assert.Equal(new[] { "/dashboard" }, gorunen);
    }

    [Fact]
    public void Yalniz_fatura_yetkisi_olan_kullanici_faturalari_gorur_Cari_grubunu_gormez()
    {
        var baglam = Bagla(["kesilenfatura.oku", "gelenfatura.oku"]);

        var gorunen = Gorunenler(baglam);

        Assert.Contains("/efatura/giden", gorunen);
        Assert.Contains("/efatura/gelen", gorunen);
        Assert.Contains("/efatura/stok-turu-eslestir", gorunen);
        Assert.DoesNotContain("/cariler", gorunen);
        Assert.DoesNotContain("/faturalar", gorunen);   // faturalar.oku yok
        Assert.DoesNotContain("cari", GorunenGruplar(baglam));
        Assert.Contains("fatura", GorunenGruplar(baglam));
    }

    [Fact]
    public void Fatura_modulu_lisanssizsa_fatura_baglantilari_gizlenir()
    {
        var baglam = Bagla(["kesilenfatura.oku", "gelenfatura.oku", "faturalar.oku"], lisansli: kod => !kod.Contains("fatura", StringComparison.Ordinal) && kod != "menu.fatura");

        Assert.DoesNotContain("fatura", GorunenGruplar(baglam));
    }

    [Fact]
    public void Arac_kar_zarar_raporu_iki_eski_yetkiden_biriyle_Raporlar_grubunda_gorunur()
    {
        var rentACar = Bagla(["rentacar.oku", Yetkiler.RentACarKarlilikOku]);
        var muhasebe = Bagla([Yetkiler.MuhasebeAracKarlilikOku]);
        var hicbiri = Bagla(["rentacar.oku"]);

        Assert.Contains("/raporlar/arac-karlilik", Gorunenler(rentACar));
        Assert.Contains("/raporlar/arac-karlilik", Gorunenler(muhasebe));
        Assert.DoesNotContain("/raporlar/arac-karlilik", Gorunenler(hicbiri));
        Assert.Contains("raporlar", GorunenGruplar(muhasebe));
    }

    [Fact]
    public void Kiralik_C_plaka_takip_araclar_ya_da_raporlar_yetkisiyle_gorunur()
    {
        Assert.Contains("/araclar/plaka-takip", Gorunenler(Bagla(["araclar.oku"])));
        Assert.Contains("/araclar/plaka-takip", Gorunenler(Bagla(["raporlar.oku"])));   // eski Satış/Kiralama yolu
        Assert.DoesNotContain("/araclar/plaka-takip", Gorunenler(Bagla(["cariler.oku"])));
    }

    [Fact]
    public void Kara_liste_yalniz_Admin_rolunde_gorunur()
    {
        var yonetici = Gorunenler(Bagla([], yonetici: true));
        var rentACar = Gorunenler(Bagla(["rentacar.oku"]));

        Assert.Contains("/rent-a-car/kara-liste", yonetici);
        Assert.DoesNotContain("/rent-a-car/kara-liste", rentACar);
        Assert.Contains("/rent-a-car?sekme=rezervasyon", rentACar);
    }

    [Fact]
    public void Belge_uyarilari_artik_Belge_Yonetimi_grubundadir_ve_ebys_yetkisi_istemez()
    {
        // Eskiden grubun dışında, yalnız belgeuyari.oku ile görünürdü; aynı kural korunur.
        var baglam = Bagla(["belgeuyari.oku"]);
        var ebys = MenuFiltresi.Filtrele(AnaMenuYapisi.Kokler, baglam).Single(k => k.Grup?.Grup.Anahtar == "ebys");

        Assert.Contains(ebys.TumOgeler, o => o.Href == "/belge-uyarilari");
    }

    [Fact]
    public void Destek_talep_acma_herkese_aciktir_ekip_ekranlari_yetki_ister()
    {
        // Yetkisi olmayan kullanıcı da sorun bildirebilmeli: talep açma, Taleplerim ve Bilgi Bankası herkese açık.
        var yetkisiz = Bagla([]);
        var gorunen = Gorunenler(yetkisiz);

        Assert.Contains("destek", GorunenGruplar(yetkisiz));
        Assert.Contains("/destek-talepleri/talep-giris", gorunen);
        Assert.Contains("/destek-talepleri/taleplerim", gorunen);
        Assert.Contains("/destek-talepleri/bilgi-bankasi", gorunen);
        Assert.DoesNotContain("/destek-talepleri", gorunen);
        Assert.DoesNotContain("/destek-talepleri/yonetim", gorunen);
        Assert.DoesNotContain("/destek-talepleri/ayarlar", gorunen);
        Assert.DoesNotContain("ayarlar", GorunenGruplar(yetkisiz));
    }

    [Theory]
    [InlineData("destektalebi.yaz")]
    [InlineData("destektalebi.yazilim")]
    public void Destek_ekip_ekranlari_personel_ya_da_yazilim_yetkisiyle_gorunur(string yetki)
    {
        var gorunen = Gorunenler(Bagla([yetki]));

        Assert.Contains("/destek-talepleri", gorunen);
        Assert.Contains("/destek-talepleri/yonetim", gorunen);
        Assert.DoesNotContain("/destek-talepleri/ayarlar", gorunen);   // destektalebi.duzenle gerekir
    }

    [Fact]
    public void Destek_ayarlari_duzenle_yetkisiyle_gorunur()
    {
        var baglam = Bagla(["destektalebi.duzenle"]);

        Assert.Contains("/destek-talepleri/ayarlar", Gorunenler(baglam));
        Assert.DoesNotContain("/ayarlar/firmalar", Gorunenler(baglam));
    }

    // ── adres eşleştirme (tek vurgu) ─────────────────────────────────────────────

    [Theory]
    [InlineData("butce/odemeler", "Ödeme Yönetimi")]                       // üstteki "Bütçe Analiz" değil
    [InlineData("butce", "Bütçe Analiz")]
    [InlineData("faturalar/123", "Tüm Faturalar")]
    [InlineData("faturalar/toplu-fatura", "Toplu Fatura")]
    [InlineData("cariler/yeni", "Cari Listesi")]
    [InlineData("efatura/giden?yil=2026&ay=5", "Kesilen Faturalar (Satış)")]
    [InlineData("rent-a-car?sekme=araclar", "Araçlar / Müsaitlik")]
    [InlineData("rent-a-car?sekme=teslim-iade", "Teslim / İade")]
    [InlineData("rent-a-car", "Rezervasyon")]                               // sekme verilmezse sayfa varsayılan sekmeyi açar
    [InlineData("rent-a-car?sekme=bilinmeyen", "Rezervasyon")]
    [InlineData("rent-a-car/takvim", "Takvim")]
    [InlineData("proforma-fatura/12/duzenle", "Proforma Fatura")]
    [InlineData("soforler/5", "Personel Listesi")]
    [InlineData("filo-operasyon/kiral%C4%B1k-c-plaka-takip", "Kiralık C Plaka Takip")]   // yüzde kodlu Türkçe alias
    [InlineData("muhasebe/arac-karlilik", "Araç Kâr/Zarar Raporu")]
    [InlineData("EFATURA/GELEN", "Gelen Faturalar (Alış)")]                // büyük/küçük harf duyarsız
    public void Adres_tek_bir_baglantiyi_vurgular(string adres, string beklenenEtiket)
    {
        var secilen = MenuEslestirici.EnIyiEslesme(TumOgeler, adres);

        Assert.Equal(beklenenEtiket, secilen?.Etiket);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bilinmeyen/sayfa")]
    [InlineData("login")]
    public void Menude_karsiligi_olmayan_adres_hicbir_baglantiyi_vurgulamaz(string adres)
        => Assert.Null(MenuEslestirici.EnIyiEslesme(TumOgeler, adres));

    // ── yardımcılar ─────────────────────────────────────────────────────────────

    /// <summary>"@page" rotası → sayfa dosyası (rota küçük harfe duyarsız).</summary>
    private static Dictionary<string, string> SayfaDosyalari()
    {
        var harita = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dosya in Directory.EnumerateFiles(BilesenlerKlasoru(), "*.razor", SearchOption.AllDirectories))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(dosya), @"(?m)^\s*@page\s+""([^""]+)"""))
                harita.TryAdd(m.Groups[1].Value.TrimEnd('/'), dosya);
        }

        return harita;
    }

    // Yönlendirilebilir (@page) bileşenler yalnız Pages altında olmak zorunda değildir; Components kökü taranır.
    // Depo kökü önce bu dosyanın derleme anındaki yolundan bulunur (çıktı klasöründen bağımsız); olmazsa çıktı/çalışma
    // klasöründen yukarı yürünür.
    private static string BilesenlerKlasoru([CallerFilePath] string testDosyasi = "")
    {
        var baslangiclar = new List<string>();
        if (!string.IsNullOrEmpty(testDosyasi))
            baslangiclar.Add(Path.GetDirectoryName(testDosyasi)!);   // …\MKFiloServis.Tests
        baslangiclar.Add(AppContext.BaseDirectory);
        baslangiclar.Add(Directory.GetCurrentDirectory());

        foreach (var baslangic in baslangiclar)
        {
            for (var dizin = new DirectoryInfo(baslangic); dizin is not null; dizin = dizin.Parent)
            {
                var aday = Path.Combine(dizin.FullName, "MKFiloServis.Web", "Components");
                if (Directory.Exists(Path.Combine(aday, "Pages")))
                    return aday;
            }
        }

        throw new DirectoryNotFoundException("MKFiloServis.Web/Components bulunamadı (testler depo içinden çalıştırılmalı).");
    }
}

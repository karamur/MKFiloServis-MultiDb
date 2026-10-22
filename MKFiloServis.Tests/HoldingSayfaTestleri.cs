using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MKFiloServis.Shared.Time;
using MKFiloServis.Web.Components.Pages.Holding;
using MKFiloServis.Web.Components.Pages.Holding.Bilesenler;
using MKFiloServis.Web.Services;
using static MKFiloServis.Tests.HoldingTestAltyapisi;

namespace MKFiloServis.Tests;

/// <summary>
/// Holding sayfalarının veri hazırlığı (sahte servisle), ortak sayfa tabanı (açılış dönemi, eski yanıtı atma, hata) ve yedi
/// sayfanın duman testleri: dolu, boş ve hatalı durumda çizim özelliğini yitirmez.
/// </summary>
public sealed class HoldingSayfaTestleri
{
    // ---------------------------------------------------------------- veri hazırlığı

    [Fact]
    public async Task Genel_bakis_analizi_butceyi_vadeyi_cumleleri_ve_bulgulari_birlestirir()
    {
        var yil = BusinessTime.Today.Year;
        var servis = OrnekServis();

        var g = await HoldingSayfaVerisi.GenelBakisAsync(servis, yil, 3, BusinessTime.Today);

        Assert.True(g.Analiz.VeriVar);
        Assert.Equal(1600m, g.Analiz.Toplam.Gelir);                              // 1200 + 400
        Assert.NotNull(g.Butce);
        Assert.Equal(3, g.Butce!.Ay);
        Assert.NotNull(g.Vade);
        Assert.Equal(800m, g.Vade!.Borc.Toplam);
        Assert.Contains(g.Cumleler, c => c.Contains("firmanın toplam geliri"));
        Assert.Contains(g.Bulgular, b => b.Baslik == "Birinci Firma: zarar");    // mart: 1200 gelir, 1300 gider
        Assert.Contains(g.Bulgular, b => b.Baslik == "Vadesi geçmiş ödemeler");
        Assert.Equal("Şubat " + yil, g.Analiz.OncekiEtiket);                     // mart'ın karşılaştırma dönemi şubat
    }

    [Fact]
    public async Task Genel_bakis_butce_ya_da_vade_hatasinda_sayfanin_geri_kalanini_dusurmez()
    {
        var yil = BusinessTime.Today.Year;
        var servis = OrnekServis();
        servis.ButceHatasi = true;
        servis.VadeHatasi = true;
        var gunluk = new List<string>();

        var g = await HoldingSayfaVerisi.GenelBakisAsync(servis, yil, 3, BusinessTime.Today, (_, mesaj) => gunluk.Add(mesaj));

        Assert.True(g.Analiz.VeriVar);
        Assert.Null(g.Butce);
        Assert.Null(g.Vade);
        Assert.Equal(2, gunluk.Count);

        // Ana veri (kâr/zarar) alınamazsa hata yutulmaz: sayfa bunu kullanıcıya gösterir.
        servis.KarHatasi = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => HoldingSayfaVerisi.GenelBakisAsync(servis, yil, 3, BusinessTime.Today));
    }

    [Fact]
    public async Task Odeme_sayfasi_vade_hatasini_mesaj_olarak_tasir_analizi_korur()
    {
        var yil = BusinessTime.Today.Year;
        var servis = OrnekServis();
        servis.VadeHatasi = true;
        Exception? yakalanan = null;

        var s = await HoldingSayfaVerisi.OdemeSayfasiAsync(servis, yil, 3, BusinessTime.Today, ex => yakalanan = ex);

        Assert.True(s.Analiz.VeriVar);
        Assert.Null(s.Vade);
        Assert.Equal("Vade bilgisi şu anda alınamadı.", s.VadeHatasi);
        Assert.NotNull(yakalanan);

        servis.VadeHatasi = false;
        var iyi = await HoldingSayfaVerisi.OdemeSayfasiAsync(servis, yil, 3, BusinessTime.Today);
        Assert.NotNull(iyi.Vade);
        Assert.Null(iyi.VadeHatasi);
    }

    [Theory]
    [InlineData(4, 4)]     // seçili ay aynen
    [InlineData(0, 3)]     // tüm yıl: verisi olan son ay (örnek veri mart'a kadar)
    public async Task Butce_ayi_secili_ayi_ya_da_tum_yilda_verisi_olan_son_ayi_kullanir(int ay, int beklenen)
    {
        var yil = BusinessTime.Today.Year;
        var servis = OrnekServis();
        var analiz = await HoldingSayfaVerisi.AnalizAsync(servis, yil, ay);

        Assert.Equal(beklenen, HoldingSayfaVerisi.ButceAyi(analiz, yil, ay, BusinessTime.Today));
    }

    [Fact]
    public async Task Butce_ayi_veri_yoksa_icinde_bulunulan_yilda_bugunku_ay_baska_yilda_aralik_olur()
    {
        var bugun = BusinessTime.Today;
        var bos = await HoldingSayfaVerisi.AnalizAsync(new SahteHoldingServisi(), bugun.Year, 0);

        Assert.Equal(bugun.Month, HoldingSayfaVerisi.ButceAyi(bos, bugun.Year, 0, bugun));
        Assert.Equal(12, HoldingSayfaVerisi.ButceAyi(bos, bugun.Year - 1, 0, bugun));
    }

    // ---------------------------------------------------------------- ortak sayfa tabanı

    private class TestSayfa : HoldingSayfaTabani<string>
    {
        public readonly List<(int Yil, int Ay)> Istekler = new();
        public readonly Dictionary<(int, int), TaskCompletionSource<string>> Bekleyenler = new();
        public bool IlkIstekHata { get; set; }

        protected override Task<string> VeriGetirAsync(int yil, int ay)
        {
            Istekler.Add((yil, ay));

            // İlk (açılış) yükleme hemen biter; sonrakiler testin denetiminde bekler.
            if (Istekler.Count == 1)
                return IlkIstekHata ? Task.FromException<string>(new InvalidOperationException("ilk yükleme hatası")) : Task.FromResult("ilk");

            var tcs = new TaskCompletionSource<string>();
            Bekleyenler[(yil, ay)] = tcs;
            return tcs.Task;
        }

        public Task Degistir(int yil, int ay) => DonemDegistiAsync(new HoldingDonemSecimi(yil, ay));
        public Task Yenile() => YenidenYukleAsync();
        public string? Goster => Veri;
        public bool YukleniyorMu => Yukleniyor;
        public string? HataMetni => Hata;
        public int SeciliYil => Yil;
        public int SeciliAy => Ay;

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Veri ?? "yok");
    }

    private sealed class TumYilSayfa : TestSayfa
    {
        protected override bool VarsayilanTumYil => true;
    }

    private sealed class YilsizSayfa : TestSayfa
    {
        protected override bool TumYilDestekli => false;
        protected override bool VarsayilanTumYil => true;
    }

    private static Action<IServiceCollection> Servis(SahteHoldingServisi s) => c => c.AddSingleton<IHoldingService>(s);

    [Fact]
    public async Task Sayfa_tabani_verisi_olan_son_donemle_acilir()
    {
        var yil = BusinessTime.Today.Year;
        TestSayfa? sayfa = null;
        var html = await CizAsync<TestSayfa>(servisler: Servis(new SahteHoldingServisi { SonDonem = new HoldingDonemSecimi(yil, 3) }), sonra: s => sayfa = s);

        Assert.Equal("ilk", html);
        Assert.Equal(yil, sayfa!.SeciliYil);
        Assert.Equal(3, sayfa.SeciliAy);
        Assert.Equal(new[] { (yil, 3) }, sayfa.Istekler.ToArray());
        Assert.False(sayfa.YukleniyorMu);
        Assert.Null(sayfa.HataMetni);
    }

    [Fact]
    public async Task Sayfa_tabani_veri_yoksa_ya_da_donem_okunamazsa_icinde_bulunulan_aya_duser()
    {
        var bugun = BusinessTime.Today;

        TestSayfa? veriYok = null;
        await CizAsync<TestSayfa>(servisler: Servis(new SahteHoldingServisi()), sonra: s => veriYok = s);
        Assert.Equal((bugun.Year, bugun.Month), (veriYok!.SeciliYil, veriYok.SeciliAy));

        TestSayfa? hatali = null;
        await CizAsync<TestSayfa>(servisler: Servis(new SahteHoldingServisi { SonDonemHatasi = true }), sonra: s => hatali = s);
        Assert.Equal((bugun.Year, bugun.Month), (hatali!.SeciliYil, hatali.SeciliAy));
        Assert.Equal("ilk", hatali.Goster);                                      // dönem okunamasa da sayfa açılır
    }

    [Fact]
    public async Task Sayfa_tabani_cok_eski_donemi_secilebilir_yil_araligina_kirpar()
    {
        var bugun = BusinessTime.Today;
        TestSayfa? sayfa = null;

        await CizAsync<TestSayfa>(servisler: Servis(new SahteHoldingServisi { SonDonem = new HoldingDonemSecimi(bugun.Year - 10, 6) }), sonra: s => sayfa = s);

        Assert.Equal(bugun.Year - 3, sayfa!.SeciliYil);
        Assert.Equal(6, sayfa.SeciliAy);
    }

    [Fact]
    public async Task Sayfa_tabani_tum_yil_ister_ve_destekliyorsa_ay_sifirla_acilir()
    {
        var yil = BusinessTime.Today.Year;
        var servis = new SahteHoldingServisi { SonDonem = new HoldingDonemSecimi(yil, 3) };

        TumYilSayfa? tumYil = null;
        await CizAsync<TumYilSayfa>(servisler: Servis(servis), sonra: s => tumYil = s);
        Assert.Equal(0, tumYil!.SeciliAy);
        Assert.Equal(yil, tumYil.SeciliYil);

        YilsizSayfa? yilsiz = null;
        await CizAsync<YilsizSayfa>(servisler: Servis(servis), sonra: s => yilsiz = s);
        Assert.Equal(3, yilsiz!.SeciliAy);                                       // tüm yıl desteklenmiyorsa verisi olan son ay
    }

    [Fact]
    public async Task Sayfa_tabani_ilk_yukleme_hatasini_gosterir_yenileyince_duzeltir()
    {
        var yil = BusinessTime.Today.Year;

        // İlk istek hata fırlatır; Veri boş kalır, kullanıcıya mesaj verilir; yenileyince hata temizlenir.
        var hataliHtml = await CizAsync<HataliIlkIstekSayfasi>(
            servisler: Servis(new SahteHoldingServisi { SonDonem = new HoldingDonemSecimi(yil, 3) }),
            sonraAsync: async s =>
            {
                Assert.Null(s.Goster);
                Assert.Contains("yüklenemedi", s.HataMetni);
                Assert.False(s.YukleniyorMu);

                var yenile = s.Yenile();
                s.Bekleyenler[(yil, 3)].SetResult("düzeldi");
                await yenile;
                Assert.Equal("düzeldi", s.Goster);
                Assert.Null(s.HataMetni);
            });
        Assert.Equal("düzeldi", hataliHtml);
    }

    private sealed class HataliIlkIstekSayfasi : TestSayfa
    {
        public HataliIlkIstekSayfasi() => IlkIstekHata = true;
    }

    [Fact]
    public async Task Sayfa_tabani_hizli_donem_degisiminde_eski_yaniti_atar()
    {
        var yil = BusinessTime.Today.Year;

        await CizAsync<TestSayfa>(
            servisler: Servis(new SahteHoldingServisi { SonDonem = new HoldingDonemSecimi(yil, 3) }),
            sonraAsync: async s =>
            {
                var birinci = s.Degistir(yil, 1);
                var ikinci = s.Degistir(yil, 2);
                Assert.True(s.YukleniyorMu);

                // Yeni istek önce biter, eski istek sonra gelir: ekranda yeni istek kalmalı.
                s.Bekleyenler[(yil, 2)].SetResult("ikinci");
                await ikinci;
                Assert.Equal("ikinci", s.Goster);
                Assert.False(s.YukleniyorMu);

                s.Bekleyenler[(yil, 1)].SetResult("birinci");
                await birinci;
                Assert.Equal("ikinci", s.Goster);
                Assert.Equal(2, s.SeciliAy);
            });
    }

    // ---------------------------------------------------------------- sayfa duman testleri

    private static Task<string> Ciz(string sayfa, IHoldingService servis) => sayfa switch
    {
        "dashboard" => SayfaCizAsync<HoldingDashboard>(servis),
        "karsilastirma" => SayfaCizAsync<FirmaKarsilastirma>(servis),
        "butce" => SayfaCizAsync<ButceKonsolidasyonu>(servis),
        "odemeler" => SayfaCizAsync<OdemePlani>(servis),
        "arac" => SayfaCizAsync<AracMaliyetOzeti>(servis),
        "personel" => SayfaCizAsync<PersonelGiderOzeti>(servis),
        "hakedis" => SayfaCizAsync<HakedisOzeti>(servis),
        _ => throw new ArgumentOutOfRangeException(nameof(sayfa))
    };

    [Theory]
    [InlineData("dashboard", "Holding Dashboard", "Dönem Özeti", "Firma Bazlı Özet", "Detaylı Raporlar", "Mart")]
    [InlineData("karsilastirma", "Firma Karşılaştırma", "Firma Karşılaştırma Tablosu", "Aylık Gelir — Firma Payları", "Kâr Marjı Sıralaması", "yılı")]
    [InlineData("butce", "Bütçe Konsolidasyonu", "Bütçe Tablosu", "Holding Bütçe Kullanımı", "harcama sınırıdır", "Mart")]
    [InlineData("odemeler", "Ödeme Planı", "Güncel Vade Takvimi", "Dönem Ödeme Tablosu", "Toplam Ödenecek", "Mart")]
    [InlineData("arac", "Araç Maliyet Özeti", "Firma Bazında Araç Maliyeti", "Aylık Araç Maliyeti", "Araç Başına Maliyet", "yılı")]
    [InlineData("personel", "Personel Gider Özeti", "Firma Bazında Personel Gideri", "Aylık Personel Maliyeti", "Kişi Başına Maliyet", "yılı")]
    [InlineData("hakedis", "Hakediş Özeti", "Firma Bazında Hakediş", "Aylık Hakediş", "onaylanmış", "yılı")]
    public async Task Sayfa_dolu_veriyle_basliklari_kartlari_tabloyu_ve_firmalari_cizer(
        string sayfa, string a, string b, string c, string d, string e)
    {
        var html = await Ciz(sayfa, OrnekServis());

        foreach (var metin in new[] { a, b, c, d, e })
            Assert.Contains(metin, html);

        Assert.Contains("Birinci Firma", html);
        Assert.Contains("İkinci Firma", html);
        Assert.Contains("hds-aylar", html);                                      // dönem seçici
        Assert.DoesNotContain("yüklenemedi", html);
        Assert.DoesNotContain("placeholder", html);                              // yükleme iskeleti kalmaz
        Assert.DoesNotContain("Bu dönem için veri yok", html);
    }

    [Theory]
    [InlineData("dashboard")]
    [InlineData("karsilastirma")]
    [InlineData("butce")]
    [InlineData("odemeler")]
    [InlineData("arac")]
    [InlineData("personel")]
    [InlineData("hakedis")]
    public async Task Sayfa_veri_yoksa_yonlendirici_bos_durum_gosterir(string sayfa)
    {
        var html = await Ciz(sayfa, new SahteHoldingServisi());

        Assert.Contains("Bu dönem için veri yok", html);
        Assert.Contains("güncelle", html, StringComparison.OrdinalIgnoreCase);   // "Güncelle" (ay) ya da "Yılı güncelle" (tüm yıl)
        Assert.DoesNotContain("yüklenemedi", html);
    }

    [Theory]
    [InlineData("dashboard")]
    [InlineData("karsilastirma")]
    [InlineData("butce")]
    [InlineData("odemeler")]
    [InlineData("arac")]
    [InlineData("personel")]
    [InlineData("hakedis")]
    public async Task Sayfa_ana_veri_alinamazsa_anlasilir_hata_ve_yeniden_dene_gosterir(string sayfa)
    {
        var servis = OrnekServis();
        servis.KarHatasi = true;

        var html = await Ciz(sayfa, servis);

        Assert.Contains("Veriler şu anda yüklenemedi", html);
        Assert.Contains("Yeniden dene", html);
        Assert.DoesNotContain("Firma Bazlı Özet", html);
    }

    [Fact]
    public async Task Dashboard_vade_ve_butce_hatasinda_acilir_ilgili_kartlar_yardimci_mesaj_verir()
    {
        var servis = OrnekServis();
        servis.VadeHatasi = true;
        servis.ButceHatasi = true;

        var html = await Ciz("dashboard", servis);

        Assert.Contains("Firma Bazlı Özet", html);                               // ana içerik sağlam
        Assert.Contains("Vade bilgisi şu anda alınamadı.", html);
        Assert.Contains("Bütçe hedefi tanımlı değil ya da bütçe verisi toplanmamış.", html);
        Assert.DoesNotContain("yüklenemedi", html);
    }

    [Fact]
    public async Task Odeme_plani_vade_hatasinda_donem_odeme_durumunu_yine_gosterir()
    {
        var servis = OrnekServis();
        servis.VadeHatasi = true;

        var html = await Ciz("odemeler", servis);

        Assert.Contains("Vade bilgisi şu anda alınamadı.", html);
        Assert.Contains("Dönem Ödeme Tablosu", html);
        Assert.DoesNotContain("Toplam Ödenecek", html);                           // vade göstergeleri hiç çizilmez
    }

    [Fact]
    public async Task Dashboard_eski_yontemle_toplanmis_ayi_uyarir_yeni_ayda_uyarmaz()
    {
        var yil = BusinessTime.Today.Year;
        var servis = new SahteHoldingServisi { SonDonem = new HoldingDonemSecimi(yil, 2) };
        servis.Kar.Add(Satir(1, yil, 1, 100, 50, eski: true));
        servis.Kar.Add(Satir(1, yil, 2, 100, 50, eski: true));

        var eski = await Ciz("dashboard", servis);
        Assert.Contains("eski yöntemle toplanmış", eski);

        servis.Kar.Clear();
        servis.Kar.Add(Satir(1, yil, 2, 100, 50));
        var yeni = await Ciz("dashboard", servis);
        Assert.DoesNotContain("eski yöntemle toplanmış", yeni);
    }

    [Fact]
    public async Task Karsilastirma_tum_yili_ay_ay_degil_firma_basina_toplar()
    {
        var html = await Ciz("karsilastirma", OrnekServis());

        // Üç aylık örnek veri: Birinci Firma gelir 1000 + 1100 + 1200 = 3.300 TL (tek satır, aylık satır değil).
        Assert.Contains("3.300 TL", html);
        Assert.Equal(1, Say(html, @"<td><span class=""hfe"" title=""Birinci Firma"""));
    }
}

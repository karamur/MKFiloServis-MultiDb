using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

/// <summary>Yönetici özetinin saf hesapları: dönem aralıkları, değişim yüzdesi, cümleler.</summary>
public sealed class YoneticiOzetTests
{
    // 19 Ekim 2026 Pazartesi
    private static readonly DateTime Bugun = new(2026, 10, 19);

    [Fact]
    public void Bu_ay_bugune_kirpilir_ve_gecen_ayin_ayni_gunleriyle_karsilastirilir()
    {
        var d = YoneticiOzetDonemi.Hesapla(YoneticiOzetDonemTuru.BuAy, Bugun);

        Assert.Equal(new DateTime(2026, 10, 1), d.Baslangic);
        Assert.Equal(new DateTime(2026, 10, 31), d.Bitis);
        Assert.Equal(Bugun, d.EtkinBitis);
        Assert.Equal(19, d.GecenGunSayisi);
        Assert.Equal(31, d.ToplamGunSayisi);
        Assert.True(d.GelecekGunVar);
        Assert.Equal(new DateTime(2026, 9, 1), d.OncekiBaslangic);
        Assert.Equal(new DateTime(2026, 9, 19), d.OncekiBitis);
        Assert.Equal("Ekim 2026", d.Etiket);
        Assert.Equal("1 – 31 Ekim 2026", d.AralikMetni);
        Assert.Equal("1 – 19 Ekim 2026", d.EtkinAralikMetni);
        Assert.Equal("1 – 19 Eylül 2026", d.KarsilastirmaMetni);
        Assert.Equal("Bu ay", d.CumleGirisi);
        Assert.False(d.GrafikAylik);
    }

    [Fact]
    public void Ayin_sonunda_onceki_ay_daha_kisaysa_karsilastirma_onceki_ayin_sonunda_biter()
    {
        var d = YoneticiOzetDonemi.Hesapla(YoneticiOzetDonemTuru.BuAy, new DateTime(2026, 3, 31));

        Assert.Equal(new DateTime(2026, 2, 1), d.OncekiBaslangic);
        Assert.Equal(new DateTime(2026, 2, 28), d.OncekiBitis);
    }

    [Fact]
    public void Gecen_ay_tum_ayi_onceki_ay_da_tum_ayi_kapsar()
    {
        var d = YoneticiOzetDonemi.Hesapla(YoneticiOzetDonemTuru.GecenAy, Bugun);

        Assert.Equal(new DateTime(2026, 9, 1), d.Baslangic);
        Assert.Equal(new DateTime(2026, 9, 30), d.Bitis);
        Assert.Equal(d.Bitis, d.EtkinBitis);
        Assert.False(d.GelecekGunVar);
        Assert.Equal(new DateTime(2026, 8, 1), d.OncekiBaslangic);
        Assert.Equal(new DateTime(2026, 8, 31), d.OncekiBitis);
        Assert.Equal("Eylül 2026", d.Etiket);
    }

    [Theory]
    [InlineData(2026, 10, 19, "2026-10-19", "2026-10-25", "2026-10-12", "2026-10-12")] // pazartesi: haftanın 1. günü
    [InlineData(2026, 10, 22, "2026-10-19", "2026-10-25", "2026-10-12", "2026-10-15")] // perşembe: 4 gün
    [InlineData(2026, 10, 25, "2026-10-19", "2026-10-25", "2026-10-12", "2026-10-18")] // pazar: tam hafta
    public void Bu_hafta_pazartesi_baslar_ve_gecen_haftanin_ayni_gunleriyle_karsilastirilir(
        int y, int m, int g, string bas, string bit, string oncekiBas, string oncekiBit)
    {
        var d = YoneticiOzetDonemi.Hesapla(YoneticiOzetDonemTuru.BuHafta, new DateTime(y, m, g));

        Assert.Equal(DateTime.Parse(bas), d.Baslangic);
        Assert.Equal(DateTime.Parse(bit), d.Bitis);
        Assert.Equal(DateTime.Parse(oncekiBas), d.OncekiBaslangic);
        Assert.Equal(DateTime.Parse(oncekiBit), d.OncekiBitis);
    }

    [Fact]
    public void Bu_yil_aylik_grafik_verir_ve_gecen_yilin_ayni_gunleriyle_karsilastirilir()
    {
        var d = YoneticiOzetDonemi.Hesapla(YoneticiOzetDonemTuru.BuYil, Bugun);

        Assert.Equal(new DateTime(2026, 1, 1), d.Baslangic);
        Assert.Equal(new DateTime(2026, 12, 31), d.Bitis);
        Assert.Equal(Bugun, d.EtkinBitis);
        Assert.True(d.GrafikAylik);
        Assert.Equal(new DateTime(2025, 1, 1), d.OncekiBaslangic);
        Assert.Equal(new DateTime(2025, 10, 19), d.OncekiBitis);
        Assert.Equal("2026", d.Etiket);
    }

    [Fact]
    public void Bugun_dunle_karsilastirilir_ve_grafik_son_on_dort_gunu_gosterir()
    {
        var d = YoneticiOzetDonemi.Hesapla(YoneticiOzetDonemTuru.Bugun, Bugun);

        Assert.Equal(Bugun, d.Baslangic);
        Assert.Equal(Bugun, d.Bitis);
        Assert.Equal(new DateTime(2026, 10, 18), d.OncekiBaslangic);
        Assert.Equal(new DateTime(2026, 10, 18), d.OncekiBitis);
        Assert.Equal(new DateTime(2026, 10, 6), d.GrafikBaslangic);
        Assert.Equal(Bugun, d.GrafikBitis);
        Assert.Equal("Bugün", d.Etiket);
        Assert.Equal("19 Ekim 2026", d.AralikMetni);
    }

    [Fact]
    public void Ozel_aralik_ters_girilirse_duzeltilir_ve_ayni_uzunlukta_onceki_donemle_kiyaslanir()
    {
        var d = YoneticiOzetDonemi.Hesapla(
            YoneticiOzetDonemTuru.Ozel, Bugun, new DateTime(2026, 10, 15), new DateTime(2026, 10, 11));

        Assert.Equal(new DateTime(2026, 10, 11), d.Baslangic);
        Assert.Equal(new DateTime(2026, 10, 15), d.Bitis);
        Assert.Equal(new DateTime(2026, 10, 6), d.OncekiBaslangic);
        Assert.Equal(new DateTime(2026, 10, 10), d.OncekiBitis);
        Assert.Equal("11 – 15 Ekim 2026", d.Etiket);
        Assert.Equal("Bu aralıkta", d.CumleGirisi);
    }

    [Fact]
    public void Ozel_aralik_gelecege_uzanirsa_bugune_kirpilir_ve_tamamen_gelecekteyse_bos_kalir()
    {
        var kismen = YoneticiOzetDonemi.Hesapla(
            YoneticiOzetDonemTuru.Ozel, Bugun, new DateTime(2026, 10, 15), new DateTime(2026, 10, 25));
        Assert.Equal(Bugun, kismen.EtkinBitis);
        Assert.Equal(5, kismen.GecenGunSayisi);
        Assert.Equal(new DateTime(2026, 10, 10), kismen.OncekiBaslangic);
        Assert.Equal(new DateTime(2026, 10, 14), kismen.OncekiBitis);

        var gelecek = YoneticiOzetDonemi.Hesapla(
            YoneticiOzetDonemTuru.Ozel, Bugun, new DateTime(2026, 11, 1), new DateTime(2026, 11, 5));
        Assert.Equal(0, gelecek.GecenGunSayisi);
        Assert.True(gelecek.EtkinBitis < gelecek.Baslangic);
    }

    [Fact]
    public void Ozel_aralik_en_fazla_iki_yil_tutulur()
    {
        var d = YoneticiOzetDonemi.Hesapla(
            YoneticiOzetDonemTuru.Ozel, Bugun, new DateTime(2000, 1, 1), new DateTime(2026, 10, 1));

        Assert.Equal(YoneticiOzetDonemi.AzamiOzelGun, d.ToplamGunSayisi);
        Assert.Equal(new DateTime(2026, 10, 1), d.Bitis);
        Assert.True(d.GrafikAylik);
    }

    [Fact]
    public void Ozel_aralik_verilmezse_son_otuz_gun_alinir()
    {
        var d = YoneticiOzetDonemi.Hesapla(YoneticiOzetDonemTuru.Ozel, Bugun);

        Assert.Equal(new DateTime(2026, 9, 20), d.Baslangic);
        Assert.Equal(Bugun, d.Bitis);
        Assert.Equal(30, d.ToplamGunSayisi);
    }

    [Theory]
    [InlineData("2026-10-19", "2026-10-19", "19 Ekim 2026")]
    [InlineData("2026-10-01", "2026-10-19", "1 – 19 Ekim 2026")]
    [InlineData("2026-09-28", "2026-10-04", "28 Eylül – 4 Ekim 2026")]
    [InlineData("2025-12-30", "2026-01-02", "30 Aralık 2025 – 2 Ocak 2026")]
    public void Aralik_etiketi_gun_ay_yil_ortakliklarina_gore_kisalir(string bas, string bit, string beklenen)
        => Assert.Equal(beklenen, YoneticiOzetDonemi.AralikEtiketi(DateTime.Parse(bas), DateTime.Parse(bit)));

    [Theory]
    [InlineData(0, 0, DegisimYonu.Yok, 0)]
    [InlineData(5, 0, DegisimYonu.Yeni, 0)]
    [InlineData(5, 5, DegisimYonu.Ayni, 0)]
    [InlineData(110, 100, DegisimYonu.Artti, 10)]
    [InlineData(90, 100, DegisimYonu.Azaldi, 10)]
    [InlineData(0, 40, DegisimYonu.Azaldi, 100)]
    public void Degisim_yuzdesi_yone_gore_hesaplanir(decimal simdi, decimal onceki, DegisimYonu yon, int yuzde)
    {
        var d = OzetDegisim.Hesapla(simdi, onceki);

        Assert.Equal(yon, d.Yon);
        Assert.Equal(yuzde, d.Yuzde);
    }

    [Fact]
    public void Degisim_yuvarlamayla_sifira_inerse_ayni_sayilir_ve_eksi_taban_mutlak_alinir()
    {
        Assert.Equal(DegisimYonu.Ayni, OzetDegisim.Hesapla(100.4m, 100m).Yon);

        var net = OzetDegisim.Hesapla(-50m, -100m); // zarar yarıya indi → iyileşme
        Assert.Equal(DegisimYonu.Artti, net.Yon);
        Assert.Equal(50, net.Yuzde);
    }

    [Theory]
    [InlineData(168000, "168.000 TL")]
    [InlineData(999999, "999.999 TL")]
    [InlineData(1240000, "1,24 Mn TL")]
    [InlineData(2500000000, "2,5 Mr TL")]
    [InlineData(-1500000, "-1,5 Mn TL")]
    public void Buyuk_tutarlar_kartlara_sigacak_sekilde_kisaltilir(decimal tutar, string beklenen)
        => Assert.Equal(beklenen, YoneticiOzetMetni.ParaKisalt(tutar));

    [Theory]
    [InlineData(52, "52")]
    [InlineData(52.5, "52,5")]
    [InlineData(1234, "1.234")]
    public void Sefer_sayisi_tam_sayiysa_ondaliksiz_gosterilir(decimal sayi, string beklenen)
        => Assert.Equal(beklenen, YoneticiOzetMetni.Sayi(sayi));

    [Fact]
    public void Cumleler_dolu_donemi_okunakli_anlatir()
    {
        var rapor = Rapor(new ServisOzeti { YapilanSefer = 52, CalisanAracSayisi = 31, KurumTahakkuku = 168000 },
            hakedis: new HakedisOzeti { UretilenAdet = 3, GelirAdet = 1, GelirTutari = 504000, OnaylananAdet = 2 },
            fatura: new FaturaOzeti { KesilenAdet = 41, KesilenTutar = 1240000, AlinanAdet = 12, AlinanTutar = 320000 },
            kasa: new KasaOzeti { TahsilatAdet = 5, TahsilatTutari = 350000, OdemeAdet = 4, OdemeTutari = 210000 },
            butce: new ButceOzeti { Planlanan = 1000, Odenen = 720, Bekleyen = 280, KalemSayisi = 4 });

        var cumleler = YoneticiOzetMetni.Olustur(rapor).ToArray();

        Assert.Equal(
            new[]
            {
                "Bu ay 52 sefer yapıldı (31 araç); kurum tahakkuku 168.000 TL.",
                "Bu ay 3 hakediş hazırlandı (504.000 TL gelir); 2 hakediş onaylandı.",
                "Bu ay 41 fatura kesildi (1.240.000 TL), 12 fatura alındı (320.000 TL).",
                "Bu ay 350.000 TL tahsilat, 210.000 TL ödeme gerçekleşti; net nakit +140.000 TL.",
                "Bütçe ödemeleri: 720 TL / 1.000 TL gerçekleşti (%72)."
            },
            cumleler);
        Assert.False(YoneticiOzetMetni.Bos(rapor));
    }

    [Fact]
    public void Cumleler_sefer_yoksa_planli_seferi_soyler_ve_hic_hareket_yoksa_bos_mesaj_verir()
    {
        var planli = YoneticiOzetMetni.Olustur(Rapor(new ServisOzeti { PlanliSefer = 36 }));
        Assert.Equal(new[] { "Bu ay henüz tamamlanan sefer yok; 36 sefer planlı." }, planli.ToArray());

        var bos = Rapor(new ServisOzeti(), hakedis: new HakedisOzeti(), fatura: new FaturaOzeti(), kasa: new KasaOzeti());
        Assert.Equal(new[] { YoneticiOzetMetni.BosMesaj }, YoneticiOzetMetni.Olustur(bos).ToArray());
        Assert.True(YoneticiOzetMetni.Bos(bos));
    }

    [Fact]
    public void Cumle_girisi_ozel_donemde_kisadir_cunku_aralik_baslikta_gorunur()
    {
        var d = YoneticiOzetDonemi.Hesapla(YoneticiOzetDonemTuru.Ozel, Bugun, new DateTime(2026, 10, 1), new DateTime(2026, 10, 15));
        var rapor = new YoneticiOzetRapor { Donem = d, Servis = new ServisOzeti { YapilanSefer = 12 } };

        Assert.Equal(new[] { "Bu aralıkta 12 sefer yapıldı." }, YoneticiOzetMetni.Olustur(rapor).ToArray());
    }

    [Fact]
    public void Tutari_sifir_olan_butce_kalemleri_cumle_uretmez_ve_bos_sayilir()
    {
        var sifir = new ButceOzeti { KalemSayisi = 1, Planlanan = 0, Odenen = 0, Bekleyen = 0 };
        Assert.True(sifir.Bos);
        Assert.True(YoneticiOzetMetni.Bos(Rapor(butce: sifir)));
        Assert.Equal(new[] { YoneticiOzetMetni.BosMesaj }, YoneticiOzetMetni.Olustur(Rapor(butce: sifir)).ToArray());

        var dolu = new ButceOzeti { KalemSayisi = 2, Planlanan = 500, Odenen = 100, Bekleyen = 400 };
        Assert.False(dolu.Bos);
        Assert.Equal(new[] { "Bütçe ödemeleri: 100 TL / 500 TL gerçekleşti (%20)." }, YoneticiOzetMetni.Olustur(Rapor(butce: dolu)).ToArray());

        Assert.True(new ButceOzeti().Bos);
    }

    [Theory]
    [InlineData(1000, 1148233, "%1'den az")]   // ödenen var ama %0,09: "%0" yanıltıcı olurdu
    [InlineData(0, 500, "%0")]
    [InlineData(250, 500, "%50")]
    [InlineData(600, 500, "%120")]            // planlanandan fazla ödeme
    public void Butce_gerceklesme_metni_kucuk_oranlari_sifir_gostermez(decimal odenen, decimal planlanan, string beklenen)
        => Assert.Equal(beklenen, new ButceOzeti { Planlanan = planlanan, Odenen = odenen, KalemSayisi = 1 }.GerceklesmeMetni);

    [Fact]
    public void Kapsam_hicbiri_hicbir_bolum_acmaz_tumu_hepsini_acar()
    {
        Assert.False(YoneticiOzetKapsami.Hicbiri.HerhangiBiri);
        Assert.True(YoneticiOzetKapsami.Tumu.HerhangiBiri);
        Assert.True((YoneticiOzetKapsami.Hicbiri with { Evrak = true }).HerhangiBiri);
    }

    private static YoneticiOzetRapor Rapor(
        ServisOzeti? servis = null,
        HakedisOzeti? hakedis = null,
        FaturaOzeti? fatura = null,
        KasaOzeti? kasa = null,
        ButceOzeti? butce = null)
        => new()
        {
            Donem = YoneticiOzetDonemi.Hesapla(YoneticiOzetDonemTuru.BuAy, Bugun),
            Servis = servis,
            Hakedis = hakedis,
            Fatura = fatura,
            Kasa = kasa,
            Butce = butce
        };
}

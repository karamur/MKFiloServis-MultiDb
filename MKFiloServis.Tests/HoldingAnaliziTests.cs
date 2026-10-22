using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

/// <summary>
/// Holding ekranlarının saf hesapları: dönem toplamı, firma özetleri, karşılaştırma, dikkat gerektirenler,
/// bütçe, vade ve grafik ölçeği. Veritabanı gerektirmez.
/// </summary>
public sealed class HoldingAnaliziTests
{
    private static HoldingVeri V(
        int firma, int yil, int ay, decimal gelir, decimal gider, decimal arac = 0m, decimal personel = 0m, decimal hakedis = 0m,
        int aktifArac = 0, int personelSayisi = 0, decimal? odenmemis = null, decimal? hakedisGider = null, string? ad = null) => new()
    {
        FirmaId = firma, FirmaKodu = "F" + firma, FirmaAdi = ad ?? (firma == 1 ? "Birinci Firma" : "İkinci Firma"),
        Yil = yil, Ay = ay, Kategori = "KARZARAR",
        ToplamGelir = gelir, ToplamGider = gider, Kar = gelir - gider,
        AraclarMaliyet = arac, PersonelMaliyet = personel, HakedisToplam = hakedis,
        AktifAracSayisi = aktifArac, PersonelSayisi = personelSayisi, OdenmemisFaturaToplam = odenmemis,
        JsonDetay = hakedisGider is null ? null : HoldingDetay.Yaz(new HoldingDetay { HakedisGider = hakedisGider.Value }),
        OlusturmaTarihi = new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc)
    };

    private static HoldingAnaliz Analiz(IEnumerable<HoldingVeri> yil, int ay, IEnumerable<HoldingVeri>? onceki = null, int yilNo = 2026)
        => HoldingAnalizi.Olustur(yil, onceki ?? [], yilNo, ay);

    // ---- dönem toplamı ----

    [Fact]
    public void Ay_secilince_yalniz_o_ayin_toplamini_alir_firmalari_gelire_gore_siralar_ve_onceki_ayla_karsilastirir()
    {
        var a = Analiz(
        [
            V(1, 2026, 4, 1000, 400, arac: 50, personel: 100, hakedis: 200, aktifArac: 5, personelSayisi: 4),
            V(2, 2026, 4, 3000, 3500),
            V(1, 2026, 3, 800, 300)
        ], 4);

        Assert.True(a.VeriVar);
        Assert.Equal(4000m, a.Toplam.Gelir);
        Assert.Equal(3900m, a.Toplam.Gider);
        Assert.Equal(100m, a.Toplam.Kar);
        Assert.Equal(2.5m, a.Toplam.Marj);
        Assert.Equal(2, a.Toplam.FirmaSayisi);
        Assert.Equal(2, a.Toplam.HareketliFirmaSayisi);
        Assert.Equal(5, a.Toplam.AktifArac);

        Assert.Equal(new[] { 2, 1 }, a.Firmalar.Select(f => f.FirmaId).ToArray());
        Assert.Equal(75m, a.Firmalar[0].GelirPayi);
        Assert.Equal(25m, a.Firmalar[1].GelirPayi);

        Assert.Equal("Mart 2026", a.OncekiEtiket);
        Assert.NotNull(a.Onceki);
        Assert.Equal(800m, a.Onceki!.Gelir);
        Assert.Equal(500m, a.Onceki.Kar);
        Assert.NotNull(a.Firmalar[1].Onceki);          // Birinci Firma'nın mart kaydı var
        Assert.Null(a.Firmalar[0].Onceki);             // İkinci Firma'nın yok
    }

    [Fact]
    public void Tum_yil_son_verili_aya_kadar_toplar_ve_gecen_yilin_ayni_donemiyle_karsilastirir()
    {
        var bu = new[] { V(1, 2026, 1, 100, 50), V(1, 2026, 2, 200, 100), V(1, 2026, 3, 300, 150) };
        var gecen = Enumerable.Range(1, 12).Select(ay => V(1, 2025, ay, 90, 40)).ToList();

        var a = Analiz(bu, 0, gecen);

        Assert.Equal(600m, a.Toplam.Gelir);
        Assert.Equal(300m, a.Toplam.Kar);
        Assert.Equal(3, a.SonVeriAyi);
        Assert.Equal("2025 yılının aynı dönemi", a.OncekiEtiket);
        Assert.Equal(270m, a.Onceki!.Gelir);           // yalnız ocak–mart
    }

    [Fact]
    public void Ocak_ayinda_onceki_donem_gecen_yilin_aralik_ayidir()
    {
        var a = Analiz([V(1, 2026, 1, 100, 50)], 1, [V(1, 2025, 12, 70, 20), V(1, 2025, 11, 999, 0)]);

        Assert.Equal("Aralık 2025", a.OncekiEtiket);
        Assert.Equal(70m, a.Onceki!.Gelir);
    }

    [Fact]
    public void Onceki_donem_verisi_yoksa_karsilastirma_null_olur()
    {
        var a = Analiz([V(1, 2026, 4, 100, 50)], 4);

        Assert.Null(a.Onceki);
        Assert.Equal("Mart 2026", a.OncekiEtiket);
    }

    [Fact]
    public void Arac_ve_personel_adedi_aylara_bolunmez_son_aydaki_goruntuden_alinir()
    {
        var a = Analiz(
        [
            V(1, 2026, 1, 100, 50, aktifArac: 10, personelSayisi: 4),
            V(1, 2026, 2, 100, 50, aktifArac: 12, personelSayisi: 5)
        ], 0);

        Assert.Equal(12, a.Toplam.AktifArac);
        Assert.Equal(5, a.Toplam.Personel);
        Assert.Equal(200m, a.Toplam.Gelir);
    }

    [Fact]
    public void Aylik_seri_on_iki_ay_verir_verisiz_ayi_isaretler()
    {
        var a = Analiz([V(1, 2026, 1, 100, 40), V(2, 2026, 1, 50, 10), V(1, 2026, 3, 70, 80)], 3);

        Assert.Equal(12, a.Aylar.Count);
        Assert.Equal(150m, a.Aylar[0].Gelir);
        Assert.Equal(100m, a.Aylar[0].Kar);
        Assert.True(a.Aylar[0].VeriVar);
        Assert.False(a.Aylar[1].VeriVar);
        Assert.Equal(0m, a.Aylar[1].Gelir);
        Assert.Equal(-10m, a.Aylar[2].Kar);
        Assert.Equal(new[] { 1, 3 }, a.VeriliAylar.ToArray());
        Assert.Equal(new[] { 2 }, HoldingAnalizi.EksikAylar(a).ToArray());
    }

    [Fact]
    public void Odeme_orani_odenmemis_tutar_toplanmadiysa_bilinmez_aksi_halde_hesaplanir()
    {
        var bilinmez = Analiz([V(1, 2026, 4, 100, 1000)], 4);
        Assert.Null(bilinmez.Toplam.OdenmeOrani);

        var bilinen = Analiz([V(1, 2026, 4, 100, 1000, odenmemis: 250)], 4);
        Assert.Equal(75m, bilinen.Toplam.OdenmeOrani);

        var asiri = Analiz([V(1, 2026, 4, 100, 1000, odenmemis: 1500)], 4);
        Assert.Equal(0m, asiri.Toplam.OdenmeOrani);
    }

    [Fact]
    public void Hakedis_gideri_detaydan_okunur_firma_aylik_serisi_eksik_ayi_bos_birakir()
    {
        var a = Analiz([V(1, 2026, 1, 100, 50, hakedis: 80, hakedisGider: 30), V(1, 2026, 3, 70, 80)], 0);

        Assert.Equal(80m, a.Toplam.Hakedis);
        Assert.Equal(30m, a.Toplam.HakedisGider);

        var seri = a.FirmaAylik(1, v => v.ToplamGelir);
        Assert.Equal(12, seri.Count);
        Assert.Equal(100m, seri[0]);
        Assert.Null(seri[1]);
        Assert.Equal(70m, seri[2]);
    }

    [Fact]
    public void Firma_renk_sirasi_secilen_aydan_bagimsiz_firma_numarasina_gore_sabittir()
    {
        var satirlar = new[] { V(5, 2026, 3, 10, 5), V(2, 2026, 3, 10, 5), V(9, 2026, 4, 10, 5) };

        var nisan = Analiz(satirlar, 4);

        Assert.Equal(2, nisan.Firmalar.Single(f => f.FirmaId == 9).Sira);
        Assert.Equal(0, nisan.Firmalar.Single(f => f.FirmaId == 2).Sira);
        Assert.Equal(1, nisan.Firmalar.Single(f => f.FirmaId == 5).Sira);
        Assert.False(nisan.Firmalar.Single(f => f.FirmaId == 2).VeriVar); // nisanda kaydı yok ama listede
    }

    [Fact]
    public void Bos_veri_gecerli_bir_analiz_uretir()
    {
        var a = Analiz([], 4);

        Assert.False(a.VeriVar);
        Assert.Empty(a.Firmalar);
        Assert.Equal(12, a.Aylar.Count);
        Assert.Null(a.SonVeriAyi);
        Assert.True(a.Toplam.Bos);
    }

    [Fact]
    public void Diger_kategorilerin_satirlari_kar_zarar_analizine_girmez()
    {
        var butce = V(1, 2026, 4, 999, 0);
        butce.Kategori = "BUTCE";

        var a = Analiz([butce, V(1, 2026, 4, 100, 40)], 4);

        Assert.Equal(100m, a.Toplam.Gelir);
    }

    // ---- yönetici cümleleri ----

    [Fact]
    public void Ozet_cumleleri_donemi_marji_degisimi_ve_en_karli_firmayi_anlatir()
    {
        var a = Analiz(
        [
            V(1, 2026, 4, 1000, 400, arac: 50, personel: 100, aktifArac: 5, personelSayisi: 4),
            V(2, 2026, 4, 3000, 3500),
            V(1, 2026, 3, 500, 400)
        ], 4);

        var cumleler = HoldingAnalizi.OzetCumleleri(a);

        Assert.Contains("Nisan 2026: 2 firmanın toplam geliri 4.000 TL", cumleler[0]);
        Assert.Contains("net kâr 100 TL (kâr marjı %2,5)", cumleler[0]);
        Assert.Contains(cumleler, c => c.Contains("Mart 2026 ile karşılaştırınca") && c.Contains("Gelir %700 arttı"));
        Assert.Contains(cumleler, c => c.Contains("En yüksek kâr: Birinci Firma (600 TL)"));
        Assert.Contains(cumleler, c => c.Contains("5 aktif araç, 4 personel"));
    }

    [Fact]
    public void Ozet_cumleleri_veri_yoksa_tek_cumle_dondurur_zararda_net_zarar_der()
    {
        Assert.Equal(
            new[] { "Nisan 2026 için henüz holding verisi toplanmamış." },
            HoldingAnalizi.OzetCumleleri(Analiz([], 4)).ToArray());

        var zarar = HoldingAnalizi.OzetCumleleri(Analiz([V(1, 2026, 4, 100, 250)], 4));
        Assert.Contains("net zarar 150 TL", zarar[0]);
        Assert.DoesNotContain("kâr marjı", zarar[0]);       // zararda "kâr marjı %-150" gibi anlamsız bir oran yazılmaz
    }

    // ---- dikkat gerektirenler ----

    [Fact]
    public void Bulgular_zarari_kritik_dusuk_marji_uyari_olarak_siralar()
    {
        var a = Analiz([V(1, 2026, 4, 1000, 950), V(2, 2026, 4, 500, 800)], 4);

        var b = HoldingAnalizi.Bulgular(a);

        Assert.Equal(HoldingBulguOnemi.Kritik, b[0].Onem);
        Assert.Contains(b, x => x.Onem == HoldingBulguOnemi.Kritik && x.Baslik == "Holding zararda");
        Assert.Contains(b, x => x.Onem == HoldingBulguOnemi.Kritik && x.Baslik == "İkinci Firma: zarar");
        Assert.Contains(b, x => x.Onem == HoldingBulguOnemi.Uyari && x.Baslik == "Birinci Firma: düşük kâr marjı");
        Assert.Equal(b.Select(x => x.Onem).OrderBy(o => o), b.Select(x => x.Onem));  // önem sırasında
    }

    [Fact]
    public void Bulgular_gelir_dususunu_gider_artisini_uyarir_kar_artisini_iyi_haber_sayar()
    {
        var dusus = HoldingAnalizi.Bulgular(Analiz([V(1, 2026, 3, 1000, 100), V(1, 2026, 4, 700, 100)], 4));
        Assert.Contains(dusus, x => x.Baslik == "Gelir düştü" && x.Metin.Contains("%30 azaldı"));

        var gider = HoldingAnalizi.Bulgular(Analiz([V(1, 2026, 3, 1000, 500), V(1, 2026, 4, 1000, 700)], 4));
        Assert.Contains(gider, x => x.Baslik == "Gider hızlı arttı" && x.Metin.Contains("%40 arttı"));

        var iyi = HoldingAnalizi.Bulgular(Analiz([V(1, 2026, 3, 1000, 900), V(1, 2026, 4, 1000, 800)], 4));
        Assert.Contains(iyi, x => x.Onem == HoldingBulguOnemi.Iyi && x.Baslik == "Kâr arttı");
    }

    [Fact]
    public void Bulgular_gider_gelirle_birlikte_artiyorsa_gider_uyarisi_vermez()
    {
        var b = HoldingAnalizi.Bulgular(Analiz([V(1, 2026, 3, 1000, 500), V(1, 2026, 4, 2000, 700)], 4));

        Assert.DoesNotContain(b, x => x.Baslik == "Gider hızlı arttı");
    }

    [Fact]
    public void Bulgular_kaydi_olmayan_ve_verisi_toplanmamis_firmayi_bilgi_olarak_bildirir()
    {
        var a = Analiz(
        [
            V(1, 2026, 4, 1000, 400),
            V(2, 2026, 4, 0, 0),                                   // kaydı yok (hepsi 0)
            V(3, 2026, 3, 500, 100, ad: "Üçüncü Firma")             // nisanda hiç görüntüsü yok
        ], 4);

        var b = HoldingAnalizi.Bulgular(a);

        Assert.Contains(b, x => x.Onem == HoldingBulguOnemi.Bilgi && x.Baslik == "İkinci Firma: kayıt yok");
        Assert.Contains(b, x => x.Onem == HoldingBulguOnemi.Bilgi && x.Baslik == "Üçüncü Firma: veri toplanmamış");
    }

    [Fact]
    public void Bulgular_toplanmamis_ayi_ve_odenmemis_gideri_bildirir()
    {
        var eksik = HoldingAnalizi.Bulgular(Analiz([V(1, 2026, 1, 100, 10), V(1, 2026, 3, 100, 10)], 3));
        Assert.Contains(eksik, x => x.Baslik == "Toplanmamış aylar var" && x.Metin.StartsWith("Şubat ayının"));

        var odenmemis = HoldingAnalizi.Bulgular(Analiz([V(1, 2026, 4, 5000, 1000, odenmemis: 800)], 4));
        Assert.Contains(odenmemis, x => x.Baslik == "Giderlerin büyük kısmı ödenmemiş" && x.Link == "/holding/odemeler");
    }

    [Fact]
    public void Bulgular_veri_yoksa_tek_bilgi_verir_en_fazla_sekiz_madde_dondurur()
    {
        var bos = HoldingAnalizi.Bulgular(Analiz([], 4));
        Assert.Single(bos);
        Assert.Equal("Bu dönem için veri yok", bos[0].Baslik);

        var cok = Analiz(Enumerable.Range(1, 12).Select(i => V(i, 2026, 4, 100, 500, ad: "Zarar " + i)), 4);
        Assert.Equal(8, HoldingAnalizi.Bulgular(cok).Count);
    }

    [Fact]
    public void Bulgular_butce_asimini_ve_gecikmis_odemeyi_kritik_olarak_ekler()
    {
        var a = Analiz([V(1, 2026, 4, 1000, 100)], 4);
        var butce = HoldingButceAnalizi.Olustur([Butce(1, 2026, 4, 1000, 400, 1200)], 2026, 4);
        var vade = HoldingVadeAnalizi.Olustur(
        [
            new HoldingVadeKalemi(1, "Birinci Firma", true, new DateTime(2026, 10, 10), 700m),
            new HoldingVadeKalemi(1, "Birinci Firma", true, new DateTime(2026, 10, 23), 300m),
            new HoldingVadeKalemi(1, "Birinci Firma", false, new DateTime(2026, 10, 1), 900m)
        ], new DateTime(2026, 10, 21));

        var b = HoldingAnalizi.Bulgular(a, butce, vade);

        Assert.Contains(b, x => x.Onem == HoldingBulguOnemi.Kritik && x.Baslik == "Bütçe aşıldı");
        Assert.Contains(b, x => x.Onem == HoldingBulguOnemi.Kritik && x.Baslik == "Birinci Firma: bütçe aşımı");
        Assert.Contains(b, x => x.Onem == HoldingBulguOnemi.Kritik && x.Baslik == "Vadesi geçmiş ödemeler" && x.Metin.Contains("700 TL"));
        Assert.Contains(b, x => x.Baslik == "Yaklaşan ödemeler" && x.Metin.Contains("300 TL"));
        Assert.Contains(b, x => x.Onem == HoldingBulguOnemi.Uyari && x.Baslik == "Vadesi geçmiş alacaklar");
    }

    // ---- bütçe ----

    private static HoldingVeri Butce(int firma, int yil, int ay, decimal hedef, decimal aylik, decimal kumulatif) => new()
    {
        FirmaId = firma, FirmaKodu = "F" + firma, FirmaAdi = firma == 1 ? "Birinci Firma" : "İkinci Firma", Yil = yil, Ay = ay,
        Kategori = "BUTCE", ButceHedef = hedef, ButceGerceklesen = aylik, ToplamGelir = kumulatif
    };

    [Fact]
    public void Butce_secilen_aya_kadar_kumulatifi_alir_toplanmamis_ayi_bos_birakir()
    {
        var satirlar = new[]
        {
            Butce(1, 2026, 1, 12000, 500, 500),
            Butce(1, 2026, 2, 12000, 600, 1100),
            Butce(1, 2026, 3, 12000, 400, 1500)
        };

        var nisan = HoldingButceAnalizi.Olustur(satirlar, 2026, 4);

        var f = Assert.Single(nisan.Firmalar);
        Assert.Equal(12000m, f.Hedef);
        Assert.Equal(1500m, f.Gerceklesen);            // nisan toplanmamış: en yakın önceki ay (mart)
        Assert.Equal(12.5m, f.Kullanim);
        Assert.Equal(10500m, f.Kalan);
        Assert.Equal(HoldingButceDurumu.Normal, f.Durum);
        Assert.Equal(500m, f.Aylik[0]);
        Assert.Equal(400m, f.Aylik[2]);
        Assert.Null(f.Aylik[3]);
        Assert.Equal(1000m, nisan.AylikBeklenen);
        Assert.Equal(33.3m, Math.Round(nisan.BeklenenYuzde, 1));

        var subat = HoldingButceAnalizi.Olustur(satirlar, 2026, 2);
        Assert.Equal(1100m, subat.Gerceklesen);
    }

    [Theory]
    [InlineData(0, 500, 4, HoldingButceDurumu.HedefYok)]
    [InlineData(1000, 100, 6, HoldingButceDurumu.Normal)]      // %10, beklenen %50
    [InlineData(1000, 500, 6, HoldingButceDurumu.Normal)]      // tam beklenen düzey
    [InlineData(1000, 610, 6, HoldingButceDurumu.Hizli)]       // beklenenin 10 puandan fazla üstü
    [InlineData(1000, 1000, 6, HoldingButceDurumu.Hizli)]      // tam hedef: henüz aşılmadı
    [InlineData(1000, 1001, 6, HoldingButceDurumu.Asim)]
    public void Butce_durumu_hedefi_gecen_sureye_gore_degerlendirir(double hedef, double gerceklesen, int ay, HoldingButceDurumu beklenen)
        => Assert.Equal(beklenen, HoldingButceAnalizi.DurumBul((decimal)hedef, (decimal)gerceklesen, ay / 12m * 100m));

    [Fact]
    public void Butce_toplami_firmalari_birlestirir_ve_yalniz_sonraki_aylari_olan_firmayi_verisiz_sayar()
    {
        var a = HoldingButceAnalizi.Olustur(
        [
            Butce(1, 2026, 1, 1000, 100, 100),
            Butce(2, 2026, 4, 2000, 300, 700)
        ], 2026, 2);

        Assert.Equal(3000m, a.Hedef);
        Assert.Equal(100m, a.Gerceklesen);
        var iki = a.Firmalar.Single(f => f.FirmaId == 2);
        Assert.False(iki.VeriVar);
        Assert.Equal(2000m, iki.Hedef);
        Assert.Equal(0m, iki.Gerceklesen);
        Assert.Equal(100m, a.Aylik[0]);
        Assert.Equal(300m, a.Aylik[3]);
        Assert.Null(a.Aylik[1]);
    }

    // ---- vade ----

    [Fact]
    public void Vade_kovalari_ve_takvim_bugune_gore_dagitilir()
    {
        var bugun = new DateTime(2026, 10, 21);
        HoldingVadeKalemi B(DateTime? vade, decimal kalan) => new(1, "Birinci Firma", true, vade, kalan);
        var a = HoldingVadeAnalizi.Olustur(
        [
            B(new DateTime(2026, 10, 10), 100),            // gecikmiş
            B(new DateTime(2026, 10, 21), 50),             // bugün: 7 gün içinde
            B(new DateTime(2026, 10, 28), 30),             // tam 7. gün
            B(new DateTime(2026, 10, 29), 20),             // 8. gün
            B(new DateTime(2026, 11, 20), 10),             // 30. gün
            B(new DateTime(2026, 11, 21), 5),              // 31. gün
            B(new DateTime(2027, 1, 19), 7),               // 90. gün
            B(new DateTime(2027, 1, 20), 3),               // 91. gün
            B(null, 11),                                    // vadesiz
            new HoldingVadeKalemi(1, "Birinci Firma", false, new DateTime(2026, 10, 15), 200),
            new HoldingVadeKalemi(2, "İkinci Firma", true, new DateTime(2026, 10, 25), 50),
            new HoldingVadeKalemi(2, "İkinci Firma", true, new DateTime(2026, 10, 25), 0)   // kalan 0: yok sayılır
        ], bugun);

        Assert.Equal(286m, a.Borc.Toplam);
        Assert.Equal(10, a.Borc.Adet);
        Assert.Equal(100m, a.Borc.Gecikmis);
        Assert.Equal(1, a.Borc.GecikmisAdet);
        Assert.Equal(130m, a.Borc.YediGunIcinde);        // 50 + 30 + ikinci firmanın 50'si
        Assert.Equal(30m, a.Borc.Kova(HoldingVadeKovasi.OtuzGun).Tutar);
        Assert.Equal(12m, a.Borc.Kova(HoldingVadeKovasi.DoksanGun).Tutar);
        Assert.Equal(3m, a.Borc.Kova(HoldingVadeKovasi.DahaSonra).Tutar);
        Assert.Equal(11m, a.Borc.Kova(HoldingVadeKovasi.Vadesiz).Tutar);
        Assert.Equal(200m, a.Alacak.Gecikmis);
        Assert.Equal(-86m, a.Net);                       // alacak 200 − borç 286

        Assert.Equal(["Birinci Firma", "İkinci Firma"], a.Firmalar.Select(f => f.FirmaAdi).ToArray());

        Assert.Equal(["Gecikmiş", "Eki", "Kas", "Ara", "Oca", "Şub", "Mar", "Sonrası"], a.Takvim.Select(t => t.Etiket).ToArray());
        Assert.True(a.Takvim[0].Gecikmis);
        Assert.Equal(100m, a.Takvim[0].Borc);
        Assert.Equal(200m, a.Takvim[0].Alacak);
        Assert.Equal(150m, a.Takvim[1].Borc);            // ekim: 50 + 30 + 20 + ikinci firmanın 50'si
        Assert.Equal(15m, a.Takvim[2].Borc);
        Assert.Equal(0m, a.Takvim[3].Borc);
        Assert.Equal(10m, a.Takvim[4].Borc);
        Assert.Equal(0m, a.Takvim[7].Borc);
    }

    [Fact]
    public void Vade_bos_liste_bos_ozet_verir()
    {
        var a = HoldingVadeAnalizi.Olustur([], new DateTime(2026, 10, 21));

        Assert.True(a.Bos);
        Assert.Equal(8, a.Takvim.Count);
        Assert.Equal(0m, a.Borc.Toplam);
    }

    // ---- grafik ölçeği ----

    [Theory]
    [InlineData(1, AylikBirim.Adet)]
    [InlineData(3, AylikBirim.Adet)]
    [InlineData(7, AylikBirim.Adet)]
    [InlineData(52, AylikBirim.Adet)]
    [InlineData(1234, AylikBirim.Para)]
    [InlineData(2300, AylikBirim.Para)]
    [InlineData(1000000, AylikBirim.Para)]
    [InlineData(4567890123, AylikBirim.Para)]
    public void Olcek_yalniz_pozitif_degerlerde_aylik_grafik_olcegiyle_ayni_ust_siniri_verir(double azami, AylikBirim birim)
    {
        var yeni = HoldingOlcek.Hesapla(0m, (decimal)azami, birim);
        var eski = GrafikOlcegi.Hesapla((decimal)azami, birim);

        Assert.Equal(eski.Ust, yeni.Ust);
        Assert.Equal(eski.Adim, yeni.Adim);
        Assert.Equal(0m, yeni.Alt);
        Assert.Equal(eski.AralikSayisi + 1, yeni.Isaretler.Count);
    }

    [Fact]
    public void Olcek_negatif_degerleri_kapsar_ve_sifir_cizgisini_dogru_yerlestirir()
    {
        var o = HoldingOlcek.Hesapla(-300m, 1000m, AylikBirim.Para);

        Assert.Equal(-500m, o.Alt);
        Assert.Equal(1000m, o.Ust);
        Assert.Equal(500m, o.Adim);
        Assert.Equal(new[] { -500m, 0m, 500m, 1000m }, o.Isaretler.ToArray());
        Assert.Equal(100m, o.Konum(1000m));
        Assert.Equal(0m, o.Konum(-500m));
        Assert.Equal(33.3m, Math.Round(o.Sifir, 1));
    }

    [Fact]
    public void Olcek_yalniz_negatif_degerlerde_ust_sinir_sifirdir_ve_bos_veri_gecerli_olcek_verir()
    {
        var negatif = HoldingOlcek.Hesapla(-1200m, -100m, AylikBirim.Para);
        Assert.Equal(0m, negatif.Ust);
        Assert.Equal(-1250m, negatif.Alt);
        Assert.Equal(250m, negatif.Adim);
        Assert.Equal(100m, negatif.Sifir);

        var bos = HoldingOlcek.Hesapla(0m, 0m, AylikBirim.Para);
        Assert.Equal(new[] { 0m, 1m }, bos.Isaretler.ToArray());
    }

    // ---- rozet, renk, biçim ----

    [Fact]
    public void Rozet_artisi_ve_azalisi_anlama_gore_iyi_ya_da_kotu_renklendirir()
    {
        var gelirArtti = HoldingRozet.Degisim(OzetDegisim.Hesapla(120m, 100m), yuksekIyi: true);
        Assert.Equal(HoldingRozetYonu.Yukari, gelirArtti.Yon);
        Assert.Equal("%20", gelirArtti.Metin);
        Assert.True(gelirArtti.Iyi);

        var maliyetAzaldi = HoldingRozet.Degisim(OzetDegisim.Hesapla(80m, 100m), yuksekIyi: false);
        Assert.Equal(HoldingRozetYonu.Asagi, maliyetAzaldi.Yon);
        Assert.True(maliyetAzaldi.Iyi);

        var gelirDustu = HoldingRozet.Degisim(OzetDegisim.Hesapla(80m, 100m), yuksekIyi: true);
        Assert.False(gelirDustu.Iyi);

        Assert.False(HoldingRozet.Degisim(OzetDegisim.Hesapla(50m, 0m), true).Var);    // önceki dönemde değer yok
        Assert.False(HoldingRozet.Degisim(OzetDegisim.Hesapla(0m, 0m), true).Var);
        Assert.Equal("=", HoldingRozet.Degisim(OzetDegisim.Hesapla(100m, 100m), true).Metin);
    }

    [Fact]
    public void Puan_rozeti_yuzde_puani_farkini_gosterir_ihmal_edilebilir_farki_esit_sayar()
    {
        var artti = HoldingRozet.Puan(2.34m, yuksekIyi: true);
        Assert.Equal("2,3 puan", artti.Metin);
        Assert.Equal(HoldingRozetYonu.Yukari, artti.Yon);

        Assert.Equal(HoldingRozetYonu.Ayni, HoldingRozet.Puan(-0.04m, true).Yon);
        Assert.Equal(HoldingRozetYonu.Asagi, HoldingRozet.Puan(-3m, true).Yon);
    }

    [Fact]
    public void Renkler_anlamli_belirtecleri_ve_firma_paletini_cozer()
    {
        Assert.Equal("#10b981", HoldingRenk.Hex(HoldingRenk.Gelir));
        Assert.Equal("#ef4444", HoldingRenk.Hex(HoldingRenk.Gider));
        Assert.Equal("#94a3b8", HoldingRenk.Hex("bilinmeyen"));
        Assert.Equal("#94a3b8", HoldingRenk.Hex(null));

        Assert.Equal("f0", HoldingRenk.Firma(0));
        Assert.Equal(HoldingRenk.Hex("f0"), HoldingRenk.Hex(HoldingRenk.Firma(10)));  // palet başa döner
        Assert.NotEqual(HoldingRenk.Hex("f0"), HoldingRenk.Hex("f1"));
    }

    [Fact]
    public void Bicimler_turkce_ay_adlarini_ve_sabit_kulturlu_css_sayilarini_verir()
    {
        Assert.Equal("Nisan 2026", HoldingBicim.Donem(2026, 4));
        Assert.Equal("2026 yılı", HoldingBicim.Donem(2026, 0));
        Assert.Equal("Tüm Yıl", HoldingBicim.AyAdi(0));
        Assert.Equal("Ağu", HoldingBicim.AyKisa(8));
        Assert.Equal("%43,3", HoldingBicim.Yuzde(43.33m));
        Assert.Equal("-%4,4", HoldingBicim.Yuzde(-4.44m));
        Assert.Equal("%0,0", HoldingBicim.Yuzde(-0.04m));       // yuvarlayınca sıfır: eksi işareti yazılmaz
        Assert.Equal("%95", HoldingBicim.Yuzde(94.8m, 0));
        Assert.Equal("12.5", HoldingBicim.Css(12.5m));      // virgül olsa CSS/SVG bozulurdu
        Assert.Equal("13,17 Mn TL", HoldingBicim.ParaKisa(13_166_594.37m));
        Assert.Equal("250.000 TL", HoldingBicim.ParaKisa(250_000m));
    }
}

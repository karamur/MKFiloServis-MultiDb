using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

/// <summary>
/// Yönetici özeti servisi: aynı tohum veriyle her bölümün sayıları, dönem/saat dilimi sınırları,
/// firma süzme, yetki kapsamı ve bölüm hatası dayanıklılığı.
/// </summary>
public sealed class YoneticiOzetSqliteTests
{
    // 19 Ekim 2026 Pazartesi (iş günü)
    private static readonly DateTime Bugun = new(2026, 10, 19);
    private static readonly DateTime Eski = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime Utc(int y, int m, int d, int h = 9, int min = 0) => new(y, m, d, h, min, 0, DateTimeKind.Utc);

    private static YoneticiOzetIstegi Istek(
        YoneticiOzetDonemTuru tur = YoneticiOzetDonemTuru.BuAy,
        YoneticiOzetKapsami? kapsam = null,
        int? kritikBelge = null)
        => new() { DonemTuru = tur, Kapsam = kapsam ?? YoneticiOzetKapsami.Tumu, Bugun = Bugun, KritikBelgeSayisi = kritikBelge };

    private static YoneticiOzetService Servis(PayrollWriteSqliteTests.Fixture f)
        => new(f.Factory, f.ActiveFirm, NullLogger<YoneticiOzetService>.Instance);

    [Fact]
    public async Task Servis_yapilan_planli_yapilamayan_ve_onceki_donemi_ayirir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var s = (await Servis(f).GetRaporAsync(Istek())).Servis!;

        Assert.Equal(4m, s.YapilanSefer);              // 2 + 1 + 1 (arıza da yapılan sayılır; gitmedi sayılmaz)
        Assert.Equal(2, s.CalisanAracSayisi);
        Assert.Equal(3, s.CalisilanGunSayisi);         // 1, 2 ve 6 Ekim
        Assert.Equal(1, s.YapilamayanKayit);
        Assert.Equal(1, s.ArizaKaydi);
        Assert.Equal(3m, s.PlanliSefer);               // 25 Ekim (bugünden sonra)
        Assert.Equal(400m, s.KurumTahakkuku);
        Assert.Equal(50m, s.TaseronTahakkuku);
        Assert.Equal(350m, s.BrutFark);
        Assert.Equal(3, s.KayitSayisi);
        Assert.Equal(1, s.OnayliKayitSayisi);
        Assert.Equal(33, s.OnayYuzdesi);
        Assert.Equal(4m, s.OncekiYapilanSefer);        // 1 Eylül; 25 Eylül karşılaştırma penceresinin (1–19 Eylül) dışında
        Assert.Equal(400m, s.OncekiKurumTahakkuku);
        Assert.False(s.Bos);

        Assert.False(s.Seri.Aylik);
        Assert.Equal(31, s.Seri.Noktalar.Count);
        Assert.Equal(2m, s.Seri.Noktalar[0].Deger1);   // 1 Ekim
        Assert.Equal(0m, s.Seri.Noktalar[4].Deger1);   // 5 Ekim: gitmedi
        Assert.Equal(3m, s.Seri.Noktalar[24].Deger2);  // 25 Ekim: planlı
        Assert.True(s.Seri.Noktalar[24].Gelecek);
        Assert.False(s.Seri.Noktalar[0].Gelecek);
    }

    [Fact]
    public async Task Servis_bugun_doneminde_grafik_son_on_dort_gunu_gosterir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var s = (await Servis(f).GetRaporAsync(Istek(YoneticiOzetDonemTuru.Bugun))).Servis!;

        Assert.True(s.Bos);                            // 19 Ekim'de kayıt yok
        Assert.Equal(0m, s.YapilanSefer);
        Assert.Equal(14, s.Seri.Noktalar.Count);       // 6–19 Ekim
        Assert.Equal(new DateTime(2026, 10, 6), s.Seri.Noktalar[0].Baslangic);
        Assert.Equal(1m, s.Seri.Noktalar[0].Deger1);   // 6 Ekim arıza kaydı
    }

    [Fact]
    public async Task Servis_bu_yil_aylik_seri_verir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var s = (await Servis(f).GetRaporAsync(Istek(YoneticiOzetDonemTuru.BuYil))).Servis!;

        Assert.True(s.Seri.Aylik);
        Assert.Equal(12, s.Seri.Noktalar.Count);
        Assert.Equal(9m, s.Seri.Noktalar[8].Deger1);   // Eylül: 4 + 5
        Assert.Equal(4m, s.Seri.Noktalar[9].Deger1);   // Ekim yapılan
        Assert.Equal(3m, s.Seri.Noktalar[9].Deger2);   // Ekim planlı
        Assert.False(s.Seri.Noktalar[9].Gelecek);      // içinde bulunulan ay "gelecek" değil
        Assert.True(s.Seri.Noktalar[10].Gelecek);      // Kasım
        Assert.Equal(13m, s.YapilanSefer);             // yıl başından bugüne: 9 + 4
    }

    [Fact]
    public async Task Servis_grafik_etiketi_pencere_iki_aya_yayilirsa_gun_ay_icerir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        // Tek ay: yalnız gün numarası.
        var tekAy = (await Servis(f).GetRaporAsync(Istek())).Servis!;
        Assert.Equal("1", tekAy.Seri.Noktalar[0].Etiket);
        Assert.Equal("31", tekAy.Seri.Noktalar[30].Etiket);

        // 25 Eylül – 5 Ekim: gün numarası belirsiz kalacağından gün.ay.
        var istek = new YoneticiOzetIstegi
        {
            DonemTuru = YoneticiOzetDonemTuru.Ozel,
            OzelBaslangic = new DateTime(2026, 9, 25),
            OzelBitis = new DateTime(2026, 10, 5),
            Bugun = Bugun
        };
        var ikiAy = (await Servis(f).GetRaporAsync(istek)).Servis!;
        Assert.Equal(11, ikiAy.Seri.Noktalar.Count);
        Assert.Equal("25.9", ikiAy.Seri.Noktalar[0].Etiket);
        Assert.Equal("1.10", ikiAy.Seri.Noktalar[6].Etiket);
        Assert.Equal("5.10", ikiAy.Seri.Noktalar[10].Etiket);
    }

    [Fact]
    public async Task Hakedis_uretilen_gelir_gider_ic_ve_onaylananlari_ayirir_iptali_saymaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var h = (await Servis(f).GetRaporAsync(Istek())).Hakedis!;

        Assert.Equal(3, h.UretilenAdet);               // taslak kurum + onaylı tedarikçi + taslak araç (iptal hariç)
        Assert.Equal(1, h.GelirAdet);
        Assert.Equal(1200m, h.GelirTutari);
        Assert.Equal(1, h.GiderAdet);
        Assert.Equal(480m, h.GiderTutari);
        Assert.Equal(1, h.IcAdet);
        Assert.Equal(100m, h.IcTutar);
        Assert.Equal(720m, h.NetTutar);
        Assert.Equal(1, h.OnaylananAdet);              // onay tarihi 17 Ekim
        Assert.Equal(2, h.TaslakAdet);
        Assert.Equal(1, h.OnayliAdet);
        Assert.Equal(0, h.FaturaliAdet);
        Assert.Equal(0, h.TamamlananAdet);
        Assert.Equal(1, h.OncekiUretilenAdet);         // Eylül'de üretilen
        Assert.Equal(600m, h.OncekiGelirTutari);
    }

    [Fact]
    public async Task Fatura_kesilen_alinan_iade_ve_tahsilat_oranini_hesaplar_iptali_saymaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var fa = (await Servis(f).GetRaporAsync(Istek())).Fatura!;

        Assert.Equal(2, fa.KesilenAdet);
        Assert.Equal(2000m, fa.KesilenTutar);
        Assert.Equal(1400m, fa.KesilenTahsilEdilen);
        Assert.Equal(70, fa.TahsilatYuzdesi);
        Assert.Equal(2, fa.AlinanAdet);
        Assert.Equal(800m, fa.AlinanTutar);
        Assert.Equal(0, fa.OdemeYuzdesi);
        Assert.Equal(1, fa.IadeAdet);
        Assert.Equal(1, fa.OncekiKesilenAdet);
        Assert.Equal(1000m, fa.OncekiKesilenTutar);
    }

    [Fact]
    public async Task Kasa_hesaplar_arasi_aktarimi_ve_personel_cebinden_harcamayi_nakit_saymaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var k = (await Servis(f).GetRaporAsync(Istek())).Kasa!;

        Assert.Equal(1, k.TahsilatAdet);
        Assert.Equal(1000m, k.TahsilatTutari);
        Assert.Equal(1, k.OdemeAdet);                  // 20 Ekim'deki gelecek tarihli ödeme bugüne kadar sayılmaz
        Assert.Equal(400m, k.OdemeTutari);
        Assert.Equal(600m, k.NetNakit);
        Assert.Equal(500m, k.OncekiTahsilat);
        Assert.Equal(0m, k.OncekiOdeme);
        Assert.Equal(1000m, k.Seri.Noktalar[2].Deger1); // 3 Ekim tahsilat
        Assert.Equal(400m, k.Seri.Noktalar[3].Deger2);  // 4 Ekim ödeme
        Assert.Equal(0m, k.Seri.Noktalar[4].Deger1);    // 5 Ekim: mahsup nakit sayılmaz
        Assert.Equal(0m, k.Seri.Noktalar[5].Deger2);    // 6 Ekim: personel cebinden harcama nakit sayılmaz
    }

    [Fact]
    public async Task Butce_firmayi_suzer_ve_BudgetService_ile_ayni_toplami_verir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var b = (await Servis(f).GetRaporAsync(Istek())).Butce!;

        Assert.Equal(3, b.KalemSayisi);                // 2. firmanın kaydı hariç, firmasız kayıt dahil
        Assert.Equal(2300m, b.Planlanan);
        Assert.Equal(800m, b.Odenen);                  // 500 ödenen + 300 kısmi ödenen
        Assert.Equal(1500m, b.Bekleyen);               // 1000 bekleyen + 500 kısmi kalan
        Assert.Equal(2, b.OdenenKalem);
        Assert.Equal(2, b.BekleyenKalem);
        Assert.Equal(35, b.GerceklesmeYuzdesi);

        var numaralar = new NumaraSerisiService(f.Factory);
        var hesaplar = new BankaHesapService(f.Factory, numaralar, f.Guard, f.ActiveFirm);
        var muhasebe = new MuhasebeService(f.Factory, f.Guard);
        var hareketler = new BankaKasaHareketService(f.Factory, muhasebe, hesaplar, numaralar, f.ActiveFirm, f.Guard);
        var butceServisi = new BudgetService(f.Factory, hareketler, muhasebe, NullLogger<BudgetService>.Instance);
        var referans = await butceServisi.GetAylikOzetAsync(2026, 10, 1);

        Assert.Equal(referans.ToplamOdeme, b.Planlanan);
        Assert.Equal(referans.OdenenToplam, b.Odenen);
        Assert.Equal(referans.BekleyenToplam, b.Bekleyen);
        Assert.Equal(referans.ToplamKayit, b.KalemSayisi);
        Assert.Equal(referans.OdenenKayit, b.OdenenKalem);
        Assert.Equal(referans.BekleyenKayit, b.BekleyenKalem);
    }

    [Fact]
    public async Task Butce_tum_firmalar_modunda_digher_firmalarin_odemelerini_de_toplar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);
        var servis = new YoneticiOzetService(f.Factory, new TumFirmalarSaglayici(), NullLogger<YoneticiOzetService>.Instance);

        var rapor = await servis.GetRaporAsync(Istek());

        Assert.Equal(4, rapor.Butce!.KalemSayisi);     // 2. firmanın 700 TL'lik kaydı da dahil
        Assert.Equal(3000m, rapor.Butce.Planlanan);
        Assert.Equal("Tüm Firmalar", rapor.KapsamAdi);
    }

    [Fact]
    public async Task Personel_puantaj_maas_ve_avans_ozetini_cikarir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var p = (await Servis(f).GetRaporAsync(Istek())).Personel!;

        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(await db.Soforler.CountAsync(s => s.Aktif && !s.IsDeleted), p.AktifPersonel);
        Assert.Equal(3, p.PuantajKaydi);               // Eylül kaydı sayılmaz
        Assert.Equal(1, p.PuantajOnayli);
        Assert.Equal(1, p.PuantajOnayBekleyen);
        Assert.Equal(1, p.PuantajTaslak);
        Assert.Equal(0, p.PuantajReddedilen);
        Assert.Equal(2, p.MaasKaydiPersonel);
        Assert.Equal(3000m, p.MaasOdenecek);
        Assert.Equal(1, p.AvansAdet);                  // iptal edilen avans sayılmaz
        Assert.Equal(500m, p.AvansTutar);
    }

    [Fact]
    public async Task Bu_yil_personel_ozeti_ocaktan_bugune_tum_aylari_kapsar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var p = (await Servis(f).GetRaporAsync(Istek(YoneticiOzetDonemTuru.BuYil))).Personel!;

        Assert.Equal(4, p.PuantajKaydi);               // Ekim'in 3'ü + Eylül'ün 1'i
        Assert.Equal(2, p.MaasKaydiPersonel);          // Eylül+Ekim kayıtları 2 farklı kişiye ait
        Assert.Equal(3500m, p.MaasOdenecek);           // 500 + 1000 + 2000
    }

    [Fact]
    public async Task Arac_ve_yeni_kayitlar_is_gunu_saatine_gore_gun_sinirini_uygular()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var rapor = await Servis(f).GetRaporAsync(Istek());

        var a = rapor.Arac!;
        Assert.Equal(2, a.AktifArac);
        Assert.Equal(2, a.YeniArac);
        Assert.Equal(2, a.MasrafAdet);
        Assert.Equal(500m, a.MasrafTutari);
        Assert.Equal(100m, a.OncekiMasrafTutari);

        var k = rapor.Kayitlar!;
        // 30 Eylül 22:00 UTC = 1 Ekim 01:00 (iş günü) → Ekim'e yazılır; 30 Eylül 20:00 UTC = 30 Eylül 23:00 → yazılmaz.
        Assert.Equal(2, k.YeniCari);
        Assert.Equal(1, k.YeniPersonel);
        Assert.Equal(2, k.GelenEvrak);
        Assert.Equal(1, k.GidenEvrak);
    }

    [Fact]
    public async Task RentACar_yeni_tamamlanan_ve_aktif_kiralamalari_sayar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var r = (await Servis(f).GetRaporAsync(Istek())).RentACar!;

        Assert.Equal(2, r.YeniKiralama);               // iptal edilen sayılmaz
        Assert.Equal(1, r.TamamlananKiralama);
        Assert.Equal(900m, r.TamamlananCiro);
        Assert.Equal(1, r.AktifKiralama);
    }

    [Fact]
    public async Task Ekip_yalniz_adi_bilinen_kullanicilarin_ana_kayit_islemlerini_sayar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var e = (await Servis(f).GetRaporAsync(Istek())).Ekip!;

        // Sistem/boş kullanıcı, detay satırı (FaturaKalem), "Görüntüleme" ve dönem dışı satırlar sayılmaz.
        Assert.Equal(6, e.ToplamIslem);
        Assert.Equal(2, e.AktifKullanici);
        Assert.DoesNotContain(e.Kullanicilar, k => k.KullaniciAdi == "Sistem");

        var ayse = e.Kullanicilar[0];
        Assert.Equal("Ayşe", ayse.KullaniciAdi);
        Assert.Equal(5, ayse.Toplam);
        Assert.Equal(4, ayse.Eklenen);                 // 3 "Added" + 1 "Ekleme"
        Assert.Equal(1, ayse.Guncellenen);
        Assert.Equal(0, ayse.Silinen);

        var mehmet = e.Kullanicilar[1];
        Assert.Equal("Mehmet", mehmet.KullaniciAdi);
        Assert.Equal(1, mehmet.Silinen);

        Assert.Equal(new[] { ("Fatura", 4), ("Cari", 1), ("Hakediş", 1) },
            e.IsTurleri.Select(t => (t.Etiket, t.Adet)).ToArray());

        Assert.Equal(6, e.SonIslemler.Count);
        var son = e.SonIslemler[0];
        Assert.Equal("Mehmet", son.Kullanici);
        Assert.Equal("Hakediş", son.Etiket);
        Assert.Equal(EkipIslemTuru.Silindi, son.Tur);
        Assert.Equal(new DateTime(2026, 10, 8, 12, 0, 0), son.Zaman);    // 09:00 UTC = 12:00 iş günü saati
        Assert.Equal("Fatura No 5", e.SonIslemler.Single(i => i.Ayrinti != null).Ayrinti);
    }

    [Fact]
    public async Task Dikkat_listesi_kritik_once_gelir_ve_dashboardla_ayni_devir_tanimini_kullanir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var dikkat = (await Servis(f).GetRaporAsync(Istek(kritikBelge: 3))).Dikkat;

        Assert.Equal(
            new[]
            {
                "devir-borc", "kritik-belge",
                "taslak-hakedis", "faturasiz-hakedis", "onaysiz-servis-puantaji", "onay-bekleyen-personel-puantaji",
                "geciken-tahsilat", "geciken-odeme",
                "bekleyen-evrak"
            },
            dikkat.Select(d => d.Anahtar).ToArray());

        var devir = dikkat.Single(d => d.Anahtar == "devir-borc");
        Assert.Equal(DikkatSeviyesi.Kritik, devir.Seviye);
        Assert.Equal(2, devir.Adet);                   // ay başından önceki ödenmemiş 2 kayıt
        Assert.Equal(1000m, devir.Tutar);
        Assert.Equal("1 kayıt 180 günü aştı", devir.Aciklama);

        var taslak = dikkat.Single(d => d.Anahtar == "taslak-hakedis");
        Assert.Equal(2, taslak.Adet);
        Assert.Equal(1300m, taslak.Tutar);

        var faturasiz = dikkat.Single(d => d.Anahtar == "faturasiz-hakedis");
        Assert.Equal(1, faturasiz.Adet);
        Assert.Equal(480m, faturasiz.Tutar);

        Assert.Equal(5, dikkat.Single(d => d.Anahtar == "onaysiz-servis-puantaji").Adet);
        Assert.Equal(1, dikkat.Single(d => d.Anahtar == "onay-bekleyen-personel-puantaji").Adet);

        var tahsilat = dikkat.Single(d => d.Anahtar == "geciken-tahsilat");
        Assert.Equal(1, tahsilat.Adet);
        Assert.Equal(600m, tahsilat.Tutar);
        Assert.Equal(DikkatSeviyesi.Uyari, tahsilat.Seviye);   // 9 gün gecikti (< 30)

        var odeme = dikkat.Single(d => d.Anahtar == "geciken-odeme");
        Assert.Equal(300m, odeme.Tutar);

        Assert.Equal(4, dikkat.Single(d => d.Anahtar == "bekleyen-evrak").Adet);
        Assert.Equal(3, dikkat.Single(d => d.Anahtar == "kritik-belge").Adet);
    }

    [Fact]
    public async Task Kapsam_disindaki_bolumler_hic_uretilmez()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var yalnizFatura = await Servis(f).GetRaporAsync(Istek(kapsam: YoneticiOzetKapsami.Hicbiri with { Fatura = true }, kritikBelge: 3));

        Assert.NotNull(yalnizFatura.Fatura);
        Assert.Null(yalnizFatura.Servis);
        Assert.Null(yalnizFatura.Hakedis);
        Assert.Null(yalnizFatura.Kasa);
        Assert.Null(yalnizFatura.Butce);
        Assert.Null(yalnizFatura.Personel);
        Assert.Null(yalnizFatura.Arac);
        Assert.Null(yalnizFatura.Kayitlar);
        Assert.Null(yalnizFatura.RentACar);
        Assert.Null(yalnizFatura.Ekip);
        Assert.Equal(new[] { "geciken-tahsilat", "geciken-odeme" }, yalnizFatura.Dikkat.Select(d => d.Anahtar).ToArray());

        var hicbiri = await Servis(f).GetRaporAsync(Istek(kapsam: YoneticiOzetKapsami.Hicbiri, kritikBelge: 3));
        Assert.Empty(hicbiri.Dikkat);
        Assert.Empty(hicbiri.Hatalar);
        Assert.True(YoneticiOzetMetni.Bos(hicbiri));
    }

    [Fact]
    public async Task Bos_veritabaninda_tum_bolumler_sifirla_doner_ve_hata_vermez()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();

        var rapor = await Servis(f).GetRaporAsync(Istek());

        Assert.Empty(rapor.Hatalar);
        Assert.NotNull(rapor.Servis);
        Assert.True(rapor.Servis!.Bos);
        Assert.True(rapor.Hakedis!.Bos);
        Assert.True(rapor.Fatura!.Bos);
        Assert.True(rapor.Kasa!.Bos);
        Assert.True(rapor.Butce!.Bos);
        Assert.Equal(0, rapor.Personel!.PuantajKaydi);
        Assert.Equal(new[] { YoneticiOzetMetni.BosMesaj }, YoneticiOzetMetni.Olustur(rapor).ToArray());
        Assert.Equal("Aktif firma", rapor.KapsamAdi);
    }

    [Fact]
    public async Task Gelecekteki_ozel_aralik_hata_vermeden_bos_rapor_doner()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var istek = new YoneticiOzetIstegi
        {
            DonemTuru = YoneticiOzetDonemTuru.Ozel,
            OzelBaslangic = new DateTime(2026, 11, 1),
            OzelBitis = new DateTime(2026, 11, 30),
            Bugun = Bugun
        };

        var rapor = await Servis(f).GetRaporAsync(istek);

        Assert.Empty(rapor.Hatalar);
        Assert.True(rapor.Servis!.Bos);
        Assert.True(rapor.Fatura!.Bos);
        Assert.True(rapor.Kasa!.Bos);
        Assert.Equal(0, rapor.Personel!.PuantajKaydi);
        Assert.Equal(0, rapor.Hakedis!.UretilenAdet);
    }

    [Fact]
    public async Task Bir_bolum_hata_verirse_rapor_yine_doner_ve_bolum_adi_hatalara_yazilir()
    {
        var servis = new YoneticiOzetService(new PatlayanFabrika(), new TumFirmalarSaglayici(), NullLogger<YoneticiOzetService>.Instance);

        var rapor = await servis.GetRaporAsync(Istek());

        Assert.Null(rapor.Servis);
        Assert.Null(rapor.Fatura);
        Assert.Null(rapor.Ekip);
        Assert.Empty(rapor.Dikkat);
        Assert.Equal(
            new[]
            {
                "Servis operasyonu", "Hakediş", "Faturalar", "Banka/Kasa", "Bütçe", "Personel ve maaş",
                "Araç", "Yeni kayıtlar", "Rent a Car", "Ekip hareketleri", "Dikkat gerektirenler"
            },
            rapor.Hatalar.ToArray());
    }

    [Fact]
    public async Task Iptal_istegi_hata_olarak_yutulmaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        using var iptal = new CancellationTokenSource();
        await iptal.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Servis(f).GetRaporAsync(Istek(), iptal.Token));
    }

    // ---- tohum veri ----

    internal static async Task TohumlaAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = factory.CreateDbContext();

        // Fixture'ın kendi personeli şimdiki zaman damgalı gelir; yeni-kayıt sayımları gerçek saatten etkilenmesin.
        await db.Soforler.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAt, Eski));

        db.Firmalar.Add(new Firma { Id = 2, FirmaKodu = "F2", FirmaAdi = "Diğer Firma" });

        Cari YeniCari(string kod, DateTime olusturma) => new()
        {
            CariKodu = kod, Unvan = "Cari " + kod, CariTipi = CariTipi.Musteri, FirmaId = 1, CreatedAt = olusturma
        };
        var cari = YeniCari("A", Utc(2026, 9, 1));
        db.Cariler.Add(cari);
        db.AddRange(
            YeniCari("N1", Utc(2026, 9, 30, 22)),   // iş gününde 1 Ekim 01:00 → Ekim
            YeniCari("N2", Utc(2026, 10, 10)),
            YeniCari("N3", Utc(2026, 9, 30, 20)));  // iş gününde 30 Eylül 23:00 → Eylül
        await db.SaveChangesAsync();

        var guzergah = new Guzergah { GuzergahKodu = "G1", GuzergahAdi = "Rota", CariId = cari.Id, FirmaId = 1 };
        Arac YeniArac(string sase, string plaka, bool aktif, DateTime olusturma) => new()
        {
            SaseNo = sase, AktifPlaka = plaka, SahiplikTipi = AracSahiplikTipi.Ozmal, FirmaId = 1, Aktif = aktif, CreatedAt = olusturma
        };
        var aracA = YeniArac("S1", "34 AAA 001", true, Utc(2026, 10, 2));
        var aracB = YeniArac("S2", "34 BBB 002", true, Utc(2026, 10, 2));
        var aracPasif = YeniArac("S3", "34 CCC 003", false, Utc(2026, 9, 1));
        Sofor YeniSofor(string kod, string ad, bool aktif, DateTime olusturma) => new()
        {
            SoforKodu = kod, Ad = ad, Soyad = "Test", FirmaId = 1, Aktif = aktif, CreatedAt = olusturma
        };
        var s1 = YeniSofor("P1", "Ali", true, Utc(2026, 10, 3));
        var s2 = YeniSofor("P2", "Veli", true, Utc(2026, 9, 1));
        var s3 = YeniSofor("P3", "Can", true, Utc(2026, 9, 1));
        var sPasif = YeniSofor("P4", "Eski", false, Utc(2026, 9, 1));
        db.AddRange(guzergah, aracA, aracB, aracPasif, s1, s2, s3, sPasif);
        await db.SaveChangesAsync();

        // --- servis puantajı ---
        FiloGunlukPuantaj P(DateTime tarih, int arac, decimal sefer, decimal kurum, decimal taseron,
            OperasyonDurumu durum = OperasyonDurumu.Gitti, bool onay = false) => new()
        {
            FirmaId = 1, Tarih = tarih, KurumFirmaId = cari.Id, GuzergahId = guzergah.Id, AracId = arac, SoforId = s1.Id,
            SeferSayisi = sefer, TahakkukEdenKurumUcreti = kurum, TahakkukEdenTaseronUcreti = taseron, Durum = durum, Onaylandi = onay
        };
        db.FiloGunlukPuantajlar.AddRange(
            P(new DateTime(2026, 10, 1), aracA.Id, 2, 200, 50, onay: true),
            P(new DateTime(2026, 10, 2), aracA.Id, 1, 100, 0),
            P(new DateTime(2026, 10, 5), aracA.Id, 1, 0, 0, OperasyonDurumu.Gitmedi_Mazeretli),
            P(new DateTime(2026, 10, 6), aracB.Id, 1, 100, 0, OperasyonDurumu.Arizalandi_YoldaKaldi),
            P(new DateTime(2026, 10, 25), aracA.Id, 3, 300, 0),                  // planlı (bugünden sonra)
            P(new DateTime(2026, 9, 1), aracA.Id, 4, 400, 0),                    // karşılaştırma penceresinde
            P(new DateTime(2026, 9, 25), aracA.Id, 5, 500, 0));                  // pencerenin dışında

        // --- hakediş ---
        var referans = 100;
        Hakedis H(HakedisTipi tip, HakedisDurum durum, decimal genel, DateTime olusturma, DateTime? onay = null) => new()
        {
            FirmaId = 1, Yil = 2026, Ay = 10, Tip = tip, Durum = durum, ReferansId = referans++,
            GenelToplam = genel, Tutar = Math.Round(genel / 1.2m, 2), CreatedAt = olusturma, OnayTarihi = onay
        };
        db.Hakedisler.AddRange(
            H(HakedisTipi.Kurum, HakedisDurum.Taslak, 1200, Utc(2026, 10, 18)),
            H(HakedisTipi.Tedarikci, HakedisDurum.Onaylandi, 480, Utc(2026, 10, 10), Utc(2026, 10, 17)),
            H(HakedisTipi.Kurum, HakedisDurum.Iptal, 999, Utc(2026, 10, 12)),
            H(HakedisTipi.Kurum, HakedisDurum.Faturalandi, 600, Utc(2026, 9, 10)),
            H(HakedisTipi.Arac, HakedisDurum.Taslak, 100, Utc(2026, 10, 11)));

        // --- fatura ---
        Fatura F(string no, DateTime tarih, FaturaYonu yon, FaturaTipi tip, decimal genel, decimal odenen,
            FaturaDurum durum, DateTime? vade = null) => new()
        {
            FaturaNo = no, FaturaTarihi = tarih, FaturaYonu = yon, FaturaTipi = tip, GenelToplam = genel, OdenenTutar = odenen,
            Durum = durum, VadeTarihi = vade, CariId = cari.Id, FirmaId = 1
        };
        db.Faturalar.AddRange(
            F("F1", new DateTime(2026, 10, 3), FaturaYonu.Giden, FaturaTipi.SatisFaturasi, 1200, 600, FaturaDurum.KismiOdendi, new DateTime(2026, 10, 10)),
            F("F2", new DateTime(2026, 10, 10), FaturaYonu.Giden, FaturaTipi.SatisFaturasi, 800, 800, FaturaDurum.Odendi),
            F("F3", new DateTime(2026, 10, 5), FaturaYonu.Gelen, FaturaTipi.AlisFaturasi, 500, 0, FaturaDurum.Beklemede, new DateTime(2026, 10, 30)),
            F("F4", new DateTime(2026, 10, 7), FaturaYonu.Giden, FaturaTipi.SatisIadeFaturasi, 100, 0, FaturaDurum.Beklemede),
            F("F5", new DateTime(2026, 10, 8), FaturaYonu.Giden, FaturaTipi.SatisFaturasi, 999, 0, FaturaDurum.IptalEdildi),
            F("F6", new DateTime(2026, 9, 3), FaturaYonu.Giden, FaturaTipi.SatisFaturasi, 1000, 1000, FaturaDurum.Odendi),
            F("F7", new DateTime(2026, 10, 1), FaturaYonu.Gelen, FaturaTipi.AlisFaturasi, 300, 0, FaturaDurum.Beklemede, new DateTime(2026, 10, 15)));

        // --- banka / kasa ---
        var hesap = new BankaHesap { HesapKodu = "KASA", HesapAdi = "Kasa", FirmaId = 1, HesapTipi = HesapTipi.Kasa };
        db.BankaHesaplari.Add(hesap);
        await db.SaveChangesAsync();
        BankaKasaHareket K(string no, DateTime tarih, HareketTipi tip, decimal tutar, IslemKaynak kaynak = IslemKaynak.Manuel) => new()
        {
            FirmaId = 1, BankaHesapId = hesap.Id, IslemNo = no, IslemTarihi = tarih, HareketTipi = tip, Tutar = tutar, IslemKaynak = kaynak
        };
        db.BankaKasaHareketleri.AddRange(
            K("K1", new DateTime(2026, 10, 3), HareketTipi.Giris, 1000),
            K("K2", new DateTime(2026, 10, 4), HareketTipi.Cikis, 400),
            K("K3", new DateTime(2026, 10, 5), HareketTipi.Giris, 200, IslemKaynak.Mahsup),
            K("K4", new DateTime(2026, 10, 6), HareketTipi.Cikis, 100, IslemKaynak.PersonelCebinden),
            K("K5", new DateTime(2026, 9, 5), HareketTipi.Giris, 500),
            K("K6", new DateTime(2026, 10, 20), HareketTipi.Cikis, 50));          // bugünden sonra

        // --- bütçe ---
        BudgetOdeme B(DateTime tarih, decimal miktar, OdemeDurum durum, int? firma, decimal? odenen = null, decimal kismi = 0) => new()
        {
            OdemeTarihi = tarih, OdemeAy = tarih.Month, OdemeYil = tarih.Year, MasrafKalemi = "Kira", Miktar = miktar,
            Durum = durum, FirmaId = firma, OdenenTutar = odenen, ToplamKismiOdenen = kismi
        };
        db.BudgetOdemeler.AddRange(
            B(new DateTime(2026, 10, 10), 1000, OdemeDurum.Bekliyor, 1),
            B(new DateTime(2026, 10, 12), 500, OdemeDurum.Odendi, 1, odenen: 500),
            B(new DateTime(2026, 10, 15), 800, OdemeDurum.KismiOdendi, null, odenen: 300, kismi: 300),
            B(new DateTime(2026, 10, 20), 700, OdemeDurum.Bekliyor, 2),
            B(new DateTime(2026, 9, 5), 700, OdemeDurum.Bekliyor, 1),             // devir
            B(new DateTime(2026, 3, 1), 300, OdemeDurum.Bekliyor, 1));            // devir, 180 günü aştı

        // --- personel ---
        PersonelPuantaj PP(int personel, int ay, PersonelPuantajOnayDurumu onay) => new()
        {
            FirmaId = 1, PersonelId = personel, Yil = 2026, Ay = ay, OnayDurumu = onay
        };
        db.PersonelPuantajlar.AddRange(
            PP(s1.Id, 10, PersonelPuantajOnayDurumu.Onaylandi),
            PP(s2.Id, 10, PersonelPuantajOnayDurumu.OnayBekliyor),
            PP(s3.Id, 10, PersonelPuantajOnayDurumu.Taslak),
            PP(s1.Id, 9, PersonelPuantajOnayDurumu.Onaylandi));
        MaasOdemeSnapshot MS(int personel, string ad, int ay, decimal odenecek) => new()
        {
            FirmaId = 1, Yil = 2026, Ay = ay, PersonelId = personel, PersonelAdSoyad = ad, Odenecek = odenecek
        };
        db.MaasOdemeSnapshotlar.AddRange(
            MS(s1.Id, "Ali Test", 10, 1000),
            MS(s2.Id, "Veli Test", 10, 2000),
            MS(s1.Id, "Ali Test", 9, 500));
        PersonelAvans AV(int personel, DateTime tarih, decimal tutar, AvansDurum durum) => new()
        {
            PersonelId = personel, FirmaId = 1, AvansTarihi = tarih, Tutar = tutar, Durum = durum
        };
        db.PersonelAvanslar.AddRange(
            AV(s1.Id, new DateTime(2026, 10, 8), 500, AvansDurum.Verildi),
            AV(s2.Id, new DateTime(2026, 10, 9), 300, AvansDurum.IptalEdildi));

        // --- araç masrafı ---
        var kalem = new MasrafKalemi { MasrafKodu = "YKT", MasrafAdi = "Yakıt", Kategori = MasrafKategori.Yakit, FirmaId = 1 };
        db.MasrafKalemleri.Add(kalem);
        await db.SaveChangesAsync();
        AracMasraf M(DateTime tarih, decimal tutar) => new()
        {
            FirmaId = 1, AracId = aracA.Id, MasrafKalemiId = kalem.Id, MasrafTarihi = tarih, Tutar = tutar
        };
        db.AracMasraflari.AddRange(
            M(new DateTime(2026, 10, 4), 300),
            M(new DateTime(2026, 10, 12), 200),
            M(new DateTime(2026, 9, 10), 100));

        // --- evrak ---
        EbysEvrak E(string no, EvrakYonu yon, DateTime kayit) => new() { EvrakNo = no, Konu = "Konu", Yon = yon, KayitTarihi = kayit };
        db.EbysEvraklar.AddRange(
            E("E1", EvrakYonu.Gelen, Utc(2026, 10, 2)),
            E("E2", EvrakYonu.Gelen, Utc(2026, 10, 3)),
            E("E3", EvrakYonu.Giden, Utc(2026, 10, 4)),
            E("E4", EvrakYonu.Gelen, Utc(2026, 9, 5)));

        // --- rent a car ---
        MusteriKiralama R(KiralamaDurumu durum, DateTime olusturma, decimal tutar, DateTime? gercekBitis = null) => new()
        {
            FirmaId = 1, MusteriId = cari.Id, AracId = aracA.Id, BaslangicTarihi = new DateTime(2026, 10, 1),
            PlanlananBitisTarihi = new DateTime(2026, 10, 20), Durum = durum, CreatedAt = olusturma,
            ToplamTutar = tutar, GercekBitisTarihi = gercekBitis
        };
        db.MusteriKiralamalar.AddRange(
            R(KiralamaDurumu.Tamamlandi, Utc(2026, 10, 2), 900, new DateTime(2026, 10, 12)),
            R(KiralamaDurumu.Aktif, Utc(2026, 10, 15), 400),
            R(KiralamaDurumu.IptalEdildi, Utc(2026, 10, 3), 200));

        // --- aktivite günlüğü ---
        AktiviteLog L(string? kullanici, string tip, string entity, DateTime zaman, string? adi = null) => new()
        {
            FirmaId = 1, KullaniciAdi = kullanici, IslemTipi = tip, Modul = "Genel", EntityTipi = entity, IslemZamani = zaman, EntityAdi = adi
        };
        db.AktiviteLoglar.AddRange(
            L("Ayşe", "Added", "Fatura", Utc(2026, 10, 5, 10)),
            L("Ayşe", "Added", "Fatura", Utc(2026, 10, 5, 11)),
            L("Ayşe", "Added", "Fatura", Utc(2026, 10, 5, 12), "Fatura No 5"),
            L("Ayşe", "Modified", "Fatura", Utc(2026, 10, 6)),
            L("Ayşe", "Ekleme", "Cari", Utc(2026, 10, 7)),
            L("Mehmet", "Deleted", "Hakedis", Utc(2026, 10, 8)),
            L("Mehmet", "Modified", "FaturaKalem", Utc(2026, 10, 8, 10)),        // detay satırı: sayılmaz
            L("Sistem", "Added", "Fatura", Utc(2026, 10, 8, 11)),                // sistem: sayılmaz
            L(null, "Added", "Fatura", Utc(2026, 10, 8, 12)),                    // kullanıcısız: sayılmaz
            L("Ayşe", "Görüntüleme", "Fatura", Utc(2026, 10, 9)),                // görüntüleme: sayılmaz
            L("Ayşe", "Added", "Fatura", Utc(2026, 9, 15)));                     // dönem dışı

        await db.SaveChangesAsync();
    }

    // ---- test çiftleri ----

    internal sealed class PatlayanFabrika : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => throw new InvalidOperationException("test: bağlantı yok");

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("test: bağlantı yok");
    }

    internal sealed class TumFirmalarSaglayici : IAktifFirmaProvider
    {
        public int? AktifFirmaId => null;
        public bool HasAktifFirma => true;
        public bool TumFirmalar => true;
        public AktifFirmaBilgisi Mevcut { get; } = new() { TumFirmalar = true };
        public event Action? AktifFirmaDegisti { add { } remove { } }
        public void Set(AktifFirmaBilgisi firma) => throw new NotSupportedException();
        public void SetTumFirmalar(bool tumFirmalar) => throw new NotSupportedException();
        public void SetDonem(int yil, int ay) => throw new NotSupportedException();
        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }
}

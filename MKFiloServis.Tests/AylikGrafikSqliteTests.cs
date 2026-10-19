using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

/// <summary>
/// Aylık grafik raporu: yönetici özetiyle aynı tohum veri üzerinde her bölümün aylık serisi, yıl toplamları,
/// geçen yılla karşılaştırma, saat dilimi sınırları, firma süzme, yetki kapsamı ve bölüm hatası dayanıklılığı.
/// </summary>
public sealed class AylikGrafikSqliteTests
{
    // 19 Ekim 2026 Pazartesi (iş günü)
    private static readonly DateTime Bugun = new(2026, 10, 19);

    private static DateTime Utc(int y, int m, int d, int h = 9, int min = 0) => new(y, m, d, h, min, 0, DateTimeKind.Utc);

    private static AylikGrafikIstegi Istek(int yil = 2026, YoneticiOzetKapsami? kapsam = null)
        => new() { Yil = yil, Kapsam = kapsam ?? YoneticiOzetKapsami.Tumu, Bugun = Bugun };

    private static YoneticiOzetService Servis(PayrollWriteSqliteTests.Fixture f)
        => new(f.Factory, f.ActiveFirm, NullLogger<YoneticiOzetService>.Instance);

    private static AylikGrafikKarti Kart(AylikGrafikRaporu r, string anahtar) => r.Kartlar.Single(k => k.Anahtar == anahtar);

    private static decimal[] D1(AylikGrafikKarti k) => k.Seri.Noktalar.Select(n => n.Deger1).ToArray();

    private static decimal[] D2(AylikGrafikKarti k) => k.Seri.Noktalar.Select(n => n.Deger2).ToArray();

    // Eylül = 9. ay (dizin 8), Ekim = 10. ay (dizin 9)
    private static decimal[] Aylar(params (int Ay, decimal Deger)[] degerler)
    {
        var dizi = new decimal[12];
        foreach (var (ay, deger) in degerler)
            dizi[ay - 1] = deger;
        return dizi;
    }

    [Fact]
    public async Task Servis_yapilan_ve_planli_seferi_aylara_dagitir_toplam_bugune_kadardir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var k = Kart(await Servis(f).GetAylikGrafikAsync(Istek()), "servis");

        Assert.Equal(Aylar((9, 9), (10, 4)), D1(k));         // Ekim: 2 + 1 + 1 (arıza dahil, gitmedi hariç)
        Assert.Equal(Aylar((10, 3)), D2(k));                  // 25 Ekim planlı
        Assert.True(k.Yigili);
        Assert.Equal(AylikBirim.Adet, k.Birim);
        Assert.Equal("sefer", k.AdetEki);
        Assert.Equal(13m, k.Toplam1);                         // planlı toplama girmez
        Assert.Equal(0m, k.Toplam2);
        Assert.Equal("Yapılan sefer", k.Seri.Etiket1);
        Assert.Equal("Planlı sefer", k.Seri.Etiket2);
        Assert.False(k.Seri.Noktalar[9].Gelecek);
        Assert.True(k.Seri.Noktalar[10].Gelecek);
        Assert.True(k.Seri.Aylik);
        Assert.Equal(12, k.Seri.Noktalar.Count);
    }

    [Fact]
    public async Task Hakedis_gelir_ve_gideri_aylara_dagitir_ic_ve_iptal_hakedisi_saymaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var k = Kart(await Servis(f).GetAylikGrafikAsync(Istek()), "hakedis");

        Assert.Equal(Aylar((9, 600), (10, 1200)), D1(k));     // gelir: Eylül faturalanan + Ekim taslak
        Assert.Equal(Aylar((10, 480)), D2(k));                // gider: tedarikçi hakedişi
        Assert.Equal(1800m, k.Toplam1);
        Assert.Equal(480m, k.Toplam2);
        Assert.Equal(AylikBirim.Para, k.Birim);
    }

    [Fact]
    public async Task Fatura_kesilen_ve_alinani_aylara_dagitir_iade_ve_iptali_saymaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var k = Kart(await Servis(f).GetAylikGrafikAsync(Istek()), "fatura");

        Assert.Equal(Aylar((9, 1000), (10, 2000)), D1(k));    // kesilen: 1200 + 800 (iade ve iptal hariç)
        Assert.Equal(Aylar((10, 800)), D2(k));                // alınan: 500 + 300
        Assert.Equal(3000m, k.Toplam1);
        Assert.Equal(800m, k.Toplam2);
    }

    [Fact]
    public async Task Kasa_hesaplar_arasi_aktarimi_personel_cebinden_harcamayi_ve_gelecek_tarihli_hareketi_saymaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var k = Kart(await Servis(f).GetAylikGrafikAsync(Istek()), "kasa");

        Assert.Equal(Aylar((9, 500), (10, 1000)), D1(k));     // tahsilat
        Assert.Equal(Aylar((10, 400)), D2(k));                // ödeme (20 Ekim'deki 50 TL gelecekte: sayılmaz)
        Assert.Equal(1500m, k.Toplam1);
        Assert.Equal(400m, k.Toplam2);
    }

    [Fact]
    public async Task Butce_firmayi_suzer_planlanan_ileri_aylara_uzanir_toplam_bugune_kadardir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var k = Kart(await Servis(f).GetAylikGrafikAsync(Istek()), "butce");

        // Ekim: 1000 bekleyen + 500 ödenen + 800 kısmi (firmasız kayıt dahil); 2. firmanın 700 TL'si hariç.
        Assert.Equal(Aylar((3, 300), (9, 700), (10, 2300)), D1(k));
        Assert.Equal(Aylar((10, 800)), D2(k));                // 500 ödenen + 300 kısmi ödenen
        Assert.Equal(3300m, k.Toplam1);
        Assert.Equal(800m, k.Toplam2);
    }

    [Fact]
    public async Task Butce_tum_firmalar_modunda_diger_firmalarin_odemelerini_de_gosterir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);
        var servis = new YoneticiOzetService(f.Factory, new YoneticiOzetSqliteTests.TumFirmalarSaglayici(), NullLogger<YoneticiOzetService>.Instance);

        var rapor = await servis.GetAylikGrafikAsync(Istek());
        var k = Kart(rapor, "butce");

        Assert.Equal(3000m, k.Seri.Noktalar[9].Deger1);       // 2. firmanın 20 Ekim'deki 700 TL'si de grafikte
        Assert.Equal(3300m, k.Toplam1);                       // ama 20 Ekim bugünden sonra: toplama girmez
        Assert.Equal("Tüm Firmalar", rapor.KapsamAdi);
    }

    [Fact]
    public async Task Personel_maas_donem_kaydini_ve_avansi_aylara_dagitir_iptal_edilen_avansi_saymaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var k = Kart(await Servis(f).GetAylikGrafikAsync(Istek()), "personel");

        Assert.Equal(Aylar((9, 500), (10, 3000)), D1(k));     // snapshot Odenecek: 1000 + 2000 (Ekim), 500 (Eylül)
        Assert.Equal(Aylar((10, 500)), D2(k));                // avans 500 (iptal edilen 300 hariç)
        Assert.Equal(3500m, k.Toplam1);
        Assert.Equal(500m, k.Toplam2);
    }

    [Fact]
    public async Task Arac_masrafi_ve_rent_a_car_cirosu_aylara_dagilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var rapor = await Servis(f).GetAylikGrafikAsync(Istek());

        var masraf = Kart(rapor, "arac");
        Assert.Equal(Aylar((9, 100), (10, 500)), D1(masraf));
        Assert.Equal(600m, masraf.Toplam1);
        Assert.False(masraf.IkiSeri);

        var rent = Kart(rapor, "rentacar");
        Assert.Equal(Aylar((10, 900)), D1(rent));             // yalnız tamamlanan kiralama, gerçek bitiş ayına
        Assert.Equal(900m, rent.Toplam1);
    }

    [Fact]
    public async Task Yeni_kayitlar_is_gunu_saatine_gore_aylara_yazilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var k = Kart(await Servis(f).GetAylikGrafikAsync(Istek()), "kayit");

        // Cari: A (1 Eylül), N3 (30 Eylül 23:00 iş günü) → Eylül; N1 (30 Eylül 22:00 UTC = 1 Ekim 01:00), N2 → Ekim.
        Assert.Equal(Aylar((9, 2), (10, 2)), D1(k));
        // Personel: 3 kişi Eylül'de, 1 kişi Ekim'de açıldı (fixture'ın kendi personeli 2020'ye çekildi).
        Assert.Equal(Aylar((9, 3), (10, 1)), D2(k));
        Assert.Equal("Yeni cari", k.Seri.Etiket1);
        Assert.Equal("Yeni personel", k.Seri.Etiket2);
        Assert.Equal(4m, k.Toplam1);
        Assert.Equal(4m, k.Toplam2);
    }

    [Fact]
    public async Task Evrak_gelen_ve_giden_olarak_aylara_dagilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var k = Kart(await Servis(f).GetAylikGrafikAsync(Istek()), "evrak");

        Assert.Equal(Aylar((9, 1), (10, 2)), D1(k));          // gelen
        Assert.Equal(Aylar((10, 1)), D2(k));                  // giden
        Assert.Equal("evrak", k.AdetEki);
    }

    [Fact]
    public async Task Ekip_yalniz_adi_bilinen_kullanicilarin_ana_kayit_islemlerini_sayar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var k = Kart(await Servis(f).GetAylikGrafikAsync(Istek()), "ekip");

        // Sistem/boş kullanıcı, detay satırı (FaturaKalem) ve "Görüntüleme" sayılmaz.
        Assert.Equal(Aylar((9, 1), (10, 6)), D1(k));
        Assert.Equal(7m, k.Toplam1);
    }

    [Fact]
    public async Task Tum_bolumler_tanimli_sirayla_gelir_ve_hata_olmaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var rapor = await Servis(f).GetAylikGrafikAsync(Istek());

        Assert.Equal(
            new[] { "servis", "hakedis", "fatura", "kasa", "butce", "personel", "arac", "rentacar", "kayit", "evrak", "ekip" },
            rapor.Kartlar.Select(k => k.Anahtar).ToArray());
        Assert.Empty(rapor.Hatalar);
        Assert.False(rapor.Bos);
        Assert.Equal(2026, rapor.Yil);
        Assert.Equal(Bugun, rapor.Bugun);
        Assert.Equal(Bugun, rapor.KesimGunu);
        Assert.Equal("Aktif firma", rapor.KapsamAdi);
        Assert.Equal("1 Ocak – 19 Ekim 2025", rapor.KarsilastirmaMetni);
        Assert.All(rapor.Kartlar, k => Assert.Equal(12, k.Seri.Noktalar.Count));
    }

    [Fact]
    public async Task Gecen_yilin_ayni_donemiyle_karsilastirir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);
        await GecenYilFaturalariAsync(f);

        var k = Kart(await Servis(f).GetAylikGrafikAsync(Istek()), "fatura");

        Assert.Equal(3000m, k.Toplam1);
        Assert.Equal(1500m, k.OncekiYilToplam1);              // 500 (1 Mart) + 1000 (5 Ekim); 5 Kasım 2025 aynı dönemde değil
        Assert.Equal(DegisimYonu.Artti, k.Degisim1.Yon);
        Assert.Equal(100, k.Degisim1.Yuzde);
        Assert.Equal(800m, k.Toplam2);
        Assert.Equal(0m, k.OncekiYilToplam2);
        Assert.Equal(DegisimYonu.Yeni, k.Degisim2.Yon);
        Assert.True(k.KarsilastirmaVar);
        Assert.Equal(0m, k.Seri.Noktalar.Sum(n => n.Deger1) - 3000m);   // geçen yılın kayıtları 2026 grafiğine karışmaz
    }

    [Fact]
    public async Task Gecmis_yil_secilince_yilin_tamami_ve_onceki_yilin_tamami_kullanilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);
        await GecenYilFaturalariAsync(f);

        var rapor = await Servis(f).GetAylikGrafikAsync(Istek(yil: 2025));
        var k = Kart(rapor, "fatura");

        Assert.Equal(Aylar((3, 500), (10, 1000), (11, 5000)), D1(k));
        Assert.Equal(6500m, k.Toplam1);                       // 5 Kasım da dahil: yılın tamamı
        Assert.Equal(0m, k.OncekiYilToplam1);
        Assert.Equal(new DateTime(2025, 12, 31), rapor.KesimGunu);
        Assert.All(k.Seri.Noktalar, n => Assert.False(n.Gelecek));
        Assert.Equal("1 Ocak – 31 Aralık 2024", rapor.KarsilastirmaMetni);
    }

    [Fact]
    public async Task Gelecek_yilda_yalniz_planli_butce_gorunur_karsilastirma_yoktur()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);
        await using (var db = f.Factory.CreateDbContext())
        {
            db.BudgetOdemeler.Add(new BudgetOdeme
            {
                OdemeTarihi = new DateTime(2027, 2, 10), OdemeAy = 2, OdemeYil = 2027, MasrafKalemi = "Kira",
                Miktar = 1500, Durum = OdemeDurum.Bekliyor, FirmaId = 1
            });
            await db.SaveChangesAsync();
        }

        var rapor = await Servis(f).GetAylikGrafikAsync(Istek(yil: 2027));

        Assert.Null(rapor.KesimGunu);
        Assert.Equal(string.Empty, rapor.KarsilastirmaMetni);
        var butce = Kart(rapor, "butce");
        Assert.Equal(Aylar((2, 1500)), D1(butce));
        Assert.Equal(0m, butce.Toplam1);
        Assert.False(butce.KarsilastirmaVar);
        Assert.All(butce.Seri.Noktalar, n => Assert.True(n.Gelecek));
        Assert.True(Kart(rapor, "fatura").Bos);
        Assert.True(Kart(rapor, "servis").Bos);
    }

    [Fact]
    public async Task Yil_sinirin_disindaysa_kirpilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();

        Assert.Equal(2022, (await Servis(f).GetAylikGrafikAsync(Istek(yil: 1900))).Yil);
        Assert.Equal(2027, (await Servis(f).GetAylikGrafikAsync(Istek(yil: 3000))).Yil);
        Assert.Equal(2026, (await Servis(f).GetAylikGrafikAsync(new AylikGrafikIstegi { Bugun = Bugun })).Yil);
    }

    [Fact]
    public async Task Yil_basi_sinirinda_is_gunu_saatine_gore_ocak_ayina_yazilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);
        await using (var db = f.Factory.CreateDbContext())
        {
            db.Cariler.AddRange(
                new Cari { CariKodu = "Y1", Unvan = "Yıl başı 1", CariTipi = CariTipi.Musteri, FirmaId = 1, CreatedAt = Utc(2025, 12, 31, 22) },   // 1 Ocak 01:00
                new Cari { CariKodu = "Y2", Unvan = "Yıl sonu", CariTipi = CariTipi.Musteri, FirmaId = 1, CreatedAt = Utc(2025, 12, 31, 20) });    // 31 Aralık 23:00
            await db.SaveChangesAsync();
        }

        var k2026 = Kart(await Servis(f).GetAylikGrafikAsync(Istek(2026)), "kayit");
        Assert.Equal(1m, k2026.Seri.Noktalar[0].Deger1);      // Ocak 2026
        var k2025 = Kart(await Servis(f).GetAylikGrafikAsync(Istek(2025)), "kayit");
        Assert.Equal(1m, k2025.Seri.Noktalar[11].Deger1);     // Aralık 2025
    }

    [Fact]
    public async Task Kapsam_disindaki_bolumler_hic_uretilmez()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var yalnizFatura = await Servis(f).GetAylikGrafikAsync(Istek(kapsam: YoneticiOzetKapsami.Hicbiri with { Fatura = true }));
        Assert.Equal(new[] { "fatura" }, yalnizFatura.Kartlar.Select(k => k.Anahtar).ToArray());

        var finans = await Servis(f).GetAylikGrafikAsync(Istek(kapsam: YoneticiOzetKapsami.Hicbiri with { Finans = true }));
        Assert.Equal(new[] { "kasa", "butce" }, finans.Kartlar.Select(k => k.Anahtar).ToArray());

        var hicbiri = await Servis(f).GetAylikGrafikAsync(Istek(kapsam: YoneticiOzetKapsami.Hicbiri));
        Assert.Empty(hicbiri.Kartlar);
        Assert.Empty(hicbiri.Hatalar);
    }

    [Fact]
    public async Task Yeni_kayit_karti_yetkiye_gore_tek_ya_da_iki_seri_olur()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await YoneticiOzetSqliteTests.TohumlaAsync(f.Factory);

        var yalnizCari = Kart(await Servis(f).GetAylikGrafikAsync(Istek(kapsam: YoneticiOzetKapsami.Hicbiri with { Cari = true })), "kayit");
        Assert.False(yalnizCari.IkiSeri);
        Assert.Equal("Yeni cari", yalnizCari.Seri.Etiket1);
        Assert.Equal(Aylar((9, 2), (10, 2)), D1(yalnizCari));
        Assert.Equal("/cariler", yalnizCari.Link);

        var yalnizPersonel = Kart(await Servis(f).GetAylikGrafikAsync(Istek(kapsam: YoneticiOzetKapsami.Hicbiri with { Personel = true })), "kayit");
        Assert.Equal("kayit", yalnizPersonel.Anahtar);
        Assert.False(yalnizPersonel.IkiSeri);
        Assert.Equal("Yeni personel", yalnizPersonel.Seri.Etiket1);
        Assert.Equal(Aylar((9, 3), (10, 1)), D1(yalnizPersonel));
        Assert.Equal("/soforler", yalnizPersonel.Link);
    }

    [Fact]
    public async Task Bos_veritabaninda_tum_kartlar_bos_doner_ve_hata_vermez()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await using (var db = f.Factory.CreateDbContext())
        {
            // Fixture'ın kendi personeli gerçek saat damgalı gelir; "yeni kayıt" kartı gerçek saatten etkilenmesin.
            await db.Soforler.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAt, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        var rapor = await Servis(f).GetAylikGrafikAsync(Istek());

        Assert.Empty(rapor.Hatalar);
        Assert.Equal(11, rapor.Kartlar.Count);
        Assert.True(rapor.Bos);
        Assert.All(rapor.Kartlar, k => Assert.True(k.Bos, k.Anahtar));
        Assert.All(rapor.Kartlar, k => Assert.Equal(DegisimYonu.Yok, k.Degisim1.Yon));
    }

    [Fact]
    public async Task Bir_bolum_hata_verirse_rapor_yine_doner_ve_bolum_adi_hatalara_yazilir()
    {
        var servis = new YoneticiOzetService(
            new YoneticiOzetSqliteTests.PatlayanFabrika(), new YoneticiOzetSqliteTests.TumFirmalarSaglayici(), NullLogger<YoneticiOzetService>.Instance);

        var rapor = await servis.GetAylikGrafikAsync(Istek());

        Assert.Empty(rapor.Kartlar);
        Assert.Equal(
            new[]
            {
                "Servis operasyonu", "Hakediş", "Faturalar", "Banka/Kasa", "Bütçe", "Personel ve maaş",
                "Araç masrafları", "Rent a Car", "Yeni kayıtlar", "Evrak", "Ekip hareketleri"
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
            () => Servis(f).GetAylikGrafikAsync(Istek(), iptal.Token));
    }

    // ---- yardımcılar ----

    // Geçen yıl: 1 Mart (500) ve 5 Ekim (1000) 19 Ekim'e kadar; 5 Kasım (5000) aynı dönemin dışında.
    private static async Task GecenYilFaturalariAsync(PayrollWriteSqliteTests.Fixture f)
    {
        await using var db = f.Factory.CreateDbContext();
        var cari = await db.Cariler.FirstAsync();
        Fatura F(string no, DateTime tarih, decimal tutar) => new()
        {
            FaturaNo = no, FaturaTarihi = tarih, FaturaYonu = FaturaYonu.Giden, FaturaTipi = FaturaTipi.SatisFaturasi,
            GenelToplam = tutar, OdenenTutar = 0, Durum = FaturaDurum.Beklemede, CariId = cari.Id, FirmaId = 1
        };
        db.Faturalar.AddRange(
            F("G1", new DateTime(2025, 3, 1), 500),
            F("G2", new DateTime(2025, 10, 5), 1000),
            F("G3", new DateTime(2025, 11, 5), 5000));
        await db.SaveChangesAsync();
    }
}
